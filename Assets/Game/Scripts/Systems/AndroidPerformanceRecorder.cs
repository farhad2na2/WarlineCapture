using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Runtime
{
    public sealed partial class AndroidPerformanceRecorder
    {
        private const string GateCommandLineArgument = "-warlineAndroidPerformanceGate";
        private const string FrameRateCommandLineArgument = "-warlinePerformanceFrameRate";
        private const string PerformanceRouteCommandLineArgument = "-warlinePerformanceRoute";
        private const string DevelopmentTaskId = "APH-803";
        private const string ReleaseTaskId = "APH-804";
        private const string Vrp092TaskId = "VRP-092";
        private const string OutputDirectoryName = "WarlineCapture/Diagnostics";
        private const string DevelopmentOutputFileName = "aph803_android_development_recorder.json";
        private const string ReleaseOutputFileName = "aph804_android_release_recorder.json";
        private const float ReleaseWarmupSeconds = 60f;
        private const float ReleaseCaptureSeconds = 600f;
        private const float Vrp092WarmupSeconds = 15f;
        private const float Vrp092CaptureSeconds = 120f;
        private const float SlowMetricIntervalSeconds = 1f;
        private const int RequiredReleaseFrameRate = 60;
        private const int MaximumSamples = 90000;
        private const long BytesPerMegabyte = 1024L * 1024L;

        private static double s_LaunchRealtimeSeconds;

        private enum RecorderMode
        {
            Disabled,
            Development,
            Release
        }

        private FrameTiming[] _latestFrameTiming;
        private float[] _frameTimesMs;
        private float[] _cpuFrameTimesMs;
        private float[] _gpuFrameTimesMs;
        private AndroidJavaClass _androidDebugClass;
        private ProfilerRecorder _gcAllocatedRecorder;
        private RecorderMode _mode;
        private string _taskId;
        private string _routeId;
        private string _outputFileName;
        private float _requiredWarmupSeconds;
        private float _requiredCaptureSeconds;
        private double _matchReadyRealtimeSeconds;
        private double _activeWarmupSeconds;
        private double _capturedSeconds;
        private double _nextSlowMetricSeconds;
        private double _batteryStartPercent;
        private double _batteryEndPercent;
        private double _batchesTotal;
        private double _setPassCallsTotal;
        private double _trianglesTotal;
        private double _verticesTotal;
        private long _totalGcAllocatedBytes;
        private long _peakAllocatedMemoryBytes;
        private long _peakMonoMemoryBytes;
        private long _peakResidentSetBytes;
        private int _sampleCount;
        private int _cpuTimingSampleCount;
        private int _gpuTimingSampleCount;
        private int _gcCounterSampleCount;
        private int _renderCounterSampleCount;
        private long _mainThreadAllocatedBytesAtPreviousSample;
        private string _gcAllocationSource;
        private int _collectionCountAtCaptureStart;
        private bool _developmentBuild;
        private bool _scriptDebugging;
        private bool _profilerAttached;
        private bool _profilerMarkersEnabled;
        private bool _matchReady;
        private bool _captureStarted;
        private bool _finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLaunchClock()
        {
            s_LaunchRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
        }

        public bool IsEnabled => _mode != RecorderMode.Disabled;

        public bool SuppressesStandardDiagnostics =>
            IsEnabled && string.Equals(TaskId, Vrp092TaskId, StringComparison.Ordinal);

        public void Initialize(bool profilerMarkersEnabled)
        {
            Initialize(
                Environment.GetCommandLineArgs(),
                Debug.isDebugBuild,
                IsScriptDebuggingEnabled(),
                Profiler.enabled,
                profilerMarkersEnabled);
        }

        public void MarkMatchReady()
        {
            if ((!IsEnabled && !_vrp067DestructionMatrixEnabled &&
                 !_vrp095StateScenarioEnabled) || _matchReady)
                return;

            _matchReady = true;
            _matchReadyRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            string taskId = _vrp095StateScenarioEnabled && !IsEnabled
                ? "VRP-095"
                : _vrp067DestructionMatrixEnabled && !IsEnabled
                    ? "VRP-067"
                    : TaskId;
            LogNoStackTrace(
                $"[{taskId} MatchReady] realtimeMs={(_matchReadyRealtimeSeconds - s_LaunchRealtimeSeconds) * 1000d:F3}");
        }

        public void Sample(
            bool gameplayActive,
            ProfilerRecorder batches,
            ProfilerRecorder setPassCalls,
            ProfilerRecorder triangles,
            ProfilerRecorder vertices)
        {
            if (!IsEnabled || !_matchReady || _finished || !gameplayActive ||
                !Application.isFocused || Time.unscaledDeltaTime <= 0f)
            {
                return;
            }

            float deltaSeconds = Time.unscaledDeltaTime;
            if (_activeWarmupSeconds < _requiredWarmupSeconds)
            {
                _activeWarmupSeconds += deltaSeconds;
                return;
            }

            if (!_captureStarted)
                BeginCapture();

            if (_sampleCount >= MaximumSamples)
            {
                Finish(false, $"sample capacity {MaximumSamples} exceeded before {_requiredCaptureSeconds:F0}s completed");
                return;
            }

            CaptureFrameTiming(out float cpuFrameMs, out float gpuFrameMs);
            _frameTimesMs[_sampleCount] = deltaSeconds * 1000f;
            _cpuFrameTimesMs[_sampleCount] = cpuFrameMs;
            _gpuFrameTimesMs[_sampleCount] = gpuFrameMs;
            if (cpuFrameMs > 0f)
                _cpuTimingSampleCount++;
            if (gpuFrameMs > 0f)
                _gpuTimingSampleCount++;

            if (_mode == RecorderMode.Release)
            {
                _profilerAttached |= Profiler.enabled;
                if (ShouldReadRenderCounters(TaskId))
                {
                    CaptureReleaseFrameMetrics(
                        ReadCounter(batches),
                        ReadCounter(setPassCalls),
                        ReadCounter(triangles),
                        ReadCounter(vertices));
                }
                else
                {
                    CaptureReleaseFrameMetrics(-1L, -1L, -1L, -1L);
                }
            }

            _sampleCount++;
            _capturedSeconds += deltaSeconds;
            bool samplesManagedMemoryEveryFrame = ShouldSampleManagedMemoryEveryFrame(TaskId);
            if (samplesManagedMemoryEveryFrame)
                CaptureManagedMemory();
            if (_mode == RecorderMode.Release && _capturedSeconds >= _nextSlowMetricSeconds)
            {
                if (!samplesManagedMemoryEveryFrame)
                    CaptureManagedMemory();
                if (ShouldCapturePeriodicResidentSet(TaskId))
                    CaptureResidentSet();
                _nextSlowMetricSeconds += SlowMetricIntervalSeconds;
            }

            if (_capturedSeconds >= _requiredCaptureSeconds)
                Finish(true, string.Empty);
        }

        public void Dispose()
        {
            if (IsEnabled && !_finished && _sampleCount > 0)
                Finish(false, "recorder disposed before the sustained capture completed");

            ReleaseBuffers();
            _mode = RecorderMode.Disabled;
        }

        internal static bool TryGetRequestedReleaseFrameRate(
            IReadOnlyList<string> commandLineArguments,
            out int frameRate)
        {
            return TryResolveReleaseConfiguration(
                commandLineArguments,
                out _,
                out frameRate,
                out _,
                out _,
                out _,
                out _);
        }

        private string TaskId => string.IsNullOrWhiteSpace(_taskId)
            ? DevelopmentTaskId
            : _taskId;

        private string OutputFileName => string.IsNullOrWhiteSpace(_outputFileName)
            ? DevelopmentOutputFileName
            : _outputFileName;

        private void Initialize(
            IReadOnlyList<string> commandLineArguments,
            bool isDevelopmentBuild,
            bool scriptDebugging,
            bool profilerAttached,
            bool profilerMarkersEnabled)
        {
            InitializeRenderVirtualizationMetrics(commandLineArguments);
            _mode = ResolveMode(
                commandLineArguments,
                isDevelopmentBuild,
                out _taskId,
                out _routeId,
                out _requiredWarmupSeconds,
                out _requiredCaptureSeconds,
                out _outputFileName);
            if (_mode == RecorderMode.Disabled)
                return;

            _developmentBuild = isDevelopmentBuild;
            _scriptDebugging = scriptDebugging;
            _profilerAttached = profilerAttached;
            _profilerMarkersEnabled = profilerMarkersEnabled;
            _frameTimesMs = new float[MaximumSamples];
            _cpuFrameTimesMs = new float[MaximumSamples];
            _gpuFrameTimesMs = new float[MaximumSamples];
            _latestFrameTiming = new FrameTiming[1];
            _matchReadyRealtimeSeconds = 0d;
            _activeWarmupSeconds = 0d;
            _capturedSeconds = 0d;
            _nextSlowMetricSeconds = 0d;
            _batteryStartPercent = -1d;
            _batteryEndPercent = -1d;
            _batchesTotal = 0d;
            _setPassCallsTotal = 0d;
            _trianglesTotal = 0d;
            _verticesTotal = 0d;
            _totalGcAllocatedBytes = 0L;
            _peakAllocatedMemoryBytes = 0L;
            _peakMonoMemoryBytes = 0L;
            _peakResidentSetBytes = 0L;
            _sampleCount = 0;
            _cpuTimingSampleCount = 0;
            _gpuTimingSampleCount = 0;
            _gcCounterSampleCount = 0;
            _renderCounterSampleCount = 0;
            _mainThreadAllocatedBytesAtPreviousSample = -1L;
            _gcAllocationSource = string.Empty;
            _collectionCountAtCaptureStart = 0;
            _matchReady = false;
            _captureStarted = false;
            _finished = false;
            if (_mode == RecorderMode.Release)
            {
                try
                {
                    _gcAllocatedRecorder = ProfilerRecorder.StartNew(
                        ProfilerCategory.Memory,
                        "GC Allocated In Frame");
                }
                catch
                {
                    _gcAllocatedRecorder = default;
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_mode == RecorderMode.Release)
            {
                try
                {
                    _androidDebugClass = new AndroidJavaClass("android.os.Debug");
                }
                catch
                {
                    _androidDebugClass = null;
                }
            }
#endif
        }

        private static RecorderMode ResolveMode(
            IReadOnlyList<string> commandLineArguments,
            bool isDevelopmentBuild,
            out string taskId,
            out string routeId,
            out float warmupSeconds,
            out float captureSeconds,
            out string outputFileName)
        {
            taskId = string.Empty;
            routeId = string.Empty;
            warmupSeconds = 0f;
            captureSeconds = 0f;
            outputFileName = string.Empty;
            if (!ContainsRequiredFlag(commandLineArguments))
                return RecorderMode.Disabled;

            if (TryResolveReleaseConfiguration(
                    commandLineArguments,
                    out taskId,
                    out _,
                    out routeId,
                    out warmupSeconds,
                    out captureSeconds,
                    out outputFileName))
            {
                return RecorderMode.Release;
            }

            if (!isDevelopmentBuild)
                return RecorderMode.Disabled;

            if (!TryGetArgumentValue(commandLineArguments, GateCommandLineArgument, out string developmentTaskId))
                developmentTaskId = DevelopmentTaskId;
            if (!string.Equals(developmentTaskId, DevelopmentTaskId, StringComparison.OrdinalIgnoreCase))
                return RecorderMode.Disabled;

            taskId = DevelopmentTaskId;
            warmupSeconds = ReleaseWarmupSeconds;
            captureSeconds = ReleaseCaptureSeconds;
            outputFileName = DevelopmentOutputFileName;
            return RecorderMode.Development;
        }

        private static bool TryResolveReleaseConfiguration(
            IReadOnlyList<string> commandLineArguments,
            out string taskId,
            out int frameRate,
            out string routeId,
            out float warmupSeconds,
            out float captureSeconds,
            out string outputFileName)
        {
            taskId = string.Empty;
            frameRate = 0;
            routeId = string.Empty;
            warmupSeconds = 0f;
            captureSeconds = 0f;
            outputFileName = string.Empty;
            if (!TryGetArgumentValue(commandLineArguments, GateCommandLineArgument, out string requestedTaskId) ||
                !TryGetArgumentValue(commandLineArguments, FrameRateCommandLineArgument, out string frameRateText) ||
                !int.TryParse(frameRateText, out frameRate) ||
                frameRate != RequiredReleaseFrameRate)
            {
                frameRate = 0;
                return false;
            }

            if (string.Equals(requestedTaskId, ReleaseTaskId, StringComparison.OrdinalIgnoreCase))
            {
                taskId = ReleaseTaskId;
                warmupSeconds = ReleaseWarmupSeconds;
                captureSeconds = ReleaseCaptureSeconds;
                outputFileName = ReleaseOutputFileName;
                return true;
            }

            if (!string.Equals(requestedTaskId, Vrp092TaskId, StringComparison.OrdinalIgnoreCase) ||
                !TryGetArgumentValue(commandLineArguments, PerformanceRouteCommandLineArgument, out routeId) ||
                !IsSupportedVrp092Route(routeId))
            {
                frameRate = 0;
                routeId = string.Empty;
                return false;
            }

            routeId = routeId.ToLowerInvariant();
            taskId = Vrp092TaskId;
            warmupSeconds = Vrp092WarmupSeconds;
            captureSeconds = Vrp092CaptureSeconds;
            outputFileName = $"vrp092_{routeId.Replace('-', '_')}_android_release_recorder.json";
            return true;
        }

        private static bool IsSupportedVrp092Route(string routeId)
        {
            return string.Equals(routeId, "fixed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "slow-pan", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "fast-pan", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "zoom", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "tactical-follow", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "destruction", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "fullscreen-map", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "steady", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(routeId, "thermal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsRequiredFlag(IReadOnlyList<string> arguments)
        {
            if (arguments == null)
                return false;

            for (int i = 0; i < arguments.Count; i++)
            {
                if (string.Equals(arguments[i], GateCommandLineArgument, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool TryGetArgumentValue(
            IReadOnlyList<string> arguments,
            string argumentName,
            out string value)
        {
            value = string.Empty;
            if (arguments == null)
                return false;

            for (int i = 0; i < arguments.Count; i++)
            {
                if (!string.Equals(arguments[i], argumentName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (i + 1 >= arguments.Count || string.IsNullOrWhiteSpace(arguments[i + 1]) ||
                    arguments[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    return false;
                }

                value = arguments[i + 1];
                return true;
            }

            return false;
        }

        private void BeginCapture()
        {
            _captureStarted = true;
            _collectionCountAtCaptureStart = ReadCollectionCount();
            if (_mode == RecorderMode.Release && !_gcAllocatedRecorder.Valid &&
                string.Equals(TaskId, Vrp092TaskId, StringComparison.Ordinal))
            {
                _mainThreadAllocatedBytesAtPreviousSample =
                    GC.GetAllocatedBytesForCurrentThread();
                _gcAllocationSource = "main-thread-allocated-bytes";
            }
            _batteryStartPercent = ReadBatteryPercent();
            _nextSlowMetricSeconds = SlowMetricIntervalSeconds;
            CaptureManagedMemory();
            CaptureResidentSet();
        }

        private void CaptureReleaseFrameMetrics(
            long batches,
            long setPassCalls,
            long triangles,
            long vertices)
        {
            if (_gcAllocatedRecorder.Valid)
            {
                _totalGcAllocatedBytes += Math.Max(0L, _gcAllocatedRecorder.LastValue);
                _gcCounterSampleCount++;
                _gcAllocationSource = "profiler-recorder";
            }
            else if (_mainThreadAllocatedBytesAtPreviousSample >= 0L &&
                     string.Equals(TaskId, Vrp092TaskId, StringComparison.Ordinal))
            {
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread();
                _totalGcAllocatedBytes += Math.Max(
                    0L,
                    allocatedBytes - _mainThreadAllocatedBytesAtPreviousSample);
                _mainThreadAllocatedBytesAtPreviousSample = allocatedBytes;
                _gcCounterSampleCount++;
            }

            if (batches < 0L || setPassCalls < 0L || triangles < 0L || vertices < 0L)
                return;

            _batchesTotal += batches;
            _setPassCallsTotal += setPassCalls;
            _trianglesTotal += triangles;
            _verticesTotal += vertices;
            _renderCounterSampleCount++;
        }

        private static long ReadCounter(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : -1L;
        }

        private void CaptureFrameTiming(out float cpuFrameMs, out float gpuFrameMs)
        {
            FrameTimingManager.CaptureFrameTimings();
            uint timingCount = FrameTimingManager.GetLatestTimings(1, _latestFrameTiming);
            if (timingCount == 0)
            {
                cpuFrameMs = 0f;
                gpuFrameMs = 0f;
                return;
            }

            FrameTiming timing = _latestFrameTiming[0];
            cpuFrameMs = ResolveCpuMainThreadFrameTime(timing);
            gpuFrameMs = timing.gpuFrameTime > 0d ? (float)timing.gpuFrameTime : 0f;
        }

        private static float ResolveCpuMainThreadFrameTime(FrameTiming timing)
        {
            return timing.cpuMainThreadFrameTime > 0d
                ? (float)timing.cpuMainThreadFrameTime
                : 0f;
        }

        private static bool ShouldCapturePeriodicResidentSet(string taskId)
        {
            // VRP-092 owns frame-time acceptance. Android Debug.getPss is a
            // synchronous process-memory probe, so capture it at the existing
            // start/end boundaries without contaminating the timed frames.
            // VRP-094 retains the separate peak-memory acceptance route.
            return !string.Equals(taskId, Vrp092TaskId, StringComparison.Ordinal);
        }

        private static bool ShouldSampleManagedMemoryEveryFrame(string taskId)
        {
            // VRP-092 owns clean frame-time acceptance, while VRP-094 owns
            // independent peak-memory acceptance. Retain boundary plus 1 Hz
            // managed-memory evidence without adding two profiler queries to
            // every measured VRP-092 frame.
            return !string.Equals(taskId, Vrp092TaskId, StringComparison.Ordinal);
        }

        private static bool ShouldReadRenderCounters(string taskId)
        {
            // The release Samsung player does not expose these counters and
            // VRP-092 explicitly accepts that absence. Avoid four known-empty
            // recorder reads per timed frame; APH-804 remains unchanged.
            return !string.Equals(taskId, Vrp092TaskId, StringComparison.Ordinal);
        }

        private void CaptureManagedMemory()
        {
            _peakAllocatedMemoryBytes = Math.Max(
                _peakAllocatedMemoryBytes,
                Profiler.GetTotalAllocatedMemoryLong());
            _peakMonoMemoryBytes = Math.Max(
                _peakMonoMemoryBytes,
                Profiler.GetMonoUsedSizeLong());
        }

        private void CaptureResidentSet()
        {
            long residentSetBytes = 0L;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_androidDebugClass != null)
            {
                try
                {
                    residentSetBytes = Math.Max(0L, _androidDebugClass.CallStatic<long>("getPss")) * 1024L;
                }
                catch
                {
                    residentSetBytes = 0L;
                }
            }
#else
            residentSetBytes = Profiler.GetTotalReservedMemoryLong();
#endif
            _peakResidentSetBytes = Math.Max(_peakResidentSetBytes, residentSetBytes);
        }

    }
}
