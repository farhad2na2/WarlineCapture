using System.Text.RegularExpressions;
using TMPro;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Explicit presentation boundary for authored catalog text. Never use for player-entered names.
    /// Keeps the source on the binding so an open screen can be translated again on locale changes.
    /// </summary>
    public static class UiLocalizedText
    {
        private static readonly Regex PositionedName = new(@"^(.+) \((-?\d+),(-?\d+)\)$");

        // Forward navigation follows the reading direction. Ignore TMP rich-text tags,
        // whose closing '>' is markup rather than a visible arrow.
        public static string MirrorForwardArrows(string value, bool rightToLeft)
        {
            if (!rightToLeft || string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var result = new System.Text.StringBuilder(value.Length);
            bool inTag = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '<' && !inTag && i + 1 < value.Length &&
                    (char.IsLetter(value[i + 1]) || value[i + 1] == '/' && i + 2 < value.Length && char.IsLetter(value[i + 2])) &&
                    value.IndexOf('>', i + 1) > i)
                    inTag = true;
                if (!inTag)
                    result.Append(character switch
                    {
                        '>' => '<',
                        '›' => '‹',
                        '→' => '←',
                        '▶' => '◀',
                        _ => character
                    });
                else
                    result.Append(character);
                if (inTag && character == '>') inTag = false;
            }
            return result.ToString();
        }

        public static void Set(TMP_Text target, string source)
        {
            if (target == null) return;
            var binding = target.GetComponent<V3LocalizedTextBindingView>() ??
                          target.gameObject.AddComponent<V3LocalizedTextBindingView>();
            binding.SetLocalizedValue(source ?? string.Empty);
        }

        public static string CatalogLabel(string source) => UiShellRuntimeGateway.Localization.GetBySource(source ?? string.Empty);

        public static string SelectionLabel(string source)
        {
            source ??= string.Empty;
            var match = PositionedName.Match(source);
            return match.Success
                ? $"{CatalogLabel(match.Groups[1].Value)} ({match.Groups[2].Value},{match.Groups[3].Value})"
                : CatalogLabel(source);
        }

        public static string PlacementStatus(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            int separator = source.IndexOf(": ", System.StringComparison.Ordinal);
            return separator < 0 ? CatalogLabel(source)
                : CatalogLabel(source.Substring(0, separator)) + ": " + CatalogLabel(source.Substring(separator + 2));
        }
    }
}
