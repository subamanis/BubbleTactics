using UnityEngine;

using Firebase.Database;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseRealtime : MonoBehaviour
{
    private DatabaseReference databaseReference;

    void Start()
    {
        // Initialize the Firebase Database reference
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Add a new player and initialize a new round
        string roomId = "12345"; // Replace with the actual room ID where you want to make changes
        // AddPlayerToRoom(roomId, "Petros");
        // // Fetch all players from all rooms
        // GetAllPlayers();

        // Subscribe all players to the "winner" value
        SubscribeToWinner(roomId);

        // Set the "winner" value to test the subscription
        TestWinnerUpdate(roomId, "Petros"); // "player3" corresponds to "Petros"
    }

    void GetAllPlayers()
    {
        // Reference to "rooms" in the database
        databaseReference.Child("rooms").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // Dictionary to store players
                Dictionary<string, Dictionary<string, object>> allPlayers = new Dictionary<string, Dictionary<string, object>>();

                foreach (var room in snapshot.Children)
                {
                    string roomId = room.Key; // e.g., "room1", "room2"
                    DataSnapshot playersSnapshot = room.Child("players");

                    foreach (var player in playersSnapshot.Children)
                    {
                        string playerId = player.Key; // e.g., "player1"
                        string playerName = player.Child("name").Value.ToString();
                        string playerJoinTime = player.Child("joinTime").Value.ToString();

                        Debug.Log($"Room: {roomId}, Player: {playerId}, Name: {playerName}, Join Time: {playerJoinTime}");
                        
                        // Add to dictionary (optional)
                        allPlayers[playerId] = new Dictionary<string, object>
                        {
                            { "roomId", roomId },
                            { "name", playerName },
                            { "joinTime", playerJoinTime }
                        };
                    }
                }

                Debug.Log("All players retrieved successfully.");
            }
            else
            {
                Debug.LogError($"Failed to get players: {task.Exception}");
            }
        });
    }
    void AddPlayerToRoom(string roomId, string playerName)
    {
        // Reference to the room's "players" node
        DatabaseReference playersRef = databaseReference.Child("rooms").Child(roomId).Child("players");

        // Add the new player with the current timestamp
        string newPlayerId = "player" + System.DateTime.UtcNow.Ticks; // Unique player ID
        Dictionary<string, object> newPlayerData = new Dictionary<string, object>
        {
            { "name", playerName },
            { "joinTime", System.DateTime.UtcNow.ToString("o") } // ISO 8601 format
        };

        playersRef.Child(newPlayerId).SetValueAsync(newPlayerData).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Player '{playerName}' added to room '{roomId}'.");
                // After adding the player, initialize the round
                InitializeRound(roomId);
            }
            else
            {
                Debug.LogError($"Failed to add player: {task.Exception}");
            }
        });
    }

    void InitializeRound(string roomId)
    {
        // Reference to the room's "rounds" node
        DatabaseReference roundsRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child("0");

        // Prepare data for the new round
        Dictionary<string, object> scores = new Dictionary<string, object>();

        // Get all players to populate scores
        databaseReference.Child("rooms").Child(roomId).Child("players").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (var player in snapshot.Children)
                {
                    string playerName = player.Child("name").Value.ToString();
                    scores[playerName] = 10; // Initial score for all players
                }

                // Prepare round data
                Dictionary<string, object> roundData = new Dictionary<string, object>
                {
                    { "scores", scores },
                    { "winner", null } // Winner initially set to null
                };

                // Set the round data
                roundsRef.SetValueAsync(roundData).ContinueWith(roundTask =>
                {
                    if (roundTask.IsCompleted)
                    {
                        Debug.Log($"Round 0 initialized in room '{roomId}' with scores and no winner.");
                    }
                    else
                    {
                        Debug.LogError($"Failed to initialize round: {roundTask.Exception}");
                    }
                });
            }
            else
            {
                Debug.LogError($"Failed to retrieve players for scores: {task.Exception}");
            }
        });
    }

    void SubscribeToWinner(string roomId)
    {
        DatabaseReference winnerRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child("0").Child("winner");

        // Attach a listener to the "winner" value
        winnerRef.ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"Failed to subscribe to 'winner': {args.DatabaseError.Message}");
                return;
            }

            string newWinnerId = args.Snapshot.Value?.ToString();

            if (!string.IsNullOrEmpty(newWinnerId))
            {
                Debug.Log($"Winner updated to: {newWinnerId}");

                // Update scores for all players based on the new winner
                UpdateScores(roomId, newWinnerId);
            }
        };
    }

    void UpdateScores(string roomId, string winnerId)
    {
        DatabaseReference playersRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child("0").Child("scores");

        // Get the current scores
        playersRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                Dictionary<string, object> updatedScores = new Dictionary<string, object>();

                foreach (var playerScore in snapshot.Children)
                {
                    string playerId = playerScore.Key;
                    int currentScore = int.Parse(playerScore.Value.ToString());

                    // Adjust the score based on whether the player is the winner or not
                    int updatedScore = playerId == winnerId ? currentScore + 2 : currentScore - 2;

                    updatedScores[playerId] = updatedScore;
                }

                // Update all scores in the database
                playersRef.UpdateChildrenAsync(updatedScores).ContinueWith(scoreUpdateTask =>
                {
                    if (scoreUpdateTask.IsCompleted)
                    {
                        Debug.Log("Scores updated successfully.");
                    }
                    else
                    {
                        Debug.LogError($"Failed to update scores: {scoreUpdateTask.Exception}");
                    }
                });
            }
            else
            {
                Debug.LogError($"Failed to retrieve scores: {task.Exception}");
            }
        });
    }

    void TestWinnerUpdate(string roomId, string winnerId)
    {
        // Set the winner value to "Petros" (player3) to test the subscription
        DatabaseReference winnerRef = databaseReference.Child("rooms").Child(roomId).Child("rounds").Child("0").Child("winner");

        winnerRef.SetValueAsync(winnerId).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"Winner set to: {winnerId}");
            }
            else
            {
                Debug.LogError($"Failed to set winner: {task.Exception}");
            }
        });
    }
}
