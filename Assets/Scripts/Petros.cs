using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class Petros : MonoBehaviour
{
    private DatabaseReference databaseReference;
    private const string roomsPath = "rooms"; // Assuming rooms are stored under 'rooms' in your database

    private void Start()
    {
        // Initialize Firebase and create a room
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        // Initialize Firebase asynchronously
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

            Debug.Log("app: "+app);

            // Now that Firebase is initialized, create the room
             CreateRoomAsync();
        });
    }

    public string CreateRoomAsync()
    {
        bool roomExists = false;
        string roomIdChosen = null;

        do
        {
            // Generate a random room ID (4 digits, over 1000)
            roomIdChosen = UnityEngine.Random.Range(1000, 10000).ToString();
            DatabaseReference roomPath = FirebaseDatabase.DefaultInstance.GetReference($"{roomsPath}/{roomIdChosen}");

            // Check if the room already exists
            DataSnapshot snapshot = await roomPath.GetValueAsync();
            roomExists = snapshot.Exists;

        } while (roomExists); // Keep checking until a unique room ID is found

        // Set the room state to "WaitingForPlayers"
        DatabaseReference newRoomPath = FirebaseDatabase.DefaultInstance.GetReference($"{roomsPath}/{roomIdChosen}");
        await newRoomPath.SetValueAsync(new { RoomState = "WaitingForPlayers" });

        Debug.Log($"Room {roomIdChosen} created");

        // Print the room ID to the console
        Debug.Log($"The created room ID is: {roomIdChosen}");
    }
}
