using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    // Text hien o tam man hinh khi raycast trung object nay.
    [SerializeField] private string pickUpMessage;

    [Header("Interaction Content")]
    // Noi dung se mo ra sau khi bam E.
    [TextArea(2, 6)]
    [SerializeField] private string dialogueText;
    [SerializeField] private Sprite interactionImage;
    [SerializeField] private bool showContentOnInteract = true;

    public bool HasPromptText => !string.IsNullOrWhiteSpace(pickUpMessage);
    public virtual string PromptText => pickUpMessage;
    public virtual string DialogueText => dialogueText;
    public virtual Sprite InteractionImage => interactionImage;

    public bool HasInteractionContent =>
        !string.IsNullOrWhiteSpace(DialogueText) || InteractionImage != null;

    public void BaseInteract(PlayerUI playerUI)
    {
        // Neu dang mo dung panel cua object nay thi bam E lan nua se dong panel.
        if (playerUI != null && playerUI.IsShowingContent(this))
        {
            playerUI.HideInteractionContent();
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

        // PlayerUI tu quyet dinh hien text, image, hoac ca hai.
        playerUI.ToggleInteractionContent(this);
    }
}
