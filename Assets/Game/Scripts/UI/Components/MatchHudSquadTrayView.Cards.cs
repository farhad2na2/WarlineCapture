using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSquadTrayView
    {
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

            TintMissionDisabledGraphics(card, unavailable);

            Image wash = EnsureMissionDisabledWash(index, card);
            if (wash != null)
            {
                // Reassert the shared V3 overlay after the grayscale material pass so every
                // unavailable card has the same static blue tint instead of a one-off flash.
                wash.material = null;
                wash.color = V3MissionDisabledWashColor;
                wash.transform.SetAsLastSibling();
                if (wash.gameObject.activeSelf != unavailable)
                    wash.gameObject.SetActive(unavailable);
            }

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

        private static void TintMissionDisabledGraphics(Card card, bool unavailable)
        {
            if (!unavailable || card?.Button == null)
                return;

            Graphic[] graphics = card.Button.GetComponentsInChildren<Graphic>(true);
            for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
            {
                Graphic graphic = graphics[graphicIndex];
                if (graphic == null || graphic is TMP_Text ||
                    graphic.gameObject.name == "MissionDisabledBlueWash")
                    continue;

                UiDisabledMaterialStateView disabledState = graphic.GetComponent<UiDisabledMaterialStateView>();
                Color source = disabledState != null ? disabledState.OriginalColor : graphic.color;
                float neutral = Mathf.Clamp(source.grayscale, 0.42f, 0.82f);
                Color disabledBlue = new(0.12f, 0.50f, 0.82f, source.a);
                graphic.color = Color.Lerp(
                    new Color(neutral, neutral, neutral, source.a),
                    disabledBlue,
                    0.46f);
            }
        }

        private Image EnsureMissionDisabledWash(int index, Card card)
        {
            if (_missionDisabledWashes[index] != null)
                return _missionDisabledWashes[index];

            Transform existing = card.DisabledWash != null ? card.DisabledWash.transform : null;
            GameObject washObject = existing != null
                ? existing.gameObject
                : new GameObject("MissionDisabledBlueWash", typeof(RectTransform), typeof(Image));
            if (existing == null)
                washObject.transform.SetParent(card.Button.transform, false);
            washObject.layer = card.Button.gameObject.layer;
            washObject.transform.SetAsLastSibling();
            RectTransform rect = washObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image wash = washObject.GetComponent<Image>();
            wash.material = null;
            wash.color = V3MissionDisabledWashColor;
            wash.raycastTarget = false;
            _missionDisabledWashes[index] = wash;
            card.DisabledWash = wash;
            return wash;
        }

    }
}
