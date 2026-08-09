using System;
using System.IO;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class AndroidPerformanceRecorder
    {
        [Serializable]
        private abstract class ReportBase
        {
            public int schemaVersion = 1;
            public string taskId;
            public bool complete;
            public string failure;
            public double launchRealtimeSeconds;
            public double matchReadyRealtimeSeconds;
            public double processToMatchReadyMs;
            public int cpuTimingSampleCount;
            public int gpuTimingSampleCount;
        }

        [Serializable]
        private sealed class DevelopmentReport : ReportBase
        {
            public DevelopmentSustainedRun sustainedRun;
        }

        [Serializable]
        private sealed class ReleaseReport : ReportBase
        {
            public string recorderMode = "release-performance-evidence";
            public string routeId;
            public string buildType;
            public bool developmentBuild;
            public bool scriptDebugging;
            public bool profilerAttached;
            public bool profilerMarkersEnabled;
            public ReleaseSustainedRun sustainedRun;
        }

        [Serializable]
        private sealed class DevelopmentSustainedRun
        {
            public string source = "structured-per-frame-recorder";
            public bool startupFramesExcluded = true;
            public double warmupSeconds;
            public double sampleDurationSeconds;
            public float[] frameTimesMs;
            public double averageFrameMs;
            public double p95FrameMs;
            public double p99FrameMs;
            public double maximumFrameMs;
            public double p95CpuFrameMs;
            public double p95GpuFrameMs;
            public double peakAllocatedMemoryMB;
            public double peakMonoMemoryMB;
        }

        [Serializable]
        private sealed class ReleaseSustainedRun
        {
            public string source = "structured-per-frame-recorder";
            public bool startupFramesExcluded = true;
            public double warmupSeconds;
            public double sampleDurationSeconds;
            public float[] frameTimesMs;
            public double averageFrameMs;
            public double p95FrameMs;
            public double p99FrameMs;
            public double maximumFrameMs;
            public GcMetrics gc;
            public MemoryMetrics memory;
            public BatteryMetrics battery;
            public CounterMetrics counters;
        }

        [Serializable]
        private sealed class GcMetrics
        {
            public string allocationSource;
            public long totalAllocatedBytes;
            public double averageAllocatedBytesPerFrame;
            public int collectionCount;
        }

        [Serializable]
        private sealed class MemoryMetrics
        {
            public double peakAllocatedMemoryMB;
            public double peakMonoMemoryMB;
            public double peakResidentSetMB;
        }

        [Serializable]
        private sealed class BatteryMetrics
        {
            public double startPercent;
            public double endPercent;
            public double drainPercent;
        }

        [Serializable]
        private sealed class CounterMetrics
        {
            public int cpuTimingSampleCount;
            public int gpuTimingSampleCount;
            public double averageCpuFrameMs;
            public double p95CpuFrameMs;
            public double averageGpuFrameMs;
            public double p95GpuFrameMs;
            public double averageBatches;
            public double averageSetPassCalls;
            public double averageTriangles;
            public double averageVertices;
        }

        private void Finish(bool complete, string failure)
        {
            if (_finished)
                return;

            _finished = true;
            if (_mode == RecorderMode.Release)
            {
                _batteryEndPercent = ReadBatteryPercent();
                CaptureManagedMemory();
                CaptureResidentSet();
                if (complete && !TryValidateReleaseCapture(out string validationFailure))
                {
                    complete = false;
                    failure = validationFailure;
                }
            }

            try
            {
                object report = _mode == RecorderMode.Release
                    ? BuildReleaseReport(complete, failure)
                    : BuildDevelopmentReport(complete, failure);
                string directory = Path.Combine(Application.persistentDataPath, OutputDirectoryName);
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, OutputFileName);
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                LogNoStackTrace(
                    $"[{TaskId} Recorder] complete={(complete ? 1 : 0)} samples={_sampleCount} duration={_capturedSeconds:F3}s path={path}");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{TaskId} Recorder] failed to write evidence: {exception.GetType().Name}: {exception.Message}");
            }
        }

        private bool TryValidateReleaseCapture(out string failure)
        {
            if (_developmentBuild || _scriptDebugging || _profilerAttached || _profilerMarkersEnabled)
            {
                failure = "release recorder provenance is not clean";
                return false;
            }

            if (_cpuTimingSampleCount <= 0 || _gpuTimingSampleCount <= 0)
            {
                failure = "release recorder did not receive CPU and GPU frame timings";
                return false;
            }

            if (_gcCounterSampleCount <= 0 ||
                (!string.Equals(TaskId, Vrp092TaskId, StringComparison.Ordinal) &&
                 _renderCounterSampleCount <= 0))
            {
                failure = "release recorder required profiler counters were unavailable";
                return false;
            }

            if (_batteryStartPercent < 0d || _batteryEndPercent < 0d ||
                _batteryEndPercent > _batteryStartPercent)
            {
                failure = "release recorder battery measurement was unavailable or increased during capture";
                return false;
            }

            if (_peakResidentSetBytes <= 0L)
            {
                failure = "release recorder resident-set measurement was unavailable";
                return false;
            }

            failure = string.Empty;
            return true;
        }

        private DevelopmentReport BuildDevelopmentReport(bool complete, string failure)
        {
            float[] frameTimes = CopySamples(_frameTimesMs, _sampleCount);
            float[] cpuTimes = CopySamples(_cpuFrameTimesMs, _sampleCount);
            float[] gpuTimes = CopySamples(_gpuFrameTimesMs, _sampleCount);
            DevelopmentReport report = PopulateCommonReport(new DevelopmentReport(), complete, failure);
            report.sustainedRun = new DevelopmentSustainedRun
            {
                warmupSeconds = _activeWarmupSeconds,
                sampleDurationSeconds = _capturedSeconds,
                frameTimesMs = frameTimes,
                averageFrameMs = Average(frameTimes, _sampleCount),
                p95FrameMs = Percentile(frameTimes, _sampleCount, 95d),
                p99FrameMs = Percentile(frameTimes, _sampleCount, 99d),
                maximumFrameMs = Maximum(frameTimes, _sampleCount),
                p95CpuFrameMs = PercentilePositive(cpuTimes, _sampleCount, _cpuTimingSampleCount, 95d),
                p95GpuFrameMs = PercentilePositive(gpuTimes, _sampleCount, _gpuTimingSampleCount, 95d),
                peakAllocatedMemoryMB = BytesToMegabytes(_peakAllocatedMemoryBytes),
                peakMonoMemoryMB = BytesToMegabytes(_peakMonoMemoryBytes)
            };
            return report;
        }

        private ReleaseReport BuildReleaseReport(bool complete, string failure)
        {
            float[] frameTimes = CopySamples(_frameTimesMs, _sampleCount);
            float[] cpuTimes = CopySamples(_cpuFrameTimesMs, _sampleCount);
            float[] gpuTimes = CopySamples(_gpuFrameTimesMs, _sampleCount);
            ReleaseReport report = PopulateCommonReport(new ReleaseReport(), complete, failure);
            report.buildType = _developmentBuild ? "development" : "release";
            report.routeId = _routeId ?? string.Empty;
            report.developmentBuild = _developmentBuild;
            report.scriptDebugging = _scriptDebugging;
            report.profilerAttached = _profilerAttached;
            report.profilerMarkersEnabled = _profilerMarkersEnabled;
            report.sustainedRun = new ReleaseSustainedRun
            {
                warmupSeconds = _activeWarmupSeconds,
                sampleDurationSeconds = _capturedSeconds,
                frameTimesMs = frameTimes,
                averageFrameMs = Average(frameTimes, _sampleCount),
                p95FrameMs = Percentile(frameTimes, _sampleCount, 95d),
                p99FrameMs = Percentile(frameTimes, _sampleCount, 99d),
                maximumFrameMs = Maximum(frameTimes, _sampleCount),
                gc = new GcMetrics
                {
                    allocationSource = _gcAllocationSource ?? string.Empty,
                    totalAllocatedBytes = _totalGcAllocatedBytes,
                    averageAllocatedBytesPerFrame = _sampleCount > 0
                        ? _totalGcAllocatedBytes / (double)_sampleCount
                        : 0d,
                    collectionCount = Math.Max(0, ReadCollectionCount() - _collectionCountAtCaptureStart)
                },
                memory = new MemoryMetrics
                {
                    peakAllocatedMemoryMB = BytesToMegabytes(_peakAllocatedMemoryBytes),
                    peakMonoMemoryMB = BytesToMegabytes(_peakMonoMemoryBytes),
                    peakResidentSetMB = BytesToMegabytes(_peakResidentSetBytes)
                },
                battery = new BatteryMetrics
                {
                    startPercent = Math.Max(0d, _batteryStartPercent),
                    endPercent = Math.Max(0d, _batteryEndPercent),
                    drainPercent = Math.Max(0d, _batteryStartPercent - _batteryEndPercent)
                },
                counters = new CounterMetrics
                {
                    cpuTimingSampleCount = _cpuTimingSampleCount,
                    gpuTimingSampleCount = _gpuTimingSampleCount,
                    averageCpuFrameMs = AveragePositive(cpuTimes, _sampleCount, _cpuTimingSampleCount),
                    p95CpuFrameMs = PercentilePositive(cpuTimes, _sampleCount, _cpuTimingSampleCount, 95d),
                    averageGpuFrameMs = AveragePositive(gpuTimes, _sampleCount, _gpuTimingSampleCount),
                    p95GpuFrameMs = PercentilePositive(gpuTimes, _sampleCount, _gpuTimingSampleCount, 95d),
                    averageBatches = AverageCounter(_batchesTotal),
                    averageSetPassCalls = AverageCounter(_setPassCallsTotal),
                    averageTriangles = AverageCounter(_trianglesTotal),
                    averageVertices = AverageCounter(_verticesTotal)
                }
            };
            return report;
        }

        private T PopulateCommonReport<T>(T report, bool complete, string failure)
            where T : ReportBase
        {
            report.taskId = TaskId;
            report.complete = complete;
            report.failure = failure ?? string.Empty;
            report.launchRealtimeSeconds = s_LaunchRealtimeSeconds;
            report.matchReadyRealtimeSeconds = _matchReadyRealtimeSeconds;
            report.processToMatchReadyMs =
                Math.Max(0d, (_matchReadyRealtimeSeconds - s_LaunchRealtimeSeconds) * 1000d);
            report.cpuTimingSampleCount = _cpuTimingSampleCount;
            report.gpuTimingSampleCount = _gpuTimingSampleCount;
            return report;
        }

        private double AverageCounter(double total)
        {
            return _renderCounterSampleCount > 0 ? total / _renderCounterSampleCount : 0d;
        }

        private static double ReadBatteryPercent()
        {
            float level = SystemInfo.batteryLevel;
            return level < 0f ? -1d : Math.Clamp(level * 100d, 0d, 100d);
        }

        private static int ReadCollectionCount()
        {
            return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        private static bool IsScriptDebuggingEnabled()
        {
#if ENABLE_SCRIPTING_DEBUGGER
            return true;
#else
            return false;
#endif
        }

        private static double BytesToMegabytes(long bytes)
        {
            return bytes / (double)BytesPerMegabyte;
        }

        private static float[] CopySamples(float[] samples, int count)
        {
            if (samples == null || count <= 0)
                return Array.Empty<float>();

            float[] copy = new float[count];
            Array.Copy(samples, copy, count);
            return copy;
        }

        private static double Average(float[] samples, int count)
        {
            if (samples == null || count <= 0)
                return 0d;

            double total = 0d;
            for (int i = 0; i < count; i++)
                total += samples[i];
            return total / count;
        }

        private static double AveragePositive(float[] samples, int count, int positiveCount)
        {
            if (samples == null || count <= 0 || positiveCount <= 0)
                return 0d;

            double total = 0d;
            for (int i = 0; i < count; i++)
            {
                if (samples[i] > 0f)
                    total += samples[i];
            }

            return total / positiveCount;
        }

        private static double Maximum(float[] samples, int count)
        {
            double maximum = 0d;
            for (int i = 0; samples != null && i < count; i++)
                maximum = Math.Max(maximum, samples[i]);
            return maximum;
        }

        private static double Percentile(float[] samples, int count, double percentile)
        {
            if (samples == null || count <= 0)
                return 0d;

            float[] ordered = CopySamples(samples, count);
            Array.Sort(ordered);
            int index = (int)Math.Floor(((count - 1) * percentile / 100d) + 0.5d);
            return ordered[Math.Clamp(index, 0, count - 1)];
        }

        private static double PercentilePositive(
            float[] samples,
            int count,
            int positiveCount,
            double percentile)
        {
            if (samples == null || count <= 0 || positiveCount <= 0)
                return 0d;

            float[] positive = new float[positiveCount];
            int writeIndex = 0;
            for (int i = 0; i < count && writeIndex < positive.Length; i++)
            {
                if (samples[i] > 0f)
                    positive[writeIndex++] = samples[i];
            }

            return Percentile(positive, writeIndex, percentile);
        }

        private void ReleaseBuffers()
        {
            _frameTimesMs = null;
            _cpuFrameTimesMs = null;
            _gpuFrameTimesMs = null;
            _latestFrameTiming = null;
            if (_gcAllocatedRecorder.Valid)
                _gcAllocatedRecorder.Dispose();
            _gcAllocatedRecorder = default;
            _androidDebugClass?.Dispose();
            _androidDebugClass = null;
        }

        private static void LogNoStackTrace(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        }
    }
}
