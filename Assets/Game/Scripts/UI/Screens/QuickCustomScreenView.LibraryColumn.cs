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
            SetCopy(caption, LibraryText("ui.skirmish.library.zone_progress", fa), 18f, 22f);
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
                    ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, difficulties[i]), earned ? Color.white : new Color(1f, 1f, 1f, battlefield ? 0.32f : 0.78f));
                TMP_Text count = slot.Find("Count")?.GetComponent<TMP_Text>();
                SetCopy(count, battlefield ? string.Empty : counts[i].ToString(), 18f, 22f);
                if (battlefield)
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

        /// <summary>
        /// approved-1 left column polish (colour/weight only; structure and counts untouched): framed medal tiles and a
        /// gold bold RECOMMENDED NEXT heading. Runs from PlaceLibraryHeader, after the late text passes.
        /// </summary>
        private void ReassertTheatreColumn()
        {
            if (LibraryLevel == SkirmishLibraryLevel.Battlefield)
                return;
            Transform composition = transform.Find("SkirmishSetupComposition");
            string[] tiers = { "Recruit", "Regular", "Veteran", "Commander" };
            for (int i = 0; i < tiers.Length; i++)
            {
                Transform tile = FindDeep(composition, "TheatreMedal_" + tiers[i]);
                Image frame = tile != null ? tile.GetComponent<Image>() : null;
                if (frame == null)
                    continue;
                frame.color = new Color(0.1f, 0.13f, 0.14f, 0.96f);
                Outline edge = tile.GetComponent<Outline>() ?? tile.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0.93f, 0.78f, 0.38f, 0.45f);
                edge.effectDistance = new Vector2(1.5f, -1.5f);
            }

            if (_recommendedPanel != null && _recommendedPanel.gameObject.activeInHierarchy)
            {
                TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
                if (heading != null)
                {
                    heading.fontStyle = FontStyles.Bold;
                    heading.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                    FitDetailLine(heading, 16f, 22f);
                }

                TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
                if (body != null)
                    body.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            }
        }

        private void PlaceMedalSlot(RectTransform slot, int index, bool fa)
        {
            if (slot == null)
                return;
            if (LibraryLevel != SkirmishLibraryLevel.Battlefield)
                LayoutMedalTile(slot, fa);
            else
                LayoutMedalEntry(slot, fa, 24f, 36f);
            BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), index);
        }

        private const float MedalTileHeight = 100f;
        private const float TheatreMedalRowHeight = MedalTileHeight * 2f + 14f;

        /// <summary>approved-1 medal tile: a big medal, the tier name and the count stacked and centred.</summary>
        private static void LayoutMedalTile(RectTransform slot, bool fa)
        {
            if (slot == null)
                return;
            HorizontalLayoutGroup row = slot.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
                row.enabled = false;
            float width = slot.sizeDelta.x > 40f ? slot.sizeDelta.x : Mathf.Max(120f, slot.rect.width);
            const float icon = 52f;
            RectTransform iconRect = slot.Find("Icon") as RectTransform;
            RectTransform caption = slot.Find("Caption") as RectTransform;
            RectTransform count = slot.Find("Count") as RectTransform;
            PlaceTopLeft(iconRect, (width - icon) * 0.5f, 4f, icon, icon);
            PlaceTopLeft(caption, 4f, 54f, width - 8f, 22f);
            PlaceTopLeft(count, 4f, 72f, width - 8f, 28f);
            HoldFixed(iconRect, icon, icon, 0f);
            HoldFixed(caption, width - 8f, 24f, 0f);
            HoldFixed(count, width - 8f, 28f, 0f);
            Image image = iconRect != null ? iconRect.GetComponent<Image>() : null;
            if (image != null)
            {
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                captionText.alignment = TextAlignmentOptions.Center;
                captionText.isRightToLeftText = fa;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.enableAutoSizing = true;
                captionText.fontSizeMin = 13f;
                captionText.fontSizeMax = 20f;
                captionText.maxVisibleLines = 1;
                captionText.margin = Vector4.zero;
                captionText.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                V3LocalizedTextBindingView binding = captionText.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(13f, 20f);
            }

            TMP_Text countText = count != null ? count.GetComponent<TMP_Text>() : null;
            if (countText != null)
            {
                countText.isRightToLeftText = false;
                countText.alignment = TextAlignmentOptions.Center;
                countText.textWrappingMode = TextWrappingModes.NoWrap;
                countText.overflowMode = TextOverflowModes.Overflow;
                countText.enableAutoSizing = true;
                countText.fontSizeMin = 24f;
                countText.fontSizeMax = 30f;
                countText.fontStyle = FontStyles.Bold;
                countText.color = Color.white;
                V3LocalizedTextBindingView binding = countText.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(24f, 30f);
            }
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
                bool tiles = LibraryLevel != SkirmishLibraryLevel.Battlefield;
                if (tiles)
                {
                    // approved-1: 2 x 2 big medal tiles.
                    float tile = Mathf.Max(100f, (width - 10f) * 0.5f);
                    PlaceTopLeft(slot, column * (tile + 10f), 2f + line * (MedalTileHeight + 8f), tile, MedalTileHeight);
                }
                else
                {
                    // approved-2: MEDALS heading over four tier circles with names, one row.
                    float quarter = width / 4f;
                    int visual = fa ? 3 - i : i;
                    PlaceTopLeft(slot, visual * quarter, MedalCircleTop, quarter, MedalCircleSlot);
                }
                LayoutElement element = slot.GetComponent<LayoutElement>();
                if (element != null)
                    element.ignoreLayout = false;
                DropExtraMedalImages(slot);
                if (tiles)
                    LayoutMedalTile(slot, fa);
                else
                    LayoutMedalCircle(slot, fa, width / 4f);
                BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), i);
            }

            if (LibraryLevel == SkirmishLibraryLevel.Battlefield)
            {
                PlaceMedalsHeading(row, fa, width);
                FitMedalCaptions(row, fa, width);
            }
        }

        /// <summary>
        /// approved-2/3 medal captions: one shared size (smallest fit to quarter - 8 px gap), autosize off, held each
        /// frame so the later text passes cannot re-autosize them. FA gets a taller box (FitArabicLineBox caps at h/1.65).
        /// </summary>
        private void FitMedalCaptions(RectTransform row, bool fa, float width)
        {
            float quarter = width / 4f;
            float boxWidth = Mathf.Max(40f, quarter - 8f);
            float boxHeight = MedalCircleSlot - MedalCircleIcon(fa);
            float max = 18f; // v33: FA caption shares EN box (78 - 58 = 20 px)
            Transform titleTransform = FindDeep(transform.Find("SkirmishSetupComposition/BaseAssaultRules"), "Title");
            TMP_FontAsset latin = titleTransform != null ? titleTransform.GetComponent<TMP_Text>()?.font : null;
            var captions = new List<TMP_Text>(4);
            float shared = max;
            for (int i = 0; i < 4; i++)
            {
                TMP_Text caption = row.Find("Slot" + i)?.Find("Caption")?.GetComponent<TMP_Text>();
                if (caption == null)
                    continue;
                PlaceTopLeft(caption.rectTransform, 4f, MedalCircleIcon(fa), boxWidth, boxHeight);
                HoldFixed(caption.rectTransform, boxWidth, boxHeight, 0f);
                if (!fa && latin != null && !ContainsArabicScript(caption.text))
                    caption.font = latin;
                caption.fontStyle = FontStyles.Bold;
                caption.alignment = TextAlignmentOptions.Center;
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                caption.enableAutoSizing = false;
                caption.fontSize = max;
                float natural = caption.GetPreferredValues(caption.text, 9999f, 100f).x;
                if (natural > boxWidth - 2f && natural > 1f)
                    shared = Mathf.Min(shared, Mathf.Floor(max * (boxWidth - 2f) / natural));
                captions.Add(caption);
            }

            shared = Mathf.Max(11f, shared);
            for (int i = 0; i < captions.Count; i++)
            {
                LibraryFixedTextSize hold = captions[i].GetComponent<LibraryFixedTextSize>() ?? captions[i].gameObject.AddComponent<LibraryFixedTextSize>();
                hold.Size = shared;
                hold.Apply();
                captions[i].ForceMeshUpdate();
            }
        }

        // v32: the medal sprites carry ~14% transparent padding per side, so a 40 px rect drew ~34 px circles.
        // Same 104 px row (Library.cs approved height): heading 26, slot 78 = icon + caption.
        private const float MedalCircleTop = 26f;
        private const float MedalCircleSlot = 78f;
        private static float MedalCircleIcon(bool fa) => 58f; // v33: FA equals EN; FA caption fits a 20 px box at <=18 px

        private void PlaceMedalsHeading(RectTransform row, bool fa, float width)
        {
            Transform existing = row.Find("MedalsHeading");
            TMP_Text heading = existing != null ? existing.GetComponent<TMP_Text>() : CreateLabel(row, "MedalsHeading", 22f);
            LayoutElement element = heading.GetComponent<LayoutElement>() ?? heading.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            PlaceTopLeft(heading.rectTransform, 8f, 0f, width - 16f, MedalCircleTop - 2f);
            SetCopy(heading, fa ? "مدال ها" : "MEDALS", 20f, 24f);
            heading.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            heading.isRightToLeftText = fa;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.overflowMode = TextOverflowModes.Overflow;
            heading.fontStyle = FontStyles.Bold;
            heading.color = new Color(0.96f, 0.93f, 0.84f, 1f);
        }

        /// <summary>approved-2 medal circle: the tier medal centred over its name; count hidden on the battlefield.</summary>
        private static void LayoutMedalCircle(RectTransform slot, bool fa, float width)
        {
            if (slot == null)
                return;
            HorizontalLayoutGroup row = slot.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
                row.enabled = false;
            float icon = MedalCircleIcon(fa); // approved-2/3: medal art dominates the slot
            RectTransform iconRect = slot.Find("Icon") as RectTransform;
            RectTransform caption = slot.Find("Caption") as RectTransform;
            RectTransform count = slot.Find("Count") as RectTransform;
            PlaceTopLeft(iconRect, (width - icon) * 0.5f, 0f, icon, icon);
            PlaceTopLeft(caption, 2f, icon, width - 4f, MedalCircleSlot - icon);
            HoldFixed(iconRect, icon, icon, 0f);
            HoldFixed(caption, width - 4f, MedalCircleSlot - icon, 0f);
            if (count != null)
                count.gameObject.SetActive(false); // approved-2: no duplicate count badges on the selected-battle panel
            Image image = iconRect != null ? iconRect.GetComponent<Image>() : null;
            if (image != null)
            {
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                captionText.alignment = TextAlignmentOptions.Center;
                captionText.isRightToLeftText = fa;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.enableAutoSizing = true;
                captionText.fontSizeMin = 14f;
                captionText.fontSizeMax = 19f;
                captionText.maxVisibleLines = 1;
                captionText.margin = Vector4.zero;
                captionText.color = new Color(0.86f, 0.86f, 0.82f, 1f);
                V3LocalizedTextBindingView binding = captionText.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(14f, 19f);
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
            const float gap = 10f;
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
                float fitted = Mathf.Clamp(labelWidth / 5.4f, 10f, 18f);
                // Long captions (COMMANDER, FA labels) must end before the count: shrink to the measured width.
                if (!string.IsNullOrEmpty(captionText.text))
                {
                    captionText.fontSize = fitted;
                    float natural = captionText.GetPreferredValues(captionText.text, 9999f, 30f).x;
                    if (natural > labelWidth - 2f && natural > 1f)
                        fitted = Mathf.Max(10f, Mathf.Floor(fitted * (labelWidth - 2f) / natural));
                }
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
                FitLayout(_recommendedPanel, 0f, RecommendedCompactHeight, 0f);
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
                FitLayout(_medalRow, 0f, LibraryLevel == SkirmishLibraryLevel.Battlefield ? 104f : TheatreMedalRowHeight, 1f);
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
            index = StackColumnChild(summary as RectTransform, index, TheatreSummaryHeight, !battlefield && summary != null, true); // v43b: the plate absorbs the spare column height
            bool previewShown = battlefield && _briefingPreview != null && _briefingPreview.gameObject.activeSelf;
            index = StackColumnChild(_briefingPreview != null ? _briefingPreview.rectTransform : null, index, 188f, previewShown);
            // The heading label is authored under RulesScroll/Content, which is hidden unless Advanced is open, so it was
            // filled in but never drawn. Adopt it into the briefing column so it stacks under the preview as in approved-2/3.
            Transform title = rules.Find("BriefingColumn/Title") ?? rules.Find("Title") ?? rules.Find("RulesScroll/Content/Title");
            // The Theatre column keeps its pass-3 stack (TheatreSummary carries the map name), so the label goes back there.
            Transform rulesContent = rules.Find("RulesScroll/Content");
            if (title != null && battlefield && title.parent != column)
                title.SetParent(column, false);
            else if (title != null && !battlefield && rulesContent != null && title.parent == column)
                title.SetParent(rulesContent, false);
            bool titleInColumn = title != null && title.parent == column;
            index = StackColumnChild(titleInColumn ? title as RectTransform : null, index, 40f, titleInColumn);
            index = StackColumnChild(scenarioDescription != null ? scenarioDescription.rectTransform : null, index, battlefield ? 96f : 80f, scenarioDescription != null);
            index = StackColumnChild(_progressTrack, index, 84f, false);
            index = StackColumnChild(_statRow, index, 84f, battlefield && _statRow != null);
            index = StackColumnChild(_medalRow, index, 104f, battlefield && _medalRow != null);
            index = StackColumnChild(_difficultyRow, index, 116f, battlefield && _difficultyRow != null);
            StackColumnChild(_recommendedPanel, index, RecommendedCompactHeight, !battlefield && _recommendedPanel != null);
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
                scenarioDescription.fontSizeMin = 20f;
                scenarioDescription.fontSizeMax = 26f;
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
                PlaceSummaryLine(heading.rectTransform, 0f, 32f, fa, ornamentInset);
                string battles = UpperLatin(LibraryText("ui.skirmish.library.battles_won", fa));
                SetCopy(heading, battles, 22f, 30f);
                heading.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                heading.isRightToLeftText = fa;
                heading.textWrappingMode = TextWrappingModes.NoWrap;
                heading.overflowMode = TextOverflowModes.Overflow;
                heading.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                heading.enableAutoSizing = true;
                heading.fontSizeMin = 18f;
                heading.fontSizeMax = 30f; // Game PM: larger BATTLES WON heading (was forced to 22)
                heading.maxVisibleLines = 1;
                // v45: in fa-IR, V3LocalizedTextBindingView.FitArabicLineBox capped the 32 px box at 32 / 1.65 = 19.4 px
                // (autosize then settled lower), so the Farsi heading drew far smaller than en's ~25 px. Like the
                // Recommended-next heading fix, fa is sized from en: the en pass's settled size (else what the 32 px box
                // gives Oxanium, 32 / 1.25) x the 1.15 Farsi allowance, pinned by LibraryFixedTextSize. The rect stays put.
                // v45b: en cache is keyed per heading box (a 60x32 pre-layout box gave en 18 but fa used the 496 px box's
                // 30). Game PM: fa may exceed en's 30 cap up to en x 1.15, limited by the box height (Noto ink is 0.946 em
                // for this copy, so h / 0.95) and shrunk to the box width - fixed size, no rect move, no overflow.
                Rect headingBox = heading.rectTransform.rect;
                int headingKey = Mathf.RoundToInt(headingBox.width) * 1000 + Mathf.RoundToInt(headingBox.height);
                string headingSource;
                float headingSize;
                float enHeading = -1f;
                if (fa)
                {
                    bool cached = EnMedalHeadingSizeByBox.TryGetValue(headingKey, out enHeading);
                    if (!cached)
                        enHeading = Mathf.Min(30f, headingBox.height / 1.25f); // what the box gives Oxanium
                    headingSource = cached ? "en-cache" : "en-box";
                    float wanted = enHeading * FaTierNameAllowance;
                    float heightCap = headingBox.height > 1f ? headingBox.height / FaHeadingInkRatio : wanted;
                    headingSize = Mathf.Min(wanted, heightCap);
                    if (headingBox.width > 1f)
                    {
                        heading.enableAutoSizing = false;
                        heading.fontSize = headingSize;
                        float natural = heading.GetPreferredValues(heading.text, 9999f, 100f).x;
                        if (natural > headingBox.width - 2f && natural > 1f)
                        {
                            headingSize = Mathf.Max(12f, Mathf.Floor(headingSize * (headingBox.width - 2f) / natural));
                            headingSource += "+width";
                        }
                    }
                    PinFixedSize(heading, headingSize);
                }
                else
                {
                    heading.ForceMeshUpdate();
                    headingSize = heading.fontSize;
                    if (headingSize > 1f)
                        EnMedalHeadingSizeByBox[headingKey] = headingSize;
                    enHeading = headingSize;
                    headingSource = "autosize";
                }

                Debug.Log("[MedalHeading] font=" + headingSize.ToString("0.0") + " source=" + headingSource +
                    " en=" + (enHeading > 0f ? enHeading.ToString("0.0") : "-") +
                    " box=" + heading.rectTransform.rect.width.ToString("0") + "x" + heading.rectTransform.rect.height.ToString("0") +
                    " locale=" + (fa ? "fa-IR" : "en"));
            }

            TMP_Text summary = plate.Find("Summary")?.GetComponent<TMP_Text>();
            if (summary == null)
                return;
            PlaceSummaryLine(summary.rectTransform, 31f, 48f, fa, ornamentInset);
            SetCopy(summary, won + " / " + published, 46f, 46f); // approved-1: large bold count
            summary.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            summary.isRightToLeftText = false;
            summary.textWrappingMode = TextWrappingModes.NoWrap;
            summary.overflowMode = TextOverflowModes.Overflow;
            summary.color = new Color(0.98f, 0.96f, 0.9f, 1f);
            summary.fontStyle = FontStyles.Bold;
            summary.maxVisibleLines = 1;
            LibraryFixedTextSize heldCount = summary.GetComponent<LibraryFixedTextSize>();
            if (heldCount == null)
                heldCount = summary.gameObject.AddComponent<LibraryFixedTextSize>();
            heldCount.Size = 46f;
            heldCount.Apply();
            PresentTheatreProgressBar(plate, won, published, fa, ornamentInset);
            PresentTheatreMedalTiles(plate, _libraryViewModel.Theater.OverallMedalTotals, fa);
        }

        /// <summary>Height of the Theatre left-column progress block: heading, X / N, bar and 2 x 2 medal tiles.</summary>
        internal const float TheatreSummaryHeight = 350f; // v33: 106 + 2 x 118 + 8

        private void PresentTheatreProgressBar(RectTransform plate, int won, int published, bool fa, float inset)
        {
            var track = plate.Find("ProgressTrack") as RectTransform;
            if (track != null && (track.GetComponent<Image>() == null || track.Find("Fill")?.GetComponent<Image>() == null))
            {
                DestroyUi(track.gameObject);
                track = null;
            }

            if (track == null)
            {
                track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                track.SetParent(plate, false);
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                fill.SetParent(track, false);
            }

            PlaceSummaryLine(track, 80f, 12f, fa, inset);
            Image back = track.GetComponent<Image>();
            back.color = new Color(0.12f, 0.13f, 0.14f, 1f);
            back.raycastTarget = false;
            var fillRect = track.Find("Fill") as RectTransform;
            float ratio = published > 0 ? Mathf.Clamp01(won / (float)published) : 0f;
            // Fills from the leading edge: left in LTR, right in RTL.
            fillRect.anchorMin = new Vector2(fa ? 1f - ratio : 0f, 0f);
            fillRect.anchorMax = new Vector2(fa ? 1f : ratio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fillRect.GetComponent<Image>();
            fillImage.color = new Color(0.93f, 0.7f, 0.2f, 1f);
            fillImage.raycastTarget = false;
        }

        /// <summary>v43b: the plate's laid-out height = column height minus padding, spacing and the other stacked rows.</summary>
        private static float TheatreSummarySpareHeight(RectTransform plate)
        {
            var column = plate.parent as RectTransform;
            VerticalLayoutGroup group = column != null ? column.GetComponent<VerticalLayoutGroup>() : null;
            if (column == null || group == null || column.rect.height <= 1f)
                return Mathf.Max(TheatreSummaryHeight, plate.rect.height);
            float used = group.padding.top + group.padding.bottom;
            int shown = 0;
            for (int i = 0; i < column.childCount; i++)
            {
                var child = column.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                    continue;
                shown++;
                if (child != plate)
                    used += element != null ? Mathf.Max(element.preferredHeight, element.minHeight) : child.rect.height;
            }
            used += group.spacing * Mathf.Max(0, shown - 1);
            return Mathf.Max(TheatreSummaryHeight, column.rect.height - used);
        }

        /// <summary>approved-1 2 x 2 medal tiles (icon, tier name, count) with whole-theatre ledger totals.</summary>
        private void PresentTheatreMedalTiles(RectTransform plate, SkirmishMedalTotals totals, bool fa)
        {
            SkirmishDifficultyId[] tiers = { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander };
            int[] counts = { totals.Recruit, totals.Regular, totals.Veteran, totals.Commander };
            string[] en = { "RECRUIT", "REGULAR", "VETERAN", "COMMANDER" };
            string[] faNames = { "تازه‌کار", "عادی", "کهنه‌کار", "فرمانده" };
            const float top = 106f;
            const float baseTileHeight = 118f; // v33 approved-1: big medal over name over count
            const float gap = 8f;
            const float bottomPad = 4f;
            // v43 (Game PM, approved-1): the Theatre summary plate takes the column's spare height, and the four tiles
            // grow into it evenly - icon, name, count and their offsets all scale by the same factor (capped so the
            // icon stays inside half the tile width).
            float plateHeight = TheatreSummarySpareHeight(plate); // v43b: from the column, not the pre-layout plate rect
            // v44: the plate is laid out by the column (childControlWidth), so its width is the column's inner width.
            // Reading plate.rect first gave the 100 px default of a freshly created RectTransform -> 38 px tiles.
            float plateWidth = TheatreSummaryWidth(plate, out string widthSource);
            int tileColumns = (plateWidth - 16f - gap) * 0.5f >= TheatreMedalTileMinWidth ? 2 : 1;
            int tileRows = 4 / tileColumns;
            float wanted = (plateHeight - top - gap * (tileRows - 1) - bottomPad) / tileRows;
            float scale = Mathf.Max(1f, wanted / baseTileHeight);
            float tileWidth = (plateWidth - 16f - gap * (tileColumns - 1)) / tileColumns;
            scale = Mathf.Min(scale, Mathf.Max(1f, tileWidth * 0.55f / 54f), 2f);
            // Tiles always fill the spare height; if the width cap stops the content scale, the content is centred in
            // the taller tile instead of leaving a gap under the grid.
            float tileHeight = Mathf.Max(baseTileHeight * scale, wanted);
            float contentTop = (tileHeight - baseTileHeight * scale) * 0.5f;
            // v45: when the width cap stops the scale and the tile is much taller than its content (4:3: 172 px of
            // content in a 251 px tile), spread the spare height between icon, name and count instead of centring the
            // block. Small spares (16:9: 15 px) stay centred, so 16:9 is unchanged.
            float spare = tileHeight - baseTileHeight * scale;
            bool spread = spare > TheatreMedalSpreadThreshold;
            float iconTop = spread ? spare * 0.25f : contentTop;
            float nameTop = spread ? spare * 0.5f : contentTop;
            float countTop = spread ? spare * 0.75f : contentTop;
            float leftover = Mathf.Max(0f, plateHeight - (top + tileRows * tileHeight + gap * (tileRows - 1) + bottomPad));
            var tierNames = new List<TMP_Text>(4);
            // v45b: name line width from the computed tile (PlaceTileLine insets 4 + 4, name offsets 8 + 8), not the rect.
            float nameLineWidth = tileWidth - 24f;
            var tierCounts = new List<TMP_Text>(4);
            float enEquivalent = float.MaxValue; // v43c: the size the English names would get in these tiles
            for (int i = 0; i < tiers.Length; i++)
            {
                int column = i % tileColumns;
                int row = i / tileColumns;
                int visual = tileColumns == 1 ? 0 : fa ? 1 - column : column;
                string name = "TheatreMedal_" + tiers[i];
                var tile = plate.Find(name) as RectTransform;
                // Rebuilt each pass: the library's blank-image sweep can strip Image components between passes.
                if (tile != null)
                    DestroyUi(tile.gameObject);
                tile = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                tile.SetParent(plate, false);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                icon.SetParent(tile, false);
                CreateLabel(tile, "TierName", 22f);
                CreateLabel(tile, "TierCount", 30f);

                float span = 1f / tileColumns;
                tile.anchorMin = new Vector2(visual * span, 1f);
                tile.anchorMax = new Vector2((visual + 1) * span, 1f);
                tile.pivot = new Vector2(0.5f, 1f);
                float yTop = top + row * (tileHeight + gap);
                bool lead = visual == 0, trail = visual == tileColumns - 1;
                tile.offsetMin = new Vector2(lead ? 8f : gap * 0.5f, -(yTop + tileHeight));
                tile.offsetMax = new Vector2(trail ? -8f : -gap * 0.5f, -yTop);
                Image frame = tile.GetComponent<Image>();
                if (frame != null)
                {
                    frame.color = new Color(0.06f, 0.08f, 0.09f, 0.9f);
                    frame.raycastTarget = false;
                }

                var iconRect = tile.Find("Icon") as RectTransform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(54f * scale, 54f * scale); // sprite has ~14% padding: ~46 px drawn, clears TierName at y58
                iconRect.anchoredPosition = new Vector2(0f, -(2f * scale + iconTop));
                Image iconImage = iconRect.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    ShowIcon(iconImage, SkirmishBattleLibraryArt.Medal(true, tiers[i]), Color.white);
                }

                TMP_Text tierName = tile.Find("TierName")?.GetComponent<TMP_Text>();
                if (tierName != null)
                {
                    PlaceTileLine(tierName.rectTransform, nameTop + 58f * scale, 28f * scale);
                    SetCopy(tierName, fa ? faNames[i] : en[i], (fa ? 30f : 26f) * scale, (fa ? 32f : 28f) * scale); // FA glyphs run small: higher floor
                    tierName.alignment = TextAlignmentOptions.Center;
                    tierName.isRightToLeftText = fa;
                    tierName.textWrappingMode = TextWrappingModes.NoWrap;
                    tierName.overflowMode = TextOverflowModes.Overflow;
                    tierName.color = new Color(0.9f, 0.9f, 0.86f, 1f);
                    // Per-tile fit: keep the bigger size, but shrink a long name (4:3 VETERAN/COMMANDER) to its tile with a gap.
                    tierName.rectTransform.offsetMin = new Vector2(8f, tierName.rectTransform.offsetMin.y);
                    tierName.rectTransform.offsetMax = new Vector2(-8f, tierName.rectTransform.offsetMax.y);
                    // v43c: fa-IR no longer pins its own 30 x scale. It measures the English name of the same tier in this
                    // tile (same 16..28 x scale fit as en) and is sized from that after the loop.
                    if (!fa)
                        enEquivalent = Mathf.Min(enEquivalent, TierNameFit(tierName, en[i], 16f, 28f * scale, nameLineWidth));
                    else if (!EnTierNameSizeByTile.ContainsKey(TierTileKey(tileWidth, scale)))
                        enEquivalent = Mathf.Min(enEquivalent, TierNameFitLatin(tierName, en[i], 16f, 28f * scale, nameLineWidth));
                    // v45b: en is fitted to the same logical width it is measured against. On the first pass the rect
                    // was still the pre-layout 100 px default, so FitDetailLine was skipped (width <= 40) and en kept its
                    // SetCopy 22 while the measure said 55.3.
                    if (!fa)
                        FitTierLine(tierName, 16f, 28f * scale, nameLineWidth);
                    tierNames.Add(tierName);
                }

                TMP_Text tierCount = tile.Find("TierCount")?.GetComponent<TMP_Text>();
                if (tierCount != null)
                {
                    PlaceTileLine(tierCount.rectTransform, countTop + 84f * scale, 34f * scale);
                    SetCopy(tierCount, fa ? SkirmishWidgetContext.ToPersianDigits(counts[i].ToString()) : counts[i].ToString(), (fa ? 36f : 32f) * scale, (fa ? 40f : 38f) * scale);
                    tierCount.alignment = TextAlignmentOptions.Center;
                    tierCount.isRightToLeftText = false;
                    tierCount.fontStyle = FontStyles.Bold;
                    tierCount.color = new Color(0.98f, 0.95f, 0.86f, 1f);
                    tierCounts.Add(tierCount);
                }
            }

            // v43c: fa-IR = the en tile-scaled size x the Farsi glyph allowance, then shrunk only if a Farsi name is
            // wider than its tile. Noto Sans Arabic's alef/keheh stand ~64/90 em against Oxanium's 62/90 cap, but most
            // name letters sit ~45/90 em, so 1.15 keeps the ascenders <= ~1.19x the Latin cap (not oversized) while the
            // letter bodies reach ~0.83x of it.
            string enSource = fa ? "measure" : "en";
            if (fa && EnTierNameSizeByTile.TryGetValue(TierTileKey(tileWidth, scale), out float cachedEn))
            {
                enEquivalent = cachedEn;
                enSource = "en-cache";
            }
            else if (fa && TryNearestEnTierSize(tileWidth, scale, out float nearEn))
            {
                // v44c: the first pass (column height not settled, e.g. 536x584 at scale 1.97) has no exact key; the
                // Latin measure in the Farsi label never shrank (fa 63.6 vs en 36), so use the same-width en fit.
                enEquivalent = Mathf.Min(enEquivalent, nearEn);
                enSource = "en-near";
            }
            float faMax = enEquivalent < float.MaxValue ? Mathf.Max(16f, enEquivalent * FaTierNameAllowance) : 28f * scale;
            if (fa)
            {
                for (int i = 0; i < tierNames.Count; i++)
                {
                    if (nameLineWidth > 40f)
                        FitTierLine(tierNames[i], 16f, faMax, nameLineWidth);
                    else
                    {
                        tierNames[i].enableAutoSizing = false;
                        tierNames[i].fontSize = faMax;
                    }
                }
            }

            // v44: counts follow the same rule as the names. en pins 32 x scale (its autosize floor, what the 34 x scale
            // line gives it); fa-IR = that x the Farsi allowance, capped at the old 38 x scale pin, never above it.
            float enCount = 32f * scale;
            float countFont = fa ? Mathf.Min(enCount * FaTierNameAllowance, 38f * scale) : enCount;
            for (int i = 0; i < tierCounts.Count; i++)
            {
                TMP_Text c = tierCounts[i];
                c.enableAutoSizing = false;
                c.fontSize = countFont;
                c.fontSizeMin = countFont;
                c.fontSizeMax = countFont;
                if (fa)
                    PinFixedSize(c, countFont);
                V3LocalizedTextBindingView countBinding = c.GetComponent<V3LocalizedTextBindingView>();
                if (countBinding != null)
                    countBinding.ConfigureFontBounds(countFont, countFont);
                c.ForceMeshUpdate();
            }

            // One shared size: the smallest per-tile fit, applied to all four, autosize off (per aspect/locale).
            float shared = float.MaxValue;
            for (int i = 0; i < tierNames.Count; i++)
                shared = Mathf.Min(shared, tierNames[i].fontSize);
            for (int i = 0; i < tierNames.Count && shared < float.MaxValue; i++)
            {
                TMP_Text t = tierNames[i];
                t.enableAutoSizing = false;
                t.fontSize = shared;
                t.fontSizeMin = shared;
                t.fontSizeMax = shared;
                if (fa)
                    PinFixedSize(t, shared);
                V3LocalizedTextBindingView binding = t.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(shared, shared);
                t.ForceMeshUpdate();
            }

            if (!fa && shared < float.MaxValue && tierNames.Count > 0 && nameLineWidth > 40f)
                EnTierNameSizeByTile[TierTileKey(tileWidth, scale)] = shared;
            Debug.Log("[MedalGrid] icon=" + (54f * scale).ToString("0.0") +
                " scale=" + scale.ToString("0.00") +
                " tile=" + tileWidth.ToString("0") + "x" + tileHeight.ToString("0") +
                " nameFont=" + (shared < float.MaxValue ? shared.ToString("0.0") : "-") +
                " enEquivalent=" + (enEquivalent < float.MaxValue ? enEquivalent.ToString("0.0") : "-") +
                " enSource=" + enSource +
                " countFont=" + countFont.ToString("0.0") + " enCount=" + enCount.ToString("0.0") +
                " tiles=" + tileColumns + "x" + tileRows + " width=" + widthSource +
                " spare=" + spare.ToString("0") + " content=" + (spread ? "spread" : "centred") +
                (fa ? " faAllowance=" + FaTierNameAllowance.ToString("0.00") + " faMax=" + faMax.ToString("0.0") +
                    " nameLineH=" + (tierNames.Count > 0 ? tierNames[0].rectTransform.rect.height.ToString("0.0") : "-") : "") +
                " plate=" + plateWidth.ToString("0") + "x" + plateHeight.ToString("0") +
                " leftover=" + leftover.ToString("0") +
                " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>v45b: the en Battles-won heading's settled autosize per box (width x 1000 + height).</summary>
        private static readonly Dictionary<int, float> EnMedalHeadingSizeByBox = new Dictionary<int, float>();

        /// <summary>v45b: ink height / point size of the Farsi heading copy in Noto Sans Arabic (64.3 + 20.9) / 90.</summary>
        private const float FaHeadingInkRatio = 0.95f;

        /// <summary>v45: tile spare height (px) above which icon/name/count spread out instead of centring.</summary>
        private const float TheatreMedalSpreadThreshold = 24f;

        /// <summary>v44: narrowest medal tile; below this the 2 x 2 grid reflows to one column of four.</summary>
        private const float TheatreMedalTileMinWidth = 96f;

        /// <summary>v44: plate width from the laid-out column (inner width), else the plate rect if it is past the
        /// 100 px RectTransform default, else 300. Reports which source was used for [MedalGrid].</summary>
        private static float TheatreSummaryWidth(RectTransform plate, out string source)
        {
            var column = plate.parent as RectTransform;
            VerticalLayoutGroup group = column != null ? column.GetComponent<VerticalLayoutGroup>() : null;
            if (column != null && group != null && column.rect.width > 1f)
            {
                source = "column";
                return column.rect.width - group.padding.left - group.padding.right;
            }

            if (plate.rect.width > 1f && !Mathf.Approximately(plate.rect.width, 100f))
            {
                source = "plate";
                return plate.rect.width;
            }

            source = plate.rect.width > 1f ? "prelayout-default" : "fallback";
            return 300f;
        }

        /// <summary>v43c: fa-IR medal tier names are this much larger than the en tile-scaled size (glyph-body allowance).</summary>
        private const float FaTierNameAllowance = 1.15f;

        /// <summary>v43c: the single-line size <paramref name="copy"/> would get in <paramref name="text"/>'s rect, using
        /// the same rule as FitDetailLine (max, shrunk to the width minus 2 px, floored at min), without changing the text.</summary>
        /// <summary>v44b: TierNameFit with a Latin-capable font when the label's font has no glyphs for the copy.</summary>
        private static float TierNameFitLatin(TMP_Text text, string copy, float min, float max, float width)
        {
            TMP_FontAsset own = text.font;
            TMP_FontAsset latin = TMP_Settings.defaultFontAsset;
            bool swap = own != null && latin != null && own != latin && !own.HasCharacters(copy);
            if (swap)
                text.font = latin;
            float size = TierNameFit(text, copy, min, max, width);
            if (swap)
                text.font = own;
            return size;
        }

        /// <summary>v45b: FitDetailLine's rule on an explicit width (the rect may not be laid out yet).</summary>
        private static void FitTierLine(TMP_Text text, float min, float max, float width)
        {
            if (text == null)
                return;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.enableAutoSizing = false;
            float size = TierNameFit(text, text.text, min, max, width);
            text.fontSize = size;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            V3LocalizedTextBindingView binding = text.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            text.ForceMeshUpdate();
        }

        private static float TierNameFit(TMP_Text text, string copy, float min, float max, float width)
        {
            if (width <= 40f || string.IsNullOrEmpty(copy))
                return max;
            float held = text.fontSize;
            text.fontSize = max;
            float natural = text.GetPreferredValues(copy, 9999f, 100f).x;
            text.fontSize = held;
            if (natural > width - 2f && natural > 1f)
                return Mathf.Max(min, Mathf.Floor(max * (width - 2f) / natural));
            return max;
        }

        /// <summary>v44b: V3LocalizedTextBindingView.FitArabicLineBox caps Arabic-script size at rect height / 1.65 on a
        /// later localization pass. Growing the line box (v43c) pushed TierName under the medal icon, so the size is held
        /// by LibraryFixedTextSize (re-applied each frame and by LibraryPass) and the rect stays where it was.</summary>
        private static void PinFixedSize(TMP_Text text, float size)
        {
            LibraryFixedTextSize held = text.GetComponent<LibraryFixedTextSize>() ?? text.gameObject.AddComponent<LibraryFixedTextSize>();
            held.Size = size;
            held.Apply();
        }

        /// <summary>v44b: the en tier-name size last fitted per tile geometry, so fa-IR derives from the real en result.
        /// Measuring the English names in the Farsi label used the Farsi font (no Latin glyphs) and never shrank them.</summary>
        private static readonly Dictionary<int, float> EnTierNameSizeByTile = new Dictionary<int, float>();

        /// <summary>v44c: en fit for the same tile width at another scale, rescaled (the fit is linear in scale).</summary>
        private static bool TryNearestEnTierSize(float tileWidth, float scale, out float size)
        {
            size = 0f;
            int width = Mathf.RoundToInt(tileWidth);
            float best = float.MaxValue;
            foreach (KeyValuePair<int, float> pair in EnTierNameSizeByTile)
            {
                if (pair.Key / 1000 != width)
                    continue;
                float cachedScale = (pair.Key % 1000) / 100f;
                float distance = Mathf.Abs(cachedScale - scale);
                if (cachedScale > 0f && distance < best)
                {
                    best = distance;
                    size = Mathf.Max(16f, pair.Value * scale / cachedScale);
                }
            }

            return best < float.MaxValue;
        }

        private static int TierTileKey(float tileWidth, float scale) =>
            Mathf.RoundToInt(tileWidth) * 1000 + Mathf.RoundToInt(scale * 100f);

        private static void PlaceTileLine(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(4f, -(y + height));
            rect.offsetMax = new Vector2(-4f, -y);
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
            // v44 (Game PM): the localised battlefield name and objective, not the raw id/enum codes.
            SkirmishBattleLibraryArt.ObjectiveCopy(entry.ObjectiveId, fa, out string objective, out _);
            string name = string.Empty;
            if (_libraryViewModel != null && TryFindTheaterPin(_libraryViewModel.Theater, entry.MapId, out SkirmishTheaterPinState pin) &&
                !string.IsNullOrEmpty(pin.TitleLocKey))
                name = LibraryText(pin.TitleLocKey, fa);
            if (string.IsNullOrEmpty(name))
                name = LibraryTitle(scenarioId, fa);
            return string.IsNullOrEmpty(objective) ? name : name + " – " + objective;
        }

        private bool IsBriefingColumnChild(Transform child, Transform medalKeep, bool battlefield, string descriptionName)
        {
            if (child == null)
                return false;
            if (child.name == "MedalRow")
                return battlefield && child == medalKeep; // Theatre medal counts live in P2's bottom strip.
            if (child.name == "Title")
                return true;
            if (child.name == "LibraryModeToggle")
                return battlefield; // approved-2: MAP/GRID toggle heads the right panel
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
                if (child.name == "BriefingSubtitle")
                    return _briefingSubtitleShown;
                if (child.name == "MedalDifficultySelector")
                    return _difficultyRow == null || child == _difficultyRow;
                return false;
            }

            if (child.name == "TheatreSummary")
                return true; // Pass 12: the Theatre left column is the only progress display (approved-1).
            if (child.name == "BattlesWonTrack")
                return false; // approved-1 left column: battles won, medal tiles, recommended next only.
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
            // v41b (Game PM v90 review): compact row - small thumbnail beside the id/objective copy (2 lines, >=14px)
            // instead of the 160px image block under the caption.
            // v42: the row grows into the height the column gives it (RecommendedNext takes the leftover space), so the
            // thumbnail scales up to half the panel width; the heading box is 38px so the Farsi line-box fitter
            // (height / 1.65) no longer caps the heading below the English size.
            const float headingHeight = RecommendedHeadingHeight;
            const float rowGap = 10f;
            float panelHeight = _recommendedPanel.rect.height > 1f ? _recommendedPanel.rect.height : RecommendedCompactHeight;
            // v43 (Game PM): fixed compact row again (~96x72 thumbnail + 2-line copy) pinned under the medal tiles; the
            // spare column height goes to the medal tiles (PresentTheatreMedalTiles), not to this row.
            const float rowHeight = 76f;
            float thumbW = Mathf.Min((rowHeight - 4f) * 4f / 3f, textWidth * 0.5f);
            float thumbH = thumbW * 0.75f;
            float frameW = thumbW + 4f;
            float frameHeight = thumbH + 4f;
            float previewY = headingHeight + 2f;
            float frameX = fa ? 8f + textWidth - frameW : 8f;
            float captionX = fa ? 8f : 8f + frameW + rowGap;
            float captionW = Mathf.Max(80f, textWidth - frameW - rowGap);
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
                PlaceTopLeft(frame.rectTransform, frameX, previewY, frameW, frameHeight);
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
                PlaceTopLeft(preview.rectTransform, frameX + 2f, previewY + 2f, thumbW, thumbH);
                CoverCrop(preview, thumbW, thumbH);
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
                    headingText.fontSizeMin = 16f;
                    headingText.fontSizeMax = 22f;
                    headingText.maxVisibleLines = 1;
                }
            }

            if (_recommendedPanel.Find("Body") is RectTransform body)
            {
                // v45: fa-IR's taller Arabic line box put the second wrapped line on the column's bottom ornament
                // (4:3); keep the body clear of it.
                float bodyHeight = fa ? Mathf.Max(40f, frameHeight - RecommendedFaBottomInset) : frameHeight;
                if (showPreview)
                    PlaceTopLeft(body, captionX, previewY, captionW, bodyHeight);
                else
                    PlaceTopLeft(body, 8f, previewY, textWidth, bodyHeight);
                body.SetAsLastSibling();
                TMP_Text bodyText = body.GetComponent<TMP_Text>();
                if (bodyText != null)
                {
                    bodyText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    bodyText.isRightToLeftText = fa;
                    bodyText.textWrappingMode = TextWrappingModes.Normal;
                    bodyText.overflowMode = TextOverflowModes.Ellipsis;
                    bodyText.enableAutoSizing = true;
                    bodyText.fontSizeMin = 14f;
                    bodyText.fontSizeMax = 18f;
                    bodyText.maxVisibleLines = 2;
                    V3LocalizedTextBindingView bodyBinding = bodyText.GetComponent<V3LocalizedTextBindingView>();
                    if (bodyBinding != null)
                        bodyBinding.ConfigureFontBounds(14f, 18f);
                    if (!string.IsNullOrWhiteSpace(bodyText.text))
                        bodyText.text = UpperLatin(bodyText.text);
                }
            }

            FitRecommendedText(fa);
            TMP_Text loggedBody = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
            TMP_Text loggedHeading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
            Debug.Log("[RecommendedNext] title=" + (loggedBody != null ? loggedBody.text.Replace("\n", " / ") : "-") +
                " locale=" + (fa ? "fa-IR" : "en"));
            Debug.Log("[RecommendedNext] layout=compact scenario=" + (scenarioId ?? string.Empty) +
                " thumb=" + (showPreview ? thumbW.ToString("0") + "x" + thumbH.ToString("0") : "none") +
                " bodyFont=" + (loggedBody != null ? loggedBody.fontSize.ToString("0.0") : "-") +
                " bodyMin=" + (loggedBody != null ? loggedBody.fontSizeMin.ToString("0.0") : "-") +
                " headingFont=" + (loggedHeading != null ? loggedHeading.fontSize.ToString("0.0") : "-") +
                " panelW=" + panelWidth.ToString("0") + " panelH=" + panelHeight.ToString("0") + " locale=" + (fa ? "fa-IR" : "en"));
        }

        private const float RecommendedHeadingHeight = 38f;
        /// <summary>v45: fa-IR Recommended-next body bottom inset (clears the column's bottom ornament).</summary>
        private const float RecommendedFaBottomInset = 12f;
        private const float RecommendedCompactHeight = 124f; // v42: heading 38 + 76px minimum thumbnail row + padding (v41b 112, was 272)
        private const float RecommendedHeadingMax = 22f;
        private const float RecommendedHeadingMin = 16f;
        private const float RecommendedBodyMax = 24f;
        private const float RecommendedBodyMin = 14f;

        /// <summary>
        /// v42: final sizes for the Recommended next copy, run last (FinishLibraryText calls it instead of the old
        /// 18-24 / 3-line relax). Heading: one line, the largest size up to 22px that fits, same rule in en and fa-IR.
        /// Body: the largest size from 24 down to 14px whose wrapped text fits the box in at most two lines.
        /// Both are pinned (auto-size off, binding bounds = size) so no later pass can grow them past the fit.
        /// </summary>
        private void FitRecommendedText(bool fa)
        {
            if (_recommendedPanel == null)
                return;
            TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
            if (heading != null)
            {
                heading.textWrappingMode = TextWrappingModes.NoWrap;
                float width = heading.rectTransform.sizeDelta.x > 1f ? heading.rectTransform.sizeDelta.x : heading.rectTransform.rect.width;
                float size = RecommendedHeadingMax;
                for (; size > RecommendedHeadingMin; size -= 1f)
                {
                    heading.fontSize = size;
                    if (width <= 1f || heading.GetPreferredValues(heading.text, 9999f, 100f).x <= width - 2f)
                        break;
                }

                PinRecommendedSize(heading, size, false, 1);
                heading.overflowMode = TextOverflowModes.Overflow;
            }

            TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
            if (body != null)
            {
                body.textWrappingMode = TextWrappingModes.Normal;
                body.enableAutoSizing = false;
                Vector2 box = body.rectTransform.sizeDelta;
                float width = box.x > 1f ? box.x : body.rectTransform.rect.width;
                float height = box.y > 1f ? box.y : body.rectTransform.rect.height;
                float size = RecommendedBodyMax;
                for (; size > RecommendedBodyMin; size -= 1f)
                {
                    body.fontSize = size;
                    if (width <= 1f || height <= 1f)
                        break;
                    TMP_TextInfo info = body.GetTextInfo(body.text);
                    float needed = body.GetPreferredValues(body.text, width - body.margin.x - body.margin.z, 0f).y;
                    if (info != null && info.lineCount <= 2 && needed <= height)
                        break;
                }

                PinRecommendedSize(body, size, true, 2);
                body.overflowMode = TextOverflowModes.Ellipsis; // at 14px a longer copy is clipped with an ellipsis, never drawn outside
            }
        }

        private static void PinRecommendedSize(TMP_Text text, float size, bool wrap, int lines)
        {
            text.enableAutoSizing = false;
            text.fontSize = text.fontSizeMin = text.fontSizeMax = size;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.maxVisibleLines = lines;
            V3LocalizedTextBindingView binding = text.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
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
