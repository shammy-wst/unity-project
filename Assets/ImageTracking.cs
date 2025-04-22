using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestionnaire du tracking d'images AR
/// </summary>
public class ImageTracking : ARManagerBase
{
    [Header("Settings")]
    [SerializeField] private string poolTag = "TrackedObject";
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private float trackingLossTimeout = 3.0f; // Temps en secondes avant de masquer l'objet après perte du tracking
    [SerializeField] private float positionSmoothTime = 0.1f; // Temps de lissage pour les mouvements
    
    private ARTrackedImageManager _trackedImageManager;
    public GameObject trackedPrefab;
    private Dictionary<TrackableId, GameObject> spawnedPrefabs = new Dictionary<TrackableId, GameObject>();
    private Dictionary<TrackableId, float> lastTrackingTimes = new Dictionary<TrackableId, float>();
    private Dictionary<TrackableId, Vector3> velocities = new Dictionary<TrackableId, Vector3>();
    private Dictionary<TrackableId, bool> isCurrentlyTracked = new Dictionary<TrackableId, bool>();
    private ObjectPool objectPool;

    // Variable pour le message UI
    private GameObject canvasRechercheImage;
    private bool isAnyImageTracked = false;

    protected override void ValidateComponents()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
        if (_trackedImageManager == null)
        {
            Debug.LogError($"{GetType().Name} : ARTrackedImageManager non trouvé!");
            return;
        }

        if (trackedPrefab == null)
        {
            Debug.LogError($"{GetType().Name} : Aucun prefab défini pour le tracking!");
            return;
        }

        // Trouver le Canvas de recherche d'image
        canvasRechercheImage = GameObject.Find("CanvasRechercheImage");
        if (canvasRechercheImage != null)
        {
            Debug.Log("Canvas de recherche d'image trouvé: " + canvasRechercheImage.name);
        }
        else
        {
            Debug.LogWarning("Canvas 'CanvasRechercheImage' non trouvé!");
        }

