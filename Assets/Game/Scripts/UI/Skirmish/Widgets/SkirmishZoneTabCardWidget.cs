using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Zone picture plate (grid header strip): plate art, name, won/total progress and a gold selected frame.</summary>
    public static class SkirmishZoneTabCardWidget
    {
        public const string PlateName = "Plate";
        public const string TitleName = "Title";
        public const string ProgressName = "Progress";

        public static string TabName(string mapId) => "ZoneTab_" + (mapId ?? string.Empty);

        public static string ProgressText(SkirmishTheaterPinState pin, SkirmishWidgetContext context)
        {
            // Counts are won/playable; a zone with no playable battle reads "Coming soon", never 0/0.
            if (pin.PublishedCount <= 0)
                return SkirmishBattleLibraryArt.ComingSoon(context.RightToLeft);
            string text = context.Number(pin.WonCount) + "/" + context.Number(pin.PublishedCount);
            return context.RightToLeft ? "\u2066" + text + "\u2069" : text;
        }

        /// <summary>Lays out <paramref name="count"/> tabs across <paramref name="width"/>; RTL reverses the order.</summary>
        public static Rect TabRect(int index, int count, float width, float height, float gap, bool rightToLeft)
        {
            count = Mathf.Max(1, count);
            float w = (width - gap * (count - 1)) / count;
            int visual = rightToLeft ? count - 1 - index : index;
            return new Rect(visual * (w + gap), 0f, w, height);
        }

        public static RectTransform Build(Transform parent, SkirmishTheaterPinState pin, bool selected, Vector2 size,
            SkirmishWidgetContext context, Action<string> onSelect = null)
        {
            RectTransform root = SkirmishWidgetFactory.Rect(TabName(pin.MapId), parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;
            Image background = root.gameObject.AddComponent<Image>();
            background.color = SkirmishWidgetFactory.Panel;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            string mapId = pin.MapId;
            button.onClick.AddListener(() =>
            {
                context.Play(UIAudioEventKind.TabSelect);
                onSelect?.Invoke(mapId);
            });

            Texture plate = context.LoadPreview(pin.PlatePreviewKey);
            RawImage art = SkirmishWidgetFactory.Raw(PlateName, root, plate, plate != null ? Color.white : SkirmishWidgetFactory.EmptyCell);
            float inset = Mathf.Max(1f, context.Px(2f));
            SkirmishWidgetFactory.Stretch(art.rectTransform, inset, inset, inset, inset);
            SkirmishBattleCardWidget.FitCover(art, size.x - inset * 2f, size.y - inset * 2f);

            float band = Mathf.Min(size.y * 0.52f, context.Px(54f));
            Image shade = SkirmishWidgetFactory.Image("Band", root, new Color(0f, 0f, 0f, 0.62f));
            SkirmishWidgetFactory.TopLeft(shade.rectTransform, inset, size.y - band - inset, size.x - inset * 2f, band);

            string fallbackTitle;
            SkirmishBattleLibraryArt.Copy(pin.MapId, context.RightToLeft, out fallbackTitle, out _);
            TextMeshProUGUI title = SkirmishWidgetFactory.Label(TitleName, shade.rectTransform, context.ZoneTitle != null ? context.ZoneTitle(pin.MapId) : context.Text(pin.TitleLocKey, fallbackTitle),
                context.Px(17f), SkirmishWidgetFactory.TextPrimary, context, TextAlignmentOptions.Center);
            SkirmishWidgetFactory.TopLeft(title.rectTransform, context.Px(4f), 0f, size.x - inset * 2f - context.Px(8f), band * 0.55f);
            TextMeshProUGUI progress = SkirmishWidgetFactory.Label(ProgressName, shade.rectTransform, ProgressText(pin, context),
                context.Px(16f), SkirmishWidgetFactory.TextPrimary, context, TextAlignmentOptions.Center);
            SkirmishWidgetFactory.TopLeft(progress.rectTransform, context.Px(4f), band * 0.5f, size.x - inset * 2f - context.Px(8f), band * 0.5f);

            if (selected)
                SkirmishGlowFrameWidget.Apply(root, context, null, 3f, 6f);
            return root;
        }
    }
}
