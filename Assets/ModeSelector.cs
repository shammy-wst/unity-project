using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System;
using UnityEngine.XR.ARSubsystems;
using System.Linq;

#pragma warning disable 0618  // Supprime les avertissements d'obsolescence

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

    [Header("Camera AR")]
    public ARCameraBackground arCameraBackground;

    private const float FADE_DURATION = 0.5f;

    private void OnEnable()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }
    }

    private void OnDisable()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }

    private void Start()
    {
        InitializeCanvasGroups();
        DisableAllFeatures();
        ShowMainMenu();

        var monoBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var features = monoBehaviours.OfType<IARFeature>();
        foreach (var feature in features)
        {
            feature.Enable();
        }
    }

    private void InitializeCanvasGroups()
    {
        menuCanvasGroup = menuCanvas?.GetComponent<CanvasGroup>();
        planeCanvasGroup = planeCanvas?.GetComponent<CanvasGroup>();
        imageCanvasGroup = imageCanvas?.GetComponent<CanvasGroup>();

        if (menuCanvasGroup == null || planeCanvasGroup == null || imageCanvasGroup == null)
        {
            Debug.LogError("CanvasGroup manquant sur un des canvas!");
        }
    }

    private void DisableAllFeatures()
    {
        if (placeOnPlaneScript != null) placeOnPlaneScript.Disable();
        if (imageTrackingScript != null) imageTrackingScript.Disable();
    }

    private void ShowMainMenu()
    {
        menuCanvas.SetActive(true);
        planeCanvas.SetActive(false);
        imageCanvas.SetActive(false);

        menuCanvasGroup.alpha = 1f;
        planeCanvasGroup.alpha = 0f;
        imageCanvasGroup.alpha = 0f;

        if (arCameraBackground != null) arCameraBackground.enabled = false;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (args.added != null)
        {
            foreach (var plane in args.added)
            {
                if (plane != null)
                {
                    plane.gameObject.SetActive(true);
                    var renderer = plane.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        if (renderer.material != null && renderer.material.color.a < 0.1f)
                        {
                            var color = renderer.material.color;
                            color.a = 0.5f;
                            renderer.material.color = color;
                        }
                    }
                }
            }
        }
    }

    public void StartPlaneMode()
    {
        planeManager.enabled = true;
        raycastManager.enabled = true;
        
        if (planeManager != null)
        {
            if (planeManager.planePrefab == null)
            {
                Debug.LogError("Aucun prefab assigné au ARPlaneManager!");
                return;
            }
            
            planeManager.enabled = true;
            planeManager.SetTrackablesActive(true);
            
            var planes = UnityEngine.Object.FindObjectsByType<ARPlane>(FindObjectsSortMode.None);
            foreach (var plane in planes)
            {
                if (plane != null)
                {
                    plane.gameObject.SetActive(true);
                    var renderer = plane.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }
        
        if (placeOnPlaneScript != null)
        {
            placeOnPlaneScript.arCamera = Camera.main;
            placeOnPlaneScript.Enable();
        }
        
        if (arCameraBackground != null)
        {
            arCameraBackground.enabled = true;
        }
        
        StartCoroutine(SwitchToMode(planeCanvas, planeCanvasGroup));
    }

    public void StartImageMode()
    {
        if (placeOnPlaneScript != null) placeOnPlaneScript.Disable();
        if (imageTrackingScript != null) imageTrackingScript.Enable();

        if (arCameraBackground != null) arCameraBackground.enabled = true;
        StartCoroutine(SwitchToMode(imageCanvas, imageCanvasGroup));
    }

    public void ReturnToMainMenu()
    {
        DisableAllFeatures();
        
        if (arCameraBackground != null) arCameraBackground.enabled = false;

        StartCoroutine(SwitchToMainMenu());
    }

    private IEnumerator SwitchToMode(GameObject targetCanvas, CanvasGroup targetCanvasGroup)
    {
        targetCanvas.SetActive(true);
        yield return StartCoroutine(UIAnimationUtility.FadeOutCanvas(menuCanvasGroup, FADE_DURATION));
        menuCanvas.SetActive(false);
        yield return StartCoroutine(UIAnimationUtility.FadeInCanvas(targetCanvasGroup, FADE_DURATION));
    }

    private IEnumerator SwitchToMainMenu()
    {
        yield return StartCoroutine(UIAnimationUtility.FadeOutCanvas(planeCanvasGroup, FADE_DURATION));
        yield return StartCoroutine(UIAnimationUtility.FadeOutCanvas(imageCanvasGroup, FADE_DURATION));
        
        planeCanvas.SetActive(false);
        imageCanvas.SetActive(false);
        menuCanvas.SetActive(true);
        
        yield return StartCoroutine(UIAnimationUtility.FadeInCanvas(menuCanvasGroup, FADE_DURATION));
    }
}
