using System.Text;
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
        [SerializeField] private float minimumPresentationSize,maximumPresentationSize;
        private bool meshRefreshQueued;
        private TextOverflowModes sourceOverflow;

        public string LocalizationKey => localizationKey;
        public string EnglishFallback => englishFallback;
        public string SourceValue => hasRuntimeSource ? runtimeSource : englishFallback;

        public void Configure(string key, string fallback, bool observeRuntimeChanges = true)
        {
            localizationKey = key ?? string.Empty;
            englishFallback = fallback ?? string.Empty;
            observeRuntimeSourceChanges = observeRuntimeChanges;
            runtimeSource = null;
            hasRuntimeSource = false;
            lastApplied = null;
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
            QueueMeshRefresh();
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= RefreshLocalizedMesh;
            meshRefreshQueued = false;
            UiShellRuntimeGateway.Localization.LocaleChanged -= ApplyLocalization;
            if(target is Graphic graphic) graphic.UnregisterDirtyVerticesCallback(ObserveRuntimeSource);
        }

        private void ObserveRuntimeSource()
        {
            if (applyingLocalization || !observeRuntimeSourceChanges || target == null || specializedOwner)
                return;
            // The zoom cue inserts its own line break while fitting. That edit is layout,
            // not a new translation. Reapplying here turned auto-size back on at 16–26 and
            // the single-line fit settled at 17–22.
            if (target.name == "ZoomCue")
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

            localized = StripMissingFormatGlyphs(localized);

            bool rightToLeft = UiShellRuntimeGateway.Localization.IsRightToLeft;
            // A translated label owns its arrow direction (including Previous). Only
            // English fallbacks and runtime text need automatic forward-arrow mirroring.
            if (string.Equals(localized, source, System.StringComparison.Ordinal))
                localized = UiLocalizedText.MirrorForwardArrows(localized, rightToLeft);
            bool containsRightToLeftText = rightToLeft && ContainsArabicScript(localized);
            TMP_FontAsset localeFont = UiShellRuntimeGateway.Localization.CurrentFontAsset as TMP_FontAsset;
            TMP_FontAsset nextFont = localeFont != null && (containsRightToLeftText || !rightToLeft) ? localeFont : sourceFont;
            if (target.font != nextFont)
                QueueMeshRefresh();
            target.font = nextFont;
            // A serialized material from the previous font can leave correctly shaped
            // Farsi characters sampling the Latin atlas after an authored popup rebuild.
            if(target.font!=null && target.font.material!=null && target.fontSharedMaterial!=null &&
                target.fontSharedMaterial.mainTexture!=target.font.material.mainTexture)
                target.fontSharedMaterial=target.font.material;
            target.alignment = rightToLeft ? Mirror(sourceAlignment) : sourceAlignment;
            ApplyLocaleSizing(containsRightToLeftText);

            if (target is RTLTextMeshPro rtlText)
            {
                rtlText.Farsi = containsRightToLeftText;
                rtlText.PreserveNumbers = !ContainsEasternDigits(localized);
                rtlText.ForceFix = containsRightToLeftText;
                // Keep scenario ids such as S004 in source order inside Persian sentences.
                rtlText.text = containsRightToLeftText ? IsolateLatinRuns(localized) : localized;
                lastApplied = rtlText.OriginalText;
                return;
            }

            // Codes, filenames, acronyms, and callsigns can intentionally remain Latin in an RTL
            // locale. Do not reverse those strings (for example APC must not become CPA).
            target.isRightToLeftText = containsRightToLeftText;
            target.text = containsRightToLeftText ? ShapeForRendering(IsolateLatinRuns(localized)) : localized;
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
            sourceOverflow = target.overflowMode;
            hasPresentationDefaults = true;
            if (string.IsNullOrEmpty(englishFallback))
                englishFallback = ReadAuthoredText();
        }

        private void QueueMeshRefresh()
        {
            if (meshRefreshQueued || !isActiveAndEnabled) return;
            meshRefreshQueued = true;
            Canvas.preWillRenderCanvases += RefreshLocalizedMesh;
        }

        private void RefreshLocalizedMesh()
        {
            Canvas.preWillRenderCanvases -= RefreshLocalizedMesh;
            meshRefreshQueued = false;
            // A popup can localize before TMP finishes its first activation. Rebuild once,
            // after activation, so glyph UVs and the renderer use the same locale atlas.
            if (target != null && target.isActiveAndEnabled && !specializedOwner)
                target.ForceMeshUpdate();
        }

        public void SetLocalizedValue(string value)
        {
            string source = value ?? string.Empty;
            if (UiShellRuntimeGateway.Localization.TryGetSourceByLocalized(source, out _, out string authoredSource))
                source = authoredSource;
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
                if (containsRightToLeftText)
                    FitArabicLineBox();
                return;
            }
            if (!containsRightToLeftText)
            {
                target.overflowMode = sourceOverflow;
                target.enableAutoSizing = sourceAutoSizing;
                target.fontSize = sourceFontSize*presentationScale;
                target.fontSizeMin = sourceFontSizeMin*presentationScale;
                target.fontSizeMax = sourceFontSizeMax*presentationScale;
                return;
            }

            // Noto Arabic's line box is taller than the Latin authoring font. A short rect with
            // Ellipsis rejects the entire line, which shows up as an empty label after Farsi.
            target.enableAutoSizing = true;
            float authoredMax = (sourceAutoSizing ? sourceFontSizeMax : sourceFontSize) * presentationScale;
            float authoredMin = sourceAutoSizing
                ? sourceFontSizeMin * presentationScale
                : Mathf.Min(sourceFontSize, Mathf.Max(8f, sourceFontSize * 0.55f)) * presentationScale;
            FitArabicLineBox(ref authoredMin, ref authoredMax);
            if (authoredMin > authoredMax)
                authoredMin = authoredMax;
            target.fontSizeMax = authoredMax;
            target.fontSizeMin = authoredMin;
        }

        private void FitArabicLineBox()
        {
            float authoredMin = target.fontSizeMin;
            float authoredMax = target.fontSizeMax;
            FitArabicLineBox(ref authoredMin, ref authoredMax);
            if (authoredMin > authoredMax)
                authoredMin = authoredMax;
            target.fontSizeMin = authoredMin;
            target.fontSizeMax = authoredMax;
        }

        private void FitArabicLineBox(ref float authoredMin, ref float authoredMax)
        {
            // The zoom tab owns a 16–26px lock. This fitter's 12px short-rect floor
            // was the size that reached the screen on the 4:3 Farsi cue.
            if (target != null && target.name == "ZoomCue")
                return;
            float height = target.rectTransform != null ? target.rectTransform.rect.height : 0f;
            if (height < 16f)
                return;
            float fittedMax = Mathf.Max(14f, height / 1.65f);
            authoredMax = Mathf.Min(authoredMax, fittedMax);
            authoredMin = Mathf.Min(authoredMin, Mathf.Max(height >= 32f ? 15f : 12f, authoredMax * 0.62f));
            target.overflowMode = TextOverflowModes.Overflow;
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
            value = StripMissingFormatGlyphs(value);
            if (string.IsNullOrEmpty(value) || !ContainsArabicScript(value))
                return value ?? string.Empty;

            FastStringBuilder output = new(Mathf.Max(RTLSupport.DefaultBufferSize, value.Length * 4));
            RTLSupport.FixRTL(value, output, farsi: true, fixTextTags: true, preserveNumbers: !ContainsEasternDigits(value));
            output.Reverse();
            return output.ToString();
        }
        public void SetFontBounds(float minimum,float maximum)
        {minimumPresentationSize=Mathf.Max(8,minimum); maximumPresentationSize=Mathf.Max(minimumPresentationSize,maximum); ApplyLocalization();}
        public void ConfigureFontBounds(float minimum,float maximum)
        {minimumPresentationSize=Mathf.Max(8,minimum); maximumPresentationSize=Mathf.Max(minimumPresentationSize,maximum);}
        /// <summary>
        /// ZWNJ, ZWJ, bidi isolates and a few Arabic punctuation marks are absent from
        /// the Farsi TMP atlas and draw as a missing-glyph box.
        /// </summary>
        private static string StripMissingFormatGlyphs(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;
            bool dirty = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (IsMissingFormatGlyph(value[i]))
                {
                    dirty = true;
                    break;
                }
            }

            if (!dirty)
                return value;
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\u200C' || c == '\u200D' || c == '\u200B' || c == '\uFEFF' || c == '\u2060' || c == '\u061C'
                    || c == '\u200E' || c == '\u200F' || c == '\u202A' || c == '\u202B' || c == '\u202C'
                    || c == '\u202D' || c == '\u202E' || c == '\u2066' || c == '\u2067' || c == '\u2068' || c == '\u2069')
                    continue;
                if (c == '\u2026')
                {
                    builder.Append("...");
                    continue;
                }
                if (c == '\u060C')
                {
                    builder.Append(',');
                    continue;
                }

                if (c == '\u061B')
                {
                    builder.Append(';');
                    continue;
                }

                if (c == '\u061F' || c == '\u06D4')
                {
                    builder.Append(c == '\u061F' ? '?' : '.');
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static bool IsMissingFormatGlyph(char value)
        {
            return value == '\u200C' || value == '\u200D' || value == '\u200B' || value == '\uFEFF'
                || value == '\u200E' || value == '\u200F' || value == '\u2060' || value == '\u061C'
                || value == '\u202A' || value == '\u202B' || value == '\u202C' || value == '\u202D' || value == '\u202E'
                || value == '\u2066' || value == '\u2067' || value == '\u2068' || value == '\u2069'
                || value == '\u2026' || value == '\u060C' || value == '\u061B'
                || value == '\u061F' || value == '\u06D4';
        }

        private static string IsolateLatinRuns(string value)
        {
            // U+2066/U+2069 are missing from the Farsi TMP atlas, so isolation stays in the
            // source string and Latin ids are left for the RTL shaper.
            return value ?? string.Empty;
        }

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
