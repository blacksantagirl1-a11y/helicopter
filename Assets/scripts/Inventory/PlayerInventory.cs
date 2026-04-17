using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private int amount;

        public InventoryItemDefinition Item => item;
        public int Amount => amount;
        public bool IsEmpty => item == null || amount <= 0;

        public void Set(InventoryItemDefinition nextItem, int nextAmount)
        {
            item = nextItem;
            amount = Mathf.Max(0, nextAmount);

            if (amount == 0)
            {
                item = null;
            }
        }

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

    public event Action InventoryChanged;
    public event Action<string> FeedbackRequested;

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
            InventoryChanged?.Invoke();
            FeedbackRequested?.Invoke($"Da nhat {addedAmount}x {itemDefinition.DisplayName}.");
        }

        if (remainingAmount > 0)
        {
            FeedbackRequested?.Invoke("Tui do da day.");
        }

        return addedAmount > 0;
    }

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
