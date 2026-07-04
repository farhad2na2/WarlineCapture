#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AndroidVisualQualityValidationTests
{
    private const string MobileRenderPipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string VisualQualityProfilePath = "Assets/Game/Rendering/VisualQualityConfig.asset";
    private const float MinimumLowRenderScale = 0.50f;
    private const float BalancedMobileRenderScale = 0.50f;
    private const int BalancedMobileMsaa = 1;
    private const int BalancedMobileUpscalingFilter = 3;
    private const float BalancedMobileFsrSharpness = 0.72f;
    private const float BalancedMobileShadowDistance = 16f;

    public static void RunFocusedValidation()
    {
        try
        {
            int passed = 0;
            RunCase(() => MobileRenderPipelineUsesBalancedScaleAndMsaa(), ref passed);
            RunCase(() => VisualQualityProfileUsesBalancedAndroidMatchRendering(), ref passed);
            RunCase(() => MobileQualityTierUsesBalancedMsaaAndShadows(), ref passed);
            Debug.Log($"[AndroidVisualQualityValidation] result=Passed tests={passed}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AndroidVisualQualityValidation] result=Failed error={exception}");
            throw;
        }
    }

    private static void RunCase(Action test, ref int passed)
    {
        test();
        passed++;
    }

    [Test]
    public static void MobileRenderPipelineUsesBalancedScaleAndMsaa()
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MobileRenderPipelinePath);
        Assert.NotNull(asset, $"Missing mobile render pipeline asset at {MobileRenderPipelinePath}.");

        SerializedObject serializedAsset = new(asset);
        SerializedProperty msaa = serializedAsset.FindProperty("m_MSAA");
        SerializedProperty renderScale = serializedAsset.FindProperty("m_RenderScale");
        SerializedProperty upscalingFilter = serializedAsset.FindProperty("m_UpscalingFilter");
        SerializedProperty fsrSharpness = serializedAsset.FindProperty("m_FsrSharpness");
        SerializedProperty shadowDistance = serializedAsset.FindProperty("m_ShadowDistance");

        Assert.NotNull(msaa, "Mobile URP asset is missing serialized m_MSAA.");
        Assert.NotNull(renderScale, "Mobile URP asset is missing serialized m_RenderScale.");
        Assert.NotNull(upscalingFilter, "Mobile URP asset is missing serialized m_UpscalingFilter.");
        Assert.NotNull(fsrSharpness, "Mobile URP asset is missing serialized m_FsrSharpness.");
        Assert.NotNull(shadowDistance, "Mobile URP asset is missing serialized m_ShadowDistance.");
        Assert.AreEqual(BalancedMobileMsaa, msaa.intValue, "Android/mobile pipeline should avoid MSAA bandwidth cost and rely on FSR plus camera AA for 60 FPS.");
        Assert.That(renderScale.floatValue, Is.EqualTo(BalancedMobileRenderScale).Within(0.001f), "Android/mobile pipeline should use FSR-backed 0.50 render scale for 60 FPS.");
        Assert.AreEqual(BalancedMobileUpscalingFilter, upscalingFilter.intValue, "Android/mobile pipeline should use FSR upscaling to preserve edge quality at the balanced render scale.");
        Assert.That(fsrSharpness.floatValue, Is.EqualTo(BalancedMobileFsrSharpness).Within(0.001f), "Android/mobile FSR sharpness should avoid ringing and jagged terrain edges when the match camera zooms out.");
        Assert.That(shadowDistance.floatValue, Is.EqualTo(BalancedMobileShadowDistance).Within(0.001f), "Android/mobile shadows should stay bounded for 60 FPS.");
    }

    [Test]
    public static void VisualQualityProfileUsesBalancedAndroidMatchRendering()
    {
        VisualQualityProfileAsset profile =
            AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(VisualQualityProfilePath);
        Assert.NotNull(profile, $"Missing visual quality profile at {VisualQualityProfilePath}.");

        Assert.GreaterOrEqual(
            profile.LowRenderScaleOverride,
            MinimumLowRenderScale,
            "Low mode can be cheaper, but it must stay above visibly broken mobile undersampling.");
        Assert.That(
            profile.MediumRenderScaleOverride,
            Is.EqualTo(BalancedMobileRenderScale).Within(0.001f),
            "Match High mode uses the balanced mobile render scale on Android.");
        Assert.GreaterOrEqual(
            profile.CameraRenderScaleOverride,
            BalancedMobileRenderScale,
            "Ultra camera render scale must not undersample the match world.");
    }

    [Test]
    public static void MobileQualityTierUsesBalancedMsaaAndShadows()
    {
        string qualitySettingsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/QualitySettings.asset"));
        string qualitySettings = File.ReadAllText(qualitySettingsPath);
        int mobileIndex = qualitySettings.IndexOf("name: Mobile", StringComparison.Ordinal);
        Assert.GreaterOrEqual(mobileIndex, 0, "QualitySettings.asset must contain a Mobile quality tier.");

        int nextTierIndex = qualitySettings.IndexOf("\n  - serializedVersion:", mobileIndex + 1, StringComparison.Ordinal);
        string mobileBlock = nextTierIndex >= 0
            ? qualitySettings.Substring(mobileIndex, nextTierIndex - mobileIndex)
            : qualitySettings.Substring(mobileIndex);

        StringAssert.Contains("antiAliasing: 0", mobileBlock, "Android Mobile quality tier should avoid MSAA bandwidth cost for 60 FPS.");
        StringAssert.Contains("shadowDistance: 16", mobileBlock, "Android Mobile quality tier should cap shadow distance for 60 FPS.");
    }
}
#endif
