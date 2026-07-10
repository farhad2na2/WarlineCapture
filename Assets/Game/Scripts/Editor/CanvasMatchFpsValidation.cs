using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;
using Game.Composition;

namespace Game.Editor
{
    public static class CanvasMatchFpsValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const int MatchReadyTimeoutFrames = 12000;

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
        private static bool buildingSliceDiagnosticsEnabled;
        private static bool rawProfilerCaptureEnabled;
        private static bool rawProfilerCaptureStarted;
        private static string rawProfilerCapturePath;
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
            "GameplayRuntimeUpdate.Selection.CommandFlush",
            "GameplayRuntimeUpdate.Selection.Input",
            "GameplayRuntimeUpdate.Selection.FocusedReadModel",
            "GameplayRuntimeUpdate.Selection.Panel",
            "GameplayRuntimeUpdate.Selection.TacticalCamera",
            "GameplayRuntimeUpdate.Selection.MarkerPreview",
            "GameplayRuntimeUpdate.Selection.Camera",
            "MainMenuPlayUI.MinimapUpdate",
            "MainMenuPlayUI.FeedbackLifetime",
            "BuildingPlacementRuntimeTick.EnqueueMapBuildingPlacements",
            "BuildingPlacementRuntimeTick.EnqueueMapVehiclePlacements",
            "BuildingPlacementRuntimeTick.UpdateBuildingRuntimeState",
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
            "BuildingPlacementRuntimeTick.UpdateInput",
            "BuildingDefenseAttackSystem.TargetCollection",
            "BuildingDefenseAttackSystem.TargetSelection",
            "BuildingDefenseAttackSystem.EffectApplication",
            "Default World Game.Runtime.UnitMotionAudioSystem",
            "Default World Game.Runtime.AudioCooldownSystem",
            "Default World Game.Runtime.ResourceExchangeQueueTickSystem",
            "Default World Game.UI.Runtime.UiResourceExchangeReadModelSystem",
            "Default World Unity.Entities.SimulationSystemGroup"
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
                GameplayRuntimeUpdateDebugFlags.Reset();
                InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics = false;
                buildingSliceDiagnosticsEnabled = false;
                rawProfilerCaptureEnabled = ResolveBool("WARLINE_CANVAS_MATCH_FPS_CAPTURE_RAW");
                rawProfilerCaptureStarted = false;
                rawProfilerCapturePath = Environment.GetEnvironmentVariable("WARLINE_CANVAS_MATCH_FPS_CAPTURE_PATH") ??
                    "/private/tmp/warline-canvas-match-fps-capture";
                RuntimeHelpers.RunClassConstructor(typeof(BuildingDefenseAttackSystem).TypeHandle);
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

                    if (!matchScene.GameplayStartComplete)
                    {
                        if (frameCount - deployFrame >= MatchReadyTimeoutFrames)
                        {
                            Complete(false, $"Timed out waiting for gameplay start complete variant={variant} frame={frameCount} deployFrame={deployFrame} scene={SceneManager.GetActiveScene().name} progress={matchScene.GameplayStartProgress01:0.00} status={matchScene.GameplayStartStatus}");
                            return;
                        }

                        return;
                    }

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

