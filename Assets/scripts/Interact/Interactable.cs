using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    [Tooltip("Nội dung prompt hiện giữa màn hình khi người chơi nhìn vào object")]
    // Text hien o tam man hinh khi raycast trung object nay.
    [SerializeField] private string pickUpMessage;

    [Header("Dialogue")]
    [Tooltip("Tên nhân vật/người nói hiển thị trong hộp thoại")]
    [SerializeField] private string dialogueSpeaker;
    [TextArea(2, 6)]
    [Tooltip("Nội dung hội thoại hiển thị khi tương tác")]
    [SerializeField] private string dialogueText;

    [Header("Interaction Content")]
    [Tooltip("Hình minh họa hiển thị khi tương tác")]
    [SerializeField] private Sprite interactionImage;
    [Tooltip("Bật để hiển thị nội dung hội thoại/hình khi người chơi tương tác")]
    [SerializeField] private bool showContentOnInteract = true;
    [Min(0.25f)]
    [Tooltip("Thời gian giữ nội dung tương tác trên màn hình (giây)")]
    [SerializeField] private float contentDisplaySeconds = 3f;

    public virtual bool CanInteract => true;
    public virtual bool HasPromptText => !string.IsNullOrWhiteSpace(PromptText);
    public virtual string PromptText => pickUpMessage;
    public virtual string DialogueSpeaker => dialogueSpeaker;
    public virtual string DialogueText => dialogueText;
    public virtual Sprite InteractionImage => interactionImage;
    public virtual float ContentDisplaySeconds => Mathf.Max(0.25f, contentDisplaySeconds);

    public bool HasInteractionContent =>
        !string.IsNullOrWhiteSpace(DialogueText) || InteractionImage != null;

    public void BaseInteract(PlayerUI playerUI)
    {
        if (!CanInteract)
        {
            if (playerUI != null)
            {
                playerUI.HideInteractionContent();
            }

            return;
        }

        Interact();
        PresentInteraction(playerUI);
    }

    protected virtual void Interact()
    {
    }

    protected virtual void PresentInteraction(PlayerUI playerUI)
    {
        if (playerUI == null || !showContentOnInteract)
        {
            return;
        }

        if (!HasInteractionContent)
        {
            playerUI.HideInteractionContent();
            return;
        }

        // PlayerUI hien dialogue/image va tu tat sau so giay cua object nay.
        playerUI.ShowInteractionContent(this);
    }
}
