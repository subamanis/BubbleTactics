using System.Collections;
using UnityEngine;

public class OpacityChanger : MonoBehaviour
{
    public CanvasGroup canvasGroup;  // Reference to the CanvasGroup
    public float fadeDuration = 5000f;  // Duration for the fade-in effect

    private void Start()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();  // Get the CanvasGroup if not assigned in the inspector
        }

        // Initially set the CanvasGroup alpha to 0 (hidden)
        canvasGroup.alpha = 0f;

        // Start the fade-in process after a 5-second delay
        StartCoroutine(FadeInAfterDelay(13.5f));
    }

    private IEnumerator FadeInAfterDelay(float delay)
    {
        // Wait for the specified delay (5 seconds)
        yield return new WaitForSeconds(delay);

        // Fade in by increasing alpha over time
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Ensure the final alpha is set to 1 (fully visible)
        //canvasGroup.alpha = 1f;
    }
}
