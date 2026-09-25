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
            var pagination = footer.Find("SquadPagination") as RectTransform;
            if (pagination == null)
            {
                var go = new GameObject("SquadPagination", typeof(RectTransform), typeof(V3GradientGraphic));
                go.layer = tray.gameObject.layer;
                go.transform.SetParent(footer, false);
                pagination = go.GetComponent<RectTransform>();
            }
            pagination.anchorMin = tray.anchorMin;
            pagination.anchorMax = tray.anchorMax;
            pagination.pivot = tray.pivot;
            pagination.sizeDelta = new Vector2(707f, 72f);
            pagination.anchoredPosition = tray.anchoredPosition + new Vector2(0f, 80f);
            pagination.SetAsLastSibling();
            V3GradientGraphic surface = pagination.GetComponent<V3GradientGraphic>();
            surface.ConfigureCorners(DarkTop, DarkTop, DarkBottom, DarkBottom, new Color32(65, 78, 82, 255), 2f);
            surface.raycastTarget = false;
            var row = EnsureRect("NavigationRow", pagination);
            SetTopLeft(row, 0, 0, 707, 72);
            var clear = EnsureGeneratedButton(row, "ClearSquadSelection", 0, 0, 88, 64,
                DarkTop, DarkBottom, new Color32(86, 96, 99, 255), "CLEAR", 21);
            var previous = EnsureGeneratedButton(row, "PreviousSquadPage", 0, 0, 80, 64,
                new Color32(14, 81, 101, 255), new Color32(4, 30, 39, 255), new Color32(0, 202, 230, 255), "‹", 42);
            var next = EnsureGeneratedButton(row, "NextSquadPage", 0, 0, 80, 64,
                new Color32(14, 81, 101, 255), new Color32(4, 30, 39, 255), new Color32(0, 202, 230, 255), "›", 42);
            var range = EnsureText(row, "PageRange");
            ConfigureText(range, "SQUADS 1–5 OF 12", 24, boldFont, theme.TextPrimary, TextAlignmentOptions.Center);
            range.margin = new Vector4(0f, 1f, 0f, 36f);
            range.raycastTarget = false;
            var spacer = EnsureRect("FlexibleSpacer", row);
            LayoutElement spacerElement = spacer.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
            spacerElement.minWidth = 0; spacerElement.preferredWidth = 0; spacerElement.flexibleWidth = 1;
            ConfigureChevron(previous, left: true);
            ConfigureChevron(next, left: false);
            LayoutElement prevElement = previous.GetComponent<LayoutElement>() ?? previous.gameObject.AddComponent<LayoutElement>();
            prevElement.minWidth = prevElement.preferredWidth = 80; prevElement.flexibleWidth = 0;
            LayoutElement rangeElement = range.GetComponent<LayoutElement>() ?? range.gameObject.AddComponent<LayoutElement>();
            rangeElement.minWidth = 280; rangeElement.preferredWidth = 435; rangeElement.flexibleWidth = 1;
            LayoutElement nextElement = next.GetComponent<LayoutElement>() ?? next.gameObject.AddComponent<LayoutElement>();
            nextElement.minWidth = nextElement.preferredWidth = 80; nextElement.flexibleWidth = 0;
            LayoutElement clearElement = clear.GetComponent<LayoutElement>() ?? clear.gameObject.AddComponent<LayoutElement>();
            clearElement.minWidth = clearElement.preferredWidth = 88; clearElement.flexibleWidth = 0;
            var horizontal = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8; horizontal.childControlWidth = horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false; horizontal.childForceExpandHeight = true;
            horizontal.padding = new RectOffset(0, 0, 0, 0);
            previous.transform.SetAsFirstSibling(); range.transform.SetSiblingIndex(1);
            next.transform.SetSiblingIndex(2); clear.transform.SetAsLastSibling(); spacer.SetAsLastSibling();
            var summary = range.transform.Find("SelectionSummary")?.GetComponent<TMP_Text>() ??
                pagination.Find("SelectionSummary")?.GetComponent<TMP_Text>() ??
                EnsureText(pagination, "SelectionSummary");
            summary.transform.SetParent(range.transform, false);
            for (int i = range.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = range.transform.GetChild(i);
                if (child != summary.transform && child.name == "SelectionSummary")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            for (int i = pagination.childCount - 1; i >= 0; i--)
            {
                Transform child = pagination.GetChild(i);
                if (child.name == "SelectionSummary") UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            summary.rectTransform.anchorMin = new Vector2(0f, 0f);
            summary.rectTransform.anchorMax = new Vector2(1f, 0f);
            summary.rectTransform.pivot = new Vector2(.5f, 0f);
            summary.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            summary.rectTransform.sizeDelta = new Vector2(-8f, 34f);
            ConfigureText(summary, "Tap squads to select", 24, mediumFont, theme.TextPrimary, TextAlignmentOptions.Center);
            summary.raycastTarget = false;

            MatchHudSquadTrayView trayView = footer.GetComponentInChildren<MatchHudSquadTrayView>(true);
            if (trayView != null)
            {
                SerializedObject serialized = new(trayView);
                serialized.FindProperty("paginationRoot").objectReferenceValue = pagination;
                serialized.FindProperty("previousPageButton").objectReferenceValue = previous;
                serialized.FindProperty("nextPageButton").objectReferenceValue = next;
                serialized.FindProperty("clearSelectionButton").objectReferenceValue = clear;
                serialized.FindProperty("pageRangeLabel").objectReferenceValue = range;
                serialized.FindProperty("selectionSummaryLabel").objectReferenceValue = summary;
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
            pagination.gameObject.SetActive(false);
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
