using System;
using System.Collections;
using System.Collections.Generic;
using Prefabs.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TeaserTrailerManager : MonoBehaviour
{
    public string chosenUserName;
    public BubbleBehaviour bubble;
    public TMP_InputField roomIdInput;
    
    public RectTransform staringGameTransform;
    public RectTransform roundScreenTransform;
    public RectTransform fusionScreenTransform;
    
    public BubbleBehaviour bubblePrefab;
    
    public List<string> PlayerNames = new List<string>();
    private int playerNameIndex = 0;
    
    private BubbleBehaviour onePlayer;
    private BubbleBehaviour twoPlayer;

    private void Start()
    {
        staringGameTransform.gameObject.SetActive(true);
        roundScreenTransform.gameObject.SetActive(false);
        fusionScreenTransform.gameObject.SetActive(false);
    }

    public void SetPlayerUserName(string userName)
    {
        this.chosenUserName = userName;
        bubble.SetPlayerUserName(userName);
    }

    public void CopyUserNameClicked()
    {
        Debug.Log("Copying user name");
        ChoseNextName();
        SetPlayerUserName(PlayerNames[playerNameIndex]);
    }

    private void ChoseNextName()
    {
        playerNameIndex++;
        if (playerNameIndex >= PlayerNames.Count)
            playerNameIndex = 0;
    }

    public void StartGameClicked()
    {
        Debug.Log("Starting game...");
        this.staringGameTransform.gameObject.SetActive(false);
        this.roundScreenTransform.gameObject.SetActive(true);

        StartCoroutine(bubble.MoveBubbleSmoothly(new Vector3(0.47f, 0.49f, 0.34f), 1f));
        bubble.isHovering = true;

        ChoseNextName();
        onePlayer = Instantiate(bubblePrefab, new Vector3(3, 2, +4), Quaternion.identity);
        StartCoroutine(onePlayer.MoveBubbleSmoothly(new Vector3(-0.56f, -0.88f, 0.33f), 1.5f));
        onePlayer.SetPlayerUserName(PlayerNames[playerNameIndex]);
        
        ChoseNextName();
        twoPlayer = Instantiate(bubblePrefab, new Vector3(1, 2,-5), Quaternion.identity);
        StartCoroutine(twoPlayer.MoveBubbleSmoothly(new Vector3(0.08f, -0.33f, -0.41f), 2f));
        twoPlayer.SetPlayerUserName(PlayerNames[playerNameIndex]);
    }

    public void MoveToFusionScreen()
    {
        roundScreenTransform.gameObject.SetActive(false);
        fusionScreenTransform.gameObject.SetActive(true);
        StartCoroutine(onePlayer.MoveBubbleSmoothly(new Vector3(3, 2, +4), 1.5f));
        StartCoroutine(twoPlayer.MoveBubbleSmoothly(new Vector3(1, 2,-5), 2f));
        StartCoroutine(bubble.MoveBubbleSmoothly(new Vector3(0.0f, -0.2f, 0f), 2f));
    }
}
