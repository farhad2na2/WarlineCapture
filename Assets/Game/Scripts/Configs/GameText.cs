using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public static class GameText
    {
        private static GameTextCatalogSnapshot currentSnapshot = GameTextCatalogSnapshot.Uninitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            currentSnapshot = GameTextCatalogSnapshot.Uninitialized;
        }

        public static void Init(GameStringsConfig config)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            var audioEventIds = new Dictionary<string, string>(StringComparer.Ordinal);

            if (config != null && config.Entries != null)
            {
                for (int i = 0; i < config.Entries.Count; i++)
                {
                    GameStringConfigEntry entry = config.Entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                        continue;

                    entries[entry.Key] = entry.Value ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(entry.AudioEventId))
                        audioEventIds[entry.Key] = entry.AudioEventId;
                }
            }

            currentSnapshot = GameTextCatalogSnapshot.Create(entries, audioEventIds);
        }

        public static string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            GameTextCatalogSnapshot snapshot = currentSnapshot;
            if (snapshot.TryGet(key, out string value))
                return GameLocalization.Get(key, value);

            return GameLocalization.Get(key, fallback ?? key);
        }

        public static bool TryGet(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            if (currentSnapshot.TryGet(key, out string source))
            {
                value = GameLocalization.Get(key, source);
                return true;
            }

            return GameLocalization.TryGet(key, out value);
        }

        public static string GetAudioEventId(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            return currentSnapshot.TryGetAudioEventId(key, out string value)
                ? value
                : fallback ?? string.Empty;
        }

        public static bool TryGetAudioEventId(string key, out string audioEventId)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                audioEventId = string.Empty;
                return false;
            }

            return currentSnapshot.TryGetAudioEventId(key, out audioEventId);
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            return GameLocalization.Format(key, Get(key, fallback), args);
        }

        public static bool IsInitialized => currentSnapshot.IsInitialized;

        private sealed class GameTextCatalogSnapshot
        {
            public static readonly GameTextCatalogSnapshot Uninitialized = new(
                false,
                new Dictionary<string, string>(StringComparer.Ordinal),
                new Dictionary<string, string>(StringComparer.Ordinal));

            private readonly Dictionary<string, string> _entries;
            private readonly Dictionary<string, string> _audioEventIds;

            private GameTextCatalogSnapshot(
                bool isInitialized,
                Dictionary<string, string> entries,
                Dictionary<string, string> audioEventIds)
            {
                IsInitialized = isInitialized;
                _entries = entries;
                _audioEventIds = audioEventIds;
            }

            public bool IsInitialized { get; }

            public static GameTextCatalogSnapshot Create(
                Dictionary<string, string> entries,
                Dictionary<string, string> audioEventIds)
            {
                return new GameTextCatalogSnapshot(
                    true,
                    entries,
                    audioEventIds);
            }

            public bool TryGet(string key, out string value)
            {
                return TryGetValue(_entries, key, out value);
            }

            public bool TryGetAudioEventId(string key, out string value)
            {
                return TryGetValue(_audioEventIds, key, out value);
            }

            private static bool TryGetValue(
                Dictionary<string, string> entries,
                string key,
                out string value)
            {
                return entries.TryGetValue(key, out value);
            }
        }
    }
}
