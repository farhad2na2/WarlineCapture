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
                // v50 (P2: chipWidth is 0 whenever HasCommanderIdentity() is false): the chip is sized from its own content.
                float creditsWidth = 0f;
                RectTransform creditsChip = null;
                if (read)
                    creditsChip = BuildCreditsChip(view, composition, credits, fa, out creditsWidth);
                bool room = read && titleWidth - creditsWidth - Gap >= MinTitleRoom;
                bool creditsShown = read && room;
                float panelWidth = creditsShown ? titleWidth - creditsWidth - Gap : titleWidth;
                if (titlePanel != null)
                    PlaceTopLeft(titlePanel, titleX, 10f, panelWidth, 78f);
                if (creditsShown)
                    PlaceTopLeft(creditsChip, titleX + panelWidth + Gap, ChipTop, creditsWidth, ChipHeight);
                else
                {
                    if (creditsChip == null)
                        creditsChip = composition.Find(CreditsChipName) as RectTransform;
                    if (creditsChip != null)
                        creditsChip.gameObject.SetActive(false);
                    creditsChip = null;
                }

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
                        // v50b: one logical " > " in both locales; the fa shaper mirrors it (v50 fa '<' was drawn as '>'),
                        // so fa reads right to left with the chevron pointing at the leaf.
                        crumb = root + " > " + leaf;
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

                view.ReassertMapCardStrips(composition); // v50b: after the shared text floor (runs before PlaceLibraryHeader)
                string overlap = Overlaps(composition, creditsChip, titlePanel, chipX, chipWidth);
                Debug.Log("[LibraryHeader] locale=" + (fa ? "fa-IR" : "en") + " ratio=" + view.TheatreRatioLabel() +
                    " crumb=" + crumb + " crumbFits=" + (crumbFits ? 1 : 0) +
                    " credits=" + (read ? credits.ToString(CultureInfo.InvariantCulture) : "none") +
                    " creditsShown=" + (creditsShown ? 1 : 0) + (read && !room ? " creditsRoom=0" : string.Empty) +
                    " creditsW=" + (creditsChip != null ? creditsChip.sizeDelta.x.ToString("0") : "0") +
                    " overlap=" + overlap);
            }

            /// <summary>
            /// v50: the crumb root from ui.skirmish.library.crumb.theatre ("THEATER" / "صحنه نبرد"); if a table lacks the
            /// key, the theatre-map title's part after "|" ("THEATRE MAP" / "نقشه صحنه").
            /// </summary>
            private static string TheatreRoot(bool fa)
            {
                string root = LibraryTitleCopy("ui.skirmish.library.crumb.theatre", string.Empty).Trim();
                if (!string.IsNullOrEmpty(root))
                    return root;
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

            private const float CoinSize = 44f;
            private const float ChipPad = 12f;
            private const float LabelSize = 22f;  // library floor (LibraryFontFloor)
            private const float ValueSize = 28f;

            /// <summary>
            /// v50: approved-2 CREDITS chip: coin, "CREDITS" (ui.common.credits) over the real balance, sized from that content
            /// (pad + coin + gap + widest line + pad). fa mirrors it (coin on the right, right-aligned text); digits stay Latin
            /// and LTR, as PresentWallet writes them. The label is omitted, never invented, when the table has no text.
            /// </summary>
            private static RectTransform BuildCreditsChip(QuickCustomScreenView view, RectTransform composition, int credits, bool fa, out float width)
            {
                RectTransform chip = composition.Find(CreditsChipName) as RectTransform;
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
                    view.CreateLabel(chip, "Label", LabelSize);
                    view.CreateLabel(chip, "Value", ValueSize);
                }

                chip.gameObject.SetActive(true);
                Image coin = chip.Find("Icon")?.GetComponent<Image>();
                bool hasCoin = coin != null && coin.sprite != null;
                if (coin != null)
                    coin.enabled = hasCoin;

                string caption = LibraryTitleCopy("ui.common.credits", string.Empty).Trim();
                TMP_Text label = chip.Find("Label")?.GetComponent<TMP_Text>();
                float labelH = fa ? 38f : 28f; // Noto line box: FitArabicLineBox allows box/1.65 >= 22
                float labelW = 0f;
                if (label != null)
                {
                    label.gameObject.SetActive(caption.Length > 0);
                    if (caption.Length > 0)
                    {
                        view.SetCopy(label, caption, LabelSize, LabelSize);
                        label.fontStyle = FontStyles.Bold;
                        label.isRightToLeftText = fa;
                        label.textWrappingMode = TextWrappingModes.NoWrap;
                        label.overflowMode = TextOverflowModes.Overflow;
                        label.color = new Color(0.78f, 0.8f, 0.76f, 1f);
                        labelW = WidthAtSize(label, LabelSize);
                    }
                }

                TMP_Text value = chip.Find("Value")?.GetComponent<TMP_Text>();
                float valueW = 0f;
                if (value != null)
                {
                    value.text = credits.ToString("N0", CultureInfo.InvariantCulture);
                    value.isRightToLeftText = false;
                    value.fontStyle = FontStyles.Bold;
                    value.textWrappingMode = TextWrappingModes.NoWrap;
                    value.overflowMode = TextOverflowModes.Overflow;
                    value.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                    valueW = WidthAtSize(value, ValueSize);
                    value.enableAutoSizing = false;
                    value.fontSize = ValueSize;
                }

                float textW = Mathf.Ceil(Mathf.Max(labelW, valueW)) + 2f;
                float coinW = hasCoin ? CoinSize + Gap : 0f;
                width = ChipPad + coinW + textW + ChipPad;
                float textX = fa ? ChipPad : ChipPad + coinW;
                if (coin != null)
                    PlaceTopLeft(coin.rectTransform, fa ? width - ChipPad - CoinSize : ChipPad, (ChipHeight - CoinSize) * 0.5f, CoinSize, CoinSize);
                bool labelShown = label != null && label.gameObject.activeSelf;
                float valueTop = labelShown ? 4f + labelH : 8f;
                if (labelShown)
                {
                    PlaceTopLeft(label.rectTransform, textX, 4f, textW, labelH);
                    label.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                }

                if (value != null)
                {
                    PlaceTopLeft(value.rectTransform, textX, valueTop, textW, ChipHeight - valueTop - 4f);
                    value.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
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
