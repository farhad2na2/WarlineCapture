using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSquadTrayView
    {
        private void SetNextPageArrow(bool visible)
        {
            if (visible && nextPageArrow == null && TryGetCard(4, out var card) && card.Button != null)
            {
                var arrow = new GameObject("NextPageArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
                arrow.layer = card.Button.gameObject.layer;
                arrow.transform.SetParent(card.Button.transform, false);
                nextPageArrow = arrow.GetComponent<TextMeshProUGUI>();
                nextPageArrow.font = card.NameLabel.font;
                nextPageArrow.text = ">>";
                nextPageArrow.fontSize = 56;
                nextPageArrow.alignment = TextAlignmentOptions.Center;
                nextPageArrow.color = CardLabelColor;
                nextPageArrow.raycastTarget = false;
                var rect = nextPageArrow.rectTransform;
                rect.anchorMin = new Vector2(0, 0.3f);
                rect.anchorMax = new Vector2(1, 0.8f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            if (nextPageArrow != null) nextPageArrow.gameObject.SetActive(visible);
        }

        private void CreateCardLabels()
        {
            for (int i = 0; i < cards.Length && i < CardLabels.Length; i++)
            {
                if (!TryGetCard(i, out Card card) || card.Button == null)
                    continue;

                RectTransform cardRect = card.Button.transform as RectTransform;
                if (cardRect == null)
                    continue;

                Transform existingStrip = card.NameStrip;
                GameObject stripObject = existingStrip != null ? existingStrip.gameObject : new GameObject("NameStrip");
                if (existingStrip == null)
                    stripObject.transform.SetParent(cardRect, false);
                stripObject.layer = cardRect.gameObject.layer;
                RectTransform stripRect = stripObject.GetComponent<RectTransform>() ?? stripObject.AddComponent<RectTransform>();
                stripRect.anchorMin = new Vector2(0f, 0f);
                stripRect.anchorMax = new Vector2(1f, 0f);
                stripRect.pivot = new Vector2(0.5f, 0f);
                stripRect.anchoredPosition = new Vector2(0f, 6f);
                stripRect.sizeDelta = new Vector2(-12f, 34f);
                Image stripImage = stripObject.GetComponent<Image>() ?? stripObject.AddComponent<Image>();
                stripImage.color = CardLabelStripColor;
                stripImage.raycastTarget = false;

                Transform existingLabel = card.NameLabel != null ? card.NameLabel.transform : null;
                GameObject labelObject = existingLabel != null ? existingLabel.gameObject : new GameObject("Label");
                if (existingLabel == null)
                    labelObject.transform.SetParent(stripRect, false);
                labelObject.layer = stripObject.layer;
                RectTransform labelRect = labelObject.GetComponent<RectTransform>() ?? labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(4f, 0f);
                labelRect.offsetMax = new Vector2(-4f, 0f);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>() ?? labelObject.AddComponent<TextMeshProUGUI>();
                if (cardLabelFont != null)
                    label.font = cardLabelFont;
                label.text = CardLabels[i];
                label.fontStyle = FontStyles.Bold;
                label.fontSize = 21f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 21f;
                label.color = CardLabelColor;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.raycastTarget = false;
                card.NameStrip = stripRect; card.NameLabel = label;
            }
        }

        private void ApplyMissionDisabledTreatment(int index, bool unavailable)
        {
            if (!TryGetCard(index, out Card card) || card.Button == null)
                return;

            // Older prefabs may still contain the blue overlay. The shared grayscale
            // material is the only disabled treatment, including during cinematics.
            if (card.DisabledWash != null)
                card.DisabledWash.gameObject.SetActive(false);

            V3GradientGraphic v3Frame = card.Button.GetComponentInChildren<V3GradientGraphic>(true);
            if (v3Frame != null)
            {
                bool selected = ToSlot(index) == _selectedSlot;
                Color border = unavailable
                    ? V3MissionDisabledBorderColor
                    : selected ? V3SelectedBorderColor : V3NormalBorderColor;
                v3Frame.SetBorder(border, unavailable ? 5f : 3f);
            }
        }

    }
}
