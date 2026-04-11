using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ActionScript : MonoBehaviour
{
    [Header("Hand Layer")]
    [SerializeField] private string handLayerName = "HandAnim";
    [SerializeField] private float handLayerWeight = 1f;
    [SerializeField] private string equipWeaponStateName = "EquipWeapon";
    [SerializeField] private string unequipWeaponStateName = "UnequipWeapon";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private float handTransitionDuration = 0.1f;

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
    private int equipWeaponHash;
    private int unequipWeaponHash;
    private int attackHash;

    private bool isWeaponEquipped;
    private bool isHandActionLocked;

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
        if (animator == null || handLayerIndex < 0)
        {
            return;
        }

        animator.SetLayerWeight(handLayerIndex, handLayerWeight);
        SnapToHandState(unequipWeaponHash);
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

    private IEnumerator PlayEquipSequence()
    {
        isHandActionLocked = true;
        isWeaponEquipped = true;

        SetWeaponVisible(true);
        PlayHandState(equipWeaponHash, handTransitionDuration);

        yield return WaitForHandAnimation(equipWeaponHash, equipWeaponStateName);

        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator PlayUnequipSequence()
    {
        isHandActionLocked = true;
        isWeaponEquipped = false;

        PlayHandState(unequipWeaponHash, handTransitionDuration);

        yield return WaitForHandAnimation(unequipWeaponHash, unequipWeaponStateName);

        if (hideWeaponWhenUnequipped)
        {
            SetWeaponVisible(false);
        }

        SnapToHandState(unequipWeaponHash);
        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator PlayAttackSequence()
    {
        isHandActionLocked = true;

        PlayHandState(attackHash, handTransitionDuration);

        yield return WaitForHandAnimation(attackHash, attackStateName);

        if (isWeaponEquipped)
        {
            SnapToHandState(equipWeaponHash);
        }

        isHandActionLocked = false;
        handActionRoutine = null;
    }

    private IEnumerator WaitForHandAnimation(int stateHash, string clipName)
    {
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
                stateInfo.shortNameHash == stateHash &&
                stateInfo.normalizedTime >= 1f)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PlayHandState(int stateHash, float transitionDuration)
    {
        if (animator == null || handLayerIndex < 0 || stateHash == 0)
        {
            return;
        }

        animator.SetLayerWeight(handLayerIndex, handLayerWeight);
        animator.CrossFadeInFixedTime(stateHash, transitionDuration, handLayerIndex);
    }

    private void SnapToHandState(int stateHash)
    {
        if (animator == null || handLayerIndex < 0 || stateHash == 0)
        {
            return;
        }

        animator.SetLayerWeight(handLayerIndex, handLayerWeight);
        animator.Play(stateHash, handLayerIndex, 1f);
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
        equipWeaponHash = GetStateHash(equipWeaponStateName);
        unequipWeaponHash = GetStateHash(unequipWeaponStateName);
        attackHash = GetStateHash(attackStateName);

        if (animator == null || string.IsNullOrWhiteSpace(handLayerName))
        {
            handLayerIndex = -1;
            return;
        }

        handLayerIndex = animator.GetLayerIndex(handLayerName);
    }

    private int GetStateHash(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return 0;
        }

        return Animator.StringToHash(stateName);
    }
}
