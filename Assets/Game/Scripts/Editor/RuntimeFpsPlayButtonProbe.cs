using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Game.Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RuntimeFpsPlayButtonProbe
{
    private const string ActiveKey = "WarlineCapture.RuntimeFpsPlayButtonProbe.Active";
    private const string StageKey = "WarlineCapture.RuntimeFpsPlayButtonProbe.Stage";
    private const string OutputPath = "/private/tmp/warlinecapture-runtime-fps-probe.json";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const double StartupTimeoutSeconds = 30d;
    private const double GameplayWarmupSeconds = 22d;
    private const double GameplaySampleSeconds = 45d;
    private static readonly Regex FpsRegex = new(@"fps=(?<fps>[0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);

    private static readonly List<string> RelevantLogs = new();
    private static readonly List<float> FpsSamples = new();
    private static double s_stageStartTime;
    private static double s_gameplayStartTime;
    private static int s_frameRateDiagCount;
    private static bool s_clickedGameButton;
    private static bool s_requestFallbackUsed;
    private static bool s_finished;

    static RuntimeFpsPlayButtonProbe()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    public static void Run()
    {
        RelevantLogs.Clear();
        FpsSamples.Clear();
        s_frameRateDiagCount = 0;
        s_clickedGameButton = false;
        s_requestFallbackUsed = false;
        s_finished = false;
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_gameplayStartTime = 0d;

        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void Detach()
    {
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(StageKey, 1);
            s_stageStartTime = EditorApplication.timeSinceStartup;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && s_finished)
        {
            SessionState.SetInt(ActiveKey, 0);
            SessionState.SetInt(StageKey, 0);
            Detach();
            EditorApplication.Exit(0);
        }
    }

    private static void Update()
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        int stage = SessionState.GetInt(StageKey, 0);
        double now = EditorApplication.timeSinceStartup;
        if (stage == 1)
        {
            if (TryClickGameButton())
            {
                SessionState.SetInt(StageKey, 2);
                s_stageStartTime = now;
                s_gameplayStartTime = now;
                return;
            }

            if (now - s_stageStartTime > StartupTimeoutSeconds)
                Finish("timeout_waiting_for_menu");
        }
        else if (stage == 2)
        {
            double gameplayElapsed = now - s_gameplayStartTime;
            if (gameplayElapsed >= GameplayWarmupSeconds && Time.unscaledDeltaTime > 0f)
                FpsSamples.Add(1f / Time.unscaledDeltaTime);

            if (gameplayElapsed >= GameplayWarmupSeconds + GameplaySampleSeconds)
                Finish("completed");
        }
    }

    private static bool TryClickGameButton()
    {
        Scene menuScene = SceneManager.GetSceneByName("Menu");
        if (!menuScene.IsValid() || !menuScene.isLoaded)
            return false;

        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            WarlineCaptureShellRouteButtonView routeButton = FindComponentInTree<WarlineCaptureShellRouteButtonView>(root.transform, IsDeployCommandButton);
            if (routeButton == null)
                continue;

            routeButton.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            s_clickedGameButton = true;
            return true;
        }

        MenuView menu = FindMenuView(menuScene);
        if (menu == null)
            return false;

        if (menu.buttonGame != null)
        {
            menu.buttonGame.onClick.Invoke();
            s_clickedGameButton = true;
            return true;
        }

        menu.RequestGameStart();
        s_requestFallbackUsed = true;
        return true;
    }

    private static bool IsDeployCommandButton(WarlineCaptureShellRouteButtonView routeButton)
    {
        return routeButton != null &&
               routeButton.name == "DeployCommandButton" &&
               routeButton.Intent == UiShellRouteIntent.EnterMatch &&
               routeButton.Route == WarlineCaptureRoute.Match;
    }

    private static MenuView FindMenuView(Scene menuScene)
    {
        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            MenuView menu = FindComponentInTree<MenuView>(root.transform, static candidate => candidate != null);
            if (menu != null)
                return menu;
        }

        return null;
    }

    private static T FindComponentInTree<T>(Transform root, Func<T, bool> predicate)
        where T : Component
    {
        if (root == null)
            return null;

        T component = root.GetComponent<T>();
        if (component != null && predicate(component))
            return component;

        for (int i = 0; i < root.childCount; i++)
        {
            T child = FindComponentInTree(root.GetChild(i), predicate);
            if (child != null)
                return child;
        }

        return null;
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (condition.Contains("[FrameRateDiag]", StringComparison.Ordinal) ||
            condition.Contains("[FrameRateDiag:PreGame]", StringComparison.Ordinal))
        {
            s_frameRateDiagCount++;
            Match match = FpsRegex.Match(condition);
            if (match.Success && float.TryParse(match.Groups["fps"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fps))
                FpsSamples.Add(fps);
        }

        if (type == LogType.Exception ||
            type == LogType.Error ||
            condition.Contains("[FrameRateDiag", StringComparison.Ordinal) ||
            condition.Contains("[PerfDiag", StringComparison.Ordinal) ||
            condition.Contains("[FreezeDetect", StringComparison.Ordinal) ||
            condition.Contains("[MenuCanvasDiag", StringComparison.Ordinal) ||
            condition.Contains("[UnitRenderBudget", StringComparison.Ordinal))
        {
            RelevantLogs.Add(condition);
        }
    }

    private static void Finish(string result)
    {
        if (s_finished)
            return;

        s_finished = true;
        WriteReport(result);
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            EditorApplication.Exit(0);
    }

    private static void WriteReport(string result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        float minFps = FpsSamples.Count > 0 ? float.MaxValue : 0f;
        float maxFps = FpsSamples.Count > 0 ? float.MinValue : 0f;
        double totalFps = 0d;
        for (int i = 0; i < FpsSamples.Count; i++)
        {
            float fps = FpsSamples[i];
            minFps = Mathf.Min(minFps, fps);
            maxFps = Mathf.Max(maxFps, fps);
            totalFps += fps;
        }

        double avgFps = FpsSamples.Count > 0 ? totalFps / FpsSamples.Count : 0d;
        StringBuilder json = new();
        json.AppendLine("{");
        AppendJson(json, "result", result, comma: true);
        AppendJson(json, "clickedGameButton", s_clickedGameButton, comma: true);
        AppendJson(json, "requestFallbackUsed", s_requestFallbackUsed, comma: true);
        AppendJson(json, "sampleCount", FpsSamples.Count, comma: true);
        AppendJson(json, "avgFps", avgFps, comma: true);
        AppendJson(json, "minFps", minFps, comma: true);
        AppendJson(json, "maxFps", maxFps, comma: true);
        AppendJson(json, "frameRateDiagCount", s_frameRateDiagCount, comma: true);
        json.AppendLine("  \"logs\": [");
        for (int i = 0; i < RelevantLogs.Count; i++)
        {
            json.Append("    \"");
            json.Append(EscapeJson(RelevantLogs[i]));
            json.Append(i + 1 < RelevantLogs.Count ? "\"," : "\"");
            json.AppendLine();
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(OutputPath, json.ToString());
        Debug.Log($"[RuntimeFpsPlayButtonProbe] result={result} avgFps={avgFps:F1} minFps={minFps:F1} maxFps={maxFps:F1} logs={RelevantLogs.Count} output={OutputPath}");
    }

    private static void AppendJson(StringBuilder json, string name, string value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": \"").Append(EscapeJson(value)).Append(comma ? "\"," : "\"").AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, bool value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false").Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, int value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, double value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString("F2", CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }
}
