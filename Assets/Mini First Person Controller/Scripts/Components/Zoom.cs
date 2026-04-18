using UnityEngine;

[ExecuteInEditMode]
public class Zoom : MonoBehaviour
{
    Camera camera;
    [Tooltip("FOV mặc định của camera khi không zoom")]
    public float defaultFOV = 60;
    [Tooltip("FOV nhỏ nhất khi zoom tối đa")]
    public float maxZoomFOV = 15;
    [Range(0, 1)]
    [Tooltip("Mức zoom hiện tại (0 = không zoom, 1 = tối đa)")]
    public float currentZoom;
    [Tooltip("Độ nhạy cuộn chuột khi zoom")]
    public float sensitivity = 1;


    void Awake()
    {
        // Get the camera on this gameObject and the defaultZoom.
        camera = GetComponent<Camera>();
        if (camera)
        {
            defaultFOV = camera.fieldOfView;
        }
    }

    void Update()
    {
        // Update the currentZoom and the camera's fieldOfView.
        currentZoom += Input.mouseScrollDelta.y * sensitivity * .05f;
        currentZoom = Mathf.Clamp01(currentZoom);
        camera.fieldOfView = Mathf.Lerp(defaultFOV, maxZoomFOV, currentZoom);
    }
}
