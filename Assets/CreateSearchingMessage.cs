using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System;

/// <summary>
/// Crée un message de recherche d'image pour le mode de tracking d'images
/// </summary>
public class CreateSearchingMessage : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private string message = "Recherche de la photo...";
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Color textColor = Color.white;

    void Start()
    {
        var existingMessages = UnityEngine.Object.FindObjectsByType<CreateSearchingMessage>(UnityEngine.FindObjectsSortMode.None);
        if (existingMessages.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Trouver automatiquement le canvas si non spécifié
        if (targetCanvas == null)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(UnityEngine.FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name.Contains("Image") || canvas.name.Contains("image"))
                {
                    targetCanvas = canvas;
                    Debug.Log($"Canvas trouvé automatiquement: {targetCanvas.name}");
                    break;
                }
            }
        }

        if (targetCanvas == null)
        {
            Debug.LogError("Aucun canvas trouvé pour le message de recherche!");
            return;
        }

        // Créer le message
        CreateMessage();
    }

    private void CreateMessage()
    {
        // Créer un GameObject pour le message
        GameObject messageObject = new GameObject("SearchingImageMessage");
        messageObject.transform.SetParent(targetCanvas.transform, false);

        // Ajouter un panneau de fond
        Image backgroundPanel = messageObject.AddComponent<Image>();
        backgroundPanel.color = backgroundColor;
        RectTransform rectTransform = messageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.1f, 0.1f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.2f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Créer le texte
        GameObject textObject = new GameObject("MessageText");
        textObject.transform.SetParent(messageObject.transform, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = message;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 36;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 36;

        // Positionner le texte
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0, 0);
        textRectTransform.anchorMax = new Vector2(1, 1);
        textRectTransform.offsetMin = new Vector2(20, 10);
        textRectTransform.offsetMax = new Vector2(-20, -10);

        Debug.Log("Message de recherche d'image créé avec succès");
    }
} 