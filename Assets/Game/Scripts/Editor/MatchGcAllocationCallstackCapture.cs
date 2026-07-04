using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;
using Game.Composition;

namespace Game.Editor
{
    #if ENABLE_PROFILER && UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Profiling;
    using UnityEditor;
    using UnityEditor.Profiling;
    using UnityEditor.SceneManagement;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Profiling;
    using UnityEngine.SceneManagement;
    using Unity.Transforms;

    public static class MatchGcAllocationCallstackCapture
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MatchSceneName = "Match";
        private const string MatchHudContentName = "SCN08_MatchHudContent";
        private const string SteadyStateReportPath = "Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md";
        private const string BattleReportPath = "Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture-battle.md";
        private const string ProfilerLogPrefix = "/private/tmp/warline-match-gc-callstack-capture";
        private const int CaptureFrameCount = 300;
        private const int WarmupFrameCount = 180;
        private const int BattleCaptureMaxAttackers = 64;
        private const int BattleCaptureTargetHealth = 1_000_000_000;
        private const int BattleVfxPrewarmCount = 64;
        private const int TopSiteCount = 15;
        private const double TimeoutSeconds = 360d;

        private const string ActiveKey = "MatchGcAllocationCallstackCapture.Active";
        private const string PhaseKey = "MatchGcAllocationCallstackCapture.Phase";
        private const string CaptureModeKey = "MatchGcAllocationCallstackCapture.CaptureMode";
        private const string StartedAtKey = "MatchGcAllocationCallstackCapture.StartedAt";
        private const string ErrorCountKey = "MatchGcAllocationCallstackCapture.ErrorCount";
        private const string CaptureStartFrameKey = "MatchGcAllocationCallstackCapture.CaptureStartFrame";
        private const string WarmupStartFrameKey = "MatchGcAllocationCallstackCapture.WarmupStartFrame";
        private const string ProfilerWasEnabledKey = "MatchGcAllocationCallstackCapture.ProfilerWasEnabled";
        private const string ProfilerAllocationCallstacksWasEnabledKey = "MatchGcAllocationCallstackCapture.AllocationCallstacksWasEnabled";
        private const string ProfilerBinaryLogWasEnabledKey = "MatchGcAllocationCallstackCapture.BinaryLogWasEnabled";
        private const string ProfilerLogFileKey = "MatchGcAllocationCallstackCapture.ProfilerLogFile";
        private const string ProfilerDeepProfilingWasEnabledKey = "MatchGcAllocationCallstackCapture.DeepProfilingWasEnabled";
        private const string ScriptsCategoryWasEnabledKey = "MatchGcAllocationCallstackCapture.ScriptsCategoryWasEnabled";
        private const string MemoryCategoryWasEnabledKey = "MatchGcAllocationCallstackCapture.MemoryCategoryWasEnabled";
        private const string WarningStackTraceLogTypeKey = "MatchGcAllocationCallstackCapture.WarningStackTraceLogType";
        private const string ProfilerStateStoredKey = "MatchGcAllocationCallstackCapture.ProfilerStateStored";
        private const string EditorLiveConversionDisabledCountKey = "MatchGcAllocationCallstackCapture.EditorLiveConversionDisabledCount";

        private static bool hasPendingBatchExit;
        private static int pendingBatchExitCode;

        private enum Phase
        {
            Idle = 0,
            WaitingForPlayMode = 1,
            WaitingForShellReady = 2,
            WaitingForMatchReady = 3,
            PreparingBattle = 4,
            WarmingUp = 5,
            Capturing = 6
        }

        private enum CaptureMode
        {
            SteadyState = 0,
            Battle = 1
        }

        private sealed class AllocationSite
        {
            public string Key = string.Empty;
            public string SampleName = string.Empty;
            public string ThreadName = string.Empty;
            public string Callstack = string.Empty;
            public string HierarchyPath = string.Empty;
            public long Bytes;
            public int Samples;
            public int Frames;
            public int LastFrameIndex = -1;
        }

        private struct FrameAllocationSummary
        {
            public int FrameIndex;
            public long Bytes;
            public int Samples;
        }

