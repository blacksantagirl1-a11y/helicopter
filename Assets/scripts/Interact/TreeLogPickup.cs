using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
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

    private void TryAssignDefaultItem()
    {
#if UNITY_EDITOR
        if (ItemDefinition == null)
        {
            SetItemDefinition(AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(DefaultItemAssetPath));
        }
#endif
    }

    private void TryAssignRuntimeItem()
    {
        if (ItemDefinition == null)
        {
            SetItemDefinition(Resources.Load<InventoryItemDefinition>(DefaultItemResourcePath));
        }
    }
}
