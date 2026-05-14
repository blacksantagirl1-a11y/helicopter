using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuButtonHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button targetButton;
    private Action<Button, bool> callback;

    public void Initialize(Button button, Action<Button, bool> onHoverChanged)
    {
        targetButton = button;
        callback = onHoverChanged;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        callback?.Invoke(targetButton, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        callback?.Invoke(targetButton, false);
    }
}
