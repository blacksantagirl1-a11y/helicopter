using UnityEngine;

[DisallowMultipleComponent]
public class FishingRodReferences : MonoBehaviour
{
    [SerializeField] private Transform robPoint;

    public Transform RobPoint => robPoint;
}
