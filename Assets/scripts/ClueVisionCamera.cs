using UnityEngine;

/// <summary>
/// Hiệu ứng post-process trên camera: làm tối màn hình khi Clue Vision bật.
/// Gắn vào Main Camera. EchoVision gọi SetEchoMode(true/false) để bật/tắt.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraEffect : MonoBehaviour
{
    private Material mat;

    [Header("Hiệu ứng Clue Vision")]
    [Tooltip("Shader tối camera (để trống sẽ dùng ClueVisionDarken mặc định)")]
    public Shader darkenShader;
    [Range(0f, 1f)]
    [Tooltip("Mức độ tối (0 = không tối, 1 = đen hoàn toàn)")]
    public float darkness = 0.7f;

    private bool echoMode = false;

    void Start()
    {
        Shader shader = darkenShader != null ? darkenShader : Shader.Find("Custom/ClueVisionDarken");
        if (shader != null)
            mat = new Material(shader);
    }

    /// <summary>
    /// Bật/tắt chế độ tối màn hình. Được EchoVision gọi khi nhấn phím Clue Vision.
    /// </summary>
    public void SetEchoMode(bool active)
    {
        echoMode = active;
    }

    /// <summary>
    /// Unity gọi mỗi frame sau khi camera vẽ xong: áp dụng shader tối lên toàn màn hình.
    /// </summary>
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (echoMode && mat != null)
        {
            mat.SetFloat("_Darkness", darkness);
            Graphics.Blit(src, dest, mat);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
