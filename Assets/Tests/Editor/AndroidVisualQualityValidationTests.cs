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
    private const float MinimumLowRenderScale = 0.72f;
    private const float BalancedMobileRenderScale = 0.9f;
    private const int BalancedMobileMsaa = 2;
    private const float BalancedMobileShadowDistance = 22f;

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
        SerializedProperty shadowDistance = serializedAsset.FindProperty("m_ShadowDistance");

        Assert.NotNull(msaa, "Mobile URP asset is missing serialized m_MSAA.");
        Assert.NotNull(renderScale, "Mobile URP asset is missing serialized m_RenderScale.");
        Assert.NotNull(shadowDistance, "Mobile URP asset is missing serialized m_ShadowDistance.");
        Assert.AreEqual(BalancedMobileMsaa, msaa.intValue, "Android/mobile pipeline should use balanced 2x MSAA for 60 FPS.");
        Assert.That(renderScale.floatValue, Is.EqualTo(BalancedMobileRenderScale).Within(0.001f), "Android/mobile pipeline should use balanced 0.90 render scale.");
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

        StringAssert.Contains("antiAliasing: 2", mobileBlock, "Android Mobile quality tier should use balanced 2x MSAA.");
        StringAssert.Contains("shadowDistance: 22", mobileBlock, "Android Mobile quality tier should cap shadow distance for 60 FPS.");
    }
}
#endif
