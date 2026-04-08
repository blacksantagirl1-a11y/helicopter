using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Prompt UI")]
    [SerializeField] public TextMeshProUGUI PickUpText;

    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Image interactionImage;
    [SerializeField] private TextMeshProUGUI interactionHintText;

    private Interactable currentInteractionSource;

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

    public void ToggleInteractionContent(Interactable interactable)
    {
        if (interactable == null)
        {
            HideInteractionContent();
            return;
        }

        if (interactionPanel != null &&
            interactionPanel.activeSelf &&
            currentInteractionSource == interactable)
        {
            HideInteractionContent();
            return;
        }

        ShowInteractionContent(interactable);
    }

    public void ShowInteractionContent(Interactable interactable)
    {
        if (interactable == null)
        {
            HideInteractionContent();
            return;
        }

        EnsureInteractionUI();
        if (interactionPanel == null)
        {
            return;
        }

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
        interactionPanel.SetActive(true);

        if (interactionText != null)
        {
            interactionText.text = bodyText;
            interactionText.gameObject.SetActive(hasText);
        }

        if (interactionImage != null)
        {
            interactionImage.sprite = sprite;
            interactionImage.enabled = hasImage;
            interactionImage.gameObject.SetActive(hasImage);
        }

        if (interactionHintText != null)
        {
            interactionHintText.gameObject.SetActive(true);
        }
    }

    public void HideInteractionContent()
    {
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
    }

    public bool IsShowingContent(Interactable interactable)
    {
        return interactionPanel != null &&
               interactionPanel.activeSelf &&
               currentInteractionSource == interactable;
    }

    private void EnsureInteractionUI()
    {
        if (interactionPanel != null && interactionText != null && interactionImage != null)
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
            if (interactionText == null)
            {
                Transform textTransform = interactionPanel.transform.Find("InteractionText");
                if (textTransform != null)
                {
                    interactionText = textTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (interactionImage == null)
            {
                Transform imageTransform = interactionPanel.transform.Find("InteractionImage");
                if (imageTransform != null)
                {
                    interactionImage = imageTransform.GetComponent<Image>();
                }
            }

            if (interactionHintText == null)
            {
                Transform hintTransform = interactionPanel.transform.Find("InteractionHint");
                if (hintTransform != null)
                {
                    interactionHintText = hintTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (interactionPanel == null)
        {
            CreateInteractionUI(canvas);
        }
    }

    private void CreateInteractionUI(Canvas canvas)
    {
        // Panel nay la phan UI lon de hien hoi thoai/hinh anh sau khi interact.
        interactionPanel = new GameObject("InteractionPanel", typeof(RectTransform), typeof(Image));
        interactionPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = interactionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 8f);
        panelRect.sizeDelta = new Vector2(560f, 320f);

        Image background = interactionPanel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);
        background.raycastTarget = false;

        interactionText = CreateTextElement(
            "InteractionText",
            panelRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(480f, 120f));
        interactionText.alignment = TextAlignmentOptions.TopLeft;
        interactionText.enableWordWrapping = true;
        interactionText.fontSize = 28f;

        interactionImage = CreateImageElement(
            "InteractionImage",
            panelRect,
            new Vector2(0.5f, 0.34f),
            new Vector2(0.5f, 0.34f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(420f, 130f));
        interactionImage.preserveAspect = true;
        interactionImage.raycastTarget = false;

        interactionHintText = CreateTextElement(
            "InteractionHint",
            panelRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 20f),
            new Vector2(300f, 28f));
        interactionHintText.text = "Press E again or Esc to close";
        interactionHintText.alignment = TextAlignmentOptions.Center;
        interactionHintText.fontSize = 18f;
        interactionHintText.color = new Color(1f, 1f, 1f, 0.75f);

        interactionPanel.SetActive(false);
    }

    private static TextMeshProUGUI CreateTextElement(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.color = Color.white;
        text.enableWordWrapping = true;

        return text;
    }

    private static Image CreateImageElement(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;

        return image;
    }
}
