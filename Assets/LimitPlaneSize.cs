using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlane))]
public class LimitPlaneSize : MonoBehaviour
{
    private ARPlane plane;
    public float maxPlaneSize = 1.5f; // Rayon max en mètres

    void Awake()
    {
        plane = GetComponent<ARPlane>();
    }

    void Update()
    {
        // Si le plan devient trop grand, on le désactive
        if (plane.extents.magnitude > maxPlaneSize)
        {
            gameObject.SetActive(false);
        }
    }
}
