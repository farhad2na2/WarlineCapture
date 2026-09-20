using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        private const float ContentBottomMargin = 12f;
        private float selectionHeightLimit = float.PositiveInfinity;
        private RectTransform compactFrame;
        private ScrollRect selectionScroll;
        private readonly Vector3[] selectionCorners = new Vector3[4];
        public RectTransform VisiblePanelRect => selectedSquadPanel != null && selectedSquadPanel.activeInHierarchy
            ? selectedSquadPanel.transform as RectTransform : null;

        public void FitAboveMinimap(RectTransform map)
        {
            var panel = VisiblePanelRect;
            if (panel == null) return;
            map.GetWorldCorners(selectionCorners);
            float mapTop = panel.InverseTransformPoint(selectionCorners[1]).y;
            selectionHeightLimit = Mathf.Max(80, panel.rect.yMax - mapTop - 12);
            LayoutCompactSelection();
        }

        private void LateUpdate() => LayoutCompactSelection();

        private void LayoutCompactSelection()
        {
            var panel = VisiblePanelRect;
            if (panel == null || _commandButtonsRoot == null || _commandButtonsRoot.activeSelf) return;
            var frame = _commandButtonsRoot.transform.parent as RectTransform;
            if (frame == null) return;
            if (compactFrame == null)
            {
                compactFrame = frame;
                // Text remains bound to the existing localized read model. Only its layout changes.
                var portrait = frame.Find("PortraitFrame") as RectTransform;
                if (portrait != null)
                {
                    var shade = new GameObject("SelectionTitleBackdrop", typeof(RectTransform), typeof(Image));
                    shade.transform.SetParent(frame, false);
                    var image = shade.GetComponent<Image>();
                    image.color = new Color32(2, 12, 18, 215); image.raycastTarget = false;
                    Place((RectTransform)shade.transform, 14, 12, -28, 64);
                    shade.transform.SetSiblingIndex(portrait.GetSiblingIndex() + 1);
                    titleText?.transform.SetAsLastSibling();
                    subtitleText?.transform.SetAsLastSibling();
                    badgeRoot?.transform.SetAsLastSibling();
                }
                // Overflow is rare (short screens / extended building details), but must
                // remain accessible without covering or displacing the fixed minimap.
                var surface = panel.GetComponent<Image>();
                if (surface == null) { surface = panel.gameObject.AddComponent<Image>(); surface.color = Color.clear; }
                surface.raycastTarget = true;
                if (panel.GetComponent<RectMask2D>() == null) panel.gameObject.AddComponent<RectMask2D>();
                selectionScroll = panel.GetComponent<ScrollRect>();
                if (selectionScroll == null) selectionScroll = panel.gameObject.AddComponent<ScrollRect>();
                if (passengerDrawer != null)
                {
                    // The passenger drawer floats beside the card and must not inherit its clipping.
                    var drawerCanvas = passengerDrawer.GetComponent<Canvas>();
                    if (drawerCanvas == null) drawerCanvas = passengerDrawer.gameObject.AddComponent<Canvas>();
                    drawerCanvas.overrideSorting = true;
                    var parentCanvas = panel.GetComponentInParent<Canvas>();
                    drawerCanvas.sortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 1 : 1;
                    if (passengerDrawer.GetComponent<GraphicRaycaster>() == null)
                        passengerDrawer.gameObject.AddComponent<GraphicRaycaster>();
                }
                selectionScroll.content = frame; selectionScroll.viewport = panel;
                selectionScroll.horizontal = false; selectionScroll.movementType = ScrollRect.MovementType.Clamped;
                selectionScroll.inertia = false;
                frame.anchorMin = new Vector2(0, 1); frame.anchorMax = Vector2.one;
                frame.pivot = new Vector2(.5f, 1); frame.anchoredPosition = Vector2.zero;
                frame.sizeDelta = new Vector2(0, frame.rect.height);
            }
            float chipHeight = passengerChipRoot != null && passengerChipRoot.activeSelf
                ? ((RectTransform)passengerChipRoot.transform).rect.height + 8 : 0;
            float portraitHeight = Mathf.Clamp(selectionHeightLimit - 247 - chipHeight, 100, 180);
            Place(frame.Find("PortraitFrame") as RectTransform, 14, 12, -28, portraitHeight);
            if (titleText != null) Place(titleText.rectTransform, 62, 15, -78, 34);
            if (subtitleText != null) Place(subtitleText.rectTransform, 62, 49, -78, 24);
            float y = portraitHeight + 20;
            Place(commandWheelOpenButton != null ? commandWheelOpenButton.transform as RectTransform : null, 14, y, -28, 72);
            y += 80;
            Place(frame.Find("HealthPanel") as RectTransform, 17, y, -34, 31); y += 39;
            Place(frame.Find("OrderLabel") as RectTransform, 17, y, -34, 55); y += 63;
            Place(frame.Find("V3PlayerControl") as RectTransform, 17, y, -34, 40); y += 48;
            if (chipHeight > 0)
            {
                var chip = (RectTransform)passengerChipRoot.transform;
                // Preserve the specialized fabrication chip's width and internal layout.
                chip.anchoredPosition = new Vector2(chip.anchoredPosition.x, -y - (1 - chip.pivot.y) * chip.rect.height);
                y += chip.rect.height + 8;
            }
            float contentHeight = y + ContentBottomMargin;
            float height = Mathf.Min(contentHeight, selectionHeightLimit);
            if (!Mathf.Approximately(panel.rect.height, height))
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (!Mathf.Approximately(frame.rect.height, contentHeight))
                frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            selectionScroll.vertical = contentHeight > height + .5f;
            if (!selectionScroll.vertical) frame.anchoredPosition = Vector2.zero;
        }

        private static void Place(RectTransform rect, float left, float top, float widthDelta, float height)
        {
            if (rect == null) return;
            var topLeft = new Vector2(0, 1);
            if (rect.anchorMin != topLeft) rect.anchorMin = topLeft;
            if (rect.anchorMax != Vector2.one) rect.anchorMax = Vector2.one;
            if (rect.pivot != topLeft) rect.pivot = topLeft;
            var position = new Vector2(left, -top); var size = new Vector2(widthDelta, height);
            if (rect.anchoredPosition != position) rect.anchoredPosition = position;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
        }
    }
}
