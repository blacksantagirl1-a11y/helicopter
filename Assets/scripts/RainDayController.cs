using UnityEngine;

[DisallowMultipleComponent]
public sealed class RainDayController : MonoBehaviour
{
    [Header("Rain Target")]
    [SerializeField] private GameObject rainRoot;
    [SerializeField] private string rainObjectName = "RainPrefab";

    [Header("Day Rule")]
    [SerializeField] private DialogueDay rainyDay = DialogueDay.Day3;
    [SerializeField] private bool rainFromTargetDayOnward;

    private void OnEnable()
    {
        DialogueSaveService.CurrentDayChanged += HandleCurrentDayChanged;
        ApplyCurrentDay();
    }

    private void OnDisable()
    {
        DialogueSaveService.CurrentDayChanged -= HandleCurrentDayChanged;
    }

    private void OnValidate()
    {
        if (rainRoot == null)
        {
            ResolveRainRoot();
        }
    }

    private void HandleCurrentDayChanged(DialogueDay currentDay)
    {
        ApplyRainState(currentDay);
    }

    private void ApplyCurrentDay()
    {
        ApplyRainState(DialogueSaveService.GetCurrentDay());
    }

    private void ApplyRainState(DialogueDay currentDay)
    {
        ResolveRainRoot();
        if (rainRoot == null)
        {
            return;
        }

        bool shouldRain = rainFromTargetDayOnward
            ? currentDay >= rainyDay
            : currentDay == rainyDay;

        if (rainRoot.activeSelf != shouldRain)
        {
            rainRoot.SetActive(shouldRain);
        }
    }

    private void ResolveRainRoot()
    {
        if (rainRoot != null || string.IsNullOrWhiteSpace(rainObjectName))
        {
            return;
        }

        GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform rainTransform = FindChildRecursive(rootObjects[i].transform, rainObjectName);
            if (rainTransform != null)
            {
                rainRoot = rainTransform.gameObject;
                return;
            }
        }
    }

    private static Transform FindChildRecursive(Transform current, string targetName)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, targetName, System.StringComparison.Ordinal))
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindChildRecursive(current.GetChild(i), targetName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
