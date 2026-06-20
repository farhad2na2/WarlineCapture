using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public sealed partial class UiDiagnosticsReadModelSystem : SystemBase
{
    private const int MaxVisibleLogEntries = 50;
    private const double FpsUpdateIntervalSeconds = 0.25d;

    private readonly Queue<RuntimeLogEntry> runtimeLogEntries = new(MaxVisibleLogEntries);
    private readonly StringBuilder runtimeLogBuilder = new(8192);
    private EntityQuery boundaryQuery;
    private double accumulatedSeconds;
    private int accumulatedFrames;
    private bool subscribed;

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

    protected override void OnCreate()
    {
        boundaryQuery = EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
            ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>());
    }

    protected override void OnStartRunning()
    {
        if (subscribed)
            return;

        Application.logMessageReceived += HandleRuntimeLogMessage;
        subscribed = true;
    }

    protected override void OnStopRunning()
    {
        if (!subscribed)
            return;

        Application.logMessageReceived -= HandleRuntimeLogMessage;
        subscribed = false;
    }

    protected override void OnDestroy()
    {
        OnStopRunning();
    }

    protected override void OnUpdate()
    {
        if (boundaryQuery.IsEmptyIgnoreFilter)
            return;

        Entity boundary = boundaryQuery.GetSingletonEntity();
        UiDiagnosticsOverlayComponent component =
            EntityManager.GetComponentData<UiDiagnosticsOverlayComponent>(boundary);

        accumulatedFrames++;
        accumulatedSeconds += Mathf.Max(0f, UnityEngine.Time.unscaledDeltaTime);
        if (accumulatedSeconds >= FpsUpdateIntervalSeconds)
        {
            double fps = accumulatedSeconds > 0d ? accumulatedFrames / accumulatedSeconds : 0d;
            component.Fps = Mathf.Max(0, Mathf.RoundToInt((float)fps));
            accumulatedFrames = 0;
            accumulatedSeconds = 0d;
        }

        component.LogText = BuildLogText();
        EntityManager.SetComponentData(boundary, component);
    }

    private void HandleRuntimeLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log && string.IsNullOrWhiteSpace(condition))
            return;

        AddRuntimeLogEntry(BuildRuntimeLogMessage(condition, stackTrace, type), type);
    }

    private void AddRuntimeLogEntry(string message, LogType type)
    {
        while (runtimeLogEntries.Count >= MaxVisibleLogEntries)
            runtimeLogEntries.Dequeue();

        runtimeLogEntries.Enqueue(new RuntimeLogEntry(message, type));
    }

    private FixedString4096Bytes BuildLogText()
    {
        runtimeLogBuilder.Clear();
        foreach (RuntimeLogEntry entry in runtimeLogEntries)
        {
            if (runtimeLogBuilder.Length > 0)
                runtimeLogBuilder.Append('\n').Append('\n');

            runtimeLogBuilder.Append(GetLogPrefix(entry.Type));
            runtimeLogBuilder.Append(entry.Message);
        }

        if (runtimeLogBuilder.Length == 0)
            runtimeLogBuilder.Append("Runtime log ready.");

        var fixedLog = new FixedString4096Bytes();
        fixedLog.Append(runtimeLogBuilder.ToString());
        return fixedLog;
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
