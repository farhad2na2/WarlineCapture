#if ENABLE_PROFILER && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Entities;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public static class MatchGcAllocationCallstackCapture
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string MatchSceneName = "Match";
    private const string MatchHudContentName = "SCN08_MatchHudContent";
    private const string ReportPath = "Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md";
    private const string ProfilerLogPath = "/private/tmp/warline-match-gc-callstack-capture";
    private const string ProfilerRawPath = ProfilerLogPath + ".raw";
    private const int CaptureFrameCount = 300;
    private const int TopSiteCount = 15;
    private const double TimeoutSeconds = 180d;

    private const string ActiveKey = "MatchGcAllocationCallstackCapture.Active";
    private const string PhaseKey = "MatchGcAllocationCallstackCapture.Phase";
    private const string StartedAtKey = "MatchGcAllocationCallstackCapture.StartedAt";
    private const string ErrorCountKey = "MatchGcAllocationCallstackCapture.ErrorCount";
    private const string CaptureStartFrameKey = "MatchGcAllocationCallstackCapture.CaptureStartFrame";
    private const string ProfilerWasEnabledKey = "MatchGcAllocationCallstackCapture.ProfilerWasEnabled";
    private const string ProfilerAllocationCallstacksWasEnabledKey = "MatchGcAllocationCallstackCapture.AllocationCallstacksWasEnabled";
    private const string ProfilerBinaryLogWasEnabledKey = "MatchGcAllocationCallstackCapture.BinaryLogWasEnabled";
    private const string ProfilerLogFileKey = "MatchGcAllocationCallstackCapture.ProfilerLogFile";
    private const string ProfilerDeepProfilingWasEnabledKey = "MatchGcAllocationCallstackCapture.DeepProfilingWasEnabled";
    private const string ScriptsCategoryWasEnabledKey = "MatchGcAllocationCallstackCapture.ScriptsCategoryWasEnabled";
    private const string MemoryCategoryWasEnabledKey = "MatchGcAllocationCallstackCapture.MemoryCategoryWasEnabled";

    private enum Phase
    {
        Idle = 0,
        WaitingForPlayMode = 1,
        WaitingForShellReady = 2,
        WaitingForMatchReady = 3,
        Capturing = 4
    }

    private sealed class AllocationSite
    {
        public string Key = string.Empty;
        public string SampleName = string.Empty;
        public string ThreadName = string.Empty;
        public string Callstack = string.Empty;
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
        try
        {
            ResetState();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
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
        Finish(true, $"[MatchGcAllocationCallstackCapture] result=Passed frames={CaptureFrameCount} report={ReportPath} raw={ProfilerRawPath}");
    }

    private static void StartProfilerCapture()
    {
        SessionState.SetBool(ProfilerWasEnabledKey, Profiler.enabled);
        SessionState.SetBool(ProfilerAllocationCallstacksWasEnabledKey, Profiler.enableAllocationCallstacks);
        SessionState.SetBool(ProfilerBinaryLogWasEnabledKey, Profiler.enableBinaryLog);
        SessionState.SetString(ProfilerLogFileKey, Profiler.logFile ?? string.Empty);
        SessionState.SetBool(ProfilerDeepProfilingWasEnabledKey, ProfilerDriver.deepProfiling);
        SessionState.SetBool(ScriptsCategoryWasEnabledKey, Profiler.IsCategoryEnabled(ProfilerCategory.Scripts));
        SessionState.SetBool(MemoryCategoryWasEnabledKey, Profiler.IsCategoryEnabled(ProfilerCategory.Memory));

        if (File.Exists(ProfilerRawPath))
            File.Delete(ProfilerRawPath);

        ProfilerDriver.ClearAllFrames();
        ProfilerDriver.deepProfiling = false;
        Profiler.logFile = ProfilerLogPath;
        Profiler.enableBinaryLog = true;
        Profiler.enableAllocationCallstacks = true;
        Profiler.SetCategoryEnabled(ProfilerCategory.Scripts, true);
        Profiler.SetCategoryEnabled(ProfilerCategory.Memory, true);
        Profiler.enabled = true;
        SessionState.SetInt(CaptureStartFrameKey, Time.frameCount);
        Debug.Log($"[MatchGcAllocationCallstackCapture] captureStarted frames={CaptureFrameCount} raw={ProfilerRawPath}");
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

        StringBuilder builder = new(16384);
        builder.AppendLine("# Match GC Allocation Call-Stack Capture");
        builder.AppendLine();
        builder.AppendLine($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine("Lane: Gameplay/Performance");
        builder.AppendLine("Capture type: automated Match steady-state after Menu -> Match route");
        builder.AppendLine();
        builder.AppendLine("## Capture Summary");
        builder.AppendLine();
        builder.AppendLine($"- Requested frames: {CaptureFrameCount}");
        builder.AppendLine($"- Profiler frame range: {firstFrame}..{lastFrame}");
        builder.AppendLine($"- Scanned frames with data: {scannedFrames}");
        builder.AppendLine($"- Scanned thread views: {scannedThreads}");
        builder.AppendLine($"- GC.Alloc samples: {totalSamples}");
        builder.AppendLine($"- GC.Alloc bytes from hierarchy column: {totalBytes}");
        builder.AppendLine($"- Raw load status: `{loadStatus}`");
        builder.AppendLine($"- Raw capture: `{ProfilerRawPath}`");
        builder.AppendLine();
        builder.AppendLine("## Top Allocation Sites");
        builder.AppendLine();
        builder.AppendLine("| Rank | Bytes | Samples | Frames | Thread | Sample | Top managed frame |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | --- | --- | --- |");

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
                .AppendLine(" |");
        }

        if (limit == 0)
            builder.AppendLine("| 0 | 0 | 0 | 0 | n/a | n/a | No GC.Alloc samples found in this automated steady-state capture. |");

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
        for (int i = 0; i < limit; i++)
        {
            AllocationSite site = rankedSites[i];
            builder.AppendLine($"### {i + 1}. {GetTopManagedFrame(site.Callstack)}");
            builder.AppendLine();
            builder.AppendLine($"Bytes: {site.Bytes}  ");
            builder.AppendLine($"Samples: {site.Samples}  ");
            builder.AppendLine($"Frames: {site.Frames}  ");
            builder.AppendLine($"Thread: {site.ThreadName}");
            builder.AppendLine();
            builder.AppendLine("```");
            builder.AppendLine(string.IsNullOrWhiteSpace(site.Callstack) ? "(no managed call stack captured)" : site.Callstack);
            builder.AppendLine("```");
            builder.AppendLine();
        }

        builder.AppendLine("## Coverage Notes");
        builder.AppendLine();
        builder.AppendLine("- This automated pass covers steady-state Match HUD/runtime after the shell completes the Menu -> Match transition.");
        builder.AppendLine("- Battle and spike-frame call stacks still require a deterministic battle-driver capture or an interactive Profiler capture with Call Stacks -> GC.Alloc enabled.");
        builder.AppendLine("- Do not use this report to edit unrelated files unless they appear in the call stacks above.");
        return builder.ToString();
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
            ScanHierarchyItem(frame, children[i], sites, ref frameBytes, ref frameSamples);

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
            if (ScanHierarchyItem(frame, children[i], sites, ref frameBytes, ref frameSamples))
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
            RecordSite(sites, itemName, frame.threadName, callstack, bytes, frame.frameIndex);
            frameBytes += bytes;
            frameSamples++;
        }

        return true;
    }

    private static void RecordSite(
        Dictionary<string, AllocationSite> sites,
        string sampleName,
        string threadName,
        string callstack,
        long bytes,
        int frameIndex)
    {
        if (string.IsNullOrWhiteSpace(callstack))
            callstack = "(no managed call stack captured)";

        string key = sampleName + "\n" + callstack;
        if (!sites.TryGetValue(key, out AllocationSite site))
        {
            site = new AllocationSite
            {
                Key = key,
                SampleName = sampleName,
                ThreadName = threadName,
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>());
        if (query.IsEmptyIgnoreFilter)
        {
            error = "UI shell boundary is missing.";
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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

        SessionState.SetInt(ErrorCountKey, SessionState.GetInt(ErrorCountKey, 0) + 1);
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
            EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.ExitPlaymode();
    }

    private static void ResetState()
    {
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseInt(ErrorCountKey);
        SessionState.EraseInt(CaptureStartFrameKey);
    }
}
#endif
