using Game.Configs;
using RTLTMPro;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Generic text binding used by every V3 screen. The prefab stores one stable key and its
    /// English authoring fallback; locale values, direction, and fonts come from the shared catalog.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class V3LocalizedTextBinding : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField, TextArea] private string englishFallback;
        [SerializeField] private bool observeRuntimeSourceChanges = true;

        private TMP_Text target;
        private TMP_FontAsset sourceFont;
        private TextAlignmentOptions sourceAlignment;
        private bool sourceAutoSizing;
        private float sourceFontSize;
        private float sourceFontSizeMin;
        private float sourceFontSizeMax;
        private bool hasPresentationDefaults;
        private string runtimeSource;
        private string lastApplied;
        private bool hasRuntimeSource;

        public string LocalizationKey => localizationKey;
        public string EnglishFallback => englishFallback;

        public void Configure(string key, string fallback, bool observeRuntimeChanges = true)
        {
            localizationKey = key ?? string.Empty;
            englishFallback = fallback ?? string.Empty;
            observeRuntimeSourceChanges = observeRuntimeChanges;
            // Prefab builders configure bindings in edit mode. Never rewrite authored TMP text from
            // the Editor's current PlayerPrefs locale while saving a prefab.
            if (Application.isPlaying && isActiveAndEnabled)
                ApplyLocalization();
        }

        private void Awake()
        {
            CapturePresentationDefaults();
        }

        private void OnEnable()
        {
            CapturePresentationDefaults();
            GameLocalization.LocaleChanged += ApplyLocalization;
            ApplyLocalization();
        }

        private void OnDisable()
        {
            GameLocalization.LocaleChanged -= ApplyLocalization;
        }

        private void LateUpdate()
        {
            if (!observeRuntimeSourceChanges || target == null)
                return;

            string current = ReadAuthoredText();
            if (string.Equals(current, lastApplied, System.StringComparison.Ordinal))
                return;

            if (GameLocalization.TryGetSourceByLocalized(
                    current,
                    out string resolvedKey,
                    out string resolvedSource))
            {
                localizationKey = resolvedKey;
                runtimeSource = resolvedSource;
            }
            else
            {
                runtimeSource = current ?? string.Empty;
            }
            hasRuntimeSource = true;
            ApplyLocalization();
        }

        public void ApplyLocalization()
        {
            CapturePresentationDefaults();
            if (target == null)
                return;

            string source = hasRuntimeSource
                ? runtimeSource
                : string.IsNullOrEmpty(englishFallback) ? ReadAuthoredText() : englishFallback;
            string localized;
            if (hasRuntimeSource)
            {
                if (!GameLocalization.TryGetBySource(source, out _, out localized))
                    localized = source;
            }
            else
            {
                localized = GameLocalization.Get(localizationKey, source);
            }

            bool rightToLeft = GameLocalization.IsRightToLeft;
            bool containsRightToLeftText = rightToLeft && TextUtils.IsRTLInput(localized);
            TMP_FontAsset localeFont = GameLocalization.CurrentFontAsset as TMP_FontAsset;
            target.font = containsRightToLeftText && localeFont != null ? localeFont : sourceFont;
            target.alignment = rightToLeft ? Mirror(sourceAlignment) : sourceAlignment;
            ApplyLocaleSizing(containsRightToLeftText);

            if (target is RTLTextMeshPro rtlText)
            {
                rtlText.Farsi = containsRightToLeftText;
                rtlText.PreserveNumbers = true;
                rtlText.ForceFix = containsRightToLeftText;
                rtlText.text = localized;
                lastApplied = rtlText.OriginalText;
                return;
            }

            // Codes, filenames, acronyms, and callsigns can intentionally remain Latin in an RTL
            // locale. Do not reverse those strings (for example APC must not become CPA).
            target.isRightToLeftText = containsRightToLeftText;
            target.text = containsRightToLeftText ? ShapePersian(localized) : localized;
            lastApplied = target.text;
        }

        private void CapturePresentationDefaults()
        {
            target ??= GetComponent<TMP_Text>();
            if (target == null || hasPresentationDefaults)
                return;

            sourceFont = target.font;
            sourceAlignment = target.alignment;
            sourceAutoSizing = target.enableAutoSizing;
            sourceFontSize = target.fontSize;
            sourceFontSizeMin = target.fontSizeMin;
            sourceFontSizeMax = target.fontSizeMax;
            hasPresentationDefaults = true;
            if (string.IsNullOrEmpty(englishFallback))
                englishFallback = ReadAuthoredText();
        }

        private void ApplyLocaleSizing(bool containsRightToLeftText)
        {
            if (!containsRightToLeftText)
            {
                target.enableAutoSizing = sourceAutoSizing;
                target.fontSize = sourceFontSize;
                target.fontSizeMin = sourceFontSizeMin;
                target.fontSizeMax = sourceFontSizeMax;
                return;
            }

            // Noto Arabic's ascender/descender line box is taller than the V3 Latin authoring font.
            // Let translated labels shrink within their authored bounds so TMP's Ellipsis mode does
            // not reject the entire line. Existing authored auto-size ranges remain authoritative.
            target.enableAutoSizing = true;
            if (sourceAutoSizing)
            {
                target.fontSizeMin = sourceFontSizeMin;
                target.fontSizeMax = sourceFontSizeMax;
                return;
            }

            target.fontSizeMax = sourceFontSize;
            target.fontSizeMin = Mathf.Min(sourceFontSize, Mathf.Max(8f, sourceFontSize * 0.55f));
        }

        private string ReadAuthoredText()
        {
            if (target is RTLTextMeshPro rtlText)
                return rtlText.OriginalText;
            return target?.text ?? string.Empty;
        }

        private static string ShapePersian(string value)
        {
            if (string.IsNullOrEmpty(value) || !TextUtils.IsRTLInput(value))
                return value ?? string.Empty;

            FastStringBuilder output = new(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 4));
            RTLSupport.FixRTL(value, output, farsi: true, fixTextTags: true, preserveNumbers: true);
            output.Reverse();
            return output.ToString();
        }

        private static TextAlignmentOptions Mirror(TextAlignmentOptions alignment)
        {
            return alignment switch
            {
                TextAlignmentOptions.TopLeft => TextAlignmentOptions.TopRight,
                TextAlignmentOptions.TopRight => TextAlignmentOptions.TopLeft,
                TextAlignmentOptions.Left => TextAlignmentOptions.Right,
                TextAlignmentOptions.Right => TextAlignmentOptions.Left,
                TextAlignmentOptions.BottomLeft => TextAlignmentOptions.BottomRight,
                TextAlignmentOptions.BottomRight => TextAlignmentOptions.BottomLeft,
                TextAlignmentOptions.BaselineLeft => TextAlignmentOptions.BaselineRight,
                TextAlignmentOptions.BaselineRight => TextAlignmentOptions.BaselineLeft,
                TextAlignmentOptions.MidlineLeft => TextAlignmentOptions.MidlineRight,
                TextAlignmentOptions.MidlineRight => TextAlignmentOptions.MidlineLeft,
                TextAlignmentOptions.CaplineLeft => TextAlignmentOptions.CaplineRight,
                TextAlignmentOptions.CaplineRight => TextAlignmentOptions.CaplineLeft,
                _ => alignment
            };
        }
    }
}
