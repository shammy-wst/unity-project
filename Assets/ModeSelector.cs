using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class ModeSelector : MonoBehaviour
{
    [Header("Canvas UI")]
    public GameObject menuCanvas;
    public GameObject planeCanvas;
    public GameObject imageCanvas;
    private CanvasGroup menuCanvasGroup;
    private CanvasGroup planeCanvasGroup;
    private CanvasGroup imageCanvasGroup;

    [Header("Managers & Scripts")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public PlaceOnPlane placeOnPlaneScript;
    public ARTrackedImageManager trackedImageManager;
    public ImageTracking imageTrackingScript;

    void Start()
    {
        menuCanvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        planeCanvasGroup = planeCanvas.GetComponent<CanvasGroup>();
        imageCanvasGroup = imageCanvas.GetComponent<CanvasGroup>();

        planeManager.enabled = false;
        raycastManager.enabled = false;
        placeOnPlaneScript.enabled = false;
        trackedImageManager.enabled = false;
        imageTrackingScript.enabled = false;

        menuCanvas.SetActive(true);
        planeCanvas.SetActive(false);
        imageCanvas.SetActive(false);

        planeCanvasGroup.alpha = 0f;
        imageCanvasGroup.alpha = 0f;
    }

    public void StartPlaneMode()
    {
        planeManager.enabled = true;
        raycastManager.enabled = true;
        placeOnPlaneScript.enabled = true;

        trackedImageManager.enabled = false;
        imageTrackingScript.enabled = false;

        planeCanvas.SetActive(true);
        imageCanvas.SetActive(false);

        StartCoroutine(FadeOutMenu());
        StartCoroutine(FadeInCanvas(planeCanvasGroup));
    }

    public void StartImageMode()
    {
        planeManager.enabled = false;
        raycastManager.enabled = false;
        placeOnPlaneScript.enabled = false;

        trackedImageManager.enabled = true;
        imageTrackingScript.enabled = true;

        planeCanvas.SetActive(false);
        imageCanvas.SetActive(true);

        StartCoroutine(FadeOutMenu());
        StartCoroutine(FadeInCanvas(imageCanvasGroup));
    }

    public void ReturnToMainMenu()
    {
        planeManager.enabled = false;
        raycastManager.enabled = false;
        placeOnPlaneScript.enabled = false;
        trackedImageManager.enabled = false;
        imageTrackingScript.enabled = false;

        planeCanvas.SetActive(false);
        imageCanvas.SetActive(false);
        menuCanvas.SetActive(true);

        menuCanvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutMenu()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        menuCanvas.SetActive(false);
    }

    IEnumerator FadeInCanvas(CanvasGroup canvasGroup)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
