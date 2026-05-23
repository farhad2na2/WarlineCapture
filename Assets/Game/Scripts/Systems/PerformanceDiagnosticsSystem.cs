using System;
using System.Collections.Generic;
using Game.Scripts.UI;
using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

public sealed class PerformanceDiagnosticsSystem
{
    private const double FreezeLogThresholdSeconds = 0.15d;
    private const double LowFpsDiagThreshold = 30d;
    private const double FrameRateDiagIntervalSeconds = 2d;
    private const double FpsUiUpdateIntervalSeconds = 0.25d;
    private const double SlowFrameDiagThresholdSeconds = 0.025d;
    private const double SlowFrameDiagCooldownSeconds = 0.5d;
    private const int MaxAutoProfilerMarkerRecorders = 32;

    private readonly bool _enableFrameRateDiagnostics = true;
    private readonly bool _enableSlowFrameDiagnostics = true;
    private readonly System.Text.StringBuilder _freezeLogBuilder = new();
    private readonly System.Text.StringBuilder _lastStepLogBuilder = new();
    private readonly Dictionary<string, StepPerfStats> _stepPerfStats = new();
    private readonly List<NamedProfilerRecorder> _markerRecorders = new();
    private double _lastUpdateTimestamp;
    private double _suppressFrameGapUntilTimestamp;
    private double _nextFrameRateDiagTimestamp;
    private double _nextSlowFrameDiagTimestamp;
    private double _frameRateDiagAccumulatedSeconds;
    private double _frameRateDiagUpdateAccumulatedSeconds;
    private double _frameRateDiagMaxUpdateSeconds;
    private double _fpsUiAccumulatedSeconds;
    private double _frameStartTimestamp;
    private int _frameRateDiagFrames;
    private int _fpsUiFrames;
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

    private struct StepPerfStats
    {
        public double TotalSeconds;
        public double MaxSeconds;
        public int Samples;
    }

    private struct NamedProfilerRecorder
    {
        public string Name;
        public ProfilerRecorder Recorder;
    }

    public void Initialize()
    {
        _lastApplicationFocused = Application.isFocused;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _nextFrameRateDiagTimestamp = _lastUpdateTimestamp + FrameRateDiagIntervalSeconds;
        StartProfilerRecorders();
        CaptureGcCounts();
    }

