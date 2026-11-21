using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Card = Game.UI.Runtime.SkirmishObjectiveCardLayout;

namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        private TMP_Text clockHeading;
        private string clockCaption = "TIME LEFT";
        private string lastClockSource, lastClockLocale;
        private bool clockLayoutReady, clockLayoutRtl;

        // The timer sits at the end of the card's purpose row (W40): a caption over the countdown.
        private void CreateClock(RectTransform parent)
        {
            var panel = Panel("MatchClock", parent, false);
            clockHeading = Label(panel, "TIME LEFT", Card.CaptionSize);
            clockHeading.gameObject.name = "MatchClockCaption";
            Stretch(clockHeading.rectTransform);
            clockHeading.rectTransform.anchorMin = new Vector2(0, .6f);
            clockHeading.rectTransform.offsetMin = new Vector2(4, 0);
            clockHeading.rectTransform.offsetMax = new Vector2(-4, 0);
            clockHeading.color = Card.MutedText;
            clock = Label(panel, "", Card.TimerSize);
            clock.gameObject.name = "MatchClockValue";
            Stretch(clock.rectTransform);
            clock.rectTransform.anchorMax = new Vector2(1, .6f);
            clock.rectTransform.offsetMin = new Vector2(4, 0);
            clock.rectTransform.offsetMax = new Vector2(-4, 0);
            clock.enableAutoSizing = clockHeading.enableAutoSizing = false;
            ConfigureClockLayout();
        }

        // Objective score (tickets, or completed and alive) is its own status cell, never the countdown.
        private void CreateObjectiveScore(RectTransform row)
        {
            var cell = Panel("ObjectiveScoreCell", row, false);
            var element = cell.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = Card.ScoreMinWidth; element.preferredHeight = Card.ControlHeight;
            objectiveScore = Label(cell, "", Card.ScoreSize);
            objectiveScore.gameObject.name = "ObjectiveScore";
            Stretch(objectiveScore.rectTransform);
            objectiveScore.rectTransform.offsetMin = new Vector2(4, 4);
            objectiveScore.rectTransform.offsetMax = new Vector2(-4, -4);
            objectiveScore.textWrappingMode = TextWrappingModes.NoWrap;
            objectiveScore.overflowMode = TextOverflowModes.Overflow;
            objectiveScore.enableAutoSizing = true;
            objectiveScore.fontSizeMin = Card.ScoreMinSize; objectiveScore.fontSizeMax = Card.ScoreSize;
            objectiveScore.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(Card.ScoreMinSize, Card.ScoreSize);
            cell.gameObject.SetActive(false);
        }

        private void RefreshClock(string source)
        {
            source ??= string.Empty;
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            if (lastClockSource == source && lastClockLocale == locale) return;
            lastClockSource = source; lastClockLocale = locale;
            int separator = source.LastIndexOf("  ", StringComparison.Ordinal);
            string heading = separator >= 0 ? source.Substring(0, separator) : "TIME LEFT";
            string value = separator >= 0 ? source.Substring(separator + 2).Trim() : source;
            clockCaption = heading;
            ConfigureClockLayout();
            UiLocalizedText.Set(clockHeading, heading);
            UiLocalizedText.Set(clock, value);
            // A numeric readout has its own LTR element; RTL captions cannot reorder
            // the colon or move the countdown while seconds change.
            clock.isRightToLeftText = false;
            clock.alignment = clockHeading.alignment = TextAlignmentOptions.Center;
            clock.color = value.StartsWith("0:", StringComparison.Ordinal)
                ? new Color(1, .65f, .1f) : Color.white;
        }

        private void PresentObjectiveClock(string score)
        {
            ConfigureClockLayout();
            objectiveScore.transform.parent.gameObject.SetActive(true);
            UiLocalizedText.Set(objectiveScore, score);
            objectiveScore.alignment = TextAlignmentOptions.Center;
        }

        private void RestoreClockCaption()
        {
            ConfigureClockLayout();
            if (objectiveScore != null) objectiveScore.transform.parent.gameObject.SetActive(false);
            UiLocalizedText.Set(clockHeading, clockCaption);
        }

        private void ConfigureClockLayout()
        {
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (clockLayoutReady && clockLayoutRtl == rtl) return;
            clockLayoutReady = true; clockLayoutRtl = rtl;
            clockHeading.fontSize = Card.CaptionSize;
            clock.fontSize = Card.TimerSize;
            // Locale bindings must retain these sizes when their source text changes.
            clockHeading.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(Card.CaptionSize * .8f, Card.CaptionSize);
            clock.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(clock.fontSize, clock.fontSize);
            clockHeading.textWrappingMode = TextWrappingModes.NoWrap;
            clock.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
