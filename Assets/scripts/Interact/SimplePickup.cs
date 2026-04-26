using UnityEngine;

public class SimplePickup : Interactable
{
    [SerializeField] private string pickupPromptOverride = "Phá Hủy";

    public override string PromptText => pickupPromptOverride;

    protected override void Interact()
    {
        Destroy(gameObject);
    }

    protected override void PresentInteraction(PlayerUI playerUI)
    {
        // Không hiển thị nội dung tương tác (không có hội thoại hoặc hình ảnh)
    }
}