using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RunnerPlayerController : MonoBehaviour
{
    [Header("Refs")]
    public RunnerGameManager game;
    public Transform groundCheck;
    public LayerMask groundMask = ~0;

    [Header("Lane")]
    [Min(0.5f)] public float laneOffset = 2f; // x positions: -laneOffset, 0, +laneOffset
    [Min(1f)] public float laneChangeSpeed = 12f;

    [Header("Jump")]
    [Min(0f)] public float jumpHeight = 1.6f;
    [Min(0f)] public float gravity = -22f;
    [Min(0.01f)] public float groundCheckRadius = 0.18f;

    [Header("Fail")]
    public bool collideWithAnyRunnerObstacle = true;

    CharacterController _cc;
    int _lane; // -1, 0, +1
    float _yVel;
    bool _isGrounded;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!game) game = FindRunnerGameManager();
    }

    void Update()
    {
        if (!game || !game.IsRunning) return;

        HandleInput();
        UpdateGrounded();
        ApplyVertical();
        ApplyLane();
    }

    void HandleInput()
    {
        // A/D or Left/Right to change lane
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            _lane = Mathf.Clamp(_lane - 1, -1, 1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            _lane = Mathf.Clamp(_lane + 1, -1, 1);

        // Space to jump
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && _isGrounded)
        {
            // v = sqrt(2gh) with g negative in our convention
            _yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void UpdateGrounded()
    {
        Vector3 pos = groundCheck ? groundCheck.position : (transform.position + Vector3.up * 0.1f);
        _isGrounded = Physics.CheckSphere(pos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (_isGrounded && _yVel < 0f)
            _yVel = -2f; // keeps controller grounded
    }

    void ApplyVertical()
    {
        _yVel += gravity * Time.deltaTime;
        _cc.Move(new Vector3(0f, _yVel, 0f) * Time.deltaTime);
    }

    void ApplyLane()
    {
        float targetX = _lane * laneOffset;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        Vector3 delta = new Vector3(newX - transform.position.x, 0f, 0f);
        _cc.Move(delta);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!game || !game.IsRunning) return;
        if (!hit.collider) return;

        if (collideWithAnyRunnerObstacle)
        {
            if (hit.collider.GetComponentInParent<RunnerObstacle>() != null)
                game.GameOver();
        }
    }

    static RunnerGameManager FindRunnerGameManager()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<RunnerGameManager>();
#else
        return FindObjectOfType<RunnerGameManager>();
#endif
    }
}

