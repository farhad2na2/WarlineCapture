using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        private TMP_Text clockHeading;
        private string clockCaption = "TIME LEFT";
        private string lastClockSource, lastClockLocale;
        private bool clockLayoutReady, clockShowsScore, clockLayoutRtl;

        private void CreateClock(RectTransform parent)
        {
            var panel = Panel("MatchClock", parent, false);
            var element = panel.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 240;
            element.minWidth = 160;
            clockHeading = Label(panel, "TIME LEFT", 23);
            Stretch(clockHeading.rectTransform);
            clockHeading.rectTransform.anchorMin = new Vector2(0, .55f);
            clockHeading.rectTransform.offsetMin = new Vector2(8, 0);
            clockHeading.rectTransform.offsetMax = new Vector2(-8, -4);
            clock = Label(panel, "", 32);
            Stretch(clock.rectTransform);
            clock.rectTransform.anchorMax = new Vector2(1, .55f);
            clock.rectTransform.offsetMin = new Vector2(8, 4);
            clock.rectTransform.offsetMax = new Vector2(-8, 0);
            clock.enableAutoSizing = clockHeading.enableAutoSizing = false;
            ConfigureClockLayout(false);
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
            if (!previousPublicApplicable)
            {
                ConfigureClockLayout(false);
                UiLocalizedText.Set(clockHeading, heading);
            }
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
            ConfigureClockLayout(true);
            UiLocalizedText.Set(clockHeading, score);
        }

        private void RestoreClockCaption()
        {
            ConfigureClockLayout(false);
            UiLocalizedText.Set(clockHeading, clockCaption);
        }

        private void ConfigureClockLayout(bool score)
        {
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (clockLayoutReady && clockShowsScore == score && clockLayoutRtl == rtl) return;
            clockLayoutReady = true; clockShowsScore = score; clockLayoutRtl = rtl;
            float split = score ? .35f : .45f;
            clockHeading.rectTransform.anchorMin = new Vector2(0, split);
            clock.rectTransform.anchorMax = new Vector2(1, split);
            clockHeading.fontSize = score ? (rtl ? 18 : 20) : 23;
            clock.fontSize = score ? 28 : 32;
            // Locale bindings must retain these sizes when their source text changes.
            clockHeading.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(clockHeading.fontSize, clockHeading.fontSize);
            clock.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(clock.fontSize, clock.fontSize);
            clockHeading.textWrappingMode = score ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            clock.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
