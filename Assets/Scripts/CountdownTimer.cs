using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI countdownText;

    private float timeRemaining;
    private bool isCounting = false;

    public delegate void CountdownFinished();
    public event CountdownFinished OnCountdownFinished;

    public void StartCountdown(int countdownDurationSecs)
    {
        countdownText.text = countdownDurationSecs.ToString();
        if (!isCounting)
        {
            timeRemaining = countdownDurationSecs;
            StartCoroutine(CountdownCoroutine());
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        isCounting = true;

        while (timeRemaining > 0)
        {
            countdownText.text = Mathf.Ceil(timeRemaining).ToString();
            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        countdownText.text = "0";

        OnCountdownFinished?.Invoke();

        isCounting = false;
        OnCountdownFinished = null;
    }
}
