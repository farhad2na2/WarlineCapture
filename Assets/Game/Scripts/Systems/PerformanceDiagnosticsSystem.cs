using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

public sealed class PerformanceDiagnosticsSystem
{
    private const double FreezeLogThresholdSeconds = 0.15d;
    private const double LowFpsDiagThreshold = 55d;
    private const double FrameRateDiagIntervalSeconds = 2d;
    private const double SlowFrameDiagThresholdSeconds = 0.025d;
    private const double SlowFrameDiagCooldownSeconds = 0.5d;
    private const double RenderSceneBreakdownDiagIntervalSeconds = 10d;
    private const int MaxAutoProfilerMarkerRecorders = 200;
    private const int MaxTopSystemProfilerMarkers = 8;
    private const int MaxLastStepSamples = 16;
    private static readonly string[] PriorityProfilerMarkerNameFragments =
    {
        "UnitEngagementSystem",
        "UnitPathfindingSystem",
        "DynamicOccupancyRebuildSystem",
        "UnitAnimationIndexSystem",
        "UnitMove",
        "AITargetingSystem",
        "AICombatOrderSystem",
        "AISquadSystem",
        "AIBuildPlannerSystem",
        "AIProductionSystem",
        "AIEconomySystem",
        "AIFactionControlSystem",
        "UnitSurfaceTrackingSystem",
        "UnitGroundingSystem",
        "VehicleSlopeAlignmentSystem",
        "UnitGridMovementSystem",
        "UnitMoveOrderSystem",
        "UnitEngagedMovementSystem",
        "UnitIdleWanderSystem",
        "UnitLookAtTargetSystem",
        "UnitRenderBudgetSystem"
    };

    private readonly bool _enableFrameRateDiagnostics = true;
    private readonly bool _enableSlowFrameDiagnostics = true;
    private readonly System.Text.StringBuilder _freezeLogBuilder = new(256);
    private readonly System.Text.StringBuilder _lastStepLogBuilder = new(256);
    private readonly StepSample[] _lastStepSamples = new StepSample[MaxLastStepSamples];
    private readonly List<NamedProfilerRecorder> _markerRecorders = new();
    private double _lastUpdateTimestamp;
    private double _suppressFrameGapUntilTimestamp;
    private double _nextFrameRateDiagTimestamp;
    private double _nextSlowFrameDiagTimestamp;
    private double _nextRenderSceneBreakdownDiagTimestamp;
    private double _frameRateDiagAccumulatedSeconds;
    private double _frameRateDiagUpdateAccumulatedSeconds;
    private double _frameRateDiagMaxUpdateSeconds;
    private double _frameStartTimestamp;
    private int _frameRateDiagFrames;
    private int _lastStepSampleCount;
    private bool _lastApplicationFocused;
    private bool _applicationPaused;
    private int _lastGcGen0Count;
    private int _lastGcGen1Count;
    private int _lastGcGen2Count;
    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _batchesRecorder;
    private ProfilerRecorder _setPassCallsRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _verticesRecorder;

    private struct StepSample
    {
        public string Name;
        public double Seconds;
    }

    private struct NamedProfilerRecorder
    {
        public string Name;
        public ProfilerRecorder Recorder;
    }

    private struct ProfilerMarkerSample
    {
        public string Name;
        public long Value;
    }

    public void Initialize()
    {
        _lastApplicationFocused = Application.isFocused;
        _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
        _nextFrameRateDiagTimestamp = _lastUpdateTimestamp + FrameRateDiagIntervalSeconds;
        StartProfilerRecorders();
        CaptureGcCounts();
    }

