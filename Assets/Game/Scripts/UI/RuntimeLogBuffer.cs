using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Runtime
{
    internal static class RuntimeLogBuffer
    {
        internal readonly struct Entry
        {
            public readonly string Condition;
            public readonly string StackTrace;
            public readonly LogType Type;

            public Entry(string condition, string stackTrace, LogType type)
            {
                Condition = condition ?? string.Empty;
                StackTrace = stackTrace ?? string.Empty;
                Type = type;
            }
        }

        private const int Capacity = 200;
        private static readonly Queue<Entry> Entries = new(Capacity);
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            if (_initialized)
                return;

            _initialized = true;
            AddEntry("[RuntimeLog] Capture started before scene load.", string.Empty, LogType.Log);
            Application.logMessageReceived += HandleLogMessage;
        }

        internal static IReadOnlyList<Entry> Snapshot()
        {
            if (!_initialized && Application.isPlaying)
                InitializeBeforeSceneLoad();

            return Entries.ToArray();
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && string.IsNullOrWhiteSpace(condition))
                return;

            AddEntry(condition, stackTrace, type);
        }

        private static void AddEntry(string condition, string stackTrace, LogType type)
        {
            while (Entries.Count >= Capacity)
                Entries.Dequeue();

            Entries.Enqueue(new Entry(condition, stackTrace, type));
        }
    }
}
