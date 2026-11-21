using System;
using System.Collections.Generic;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    // FC/BT/CE objective skill facts read from the goal HUD's rendered frame only (AriaPresentedObjectiveFrame):
    // objective type, the score line, the infantry strength to keep, BT original hold-back and one vehicle
    // escort order offer. No Entity, enemy roster or hidden simulation value crosses this boundary.
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private byte lastObjectiveType;
        private readonly HashSet<string> escortDone = new(StringComparer.Ordinal), escortSkipped = new(StringComparer.Ordinal);
        private readonly List<AriaEscortCandidate> escortCandidates = new();
        private string escortOfferKey;
        private AriaGarrisonState escortGarrison;
        private float escortOfferedAt, escortAttemptSeconds;
        private uint escortEpoch;
        // W37f-c (S046 gate 3): groups that had any accepted order this match, and the one-time group->role map log.
        private readonly HashSet<uint> everOrdered = new();
        private bool groupMapLogged;
        private float everOrderedSeenAt = -1f;

        private void ObserveObjectiveSkillFacts(ref AriaSkirmishObservation view, AriaPresentedObjectiveFrame frame)
        {
            var goal = frame.Goal;
            var type = AriaObjectiveSkillPolicy.TypeOf(goal.Perspective);
            lastObjectiveType = (byte)type;
            view.ObjectiveType = (byte)type;
            view.ObjectiveFrameReason = 0;
            var score = new AriaObjectiveScoreObservation { Known = true, TicketsKnown = goal.Score.TicketsKnown,
                ExitOpen = goal.Score.ExitStateKnown && goal.Score.ExitOpen,
                OwnTickets = goal.Score.OwnTickets, OpposingTickets = goal.Score.OpposingTickets,
                Completed = goal.Score.Completed, Required = goal.Score.Required, Lost = goal.Score.SubjectsLost,
                Alive = goal.Score.ProgressKnown ? goal.Score.OriginalSubjects - goal.Score.Completed - goal.Score.SubjectsLost : 0 };
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.Kind != SkirmishPublicGoalNodeKind.CaptureZone || !node.OwnerKnown || node.OwnerFaction == 0) continue;
                if (node.OwnerFaction == goal.ObserverFaction) score.OwnZones++; else score.OpposingZones++;
            }
            view.ObjectiveScore = score;
            view.ObjectiveSupport.InfantryTarget = AriaObjectiveSkillPolicy.InfantryTarget(type);
        }

        private bool HoldBackOriginals(AriaPresentedObjectiveFrame frame, in SkirmishPlayerObjectiveChoice choice)
        {
            var goal = frame.Goal;
            bool chosenOriginals = false, support = false;
            foreach (var group in frame.Groups ?? Array.Empty<SkirmishPublicPlayerGroup>())
            {
                if (group == null || !group.Own || !group.Living) continue;
                bool originals = group.DesignatedSubjects != null && group.DesignatedSubjects.Length > 0;
                if (group.GroupId == choice.GroupId) chosenOriginals = originals;
                if (group.CombatInfantry && group.Dismounted && !originals) support = true;
            }
            return AriaObjectiveSkillPolicy.HoldBackOriginals(AriaObjectiveSkillPolicy.TypeOf(goal.Perspective),
                goal.Score.ExitStateKnown && goal.Score.ExitOpen, frame.SimulationSeconds, chosenOriginals, support);
        }

        private void ObserveObjectiveEscort(ref AriaSkirmishObservation view, AriaPresentedObjectiveFrame frame, string chosenGoal)
        {
            if (frame.PresentationEpoch != escortEpoch)
            { escortEpoch = frame.PresentationEpoch; escortDone.Clear(); escortSkipped.Clear(); escortOfferKey = null; escortGarrison = default; }
            var model = frame.OwnModel; var goal = frame.Goal;
            if (model == null || goal == null || goal.Outcome != SkirmishOutcomeKind.None || frame.Paused) return;
            if (frame.SimulationSeconds + 1f < everOrderedSeenAt) { everOrdered.Clear(); groupMapLogged = false; }
            everOrderedSeenAt = frame.SimulationSeconds;
            if (frame.AcceptedGroup != 0 && frame.AcceptedAction != AriaObjectiveAction.None && frame.AcceptedOrderRevision != 0) everOrdered.Add(frame.AcceptedGroup);
            foreach (var row in model.OwnGroups) if (row?.Policy != null && row.Policy.AcceptedOrderActive) everOrdered.Add(row.Policy.GroupId);
            if (!groupMapLogged && model.OwnGroups.Length > 0 && (Game.Runtime.AriaCommandEvidence.Active || UnityEngine.Debug.isDebugBuild))
            {
                groupMapLogged = true;
                var map = new System.Text.StringBuilder();
                foreach (var row in model.OwnGroups)
                    if (row?.Policy != null) map.Append(map.Length > 0 ? "," : "").Append(row.Policy.GroupId).Append(':').Append(row.Role).Append(row.Originals ? "/original" : "");
                UnityEngine.Debug.Log("[AriaObjective] type=" + AriaObjectiveSkillPolicy.TypeOf(goal.Perspective) + " event=groups map=" + map + " t=" + frame.SimulationSeconds.ToString("F2"));
            }
            // The displayed accepted Move for a group/goal completes that escort (normal HUD order receipt).
            if (frame.AcceptedGroup != 0 && !string.IsNullOrEmpty(frame.AcceptedGoalId) && frame.AcceptedAction == AriaObjectiveAction.Move)
                escortDone.Add(frame.AcceptedGroup + "|" + frame.AcceptedGoalId);
            // W16 attempt 2: the garrisoning group's displayed accepted Move ends the garrison offers.
            if (escortGarrison.Group != 0 && escortDone.Contains(escortGarrison.Group + "|" + escortGarrison.Zone)) escortGarrison.Ordered = true;
            string goalId = AriaObjectiveEscortPolicy.Goal(goal, frame.Groups, chosenGoal, ref escortGarrison);
            if (goalId == null) { escortOfferKey = null; return; }
            bool garrison = escortGarrison.Zone != null && goalId == escortGarrison.Zone;
            escortCandidates.Clear();
            foreach (var row in model.OwnGroups)
            {
                if (row?.Policy == null) continue;
                var policy = row.Policy; string key = policy.GroupId + "|" + goalId;
                bool target = false;
                foreach (var shown in frame.Targets) if (shown.GroupId == policy.GroupId && shown.PublicId == goalId) { target = true; break; }
                escortCandidates.Add(new AriaEscortCandidate { GroupId = policy.GroupId, Role = row.Role, Domain = row.Domain,
                    CanMove = policy.CanMove, Originals = row.Originals, Target = target, Skipped = escortSkipped.Contains(key),
                    Chosen = publicObjectiveChoice.Available && publicObjectiveChoice.GroupId == policy.GroupId,
                    Done = escortDone.Contains(key) || policy.AcceptedOrderActive && policy.AcceptedGoalId == goalId,
                    Holding = garrison && policy.AcceptedOrderActive && !string.IsNullOrEmpty(policy.AcceptedGoalId) && policy.AcceptedGoalId != goalId,
                    Committed = garrison && policy.AcceptedGoalId == escortGarrison.Committed, EverOrdered = everOrdered.Contains(policy.GroupId) });
            }
            {
                // W37d (lead): accepted = a fresh displayed Move toward this escort goal (this presentation epoch). The W37c
                // count (Done, which also counts an older accepted order already at the goal) is kept as Old for the trace.
                var escortType = AriaObjectiveSkillPolicy.TypeOf(goal.Perspective); int capable = 0, accepted = 0, old = 0;
                foreach (var c in escortCandidates)
                    if (AriaObjectiveEscortPolicy.Eligible(c, escortType))
                    { capable++; if (c.Done) old++; if (escortDone.Contains(c.GroupId + "|" + goalId)) accepted++; }
                view.EscortCapable = (byte)System.Math.Min(capable, 255); view.EscortAccepted = (byte)System.Math.Min(accepted, 255);
                view.EscortAcceptedOld = (byte)System.Math.Min(old, 255);
            }
            int pick = AriaObjectiveEscortPolicy.Pick(escortCandidates, AriaObjectiveSkillPolicy.TypeOf(goal.Perspective), garrison);
            if (pick < 0) { escortOfferKey = null; return; }
            var chosen = escortCandidates[pick];
            if (garrison) AriaObjectiveEscortPolicy.BindGarrison(ref escortGarrison, chosen.GroupId);
            string offer = chosen.GroupId + "|" + goalId;
            // W32: the offer expires after OfferSeconds of actual attempt (the offered group selected, as ARIA does to
            // order it), not elapsed time: an offer ARIA never got to while busy elsewhere stays open.
            if (offer != escortOfferKey) { escortOfferKey = offer; escortOfferedAt = frame.SimulationSeconds; escortAttemptSeconds = 0; }
            else
            {
                float elapsed = frame.SimulationSeconds - escortOfferedAt; escortOfferedAt = frame.SimulationSeconds;
                if (elapsed > 0 && frame.ExclusiveSelectedGroup == chosen.GroupId) escortAttemptSeconds += elapsed;
                if (AriaObjectiveEscortPolicy.OfferExpired(escortAttemptSeconds))
                {
                    escortSkipped.Add(offer); escortOfferKey = null; // Bounded: the next group gets its turn.
                    if (garrison && escortGarrison.Group == chosen.GroupId) escortGarrison.Group = 0;
                    return;
                }
            }
            AriaPresentedObjectiveTarget target1 = default;
            foreach (var shown in frame.Targets) if (shown.GroupId == chosen.GroupId && shown.PublicId == goalId) { target1 = shown; break; }
            var escort = new AriaObjectiveTouchObservation { Applicable = true, Supported = true, Presented = true,
                Intent = AriaSkirmishIntent.Advance, CameraSettled = frame.CameraSettled, Paused = frame.Paused,
                RosterRevision = frame.RosterRevision, PresentationEpoch = frame.PresentationEpoch, MoveMode = frame.MoveMode,
                AcceptedOrderRevision = frame.AcceptedOrderRevision, AcceptedGroupId = frame.AcceptedGroup,
                AcceptedNavigationGroupId = frame.AcceptedNavigationGroupId, AcceptedAction = frame.AcceptedAction,
                AcceptedContactTargetId = frame.AcceptedContactTargetId, AcceptedFrame = frame.AcceptedFrame,
                AcceptedPublicKey = PublicObjectiveKey(frame.AcceptedGoalId),
                PublicProgressRevision = frame.PublicProgressRevision, ProgressKnown = frame.ProgressKnown,
                GroupId = chosen.GroupId, NavigationGroupId = chosen.GroupId, PublicKey = PublicObjectiveKey(goalId),
                Action = AriaObjectiveAction.Move };
            escort.ExclusiveGroupSelected = frame.ExclusiveSelectedGroup == chosen.GroupId;
            escort.SelectGroup = MatchHudSquadTouchNavigation.GroupTarget(watchSquads, chosen.GroupId);
            escort.ClearOtherSelection = view.ExpandedSelectedGroups > 0 && !escort.ExclusiveGroupSelected
                ? MatchHudSquadTouchNavigation.SelectedCardTarget(watchSquads) : default;
            escort.Focus = ObserveWatchButton(target1.FocusButton);
            if (!escort.Focus.Available) escort.Focus = ObservePublicObjectiveMapFocus(target1.PublicWorld, view);
            escort.Move = ObserveWatchButton(_commandControlsView?.MoveButton);
            escort.Hold = ObserveWatchButton(_commandControlsView?.HoldButton);
            escort.GoalInView = target1.InView; escort.GroundFitsPublicGoal = target1.GroundFitsPublicGoal;
            escort.Ground = target1.Ground;
            if (escort.Ground.Available && !WatchTargetIsReachable(escort.Ground.Position, escort.Ground.Id, true)) escort.Ground = default;
            view.ObjectiveEscort = escort;
        }
    }
}
