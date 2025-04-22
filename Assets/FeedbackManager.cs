using System.Collections;
using UnityEngine;
using TMPro;

public class FeedbackManager : MonoBehaviour
{
    public CanvasGroup feedbackCanvasGroup;
    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        feedbackCanvasGroup.alpha = 0f;
    }

    public void ShowMessage(string message, float displayDuration = 2f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeMessage(message, displayDuration));
    }

    IEnumerator FadeMessage(string message, float displayDuration)
    {
        feedbackText.text = message;

        // Fade In
        float duration = 0.5f;
        float elapsed = 0f;
        feedbackCanvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            feedbackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        feedbackCanvasGroup.alpha = 1f;

        // Attendre
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            feedbackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        feedbackCanvasGroup.alpha = 0f;
        feedbackText.text = "";
    }
}
