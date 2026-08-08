#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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
            RunCase(() => MobileRenderPipelineUsesGpuInstancedDrawingDefaults(), ref passed);
            RunCase(() => MobileRendererUsesForwardPlusForEntitiesGraphics(), ref passed);
            RunCase(() => GraphicsSettingsRetainsBatchRendererGroupShaderVariants(), ref passed);
            RunCase(() => UniversalRenderPipelineGlobalSettingsRetainRuntimeResources(), ref passed);
            RunCase(() => AndroidBuildDisablesStaticBatchingForGpuResidentDrawer(), ref passed);
            RunCase(() => AndroidBuildEnablesOptimizedFramePacing(), ref passed);
            RunCase(() => VisualQualityProfileUsesBalancedAndroidMatchRendering(), ref passed);
            RunCase(() => HighModeKeepsCameraPostProcessingDisabled(), ref passed);
            RunCase(() => MobileQualityTierUsesBalancedMsaaAndShadows(), ref passed);
            RunCase(() => AndroidFrameRatePolicyClampsOneTwentyToSixty(), ref passed);
            RunCase(() => AndroidFrameRatePolicyPreservesThirtyAndSixty(), ref passed);
            RunCase(() => AndroidFrameRatePersistenceMigratesOneTwentyToSixty(), ref passed);
            RunCase(() => QualitySelectionPreservesMobileIntentAcrossPlatformTierLists(), ref passed);
            RunCase(() => MatchCompositionRoutesVisualQualityChangesAndUnsubscribes(), ref passed);
            RunCase(() => VisualQualityRoutingRemainsEventDriven(), ref passed);
            RunCase(() => DayNightRemainsAuthoritativeAcrossQualityChanges(), ref passed);
            RunCase(() => RuntimeQualityTierMappingsAreComplete(), ref passed);
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
    public static void MobileRenderPipelineUsesGpuInstancedDrawingDefaults()
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MobileRenderPipelinePath);
        Assert.NotNull(asset, $"Missing mobile render pipeline asset at {MobileRenderPipelinePath}.");

        SerializedObject serializedAsset = new(asset);
        SerializedProperty mode = serializedAsset.FindProperty("m_GPUResidentDrawerMode");
        SerializedProperty smallMeshPercentage = serializedAsset.FindProperty("m_SmallMeshScreenPercentage");
        SerializedProperty occlusion = serializedAsset.FindProperty(
            "m_GPUResidentDrawerEnableOcclusionCullingInCameras");
        Assert.NotNull(mode, "Mobile URP asset is missing serialized m_GPUResidentDrawerMode.");
        Assert.NotNull(smallMeshPercentage, "Mobile URP asset is missing serialized m_SmallMeshScreenPercentage.");
        Assert.NotNull(occlusion, "Mobile URP asset is missing serialized GPU Resident Drawer occlusion setting.");
        Assert.AreEqual(1, mode.intValue, "Android/mobile must use GPU Resident Drawer InstancedDrawing.");
        Assert.That(
            smallMeshPercentage.floatValue,
            Is.Zero.Within(0.001f),
            "Initial acceptance must not add small-mesh culling before visual comparison.");
        Assert.False(
            occlusion.boolValue,
            "Initial acceptance must retain frustum-only culling until device visual evidence approves occlusion.");
    }

    [Test]
    public static void GraphicsSettingsRetainsBatchRendererGroupShaderVariants()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/GraphicsSettings.asset"));
        string settings = File.ReadAllText(path);
        StringAssert.Contains(
            "m_BrgStripping: 2",
            settings,
            "GPU Resident Drawer requires BatchRendererGroup shader stripping mode Keep All.");
        StringAssert.Contains(
            "m_InstancingStripping: 0",
            settings,
            "Standard instancing variants must remain available for renderers outside GPU Resident Drawer.");
    }

    [Test]
    public static void UniversalRenderPipelineGlobalSettingsRetainRuntimeResources()
    {
        string path = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "../Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"));
        string settings = File.ReadAllText(path);

        StringAssert.DoesNotContain(
            "m_RuntimeSettings:\n      m_List: []",
            settings,
            "URP runtime resources must not be emptied during settings migration.");
        StringAssert.Contains(
            "class: GPUResidentDrawerResources",
            settings,
            "GPU Resident Drawer runtime resources must remain registered in URP global settings.");

        Match runtimeBlock = Regex.Match(
            settings,
            @"m_RuntimeSettings:\s*\r?\n(?<body>[\s\S]*?)\r?\n  m_AssetVersion:",
            RegexOptions.CultureInvariant);
        Assert.True(
            runtimeBlock.Success,
            "URP global settings must retain a readable m_RuntimeSettings block.");

        HashSet<string> runtimeRids = CollectRids(runtimeBlock.Groups["body"].Value);
        Assert.That(
            runtimeRids.Count,
            Is.GreaterThan(0),
            "URP runtime settings must contain at least one managed-reference RID.");

        Match referencesBlock = Regex.Match(
            settings,
            @"\r?\n  references:\s*\r?\n(?<body>[\s\S]*)$",
            RegexOptions.CultureInvariant);
        Assert.True(
            referencesBlock.Success,
            "URP global settings must retain the managed-reference definitions block.");
        HashSet<string> definedRids = CollectRids(referencesBlock.Groups["body"].Value);
        foreach (string runtimeRid in runtimeRids)
        {
            Assert.True(
                definedRids.Contains(runtimeRid),
                $"URP runtime settings RID {runtimeRid} has no managed-reference definition.");
        }

        Match gpuResidentDrawer = Regex.Match(
            referencesBlock.Groups["body"].Value,
            @"-\s+rid:\s+(?<rid>\d+)\s*\r?\n\s+type:\s+\{class:\s+GPUResidentDrawerResources,",
            RegexOptions.CultureInvariant);
        Assert.True(
            gpuResidentDrawer.Success,
            "URP global settings must define GPUResidentDrawerResources.");
        Assert.True(
            runtimeRids.Contains(gpuResidentDrawer.Groups["rid"].Value),
            "GPUResidentDrawerResources must be included in m_RuntimeSettings, not merely serialized as an unused reference.");
    }

    private static HashSet<string> CollectRids(string yamlBlock)
    {
        var rids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(
                     yamlBlock,
                     @"^\s*-\s+rid:\s+(?<rid>\d+)\s*$",
                     RegexOptions.CultureInvariant | RegexOptions.Multiline))
        {
            Assert.True(
                rids.Add(match.Groups["rid"].Value),
                $"Managed-reference RID {match.Groups["rid"].Value} is duplicated within its YAML list.");
        }

        return rids;
    }

    [Test]
    public static void AndroidBuildDisablesStaticBatchingForGpuResidentDrawer()
    {
        Assert.False(
            PlayerSettings.GetStaticBatchingForPlatform(BuildTarget.Android),
            "Android static batching must remain disabled when GPU Resident Drawer owns instanced submission.");
    }

    [Test]
    public static void AndroidBuildEnablesOptimizedFramePacing()
    {
        Assert.True(
            PlayerSettings.Android.optimizedFramePacing,
            "Android must use Unity optimized frame pacing so the 60 FPS target is evenly distributed on high-refresh displays.");
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
        StringAssert.Contains("globalTextureMipmapLimit: 1", mobileBlock, "Android Mobile should omit oversized top world mips while UI and exempt textures preserve native resolution.");
        StringAssert.Contains("streamingMipmapsActive: 1", mobileBlock, "Android Mobile should stream only textures explicitly marked as eligible.");
        StringAssert.Contains("streamingMipmapsMemoryBudget: 256", mobileBlock, "Android Mobile texture streaming must stay inside its bounded memory budget.");
        StringAssert.Contains("asyncUploadTimeSlice: 4", mobileBlock, "Android Mobile loading should provide enough upload time to avoid multi-minute scene stalls.");
        StringAssert.Contains("asyncUploadBufferSize: 32", mobileBlock, "Android Mobile must fit the largest accepted packed texture in the persistent upload buffer.");
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
        int previousVSyncCount = QualitySettings.vSyncCount;
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
            Assert.AreEqual(0, QualitySettings.vSyncCount);
            Assert.AreEqual(UIFrameRateMode.Sixty, applied.Graphics.FrameRateMode);
        }
        finally
        {
            SettingsService.RuntimeApplied -= CaptureApplied;
            Application.targetFrameRate = previousTargetFrameRate;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(previousQualityLevel, true);
            QualitySettings.vSyncCount = previousVSyncCount;
            AudioListener.volume = previousListenerVolume;
        }
    }

    [Test]
    public static void EditorFrameRatePolicyRemainsUncappedAfterQualityApplication()
    {
        int previousTargetFrameRate = Application.targetFrameRate;
        int previousVSyncCount = QualitySettings.vSyncCount;
        int previousQualityLevel = QualitySettings.GetQualityLevel();
        float previousListenerVolume = AudioListener.volume;
        try
        {
            UISettingsModel requested = SettingsService.DefaultsForPlatform(isAndroid: false);
            requested.Graphics.FrameRateMode = UIFrameRateMode.Thirty;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 2;

            SettingsService.ApplyRuntimeForEnvironment(requested, isAndroid: false, isEditor: true);

            Assert.AreEqual(-1, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount);
        }
        finally
        {
            Application.targetFrameRate = previousTargetFrameRate;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(previousQualityLevel, true);
            QualitySettings.vSyncCount = previousVSyncCount;
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
    public static void QualitySelectionPreservesMobileIntentAcrossPlatformTierLists()
    {
        Assert.AreEqual(
            0,
            SettingsService.ResolveUnityQualityIndex(UIGraphicsQuality.High, new[] { "PC", "Ultra" }),
            "High must fall back to PC when Standalone excludes the Mobile tier, not select Ultra.");
        Assert.AreEqual(
            1,
            SettingsService.ResolveUnityQualityIndex(UIGraphicsQuality.High, new[] { "Low", "Mobile", "Ultra" }),
            "High must select Mobile when that tier is available.");
        Assert.AreEqual(
            1,
            SettingsService.ResolveUnityQualityIndex(UIGraphicsQuality.Ultra, new[] { "Low", "Mobile" }),
            "Ultra must fall back to the best available Android tier.");
        Assert.AreEqual(
            0,
            SettingsService.ResolveUnityQualityIndex(UIGraphicsQuality.Balanced, Array.Empty<string>()),
            "An empty quality list must return the only safe index.");
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
            "public bool ApplyRuntimeMode(VisualQualityRuntimeMode mode)",
            visualQualitySource);
        StringAssert.DoesNotContain("ApplyModeDynamicSettings", visualQualitySource);
        StringAssert.DoesNotContain("ApplyMobileDynamicSettings", visualQualitySource);
        StringAssert.DoesNotContain("ApplyUltraDynamicSettings", visualQualitySource);
    }

    [Test]
    public static void DayNightRemainsAuthoritativeAcrossQualityChanges()
    {
        UISettingsModel previousSettings = SettingsService.Load();
        Material previousSkybox = RenderSettings.skybox;
        VisualQualityProfileAsset visualProfile = ScriptableObject.CreateInstance<VisualQualityProfileAsset>();
        ScriptableObject originalVolumeProfile = null;
        ScriptableObject ultraVolumeProfile = null;
        GameObject lightObject = new("DayNightAuthorityLight", typeof(Light));
        GameObject volumeObject = new("DayNightAuthorityVolume");
        World world = new("DayNightAuthorityValidation");
        MatchBootstrapCompositionSystemHelper composition = new();
        Material testSkybox = null;

        try
        {
            Type volumeType = Type.GetType(
                "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            Type volumeProfileType = Type.GetType(
                "UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime");
            Assert.NotNull(volumeType, "Volume type must be available for Day/Night authority validation.");
            Assert.NotNull(volumeProfileType, "VolumeProfile type must be available for Day/Night authority validation.");

            Component volume = volumeObject.AddComponent(volumeType);
            originalVolumeProfile = ScriptableObject.CreateInstance(volumeProfileType);
            ultraVolumeProfile = ScriptableObject.CreateInstance(volumeProfileType);
            WriteMember(volume, "sharedProfile", originalVolumeProfile);

            SerializedObject serializedVisualProfile = new(visualProfile);
            serializedVisualProfile.FindProperty("runtimeMode").intValue = (int)VisualQualityRuntimeMode.Ultra;
            serializedVisualProfile.FindProperty("globalVolumeProfile").objectReferenceValue = ultraVolumeProfile;
            serializedVisualProfile.ApplyModifiedPropertiesWithoutUndo();

            Shader skyboxShader = Shader.Find("Skybox/Procedural") ?? Shader.Find("Unlit/Color");
            Assert.NotNull(skyboxShader, "A shader is required to verify Day/Night skybox ownership.");
            testSkybox = new Material(skyboxShader);
            RenderSettings.skybox = testSkybox;

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            VisualQualitySettingsSystem visualQuality =
                world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>();
            DayNightSystem dayNight = world.GetOrCreateSystemManaged<DayNightSystem>();
            MethodInfo initializeVisualQuality = typeof(VisualQualitySettingsSystem).GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo initializeDayNight = typeof(DayNightSystem).GetMethod(
                "Init",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo applyDayNight = typeof(DayNightSystem).GetMethod(
                "ApplyVisualState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo currentHour = typeof(DayNightSystem).GetField(
                "_currentHour",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(initializeVisualQuality);
            Assert.NotNull(initializeDayNight);
            Assert.NotNull(applyDayNight);
            Assert.NotNull(currentHour);

            initializeVisualQuality.Invoke(visualQuality, new object[] { visualProfile, null, light, volume });
            composition.Initialize(null);
            composition.BindVisualQualitySettingsSystem(visualQuality);

            UISettingsModel ultra = SettingsService.DefaultsForPlatform(isAndroid: true);
            ultra.Graphics.Quality = UIGraphicsQuality.Ultra;
            SettingsService.PublishRuntimeSettings(ultra);

            initializeDayNight.Invoke(dayNight, new object[] { null, light, volume });
            composition.BindDayNightSystem(dayNight);
            currentHour.SetValue(dayNight, 22f);
            applyDayNight.Invoke(dayNight, null);

            Color sunColor = light.color;
            float sunIntensity = light.intensity;
            float shadowStrength = light.shadowStrength;
            Quaternion sunRotation = light.transform.rotation;
            object ambientMode = RenderSettings.ambientMode;
            Color ambientSky = RenderSettings.ambientSkyColor;
            Color ambientEquator = RenderSettings.ambientEquatorColor;
            Color ambientGround = RenderSettings.ambientGroundColor;
            float ambientIntensity = RenderSettings.ambientIntensity;
            float reflectionIntensity = RenderSettings.reflectionIntensity;
            bool fog = RenderSettings.fog;
            object fogMode = RenderSettings.fogMode;
            Color fogColor = RenderSettings.fogColor;
            float fogDensity = RenderSettings.fogDensity;
            Material runtimeSkybox = RenderSettings.skybox;
            float volumeWeight = Convert.ToSingle(ReadMember(volume, "weight"));
            float postExposure = ReadVolumeFloat(dayNight, "_colorAdjustments", "postExposure");
            float temperature = ReadVolumeFloat(dayNight, "_whiteBalance", "temperature");
            float bloomIntensity = ReadVolumeFloat(dayNight, "_bloom", "intensity");

            UISettingsModel high = ultra;
            high.Graphics.Quality = UIGraphicsQuality.High;
            SettingsService.PublishRuntimeSettings(high);

            Assert.AreEqual(VisualQualityRuntimeMode.High, visualQuality.AppliedMode);
            Assert.That(
                dayNight.QualityShadowStrengthCap,
                Is.EqualTo(visualQuality.AppliedShadowStrengthCap).Within(0.0001f));
            Assert.AreEqual(sunColor, light.color);
            Assert.That(light.intensity, Is.EqualTo(sunIntensity).Within(0.0001f));
            Assert.That(light.shadowStrength, Is.EqualTo(shadowStrength).Within(0.0001f));
            Assert.AreEqual(sunRotation, light.transform.rotation);
            Assert.AreEqual(ambientMode, RenderSettings.ambientMode);
            Assert.AreEqual(ambientSky, RenderSettings.ambientSkyColor);
            Assert.AreEqual(ambientEquator, RenderSettings.ambientEquatorColor);
            Assert.AreEqual(ambientGround, RenderSettings.ambientGroundColor);
            Assert.That(RenderSettings.ambientIntensity, Is.EqualTo(ambientIntensity).Within(0.0001f));
            Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(reflectionIntensity).Within(0.0001f));
            Assert.AreEqual(fog, RenderSettings.fog);
            Assert.AreEqual(fogMode, RenderSettings.fogMode);
            Assert.AreEqual(fogColor, RenderSettings.fogColor);
            Assert.That(RenderSettings.fogDensity, Is.EqualTo(fogDensity).Within(0.0001f));
            Assert.AreSame(runtimeSkybox, RenderSettings.skybox);
            Assert.That(Convert.ToSingle(ReadMember(volume, "weight")), Is.EqualTo(volumeWeight).Within(0.0001f));
            Assert.That(ReadVolumeFloat(dayNight, "_colorAdjustments", "postExposure"), Is.EqualTo(postExposure).Within(0.0001f));
            Assert.That(ReadVolumeFloat(dayNight, "_whiteBalance", "temperature"), Is.EqualTo(temperature).Within(0.0001f));
            Assert.That(ReadVolumeFloat(dayNight, "_bloom", "intensity"), Is.EqualTo(bloomIntensity).Within(0.0001f));
        }
        finally
        {
            composition.Shutdown();
            world.Dispose();
            RenderSettings.skybox = previousSkybox;
            UnityEngine.Object.DestroyImmediate(testSkybox);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(volumeObject);
            UnityEngine.Object.DestroyImmediate(visualProfile);
            UnityEngine.Object.DestroyImmediate(originalVolumeProfile);
            UnityEngine.Object.DestroyImmediate(ultraVolumeProfile);
            SettingsService.Save(previousSettings);
        }
    }

    [Test]
    public static void RuntimeQualityTierMappingsAreComplete()
    {
        VisualQualityProfileAsset profile =
            AssetDatabase.LoadAssetAtPath<VisualQualityProfileAsset>(VisualQualityProfilePath);
        Assert.NotNull(profile, $"Missing visual quality profile at {VisualQualityProfilePath}.");

        SerializedObject serializedProfile = new(profile);
        UnityEngine.Object lowPipeline = serializedProfile.FindProperty("lowRenderPipelineAsset").objectReferenceValue;
        UnityEngine.Object mediumPipeline = serializedProfile.FindProperty("mediumRenderPipelineAsset").objectReferenceValue;
        UnityEngine.Object ultraPipeline = serializedProfile.FindProperty("renderPipelineAsset").objectReferenceValue;
        UnityEngine.Object ultraVolumeProfile = serializedProfile.FindProperty("globalVolumeProfile").objectReferenceValue;
        int configuredAntialiasing = serializedProfile.FindProperty("cameraAntialiasingMode").intValue;
        Assert.NotNull(lowPipeline);
        Assert.NotNull(mediumPipeline);
        Assert.NotNull(ultraPipeline);
        Assert.NotNull(ultraVolumeProfile);

        Type cameraDataType = Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        Type volumeType = Type.GetType(
            "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
        Type volumeProfileType = Type.GetType(
            "UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime");
        Assert.NotNull(cameraDataType);
        Assert.NotNull(volumeType);
        Assert.NotNull(volumeProfileType);

        GameObject cameraObject = new("QualityTierMappingCamera", typeof(Camera));
        GameObject volumeObject = new("QualityTierMappingVolume");
        ScriptableObject baselineVolumeProfile = ScriptableObject.CreateInstance(volumeProfileType);
        World world = new("QualityTierMappingValidation");

        try
        {
            Component cameraData = cameraObject.AddComponent(cameraDataType);
            Component volume = volumeObject.AddComponent(volumeType);
            WriteMember(volume, "sharedProfile", baselineVolumeProfile);
            VisualQualitySettingsSystem system =
                world.GetOrCreateSystemManaged<VisualQualitySettingsSystem>();
            MethodInfo initialize = typeof(VisualQualitySettingsSystem).GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(initialize);
            initialize.Invoke(system, new object[]
            {
                profile,
                cameraObject.GetComponent<Camera>(),
                null,
                volume
            });

            AssertRuntimeQualityTier(
                system,
                VisualQualityRuntimeMode.Low,
                lowPipeline,
                profile.LowRenderScaleOverride,
                baselineVolumeProfile,
                cameraData,
                volume,
                expectedPostProcessing: false,
                expectedAntialiasing: 0,
                expectedShadowStrengthCap: profile.LowSunShadowStrength);
            AssertRuntimeQualityTier(
                system,
                VisualQualityRuntimeMode.Medium,
                mediumPipeline,
                profile.MediumRenderScaleOverride,
                baselineVolumeProfile,
                cameraData,
                volume,
                expectedPostProcessing: false,
                expectedAntialiasing: 0,
                expectedShadowStrengthCap: profile.MediumSunShadowStrength);
            AssertRuntimeQualityTier(
                system,
                VisualQualityRuntimeMode.High,
                mediumPipeline,
                profile.MediumRenderScaleOverride,
                baselineVolumeProfile,
                cameraData,
                volume,
                expectedPostProcessing: false,
                expectedAntialiasing: configuredAntialiasing,
                expectedShadowStrengthCap: profile.MediumSunShadowStrength);
            AssertRuntimeQualityTier(
                system,
                VisualQualityRuntimeMode.Ultra,
                ultraPipeline,
                profile.CameraRenderScaleOverride,
                ultraVolumeProfile,
                cameraData,
                volume,
                profile.EnableCameraPostProcessing,
                configuredAntialiasing,
                profile.PremiumSunShadowStrength);
        }
        finally
        {
            world.Dispose();
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(volumeObject);
            UnityEngine.Object.DestroyImmediate(baselineVolumeProfile);
        }
    }

    private static void AssertRuntimeQualityTier(
        VisualQualitySettingsSystem system,
        VisualQualityRuntimeMode mode,
        UnityEngine.Object expectedPipeline,
        float expectedRenderScale,
        UnityEngine.Object expectedVolumeProfile,
        Component cameraData,
        Component volume,
        bool expectedPostProcessing,
        int expectedAntialiasing,
        float expectedShadowStrengthCap)
    {
        system.ApplyRuntimeMode(mode);

        Assert.AreEqual(mode, system.AppliedMode);
        Assert.AreSame(expectedPipeline, QualitySettings.renderPipeline);
        SerializedProperty renderScale = new SerializedObject(expectedPipeline).FindProperty("m_RenderScale");
        Assert.NotNull(renderScale, $"{expectedPipeline.name} is missing m_RenderScale.");
        Assert.That(renderScale.floatValue, Is.EqualTo(expectedRenderScale).Within(0.001f));
        Assert.AreSame(expectedVolumeProfile, ReadMember(volume, "sharedProfile"));
        Assert.AreEqual(expectedPostProcessing, Convert.ToBoolean(ReadMember(cameraData, "renderPostProcessing")));
        Assert.AreEqual(expectedAntialiasing, Convert.ToInt32(ReadMember(cameraData, "antialiasing")));
        Assert.That(system.AppliedShadowStrengthCap, Is.EqualTo(expectedShadowStrengthCap).Within(0.001f));
    }

    private static float ReadVolumeFloat(DayNightSystem dayNight, string componentFieldName, string parameterName)
    {
        FieldInfo componentField = typeof(DayNightSystem).GetField(
            componentFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(componentField, $"DayNightSystem is missing {componentFieldName}.");
        object component = componentField.GetValue(dayNight);
        Assert.NotNull(component, $"DayNightSystem did not bind {componentFieldName}.");
        object parameter = ReadMember(component, parameterName);
        return Convert.ToSingle(ReadMember(parameter, "value"));
    }

    private static object ReadMember(object target, string memberName)
    {
        Assert.NotNull(target, $"Cannot read {memberName} from a null target.");
        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (property != null)
            return property.GetValue(target);

        FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field, $"{type.Name} is missing {memberName}.");
        return field.GetValue(target);
    }

    private static void WriteMember(object target, string memberName, object value)
    {
        Assert.NotNull(target, $"Cannot write {memberName} on a null target.");
        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (property != null)
        {
            property.SetValue(target, value);
            return;
        }

        FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field, $"{type.Name} is missing {memberName}.");
        field.SetValue(target, value);
    }
}
#endif
