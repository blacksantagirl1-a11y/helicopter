using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform character;

    public float sensitivity = 2f;
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
}
