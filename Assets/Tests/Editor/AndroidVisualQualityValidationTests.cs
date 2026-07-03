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
    private const float MinimumMediumRenderScale = 0.99f;

    public static void RunFocusedValidation()
    {
        try
        {
            int passed = 0;
            RunCase(() => MobileRenderPipelineUsesNativeScaleAndMsaa(), ref passed);
            RunCase(() => VisualQualityProfileDoesNotUndersampleAndroidMatchRendering(), ref passed);
            RunCase(() => MobileQualityTierKeepsMsaaEnabled(), ref passed);
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
    public static void MobileRenderPipelineUsesNativeScaleAndMsaa()
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MobileRenderPipelinePath);
        Assert.NotNull(asset, $"Missing mobile render pipeline asset at {MobileRenderPipelinePath}.");

        SerializedObject serializedAsset = new(asset);
        SerializedProperty msaa = serializedAsset.FindProperty("m_MSAA");
        SerializedProperty renderScale = serializedAsset.FindProperty("m_RenderScale");

        Assert.NotNull(msaa, "Mobile URP asset is missing serialized m_MSAA.");
        Assert.NotNull(renderScale, "Mobile URP asset is missing serialized m_RenderScale.");
        Assert.GreaterOrEqual(msaa.intValue, 4, "Android/mobile pipeline must keep geometry edges anti-aliased.");
        Assert.GreaterOrEqual(renderScale.floatValue, 0.99f, "Android/mobile pipeline must not upscale an undersampled world render.");
    }

    [Test]
    public static void VisualQualityProfileDoesNotUndersampleAndroidMatchRendering()
    {
        VisualQualityProfileAsset profile =
            AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(VisualQualityProfilePath);
        Assert.NotNull(profile, $"Missing visual quality profile at {VisualQualityProfilePath}.");

        Assert.GreaterOrEqual(
            profile.LowRenderScaleOverride,
            MinimumLowRenderScale,
            "Low mode can be cheaper, but it must stay above visibly broken mobile undersampling.");
        Assert.GreaterOrEqual(
            profile.MediumRenderScaleOverride,
            MinimumMediumRenderScale,
            "Match High mode uses the mobile pipeline and must render at native scale on Android.");
        Assert.GreaterOrEqual(
            profile.CameraRenderScaleOverride,
            MinimumMediumRenderScale,
            "Ultra camera render scale must not undersample the match world.");
    }

    [Test]
    public static void MobileQualityTierKeepsMsaaEnabled()
    {
        string qualitySettingsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/QualitySettings.asset"));
        string qualitySettings = File.ReadAllText(qualitySettingsPath);
        int mobileIndex = qualitySettings.IndexOf("name: Mobile", StringComparison.Ordinal);
        Assert.GreaterOrEqual(mobileIndex, 0, "QualitySettings.asset must contain a Mobile quality tier.");

        int nextTierIndex = qualitySettings.IndexOf("\n  - serializedVersion:", mobileIndex + 1, StringComparison.Ordinal);
        string mobileBlock = nextTierIndex >= 0
            ? qualitySettings.Substring(mobileIndex, nextTierIndex - mobileIndex)
            : qualitySettings.Substring(mobileIndex);

        StringAssert.Contains("antiAliasing: 4", mobileBlock, "Android Mobile quality tier must keep 4x MSAA enabled.");
    }
}
#endif
