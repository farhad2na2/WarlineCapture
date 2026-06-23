using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;

public static class CanvasMatchFpsValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";

    private static readonly List<float> frameTimes = new(360);
    private static int frameCount;
    private static int deployFrame;
    private static int matchReadyFrame;
    private static bool deploySubmitted;
    private static bool matchReady;
    private static bool completed;
    private static double startedAt;
    private static int warmupFrames;
    private static int sampleFrames;
    private static string variant;
    private static bool variantApplied;
    private static readonly List<NamedRecorderSample> recorderSamples = new();
    private static readonly string[] markerNames =
    {
        "GameplayRuntimeUpdate.RoadBuild",
        "GameplayRuntimeUpdate.BuildingPlacement",
        "GameplayRuntimeUpdate.Selection",
        "GameplayRuntimeUpdate.RuntimeCity",
        "GameplayRuntimeUpdate.RuntimeGridBlockers",
        "GameplayRuntimeUpdate.RuntimeDecorations",
        "GameplayRuntimeUpdate.DayNight",
        "GameplayRuntimeUpdate.CitizenPopulation",
        "GameplayRuntimeUpdate.MainMenu",
        "GameplayRuntimeUpdate.EndUpdate",
        "MainMenuPlayUI.MinimapUpdate",
        "MainMenuPlayUI.FeedbackLifetime",
        "BuildingPlacementRuntimeTick.EnqueueMapBuildingPlacements",
        "BuildingPlacementRuntimeTick.EnqueueMapVehiclePlacements",
        "BuildingPlacementRuntimeTick.UpdateBuildingRuntimeBoundary",
        "BuildingPlacementRuntimeTick.ProcessPendingProductions",
        "BuildingPlacementRuntimeTick.UpdateActiveProductionTransports",
        "BuildingPlacementRuntimeTick.UpdateResourceProduction",
        "BuildingPlacementRuntimeTick.UpdateResourceHaulers",
        "BuildingPlacementRuntimeTick.UpdateBuildingResourceVisuals",
        "BuildingPlacementRuntimeTick.CleanupRecentSpawnReservations",
        "BuildingPlacementRuntimeTick.SyncDestroyedRuntimeBuildingCombatEntities",
        "BuildingPlacementRuntimeTick.UpdateDestroyedBuildings",
        "BuildingPlacementRuntimeTick.UpdateRoadBarrierDoors",
        "BuildingPlacementRuntimeTick.FlushPendingMarkerRefresh",
        "BuildingPlacementRuntimeTick.UpdateInput"
    };

    private struct NamedRecorderSample
    {
        public string Name;
        public ProfilerRecorder Recorder;
        public double TotalNs;
        public long MaxNs;
        public int Samples;
    }

    public static void Run()
    {
        try
        {
            RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

            SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            warmupFrames = ResolvePositiveInt("WARLINE_CANVAS_MATCH_FPS_WARMUP_FRAMES", 120);
            sampleFrames = ResolvePositiveInt("WARLINE_CANVAS_MATCH_FPS_SAMPLE_FRAMES", 240);
            variant = Environment.GetEnvironmentVariable("WARLINE_CANVAS_MATCH_FPS_VARIANT") ?? "Normal";
            StartMarkerRecorders();

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            frameTimes.Clear();
            frameCount = 0;
            deployFrame = 0;
            matchReadyFrame = 0;
            deploySubmitted = false;
            matchReady = false;
            completed = false;
            variantApplied = false;
            startedAt = EditorApplication.timeSinceStartup;

            EditorApplication.update -= Continue;
            EditorApplication.update += Continue;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CanvasMatchFpsValidation] result=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void Continue()
    {
        if (completed)
            return;

        try
        {
            if (!EditorApplication.isPlaying)
                return;

            frameCount++;
            if (frameCount == 1)
            {
                startedAt = EditorApplication.timeSinceStartup;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }

            if (EditorApplication.timeSinceStartup - startedAt > 180d)
            {
                Complete(false, $"Timed out variant={variant} frame={frameCount} deploy={deploySubmitted} matchReady={matchReady} scene={SceneManager.GetActiveScene().name}");
                return;
            }

            if (frameCount < 45)
                return;

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                Complete(false, "Menu scene is missing MenuBootstrapView.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            if (bootstrap.UiMode != RuntimeUiMode.Canvas)
            {
                Complete(false, "Runtime UI mode is not Canvas.");
                return;
            }

            if (!deploySubmitted)
            {
                UnityEngine.UI.Button deployButton = FindDeployButton();
                if (deployButton == null)
                    return;

                deployButton.onClick.Invoke();
                deploySubmitted = true;
                deployFrame = frameCount;
                return;
            }

            if (!matchReady)
            {
                MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                if (matchScene == null || !SceneManager.GetSceneByName("Match").isLoaded)
                    return;

                if (!matchScene.GameplayStartComplete && frameCount - deployFrame < 360)
                    return;

                matchReady = true;
                matchReadyFrame = frameCount;
                ApplyVariant(bootstrap);
                return;
            }

            if (!variantApplied)
                ApplyVariant(bootstrap);

            int sampledFrame = frameCount - matchReadyFrame - warmupFrames;
            if (sampledFrame < 0)
                return;

            float deltaSeconds = Time.unscaledDeltaTime;
            if (deltaSeconds > 0f)
            {
                frameTimes.Add(deltaSeconds);
                SampleMarkerRecorders();
            }

            if (frameTimes.Count >= sampleFrames)
                Complete(true, BuildResult());
        }
        catch (Exception exception)
        {
            Complete(false, exception.ToString());
        }
    }

    private static UnityEngine.UI.Button FindDeployButton()
    {
        UnityEngine.UI.Button[] buttons =
            UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude);
        for (int i = 0; i < buttons.Length; i++)
        {
            UnityEngine.UI.Button candidate = buttons[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            string objectName = candidate.gameObject.name;
            if (string.Equals(objectName, "DeployCommandButton", StringComparison.Ordinal) ||
                string.Equals(objectName, "DeployOperationButton", StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string BuildResult()
    {
        float total = 0f;
        float min = float.MaxValue;
        float max = 0f;
        for (int i = 0; i < frameTimes.Count; i++)
        {
            float sample = frameTimes[i];
            total += sample;
            min = Mathf.Min(min, sample);
            max = Mathf.Max(max, sample);
        }

        float average = frameTimes.Count > 0 ? total / frameTimes.Count : 0f;
        float fps = average > 0f ? 1f / average : 0f;
        frameTimes.Sort();
        float median = Percentile(0.50f);
        float p95 = Percentile(0.95f);
        float medianFps = median > 0f ? 1f / median : 0f;
        return $"variant={variant} samples={frameTimes.Count} warmupFrames={warmupFrames} avgMs={average * 1000f:0.000} fps={fps:0.0} medianMs={median * 1000f:0.000} medianFps={medianFps:0.0} p95Ms={p95 * 1000f:0.000} minMs={min * 1000f:0.000} maxMs={max * 1000f:0.000} deployFrame={deployFrame} matchReadyFrame={matchReadyFrame} markers={BuildMarkerSummary()}";
    }

    private static void StartMarkerRecorders()
    {
        DisposeMarkerRecorders();
        recorderSamples.Clear();
        for (int i = 0; i < markerNames.Length; i++)
        {
            ProfilerRecorder recorder;
            try
            {
                recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, markerNames[i]);
            }
            catch
            {
                continue;
            }

            if (!recorder.Valid)
            {
                recorder.Dispose();
                continue;
            }

            recorderSamples.Add(new NamedRecorderSample
            {
                Name = markerNames[i],
                Recorder = recorder
            });
        }
    }

    private static void SampleMarkerRecorders()
    {
        for (int i = 0; i < recorderSamples.Count; i++)
        {
            NamedRecorderSample sample = recorderSamples[i];
            if (!sample.Recorder.Valid)
                continue;

            long value = sample.Recorder.LastValue;
            if (value <= 0L)
                continue;

            sample.TotalNs += value;
            sample.MaxNs = Math.Max(sample.MaxNs, value);
            sample.Samples++;
            recorderSamples[i] = sample;
        }
    }

    private static string BuildMarkerSummary()
    {
        StringBuilder builder = new(512);
        for (int i = 0; i < recorderSamples.Count; i++)
        {
            NamedRecorderSample sample = recorderSamples[i];
            if (sample.Samples <= 0)
                continue;

            if (builder.Length > 0)
                builder.Append("; ");

            double averageMs = sample.TotalNs / sample.Samples / 1000000d;
            double maxMs = sample.MaxNs / 1000000d;
            builder.Append(sample.Name);
            builder.Append(" avg=");
            builder.Append(averageMs.ToString("F3", CultureInfo.InvariantCulture));
            builder.Append("ms max=");
            builder.Append(maxMs.ToString("F3", CultureInfo.InvariantCulture));
            builder.Append("ms");
        }

        return builder.Length > 0 ? builder.ToString() : "none";
    }

    private static void DisposeMarkerRecorders()
    {
        for (int i = 0; i < recorderSamples.Count; i++)
        {
            ProfilerRecorder recorder = recorderSamples[i].Recorder;
            if (recorder.Valid)
                recorder.Dispose();
        }

        recorderSamples.Clear();
    }

    private static float Percentile(float percentile01)
    {
        if (frameTimes.Count <= 0)
            return 0f;

        int index = Mathf.Clamp(Mathf.RoundToInt((frameTimes.Count - 1) * percentile01), 0, frameTimes.Count - 1);
        return frameTimes[index];
    }

    private static void ApplyVariant(MenuBootstrapView bootstrap)
    {
        variantApplied = true;
        bool hideCanvas = string.Equals(variant, "HideCanvas", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(variant, "HideCanvasAndCamera", StringComparison.OrdinalIgnoreCase);
        bool disableCamera = string.Equals(variant, "DisableWorldCamera", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(variant, "HideCanvasAndCamera", StringComparison.OrdinalIgnoreCase);
        bool disableMinimap = string.Equals(variant, "DisableMinimap", StringComparison.OrdinalIgnoreCase);
        bool unbindMinimap = string.Equals(variant, "UnbindMinimap", StringComparison.OrdinalIgnoreCase);

        if (hideCanvas && bootstrap.UiCanvas != null && bootstrap.UiCanvas.gameObject.activeSelf)
            bootstrap.UiCanvas.gameObject.SetActive(false);

        if (disableMinimap)
        {
            MatchHudMinimapView minimap =
                UnityEngine.Object.FindAnyObjectByType<MatchHudMinimapView>(FindObjectsInactive.Exclude);
            if (minimap != null && minimap.gameObject.activeSelf)
                minimap.gameObject.SetActive(false);
        }

        if (unbindMinimap)
        {
            MatchSceneView runtimeMatchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
            runtimeMatchScene?.MatchBootstrap.MainMenu?.BindMatchHudMinimap(null);
        }

        if (!disableCamera)
            return;

        MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
        if (matchScene != null && matchScene.WorldCamera != null && matchScene.WorldCamera.enabled)
            matchScene.WorldCamera.enabled = false;
    }

    private static int ResolvePositiveInt(string name, int fallback)
    {
        string configured = Environment.GetEnvironmentVariable(name);
        return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0
            ? value
            : fallback;
    }

    private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
    {
        SerializedObject serialized = new(runtimeConfig);
        SerializedProperty modeProperty = serialized.FindProperty("mode");
        if (modeProperty == null)
            throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");

        modeProperty.enumValueIndex = (int)mode;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Complete(bool success, string message)
    {
        if (completed)
            return;

        completed = true;
        EditorApplication.update -= Continue;
        DisposeMarkerRecorders();
        if (success)
            Debug.Log($"[CanvasMatchFpsValidation] result=Passed {message}");
        else
            Debug.LogError($"[CanvasMatchFpsValidation] result=Failed {message}");
        EditorApplication.Exit(success ? 0 : 1);
    }
}
