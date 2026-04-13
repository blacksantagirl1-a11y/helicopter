using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ActionScript : MonoBehaviour
{
    private const int HandStateHolding = -1;
    private const int HandStateUnequipped = 0;
    private const int HandStateEquipped = 1;
    private const int HandStateAttack = 2;
    private const int HandStateFishing = 3;

    [Header("Hand Layer")]
    [SerializeField] private string handLayerName = "HandAnim";
    [SerializeField] private float handLayerWeight = 1f;
    [SerializeField] private string handStateParameterName = "HandState";
    [SerializeField] private string equipWeaponStateName = "EquipWeapon";
    [SerializeField] private string unequipWeaponStateName = "UnequipWeapon";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string holdWeaponStateName = "HoldItem";

    [Header("Input")]
    [SerializeField] private KeyCode toggleWeaponKey = KeyCode.F;

    [Header("Weapon Prefab")]
    [SerializeField] private GameObject axePrefab;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private bool hideWeaponWhenUnequipped = true;

    private Animator animator;
    private PlayerMovement playerMovement;
    private Coroutine handActionRoutine;
    private GameObject spawnedAxe;
    private int handLayerIndex = -1;
    private int handStateParameterHash;
    private int currentHandState = HandStateUnequipped;
    private string equipWeaponStatePath = string.Empty;
    private string unequipWeaponStatePath = string.Empty;
    private string attackStatePath = string.Empty;
    private string holdWeaponStatePath = string.Empty;
    private bool isWeaponEquipped;
    private bool isHandActionLocked;

    public bool IsWeaponEquipped => isWeaponEquipped;
    public event System.Action AttackPerformed;

    private void Reset()
    {
        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void Awake()
    {
        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void Start()
    {
        RefreshAnimatorState();
        SetWeaponVisible(false);
    }

    private void Update()
    {
        if (animator == null || handLayerIndex < 0 || isHandActionLocked)
        {
            return;
        }

        if (playerMovement != null && playerMovement.IsCutscenePlaying)
        {
            return;
        }

        if (Input.GetKeyDown(toggleWeaponKey))
        {
            handActionRoutine = StartCoroutine(isWeaponEquipped
                ? PlayUnequipSequence()
                : PlayEquipSequence());
            return;
        }

        if (Input.GetMouseButtonDown(0) && isWeaponEquipped)
        {
            handActionRoutine = StartCoroutine(PlayAttackSequence());
        }
    }

    public void RefreshAnimatorState()
    {
        if (isWeaponEquipped)
        {
            SetWeaponVisible(true);
            EnterHoldState();
            return;
        }

        ApplyHandState(HandStateUnequipped, true);
        if (!isWeaponEquipped && hideWeaponWhenUnequipped)
        {
            SetWeaponVisible(false);
        }
    }

    public void PlayFishingState()
    {
        ApplyHandState(HandStateFishing, true);
    }

    private IEnumerator PlayEquipSequence()
    {
        isHandActionLocked = true;
        isWeaponEquipped = true;
        SetWeaponVisible(true);
        ApplyHandState(HandStateEquipped, false);

        yield return WaitForHandAnimation(equipWeaponStatePath, equipWeaponStateName);

        if (isWeaponEquipped)
        {
            EnterHoldState();
        }

        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator PlayUnequipSequence()
    {
        isHandActionLocked = true;
        isWeaponEquipped = false;
        ApplyHandState(HandStateUnequipped, false);

        yield return WaitForHandAnimation(unequipWeaponStatePath, unequipWeaponStateName);

        if (hideWeaponWhenUnequipped)
        {
            SetWeaponVisible(false);
        }

        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator PlayAttackSequence()
    {
        isHandActionLocked = true;
        ApplyHandState(HandStateAttack, true);
        AttackPerformed?.Invoke();

        yield return WaitForHandAnimation(attackStatePath, attackStateName);

        if (isWeaponEquipped)
        {
            EnterHoldState();
        }

        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator WaitForHandAnimation(string statePath, string clipName)
    {
        if (animator == null || handLayerIndex < 0 || string.IsNullOrWhiteSpace(statePath))
        {
            yield break;
        }

        yield return null;

        float waitTimeout = GetClipLength(clipName) + 0.25f;
        if (waitTimeout <= 0.25f)
        {
            waitTimeout = 1f;
        }

        float elapsed = 0f;
        while (elapsed < waitTimeout)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(handLayerIndex);
            if (!animator.IsInTransition(handLayerIndex) &&
                stateInfo.IsName(statePath) &&
                stateInfo.normalizedTime >= 1f)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void ApplyHandState(int handState, bool force)
    {
        if (animator == null || handLayerIndex < 0 || handStateParameterHash == 0)
        {
            return;
        }

        if (!force && currentHandState == handState)
        {
            return;
        }

        animator.SetLayerWeight(handLayerIndex, handLayerWeight);
        animator.SetInteger(handStateParameterHash, handState);
        currentHandState = handState;
    }

    private void EnterHoldState()
    {
        if (animator == null || handLayerIndex < 0)
        {
            return;
        }

        animator.SetLayerWeight(handLayerIndex, handLayerWeight);

        if (handStateParameterHash != 0)
        {
            animator.SetInteger(handStateParameterHash, HandStateHolding);
        }

        if (!string.IsNullOrWhiteSpace(holdWeaponStatePath))
        {
            animator.CrossFadeInFixedTime(holdWeaponStatePath, 0.08f, handLayerIndex);
        }

        currentHandState = HandStateHolding;
    }

    private void SetWeaponVisible(bool isVisible)
    {
        if (axePrefab == null)
        {
            return;
        }

        if (isVisible)
        {
            if (spawnedAxe == null)
            {
                Transform parent = weaponSocket != null ? weaponSocket : transform;
                spawnedAxe = Instantiate(axePrefab, parent);
                spawnedAxe.transform.localPosition = Vector3.zero;
                spawnedAxe.transform.localRotation = Quaternion.identity;
            }
            else
            {
                spawnedAxe.SetActive(true);
            }

            return;
        }

        if (spawnedAxe != null)
        {
            spawnedAxe.SetActive(false);
        }
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(clipName))
        {
            return 0f;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == clipName)
            {
                return clip.length;
            }
        }

        return 0f;
    }

    private void TryAutoAssignReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void CacheAnimatorData()
    {
        if (animator == null || string.IsNullOrWhiteSpace(handLayerName))
        {
            handLayerIndex = -1;
            handStateParameterHash = 0;
            return;
        }

        handLayerIndex = animator.GetLayerIndex(handLayerName);
        if (handLayerIndex < 0)
        {
            handStateParameterHash = 0;
            return;
        }

        handStateParameterHash = string.IsNullOrWhiteSpace(handStateParameterName)
            ? 0
            : Animator.StringToHash(handStateParameterName);

        equipWeaponStatePath = GetStatePath(equipWeaponStateName);
        unequipWeaponStatePath = GetStatePath(unequipWeaponStateName);
        attackStatePath = GetStatePath(attackStateName);
        holdWeaponStatePath = GetStatePath(holdWeaponStateName);
    }

    private string GetStatePath(string stateName)
    {
        if (handLayerIndex < 0 || string.IsNullOrWhiteSpace(stateName))
        {
            return string.Empty;
        }

        return handLayerName + "." + stateName;
    }
}
