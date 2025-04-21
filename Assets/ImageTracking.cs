using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ImageTracking : MonoBehaviour
{
    private ARTrackedImageManager _trackedImageManager;

    public GameObject trackedPrefab;
    private Dictionary<TrackableId, GameObject> spawnedPrefabs = new Dictionary<TrackableId, GameObject>();

    void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void Update()
    {
        foreach (var trackedImage in _trackedImageManager.trackables)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (!spawnedPrefabs.ContainsKey(trackedImage.trackableId))
                {
                    Debug.Log("Image détectée : " + trackedImage.referenceImage.name);

                    if (trackedPrefab != null)
                    {
                        GameObject newPrefab = Instantiate(trackedPrefab, trackedImage.transform.position, trackedImage.transform.rotation);
                        spawnedPrefabs.Add(trackedImage.trackableId, newPrefab);
                    }
                }
                else
                {
                    GameObject prefab = spawnedPrefabs[trackedImage.trackableId];
                    prefab.transform.position = trackedImage.transform.position;
                    prefab.transform.rotation = trackedImage.transform.rotation;
                }
            }
            else
            {
                if (spawnedPrefabs.ContainsKey(trackedImage.trackableId))
                {
                    GameObject prefab = spawnedPrefabs[trackedImage.trackableId];
                    Destroy(prefab);
                    spawnedPrefabs.Remove(trackedImage.trackableId);
                }
            }
        }
    }
}
