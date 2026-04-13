using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    public Slider staminaSlider;
    public PlayerMovement playerMovement;
    public Rigidbody playerRigidbody;
    public float maxStamina = 100f;
    public float staminaFallRate = 15f;
    public float idleStaminaFallRate = 10f;
    public float movementThreshold = 0.05f;

    private void Awake()
    {
        TryAssignReferences();
    }

    private void Reset()
    {
        TryAssignReferences();
    }

    void Start()
    {
        if (staminaSlider == null)
        {
            return;
        }

        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = maxStamina;
    }

    void Update()
    {
        if (staminaSlider == null)
        {
            return;
        }

        bool isMoving = IsPlayerMoving();

        if (isMoving)
        {
            staminaSlider.value -= staminaFallRate * Time.deltaTime;
        }
        else
        {
            staminaSlider.value -= idleStaminaFallRate * Time.deltaTime;
        }

        staminaSlider.value = Mathf.Clamp(staminaSlider.value, 0, maxStamina);
    }

    private bool IsPlayerMoving()
    {
        if (playerMovement != null && playerMovement.IsCutscenePlaying)
        {
            return false;
        }

        if (playerRigidbody != null)
        {
            Vector3 horizontalVelocity = playerRigidbody.linearVelocity;
            horizontalVelocity.y = 0f;
            return horizontalVelocity.sqrMagnitude > movementThreshold * movementThreshold;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        return moveX != 0f || moveZ != 0f;
    }

    private void TryAssignReferences()
    {
        if (staminaSlider == null)
        {
            staminaSlider = GetComponent<Slider>();
        }

        if (playerMovement == null)
        {
            playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        }

        if (playerRigidbody == null && playerMovement != null)
        {
            playerRigidbody = playerMovement.GetComponent<Rigidbody>();
        }
    }
}
