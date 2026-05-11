using UnityEngine;

[DisallowMultipleComponent]
public sealed class Day5DataCubeInteractable : Interactable
{
    [SerializeField] private string promptText = "Nhan E de mo goi du lieu";

    public override bool CanInteract => DailyQuestManager.CanOpenDay5DataCube();
    public override bool HasPromptText => true;
    public override string PromptText => promptText;

    protected override void Interact()
    {
        DailyQuestManager.TryOpenDay5DataCube();
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        playerUI?.HideInteractionContent();
    }
}
