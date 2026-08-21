using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>
    /// Executes M01 ARIA takeover moves through the same full-squad formation and
    /// authored-street route used by the player's guided RTS command.
    /// </summary>
    internal static class CampaignMissionGuidedSquadMoveUtility
    {
        internal static bool TryIssue(
            EntityManager entityManager,
            Entity sourceEntity,
            int2 requestedGoal,
            int currentFrame,
            out UnitMoveOrderSystem.MoveOrderCommandResult aggregateResult)
        {
            aggregateResult = default;
            using EntityQuery gridQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>(),
                ComponentType.ReadOnly<PathPoolComponent>());
            if (sourceEntity == Entity.Null || !entityManager.Exists(sourceEntity) ||
                gridQuery.CalculateEntityCount() != 1)
            {
                return false;
            }

            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = entityManager.GetComponentData<GridConfig>(gridEntity);
            if (!CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                    entityManager,
                    grid,
                    requestedGoal,
                    out CampaignMissionGuidedMoveRouteUtility.Context context))
            {
                return false;
            }

            using NativeList<Entity> squad = new(Allocator.Temp);
            if (!CampaignMissionGuidedMoveRouteUtility.TryCollectFullFriendlySquad(
                    entityManager,
                    context,
                    squad) ||
                !Contains(squad, sourceEntity))
            {
                return false;
            }

            NativeArray<Entity> entities = squad.AsArray();
            var moveOrderSystem = new UnitMoveOrderSystem();
            NativeArray<GridWalkable> walkable =
                entityManager.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockers =
                entityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray occupied =
                entityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            HashSet<int> currentCells =
                moveOrderSystem.BuildSelectedCurrentFootprintCells(entityManager, grid, entities);
            MapSurfacePathfindingSnapshot surfaceSnapshot = new();
            using EntityQuery surfaceQuery =
                entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            MapSurfacePathfindingSnapshot.Context surfaceContext =
                surfaceSnapshot.TryCreateContext(
                    entityManager,
                    surfaceQuery,
                    out MapSurfacePathfindingSnapshot.Context resolvedSurface)
                    ? resolvedSurface
                    : surfaceSnapshot.CreateFlatFallbackContext();
            var formationGoals = new int2[entities.Length];
            if (!CampaignMissionGuidedMoveRouteUtility.TryResolveStreetFormationGoals(
                    entityManager,
                    gridEntity,
                    grid,
                    moveOrderSystem,
                    entities,
                    walkable,
                    blockers.Blocked,
                    blockers.FriendlyPassFactionIds,
                    occupied,
                    currentCells,
                    surfaceContext,
                    context,
                    formationGoals))
            {
                return false;
            }

            // Preflight every route before mutating any soldier. This keeps the command atomic:
            // either all four receive the authored street route or none of them do.
            for (int index = 0; index < entities.Length; index++)
            {
                if (!CampaignMissionGuidedMoveRouteUtility.CanIssueStreetRoute(
                        entityManager,
                        gridEntity,
                        grid,
                        entities[index],
                        formationGoals[index],
                        context))
                {
                    return false;
                }
            }

            int issuedCount = 0;
            for (int index = 0; index < entities.Length; index++)
            {
                if (!CampaignMissionGuidedMoveRouteUtility.TryIssueStreetRoute(
                        entityManager,
                        gridEntity,
                        grid,
                        moveOrderSystem,
                        entities[index],
                        formationGoals[index],
                        context,
                        currentFrame,
                        out UnitMoveOrderSystem.MoveOrderCommandResult result) ||
                    !result.Issued)
                {
                    return false;
                }

                issuedCount++;
                Add(ref aggregateResult, in result);
            }

            aggregateResult.Issued = issuedCount == entities.Length && issuedCount > 0;
            return aggregateResult.Issued;
        }

        private static bool Contains(NativeList<Entity> entities, Entity expected)
        {
            for (int index = 0; index < entities.Length; index++)
                if (entities[index] == expected)
                    return true;
            return false;
        }

        private static void Add(
            ref UnitMoveOrderSystem.MoveOrderCommandResult aggregate,
            in UnitMoveOrderSystem.MoveOrderCommandResult current)
        {
            aggregate.StructuralAdds += current.StructuralAdds;
            aggregate.StructuralRemoves += current.StructuralRemoves;
            aggregate.PathRequests += current.PathRequests;
            aggregate.StaggeredPathRequests += current.StaggeredPathRequests;
            aggregate.MaxStaggerDelayFrames = math.max(
                aggregate.MaxStaggerDelayFrames,
                current.MaxStaggerDelayFrames);
            aggregate.AirUnits += current.AirUnits;
        }
    }
}
