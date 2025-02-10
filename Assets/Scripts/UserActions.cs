using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class UserActions: MonoBehaviour
{
    enum GameState
    {
        Idle,
        JoiningRoom,
        WaitingForPlayersReady,
        WaitingForPlayerActions,
        ResolvingActions,
    }

    private DatabaseReference databaseReference;
    private DatabaseReference roundsRef;
    private DatabaseReference isRoundReadyRef;
    private DatabaseReference battlePairsRef;
    private FirebaseAPIFetch firebaseFetchAPI;
    private FirebaseWriteAPI firebaseWriteAPI;
    private BubbleBattleAction playerRoundAction = BubbleBattleAction.NoAction;
    private GameState gameState = GameState.Idle;
    private string currentPlayerId;
    private string currentRoomId;
    
    public int CurrentRoundId { get; private set; } = 0;
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;
    public GameObject createJoinPanel; 
    public GameObject lobbyPanel; 
    public GameObject actionsPanel; 
    public GameObject winLosePanel; 

    public void Awake()
    {
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
    }

    public void Start()
    {
        OpenCreateJoinRoomPanel();

        InitFirebase().ContinueWithOnMainThread((result) =>
        {
            gameState = GameState.JoiningRoom;
            Debug.Log("Init Firebase with status: " + result.Result);
        });
    }

    // ================================ UI INTERACTIBLE ================================

    public void UserClickedCreateRoom()
    {
        HandleRoomCreationAsync();
    }

    public void UserClickedJoinRoom () 
    {
        HandleRoomJoinAsync();
    }

    public void UserClickedReady () {
        firebaseWriteAPI.UpdateIsReadyForPlayerAsync(currentRoomId, CurrentRoundId, currentPlayerId, true).ContinueWithOnMainThread(task => {
            ObserveIsReady(currentRoomId, CurrentRoundId.ToString());
        });
    }

    public void UserClickedAction(string actionStr)
    {
        if (Enum.TryParse(actionStr, false, out BubbleBattleAction action))
        {
            playerRoundAction = action;
        } 
        else
        {
            Console.WriteLine($"Could not parse action: {action}");
        }
    }

    public void UserClickedLock()
    {
        Debug.Log($"UserClickedLock locking {playerRoundAction}");
        if (playerRoundAction == BubbleBattleAction.NoAction)
        {
            print("No player action selected, while trying to set player action in DB");
            return;
        }

        _ = firebaseWriteAPI.UpdatePlayerAction(currentRoomId, CurrentRoundId, currentPlayerId, playerRoundAction);
    }

    // =====================================================

    public Task<bool> InitFirebase()
    {
        return FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseFetchAPI = GetComponent<FirebaseAPIFetch>();
                firebaseWriteAPI = GetComponent<FirebaseWriteAPI>();

                firebaseFetchAPI.DatabaseReference = databaseReference;
                firebaseWriteAPI.DatabaseReference = databaseReference;
                return true;
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }

            return false;
        });
    }

    private async void HandleRoomCreationAsync()
    {
        try
        {
            // Attempt to create a room asynchronously
            var createRoomResult = await firebaseWriteAPI.CreateRoomAsync();
            currentRoomId = createRoomResult;

            Debug.Log($"Created room with ID: {currentRoomId}");

            // Attempt to join the created room asynchronously
            var joinRoomResult = await firebaseWriteAPI.JoinRoomAsync(currentRoomId, playerNameInput.text, true);
            currentPlayerId = joinRoomResult;

            gameState = GameState.WaitingForPlayersReady;

            Debug.Log($"Joined room with ID: {currentRoomId}, Player ID: {currentPlayerId}");

            // Start observing rounds for the created room
            ObserveRounds(currentRoomId);
            OpenLobbyPanel();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create or join the room: {ex.Message}");
        }
    }

    private async void HandleRoomJoinAsync()
    {
        currentRoomId = roomIdInput.text;
        try
        {
            var joinRoomResult = await firebaseWriteAPI.JoinRoomAsync(currentRoomId, playerNameInput.text, true);
            currentPlayerId = joinRoomResult;

            gameState = GameState.WaitingForPlayersReady;

            Debug.Log($"Room joined with ID: {currentRoomId}, Player ID: {currentPlayerId}");

            // Start observing rounds for the created room
            ObserveRounds(currentRoomId);
            OpenLobbyPanel();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed tojoin the room: {ex.Message}");
        }
    }

    private void ObserveRounds(string roomId)
    {
        Debug.Log($"creating rounds observer");

        if (roundsRef != null)
        {
            roundsRef.ValueChanged -= RoundsObserver;
        }

        roundsRef = databaseReference.Child("rooms").Child(roomId).Child("rounds");

        roundsRef.ValueChanged += RoundsObserver;
    }

    private void RoundsObserver(object _, ValueChangedEventArgs args)
    {
        if (gameState is not GameState.WaitingForPlayersReady) {
            return;
        }

        Debug.Log($"calling rounds observer");
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Failed to observe rounds: {args.DatabaseError.Message}");
            return;
        }

        if (args.Snapshot.Exists)
        {
            // Find the latest round (highest round ID)
            int latestRoundId = -1;
            Dictionary<string, object> latestRoundData = null;

            foreach (var roundSnapshot in args.Snapshot.Children)
            {
                int roundId = int.Parse(roundSnapshot.Key);
                if (roundId > latestRoundId)
                {
                    latestRoundId = roundId;
                    latestRoundData = new Dictionary<string, object>();

                    foreach (var child in roundSnapshot.Children)
                    {
                        latestRoundData[child.Key] = child.Value;
                    }
                }
            }

            CurrentRoundId = latestRoundId;
            Debug.Log($"Current Round ID: {CurrentRoundId}");

            ObserveIsReady(currentRoomId, CurrentRoundId.ToString());
        }
        else
        {
            Debug.LogWarning("No rounds found for this room.");
        }
    }

    private void ObserveIsReady(string roomId, string roundId)
    {
        if (isRoundReadyRef != null)
        {
            isRoundReadyRef.ValueChanged -= IsReadyObserver;
        }

        Debug.Log($"creating isReady observer");
        isRoundReadyRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId).Child("isReady");

        isRoundReadyRef.ValueChanged += IsReadyObserver;
    }
    
    private async void IsReadyObserver(object _, ValueChangedEventArgs args)
    {
        if (gameState is not GameState.WaitingForPlayersReady) {
            return;
        }

        Debug.Log($"calling observer isReady");
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Failed to observe isReady key: {args.DatabaseError.Message}");
            return;
        }

        if (args.Snapshot.Exists)
        {
            bool areAllPlayersReady = true;

            foreach (var isReadySnapshot in args.Snapshot.Children)
            {
                bool isPlayerReady = bool.Parse(isReadySnapshot.Value.ToString());
                if (!isPlayerReady)
                {
                    areAllPlayersReady = false;
                    break;
                }
            }

            if (areAllPlayersReady)
            {
                OpenActionsPanel();
                gameState = GameState.WaitingForPlayerActions;
                Debug.Log("All players are ready.");

                bool isFirstPlayer = await IsCurrentPlayerFirstAsync(currentRoomId, currentPlayerId);

                if (isFirstPlayer)
                {
                    Debug.Log("Handling game logic because the current player is in last place or joined first.");

                    try
                    {
                        await firebaseWriteAPI.CalculateBattlePairs(currentRoomId, CurrentRoundId);
                        Debug.Log("Battle pairs calculated successfully.");

                        ObservePlayerActions(currentRoomId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to calculate battle pairs: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log("Current player is neither in last place nor the first to join.");
                }
            }
            else
            {
                Debug.Log("Not all players are ready.");
            }
        }
        else
        {
            Debug.LogWarning("No rounds found for this room.");
        }
    }

    private void ObservePlayerActions(string roomId)
    {
        if (battlePairsRef != null)
        {
            battlePairsRef.ValueChanged -= PlayerActionsObserver;
        }

        Debug.Log($"creating observer player actions {roomId} - {CurrentRoundId}");

        // battlePairsRef = GetDbReference($"rooms/{roomId}/rounds/{CurrentRoundId}/battlePairs");
        battlePairsRef = databaseReference
            .Child("rooms")
            .Child(roomId)
            .Child("rounds")
            .Child(CurrentRoundId.ToString())
            .Child("battlePairs");

        battlePairsRef.ValueChanged += PlayerActionsObserver;
    }

    private async void PlayerActionsObserver(object _, ValueChangedEventArgs args)
    {
        if (gameState is not GameState.WaitingForPlayerActions) {
            return;
        }

        Debug.Log($"calling observer player actions");

        if (args.DatabaseError != null)
        {
            Debug.LogError($"Failed to observe battle pairs: {args.DatabaseError.Message}");
            return;
        }

        if (args.Snapshot.Exists)
        {
            bool allPlayersHaveAction = true;
            List<string> allPlayers = new List<string>();

            // Fetch all players from the room
            DataSnapshot playersSnapshot = await databaseReference
                .Child("rooms")
                .Child(currentRoomId)
                .Child("players")
                .GetValueAsync();

            if (!playersSnapshot.Exists)
            {
                Debug.LogError("No players found in the room.");
                return;
            }

            foreach (var playerSnapshot in playersSnapshot.Children)
            {
                allPlayers.Add(playerSnapshot.Key);
            }

            // Check actions for all players inside battlePairs
            foreach (var playerSnapshot in args.Snapshot.Children)
            {
                string playerId = playerSnapshot.Key;

                // Ensure that action exists inside the battlePairs structure
                if (!playerSnapshot.Child("action").Exists || string.IsNullOrEmpty(playerSnapshot.Child("action").Value.ToString()))
                {
                    allPlayersHaveAction = false;
                    break;
                }
            }

            if (allPlayersHaveAction)
            {
                gameState = GameState.ResolvingActions;

                Debug.Log("All players have selected an action.");

                bool isFirstPlayer = await IsCurrentPlayerFirstAsync(currentRoomId, currentPlayerId);

                OpenLobbyPanel();
                if (isFirstPlayer)
                {
                    Debug.Log("Current player is in last place or first to join, calculating round score diffs...");

                    // Run CalculateAndSetPlayerRoundScoreDiff for every player
                    List<Task> scoreTasks = new List<Task>();

                    foreach (string playerId in allPlayers)
                    {
                        scoreTasks.Add(firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(currentRoomId, CurrentRoundId, playerId));
                    }

                    try
                    {
                        await Task.WhenAll(scoreTasks);
                        Debug.Log("Successfully calculated and set round score diffs for all players.");

                        gameState = GameState.WaitingForPlayersReady;

                        // Create the next round after updating scores
                        await firebaseWriteAPI.CreateNewRoundAsync(currentRoomId);
                        Debug.Log("Next round created successfully.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error calculating player round score diffs or creating next round: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log("All players have chosen an action, but the current player is neither first nor last.");
                }
            }
            else
            {
                Debug.Log("Not all players have chosen an action yet.");
            }
        }
        else
        {
            Debug.LogWarning("No battlePairs data found for this round.");
        }
    }

    private async Task<bool> IsCurrentPlayerInLastPlaceAsync(string roomId, string roundId, string playerId)
    {
        try
        {
            DataSnapshot snapshot = await databaseReference
                .Child("rooms")
                .Child(roomId)
                .Child("rounds")
                .Child(roundId)
                .Child("scores")
                .GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"Scores for room {roomId}, round {roundId} do not exist.");
                return false;
            }

            Dictionary<string, int> scores = new Dictionary<string, int>();
            foreach (var scoreSnapshot in snapshot.Children)
            {
                string id = scoreSnapshot.Key;
                int score = int.Parse(scoreSnapshot.Value.ToString());
                scores[id] = score;
            }

            // Find the lowest score and check for ties
            int minScore = int.MaxValue;
            int minCount = 0; // Count how many players have the minimum score

            foreach (var score in scores.Values)
            {
                if (score < minScore)
                {
                    minScore = score;
                    minCount = 1; // Reset count to 1 for the new minimum
                }
                else if (score == minScore)
                {
                    minCount++;
                }
            }

            if (minCount > 1)
            {
                Debug.Log("There is a tie for the lowest score. No last player.");
                return false;
            }

            // Check if the provided player is the one with the lowest score
            return scores.ContainsKey(playerId) && scores[playerId] == minScore;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking if the current player is in last place: {ex.Message}");
            return false;
        }
    }


    private async Task<bool> IsCurrentPlayerFirstAsync(string roomId, string currentPlayerId)
    {
        try
        {
            // Fetch players asynchronously
            DataSnapshot snapshot = await databaseReference
                .Child("rooms")
                .Child(roomId)
                .Child("players")
                .GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"Players for room {roomId} do not exist.");
                return false; // No players exist
            }

            // Find the player with the earliest joinTime
            long earliestJoinTime = long.MaxValue;
            string firstPlayerId = null;

            foreach (var playerSnapshot in snapshot.Children)
            {
                string playerId = playerSnapshot.Key;

                // Parse the joinTime value
                if (playerSnapshot.Child("joinTime").Exists)
                {
                    long joinTime = long.Parse(playerSnapshot.Child("joinTime").Value.ToString());

                    if (joinTime < earliestJoinTime)
                    {
                        earliestJoinTime = joinTime;
                        firstPlayerId = playerId;
                    }
                }
            }

            // Check if the current player is the one who joined first
            if (firstPlayerId == currentPlayerId)
            {
                Debug.Log($"Current player ({currentPlayerId}) is the first to join the room.");
                return true;
            }

            Debug.Log($"Current player ({currentPlayerId}) is not the first to join the room.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking if the current player is the first to join: {ex.Message}");
            return false;
        }
    }

    private void OpenCreateJoinRoomPanel()
    {
        lobbyPanel.SetActive(false);
        actionsPanel.SetActive(false);
        createJoinPanel.SetActive(true);
    }

    private void OpenLobbyPanel()
    {
        actionsPanel.SetActive(false);
        createJoinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void OpenActionsPanel()
    {
        lobbyPanel.SetActive(false);
        actionsPanel.SetActive(true);
    }
}
