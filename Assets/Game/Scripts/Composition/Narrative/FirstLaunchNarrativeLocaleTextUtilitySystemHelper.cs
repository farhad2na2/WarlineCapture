using System;
using System.Collections.Generic;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeLocaleTextUtilitySystemHelper : IGameTextResolver
    {
        private readonly IGameTextResolver fallbackResolver;
        private readonly Dictionary<string, string> entries = new(StringComparer.Ordinal);

        public FirstLaunchNarrativeLocaleTextUtilitySystemHelper(
            IGameTextResolver fallback,
            NarrativeLocaleConfig locale)
        {
            fallbackResolver = fallback ?? FallbackGameTextResolver.Instance;
            if (locale == null)
                return;

            IReadOnlyList<NarrativeLocaleTextRecord> source = locale.Text;
            for (int i = 0; i < source.Count; i++)
            {
                NarrativeLocaleTextRecord entry = source[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                    continue;
                entries[entry.Key] = entry.Value ?? string.Empty;
            }
        }

        public string Get(string key, string fallback = "")
        {
            if (!string.IsNullOrWhiteSpace(key) && entries.TryGetValue(key, out string value))
                return value;
            return fallbackResolver.Get(key, fallback);
        }

        public bool TryGet(string key, out string value)
        {
            if (!string.IsNullOrWhiteSpace(key) && entries.TryGetValue(key, out value))
                return true;
            return fallbackResolver.TryGet(key, out value);
        }

        public string Format(string key, string fallback, params object[] args)
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
                return fallback ?? format;
            }
        }
    }
}
