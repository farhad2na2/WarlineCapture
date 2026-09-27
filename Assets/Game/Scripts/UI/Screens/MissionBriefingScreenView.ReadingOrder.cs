using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private struct AuthoredText
        {
            public bool Captured;
            public Vector2 Position;
            public Vector2 Size;
            public TextAlignmentOptions Alignment;
            public float FontSize;
            public bool AutoSize;
            public float FontSizeMin;
            public float FontSizeMax;
            public float LineSpacing;
            public TextOverflowModes Overflow;
            public TextWrappingModes Wrap;
            public bool Active;
        }

        private AuthoredText _screenTitleBox;
        private AuthoredText _missionNumberBox;
        private AuthoredText _missionTitleBox;
        private AuthoredText _chapterBox;
        private AuthoredText _briefingBox;
        private AuthoredText _summaryBox;
        private AuthoredText[] _conditionNameBoxes;
        private AuthoredText[] _conditionValueBoxes;
        private AuthoredText _enemyIntelRowBox;
        private AuthoredText _enemyIntelLabelBox;
        private AuthoredText _vehicleRowBox;
        private AuthoredText _airRowBox;
        private TMP_Text _briefingLabel;
        private bool _readingOrderQueued;

        private void Awake() => CaptureAuthoredLayout();

        private void OnEnable() => UiShellRuntimeGateway.Localization.LocaleChanged += QueueReadingOrder;

        private void OnDisable()
        {
            UiShellRuntimeGateway.Localization.LocaleChanged -= QueueReadingOrder;
            Canvas.preWillRenderCanvases -= ApplyQueuedReadingOrder;
            _readingOrderQueued = false;
        }

        private void QueueReadingOrder()
        {
            if (_readingOrderQueued)
                return;
            _readingOrderQueued = true;
            Canvas.preWillRenderCanvases += ApplyQueuedReadingOrder;
        }

        private void ApplyQueuedReadingOrder()
        {
            Canvas.preWillRenderCanvases -= ApplyQueuedReadingOrder;
            _readingOrderQueued = false;
            ApplyReadingOrder();
        }

        private void CaptureAuthoredLayout()
        {
            Capture(ref _screenTitleBox, screenTitle);
            Capture(ref _missionNumberBox, missionNumber);
            Capture(ref _missionTitleBox, missionTitle);
            Capture(ref _chapterBox, screenSubtitle);
            _briefingLabel = missionOverview != null ? missionOverview.Find("BriefingLabel")?.GetComponent<TMP_Text>() : null;
            Capture(ref _briefingBox, _briefingLabel);
            Capture(ref _summaryBox, missionSummary);
            CaptureMany(ref _conditionNameBoxes, conditionNameLabels);
            CaptureMany(ref _conditionValueBoxes, conditionLabels);
            CaptureRow(ref _enemyIntelRowBox, ref _enemyIntelLabelBox, enemyIntelLabel);
            CaptureNamedRow(ref _vehicleRowBox, "Row_LIGHT_VEHICLES");
            CaptureNamedRow(ref _airRowBox, "Row_AIR_THREAT");
        }

        private void ApplyReadingOrder()
        {
            if (!_screenTitleBox.Captured)
                CaptureAuthoredLayout();

            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (rtl)
                PlaceIntroColumn();
            else
                RestoreIntroColumn();

            FitConditionRows(rtl);
            FitEnemyIntel(rtl);
            ContainObjectiveRows(rtl);
        }

        private void PlaceIntroColumn()
        {
            float panelWidth = PanelWidth(missionOverview, 911f);
            float columnWidth = 470f;
            float x = Mathf.Max(390f, panelWidth - 28f - columnWidth);
            float columnRight = panelWidth - 28f - x;
            Place(_screenTitleBox, screenTitle, 70f, 2f, BandWidth(screenTitle, 905f) - 88f, 60f, 28f, false, TextAlignmentOptions.MidlineRight);
            Place(_missionNumberBox, missionNumber, 25f, 82f, 340f, 66f, 48f, false, TextAlignmentOptions.MidlineLeft);
            Place(_missionTitleBox, missionTitle, x, 82f, columnRight, 80f, 36f, false, TextAlignmentOptions.MidlineRight);
            Place(_chapterBox, screenSubtitle, x, 174f, columnRight, 52f, 24f, false, TextAlignmentOptions.MidlineRight);
            Place(_briefingBox, _briefingLabel, x, 236f, columnRight, 44f, 20f, false, TextAlignmentOptions.MidlineRight);
            // Noto Arabic's line box is about 2.1em. A fixed size in a tall rect keeps wrapped lines apart.
            Place(_summaryBox, missionSummary, x, 290f, columnRight, 250f, 20f, true, TextAlignmentOptions.TopRight);
        }

        private void RestoreIntroColumn()
        {
            Restore(_screenTitleBox, screenTitle);
            Restore(_missionNumberBox, missionNumber);
            Restore(_missionTitleBox, missionTitle);
            Restore(_chapterBox, screenSubtitle);
            Restore(_briefingBox, _briefingLabel);
            Restore(_summaryBox, missionSummary);
        }

        private void FitConditionRows(bool rtl)
        {
            if (conditionLabels == null || tacticalConditions == null)
                return;

            float panelWidth = PanelWidth(tacticalConditions, 333f);
            for (int i = 0; i < conditionLabels.Length; i++)
            {
                TMP_Text value = conditionLabels[i];
                TMP_Text name = conditionNameLabels != null && i < conditionNameLabels.Length ? conditionNameLabels[i] : null;
                if (value == null || _conditionValueBoxes == null || i >= _conditionValueBoxes.Length || !_conditionValueBoxes[i].Captured)
                    continue;

                float needed = value.GetPreferredValues(value.text, 4096f, 0f).x;
                bool sentence = needed > _conditionValueBoxes[i].Size.x + 8f;
                if (!sentence)
                {
                    Restore(_conditionValueBoxes[i], value);
                    if (name != null && _conditionNameBoxes != null && i < _conditionNameBoxes.Length)
                    {
                        Restore(_conditionNameBoxes[i], name);
                        name.gameObject.SetActive(_conditionNameBoxes[i].Active);
                    }
                    if (rtl)
                        KeepPairApart(name, value);
                    continue;
                }

                if (name != null)
                    name.gameObject.SetActive(false);
                float y = -_conditionValueBoxes[i].Position.y;
                Place(_conditionValueBoxes[i], value, 58f, y, panelWidth - 74f, 52f, 12f, true, rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
            }
        }

        private void FitEnemyIntel(bool rtl)
        {
            if (enemyIntelLabel == null || enemyIntel == null || !_enemyIntelLabelBox.Captured)
                return;

            float panelWidth = PanelWidth(enemyIntel, 375f);
            float needed = enemyIntelLabel.GetPreferredValues(enemyIntelLabel.text, 4096f, 0f).x;
            RectTransform row = enemyIntelLabel.transform.parent as RectTransform;
            bool sentence = needed > panelWidth - 90f;
            if (!sentence)
            {
                Restore(_enemyIntelRowBox, row);
                Restore(_enemyIntelLabelBox, enemyIntelLabel);
                Restore(_vehicleRowBox, FindRow("Row_LIGHT_VEHICLES"));
                Restore(_airRowBox, FindRow("Row_AIR_THREAT"));
                ContainRow(row, rtl);
                ContainRow(FindRow("Row_LIGHT_VEHICLES"), rtl);
                ContainRow(FindRow("Row_AIR_THREAT"), rtl);
                return;
            }

            Place(_enemyIntelRowBox, row, 12f, 58f, panelWidth - 24f, 78f, 0f, false, TextAlignmentOptions.TopLeft);
            Place(_enemyIntelLabelBox, enemyIntelLabel, 46f, 2f, panelWidth - 24f - 58f, 74f, 14f, true, rtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
            Place(_vehicleRowBox, FindRow("Row_LIGHT_VEHICLES"), 12f, 142f, panelWidth - 24f, 38f, 0f, false, TextAlignmentOptions.MidlineLeft);
            Place(_airRowBox, FindRow("Row_AIR_THREAT"), 12f, 184f, panelWidth - 24f, 38f, 0f, false, TextAlignmentOptions.MidlineLeft);
            ContainRow(FindRow("Row_LIGHT_VEHICLES"), rtl);
            ContainRow(FindRow("Row_AIR_THREAT"), rtl);
        }

        private void ContainObjectiveRows(bool rtl)
        {
            if (!rtl || objectiveLabels == null)
                return;
            for (int i = 0; i < objectiveLabels.Length; i++)
            {
                if (objectiveLabels[i] == null)
                    continue;
                ContainRow(objectiveLabels[i].transform.parent as RectTransform, true);
            }
        }

        private static void KeepPairApart(TMP_Text name, TMP_Text value)
        {
            if (name != null)
            {
                name.alignment = TextAlignmentOptions.MidlineLeft;
                FitSingleLine(name);
            }
            if (value != null)
            {
                value.alignment = TextAlignmentOptions.MidlineRight;
                FitSingleLine(value);
            }
        }

        private static void ContainRow(RectTransform row, bool rtl)
        {
            if (row == null)
                return;
            TMP_Text label = row.Find("Label")?.GetComponent<TMP_Text>();
            TMP_Text value = row.Find("Value")?.GetComponent<TMP_Text>();
            float rowWidth = row.sizeDelta.x;
            float rowHeight = row.sizeDelta.y;
            if (label != null)
            {
                float labelWidth = value != null ? rowWidth - 128f : rowWidth - 60f;
                label.rectTransform.anchoredPosition = new Vector2(48f, 0f);
                label.rectTransform.sizeDelta = new Vector2(Mathf.Max(40f, labelWidth), rowHeight);
                FitSingleLine(label, rowHeight);
                label.alignment = rtl ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            }
            if (value != null)
            {
                value.rectTransform.anchoredPosition = new Vector2(rowWidth - 72f, 0f);
                value.rectTransform.sizeDelta = new Vector2(64f, rowHeight);
                FitSingleLine(value, rowHeight);
                value.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        private static void FitSingleLine(TMP_Text text, float height)
        {
            text.enableAutoSizing = false;
            text.fontSize = Mathf.Max(12f, height / 2.25f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.lineSpacing = 0f;
        }

        private static void FitSingleLine(TMP_Text text) => FitSingleLine(text, text.rectTransform.sizeDelta.y);

        private RectTransform FindRow(string name) => enemyIntel != null ? enemyIntel.Find(name) as RectTransform : null;

        private void CaptureNamedRow(ref AuthoredText slot, string name) => Capture(ref slot, FindRow(name));

        private void CaptureRow(ref AuthoredText rowSlot, ref AuthoredText labelSlot, TMP_Text label)
        {
            if (label == null)
                return;
            Capture(ref rowSlot, label.transform.parent as RectTransform);
            Capture(ref labelSlot, label);
        }

        private static void CaptureMany(ref AuthoredText[] slots, TMP_Text[] texts)
        {
            if (texts == null)
                return;
            slots ??= new AuthoredText[texts.Length];
            if (slots.Length != texts.Length)
                slots = new AuthoredText[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                Capture(ref slots[i], texts[i]);
        }

        private static void Capture(ref AuthoredText slot, TMP_Text text)
        {
            if (text == null)
                return;
            Capture(ref slot, text.rectTransform);
            if (!slot.Captured)
                return;
            slot.Alignment = text.alignment;
            slot.FontSize = text.fontSize;
            slot.AutoSize = text.enableAutoSizing;
            slot.FontSizeMin = text.fontSizeMin;
            slot.FontSizeMax = text.fontSizeMax;
            slot.LineSpacing = text.lineSpacing;
            slot.Overflow = text.overflowMode;
            slot.Wrap = text.textWrappingMode;
            slot.Active = text.gameObject.activeSelf;
        }

        private static void Capture(ref AuthoredText slot, RectTransform rect)
        {
            if (slot.Captured || rect == null)
                return;
            slot.Position = rect.anchoredPosition;
            slot.Size = rect.sizeDelta;
            slot.Captured = true;
            slot.Active = rect.gameObject.activeSelf;
        }

        private static void Place(in AuthoredText authored, TMP_Text text, float x, float y, float width, float height, float fontSize, bool wrap, TextAlignmentOptions alignment)
        {
            if (text == null)
                return;
            Place(authored, text.rectTransform, x, y, width, height, 0f, false, alignment);
            text.enableAutoSizing = false;
            if (fontSize > 0f)
                text.fontSize = fontSize;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.lineSpacing = 0f;
        }

        private static void Place(in AuthoredText authored, RectTransform rect, float x, float y, float width, float height, float fontSize, bool wrap, TextAlignmentOptions alignment)
        {
            if (rect == null || width < 8f || height < 8f)
                return;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Restore(in AuthoredText slot, TMP_Text text)
        {
            if (text == null)
                return;
            Restore(slot, text.rectTransform);
            if (!slot.Captured)
                return;
            text.alignment = slot.Alignment;
            text.fontSize = slot.FontSize;
            text.enableAutoSizing = slot.AutoSize;
            text.fontSizeMin = slot.FontSizeMin;
            text.fontSizeMax = slot.FontSizeMax;
            text.lineSpacing = slot.LineSpacing;
            text.overflowMode = slot.Overflow;
            text.textWrappingMode = slot.Wrap;
            text.gameObject.SetActive(slot.Active);
        }

        private static void Restore(in AuthoredText slot, RectTransform rect)
        {
            if (!slot.Captured || rect == null)
                return;
            rect.anchoredPosition = slot.Position;
            rect.sizeDelta = slot.Size;
            rect.gameObject.SetActive(slot.Active);
        }

        private static float PanelWidth(RectTransform panel, float fallback) =>
            panel != null && panel.sizeDelta.x > 100f ? panel.sizeDelta.x : fallback;

        private static float BandWidth(TMP_Text text, float fallback)
        {
            if (text == null || text.rectTransform.parent is not RectTransform parent || parent.sizeDelta.x < 100f)
                return fallback;
            return parent.sizeDelta.x;
        }
    }
}
