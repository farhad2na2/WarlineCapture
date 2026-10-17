using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private static string LibraryTitleCopy(string key, string fallback)
        {
            string localized = UiShellRuntimeGateway.Localization.Get(key, string.Empty);
            return string.IsNullOrEmpty(localized) || localized == key ? fallback : localized;
        }

        private static string FormatLibraryTitle(string pattern, string name)
        {
            return pattern.Contains("{0}") ? pattern.Replace("{0}", name) : pattern + " " + name;
        }

        private void PresentScreenHeading(SkirmishTheaterState theater, SkirmishBattlefieldState field, bool fa, bool battlefield)
        {
            TMP_Text screenTitle = transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>();
            string headingMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
            bool hasHeading = TryFindTheaterPin(theater, headingMap, out SkirmishTheaterPinState headingPin);
            string mapTitle = hasHeading ? MapDisplayTitle(headingPin.MapId, fa) : (fa ? "صحنه نبرد" : "THEATRE");
            // approved-1: "SKIRMISH | THEATRE MAP"; Map/Grid: "SKIRMISH | <BATTLEFIELD NAME>" (localised keys, FA mirrored).
            string route = !battlefield
                ? LibraryTitleCopy("ui.skirmish.library.title.theatre_map", fa ? "زد و خورد | نقشه صحنه" : "SKIRMISH | THEATRE MAP")
                : FormatLibraryTitle(LibraryTitleCopy("ui.skirmish.library.title.battlefield", fa ? "زد و خورد | {0}" : "SKIRMISH | {0}"), fa ? mapTitle : mapTitle.ToUpperInvariant());
            SetCopy(screenTitle, MixedText(route, fa), 30f, 50f); // v33: FA same size as EN // approved-1/2/3: large gold title
            if (screenTitle != null)
                screenTitle.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            SetCopy(mapNameText, mapTitle, 18f, 28f);
            if (mapNameText != null)
                mapNameText.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            if (screenTitle != null)
            {
                screenTitle.textWrappingMode = TextWrappingModes.NoWrap;
                screenTitle.overflowMode = TextOverflowModes.Overflow;
            }
        }


        private void PresentWallet()
        {
            MainMenuAccountHeaderView header = GetComponentInChildren<MainMenuAccountHeaderView>(true);
            if (header == null)
                return;
            int credits = 0;
            bool shown = SkirmishLibraryWallet.ReadCredits != null && SkirmishLibraryWallet.ReadCredits(out credits);
            header.enabled = false;
            if (!shown || header.Value == null || HasCommanderIdentity())
                return;
            header.gameObject.SetActive(true);
            header.Value.isRightToLeftText = false;
            header.Value.text = credits.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Game PM: larger zone-rail labels. FinishLibraryText (Library.cs zoneRail branch) relaxes them to 11-16 px;
        /// this runs after it, so re-pin the rail labels at 24 px bold.
        /// </summary>
        private void ReassertRailLabels()
        {
            if (_objectiveColumn == null)
                return;
            for (int i = 0; i < _objectiveColumn.childCount; i++)
            {
                Transform tab = _objectiveColumn.GetChild(i);
                if (!tab.name.StartsWith("ObjectiveTab_", System.StringComparison.Ordinal))
                    continue;
                TMP_Text label = tab.Find("Label")?.GetComponent<TMP_Text>();
                if (label == null)
                    continue;
                label.enableAutoSizing = false;
                label.fontSize = 24f;
                label.fontStyle = FontStyles.Bold;
                label.maxVisibleLines = 2;
            }
        }

        private void PlaceLibraryHeader()
        {
            ReassertRailLabels();
            ReassertFooterLabels();
            FitTheatreDetailHeadings();
            ReassertBriefingPanel();
            ReassertTheatreColumn();
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition is not RectTransform compositionRect)
                return;
            float width = compositionRect.rect.width;
            if (width < 200f)
                width = compositionRect.sizeDelta.x > 200f ? compositionRect.sizeDelta.x : 1672f;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float settingsWidth = 72f;
            bool showChip = HasCommanderIdentity();
            float chipWidth = showChip ? Mathf.Clamp(width * 0.22f, 240f, 360f) : 0f;
            float logoWidth = Mathf.Clamp(width * 0.24f, 220f, 400f);
            const float logoHeight = 96f;
            const float logoX = 18f;
            const float gap = 8f;
            float settingsX = width - 12f - settingsWidth;
            float chipX = settingsX - gap - chipWidth;
            float titleX = logoX + logoWidth + gap;
            float titleWidth = Mathf.Max(140f, chipX - gap - titleX);
            if (composition.Find("WarlineLogo") is RectTransform logo)
                FitBrandLogo(logo, logoX, 6f, logoWidth, logoHeight);

            if (composition.Find("ScreenTitlePanel") is RectTransform title)
            {
                PlaceTopLeft(title, titleX, 10f, titleWidth, 78f);
                if (title.Find("ScreenTitle") is RectTransform screenTitle)
                {
                    screenTitle.anchorMin = Vector2.zero;
                    screenTitle.anchorMax = Vector2.one;
                    // FA: the binding's Arabic fit caps size at rect height / 1.65, so give the Noto line a taller box.
                    screenTitle.offsetMin = new Vector2(52f, fa ? -16f : 2f);
                    screenTitle.offsetMax = new Vector2(-8f, fa ? 16f : -2f);
                    TMP_Text screenTitleText = screenTitle.GetComponent<TMP_Text>();
                    if (screenTitleText != null)
                    {
                        screenTitleText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                        screenTitleText.isRightToLeftText = fa;
                        screenTitleText.textWrappingMode = TextWrappingModes.NoWrap;
                        screenTitleText.overflowMode = TextOverflowModes.Ellipsis;
                        screenTitleText.enableAutoSizing = true;
                        // approved-1/2/3: the gold title fills the 78 px header box.
                        screenTitleText.fontSizeMin = fa ? 36f : 28f;
                        screenTitleText.fontSizeMax = fa ? 64f : 50f;
                        screenTitleText.fontStyle = FontStyles.Bold;
                        screenTitleText.margin = Vector4.zero;
                        V3LocalizedTextBindingView titleBinding = screenTitleText.GetComponent<V3LocalizedTextBindingView>();
                        if (titleBinding != null)
                            {
                            titleBinding.ConfigureFontBounds(fa ? 36f : 28f, fa ? 64f : 50f);
                            if (fa)
                                titleBinding.ApplyLocalization(); // re-pick the locale (Noto) font and re-fit to the taller box
                            // ApplyLocalization re-mirrors the captured source alignment (V3LocalizedTextBindingView
                            // ApplyLocalizedPresentation), which lands FA on the left beside the crest; mirror explicitly.
                            screenTitleText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                        }
                        // Runs after EnforceLibraryFontFloor/FinishLibraryText: re-assert gold, and in FA pin the size,
                        // since the binding's Arabic line-box fit re-autosizes the Noto face small.
                        screenTitleText.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                        if (fa)
                            screenTitleText.overflowMode = TextOverflowModes.Overflow; // Ellipsis drops the whole Noto Arabic line
                    }
                }
            }

            Transform settings = FindDeep(transform, "SettingsButton");
            if (settings is RectTransform settingsRect)
            {
                if (settings.parent != composition)
                    settings.SetParent(composition, false);
                settings.gameObject.SetActive(true);
                PlaceTopLeft(settingsRect, settingsX, 12f, settingsWidth, 72f);
            }

            MainMenuAccountHeaderView header = GetComponentInChildren<MainMenuAccountHeaderView>(true);
            if (header == null)
                return;
            header.enabled = false;
            header.gameObject.SetActive(showChip);
            if (!showChip)
                return;
            if (header.transform is RectTransform chip)
            {
                if (chip.parent != composition)
                    chip.SetParent(composition, false);
                PlaceTopLeft(chip, chipX, 10f, chipWidth, 88f);
                Image chipImage = chip.GetComponent<Image>();
                if (chipImage != null)
                {
                    chipImage.enabled = true;
                    if (chipImage.color.a < 0.98f || SkirmishLibraryBlankImage.IsLightNeutral(chipImage.color))
                        chipImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                }
            }

            if (header.transform.Find("Icon") is RectTransform icon)
                PlaceTopLeft(icon, 10f, 16f, 56f, 56f);
            BindCommanderChip(header, fa, chipWidth);
        }

        private static void FitBrandLogo(RectTransform logo, float x, float y, float width, float height)
        {
            if (logo == null)
                return;
            logo.gameObject.SetActive(true);
            PlaceTopLeft(logo, x, y, width, height);
            if (logo.childCount == 0)
                return;
            RectTransform mark = logo.GetChild(0) as RectTransform;
            if (mark == null)
                return;
            float nativeWidth = mark.sizeDelta.x > 64f ? mark.sizeDelta.x : 513f;
            float nativeHeight = mark.sizeDelta.y > 32f ? mark.sizeDelta.y : 137f;
            float scale = Mathf.Min(
                Mathf.Max(1f, width - 16f) / nativeWidth,
                Mathf.Max(1f, height - 12f) / nativeHeight);
            mark.anchorMin = new Vector2(0.5f, 0.5f);
            mark.anchorMax = new Vector2(0.5f, 0.5f);
            mark.pivot = new Vector2(0.5f, 0.5f);
            mark.sizeDelta = new Vector2(nativeWidth, nativeHeight);
            mark.localScale = new Vector3(scale, scale, 1f);
            mark.anchoredPosition = Vector2.zero;
        }

        private static bool HasCommanderIdentity()
        {
            if (!UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile))
                return false;
            return !string.IsNullOrWhiteSpace(profile.Name);
        }

        private void BindCommanderChip(MainMenuAccountHeaderView header, bool fa, float chipWidth)
        {
            if (header == null || !UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile))
                return;
            string name = profile.Name ?? string.Empty;
            if (string.Equals(name, "Commander", StringComparison.OrdinalIgnoreCase) && fa)
                name = "فرمانده";
            Transform label = header.transform.Find("Label");
            if (label != null)
                label.gameObject.SetActive(true);
            if (label is RectTransform labelRect)
                PlaceTopLeft(labelRect, 74f, 8f, Mathf.Max(96f, chipWidth - 88f), 34f);
            TMP_Text caption = label != null ? label.GetComponent<TMP_Text>() : null;
            if (caption != null)
            {
                SetCopy(caption, name, 20f, 26f);
                caption.alignment = ContainsArabicScript(name) ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.isRightToLeftText = ContainsArabicScript(caption.text);
                caption.overflowMode = TextOverflowModes.Ellipsis;
                caption.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            }

            if (header.Value == null)
                return;
            PlaceTopLeft(header.Value.rectTransform, 74f, 44f, Mathf.Max(96f, chipWidth - 88f), 36f);
            string level = "Lv " + profile.Level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SetCopy(header.Value, level, 22f, 30f);
            header.Value.isRightToLeftText = false;
            header.Value.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            header.Value.overflowMode = TextOverflowModes.Overflow;
            header.Value.text = level;
        }
    }
}
