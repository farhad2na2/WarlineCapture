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
