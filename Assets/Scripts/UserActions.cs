using TMPro;
using UnityEngine;

public class UserActions: MonoBehaviour
{
    private FirebaseWriteAPI firebaseWriteAPI;
    public TMP_InputField roomIdInput;

    void Start() {
        this.firebaseWriteAPI = this.GetComponent<FirebaseWriteAPI>();
    }

    public void UserClickedCreateRoom () {
        this.firebaseWriteAPI.CreateRoomAsync().ContinueWith(task => {});
    }

    public void UserClickedJoinRoom () {
        this.firebaseWriteAPI.JoinRoomAsync(roomIdInput.text, "Bubble Slayer").ContinueWith(task => {});
    }
}
