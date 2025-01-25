using UnityEngine;

using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirebaseAPIFetch: MonoBehaviour
{
    private DatabaseReference databaseReference;

    void Start(){
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public async Task<bool?> GetHasGameStartedAsync(string roomId)
    {
        bool? hasGameStarted = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("hasGameStarted").GetValueAsync();
            if (snapshot.Exists)
            {
                hasGameStarted = bool.Parse(snapshot.Value.ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch hasGameStarted for room {roomId}: {ex.Message}");
        }
        return hasGameStarted;
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> GetAllPlayersAsync(string roomId)
    {
        var players = new Dictionary<string, Dictionary<string, object>>();

        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("players").GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (var playerSnapshot in snapshot.Children)
                {
                    string playerId = playerSnapshot.Key;
                    var playerData = new Dictionary<string, object>();

                    foreach (var child in playerSnapshot.Children)
                    {
                        playerData[child.Key] = child.Value;
                    }

                    players[playerId] = playerData;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch players: {ex.Message}");
        }

        return players;
    }
    public async Task<Dictionary<string, string>> GetAllActionSelectionsAsync(string roomId, int roundId)
    {
        var actionSelections = new Dictionary<string, string>();
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("actionSelection").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    actionSelections[child.Key] = child.Value.ToString();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch action selections: {ex.Message}");
        }
        return actionSelections;
    }

    public async Task<string> GetActionSelectionAsync(string roomId, int roundId, string playerId)
    {
        string actionSelection = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("actionSelection").Child(playerId).GetValueAsync();
            if (snapshot.Exists)
            {
                actionSelection = snapshot.Value.ToString();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch action selection for player {playerId}: {ex.Message}");
        }
        return actionSelection;
    }

    public async Task<Dictionary<string, string>> GetAllAvailableOpponentsAsync(string roomId, int roundId)
    {
        var availableOpponents = new Dictionary<string, string>();
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("availableOpponents").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    availableOpponents[child.Key] = child.Value.ToString();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch available opponents: {ex.Message}");
        }
        return availableOpponents;
    }

    public async Task<string> GetAvailableOpponentAsync(string roomId, int roundId, string playerId)
    {
        string availableOpponent = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("availableOpponents").Child(playerId).GetValueAsync();
            if (snapshot.Exists)
            {
                availableOpponent = snapshot.Value.ToString();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch available opponent for player {playerId}: {ex.Message}");
        }
        return availableOpponent;
    }

    public async Task<Dictionary<string, bool>> GetAllHasLockedActionsAsync(string roomId, int roundId)
    {
        var hasLockedActions = new Dictionary<string, bool>();
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("hasLockedAction").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    hasLockedActions[child.Key] = bool.Parse(child.Value.ToString());
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch hasLockedAction values: {ex.Message}");
        }
        return hasLockedActions;
    }

    public async Task<bool?> GetHasLockedActionAsync(string roomId, int roundId, string playerId)
    {
        bool? hasLockedAction = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("hasLockedAction").Child(playerId).GetValueAsync();
            if (snapshot.Exists)
            {
                hasLockedAction = bool.Parse(snapshot.Value.ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch hasLockedAction for player {playerId}: {ex.Message}");
        }
        return hasLockedAction;
    }

    public async Task<Dictionary<string, bool>> GetAllIsReadyAsync(string roomId, int roundId)
    {
        var isReadyValues = new Dictionary<string, bool>();
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("isReady").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    isReadyValues[child.Key] = bool.Parse(child.Value.ToString());
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch isReady values: {ex.Message}");
        }
        return isReadyValues;
    }

    public async Task<bool?> GetIsReadyAsync(string roomId, int roundId, string playerId)
    {
        bool? isReady = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("isReady").Child(playerId).GetValueAsync();
            if (snapshot.Exists)
            {
                isReady = bool.Parse(snapshot.Value.ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch isReady for player {playerId}: {ex.Message}");
        }
        return isReady;
    }

    public async Task<Dictionary<string, int>> GetAllScoresAsync(string roomId, int roundId)
    {
        var scores = new Dictionary<string, int>();
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("scores").GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    scores[child.Key] = int.Parse(child.Value.ToString());
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch scores: {ex.Message}");
        }
        return scores;
    }

    public async Task<int?> GetScoreAsync(string roomId, int roundId, string playerName)
    {
        int? score = null;
        try
        {
            DataSnapshot snapshot = await databaseReference.Child("rooms").Child(roomId).Child("rounds").Child(roundId.ToString()).Child("scores").Child(playerName).GetValueAsync();
            if (snapshot.Exists)
            {
                score = int.Parse(snapshot.Value.ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to fetch score for player {playerName}: {ex.Message}");
        }
        return score;
    }

}
