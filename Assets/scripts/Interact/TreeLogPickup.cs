using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
// TreeLogPickup la pickup dac biet duoc sinh ra sau khi chat cay.
// No ke thua InventoryPickup, chi bo sung viec tu gan item "WoodLog".
public class TreeLogPickup : InventoryPickup
{
    private const string DefaultItemAssetPath = "Assets/Resources/Inventory/WoodLog.asset";
    private const string DefaultItemResourcePath = "Inventory/WoodLog";

    private void Awake()
    {
        TryAssignRuntimeItem();
    }

    private void Reset()
    {
        TryAssignDefaultItem();
    }

    private void OnValidate()
    {
        TryAssignDefaultItem();
    }

    // Trong Editor, co gang gan san asset WoodLog cho prefab / object.
    private void TryAssignDefaultItem()
    {
#if UNITY_EDITOR
        if (ItemDefinition == null)
        {
            SetItemDefinition(AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(DefaultItemAssetPath));
        }
#endif
    }

    // Luc runtime, neu chua co item thi tu load tu Resources.
    private void TryAssignRuntimeItem()
    {
        if (ItemDefinition == null)
        {
            SetItemDefinition(Resources.Load<InventoryItemDefinition>(DefaultItemResourcePath));
        }
    }
}
