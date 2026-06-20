using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UiDiagnosticsReadModelSystem : ISystem
{
    private const double FpsUpdateIntervalSeconds = 0.25d;

    private EntityQuery boundaryQuery;
    private double accumulatedSeconds;
    private int accumulatedFrames;

    public void OnCreate(ref SystemState state)
    {
        boundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
            ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>());
        UiDiagnosticsRuntimeLogBuffer.EnsureSubscribed();
    }

    public void OnDestroy(ref SystemState state)
    {
        UiDiagnosticsRuntimeLogBuffer.ReleaseSubscription();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (boundaryQuery.IsEmptyIgnoreFilter)
            return;

        Entity boundary = boundaryQuery.GetSingletonEntity();
        UiDiagnosticsOverlayComponent component =
            state.EntityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);

        accumulatedFrames++;
        accumulatedSeconds += Mathf.Max(0f, UnityEngine.Time.unscaledDeltaTime);
        if (accumulatedSeconds >= FpsUpdateIntervalSeconds)
        {
            double fps = accumulatedSeconds > 0d ? accumulatedFrames / accumulatedSeconds : 0d;
            component.Fps = Mathf.Max(0, Mathf.RoundToInt((float)fps));
            accumulatedFrames = 0;
            accumulatedSeconds = 0d;
        }

        component.LogText = UiDiagnosticsRuntimeLogBuffer.BuildLogText();
        state.EntityManager.SetComponentData(boundary, component);
    }
}

public static class UiDiagnosticsRuntimeLogBuffer
{
    private const int MaxVisibleLogEntries = 50;
    private static readonly Queue<RuntimeLogEntry> RuntimeLogEntries = new(MaxVisibleLogEntries);
    private static readonly StringBuilder RuntimeLogBuilder = new(8192);
    private static bool subscribed;

    private readonly struct RuntimeLogEntry
    {
        public readonly string Message;
        public readonly LogType Type;

        public RuntimeLogEntry(string message, LogType type)
        {
            Message = message ?? string.Empty;
            Type = type;
        }
    }

    public static void EnsureSubscribed()
    {
        if (subscribed)
            return;

        Application.logMessageReceived += HandleRuntimeLogMessage;
        subscribed = true;
    }

    public static void ReleaseSubscription()
    {
        if (!subscribed)
            return;

        Application.logMessageReceived -= HandleRuntimeLogMessage;
        subscribed = false;
    }

    public static FixedString4096Bytes BuildLogText()
    {
        RuntimeLogBuilder.Clear();
        foreach (RuntimeLogEntry entry in RuntimeLogEntries)
        {
            if (RuntimeLogBuilder.Length > 0)
                RuntimeLogBuilder.Append('\n').Append('\n');

            RuntimeLogBuilder.Append(GetLogPrefix(entry.Type));
            RuntimeLogBuilder.Append(entry.Message);
        }

        if (RuntimeLogBuilder.Length == 0)
            RuntimeLogBuilder.Append("Runtime log ready.");

        var fixedLog = new FixedString4096Bytes();
        fixedLog.Append(RuntimeLogBuilder.ToString());
        return fixedLog;
    }

    private static void HandleRuntimeLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log && string.IsNullOrWhiteSpace(condition))
            return;

        AddRuntimeLogEntry(BuildRuntimeLogMessage(condition, stackTrace, type), type);
    }

    private static void AddRuntimeLogEntry(string message, LogType type)
    {
        while (RuntimeLogEntries.Count >= MaxVisibleLogEntries)
            RuntimeLogEntries.Dequeue();

        RuntimeLogEntries.Enqueue(new RuntimeLogEntry(message, type));
    }

    private static string BuildRuntimeLogMessage(string condition, string stackTrace, LogType type)
    {
        bool includeStackTrace = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
        if (!includeStackTrace || string.IsNullOrWhiteSpace(stackTrace))
            return condition ?? string.Empty;

        return $"{condition}\n{stackTrace}";
    }

    private static string GetLogPrefix(LogType type)
    {
        return type switch
        {
            LogType.Warning => "[WARN] ",
            LogType.Error => "[ERROR] ",
            LogType.Exception => "[EXCEPTION] ",
            LogType.Assert => "[ASSERT] ",
            _ => string.Empty
        };
    }
}
