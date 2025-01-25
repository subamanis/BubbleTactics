using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseWriteAPI: MonoBehaviour
{
    private DatabaseReference databaseReference;

    void Start(){
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private string GenerateFiveDigitRoomId()
    {
        // Generate a random 5-digit number
        System.Random random = new System.Random();
        return random.Next(10000, 99999).ToString();
    }

    private long GetUnixTimestamp()
    {
        // Return the current Unix timestamp in seconds
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public async Task<string> CreateRoomAsync(string playerName)
    {
        string roomId = GenerateFiveDigitRoomId();

        try
        {
            // Check if the room ID already exists in the database
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).GetValueAsync();

            // If the room ID already exists, regenerate and try again (handle collisions)
            while (snapshot.Exists)
            {
                roomId = GenerateFiveDigitRoomId();
                snapshot = await databaseReference.Child("rooms").Child(roomId).GetValueAsync();
            }

            // Create the player data
            var playerId = databaseReference.Child("rooms").Child(roomId).Child("players").Push().Key;
            var playerData = new Dictionary<string, object>
            {
                { "name", playerName },
                { "joinTime", GetUnixTimestamp() } // ISO 8601 format
            };

            // Initial room structure
            var roomData = new Dictionary<string, object>
            {
                { "hasGameStarted", false },
                { "players", new Dictionary<string, object> { { playerId, playerData } } }, // Add the player
                { "rounds", new List<object>() } // Empty rounds initially
            };

            // Write the new room to the database
            await databaseReference.Child("rooms").Child(roomId).SetValueAsync(roomData);

            Debug.Log($"Room {roomId} created successfully with player {playerName}.");
            return roomId; // Return the generated room ID
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create room: {ex.Message}");
            return null; // Return null if the operation fails
        }
    }

    public async Task<string> JoinRoomAsync(string roomId, string playerName)
    {
        string playerId = databaseReference.Child("rooms").Child(roomId).Child("players").Push().Key; // Auto-generate a unique ID

        try
        {
            // Create the player data
            var playerData = new Dictionary<string, object>
            {
                { "name", playerName },
                { "joinTime", GetUnixTimestamp() } // ISO 8601 format
            };

            // Add the player to the specified room
            await databaseReference.Child("rooms").Child(roomId).Child("players").Child(playerId).SetValueAsync(playerData);

            Debug.Log($"Player {playerName} (ID: {playerId}) joined room {roomId} successfully.");

            return playerId; // Return the generated player ID
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to add player {playerName} to room {roomId}: {ex.Message}");
            return null;
        }
    }

    public async Task<int> CreateNewRoundAsync(string roomId)
    {
        try
        {
            DatabaseReference roundsRef = databaseReference.Child("rooms").Child(roomId).Child("rounds");

            // Get the existing rounds to determine the new round ID
            DataSnapshot roundsSnapshot = await roundsRef.GetValueAsync();
            int newRoundId = 0;

            if (roundsSnapshot.Exists && roundsSnapshot.ChildrenCount > 0)
            {
                // Get the highest existing round ID
                foreach (var round in roundsSnapshot.Children)
                {
                    int roundId = int.Parse(round.Key);
                    if (roundId >= newRoundId)
                    {
                        newRoundId = roundId + 1;
                    }
                }
            }

            // Default scores: Carry forward or assign 5 if no previous round exists
            Dictionary<string, int> scores = new Dictionary<string, int>();

            if (newRoundId > 0) // Carry scores from the last round if it exists
            {
                DataSnapshot previousRoundScores = roundsSnapshot.Child((newRoundId - 1).ToString()).Child("scores");
                foreach (var playerScore in previousRoundScores.Children)
                {
                    string playerName = playerScore.Key;
                    int score = int.Parse(playerScore.Value.ToString());
                    scores[playerName] = score;
                }
            }
            else // Default scores for the first round
            {
                DataSnapshot playersSnapshot = await databaseReference.Child("rooms").Child(roomId).Child("players").GetValueAsync();
                foreach (var player in playersSnapshot.Children)
                {
                    string playerName = player.Child("name").Value.ToString();
                    scores[playerName] = 5; // Default score
                }
            }

            // Default data for the new round
            var newRoundData = new Dictionary<string, object>
            {
                { "actionSelection", new Dictionary<string, object>() },
                { "availableOpponents", new Dictionary<string, object>() },
                { "hasLockedAction", new Dictionary<string, object>() },
                { "isReady", new Dictionary<string, object>() },
                { "scores", scores }
            };

            // Write the new round to the database
            await roundsRef.Child(newRoundId.ToString()).SetValueAsync(newRoundData);

            Debug.Log($"Round {newRoundId} created successfully in room {roomId}.");
            return newRoundId; // Return the new round ID
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create a new round in room {roomId}: {ex.Message}");
            return -1; // Return -1 to indicate failure
        }
    }

    public async Task UpdateAvailableOpponentsAsync(string roomId, int roundId, Dictionary<string, string> availableOpponents)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("availableOpponents").SetValueAsync(availableOpponents);
            Debug.Log($"Updated availableOpponents for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update availableOpponents for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateAvailableOpponentAsync(string roomId, int roundId, string playerId, string opponents)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("availableOpponents").Child(playerId).SetValueAsync(opponents);
            Debug.Log($"Updated availableOpponent for player {playerId} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update availableOpponent for player {playerId} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateActionSelectionsAsync(string roomId, int roundId, Dictionary<string, string> actionSelections)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("actionSelection").SetValueAsync(actionSelections);
            Debug.Log($"Updated actionSelections for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update actionSelections for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateActionSelectionAsync(string roomId, int roundId, string playerId, string action)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("actionSelection").Child(playerId).SetValueAsync(action);
            Debug.Log($"Updated actionSelection for player {playerId} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update actionSelection for player {playerId} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateHasLockedActionsAsync(string roomId, int roundId, Dictionary<string, bool> hasLockedActions)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("hasLockedAction").SetValueAsync(hasLockedActions);
            Debug.Log($"Updated hasLockedActions for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update hasLockedActions for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateHasLockedActionAsync(string roomId, int roundId, string playerId, bool hasLocked)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("hasLockedAction").Child(playerId).SetValueAsync(hasLocked);
            Debug.Log($"Updated hasLockedAction for player {playerId} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update hasLockedAction for player {playerId} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateIsReadyAsync(string roomId, int roundId, Dictionary<string, bool> isReady)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("isReady").SetValueAsync(isReady);
            Debug.Log($"Updated isReady for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update isReady for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateIsReadyForPlayerAsync(string roomId, int roundId, string playerId, bool isReady)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("isReady").Child(playerId).SetValueAsync(isReady);
            Debug.Log($"Updated isReady for player {playerId} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update isReady for player {playerId} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateScoresAsync(string roomId, int roundId, Dictionary<string, int> scores)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("scores").SetValueAsync(scores);
            Debug.Log($"Updated scores for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update scores for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateScoreForPlayerAsync(string roomId, int roundId, string playerName, int score)
    {
        try
        {
            await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("scores").Child(playerName).SetValueAsync(score);
            Debug.Log($"Updated score for player {playerName} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update score for player {playerName} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

}