    public void BeginUpdate(bool gameplayActive)
    {
        double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
        bool applicationFocused = Application.isFocused;
        if (applicationFocused != _lastApplicationFocused)
        {
            _lastApplicationFocused = applicationFocused;
            _lastUpdateTimestamp = now;
            _suppressFrameGapUntilTimestamp = now + 0.5d;
        }

        bool canReportFrameGap =
            gameplayActive &&
            applicationFocused &&
            !_applicationPaused &&
            now >= _suppressFrameGapUntilTimestamp;

        if (canReportFrameGap && _lastUpdateTimestamp > 0d)
        {
            double gapSeconds = now - _lastUpdateTimestamp;
            if (gapSeconds >= FreezeLogThresholdSeconds)
            {
                Debug.Log($"[FreezeDetect] Frame gap frame={UnityEngine.Time.frameCount} Gap={FormatMilliseconds(gapSeconds)}ms GC={BuildGcDeltaString()} LastSteps={BuildLastStepLogString()}");
            }
        }

        _lastUpdateTimestamp = now;
        _frameStartTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
        _freezeLogBuilder.Clear();
        _lastStepLogBuilder.Clear();
        _lastStepSampleCount = 0;
    }

    public double BeginStep()
    {
        return UnityEngine.Time.realtimeSinceStartupAsDouble;
    }

    public bool EndStep(string name, double start)
    {
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
        RecordLastStepSample(name, elapsed);

        if (elapsed < FreezeLogThresholdSeconds)
            return false;

        if (_freezeLogBuilder.Length > 0)
            _freezeLogBuilder.Append(", ");

        AppendStepTiming(_freezeLogBuilder, name, elapsed);
        return true;
    }

    private void RecordLastStepSample(string name, double elapsed)
    {
        if (_lastStepSampleCount >= _lastStepSamples.Length)
            return;

        _lastStepSamples[_lastStepSampleCount++] = new StepSample
        {
            Name = name,
            Seconds = elapsed
        };
    }

    private string BuildLastStepLogString()
    {
        if (_lastStepSampleCount == 0)
            return "none";

        _lastStepLogBuilder.Clear();
        for (int i = 0; i < _lastStepSampleCount; i++)
        {
            StepSample sample = _lastStepSamples[i];
            if (_lastStepLogBuilder.Length > 0)
                _lastStepLogBuilder.Append(", ");

            AppendStepTiming(_lastStepLogBuilder, sample.Name, sample.Seconds);
        }

        return _lastStepLogBuilder.ToString();
    }

    private static void AppendStepTiming(System.Text.StringBuilder builder, string name, double seconds)
    {
        builder.Append(name);
        builder.Append('=');
        AppendMilliseconds(builder, seconds);
        builder.Append("ms");
    }

    private static void AppendMilliseconds(System.Text.StringBuilder builder, double seconds)
    {
        int tenths = (int)Math.Round(seconds * 10000d, MidpointRounding.AwayFromZero);
        int whole = tenths / 10;
        int fraction = Math.Abs(tenths % 10);
        builder.Append(whole);
        builder.Append('.');
        builder.Append(fraction);
    }

    private static string FormatMilliseconds(double seconds)
    {
        return (seconds * 1000d).ToString("F1");
    }

    public void EndUpdate(
        bool gameplayActive,
        bool hadSlowStep,
        int impostorCount,
        bool gameplayInitialized,
        bool playRequested,
        bool simulationActive)
    {
        double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
        double totalSeconds = now - _frameStartTimestamp;
        RecordUpdateFrameStats(totalSeconds);
        if (gameplayActive && (hadSlowStep || totalSeconds >= FreezeLogThresholdSeconds))
        {
            if (_freezeLogBuilder.Length > 0)
                _freezeLogBuilder.Append(", ");

            _freezeLogBuilder.Append("GC=");
            _freezeLogBuilder.Append(BuildGcDeltaString());
            _freezeLogBuilder.Append(", ");
            _freezeLogBuilder.Append("Total=");
            AppendMilliseconds(_freezeLogBuilder, totalSeconds);
            _freezeLogBuilder.Append("ms");
            Debug.Log($"[FreezeDetect] Update hitch frame={UnityEngine.Time.frameCount} {_freezeLogBuilder}");
        }

        LogSlowUpdateDiagnosticsIfNeeded(gameplayActive, totalSeconds, now, gameplayInitialized, playRequested, simulationActive);
        UpdateFrameRateDiagnostics(gameplayActive, now, impostorCount, playRequested, simulationActive);
        CaptureGcCounts();
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        _lastApplicationFocused = hasFocus;
        _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        _applicationPaused = pauseStatus;
        _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    public double BeginTimedSection()
    {
        return UnityEngine.Time.realtimeSinceStartupAsDouble;
    }

    public void EndLateUpdate(double start, int impostorCount)
    {
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] LateUpdate hitch frame={UnityEngine.Time.frameCount} UnitRenderLate={FormatMilliseconds(elapsed)}ms impostors={impostorCount} GC={BuildGcDeltaString()}");
    }

