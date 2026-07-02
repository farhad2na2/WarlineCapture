using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Configs;

namespace Game.Runtime
{
    public static class GameStrings
    {
            private static readonly Dictionary<string, string> Entries = new(StringComparer.Ordinal);
            private static bool _initialized;

            public static void Init(GameStringsConfig config)
            {
                Entries.Clear();
                _initialized = true;

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

            public static string Get(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return string.Empty;

                if (Entries.TryGetValue(key, out string value))
                    return value;

                return key;
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

            public static string Format(string key, params object[] args)
            {
                string format = Get(key);
                if (string.IsNullOrEmpty(format))
                    return format;

                if (args == null || args.Length == 0)
                    return format;

                try
                {
                    return string.Format(format, args);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"[GameStrings] Invalid format for key '{key}': {format}");
                    return format;
                }
            }

            public static bool IsInitialized => _initialized;
    }
}
