using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public static class GameText
    {
        private static readonly Dictionary<string, string> Entries = new(StringComparer.Ordinal);
        private static bool initialized;

        public static void Init(GameStringsConfig config)
        {
            Entries.Clear();
            initialized = true;

            if (config == null || config.Entries == null)
                return;

            for (int i = 0; i < config.Entries.Count; i++)
            {
                GameStringConfigEntry entry = config.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                Entries[entry.Key] = entry.Value ?? string.Empty;
            }
        }

        public static string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            if (Entries.TryGetValue(key, out string value))
                return value;

            return fallback ?? key;
        }

        public static bool TryGet(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return Entries.TryGetValue(key, out value);
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            string format = Get(key, fallback);
            if (string.IsNullOrEmpty(format) || args == null || args.Length == 0)
                return format;

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[GameText] Invalid format for key '{key}': {format}");
                return fallback ?? format;
            }
        }

        public static bool IsInitialized => initialized;
    }
}
