using UnityEngine;

/// <summary>
/// Điều khiển chế độ "Clue Vision": nhấn phím để bật/tắt hiệu ứng tìm manh mối.
/// Tìm mọi object có tag "Clue" và bật/tắt trạng thái highlight cho chúng.
/// </summary>
public class EchoVision : MonoBehaviour
{
    [Header("Cài đặt phím & thời gian")]
    [Tooltip("Phím bật/tắt Clue Vision")]
    public KeyCode activateKey = KeyCode.F;
    [Tooltip("Tự tắt sau bao nhiêu giây (0 = không giới hạn)")]
    public float visionDuration = 5f;

    private bool visionActive = false;
    private float timer = 0f;

    void Update()
    {
        // --- Bật/tắt khi nhấn phím ---
        if (Input.GetKeyDown(activateKey))
        {
            visionActive = !visionActive;
            timer = 0f;
            // Báo cho camera áp dụng hiệu ứng tối màn hình
            Camera.main.GetComponent<CameraEffect>()?.SetEchoMode(visionActive);
        }

        if (visionActive)
        {
            timer += Time.deltaTime;
            // Tự tắt sau visionDuration giây
            if (timer >= visionDuration)
            {
                visionActive = false;
                Camera.main.GetComponent<CameraEffect>()?.SetEchoMode(false);
            }
            HighlightClues(true);
        }
        else
        {
            HighlightClues(false);
        }
    }

    /// <summary>
    /// Bật hoặc tắt trạng thái "đang được highlight" cho tất cả Clue.
    /// Dựa vào tag "Clue" để tìm object, sau đó set isActive trên component EchoObject.
    /// </summary>
    void HighlightClues(bool state)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("Clue"))
        {
            obj.GetComponent<EchoObject>().isActive = state;
        }
    }
}
