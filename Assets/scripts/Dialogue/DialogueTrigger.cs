using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTrigger : Interactable
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    //public override bool CanLookInteract => false;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanTriggerFrom(other))
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }

        //if (!TryRequestDialogueEvent())
        {
            return;
        }

        if (triggerOnce)
        {
            hasTriggered = true;
        }
    }

    private bool CanTriggerFrom(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        return other.GetComponentInParent<PlayerMovement>() != null ||
               other.GetComponentInParent<PickUpScript>() != null ||
               other.CompareTag("Player");
    }

    private void EnsureTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
