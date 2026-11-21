using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        // W39 Board 1: 16 px inner padding and 12 px between card sections.
        private static float CardPadding => MatchHudSidePanelLayout.Units(MatchHudSidePanelLayout.CardPaddingPx);
        private static float SectionGap => MatchHudSidePanelLayout.Units(MatchHudSidePanelLayout.CardSectionGapPx);
        private const float CommandsHeight = 72f, HealthHeight = 31f, OrderHeight = 55f, PlayerControlHeight = 40f;
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
            // The card, minimap and first category card share one left safe inset.
            float shift = panel.InverseTransformPoint(selectionCorners[0]).x - panel.rect.xMin;
            if (Mathf.Abs(shift) > .01f)
                panel.anchoredPosition += new Vector2(shift * panel.localScale.x, 0);
            map.GetWorldCorners(selectionCorners);
            float mapTop = panel.InverseTransformPoint(selectionCorners[1]).y;
            selectionHeightLimit = MatchHudSidePanelLayout.SelectionHeightLimit(panel.rect.yMax, mapTop);
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
                    Place((RectTransform)shade.transform, CardPadding, CardPadding, -2 * CardPadding, 64);
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
            // The passenger drawer is intentionally placed beside the compact card.
            // RectMask2D on the card otherwise clips both its artwork and its raycast
            // geometry, leaving the visible passenger chip unable to reach Exit All.
            var cardMask = panel.GetComponent<RectMask2D>();
            if (cardMask != null && cardMask.enabled == _passengerDrawerOpen)
                cardMask.enabled = !_passengerDrawerOpen;
            float p = CardPadding, g = SectionGap;
            float chipHeight = passengerChipRoot != null && passengerChipRoot.activeSelf
                ? ((RectTransform)passengerChipRoot.transform).rect.height + g : 0;
            float repairHeight = FieldRepairContextHeight;
            float fixedHeight = p + g + CommandsHeight + g + HealthHeight + g + OrderHeight + g + PlayerControlHeight + p;
            float portraitHeight = Mathf.Clamp(selectionHeightLimit - fixedHeight - chipHeight - repairHeight, 100, 180);
            Place(frame.Find("PortraitFrame") as RectTransform, p, p, -2 * p, portraitHeight);
            if (titleText != null) Place(titleText.rectTransform, p + 48, p + 3, -2 * p - 50, 34);
            if (subtitleText != null) Place(subtitleText.rectTransform, p + 48, p + 37, -2 * p - 50, 24);
            float y = p + portraitHeight + g;
            Place(commandWheelOpenButton != null ? commandWheelOpenButton.transform as RectTransform : null, p, y, -2 * p, CommandsHeight);
            y += CommandsHeight + g;
            Place(frame.Find("HealthPanel") as RectTransform, p, y, -2 * p, HealthHeight); y += HealthHeight + g;
            Place(frame.Find("OrderLabel") as RectTransform, p, y, -2 * p, OrderHeight); y += OrderHeight + g;
            Place(frame.Find("V3PlayerControl") as RectTransform, p, y, -2 * p, PlayerControlHeight); y += PlayerControlHeight + g;
            LayoutFieldRepairContext(ref y);
            if (chipHeight > 0)
            {
                var chip = (RectTransform)passengerChipRoot.transform;
                // Preserve the specialized fabrication chip's width and internal layout.
                chip.anchoredPosition = new Vector2(chip.anchoredPosition.x, -y - (1 - chip.pivot.y) * chip.rect.height);
                y += chip.rect.height + g;
            }
            // The last section's trailing gap becomes the 16 px bottom padding.
            float contentHeight = y - g + p;
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
