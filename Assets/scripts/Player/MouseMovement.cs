using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    [Tooltip("Độ nhạy chuột khi xoay nhân vật")]
    public float mouseSensitivity = 100f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

    }
}