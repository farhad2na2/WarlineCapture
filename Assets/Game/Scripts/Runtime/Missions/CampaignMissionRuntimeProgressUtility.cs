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
                 next.Phase is MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor;
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
    }
}
