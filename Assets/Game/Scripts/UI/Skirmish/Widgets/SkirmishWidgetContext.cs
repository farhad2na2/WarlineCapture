using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>Aspect class of the safe frame the library widgets are laid out in.</summary>
    public enum SkirmishWidgetAspect
    {
        Standard,   // ~16:9
        Wide,       // 20:9 and wider
        Narrow      // 4:3 and narrower
    }

    /// <summary>
    /// Everything a standalone battle-library widget needs besides its view-model state:
    /// locale direction, string and art resolvers, font, and a layout scale derived from the safe frame.
    /// Widgets never reach into global singletons, so they are testable and capture-able in isolation.
    /// </summary>
    public sealed class SkirmishWidgetContext
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public bool RightToLeft;
        public float Scale = 1f;
        public SkirmishWidgetAspect Aspect = SkirmishWidgetAspect.Standard;
        public Rect SafeFrame = new Rect(0f, 0f, ReferenceWidth, ReferenceHeight);
        public TMP_FontAsset Font;
        public Func<string, string> Localize = key => null;
        public Func<string, Sprite> LoadSprite = path => string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        public Func<string, Texture> LoadPreview = key => null;
        /// <summary>Optional host text pipeline (locale font swap, font bounds). Receives text, value, min and max size.</summary>
        public Action<TMP_Text, string, float, float> ApplyText;
        /// <summary>Optional shared zone-name source (map id to display title). Preferred over loc keys for zone titles.</summary>
        public Func<string, string> ZoneTitle;
        public Action<UIAudioEventKind> PlaySound = kind => UIAudioEventGateway.Raise(kind);

        /// <summary>Builds a context for a canvas of <paramref name="canvasSize"/> with the given safe frame (canvas units).</summary>
        public static SkirmishWidgetContext ForFrame(Vector2 canvasSize, Rect safeFrame, bool rightToLeft)
        {
            var context = new SkirmishWidgetContext { RightToLeft = rightToLeft };
            context.ApplyFrame(canvasSize, safeFrame);
            return context;
        }

        public void ApplyFrame(Vector2 canvasSize, Rect safeFrame)
        {
            if (safeFrame.width <= 0f || safeFrame.height <= 0f)
                safeFrame = new Rect(Vector2.zero, canvasSize);
            SafeFrame = safeFrame;
            Aspect = ClassifyAspect(safeFrame.width, safeFrame.height);
            Scale = ResolveScale(safeFrame.width, safeFrame.height);
        }

        public static SkirmishWidgetAspect ClassifyAspect(float width, float height)
        {
            float aspect = height > 0f ? width / height : 16f / 9f;
            if (aspect >= 2.05f) return SkirmishWidgetAspect.Wide;
            if (aspect <= 1.55f) return SkirmishWidgetAspect.Narrow;
            return SkirmishWidgetAspect.Standard;
        }

        /// <summary>Height drives scale; narrow frames are additionally limited by width so cards never overflow.</summary>
        public static float ResolveScale(float width, float height)
        {
            float byHeight = height / ReferenceHeight;
            float byWidth = width / ReferenceWidth;
            float scale = ClassifyAspect(width, height) == SkirmishWidgetAspect.Narrow
                ? Mathf.Min(byHeight, byWidth * 1.18f)
                : byHeight;
            return Mathf.Clamp(scale, 0.5f, 2f);
        }

        public float Px(float referencePixels) => referencePixels * Scale;

        public string Text(string locKey, string fallback = "")
        {
            if (string.IsNullOrEmpty(locKey))
                return fallback ?? string.Empty;
            string value = Localize != null ? Localize(locKey) : null;
            return string.IsNullOrEmpty(value) ? (fallback ?? locKey) : value;
        }

        /// <summary>Formats a number with Persian digits in RTL locales.</summary>
        public string Number(int value) => RightToLeft ? ToPersianDigits(value.ToString()) : value.ToString();

        public static string ToPersianDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
                builder.Append(c >= '0' && c <= '9' ? (char)('\u06F0' + (c - '0')) : c);
            return builder.ToString();
        }

        public void Play(UIAudioEventKind kind)
        {
            if (kind != UIAudioEventKind.None)
                PlaySound?.Invoke(kind);
        }
    }
}
