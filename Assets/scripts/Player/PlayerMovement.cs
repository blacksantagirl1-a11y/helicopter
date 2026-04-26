using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const int MoveStateIdle = 0;
    private const int MoveStateForward = 1;
    private const int MoveStateRight = 2;
    private const int MoveStateBackward = 3;
    private const int MoveStateLeft = 4;
    private const int MoveStateRunning = 5;

    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển cơ bản của nhân vật")]
    public float speed = 5f;

    [Header("Running")]
    [Tooltip("Cho phép nhân vật chạy")]
    public bool canRun = true;
    [Tooltip("Tốc độ khi giữ phím chạy")]
    public float runSpeed = 9f;
    [Tooltip("Phím dùng để chạy")]
    public KeyCode runningKey = KeyCode.LeftShift;
    public bool IsRunning { get; private set; }
    public bool IsCutscenePlaying => isCutscenePlaying;

    [Header("Animation Parameters")]
    [Tooltip("Tên tham số MoveState trong Animator")]
    [SerializeField] private string moveStateParameterName = "MoveState";

    [Header("Cutscene Trigger")]
    [Tooltip("Nếu chưa gán trigger cụ thể thì cho phép mọi trigger kích hoạt cutscene")]
    [SerializeField] private bool useAnyTriggerWhenCutsceneTriggerIsEmpty = true;
    [Tooltip("Trigger collider dùng để kích hoạt cutscene")]
    [SerializeField] private Collider cutsceneTrigger;
    [Tooltip("Camera gameplay chính")]
    [SerializeField] private Camera cameraMain;
    [Tooltip("Camera dùng khi phát cutscene")]
    [SerializeField] private Camera cameraForCutscene;
    [Tooltip("Tên state cutscene trong Animator")]
    [SerializeField] private string cutsceneStateName = "Fishing";
    [Tooltip("Bật nếu cutscene Fishing cần ép ActionScript sang state câu cá thay vì phát state trên Animator")]
    [SerializeField] private bool useActionScriptFishingStateForCutscene = false;
    [Tooltip("Tên layer Animator chứa animation cutscene")]
    [SerializeField] private string cutsceneLayerName = "HandAnim";
    [Tooltip("Thời gian chuyển state animation cutscene")]
    [SerializeField] private float cutsceneTransitionDuration = 0.1f;
    [Tooltip("Bật để cho phép root motion trong cutscene")]
    [SerializeField] private bool useCutsceneRootMotion = false;
    [Tooltip("Tắt trigger sau khi cutscene đã chạy xong")]
    [SerializeField] private bool disableCutsceneTriggerAfterUse = true;
    [Tooltip("Danh sách component điều khiển sẽ bị tắt trong cutscene")]
    [SerializeField] private Behaviour[] controlsToDisableDuringCutscene;

    [Header("Intro Dialogue")]
    [Tooltip("Neu scene khong co IntroSequenceController thi van tu goi hoi thoai IntroWakeUp luc bat dau game")]
    [SerializeField] private bool requestIntroWakeUpWhenNoIntroSequence = true;

    [Tooltip("Danh sách hàm override tốc độ di chuyển theo ngữ cảnh")]
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private Animator animator;
    private Rigidbody rigidbodyComponent;
    private ActionScript actionScript;
    private FishingRob fishingRob;
    private RigidbodyConstraints originalConstraints;
    private bool originalRigidbodyIsKinematic;
    private bool originalAnimatorApplyRootMotion;
    private Collider currentCutsceneTrigger;
    private Coroutine cutsceneRoutine;
    private int moveStateParameterHash;
    private int currentMoveState = MoveStateIdle;
    private int cutsceneLayerIndex;
    private bool isCutscenePlaying;
    private bool hasRequestedIntroWakeUpDialogue;

    private void Reset()
    {
        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigidbodyComponent = GetComponent<Rigidbody>();
        actionScript = GetComponent<ActionScript>();
        originalConstraints = rigidbodyComponent != null
            ? rigidbodyComponent.constraints
            : RigidbodyConstraints.None;

        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetLayerWeight(0, 1f);
            ApplyMoveState(MoveStateIdle, true);
        }

        if (actionScript != null)
        {
            actionScript.RefreshAnimatorState();
        }

        if (requestIntroWakeUpWhenNoIntroSequence &&
            FindFirstObjectByType<IntroSequenceController>() == null)
        {
            RequestIntroWakeUpDialogue();
        }
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        CacheAnimatorData();
    }

    private void FixedUpdate()
    {
        if (rigidbodyComponent == null)
        {
            return;
        }

        if (isCutscenePlaying)
        {
            if (!rigidbodyComponent.isKinematic)
            {
                rigidbodyComponent.linearVelocity = Vector3.zero;
                rigidbodyComponent.angularVelocity = Vector3.zero;
            }

            return;
        }

        IsRunning = canRun && HasMovementInput() && Input.GetKey(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 targetVelocity = new Vector2(
            Input.GetAxis("Horizontal") * targetMovingSpeed,
            Input.GetAxis("Vertical") * targetMovingSpeed);

        rigidbodyComponent.linearVelocity =
            transform.rotation * new Vector3(targetVelocity.x, rigidbodyComponent.linearVelocity.y, targetVelocity.y);
    }

    private void Update()
    {
        if (isCutscenePlaying)
        {
            return;
        }

        bool shouldDeferInteractToFishing =
            fishingRob != null &&
            fishingRob.ShouldOverrideDefaultInteraction;

        if (!shouldDeferInteractToFishing &&
            Input.GetKeyDown(KeyCode.E) &&
            currentCutsceneTrigger != null &&
            cutsceneRoutine == null)
        {
            TryPlayConfiguredCutscene(disableCutsceneTriggerAfterUse, currentCutsceneTrigger);
            return;
        }

        UpdateMovementAnimation();
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySetCutsceneTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetCutsceneTrigger(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentCutsceneTrigger)
        {
            currentCutsceneTrigger = null;
        }
    }

    private void TrySetCutsceneTrigger(Collider other)
    {
        if (other == null || !other.isTrigger || isCutscenePlaying)
        {
            return;
        }

        if (cutsceneTrigger != null)
        {
            if (other == cutsceneTrigger)
            {
                currentCutsceneTrigger = other;
            }

            return;
        }

        if (useAnyTriggerWhenCutsceneTriggerIsEmpty && IsFallbackCutsceneTrigger(other))
        {
            currentCutsceneTrigger = other;
        }
    }

    private bool IsFallbackCutsceneTrigger(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponent<CutsceneTrigger>() != null ||
            other.GetComponentInParent<CutsceneTrigger>() != null)
        {
            return true;
        }

        string triggerName = other.gameObject.name;
        return !string.IsNullOrWhiteSpace(triggerName) &&
               triggerName.IndexOf("CutsceneTrigger", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UpdateMovementAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool rightPressed = Input.GetKey(KeyCode.D);
        bool backwardPressed = Input.GetKey(KeyCode.S);
        bool leftPressed = Input.GetKey(KeyCode.A);

        int nextMoveState = MoveStateIdle;
        if (canRun && (forwardPressed || rightPressed || backwardPressed || leftPressed) && Input.GetKey(runningKey))
        {
            nextMoveState = MoveStateRunning;
        }
        else if (forwardPressed && !backwardPressed)
        {
            nextMoveState = MoveStateForward;
        }
        else if (backwardPressed && !forwardPressed)
        {
            nextMoveState = MoveStateBackward;
        }
        else if (rightPressed && !leftPressed)
        {
            nextMoveState = MoveStateRight;
        }
        else if (leftPressed && !rightPressed)
        {
            nextMoveState = MoveStateLeft;
        }

        ApplyMoveState(nextMoveState, false);
    }

    private bool HasMovementInput()
    {
        return Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.D);
    }

    private void ApplyMoveState(int moveState, bool force)
    {
        if (animator == null || moveStateParameterHash == 0)
        {
            return;
        }

        if (!force && currentMoveState == moveState)
        {
            return;
        }

        animator.SetInteger(moveStateParameterHash, moveState);
        currentMoveState = moveState;
    }

    private void OnAnimatorMove()
    {
        if (!isCutscenePlaying || !useCutsceneRootMotion || animator == null)
        {
            return;
        }

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.MovePosition(animator.rootPosition);
            rigidbodyComponent.MoveRotation(animator.rootRotation);
            return;
        }

        transform.SetPositionAndRotation(animator.rootPosition, animator.rootRotation);
    }

    public bool TryPlayConfiguredCutscene(bool shouldDisableTriggerAfterUse, Collider triggerToDisable)
    {
        if (!isActiveAndEnabled || isCutscenePlaying || cutsceneRoutine != null)
        {
            return false;
        }

        Collider resolvedTrigger = triggerToDisable != null
            ? triggerToDisable
            : currentCutsceneTrigger;

        cutsceneRoutine = StartCoroutine(PlayCutscene(shouldDisableTriggerAfterUse, resolvedTrigger));
        return true;
    }

    private IEnumerator PlayCutscene(bool shouldDisableTriggerAfterUse, Collider triggerToDisable)
    {
        isCutscenePlaying = true;
        IsRunning = false;
        ApplyMoveState(MoveStateIdle, true);

        if (animator != null)
        {
            originalAnimatorApplyRootMotion = animator.applyRootMotion;
            animator.applyRootMotion = useCutsceneRootMotion;
        }

        if (rigidbodyComponent != null)
        {
            originalRigidbodyIsKinematic = rigidbodyComponent.isKinematic;
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
            rigidbodyComponent.isKinematic = useCutsceneRootMotion;
            rigidbodyComponent.constraints = useCutsceneRootMotion
                ? originalConstraints
                : RigidbodyConstraints.FreezeAll;
        }

        SetControlsState(false);

        bool shouldSwitchToCutsceneCamera =
            cameraForCutscene != null &&
            cameraForCutscene.gameObject.activeInHierarchy;

        if (cameraForCutscene != null && !shouldSwitchToCutsceneCamera)
        {
            Debug.LogWarning(
                "PlayerMovement skipped cutscene camera switch because CameraForCutscene is inactive in hierarchy.",
                this);
        }

        if (shouldSwitchToCutsceneCamera)
        {
            SetCameraState(cameraMain, false);
            SetCameraState(cameraForCutscene, true);
        }

        if (animator != null && cutsceneLayerIndex > 0)
        {
            animator.SetLayerWeight(cutsceneLayerIndex, 1f);
        }

        PlayCutsceneAnimation();

        yield return null;

        string cutsceneStatePath = GetCutsceneStatePath();
        float waitTimeout = GetClipLength(cutsceneStateName) + 0.25f;
        if (waitTimeout <= 0.25f)
        {
            waitTimeout = 1f;
        }

        float elapsed = 0f;
        while (elapsed < waitTimeout)
        {
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(cutsceneLayerIndex);
                if (!animator.IsInTransition(cutsceneLayerIndex) &&
                    stateInfo.IsName(cutsceneStatePath) &&
                    stateInfo.normalizedTime >= 1f)
                {
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (shouldDisableTriggerAfterUse && triggerToDisable != null)
        {
            triggerToDisable.enabled = false;
        }

        if (shouldSwitchToCutsceneCamera)
        {
            SetCameraState(cameraForCutscene, false);
            SetCameraState(cameraMain, true);
        }

        SetControlsState(true);

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.isKinematic = originalRigidbodyIsKinematic;
            rigidbodyComponent.constraints = originalConstraints;
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.applyRootMotion = originalAnimatorApplyRootMotion;
        }

        isCutscenePlaying = false;
        currentCutsceneTrigger = null;

        if (actionScript != null)
        {
            actionScript.RefreshAnimatorState();
        }

        ApplyMoveState(MoveStateIdle, true);
        cutsceneRoutine = null;
    }

    private void SetControlsState(bool isEnabled)
    {
        if (controlsToDisableDuringCutscene == null)
        {
            return;
        }

        for (int i = 0; i < controlsToDisableDuringCutscene.Length; i++)
        {
            Behaviour behaviour = controlsToDisableDuringCutscene[i];
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = isEnabled;
            }
        }
    }

    private void SetCameraState(Camera targetCamera, bool isEnabled)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.enabled = isEnabled;

        AudioListener listener = targetCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = isEnabled;
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

        if (rigidbodyComponent == null)
        {
            rigidbodyComponent = GetComponent<Rigidbody>();
        }

        if (actionScript == null)
        {
            actionScript = GetComponent<ActionScript>();
        }

        if (fishingRob == null)
        {
            fishingRob = GetComponent<FishingRob>();
        }

        if (cameraMain == null)
        {
            Camera taggedMainCamera = Camera.main;
            if (taggedMainCamera != null)
            {
                cameraMain = taggedMainCamera;
            }
            else
            {
                GameObject cameraMainObject = GameObject.Find("CameraMain");
                if (cameraMainObject != null)
                {
                    cameraMain = cameraMainObject.GetComponent<Camera>();
                }
            }
        }

        if (cameraForCutscene == null)
        {
            GameObject cutsceneCameraObject = GameObject.Find("CameraForCutscene");
            if (cutsceneCameraObject != null)
            {
                cameraForCutscene = cutsceneCameraObject.GetComponent<Camera>();
            }
        }
    }

    private void CacheAnimatorData()
    {
        moveStateParameterHash = string.IsNullOrWhiteSpace(moveStateParameterName)
            ? 0
            : Animator.StringToHash(moveStateParameterName);

        if (animator == null || string.IsNullOrWhiteSpace(cutsceneLayerName))
        {
            cutsceneLayerIndex = 0;
            return;
        }

        cutsceneLayerIndex = animator.GetLayerIndex(cutsceneLayerName);
        if (cutsceneLayerIndex < 0)
        {
            cutsceneLayerIndex = 0;
        }
    }

    private void PlayCutsceneAnimation()
    {
        if (useActionScriptFishingStateForCutscene &&
            actionScript != null &&
            string.Equals(cutsceneStateName, "Fishing", System.StringComparison.Ordinal))
        {
            actionScript.PlayFishingState();
            return;
        }

        if (animator == null)
        {
            return;
        }

        string cutsceneStatePath = GetCutsceneStatePath();
        if (string.IsNullOrWhiteSpace(cutsceneStatePath))
        {
            return;
        }

        animator.CrossFadeInFixedTime(cutsceneStatePath, cutsceneTransitionDuration, cutsceneLayerIndex);
    }

    private string GetCutsceneStatePath()
    {
        if (string.IsNullOrWhiteSpace(cutsceneStateName))
        {
            return string.Empty;
        }

        return GetLayerNameForIndex(cutsceneLayerIndex) + "." + cutsceneStateName;
    }

    private string GetLayerNameForIndex(int layerIndex)
    {
        if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount)
        {
            return "Base Layer";
        }

        return animator.GetLayerName(layerIndex);
    }

    public void RequestIntroWakeUpDialogue()
    {
        if (hasRequestedIntroWakeUpDialogue)
        {
            return;
        }

        hasRequestedIntroWakeUpDialogue = true;
        DialogueController.RequestDialogue(DialogueEventId.IntroWakeUp);
    }
}
