using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Prompt UI")]
    [SerializeField] public TextMeshProUGUI PickUpText;

    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Image interactionPanelBackground;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private GameObject interactionImagePanel;
    [SerializeField] private Image interactionImage;

    private Interactable currentInteractionSource;
    private Coroutine autoHideRoutine;

    public bool IsShowingAnyContent =>
        (interactionPanel != null && interactionPanel.activeSelf) ||
        (interactionImagePanel != null && interactionImagePanel.activeSelf);

    private void Awake()
    {
        // Neu scene chua co panel noi dung thi script tu tao runtime de dung ngay.
        EnsureInteractionUI();
        UpdatePrompt(string.Empty);
        HideInteractionContent();
    }

    public void UpdatePrompt(string promptMessage)
    {
        if (PickUpText == null)
        {
            return;
        }

        PickUpText.text = promptMessage;
        PickUpText.gameObject.SetActive(!string.IsNullOrWhiteSpace(promptMessage));
    }

    public void ShowInteractionContent(Interactable interactable)
    {
        if (DialogueController.IsDialogueActive)
        {
            HideInteractionContent();
            return;
        }

        if (interactable == null)
        {
            HideInteractionContent();
            return;
        }

        StopAutoHideTimer();
        EnsureInteractionUI();
        if (interactionPanel == null && interactionImagePanel == null)
        {
            return;
        }

        string speakerName = interactable.DialogueSpeaker;
        string bodyText = interactable.DialogueText;
        Sprite sprite = interactable.InteractionImage;
        bool hasText = !string.IsNullOrWhiteSpace(bodyText);
        bool hasImage = sprite != null;

        if (!hasText && !hasImage)
        {
            HideInteractionContent();
            return;
        }

        currentInteractionSource = interactable;

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(hasText);
        }

        if (interactionText != null)
        {
            interactionText.text = BuildDialogueMarkup(speakerName, bodyText);
            interactionText.gameObject.SetActive(hasText);
        }

        if (interactionImagePanel != null)
        {
            interactionImagePanel.SetActive(hasImage);
        }

        if (interactionImage != null)
        {
            interactionImage.sprite = sprite;
            interactionImage.enabled = hasImage;
            interactionImage.gameObject.SetActive(hasImage);
        }

        if (hasText || hasImage)
        {
            autoHideRoutine = StartCoroutine(AutoHideAfterDelay(interactable.ContentDisplaySeconds));
        }
    }

    public void HideInteractionContent()
    {
        StopAutoHideTimer();
        currentInteractionSource = null;

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }

        if (interactionText != null)
        {
            interactionText.text = string.Empty;
        }

        if (interactionImage != null)
        {
            interactionImage.sprite = null;
            interactionImage.enabled = false;
        }

        if (interactionImagePanel != null)
        {
            interactionImagePanel.SetActive(false);
        }
    }

    public bool IsShowingContent(Interactable interactable)
    {
        return IsShowingAnyContent && currentInteractionSource == interactable;
    }

    private void EnsureInteractionUI()
    {
        if (interactionPanel != null &&
            interactionPanelBackground != null &&
            interactionText != null &&
            interactionImagePanel != null &&
            interactionImage != null)
        {
            return;
        }

        Canvas canvas = PickUpText != null ? PickUpText.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (interactionPanel == null)
        {
            Transform existingPanel = canvas.transform.Find("InteractionPanel");
            if (existingPanel != null)
            {
                interactionPanel = existingPanel.gameObject;
            }
        }

        if (interactionPanel != null)
        {
            interactionPanelBackground ??= interactionPanel.GetComponent<Image>();

            if (interactionText == null)
            {
                Transform textTransform = interactionPanel.transform.Find("InteractionText");
                if (textTransform != null)
                {
                    interactionText = textTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (interactionImagePanel == null)
        {
            Transform imagePanelTransform = canvas.transform.Find("InteractionImagePanel");
            if (imagePanelTransform != null)
            {
                interactionImagePanel = imagePanelTransform.gameObject;
            }
        }

        if (interactionImagePanel != null && interactionImage == null)
        {
            Transform imageTransform = interactionImagePanel.transform.Find("InteractionImage");
            if (imageTransform != null)
            {
                interactionImage = imageTransform.GetComponent<Image>();
            }
        }

        CreateMissingInteractionUI(canvas);
        ConfigureDialoguePanel();
        ConfigureDialogueText();
        ConfigureImagePanel();
        ConfigureInteractionImage();
    }

    private void CreateMissingInteractionUI(Canvas canvas)
    {
        if (interactionPanel == null)
        {
            interactionPanel = new GameObject("InteractionPanel", typeof(RectTransform), typeof(Image));
            interactionPanel.transform.SetParent(canvas.transform, false);
        }

        if (interactionPanelBackground == null)
        {
            interactionPanelBackground = interactionPanel.GetComponent<Image>();
            if (interactionPanelBackground == null)
            {
                interactionPanelBackground = interactionPanel.AddComponent<Image>();
            }
        }

        if (interactionText == null)
        {
            interactionText = CreateTextElement("InteractionText", interactionPanel.transform);
        }

        if (interactionImagePanel == null)
        {
            interactionImagePanel = new GameObject("InteractionImagePanel", typeof(RectTransform), typeof(Image));
            interactionImagePanel.transform.SetParent(canvas.transform, false);
        }

        if (interactionImage == null)
        {
            interactionImage = CreateImageElement("InteractionImage", interactionImagePanel.transform);
        }
    }

    private void ConfigureDialoguePanel()
    {
        if (interactionPanel == null || interactionPanelBackground == null)
        {
            return;
        }

        RectTransform panelRect = interactionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 28f);
        panelRect.sizeDelta = new Vector2(860f, 96f);

        interactionPanelBackground.color = new Color(0f, 0f, 0f, 0.30f);
        interactionPanelBackground.raycastTarget = false;
    }

    private void ConfigureDialogueText()
    {
        if (interactionText == null)
        {
            return;
        }

        RectTransform textRect = interactionText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(24f, 12f);
        textRect.offsetMax = new Vector2(-24f, -12f);

        interactionText.text = string.Empty;
        interactionText.color = Color.white;
        interactionText.alignment = TextAlignmentOptions.Center;
        interactionText.enableWordWrapping = true;
        interactionText.fontSize = 34f;
        interactionText.lineSpacing = -6f;
        interactionText.richText = true;
        interactionText.raycastTarget = false;
    }

    private void ConfigureImagePanel()
    {
        if (interactionImagePanel == null)
        {
            return;
        }

        RectTransform panelRect = interactionImagePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 10f);
        panelRect.sizeDelta = new Vector2(620f, 360f);

        Image background = interactionImagePanel.GetComponent<Image>();
        if (background == null)
        {
            background = interactionImagePanel.AddComponent<Image>();
        }
        background.color = new Color(0f, 0f, 0f, 0.6f);
        background.raycastTarget = false;
    }

    private void ConfigureInteractionImage()
    {
        if (interactionImage == null)
        {
            return;
        }

        RectTransform imageRect = interactionImage.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(560f, 300f);

        interactionImage.color = Color.white;
        interactionImage.preserveAspect = true;
        interactionImage.raycastTarget = false;
    }

    private string BuildDialogueMarkup(string speakerName, string bodyText)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
        {
            return bodyText;
        }

        return $"<color=#D79E37>{speakerName}:</color> {bodyText}";
    }

    private void StopAutoHideTimer()
    {
        if (autoHideRoutine == null)
        {
            return;
        }

        StopCoroutine(autoHideRoutine);
        autoHideRoutine = null;
    }

    private IEnumerator AutoHideAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        autoHideRoutine = null;
        HideInteractionContent();
    }

    private static TextMeshProUGUI CreateTextElement(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.color = Color.white;

        return text;
    }

    private static Image CreateImageElement(string objectName, Transform parent)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;

        return image;
    }
}
