using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerMovement : MonoBehaviour
{
    Animator animator;
    int ForwardHash;
    int LeftHash;
    int RightHash;
    int BackwardHash;

    public float speed = 5;
    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;
    

    Rigidbody rigidbody;
    /// <summary> Các hàm để ghi đè tốc độ di chuyển. Sẽ sử dụng ghi đè được thêm cuối cùng. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();


   
    void Awake()
    {
        // Lấy rigidbody trên đối tượng này.
        rigidbody = GetComponent<Rigidbody>();
      
        

    }
    private void Start()
    {
        animator = GetComponent<Animator>();
        ForwardHash = Animator.StringToHash("MoveForward");
        LeftHash = Animator.StringToHash("MoveLeft");
        RightHash = Animator.StringToHash("MoveRight");
        BackwardHash = Animator.StringToHash("MoveBackward");
    }

    void FixedUpdate()
    {
        // Cập nhật IsRunning từ input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Lấy targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity =new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Áp dụng di chuyển.
        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);
    }
    private void Update()
    {
        bool MoveForward = animator.GetBool("MoveForward");
        bool forwardPressed = Input.GetKey("w");
        bool MoveLeft = animator.GetBool("MoveLeft");
        bool LeftPressed = Input.GetKey("a");
        bool MoveRight = animator.GetBool("MoveRight");
        bool rightPressed = Input.GetKey("d");
        bool MoveBackward = animator.GetBool("MoveBackward");
        bool backwardPressed = Input.GetKey("s");
        
        if (!MoveForward && forwardPressed) 
        {
            animator.SetBool(ForwardHash, true);
        }
        if (MoveForward && !forwardPressed) 
        {
            animator.SetBool(ForwardHash, false);
        }
        if (!MoveLeft && LeftPressed)
        {
            animator.SetBool(LeftHash, true);
        }
        if (MoveLeft && !LeftPressed)
        {
            animator.SetBool(LeftHash, false);
        }
        if (!MoveRight && rightPressed)
        {
            animator.SetBool(RightHash, true);
        }
        if (MoveRight && !rightPressed)
        {
            animator.SetBool(RightHash, false);
        }
        if (!MoveBackward && backwardPressed)
        {
            animator.SetBool(BackwardHash, true);
        }
        if (MoveBackward && !backwardPressed)
        {
            animator.SetBool(BackwardHash, false);
        }
    }
}