        InitializeObjectPool();
    }

    private void InitializeObjectPool()
    {
        objectPool = FindFirstObjectByType<ObjectPool>();
        if (objectPool == null)
        {
            GameObject poolObj = new GameObject("ObjectPool");
            objectPool = poolObj.AddComponent<ObjectPool>();
        }

        // Ajouter le prefab au pool s'il n'existe pas déjà
        if (!objectPool.pools.Exists(p => p.tag == poolTag))
        {
            objectPool.pools.Add(new ObjectPool.Pool
            {
                tag = poolTag,
                prefab = trackedPrefab,
                size = initialPoolSize
            });
        }
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();
        if (_trackedImageManager != null)
        {
            _trackedImageManager.enabled = true;
            ShowFeedback("Mode tracking d'images activé", FeedbackType.Success);
        }

        // Assurer que le canvas de recherche est visible au démarrage
        UpdateSearchingMessageVisibility(false);
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();
        if (_trackedImageManager != null)
        {
            _trackedImageManager.enabled = false;
        }
        CleanupSpawnedPrefabs();
    }

    void Update()
    {
        if (!isEnabled) return;

        bool anyTrackedThisFrame = false;
        
        foreach (var trackedImage in _trackedImageManager.trackables)
        {
            bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
            
            // Mettre à jour l'état de tracking pour cette image
            if (isTracking)
            {
                lastTrackingTimes[trackedImage.trackableId] = Time.time;
                anyTrackedThisFrame = true;
                
                if (!isCurrentlyTracked.ContainsKey(trackedImage.trackableId) || !isCurrentlyTracked[trackedImage.trackableId])
                {
                    isCurrentlyTracked[trackedImage.trackableId] = true;
                    Debug.Log($"Image {trackedImage.referenceImage.name} est maintenant trackée");
                }
            }
            
            // Vérifier si l'image a été perdue récemment mais qu'on veut toujours afficher l'objet
            bool isRecentlyLost = !isTracking && 
                                 lastTrackingTimes.ContainsKey(trackedImage.trackableId) && 
                                 Time.time - lastTrackingTimes[trackedImage.trackableId] < trackingLossTimeout;
            
            if (isTracking || isRecentlyLost)
            {
                HandleTrackedImage(trackedImage, isTracking);
            }
            else
            {
                // Le tracking est perdu depuis trop longtemps
                if (isCurrentlyTracked.ContainsKey(trackedImage.trackableId) && isCurrentlyTracked[trackedImage.trackableId])
                {
                    isCurrentlyTracked[trackedImage.trackableId] = false;
                    Debug.Log($"Image {trackedImage.referenceImage.name} n'est plus trackée");
                }
                
                RemovePrefab(trackedImage.trackableId);
            }
        }
        
        // Mettre à jour l'état global du tracking
        if (anyTrackedThisFrame != isAnyImageTracked)
        {
            isAnyImageTracked = anyTrackedThisFrame;
            UpdateSearchingMessageVisibility(isAnyImageTracked);
        }
        
        // Mettre à jour l'indicateur de tracking
        ShowTrackingState(isAnyImageTracked);
    }

    private void UpdateSearchingMessageVisibility(bool isTracking)
    {
        if (canvasRechercheImage != null)
        {
            canvasRechercheImage.SetActive(!isTracking);
            Debug.Log($"Message de recherche d'image: {(!isTracking ? "affiché" : "masqué")}");
        }
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage, bool isActivelyTracked)
    {
        if (!spawnedPrefabs.ContainsKey(trackedImage.trackableId))
        {
            SpawnPrefab(trackedImage);
        }
        else
        {
            UpdatePrefabPosition(trackedImage, isActivelyTracked);
        }
    }

    private void SpawnPrefab(ARTrackedImage trackedImage)
    {
        Debug.Log($"Image détectée : {trackedImage.referenceImage.name}");

        if (trackedPrefab != null)
        {
            GameObject newPrefab = objectPool.SpawnFromPool(poolTag, trackedImage.transform.position, trackedImage.transform.rotation);
            if (newPrefab != null)
            {
                spawnedPrefabs.Add(trackedImage.trackableId, newPrefab);
                velocities[trackedImage.trackableId] = Vector3.zero;
                ShowFeedback("Image reconnue !", FeedbackType.Success);
            }
            else
            {
                Debug.LogError("Échec de la création d'objet depuis le pool");
            }
        }
    }

    private void UpdatePrefabPosition(ARTrackedImage trackedImage, bool isActivelyTracked)
    {
        if (spawnedPrefabs.TryGetValue(trackedImage.trackableId, out GameObject prefab))
        {
            if (isActivelyTracked)
            {
                // Lissage de la position pour éviter les sauts
                Vector3 targetPosition = trackedImage.transform.position;
                Vector3 currentVelocity = velocities.ContainsKey(trackedImage.trackableId) ? 
                    velocities[trackedImage.trackableId] : Vector3.zero;
                
                // Calculer la nouvelle position lissée
                Vector3 smoothedPosition = Vector3.SmoothDamp(
                    prefab.transform.position, 
                    targetPosition, 
                    ref currentVelocity, 
                    positionSmoothTime
                );
                
                // Mettre à jour la position et la rotation
                prefab.transform.position = smoothedPosition;
                prefab.transform.rotation = Quaternion.Slerp(
                    prefab.transform.rotation, 
                    trackedImage.transform.rotation, 
                    Time.deltaTime / positionSmoothTime
                );
                
                // Sauvegarder la vélocité pour la prochaine frame
                velocities[trackedImage.trackableId] = currentVelocity;
            }
        }
    }

    private void RemovePrefab(TrackableId trackableId)
    {
        if (spawnedPrefabs.TryGetValue(trackableId, out GameObject prefab))
        {
            objectPool.ReturnToPool(poolTag, prefab);
            spawnedPrefabs.Remove(trackableId);
            velocities.Remove(trackableId);
        }
    }

    private void CleanupSpawnedPrefabs()
    {
        foreach (var prefab in spawnedPrefabs.Values)
        {
            if (prefab != null)
            {
                objectPool.ReturnToPool(poolTag, prefab);
            }
        }
        spawnedPrefabs.Clear();
        velocities.Clear();
        lastTrackingTimes.Clear();
        isCurrentlyTracked.Clear();
    }
}
