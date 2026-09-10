using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Generic text binding used by every V3 screen. The prefab stores one stable key and its
    /// English authoring fallback; locale values, direction, and fonts come from the shared catalog.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class V3LocalizedTextBindingView : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField, TextArea] private string englishFallback;
        [SerializeField] private bool observeRuntimeSourceChanges = true;

        private TMP_Text target;
        private bool applyingLocalization;
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
        private bool specializedOwner;
        private float presentationScale=1f;
        private float minimumPresentationSize,maximumPresentationSize;

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
            UiShellRuntimeGateway.Localization.LocaleChanged += ApplyLocalization;
            if(target is Graphic graphic) graphic.RegisterDirtyVerticesCallback(ObserveRuntimeSource);
            ApplyLocalization();
        }

        private void OnDisable()
        {
            UiShellRuntimeGateway.Localization.LocaleChanged -= ApplyLocalization;
            if(target is Graphic graphic) graphic.UnregisterDirtyVerticesCallback(ObserveRuntimeSource);
        }

        private void ObserveRuntimeSource()
        {
            if (applyingLocalization || !observeRuntimeSourceChanges || target == null || specializedOwner)
                return;

            string current = ReadAuthoredText();
            if (string.Equals(current, lastApplied, System.StringComparison.Ordinal))
                return;

            if (UiShellRuntimeGateway.Localization.TryGetSourceByLocalized(
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
            if(applyingLocalization) return;
            applyingLocalization = true;
            try { ApplyLocalizedPresentation(); }
            finally { applyingLocalization = false; }
        }

        private void ApplyLocalizedPresentation()
        {
            CapturePresentationDefaults();
            if (target == null || specializedOwner)
                return;

            string source = hasRuntimeSource
                ? runtimeSource
                : string.IsNullOrEmpty(englishFallback) ? ReadAuthoredText() : englishFallback;
            string localized;
            if (hasRuntimeSource)
            {
                if (!UiShellRuntimeGateway.Localization.TryGetBySource(source, out _, out localized))
                    localized = source;
            }
            else
            {
                localized = UiShellRuntimeGateway.Localization.Get(localizationKey, source);
            }

            bool rightToLeft = UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool containsRightToLeftText = rightToLeft && ContainsArabicScript(localized);
            TMP_FontAsset localeFont = UiShellRuntimeGateway.Localization.CurrentFontAsset as TMP_FontAsset;
            target.font = containsRightToLeftText && localeFont != null ? localeFont : sourceFont;
            target.alignment = rightToLeft ? Mirror(sourceAlignment) : sourceAlignment;
            ApplyLocaleSizing(containsRightToLeftText);

            if (target is RTLTextMeshPro rtlText)
            {
                rtlText.Farsi = containsRightToLeftText;
                rtlText.PreserveNumbers = !ContainsEasternDigits(localized);
                rtlText.ForceFix = containsRightToLeftText;
                rtlText.text = localized;
                lastApplied = rtlText.OriginalText;
                return;
            }

            // Codes, filenames, acronyms, and callsigns can intentionally remain Latin in an RTL
            // locale. Do not reverse those strings (for example APC must not become CPA).
            target.isRightToLeftText = containsRightToLeftText;
            target.text = containsRightToLeftText ? ShapeForRendering(localized) : localized;
            lastApplied = target.text;
        }

        private void CapturePresentationDefaults()
        {
            target ??= GetComponent<TMP_Text>();
            if (target == null || hasPresentationDefaults)
                return;
            specializedOwner=target.GetComponentInParent<AriaTutorialBriefingView>(true)?.OwnsLocalizedText(target)==true;

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

        public void SetLocalizedValue(string value)
        {
            string source = value ?? string.Empty;
            if (hasRuntimeSource && runtimeSource == source &&
                string.Equals(ReadAuthoredText(), lastApplied, System.StringComparison.Ordinal))
                return; // LocaleChanged owns reapplication when the language changes.
            runtimeSource=source;
            hasRuntimeSource=true;
            ApplyLocalization();
        }
        public void SetPresentationScale(float scale)
        {
            presentationScale=Mathf.Clamp(scale,1f,1.5f);
            ApplyLocalization();
        }

        private void ApplyLocaleSizing(bool containsRightToLeftText)
        {
            if(maximumPresentationSize>0)
            {
                target.enableAutoSizing=true;
                target.fontSizeMin=minimumPresentationSize*presentationScale;
                target.fontSizeMax=maximumPresentationSize*presentationScale;
                return;
            }
            if (!containsRightToLeftText)
            {
                target.enableAutoSizing = sourceAutoSizing;
                target.fontSize = sourceFontSize*presentationScale;
                target.fontSizeMin = sourceFontSizeMin*presentationScale;
                target.fontSizeMax = sourceFontSizeMax*presentationScale;
                return;
            }

            // Noto Arabic's ascender/descender line box is taller than the V3 Latin authoring font.
            // Let translated labels shrink within their authored bounds so TMP's Ellipsis mode does
            // not reject the entire line. Existing authored auto-size ranges remain authoritative.
            target.enableAutoSizing = true;
            if (sourceAutoSizing)
            {
                target.fontSizeMin = sourceFontSizeMin*presentationScale;
                target.fontSizeMax = sourceFontSizeMax*presentationScale;
                return;
            }

            target.fontSizeMax = sourceFontSize*presentationScale;
            target.fontSizeMin = Mathf.Min(sourceFontSize, Mathf.Max(8f, sourceFontSize * 0.55f))*presentationScale;
        }

        private string ReadAuthoredText()
        {
            if (target is RTLTextMeshPro rtlText)
                return rtlText.OriginalText;
            return target?.text ?? string.Empty;
        }

        /// <summary>
        /// Converts Arabic-script source text into the presentation forms consumed by the
        /// project's TMP/RTL renderer. The Editor catalog builder uses the same conversion to
        /// bake every required glyph into the shared Persian font asset.
        /// </summary>
        public static string ShapeForRendering(string value)
        {
            if (string.IsNullOrEmpty(value) || !ContainsArabicScript(value))
                return value ?? string.Empty;

            FastStringBuilder output = new(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 4));
            RTLSupport.FixRTL(value, output, farsi: true, fixTextTags: true, preserveNumbers: !ContainsEasternDigits(value));
            output.Reverse();
            return output.ToString();
        }
        public void SetFontBounds(float minimum,float maximum)
        {minimumPresentationSize=Mathf.Max(8,minimum); maximumPresentationSize=Mathf.Max(minimumPresentationSize,maximum); ApplyLocalization();}
        private static bool ContainsArabicScript(string value)
        {
            if(value==null) return false;
            foreach(char c in value) if(c is >= '\u0600' and <= '\u06ff' or >= '\u0750' and <= '\u077f' or >= '\u08a0' and <= '\u08ff' or >= '\ufb50' and <= '\ufdff' or >= '\ufe70' and <= '\ufeff') return true;
            return false;
        }

        private static bool ContainsEasternDigits(string value)
        {
            // RTLTMPro's preserveNumbers mode recognizes only ASCII digits. Native
            // Persian digits require its localized number mode to retain their order.
            foreach(char c in value) if(c is >= '\u06f0' and <= '\u06f9' or >= '\u0660' and <= '\u0669') return true;
            return false;
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
