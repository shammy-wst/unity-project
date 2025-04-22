using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

/// <summary>
/// Gestionnaire de placement d'objets sur les plans AR
/// </summary>
public class PlaceOnPlane : ARManagerBase
{
    [Header("Settings")]
    [SerializeField] private string poolTag = "PlacedObject";
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private float raycastCacheDuration = 0.1f;
    
    public GameObject objectToPlace;
    private ARRaycastManager _raycastManager;
    private ARPlaneManager _planeManager;
    public Camera arCamera;

    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private ObjectPool objectPool;
    private RaycastCache raycastCache;

    private class RaycastCache
    {
        public Vector2 touchPosition;
        public List<ARRaycastHit> hits;
        public float timestamp;
    }

    protected override void ValidateComponents()
    {
        _raycastManager = GetComponent<ARRaycastManager>();
        _planeManager = GetComponent<ARPlaneManager>();

        if (_raycastManager == null || _planeManager == null || arCamera == null)
        {
            Debug.LogError($"{GetType().Name} : Composants AR requis non trouvés!");
            return;
        }

        if (objectToPlace == null)
        {
            Debug.LogError($"{GetType().Name} : Aucun objet à placer défini!");
            return;
        }

        InitializeObjectPool();
        raycastCache = new RaycastCache();
    }

    private void InitializeObjectPool()
    {
        objectPool = FindFirstObjectByType<ObjectPool>();
        if (objectPool == null)
        {
            GameObject poolObj = new GameObject("ObjectPool");
            objectPool = poolObj.AddComponent<ObjectPool>();
        }

        if (!objectPool.pools.Exists(p => p.tag == poolTag))
        {
            objectPool.pools.Add(new ObjectPool.Pool
            {
                tag = poolTag,
                prefab = objectToPlace,
                size = initialPoolSize
            });
        }
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();
        if (_raycastManager != null && _planeManager != null)
        {
            _raycastManager.enabled = true;
            _planeManager.enabled = true;
            ShowFeedback("Mode placement activé", FeedbackType.Success);
        }
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();
        if (_raycastManager != null && _planeManager != null)
        {
            _raycastManager.enabled = false;
            _planeManager.enabled = false;
        }
    }

