using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private void PresentBattlefieldSummary(bool hasPin, SkirmishTheaterPinState pin, bool fa, bool battlefield)
        {
            if (_progressTrack == null)
                return;
            if (battlefield || !hasPin)
            {
                _progressTrack.gameObject.SetActive(false);
                return;
            }

            _progressTrack.gameObject.SetActive(true);
            TMP_Text caption = _progressTrack.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            SetCopy(caption, LibraryText("ui.skirmish.library.zone_progress", fa), 14f, 18f);
            bool pending = pin.PublishedCount <= 0;
            SetCopy(
                count,
                pending ? SkirmishBattleLibraryArt.ComingSoon(fa) : pin.WonCount + " / " + pin.PublishedCount,
                14f,
                20f);
            _sideWonRatio = pending ? 0f : Mathf.Clamp01(pin.WonCount / (float)pin.PublishedCount);
            PlaceSideBattlesCopy();
            EnsureThinProgressBar(_progressTrack, 16f);
            PaintSideProgress();
        }

        private void PlaceSideBattlesCopy()
        {
            if (_progressTrack == null || !_progressTrack.gameObject.activeInHierarchy)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float width = _progressTrack.rect.width;
            if (width < 120f)
                width = _progressTrack.sizeDelta.x > 120f ? _progressTrack.sizeDelta.x : 360f;
            float textWidth = Mathf.Max(96f, width - 16f);
            TMP_Text caption = _progressTrack.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            float labelSize = ColumnLabelSize();
            string battles = UpperLatin(LibraryText("ui.skirmish.library.zone_progress", fa));
            SetCopy(caption, battles, labelSize, labelSize);
            if (caption != null)
            {
                caption.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.isRightToLeftText = fa;
                caption.overflowMode = TextOverflowModes.Overflow;
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                caption.enableAutoSizing = false;
                caption.fontSize = labelSize;
                caption.fontSizeMin = labelSize;
                caption.fontSizeMax = labelSize;
                caption.maxVisibleLines = 1;
                V3LocalizedTextBindingView binding = caption.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(labelSize, labelSize);
                PlaceTopLeft(caption.rectTransform, 8f, 0f, textWidth, 36f);
            }

            if (count != null)
            {
                bool pending = SkirmishBattleLibraryArt.IsComingSoon(count.text);
                count.isRightToLeftText = pending && fa;
                count.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                count.overflowMode = TextOverflowModes.Overflow;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.maxVisibleLines = 1;
                PlaceTopLeft(count.rectTransform, 8f, 38f, textWidth, pending ? 32f : 22f);
            }
        }

        private static float ColumnLabelSize()
        {
            return LibraryFontFloor;
        }

        private static string UpperLatin(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char letter = value[i];
                builder.Append(letter <= '\u007f' ? char.ToUpperInvariant(letter) : letter);
            }

            return builder.ToString();
        }

        private void PaintSideProgress()
        {
            if (_progressTrack == null || !_progressTrack.gameObject.activeInHierarchy)
                return;
            V3GradientGraphic plate = _progressTrack.GetComponent<V3GradientGraphic>();
            if (plate != null)
                plate.enabled = false;
            RectTransform bar = _progressTrack.Find("Bar") as RectTransform;
            if (bar == null)
                bar = EnsureThinProgressBar(_progressTrack, 18f);
            if (bar == null || _progressFill == null)
                return;
            float trackWidth = _progressTrack.rect.width;
            if (trackWidth < 120f)
                trackWidth = 360f;
            float barWidth = Mathf.Min(270f, trackWidth - 16f);
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float barX = fa ? Mathf.Max(8f, trackWidth - 8f - barWidth) : 8f;
            PlaceTopLeft(bar, barX, 62f, barWidth, 16f);
            RectMask2D mask = bar.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
            Image barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.sprite = SolidSprite();
                barImage.type = Image.Type.Simple;
                barImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                barImage.enabled = true;
                barImage.raycastTarget = false;
            }

            _progressFill.transform.SetParent(bar, false);
            _progressFill.sprite = SolidSprite();
            _progressFill.type = Image.Type.Simple;
            _progressFill.color = new Color(0.93f, 0.74f, 0.22f, 1f);
            _progressFill.enabled = true;
            _progressFill.raycastTarget = false;
            Outline outline = bar.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            bool pending = count != null && SkirmishBattleLibraryArt.IsComingSoon(count.text);
            float ratio = pending ? 0f : Mathf.Clamp01(_sideWonRatio);
            if (!pending && WonRatio(count != null ? count.text : null, out float parsed))
                ratio = parsed;
            float inner = barWidth - 6f;
            if (pending || ratio <= 0.001f)
            {
                _progressFill.enabled = false;
                PlaceTopLeft(_progressFill.rectTransform, 2f, 2f, 0f, 12f);
            }
            else
            {
                _progressFill.enabled = true;
                float fillWidth = ratio >= 0.999f ? inner : Mathf.Max(12f, inner * ratio);
                PlaceTopLeft(_progressFill.rectTransform, 2f, 2f, fillWidth, 12f);
            }
            _progressFill.transform.SetAsLastSibling();
        }

        private static bool WonRatio(string value, out float ratio)
        {
            ratio = 0f;
            if (string.IsNullOrEmpty(value))
                return false;
            int slash = value.IndexOf('/');
            if (slash <= 0 || slash >= value.Length - 1)
                return false;
            if (!int.TryParse(value.Substring(0, slash).Trim(), out int won))
                return false;
            if (!int.TryParse(value.Substring(slash + 1).Trim(), out int published) || published <= 0)
                return false;
            ratio = Mathf.Clamp01(won / (float)published);
            return true;
        }

        private RectTransform EnsureThinProgressBar(RectTransform track, float height)
        {
            if (track == null)
                return null;
            for (int i = track.childCount - 1; i >= 0; i--)
            {
                Transform child = track.GetChild(i);
                if (child.name != "Bar")
                    continue;
                if (track.Find("Bar") != child)
                    DestroyUi(child.gameObject);
            }

            RectTransform bar = track.Find("Bar") as RectTransform;
            if (bar == null)
            {
                var created = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                created.transform.SetParent(track, false);
                created.color = new Color(0.08f, 0.1f, 0.12f, 1f);
                created.raycastTarget = false;
                bar = created.rectTransform;
            }

            Image trackImage = track.GetComponent<Image>();
            if (trackImage != null)
                trackImage.enabled = false;
            float width = track.rect.width > 40f ? track.rect.width : 520f;
            PlaceTopLeft(bar, 0f, 36f, width, height);
            if (bar.GetComponent<RectMask2D>() == null)
                bar.gameObject.AddComponent<RectMask2D>();
            Image barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                barImage.raycastTarget = false;
            }

            Outline rim = bar.GetComponent<Outline>() ?? bar.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.93f, 0.78f, 0.38f, 1f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);

            if (_progressFill == null || _progressFill.transform.parent != bar)
            {
                Transform existing = bar.Find("Fill");
                if (existing != null)
                    DestroyUi(existing.gameObject);
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                fill.transform.SetParent(bar, false);
                fill.color = new Color(0.91f, 0.72f, 0.22f, 1f);
                fill.raycastTarget = false;
                _progressFill = fill;
            }

            for (int i = track.childCount - 1; i >= 0; i--)
            {
                Transform child = track.GetChild(i);
                if (child.name == "Fill")
                    DestroyUi(child.gameObject);
            }

            return bar;
        }

        private void PresentMedalRow(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool fa, bool battlefield)
        {
            if (_medalRow == null)
                return;
            HorizontalLayoutGroup medalLayout = _medalRow.GetComponent<HorizontalLayoutGroup>();
            if (medalLayout != null)
                medalLayout.enabled = false;
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            SkirmishMedalTotals totals = hasPin ? pin.Medals : default;
            int[] counts = { totals.Recruit, totals.Regular, totals.Veteran, totals.Commander };
            SkirmishMedalMask mask = briefing.Medals;
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _medalRow.Find("Slot" + i);
                if (slot == null)
                    continue;
                bool earned = battlefield && MedalEarned(mask, difficulties[i]);
                if (!battlefield)
                    earned = counts[i] > 0;
                var image = slot.Find("Icon")?.GetComponent<Image>();
                if (image == null)
                    image = slot.GetComponentInChildren<Image>(true);
                if (image != null)
                    ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, difficulties[i]), earned ? Color.white : new Color(1f, 1f, 1f, 0.78f));
                TMP_Text count = slot.Find("Count")?.GetComponent<TMP_Text>();
                SetCopy(count, battlefield ? string.Empty : counts[i].ToString(), 14f, 18f);
                KeepSingleLine(count, 12f, 16f);
                if (count != null)
                    count.isRightToLeftText = false;
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                TMP_Text caption = slot.Find("Caption")?.GetComponent<TMP_Text>();
                SetCopy(caption, label, LibraryFontFloor, 26f);
                if (caption != null)
                {
                    caption.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    caption.isRightToLeftText = fa;
                    caption.textWrappingMode = TextWrappingModes.NoWrap;
                    caption.overflowMode = TextOverflowModes.Overflow;
                    caption.enableAutoSizing = true;
                    caption.fontSizeMin = LibraryFontFloor;
                    caption.fontSizeMax = 26f;
                }

                if (slot is RectTransform slotRect)
                    PlaceMedalSlot(slotRect, i, fa);
            }
        }

        private void PlaceMedalSlot(RectTransform slot, int index, bool fa)
        {
            if (slot == null)
                return;
            LayoutMedalEntry(slot, fa, 24f, 36f);
            BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), index);
        }

        private void LockMedalArtwork()
        {
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            HoldDescriptionClear();
            ArrangeMedalGrid(_medalRow, fa);
            LayoutTheatreMedals(fa);
            PaintSideProgress();
            PurgeStrayMedalImages();
        }

        private void ArrangeMedalGrid(RectTransform row, bool fa)
        {
            if (row == null)
                return;
            StopExpanding(row);
            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            if (grid != null)
                grid.enabled = false;
            float width = row.rect.width > 80f ? row.rect.width : MedalRowWidth();
            float cell = Mathf.Min(220f, Mathf.Max(148f, (width - 8f) * 0.5f));
            if (cell * 2f + 8f > width)
                cell = Mathf.Max(120f, (width - 8f) * 0.5f);
            for (int i = 0; i < 4; i++)
            {
                if (row.Find("Slot" + i) is not RectTransform slot)
                    continue;
                int column = i % 2;
                if (fa)
                    column = 1 - column;
                int line = i / 2;
                PlaceTopLeft(slot, column * (cell + 8f), 4f + line * 50f, cell, 46f);
                LayoutElement element = slot.GetComponent<LayoutElement>();
                if (element != null)
                    element.ignoreLayout = false;
                DropExtraMedalImages(slot);
                LayoutMedalEntry(slot, fa, 24f, 36f);
                BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), i);
            }
        }

        private void HoldDescriptionClear()
        {
            if (scenarioDescription == null)
                return;
            scenarioDescription.spriteAsset = null;
            string source = scenarioDescription.text ?? string.Empty;
            string stripped = StripSpriteTags(source);
            if (!string.Equals(stripped, source, StringComparison.Ordinal))
                scenarioDescription.text = stripped;
            scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
            scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
            if (scenarioDescription.GetComponent<RectMask2D>() == null)
                scenarioDescription.gameObject.AddComponent<RectMask2D>();
            Image[] images = scenarioDescription.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    DestroyUi(images[i].gameObject);
            }
        }

        private void PurgeStrayMedalImages()
        {
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            if (column == null)
                return;
            Image[] images = column.GetComponentsInChildren<Image>(true);
            var stray = new List<GameObject>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !SkirmishLibraryBlankImage.IsMedalArt(image))
                    continue;
                Transform parent = image.transform.parent;
                bool kept = image.name == "Icon" && parent != null && parent.name.StartsWith("Slot", StringComparison.Ordinal);
                if (!kept)
                    stray.Add(image.gameObject);
            }

            for (int i = 0; i < stray.Count; i++)
                DestroyUi(stray[i]);
        }

        private static void DropExtraMedalImages(RectTransform slot)
        {
            if (slot == null)
                return;
            var stray = new List<GameObject>();
            for (int i = 0; i < slot.childCount; i++)
            {
                Transform child = slot.GetChild(i);
                Image image = child.GetComponent<Image>();
                if (image == null || child.name == "Icon")
                    continue;
                if (SkirmishLibraryBlankImage.IsMedalArt(image))
                    stray.Add(child.gameObject);
            }

            for (int i = 0; i < stray.Count; i++)
                DestroyUi(stray[i]);
        }

        private float MedalRowWidth()
        {
            RectTransform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn") as RectTransform;
            RectTransform briefing = column != null ? column.parent as RectTransform : null;
            float panel = PointSpan(briefing);
            if (panel >= 200f)
                return Mathf.Max(160f, panel - 26f);

            if (_medalRow != null)
            {
                float rowWidth = PointSpan(_medalRow);
                if (rowWidth >= 160f)
                    return rowWidth;
            }

            return 548f;
        }

        private static float PointSpan(RectTransform rect)
        {
            if (rect == null)
                return 0f;
            bool point = Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) < 0.01f;
            if (point && rect.sizeDelta.x >= 160f)
                return rect.sizeDelta.x;
            if (rect.rect.width >= 160f)
                return rect.rect.width;
            if (rect.sizeDelta.x >= 160f)
                return rect.sizeDelta.x;
            return 0f;
        }

        private static void BindMedalIcon(Image image, int index)
        {
            if (image == null)
                return;
            Color tint = image.color.a > 0.2f ? image.color : Color.white;
            ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, MedalTier(index)), tint);
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            if (image.sprite == null)
            {
                image.enabled = false;
                image.gameObject.SetActive(false);
            }
        }

        private static SkirmishDifficultyId MedalTier(int index)
        {
            switch (index)
            {
                case 1: return SkirmishDifficultyId.Regular;
                case 2: return SkirmishDifficultyId.Veteran;
                case 3: return SkirmishDifficultyId.Commander;
                default: return SkirmishDifficultyId.Recruit;
            }
        }

        private static void StopExpanding(RectTransform rect)
        {
            if (rect == null)
                return;
            HorizontalLayoutGroup group = rect.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
                return;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            group.enabled = false;
        }

        private static void LayoutMedalEntry(RectTransform slot, bool fa, float iconSize, float countWidth)
        {
            if (slot == null)
                return;
            HorizontalLayoutGroup row = slot.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
                row.enabled = false;
            GridLayoutGroup grid = slot.GetComponent<GridLayoutGroup>();
            if (grid != null)
                grid.enabled = false;
            float slotWidth = slot.sizeDelta.x > 40f ? slot.sizeDelta.x : slot.rect.width;
            if (slotWidth < 40f)
                slotWidth = 148f;
            const float gap = 8f;
            float icon = iconSize;
            float countBox = Mathf.Max(28f, countWidth);
            float labelWidth = slotWidth - (4f + icon + gap + gap + countBox + 4f);
            if (labelWidth < 64f)
            {
                icon = Mathf.Min(icon, 18f);
                countBox = 28f;
                labelWidth = Mathf.Max(48f, slotWidth - (4f + icon + gap + gap + countBox + 4f));
            }

            RectTransform iconRect = slot.Find("Icon") as RectTransform;
            RectTransform caption = slot.Find("Caption") as RectTransform;
            RectTransform count = slot.Find("Count") as RectTransform;
            float iconX = fa ? slotWidth - 4f - icon : 4f;
            float countX = fa ? 4f : slotWidth - 4f - countBox;
            float labelX = fa ? 4f + countBox + gap : 4f + icon + gap;
            PlaceTopLeft(iconRect, iconX, 12f, icon, icon);
            PlaceTopLeft(caption, labelX, 8f, labelWidth, 30f);
            PlaceTopLeft(count, countX, 12f, countBox, 22f);
            HoldFixed(iconRect, icon, icon, 0f);
            HoldFixed(caption, labelWidth, 30f, 0f);
            HoldFixed(count, countBox, 22f, 0f);
            Image image = iconRect != null ? iconRect.GetComponent<Image>() : null;
            if (image != null)
            {
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                float fitted = Mathf.Max(10f, labelWidth / 5.4f);
                captionText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                captionText.isRightToLeftText = fa;
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.enableAutoSizing = false;
                captionText.fontSize = fitted;
                captionText.fontSizeMin = fitted;
                captionText.fontSizeMax = fitted;
                captionText.maxVisibleLines = 1;
                captionText.margin = Vector4.zero;
                if (caption.GetComponent<RectMask2D>() == null)
                    caption.gameObject.AddComponent<RectMask2D>();
                V3LocalizedTextBindingView binding = captionText.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(fitted, fitted);
            }

            TMP_Text countText = count != null ? count.GetComponent<TMP_Text>() : null;
            if (countText != null)
            {
                countText.isRightToLeftText = false;
                countText.alignment = TextAlignmentOptions.Center;
                countText.overflowMode = TextOverflowModes.Overflow;
                countText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private static void HoldFixed(RectTransform rect, float width, float height, float flexibleWidth)
        {
            if (rect == null)
                return;
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = false;
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = 0f;
        }

        private void EnsureMedalSlots(RectTransform row)
        {
            if (row == null)
                return;
            StopExpanding(row);
            if (row.childCount == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var slot = new GameObject("Slot" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                    slot.SetParent(row, false);
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    icon.transform.SetParent(slot, false);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    ShowIcon(icon, SkirmishBattleLibraryArt.Medal(true, MedalTier(i)), Color.white);
                    var caption = CreateBoundLabel(slot, "Caption", "ui.skirmish.library_medal_" + i, "MEDAL");
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                    caption.overflowMode = TextOverflowModes.Overflow;
                    caption.textWrappingMode = TextWrappingModes.Normal;
                    var count = CreateBoundLabel(slot, "Count", "ui.skirmish.library_medal_count", "0");
                    count.alignment = TextAlignmentOptions.Center;
                    count.isRightToLeftText = false;
                    count.overflowMode = TextOverflowModes.Overflow;
                }
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            ArrangeMedalGrid(row, fa);
        }

        private void EnsureRecommended(RectTransform panel)
        {
            if (panel == null)
                return;
            var group = panel.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(8, 8, 2, 2);
            group.spacing = 2f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            if (panel.Find("Heading") == null)
            {
                var heading = CreateBoundLabel(panel, "Heading", "ui.skirmish.library_recommended", "RECOMMENDED NEXT");
                heading.alignment = TextAlignmentOptions.MidlineLeft;
                var body = CreateBoundLabel(panel, "Body", "ui.skirmish.library_recommended_empty", "No recommendation yet.");
                body.alignment = TextAlignmentOptions.TopLeft;
            }

            FitLayout(panel.Find("Heading") as RectTransform, 0f, 22f, 1f);
            FitLayout(panel.Find("Body") as RectTransform, 0f, 36f, 1f);
            TMP_Text headingText = panel.Find("Heading")?.GetComponent<TMP_Text>();
            TMP_Text bodyText = panel.Find("Body")?.GetComponent<TMP_Text>();
            KeepSingleLine(headingText, 13f, 16f);
            if (bodyText != null)
            {
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                bodyText.overflowMode = TextOverflowModes.Truncate;
                bodyText.enableAutoSizing = true;
                bodyText.fontSizeMin = 13f;
                bodyText.fontSizeMax = 16f;
            }
        }

        private void PlaceBriefingLayout(RectTransform rules, bool battlefield)
        {
            if (rules == null)
                return;
            RectTransform column = FindOrCreate(rules, "BriefingColumn");
            float bottom = _advancedOpen ? 196f : 8f;
            Stretch(column, 24f, 24f, 6f, bottom);
            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>();
            if (group == null)
                group = column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.enabled = false;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            Transform title = rules.Find("Title") ?? column.Find("Title");
            if (title is RectTransform titleRect)
            {
                titleRect.SetParent(column, false);
                FitLayout(titleRect, 0f, 30f, 0f);
                TMP_Text titleText = titleRect.GetComponent<TMP_Text>();
                KeepSingleLine(titleText, 16f, 26f);
            }

            if (scenarioDescription != null)
            {
                scenarioDescription.transform.SetParent(column, false);
                var purposeBinding = scenarioDescription.GetComponent<V3LocalizedTextBindingView>()
                    ?? scenarioDescription.gameObject.AddComponent<V3LocalizedTextBindingView>();
                if (string.IsNullOrEmpty(purposeBinding.LocalizationKey) ||
                    !purposeBinding.LocalizationKey.StartsWith("ui.skirmish.", StringComparison.Ordinal))
                    purposeBinding.Configure("ui.skirmish.library_purpose", "Same map. Seed changes AI choices.");
                FitLayout(scenarioDescription.rectTransform, 0f, 96f, 1f);
                scenarioDescription.margin = new Vector4(4f, 2f, 4f, 2f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Truncate;
                scenarioDescription.alignment = TextAlignmentOptions.TopLeft;
            }

            if (_recommendedPanel != null)
            {
                _recommendedPanel.SetParent(column, false);
                FitLayout(_recommendedPanel, 0f, 272f, 0f);
            }

            if (_briefingPreview != null)
            {
                _briefingPreview.transform.SetParent(column, false);
                var previewLayout = FitLayout(_briefingPreview.rectTransform, 0f, battlefield ? 200f : 0f, 0f);
                previewLayout.minHeight = battlefield ? 180f : 0f;
                previewLayout.preferredHeight = battlefield ? 200f : 0f;
                previewLayout.flexibleHeight = 0f;
                previewLayout.ignoreLayout = !battlefield;
            }

            if (_statRow != null)
            {
                _statRow.SetParent(column, false);
                FitLayout(_statRow, 0f, 76f, 0f);
            }

            if (_progressTrack != null)
            {
                _progressTrack.SetParent(column, false);
                FitLayout(_progressTrack, 0f, 84f, 1f);
            }

            if (_medalRow != null)
            {
                _medalRow.SetParent(column, false);
                FitLayout(_medalRow, 0f, 104f, 1f);
            }

            if (_difficultyRow != null)
            {
                _difficultyRow.SetParent(column, false);
                FitLayout(_difficultyRow, 0f, 92f, 0f);
            }

            if (_medalGrid != null)
            {
                _medalGrid.SetParent(column, false);
                FitLayout(_medalGrid, 0f, 96f, 0f);
            }

            if (_libraryStoreButton != null)
            {
                RectTransform store = _libraryStoreButton.GetComponent<RectTransform>();
                store.SetParent(rules, false);
                var ignore = store.GetComponent<LayoutElement>() ?? store.gameObject.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
                float rulesWidth = rules.rect.width > 200f ? rules.rect.width : rules.sizeDelta.x;
                if (rulesWidth < 200f)
                    rulesWidth = 550f;
                PlaceTopLeft(store, rulesWidth - 226f, 4f, 210f, 30f);
            }

            if (_progressTrack != null && LibraryLevel == SkirmishLibraryLevel.Battlefield)
                _progressTrack.gameObject.SetActive(false);
            ComposeBriefingColumn(battlefield);
        }

        private void ComposeBriefingColumn(bool battlefield)
        {
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            RectTransform column = rules != null ? rules.Find("BriefingColumn") as RectTransform : null;
            if (column == null)
                return;
            if (_progressTrack != null && _progressTrack.parent != column)
                _progressTrack.SetParent(column, false);
            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>() ?? column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.enabled = true;
            group.padding = new RectOffset(8, 8, 4, 6);
            group.spacing = 6f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childScaleWidth = false;
            group.childScaleHeight = false;
            int index = 0;
            Transform summary = column.Find("TheatreSummary");
            index = StackColumnChild(summary as RectTransform, index, 76f, !battlefield && summary != null);
            bool previewShown = battlefield && _briefingPreview != null && _briefingPreview.gameObject.activeSelf;
            index = StackColumnChild(_briefingPreview != null ? _briefingPreview.rectTransform : null, index, 188f, previewShown);
            Transform title = rules.Find("BriefingColumn/Title") ?? rules.Find("Title");
            index = StackColumnChild(title as RectTransform, index, 40f, title != null);
            index = StackColumnChild(scenarioDescription != null ? scenarioDescription.rectTransform : null, index, battlefield ? 96f : 80f, scenarioDescription != null);
            index = StackColumnChild(_progressTrack, index, 84f, !battlefield && _progressTrack != null);
            index = StackColumnChild(_statRow, index, 84f, battlefield && _statRow != null);
            index = StackColumnChild(_medalRow, index, 104f, _medalRow != null);
            index = StackColumnChild(_difficultyRow, index, 116f, battlefield && _difficultyRow != null);
            StackColumnChild(_recommendedPanel, index, 272f, !battlefield && _recommendedPanel != null);
            if (_medalGrid != null)
            {
                _medalGrid.gameObject.SetActive(false);
                LayoutElement grid = _medalGrid.GetComponent<LayoutElement>() ?? _medalGrid.gameObject.AddComponent<LayoutElement>();
                grid.ignoreLayout = true;
                grid.preferredHeight = 0f;
                grid.minHeight = 0f;
            }

            if (scenarioDescription != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                float leading = fa ? 28f : 16f;
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
                scenarioDescription.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                scenarioDescription.isRightToLeftText = fa;
                scenarioDescription.margin = new Vector4(fa ? 12f : leading, 6f, fa ? leading : 12f, 6f);
                scenarioDescription.fontSizeMin = 15f;
                scenarioDescription.fontSizeMax = 24f;
                ReleaseTextClip(column);
                ReleaseTextClip(rules);
            }

            TMP_Text titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            if (titleText != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                titleText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                titleText.isRightToLeftText = fa;
                titleText.overflowMode = TextOverflowModes.Overflow;
            }

            FitBriefingPicture();
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        private static void ReleaseTextClip(Transform root)
        {
            if (root == null)
                return;
            RectMask2D mask = root.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static int StackColumnChild(RectTransform rect, int index, float height, bool show, bool expand = false)
        {
            if (rect == null)
                return index;
            rect.gameObject.SetActive(show);
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = !show;
            element.minWidth = 0f;
            element.preferredWidth = -1f;
            element.flexibleWidth = 1f;
            element.minHeight = show ? height : 0f;
            element.preferredHeight = show ? height : 0f;
            element.flexibleHeight = show && expand ? 1f : 0f;
            if (!show)
                return index;
            rect.SetSiblingIndex(index);
            return index + 1;
        }

        private void PresentTheatreSummary()
        {
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            Transform stray = preview != null ? preview.Find("TheatreSummary") : null;
            if (stray != null)
                DestroyUi(stray.gameObject);
            if (column == null)
                return;
            bool show = LibraryLevel != SkirmishLibraryLevel.Battlefield && _libraryViewModel != null;
            var plate = column.Find("TheatreSummary") as RectTransform;
            if (!show)
            {
                if (plate != null)
                    plate.gameObject.SetActive(false);
                return;
            }

            if (plate == null)
            {
                plate = new GameObject("TheatreSummary", typeof(RectTransform)).GetComponent<RectTransform>();
                plate.SetParent(column, false);
            }

            if (plate.Find("Heading") == null)
                CreateLabel(plate, "Heading", 22f);
            if (plate.Find("Summary") == null)
                CreateLabel(plate, "Summary", 36f);

            plate.gameObject.SetActive(true);
            int won = _libraryViewModel.Theater.OverallWonCount;
            int published = _libraryViewModel.Theater.OverallPublishedCount;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            const float ornamentInset = 32f;
            TMP_Text heading = plate.Find("Heading")?.GetComponent<TMP_Text>();
            if (heading != null)
            {
                PlaceSummaryLine(heading.rectTransform, 0f, 28f, fa, ornamentInset);
                string battles = UpperLatin(LibraryText("ui.skirmish.library.battles_won", fa));
                SetCopy(heading, battles, 18f, 22f);
                heading.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                heading.isRightToLeftText = fa;
                heading.textWrappingMode = TextWrappingModes.NoWrap;
                heading.overflowMode = TextOverflowModes.Overflow;
                heading.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                heading.enableAutoSizing = true;
                heading.fontSizeMin = 18f;
                heading.fontSizeMax = 22f;
                heading.maxVisibleLines = 1;
            }

            TMP_Text summary = plate.Find("Summary")?.GetComponent<TMP_Text>();
            if (summary == null)
                return;
            PlaceSummaryLine(summary.rectTransform, 30f, 40f, fa, ornamentInset);
            SetCopy(summary, won + " / " + published, 28f, 36f);
            summary.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            summary.isRightToLeftText = false;
            summary.textWrappingMode = TextWrappingModes.NoWrap;
            summary.overflowMode = TextOverflowModes.Overflow;
            summary.color = new Color(0.98f, 0.91f, 0.64f, 1f);
            summary.enableAutoSizing = true;
            summary.fontSizeMin = 28f;
            summary.fontSizeMax = 36f;
            summary.maxVisibleLines = 1;
        }

        private static void PlaceSummaryLine(RectTransform rect, float y, float height, bool fa, float inset)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(fa ? 8f : inset, -(y + height));
            rect.offsetMax = new Vector2(fa ? -inset : -8f, -y);
        }

        private void RebuildBriefingColumn()
        {
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            if (column is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private string FocusedRecommendation(SkirmishTheaterState theater, SkirmishBattlefieldState field)
        {
            string focused = theater.SelectedMapId;
            if (!string.IsNullOrEmpty(theater.RecommendedScenarioId) &&
                TryFindEntry(theater.RecommendedScenarioId, out SkirmishBattleCatalogEntry recommended) &&
                string.Equals(recommended.MapId, focused, StringComparison.OrdinalIgnoreCase))
                return theater.RecommendedScenarioId;

            if (field.GridCards != null)
            {
                for (int i = 0; i < field.GridCards.Length; i++)
                {
                    SkirmishBattleCardState card = field.GridCards[i];
                    if (IsEmptyCard(card) || card.Access != SkirmishCardAccess.Playable)
                        continue;
                    if ((card.Medals & SkirmishMedalMask.Regular) == 0)
                        return card.ScenarioId;
                }
            }

            return string.Empty;
        }

        private string RecommendationLabel(string scenarioId, bool fa)
        {
            if (!TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
                return scenarioId ?? string.Empty;
            SkirmishBattleLibraryArt.ObjectiveCopy(entry.ObjectiveId, fa, out _, out string objective);
            SkirmishBattleLibraryArt.ProfileCopy(entry.ArmyProfile, fa, out string army);
            SkirmishBattleLibraryArt.ProfileCopy(entry.StartProfile, fa, out string start);
            string detail = string.IsNullOrEmpty(start) ? army : army + " · " + start;
            return scenarioId + " · " + objective + "\n" + detail;
        }

        private bool IsBriefingColumnChild(Transform child, Transform medalKeep, bool battlefield, string descriptionName)
        {
            if (child == null)
                return false;
            if (child.name == "MedalRow")
                return child == medalKeep;
            if (child.name == "Title")
                return true;
            if (scenarioDescription != null && child == scenarioDescription.transform)
                return true;
            if (child.name == descriptionName && scenarioDescription != null && child != scenarioDescription.transform)
                return false;
            if (battlefield)
            {
                if (child.name == "BriefingPreview")
                    return _briefingPreview == null || child == _briefingPreview.transform;
                if (child.name == "BriefingStats")
                    return _statRow == null || child == _statRow;
                if (child.name == "MedalDifficultySelector")
                    return _difficultyRow == null || child == _difficultyRow;
                return false;
            }

            if (child.name == "TheatreSummary")
                return true;
            if (child.name == "BattlesWonTrack")
                return _progressTrack == null || child == _progressTrack;
            if (child.name == "RecommendedNext")
                return _recommendedPanel == null || child == _recommendedPanel;
            return false;
        }

        private Transform ChooseMedalRow(Transform column)
        {
            Transform best = null;
            int bestScore = -1;
            for (int i = 0; i < column.childCount; i++)
            {
                Transform child = column.GetChild(i);
                if (child.name != "MedalRow")
                    continue;
                int score = 0;
                if (child.Find("Slot0") != null)
                    score += 2;
                if (child.Find("Slot0/Caption") != null)
                    score += 2;
                if (child == _medalRow)
                    score += 1;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }

            return best;
        }

        private void PresentRecommendedPreview(string scenarioId)
        {
            if (_recommendedPanel == null)
                return;
            VerticalLayoutGroup recommendedGroup = _recommendedPanel.GetComponent<VerticalLayoutGroup>();
            if (recommendedGroup != null)
                recommendedGroup.enabled = false;
            Image[] images = _recommendedPanel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].name != "PreviewFrame" && SkirmishLibraryBlankImage.IsBlank(images[i]))
                    images[i].enabled = false;
            }

            RawImage preview = _recommendedPanel.Find("Preview")?.GetComponent<RawImage>();
            string key = string.IsNullOrEmpty(scenarioId) ? null : "skirmish.preview." + scenarioId;
            Texture2D texture = string.IsNullOrEmpty(key) ? null : SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null && !string.IsNullOrEmpty(key))
                texture = SkirmishLibraryPreviewAssets.Placeholder();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float panelWidth = _recommendedPanel.rect.width > 80f ? _recommendedPanel.rect.width : _recommendedPanel.sizeDelta.x;
            if (panelWidth < 80f)
                panelWidth = 360f;
            float textWidth = Mathf.Max(140f, panelWidth - 16f);
            const float headingHeight = 32f;
            const float captionHeight = 64f;
            const float previewCap = 160f;
            const float frameHeight = 168f;
            float previewY = headingHeight + captionHeight + 6f;
            bool showPreview = texture != null;
            Image frame = _recommendedPanel.Find("PreviewFrame")?.GetComponent<Image>();
            if (frame == null)
            {
                frame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                frame.transform.SetParent(_recommendedPanel, false);
                frame.raycastTarget = false;
            }

            frame.sprite = SolidSprite();
            frame.type = Image.Type.Simple;
            frame.color = new Color(0.93f, 0.72f, 0.28f, 1f);
            frame.enabled = showPreview;
            frame.gameObject.SetActive(showPreview);
            if (showPreview)
                PlaceTopLeft(frame.rectTransform, 8f, previewY, textWidth, frameHeight);
            if (!showPreview)
            {
                if (preview != null)
                    preview.gameObject.SetActive(false);
            }
            else
            {
                if (preview == null)
                {
                    preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                    preview.transform.SetParent(_recommendedPanel, false);
                }

                preview.gameObject.SetActive(true);
                preview.texture = texture;
                preview.color = Color.white;
                preview.raycastTarget = false;
                preview.enabled = true;
                PlaceTopLeft(preview.rectTransform, 12f, previewY + 4f, Mathf.Max(80f, textWidth - 8f), previewCap);
                CoverCrop(preview, Mathf.Max(80f, textWidth - 8f), previewCap);
                preview.transform.SetSiblingIndex(frame.transform.GetSiblingIndex() + 1);
            }

            if (_recommendedPanel.Find("Heading") is RectTransform heading)
            {
                PlaceTopLeft(heading, 8f, 0f, textWidth, headingHeight);
                heading.SetAsLastSibling();
                TMP_Text headingText = heading.GetComponent<TMP_Text>();
                if (headingText != null)
                {
                    headingText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    headingText.isRightToLeftText = fa;
                    headingText.textWrappingMode = TextWrappingModes.NoWrap;
                    headingText.overflowMode = TextOverflowModes.Overflow;
                    headingText.enableAutoSizing = true;
                    headingText.fontSizeMin = 20f;
                    headingText.fontSizeMax = 26f;
                    headingText.maxVisibleLines = 1;
                }
            }

            if (_recommendedPanel.Find("Body") is RectTransform body)
            {
                PlaceTopLeft(body, 8f, headingHeight, textWidth, captionHeight);
                body.SetAsLastSibling();
                TMP_Text bodyText = body.GetComponent<TMP_Text>();
                if (bodyText != null)
                {
                    bodyText.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                    bodyText.isRightToLeftText = fa;
                    bodyText.textWrappingMode = TextWrappingModes.Normal;
                    bodyText.overflowMode = TextOverflowModes.Overflow;
                    bodyText.enableAutoSizing = true;
                    bodyText.fontSizeMin = 18f;
                    bodyText.fontSizeMax = 24f;
                    bodyText.maxVisibleLines = 3;
                    if (!string.IsNullOrWhiteSpace(bodyText.text))
                        bodyText.text = UpperLatin(bodyText.text);
                }
            }
        }

        private void FitRecommendedPreview()
        {
            if (_recommendedPanel == null || !_recommendedPanel.gameObject.activeInHierarchy)
                return;
            if (string.IsNullOrEmpty(_recommendedScenarioId) && _libraryViewModel != null)
                _recommendedScenarioId = FocusedRecommendation(_libraryViewModel.Theater, _libraryViewModel.Battlefield);
            PresentRecommendedPreview(_recommendedScenarioId);
        }
    }
}
