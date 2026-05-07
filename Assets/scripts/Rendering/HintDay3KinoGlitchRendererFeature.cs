using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class HintDay3KinoGlitchRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader glitchShader;
    [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField] private float maxScanLineJitter = 0.55f;
    [SerializeField] private float maxVerticalJump = 0.18f;
    [SerializeField] private float maxHorizontalShake = 0.12f;
    [SerializeField] private float maxColorDrift = 0.24f;

    private const string ShaderName = "Hidden/Kino/Glitch/AnalogURP";

    private Material glitchMaterial;
    private GlitchPass glitchPass;

    public override void Create()
    {
        if (glitchShader == null)
        {
            glitchShader = Shader.Find(ShaderName);
        }

        CoreUtils.Destroy(glitchMaterial);
        glitchMaterial = glitchShader != null ? CoreUtils.CreateEngineMaterial(glitchShader) : null;

        glitchPass = new GlitchPass();
        glitchPass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        if (glitchMaterial == null)
        {
            if (glitchShader == null)
            {
                glitchShader = Shader.Find(ShaderName);
            }

            if (glitchShader == null)
            {
                return;
            }

            glitchMaterial = CoreUtils.CreateEngineMaterial(glitchShader);
        }

        if (HintDay3KinoGlitchState.Amount <= 0.001f)
        {
            return;
        }

        glitchPass.Setup(glitchMaterial, maxScanLineJitter, maxVerticalJump, maxHorizontalShake, maxColorDrift);
        renderer.EnqueuePass(glitchPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(glitchMaterial);
    }

    private sealed class GlitchPass : ScriptableRenderPass
    {
        private const string PassName = "HintDay3 Kino Glitch";
        private static readonly int ScanLineJitterId = Shader.PropertyToID("_ScanLineJitter");
        private static readonly int VerticalJumpId = Shader.PropertyToID("_VerticalJump");
        private static readonly int HorizontalShakeId = Shader.PropertyToID("_HorizontalShake");
        private static readonly int ColorDriftId = Shader.PropertyToID("_ColorDrift");

        private Material material;
        private float maxScanLineJitter;
        private float maxVerticalJump;
        private float maxHorizontalShake;
        private float maxColorDrift;
        private float verticalJumpTime;

        public GlitchPass()
        {
            requiresIntermediateTexture = true;
        }

        public void Setup(
            Material glitchMaterial,
            float scanLineJitter,
            float verticalJump,
            float horizontalShake,
            float colorDrift)
        {
            material = glitchMaterial;
            maxScanLineJitter = scanLineJitter;
            maxVerticalJump = verticalJump;
            maxHorizontalShake = horizontalShake;
            maxColorDrift = colorDrift;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
            {
                return;
            }

            float amount = HintDay3KinoGlitchState.Amount;
            if (amount <= 0.001f)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            ApplyMaterialProperties(amount);

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_HintDay3KinoGlitchColor";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, material, 0);
            renderGraph.AddBlitPass(parameters, PassName);
            resourceData.cameraColor = destination;
        }

        private void ApplyMaterialProperties(float amount)
        {
            amount = Mathf.Clamp01(amount);
            verticalJumpTime += Time.unscaledDeltaTime * maxVerticalJump * amount * 11.3f;

            material.SetVector(ScanLineJitterId, new Vector2(maxScanLineJitter * amount, Mathf.Lerp(1f, 0.05f, amount)));
            material.SetVector(VerticalJumpId, new Vector2(maxVerticalJump * amount, verticalJumpTime));
            material.SetFloat(HorizontalShakeId, maxHorizontalShake * amount);
            material.SetVector(ColorDriftId, new Vector2(maxColorDrift * amount, Time.unscaledTime * 606.11f));
        }
    }
}
