using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Runtime;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UiDiagnosticsReadModelSystem : ISystem
    {
        private const double FpsUpdateIntervalSeconds = 0.25d;

        private EntityQuery boundaryQuery;
        private double accumulatedSeconds;
        private int accumulatedFrames;
        private int appliedLogVersion;
        private byte previousLogVisible;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiDiagnosticsOverlayComponent>());
            appliedLogVersion = -1;
            UiDiagnosticsRuntimeLogBuffer.EnsureSubscribed();
            state.RequireForUpdate(boundaryQuery);
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

            bool changed = false;
            accumulatedFrames++;
            accumulatedSeconds += Mathf.Max(0f, UnityEngine.Time.unscaledDeltaTime);
            if (accumulatedSeconds >= FpsUpdateIntervalSeconds)
            {
                double fps = accumulatedSeconds > 0d ? accumulatedFrames / accumulatedSeconds : 0d;
                int nextFps = Mathf.Max(0, Mathf.RoundToInt((float)fps));
                if (component.Fps != nextFps)
                {
                    component.Fps = nextFps;
                    changed = true;
                }

                accumulatedFrames = 0;
                accumulatedSeconds = 0d;
            }

            int logVersion = UiDiagnosticsRuntimeLogBuffer.Version;
            bool logVisible = component.LogVisible != 0;
            bool logBecameVisible = logVisible && previousLogVisible == 0;
            if (logVisible && (logBecameVisible || logVersion != appliedLogVersion))
            {
                FixedString4096Bytes logText = UiDiagnosticsRuntimeLogBuffer.BuildLogText();
                if (!component.LogText.Equals(logText))
                {
                    component.LogText = logText;
                    changed = true;
                }

                appliedLogVersion = logVersion;
            }
            else if (!logVisible)
            {
                appliedLogVersion = logVersion;
            }

            previousLogVisible = component.LogVisible;

            if (changed)
                state.EntityManager.SetComponentData(boundary, component);
        }
    }

    public static class UiDiagnosticsRuntimeLogBuffer
    {
        private const int MaxVisibleLogEntries = 50;
        private static readonly Queue<RuntimeLogEntry> RuntimeLogEntries = new(MaxVisibleLogEntries);
        private static readonly StringBuilder RuntimeLogBuilder = new(8192);
        private static bool subscribed;
        private static int version;

        public static int Version => version;

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
            version++;
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
}
