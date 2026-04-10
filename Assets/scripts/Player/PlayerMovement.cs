using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Running")]
    public bool canRun = true;
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;
    public bool IsRunning { get; private set; }

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkingForwardStateName = "WalkingForward";
    [SerializeField] private string walkingRightStateName = "WalkingRight";
    [SerializeField] private string walkingBackwardStateName = "WalkingBackWard";
    [SerializeField] private string walkingLeftStateName = "WalkingLeft";
    [SerializeField] private string runningStateName = "Running";
    [SerializeField] private float movementTransitionDuration = 0.1f;

    [Header("Cutscene Trigger")]
    [SerializeField] private bool useAnyTriggerWhenCutsceneTriggerIsEmpty = true;
    [SerializeField] private Collider cutsceneTrigger;
    [SerializeField] private Camera cameraMain;
    [SerializeField] private Camera cameraForCutscene;
    [SerializeField] private string cutsceneStateName = "Crouch_Cutscene";
    [SerializeField] private float cutsceneTransitionDuration = 0.1f;
    [SerializeField] private bool useCutsceneRootMotion = true;
    [SerializeField] private bool disableCutsceneTriggerAfterUse = true;
    [SerializeField] private Behaviour[] controlsToDisableDuringCutscene;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private Animator animator;
    private Rigidbody rigidbodyComponent;
    private RigidbodyConstraints originalConstraints;
    private bool originalRigidbodyIsKinematic;
    private bool originalAnimatorApplyRootMotion;
    private Collider currentCutsceneTrigger;
    private Coroutine cutsceneRoutine;
    private int currentAnimationHash;

    private int idleHash;
    private int walkingForwardHash;
    private int walkingRightHash;
    private int walkingBackwardHash;
    private int walkingLeftHash;
    private int runningHash;
    private int cutsceneHash;

    private bool isCutscenePlaying;

    private void Reset()
    {
        TryAutoAssignReferences();
        CacheAnimationHashes();
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigidbodyComponent = GetComponent<Rigidbody>();
        originalConstraints = rigidbodyComponent != null
            ? rigidbodyComponent.constraints
            : RigidbodyConstraints.None;

        TryAutoAssignReferences();
        CacheAnimationHashes();
    }

    private void OnValidate()
    {
        TryAutoAssignReferences();
        CacheAnimationHashes();
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

        int nextAnimationHash = idleHash;

        if (canRun && (forwardPressed || rightPressed || backwardPressed || leftPressed) && Input.GetKey(runningKey))
        {
            nextAnimationHash = runningHash;
        }
        else if (forwardPressed && !backwardPressed)
        {
            nextAnimationHash = walkingForwardHash;
        }
        else if (backwardPressed && !forwardPressed)
        {
            nextAnimationHash = walkingBackwardHash;
        }
        else if (rightPressed && !leftPressed)
        {
            nextAnimationHash = walkingRightHash;
        }
        else if (leftPressed && !rightPressed)
        {
            nextAnimationHash = walkingLeftHash;
        }

        PlayAnimation(nextAnimationHash, movementTransitionDuration);
    }

    private bool HasMovementInput()
    {
        return Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.D);
    }

    private void PlayAnimation(int stateHash, float transitionDuration)
    {
        if (animator == null || stateHash == 0 || currentAnimationHash == stateHash)
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateHash, transitionDuration, 0);
        currentAnimationHash = stateHash;
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

        PlayAnimation(cutsceneHash, cutsceneTransitionDuration);

        yield return null;

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
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) &&
                    stateInfo.shortNameHash == cutsceneHash &&
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
        currentAnimationHash = 0;
        PlayAnimation(idleHash, movementTransitionDuration);
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

    private void CacheAnimationHashes()
    {
        idleHash = GetStateHash(idleStateName);
        walkingForwardHash = GetStateHash(walkingForwardStateName);
        walkingRightHash = GetStateHash(walkingRightStateName);
        walkingBackwardHash = GetStateHash(walkingBackwardStateName);
        walkingLeftHash = GetStateHash(walkingLeftStateName);
        runningHash = GetStateHash(runningStateName);
        cutsceneHash = GetStateHash(cutsceneStateName);
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
