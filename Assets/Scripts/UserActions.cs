using TMPro;
using UnityEngine;

public class UserActions: MonoBehaviour
{
    private FirebaseWriteAPI firebaseWriteAPI;
    public TMP_InputField roomIdInput;
    public TMP_InputField playerNameInput;

    void Start() {
        this.firebaseWriteAPI = this.GetComponent<FirebaseWriteAPI>();
    }

    public void UserClickedCreateRoom () {
        this.firebaseWriteAPI.CreateRoomAsync(playerNameInput.text).ContinueWith(task => {});
    }

    public void UserClickedJoinRoom () {
        this.firebaseWriteAPI.JoinRoomAsync(roomIdInput.text, playerNameInput.text).ContinueWith(task => {});
    }
}