    public void BeginUpdate(bool gameplayActive)
    {
        double now = Time.realtimeSinceStartupAsDouble;
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
                Debug.Log($"[FreezeDetect] Frame gap frame={Time.frameCount} Gap={(gapSeconds * 1000d):F1}ms GC={BuildGcDeltaString()} LastSteps={_lastStepLogBuilder}");
            }
        }

        _lastUpdateTimestamp = now;
        _frameStartTimestamp = Time.realtimeSinceStartupAsDouble;
        _freezeLogBuilder.Clear();
        _lastStepLogBuilder.Clear();
    }

    public double BeginStep()
    {
        return Time.realtimeSinceStartupAsDouble;
    }

    public bool EndStep(string name, double start)
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        RecordStepStats(name, elapsed);

        if (_lastStepLogBuilder.Length > 0)
            _lastStepLogBuilder.Append(", ");

        _lastStepLogBuilder.Append(name);
        _lastStepLogBuilder.Append('=');
        _lastStepLogBuilder.Append((elapsed * 1000d).ToString("F1"));
        _lastStepLogBuilder.Append("ms");

        if (elapsed < FreezeLogThresholdSeconds)
            return false;

        if (_freezeLogBuilder.Length > 0)
            _freezeLogBuilder.Append(", ");

        _freezeLogBuilder.Append(name);
        _freezeLogBuilder.Append('=');
        _freezeLogBuilder.Append((elapsed * 1000d).ToString("F1"));
        _freezeLogBuilder.Append("ms");
        return true;
    }

    public void EndUpdate(
        bool gameplayActive,
        bool hadSlowStep,
        MenuView menuView,
        int impostorCount,
        bool gameplayInitialized,
        bool playRequested)
    {
        double now = Time.realtimeSinceStartupAsDouble;
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
            _freezeLogBuilder.Append((totalSeconds * 1000d).ToString("F1"));
            _freezeLogBuilder.Append("ms");
            Debug.Log($"[FreezeDetect] Update hitch frame={Time.frameCount} {_freezeLogBuilder}");
        }

        LogSlowUpdateDiagnosticsIfNeeded(gameplayActive, totalSeconds, now, gameplayInitialized, playRequested);
        UpdateFpsLabel(menuView);
        UpdateFrameRateDiagnostics(gameplayActive, now, impostorCount);
        CaptureGcCounts();
    }

    public void OnApplicationFocus(bool hasFocus)
    {
        _lastApplicationFocused = hasFocus;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        _applicationPaused = pauseStatus;
        _lastUpdateTimestamp = Time.realtimeSinceStartupAsDouble;
        _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
    }

    public double BeginTimedSection()
    {
        return Time.realtimeSinceStartupAsDouble;
    }

    public void EndLateUpdate(double start, int impostorCount)
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] LateUpdate hitch frame={Time.frameCount} UnitRenderLate={(elapsed * 1000d):F1}ms impostors={impostorCount} GC={BuildGcDeltaString()}");
    }

    public void EndOnGui(double start)
    {
        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        if (elapsed >= FreezeLogThresholdSeconds)
            Debug.Log($"[FreezeDetect] OnGUI hitch frame={Time.frameCount} Total={(elapsed * 1000d):F1}ms GC={BuildGcDeltaString()}");
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

    private void UpdateFrameRateDiagnostics(bool gameplayActive, double now, int impostorCount)
    {
        if (!_enableFrameRateDiagnostics)
            return;

        if (_applicationPaused)
        {
            _frameRateDiagFrames = 0;
            _frameRateDiagAccumulatedSeconds = 0d;
            _frameRateDiagUpdateAccumulatedSeconds = 0d;
            _frameRateDiagMaxUpdateSeconds = 0d;
            _stepPerfStats.Clear();
            _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
            return;
        }

        _frameRateDiagFrames++;
        _frameRateDiagAccumulatedSeconds += Time.unscaledDeltaTime;
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
            GetRuntimeUnitCounts(out int units, out int modelInstances);
            string label = gameplayActive ? "FrameRateDiag" : "FrameRateDiag:PreGame";
            string preGameDetails = gameplayActive
                ? string.Empty
                : $" vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} lastSteps={_lastStepLogBuilder}";
            Debug.Log(
                $"[{label}] fps={averageFps:F1} avgFrame={averageFrameMs:F1}ms " +
                $"updateAvg={updateAverageMs:F1}ms updateMax={_frameRateDiagMaxUpdateSeconds * 1000d:F1}ms " +
                $"{BuildFrameTimingDiagString()} " +
                $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                $"units={units} models={modelInstances} impostors={impostorCount} " +
                $"memory={BuildMemoryDiagString()} focused={(Application.isFocused ? 1 : 0)}{preGameDetails} " +
                $"stepStats={BuildStepStatsString()} markers={BuildProfilerMarkerDiagString()}");
        }

        _frameRateDiagFrames = 0;
        _frameRateDiagAccumulatedSeconds = 0d;
        _frameRateDiagUpdateAccumulatedSeconds = 0d;
        _frameRateDiagMaxUpdateSeconds = 0d;
        _stepPerfStats.Clear();
        _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
    }

    private void UpdateFpsLabel(MenuView menuView)
    {
        if (menuView == null)
            return;

        _fpsUiFrames++;
        _fpsUiAccumulatedSeconds += Time.unscaledDeltaTime;
        if (_fpsUiAccumulatedSeconds < FpsUiUpdateIntervalSeconds)
            return;

        double fps = _fpsUiAccumulatedSeconds > 0d ? _fpsUiFrames / _fpsUiAccumulatedSeconds : 0d;
        menuView.SetFpsLabel(Mathf.RoundToInt((float)fps));
        _fpsUiFrames = 0;
        _fpsUiAccumulatedSeconds = 0d;
    }

    private long ReadProfilerRecorder(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : -1L;
    }

    private void GetRuntimeUnitCounts(out int units, out int modelInstances)
    {
        units = 0;
        modelInstances = 0;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<Faction>());
        using EntityQuery modelQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitModelInstanceReference>());
        units = unitQuery.CalculateEntityCount();
        modelInstances = modelQuery.CalculateEntityCount();
    }

    private void RecordUpdateFrameStats(double totalSeconds)
    {
        _frameRateDiagUpdateAccumulatedSeconds += totalSeconds;
        if (totalSeconds > _frameRateDiagMaxUpdateSeconds)
            _frameRateDiagMaxUpdateSeconds = totalSeconds;
    }

    private void RecordStepStats(string name, double elapsed)
    {
        if (!_stepPerfStats.TryGetValue(name, out StepPerfStats stats))
            stats = default;

        stats.TotalSeconds += elapsed;
        stats.Samples++;
        if (elapsed > stats.MaxSeconds)
            stats.MaxSeconds = elapsed;
        _stepPerfStats[name] = stats;
    }

    private void LogSlowUpdateDiagnosticsIfNeeded(
        bool gameplayActive,
        double totalSeconds,
        double now,
        bool gameplayInitialized,
        bool playRequested)
    {
        if (!_enableSlowFrameDiagnostics || totalSeconds < SlowFrameDiagThresholdSeconds || now < _nextSlowFrameDiagTimestamp)
            return;

        _nextSlowFrameDiagTimestamp = now + SlowFrameDiagCooldownSeconds;
        GetRuntimeUnitCounts(out int units, out int modelInstances);
        string label = gameplayActive ? "PerfDiag" : "PerfDiag:PreGame";
        Debug.Log(
            $"[{label}] slowUpdate frame={Time.frameCount} total={totalSeconds * 1000d:F1}ms " +
            $"gc={BuildGcDeltaString()} {BuildFrameTimingDiagString()} steps={_lastStepLogBuilder} units={units} models={modelInstances} " +
            $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
            $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
            $"memory={BuildMemoryDiagString()} uiToolkit=0 " +
            $"gameplayInitialized={(gameplayInitialized ? 1 : 0)} playRequested={(playRequested ? 1 : 0)} " +
            $"focused={(Application.isFocused ? 1 : 0)} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} " +
            $"markers={BuildProfilerMarkerDiagString()}");
    }

    private string BuildStepStatsString()
    {
        if (_stepPerfStats.Count == 0)
            return "none";

        System.Text.StringBuilder builder = new();
        foreach (KeyValuePair<string, StepPerfStats> pair in _stepPerfStats)
        {
            StepPerfStats stats = pair.Value;
            double avgMs = stats.Samples > 0 ? (stats.TotalSeconds * 1000d) / stats.Samples : 0d;
            if (builder.Length > 0)
                builder.Append("|");
            builder.Append(pair.Key);
            builder.Append(":avg=");
            builder.Append(avgMs.ToString("F1"));
            builder.Append("ms,max=");
            builder.Append((stats.MaxSeconds * 1000d).ToString("F1"));
            builder.Append("ms");
        }

        return builder.ToString();
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
