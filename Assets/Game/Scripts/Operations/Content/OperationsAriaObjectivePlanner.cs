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
    /// Plans the next visible-control intents from public HUD/objective/actor state.
    /// Does not inject facts, read hidden fog, or switch on mission IDs.
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
            OperationsTacticalFact[] facts = loop.CopyPublicFacts();
            OperationsTacticalActorState[] actors = loop.CopyPublicActors();

            if (hud.CameraFocus.Length > 0)
                intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Focus, string.Empty, hud.CameraFocus, string.Empty, "operations.aria.focus"));

            for (int index = 0; index < hud.Required.Length; index++)
            {
                OperationsHudObjective row = hud.Required[index];
                if (row.Complete || row.Failed)
                    continue;
                if (!loop.TryNode(row.NodeId, out OperationsTacticalNodeState state))
                    continue;
                if (state.Phase != OperationsTacticalNodePhase.Active)
                    continue;
                AppendForNode(loop, state, facts, actors, intents);
            }

            return intents.ToArray();
        }

        static void AppendForNode(
            OperationsLoopSession loop,
            OperationsTacticalNodeState state,
            OperationsTacticalFact[] facts,
            OperationsTacticalActorState[] actors,
            List<OperationsAriaIntent> intents)
        {
            switch (state.Rule)
            {
                case OperationsObjectiveRuleKind.Scan:
                    string scanTarget = FirstUnconfirmed(state.TargetIds, facts, OperationsTacticalFactKind.ScanConfirmed);
                    if (scanTarget.Length == 0)
                        break;
                    string scout = PreferRole(actors, OperationsRosterRoleKind.ReconInfantry) ?? FirstCommandable(actors);
                    if (!AtMoveAnchor(loop, scout, scanTarget))
                    {
                        AppendApproach(loop, scout, scanTarget, intents);
                        break;
                    }

                    if (!HasFact(facts, OperationsTacticalFactKind.Observed, scanTarget))
                    {
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Observe, scout, scanTarget, string.Empty, "operations.aria.scan"));
                        break;
                    }

                    intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Scan, scout, scanTarget, string.Empty, "operations.aria.scan"));
                    break;
                case OperationsObjectiveRuleKind.Interact:
                    string interactTarget = FirstTarget(state.TargetIds);
                    string interactor = FirstCommandable(actors);
                    if (!AtMoveAnchor(loop, interactor, interactTarget))
                        AppendApproach(loop, interactor, interactTarget, intents);
                    else
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Interact, interactor, interactTarget, string.Empty, "operations.aria.interact"));
                    break;
                case OperationsObjectiveRuleKind.Repair:
                    string repairTarget = FirstTarget(state.TargetIds);
                    string repairer = PreferRole(actors, OperationsRosterRoleKind.RepairSpecialist) ?? FirstCommandable(actors);
                    if (!AtMoveAnchor(loop, repairer, repairTarget))
                        AppendApproach(loop, repairer, repairTarget, intents);
                    else
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Repair, repairer, repairTarget, string.Empty, "operations.aria.repair"));
                    break;
                case OperationsObjectiveRuleKind.Hold:
                    string holder = FirstCommandable(actors);
                    string zone = state.ZoneAnchorId;
                    if (zone.Length == 0)
                        break;
                    if (!InRangeAnchor(loop, holder, zone, OperationsTacticalRules.HoldMeters))
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Move, holder, zone, string.Empty, "operations.aria.hold"));
                    else
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Hold, holder, zone, string.Empty, "operations.aria.hold"));
                    break;
                case OperationsObjectiveRuleKind.Escort:
                    string route = FirstRoute(state.LegalRouteIds);
                    if (route.Length > 0)
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.EscortGo, string.Empty, string.Empty, route, "operations.aria.escort_go"));
                    break;
                case OperationsObjectiveRuleKind.Extract:
                    for (int index = 0; index < actors.Length; index++)
                    {
                        if (IsCommandablePlayer(actors[index]) &&
                            actors[index].Body == OperationsTacticalBodyKind.Infantry)
                            intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Extract, actors[index].ObjectId, string.Empty, string.Empty, "operations.aria.extract"));
                    }
                    break;
                case OperationsObjectiveRuleKind.Clear:
                    string hostile = FirstListedHostile(actors, state.TargetIds);
                    string attacker = FirstCommandable(actors);
                    if (hostile.Length == 0)
                        break;
                    if (!AtMoveAnchor(loop, attacker, hostile) &&
                        !InRange(loop, attacker, hostile, OperationsTacticalRules.AttackMeters))
                        AppendApproach(loop, attacker, hostile, intents);
                    else
                        intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Attack, attacker, hostile, string.Empty, "operations.aria.attack"));
                    break;
            }
        }

        static void AppendApproach(OperationsLoopSession loop, string actorId, string focusId, List<OperationsAriaIntent> intents)
        {
            if (string.IsNullOrEmpty(actorId) || string.IsNullOrEmpty(focusId))
                return;
            if (!loop.TryResolveMoveAnchor(focusId, out string anchorId))
                return;
            intents.Add(new OperationsAriaIntent(OperationsAriaSkillKind.Move, actorId, anchorId, string.Empty, "operations.aria.move"));
        }

        static bool AtMoveAnchor(OperationsLoopSession loop, string actorId, string focusId)
        {
            if (!loop.TryResolveMoveAnchor(focusId, out string anchorId))
                return false;
            return InRangeAnchor(loop, actorId, anchorId, 0.75f);
        }

        static bool InRange(OperationsLoopSession loop, string actorId, string targetId, float meters)
        {
            if (!loop.TryActor(actorId, out OperationsTacticalActorState actor) ||
                !loop.TryActor(targetId, out OperationsTacticalActorState target))
                return false;
            return OperationsTacticalRules.Within(actor.X, actor.Z, target.X, target.Z, meters);
        }

        static bool InRangeAnchor(OperationsLoopSession loop, string actorId, string anchorId, float meters)
        {
            if (!loop.TryActor(actorId, out OperationsTacticalActorState actor))
                return false;
            if (!OperationsMapGreyboxCatalog.TryGet(loop.MapId, out OperationsMapGreybox map) ||
                !map.TryGetById(anchorId, out OperationsGreyboxAnchor anchor))
                return false;
            return OperationsTacticalRules.Within(actor.X, actor.Z, anchor.X, anchor.Z, meters);
        }

        static string FirstUnconfirmed(string[] targets, OperationsTacticalFact[] facts, OperationsTacticalFactKind kind)
        {
            if (targets == null)
                return string.Empty;
            for (int index = 0; index < targets.Length; index++)
            {
                if (!HasFact(facts, kind, targets[index]))
                    return targets[index];
            }

            return string.Empty;
        }

        static string FirstTarget(string[] targets) =>
            targets != null && targets.Length > 0 ? targets[0] : string.Empty;

        static string FirstRoute(string[] routes)
        {
            if (routes == null || routes.Length == 0)
                return string.Empty;
            for (int index = 0; index < routes.Length; index++)
            {
                if (routes[index] == "route.safe")
                    return routes[index];
            }

            return routes[0];
        }

        static string PreferRole(OperationsTacticalActorState[] actors, OperationsRosterRoleKind role)
        {
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].RosterRole == role && IsCommandablePlayer(actors[index]))
                    return actors[index].ObjectId;
            }

            return null;
        }

        static string FirstCommandable(OperationsTacticalActorState[] actors)
        {
            for (int index = 0; index < actors.Length; index++)
            {
                if (IsCommandablePlayer(actors[index]))
                    return actors[index].ObjectId;
            }

            return string.Empty;
        }

        static string FirstListedHostile(OperationsTacticalActorState[] actors, string[] listed)
        {
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                if (actor.Faction != OperationsTacticalFaction.Hostile || !actor.Alive || !actor.Spawned)
                    continue;
                if (listed == null || listed.Length == 0 || Contains(listed, actor.ObjectId) || Contains(listed, actor.RoleId))
                    return actor.ObjectId;
            }

            return string.Empty;
        }

        static bool IsCommandablePlayer(OperationsTacticalActorState actor) =>
            actor.Faction == OperationsTacticalFaction.Player &&
            actor.Alive &&
            actor.Spawned &&
            actor.Commandable &&
            (actor.Body == OperationsTacticalBodyKind.Infantry ||
             actor.RosterRole == OperationsRosterRoleKind.RepairSpecialist);

        static bool HasFact(OperationsTacticalFact[] facts, OperationsTacticalFactKind kind, string objectId)
        {
            for (int index = 0; index < facts.Length; index++)
            {
                if (facts[index].Kind == kind && facts[index].ObjectId == objectId)
                    return true;
            }

            return false;
        }

        static bool Contains(string[] values, string needle)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == needle)
                    return true;
            }

            return false;
        }
    }
}
