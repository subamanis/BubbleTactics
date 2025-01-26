using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class TestBubbleTactics
    {
        // A Test behaves as an ordinary method
        [Test]
        public async Task PlayAFullRoundWithThreePlayers()
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
        
            firebaseWriteAPI.Start();

            // Create room and have players join it
            var roomCreationResult = await firebaseWriteAPI.CreateRoomAsync();
            Debug.Log("CreateRoomAndJoinRoom Room Created with ID: " + roomCreationResult);
        
            // Join in
            var joinRoomResultFirst = await firebaseWriteAPI.JoinRoomAsync(roomCreationResult, playerOneName, true);
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
            var statusesBeforeReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult,1);
            Assert.AreEqual(3, statusesBeforeReady.Count);
            Assert.IsFalse(statusesBeforeReady[0].Ready);
            Assert.IsFalse(statusesBeforeReady[1].Ready);
            Assert.IsFalse(statusesBeforeReady[2].Ready);

            // Players ready-up
            await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,1,joinRoomResultFirst,true);
            await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,1,joinRoomResultSecond,true);
            await firebaseWriteAPI.UpdateIsReadyForPlayerAsync(roomCreationResult,1,joinRoomResultThird,true);
        
            // Check ready states
            var statusesAfterReady =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult,1);
            Assert.AreEqual(3, statusesAfterReady.Count);
            Assert.IsTrue(statusesAfterReady[0].Ready);
            Assert.IsTrue(statusesAfterReady[1].Ready);
            Assert.IsTrue(statusesAfterReady[2].Ready);
            
            // Calculate battle pairs
            await firebaseWriteAPI.CalculateBattlePairs(roomCreationResult,1);
        
            // Read available opponents for each player
            var opponentsForPlayerOne = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultFirst);
            var opponentsForPlayerTwo = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultSecond);
            var opponentsForPlayerThree = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultThird);
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
            await firebaseWriteAPI.CreateBattlePair(roomCreationResult,1,joinRoomResultFirst,joinRoomResultSecond);
            await firebaseWriteAPI.CreateBattlePairEmpty(roomCreationResult,1,joinRoomResultThird);
        
            // Players play ACTIONS
            await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 1, joinRoomResultFirst, BubbleBattleAction.Merge);
            await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 1, joinRoomResultSecond, BubbleBattleAction.Merge);
            await firebaseWriteAPI.UpdatePlayerAction(roomCreationResult, 1, joinRoomResultThird, BubbleBattleAction.NoAction);
        
            // Check available opponents again
            // Read available opponents for each player
            var opponentsForPlayerOneAfterGame = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultFirst);
            var opponentsForPlayerTwoAfterGame = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultSecond);
            var opponentsForPlayerThreeAfterGame = await firebaseAPIFetch.GetAvailableOpponentAsync(roomCreationResult,1, joinRoomResultThird);
            Assert.AreEqual(1, opponentsForPlayerOneAfterGame.Count);
            Assert.AreEqual(1, opponentsForPlayerTwoAfterGame.Count);
            Assert.AreEqual(2, opponentsForPlayerThreeAfterGame.Count);
        
            Assert.AreEqual(joinRoomResultThird, opponentsForPlayerOneAfterGame[0]);
        
            Assert.AreEqual(joinRoomResultThird, opponentsForPlayerTwoAfterGame[0]);
        
            Assert.AreEqual(joinRoomResultFirst, opponentsForPlayerThreeAfterGame[0]);
            Assert.AreEqual(joinRoomResultSecond, opponentsForPlayerThreeAfterGame[1]);

            // Each player calculates their score
            await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 1, joinRoomResultFirst);
            await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 1, joinRoomResultSecond);
            await firebaseWriteAPI.CalculateAndSetPlayerRoundScoreDiff(roomCreationResult, 1, joinRoomResultThird);
        
            // Owner generates next round
            await firebaseWriteAPI.CreateNewRoundAsync(roomCreationResult);
        
            // Check ready states for next round
            var statusesBeforeSecondRound =  await firebaseAPIFetch.GetAllIsReadyAsync(roomCreationResult,2);
            Assert.AreEqual(3, statusesBeforeSecondRound.Count);
            Assert.IsFalse(statusesBeforeSecondRound[0].Ready);
            Assert.IsFalse(statusesBeforeSecondRound[1].Ready);
            Assert.IsFalse(statusesBeforeSecondRound[2].Ready);
            
            // Create battle pairs
            await firebaseWriteAPI.CalculateBattlePairs(roomCreationResult,2);

            // Clean up
            GameObject.DestroyImmediate(gameObject);
        }
    }
}