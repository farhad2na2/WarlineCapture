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
    public enum AriaObjectiveBuy : byte { None, Recruit, Waiting }

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
        // Gate 3 on FieldBase economies (450 start, cap 800, about 0.5 materials/s in lane 25 S007): coverage,
        // research and producer buildings keep one rifle squad of materials (the projection's objective masks use
        // the same value), and once coverage has started ordinary rifles only refill to this floor.
        public const int CoverageReserve = 80, CoverageInfantryFloor = 16;
        public const float OneTimeBuySeconds = 10f;

        // Rifle top-up target: the type's strength until coverage starts, then the floor (coverage infantry,
        // gunner/marksman/breacher, fills the rest, and the materials go to the other offered types).
        public static int RifleTarget(AriaObjectiveType type, bool coverageStarted)
        {
            int target = InfantryTarget(type);
            return coverageStarted && target > CoverageInfantryFloor ? CoverageInfantryFloor : target;
        }

        // One purchase of a kind the coverage policy does not list (the rocketeer), through Build and its card.
        // stage: 0 not started, 1 buying, 2 done (queued, fielded, given up or interrupted). Bounded.
        public static AriaObjectiveBuy OneTimeBuy(ref byte stage, ref float deadline, bool coverageStarted, bool busy,
            bool canAfford, bool pending, bool cardAvailable, float now)
        {
            if (stage == 2) return AriaObjectiveBuy.None;
            if (pending) { stage = 2; return AriaObjectiveBuy.None; }
            if (stage == 1)
            {
                if (now > deadline || busy) { stage = 2; return AriaObjectiveBuy.None; }
                return cardAvailable ? AriaObjectiveBuy.Recruit : AriaObjectiveBuy.Waiting;
            }
            if (!coverageStarted || busy || !canAfford || !cardAvailable) return AriaObjectiveBuy.None;
            stage = 1; deadline = now + OneTimeBuySeconds;
            return AriaObjectiveBuy.Recruit;
        }

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
        // W32: the group the objective chooser currently orders; an escort never re-tasks it.
        public bool Chosen;
        // W16 attempt 2 (garrison offers only): Holding = its accepted order is active at another zone (it holds
        // that zone); Committed = its accepted order goes to the committed zone. Holders are never sent.
        public bool Holding, Committed;
    }

    // W16 attempt 2: the FC garrison zone stays chosen until it is lost or a group arrives and holds it, instead of
    // being recomputed (and flipped back to the committed zone) whenever the enemy wave contests the committed zone.
    public struct AriaGarrisonState
    {
        public string Zone, Committed;
        public uint Group;   // the group offered the garrison Move (0 = none yet, or its offer expired)
        public bool Ordered; // that group's Move was accepted: other escorts return to the committed zone
    }

    public static class AriaObjectiveEscortPolicy
    {
        public const float OfferSeconds = 45f;
        public static bool OfferExpired(float attemptSeconds) => attemptSeconds > OfferSeconds;

        // Ground vehicles only. Radar keeps its support order; objective originals (BT riflemen, CE trucks)
        // and automatic logistics are never re-tasked by an escort order.
        public static bool Eligible(in AriaEscortCandidate c) => Eligible(c, AriaObjectiveType.None);

        // CE: ordinary combat infantry also escorts by moving to the destination ahead of the trucks (the chooser
        // no longer orders infantry onto a moving truck, whose receipt can never cover a whole squad's move goals).
        // W32 (gate 3, every starting role used): on FC/BT/CE combat infantry the objective chooser did not pick, and
        // the transport helicopter, also take an ordinary Move to the objective goal. The chosen group is never re-tasked.
        public static bool Eligible(in AriaEscortCandidate c, AriaObjectiveType type) =>
            c.GroupId != 0 && c.CanMove && !c.Originals && !c.Chosen && c.Role != SkirmishRoleKind.Radar &&
            (c.Domain == SkirmishPopulationCategory.Ground ||
             type != AriaObjectiveType.None && c.Domain == SkirmishPopulationCategory.Infantry && CombatInfantryRole(c.Role) ||
             type != AriaObjectiveType.None && c.Role == SkirmishRoleKind.TransportHeli);

        public static bool CombatInfantryRole(SkirmishRoleKind role) =>
            role == SkirmishRoleKind.Rifle || role == SkirmishRoleKind.Gunner || role == SkirmishRoleKind.Marksman ||
            role == SkirmishRoleKind.Breacher || role == SkirmishRoleKind.Rocketeer;

        public static int Pick(IReadOnlyList<AriaEscortCandidate> candidates) => Pick(candidates, AriaObjectiveType.None);

        public static int Pick(IReadOnlyList<AriaEscortCandidate> candidates, AriaObjectiveType type) => Pick(candidates, type, false);

        // Garrison: never a group holding another zone; a group not already sent to the committed zone goes first.
        public static int Pick(IReadOnlyList<AriaEscortCandidate> candidates, AriaObjectiveType type, bool garrison)
        {
            int best = -1;
            if (candidates == null) return best;
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (!Eligible(c, type) || c.Done || c.Skipped || !c.Target || garrison && c.Holding) continue;
                if (best < 0) { best = i; continue; }
                var b = candidates[best];
                if (garrison && b.Committed != c.Committed) { if (!c.Committed) best = i; continue; }
                if (c.GroupId < b.GroupId) best = i;
            }
            return best;
        }

        // The garrison offer went to this group (the helper calls this after Pick, for the garrison goal only).
        public static void BindGarrison(ref AriaGarrisonState state, uint group) { if (state.Group == 0) state.Group = group; }

        // FC: the zone own combat infantry is committed to (else the chosen or first unowned zone).
        // BT: the corridor own infantry is opening; the exit once it is open. CE: the delivery destination.
        public static string Goal(SkirmishPublicGoalSnapshot goal, IReadOnlyList<SkirmishPublicPlayerGroup> groups, string chosen)
        {
            var garrison = default(AriaGarrisonState);
            return Goal(goal, groups, chosen, ref garrison);
        }

        public static string Goal(SkirmishPublicGoalSnapshot goal, IReadOnlyList<SkirmishPublicPlayerGroup> groups, string chosen,
            ref AriaGarrisonState garrison)
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
            if (type != AriaObjectiveType.FrontlineControl) garrison = default;
            else if (committed != null)
            {
                string thin = Garrison(goal, groups, committed, ref garrison);
                if (thin != null) return thin;
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

        // W16 (DB FC S010/S012 @130365): with two zones owned, every escort followed the infantry holding the first
        // zone, the second owned zone had no committed group, and the enemy's single wave took it unopposed at ~141 s
        // (then held two zones for 500 s). While the committed zone is ours and uncontested, escorts garrison the
        // other owned, uncontested zone that has fewer own groups (any domain) moving to or holding it.
        // Attempt 1 failed (W16 proof): the choice was recomputed every frame and fell back to the committed zone
        // whenever the wave contested it, so the garrison offer was re-issued every 11-22 s and never arrived.
        // Attempt 2: once chosen, the zone stays chosen until it is lost or an own group arrives and holds it; after
        // the garrisoning group's Move is accepted, other escorts return to the committed zone (null here).
        static string Garrison(SkirmishPublicGoalSnapshot goal, IReadOnlyList<SkirmishPublicPlayerGroup> groups, string committed,
            ref AriaGarrisonState state)
        {
            state.Committed = committed;
            if (state.Zone != null)
            {
                bool owned = false;
                for (int i = 0; i < goal.NodeCount; i++)
                {
                    var node = goal.Node(i);
                    if (node.PublicId == state.Zone)
                        owned = node.Kind == SkirmishPublicGoalNodeKind.CaptureZone && node.OwnerKnown && node.OwnerFaction == goal.ObserverFaction;
                }
                if (!owned || Groups(groups, state.Zone) > 0) { state = new AriaGarrisonState { Committed = committed }; return null; }
                // The garrisoning group died before arriving: offer the zone again.
                if (state.Group != 0 && !Alive(groups, state.Group)) { state.Group = 0; state.Ordered = false; }
                return state.Ordered ? null : state.Zone;
            }
            bool committedHeld = false;
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.PublicId == committed)
                    committedHeld = node.Kind == SkirmishPublicGoalNodeKind.CaptureZone && node.OwnerKnown &&
                        node.OwnerFaction == goal.ObserverFaction && !node.Contested;
            }
            if (!committedHeld) return null;
            string thin = null; int fewest = Groups(groups, committed);
            for (int i = 0; i < goal.NodeCount; i++)
            {
                var node = goal.Node(i);
                if (node.Kind != SkirmishPublicGoalNodeKind.CaptureZone || node.PublicId == committed || !node.PositionKnown ||
                    !node.OwnerKnown || node.OwnerFaction != goal.ObserverFaction || node.Contested) continue;
                int count = Groups(groups, node.PublicId);
                if (count < fewest) { fewest = count; thin = node.PublicId; }
            }
            if (thin != null) state = new AriaGarrisonState { Zone = thin, Committed = committed };
            return thin;
        }

        static bool Alive(IReadOnlyList<SkirmishPublicPlayerGroup> groups, uint group)
        {
            if (groups != null)
                foreach (var g in groups)
                    if (g != null && g.GroupId == group && g.Own && g.Living) return true;
            return false;
        }

        static int Groups(IReadOnlyList<SkirmishPublicPlayerGroup> groups, string zone)
        {
            int count = 0;
            if (groups != null)
                foreach (var group in groups)
                    if (group != null && group.Own && group.Living && group.AcceptedOrderActive &&
                        string.Equals(group.AcceptedGoalId, zone, StringComparison.Ordinal)) count++;
            return count;
        }

        static string First(SkirmishPublicGoalSnapshot goal, SkirmishPublicGoalNodeKind kind)
        {
            for (int i = 0; i < goal.NodeCount; i++)
                if (goal.Node(i).Kind == kind && goal.Node(i).PositionKnown) return goal.Node(i).PublicId;
            return null;
        }
    }
}
