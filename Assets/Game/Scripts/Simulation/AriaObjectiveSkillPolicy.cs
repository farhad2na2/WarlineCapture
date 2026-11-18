using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Simulation
{
    // FC/BT/CE objective skill decisions (AriaSkirmishPlanSystem.ObjectiveSkill). Pure: no UI, Entity,
    // input or hidden simulation state. Every input is something the player's HUD presents.
    public enum AriaObjectiveType : byte { None, FrontlineControl, Breakthrough, ConvoyEscort }

    // What the skill does with the frame's rendered objective presentation.
    // Proceed: the frame is presented. Popup: ARIA's own drawer/placement hides the goal HUD; finish or
    // close it. ClosePopup: that popup stayed open too long; close it. Wait: the frame is missing for
    // another reason; never act on a stale objective. Block: missing for longer than the grace.
    public enum AriaObjectiveFrameAction : byte { Proceed, Popup, ClosePopup, Wait, Block }

    public struct AriaObjectiveFrameState
    {
        public float MissingSince, PopupSince;
        public byte Missing, PopupOpen;
    }

    // Bounded retries of one ordinary order sequence (select, focus, Move, ground tap, accepted receipt).
    public struct AriaObjectiveOrderState
    {
        public int Failures, Accepted;
        public float RetryAt;
    }

    public struct AriaObjectiveProgressState
    {
        public byte Known;
        public uint Revision;
        public float LastAt;
    }

    public static class AriaObjectiveSkillPolicy
    {
        // The goal HUD is rebuilt every rendered frame; one-frame gaps (shell settle, consent, camera) are normal.
        public const float FrameGraceSeconds = 20f;
        // A purchase/placement started by this skill hides the goal HUD; it never stays open longer than this.
        public const float PopupLimitSeconds = 60f, PopupCloseSeconds = 20f;
        public const float OrderRetrySeconds = 8f;
        public const int MaxOrderFailures = 6, MaxEscortFailures = 10;

        // SkirmishPublicGoalPerspective: Capture (FC), Evacuate (BT), Escort (CE).
        public static AriaObjectiveType TypeOf(SkirmishPublicGoalPerspective perspective) => perspective switch
        {
            SkirmishPublicGoalPerspective.Capture => AriaObjectiveType.FrontlineControl,
            SkirmishPublicGoalPerspective.Evacuate => AriaObjectiveType.Breakthrough,
            SkirmishPublicGoalPerspective.Escort => AriaObjectiveType.ConvoyEscort,
            _ => AriaObjectiveType.None
        };

        public static AriaObjectiveFrameAction Frame(bool presented, bool ownPopup, float now, ref AriaObjectiveFrameState state)
        {
            if (presented) { state.Missing = 0; state.PopupOpen = 0; return AriaObjectiveFrameAction.Proceed; }
            if (ownPopup)
            {
                state.Missing = 0;
                if (state.PopupOpen == 0) { state.PopupOpen = 1; state.PopupSince = now; }
                float open = now - state.PopupSince;
                return open > PopupLimitSeconds + PopupCloseSeconds ? AriaObjectiveFrameAction.Block :
                    open > PopupLimitSeconds ? AriaObjectiveFrameAction.ClosePopup : AriaObjectiveFrameAction.Popup;
            }
            state.PopupOpen = 0;
            if (state.Missing == 0) { state.Missing = 1; state.MissingSince = now; }
            return now - state.MissingSince > FrameGraceSeconds ? AriaObjectiveFrameAction.Block : AriaObjectiveFrameAction.Wait;
        }

        // A failed order sequence pauses that order and lets production/escort act; true while retries remain.
        public static bool AbsorbFailure(ref AriaObjectiveOrderState state, float now, int maximum)
        {
            state.Failures++;
            state.RetryAt = now + OrderRetrySeconds;
            return state.Failures < maximum;
        }

        public static bool Ready(in AriaObjectiveOrderState state, float now) => now >= state.RetryAt;

        // An accepted order earns back one failure, so a long match is not ended by scattered hiccups.
        public static void Accepted(ref AriaObjectiveOrderState state)
        {
            state.Accepted++;
            if (state.Failures > 0) state.Failures--;
        }

        // Public objective progress: tickets, zone owners/capture, corridor work, travel toward an accepted
        // goal, deliveries, losses. A changed displayed revision renews the input watchdogs.
        public static bool Progress(bool known, uint revision, float now, ref AriaObjectiveProgressState state)
        {
            if (!known) return false;
            if (state.Known == 0) { state.Known = 1; state.Revision = revision; state.LastAt = now; return true; }
            if (revision == state.Revision) return false;
            state.Revision = revision; state.LastAt = now;
            return true;
        }

        // BT: the twelve designated riflemen must keep eight alive for the exit. Until the exit opens they stay
        // home while no ordinary combat infantry exists to open a corridor, for at most this long.
        public const float OriginalHoldSeconds = 90f;

        public static bool HoldBackOriginals(AriaObjectiveType type, bool exitOpen, float simulationSeconds,
            bool chosenHasOriginals, bool supportInfantryLiving) =>
            type == AriaObjectiveType.Breakthrough && !exitOpen && chosenHasOriginals && !supportInfantryLiving &&
            simulationSeconds >= 0 && simulationSeconds < OriginalHoldSeconds;

        // Ordinary rifles kept on the field. FC holds two zones against waves; BT needs support infantry to
        // open a corridor without spending the designated originals; CE needs an escort screen.
        public static int InfantryTarget(AriaObjectiveType type) => type switch
        {
            AriaObjectiveType.FrontlineControl => 24,
            AriaObjectiveType.Breakthrough => 20,
            AriaObjectiveType.ConvoyEscort => 16,
            _ => 0
        };
    }

    // Own vehicle groups follow the infantry to the current objective with one ordinary Move each,
    // so every fielded vehicle type fights for it (gate 3) and the route ahead is screened.
    public struct AriaEscortCandidate
    {
        public uint GroupId;
        public SkirmishRoleKind Role;
        public SkirmishPopulationCategory Domain;
        public bool CanMove, Originals, Done, Skipped, Target;
    }

    public static class AriaObjectiveEscortPolicy
    {
        public const float OfferSeconds = 45f;

        // Ground vehicles only. Radar keeps its support order; objective originals (BT riflemen, CE trucks)
        // and automatic logistics are never re-tasked by an escort order.
        public static bool Eligible(in AriaEscortCandidate c) =>
            c.GroupId != 0 && c.CanMove && !c.Originals && c.Domain == SkirmishPopulationCategory.Ground &&
            c.Role != SkirmishRoleKind.Radar;

        public static int Pick(IReadOnlyList<AriaEscortCandidate> candidates)
        {
            int best = -1;
            if (candidates == null) return best;
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (!Eligible(c) || c.Done || c.Skipped || !c.Target) continue;
                if (best < 0 || c.GroupId < candidates[best].GroupId) best = i;
            }
            return best;
        }

        // FC: the zone own combat infantry is committed to (else the chosen or first unowned zone).
        // BT: the corridor own infantry is opening; the exit once it is open. CE: the delivery destination.
        public static string Goal(SkirmishPublicGoalSnapshot goal, IReadOnlyList<SkirmishPublicPlayerGroup> groups, string chosen)
        {
            if (goal == null || goal.Outcome != SkirmishOutcomeKind.None) return null;
            var type = AriaObjectiveSkillPolicy.TypeOf(goal.Perspective);
            if (type == AriaObjectiveType.ConvoyEscort) return First(goal, SkirmishPublicGoalNodeKind.DeliveryDestination);
            if (type == AriaObjectiveType.Breakthrough && goal.Score.ExitStateKnown && goal.Score.ExitOpen)
                return First(goal, SkirmishPublicGoalNodeKind.EvacuationExit);
            var kind = type == AriaObjectiveType.FrontlineControl ? SkirmishPublicGoalNodeKind.CaptureZone :
                type == AriaObjectiveType.Breakthrough ? SkirmishPublicGoalNodeKind.ExitCorridor : SkirmishPublicGoalNodeKind.None;
            if (kind == SkirmishPublicGoalNodeKind.None) return null;
            string committed = null; int most = 0;
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.Kind != kind) continue;
                int count = 0;
                if (groups != null)
                    foreach (var group in groups)
                        if (group != null && group.Own && group.Living && group.CombatInfantry && group.AcceptedOrderActive &&
                            string.Equals(group.AcceptedGoalId, node.PublicId, StringComparison.Ordinal)) count++;
                // A contested zone needs the armour more than one already committed group does.
                if (node.Contested) count += 2;
                if (count > most) { most = count; committed = node.PublicId; }
            }
            if (committed != null) return committed;
            if (!string.IsNullOrEmpty(chosen))
                for (int i = 0; i < goal.NodeCount; i++)
                    if (goal.Node(i).Kind == kind && goal.Node(i).PublicId == chosen) return chosen;
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.Kind == kind && !(node.OwnerKnown && node.OwnerFaction == goal.ObserverFaction)) return node.PublicId;
            }
            return First(goal, kind);
        }

        static string First(SkirmishPublicGoalSnapshot goal, SkirmishPublicGoalNodeKind kind)
        {
            for (int i = 0; i < goal.NodeCount; i++)
                if (goal.Node(i).Kind == kind && goal.Node(i).PositionKnown) return goal.Node(i).PublicId;
            return null;
        }
    }
}
