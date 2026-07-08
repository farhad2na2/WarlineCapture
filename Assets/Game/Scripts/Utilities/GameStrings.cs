using Game.Configs;

namespace Game.Runtime
{
    public static class GameStrings
    {
            public static void Init(GameStringsConfig config)
            {
                GameText.Init(config);
            }

            public static string Get(string key)
            {
                return GameText.Get(key, key);
            }

            public static bool TryGet(string key, out string value)
            {
                return GameText.TryGet(key, out value);
            }

            public static string Format(string key, params object[] args)
            {
                return GameText.Format(key, key, args);
            }

            public static string Format(string key, string fallback, params object[] args)
            {
                return GameText.Format(key, fallback, args);
            }

            public static bool IsInitialized => GameText.IsInitialized;
    }
}
