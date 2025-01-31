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
    private DatabaseReference databaseReference;
    private FirebaseAPIFetch firebaseFetchAPI;
    private FirebaseWriteAPI firebaseWriteAPI;
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;
    private string currentPlayerId = "-OHX_Lic4qwYv-KYnpWj";
    private string currentRoomId = "40625";
    public int CurrentRoundId { get; private set; }  = 1; // Store the current round ID
    public Dictionary<string, object> CurrentRoundData { get; private set; } // Store the current round's data

    public void Awake()
    {
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
    }

    public void Start()
    {
        InitFirebase().ContinueWithOnMainThread((result) =>
        {
            Debug.Log("Init Firebase with status: " + result.Result);
            ObserveRounds(currentRoomId);
            Debug.Log("lianos");
            ObserveIsReady(currentRoomId, CurrentRoundId.ToString());
        });
        
    }
    
    public Task<bool> InitFirebase() {
        return FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase is ready to use.");
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                this.firebaseFetchAPI = this.GetComponent<FirebaseAPIFetch>();
                this.firebaseWriteAPI = this.GetComponent<FirebaseWriteAPI>();

                this.firebaseFetchAPI.DatabaseReference = databaseReference;
                this.firebaseWriteAPI.DatabaseReference = databaseReference;
                return true;
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }

            return false;
        });
    }

    public void UserClickedCreateRoom()
    {
        _ = HandleRoomCreationAsync(); // Fire-and-forget the async task
    }

    private async Task HandleRoomCreationAsync()
    {
        try
        {
            // Attempt to create a room asynchronously
            var createRoomResult = await this.firebaseWriteAPI.CreateRoomAsync();
            currentRoomId = createRoomResult;

            Debug.Log($"Room created with ID: {currentRoomId}");

            // Attempt to join the created room asynchronously
            var joinRoomResult = await firebaseWriteAPI.JoinRoomAsync(createRoomResult, playerNameInput.text, true);
            currentPlayerId = joinRoomResult;

            Debug.Log($"Room joined with ID: {currentRoomId}, Player ID: {currentPlayerId}");

            // Start observing rounds for the created room
            ObserveRounds(currentRoomId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create or join the room: {ex.Message}");
        }
    }

    public void UserClickedJoinRoom () {
        this.firebaseWriteAPI.JoinRoomAsync(roomIdInput.text, playerNameInput.text).ContinueWith(task => {
            if (task.IsCompletedSuccessfully)
            {
                currentRoomId = roomIdInput.text;
                currentPlayerId = task.Result;

                Debug.Log($"Joined room with ID: {currentRoomId}, Player ID: {currentPlayerId}");

                // Start observing rounds for the joined room
                ObserveRounds(currentRoomId);
            }
            else
            {
                Debug.LogError("Failed to join room: " + task.Exception?.Message);
            }
        });
    }

    public void UserClickedReady () {
        this.firebaseWriteAPI.UpdateIsReadyForPlayerAsync(currentRoomId, CurrentRoundId, currentPlayerId, true).ContinueWith(task => {
            ObserveIsReady(currentRoomId, CurrentRoundId.ToString());
        });
    }

    private void ObserveRounds(string roomId)
    {
        Debug.Log($"observer 1" + roomId);

        DatabaseReference roundsRef = databaseReference.Child("rooms").Child(roomId).Child("rounds");

        Debug.Log($"observer 2");
        roundsRef.ValueChanged += RoundsObserver;
    }

    private void RoundsObserver(object _, ValueChangedEventArgs args)
    {
        Debug.Log($"observer 3");
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
            CurrentRoundData = latestRoundData;

            Debug.Log($"Current Round ID: {CurrentRoundId}");
            Debug.Log($"Current Round Data: {CurrentRoundData}");
        }
        else
        {
            Debug.LogWarning("No rounds found for this room.");
        }
    }

    private void ObserveIsReady(string roomId, string roundId)
    {
        DatabaseReference roundsRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId).Child("isReady");

        Debug.Log($"observer isReady before create");
        roundsRef.ValueChanged += IsReadyObserver;
    }

    private async void IsReadyObserver(object _, ValueChangedEventArgs args)
    {
        Debug.Log($"observer isReady created");
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
                // Check the value of each player's isReady key
                bool isPlayerReady = bool.Parse(isReadySnapshot.Value.ToString());
                if (!isPlayerReady)
                {
                    areAllPlayersReady = false;
                    break; // Exit early if any player is not ready
                }
            }

            if (areAllPlayersReady)
            {
                Debug.Log("All players are ready.");

                bool isInLastPlace = await IsCurrentPlayerInLastPlaceAsync(currentRoomId, CurrentRoundId.ToString(), currentPlayerId);
                bool isFirstPlayer = await IsCurrentPlayerFirstAsync(currentRoomId, currentPlayerId);

                if (isInLastPlace || isFirstPlayer)
                {
                    Debug.Log("Handling game logic because the current player is in last place or joined first.");

                    // Handle additional logic (e.g., calculate battle pairs)
                    try
                    {
                        await this.firebaseWriteAPI.CalculateBattlePairs(currentRoomId, CurrentRoundId);
                        Debug.Log("Battle pairs calculated successfully.");
                    }
                    catch (System.Exception ex)
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

    public async Task<bool> IsCurrentPlayerInLastPlaceAsync(string roomId, string roundId, string playerId)
    {
        try
        {
            // Fetch the scores asynchronously
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
                return false; // No scores exist
            }

            // Parse scores into a dictionary
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
                    minCount++; // Increment count if another player has the same minimum score
                }
            }

            // If there's a tie for the lowest score, return false
            if (minCount > 1)
            {
                Debug.Log("There is a tie for the lowest score. No last player.");
                return false;
            }

            // Check if the provided player is the one with the lowest score
            return scores.ContainsKey(playerId) && scores[playerId] == minScore;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error checking if the current player is in last place: {ex.Message}");
            return false;
        }
    }


    public async Task<bool> IsCurrentPlayerFirstAsync(string roomId, string currentPlayerId)
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
        catch (System.Exception ex)
        {
            Debug.LogError($"Error checking if the current player is the first to join: {ex.Message}");
            return false;
        }
    }

}
