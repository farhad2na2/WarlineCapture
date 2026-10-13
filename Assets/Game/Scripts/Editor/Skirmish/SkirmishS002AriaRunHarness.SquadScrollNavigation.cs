#if UNITY_EDITOR
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // A preparation swipe is an input action, never a completed squad selection.
        private static bool QueueSquadTouch(AriaTouchInputUiSystemHelper input,
            in AriaTouchTarget target, out bool cardTap)
        {
            cardTap = false;
            if (input == null || input.IsBusy || !target.Available) return false;
            if (!input.TryGesture(target.Position, target.Drag ? target.DragEnd : target.Position,
                target.Drag ? .12f : .15f, target.Drag ? .4f : 0f, Time.unscaledTime)) return false;
            cardTap = !target.Drag;
            Debug.Log("[SkirmishSquadNativeNavigation] input=Touch kind=" +
                (target.Drag ? "Swipe" : "CardTap") + " logicalTarget=" + target.ScrollTargetId +
                " selectionClaimed=" + cardTap + " programmaticScroll=0");
            return true;
        }

        private static bool PlayerResumeSquadInput(in AriaTouchTarget target, bool selection)
        {
            if (!QueueSquadTouch(playerResumeTouch, target, out var cardTap)) return false;
            playerResumeDue = EditorApplication.timeSinceStartup + .75;
            return !selection || cardTap;
        }
        private static bool PlayerResumeTapSquadSlot(MatchHudSquadTrayView tray, int slot) =>
            PlayerResumeSquadInput(MatchHudSquadTouchNavigation.SlotTarget(tray, slot), true);
        private static bool PlayerResumeSwipeSquadPage(MatchHudSquadTrayView tray, bool forward) =>
            PlayerResumeSquadInput(MatchHudSquadTouchNavigation.PageTarget(tray, forward), false);
        private static bool PlayerResumeDeselectSquads(MatchHudSquadTrayView tray)
        {
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page) && page.SelectedGroups == 0) return true;
            PlayerResumeSquadInput(MatchHudSquadTouchNavigation.SelectedCardTarget(tray), true);
            return false;
        }

        private static bool ManualSquadInput(in AriaTouchTarget target, bool selection)
        {
            if (!QueueSquadTouch(manualTouch, target, out var cardTap)) return false;
            manualActions++; manualDue = EditorApplication.timeSinceStartup + .8d;
            return !selection || cardTap;
        }
        private static bool ManualTapSquadSlot(MatchHudSquadTrayView tray, int slot) =>
            ManualSquadInput(MatchHudSquadTouchNavigation.SlotTarget(tray, slot), true);
        private static bool ManualSwipeSquadPage(MatchHudSquadTrayView tray, bool forward) =>
            ManualSquadInput(MatchHudSquadTouchNavigation.PageTarget(tray, forward), false);
        private static bool ManualDeselectSquads(MatchHudSquadTrayView tray)
        {
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page) && page.SelectedGroups == 0) return true;
            ManualSquadInput(MatchHudSquadTouchNavigation.SelectedCardTarget(tray), true);
            return false;
        }

        private static bool HelipadSquadInput(in AriaTouchTarget target, double now, bool selection)
        {
            if (!QueueSquadTouch(helipadProbeTouch, target, out var cardTap)) return false;
            helipadProbeNext = now + .8d;
            return !selection || cardTap;
        }
        private static bool TapHelipadSquadSlot(MatchHudSquadTrayView tray, int slot, double now) =>
            HelipadSquadInput(MatchHudSquadTouchNavigation.SlotTarget(tray, slot), now, true);
        private static bool SwipeHelipadSquadPage(MatchHudSquadTrayView tray, bool forward, double now) =>
            HelipadSquadInput(MatchHudSquadTouchNavigation.PageTarget(tray, forward), now, false);
        private static bool DeselectHelipadSquads(MatchHudSquadTrayView tray, double now)
        {
            if (UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page) && page.SelectedGroups == 0) return true;
            HelipadSquadInput(MatchHudSquadTouchNavigation.SelectedCardTarget(tray), now, true);
            return false;
        }
    }
}
#endif
