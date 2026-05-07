using UnityEngine;

// InventoryPickup la vat the nhat duoc trong scene.
// Khi tuong tac, no yeu cau PlayerInventory them item vao tui.
public class InventoryPickup : Interactable
{
    [Tooltip("Định nghĩa vật phẩm sẽ được nhặt")]
    [SerializeField] private InventoryItemDefinition itemDefinition;
    [Tooltip("Số lượng vật phẩm trong pickup này")]
    [SerializeField]
    [Min(1)]
    private int amount = 1;
    [Tooltip("Prompt tùy chỉnh khi nhìn vào vật phẩm (để trống sẽ tự tạo)")]
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

    // Logic nhat do:
    // - tim PlayerInventory
    // - thu them item vao tui
    // - neu nhat het thi huy object pickup
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

        PlayPickUpSound();

        if (remainingAmount <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Amount = remainingAmount;
    }

    // Pickup nay chi can prompt, khong can mo panel noi dung rieng.
    protected override void PresentInteraction(PlayerUI playerUI)
    {
    }

    private static void PlayPickUpSound()
    {
        ReSoundManager.Resolve()?.PlaySound2D(SoundIds.PickUp);
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }

    // Tao dong prompt hien len man hinh khi nguoi choi nhin vao pickup.
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
