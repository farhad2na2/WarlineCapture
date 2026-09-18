#if UNITY_EDITOR
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Consistent, localized header navigation for pages and modal children.</summary>
    internal static class ModePageNavigationPrefabBuilder
    {
        public static Button AddBackButton(RectTransform rect, TMP_FontAsset font)
        {
            var panel = rect.GetComponent<V3GradientGraphic>() ?? rect.gameObject.AddComponent<V3GradientGraphic>();
            panel.Configure(new Color32(27, 37, 42, 255), new Color32(4, 11, 14, 255), new Color32(0, 190, 230, 255), 3f);
            panel.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            var iconRect = Child("BackIcon", rect);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f);
            iconRect.pivot = new Vector2(0, .5f);
            iconRect.anchoredPosition = new Vector2(18, 0);
            iconRect.sizeDelta = new Vector2(40, 40);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.CommanderBackIconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var labelRect = Child("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(70, 8);
            labelRect.offsetMax = new Vector2(-12, -8);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 30;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "BACK";
            label.gameObject.AddComponent<V3LocalizedTextBindingView>().Configure("ui.navigation.back", "BACK", false);
            return button;
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
#endif
