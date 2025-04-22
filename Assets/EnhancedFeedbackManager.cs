using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Gestionnaire de feedback amélioré avec sons et vibrations
/// </summary>
public class EnhancedFeedbackManager : MonoBehaviour
{
    [Header("UI Components")]
    public CanvasGroup feedbackCanvasGroup;
    public TextMeshProUGUI feedbackText;
    public GameObject trackingIndicator;

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip trackingSound;
    private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float defaultDisplayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float vibrationDuration = 0.1f;
    [SerializeField] private float vibrationIntensity = 0.5f;

    private void Start()
    {
        InitializeComponents();
    }

    /// <summary>
    /// Initialise les composants nécessaires
    /// </summary>
    private void InitializeComponents()
    {
        if (feedbackCanvasGroup == null || feedbackText == null)
        {
            Debug.LogError("Composants UI manquants dans EnhancedFeedbackManager!");
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        feedbackCanvasGroup.alpha = 0f;

        if (trackingIndicator != null)
        {
            trackingIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// Affiche un message avec feedback enrichi
    /// </summary>
    /// <param name="message">Message à afficher</param>
    /// <param name="feedbackType">Type de feedback à appliquer</param>
    /// <param name="displayDuration">Durée d'affichage du message</param>
    public void ShowMessage(string message, FeedbackType feedbackType = FeedbackType.Info, float displayDuration = -1)
    {
        if (displayDuration < 0)
        {
            displayDuration = defaultDisplayDuration;
        }

        StopAllCoroutines();
        StartCoroutine(ShowFeedbackCoroutine(message, feedbackType, displayDuration));
    }

    /// <summary>
    /// Affiche l'indicateur de tracking
    /// </summary>
    /// <param name="isTracking">État du tracking</param>
    public void ShowTrackingIndicator(bool isTracking)
    {
        if (trackingIndicator != null)
        {
            trackingIndicator.SetActive(isTracking);
            if (isTracking)
            {
                PlaySound(trackingSound);
            }
        }
    }

    private IEnumerator ShowFeedbackCoroutine(string message, FeedbackType feedbackType, float displayDuration)
    {
        // Préparation
        feedbackText.text = message;
        ApplyFeedbackType(feedbackType);

        // Fade In
        yield return StartCoroutine(UIAnimationUtility.FadeInCanvas(feedbackCanvasGroup, fadeDuration));

        // Attente
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        yield return StartCoroutine(UIAnimationUtility.FadeOutCanvas(feedbackCanvasGroup, fadeDuration));
    }

    private void ApplyFeedbackType(FeedbackType feedbackType)
    {
        switch (feedbackType)
        {
            case FeedbackType.Success:
                PlaySound(successSound);
                Vibrate();
                break;
            case FeedbackType.Error:
                PlaySound(errorSound);
                Vibrate();
                break;
            case FeedbackType.Info:
                // Pas de son ni de vibration pour les messages informatifs
                break;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Méthode qui utilise explicitement les variables pour que le compilateur les détecte
    private void UseVibrationParams()
    {
        // Cette méthode existe uniquement pour éviter les avertissements du compilateur
        float tempDuration = vibrationDuration;
        float tempIntensity = vibrationIntensity;
        
        // Utilisez les variables d'une manière qui ne sera pas supprimée par l'optimisation
        if (tempDuration < 0 || tempIntensity < 0)
        {
            Debug.LogWarning($"Valeurs de vibration invalides : durée={tempDuration}, intensité={tempIntensity}");
        }
    }

    private void Vibrate()
    {
        // Appeler cette méthode pour garantir que les variables sont utilisées
        UseVibrationParams();
        
        #if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                long durationInMilliseconds = (long)(vibrationDuration * 1000);
                
                if (vibrator.Call<bool>("hasAmplitudeControl")) 
                {
                    int amplitude = Mathf.RoundToInt(vibrationIntensity * 255);
                    if (amplitude < 1) amplitude = 1;
                    
                    vibrator.Call("vibrate", durationInMilliseconds, new int[] { 255 });
                }
                else 
                {
                    vibrator.Call("vibrate", durationInMilliseconds);
                }
            }
        }
        catch (System.Exception e) {
            Debug.LogWarning($"Exception lors de la vibration : {e.Message}");
            Handheld.Vibrate();
        }
        #elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
        #else
        // Simuler la vibration dans l'éditeur
        Debug.Log($"[VIBRATION SIMULÉE] Durée: {vibrationDuration}s, Intensité: {vibrationIntensity}");
        #endif
    }
}

/// <summary>
/// Types de feedback disponibles
/// </summary>
public enum FeedbackType
{
    Success,
    Error,
    Info
} 