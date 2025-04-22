using UnityEngine;
using System.Collections;

public static class UIAnimationUtility
{
    public static IEnumerator FadeCanvas(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    public static IEnumerator FadeInCanvas(CanvasGroup canvasGroup, float duration = 0.5f)
    {
        canvasGroup.alpha = 0f;
        yield return FadeCanvas(canvasGroup, 1f, duration);
    }

    public static IEnumerator FadeOutCanvas(CanvasGroup canvasGroup, float duration = 0.5f)
    {
        yield return FadeCanvas(canvasGroup, 0f, duration);
    }
} 