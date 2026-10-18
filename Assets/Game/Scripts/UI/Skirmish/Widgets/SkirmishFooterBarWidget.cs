using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Library footer per level. Theatre: Back, Filters, Zoom In, Quick Battle.
    /// Battlefield: Back, Theatre Map, Start Battle, Advanced. Buttons raise UIAudioEventGateway sounds.
    /// Order is reversed for RTL; widths are weighted so the primary action is widest; the bar fits the safe frame.
    /// </summary>
    public static class SkirmishFooterBarWidget
    {
        public const string BarName = "LibraryFooterBar";
        public const string BackName = "BackButton";
        public const string AdvancedName = "AdvancedButton";
        public const float ReferenceHeight = 64f;
        public const float ReferenceGap = 8f;

        public readonly struct ButtonSpec
        {
            public ButtonSpec(string name, string locKey, string fallbackEn, string fallbackFa, float weight, Color color, UIAudioEventKind sound)
            {
                Name = name; LocKey = locKey; FallbackEn = fallbackEn; FallbackFa = fallbackFa; Weight = weight; Color = color; Sound = sound;
            }

            public string Name { get; }
            public string LocKey { get; }
            public string FallbackEn { get; }
            public string FallbackFa { get; }
            public float Weight { get; }
            public Color Color { get; }
            public UIAudioEventKind Sound { get; }
        }

        private static readonly Color Neutral = new Color(0.10f, 0.13f, 0.16f, 1f);
        private static readonly Color Blue = new Color(0.10f, 0.30f, 0.58f, 1f);
        private static readonly Color Green = new Color(0.22f, 0.50f, 0.14f, 1f);
        private static readonly Color Navy = new Color(0.07f, 0.18f, 0.40f, 1f);

        public static IReadOnlyList<ButtonSpec> Buttons(SkirmishLibraryLevel level)
        {
            if (level == SkirmishLibraryLevel.Theater)
            {
                return new[]
                {
                    new ButtonSpec(BackName, "ui.common.back", "BACK", "بازگشت", 0.9f, Neutral, UIAudioEventKind.ScreenBack),
                    new ButtonSpec(SkirmishLibraryControlNames.Filters, "ui.skirmish.library.filters", "FILTERS", "فیلترها", 1.1f, Blue, UIAudioEventKind.ButtonSecondaryClick),
                    new ButtonSpec(SkirmishLibraryControlNames.ZoomIn, "ui.skirmish.library.zoom_in", "ZOOM IN", "بزرگ‌نمایی", 1.7f, Green, UIAudioEventKind.ScreenForward),
                    new ButtonSpec(SkirmishLibraryControlNames.QuickBattle, "ui.skirmish.library.quick_battle", "QUICK BATTLE", "نبرد سریع", 1.1f, Navy, UIAudioEventKind.ButtonPrimaryClick)
                };
            }

            return new[]
            {
                new ButtonSpec(BackName, "ui.common.back", "BACK", "بازگشت", 0.9f, Neutral, UIAudioEventKind.ScreenBack),
                new ButtonSpec(SkirmishLibraryControlNames.TheaterMap, "ui.skirmish.library.theater_map", "THEATER MAP", "نقشه جبهه", 1.2f, Blue, UIAudioEventKind.ScreenBack),
                new ButtonSpec(SkirmishLibraryControlNames.StartBattle, "ui.skirmish.library.start_battle", "START BATTLE", "شروع نبرد", 1.6f, Green, UIAudioEventKind.ButtonPrimaryClick),
                new ButtonSpec(AdvancedName, "ui.skirmish.library.advanced", "ADVANCED", "پیشرفته", 1.0f, Navy, UIAudioEventKind.ButtonSecondaryClick)
            };
        }

        /// <summary>Button rects (x from the bar's left edge) for a bar of <paramref name="width"/>.</summary>
        public static Rect[] Layout(SkirmishLibraryLevel level, float width, float height, float gap, bool rightToLeft)
        {
            IReadOnlyList<ButtonSpec> specs = Buttons(level);
            float total = 0f;
            for (int i = 0; i < specs.Count; i++) total += specs[i].Weight;
            float usable = Mathf.Max(0f, width - gap * (specs.Count - 1));
            var rects = new Rect[specs.Count];
            float x = 0f;
            var widths = new float[specs.Count];
            for (int i = 0; i < specs.Count; i++) widths[i] = usable * specs[i].Weight / total;
            for (int visual = 0; visual < specs.Count; visual++)
            {
                int i = rightToLeft ? specs.Count - 1 - visual : visual;
                rects[i] = new Rect(x, 0f, widths[i], height);
                x += widths[i] + gap;
            }
            return rects;
        }

        /// <summary>Builds the bar along the bottom of the safe frame of <paramref name="context"/>.</summary>
        public static RectTransform Build(Transform parent, SkirmishLibraryLevel level, SkirmishWidgetContext context,
            Action<string> onClick = null, Func<string, bool> isEnabled = null)
        {
            Rect safe = context.SafeFrame;
            float height = context.Px(ReferenceHeight);
            float gap = context.Px(ReferenceGap);
            RectTransform bar = SkirmishWidgetFactory.Rect(BarName, parent);
            bar.anchorMin = bar.anchorMax = Vector2.zero;
            bar.pivot = Vector2.zero;
            bar.anchoredPosition = new Vector2(safe.xMin, safe.yMin);
            bar.sizeDelta = new Vector2(safe.width, height);

            IReadOnlyList<ButtonSpec> specs = Buttons(level);
            Rect[] rects = Layout(level, safe.width, height, gap, context.RightToLeft);
            for (int i = 0; i < specs.Count; i++)
            {
                ButtonSpec spec = specs[i];
                Image face = SkirmishWidgetFactory.Image(spec.Name, bar, spec.Color, null, true);
                SkirmishWidgetFactory.TopLeft(face.rectTransform, rects[i].x, 0f, rects[i].width, height);
                Image edge = SkirmishWidgetFactory.Image("Edge", face.rectTransform, new Color(1f, 1f, 1f, 0.12f));
                SkirmishWidgetFactory.Stretch(edge.rectTransform, 0f, 0f, 0f, height - Mathf.Max(1f, context.Px(2f)));
                Button button = face.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                bool enabled = isEnabled == null || isEnabled(spec.Name);
                button.interactable = enabled;
                string name = spec.Name;
                UIAudioEventKind sound = spec.Sound;
                button.onClick.AddListener(() =>
                {
                    context.Play(sound);
                    onClick?.Invoke(name);
                });
                string fallback = context.RightToLeft ? spec.FallbackFa : spec.FallbackEn;
                TextMeshProUGUI label = SkirmishWidgetFactory.Label("Label", face.rectTransform, context.Text(spec.LocKey, fallback),
                    context.Px(22f), SkirmishWidgetFactory.TextPrimary, context, TextAlignmentOptions.Center);
                SkirmishWidgetFactory.Stretch(label.rectTransform, context.Px(10f), context.Px(10f), 0f, 0f);
            }
            return bar;
        }
    }
}
