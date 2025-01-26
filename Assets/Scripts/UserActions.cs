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
    private string currentPlayerId;
    private string currentRoomId;
    public int CurrentRoundId { get; private set; } // Store the current round ID
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

    public void UserClickedCreateRoom () {
        this.firebaseWriteAPI.CreateRoomAsync(playerNameInput.text).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully)
            {
                var result = task.Result;
                currentRoomId = result.roomId;
                currentPlayerId = result.playerId;

                Debug.Log($"Room created with ID: {currentRoomId}, Player ID: {currentPlayerId}");
                
                ObserveRounds(currentRoomId);
            }
            else
            {
                Debug.LogError("Failed to create room: " + task.Exception?.Message);
            }
        });
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
        this.firebaseWriteAPI.UpdateIsReadyForPlayerAsync(currentRoomId, CurrentRoundId, currentPlayerId, true).ContinueWith(task => {});
    }

    private void ObserveRounds(string roomId)
    {
        Debug.Log($"observer 1");

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
}
