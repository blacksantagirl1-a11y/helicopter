using UnityEngine;

public class InventoryPickup : Interactable
{
    [SerializeField] private InventoryItemDefinition itemDefinition;
    [SerializeField]
    [Min(1)]
    private int amount = 1;
    [SerializeField] private string pickupPromptOverride = string.Empty;

    public override bool HasPromptText => true;
    public override string PromptText => BuildPromptText();

    protected InventoryItemDefinition ItemDefinition => itemDefinition;
    protected int Amount
    {
        get => Mathf.Max(1, amount);
        set => amount = Mathf.Max(1, value);
    }

    protected void SetItemDefinition(InventoryItemDefinition definition)
    {
        itemDefinition = definition;
    }

    protected override void Interact()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("InventoryPickup could not find PlayerInventory in the scene.");
            return;
        }

        if (itemDefinition == null)
        {
            Debug.LogWarning($"InventoryPickup on '{name}' has no item definition assigned.");
            return;
        }

        int requestedAmount = Amount;
        if (!inventory.TryAddItem(itemDefinition, requestedAmount, out int remainingAmount))
        {
            return;
        }

        if (remainingAmount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Amount = remainingAmount;
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }

    private string BuildPromptText()
    {
        if (!string.IsNullOrWhiteSpace(pickupPromptOverride))
        {
            return pickupPromptOverride;
        }

        if (itemDefinition == null)
        {
            return "Nhan E de nhat vat pham";
        }

        string amountLabel = Amount > 1 ? $"{Amount}x " : string.Empty;
        return $"Nhan E de nhat {amountLabel}{itemDefinition.DisplayName}";
    }
}
