using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    // Text hien o tam man hinh khi raycast trung object nay.
    [SerializeField] private string pickUpMessage;

    [Header("Dialogue")]
    [SerializeField] private string dialogueSpeaker;
    [TextArea(2, 6)]
    [SerializeField] private string dialogueText;

    [Header("Interaction Content")]
    [SerializeField] private Sprite interactionImage;
    [SerializeField] private bool showContentOnInteract = true;
    [Min(0.25f)]
    [SerializeField] private float contentDisplaySeconds = 3f;

    public bool HasPromptText => !string.IsNullOrWhiteSpace(pickUpMessage);
    public virtual string PromptText => pickUpMessage;
    public virtual string DialogueSpeaker => dialogueSpeaker;
    public virtual string DialogueText => dialogueText;
    public virtual Sprite InteractionImage => interactionImage;
    public virtual float ContentDisplaySeconds => Mathf.Max(0.25f, contentDisplaySeconds);

    public bool HasInteractionContent =>
        !string.IsNullOrWhiteSpace(DialogueText) || InteractionImage != null;

    public void BaseInteract(PlayerUI playerUI)
    {
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
