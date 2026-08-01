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
        private const string DevelopmentTaskId = "APH-803";
        private const string ReleaseTaskId = "APH-804";
        private const string OutputDirectoryName = "WarlineCapture/Diagnostics";
        private const string DevelopmentOutputFileName = "aph803_android_development_recorder.json";
        private const string ReleaseOutputFileName = "aph804_android_release_recorder.json";
        private const float WarmupSeconds = 60f;
        private const float CaptureSeconds = 600f;
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
            if ((!IsEnabled && !_vrp067DestructionMatrixEnabled) || _matchReady)
                return;

            _matchReady = true;
            _matchReadyRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            string taskId = _vrp067DestructionMatrixEnabled && !IsEnabled
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
            if (_activeWarmupSeconds < WarmupSeconds)
            {
                _activeWarmupSeconds += deltaSeconds;
                return;
            }

            if (!_captureStarted)
                BeginCapture();

            if (_sampleCount >= MaximumSamples)
            {
                Finish(false, $"sample capacity {MaximumSamples} exceeded before {CaptureSeconds:F0}s completed");
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
                CaptureReleaseFrameMetrics(
                    ReadCounter(batches),
                    ReadCounter(setPassCalls),
                    ReadCounter(triangles),
                    ReadCounter(vertices));
            }

            _sampleCount++;
            _capturedSeconds += deltaSeconds;
            _peakAllocatedMemoryBytes = Math.Max(_peakAllocatedMemoryBytes, Profiler.GetTotalAllocatedMemoryLong());
            _peakMonoMemoryBytes = Math.Max(_peakMonoMemoryBytes, Profiler.GetMonoUsedSizeLong());
            if (_mode == RecorderMode.Release && _capturedSeconds >= _nextSlowMetricSeconds)
            {
                CaptureResidentSet();
                _nextSlowMetricSeconds += SlowMetricIntervalSeconds;
            }

            if (_capturedSeconds >= CaptureSeconds)
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
            frameRate = 0;
            if (!TryGetArgumentValue(commandLineArguments, GateCommandLineArgument, out string taskId) ||
                !string.Equals(taskId, ReleaseTaskId, StringComparison.OrdinalIgnoreCase) ||
                !TryGetArgumentValue(commandLineArguments, FrameRateCommandLineArgument, out string frameRateText) ||
                !int.TryParse(frameRateText, out frameRate) ||
                frameRate != RequiredReleaseFrameRate)
            {
                frameRate = 0;
                return false;
            }

            return true;
        }

        private string TaskId => _mode == RecorderMode.Release ? ReleaseTaskId : DevelopmentTaskId;

        private string OutputFileName =>
            _mode == RecorderMode.Release ? ReleaseOutputFileName : DevelopmentOutputFileName;

        private void Initialize(
            IReadOnlyList<string> commandLineArguments,
            bool isDevelopmentBuild,
            bool scriptDebugging,
            bool profilerAttached,
            bool profilerMarkersEnabled)
        {
            InitializeRenderVirtualizationMetrics(commandLineArguments);
            _mode = ResolveMode(commandLineArguments, isDevelopmentBuild);
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
            bool isDevelopmentBuild)
        {
            if (!ContainsRequiredFlag(commandLineArguments))
                return RecorderMode.Disabled;

            if (TryGetRequestedReleaseFrameRate(commandLineArguments, out _))
                return RecorderMode.Release;

            if (!isDevelopmentBuild)
                return RecorderMode.Disabled;

            if (!TryGetArgumentValue(commandLineArguments, GateCommandLineArgument, out string taskId))
                return RecorderMode.Development;

            return string.Equals(taskId, DevelopmentTaskId, StringComparison.OrdinalIgnoreCase)
                ? RecorderMode.Development
                : RecorderMode.Disabled;
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
            _batteryStartPercent = ReadBatteryPercent();
            _nextSlowMetricSeconds = SlowMetricIntervalSeconds;
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
            cpuFrameMs = timing.cpuFrameTime > 0d ? (float)timing.cpuFrameTime : 0f;
            gpuFrameMs = timing.gpuFrameTime > 0d ? (float)timing.gpuFrameTime : 0f;
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
