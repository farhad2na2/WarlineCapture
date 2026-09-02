using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    internal static class V3UiPrefabFactory
    {
        internal static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        internal static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            bool raycastTarget,
            bool sliced = false)
        {
            RectTransform rect = CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(100f, 100f),
                Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sliced && sprite != null)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }

            return image;
        }

        internal static Button CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Sprite focusOverlay,
            V3UiTheme theme)
        {
            RectTransform rect = CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(120f, 44f),
                Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = theme.Normal,
                highlightedColor = theme.Highlighted,
                pressedColor = theme.Pressed,
                selectedColor = theme.Selected,
                disabledColor = theme.Disabled,
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };

            if (focusOverlay != null)
            {
                RectTransform focusRect = CreateRect(
                    "FocusOverlay",
                    rect,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(10f, 10f),
                    Vector2.zero);
                Image focusImage = focusRect.gameObject.AddComponent<Image>();
                focusImage.sprite = focusOverlay;
                focusImage.type = Image.Type.Sliced;
                focusImage.pixelsPerUnitMultiplier = 2f;
                focusImage.color = theme.Cyan;
                focusImage.raycastTarget = false;
                focusImage.gameObject.SetActive(false);

                V3UiSelectableFocusView focusView = rect.gameObject.AddComponent<V3UiSelectableFocusView>();
                focusView.Configure(focusImage);
            }

            return button;
        }
    }
}
