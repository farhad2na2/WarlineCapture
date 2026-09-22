using System;
using System.Collections.Generic;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    public enum OperationsAriaSkillKind : byte
    {
        Focus = 0,
        Observe = 1,
        Scan = 2,
        Interact = 3,
        Repair = 4,
        Move = 5,
        Hold = 6,
        EscortGo = 7,
        EscortHold = 8,
        Extract = 9,
        Attack = 10,
        Conclude = 11,
        Withdraw = 12
    }

    public readonly struct OperationsAriaIntent
    {
        public OperationsAriaIntent(OperationsAriaSkillKind skill, string actorId, string targetId, string routeId, string reasonKey)
        {
            Skill = skill;
            ActorId = actorId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            ReasonKey = reasonKey ?? string.Empty;
        }

        public OperationsAriaSkillKind Skill { get; }
        public string ActorId { get; }
        public string TargetId { get; }
        public string RouteId { get; }
        public string ReasonKey { get; }
    }

    /// <summary>
    /// Plans the next visible-control intents from public HUD/objective state.
    /// Does not inject facts or read hidden enemy positions.
    /// </summary>
    public static class OperationsAriaObjectivePlanner
    {
        public static OperationsAriaIntent[] Plan(OperationsLoopSession loop)
        {
            if (loop == null)
                throw new ArgumentNullException(nameof(loop));
            if (!loop.TryReadHud(out OperationsHudFrame hud) || !loop.HasMission)
                return Array.Empty<OperationsAriaIntent>();

            var intents = new List<OperationsAriaIntent>();
            if (hud.CameraFocus.Length > 0)
                intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Focus, string.Empty, hud.CameraFocus, string.Empty, "operations.aria.focus"));

            for (int index = 0; index < hud.Required.Length; index++)
            {
                OperationsHudObjective row = hud.Required[index];
                if (row.Complete || row.Failed)
                    continue;
                if (!loop.TryNode(row.NodeId, out OperationsTacticalNodeState state))
                    continue;
                AppendForNode(loop, state, intents);
            }

            if (hud.WithdrawAvailable)
                intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Withdraw, string.Empty, string.Empty, string.Empty, "operations.aria.withdraw"));
            return intents.ToArray();
        }

        static void AppendForNode(OperationsLoopSession loop, OperationsTacticalNodeState state, List<OperationsAriaIntent> intents)
        {
            switch (state.Rule)
            {
                case OperationsObjectiveRuleKind.Scan:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Observe, FirstPlayer(loop), FirstNodeTarget(loop, state.NodeId), string.Empty, "operations.aria.scan"));
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Scan, FirstPlayer(loop), FirstNodeTarget(loop, state.NodeId), string.Empty, "operations.aria.scan"));
                    break;
                case OperationsObjectiveRuleKind.Interact:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Interact, FirstPlayer(loop), FirstNodeTarget(loop, state.NodeId), string.Empty, "operations.aria.interact"));
                    break;
                case OperationsObjectiveRuleKind.Repair:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Repair, FirstRepair(loop), FirstNodeTarget(loop, state.NodeId), string.Empty, "operations.aria.repair"));
                    break;
                case OperationsObjectiveRuleKind.Hold:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Move, FirstPlayer(loop), FirstZone(loop, state.NodeId), string.Empty, "operations.aria.hold"));
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Hold, FirstPlayer(loop), FirstZone(loop, state.NodeId), string.Empty, "operations.aria.hold"));
                    break;
                case OperationsObjectiveRuleKind.Escort:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.EscortGo, string.Empty, string.Empty, "route.safe", "operations.aria.escort_go"));
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.EscortHold, string.Empty, string.Empty, string.Empty, "operations.aria.escort_hold"));
                    break;
                case OperationsObjectiveRuleKind.Extract:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Extract, FirstPlayer(loop), string.Empty, string.Empty, "operations.aria.extract"));
                    break;
                case OperationsObjectiveRuleKind.Clear:
                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Attack, FirstPlayer(loop), FirstHostile(loop), string.Empty, "operations.aria.attack"));
                    break;
            }
        }

        static string FirstPlayer(OperationsLoopSession loop)
        {
            OperationsTacticalActorState[] actors = CopyActors(loop);
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Faction == OperationsTacticalFaction.Player &&
                    actors[index].Body == OperationsTacticalBodyKind.Infantry &&
                    actors[index].Alive &&
                    actors[index].Spawned &&
                    actors[index].Commandable)
                    return actors[index].ObjectId;
            }

            return string.Empty;
        }

        static string FirstRepair(OperationsLoopSession loop)
        {
            OperationsTacticalActorState[] actors = CopyActors(loop);
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].RosterRole == OperationsRosterRoleKind.RepairSpecialist &&
                    actors[index].Alive &&
                    actors[index].Spawned)
                    return actors[index].ObjectId;
            }

            return FirstPlayer(loop);
        }

        static string FirstHostile(OperationsLoopSession loop)
        {
            OperationsTacticalActorState[] actors = CopyActors(loop);
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Faction == OperationsTacticalFaction.Hostile &&
                    actors[index].Alive &&
                    actors[index].Spawned)
                    return actors[index].ObjectId;
            }

            return string.Empty;
        }

        static OperationsTacticalActorState[] CopyActors(OperationsLoopSession loop)
        {
            if (!loop.TryActor("unit.d01.rifle.01", out _))
                return Array.Empty<OperationsTacticalActorState>();
            var list = new List<OperationsTacticalActorState>();
            // Public observation uses TryActor for known IDs exposed by HUD targets.
            string[] probes =
            {
                "unit.d01.rifle.01", "unit.d01.rifle.02", "unit.d01.rifle.03", "unit.d01.rifle.04",
                "unit.d01.recon.01", "unit.d01.recon.02", "unit.d01.repair.01", "unit.d01.repair.02",
                "hostile.d01.pump.01", "hostile.d01.pump.02", "hostile.d01.rifle.i.01",
                "hostile.d01.rifle.a.01", "hostile.d01.rifle.b.01"
            };
            for (int index = 0; index < probes.Length; index++)
            {
                if (loop.TryActor(probes[index], out OperationsTacticalActorState state))
                    list.Add(state);
            }

            return list.ToArray();
        }

        static string FirstNodeTarget(OperationsLoopSession loop, string nodeId)
        {
            if (!loop.TryReadHud(out OperationsHudFrame hud))
                return string.Empty;
            return hud.CameraFocus;
        }

        static string FirstZone(OperationsLoopSession loop, string nodeId) => FirstNodeTarget(loop, nodeId);
    }
}
