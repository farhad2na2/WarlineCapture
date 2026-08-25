using Game.Components;
using Game.Missions.Contracts;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class CampaignMissionRuntimeProgressUtility
    {
        internal readonly struct MoveTargetContext
        {
            public MoveTargetContext(
                float3 worldPosition,
                float radius,
                bool hasGrid,
                int2 cell,
                int radiusCells)
            {
                WorldPosition = worldPosition;
                Radius = radius;
                HasGrid = hasGrid;
                Cell = cell;
                RadiusCells = radiusCells;
            }

            public float3 WorldPosition { get; }
            public float Radius { get; }
            public bool HasGrid { get; }
            public int2 Cell { get; }
            public int RadiusCells { get; }
        }

        public static MoveTargetContext CreateMoveTargetContext(
            float3 worldPosition,
            float radius,
            bool hasGrid,
            in GridConfig grid)
        {
            bool validGrid = hasGrid && grid.CellSize > 0f;
            return new MoveTargetContext(
                worldPosition,
                radius,
                validGrid,
                validGrid ? GridUtils.WorldToCell(grid, worldPosition) : default,
                validGrid ? math.max(1, (int)math.ceil(radius / grid.CellSize)) : 0);
        }

        public static bool AllAliveFriendliesReachedMoveTarget(
            int aliveFriendly,
            int friendliesAtTarget) =>
            aliveFriendly > 0 && friendliesAtTarget >= aliveFriendly;

        public static bool IsAtMoveTarget(
            float3 worldPosition,
            bool hasUnitGrid,
            int2 unitCell,
            in MoveTargetContext target) =>
            IsAtMoveTarget(
                worldPosition,
                hasUnitGrid,
                unitCell,
                target.WorldPosition,
                target.Radius,
                target.HasGrid,
                target.Cell,
                target.RadiusCells);

        public static bool IsAtMoveTarget(
            float3 worldPosition,
            bool hasUnitGrid,
            int2 unitCell,
            float3 targetWorld,
            float targetRadius,
            bool hasGrid,
            int2 targetCell,
            int targetRadiusCells)
        {
            float2 worldOffset = worldPosition.xz - targetWorld.xz;
            if (math.lengthsq(worldOffset) <= targetRadius * targetRadius)
                return true;
            if (!hasGrid || !hasUnitGrid)
                return false;
            int2 cellOffset = unitCell - targetCell;
            return math.lengthsq(cellOffset) <= targetRadiusCells * targetRadiusCells;
        }

        public static int CountFriendlyUnits(ref CampaignMissionDefinitionBlob definition)
        {
            int count = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                if (group.FactionId > 1)
                    continue;
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                    count += group.Units[unitIndex].Count;
            }
            return count;
        }

        public static bool TryEvaluateSettled(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            out CampaignMissionRuntimeComponent next)
            => TryEvaluateFirstContactSettled(in current, in facts, commandSquadSelected, out next);

        public static bool TryEvaluateSettled(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            in CampaignMissionCatalogComponent catalog,
            out CampaignMissionRuntimeComponent next)
        {
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in current, out int definitionIndex))
                return TryEvaluateFirstContactSettled(in current, in facts, commandSquadSelected, out next);
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            return TryEvaluateSettled(
                in current, in facts, commandSquadSelected, ref definition, out next);
        }

        public static bool TryEvaluateSettled(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            ref CampaignMissionDefinitionBlob definition,
            out CampaignMissionRuntimeComponent next)
        {
            if (!IsEstablishBaseDefinition(ref definition))
                return TryEvaluateFirstContactSettled(in current, in facts, commandSquadSelected, out next);

            if (!TryResolveEstablishBaseTransition(
                    in current,
                    in facts,
                    ref definition,
                    out MissionPhaseKind phase,
                    out MissionOutcomeKind outcome,
                    out MissionReturnDestinationKind destination) ||
                !CampaignMissionRuntimeSystem.TryTransition(
                    in current, phase, outcome, destination, out next))
            {
                next = current;
                return false;
            }

            bool objectivesComplete = AllEstablishBaseObjectivesComplete(in facts, ref definition);
            for (int transition = 0;
                 objectivesComplete && transition < 2 && next.Outcome == MissionOutcomeKind.None &&
                 next.Phase is MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor;
                 transition++)
            {
                if (!TryResolveEstablishBaseTransition(
                        in next,
                        in facts,
                        ref definition,
                        out phase,
                        out outcome,
                        out destination) ||
                    !CampaignMissionRuntimeSystem.TryTransition(
                        in next, phase, outcome, destination, out CampaignMissionRuntimeComponent settled))
                    break;
                next = settled;
            }
            return true;
        }

        private static bool TryEvaluateFirstContactSettled(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            out CampaignMissionRuntimeComponent next)
        {
            if (!CampaignMissionRuntimeSystem.TryEvaluate(
                    in current, in facts, commandSquadSelected, out next))
            {
                return false;
            }

            bool patrolCleared = facts.HostileTotalCount > 0 &&
                                 facts.HostileDefeatedCount >= facts.HostileTotalCount;
            for (int transition = 0;
                 patrolCleared && transition < 2 && next.Outcome == MissionOutcomeKind.None &&
                 (next.Phase is MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor) &&
                 CanFinishFinale(in facts, next.Phase);
                 transition++)
            {
                CampaignMissionRuntimeComponent settled = next;
                if (!CampaignMissionRuntimeSystem.TryEvaluate(
                        in next, in facts, commandSquadSelected, out settled))
                {
                    break;
                }
                next = settled;
            }
            return true;
        }

        internal static bool IsEstablishBaseDefinition(ref CampaignMissionDefinitionBlob definition)
        {
            int buildRules = 0;
            int produceRules = 0;
            int defendRules = 0;
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                switch (definition.Objectives[index].Rule)
                {
                    case MissionObjectiveRuleKind.BuildStructure:
                        buildRules++;
                        break;
                    case MissionObjectiveRuleKind.ProduceUnit:
                        produceRules++;
                        break;
                    case MissionObjectiveRuleKind.DefendMissionRole:
                        defendRules++;
                        break;
                    default:
                        return false;
                }
            }
            return definition.Objectives.Length == 3 && buildRules == 1 && produceRules == 1 && defendRules == 1;
        }

        internal static bool AllEstablishBaseObjectivesComplete(
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            if (!IsEstablishBaseDefinition(ref definition) || facts.HostileTotalCount <= 0 ||
                facts.HostileDefeatedCount < facts.HostileTotalCount || facts.DefenseWaveActivated == 0)
                return false;

            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                bool complete = objective.Rule switch
                {
                    MissionObjectiveRuleKind.BuildStructure =>
                        facts.RequiredBuildingCompletedCount >= objective.RequiredCount,
                    MissionObjectiveRuleKind.ProduceUnit =>
                        facts.RequiredUnitProducedCount >= objective.RequiredCount,
                    MissionObjectiveRuleKind.DefendMissionRole =>
                        facts.ForwardPostBound != 0 && facts.ForwardPostDestroyed == 0,
                    _ => false
                };
                if (!complete)
                    return false;
            }
            return true;
        }

        private static bool TryResolveEstablishBaseTransition(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition,
            out MissionPhaseKind phase,
            out MissionOutcomeKind outcome,
            out MissionReturnDestinationKind destination)
        {
            phase = current.Phase;
            outcome = current.Outcome;
            destination = current.ReturnDestination;
            if (!IsEstablishBaseDefinition(ref definition) || facts.RequiredBuildingCompletedCount < 0 ||
                facts.RequiredUnitProducedCount < 0 || facts.HostileTotalCount < 0 ||
                facts.HostileDefeatedCount < 0 || facts.HostileDefeatedCount > facts.HostileTotalCount)
                return false;

            if (current.Phase == MissionPhaseKind.Preparing &&
                (current.ReadyReadiness & current.RequiredReadiness) == current.RequiredReadiness)
                phase = MissionPhaseKind.InteractiveBrief;
            else if (current.Phase == MissionPhaseKind.InteractiveBrief)
                phase = MissionPhaseKind.FindSquad;
            else if (current.Phase >= MissionPhaseKind.FindSquad &&
                     current.Phase <= MissionPhaseKind.SecureCorridor &&
                     HasBrokenFailureObjective(in facts, ref definition))
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.FindSquad)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.Engage &&
                     AllEstablishBaseObjectivesComplete(in facts, ref definition))
                phase = MissionPhaseKind.SecureCorridor;
            else if (current.Phase == MissionPhaseKind.SecureCorridor &&
                     AllEstablishBaseObjectivesComplete(in facts, ref definition))
            {
                phase = MissionPhaseKind.Result;
                outcome = MissionOutcomeKind.Victory;
                destination = current.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch
                    ? MissionReturnDestinationKind.CommandBase
                    : MissionReturnDestinationKind.CampaignOperations;
            }
            return phase != current.Phase || outcome != current.Outcome ||
                   destination != current.ReturnDestination;
        }

        private static bool HasBrokenFailureObjective(
            in CampaignMissionAttemptFactsComponent facts,
            ref CampaignMissionDefinitionBlob definition)
        {
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob objective = ref definition.Objectives[index];
                if (objective.FailureOnRuleBreak == 0)
                    continue;
                if (objective.Rule == MissionObjectiveRuleKind.DefendMissionRole &&
                    facts.ForwardPostDestroyed != 0)
                    return true;
            }
            return false;
        }

        internal static bool TryResolveAutomaticTransition(
            in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts,
            bool commandSquadSelected,
            out MissionPhaseKind phase,
            out MissionOutcomeKind outcome,
            out MissionReturnDestinationKind destination)
        {
            phase = current.Phase;
            outcome = current.Outcome;
            destination = current.ReturnDestination;
            if (current.Phase == MissionPhaseKind.Preparing &&
                (current.ReadyReadiness & current.RequiredReadiness) == current.RequiredReadiness)
                phase = MissionPhaseKind.InteractiveBrief;
            else if (current.Phase == MissionPhaseKind.InteractiveBrief)
                phase = MissionPhaseKind.FindSquad;
            else if (current.Phase == MissionPhaseKind.FindSquad &&
                     facts.CommandSquadSpawned != 0 && facts.CommandSquadAlive == 0 &&
                     facts.SquadLossCount > 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.FindSquad && current.RunKind != MissionRunKind.FirstClear &&
                     current.ReplayTutorialEnabled == 0)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.FindSquad && commandSquadSelected)
                phase = MissionPhaseKind.MoveToCover;
            else if (current.Phase == MissionPhaseKind.MoveToCover && facts.CommandSquadAlive == 0 &&
                     facts.SquadLossCount > 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.MoveToCover && facts.MoveToCoverComplete != 0)
                phase = MissionPhaseKind.ConfirmThreat;
            else if (current.Phase == MissionPhaseKind.ConfirmThreat && facts.CommandSquadAlive == 0 &&
                     facts.SquadLossCount > 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.ConfirmThreat && facts.ThreatConfirmed != 0)
                phase = MissionPhaseKind.Engage;
            else if (current.Phase == MissionPhaseKind.Engage && facts.CommandSquadAlive == 0 &&
                     facts.SquadLossCount > 0)
                return ResolveDefeat(out phase, out outcome, out destination);
            else if (current.Phase == MissionPhaseKind.Engage && facts.HostileTotalCount > 0 &&
                     facts.HostileDefeatedCount >= facts.HostileTotalCount)
                phase = MissionPhaseKind.SecureCorridor;
            else if (current.Phase == MissionPhaseKind.SecureCorridor &&
                     CanFinishFinale(in facts, current.Phase))
            {
                phase = MissionPhaseKind.Result;
                outcome = MissionOutcomeKind.Victory;
                destination = current.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch
                    ? MissionReturnDestinationKind.CommandBase
                    : MissionReturnDestinationKind.CampaignOperations;
            }
            return phase != current.Phase || outcome != current.Outcome ||
                   destination != current.ReturnDestination;
        }

        private static bool CanFinishFinale(
            in CampaignMissionAttemptFactsComponent facts,
            MissionPhaseKind phase) =>
            phase != MissionPhaseKind.SecureCorridor ||
            facts.FinalePresentationRequired == 0 ||
            facts.FinalePresentationComplete != 0;

        private static bool ResolveDefeat(
            out MissionPhaseKind phase,
            out MissionOutcomeKind outcome,
            out MissionReturnDestinationKind destination)
        {
            phase = MissionPhaseKind.Result;
            outcome = MissionOutcomeKind.Defeat;
            destination = MissionReturnDestinationKind.CampaignOperations;
            return true;
        }
    }
}
