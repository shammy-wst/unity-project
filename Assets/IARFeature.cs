using UnityEngine;

/// <summary>
/// Interface définissant les fonctionnalités communes pour les fonctionnalités AR
/// </summary>
public interface IARFeature
{
    /// <summary>
    /// Active la fonctionnalité AR
    /// </summary>
    void Enable();

    /// <summary>
    /// Désactive la fonctionnalité AR
    /// </summary>
    void Disable();

    /// <summary>
    /// Vérifie si la fonctionnalité est activée
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Initialise la fonctionnalité
    /// </summary>
    void Initialize();
} 