using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gan vao tung object la "Clue". Khi isActive = true (do EchoVision bat Clue Vision),
/// object chuyen sang shader highlight va co hieu ung phat sang nhap nhay.
/// Object can co tag "Clue" va it nhat mot Renderer (hoac con co Renderer).
/// </summary>
public class EchoObject : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

    [Tooltip("EchoVision set qua HighlightClues(); true = dang o che do Clue Vision")]
    public bool isActive;

    [Header("Shader Clue khi active")]
    [Tooltip("Shader highlight cho Clue (de trong dung Custom/ClueHighlight)")]
    public Shader clueHighlightShader;
    [Tooltip("Mau phat sang")]
    public Color emissionColor = new Color(1f, 0.8f, 0.2f);
    [Range(0.5f, 3f)]
    [Tooltip("Cuong do sang")]
    public float emissionIntensity = 1.5f;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Shader> originalShaders = new List<Shader>();
    private readonly List<Texture> originalTextures = new List<Texture>();
    private readonly List<Color> originalColors = new List<Color>();

    private Shader highlightShader;
    private bool wasActive;

    void Awake()
    {
        CacheMaterials();
    }

    void Update()
    {
        if (materials.Count == 0 || highlightShader == null)
        {
            return;
        }

        if (isActive != wasActive)
        {
            ApplyState(isActive);
        }

        if (!isActive)
        {
            return;
        }

        float pulse = Mathf.PingPong(Time.time * 2f, 1f) * 0.5f + 1f;
        foreach (Material materialInstance in materials)
        {
            if (materialInstance == null)
            {
                continue;
            }

            if (materialInstance.HasProperty(EmissionColorId))
            {
                materialInstance.SetColor(EmissionColorId, emissionColor);
            }

            if (materialInstance.HasProperty(EmissionIntensityId))
            {
                materialInstance.SetFloat(EmissionIntensityId, emissionIntensity * pulse);
            }
        }
    }

    void OnDisable()
    {
        if (wasActive)
        {
            ApplyState(false);
        }
    }

    void CacheMaterials()
    {
        highlightShader = clueHighlightShader != null ? clueHighlightShader : Shader.Find("Custom/ClueHighlight");
        if (highlightShader == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rendererComponent in renderers)
        {
            if (rendererComponent == null)
            {
                continue;
            }

            Material[] rendererMaterials = rendererComponent.materials;
            foreach (Material materialInstance in rendererMaterials)
            {
                if (materialInstance == null)
                {
                    continue;
                }

                materials.Add(materialInstance);
                originalShaders.Add(materialInstance.shader);
                originalTextures.Add(ReadMainTexture(materialInstance));
                originalColors.Add(ReadColor(materialInstance));
            }
        }
    }

    void ApplyState(bool active)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            Material materialInstance = materials[i];
            if (materialInstance == null)
            {
                continue;
            }

            if (active)
            {
                materialInstance.shader = highlightShader;
                WriteTexture(materialInstance, originalTextures[i] != null ? originalTextures[i] : Texture2D.whiteTexture);
                WriteColor(materialInstance, originalColors[i]);
            }
            else
            {
                materialInstance.shader = originalShaders[i];
                WriteTexture(materialInstance, originalTextures[i]);
                WriteColor(materialInstance, originalColors[i]);
            }
        }

        wasActive = active;
    }

    Texture ReadMainTexture(Material materialInstance)
    {
        if (materialInstance.HasProperty(BaseMapId))
        {
            return materialInstance.GetTexture(BaseMapId);
        }

        if (materialInstance.HasProperty(MainTexId))
        {
            return materialInstance.GetTexture(MainTexId);
        }

        return null;
    }

    Color ReadColor(Material materialInstance)
    {
        if (materialInstance.HasProperty(BaseColorId))
        {
            return materialInstance.GetColor(BaseColorId);
        }

        if (materialInstance.HasProperty(ColorId))
        {
            return materialInstance.GetColor(ColorId);
        }

        return Color.white;
    }

    void WriteTexture(Material materialInstance, Texture texture)
    {
        if (materialInstance.HasProperty(BaseMapId))
        {
            materialInstance.SetTexture(BaseMapId, texture);
            return;
        }

        if (materialInstance.HasProperty(MainTexId))
        {
            materialInstance.SetTexture(MainTexId, texture);
        }
    }

    void WriteColor(Material materialInstance, Color color)
    {
        if (materialInstance.HasProperty(BaseColorId))
        {
            materialInstance.SetColor(BaseColorId, color);
            return;
        }

        if (materialInstance.HasProperty(ColorId))
        {
            materialInstance.SetColor(ColorId, color);
        }
    }
}
