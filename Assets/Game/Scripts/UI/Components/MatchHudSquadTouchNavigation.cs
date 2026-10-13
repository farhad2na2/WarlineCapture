using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Read presented squad controls; callers execute normal pointer input.</summary>
    public static class MatchHudSquadTouchNavigation
    {
        private static readonly List<RaycastResult> Hits = new();
        private static PointerEventData pointer;
        private static EventSystem eventOwner;

        public static AriaTouchTarget SlotTarget(MatchHudSquadTrayView tray, int slot)
        {
            if (tray == null || slot < 0 || slot >= 5) return default;
            if (tray.ExpandedScrollViewport == null)
                return ButtonTarget(tray.VisibleCardButton(slot), 0);
            uint group = tray.ExpandedSlotGroupId(slot);
            if (!tray.TryGetExpandedSlotTarget(slot, out var card, out var from, out var to)) return default;
            return CardOrSwipe(tray, group, card, from, to);
        }

        public static AriaTouchTarget GroupTarget(MatchHudSquadTrayView tray, uint group)
        {
            if (tray == null || group == 0 ||
                !tray.TryGetExpandedCardTarget(group, out var card, out var from, out var to)) return default;
            return CardOrSwipe(tray, group, card, from, to);
        }

        public static AriaTouchTarget SelectedCardTarget(MatchHudSquadTrayView tray)
        {
            if (tray == null || !tray.TryGetFirstSelectedExpandedCardTarget(
                out var group, out var card, out var from, out var to)) return default;
            return CardOrSwipe(tray, group, card, from, to);
        }

        public static AriaTouchTarget PageTarget(MatchHudSquadTrayView tray, bool forward)
        {
            if (tray == null || !tray.TryGetExpandedPageSwipe(forward ? 1 : -1, out var from, out var to) ||
                !SwipeReachable(tray, from, to)) return default;
            var viewport = tray.ExpandedScrollViewport;
            var scroll = viewport.GetComponentInParent<ScrollRect>();
            float overflow = Mathf.Max(0f, scroll.content.rect.width - viewport.rect.width);
            bool rtl = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float logical = (rtl ? 1f - scroll.horizontalNormalizedPosition : scroll.horizontalNormalizedPosition) * overflow;
            float distance = (forward ? overflow - logical : logical) / Mathf.Max(1f, viewport.rect.width);
            return new AriaTouchTarget
            {
                Id = SwipeId(tray, forward), Available = true, Drag = true, Position = from, DragEnd = to,
                ScrollTargetId = 0x20000000 | (tray.LogicalPageIndex + (forward ? 1 : -1) + 1),
                ScrollDistanceViewports = Mathf.Max(0f, distance)
            };
        }

        private static AriaTouchTarget CardOrSwipe(MatchHudSquadTrayView tray, uint group,
            Button card, Vector2 from, Vector2 to)
        {
            if (card != null) return ButtonTarget(card, GroupId(group));
            if (group == 0 || !SwipeReachable(tray, from, to)) return default;
            return new AriaTouchTarget
            {
                Id = SwipeId(tray, to.x < from.x), Available = true, Drag = true,
                Position = from, DragEnd = to, ScrollTargetId = GroupId(group),
                ScrollDistanceViewports = tray.ExpandedCardScrollDistance(group)
            };
        }

        private static int GroupId(uint group) => (int)(0x40000000u | (group & 0x1fffffffu));
        private static int SwipeId(MatchHudSquadTrayView tray, bool forward) =>
            tray.ExpandedScrollViewport.GetEntityId().GetHashCode() ^ (forward ? 0x10000000 : 0x18000000);

        private static AriaTouchTarget ButtonTarget(Button button, int logicalId)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return default;
            var rect = button.transform as RectTransform;
            var canvas = button.GetComponentInParent<Canvas>()?.rootCanvas;
            if (rect == null || canvas == null) return default;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            if (!Screen.safeArea.Contains(point) || !ReadHits(point) ||
                Hits[0].gameObject.GetComponentInParent<Button>() != button) return default;
            return new AriaTouchTarget { Id = logicalId != 0 ? logicalId : button.GetEntityId().GetHashCode(),
                Position = point, Available = true };
        }

        private static bool SwipeReachable(MatchHudSquadTrayView tray, Vector2 from, Vector2 to)
        {
            var viewport = tray.ExpandedScrollViewport;
            if (viewport == null || (from - to).sqrMagnitude < 25f ||
                !Screen.safeArea.Contains(from) || !Screen.safeArea.Contains(to)) return false;
            var scroll = viewport.GetComponentInParent<ScrollRect>();
            if (scroll == null || !scroll.isActiveAndEnabled || !scroll.horizontal) return false;
            var canvas = viewport.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, from, camera) ||
                !RectTransformUtility.RectangleContainsScreenPoint(viewport, to, camera)) return false;
            return ReadHits(from) && Hits[0].gameObject.GetComponentInParent<ScrollRect>() == scroll &&
                ReadHits(to) && Hits[0].gameObject.GetComponentInParent<ScrollRect>() == scroll;
        }

        private static bool ReadHits(Vector2 point)
        {
            var current = EventSystem.current;
            if (current == null) return false;
            if (pointer == null || current != eventOwner)
            { eventOwner = current; pointer = new PointerEventData(current); }
            pointer.position = point;
            Hits.Clear(); current.RaycastAll(pointer, Hits);
            return Hits.Count > 0;
        }
    }
}
