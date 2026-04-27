using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventorySlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int slotIndex;
    private Action<int, PointerEventData.InputButton> clickHandler;

    public void Initialize(int index, Action<int, PointerEventData.InputButton> handler)
    {
        slotIndex = index;
        clickHandler = handler;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        clickHandler?.Invoke(slotIndex, eventData.button);
    }
}
