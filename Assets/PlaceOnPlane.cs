using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaceOnPlane : MonoBehaviour
{
    public GameObject objectToPlace;
    private ARRaycastManager _raycastManager;
    private ARPlaneManager _planeManager;
    public Camera arCamera;

    private List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    void Start()
    {
        _raycastManager = GetComponent<ARRaycastManager>();
        _planeManager = GetComponent<ARPlaneManager>();

        Debug.Log("PlaceOnPlane : Script démarré, prêt.");
    }

    void Update()
    {
        if (_planeManager.trackables.count == 0)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("Touch détecté à : " + touch.position);

                Ray ray = arCamera.ScreenPointToRay(touch.position);

                if (_raycastManager.Raycast(ray, _hits, TrackableType.PlaneWithinPolygon))
                {
                    var hitPlane = _planeManager.GetPlane(_hits[0].trackableId);

                    if (hitPlane.trackingState == TrackingState.Tracking)
                    {
                        Debug.Log("Raycast sur un plan actif.");

                        Pose hitPose = _hits[0].pose;
                        Instantiate(objectToPlace, hitPose.position, hitPose.rotation);

                        Debug.Log("Objet instancié à : " + hitPose.position);

                        // ➔ Correction ici avec FindFirstObjectByType
                        FindFirstObjectByType<FeedbackManager>().ShowMessage("Surface détectée !");
                    }
                    else
                    {
                        Debug.LogWarning("Raycast trouvé un plan non tracké, pas d'instanciation.");
                    }
                }
                else
                {
                    Debug.LogWarning("Raycast échoué : aucun plan sous le doigt.");
                }
            }
        }
    }
}
