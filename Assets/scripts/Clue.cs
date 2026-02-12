using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn vào từng object là "Clue". Khi isActive = true (do EchoVision bật Clue Vision),
/// object chuyển sang shader highlight và có hiệu ứng phát sáng nhấp nháy.
/// Object cần có tag "Clue" và ít nhất một Renderer (hoặc con có Renderer).
/// </summary>
public class EchoObject : MonoBehaviour
{
    [Tooltip("EchoVision set qua HighlightClues(); true = đang ở chế độ Clue Vision")]
    public bool isActive = false;

    [Header("Shader Clue khi active")]
    [Tooltip("Shader highlight cho Clue (để trống dùng Custom/ClueHighlight)")]
    public Shader clueHighlightShader;
    [Tooltip("Màu phát sáng")]
    public Color emissionColor = new Color(1f, 0.8f, 0.2f);
    [Range(0.5f, 3f)]
    [Tooltip("Cường độ sáng")]
    public float emissionIntensity = 1.5f;

    // Cache: tất cả material (instance) và trạng thái gốc để khôi phục khi tắt
    private Renderer[] renderers;
    private List<Material> materials = new List<Material>();
    private List<Shader> originalShaders = new List<Shader>();
    private List<Texture> originalTextures = new List<Texture>();
    private List<Color> originalColors = new List<Color>();
    private bool wasActive = false;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        Shader highlightShader = clueHighlightShader != null ? clueHighlightShader : Shader.Find("Custom/ClueHighlight");
        if (highlightShader == null) return;

        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            // .material tạo bản copy material để không ảnh hưởng object khác dùng chung material
            Material mat = rend.material;
            materials.Add(mat);
            originalShaders.Add(mat.shader);
            // Lưu texture gốc (Standard dùng _MainTex, URP Lit dùng _BaseMap)
            Texture tex = null;
            if (mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
            else if (mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap");
            originalTextures.Add(tex);
            // Lưu màu gốc
            Color col = Color.white;
            if (mat.HasProperty("_Color")) col = mat.GetColor("_Color");
            else if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");
            originalColors.Add(col);
        }
    }

    void Update()
    {
        if (materials.Count == 0) return;
        Shader highlightShader = clueHighlightShader != null ? clueHighlightShader : Shader.Find("Custom/ClueHighlight");
        if (highlightShader == null) return;

        // --- Chỉ đổi shader khi isActive đổi trạng thái (bật → tắt hoặc ngược lại) ---
        if (isActive != wasActive)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                Material mat = materials[i];
                if (mat == null) continue;

                if (isActive)
                {
                    mat.shader = highlightShader;
                    mat.SetTexture("_BaseMap", originalTextures[i] != null ? originalTextures[i] : Texture2D.whiteTexture);
                    mat.SetColor("_BaseColor", originalColors[i]);
                }
                else
                {
                    mat.shader = originalShaders[i];
                }
            }
            wasActive = isActive;
        }

        // --- Khi đang active: cập nhật cường độ phát sáng để tạo hiệu ứng nhấp nháy ---
        if (isActive)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 1f) * 0.5f + 1f;
            foreach (var mat in materials)
            {
                if (mat != null && mat.shader.name.Contains("ClueHighlight"))
                {
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.SetFloat("_EmissionIntensity", emissionIntensity * pulse);
                }
            }
        }
    }
}
