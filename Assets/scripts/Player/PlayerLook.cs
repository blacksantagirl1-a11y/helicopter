using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Tooltip("Transform thân nhân vật sẽ xoay theo trục ngang")]
    [SerializeField] private Transform character;

    [Tooltip("Độ nhạy chuột cho camera look")]
    public float sensitivity = 2f;
    [Tooltip("Độ mượt khi nội suy chuyển động nhìn (giá trị lớn hơn = mượt hơn)")]
    public float smoothing = 1.5f;

    private Vector2 velocity;
    private Vector2 frameVelocity;

    void Awake()
    {
        TryResolveCharacter();
        ApplySavedSensitivity();
    }

    void OnEnable()
    {
        LockCursor();
        TryResolveCharacter();
        ApplySavedSensitivity();
    }

    void Reset()
    {
        TryResolveCharacter();
    }

    void OnValidate()
    {
        TryResolveCharacter();
    }

    void Start()
    {
        ApplySavedSensitivity();
        LockCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockCursor();
        }
    }

    void Update()
    {
        if (character == null)
        {
            TryResolveCharacter();
            if (character == null)
            {
                return;
            }
        }

        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1f / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -60f, 90f);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    public void ForceLookTowards(Vector3 worldDirection)
    {
        TryResolveCharacter();
        if (character == null || worldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 horizontalDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
        if (horizontalDirection.sqrMagnitude > 0.0001f)
        {
            character.rotation = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
        }

        Vector3 localDirection = character.InverseTransformDirection(worldDirection.normalized);
        float signedPitch = Mathf.Atan2(
            localDirection.y,
            new Vector2(localDirection.x, localDirection.z).magnitude) * Mathf.Rad2Deg;

        velocity.x = NormalizeAngle(character.localEulerAngles.y);
        velocity.y = Mathf.Clamp(-signedPitch, -60f, 90f);
        frameVelocity = Vector2.zero;

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    void TryResolveCharacter()
    {
        if (character != null)
        {
            return;
        }

        PlayerMovement movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            character = movement.transform;
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ApplySavedSensitivity()
    {
        sensitivity = MenuSettingsService.GetLookSensitivity();
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
