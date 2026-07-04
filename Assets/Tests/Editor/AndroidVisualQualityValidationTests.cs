#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
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
    private const float BalancedMobileFsrSharpness = 0.45f;
    private const float BalancedMobileShadowDistance = 16f;

    public static void RunFocusedValidation()
    {
        try
        {
            int passed = 0;
            RunCase(() => MobileRenderPipelineUsesBalancedScaleAndMsaa(), ref passed);
            RunCase(() => VisualQualityProfileUsesBalancedAndroidMatchRendering(), ref passed);
            RunCase(() => HighModeEnablesSmaaPostProcess(), ref passed);
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

        SerializedObject serializedProfile = new(profile);
        SerializedProperty cameraAntialiasingMode = serializedProfile.FindProperty("cameraAntialiasingMode");
        Assert.NotNull(cameraAntialiasingMode, "Visual quality profile is missing serialized cameraAntialiasingMode.");

        Assert.GreaterOrEqual(
            profile.LowRenderScaleOverride,
            MinimumLowRenderScale,
            "Low mode can be cheaper, but it must stay above visibly broken mobile undersampling.");
        Assert.That(
            profile.MediumRenderScaleOverride,
            Is.EqualTo(BalancedMobileRenderScale).Within(0.001f),
            "Match High mode uses the balanced mobile render scale on Android.");
        Assert.AreEqual(
            2,
            cameraAntialiasingMode.intValue,
            "Match High mode should use SMAA to reduce jagged world edges without increasing Android render scale.");
        Assert.GreaterOrEqual(
            profile.CameraRenderScaleOverride,
            BalancedMobileRenderScale,
            "Ultra camera render scale must not undersample the match world.");
    }

    [Test]
    public static void HighModeEnablesSmaaPostProcess()
    {
        VisualQualityProfileAsset profile = ScriptableObject.CreateInstance<VisualQualityProfileAsset>();
        GameObject cameraObject = new("AndroidVisualQualityCamera", typeof(Camera));
        World world = null;

        try
        {
            SerializedObject serializedProfile = new(profile);
            serializedProfile.FindProperty("runtimeMode").intValue = (int)VisualQualityRuntimeMode.High;
            serializedProfile.FindProperty("cameraAntialiasingMode").intValue = 2;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            Type cameraDataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            Assert.NotNull(cameraDataType, "URP camera data type must be available for Android visual quality validation.");

            Component cameraData = cameraObject.AddComponent(cameraDataType);
            PropertyInfo renderPostProcessing = cameraDataType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo antialiasing = cameraDataType.GetProperty("antialiasing", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(renderPostProcessing, "URP camera data is missing renderPostProcessing.");
            Assert.NotNull(antialiasing, "URP camera data is missing antialiasing.");

            renderPostProcessing.SetValue(cameraData, false);
            antialiasing.SetValue(cameraData, Enum.ToObject(antialiasing.PropertyType, 0));

            world = new World("AndroidVisualQualityValidationWorld");
            VisualQualitySettingsSystem system = world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>();
            MethodInfo initialize = typeof(VisualQualitySettingsSystem).GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(initialize, "VisualQualitySettingsSystem is missing Initialize.");
            initialize.Invoke(system, new object[] { profile, cameraObject.GetComponent<Camera>(), null, null });

            Assert.True(
                (bool)renderPostProcessing.GetValue(cameraData),
                "Match High mode must enable camera post processing so SMAA can run.");
            Assert.AreEqual(
                2,
                Convert.ToInt32(antialiasing.GetValue(cameraData)),
                "Match High mode must apply SMAA to reduce jagged Android edges.");
        }
        finally
        {
            world?.Dispose();
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(profile);
        }
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
