using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Game.Editor
{
    public static class ProfilerCaptureSummaryExporter
    {
        private const int MainThreadIndex = 0;
        private const int RenderThreadIndex = 1;
        private const string MainThreadActiveTimeCounterName = "CPU Main Thread Active Time";
        private const string RenderThreadActiveTimeCounterName = "CPU Render Thread Active Time";
        private const string GpuFrameTimeCounterName = "GPU Frame Time";
        private const string GcAllocatedInFrameCounterName = "GC Allocated In Frame";
        private const float MsPerNs = 0.000001f;
        private const float DefaultFrameBudgetMs = 16.6667f;

        private static readonly string[] PriorityNameFragments =
        {
            "GameplayRuntimeUpdate",
            "BuildingPlacementRuntimeTick",
            "Selection",
            "MainMenuPlayUI",
            "MatchHudMinimap",
            "Canvas.",
            "Camera.Render",
            "RenderPipelineManager",
            "Gfx.",
            "WaitForTargetFPS",
            "PlayerLoop",
            "BehaviourUpdate",
            "LateBehaviourUpdate",
            "SimulationSystemGroup",
            "PresentationSystemGroup",
            "Unit",
            "Projectile",
            "Vfx",
            "Pathfinding"
        };

        public static void Export()
        {
            string[] args = Environment.GetCommandLineArgs();
            string capturePath = GetArg(args, "-capturePath");
            if (string.IsNullOrWhiteSpace(capturePath))
                throw new ArgumentException("ProfilerCaptureSummaryExporter requires -capturePath <path>.");

            string reportPath = GetArg(args, "-reportPath");
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                string captureName = Path.GetFileNameWithoutExtension(capturePath);
                reportPath = Path.Combine("Design/AgentReports", $"{DateTime.Now:yyyy-MM-dd}_perf_{captureName}_summary.md");
            }

            float frameBudgetMs = TryParseFloat(GetArg(args, "-frameBudgetMs"), DefaultFrameBudgetMs);
            int maxFrames = TryParseInt(GetArg(args, "-maxFrames"), 0);
            int startFrame = TryParseInt(GetArg(args, "-startFrame"), -1);

            if (!File.Exists(capturePath))
                throw new FileNotFoundException("Profiler capture not found.", capturePath);

            UnityEngine.Debug.Log($"[ProfilerCaptureSummaryExporter] loading capture={capturePath}");
            if (!ProfilerDriver.LoadProfile(capturePath, false))
                throw new InvalidOperationException($"Failed to load profiler capture: {capturePath}");
            UnityEngine.Debug.Log(
                $"[ProfilerCaptureSummaryExporter] loaded capture={capturePath} frames={ProfilerDriver.firstFrameIndex}..{ProfilerDriver.lastFrameIndex}");

            Analysis analysis = Analyze(capturePath, frameBudgetMs, maxFrames, startFrame);
            string report = BuildReport(analysis);
            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(reportPath, report);
            UnityEngine.Debug.Log($"[ProfilerCaptureSummaryExporter] report={Path.GetFullPath(reportPath)} frames={analysis.FrameCount} capture={capturePath}");
            EditorApplication.Exit(0);
        }

        private static Analysis Analyze(string capturePath, float frameBudgetMs, int maxFrames, int startFrame)
        {
            int firstFrame = ProfilerDriver.firstFrameIndex;
            int lastFrame = ProfilerDriver.lastFrameIndex;
            if (startFrame >= firstFrame && startFrame <= lastFrame)
                firstFrame = startFrame;
            if (maxFrames > 0 && lastFrame - firstFrame + 1 > maxFrames)
                lastFrame = firstFrame + maxFrames - 1;

            Analysis analysis = new()
            {
                CapturePath = Path.GetFullPath(capturePath),
                FirstFrame = firstFrame,
                LastFrame = lastFrame,
                FrameBudgetMs = frameBudgetMs
            };

            Dictionary<string, MarkerAggregate> selfMarkers = new(StringComparer.Ordinal);
            Dictionary<string, MarkerAggregate> priorityMarkers = new(StringComparer.Ordinal);
            List<int> stack = new(512);
            List<int> children = new(256);

            for (int frameIndex = firstFrame; frameIndex <= lastFrame; frameIndex++)
            {
                if ((frameIndex - firstFrame) % 120 == 0)
                {
                    UnityEngine.Debug.Log(
                        $"[ProfilerCaptureSummaryExporter] scanning frame={frameIndex} range={firstFrame}..{lastFrame}");
                }

                FrameSummary frame = ReadFrameSummary(frameIndex);
                if (!frame.Valid)
                    continue;

                analysis.Frames.Add(frame);
                ScanPriorityThread(frameIndex, MainThreadIndex, priorityMarkers, stack, children);
                ScanPriorityThread(frameIndex, RenderThreadIndex, priorityMarkers, stack, children);
                CollectTopSelfThread(frameIndex, MainThreadIndex, selfMarkers, children);
                CollectTopSelfThread(frameIndex, RenderThreadIndex, selfMarkers, children);
            }

            analysis.AllMarkers.AddRange(selfMarkers.Values);
            analysis.PriorityMarkers.AddRange(priorityMarkers.Values);
            analysis.AllMarkers.Sort(MarkerAggregate.CompareByTotalDescending);
            analysis.PriorityMarkers.Sort(MarkerAggregate.CompareByTotalDescending);
            analysis.Frames.Sort(FrameSummary.CompareByFrameTimeDescending);
            return analysis;
        }

        private static FrameSummary ReadFrameSummary(int frameIndex)
        {
            using RawFrameDataView mainThread = ProfilerDriver.GetRawFrameDataView(frameIndex, MainThreadIndex);
            if (!mainThread.valid)
                return default;

            ulong mainActiveNs = ReadCounter(mainThread, MainThreadActiveTimeCounterName);
            ulong renderActiveNs = ReadCounter(mainThread, RenderThreadActiveTimeCounterName);
            ulong gpuNs = 0;
            int gpuFrameIndex = frameIndex + 4;
            if (gpuFrameIndex <= ProfilerDriver.lastFrameIndex)
            {
                using RawFrameDataView gpuFrame = ProfilerDriver.GetRawFrameDataView(gpuFrameIndex, MainThreadIndex);
                if (gpuFrame.valid)
                    gpuNs = ReadCounter(gpuFrame, GpuFrameTimeCounterName);
            }

            return new FrameSummary
            {
                Valid = true,
                FrameIndex = frameIndex,
                FrameTimeMs = mainThread.frameTimeMs,
                MainThreadActiveMs = mainActiveNs * MsPerNs,
                RenderThreadActiveMs = renderActiveNs * MsPerNs,
                CpuActiveMs = Math.Max(mainActiveNs, renderActiveNs) * MsPerNs,
                GpuTimeMs = gpuNs * MsPerNs,
                GcBytes = ReadCounter(mainThread, GcAllocatedInFrameCounterName)
            };
        }

        private static ulong ReadCounter(FrameDataView frameData, string counterName)
        {
            int markerId = frameData.GetMarkerId(counterName);
            if (markerId == FrameDataView.invalidMarkerId)
                return 0;

            long value = frameData.GetCounterValueAsLong(markerId);
            return value <= 0 ? 0UL : Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        private static void ScanPriorityThread(
            int frameIndex,
            int threadIndex,
            Dictionary<string, MarkerAggregate> priorityMarkers,
            List<int> stack,
            List<int> children)
        {
            using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex,
                threadIndex,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnTotalTime,
                false);

            if (!view.valid)
                return;

            string threadName = string.IsNullOrWhiteSpace(view.threadName)
                ? $"Thread {threadIndex}"
                : view.threadName;
            int rootId = view.GetRootItemID();
            if (rootId == HierarchyFrameDataView.invalidSampleId)
                return;

            stack.Clear();
            children.Clear();
            view.GetItemChildren(rootId, children);
            for (int i = 0; i < children.Count; i++)
                stack.Add(children[i]);

            while (stack.Count > 0)
            {
                int itemId = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                string name = view.GetItemName(itemId);
                if (IsPriorityMarker(name))
                {
                    AddMarker(
                        priorityMarkers,
                        threadName,
                        name,
                        frameIndex,
                        view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime),
                        view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime),
                        view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnCalls),
                        view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory));
                }

                children.Clear();
                view.GetItemChildren(itemId, children);
                for (int i = 0; i < children.Count; i++)
                    stack.Add(children[i]);
            }
        }

        private static void CollectTopSelfThread(
            int frameIndex,
            int threadIndex,
            Dictionary<string, MarkerAggregate> selfMarkers,
            List<int> children)
        {
            using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex,
                threadIndex,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.InvertHierarchy,
                HierarchyFrameDataView.columnSelfTime,
                false);

            if (!view.valid)
                return;

            string threadName = string.IsNullOrWhiteSpace(view.threadName)
                ? $"Thread {threadIndex}"
                : view.threadName;
            int rootId = view.GetRootItemID();
            if (rootId == HierarchyFrameDataView.invalidSampleId)
                return;

            children.Clear();
            view.GetItemChildren(rootId, children);
            int count = Math.Min(children.Count, 64);
            for (int i = 0; i < count; i++)
            {
                int itemId = children[i];
                AddMarker(
                    selfMarkers,
                    threadName,
                    view.GetItemName(itemId),
                    frameIndex,
                    view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnTotalTime),
                    view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnSelfTime),
                    view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnCalls),
                    view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory));
            }
        }

        private static void AddMarker(
            Dictionary<string, MarkerAggregate> markers,
            string threadName,
            string name,
            int frameIndex,
            double totalMs,
            double selfMs,
            double calls,
            double gcBytes)
        {
            string key = $"{threadName}|{name}";
            if (!markers.TryGetValue(key, out MarkerAggregate marker))
            {
                marker = new MarkerAggregate
                {
                    ThreadName = threadName,
                    Name = name
                };
                markers.Add(key, marker);
            }

            marker.TotalMs += totalMs;
            marker.SelfMs += selfMs;
            marker.Calls += calls;
            marker.GcBytes += gcBytes;
            marker.Samples++;
            if (marker.LastFrameSeen != frameIndex)
            {
                marker.FramesSeen++;
                marker.LastFrameSeen = frameIndex;
            }

            if (totalMs > marker.MaxTotalMs)
            {
                marker.MaxTotalMs = totalMs;
                marker.MaxTotalFrame = frameIndex;
            }

            if (selfMs > marker.MaxSelfMs)
            {
                marker.MaxSelfMs = selfMs;
                marker.MaxSelfFrame = frameIndex;
            }
        }

        private static bool IsPriorityMarker(string name)
        {
            for (int i = 0; i < PriorityNameFragments.Length; i++)
            {
                if (name.IndexOf(PriorityNameFragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string BuildReport(Analysis analysis)
        {
            List<FrameSummary> frameTimeAscending = new(analysis.Frames);
            frameTimeAscending.Sort(FrameSummary.CompareByFrameTimeAscending);
            int frameCount = frameTimeAscending.Count;
            double avgFrameMs = Average(frameTimeAscending, static frame => frame.FrameTimeMs);
            double p50FrameMs = Percentile(frameTimeAscending, 50, static frame => frame.FrameTimeMs);
            double p95FrameMs = Percentile(frameTimeAscending, 95, static frame => frame.FrameTimeMs);
            double p99FrameMs = Percentile(frameTimeAscending, 99, static frame => frame.FrameTimeMs);
            double maxFrameMs = frameCount == 0 ? 0 : frameTimeAscending[^1].FrameTimeMs;
            double avgCpuActiveMs = Average(frameTimeAscending, static frame => frame.CpuActiveMs);
            double p95CpuActiveMs = Percentile(frameTimeAscending, 95, static frame => frame.CpuActiveMs);
            double avgGpuMs = Average(frameTimeAscending, static frame => frame.GpuTimeMs);
            double p95GpuMs = Percentile(frameTimeAscending, 95, static frame => frame.GpuTimeMs);
            ulong totalGcBytes = 0;
            int overBudgetFrames = 0;
            for (int i = 0; i < frameTimeAscending.Count; i++)
            {
                if (frameTimeAscending[i].FrameTimeMs > analysis.FrameBudgetMs)
                    overBudgetFrames++;
                totalGcBytes += frameTimeAscending[i].GcBytes;
            }

            StringBuilder builder = new(32768);
            builder.AppendLine("# Android Profiler Capture Summary");
            builder.AppendLine();
            builder.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            builder.AppendLine($"Capture: `{analysis.CapturePath}`");
            builder.AppendLine($"Profiler frames: `{analysis.FirstFrame}..{analysis.LastFrame}`");
            builder.AppendLine($"Scanned frames: `{frameCount}`");
            builder.AppendLine($"Frame budget: `{analysis.FrameBudgetMs:0.###}ms`");
            builder.AppendLine();
            builder.AppendLine("## Frame Time");
            builder.AppendLine();
            builder.AppendLine("| Metric | Value |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| Avg frame | {avgFrameMs:0.00} ms ({Fps(avgFrameMs):0.0} FPS) |");
            builder.AppendLine($"| P50 frame | {p50FrameMs:0.00} ms ({Fps(p50FrameMs):0.0} FPS) |");
            builder.AppendLine($"| P95 frame | {p95FrameMs:0.00} ms ({Fps(p95FrameMs):0.0} FPS) |");
            builder.AppendLine($"| P99 frame | {p99FrameMs:0.00} ms ({Fps(p99FrameMs):0.0} FPS) |");
            builder.AppendLine($"| Max frame | {maxFrameMs:0.00} ms ({Fps(maxFrameMs):0.0} FPS) |");
            builder.AppendLine($"| Frames over budget | {overBudgetFrames}/{frameCount} |");
            builder.AppendLine($"| Avg CPU active | {avgCpuActiveMs:0.00} ms |");
            builder.AppendLine($"| P95 CPU active | {p95CpuActiveMs:0.00} ms |");
            builder.AppendLine($"| Avg GPU time | {avgGpuMs:0.00} ms |");
            builder.AppendLine($"| P95 GPU time | {p95GpuMs:0.00} ms |");
            builder.AppendLine($"| Total GC allocated | {totalGcBytes} bytes |");
            builder.AppendLine();
            AppendMarkerTable(builder, "Top Priority Markers By Total Time", analysis.PriorityMarkers, 40, frameCount);
            AppendMarkerTable(builder, "Top Main Thread Markers By Self Time", FilterAndSort(analysis.AllMarkers, "Main Thread", bySelf: true), 30, frameCount);
            AppendMarkerTable(builder, "Top Render Thread Markers By Self Time", FilterAndSort(analysis.AllMarkers, "Render Thread", bySelf: true), 20, frameCount);
            AppendSlowFrameTable(builder, analysis.Frames, 12);
            builder.AppendLine("## Notes");
            builder.AppendLine();
            builder.AppendLine("- Marker totals can overlap when a parent marker and its child marker are both listed; use them for ranking, not additive budgeting.");
            builder.AppendLine("- GPU timing uses Unity's frame timing counter, which is reported with Unity's usual delayed frame offset.");
            return builder.ToString();
        }

        private static List<MarkerAggregate> FilterAndSort(List<MarkerAggregate> source, string threadName, bool bySelf)
        {
            List<MarkerAggregate> result = new();
            for (int i = 0; i < source.Count; i++)
            {
                if (string.Equals(source[i].ThreadName, threadName, StringComparison.Ordinal))
                    result.Add(source[i]);
            }

            result.Sort(bySelf ? MarkerAggregate.CompareBySelfDescending : MarkerAggregate.CompareByTotalDescending);
            return result;
        }

        private static void AppendMarkerTable(StringBuilder builder, string title, IReadOnlyList<MarkerAggregate> markers, int limit, int frameCount)
        {
            builder.AppendLine($"## {title}");
            builder.AppendLine();
            builder.AppendLine("| Marker | Thread | Total ms | Avg/frame | Max total | Max frame | Self ms | Max self | Calls | GC bytes |");
            builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            int count = Math.Min(limit, markers.Count);
            for (int i = 0; i < count; i++)
            {
                MarkerAggregate marker = markers[i];
                builder.AppendLine(
                    $"| {Escape(marker.Name)} | {Escape(marker.ThreadName)} | {marker.TotalMs:0.00} | {marker.TotalMs / Math.Max(1, frameCount):0.000} | {marker.MaxTotalMs:0.00} | {marker.MaxTotalFrame} | {marker.SelfMs:0.00} | {marker.MaxSelfMs:0.00} | {marker.Calls:0} | {marker.GcBytes:0} |");
            }

            builder.AppendLine();
        }

        private static void AppendSlowFrameTable(StringBuilder builder, IReadOnlyList<FrameSummary> frames, int limit)
        {
            builder.AppendLine("## Slowest Frames");
            builder.AppendLine();
            builder.AppendLine("| Frame | Frame ms | CPU active | Main active | Render active | GPU | GC bytes |");
            builder.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
            int count = Math.Min(limit, frames.Count);
            for (int i = 0; i < count; i++)
            {
                FrameSummary frame = frames[i];
                builder.AppendLine($"| {frame.FrameIndex} | {frame.FrameTimeMs:0.00} | {frame.CpuActiveMs:0.00} | {frame.MainThreadActiveMs:0.00} | {frame.RenderThreadActiveMs:0.00} | {frame.GpuTimeMs:0.00} | {frame.GcBytes} |");
            }

            builder.AppendLine();
        }

        private static double Average<T>(IReadOnlyList<T> values, Func<T, double> selector)
        {
            if (values.Count == 0)
                return 0;

            double sum = 0;
            for (int i = 0; i < values.Count; i++)
                sum += selector(values[i]);
            return sum / values.Count;
        }

        private static double Percentile<T>(IReadOnlyList<T> sortedValues, double percentile, Func<T, double> selector)
        {
            if (sortedValues.Count == 0)
                return 0;

            int index = (int)Math.Round((sortedValues.Count - 1) * percentile / 100.0, MidpointRounding.AwayFromZero);
            index = Math.Clamp(index, 0, sortedValues.Count - 1);
            return selector(sortedValues[index]);
        }

        private static double Fps(double frameMs)
        {
            return frameMs <= 0 ? 0 : 1000.0 / frameMs;
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|", "\\|");
        }

        private static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return string.Empty;
        }

        private static int TryParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;
        }

        private static float TryParseFloat(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : fallback;
        }

        private sealed class Analysis
        {
            public string CapturePath;
            public int FirstFrame;
            public int LastFrame;
            public float FrameBudgetMs;
            public readonly List<FrameSummary> Frames = new();
            public readonly List<MarkerAggregate> AllMarkers = new();
            public readonly List<MarkerAggregate> PriorityMarkers = new();
            public int FrameCount => Frames.Count;
        }

        private struct FrameSummary
        {
            public bool Valid;
            public int FrameIndex;
            public float FrameTimeMs;
            public float CpuActiveMs;
            public float MainThreadActiveMs;
            public float RenderThreadActiveMs;
            public float GpuTimeMs;
            public ulong GcBytes;

            public static int CompareByFrameTimeAscending(FrameSummary left, FrameSummary right)
            {
                return left.FrameTimeMs.CompareTo(right.FrameTimeMs);
            }

            public static int CompareByFrameTimeDescending(FrameSummary left, FrameSummary right)
            {
                return right.FrameTimeMs.CompareTo(left.FrameTimeMs);
            }
        }

        private sealed class MarkerAggregate
        {
            public string ThreadName;
            public string Name;
            public double TotalMs;
            public double SelfMs;
            public double Calls;
            public double GcBytes;
            public int Samples;
            public int FramesSeen;
            public int LastFrameSeen = -1;
            public double MaxTotalMs;
            public int MaxTotalFrame;
            public double MaxSelfMs;
            public int MaxSelfFrame;

            public static int CompareByTotalDescending(MarkerAggregate left, MarkerAggregate right)
            {
                int compare = right.TotalMs.CompareTo(left.TotalMs);
                return compare != 0 ? compare : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            }

            public static int CompareBySelfDescending(MarkerAggregate left, MarkerAggregate right)
            {
                int compare = right.SelfMs.CompareTo(left.SelfMs);
                return compare != 0 ? compare : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            }
        }
    }
}
