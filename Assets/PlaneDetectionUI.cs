using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro; // Important pour TextMeshPro

public class PlaneDetectionUI : MonoBehaviour
{
    public ARPlaneManager planeManager;
    public TextMeshProUGUI statusText;

    void Update()
    {
        if (planeManager.trackables.count > 0)
        {
            statusText.text = "Surface détectée ! Appuyez pour placer.";
        }
        else
        {
            statusText.text = "Recherche de surface...";
        }
    }
}
