using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class UiToolkitPerformanceProbe
{
    private const string ActiveKey = "UiToolkitPerformanceProbe.Active";
    private const string PhaseKey = "UiToolkitPerformanceProbe.Phase";
    private const string StartedAtKey = "UiToolkitPerformanceProbe.StartedAt";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string ReportPath = "/private/tmp/warline-uitoolkit-performance-probe.json";
    private const int WarmupFrames = 45;
    private const int SampleFrames = 120;
    private const double TimeoutSeconds = 90d;

    private enum Phase
    {
        Idle = 0,
        WaitingForPlayMode = 1,
        WaitingForShell = 2,
        WarmupEnabled = 3,
        SampleEnabled = 4,
        WarmupDisabled = 5,
        SampleDisabled = 6,
        Finish = 7
    }

    private sealed class SampleWindow
    {
        public readonly string Name;
        public readonly Dictionary<string, MarkerStats> MarkerStats = new(StringComparer.Ordinal);
        public int Frames;
        public double Seconds;
        public double MaxDeltaSeconds;
        public long LastDrawCalls;
        public long LastBatches;
        public long LastSetPass;
        public long LastTriangles;
        public long LastVertices;

        public SampleWindow(string name)
        {
            Name = name;
        }
    }

    private sealed class Scenario
    {
        public readonly string Name;
        public readonly string[] HiddenElementNames;
        public readonly bool DisableShellRoot;

        public Scenario(string name, string[] hiddenElementNames = null, bool disableShellRoot = false)
        {
            Name = name;
            HiddenElementNames = hiddenElementNames ?? Array.Empty<string>();
            DisableShellRoot = disableShellRoot;
        }
    }

    private sealed class MarkerStats
    {
        public long Sum;
        public long Max;
        public int Samples;

        public void Add(long value)
        {
            Sum += value;
            if (value > Max)
                Max = value;
            Samples++;
        }
    }

    private struct NamedRecorder
    {
        public string Name;
        public ProfilerRecorder Recorder;
    }

    private static readonly string[] MarkerNeedles =
    {
        "UIElements",
        "UI Toolkit",
        "UIR",
        "Panel",
        "RenderChain",
        "UpdateRuntimePanels",
        "UpdatePanels",
        "Repaint",
        "Style",
        "Layout",
        "Bindings"
    };

    private static readonly List<NamedRecorder> MarkerRecorders = new();
    private static readonly List<SampleWindow> SampleWindows = new();
    private static readonly List<VisualElement> HiddenElements = new();
    private static readonly Scenario[] Scenarios =
    {
        new("all"),
        new("no-background", new[] { "MenuBackgroundContent" }),
        new("no-background-art", new[] { "BackgroundArt" }),
        new("no-background-overlay", new[] { "BackgroundArtOverlay" }),
        new("no-header", new[] { "HeaderContent" }),
        new("no-left", new[] { "LeftContent" }),
        new("no-middle", new[] { "MiddleContent" }),
        new("no-right", new[] { "RightContent" }),
        new("no-footer", new[] { "FooterContent" }),
        new("root-disabled", disableShellRoot: true)
    };
    private static ProfilerRecorder drawCallsRecorder;
    private static ProfilerRecorder batchesRecorder;
    private static ProfilerRecorder setPassRecorder;
    private static ProfilerRecorder trianglesRecorder;
    private static ProfilerRecorder verticesRecorder;
    private static GameObject shellRoot;
    private static UiToolkitShellView shellView;
    private static SampleWindow currentWindow;
    private static int scenarioIndex;
    private static int phaseFrame;

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (SessionState.GetBool(ActiveKey, false))
            Register();
    }

    public static void Run()
    {
        CleanupRecorders();
        SampleWindows.Clear();
        currentWindow = null;
        shellRoot = null;
        shellView = null;
        scenarioIndex = 0;
        phaseFrame = 0;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
        SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
        Register();
        EditorSceneManager.OpenScene(MenuScenePath);
        EditorApplication.EnterPlaymode();
    }

    private static void Register()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
            SetPhase(Phase.WaitingForShell);
        else if (state == PlayModeStateChange.EnteredEditMode &&
            (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle) == Phase.Finish)
        {
            Cleanup();
            EditorApplication.Exit(0);
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > TimeoutSeconds)
        {
            WriteReport("timeout");
            Finish();
            return;
        }

        Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
        switch (phase)
        {
            case Phase.WaitingForPlayMode:
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.EnterPlaymode();
                break;
            case Phase.WaitingForShell:
                if (TryFindReadyShell())
                {
                    StartRecorders();
                    ApplyScenario(0);
                    SetPhase(Phase.WarmupEnabled);
                }
                break;
            case Phase.WarmupEnabled:
                if (++phaseFrame >= WarmupFrames)
                {
                    currentWindow = new SampleWindow(Scenarios[scenarioIndex].Name);
                    SampleWindows.Add(currentWindow);
                    SetPhase(Phase.SampleEnabled);
                }
                break;
            case Phase.SampleEnabled:
                RecordFrame(currentWindow);
                if (++phaseFrame >= SampleFrames)
                {
                    scenarioIndex++;
                    if (scenarioIndex >= Scenarios.Length)
                    {
                        WriteReport("completed");
                        Finish();
                    }
                    else
                    {
                        ApplyScenario(scenarioIndex);
                        SetPhase(Phase.WarmupEnabled);
                    }
                }
                break;
        }
    }

    private static bool TryFindReadyShell()
    {
        shellRoot = GameObject.Find("UiToolkitShellRoot");
        if (shellRoot == null)
            return false;

        shellView = shellRoot.GetComponent<UiToolkitShellView>();
        return shellView != null && shellView.IsMounted && shellView.HasMountedMainMenuScreen;
    }

    private static void ApplyScenario(int index)
    {
        RestoreScenario();
        if (index < 0 || index >= Scenarios.Length)
            return;

        Scenario scenario = Scenarios[index];
        if (shellRoot != null && !shellRoot.activeSelf)
            shellRoot.SetActive(true);

        if (scenario.DisableShellRoot)
        {
            if (shellRoot != null)
                shellRoot.SetActive(false);
            return;
        }

        VisualElement root = shellView != null ? shellView.MainMenuContentRoot : null;
        if (root == null)
            return;

        for (int i = 0; i < scenario.HiddenElementNames.Length; i++)
        {
            VisualElement element = root.Q<VisualElement>(scenario.HiddenElementNames[i]);
            if (element == null)
                continue;

            element.style.display = DisplayStyle.None;
            HiddenElements.Add(element);
        }
    }

    private static void RestoreScenario()
    {
        for (int i = 0; i < HiddenElements.Count; i++)
            HiddenElements[i].style.display = new StyleEnum<DisplayStyle>(StyleKeyword.Null);

        HiddenElements.Clear();
    }

    private static void SetPhase(Phase phase)
    {
        phaseFrame = 0;
        SessionState.SetInt(PhaseKey, (int)phase);
    }

    private static void StartRecorders()
    {
        CleanupRecorders();
        drawCallsRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
        batchesRecorder = StartRecorder(ProfilerCategory.Render, "Batches Count");
        setPassRecorder = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        trianglesRecorder = StartRecorder(ProfilerCategory.Render, "Triangles Count");
        verticesRecorder = StartRecorder(ProfilerCategory.Render, "Vertices Count");

        List<ProfilerRecorderHandle> handles = new();
        ProfilerRecorderHandle.GetAvailable(handles);
        for (int i = 0; i < handles.Count; i++)
        {
            ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handles[i]);
            string name = description.Name;
            if (!IsRelevantMarker(name))
                continue;

            ProfilerRecorder recorder = StartRecorder(description.Category, name);
            if (!recorder.Valid)
                continue;

            MarkerRecorders.Add(new NamedRecorder
            {
                Name = name,
                Recorder = recorder
            });
        }
    }

    private static bool IsRelevantMarker(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < MarkerNeedles.Length; i++)
        {
            if (name.IndexOf(MarkerNeedles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static ProfilerRecorder StartRecorder(ProfilerCategory category, string statName)
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

    private static void RecordFrame(SampleWindow window)
    {
        if (window == null)
            return;

        float delta = Mathf.Max(0f, Time.unscaledDeltaTime);
        window.Frames++;
        window.Seconds += delta;
        if (delta > window.MaxDeltaSeconds)
            window.MaxDeltaSeconds = delta;
        window.LastDrawCalls = Read(drawCallsRecorder);
        window.LastBatches = Read(batchesRecorder);
        window.LastSetPass = Read(setPassRecorder);
        window.LastTriangles = Read(trianglesRecorder);
        window.LastVertices = Read(verticesRecorder);

        for (int i = 0; i < MarkerRecorders.Count; i++)
        {
            NamedRecorder named = MarkerRecorders[i];
            long value = Read(named.Recorder);
            if (!window.MarkerStats.TryGetValue(named.Name, out MarkerStats stats))
            {
                stats = new MarkerStats();
                window.MarkerStats.Add(named.Name, stats);
            }

            stats.Add(value);
        }
    }

    private static long Read(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : 0L;
    }

    private static void WriteReport(string status)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "/private/tmp");
        File.WriteAllText(ReportPath, BuildReport(status));
        Debug.Log($"[UiToolkitPerformanceProbe] status={status} report={ReportPath}");
    }

    private static string BuildReport(string status)
    {
        return "{\n" +
            $"  \"status\": \"{status}\",\n" +
            $"  \"unityVersion\": \"{Application.unityVersion}\",\n" +
            "  \"samples\": [\n" +
            FormatWindows() +
            "\n  ]\n" +
            "}\n";
    }

    private static string FormatWindows()
    {
        List<string> windows = new();
        for (int i = 0; i < SampleWindows.Count; i++)
            windows.Add(FormatWindow(SampleWindows[i]));

        return string.Join(",\n", windows);
    }

    private static string FormatWindow(SampleWindow window)
    {
        if (window == null)
            return "null";

        double fps = window.Seconds > 0d ? window.Frames / window.Seconds : 0d;
        List<string> markerLines = new();
        foreach (KeyValuePair<string, MarkerStats> pair in window.MarkerStats)
        {
            MarkerStats stats = pair.Value;
            if (stats.Samples == 0 || (stats.Sum == 0L && stats.Max == 0L))
                continue;

            markerLines.Add(
                "      { \"name\": \"" + Escape(pair.Key) + "\", " +
                "\"avg\": " + FormatDouble((double)stats.Sum / stats.Samples) + ", " +
                "\"max\": " + stats.Max.ToString(CultureInfo.InvariantCulture) + " }");
        }

        markerLines.Sort(StringComparer.Ordinal);
        return "{\n" +
            $"    \"name\": \"{Escape(window.Name)}\",\n" +
            $"    \"frames\": {window.Frames},\n" +
            $"    \"seconds\": {FormatDouble(window.Seconds)},\n" +
            $"    \"fps\": {FormatDouble(fps)},\n" +
            $"    \"maxDeltaMs\": {FormatDouble(window.MaxDeltaSeconds * 1000d)},\n" +
            $"    \"drawCalls\": {window.LastDrawCalls},\n" +
            $"    \"batches\": {window.LastBatches},\n" +
            $"    \"setPass\": {window.LastSetPass},\n" +
            $"    \"triangles\": {window.LastTriangles},\n" +
            $"    \"vertices\": {window.LastVertices},\n" +
            "    \"markers\": [\n" +
            string.Join(",\n", markerLines) +
            "\n    ]\n" +
            "  }";
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? string.Empty;
    }

    private static void Finish()
    {
        SetPhase(Phase.Finish);
        CleanupRecorders();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        Cleanup();
        EditorApplication.Exit(0);
    }

    private static void Cleanup()
    {
        CleanupRecorders();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(StartedAtKey);
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private static void CleanupRecorders()
    {
        if (drawCallsRecorder.Valid)
            drawCallsRecorder.Dispose();
        if (batchesRecorder.Valid)
            batchesRecorder.Dispose();
        if (setPassRecorder.Valid)
            setPassRecorder.Dispose();
        if (trianglesRecorder.Valid)
            trianglesRecorder.Dispose();
        if (verticesRecorder.Valid)
            verticesRecorder.Dispose();

        for (int i = 0; i < MarkerRecorders.Count; i++)
        {
            ProfilerRecorder recorder = MarkerRecorders[i].Recorder;
            if (recorder.Valid)
                recorder.Dispose();
        }

        MarkerRecorders.Clear();
    }
}
