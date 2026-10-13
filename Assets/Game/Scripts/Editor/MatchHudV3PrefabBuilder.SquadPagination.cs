#if UNITY_EDITOR
using System;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        private static void ApplySquadPagination(GameObject root, RectTransform tray)
        {
            if (root == null || tray == null || tray.parent == null) return;
            Transform footer = tray.parent;
            var oldPagination = footer.Find("SquadPagination");
            if (oldPagination != null) UnityEngine.Object.DestroyImmediate(oldPagination.gameObject);
            MatchHudSquadTrayView trayView = footer.GetComponentInChildren<MatchHudSquadTrayView>(true);
            if (trayView != null)
            {
                var serialized = new SerializedObject(trayView);
                foreach (var field in new[] { "paginationRoot", "previousPageButton", "nextPageButton",
                    "clearSelectionButton", "pageRangeLabel", "selectionSummaryLabel" })
                    serialized.FindProperty(field).objectReferenceValue = null;
                SerializedProperty cards = serialized.FindProperty("cards");
                for (int i = 0; cards != null && i < cards.arraySize; i++)
                {
                    SerializedProperty card = cards.GetArrayElementAtIndex(i);
                    Transform cardRoot = FindDeepChild(tray, "SquadCard" + (i + 1));
                    if (cardRoot == null) continue;
                    var badge = EnsureRect("RosterOrdinal", cardRoot);
                    badge.anchorMin = new Vector2(0, 1); badge.anchorMax = badge.anchorMin; badge.pivot = new Vector2(0, 1);
                    badge.anchoredPosition = new Vector2(7, -7); badge.sizeDelta = new Vector2(27, 27);
                    var badgeGraphic = badge.GetComponent<V3GradientGraphic>() ?? badge.gameObject.AddComponent<V3GradientGraphic>();
                    badgeGraphic.ConfigureCorners(DarkTop, DarkTop, DarkBottom, DarkBottom, new Color32(98, 112, 115, 255), 1.5f);
                    badgeGraphic.raycastTarget = false;
                    var ordinal = EnsureText(badge, "Number");
                    ConfigureText(ordinal, (i + 1).ToString(), 18, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
                    ordinal.raycastTarget = false;
                    var check = EnsureRect("SelectedCheck", cardRoot);
                    check.anchorMin = new Vector2(1, 1); check.anchorMax = check.anchorMin; check.pivot = Vector2.one;
                    check.anchoredPosition = new Vector2(-7, -7); check.sizeDelta = new Vector2(25, 25);
                    var checkBack = check.GetComponent<Image>() ?? check.gameObject.AddComponent<Image>();
                    checkBack.color = new Color32(0, 185, 56, 255); checkBack.raycastTarget = false;
                    var shortTick = EnsureImage(check, "TickShort");
                    var longTick = EnsureImage(check, "TickLong");
                    shortTick.color = longTick.color = Color.white; shortTick.raycastTarget = longTick.raycastTarget = false;
                    var shortRect = shortTick.rectTransform; shortRect.anchorMin = shortRect.anchorMax = new Vector2(.5f, .5f);
                    shortRect.sizeDelta = new Vector2(3, 8); shortRect.anchoredPosition = new Vector2(-3, -1); shortRect.localRotation = Quaternion.Euler(0, 0, 45);
                    var longRect = longTick.rectTransform; longRect.anchorMin = longRect.anchorMax = new Vector2(.5f, .5f);
                    longRect.sizeDelta = new Vector2(3, 13); longRect.anchoredPosition = new Vector2(3, 1); longRect.localRotation = Quaternion.Euler(0, 0, -45);
                    card.FindPropertyRelative("OrdinalBadge").objectReferenceValue = ordinal;
                    card.FindPropertyRelative("SelectedCheck").objectReferenceValue = check.gameObject;
                    Transform fillTransform = FindDeepChild(cardRoot, "HealthFill");
                    if (fillTransform != null && fillTransform.TryGetComponent(out Image fill))
                    { fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; card.FindPropertyRelative("HealthFillImage").objectReferenceValue = fill; }
                    var aliveLabel = EnsureText(cardRoot, "AliveCount");
                    aliveLabel.gameObject.layer = cardRoot.gameObject.layer;
                    SetTopLeft(aliveLabel.rectTransform, 7, 170, 121, 19);
                    ConfigureText(aliveLabel, "8 units", 16, mediumFont, theme.TextPrimary, TextAlignmentOptions.Center);
                    aliveLabel.raycastTarget = false;
                    card.FindPropertyRelative("AliveCountLabel").objectReferenceValue = aliveLabel;
                    badge.gameObject.SetActive(false); check.gameObject.SetActive(false);
                    aliveLabel.gameObject.SetActive(false);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

        }

        private static void ConfigureChevron(Button button, bool left)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = string.Empty; label.raycastTarget = false; }
            Transform upper = button.transform.Find("ChevronUpper");
            if (upper == null) upper = CreateChevronBar(button.transform, "ChevronUpper");
            Transform lower = button.transform.Find("ChevronLower");
            if (lower == null) lower = CreateChevronBar(button.transform, "ChevronLower");
            RectTransform a = (RectTransform)upper, b = (RectTransform)lower;
            a.anchorMin = a.anchorMax = b.anchorMin = b.anchorMax = new Vector2(.5f, .5f);
            a.sizeDelta = b.sizeDelta = new Vector2(4f, 18f);
            a.anchoredPosition = new Vector2(left ? 4 : -4, 5);
            b.anchoredPosition = new Vector2(left ? 4 : -4, -5);
            a.localRotation = Quaternion.Euler(0, 0, left ? -45 : 45);
            b.localRotation = Quaternion.Euler(0, 0, left ? 45 : -45);
        }

        private static RectTransform CreateChevronBar(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = Color.white; image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }
    }
}
#endif
