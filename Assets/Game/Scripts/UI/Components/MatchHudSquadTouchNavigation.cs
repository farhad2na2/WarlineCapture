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
        // Opt-in evidence only; these values never participate in input decisions.
        [System.Serializable]
        public struct SlotResolutionDiagnostic
        {
            public bool collected;
            public int frame, slot, trayIdentity, page, rosterCount, rosterIndex;
            public uint group;
            public string stage, refusal;
            public string topHitName, raycastOwnerName, raycastOwnerType;
            public int topHitIdentity, raycastOwnerIdentity, requestedCardIdentity;
            public bool slotLookupEvaluated, rosterFound, presentationEvaluated, cardActive;
            public bool horizontalPresented, verticalPresented, fullyPresented;
            public bool cardInteractableEvaluated, cardInteractable;
            public Vector2 cardLow, cardHigh, point, from, to;
            public Rect viewportRect, safeAreaFrom, safeAreaTo;
            public float motionSquared, missing, half;
            public bool moveLeft, fromHitsEvaluated, toHitsEvaluated;
            public int fromHitCount, toHitCount;
            public bool fromOwnerEvaluated, fromOwnerMatch, toOwnerEvaluated, toOwnerMatch;
            public bool fromEventSystem, toEventSystem;
            public AriaTouchTarget target;
        }

        private static readonly bool CaptureSlotDiagnostics =
            System.Environment.GetEnvironmentVariable("WARLINE_SQUAD_INPUT_DIAGNOSTIC") == "1";
        private static readonly SlotResolutionDiagnostic[] SlotDiagnostics = new SlotResolutionDiagnostic[5];
        private static int captureSlot = -1;
        internal static bool CapturingSlotDiagnostic => captureSlot >= 0;
        internal static ref SlotResolutionDiagnostic CurrentSlotDiagnostic => ref SlotDiagnostics[captureSlot];
        internal static void RefuseSlot(string reason)
        {
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.refusal = reason;
        }

        // Called only on an actual top-owner mismatch. No parent search or raycast here.
        private static void RecordRaycastMismatch(GameObject topHit, Component owner, Button requested = null)
        {
            if (!CapturingSlotDiagnostic) return;
            ref var diagnostic = ref CurrentSlotDiagnostic;
            diagnostic.topHitName = topHit.name;
            diagnostic.topHitIdentity = topHit.GetEntityId().GetHashCode();
            diagnostic.raycastOwnerName = owner != null ? owner.name : null;
            diagnostic.raycastOwnerType = owner != null ? owner.GetType().FullName : null;
            diagnostic.raycastOwnerIdentity = owner != null ? owner.GetEntityId().GetHashCode() : 0;
            if (requested != null) diagnostic.requestedCardIdentity = requested.GetEntityId().GetHashCode();
        }

        public static SlotResolutionDiagnostic ReadSlotDiagnostic(int slot) =>
            slot >= 0 && slot < SlotDiagnostics.Length ? SlotDiagnostics[slot] : default;

        private static readonly List<RaycastResult> Hits = new();
        private static PointerEventData pointer;
        private static EventSystem eventOwner;

        public static AriaTouchTarget SlotTarget(MatchHudSquadTrayView tray, int slot)
        {
            if (!CaptureSlotDiagnostics || slot < 0 || slot >= 5) return ResolveSlotTarget(tray, slot);
            int previous = captureSlot;
            captureSlot = slot;
            SlotDiagnostics[slot] = new SlotResolutionDiagnostic
            {
                collected = true, frame = Time.frameCount, slot = slot,
                trayIdentity = ReferenceEquals(tray, null) ? 0 :
                    System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(tray),
                stage = "SlotTarget", refusal = "Unresolved"
            };
            try
            {
                var target = ResolveSlotTarget(tray, slot);
                CurrentSlotDiagnostic.target = target;
                if (target.Available) CurrentSlotDiagnostic.refusal = target.Drag ? "AvailableSwipe" : "AvailableTap";
                return target;
            }
            finally { captureSlot = previous; }
        }

        private static AriaTouchTarget ResolveSlotTarget(MatchHudSquadTrayView tray, int slot)
        {
            if (tray == null) { RefuseSlot("NullTray"); return default; }
            if (slot < 0 || slot >= 5) { RefuseSlot("InvalidSlot"); return default; }
            if (tray.ExpandedScrollViewport == null)
                return ButtonTarget(tray.VisibleCardButton(slot), 0);
            uint group = tray.ExpandedSlotGroupId(slot);
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.group = group;
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
            if (CapturingSlotDiagnostic)
            {
                CurrentSlotDiagnostic.stage = "CardOrSwipe";
                CurrentSlotDiagnostic.from = from; CurrentSlotDiagnostic.to = to;
            }
            if (group == 0) { RefuseSlot("ZeroGroup"); return default; }
            if (!SwipeReachable(tray, from, to)) return default;
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
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.stage = "ButtonTarget";
            if (button == null) { RefuseSlot("NullButton"); return default; }
            bool active = button.IsActive();
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.cardActive = active;
            if (!active) { RefuseSlot("ButtonInactive"); return default; }
            bool interactable = button.IsInteractable();
            if (CapturingSlotDiagnostic)
            {
                CurrentSlotDiagnostic.cardInteractableEvaluated = true;
                CurrentSlotDiagnostic.cardInteractable = interactable;
            }
            if (!interactable) { RefuseSlot("ButtonNotInteractable"); return default; }
            var rect = button.transform as RectTransform;
            var canvas = button.GetComponentInParent<Canvas>()?.rootCanvas;
            if (rect == null || canvas == null)
            { RefuseSlot(rect == null ? "ButtonRectMissing" : "RootCanvasMissing"); return default; }
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var safe = Screen.safeArea;
            if (CapturingSlotDiagnostic)
            { CurrentSlotDiagnostic.point = point; CurrentSlotDiagnostic.safeAreaFrom = safe; }
            if (!safe.Contains(point)) { RefuseSlot("ButtonOutsideSafeArea"); return default; }
            if (!ReadHits(point)) return default;
            var topHit = Hits[0].gameObject;
            var hitOwner = topHit.GetComponentInParent<Button>();
            bool ownerMatch = hitOwner == button;
            if (CapturingSlotDiagnostic)
            { CurrentSlotDiagnostic.fromOwnerEvaluated = true; CurrentSlotDiagnostic.fromOwnerMatch = ownerMatch; }
            if (!ownerMatch)
            {
                RecordRaycastMismatch(topHit, hitOwner, button);
                RefuseSlot("ButtonTopRaycastMismatch"); return default;
            }
            return new AriaTouchTarget { Id = logicalId != 0 ? logicalId : button.GetEntityId().GetHashCode(),
                Position = point, Available = true };
        }

        private static bool SwipeReachable(MatchHudSquadTrayView tray, Vector2 from, Vector2 to)
        {
            if (CapturingSlotDiagnostic)
            { CurrentSlotDiagnostic.stage = "SwipeReachable"; CurrentSlotDiagnostic.from = from; CurrentSlotDiagnostic.to = to; }
            var viewport = tray.ExpandedScrollViewport;
            if (viewport == null) { RefuseSlot("SwipeViewportMissing"); return false; }
            float motionSquared = (from - to).sqrMagnitude;
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.motionSquared = motionSquared;
            if (motionSquared < 25f) { RefuseSlot("SwipeTooShort"); return false; }
            var safeFrom = Screen.safeArea;
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.safeAreaFrom = safeFrom;
            if (!safeFrom.Contains(from)) { RefuseSlot("SwipeFromOutsideSafeArea"); return false; }
            var safeTo = Screen.safeArea;
            if (CapturingSlotDiagnostic) CurrentSlotDiagnostic.safeAreaTo = safeTo;
            if (!safeTo.Contains(to)) { RefuseSlot("SwipeToOutsideSafeArea"); return false; }
            var scroll = viewport.GetComponentInParent<ScrollRect>();
            if (scroll == null) { RefuseSlot("ScrollRectMissing"); return false; }
            if (!scroll.isActiveAndEnabled) { RefuseSlot("ScrollRectInactive"); return false; }
            if (!scroll.horizontal) { RefuseSlot("ScrollRectNotHorizontal"); return false; }
            var canvas = viewport.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, from, camera))
            { RefuseSlot("SwipeFromOutsideViewport"); return false; }
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, to, camera))
            { RefuseSlot("SwipeToOutsideViewport"); return false; }
            if (!ReadHits(from)) return false;
            var fromHit = Hits[0].gameObject;
            var fromHitOwner = fromHit.GetComponentInParent<ScrollRect>();
            bool fromOwner = fromHitOwner == scroll;
            if (CapturingSlotDiagnostic)
            { CurrentSlotDiagnostic.fromOwnerEvaluated = true; CurrentSlotDiagnostic.fromOwnerMatch = fromOwner; }
            if (!fromOwner)
            {
                RecordRaycastMismatch(fromHit, fromHitOwner);
                RefuseSlot("SwipeFromTopRaycastMismatch"); return false;
            }
            if (!ReadHits(to, true)) return false;
            var toHit = Hits[0].gameObject;
            var toHitOwner = toHit.GetComponentInParent<ScrollRect>();
            bool toOwner = toHitOwner == scroll;
            if (CapturingSlotDiagnostic)
            { CurrentSlotDiagnostic.toOwnerEvaluated = true; CurrentSlotDiagnostic.toOwnerMatch = toOwner; }
            if (!toOwner)
            {
                RecordRaycastMismatch(toHit, toHitOwner);
                RefuseSlot("SwipeToTopRaycastMismatch"); return false;
            }
            return true;
        }

        private static bool ReadHits(Vector2 point, bool secondEndpoint = false)
        {
            var current = EventSystem.current;
            if (CapturingSlotDiagnostic)
            {
                if (secondEndpoint) { CurrentSlotDiagnostic.toHitsEvaluated = true; CurrentSlotDiagnostic.toEventSystem = current != null; }
                else { CurrentSlotDiagnostic.fromHitsEvaluated = true; CurrentSlotDiagnostic.fromEventSystem = current != null; }
            }
            if (current == null) { RefuseSlot(secondEndpoint ? "ToEventSystemMissing" : "FromEventSystemMissing"); return false; }
            if (pointer == null || current != eventOwner)
            { eventOwner = current; pointer = new PointerEventData(current); }
            pointer.position = point;
            Hits.Clear(); current.RaycastAll(pointer, Hits);
            if (CapturingSlotDiagnostic)
            {
                if (secondEndpoint) CurrentSlotDiagnostic.toHitCount = Hits.Count;
                else CurrentSlotDiagnostic.fromHitCount = Hits.Count;
            }
            if (Hits.Count == 0) RefuseSlot(secondEndpoint ? "ToRaycastEmpty" : "FromRaycastEmpty");
            return Hits.Count > 0;
        }
    }
}
