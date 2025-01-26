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
        var roomCreationResult = await firebaseWriteAPI.CreateRoomAsync(playerOneName);
        Debug.Log("CreateRoomAndJoinRoom Room Created with ID: " + roomCreationResult.roomId +"and user ID: " + roomCreationResult.playerId);
        var joinRoomResultSecond = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult.roomId, playerTwoName);
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultSecond);
        var joinRoomResultThird = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult.roomId, playerThreeName);
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultThird);
        
        // Check ready states
        var allPlayers = await firebaseAPIFetch.GetAllPlayersAsync(roomCreationResult.roomId);
        // Print out each player
        foreach (var player in allPlayers)
        {
            Debug.Log($"Player ID: {player.FirebaseId}, Player Name: {player.Name}, Join Time: {player.JoinTime}");
        }
        
        Assert.AreEqual(3, allPlayers.Count);
        
        // Check ready states
        var statusesBeforeReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult.roomId,0);
        Assert.AreEqual(3, statusesBeforeReady.Count);
        Assert.IsFalse(statusesBeforeReady[0].Ready);
        Assert.IsFalse(statusesBeforeReady[1].Ready);
        Assert.IsFalse(statusesBeforeReady[2].Ready);

        // Players ready-up
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,roomCreationResult.playerId,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,joinRoomResultSecond,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,joinRoomResultThird,true);
        
        // Check ready states
        var statusesAfterReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult.roomId,0);
        Assert.AreEqual(3, statusesAfterReady.Count);
        Assert.IsTrue(statusesAfterReady[0].Ready);
        Assert.IsTrue(statusesAfterReady[1].Ready);
        Assert.IsTrue(statusesAfterReady[2].Ready);
        
        // Update available opponents for each player
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult.roomId,0, roomCreationResult.playerId,new [] { playerTwoName, playerThreeName });
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult.roomId,0, joinRoomResultSecond,new [] {playerOneName, playerThreeName});
        await firebaseWriteAPI.UpdateAvailableOpponentAsync(roomCreationResult.roomId,0, joinRoomResultThird,new [] {playerOneName, playerTwoName});
        
        // Read available opponents for each player
        var opponentsForPlayerOne = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult.roomId,0, roomCreationResult.playerId);
        var opponentsForPlayerTwo = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult.roomId,0, joinRoomResultSecond);
        var opponentsForPlayerThree = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult.roomId,0, joinRoomResultThird);
        Assert.AreEqual(2, opponentsForPlayerOne.Count);
        Assert.AreEqual(2, opponentsForPlayerTwo.Count);
        Assert.AreEqual(2, opponentsForPlayerThree.Count);
        
        Assert.AreEqual(playerTwoName, opponentsForPlayerOne[0]);
        Assert.AreEqual(playerThreeName, opponentsForPlayerOne[1]);
        
        Assert.AreEqual(playerOneName, opponentsForPlayerTwo[0]);
        Assert.AreEqual(playerThreeName, opponentsForPlayerTwo[1]);
        
        Assert.AreEqual(playerOneName, opponentsForPlayerThree[0]);
        Assert.AreEqual(playerTwoName, opponentsForPlayerThree[1]);
        
        // Create battle pairs
        await firebaseWriteAPI.CreateBattlePair(roomCreationResult.roomId,0,roomCreationResult.playerId,joinRoomResultSecond);
        await firebaseWriteAPI.CreateBattlePairEmpty(roomCreationResult.roomId,0,joinRoomResultThird);

        // Clean up
        GameObject.DestroyImmediate(gameObject);
    }
}