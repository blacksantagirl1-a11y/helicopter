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
    public float speed = 5f;

    [Header("Running")]
    public bool canRun = true;
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;
    public bool IsRunning { get; private set; }
    public bool IsCutscenePlaying => isCutscenePlaying;

    [Header("Animation Parameters")]
    [SerializeField] private string moveStateParameterName = "MoveState";

    [Header("Cutscene Trigger")]
    [SerializeField] private bool useAnyTriggerWhenCutsceneTriggerIsEmpty = true;
    [SerializeField] private Collider cutsceneTrigger;
    [SerializeField] private Camera cameraMain;
    [SerializeField] private Camera cameraForCutscene;
    [SerializeField] private string cutsceneStateName = "Fishing";
    [SerializeField] private string cutsceneLayerName = "HandAnim";
    [SerializeField] private float cutsceneTransitionDuration = 0.1f;
    [SerializeField] private bool useCutsceneRootMotion = false;
    [SerializeField] private bool disableCutsceneTriggerAfterUse = true;
    [SerializeField] private Behaviour[] controlsToDisableDuringCutscene;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private Animator animator;
    private Rigidbody rigidbodyComponent;
    private ActionScript actionScript;
    private RigidbodyConstraints originalConstraints;
    private bool originalRigidbodyIsKinematic;
    private bool originalAnimatorApplyRootMotion;
    private Collider currentCutsceneTrigger;
    private Coroutine cutsceneRoutine;
    private int moveStateParameterHash;
    private int currentMoveState = MoveStateIdle;
    private int cutsceneLayerIndex;
    private bool isCutscenePlaying;

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
        if (animator == null)
        {
            return;
        }

        animator.applyRootMotion = false;
        animator.SetLayerWeight(0, 1f);
        ApplyMoveState(MoveStateIdle, true);

        if (actionScript != null)
        {
            actionScript.RefreshAnimatorState();
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

        if (Input.GetKeyDown(KeyCode.E) && currentCutsceneTrigger != null && cutsceneRoutine == null)
        {
            cutsceneRoutine = StartCoroutine(PlayCutscene());
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

        if (useAnyTriggerWhenCutsceneTriggerIsEmpty)
        {
            currentCutsceneTrigger = other;
        }
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

    private IEnumerator PlayCutscene()
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

        if (cameraForCutscene != null)
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

        if (disableCutsceneTriggerAfterUse && currentCutsceneTrigger != null)
        {
            currentCutsceneTrigger.enabled = false;
        }

        if (cameraForCutscene != null)
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
        if (actionScript != null &&
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
}
