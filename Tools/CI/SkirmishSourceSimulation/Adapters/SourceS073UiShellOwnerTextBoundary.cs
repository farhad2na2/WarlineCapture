namespace Game.Configs
{
    // UI text lookup is not part of the non-native source host. Preserve the
    // original owner's authored fallback strings without claiming localization.
    internal static class GameText
    {
        internal static string Get(string key, string fallback) => fallback ?? string.Empty;
    }
}
