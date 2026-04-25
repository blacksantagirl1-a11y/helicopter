using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]

public class IntroSequenceController : MonoBehaviour
{
    [Header("Intro")]
    [Tooltip("Root object của cụm intro/cutscene mở đầu")]
    [SerializeField] GameObject introRoot;
    [Tooltip("Animator phát animation intro")]
    [SerializeField] Animator introAnimator;
    [Tooltip("Camera dùng trong đoạn intro")]
    [SerializeField] Camera introCamera;

    [Header("Player")]
    [Tooltip("Root object của player gameplay")]
    [SerializeField] GameObject playerRoot;
    [Tooltip("Camera gameplay của player")]
    [SerializeField] Camera playerCamera;

    readonly List<Behaviour> playerControls = new();

    Renderer[] playerRenderers = System.Array.Empty<Renderer>();
    Rigidbody playerRigidbody;
    RigidbodyConstraints originalConstraints;

    bool initialized;
    bool introFinished;

    void Reset()
    {
        // Khi vừa add component vào object trong Inspector, Unity sẽ thử tự điền reference.
        AutoResolveReferences();
    }

    void Awake()
    {
        // Awake chạy sớm khi scene bắt đầu.
        // Ở đây script tự tìm các object/camera cần dùng.
        initialized = AutoResolveReferences();
        if (!initialized)
        {
            enabled = false;
            return;
        }

        // Lưu lại trạng thái player hiện tại để lát nữa có thể bật lại đúng cách.
        CachePlayerState();
        // Áp dụng trạng thái intro ngay lập tức.
        ApplyIntroState();
    }

    void Start()
    {
        if (!initialized)
        {
            return;
        }

        // StartCoroutine = chạy một luồng "đợi" riêng.
        // Nó cho phép script chờ animation xong rồi mới làm bước tiếp theo.
        StartCoroutine(RunIntroSequence());
    }

    bool AutoResolveReferences()
    {
        // Nếu chưa gán tay trong Inspector thì tự lấy object hiện tại làm introRoot.
        introRoot ??= gameObject;
        introAnimator ??= introRoot.GetComponent<Animator>();
        introCamera ??= introRoot.GetComponentInChildren<Camera>(true);

        if (playerRoot == null)
        {
            // Tìm player thật bằng script di chuyển đang có trong scene.
            PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                playerRoot = movement.gameObject;
            }
        }

        if (playerCamera == null && playerRoot != null)
        {
            playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
        }

        bool hasReferences = introRoot != null && introAnimator != null && introCamera != null &&
            playerRoot != null && playerCamera != null;

        if (!hasReferences)
        {
            Debug.LogWarning("IntroSequenceController is missing scene references.", this);
        }

        return hasReferences;
    }

    void CachePlayerState()
    {
        // Lấy toàn bộ renderer của player để có thể ẩn/hiện model.
        playerRenderers = playerRoot.GetComponentsInChildren<Renderer>(true);
        playerRigidbody = playerRoot.GetComponent<Rigidbody>();

        if (playerRigidbody != null)
        {
            // Ghi nhớ cấu hình khóa trục ban đầu, để sau intro trả lại đúng như cũ.
            originalConstraints = playerRigidbody.constraints;
        }

        // Gom tất cả script điều khiển cần tắt lúc intro.
        playerControls.Clear();
        AddPlayerControl(playerRoot.GetComponent<PlayerMovement>());
        AddPlayerControl(playerRoot.GetComponent<Jump>());
        AddPlayerControl(playerRoot.GetComponent<Crouch>());
        AddPlayerControl(playerRoot.GetComponentInChildren<EchoVision>(true));
        AddPlayerControl(playerRoot.GetComponentInChildren<MouseMovement>(true));

        AddPlayerControl(playerCamera.GetComponent<PlayerLook>());
        AddPlayerControl(playerCamera.GetComponent<Zoom>());
        AddPlayerControl(playerCamera.GetComponent<PickUpScript>());
        AddPlayerControl(playerCamera.GetComponent<MouseMovement>());
    }

    void AddPlayerControl(Behaviour behaviour)
    {
        if (behaviour != null && !playerControls.Contains(behaviour))
        {
            playerControls.Add(behaviour);
        }
    }

    void ApplyIntroState()
    {
        // Trạng thái khi bắt đầu game:
        // player ẩn đi, không điều khiển được, camera gameplay tắt,
        // camera intro bật lên.
        SetPlayerVisible(false);
        SetPlayerControls(false);
        FreezePlayer(true);
        SetCameraState(playerCamera, false);
        SetCameraState(introCamera, true);
        SetOnlyActiveAudioListener(introCamera.GetComponent<AudioListener>());
    }

    IEnumerator RunIntroSequence()
    {
        // Rebind giúp animator quay về đúng frame bắt đầu của animation.
        introAnimator.Rebind();
        introAnimator.Update(0f);

        // Chờ 1 frame để animator cập nhật state hiện tại.
        yield return null;
        // Đợi cho đến khi animation intro chạy hết.
        yield return new WaitUntil(IsIntroAnimationComplete);

        // Sau khi intro xong thì trả game về trạng thái chơi bình thường.
        FinishIntro();
    }

    bool IsIntroAnimationComplete()
    {
        AnimatorStateInfo stateInfo = introAnimator.GetCurrentAnimatorStateInfo(0);
        // normalizedTime >= 1 nghĩa là animation đã chạy xong 1 vòng.
        return !introAnimator.IsInTransition(0) && stateInfo.normalizedTime >= 1f;
    }

    void FinishIntro()
    {
        if (introFinished)
        {
            return;
        }

        introFinished = true;

        // Bỏ khóa toàn bộ những gì đã khóa ở đầu game.
        SetPlayerVisible(true);
        FreezePlayer(false);
        SetCameraState(introCamera, false);
        SetCameraState(playerCamera, true);
        SetOnlyActiveAudioListener(playerCamera.GetComponent<AudioListener>());
        SetPlayerControls(true);

        PlayerMovement playerMovement = playerRoot.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.RequestIntroWakeUpDialogue();
        }

        // Tắt hẳn object intro để nó biến mất khỏi scene.
        introRoot.SetActive(false);
    }

    void SetPlayerVisible(bool isVisible)
    {
        foreach (Renderer rendererComponent in playerRenderers)
        {
            if (rendererComponent != null)
            {
                rendererComponent.enabled = isVisible;
            }
        }
    }

    void SetPlayerControls(bool isEnabled)
    {
        foreach (Behaviour behaviour in playerControls)
        {
            if (behaviour != null)
            {
                behaviour.enabled = isEnabled;
            }
        }
    }

    void FreezePlayer(bool shouldFreeze)
    {
        if (playerRigidbody == null)
        {
            return;
        }

        if (shouldFreeze)
        {
            // Xóa vận tốc hiện tại để player không bị trượt/rơi trong lúc intro chạy.
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            playerRigidbody.constraints = originalConstraints;
        }
    }

    void SetCameraState(Camera targetCamera, bool isEnabled)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.enabled = isEnabled;

        // Camera nào bật thì chỉ giữ AudioListener của camera đó.
        // Nếu 2 AudioListener cùng bật, Unity sẽ báo lỗi.
        AudioListener listener = targetCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = isEnabled;
        }
    }

    void SetOnlyActiveAudioListener(AudioListener activeListener)
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (AudioListener listener in listeners)
        {
            if (listener != null)
            {
                listener.enabled = listener == activeListener;
            }
        }
    }
}
