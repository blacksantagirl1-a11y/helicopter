using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory/Item Definition")]
public class InventoryItemDefinition : ScriptableObject
{
    [Tooltip("ID nội bộ duy nhất của vật phẩm")]
    [SerializeField] private string itemId = "item";
    [Tooltip("Tên hiển thị của vật phẩm")]
    [SerializeField] private string displayName = "Item";
    [TextArea(2, 4)]
    [Tooltip("Mô tả ngắn của vật phẩm")]
    [SerializeField] private string description = string.Empty;
    [Tooltip("Icon hiển thị trong inventory")]
    [SerializeField] private Sprite icon;
    [Tooltip("Số lượng tối đa có thể cộng dồn trong một ô")]
    [SerializeField]
    [Min(1)]
    private int maxStack = 20;
    [Tooltip("Cho phép dùng vật phẩm từ inventory")]
    [SerializeField] private bool canUse;
    [Tooltip("Tiêu hao vật phẩm sau khi dùng")]
    [SerializeField] private bool consumeOnUse;
    [TextArea(1, 3)]
    [Tooltip("Thông báo khi dùng vật phẩm thành công")]
    [SerializeField] private string useMessage = "Da su dung vat pham.";
    [TextArea(1, 3)]
    [Tooltip("Thông báo khi không thể dùng vật phẩm")]
    [SerializeField] private string cannotUseMessage = "Vat pham nay chua the su dung.";

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => Mathf.Max(1, maxStack);
    public bool CanUse => canUse;
    public bool ConsumeOnUse => canUse && consumeOnUse;
    public string UseMessage => string.IsNullOrWhiteSpace(useMessage)
        ? $"Da su dung {DisplayName}."
        : useMessage;
    public string CannotUseMessage => string.IsNullOrWhiteSpace(cannotUseMessage)
        ? $"{DisplayName} chua the su dung."
        : cannotUseMessage;

    public virtual bool TryUse(GameObject user, PlayerInventory inventory, out string feedbackMessage, out bool consumeItem)
    {
        if (!CanUse)
        {
            feedbackMessage = CannotUseMessage;
            consumeItem = false;
            return false;
        }

        feedbackMessage = UseMessage;
        consumeItem = ConsumeOnUse;
        return true;
    }

    private void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = name.Replace(' ', '_').ToLowerInvariant();
        }

        if (!canUse)
        {
            consumeOnUse = false;
        }
    }
}
