using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Strips URP Lit variants for render features disabled in this project.
/// This avoids Unity compiling unreachable tree/foliage variants that can OOM during Windows builds.
/// </summary>
public sealed class URPLitBuildVariantStripper : IPreprocessShaders
{
    private const string TargetShaderName = "Universal Render Pipeline/Lit";

    private static readonly ShaderKeyword ForwardPlus = new ShaderKeyword("_FORWARD_PLUS");
    private static readonly ShaderKeyword AdditionalLightShadows = new ShaderKeyword("_ADDITIONAL_LIGHT_SHADOWS");
    private static readonly ShaderKeyword LightCookies = new ShaderKeyword("_LIGHT_COOKIES");
    private static readonly ShaderKeyword LightLayers = new ShaderKeyword("_LIGHT_LAYERS");
    private static readonly ShaderKeyword ReflectionProbeBlending = new ShaderKeyword("_REFLECTION_PROBE_BLENDING");
    private static readonly ShaderKeyword ReflectionProbeBoxProjection = new ShaderKeyword("_REFLECTION_PROBE_BOX_PROJECTION");
    private static readonly ShaderKeyword ScreenSpaceOcclusion = new ShaderKeyword("_SCREEN_SPACE_OCCLUSION");

    public int callbackOrder => 10000;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader == null || shader.name != TargetShaderName || data == null)
            return;

        for (var i = data.Count - 1; i >= 0; i--)
        {
            var keywords = data[i].shaderKeywordSet;
            if (ShouldStrip(keywords))
                data.RemoveAt(i);
        }
    }

    private static bool ShouldStrip(ShaderKeywordSet keywords)
    {
        return keywords.IsEnabled(ForwardPlus)
            || keywords.IsEnabled(AdditionalLightShadows)
            || keywords.IsEnabled(LightCookies)
            || keywords.IsEnabled(LightLayers)
            || keywords.IsEnabled(ReflectionProbeBlending)
            || keywords.IsEnabled(ReflectionProbeBoxProjection)
            || keywords.IsEnabled(ScreenSpaceOcclusion);
    }
}
