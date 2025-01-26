using Prefabs.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TeaserTrailerManager : MonoBehaviour
{
    public string chosenUserName;
    public BubbleBehaviour bubble;
    public TMP_InputField roomIdInput;


    public void SetPlayerUserName(string userName)
    {
        this.chosenUserName = userName;
        bubble.SetPlayerUserName(userName);
    }

    public void CopyUserNameClicked()
    {
        Debug.Log("Copying user name");
        SetPlayerUserName(roomIdInput.text);
    }

    public void StartGameClicked()
    {
        Debug.Log("Starting game...");
    }
}
