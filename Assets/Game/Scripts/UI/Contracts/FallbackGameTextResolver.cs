using System;

namespace Game.UI.Contracts
{
    public sealed class FallbackGameTextResolver : IGameTextResolver
    {
        public static FallbackGameTextResolver Instance { get; } = new();

        private FallbackGameTextResolver()
        {
        }

        public string Get(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            return fallback ?? key;
        }

        public bool TryGet(string key, out string value)
        {
            value = string.Empty;
            return false;
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
