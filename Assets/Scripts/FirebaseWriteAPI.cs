using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseWriteAPI : MonoBehaviour
{
    public const string PlayerNameSeparator = "||";
    const int StartingPlayerScore = 5;

    public DatabaseReference DatabaseReference { get; set; }

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

    public async Task<string> CreateRoomAsync()
    {
        string roomId = GenerateFiveDigitRoomId();

        try
        {
            // Check if the room ID already exists in the database
            DataSnapshot snapshot = await DatabaseReference.Child("rooms").Child(roomId).GetValueAsync();

            // If the room ID already exists, regenerate and try again (handle collisions)
            while (snapshot.Exists)
            {
                roomId = GenerateFiveDigitRoomId();
                snapshot = await DatabaseReference.Child("rooms").Child(roomId).GetValueAsync();
            }

            // Initial room structure
            var roomData = new Dictionary<string, object>
            {
                { "createdTime", GetUnixTimestamp() } // Unix timestamp
            };

            // Write the new room to the database
            await DatabaseReference.Child("rooms").Child(roomId).SetValueAsync(roomData);

            // Create first round
            await CreateNewRoundAsync(roomId);

            Debug.Log($"Room {roomId} created successfully with the first round initialized.");
            return roomId; // Return both the roomId and playerId
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create room: {ex.Message}");
            return null; // Return null if the operation fails
        }
    }

    public async Task<string> JoinRoomAsync(string roomId, string playerName)
    {
        try
        {
            // Generate a unique playerId for the new player
            var playerId = DatabaseReference.Child("rooms").Child(roomId).Child("players").Push().Key;

            // Create the player data
            var playerData = new Dictionary<string, object>
            {
                { "name", playerName },
                { "joinTime", GetUnixTimestamp() } // Unix timestamp for joinTime
            };

            // Add the player to the players node
            await DatabaseReference.Child("rooms").Child(roomId).Child("players").Child(playerId).SetValueAsync(playerData);

            Debug.Log($"Player {playerName} (ID: {playerId}) joined room {roomId}.");

            // Update the "isReady" field in the first round (roundId = 0)
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child("0").Child("isReady").Child(playerId).SetValueAsync(false);

            Debug.Log($"Player {playerId} added to isReady for round 0 in room {roomId}.");
            return playerId; // Return the generated playerId
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to join room {roomId} for player {playerName}: {ex.Message}");
            return null; // Return null if the operation fails
        }
    }

    public async Task<int> CreateNewRoundAsync(string roomId)
    {
        try
        {
            DatabaseReference roundsRef = DatabaseReference.Child("rooms").Child(roomId).Child("rounds");

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
                DataSnapshot playersSnapshot = await DatabaseReference.Child("rooms").Child(roomId).Child("players").GetValueAsync();
                foreach (var player in playersSnapshot.Children)
                {
                    string playerName = player.Child("name").Value.ToString();
                    scores[playerName] = StartingPlayerScore; // Default score
                }
            }

            // Default data for the new round
            var newRoundData = new Dictionary<string, object>
            {
                { "scoreDiffs", scores }
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

    public async Task UpdateAvailableOpponentAsync(string roomId, int roundId, string playerId, string[] opponents)
    {
        try
        {
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("availableOpponents").Child(playerId).SetValueAsync(string.Join(PlayerNameSeparator, opponents));
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
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("actionSelection").SetValueAsync(actionSelections);
            Debug.Log($"Updated actionSelections for round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update actionSelections for round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task UpdateIsReadyForPlayerAsync(string roomId, int roundId, string playerId, bool isReady)
    {
        try
        {
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("isReady").Child(playerId).SetValueAsync(isReady);
            Debug.Log($"Updated isReady for player {playerId} in round {roundId} in room {roomId}.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to update isReady for player {playerId} in round {roundId} in room {roomId}: {ex.Message}");
        }
    }

    public async Task CreateBattlePair(string roomId, int roundId, string playerOneSide, string playerOtherSide)
    {
        try
        {
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(playerOneSide).SetValueAsync(new Dictionary<string, object>
            {
                { "opponent", playerOtherSide },
                { "isPlaying", true }
            });
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(playerOtherSide).SetValueAsync(new Dictionary<string, object>
            {
                { "opponent", playerOneSide },
                { "isPlaying", true }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create battle pair for player {playerOneSide} and {playerOtherSide} in room {roomId} and round {roundId}: {e.Message}");
        }
    }

    public async Task CreateBattlePairEmpty(string roomId, int roundId, string playerNoBattle)
    {
        try
        {
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(playerNoBattle).SetValueAsync(new Dictionary<string, bool>
            {
                { "isPlaying", false }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create empty battle pair for player {playerNoBattle} in room {roomId} and round {roundId}: {e.Message}");
        }
    }

    public async Task UpdatePlayerAction(string roomId, int roundId, string playerId, BubbleBattleAction bubbleBattleAction)
    {
        try
        {
            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(playerId).UpdateChildrenAsync(new Dictionary<string, object>
            {
                { "action", bubbleBattleAction.ToString() }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to update player action for player {playerId} in room {roomId} and round {roundId}: {e.Message}");
        }
    }

    public async Task CalculateAndSetPlayerRoundScoreDiff(string roomId, int roundId, string playerId)
    {
        try
        {
            // Get battlePair
            DataSnapshot battlePairSnapshot = await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(playerId).GetValueAsync();
            Dictionary<string, object> battlePair = battlePairSnapshot.Value as Dictionary<string, object>;
            string myActionString = battlePair["action"].ToString();
            BubbleBattleAction myActionEnum = (BubbleBattleAction)Enum.Parse(typeof(BubbleBattleAction), myActionString);

            int myScoreDiff = 0;

            // Get opponent action for that battlePair
            if (battlePair["isPlaying"].Equals(true))
            {
                DataSnapshot opponentActionSnapshot = await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("battlePairs").Child(battlePair["opponent"].ToString()).GetValueAsync();
                Dictionary<string, object> opponentAction = opponentActionSnapshot.Value as Dictionary<string, object>;
                string opponentActionString = opponentAction["action"].ToString();
                BubbleBattleAction opponentActionEnum = (BubbleBattleAction)Enum.Parse(typeof(BubbleBattleAction), opponentActionString);

                myScoreDiff = BabbleBattleScoreCalculator.CalculateScore(myActionEnum, opponentActionEnum);
            }

            await DatabaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("scoreDiffs").Child(playerId).SetValueAsync(myScoreDiff);
            Debug.Log($"Updated score for player {playerId} in round {roundId} in room {roomId}.");
            
            // Update total score atomically using a transaction
            await DatabaseReference.Child("rooms").Child(roomId).Child("totalScores").Child(playerId).RunTransaction(mutableData =>
            {
                int totalScore = StartingPlayerScore;

                // Check the current value of totalScore
                if (mutableData.Value != null)
                {
                    totalScore = Convert.ToInt32(mutableData.Value);
                }

                // Increment the totalScore by the calculated score difference
                totalScore += myScoreDiff;

                // Save the updated value
                mutableData.Value = totalScore;

                return TransactionResult.Success(mutableData);
            });

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to calculate and set player round score diff for player {playerId} in room {roomId} and round {roundId}: {e.Message}");
        }
    }
}