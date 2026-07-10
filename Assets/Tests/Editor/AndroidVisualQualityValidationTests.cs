#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using SettingsService = Game.UI.Runtime.SettingsService;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class AndroidVisualQualityValidationTests
{
    private const string MobileRenderPipelinePath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    private const string VisualQualityProfilePath = "Assets/Game/Rendering/VisualQualityConfig.asset";
    private const float MinimumLowRenderScale = 0.50f;
    private const float BalancedMobileRenderScale = 0.50f;
    private const int BalancedMobileMsaa = 1;
    private const int BalancedMobileUpscalingFilter = 3;
    private const float BalancedMobileFsrSharpness = 0.72f;
    private const float BalancedMobileShadowDistance = 16f;
    private const int DisabledMobileLightingFeature = 0;

    public static void RunFocusedValidation()
    {
        try
        {
            int passed = 0;
            RunCase(() => MobileRenderPipelineUsesBalancedScaleAndMsaa(), ref passed);
            RunCase(() => MobileRendererUsesForwardPlusForEntitiesGraphics(), ref passed);
            RunCase(() => VisualQualityProfileUsesBalancedAndroidMatchRendering(), ref passed);
            RunCase(() => HighModeKeepsCameraPostProcessingDisabled(), ref passed);
            RunCase(() => MobileQualityTierUsesBalancedMsaaAndShadows(), ref passed);
            RunCase(() => AndroidFrameRatePolicyClampsOneTwentyToSixty(), ref passed);
            RunCase(() => AndroidFrameRatePolicyPreservesThirtyAndSixty(), ref passed);
            RunCase(() => AndroidFrameRatePersistenceMigratesOneTwentyToSixty(), ref passed);
            RunCase(() => MatchCompositionRoutesVisualQualityChangesAndUnsubscribes(), ref passed);
            RunCase(() => VisualQualityRoutingRemainsEventDriven(), ref passed);
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
        SerializedProperty lightCookies = serializedAsset.FindProperty("m_SupportsLightCookies");
        SerializedProperty lightLayers = serializedAsset.FindProperty("m_SupportsLightLayers");

        Assert.NotNull(msaa, "Mobile URP asset is missing serialized m_MSAA.");
        Assert.NotNull(renderScale, "Mobile URP asset is missing serialized m_RenderScale.");
        Assert.NotNull(upscalingFilter, "Mobile URP asset is missing serialized m_UpscalingFilter.");
        Assert.NotNull(fsrSharpness, "Mobile URP asset is missing serialized m_FsrSharpness.");
        Assert.NotNull(shadowDistance, "Mobile URP asset is missing serialized m_ShadowDistance.");
        Assert.NotNull(lightCookies, "Mobile URP asset is missing serialized m_SupportsLightCookies.");
        Assert.NotNull(lightLayers, "Mobile URP asset is missing serialized m_SupportsLightLayers.");
        Assert.AreEqual(BalancedMobileMsaa, msaa.intValue, "Android/mobile pipeline should avoid MSAA bandwidth cost and rely on FSR plus camera AA for 60 FPS.");
        Assert.That(renderScale.floatValue, Is.EqualTo(BalancedMobileRenderScale).Within(0.001f), "Android/mobile pipeline should use FSR-backed 0.50 render scale for 60 FPS.");
        Assert.AreEqual(BalancedMobileUpscalingFilter, upscalingFilter.intValue, "Android/mobile pipeline should use FSR upscaling to preserve edge quality at the balanced render scale.");
        Assert.That(fsrSharpness.floatValue, Is.EqualTo(BalancedMobileFsrSharpness).Within(0.001f), "Android/mobile FSR sharpness should avoid ringing and jagged terrain edges when the match camera zooms out.");
        Assert.That(shadowDistance.floatValue, Is.EqualTo(BalancedMobileShadowDistance).Within(0.001f), "Android/mobile shadows should stay bounded for 60 FPS.");
        Assert.AreEqual(DisabledMobileLightingFeature, lightCookies.intValue, "Android/mobile pipeline should not carry light-cookie support when additional lights are disabled.");
        Assert.AreEqual(DisabledMobileLightingFeature, lightLayers.intValue, "Android/mobile pipeline should not carry light-layer support when additional lights are disabled.");
    }

    [Test]
    public static void MobileRendererUsesForwardPlusForEntitiesGraphics()
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MobileRendererPath);
        Assert.NotNull(asset, $"Missing mobile renderer asset at {MobileRendererPath}.");

        SerializedObject serializedAsset = new(asset);
        SerializedProperty renderingMode = serializedAsset.FindProperty("m_RenderingMode");
        Assert.NotNull(renderingMode, "Mobile renderer asset is missing serialized m_RenderingMode.");
        Assert.AreEqual(
            2,
            renderingMode.intValue,
            "Android/mobile renderer should use URP Forward+ so Entities Graphics remains on its supported compatibility path.");
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
            1,
            cameraAntialiasingMode.intValue,
            "Match High mode should not force SMAA because it requires the costly camera post-processing path on Android.");
        Assert.GreaterOrEqual(
            profile.CameraRenderScaleOverride,
            BalancedMobileRenderScale,
            "Ultra camera render scale must not undersample the match world.");
    }

    [Test]
    public static void HighModeKeepsCameraPostProcessingDisabled()
    {
        VisualQualityProfileAsset profile = ScriptableObject.CreateInstance<VisualQualityProfileAsset>();
        GameObject cameraObject = new("AndroidVisualQualityCamera", typeof(Camera));
        World world = null;

        try
        {
            SerializedObject serializedProfile = new(profile);
            serializedProfile.FindProperty("runtimeMode").intValue = (int)VisualQualityRuntimeMode.High;
            serializedProfile.FindProperty("cameraAntialiasingMode").intValue = 1;
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

            Assert.False(
                (bool)renderPostProcessing.GetValue(cameraData),
                "Match High mode must keep camera post processing disabled on Android to avoid color grading and FPS regressions.");
            Assert.AreEqual(
                1,
                Convert.ToInt32(antialiasing.GetValue(cameraData)),
                "Match High mode may retain the configured camera AA enum, but it must not force the post-processing path.");
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

    [Test]
    public static void AndroidFrameRatePolicyClampsOneTwentyToSixty()
    {
        Assert.AreEqual(
            UIFrameRateMode.Sixty,
            SettingsService.DefaultsForPlatform(isAndroid: true).Graphics.FrameRateMode);
        Assert.AreEqual(
            UIFrameRateMode.Sixty,
            SettingsService.NormalizeFrameRateMode(UIFrameRateMode.OneTwenty, isAndroid: true));
        Assert.AreEqual(
            60,
            SettingsService.ResolveTargetFrameRate(UIFrameRateMode.OneTwenty, isAndroid: true));

        int previousTargetFrameRate = Application.targetFrameRate;
        int previousQualityLevel = QualitySettings.GetQualityLevel();
        float previousListenerVolume = AudioListener.volume;
        UISettingsModel applied = default;
        void CaptureApplied(UISettingsModel model) => applied = model;
        SettingsService.RuntimeApplied += CaptureApplied;
        try
        {
            UISettingsModel requested = SettingsService.DefaultsForPlatform(isAndroid: false);
            requested.Graphics.FrameRateMode = UIFrameRateMode.OneTwenty;
            SettingsService.ApplyRuntimeForPlatform(requested, isAndroid: true);

            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(UIFrameRateMode.Sixty, applied.Graphics.FrameRateMode);
        }
        finally
        {
            SettingsService.RuntimeApplied -= CaptureApplied;
            Application.targetFrameRate = previousTargetFrameRate;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(previousQualityLevel, true);
            AudioListener.volume = previousListenerVolume;
        }
    }

    [Test]
    public static void AndroidFrameRatePolicyPreservesThirtyAndSixty()
    {
        Assert.AreEqual(
            UIFrameRateMode.Thirty,
            SettingsService.NormalizeFrameRateMode(UIFrameRateMode.Thirty, isAndroid: true));
        Assert.AreEqual(
            UIFrameRateMode.Sixty,
            SettingsService.NormalizeFrameRateMode(UIFrameRateMode.Sixty, isAndroid: true));
        Assert.AreEqual(30, SettingsService.ResolveTargetFrameRate(UIFrameRateMode.Thirty, isAndroid: true));
        Assert.AreEqual(60, SettingsService.ResolveTargetFrameRate(UIFrameRateMode.Sixty, isAndroid: true));
    }

    [Test]
    public static void AndroidFrameRatePersistenceMigratesOneTwentyToSixty()
    {
        UISettingsModel previous = SettingsService.Load();
        try
        {
            UISettingsModel legacy = SettingsService.DefaultsForPlatform(isAndroid: false);
            legacy.Graphics.FrameRateMode = UIFrameRateMode.OneTwenty;
            SettingsService.SaveForPlatform(legacy, isAndroid: false);

            Assert.AreEqual(
                UIFrameRateMode.Sixty,
                SettingsService.LoadForPlatform(isAndroid: true).Graphics.FrameRateMode,
                "A legacy saved 120 FPS value must load as 60 FPS on Android.");

            SettingsService.SaveForPlatform(legacy, isAndroid: true);
            Assert.AreEqual(
                UIFrameRateMode.Sixty,
                SettingsService.LoadForPlatform(isAndroid: false).Graphics.FrameRateMode,
                "Saving settings on Android must persist the clamped 60 FPS value.");
        }
        finally
        {
            SettingsService.Save(previous);
        }
    }

    [Test]
    public static void MatchCompositionRoutesVisualQualityChangesAndUnsubscribes()
    {
        UISettingsModel previousSettings = SettingsService.Load();
        int previousTargetFrameRate = Application.targetFrameRate;
        int previousQualityLevel = QualitySettings.GetQualityLevel();
        float previousListenerVolume = AudioListener.volume;
        VisualQualityProfileAsset profile = ScriptableObject.CreateInstance<VisualQualityProfileAsset>();
        World world = new("MatchSettingsRoutingValidation");
        MatchBootstrapCompositionSystemHelper composition = new();

        try
        {
            VisualQualitySettingsSystem visualQuality =
                world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>();
            MethodInfo initialize = typeof(VisualQualitySettingsSystem).GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(initialize, "VisualQualitySettingsSystem is missing Initialize.");
            initialize.Invoke(visualQuality, new object[] { profile, null, null, null });
            composition.Initialize(null);
            composition.BindVisualQualitySettingsSystem(visualQuality);

            UISettingsModel low = SettingsService.DefaultsForPlatform(isAndroid: true);
            low.Graphics.Quality = UIGraphicsQuality.Low;
            SettingsService.PublishRuntimeSettings(low);

            Assert.IsTrue(composition.IsRuntimeSettingsChangeSubscribed);
            Assert.AreEqual(VisualQualityRuntimeMode.Low, visualQuality.AppliedMode);

            composition.Shutdown();
            UISettingsModel ultra = low;
            ultra.Graphics.Quality = UIGraphicsQuality.Ultra;
            SettingsService.PublishRuntimeSettings(ultra);

            Assert.IsFalse(composition.IsRuntimeSettingsChangeSubscribed);
            Assert.AreEqual(
                VisualQualityRuntimeMode.Low,
                visualQuality.AppliedMode,
                "Visual quality must stop receiving settings events after composition shutdown.");
            Assert.AreEqual(VisualQualityRuntimeMode.Medium,
                MatchBootstrapCompositionSystemHelper.ToVisualQualityRuntimeMode(UIGraphicsQuality.Balanced));
            Assert.AreEqual(VisualQualityRuntimeMode.High,
                MatchBootstrapCompositionSystemHelper.ToVisualQualityRuntimeMode(UIGraphicsQuality.High));
        }
        finally
        {
            composition.Shutdown();
            world.Dispose();
            UnityEngine.Object.DestroyImmediate(profile);
            SettingsService.ApplyRuntime(previousSettings);
            Application.targetFrameRate = previousTargetFrameRate;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(previousQualityLevel, true);
            AudioListener.volume = previousListenerVolume;
        }
    }

    [Test]
    public static void VisualQualityRoutingRemainsEventDriven()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string compositionSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs"));
        string visualQualitySource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Systems/VisualQualitySettingsSystem.cs"));

        StringAssert.Contains(
            "SettingsService.RuntimeApplied += OnRuntimeSettingsApplied",
            compositionSource);
        StringAssert.Contains(
            "SettingsService.RuntimeApplied -= OnRuntimeSettingsApplied",
            compositionSource);
        StringAssert.DoesNotContain(
            "_visualQualitySettingsSystem?.Update()",
            compositionSource,
            "Match composition must not poll visual quality every frame.");
        StringAssert.Contains(
            "public void ApplyRuntimeMode(VisualQualityRuntimeMode mode)",
            visualQualitySource);
    }
}
#endif
