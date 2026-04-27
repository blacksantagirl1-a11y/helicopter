using UnityEngine;

[DisallowMultipleComponent]
public sealed class CampingCookingInteractable : Interactable
{
    [Header("Cooking Interaction")]
    [SerializeField] private string cookingPrompt = "Nhan E de nau an";

    public override bool HasPromptText => true;
    public override string PromptText => cookingPrompt;

    protected override void Interact()
    {
        CampingCookingModeController controller = FindFirstObjectByType<CampingCookingModeController>();
        if (controller == null)
        {
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                controller = inventory.GetComponent<CampingCookingModeController>();
                if (controller == null)
                {
                    controller = inventory.gameObject.AddComponent<CampingCookingModeController>();
                }
            }
        }

        if (controller != null)
        {
            controller.EnterCookingMode(transform);
        }
    }
}
