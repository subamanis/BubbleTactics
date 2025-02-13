using System.Collections;
using TMPro;
using UnityEngine;

public class ErrorTextDisplay : MonoBehaviour
{
    public TextMeshProUGUI errorText;
    public float displayDuration = 3f;
    public float fadeSpeed = 1f;

    public void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        errorText.gameObject.SetActive(true);

        StartCoroutine(DisplayAndFade());
    }

    private IEnumerator DisplayAndFade()
    {
        yield return new WaitForSeconds(displayDuration);

        float elapsedTime = 0f;
        Color initialColor = errorText.color;

        while (elapsedTime < fadeSpeed)
        {
            errorText.color = Color.Lerp(initialColor, new Color(initialColor.r, initialColor.g, initialColor.b, 0), elapsedTime / fadeSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        errorText.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0);
        errorText.gameObject.SetActive(false);
    }
}