    void Update()
    {
        if (!isEnabled) return;

        // Ne pas afficher les messages de debug si le jeu a commencé
        bool gameStarted = (GameManager.Instance != null && GameManager.Instance.HasGameStarted());
        
        // Vérification supplémentaire des composants
        if (_planeManager == null)
        {
            Debug.LogError("_planeManager est null dans Update");
            return;
        }

        // Log pour le débogage seulement si le jeu n'a pas commencé
        if (Time.frameCount % 60 == 0 && !gameStarted) // Log toutes les secondes environ
        {
            Debug.Log($"État du PlaceOnPlane: isEnabled={isEnabled}, planeManager.enabled={_planeManager.enabled}, planeManager.trackables.count={_planeManager.trackables.count}");
            
            // Vérifier si des plans sont présents mais non visibles
            if (_planeManager.trackables.count > 0)
            {
                Debug.Log("Plans détectés mais peut-être non visibles. Vérifiez le material dans le prefab de plan.");
                
                // Forcer l'activation des plans
                foreach (var plane in _planeManager.trackables)
                {
                    plane.gameObject.SetActive(true);
                }
            }
        }

        if (_planeManager.trackables.count == 0)
        {
            ShowTrackingState(false);
            return;
        }

        ShowTrackingState(true);
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                ProcessTouch(touch);
            }
        }
    }

    private void ProcessTouch(Touch touch)
    {
        Debug.Log($"Touch détecté à : {touch.position}");

        if (raycastCache == null)
        {
            raycastCache = new RaycastCache();
            Debug.Log("RaycastCache initialisé");
        }

        if (arCamera == null)
        {
            Debug.LogError("La caméra AR n'est pas assignée ! Réassignation automatique...");
            arCamera = Camera.main;
            if (arCamera == null)
            {
                Debug.LogError("Impossible de trouver la caméra principale");
                ShowFeedback("Erreur caméra", FeedbackType.Error);
                return;
            }
        }

        if (IsCachedRaycastValid(touch.position))
        {
            HandleRaycastHit();
        }
        else
        {
            PerformNewRaycast(touch.position);
        }
    }

    private bool IsCachedRaycastValid(Vector2 touchPosition)
    {
        return raycastCache.hits != null &&
               raycastCache.touchPosition == touchPosition &&
               Time.time - raycastCache.timestamp < raycastCacheDuration;
    }

    private void PerformNewRaycast(Vector2 touchPosition)
    {
        // Vérifications des composants critiques
        if (arCamera == null)
        {
            Debug.LogError("arCamera est null dans PerformNewRaycast");
            return;
        }

        if (_raycastManager == null)
        {
            Debug.LogError("_raycastManager est null dans PerformNewRaycast");
            return;
        }

        if (_hits == null)
        {
            Debug.LogError("_hits est null dans PerformNewRaycast, réinitialisation...");
            _hits = new List<ARRaycastHit>();
        }

        Ray ray = arCamera.ScreenPointToRay(touchPosition);
        Debug.Log($"Tentative de raycast depuis caméra: {arCamera.name} à position: {touchPosition}");

        if (_raycastManager.Raycast(ray, _hits, TrackableType.PlaneWithinPolygon))
        {
            if (_hits.Count > 0)
            {
                Debug.Log($"Raycast réussi: {_hits.Count} hits trouvés");
                
                // Initialiser raycastCache si nécessaire
                if (raycastCache == null)
                {
                    raycastCache = new RaycastCache();
                }
                
                raycastCache.touchPosition = touchPosition;
                raycastCache.hits = new List<ARRaycastHit>(_hits);
                raycastCache.timestamp = Time.time;
                HandleRaycastHit();
            }
            else
            {
                Debug.LogWarning("Raycast a retourné true mais aucun hit.");
                ShowFeedback("Surface non trouvée", FeedbackType.Error);
            }
        }
        else
        {
            Debug.LogWarning("Raycast échoué : aucun plan sous le doigt.");
            ShowFeedback("Aucune surface détectée", FeedbackType.Error);
        }
    }

    private void HandleRaycastHit()
    {
        // Vérifications de sécurité
        if (_hits == null || _hits.Count == 0)
        {
            Debug.LogError("_hits est null ou vide dans HandleRaycastHit");
            return;
        }

        if (_planeManager == null)
        {
            Debug.LogError("_planeManager est null dans HandleRaycastHit");
            return;
        }

        var hitPlane = _planeManager.GetPlane(_hits[0].trackableId);

        if (hitPlane == null)
        {
            Debug.LogWarning("Impossible de trouver le plan correspondant au trackableId");
            ShowFeedback("Plan non trouvé", FeedbackType.Error);
            return;
        }

        if (hitPlane.trackingState == TrackingState.Tracking)
        {
            PlaceObject(_hits[0].pose);
        }
        else
        {
            Debug.LogWarning("Raycast trouvé un plan non tracké, pas d'instanciation.");
            ShowFeedback("Surface non stable", FeedbackType.Error);
        }
    }

    private void PlaceObject(Pose hitPose)
    {
        if (!GameManager.Instance.CanPlaceCube())
        {
            ShowFeedback("Maximum de cubes atteint!", FeedbackType.Error);
            return;
        }

        GameObject placedObject = objectPool.SpawnFromPool(poolTag, hitPose.position, hitPose.rotation);
        
        if (placedObject != null)
        {
            // Ajouter le composant DefenseCube s'il n'existe pas déjà
            DefenseCube defenseCube = placedObject.GetComponent<DefenseCube>();
            if (defenseCube == null)
            {
                defenseCube = placedObject.AddComponent<DefenseCube>();
            }

            // Ajouter un Collider si nécessaire
            if (placedObject.GetComponent<Collider>() == null)
            {
                placedObject.AddComponent<BoxCollider>();
            }

            // Définir la couche pour les cubes de défense
            placedObject.layer = LayerMask.NameToLayer("DefenseCube");

            // Informer le GameManager du nouveau cube
            GameManager.Instance.AddCube(placedObject);
            
            ShowFeedback("Cube placé!", FeedbackType.Success);
        }
        else
        {
            ShowFeedback("Erreur lors du placement", FeedbackType.Error);
        }
    }
}
