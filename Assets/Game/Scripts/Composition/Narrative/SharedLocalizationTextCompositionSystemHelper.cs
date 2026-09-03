using System;
using Game.Configs;
using Game.UI.Contracts;

namespace Game.Composition
{
    /// <summary>
    /// Makes the shared locale catalog authoritative while retaining an older narrative locale
    /// resolver as a safe migration fallback for voice/legacy content not imported yet.
    /// </summary>
    internal sealed class SharedLocalizationTextCompositionSystemHelper : IGameTextResolver
    {
        private readonly IGameTextResolver fallbackResolver;

        public SharedLocalizationTextCompositionSystemHelper(IGameTextResolver fallback)
        {
            fallbackResolver = fallback ?? FallbackGameTextResolver.Instance;
        }

        public string Get(string key, string fallback)
        {
            string resolvedFallback = fallbackResolver.Get(key, fallback);
            return GameLocalization.Get(key, resolvedFallback);
        }

        public bool TryGet(string key, out string value)
        {
            if (GameLocalization.TryGet(key, out value))
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
