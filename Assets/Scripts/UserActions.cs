using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UserActions: MonoBehaviour
{
    /*------------------------------------------------------------------------------------------
     *                                     GAME FLOW OVERVIEW
     *------------------------------------------------------------------------------------------
     * 1. Room Creation:
     *    - The Owner (first player) creates a room.
     *    - Other players can join the room.
     *  
     * 2. "Lobby" Screen:
     *    - Non-owner players choose a name and must press "Ready".
     *    - Owner sees the number of joined players and the number of "Ready" players (y / x players ready).
     *        → If y == x, Owner can press "Start Game"
     *    - The owner also enters their name during this phase.
     *  
     *   ==========> Game Loop <==========
     * 3. Group Screen:
     *    - All players are redirected to the "Group" screen.
     *    - Displays player names, scores, and streaks.
     *    - Matchmaking takes place here.
     *  
     * 4. Action Selection:
     *    - Players are transported to the "Actions" screen.
     *    - They choose and lock in an action.
     *  
     * 5. Resolution Phase:
     *    - Once all "playing" players have locked their actions:
     *        → They are transported to the "Resolution" screen.
     *        → Battle animations play.
     *        → Score calculations are performed.
     *  
     * 6. Looping the Game:
     *    - Players return to the "Group" screen.
     *    - The process repeats from matchmaking onward until the game concludes.
     *------------------------------------------------------------------------------------------*/

    public Button startGameBtn;
    public Button readyBtn;
    public Button lockActionBtn;
    public Button[] actionButtons;
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;
    public TextMeshProUGUI playerReadyCountText;
    public TextMeshProUGUI[] PScores;
    public Toggle firstPlayerToggle;
    // When adding new Panels, make sure to register their deactivation in "CloseOtherPanels"
    public GameObject createJoinPanel;
    public GameObject lobbyPanel;
    public GameObject groupPanel;
    public GameObject actionsPanel;
    public GameObject winLosePanel;

    private const int TIME_UNTIL_FIRST_ROUND_START_SECS = 5;
    private const int TIME_UNTIL_NEXT_ROUND_START_SECS = 15;
    private const int ACTIONS_TIME_LIMIT = 20;
    private DatabaseReference databaseReference;
    private DatabaseReference playerJoinedRef;
    private DatabaseReference readyForGameRef;
    private DatabaseReference startGameRef;
    private DatabaseReference isRoundReadyRef;
    private DatabaseReference battlePairsRef;
    private FirebaseAPIFetch firebaseFetchAPI;
    private FirebaseWriteAPI firebaseWriteAPI;
    private UniqueIDManager idManager;
    private CountdownTimer countdownTimer;
    private ErrorTextDisplay errorTextManager;
    private BubbleManager bubbleManager;
    private GameState gameState = GameState.Idle;
    private BubbleBattleAction playerRoundAction = BubbleBattleAction.NoAction;
    private Dictionary<string, Player> playerDict = new Dictionary<string, Player>();
    private int currentRoundId = 1;
    private string currentPlayerId;
    private string currentRoomId;
    private bool hasLockedAction;

    public void Awake()
    {
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);

        idManager = FindFirstObjectByType<UniqueIDManager>();
        countdownTimer = FindFirstObjectByType<CountdownTimer>();
        errorTextManager = FindFirstObjectByType<ErrorTextDisplay>();
        bubbleManager = FindFirstObjectByType<BubbleManager>();
        currentPlayerId = idManager.GetUniqueDeviceID();
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

    private async void OnApplicationQuit()
    {
        if (Application.isEditor) {
            await CleanupDatabase();
        }
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

    public void UserClickedReadyForGame () {
        _ = firebaseWriteAPI.UpdatePlayerIsReadyForGameAsync(currentRoomId, currentPlayerId, true);
    }

    public void UserClickedStartGame () {
        _ = firebaseWriteAPI.UpdateHasGameStarted(currentRoomId);
    }

    public void UserClickedAction(string actionStr)
    {
        if (Enum.TryParse(actionStr, false, out BubbleBattleAction action))
        {
            playerRoundAction = action;
            DisableAllActionButtonOutlines();
            GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
            clickedButton.GetComponent<Outline>().enabled = true;
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

        hasLockedAction = true;
        MakeActionAndLockButtonsNotInteractible();
        _ = firebaseWriteAPI.UpdatePlayerAction(currentRoomId, currentRoundId, currentPlayerId, playerRoundAction);
    }

    public void UserClickedTestInstantiate()
    {
        bubbleManager.AddPlayer(new Player(Guid.NewGuid().ToString(), "manolis", 1774323423, false), false, true);
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
            var createRoomResult = await firebaseWriteAPI.CreateRoomAsync();
            currentRoomId = createRoomResult;

            Debug.Log($"Created room with ID: {currentRoomId}");

            CreateNewPlayerListObserver(currentRoomId);

            var shouldJoinAsOwner = true;
            if (Application.isEditor) {
                shouldJoinAsOwner = false;
            }
            await firebaseWriteAPI.JoinRoomAsync(currentRoomId, currentPlayerId, playerNameInput.text, shouldJoinAsOwner);

            gameState = GameState.WaitingForPlayersGameReady;

            Debug.Log($"Joined room with ID: {currentRoomId}, Player ID: {currentPlayerId}");

            CreateIsReadyForGameObserver(currentRoomId);
            CreateHasGameStartedObserver(currentRoomId);
            OpenLobbyPanel(shouldJoinAsOwner);
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
            if (Application.isEditor)
            {
                currentPlayerId = Guid.NewGuid().ToString();
            }

            CreateNewPlayerListObserver(currentRoomId);

            var shouldJoinAsOwner = false;
            if (Application.isEditor) {
                shouldJoinAsOwner = (firstPlayerToggle != null) && firstPlayerToggle.isOn;
            }
            bool result = await firebaseWriteAPI.JoinRoomAsync(currentRoomId, currentPlayerId, playerNameInput.text, shouldJoinAsOwner);
            if (!result)
            {
                errorTextManager.ShowError("Could not join room. Make sure the room id is valid.");
                return;
            }

            gameState = GameState.WaitingForPlayersGameReady;

            Debug.Log($"Room joined with ID: {currentRoomId}, Player ID: {currentPlayerId}");

            CreateIsReadyForGameObserver(currentRoomId);
            CreateHasGameStartedObserver(currentRoomId);
            OpenLobbyPanel(shouldJoinAsOwner);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to join the room: {ex.Message}");
        }
    }

    private void CreateNewPlayerListObserver(string roomId)
    {
        Debug.Log($"creating player list observer for roomid: "+roomId);

        if (playerJoinedRef != null)
        {
            playerJoinedRef.ValueChanged -= HandlePlayerListChanged;
        }

        playerJoinedRef = databaseReference.Child("rooms").Child(roomId).Child("players");

        playerJoinedRef.ValueChanged += HandlePlayerListChanged;
    }

    private async void HandlePlayerListChanged(object _, ValueChangedEventArgs args)
    {
        print("Calling HandlePlayerListChanged observer");

        var newDict = await GetPlayerDict(args.Snapshot.Value);
        if (newDict.Count < playerDict.Count) {
            Debug.LogWarning("Players in server are less than local players, disconnect happened");
            //TODO: handle disconnect
        } else {
            // When player first joins the lobby (this seems to be called even before the current player has added himself to the players)
            if (playerDict.Count == 0) {
                print("FIRST JOIN");
                foreach (KeyValuePair<string, Player> entry in newDict) 
                {
                    // Players already in lobby should appear in-position, whearas you should lerp to your position
                    if (entry.Key == currentPlayerId)
                    {
                        print("INSTANTIating current player");
                        bubbleManager.AddPlayer(entry.Value, true, true);
                    } 
                    else 
                    {
                        print("instantiating NON current player");
                        bubbleManager.AddPlayer(entry.Value, false, false);
                    }
                }
            }
            else {
                print("NOT first join");
                var newPlayerKey = newDict.Keys.Except(playerDict.Keys).FirstOrDefault();
                if (newPlayerKey != null) {
                    print("Player {"+newPlayerKey+"} joined");
                    var newPlayer = newDict[newPlayerKey];
                    var isCurrentPlayer = currentPlayerId == newPlayerKey;
                    bubbleManager.AddPlayer(newPlayer, isCurrentPlayer, true);
                }
            }
        }
        playerDict = newDict;
    }

    private void CreateIsReadyForGameObserver(string roomId)
    {
        Debug.Log($"creating is ready for game observer");

        if (readyForGameRef != null)
        {
            readyForGameRef.ValueChanged -= HandleIsReadyForGameChanged;
        }

        readyForGameRef = databaseReference.Child("rooms").Child(roomId).Child("isReadyForGame");

        readyForGameRef.ValueChanged += HandleIsReadyForGameChanged;
    }

    private void HandleIsReadyForGameChanged(object _, ValueChangedEventArgs args)
    {
        print("Calling isReadyForGameChanged observer");
        if (args.Snapshot.Children.Count() == 0) {
            Debug.LogWarning($"No children found under rooms/{currentRoomId}/isReadyForGame");
        }
        int readyPlayers = 0;
        int totalPlayers = 0;
        foreach (var isReadySnapshot in args.Snapshot.Children)
        {
            totalPlayers += 1;
            bool isPlayerReady = bool.Parse(isReadySnapshot.Value.ToString());
            if (isPlayerReady)
            {
                readyPlayers += 1;
            }
        }

        playerReadyCountText.text = readyPlayers.ToString() + "/" + totalPlayers.ToString() + " players are ready";

        if (readyPlayers == totalPlayers) {
            startGameBtn.interactable = true;
            print("All players ready to start the game");
        } else {
            startGameBtn.interactable = false;
            print("NOT all players ready to start the game");
        }
    }

    // TODO this observer is useless for first player
    private void CreateHasGameStartedObserver(string roomId)
    {
        Debug.Log($"creating has game started observer");

        if (startGameRef != null)
        {
            startGameRef.ValueChanged -= HandleHasGameStartedChanged;
        }

        // First-player updates "hasGameStarted" once, by pressing "Start Game"
        startGameRef = databaseReference.Child("rooms").Child(roomId).Child("hasGameStarted");

        startGameRef.ValueChanged += HandleHasGameStartedChanged;
    }

    private async void HandleHasGameStartedChanged(object _, ValueChangedEventArgs args)
    {
        print("hasGameStarted observer early call, gamestate: "+gameState);
        if (gameState is not GameState.WaitingForPlayersGameReady)
        {
            return;
        }

        Debug.Log($"calling has game started observer");

        if (args.Snapshot.Exists && (bool)args.Snapshot.Value)
        {
            // We don't need the observer any more
            if (startGameRef != null)
            {
                startGameRef.ValueChanged -= HandleHasGameStartedChanged;
                startGameRef = null;
            }

            Debug.Log("The game has started");

            OpenGroupPanel();
            CreateIsReadyForRoundObserver(currentRoomId, currentRoundId.ToString());
            gameState = GameState.WaitingForRoundStart;
            print("HandleHasGameStartedChanged countdown");
            countdownTimer.StartCountdown(TIME_UNTIL_FIRST_ROUND_START_SECS, HandleWaitingForRoundStartPhaseEnded);

            print("count of dict keys: "+ playerDict.Keys.Count);
            if (playerDict[currentPlayerId].IsOwner)
            {
                try
                {
                    await firebaseWriteAPI.CalculateBattlePairs(currentRoomId, currentRoundId);
                    Debug.Log("Battle pairs calculated successfully.");

                    CreatePlayerActionsObserver(currentRoomId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to calculate battle pairs: {ex.Message}");
                }
            }
        }
    }

    private async void HandleWaitingForRoundStartPhaseEnded()
    {
        if (gameState is not GameState.WaitingForRoundStart) {
            return;
        }

        OpenActionsPanel();
        gameState = GameState.WaitingForPlayerActions;
        Debug.Log("Next round has started.");
        print("HandleWaitingForRoundStartPhaseEnded countdown");
        countdownTimer.StartCountdown(ACTIONS_TIME_LIMIT, HandleActionsCountdownFinished);
        if (playerDict[currentPlayerId].IsOwner)
        {
            try
            {
                await firebaseWriteAPI.CalculateBattlePairs(currentRoomId, currentRoundId);
                Debug.Log("Battle pairs calculated successfully.");

                CreatePlayerActionsObserver(currentRoomId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to calculate battle pairs: {ex.Message}");
            }
        }
    }

    private void CreateIsReadyForRoundObserver(string roomId, string roundId)
    {
        if (isRoundReadyRef != null)
        {
            isRoundReadyRef.ValueChanged -= HandleIsReadyForRoundChanged;
        }

        Debug.Log($"creating isReady observer");
        isRoundReadyRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId).Child("isReady");

        isRoundReadyRef.ValueChanged += HandleIsReadyForRoundChanged;
    }
    
    private void HandleIsReadyForRoundChanged(object _, ValueChangedEventArgs args)
    {
        if (gameState is not GameState.WaitingForRoundStart) {
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
                Debug.Log("All players are ready: ");
                HandleWaitingForRoundStartPhaseEnded();
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

    private void CreatePlayerActionsObserver(string roomId)
    {
        if (battlePairsRef != null)
        {
            battlePairsRef.ValueChanged -= HandlePlayerActionsChanged;
        }

        Debug.Log($"creating observer player actions {roomId} - {currentRoundId}");

        battlePairsRef = databaseReference
            .Child("rooms")
            .Child(roomId)
            .Child("rounds")
            .Child(currentRoundId.ToString())
            .Child("battlePairs");

        battlePairsRef.ValueChanged += HandlePlayerActionsChanged;
    }

    private async void HandlePlayerActionsChanged(object _, ValueChangedEventArgs args)
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

            if ((int)args.Snapshot.ChildrenCount != playerDict.Count) {
                Debug.Log("Not all players have chosen an action yet.");
                return;
            }

            // Check actions for all players inside battlePairs
            foreach (var playerSnapshot in args.Snapshot.Children)
            {
                string playerId = playerSnapshot.Key;

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

                if (playerDict[currentPlayerId].IsOwner)
                {
                    Debug.Log("Current player is first to join, calculating round score diffs...");

                    List<Task> scoreTasks = new List<Task>();
                    foreach (string playerId in playerDict.Keys)
                    {
                        scoreTasks.Add(firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(currentRoomId, currentRoundId, playerId));
                    }

                    try
                    {
                        await Task.WhenAll(scoreTasks);
                        Debug.Log("Successfully calculated and set round score diffs for all players.");

                        var totalScores = await firebaseFetchAPI.GetAllScoresAsync(currentRoomId, currentRoundId);
                        bubbleManager.UpdateScores(totalScores);

                        gameState = GameState.WaitingForRoundStart;

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
                UpdateStateBetweenRounds();
                OpenGroupPanel();
                print("HandlePlayerActionsChanged countdown");
                countdownTimer.StartCountdown(TIME_UNTIL_NEXT_ROUND_START_SECS, HandleWaitingForRoundStartPhaseEnded);
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

    private void HandleActionsCountdownFinished()
    {
        print("Actions phase countdown finished and user hasn't selected an action, so setting action to NoAction");
        if (!hasLockedAction) {
            MakeActionAndLockButtonsNotInteractible();
            _ = firebaseWriteAPI.UpdatePlayerAction(currentRoomId, currentRoundId, currentPlayerId, BubbleBattleAction.NoAction);
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

    private void UpdateStateBetweenRounds()
    {
        hasLockedAction = false;
        currentRoundId += 1;
    }

    private async Task<Dictionary<string, Player>> GetPlayerDict(object playersValue = null)
    {
        if (playersValue == null) {
            DataSnapshot playersSnapshot = await databaseReference
                    .Child("rooms")
                    .Child(currentRoomId)
                    .Child("players")
                    .GetValueAsync();

            playersValue = playersSnapshot.Value;
        }
        var typedPlayerValues = (Dictionary<string, object>)playersValue;

        var firstPlayerId = "";
        var earliestJoinTime = int.MaxValue;
        var retDict = new Dictionary<string, Player>();
        foreach (KeyValuePair<string, object> entry in typedPlayerValues)
        {
            var entryValues = (Dictionary<string, object>)entry.Value;
            var joinTime = Convert.ToInt32((long)entryValues["joinTime"]);
            if (joinTime < earliestJoinTime) {
                firstPlayerId = entry.Key;
                earliestJoinTime = joinTime;
            }
            retDict.Add(entry.Key, new Player(entry.Key, (string)entryValues["name"], joinTime, false));
        }

        var firstPlayer = retDict[firstPlayerId];
        firstPlayer.IsOwner = true;

        return retDict;
    }

    private async Task CleanupDatabase()
    {
        try
        {
            DatabaseReference roomRef = databaseReference.Child("rooms").Child(currentRoomId);

            await roomRef.Child("hasGameStarted").SetValueAsync(false);
            await roomRef.Child("rounds").RemoveValueAsync();
            await roomRef.Child("players/"+currentPlayerId).RemoveValueAsync();
            await roomRef.Child("totalScores").RemoveValueAsync();

            Debug.Log("Database cleanup completed.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to clean up database: {e.Message}");
        }
    }

    private void DisableAllActionButtonOutlines()
    {
        foreach (var btn in actionButtons)
        {
            if (!btn.TryGetComponent<Outline>(out var outline))
            {
                Debug.LogWarning("No Outline component found for action button");
                continue;
            }
            outline.enabled = false;
        }
    }

    private void MakeActionAndLockButtonsNotInteractible()
    {
        foreach (var btn in actionButtons)
        {
            btn.interactable = false;
        }
        lockActionBtn.interactable = false;
    }

    private void OpenCreateJoinRoomPanel()
    {
        CloseOtherPanels(createJoinPanel);
        createJoinPanel.SetActive(true);

        if (!Application.isEditor) {
            firstPlayerToggle.gameObject.SetActive(false);
        }
    }

    private void OpenLobbyPanel(bool isFirstPlayer)
    {
        CloseOtherPanels(lobbyPanel);
        lobbyPanel.SetActive(true);

        readyBtn.gameObject.SetActive(!isFirstPlayer);
        startGameBtn.gameObject.SetActive(isFirstPlayer);
    }

    private void OpenActionsPanel()
    {
        CloseOtherPanels(actionsPanel);
        actionsPanel.SetActive(true);
    }

    private void OpenGroupPanel()
    {
        CloseOtherPanels(groupPanel);
        groupPanel.SetActive(true);
    }

    private void OpenWinLosePanel()
    {
        CloseOtherPanels(winLosePanel);
        winLosePanel.SetActive(true);
    }

    private void CloseOtherPanels(GameObject openPanel)
    {
        if (openPanel != createJoinPanel)
        {
            createJoinPanel.SetActive(false);
        }

        if (openPanel != lobbyPanel)
        {
            lobbyPanel.SetActive(false);
        }

        if (openPanel != groupPanel)
        {
            groupPanel.SetActive(false);
        }

        if (openPanel != actionsPanel)
        {
            actionsPanel.SetActive(false);
        }

        if (openPanel != winLosePanel)
        {
            winLosePanel.SetActive(false);
        }
    }
}