        [InitializeOnLoadMethod]
        private static void ResumeActiveCapture()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            RegisterCallbacks();
        }

        public static void RunSteadyState()
        {
            Run(CaptureMode.SteadyState);
        }

        public static void RunBattleState()
        {
            Run(CaptureMode.Battle);
        }

        private static void Run(CaptureMode mode)
        {
            try
            {
                ResetState();
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
                SessionState.SetInt(CaptureModeKey, (int)mode);
                SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(ErrorCountKey, 0);
                RegisterCallbacks();
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, exception.Message);
            }
        }

        private static void RegisterCallbacks()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForShellReady);
        }

        private static void Update()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > TimeoutSeconds)
            {
                IsMatchRuntimeReady(out string timeoutStatus);
                Finish(false, $"Timed out. phase={(Phase)SessionState.GetInt(PhaseKey, 0)} status={timeoutStatus}");
                return;
            }

            Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
            if (phase == Phase.WaitingForPlayMode)
            {
                EnsurePlayModeRequested();
                return;
            }

            if (phase == Phase.WaitingForShellReady)
            {
                if (!TryGetShellState(out UiShellStateComponent shellState) ||
                    shellState.CurrentMode != UiShellMode.MainMenu ||
                    shellState.ActiveRoute != UIRoute.MainMenu ||
                    shellState.IsTransitionRunning != 0)
                {
                    return;
                }

                if (!TryEnqueueMatchRoute(out string enqueueError))
                {
                    Finish(false, enqueueError);
                    return;
                }

                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMatchReady);
                return;
            }

            if (phase == Phase.WaitingForMatchReady)
            {
                if (!IsMatchRuntimeReady(out string status))
                    return;

                int errorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (errorCount > 0)
                {
                    Finish(false, $"Match reached ready state but logged {errorCount} runtime error(s). status={status}");
                    return;
                }

                if (GetCaptureMode() == CaptureMode.Battle)
                {
                    SessionState.SetInt(PhaseKey, (int)Phase.PreparingBattle);
                    return;
                }

                BeginWarmup();
                return;
            }

            if (phase == Phase.PreparingBattle)
            {
                if (!TryPrepareBattleCapture(out string battleStatus))
                {
                    Finish(false, $"Battle capture setup failed. {battleStatus}");
                    return;
                }

                int prewarmedVfxPrefabCount = PrewarmBattleVfxPools();
                Debug.Log($"[MatchGcAllocationCallstackCapture] battlePrepared {battleStatus} prewarmedVfxPrefabs={prewarmedVfxPrefabCount}");
                BeginWarmup();
                return;
            }

            if (phase == Phase.WarmingUp)
            {
                int warmupStartFrame = SessionState.GetInt(WarmupStartFrameKey, Time.frameCount);
                if (Time.frameCount - warmupStartFrame < WarmupFrameCount)
                    return;

                StartProfilerCapture();
                SessionState.SetInt(PhaseKey, (int)Phase.Capturing);
                return;
            }

            if (phase != Phase.Capturing)
                return;

            int startFrame = SessionState.GetInt(CaptureStartFrameKey, Time.frameCount);
            if (Time.frameCount - startFrame < CaptureFrameCount)
                return;

            StopProfilerCapture();
            string loadStatus = LoadRawProfileForAnalysis();
            string report = BuildReport(loadStatus);
            WriteReport(report);
            if (!TryValidateRuntimeAllocationProbes(out string allocationProbeStatus))
            {
                Finish(false, $"[MatchGcAllocationCallstackCapture] result=Failed frames={CaptureFrameCount} report={ReportPath} raw={ProfilerRawPath} {allocationProbeStatus}");
                return;
            }

            Finish(true, $"[MatchGcAllocationCallstackCapture] result=Passed frames={CaptureFrameCount} report={ReportPath} raw={ProfilerRawPath}");
        }

        private static void StartProfilerCapture()
        {
            StoreProfilerStateIfNeeded();
            Profiler.enabled = false;

            if (File.Exists(ProfilerRawPath))
                File.Delete(ProfilerRawPath);

            Debug.Log($"[MatchGcAllocationCallstackCapture] captureStarted frames={CaptureFrameCount} raw={ProfilerRawPath}");
            ProfilerDriver.ClearAllFrames();
            ResetRuntimeAllocationProbes();
            ProfilerDriver.deepProfiling = false;
            Profiler.logFile = ProfilerLogPath;
            Profiler.enableBinaryLog = true;
            Profiler.enableAllocationCallstacks = true;
            Profiler.SetCategoryEnabled(ProfilerCategory.Scripts, true);
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory, true);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Profiler.enabled = true;
            SessionState.SetInt(CaptureStartFrameKey, Time.frameCount);
        }

        private static void BeginWarmup()
        {
            int disabledLiveConversionSystems = DisableEditorLiveConversionSystems();
            SessionState.SetInt(EditorLiveConversionDisabledCountKey, disabledLiveConversionSystems);
            EnableProfilerWarmup();
            SessionState.SetInt(WarmupStartFrameKey, Time.frameCount);
            SessionState.SetInt(PhaseKey, (int)Phase.WarmingUp);
            Debug.Log($"[MatchGcAllocationCallstackCapture] warmupStarted frames={WarmupFrameCount} disabledEditorLiveConversionSystems={disabledLiveConversionSystems}");
        }

        private static void EnableProfilerWarmup()
        {
            StoreProfilerStateIfNeeded();
            ProfilerDriver.deepProfiling = false;
            Profiler.logFile = string.Empty;
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = true;
            Profiler.SetCategoryEnabled(ProfilerCategory.Scripts, true);
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory, true);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Profiler.enabled = true;
        }

        private static void StoreProfilerStateIfNeeded()
        {
            if (SessionState.GetBool(ProfilerStateStoredKey, false))
                return;

            SessionState.SetBool(ProfilerStateStoredKey, true);
            SessionState.SetBool(ProfilerWasEnabledKey, Profiler.enabled);
            SessionState.SetBool(ProfilerAllocationCallstacksWasEnabledKey, Profiler.enableAllocationCallstacks);
            SessionState.SetBool(ProfilerBinaryLogWasEnabledKey, Profiler.enableBinaryLog);
            SessionState.SetString(ProfilerLogFileKey, Profiler.logFile ?? string.Empty);
            SessionState.SetBool(ProfilerDeepProfilingWasEnabledKey, ProfilerDriver.deepProfiling);
            SessionState.SetBool(ScriptsCategoryWasEnabledKey, Profiler.IsCategoryEnabled(ProfilerCategory.Scripts));
            SessionState.SetBool(MemoryCategoryWasEnabledKey, Profiler.IsCategoryEnabled(ProfilerCategory.Memory));
            SessionState.SetInt(WarningStackTraceLogTypeKey, (int)Application.GetStackTraceLogType(LogType.Warning));
        }

        private static void StopProfilerCapture()
        {
            Profiler.enabled = SessionState.GetBool(ProfilerWasEnabledKey, false);
            Profiler.enableAllocationCallstacks = SessionState.GetBool(ProfilerAllocationCallstacksWasEnabledKey, false);
            Profiler.enableBinaryLog = SessionState.GetBool(ProfilerBinaryLogWasEnabledKey, false);
            Profiler.logFile = SessionState.GetString(ProfilerLogFileKey, string.Empty);
            ProfilerDriver.deepProfiling = SessionState.GetBool(ProfilerDeepProfilingWasEnabledKey, false);
            Profiler.SetCategoryEnabled(ProfilerCategory.Scripts, SessionState.GetBool(ScriptsCategoryWasEnabledKey, true));
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory, SessionState.GetBool(MemoryCategoryWasEnabledKey, true));
            Application.SetStackTraceLogType(
                LogType.Warning,
                (StackTraceLogType)SessionState.GetInt(WarningStackTraceLogTypeKey, (int)StackTraceLogType.ScriptOnly));
        }

        private static string LoadRawProfileForAnalysis()
        {
            if (!File.Exists(ProfilerRawPath))
                return $"rawMissing path={ProfilerRawPath}";

            bool loaded = ProfilerDriver.LoadProfile(ProfilerRawPath, false);
            return loaded
                ? $"rawLoaded path={ProfilerRawPath}"
                : $"rawLoadFailed path={ProfilerRawPath}";
        }

        private static string BuildReport(string loadStatus)
        {
            CaptureMode mode = GetCaptureMode();
            Dictionary<string, AllocationSite> sites = new(StringComparer.Ordinal);
            Dictionary<int, FrameAllocationSummary> frameSummaries = new();
            int firstFrame = ProfilerDriver.firstFrameIndex;
            int lastFrame = ProfilerDriver.lastFrameIndex;
            int scannedFrames = 0;
            int scannedThreads = 0;

            for (int frameIndex = firstFrame; frameIndex <= lastFrame; frameIndex++)
            {
                bool frameHadData = false;
                for (int threadIndex = 0; ; threadIndex++)
                {
                    using HierarchyFrameDataView frame = ProfilerDriver.GetHierarchyFrameDataView(
                        frameIndex,
                        threadIndex,
                        HierarchyFrameDataView.ViewModes.Default,
                        HierarchyFrameDataView.columnGcMemory,
                        false);
                    if (!frame.valid)
                        break;

                    scannedThreads++;
                    frameHadData = true;
                    ScanHierarchyFrame(frame, sites, frameSummaries);
                }

                if (frameHadData)
                    scannedFrames++;
            }

            List<AllocationSite> rankedSites = new(sites.Values);
            rankedSites.Sort(static (left, right) =>
            {
                int bytesCompare = right.Bytes.CompareTo(left.Bytes);
                if (bytesCompare != 0)
                    return bytesCompare;
                return right.Samples.CompareTo(left.Samples);
            });

            List<FrameAllocationSummary> rankedFrames = new(frameSummaries.Values);
            rankedFrames.Sort(static (left, right) =>
            {
                int bytesCompare = right.Bytes.CompareTo(left.Bytes);
                if (bytesCompare != 0)
                    return bytesCompare;
                return right.Samples.CompareTo(left.Samples);
            });

            long totalBytes = 0;
            int totalSamples = 0;
            for (int i = 0; i < rankedFrames.Count; i++)
            {
                totalBytes += rankedFrames[i].Bytes;
                totalSamples += rankedFrames[i].Samples;
            }

            long editorToolingBytes = 0;
            int editorToolingSamples = 0;
            long playerRelevantBytes = 0;
            int playerRelevantSamples = 0;
            List<AllocationSite> playerRelevantSites = new(rankedSites.Count);
            List<AllocationSite> editorToolingSites = new(rankedSites.Count);
            for (int i = 0; i < rankedSites.Count; i++)
            {
                AllocationSite site = rankedSites[i];
                if (IsEditorToolingAllocation(site))
                {
                    editorToolingBytes += site.Bytes;
                    editorToolingSamples += site.Samples;
                    editorToolingSites.Add(site);
                    continue;
                }

                playerRelevantBytes += site.Bytes;
                playerRelevantSamples += site.Samples;
                playerRelevantSites.Add(site);
            }

            StringBuilder builder = new(16384);
            builder.AppendLine("# Match GC Allocation Call-Stack Capture");
            builder.AppendLine();
            builder.AppendLine($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine("Lane: Gameplay/Performance");
            builder.AppendLine($"Capture type: automated Match {DescribeCaptureMode(mode)} after Menu -> Match route");
            builder.AppendLine();
            builder.AppendLine("## Capture Summary");
            builder.AppendLine();
            builder.AppendLine($"- Requested frames: {CaptureFrameCount}");
            builder.AppendLine($"- Warm-up frames before capture: {WarmupFrameCount}");
            builder.AppendLine($"- Profiler frame range: {firstFrame}..{lastFrame}");
            builder.AppendLine($"- Scanned frames with data: {scannedFrames}");
            builder.AppendLine($"- Scanned thread views: {scannedThreads}");
            builder.AppendLine($"- GC.Alloc samples: {totalSamples}");
            builder.AppendLine($"- GC.Alloc bytes from hierarchy column: {totalBytes}");
            builder.AppendLine($"- GC.Alloc samples excluding editor/tooling rows: {playerRelevantSamples}");
            builder.AppendLine($"- GC.Alloc bytes excluding editor/tooling rows: {playerRelevantBytes}");
            builder.AppendLine($"- Editor/tooling GC.Alloc samples excluded from player-relevant rows: {editorToolingSamples}");
            builder.AppendLine($"- Editor/tooling GC.Alloc bytes excluded from player-relevant rows: {editorToolingBytes}");
            builder.AppendLine($"- Raw load status: `{loadStatus}`");
            builder.AppendLine($"- Raw capture: `{ProfilerRawPath}`");
            builder.AppendLine($"- Editor live conversion systems disabled before warmup: {SessionState.GetInt(EditorLiveConversionDisabledCountKey, 0)}");
            AppendRuntimeAllocationProbeSummary(builder);
            builder.AppendLine();
            builder.AppendLine("## Top Allocation Sites Excluding Editor/Tooling Rows");
            builder.AppendLine();
            AppendAllocationSiteTable(builder, playerRelevantSites);
            builder.AppendLine();
            builder.AppendLine("## Top Editor/Tooling Allocation Sites");
            builder.AppendLine();
            AppendAllocationSiteTable(builder, editorToolingSites);
            builder.AppendLine();
            builder.AppendLine("## Top Allocation Sites (Raw)");
            builder.AppendLine();
            AppendAllocationSiteTable(builder, rankedSites);

            builder.AppendLine();
            builder.AppendLine("## Highest Allocation Frames");
            builder.AppendLine();
            builder.AppendLine("| Rank | Profiler frame | Bytes | Samples |");
            builder.AppendLine("| ---: | ---: | ---: | ---: |");
            int frameLimit = Math.Min(10, rankedFrames.Count);
            for (int i = 0; i < frameLimit; i++)
            {
                FrameAllocationSummary summary = rankedFrames[i];
                builder.Append("| ")
                    .Append(i + 1)
                    .Append(" | ")
                    .Append(summary.FrameIndex)
                    .Append(" | ")
                    .Append(summary.Bytes)
                    .Append(" | ")
                    .Append(summary.Samples)
                    .AppendLine(" |");
            }

            if (frameLimit == 0)
                builder.AppendLine("| 0 | n/a | 0 | 0 |");

            builder.AppendLine();
            builder.AppendLine("## Call Stacks");
            builder.AppendLine();
            int limit = Math.Min(TopSiteCount, rankedSites.Count);
            for (int i = 0; i < limit; i++)
            {
                AllocationSite site = rankedSites[i];
                builder.AppendLine($"### {i + 1}. {GetTopManagedFrame(site.Callstack)}");
                builder.AppendLine();
                builder.AppendLine($"Bytes: {site.Bytes}");
                builder.AppendLine($"Samples: {site.Samples}");
                builder.AppendLine($"Frames: {site.Frames}");
                builder.AppendLine($"Thread: {site.ThreadName}");
                builder.AppendLine($"Hierarchy path: {site.HierarchyPath}");
                builder.AppendLine();
                builder.AppendLine("```");
                builder.AppendLine(string.IsNullOrWhiteSpace(site.Callstack) ? "(no managed call stack captured)" : site.Callstack);
                builder.AppendLine("```");
                builder.AppendLine();
            }

            builder.AppendLine("## Coverage Notes");
            builder.AppendLine();
            if (mode == CaptureMode.Battle)
                builder.AppendLine("- This automated pass covers a deterministic Match battle state seeded after the shell completes the Menu -> Match transition.");
            else
                builder.AppendLine("- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.");
            builder.AppendLine("- Spike-frame call stacks still require an interactive Profiler capture with Call Stacks -> GC.Alloc enabled unless a deterministic spike driver is added.");
            builder.AppendLine("- Editor/tooling rows include Burst compiler threads plus Unity AI/MCP/Tracing frames. Do not treat those raw rows as gameplay work unless they also appear in the player-relevant table.");
            builder.AppendLine("- Do not use this report to edit unrelated files unless they appear in the call stacks above.");
            return builder.ToString();
        }

        private static void AppendAllocationSiteTable(StringBuilder builder, List<AllocationSite> rankedSites)
        {
            builder.AppendLine("| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame | Hierarchy path |");
            builder.AppendLine("| ---: | ---: | ---: | ---: | --- | --- | --- | --- |");

            int limit = Math.Min(TopSiteCount, rankedSites.Count);
            for (int i = 0; i < limit; i++)
            {
                AllocationSite site = rankedSites[i];
                builder.Append("| ")
                    .Append(i + 1)
                    .Append(" | ")
                    .Append(site.Bytes)
                    .Append(" | ")
                    .Append(site.Samples)
                    .Append(" | ")
                    .Append(site.Frames)
                    .Append(" | ")
                    .Append(Escape(site.ThreadName))
                    .Append(" | ")
                    .Append(Escape(site.SampleName))
                    .Append(" | ")
                    .Append(Escape(GetTopManagedFrame(site.Callstack)))
                    .Append(" | ")
                    .Append(Escape(site.HierarchyPath))
                    .AppendLine(" |");
            }

            if (limit == 0)
                builder.AppendLine("| 0 | 0 | 0 | 0 | n/a | n/a | No GC.Alloc samples found in this automated capture. | n/a |");
        }

        private static bool IsEditorToolingAllocation(AllocationSite site)
        {
            if (site == null)
                return false;

            if (!string.IsNullOrEmpty(site.ThreadName) &&
                site.ThreadName.StartsWith("Burst-CompilerThread", StringComparison.Ordinal))
                return true;

            return ContainsEditorToolingFrame(site.Callstack) ||
                   ContainsEditorToolingFrame(site.HierarchyPath);
        }

        private static bool ContainsEditorToolingFrame(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   (value.Contains("Unity.AI.MCP.Editor", StringComparison.Ordinal) ||
                    value.Contains("Unity.AI.Tracing", StringComparison.Ordinal) ||
                    value.Contains("Unity.Relay.Editor", StringComparison.Ordinal) ||
                    value.Contains("Burst.Compiler", StringComparison.Ordinal));
        }

        private static CaptureMode GetCaptureMode()
        {
            return (CaptureMode)SessionState.GetInt(CaptureModeKey, (int)CaptureMode.SteadyState);
        }

        private static string DescribeCaptureMode(CaptureMode mode)
        {
            return mode == CaptureMode.Battle ? "battle-state" : "steady-state";
        }

        private static void ResetRuntimeAllocationProbes()
        {
            UIShellEcsPresentationSystem.ResetEditorAllocationProbe();
            MenuBootstrapView.ResetEditorAllocationProbe();
            SelectionRuntimeDiagnosticsSystemHelper.ResetEditorSelectionAllocationProbe();
            RuntimeDiagnosticsSystem.ResetEditorBuildingVisualAllocationProbe();
        }

        private static void AppendRuntimeAllocationProbeSummary(StringBuilder builder)
        {
            UIShellEcsPresentationSystem.GetEditorAllocationProbe(
                out long shellBytes,
                out int shellAllocationSamples,
                out int shellUpdateSamples);
            MenuBootstrapView.GetEditorAllocationProbe(
                out long bootstrapBytes,
                out int bootstrapAllocationSamples,
                out int bootstrapUpdateSamples);
            SelectionRuntimeDiagnosticsSystemHelper.EditorSelectionAllocationProbeSnapshot selectionProbe =
                SelectionRuntimeDiagnosticsSystemHelper.GetEditorSelectionAllocationProbe();
            RuntimeDiagnosticsSystem.EditorBuildingVisualAllocationProbeSnapshot buildingVisualProbe =
                RuntimeDiagnosticsSystem.GetEditorBuildingVisualAllocationProbe();
            builder.AppendLine("- Runtime allocation probe:");
            builder.Append("  - `UIShellEcsPresentationSystem.Update`: ")
                .Append(shellBytes)
                .Append(" bytes / ")
                .Append(shellAllocationSamples)
                .Append(" allocating updates / ")
                .Append(shellUpdateSamples)
                .AppendLine(" total updates.");
            builder.Append("  - `MenuBootstrapView.Update`: ")
                .Append(bootstrapBytes)
                .Append(" bytes / ")
                .Append(bootstrapAllocationSamples)
                .Append(" allocating updates / ")
                .Append(bootstrapUpdateSamples)
                .AppendLine(" total updates.");
            builder.Append("  - `SelectionGameplayStartupSystemHelper.UpdateSelectionRuntimePhases`: ")
                .Append(selectionProbe.TotalBytes)
                .Append(" bytes / ")
                .Append(selectionProbe.TotalAllocationSamples)
                .Append(" allocating updates / ")
                .Append(selectionProbe.TotalUpdateSamples)
                .AppendLine(" total updates. Diagnostic only; not a gate yet.");
            AppendSelectionProbePhase(
                builder,
                "CommandFlush",
                selectionProbe.CommandFlushBytes,
                selectionProbe.CommandFlushAllocationSamples,
                selectionProbe.CommandFlushUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "Input",
                selectionProbe.InputBytes,
                selectionProbe.InputAllocationSamples,
                selectionProbe.InputUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "FocusedReadModel",
                selectionProbe.FocusedReadModelBytes,
                selectionProbe.FocusedReadModelAllocationSamples,
                selectionProbe.FocusedReadModelUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "Panel",
                selectionProbe.PanelBytes,
                selectionProbe.PanelAllocationSamples,
                selectionProbe.PanelUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "TacticalCamera",
                selectionProbe.TacticalCameraBytes,
                selectionProbe.TacticalCameraAllocationSamples,
                selectionProbe.TacticalCameraUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "MarkerPreview",
                selectionProbe.MarkerPreviewBytes,
                selectionProbe.MarkerPreviewAllocationSamples,
                selectionProbe.MarkerPreviewUpdateSamples);
            AppendSelectionProbePhase(
                builder,
                "Camera",
                selectionProbe.CameraBytes,
                selectionProbe.CameraAllocationSamples,
                selectionProbe.CameraUpdateSamples);
            builder.Append("  - `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`: ")
                .Append(buildingVisualProbe.TotalBytes)
                .Append(" bytes / ")
                .Append(buildingVisualProbe.AllocationSamples)
                .Append(" allocating calls / ")
                .Append(buildingVisualProbe.CreateCalls)
                .Append(" create calls. pooled=")
                .Append(buildingVisualProbe.PooledHits)
                .Append(", wrappers=")
                .Append(buildingVisualProbe.WrapperCreates)
                .Append(", prefabInstantiates=")
                .Append(buildingVisualProbe.PrefabInstantiates)
                .Append(", prefabInstantiateBytes=")
                .Append(buildingVisualProbe.PrefabInstantiateBytes)
                .Append(" / ")
                .Append(buildingVisualProbe.PrefabInstantiateAllocationSamples)
                .AppendLine(" allocating prefab instantiates. Diagnostic only; not a gate yet.");
            builder.Append("- Runtime allocation probe assertion: ")
                .Append(shellBytes == 0 && bootstrapBytes == 0 ? "Passed." : "Failed.")
                .AppendLine();
        }

        private static void AppendSelectionProbePhase(
            StringBuilder builder,
            string phaseName,
            long bytes,
            int allocationSamples,
            int updateSamples)
        {
            builder.Append("    - `Selection.")
                .Append(phaseName)
                .Append("`: ")
                .Append(bytes)
                .Append(" bytes / ")
                .Append(allocationSamples)
                .Append(" allocating updates / ")
                .Append(updateSamples)
                .AppendLine(" total updates.");
        }

        private static bool TryValidateRuntimeAllocationProbes(out string status)
        {
            UIShellEcsPresentationSystem.GetEditorAllocationProbe(
                out long shellBytes,
                out int shellAllocationSamples,
                out int shellUpdateSamples);
            MenuBootstrapView.GetEditorAllocationProbe(
                out long bootstrapBytes,
                out int bootstrapAllocationSamples,
                out int bootstrapUpdateSamples);
            if (shellBytes == 0 && bootstrapBytes == 0)
            {
                status = "runtimeAllocationProbe=Passed";
                return true;
            }

            status =
                $"runtimeAllocationProbe=Failed shell={shellBytes}B/{shellAllocationSamples}allocating/{shellUpdateSamples}updates bootstrap={bootstrapBytes}B/{bootstrapAllocationSamples}allocating/{bootstrapUpdateSamples}updates";
            return false;
        }

        private static string ReportPath => GetCaptureMode() == CaptureMode.Battle
            ? BattleReportPath
            : SteadyStateReportPath;

        private static string ProfilerLogPath => GetCaptureMode() == CaptureMode.Battle
            ? ProfilerLogPrefix + "-battle"
            : ProfilerLogPrefix;

        private static string ProfilerRawPath => ProfilerLogPath + ".raw";

        private static int DisableEditorLiveConversionSystems()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return 0;

            int disabled = 0;
            disabled += DisableManagedSystemIfPresent(world, "Unity.Scenes.Editor.LiveConversionEditorSystemGroup, Unity.Scenes.Editor");
            disabled += DisableManagedSystemIfPresent(world, "Unity.Scenes.Editor.EditorSubSceneLiveConversionSystem, Unity.Scenes.Editor");
            return disabled;
        }

        private static int DisableManagedSystemIfPresent(World world, string assemblyQualifiedTypeName)
        {
            Type type = Type.GetType(assemblyQualifiedTypeName, false);
            if (type == null)
                return 0;

            ComponentSystemBase system = world.GetExistingSystemManaged(type);
            if (system == null || !system.Enabled)
                return 0;

            system.Enabled = false;
            return 1;
        }

        private static void ScanHierarchyFrame(
            HierarchyFrameDataView frame,
            Dictionary<string, AllocationSite> sites,
            Dictionary<int, FrameAllocationSummary> frameSummaries)
        {
            int rootId = frame.GetRootItemID();
            if (rootId == HierarchyFrameDataView.invalidSampleId)
                return;

            List<int> children = new(64);
            frame.GetItemChildren(rootId, children);

            long frameBytes = 0;
            int frameSamples = 0;
            for (int i = 0; i < children.Count; i++)
            {
                int childId = children[i];
                ScanHierarchyItem(frame, childId, frame.GetItemName(childId), sites, ref frameBytes, ref frameSamples);
            }

            if (frameSamples <= 0)
                return;

            if (!frameSummaries.TryGetValue(frame.frameIndex, out FrameAllocationSummary summary))
                summary = new FrameAllocationSummary { FrameIndex = frame.frameIndex };

            summary.Bytes += frameBytes;
            summary.Samples += frameSamples;
            frameSummaries[frame.frameIndex] = summary;
        }

        private static bool ScanHierarchyItem(
            HierarchyFrameDataView frame,
            int itemId,
            string hierarchyPath,
            Dictionary<string, AllocationSite> sites,
            ref long frameBytes,
            ref int frameSamples)
        {
            double gcColumn = frame.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnGcMemory);
            long itemBytes = Math.Max(0, (long)Math.Round(gcColumn));
            bool itemHasGc = itemBytes > 0;

            List<int> children = new(16);
            frame.GetItemChildren(itemId, children);
            bool childHasGc = false;
            for (int i = 0; i < children.Count; i++)
            {
                int childId = children[i];
                string childName = frame.GetItemName(childId);
                string childPath = string.IsNullOrEmpty(hierarchyPath)
                    ? childName
                    : hierarchyPath + " > " + childName;
                if (ScanHierarchyItem(frame, childId, childPath, sites, ref frameBytes, ref frameSamples))
                    childHasGc = true;
            }

            if (!itemHasGc)
                return childHasGc;

            string itemName = frame.GetItemName(itemId);
            bool isAllocationMarker = !string.IsNullOrEmpty(itemName) &&
                                      itemName.Contains("GC.Alloc", StringComparison.Ordinal);
            if (!isAllocationMarker && childHasGc)
                return true;

            int mergedSampleCount = frame.GetItemMergedSamplesCount(itemId);
            if (mergedSampleCount <= 0)
            {
                RecordSite(
                    sites,
                    itemName,
                    frame.threadName,
                    hierarchyPath,
                    frame.ResolveItemCallstack(itemId),
                    itemBytes,
                    frame.frameIndex);
                frameBytes += itemBytes;
                frameSamples++;
                return true;
            }

            List<double> sampleBytes = new(mergedSampleCount);
            frame.GetItemMergedSamplesColumnDataAsDoubles(itemId, HierarchyFrameDataView.columnGcMemory, sampleBytes);
            for (int sampleIndex = 0; sampleIndex < mergedSampleCount; sampleIndex++)
            {
                long bytes = sampleIndex < sampleBytes.Count
                    ? Math.Max(0, (long)Math.Round(sampleBytes[sampleIndex]))
                    : itemBytes / Math.Max(1, mergedSampleCount);
                string callstack = frame.ResolveItemMergedSampleCallstack(itemId, sampleIndex);
                RecordSite(sites, itemName, frame.threadName, hierarchyPath, callstack, bytes, frame.frameIndex);
                frameBytes += bytes;
                frameSamples++;
            }

            return true;
        }

        private static void RecordSite(
            Dictionary<string, AllocationSite> sites,
            string sampleName,
            string threadName,
            string hierarchyPath,
            string callstack,
            long bytes,
            int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(callstack))
                callstack = "(no managed call stack captured)";

            string key = sampleName + "\n" + hierarchyPath + "\n" + callstack;
            if (!sites.TryGetValue(key, out AllocationSite site))
            {
                site = new AllocationSite
                {
                    Key = key,
                    SampleName = sampleName,
                    ThreadName = threadName,
                    HierarchyPath = hierarchyPath,
                    Callstack = callstack
                };
                sites.Add(key, site);
            }

            site.Bytes += bytes;
            site.Samples++;
            if (site.LastFrameIndex != frameIndex)
            {
                site.LastFrameIndex = frameIndex;
                site.Frames++;
            }
        }

        private static string GetTopManagedFrame(string callstack)
        {
            if (string.IsNullOrWhiteSpace(callstack))
                return "(no managed call stack captured)";

            string[] lines = callstack.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;

                if (line.Contains("Assets/Game/", StringComparison.Ordinal) ||
                    line.Contains("Assets/Tests/", StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return lines.Length == 0 ? "(no managed call stack captured)" : lines[0].Trim();
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", "<br>", StringComparison.Ordinal);
        }

        private static void WriteReport(string report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? ".");
            File.WriteAllText(ReportPath, report, Encoding.UTF8);
            Debug.Log($"[MatchGcAllocationCallstackCapture] wroteReport {ReportPath}");
        }

        private static bool TryEnqueueMatchRoute(out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is missing.";
                return false;
            }

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                error = "UI shell root is missing.";
                return false;
            }

            Entity boundary = query.GetSingletonEntity();
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            routeRequests.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            return true;
        }

        private static bool TryPrepareBattleCapture(out string status)
        {
            status = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                status = "Default ECS world is missing.";
                return false;
            }

            EntityManager entityManager = world.EntityManager;
            if (!TryGetGridConfig(entityManager, out GridConfig grid))
            {
                status = "GridConfig singleton is missing.";
                return false;
            }

            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitAttack>(),
                    ComponentType.ReadWrite<UnitAttackCooldownComponent>(),
                    ComponentType.ReadWrite<UnitAttackTraceComponent>(),
                    ComponentType.ReadWrite<UnitAttackAnimationComponent>(),
                    ComponentType.ReadOnly<UnitHealth>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<UnitDeathAnimationComponent>()
                }
            });

            if (query.IsEmptyIgnoreFilter)
            {
                status = "No attack-capable units found.";
                return false;
            }

            using NativeArray<Entity> candidates = query.ToEntityArray(Allocator.Temp);
            int armed = 0;
            for (int i = 0; i < candidates.Length && armed < BattleCaptureMaxAttackers; i++)
            {
                Entity attacker = candidates[i];
                if (!TryArmBattleAttacker(entityManager, grid, attacker))
                    continue;

                armed++;
            }

            status = $"candidates={candidates.Length} armed={armed}";
            return armed > 0;
        }

        private static int PrewarmBattleVfxPools()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return 0;

            EntityManager entityManager = world.EntityManager;
            HashSet<GameObject> prefabs = new();
            CollectUnitAttackImpactVfxPrefabs(entityManager, prefabs);
            CollectGroundMissileVfxPrefabs(entityManager, prefabs);
            CollectAirMissileVfxPrefabs(entityManager, prefabs);

            foreach (GameObject prefab in prefabs)
            {
                UnitAttackImpactVfxView.Prewarm(prefab, BattleVfxPrewarmCount);
            }

            return prefabs.Count;
        }

        private static void CollectUnitAttackImpactVfxPrefabs(EntityManager entityManager, HashSet<GameObject> prefabs)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackImpactVfxReference>());
            if (query.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                UnitAttackImpactVfxReference vfx = entityManager.GetComponentData<UnitAttackImpactVfxReference>(entities[i]);
                AddPrefab(prefabs, vfx.Prefab.Value);
            }
        }

        private static void CollectGroundMissileVfxPrefabs(EntityManager entityManager, HashSet<GameObject> prefabs)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GroundMissileLauncherVfxReferenceComponent>());
            if (query.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                GroundMissileLauncherVfxReferenceComponent vfx = entityManager.GetComponentData<GroundMissileLauncherVfxReferenceComponent>(entities[i]);
                AddPrefab(prefabs, vfx.LauncherBackfirePrefab.Value);
                AddPrefab(prefabs, vfx.RocketTrailPrefab.Value);
                AddPrefab(prefabs, vfx.ImpactExplosionPrefab.Value);
                AddPrefab(prefabs, vfx.ImpactSmokePrefab.Value);
            }
        }

        private static void CollectAirMissileVfxPrefabs(EntityManager entityManager, HashSet<GameObject> prefabs)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<AirMissileLauncherVfxReferenceComponent>());
            if (query.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                AirMissileLauncherVfxReferenceComponent vfx = entityManager.GetComponentData<AirMissileLauncherVfxReferenceComponent>(entities[i]);
                AddPrefab(prefabs, vfx.MissileVisualPrefab.Value);
                AddPrefab(prefabs, vfx.LaunchFlashPrefab.Value);
                AddPrefab(prefabs, vfx.LaunchSmokePrefab.Value);
                AddPrefab(prefabs, vfx.MissileTrailPrefab.Value);
                AddPrefab(prefabs, vfx.AirburstExplosionPrefab.Value);
                AddPrefab(prefabs, vfx.AirTargetImpactPrefab.Value);
                AddPrefab(prefabs, vfx.InterceptExplosionPrefab.Value);
            }
        }

        private static void AddPrefab(HashSet<GameObject> prefabs, GameObject prefab)
        {
            if (prefab != null)
                prefabs.Add(prefab);
        }

        private static bool TryGetGridConfig(EntityManager entityManager, out GridConfig grid)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                grid = default;
                return false;
            }

            grid = query.GetSingleton<GridConfig>();
            return true;
        }

        private static bool TryArmBattleAttacker(EntityManager entityManager, GridConfig grid, Entity attacker)
        {
            if (!entityManager.Exists(attacker) ||
                entityManager.HasComponent<AirMissileLauncherComponent>(attacker) ||
                !entityManager.HasComponent<UnitAttack>(attacker) ||
                !entityManager.HasComponent<UnitAttackCooldownComponent>(attacker) ||
                !entityManager.HasComponent<UnitAttackTraceComponent>(attacker) ||
                !entityManager.HasComponent<UnitAttackAnimationComponent>(attacker) ||
                !entityManager.HasComponent<UnitHealth>(attacker) ||
                !entityManager.HasComponent<LocalTransform>(attacker))
            {
                return false;
            }

            if (entityManager.HasComponent<UnitCombat>(attacker) &&
                entityManager.GetComponentData<UnitCombat>(attacker).CanAttack == 0)
            {
                return false;
            }

            UnitHealth attackerHealth = entityManager.GetComponentData<UnitHealth>(attacker);
            if (attackerHealth.Current <= 0)
                return false;

            UnitAttack attack = entityManager.GetComponentData<UnitAttack>(attacker);
            if (attack.Range <= 0f)
                return false;

            LocalTransform attackerTransform = entityManager.GetComponentData<LocalTransform>(attacker);
            float3 forward = math.rotate(attackerTransform.Rotation, new float3(0f, 0f, 1f));
            forward.y = 0f;
            forward = math.normalizesafe(forward, new float3(0f, 0f, 1f));
            float distance = math.max(0.25f, attack.Range * 0.55f);
            float3 targetPosition = attackerTransform.Position + forward * distance;
            targetPosition.y = attackerTransform.Position.y;
            int2 targetCell = GridUtils.WorldToCell(grid, targetPosition);
            targetCell = new int2(
                math.clamp(targetCell.x, 0, grid.Width - 1),
                math.clamp(targetCell.y, 0, grid.Height - 1));

            Entity target = entityManager.CreateEntity(
                typeof(UnitHealth),
                typeof(LocalTransform));
            entityManager.SetComponentData(target, new UnitHealth
            {
                Current = BattleCaptureTargetHealth,
                Max = BattleCaptureTargetHealth
            });
            entityManager.SetComponentData(target, LocalTransform.FromPosition(targetPosition));

            EngageTarget engageTarget = new()
            {
                Target = target,
                Cell = targetCell,
                Position = targetPosition,
                IsCommanded = 1
            };

            if (entityManager.HasComponent<EngageTarget>(attacker))
                entityManager.SetComponentData(attacker, engageTarget);
            else
                entityManager.AddComponentData(attacker, engageTarget);

            entityManager.SetComponentData(attacker, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
            entityManager.SetComponentData(attacker, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
            entityManager.SetComponentData(attacker, new UnitAttackAnimationComponent { TimeRemaining = 0f });
            return true;
        }

        private static void EnsurePlayModeRequested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static bool IsMatchRuntimeReady(out string status)
        {
            status = "waiting";
            if (!TryGetShellState(out UiShellStateComponent shellState))
                return false;

            if (!TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState))
                return false;

            if (!TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro))
                return false;

            bool matchSceneLoaded = IsSceneLoaded(MatchSceneName);
            bool hudLoaded = LoadedScenesContainMatchHudContent();
            bool curtainHidden = IsMatchIntroCurtainHidden();
            status =
                $"mode={shellState.CurrentMode} route={shellState.ActiveRoute} phase={shellState.Phase} " +
                $"transition={shellState.IsTransitionRunning} playRequested={runtimeState.PlayRequested} " +
                $"matchIntro={matchIntro.State} inputLocked={matchIntro.InputLocked} " +
                $"matchSceneLoaded={(matchSceneLoaded ? 1 : 0)} hudLoaded={(hudLoaded ? 1 : 0)} " +
                $"curtainHidden={(curtainHidden ? 1 : 0)}";

            return shellState.CurrentMode == UiShellMode.MatchHud &&
                   shellState.ActiveRoute == UIRoute.Match &&
                   shellState.IsTransitionRunning == 0 &&
                   runtimeState.PlayRequested != 0 &&
                   matchIntro.State == MatchIntroTransitionStateKind.Complete &&
                   matchIntro.InputLocked == 0 &&
                   matchSceneLoaded &&
                   hudLoaded &&
                   curtainHidden;
        }

        private static bool TryGetShellState(out UiShellStateComponent shellState)
        {
            shellState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<UiShellStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            shellState = entityManager.GetComponentData<UiShellStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState)
        {
            runtimeState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            runtimeState = entityManager.GetComponentData<RuntimeGameplayStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro)
        {
            matchIntro = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            matchIntro = entityManager.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool LoadedScenesContainMatchHudContent()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    if (TreeContains(roots[rootIndex].transform, MatchHudContentName) ||
                        roots[rootIndex].GetComponentInChildren<MatchOverlayCommandControlsView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<BattleHudRuntimeFeedbackView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<MatchHudMinimapView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<MatchHudSquadTrayView>(true) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMatchIntroCurtainHidden()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    MatchIntroCurtainView curtain = roots[rootIndex].GetComponentInChildren<MatchIntroCurtainView>(true);
                    if (curtain == null)
                        continue;

                    bool rootHidden = curtain.Root == null || !curtain.Root.activeSelf;
                    bool transparent = curtain.CanvasGroup == null || curtain.CanvasGroup.alpha <= 0.001f;
                    return rootHidden && transparent;
                }
            }

            return false;
        }

        private static bool TreeContains(Transform node, string objectName)
        {
            if (node.name == objectName)
                return true;

            for (int i = 0; i < node.childCount; i++)
            {
                if (TreeContains(node.GetChild(i), objectName))
                    return true;
            }

            return false;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
                return;

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            if (condition != null &&
                (condition.Contains("[MatchGcAllocationCallstackCapture] result=Failed", StringComparison.Ordinal) ||
                 condition.Contains("[Licensing::", StringComparison.Ordinal)))
            {
                return;
            }

            if (condition != null &&
                stackTrace != null &&
                condition.StartsWith("ArgumentOutOfRangeException", StringComparison.Ordinal) &&
                (stackTrace.Contains("UnityEditor.Search.SearchDatabase", StringComparison.Ordinal) ||
                 stackTrace.Contains("UnityEditor.Search.SearchInit", StringComparison.Ordinal)))
            {
                return;
            }

            if (IsEditorToolingConnectionError(condition, stackTrace))
                return;

            SessionState.SetInt(ErrorCountKey, SessionState.GetInt(ErrorCountKey, 0) + 1);
        }

        private static bool IsEditorToolingConnectionError(string condition, string stackTrace)
        {
            if (string.IsNullOrEmpty(condition) || string.IsNullOrEmpty(stackTrace))
                return false;

            return
                condition.Contains("connection.state_change", StringComparison.Ordinal) &&
                condition.Contains("WebSocketException: Unable to connect to the remote server", StringComparison.Ordinal) &&
                (stackTrace.Contains("Unity.AI.Tracing", StringComparison.Ordinal) ||
                 stackTrace.Contains("Unity.Relay.Editor.RelayService", StringComparison.Ordinal) ||
                 stackTrace.Contains("Unity.AI.MCP.Editor", StringComparison.Ordinal));
        }

        private static void Finish(bool success, string message)
        {
            try
            {
                StopProfilerCapture();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MatchGcAllocationCallstackCapture] profilerRestoreFailed {exception.Message}");
            }

            ResetState();
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Application.logMessageReceived -= OnLogMessageReceived;

            Debug.Log(success
                ? message
                : $"[MatchGcAllocationCallstackCapture] result=Failed {message}");
            if (Application.isBatchMode)
            {
                int exitCode = success ? 0 : 1;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.ExitPlaymode();
                RequestBatchExit(exitCode);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
        }

        private static void RequestBatchExit(int exitCode)
        {
            pendingBatchExitCode = exitCode;
            hasPendingBatchExit = true;
            EditorApplication.update -= ExitBatchModeWhenReady;
            EditorApplication.update += ExitBatchModeWhenReady;
        }

        private static void ExitBatchModeWhenReady()
        {
            if (!hasPendingBatchExit)
            {
                EditorApplication.update -= ExitBatchModeWhenReady;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            int exitCode = pendingBatchExitCode;
            hasPendingBatchExit = false;
            EditorApplication.update -= ExitBatchModeWhenReady;
            EditorApplication.Exit(exitCode);
        }

        private static void ResetState()
        {
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseInt(CaptureModeKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseInt(ErrorCountKey);
            SessionState.EraseInt(CaptureStartFrameKey);
            SessionState.EraseInt(WarmupStartFrameKey);
            SessionState.EraseBool(ProfilerStateStoredKey);
            SessionState.EraseInt(EditorLiveConversionDisabledCountKey);
        }
    }
    #endif
}
