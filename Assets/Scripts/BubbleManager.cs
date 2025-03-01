using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class BubbleManager : MonoBehaviour
{
    public readonly int MAX_PLAYERS = 8;
    public readonly int BUBBLE_INITIAL_SCORE = 5;
    public readonly float STARTING_TO_FINAL_POSITION_MOVEMENT_DURATION_SEC = 2.0f;
    public static float StandardOffset = 4f; 
    public GameObject BubblePrefab;
    public Transform BubbleContainer;

    /*
        Mobile Screen Layout (Bubble Final Positions):
        +-----------------------+
        |           p1          | 
        |                       |
        |     p2         p3     |
        |                       |
        |  p4               p5  | 
        |                       |
        |     p6         p7     |   
        |                       |   
        |           p8          |   
        +-----------------------+
    */
    public Transform[] BubblePositions;

    private Vector3[] spawnOffsets = new Vector3[]
    {
        new Vector3(0, StandardOffset, 0),    
        new Vector3(-StandardOffset, StandardOffset, 0), 
        new Vector3(StandardOffset, StandardOffset, 0),  
        new Vector3(-StandardOffset, 0, 0),   
        new Vector3(StandardOffset, 0, 0),    
        new Vector3(-StandardOffset, -StandardOffset, 0),
        new Vector3(StandardOffset, -StandardOffset, 0), 
        new Vector3(0, -StandardOffset, 0),   
    };
    private Dictionary<string, Bubble> playerBubbles = new Dictionary<string, Bubble>();
    private Queue<Transform> availablePositions = new Queue<Transform>();

    private void Start()
    {
        foreach (Transform pos in BubblePositions)
        {
            availablePositions.Enqueue(pos);
        }
    }

    public void AddPlayer(Player player, bool isCurrentPlayer, bool shouldLerpToPosition)
    {
        if (playerBubbles.ContainsKey(player.Id) || availablePositions.Count == 0) {
            Debug.LogWarning("Bubble instantiation failed, due to no more available possitions or id already existing.");
            return;
        }

        Transform assignedPosition = availablePositions.Dequeue();

        GameObject bubbleObject;
        print("Spawning in pos: "+ assignedPosition.position+", spawnOffset: " + spawnOffsets[MAX_PLAYERS - availablePositions.Count - 1] + "availablePosCount: "+ availablePositions.Count+", spawnOffsetsIndex: "+ (MAX_PLAYERS - availablePositions.Count - 1));
        var startingPosition = shouldLerpToPosition ? assignedPosition.position + spawnOffsets[MAX_PLAYERS - availablePositions.Count - 1] : assignedPosition.position;
        if (shouldLerpToPosition) {
            bubbleObject = Instantiate(BubblePrefab, startingPosition, Quaternion.identity, BubbleContainer);
        } else {
            bubbleObject = Instantiate(BubblePrefab, assignedPosition.position, Quaternion.identity, BubbleContainer);
        }
        Bubble bubble = bubbleObject.GetComponent<Bubble>();
        bubble.Initialize(player.Id, player.Name, BUBBLE_INITIAL_SCORE, isCurrentPlayer);
        playerBubbles[player.Id] = bubble;

        if (shouldLerpToPosition) {
            StartCoroutine(MoveOverTime(bubbleObject.transform, startingPosition, assignedPosition.position, STARTING_TO_FINAL_POSITION_MOVEMENT_DURATION_SEC));
        }
    }

    public void RemovePlayer(Player player)
    {
        throw new NotImplementedException("Implement the disconnect case you lazy motherfucker.");
    }

    public void UpdateScore(string playerId, int newTotalScore, int lastRoundScore)
    {
        if (playerBubbles.TryGetValue(playerId, out Bubble bubble))
        {
            bubble.UpdateScore(newTotalScore, lastRoundScore);
        }
    }

    public void RemovePlayer(string playerId)
    {
        if (playerBubbles.TryGetValue(playerId, out Bubble bubble))
        {
            availablePositions.Enqueue(bubble.transform);
            Destroy(bubble.gameObject);
            playerBubbles.Remove(playerId);
        }
    }

    private IEnumerator MoveOverTime(Transform obj, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            obj.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        // Make sure it ends exactly at the final position
        obj.position = endPos;
    }
}
