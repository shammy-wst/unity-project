using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Classe de base pour les managers AR
/// </summary>
public abstract class ARManagerBase : MonoBehaviour, IARFeature
{
    [Header("Settings")]
    [SerializeField] protected float initializationDelay = 0.5f;
    
    protected bool isEnabled = false;
    protected EnhancedFeedbackManager feedbackManager;
    protected bool isInitialized = false;

    public bool IsEnabled => isEnabled;

    /// <summary>
    /// Initialise le manager AR
    /// </summary>
    protected virtual void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Implémentation de IARFeature.Initialize()
    /// </summary>
    public virtual void Initialize()
    {
        if (isInitialized) return;

        feedbackManager = FindFirstObjectByType<EnhancedFeedbackManager>();
        if (feedbackManager == null)
        {
            Debug.LogError($"{GetType().Name} : EnhancedFeedbackManager non trouvé dans la scène!");
            return;
        }

        ValidateComponents();
        isInitialized = true;
    }

    /// <summary>
    /// Vérifie la présence des composants requis
    /// </summary>
    protected virtual void ValidateComponents()
    {
        // À implémenter dans les classes dérivées
    }

    /// <summary>
    /// Implémentation de IARFeature.Enable()
    /// </summary>
    public virtual void Enable()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"{GetType().Name} : Tentative d'activation avant l'initialisation");
            return;
        }

        isEnabled = true;
        gameObject.SetActive(true);
        OnEnabled();
    }

    /// <summary>
    /// Implémentation de IARFeature.Disable()
    /// </summary>
    public virtual void Disable()
    {
        isEnabled = false;
        gameObject.SetActive(false);
        OnDisabled();
    }

    /// <summary>
    /// Méthode appelée après l'activation
    /// </summary>
    protected virtual void OnEnabled()
    {
        // À implémenter dans les classes dérivées
    }

    /// <summary>
    /// Méthode appelée après la désactivation
    /// </summary>
    protected virtual void OnDisabled()
    {
        // À implémenter dans les classes dérivées
    }

    /// <summary>
    /// Affiche un message de feedback
    /// </summary>
    /// <param name="message">Message à afficher</param>
    /// <param name="feedbackType">Type de feedback</param>
    protected void ShowFeedback(string message, FeedbackType feedbackType = FeedbackType.Info)
    {
        if (feedbackManager != null)
        {
            feedbackManager.ShowMessage(message, feedbackType);
        }
    }

    /// <summary>
    /// Affiche l'état du tracking
    /// </summary>
    /// <param name="isTracking">État du tracking</param>
    protected void ShowTrackingState(bool isTracking)
    {
        if (feedbackManager != null)
        {
            feedbackManager.ShowTrackingIndicator(isTracking);
        }
    }
} 