using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Controller")]
    public Transform groundCheck;
    public Vector3 velocity;
    public LayerMask groundMask;
    public CharacterController controller;
    public bool isGrounded;
    public float speed = 12;
    public float gravity = -10;
    public float jumpHeight = 3f;
    public float groundDistance = 0.4f;

    [Header("Camera")]
    public Camera playerCamera;
    public float cameraHeight = 1.6f;  
    public float mouseSensitivity = 100f; 
    private float xRotation = 0f; 

    [Header("Camera Zoom")]
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 4f;
    public float currentDistance = 4f;


    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;  

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = (minDistance + maxDistance) / 2f;  
    }

    void Update()
    {
        mouseLook();  
        zoom();       
        controll();   
    }

    void LateUpdate()
    {
        updateCamera();  
    }

    void mouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);  
    }

    void controll()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxisRaw("Horizontal");  
        float z = Input.GetAxisRaw("Vertical");    

        Vector3 camRight = playerCamera.transform.right;
        Vector3 camForward = playerCamera.transform.forward;
        camRight.y = 0f;
        camForward.y = 0f;
        camRight.Normalize();
        camForward.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void zoom()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    void updateCamera()
    {
        if (playerCamera == null) return;

        Quaternion rotation = Quaternion.Euler(xRotation, transform.eulerAngles.y, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance) + Vector3.up * cameraHeight;

        playerCamera.transform.position = transform.position + offset;


        Vector3 lookTarget = transform.position + Vector3.up * cameraHeight;
        playerCamera.transform.LookAt(lookTarget);
    }
}