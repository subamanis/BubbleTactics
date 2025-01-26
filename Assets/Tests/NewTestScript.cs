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
        // Create a GameObject
        var gameObject = new GameObject();

        // Create instances of Firebase
        FirebaseWriteAPI firebaseWriteAPI = gameObject.AddComponent<FirebaseWriteAPI>();
        FirebaseAPIFetch firebaseAPIFetch = gameObject.AddComponent<FirebaseAPIFetch>();
        UserActions userActions = gameObject.AddComponent<UserActions>();
        userActions.Start();

        // Create room and have players join it
        var roomCreationResult = await firebaseWriteAPI.CreateRoomAsync("Thanasis-Test");
        Debug.Log("CreateRoomAndJoinRoom Room Created with ID: " + roomCreationResult.roomId +"and user ID: " + roomCreationResult.playerId);
        var joinRoomResultSecond = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult.roomId, "Second-Joiner-Test");
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultSecond);
        var joinRoomResultThird = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult.roomId, "Third-Joiner-Test");
        Debug.Log("CreateRoomAndJoinRoom Room Joined for user ID: " + joinRoomResultThird);
        
        // Check ready states
        var allPlayers = await firebaseAPIFetch.GetAllPlayersAsync(roomCreationResult.roomId);
        Debug.Log("CreateRoomAndJoinRoom All Players: " + allPlayers);
        // Print out each player
        foreach (var player in allPlayers)
        {
            Debug.Log($"Player ID: {player.FirebaseId}, Player Name: {player.Name}, Join Time: {player.JoinTime}");
        }
        
        Assert.AreEqual(3, allPlayers.Count);

        // Players ready-up
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,roomCreationResult.playerId,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,joinRoomResultSecond,true);
        await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult.roomId,0,joinRoomResultThird,true);
        

        // Clean up
        GameObject.DestroyImmediate(gameObject);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}