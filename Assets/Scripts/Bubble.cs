using UnityEngine;
using TMPro;

public class Bubble : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI lastRoundScoreText;
    public bool isCurrentPlayer;

    private string id;
    private int score; 

    public void Initialize(string id, string playerName, int startingScore, bool isCurrentPlayer)
    {
        this.id = id;
        this.isCurrentPlayer = isCurrentPlayer;
        nameText.text = playerName;
        score = startingScore;
        totalScoreText.text = score.ToString();
        lastRoundScoreText.text = "0";
        gameObject.SetActive(true);
    }

    public void UpdateScore(int newTotalScore)
    {
        lastRoundScoreText.text = (newTotalScore - score).ToString();
        totalScoreText.text = newTotalScore.ToString();
    }
}
