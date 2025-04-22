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

    [Header("Camera AR")]
    public ARCameraBackground arCameraBackground; // <-- AJOUT ici

    private const float FADE_DURATION = 0.5f;

    void Start()
    {
        InitializeCanvasGroups();
        
        // S'abonner aux événements ARPlane
        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }
        
        DisableAllFeatures();
        ShowMainMenu();
    }

    private void InitializeCanvasGroups()
    {
        menuCanvasGroup = menuCanvas.GetComponent<CanvasGroup>();
        planeCanvasGroup = planeCanvas.GetComponent<CanvasGroup>();
        imageCanvasGroup = imageCanvas.GetComponent<CanvasGroup>();

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

        if (arCameraBackground != null) arCameraBackground.enabled = false; // <-- Désactive l'image caméra au menu
    }

    public void StartPlaneMode()
    {
        // Force direct activation of components
        planeManager.enabled = true;
        raycastManager.enabled = true;
        
        // Vérifier et configurer le prefab pour la visualisation des plans
        if (planeManager != null)
        {
            // Vérifier si un prefab est assigné
            if (planeManager.planePrefab == null)
            {
                Debug.LogError("Aucun prefab assigné au ARPlaneManager! Essayez d'assigner ARPlanePrefab dans l'inspecteur.");
            }
            else
            {
                Debug.Log($"Prefab de plan utilisé: {planeManager.planePrefab.name}");
            }
            
            // Configurer la visualisation des plans
            planeManager.enabled = true;
            planeManager.SetTrackablesActive(true);
            
            // Activer le rendu des plans
            var planesInScene = FindObjectsOfType<ARPlane>();
            Debug.Log($"Nombre de plans détectés: {planesInScene.Length}");
            foreach (var plane in planesInScene)
            {
                plane.gameObject.SetActive(true);
                if (plane.GetComponent<Renderer>() != null)
                {
                    plane.GetComponent<Renderer>().enabled = true;
                }
            }
            
            Debug.Log($"Visualisation des plans activée: planeManager.enabled={planeManager.enabled}");
        }
        
        if (placeOnPlaneScript != null) {
            placeOnPlaneScript.arCamera = Camera.main; // Force camera reference
            placeOnPlaneScript.Enable();
            // Double-check critical components
            Debug.Log($"PlaneMode activated: planeManager={planeManager.enabled}, raycastManager={raycastManager.enabled}");
        }
        
        // Activer la caméra AR pour le mode surface aussi
        if (arCameraBackground != null) {
            arCameraBackground.enabled = true;
            Debug.Log("ARCameraBackground activé pour le mode surface");
        }
        
        StartCoroutine(SwitchToMode(planeCanvas, planeCanvasGroup));
    }

    public void StartImageMode()
    {
        if (placeOnPlaneScript != null) placeOnPlaneScript.Disable();
        if (imageTrackingScript != null) imageTrackingScript.Enable();

        if (arCameraBackground != null) arCameraBackground.enabled = true; // <-- Active la caméra
        StartCoroutine(SwitchToMode(imageCanvas, imageCanvasGroup));
    }

    public void ReturnToMainMenu()
    {
        DisableAllFeatures();
        
        if (arCameraBackground != null) arCameraBackground.enabled = false; // <-- Désactive la caméra

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

    private void OnDestroy()
    {
        // Se désabonner aux événements
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }
    
    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Activer le rendu des nouveaux plans
        foreach (var plane in args.added)
        {
            Debug.Log($"Nouveau plan détecté: {plane.trackableId}");
            plane.gameObject.SetActive(true);
            
            // Activer le renderer
            if (plane.GetComponent<Renderer>() != null)
            {
                var renderer = plane.GetComponent<Renderer>();
                renderer.enabled = true;
                
                // Vérifier si le matériau est visible
                if (renderer.material != null)
                {
                    Debug.Log($"Matériau du plan: {renderer.material.name}");
                    
                    // S'assurer que le matériau est visible
                    if (renderer.material.color.a < 0.1f)
                    {
                        Color color = renderer.material.color;
                        color.a = 0.5f; // Rendre le matériau semi-transparent
                        renderer.material.color = color;
                    }
                }
            }
        }
    }
}
