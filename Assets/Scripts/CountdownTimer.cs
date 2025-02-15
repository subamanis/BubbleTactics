using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI countdownText;

    private float timeRemaining;
    private bool isCounting = false;

    public event Action OnCountdownFinished;

    public void StartCountdown(int countdownDurationSecs, Action action)
    {
        print($"Inside startCountdown for {countdownDurationSecs} seconds, isCounting: {isCounting}.");
        if (!isCounting)
        {
            print($"Is not counting, so starting countdown");
            timeRemaining = countdownDurationSecs;
            OnCountdownFinished += action;
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
        isCounting = false;



        print("Countdown ended. Will call method: ");
        print(OnCountdownFinished?.Method);

        countdownText.text = "";

        var oldInvocationList = OnCountdownFinished?.GetInvocationList();
        OnCountdownFinished?.Invoke();

        if (oldInvocationList.Length > 0)
        {
            foreach (Delegate d in oldInvocationList)
            {
                print("delegate in list: "+d.Method);
                OnCountdownFinished -= (Action)d;
            }
        }
    }
}
