using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    // A Test behaves as an ordinary method
    [Test]
    public async Task CreateRoomAndJoinRoom()
    {
        var playerOneName = "Thanasis-Test";
        var playerTwoName = "Second-Joiner-Test";
        var playerThreeName = "Third-Joiner-Test";
        
        // Create a GameObject
        var gameObject = new GameObject();

        // Create instances of Firebase
        FirebaseWriteAPI firebaseWriteAPI = gameObject.AddComponent<FirebaseWriteAPI>();
        FirebaseAPIFetch firebaseAPIFetch = gameObject.AddComponent<FirebaseAPIFetch>();
        UserActions userActions = gameObject.AddComponent<UserActions>();
        var firebaseInit = await userActions.InitFirebase();
        Assert.IsTrue(firebaseInit);

        // Create room and have players join it
        var roomCreationResult = await firebaseWriteAPI.CreateRoomAsync();
        Debug.Log("CreateRoomAndJoinRoom Room Created with ID: " + roomCreationResult);
        
        // Join in
        var joinRoomResultFirst = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult, playerOneName);
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultFirst);
        var joinRoomResultSecond = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult, playerTwoName);
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultSecond);
        var joinRoomResultThird = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult, playerThreeName);
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultThird);
        
        // Check ready states
        var allPlayers = await firebaseAPIFetch.GetAllPlayersAsync(roomCreationResult);
        // Print out each player
        foreach (var player in allPlayers)
        {
            Debug.Log($"Player ID: {player.FirebaseId}, Player Name: {player.Name}, Join Time: {player.JoinTime}");
        }
        
        Assert.AreEqual(3, allPlayers.Count);
        
        // Check ready states
        var statusesBeforeReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult,0);
        Assert.AreEqual(3, statusesBeforeReady.Count);
        Assert.IsFalse(statusesBeforeReady[0].Ready);
        Assert.IsFalse(statusesBeforeReady[1].Ready);
        Assert.IsFalse(statusesBeforeReady[2].Ready);

        // Players ready-up
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,0,joinRoomResultFirst,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,0,joinRoomResultSecond,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,0,joinRoomResultThird,true);
        
        // Check ready states
        var statusesAfterReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult,0);
        Assert.AreEqual(3, statusesAfterReady.Count);
        Assert.IsTrue(statusesAfterReady[0].Ready);
        Assert.IsTrue(statusesAfterReady[1].Ready);
        Assert.IsTrue(statusesAfterReady[2].Ready);
        
        // Update available opponents for each player
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult,0, joinRoomResultFirst,new [] { joinRoomResultSecond, joinRoomResultThird });
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult,0, joinRoomResultSecond,new [] {joinRoomResultFirst, joinRoomResultThird});
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult,0, joinRoomResultThird,new [] {joinRoomResultFirst, joinRoomResultSecond});
        
        // Read available opponents for each player
        var opponentsForPlayerOne = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,0, joinRoomResultFirst);
        var opponentsForPlayerTwo = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,0, joinRoomResultSecond);
        var opponentsForPlayerThree = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,0, joinRoomResultThird);
        Assert.AreEqual(2, opponentsForPlayerOne.Count);
        Assert.AreEqual(2, opponentsForPlayerTwo.Count);
        Assert.AreEqual(2, opponentsForPlayerThree.Count);
        
        Assert.AreEqual(joinRoomResultSecond, opponentsForPlayerOne[0]);
        Assert.AreEqual(joinRoomResultThird, opponentsForPlayerOne[1]);
        
        Assert.AreEqual(joinRoomResultFirst, opponentsForPlayerTwo[0]);
        Assert.AreEqual(joinRoomResultThird, opponentsForPlayerTwo[1]);
        
        Assert.AreEqual(joinRoomResultFirst, opponentsForPlayerThree[0]);
        Assert.AreEqual(joinRoomResultSecond, opponentsForPlayerThree[1]);
        
        // Create battle pairs
        await firebaseWriteAPI.CreateBattlePair(roomCreationResult,0,joinRoomResultFirst,joinRoomResultSecond);
        await firebaseWriteAPI.CreateBattlePairEmpty(roomCreationResult,0,joinRoomResultThird);
        
        // Players play ACTIONS
        await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 0, joinRoomResultFirst, BubbleBattleAction.Merge);
        await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 0, joinRoomResultSecond, BubbleBattleAction.Merge);
        await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 0, joinRoomResultThird, BubbleBattleAction.NoAction);
        
        // Each player calculates their score
        await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 0, joinRoomResultFirst);
        await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 0, joinRoomResultSecond);
        await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 0, joinRoomResultThird);

        // Clean up
        GameObject.DestroyImmediate(gameObject);
    }
}