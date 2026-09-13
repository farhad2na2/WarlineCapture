using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        private const float ContentBottomMargin = 12f;

        private void LateUpdate()
        {
            if (selectedSquadPanel == null || !selectedSquadPanel.activeInHierarchy ||
                _commandButtonsRoot == null || _commandButtonsRoot.activeSelf)
                return;

            // The frame's background stretches with the panel. Measure only its
            // visible, top-anchored content; the passenger drawer floats beside it.
            Transform frame = _commandButtonsRoot.transform.parent;
            if (frame == null)
                return;
            float bottom = 0f;
            for (int i = 0; i < frame.childCount; i++)
            {
                if (frame.GetChild(i) is not RectTransform child || !child.gameObject.activeSelf ||
                    child.gameObject == passengerChipRoot ||
                    (passengerDrawer != null && child == passengerDrawer.transform) ||
                    child.anchorMin.y != 1f || child.anchorMax.y != 1f)
                    continue;
                bottom = Mathf.Max(bottom, BottomFromTop(child));
            }

            if (bottom <= 0f)
                return;

            if (passengerChipRoot != null && passengerChipRoot.activeSelf &&
                passengerChipRoot.transform is RectTransform chip)
            {
                Vector2 position = chip.anchoredPosition;
                position.y = -(bottom + ContentBottomMargin) - (1f - chip.pivot.y) * chip.rect.height;
                if (chip.anchoredPosition != position)
                    chip.anchoredPosition = position;
                bottom = BottomFromTop(chip);
            }

            var panel = selectedSquadPanel.transform as RectTransform;
            if (panel != null && !Mathf.Approximately(panel.rect.height, bottom + ContentBottomMargin))
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bottom + ContentBottomMargin);
        }

        private static float BottomFromTop(RectTransform rect) =>
            -rect.anchoredPosition.y + rect.pivot.y * rect.rect.height;
    }
}
