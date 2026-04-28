using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// PlayerInventory la "bo nho tui do" cua nguoi choi.
// No chi lo du lieu:
// - co bao nhieu slot
// - moi slot chua item gi
// - them item vao tui
// - dung item trong tui
// - bao cho UI biet khi inventory thay doi
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    // InventorySlot = 1 o trong tui do.
    // Moi o chi chua 1 loai item va so luong cua loai do.
    public sealed class InventorySlot
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private int amount;

        public InventoryItemDefinition Item => item;
        public int Amount => amount;
        public bool IsEmpty => item == null || amount <= 0;

        // Dat lai item va so luong cho slot nay.
        public void Set(InventoryItemDefinition nextItem, int nextAmount)
        {
            item = nextItem;
            amount = Mathf.Max(0, nextAmount);

            if (amount == 0)
            {
                item = null;
            }
        }

        // Bien slot nay thanh slot rong.
        public void Clear()
        {
            item = null;
            amount = 0;
        }
    }

    [Header("Capacity")]
    [Tooltip("Số lượng ô chứa trong túi đồ")]
    [SerializeField]
    [Min(1)]
    private int slotCount = 20;
    [Tooltip("Danh sách slot inventory được serialize")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // UI dang nghe event nay de refresh lai slot.
    public event Action InventoryChanged;
    // UI dang nghe event nay de hien thong bao ngan cho nguoi choi.
    public event Action<string> FeedbackRequested;
    public event Action<InventoryItemDefinition, int> ItemAdded;

    public int SlotCount => slots.Count;
    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Reset()
    {
        EnsureSlotCount();
    }

    private void Awake()
    {
        EnsureSlotCount();
    }

    private void OnValidate()
    {
        EnsureSlotCount();
    }

    // Co gang them vat pham vao inventory.
    // Thu tu uu tien:
    // 1. Cong vao cac stack cung item chua day.
    // 2. Neu van con du, tim slot trong de tao stack moi.
    // remainingAmount cho biet con bao nhieu item khong dua vao tui duoc.
    public bool TryAddItem(InventoryItemDefinition itemDefinition, int amount, out int remainingAmount)
    {
        EnsureSlotCount();
        remainingAmount = Mathf.Max(0, amount);

        if (itemDefinition == null || remainingAmount == 0)
        {
            return false;
        }

        for (int i = 0; i < slots.Count && remainingAmount > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.Item != itemDefinition)
            {
                continue;
            }

            int stackSpace = itemDefinition.MaxStack - slot.Amount;
            if (stackSpace <= 0)
            {
                continue;
            }

            int toAdd = Mathf.Min(stackSpace, remainingAmount);
            slot.Set(itemDefinition, slot.Amount + toAdd);
            remainingAmount -= toAdd;
        }

        for (int i = 0; i < slots.Count && remainingAmount > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty)
            {
                continue;
            }

            int toAdd = Mathf.Min(itemDefinition.MaxStack, remainingAmount);
            slot.Set(itemDefinition, toAdd);
            remainingAmount -= toAdd;
        }

        int addedAmount = amount - remainingAmount;
        if (addedAmount > 0)
        {
            ItemAdded?.Invoke(itemDefinition, addedAmount);
            InventoryChanged?.Invoke();
            FeedbackRequested?.Invoke($"Da nhat {addedAmount}x {itemDefinition.DisplayName}.");
        }

        if (remainingAmount > 0)
        {
            FeedbackRequested?.Invoke("Tui do da day.");
        }

        return addedAmount > 0;
    }

    public int GetItemCount(InventoryItemDefinition itemDefinition)
    {
        EnsureSlotCount();
        if (itemDefinition == null)
        {
            return 0;
        }

        int totalAmount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty && slot.Item == itemDefinition)
            {
                totalAmount += slot.Amount;
            }
        }

        return totalAmount;
    }

    public bool TryRemoveItem(InventoryItemDefinition itemDefinition, int amount)
    {
        EnsureSlotCount();
        amount = Mathf.Max(0, amount);
        if (itemDefinition == null || amount == 0 || GetItemCount(itemDefinition) < amount)
        {
            return false;
        }

        int remainingAmount = amount;
        for (int i = 0; i < slots.Count && remainingAmount > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (slot.IsEmpty || slot.Item != itemDefinition)
            {
                continue;
            }

            int amountToRemove = Mathf.Min(slot.Amount, remainingAmount);
            remainingAmount -= amountToRemove;
            slot.Set(slot.Item, slot.Amount - amountToRemove);
        }

        InventoryChanged?.Invoke();
        return true;
    }

    // Dung vat pham trong mot slot cu the.
    // Hanh vi "dung" that su duoc quyet dinh boi itemDefinition.TryUse(...).
    public bool TryUseSlot(int slotIndex)
    {
        EnsureSlotCount();
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return false;
        }

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty || slot.Item == null)
        {
            return false;
        }

        InventoryItemDefinition itemDefinition = slot.Item;
        if (!itemDefinition.TryUse(gameObject, this, out string feedbackMessage, out bool consumeItem))
        {
            if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                FeedbackRequested?.Invoke(feedbackMessage);
            }

            return false;
        }

        if (!string.IsNullOrWhiteSpace(feedbackMessage))
        {
            FeedbackRequested?.Invoke(feedbackMessage);
        }

        if (consumeItem)
        {
            RemoveFromSlot(slotIndex, 1);
        }

        return true;
    }

    // Dem so slot dang co vat pham.
    public int GetUsedSlotCount()
    {
        EnsureSlotCount();

        int usedCount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty)
            {
                usedCount++;
            }
        }

        return usedCount;
    }

    // Giam so luong item trong slot sau khi da dung / tieu hao.
    private void RemoveFromSlot(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count || amount <= 0)
        {
            return;
        }

        InventorySlot slot = slots[slotIndex];
        if (slot.IsEmpty)
        {
            return;
        }

        int nextAmount = slot.Amount - amount;
        if (nextAmount <= 0)
        {
            slot.Clear();
        }
        else
        {
            slot.Set(slot.Item, nextAmount);
        }

        InventoryChanged?.Invoke();
    }

    // Dam bao danh sach slots luon co dung kich thuoc nhu slotCount.
    private void EnsureSlotCount()
    {
        slotCount = Mathf.Max(1, slotCount);

        while (slots.Count < slotCount)
        {
            slots.Add(new InventorySlot());
        }

        while (slots.Count > slotCount)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }
}
