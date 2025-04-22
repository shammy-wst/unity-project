using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneVisualizerCleaner : MonoBehaviour
{
    private ARPlaneManager _planeManager;

    void Start()
    {
        _planeManager = GetComponent<ARPlaneManager>();
    }

    void Update()
    {
        foreach (var plane in _planeManager.trackables)
        {
            plane.GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
