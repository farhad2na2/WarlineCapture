using System.Globalization;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        /// <summary>
        /// v49e (P1, Game PM item 4): approved-2 header "SKIRMISH | THEATRE MAP > DESERT BASE" breadcrumb and the CREDITS chip.
        /// P2 calls Apply once from PlaceLibraryHeader(), after the SettingsButton placement and before the commander-chip
        /// block (that block returns early when there is no commander identity):
        ///     LibraryHeaderBreadcrumb.Apply(this, compositionRect, fa, titleX, titleWidth, chipX, chipWidth);
        /// Breadcrumb: root = the catalog's theatre-map title segment (ui.skirmish.library.title.theatre_map after "|":
        /// "THEATRE MAP" / "نقشه صحنه"), leaf = MapDisplayTitle of the open battlefield (the same district name the title
        /// already shows), joined into ui.skirmish.library.title.battlefield. Battlefield level only; kept only if it fits the
        /// title box at the title's minimum size, else the existing title stays (crumbFits=0).
        /// Credits: SkirmishLibraryWallet.ReadCredits; false (or no reader) means no chip at all. The chip takes the right end
        /// of the title band (titleX..titleX+titleWidth), so it never reaches the commander chip (chipX) or SettingsButton.
        /// </summary>
        internal static class LibraryHeaderBreadcrumb
        {
            internal const string CreditsChipName = "LibraryCreditsChip";
            private const float Gap = 8f;
            private const float ChipTop = 10f;
            private const float ChipHeight = 78f;
            private const float MinTitleRoom = 300f;

            internal static void Apply(QuickCustomScreenView view, RectTransform composition, bool fa, float titleX, float titleWidth, float chipX, float chipWidth)
            {
                if (view == null || composition == null)
                    return;
                RectTransform titlePanel = composition.Find("ScreenTitlePanel") as RectTransform;
                TMP_Text title = titlePanel != null ? titlePanel.Find("ScreenTitle")?.GetComponent<TMP_Text>() : null;

                // CREDITS chip: right end of the title band, title panel narrowed to make room.
                int credits = 0;
                bool read = SkirmishLibraryWallet.ReadCredits != null && SkirmishLibraryWallet.ReadCredits(out credits);
                float creditsWidth = Mathf.Clamp(titleWidth * 0.3f, 180f, 240f);
                bool room = titleWidth - creditsWidth - Gap >= MinTitleRoom;
                bool creditsShown = read && room;
                float panelWidth = creditsShown ? titleWidth - creditsWidth - Gap : titleWidth;
                if (titlePanel != null)
                    PlaceTopLeft(titlePanel, titleX, 10f, panelWidth, 78f);
                RectTransform creditsChip = PresentCreditsChip(view, composition, creditsShown, credits, titleX + panelWidth + Gap, creditsWidth, fa);

                // Breadcrumb in the title (Battlefield level only).
                string crumb = "-";
                bool crumbFits = true;
                if (title != null && view.LibraryLevel == SkirmishLibraryLevel.Battlefield && view._libraryViewModel != null)
                {
                    SkirmishTheaterState theater = view._libraryViewModel.Theater;
                    SkirmishBattlefieldState field = view._libraryViewModel.Battlefield;
                    string mapId = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
                    string root = TheatreRoot(fa);
                    if (TryFindTheaterPin(theater, mapId, out SkirmishTheaterPinState pin) && !string.IsNullOrEmpty(root))
                    {
                        string leaf = MapDisplayTitle(pin.MapId, fa);
                        if (!fa)
                            leaf = leaf.ToUpperInvariant();
                        crumb = root + (fa ? " < " : " > ") + leaf;
                        string pattern = LibraryTitleCopy("ui.skirmish.library.title.battlefield", fa ? "زد و خورد | {0}" : "SKIRMISH | {0}");
                        float min = title.fontSizeMin;
                        float max = title.fontSizeMax;
                        view.SetCopy(title, MixedText(FormatLibraryTitle(pattern, crumb), fa), min, max);
                        float available = panelWidth - Mathf.Max(0f, title.rectTransform.offsetMin.x) - Mathf.Max(0f, -title.rectTransform.offsetMax.x);
                        crumbFits = WidthAtSize(title, title.fontSizeMin) <= available;
                        if (!crumbFits)
                            view.SetCopy(title, MixedText(FormatLibraryTitle(pattern, leaf), fa), min, max); // PresentScreenHeading's title
                        title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                        title.isRightToLeftText = fa;
                        title.textWrappingMode = TextWrappingModes.NoWrap;
                        title.overflowMode = TextOverflowModes.Overflow;
                        title.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                    }
                }

                string overlap = Overlaps(composition, creditsChip, titlePanel, chipX, chipWidth);
                Debug.Log("[LibraryHeader] locale=" + (fa ? "fa-IR" : "en") + " ratio=" + view.TheatreRatioLabel() +
                    " crumb=" + crumb + " crumbFits=" + (crumbFits ? 1 : 0) +
                    " credits=" + (read ? credits.ToString(CultureInfo.InvariantCulture) : "none") +
                    " creditsShown=" + (creditsShown ? 1 : 0) + (read && !room ? " creditsRoom=0" : string.Empty) +
                    " overlap=" + overlap);
            }

            /// <summary>"THEATRE MAP" / "نقشه صحنه": the catalog theatre-map title after its "|" separator.</summary>
            private static string TheatreRoot(bool fa)
            {
                string full = LibraryTitleCopy("ui.skirmish.library.title.theatre_map", fa ? "زد و خورد | نقشه صحنه" : "SKIRMISH | THEATRE MAP");
                int bar = full.LastIndexOf('|');
                return (bar >= 0 ? full.Substring(bar + 1) : string.Empty).Trim();
            }

            private static float WidthAtSize(TMP_Text text, float size)
            {
                bool auto = text.enableAutoSizing;
                float current = text.fontSize;
                text.enableAutoSizing = false;
                text.fontSize = size;
                float width = text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x;
                text.fontSize = current;
                text.enableAutoSizing = auto;
                return width;
            }

            private static RectTransform PresentCreditsChip(QuickCustomScreenView view, RectTransform composition, bool show, int credits, float x, float width, bool fa)
            {
                RectTransform chip = composition.Find(CreditsChipName) as RectTransform;
                if (!show)
                {
                    if (chip != null)
                        chip.gameObject.SetActive(false);
                    return null;
                }

                if (chip == null)
                {
                    chip = new GameObject(CreditsChipName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                    chip.SetParent(composition, false);
                    Image plate = chip.GetComponent<Image>();
                    plate.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                    plate.raycastTarget = false;
                    Outline rim = chip.gameObject.AddComponent<Outline>();
                    rim.effectColor = new Color(0.93f, 0.78f, 0.38f, 0.55f);
                    rim.effectDistance = new Vector2(1f, -1f);

                    Image icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    icon.transform.SetParent(chip, false);
                    icon.sprite = Resources.Load<Sprite>("resource_credits");
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;

                    TextMeshProUGUI value = new GameObject("Value", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                    value.transform.SetParent(chip, false);
                    value.raycastTarget = false;
                }

                chip.gameObject.SetActive(true);
                PlaceTopLeft(chip, x, ChipTop, width, ChipHeight);
                Image coin = chip.Find("Icon")?.GetComponent<Image>();
                if (coin != null)
                {
                    coin.enabled = coin.sprite != null;
                    PlaceTopLeft(coin.rectTransform, 12f, (ChipHeight - 48f) * 0.5f, 48f, 48f);
                }

                TMP_Text label = chip.Find("Value")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    TMP_Text source = view.GetComponentInChildren<MainMenuAccountHeaderView>(true)?.Value;
                    if (source != null && source.font != null)
                        label.font = source.font;
                    float left = coin != null && coin.enabled ? 68f : 12f;
                    PlaceTopLeft(label.rectTransform, left, 8f, width - left - 12f, ChipHeight - 16f);
                    label.text = credits.ToString("N0", CultureInfo.InvariantCulture); // digits stay LTR in fa, as PresentWallet
                    label.isRightToLeftText = false;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Overflow;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 18f;
                    label.fontSizeMax = 30f;
                    label.fontStyle = FontStyles.Bold;
                    label.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                }

                return chip;
            }

            /// <summary>"none", or the names of the header pieces the CREDITS chip / title panel intersect.</summary>
            private static string Overlaps(RectTransform composition, RectTransform credits, RectTransform titlePanel, float chipX, float chipWidth)
            {
                string hits = string.Empty;
                Rect title = titlePanel != null ? TopLeftRect(titlePanel) : Rect.zero;
                Rect commander = chipWidth > 0f ? new Rect(chipX, ChipTop, chipWidth, 88f) : Rect.zero;
                Transform settingsTransform = composition.Find("SettingsButton");
                Rect settings = settingsTransform is RectTransform settingsRect && settingsTransform.gameObject.activeSelf ? TopLeftRect(settingsRect) : Rect.zero;
                if (credits != null)
                {
                    Rect chip = TopLeftRect(credits);
                    if (title.width > 0f && chip.Overlaps(title)) hits += "credits/title,";
                    if (commander.width > 0f && chip.Overlaps(commander)) hits += "credits/commander,";
                    if (settings.width > 0f && chip.Overlaps(settings)) hits += "credits/settings,";
                }

                if (title.width > 0f && commander.width > 0f && title.Overlaps(commander)) hits += "title/commander,";
                if (title.width > 0f && settings.width > 0f && title.Overlaps(settings)) hits += "title/settings,";
                return hits.Length == 0 ? "none" : hits.TrimEnd(',');
            }

            /// <summary>Rect in PlaceTopLeft space (x from the left, y down from the top) for a top-left anchored child.</summary>
            private static Rect TopLeftRect(RectTransform rect)
            {
                Vector2 size = rect.sizeDelta;
                Vector2 position = rect.anchoredPosition;
                return new Rect(position.x - rect.pivot.x * size.x, -position.y - (1f - rect.pivot.y) * size.y, size.x, size.y);
            }
        }
    }
}
