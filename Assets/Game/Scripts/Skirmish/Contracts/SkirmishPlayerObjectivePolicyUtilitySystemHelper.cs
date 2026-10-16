using System;
using System.Collections.Generic;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishPublicPlayerAction : byte { None, Move, Hold, Unload, Scout, Handback }
    public enum SkirmishPlayerObjectiveReason : byte
    { None, ContestMajority, SustainMajority, NeutralizeThenCapture, OpenCorridor, EvacuateOriginals, UnloadOriginals, EscortOriginalTruck, ProtectDwell, InspectUnknownRoute, MissingPublicCapability, Terminal }

    // Values supplied by the player's displayed roster, never gameplay Entity handles.
    // DesignatedSubjects contains only original IDs shown on the group card/inspection.
    public sealed class SkirmishPublicPlayerGroup
    {
        public uint GroupId;
        public bool Own, Living, CombatInfantry, Dismounted, CanMove, CanHold, CanUnload;
        public bool PositionKnown, UnderObservedThreat, PublicTerminalRisk, ObservedCounterAdvantage;
        public float WorldX, WorldZ;
        public string AcceptedGoalId;
        public bool AcceptedOrderActive;
        public string[] DesignatedSubjects = Array.Empty<string>();
    }

    // An inspected public map route or currently visible reachable ground target.
    // A hidden native pathfinder/collector must never manufacture this observation.
    public struct SkirmishPublicPlayerApproach
    {
        public uint GroupId;
        public string GoalId;
        public bool Inspected, GroundCompatible, CurrentlyObstructed, InsideGoalFootprint;
        public float TravelSeconds, ObservedExposure;
    }

    public struct SkirmishPlayerObjectiveChoice
    {
        public uint GroupId;
        public string GoalId;
        public SkirmishPublicPlayerAction Action;
        public SkirmishPlayerObjectiveReason Reason;
        public int Score;
        public bool Available => GroupId != 0 && !string.IsNullOrEmpty(GoalId) && Action != SkirmishPublicPlayerAction.None;
    }

    // Pure policy; no simulation, UI, input, mutable session or diagnostic dependency.
    public static class SkirmishPlayerObjectivePolicyUtilitySystemHelper
    {
        public static SkirmishPlayerObjectiveChoice Choose(SkirmishPublicGoalSnapshot goal,
            IReadOnlyList<SkirmishPublicPlayerGroup> groups,
            IReadOnlyList<SkirmishPublicPlayerApproach> approaches,
            in SkirmishPlayerObjectiveChoice current)
        {
            if (goal == null || groups == null || approaches == null || goal.Outcome != SkirmishOutcomeKind.None)
                return default;
            if (goal.Perspective != SkirmishPublicGoalPerspective.Capture &&
                goal.Perspective != SkirmishPublicGoalPerspective.Evacuate &&
                goal.Perspective != SkirmishPublicGoalPerspective.Escort) return default;
            var best = default(SkirmishPlayerObjectiveChoice);
            var retained = default(SkirmishPlayerObjectiveChoice);
            var captureAssignments = new HashSet<string>(StringComparer.Ordinal);
            if (goal.Perspective == SkirmishPublicGoalPerspective.Capture)
                for (int i=0;i<goal.NodeCount;i++)
                {
                    var zone=goal.Node(i);
                    if(zone.Kind!=SkirmishPublicGoalNodeKind.CaptureZone)continue;
                    if(zone.OwnerKnown&&zone.OwnerFaction==goal.ObserverFaction)captureAssignments.Add(zone.PublicId);
                    foreach(var unit in groups)
                        if(ValidGroup(unit)&&unit.AcceptedOrderActive&&unit.AcceptedGoalId==zone.PublicId&&unit.CombatInfantry&&unit.Dismounted)
                            captureAssignments.Add(zone.PublicId);
                }
            foreach (var group in groups)
            {
                if (!ValidGroup(group)) continue;
                for (int i = 0; i < goal.NodeCount; i++)
                {
                    var node = goal.Node(i);
                    if(goal.Perspective==SkirmishPublicGoalPerspective.Capture&&node.Kind==SkirmishPublicGoalNodeKind.CaptureZone&&!node.Contested)
                    {
                        bool anotherCommitted=false;
                        foreach(var unit in groups)
                            if(ValidGroup(unit)&&unit.GroupId!=group.GroupId&&unit.AcceptedOrderActive&&unit.AcceptedGoalId==node.PublicId&&unit.CombatInfantry&&unit.Dismounted)
                                anotherCommitted=true;
                        if(anotherCommitted&&node.OwnerFaction!=goal.ObserverFaction)continue;
                        if(captureAssignments.Count>=2&&!captureAssignments.Contains(node.PublicId))continue;
                    }
                    var choice = Candidate(goal, group, node, approaches);
                    if (!choice.Available) continue;
                    if (Same(choice, current)) retained = choice;
                    if (!best.Available || choice.Score > best.Score ||
                        choice.Score == best.Score && Compare(choice, best) < 0) best = choice;
                }
            }
            // Retention uses a freshly legal candidate, never stale coordinates or score.
            if (retained.Available && best.Available && !SkirmishObjectivePolicyUtilitySystemHelper.ShouldReplaceAssignment(retained.Score, best.Score, true, false)) return retained;
            return best;
        }

        static SkirmishPlayerObjectiveChoice Candidate(SkirmishPublicGoalSnapshot goal,
            SkirmishPublicPlayerGroup group, SkirmishPublicGoalNode node,
            IReadOnlyList<SkirmishPublicPlayerApproach> approaches)
        {
            if (!node.PositionKnown) return default;
            bool obsoleteCorridor = false;
            if (goal.Perspective == SkirmishPublicGoalPerspective.Evacuate && goal.Score.ExitStateKnown && goal.Score.ExitOpen)
                for (int i=0;i<goal.NodeCount;i++)
                    if(goal.Node(i).PublicId==group.AcceptedGoalId&&goal.Node(i).Kind==SkirmishPublicGoalNodeKind.ExitCorridor)obsoleteCorridor=true;
            if (group.AcceptedOrderActive && !obsoleteCorridor && !group.PublicTerminalRisk && group.AcceptedGoalId != node.PublicId) return default;
            var route = Approach(approaches, group.GroupId, node.PublicId);
            bool inside = route.HasValue && route.Value.Inspected && route.Value.InsideGoalFootprint;
            var action = SkirmishPublicPlayerAction.Move;
            var reason = SkirmishPlayerObjectiveReason.None;
            bool objectiveGain = true, preserve = false;
            if (goal.Perspective == SkirmishPublicGoalPerspective.Capture)
            {
                if (node.Kind != SkirmishPublicGoalNodeKind.CaptureZone || !node.OwnerKnown ||
                    !group.CombatInfantry || !group.Dismounted) return default;
                int owned = 0;
                for (int i = 0; i < goal.NodeCount; i++)
                    if (goal.Node(i).Kind == SkirmishPublicGoalNodeKind.CaptureZone &&
                        goal.Node(i).OwnerKnown && goal.Node(i).OwnerFaction == goal.ObserverFaction) owned++;
                bool ours = node.OwnerFaction == goal.ObserverFaction;
                // A contested owned zone continues counting toward the majority.
                // Keep local defenders; do not collapse all groups onto a captured point.
                bool local = inside;
                if (ours && !node.Contested && !local) return default;
                if (ours) { objectiveGain = node.Contested || owned >= 2; reason = SkirmishPlayerObjectiveReason.SustainMajority; }
                else { objectiveGain = owned < 2; reason = node.OwnerFaction == 0 ? SkirmishPlayerObjectiveReason.ContestMajority : SkirmishPlayerObjectiveReason.NeutralizeThenCapture; }
                // Never treat neutralization progress as ownership or completion.
                if (local && !node.Contested && group.CanHold) action = SkirmishPublicPlayerAction.Hold;

            }
            else if (goal.Perspective == SkirmishPublicGoalPerspective.Evacuate)
            {
                bool originals = HasLivingOriginal(goal, group, SkirmishPublicSubjectKind.DesignatedRifle);
                if (!goal.Score.ExitStateKnown) return default;
                if (goal.Score.ExitOpen)
                {
                    if (node.Kind != SkirmishPublicGoalNodeKind.EvacuationExit || !originals) return default;
                    preserve = true; reason = SkirmishPlayerObjectiveReason.EvacuateOriginals;
                    if (!group.Dismounted)
                    {
                        if (!group.CanUnload) return default;
                        action = SkirmishPublicPlayerAction.Unload; reason = SkirmishPlayerObjectiveReason.UnloadOriginals;
                    }
                    else if (inside && group.CanHold) action = SkirmishPublicPlayerAction.Hold;
                }
                else
                {
                    if (node.Kind != SkirmishPublicGoalNodeKind.ExitCorridor || !group.CombatInfantry || !group.Dismounted) return default;
                    reason = SkirmishPlayerObjectiveReason.OpenCorridor;
                    // Support infantry opens the corridor while originals are preserved.

                    if (inside && group.CanHold) action = SkirmishPublicPlayerAction.Hold;
                }
            }
            else
            {
                bool truck = HasLivingOriginal(goal, group, SkirmishPublicSubjectKind.ObjectiveConvoyTruck);
                bool escort = !truck && group.CombatInfantry && node.Kind == SkirmishPublicGoalNodeKind.OwnDesignatedSubject &&
                    node.RevealedSubjectKind == SkirmishPublicSubjectKind.ObjectiveConvoyTruck &&
                    node.SubjectDisposition == SkirmishPublicSubjectDisposition.Live && node.HealthKnown && node.Health > 0;
                if (!(truck && node.Kind == SkirmishPublicGoalNodeKind.DeliveryDestination) && !escort) return default;
                preserve = true; reason = SkirmishPlayerObjectiveReason.EscortOriginalTruck;
                if (inside && group.CanHold) { action = SkirmishPublicPlayerAction.Hold; reason = SkirmishPlayerObjectiveReason.ProtectDwell; }
            }
            if (action == SkirmishPublicPlayerAction.Move && (!group.CanMove || group.AcceptedOrderActive && group.AcceptedGoalId == node.PublicId)) return default;
            // Hold needs no navigation claim. Unload still requires inspected legal ground.
            if (action != SkirmishPublicPlayerAction.Hold &&
                (!route.HasValue || !route.Value.Inspected || !route.Value.GroundCompatible || route.Value.CurrentlyObstructed))
                return new SkirmishPlayerObjectiveChoice { GroupId = group.GroupId, GoalId = node.PublicId,
                    Action = SkirmishPublicPlayerAction.Scout, Reason = SkirmishPlayerObjectiveReason.InspectUnknownRoute, Score = 0 };
            var candidate = new SkirmishObjectivePolicyCandidate { Legal = true, Affordable = true,
                ObjectiveGain = objectiveGain, PreventTerminalLoss = group.PublicTerminalRisk,
                PreserveMandatorySubject = preserve, ObservedCounterAdvantage = group.ObservedCounterAdvantage,
                TravelSeconds = route.HasValue ? route.Value.TravelSeconds : 0,
                KnownExposurePenalty = route.HasValue ? (int)Math.Min(30, route.Value.ObservedExposure) : 0 };
            if (!SkirmishObjectivePolicyUtilitySystemHelper.TryScore(candidate, out int score)) return default;
            return new SkirmishPlayerObjectiveChoice { GroupId = group.GroupId, GoalId = node.PublicId, Action = action,
                Reason = reason, Score = score };
        }

        static bool HasLivingOriginal(SkirmishPublicGoalSnapshot goal, SkirmishPublicPlayerGroup group, SkirmishPublicSubjectKind kind)
        {
            foreach (string id in group.DesignatedSubjects ?? Array.Empty<string>())
                for (int i = 0; i < goal.NodeCount; i++)
                {
                    var node = goal.Node(i);
                    if (node.PublicId == id && node.Kind == SkirmishPublicGoalNodeKind.OwnDesignatedSubject &&
                        node.RevealedSubjectKind == kind && node.SubjectDisposition == SkirmishPublicSubjectDisposition.Live && node.HealthKnown && node.Health > 0) return true;
                }
            return false;
        }
        static SkirmishPublicPlayerApproach? Approach(IReadOnlyList<SkirmishPublicPlayerApproach> values, uint group, string goal)
        {
            SkirmishPublicPlayerApproach? found = null;
            foreach (var row in values)
                if (row.GroupId == group && row.GoalId == goal)
                {
                    if (found.HasValue || !Finite(row.TravelSeconds) || row.TravelSeconds < 0 ||
                        !Finite(row.ObservedExposure) || row.ObservedExposure < 0) return null;
                    found = row;
                }
            return found;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool ValidGroup(SkirmishPublicPlayerGroup group) => group != null && group.GroupId != 0 && group.Own && group.Living &&
            (!group.PositionKnown || Finite(group.WorldX) && Finite(group.WorldZ));
        public static bool Same(in SkirmishPlayerObjectiveChoice a, in SkirmishPlayerObjectiveChoice b) =>
            a.GroupId == b.GroupId && a.GoalId == b.GoalId && a.Action == b.Action;
        static int Compare(in SkirmishPlayerObjectiveChoice a, in SkirmishPlayerObjectiveChoice b)
        { int group = a.GroupId.CompareTo(b.GroupId); return group != 0 ? group : string.CompareOrdinal(a.GoalId, b.GoalId); }
    }
}