    public void EndOnGui(double start)
    {
        double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] OnGUI hitch frame={UnityEngine.Time.frameCount} Total={FormatMilliseconds(elapsed)}ms GC={BuildGcDeltaString()}");
    }

    public void Dispose()
    {
        DisposeProfilerRecorders();
    }

    private void StartProfilerRecorders()
    {
        _drawCallsRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Draw Calls Count");
        _batchesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Batches Count");
        _setPassCallsRecorder = StartProfilerRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        _trianglesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Triangles Count");
        _verticesRecorder = StartProfilerRecorder(ProfilerCategory.Render, "Vertices Count");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "PlayerLoop");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "EditorLoop");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "Overhead");
        AddProfilerMarkerRecorder(ProfilerCategory.Internal, "WaitForTargetFPS");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Camera.Render");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "RenderPipelineManager.DoRenderLoop_Internal");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
        AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.PresentFrame");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "BehaviourUpdate");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "LateBehaviourUpdate");
        AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "Canvas.SendWillRenderCanvases");
        AddPriorityPlayerLoopMarkerRecorders();
        AddAvailablePlayerLoopMarkerRecorders();
    }

    private ProfilerRecorder StartProfilerRecorder(ProfilerCategory category, string statName)
    {
        try
        {
            return ProfilerRecorder.StartNew(category, statName);
        }
        catch
        {
            return default;
        }
    }

    private void AddProfilerMarkerRecorder(ProfilerCategory category, string statName)
    {
        if (HasProfilerMarkerRecorder(statName))
            return;

        ProfilerRecorder recorder = StartProfilerRecorder(category, statName);
        if (!recorder.Valid)
            return;

        _markerRecorders.Add(new NamedProfilerRecorder
        {
            Name = statName,
            Recorder = recorder
        });
    }

    private void AddAvailablePlayerLoopMarkerRecorders()
    {
        try
        {
            List<ProfilerRecorderHandle> handles = new();
            ProfilerRecorderHandle.GetAvailable(handles);
            int added = 0;
            for (int i = 0; i < handles.Count && added < MaxAutoProfilerMarkerRecorders; i++)
            {
                ProfilerRecorderHandle handle = handles[i];
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                string name = description.Name.ToString();
                if (!ShouldTrackProfilerMarker(name) || HasProfilerMarkerRecorder(name))
                    continue;

                ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                if (!recorder.Valid)
                    continue;

                _markerRecorders.Add(new NamedProfilerRecorder
                {
                    Name = name,
                    Recorder = recorder
                });
                added++;
            }
        }
        catch
        {
            // Marker enumeration is diagnostic-only and can vary by Unity/editor platform.
        }
    }

    private void AddPriorityPlayerLoopMarkerRecorders()
    {
        try
        {
            List<ProfilerRecorderHandle> handles = new();
            ProfilerRecorderHandle.GetAvailable(handles);
            for (int i = 0; i < handles.Count; i++)
            {
                ProfilerRecorderHandle handle = handles[i];
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                string name = description.Name.ToString();
                if (!ShouldTrackPriorityProfilerMarker(name) || HasProfilerMarkerRecorder(name))
                    continue;

                ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                if (!recorder.Valid)
                    continue;

                _markerRecorders.Add(new NamedProfilerRecorder
                {
                    Name = name,
                    Recorder = recorder
                });
            }
        }
        catch
        {
            // Priority marker enumeration is diagnostic-only and can vary by Unity/editor platform.
        }
    }

    private bool HasProfilerMarkerRecorder(string statName)
    {
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            if (string.Equals(_markerRecorders[i].Name, statName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private bool ShouldTrackProfilerMarker(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return
            name.Contains("PlayerLoop", StringComparison.OrdinalIgnoreCase) ||
            IsEcsSystemProfilerMarker(name) ||
            name.Contains("EditorLoop", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Render", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Canvas", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Entities", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Script", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Wait", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Present", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Gfx", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldTrackPriorityProfilerMarker(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < PriorityProfilerMarkerNameFragments.Length; i++)
        {
            if (name.Contains(PriorityProfilerMarkerNameFragments[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool IsEcsSystemProfilerMarker(string name)
    {
        return name.StartsWith("Default World ", StringComparison.OrdinalIgnoreCase) &&
               name.Contains("System", StringComparison.OrdinalIgnoreCase);
    }

    private void DisposeProfilerRecorders()
    {
        if (_drawCallsRecorder.Valid) _drawCallsRecorder.Dispose();
        if (_batchesRecorder.Valid) _batchesRecorder.Dispose();
        if (_setPassCallsRecorder.Valid) _setPassCallsRecorder.Dispose();
        if (_trianglesRecorder.Valid) _trianglesRecorder.Dispose();
        if (_verticesRecorder.Valid) _verticesRecorder.Dispose();
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            ProfilerRecorder recorder = _markerRecorders[i].Recorder;
            if (recorder.Valid)
                recorder.Dispose();
        }

        _markerRecorders.Clear();
    }

    private void UpdateFrameRateDiagnostics(bool gameplayActive, double now, int impostorCount, bool playRequested, bool simulationActive)
    {
        if (!_enableFrameRateDiagnostics)
            return;

        if (_applicationPaused || (!Application.isFocused && !Application.isBatchMode))
        {
            ResetFrameRateDiagnosticWindow(now);
            return;
        }

        _frameRateDiagFrames++;
        _frameRateDiagAccumulatedSeconds += UnityEngine.Time.unscaledDeltaTime;
        if (now < _nextFrameRateDiagTimestamp)
            return;

        double averageFrameMs = _frameRateDiagFrames > 0
            ? (_frameRateDiagAccumulatedSeconds * 1000d) / _frameRateDiagFrames
            : 0d;
        double averageFps = averageFrameMs > 0d ? 1000d / averageFrameMs : 0d;
        double updateAverageMs = _frameRateDiagFrames > 0
            ? (_frameRateDiagUpdateAccumulatedSeconds * 1000d) / _frameRateDiagFrames
            : 0d;
        if (averageFps < LowFpsDiagThreshold)
        {
            GetRuntimeVisualCounts(
                out int units,
                out int modelInstances,
                out int sourceKeys,
                out int sourceKeyFallbackVisuals,
                out int initialSpawnConfigs);
            string label = gameplayActive ? "FrameRateDiag" : "FrameRateDiag:PreGame";
            string preGameDetails = gameplayActive
                ? string.Empty
                : $" vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} lastSteps={BuildLastStepLogString()}";
            Debug.Log(
                $"[{label}] fps={averageFps:F1} avgFrame={averageFrameMs:F1}ms " +
                $"updateAvg={updateAverageMs:F1}ms updateMax={_frameRateDiagMaxUpdateSeconds * 1000d:F1}ms " +
                $"{BuildFrameTimingDiagString()} " +
                $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                $"units={units} models={modelInstances} sourceKeys={sourceKeys} sourceKeyFallbackVisuals={sourceKeyFallbackVisuals} initialSpawnConfigs={initialSpawnConfigs} impostors={impostorCount} " +
                $"memory={BuildMemoryDiagString()} focused={(Application.isFocused ? 1 : 0)} playRequested={(playRequested ? 1 : 0)} simulationActive={(simulationActive ? 1 : 0)}{preGameDetails} " +
                $"stepStats={BuildStepStatsString()} topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}");
            LogRenderSceneBreakdownIfNeeded(now, averageFps);
        }

        _frameRateDiagFrames = 0;
        _frameRateDiagAccumulatedSeconds = 0d;
        _frameRateDiagUpdateAccumulatedSeconds = 0d;
        _frameRateDiagMaxUpdateSeconds = 0d;
        _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
    }

    private void ResetFrameRateDiagnosticWindow(double now)
    {
        _frameRateDiagFrames = 0;
        _frameRateDiagAccumulatedSeconds = 0d;
        _frameRateDiagUpdateAccumulatedSeconds = 0d;
        _frameRateDiagMaxUpdateSeconds = 0d;
        _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
    }

    private long ReadProfilerRecorder(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : -1L;
    }

    private void GetRuntimeVisualCounts(
        out int units,
        out int modelInstances,
        out int sourceKeys,
        out int sourceKeyFallbackVisuals,
        out int initialSpawnConfigs)
    {
        units = 0;
        modelInstances = 0;
        sourceKeys = 0;
        sourceKeyFallbackVisuals = 0;
        initialSpawnConfigs = 0;
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<Faction>());
        using EntityQuery modelQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            Any = new[]
            {
                ComponentType.ReadOnly<UnitModelInstanceReference>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>(),
            }
        });
        using EntityQuery sourceKeyQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using EntityQuery sourceKeyFallbackVisualQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.Exclude<UnitModelInstanceReference>(),
            ComponentType.Exclude<UnitDetailedVisualReference>(),
            ComponentType.Exclude<RuntimeBuildingCombatTag>(),
            ComponentType.Exclude<UnitRenderBudgetCulledUnitTag>());
        using EntityQuery initialSpawnConfigQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        units = unitQuery.CalculateEntityCount();
        modelInstances = modelQuery.CalculateEntityCount();
        sourceKeys = sourceKeyQuery.CalculateEntityCount();
        sourceKeyFallbackVisuals = sourceKeyFallbackVisualQuery.CalculateEntityCount();
        initialSpawnConfigs = initialSpawnConfigQuery.CalculateEntityCount();
    }

    private void RecordUpdateFrameStats(double totalSeconds)
    {
        _frameRateDiagUpdateAccumulatedSeconds += totalSeconds;
        if (totalSeconds > _frameRateDiagMaxUpdateSeconds)
            _frameRateDiagMaxUpdateSeconds = totalSeconds;
    }

    private void LogSlowUpdateDiagnosticsIfNeeded(
        bool gameplayActive,
        double totalSeconds,
        double now,
        bool gameplayInitialized,
        bool playRequested,
        bool simulationActive)
    {
        if (!_enableSlowFrameDiagnostics || totalSeconds < SlowFrameDiagThresholdSeconds || now < _nextSlowFrameDiagTimestamp)
            return;
        if (!Application.isFocused && !Application.isBatchMode)
            return;

        _nextSlowFrameDiagTimestamp = now + SlowFrameDiagCooldownSeconds;
        GetRuntimeVisualCounts(
            out int units,
            out int modelInstances,
            out int sourceKeys,
            out int sourceKeyFallbackVisuals,
            out int initialSpawnConfigs);
        string label = gameplayActive ? "PerfDiag" : "PerfDiag:PreGame";
        Debug.Log(
            $"[{label}] slowUpdate frame={UnityEngine.Time.frameCount} total={totalSeconds * 1000d:F1}ms " +
            $"gc={BuildGcDeltaString()} {BuildFrameTimingDiagString()} steps={BuildLastStepLogString()} units={units} models={modelInstances} sourceKeys={sourceKeys} sourceKeyFallbackVisuals={sourceKeyFallbackVisuals} initialSpawnConfigs={initialSpawnConfigs} " +
            $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
            $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
            $"memory={BuildMemoryDiagString()} uiToolkit=0 " +
            $"gameplayInitialized={(gameplayInitialized ? 1 : 0)} playRequested={(playRequested ? 1 : 0)} simulationActive={(simulationActive ? 1 : 0)} " +
            $"focused={(Application.isFocused ? 1 : 0)} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} " +
            $"topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}");
    }

    private string BuildStepStatsString()
    {
        return "none";
    }

    private string BuildMemoryDiagString()
    {
        return
            $"alloc={Profiler.GetTotalAllocatedMemoryLong() / (1024L * 1024L)}MB " +
            $"reserved={Profiler.GetTotalReservedMemoryLong() / (1024L * 1024L)}MB " +
            $"mono={Profiler.GetMonoUsedSizeLong() / (1024L * 1024L)}MB";
    }

    private string BuildFrameTimingDiagString()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTiming[] timings = new FrameTiming[1];
        uint count = FrameTimingManager.GetLatestTimings(1, timings);
        if (count == 0)
            return "frameTiming=unavailable";

        FrameTiming timing = timings[0];
        return
            $"cpuFrame={timing.cpuFrameTime:F1}ms " +
            $"cpuMain={timing.cpuMainThreadFrameTime:F1}ms " +
            $"cpuRender={timing.cpuRenderThreadFrameTime:F1}ms " +
            $"gpu={timing.gpuFrameTime:F1}ms";
    }

    private string BuildProfilerMarkerDiagString()
    {
        if (_markerRecorders.Count == 0)
            return "none";

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            NamedProfilerRecorder entry = _markerRecorders[i];
            if (!entry.Recorder.Valid)
                continue;

            long value = entry.Recorder.LastValue;
            if (value <= 0)
                continue;

            if (builder.Length > 0)
                builder.Append("|");
            builder.Append(entry.Name);
            builder.Append("=");
            builder.Append((value / 1000000d).ToString("F1"));
            builder.Append("ms");
        }

        return builder.Length > 0 ? builder.ToString() : "none-active";
    }

    private void LogRenderSceneBreakdownIfNeeded(double now, double averageFps)
    {
        if (averageFps >= LowFpsDiagThreshold || now < _nextRenderSceneBreakdownDiagTimestamp)
            return;
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        _nextRenderSceneBreakdownDiagTimestamp = now + RenderSceneBreakdownDiagIntervalSeconds;
        Debug.Log(BuildRenderSceneBreakdownDiagString());
    }

    private string BuildRenderSceneBreakdownDiagString()
    {
        return
            $"[RenderSceneDiag] frame={UnityEngine.Time.frameCount} source=profilerCounters " +
            $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
            $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
            $"topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}";
    }

    private string BuildTopSystemProfilerMarkerString()
    {
        if (_markerRecorders.Count == 0)
            return "none";

        List<ProfilerMarkerSample> samples = new();
        for (int i = 0; i < _markerRecorders.Count; i++)
        {
            NamedProfilerRecorder entry = _markerRecorders[i];
            if (!entry.Recorder.Valid || !IsEcsSystemProfilerMarker(entry.Name))
                continue;

            long value = entry.Recorder.LastValue;
            if (value <= 0)
                continue;

            samples.Add(new ProfilerMarkerSample
            {
                Name = entry.Name,
                Value = value
            });
        }

        if (samples.Count == 0)
            return "none-active";

        samples.Sort((lhs, rhs) => rhs.Value.CompareTo(lhs.Value));
        System.Text.StringBuilder builder = new();
        int count = Math.Min(MaxTopSystemProfilerMarkers, samples.Count);
        for (int i = 0; i < count; i++)
        {
            ProfilerMarkerSample sample = samples[i];
            if (builder.Length > 0)
                builder.Append("|");

            builder.Append(sample.Name);
            builder.Append("=");
            builder.Append((sample.Value / 1000000d).ToString("F1"));
            builder.Append("ms");
        }

        return builder.ToString();
    }

    private string BuildGcDeltaString()
    {
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        return $"{gen0 - _lastGcGen0Count}/{gen1 - _lastGcGen1Count}/{gen2 - _lastGcGen2Count}";
    }

    private void CaptureGcCounts()
    {
        _lastGcGen0Count = GC.CollectionCount(0);
        _lastGcGen1Count = GC.CollectionCount(1);
        _lastGcGen2Count = GC.CollectionCount(2);
    }
}
