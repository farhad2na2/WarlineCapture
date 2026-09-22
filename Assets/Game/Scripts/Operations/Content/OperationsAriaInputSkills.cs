using System;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    /// <summary>
    /// Executes ARIA intents only through the same Loop visible-control APIs as a human player.
    /// </summary>
    public static class OperationsAriaInputSkills
    {
        public static OperationsTacticalCommandResult TryExecute(OperationsLoopSession loop, OperationsAriaIntent intent)
        {
            if (loop == null)
                throw new ArgumentNullException(nameof(loop));
            switch (intent.Skill)
            {
                case OperationsAriaSkillKind.Focus:
                    return OperationsTacticalCommandResult.Ok();
                case OperationsAriaSkillKind.Observe:
                    return loop.Observe(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Scan:
                    return loop.Scan(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Interact:
                    return loop.Interact(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Repair:
                    return loop.Repair(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Move:
                    return loop.Move(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Hold:
                    return loop.Hold(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.EscortGo:
                    return loop.EscortGo(intent.RouteId);
                case OperationsAriaSkillKind.EscortHold:
                    return loop.EscortHold();
                case OperationsAriaSkillKind.Extract:
                    return loop.Extract(intent.ActorId);
                case OperationsAriaSkillKind.Attack:
                    return loop.Attack(intent.ActorId, intent.TargetId);
                case OperationsAriaSkillKind.Conclude:
                    return loop.ConcludeMission();
                case OperationsAriaSkillKind.Withdraw:
                    return loop.WithdrawMission();
                default:
                    return OperationsTacticalCommandResult.Reject(OperationsTacticalRejectKind.PreconditionFailed);
            }
        }

        /// <summary>
        /// Drives a full visible-control win for an authored vertical-slice mission.
        /// Returns false if the terminal outcome is not Victory. This wires the path;
        /// Programmer 2 / QA still record ARIA evidence later.
        /// Host coding checks may use this mission-keyed path; ACCEPTANCE AriaWon
        /// evidence must use <see cref="TryPlayUnassistedWin"/> instead.
        /// </summary>
        public static bool TryPlayVisibleControlWin(OperationsLoopSession loop, string missionId)
        {
            if (missionId == "operation.o001")
                return PlayO001(loop);
            if (missionId == "operation.o002")
                return PlayO002(loop);
            if (missionId == "operation.o003")
                return PlayO003(loop);
            return false;
        }

        /// <summary>
        /// Unassisted planner-driven win through public observation + Loop visible-control APIs.
        /// Does not switch on mission IDs. Suitable as the Operations-owned recordable path
        /// until shipping Watch virtual-touch is seam-approved for Operations.
        /// </summary>
        public static bool TryPlayUnassistedWin(OperationsLoopSession loop, int maxSteps = 1200)
        {
            if (loop == null)
                throw new ArgumentNullException(nameof(loop));
            if (!loop.HasMission)
                return false;

            for (int step = 0; step < maxSteps && !loop.MissionTerminal; step++)
            {
                OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(loop);
                OperationsAriaIntent intent = default;
                bool haveIntent = false;
                for (int index = 0; index < plan.Length; index++)
                {
                    if (plan[index].Skill == OperationsAriaSkillKind.Focus)
                        continue;
                    intent = plan[index];
                    haveIntent = true;
                    break;
                }

                if (!haveIntent)
                {
                    loop.Advance(1);
                    continue;
                }

                if (intent.Skill == OperationsAriaSkillKind.Extract)
                {
                    for (int index = 0; index < plan.Length; index++)
                    {
                        if (plan[index].Skill != OperationsAriaSkillKind.Extract)
                            continue;
                        TryExecute(loop, plan[index]);
                    }

                    loop.Advance(1);
                    continue;
                }

                if (intent.ActorId.Length > 0 &&
                    loop.TryActor(intent.ActorId, out OperationsTacticalActorState actor) &&
                    actor.ChannelTicks > 0 &&
                    (intent.Skill == OperationsAriaSkillKind.Scan ||
                     intent.Skill == OperationsAriaSkillKind.Interact ||
                     intent.Skill == OperationsAriaSkillKind.Repair ||
                     intent.Skill == OperationsAriaSkillKind.Observe))
                {
                    loop.Advance(1);
                    continue;
                }

                OperationsTacticalCommandResult result = TryExecute(loop, intent);
                if (!result.Accepted && intent.Skill == OperationsAriaSkillKind.Move)
                {
                    loop.Advance(1);
                    continue;
                }

                loop.Advance(1);
            }

            return loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory;
        }

        static bool PlayO001(OperationsLoopSession loop)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.signal_a", out OperationsGreyboxAnchor a));
            Require(map.TryGetByAlias("site.signal_b", out OperationsGreyboxAnchor b));
            Require(map.TryGetByAlias("site.signal_c", out OperationsGreyboxAnchor c));
            Require(map.TryGetByAlias("site.evidence", out OperationsGreyboxAnchor evidence));
            ScanSite(loop, "unit.d01.rifle.01", "site.d01.signal_a", a.AnchorId);
            ScanSite(loop, "unit.d01.rifle.01", "site.d01.signal_b", b.AnchorId);
            ScanSite(loop, "unit.d01.rifle.01", "site.d01.signal_c", c.AnchorId);
            Require(NodeComplete(loop, "scan_signals"));
            Require(loop.Move("unit.d01.rifle.02", evidence.AnchorId));
            for (int step = 0; step < 6; step++)
                loop.Advance(1);
            Require(loop.Interact("unit.d01.rifle.02", "site.d01.relay_evidence"));
            for (int step = 0; step < 30 && !NodeComplete(loop, "interact_relay"); step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "interact_relay"));
            // Activation of extract is next-tick; advance once so the node is active.
            loop.Advance(1);
            Require(loop.Extract("unit.d01.rifle.01"));
            Require(loop.Extract("unit.d01.rifle.02"));
            for (int step = 0; step < 40 && !loop.MissionTerminal; step++)
                loop.Advance(1);
            return loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory;
        }

        static void ScanSite(OperationsLoopSession loop, string unitId, string siteId, string anchorId)
        {
            Require(loop.Move(unitId, anchorId));
            for (int step = 0; step < 8; step++)
                loop.Advance(1);
            Require(loop.Observe(unitId, siteId));
            Require(loop.Scan(unitId, siteId));
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
        }

        static bool PlayO002(OperationsLoopSession loop)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.junction", out OperationsGreyboxAnchor junction));
            Require(map.TryGetByAlias("site.clinic", out OperationsGreyboxAnchor clinic));
            ScanSite(loop, "unit.d01.recon.01", "site.d01.junction", junction.AnchorId);
            Require(NodeComplete(loop, "scan_junction"));
            loop.Advance(1);
            Require(loop.EscortGo("route.safe"));
            Require(loop.Move("unit.d01.rifle.01", clinic.AnchorId));
            bool holding = false;
            for (int step = 0; step < 250 && !loop.MissionTerminal; step++)
            {
                loop.Advance(1);
                if (!holding &&
                    loop.TryActor("unit.d01.rifle.01", out OperationsTacticalActorState rifle) &&
                    OperationsTacticalRules.Within(rifle.X, rifle.Z, clinic.X, clinic.Z, OperationsTacticalRules.HoldMeters) &&
                    NodeComplete(loop, "escort_trucks"))
                {
                    loop.Advance(1);
                    Require(loop.Hold("unit.d01.rifle.01", clinic.AnchorId));
                    holding = true;
                }
            }

            return loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory;
        }

        static bool PlayO003(OperationsLoopSession loop)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.pump_west", out OperationsGreyboxAnchor west));
            Require(map.TryGetByAlias("site.pump_east", out OperationsGreyboxAnchor east));
            Require(map.TryGetByAlias("site.service_court", out OperationsGreyboxAnchor court));
            Require(loop.Move("unit.d01.rifle.01", west.AnchorId));
            Require(loop.Move("unit.d01.rifle.02", east.AnchorId));
            Require(loop.Move("unit.d01.repair.01", west.AnchorId));
            Require(loop.Move("unit.d01.repair.02", east.AnchorId));
            Require(loop.Move("unit.d01.rifle.03", court.AnchorId));
            for (int step = 0; step < 20; step++)
                loop.Advance(1);
            Require(loop.Attack("unit.d01.rifle.01", "hostile.d01.pump.01"));
            Require(loop.Attack("unit.d01.rifle.02", "hostile.d01.pump.02"));
            loop.Advance(1);
            for (int step = 0; step < 20 && !NodeComplete(loop, "clear_pump_guards"); step++)
                loop.Advance(1);
            Require(NodeComplete(loop, "clear_pump_guards"));
            loop.Advance(1);
            Require(loop.Repair("unit.d01.repair.01", "site.d01.pump_west"));
            Require(loop.Repair("unit.d01.repair.02", "site.d01.pump_east"));
            bool holding = false;
            for (int step = 0; step < 250 && !loop.MissionTerminal; step++)
            {
                loop.Advance(1);
                if (!holding &&
                    NodeComplete(loop, "repair_pump_west") &&
                    NodeComplete(loop, "repair_pump_east"))
                {
                    loop.Advance(1);
                    if (loop.TryActor("unit.d01.rifle.03", out OperationsTacticalActorState rifle) &&
                        OperationsTacticalRules.Within(rifle.X, rifle.Z, court.X, court.Z, OperationsTacticalRules.HoldMeters))
                    {
                        Require(loop.Hold("unit.d01.rifle.03", court.AnchorId));
                        holding = true;
                    }
                }
            }

            return loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory;
        }

        static bool NodeComplete(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
            state.Phase == OperationsTacticalNodePhase.Complete;

        static void Require(OperationsTacticalCommandResult result)
        {
            if (!result.Accepted)
                throw new InvalidOperationException("aria_reject:" + result.Reason);
        }

        static void Require(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("aria_precondition");
        }
    }
}