                if (sampledFrame == 0 && rawProfilerCaptureEnabled && !rawProfilerCaptureStarted)
                    StartRawProfilerCapture();

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
            return $"variant={variant} samples={frameTimes.Count} warmupFrames={warmupFrames} avgMs={average * 1000f:0.000} fps={fps:0.0} medianMs={median * 1000f:0.000} medianFps={medianFps:0.0} p95Ms={p95 * 1000f:0.000} minMs={min * 1000f:0.000} maxMs={max * 1000f:0.000} deployFrame={deployFrame} matchReadyFrame={matchReadyFrame} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} focused={(Application.isFocused ? 1 : 0)} batch={(Application.isBatchMode ? 1 : 0)} disableBuildingPlacement={(GameplayRuntimeUpdateDebugFlags.DisableBuildingPlacementRuntime ? 1 : 0)} disableSelection={(GameplayRuntimeUpdateDebugFlags.DisableSelectionRuntime ? 1 : 0)} disableUnitMotionAudio={(GameplayRuntimeUpdateDebugFlags.DisableUnitMotionAudioRuntime ? 1 : 0)} rawCapture={(rawProfilerCaptureStarted ? rawProfilerCapturePath + ".raw" : "disabled")} markers={BuildMarkerSummary()}";
        }

        private static void StartRawProfilerCapture()
        {
            string rawPath = rawProfilerCapturePath + ".raw";
            if (File.Exists(rawPath))
                File.Delete(rawPath);

            UnityEngine.Profiling.Profiler.enabled = false;
            ProfilerDriver.ClearAllFrames();
            UnityEngine.Profiling.Profiler.logFile = rawProfilerCapturePath;
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
            UnityEngine.Profiling.Profiler.enabled = true;
            rawProfilerCaptureStarted = true;
            Debug.Log($"[CanvasMatchFpsValidation] rawCaptureStarted path={rawPath}");
        }

        private static void StopRawProfilerCapture()
        {
            if (!rawProfilerCaptureStarted)
                return;

            UnityEngine.Profiling.Profiler.enabled = false;
            UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            UnityEngine.Profiling.Profiler.logFile = string.Empty;
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
            bool disableBuildingPlacement = string.Equals(variant, "DisableBuildingPlacement", StringComparison.OrdinalIgnoreCase);
            bool disableSelection = string.Equals(variant, "DisableSelection", StringComparison.OrdinalIgnoreCase);
            bool disableUnitMotionAudio = string.Equals(variant, "DisableUnitMotionAudio", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_UNIT_MOTION_AUDIO");
            bool disableBuildingBoundary = string.Equals(variant, "DisableBuildingBoundary", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_BOUNDARY");
            bool disableBuildingProduction = string.Equals(variant, "DisableBuildingProduction", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_PRODUCTION");
            bool disableBuildingResource = string.Equals(variant, "DisableBuildingResource", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_RESOURCE");
            bool disableBuildingResourceHauler = string.Equals(variant, "DisableBuildingResourceHauler", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_RESOURCE_HAULER");
            bool disableBuildingVisual = string.Equals(variant, "DisableBuildingVisual", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_VISUAL");
            bool disableBuildingInput = string.Equals(variant, "DisableBuildingInput", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_INPUT");
            bool disableBuildingReservationCleanup = string.Equals(variant, "DisableBuildingReservationCleanup", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_RESERVATION_CLEANUP");
            bool disableBuildingDestroyed = string.Equals(variant, "DisableBuildingDestroyed", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_DESTROYED");
            bool disableBuildingDoor = string.Equals(variant, "DisableBuildingDoor", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_DOOR");
            bool disableBuildingMarker = string.Equals(variant, "DisableBuildingMarker", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_DISABLE_BUILDING_MARKER");
            buildingSliceDiagnosticsEnabled =
                string.Equals(variant, "BuildingSliceDiagnostics", StringComparison.OrdinalIgnoreCase) ||
                ResolveBool("WARLINE_CANVAS_MATCH_FPS_BUILDING_SLICE_DIAG");

            GameplayRuntimeUpdateDebugFlags.DisableBuildingPlacementRuntime = disableBuildingPlacement;
            GameplayRuntimeUpdateDebugFlags.DisableSelectionRuntime = disableSelection;
            GameplayRuntimeUpdateDebugFlags.DisableUnitMotionAudioRuntime = disableUnitMotionAudio;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingBoundaryRuntime = disableBuildingBoundary;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingProductionRuntime = disableBuildingProduction;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingResourceRuntime = disableBuildingResource;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingResourceHaulerRuntime = disableBuildingResourceHauler;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingVisualRuntime = disableBuildingVisual;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingInputRuntime = disableBuildingInput;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingReservationCleanupRuntime = disableBuildingReservationCleanup;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingDestroyedRuntime = disableBuildingDestroyed;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingDoorRuntime = disableBuildingDoor;
            GameplayRuntimeUpdateDebugFlags.DisableBuildingMarkerRuntime = disableBuildingMarker;
            InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics = buildingSliceDiagnosticsEnabled;

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

        private static bool ResolveBool(string name)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return string.Equals(configured, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(configured, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(configured, "yes", StringComparison.OrdinalIgnoreCase);
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
            StopRawProfilerCapture();
            DisposeMarkerRecorders();
            GameplayRuntimeUpdateDebugFlags.Reset();
            InitialUnitsRuntimeState.BuildingRuntimeSliceDiagnostics = false;
            buildingSliceDiagnosticsEnabled = false;
            if (success)
                Debug.Log($"[CanvasMatchFpsValidation] result=Passed {message}");
            else
                Debug.LogError($"[CanvasMatchFpsValidation] result=Failed {message}");
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
