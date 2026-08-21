using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class CampaignMissionGuidedMoveRouteUtility
    {
        private static readonly FixedString64Bytes MissionId = "saga.ch01.m01.first_contact";
        private static readonly FixedString64Bytes FriendlyRoleId = "role.friendly.command_squad";
        private static readonly FixedString64Bytes MoveTargetAnchorId = "anchor.ch01.m01.move_target";
        private const int ExpectedCommandSquadCount = 4;

        public readonly struct Context
        {
            public readonly FixedString64Bytes SessionToken;
            public readonly int2 TargetCell;
            public readonly int TargetRadiusCells;

            public Context(FixedString64Bytes sessionToken, int2 targetCell, int targetRadiusCells)
            {
                SessionToken = sessionToken;
                TargetCell = targetCell;
                TargetRadiusCells = targetRadiusCells;
            }
        }

        public static bool TryCreateContext(
            EntityManager entityManager,
            in GridConfig grid,
            int2 requestedGoal,
            out Context context)
        {
            context = default;
            using EntityQuery runtimeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            using EntityQuery metadataQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapMetadataComponent>());
            if (runtimeQuery.CalculateEntityCount() != 1 || metadataQuery.CalculateEntityCount() != 1)
                return false;

            CampaignMissionRuntimeComponent runtime = runtimeQuery.GetSingleton<CampaignMissionRuntimeComponent>();
            OperationMapMetadataComponent metadata = metadataQuery.GetSingleton<OperationMapMetadataComponent>();
            // UI unlock and mission-phase projection are separate ECS updates. Accept the
            // authored move on either side of both boundaries so a quick Move/Do It input
            // cannot fall through to the ordinary selected-unit city pathfinder.
            bool isGuidedMovePhase = runtime.Phase == MissionPhaseKind.InteractiveBrief ||
                                     runtime.Phase == MissionPhaseKind.FindSquad ||
                                     runtime.Phase == MissionPhaseKind.MoveToCover;
            if (!runtime.MissionId.Equals(MissionId) || !isGuidedMovePhase ||
                runtime.Outcome != MissionOutcomeKind.None ||
                !metadata.Blob.IsCreated ||
                !CampaignMissionSpawnSystem.TryFindAnchor(
                    ref metadata.Blob.Value, MoveTargetAnchorId, out OperationMapAnchorBlob moveTarget))
            {
                return false;
            }

            int2 targetCell = GridUtils.WorldToCell(grid, moveTarget.Position);
            int radiusCells = math.max(2, (int)math.ceil(math.max(0.25f, moveTarget.Radius) / grid.CellSize));
            // M01 exposes one authored move during this phase. Once that phase is active,
            // never fall through to the ordinary selected-unit pathfinder because a click
            // landed near the edge of the marker; snap the whole squad to the authored lane.
            context = new Context(runtime.SessionToken, targetCell, radiusCells);
            return true;
        }

        public static bool IsGuidedMovePhaseActive(EntityManager entityManager)
        {
            using EntityQuery runtimeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (runtimeQuery.CalculateEntityCount() != 1)
                return false;

            CampaignMissionRuntimeComponent runtime = runtimeQuery.GetSingleton<CampaignMissionRuntimeComponent>();
            return runtime.MissionId.Equals(MissionId) &&
                   runtime.Outcome == MissionOutcomeKind.None &&
                   (runtime.Phase == MissionPhaseKind.InteractiveBrief ||
                    runtime.Phase == MissionPhaseKind.FindSquad ||
                    runtime.Phase == MissionPhaseKind.MoveToCover);
        }

        public static bool CanIssueStreetRoute(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            Entity entity,
            int2 goal,
            in Context context)
        {
            if (!TryResolveRouteInputs(
                    entityManager,
                    gridEntity,
                    entity,
                    context,
                    out int2 currentCell,
                    out int2 footprintSize,
                    out byte factionId,
                    out NativeArray<GridWalkable> walkable,
                    out NativeBitArray blocked,
                    out NativeArray<byte> friendlyPassFactionIds))
            {
                return false;
            }

            using NativeList<int2> route = new(Allocator.Temp);
            return CampaignMissionGuidedStreetPathUtility.TryBuild(
                entityManager,
                gridEntity,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                currentCell,
                goal,
                footprintSize,
                factionId,
                route);
        }

        public static bool TryIssueStreetRoute(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            UnitMoveOrderSystem moveOrderSystem,
            Entity entity,
            int2 goal,
            in Context context,
            int currentFrame,
            out UnitMoveOrderSystem.MoveOrderCommandResult result)
        {
            result = default;
            if (!TryResolveRouteInputs(
                    entityManager,
                    gridEntity,
                    entity,
                    context,
                    out int2 currentCell,
                    out int2 footprintSize,
                    out byte factionId,
                    out NativeArray<GridWalkable> walkable,
                    out NativeBitArray blocked,
                    out NativeArray<byte> friendlyPassFactionIds))
            {
                return false;
            }

            PathPoolComponent pool = entityManager.GetComponentData<PathPoolComponent>(gridEntity);
            using NativeList<int2> route = new(Allocator.Temp);
            if (!CampaignMissionGuidedStreetPathUtility.TryBuild(
                    entityManager,
                    gridEntity,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    currentCell,
                    goal,
                    footprintSize,
                    factionId,
                    route))
            {
                return false;
            }

            result = moveOrderSystem.IssueGroupedManualMoveOrder(
                entityManager, entity, goal, false, false, 0, currentFrame);
            if (!result.Issued)
                return false;

            if (entityManager.HasComponent<UnitPathRequest>(entity))
            {
                entityManager.RemoveComponent<UnitPathRequest>(entity);
                result.StructuralRemoves++;
            }

            int start = pool.Cells.Length;
            for (int i = 0; i < route.Length; i++)
                pool.Cells.Add(route[i]);
            entityManager.SetComponentData(gridEntity, pool);

            UnitPathFollow follow = new() { PathIndex = 0 };
            UnitPathRange range = new() { Start = start, Length = route.Length };
            if (entityManager.HasComponent<UnitPathFollow>(entity))
                entityManager.SetComponentData(entity, follow);
            else
            {
                entityManager.AddComponentData(entity, follow);
                result.StructuralAdds++;
            }
            if (entityManager.HasComponent<UnitPathRange>(entity))
                entityManager.SetComponentData(entity, range);
            else
            {
                entityManager.AddComponentData(entity, range);
                result.StructuralAdds++;
            }
            if (!entityManager.HasComponent<CampaignMissionGuidedMoveInProgressTag>(entity))
            {
                entityManager.AddComponent<CampaignMissionGuidedMoveInProgressTag>(entity);
                result.StructuralAdds++;
            }
            new UnitPathSurfaceMetadata().ClearIfPresent(entityManager, entity);
            return true;
        }

        public static bool TryResolveStreetFormationGoals(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            UnitMoveOrderSystem moveOrderSystem,
            in NativeArray<Entity> entities,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            HashSet<int> selectedCurrentCells,
            MapSurfacePathfindingSnapshot.Context surfaceContext,
            in Context context,
            int2[] goals)
        {
            if (goals == null || goals.Length != entities.Length ||
                !CampaignMissionGuidedStreetPathUtility.HasRequiredBuffers(entityManager, gridEntity))
            {
                return false;
            }

            var reservedGoalCells = new HashSet<int>();
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                Entity entity = entities[entityIndex];
                if (!IsGuidedFriendly(entityManager, entity, context) ||
                    !TryResolveStraightStreetGoal(
                        entityManager,
                        gridEntity,
                        grid,
                        moveOrderSystem,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        selectedCurrentCells,
                        surfaceContext,
                        entity,
                        context,
                        reservedGoalCells,
                        out int2 goal))
                {
                    return false;
                }

                goals[entityIndex] = goal;
            }

            return entities.Length > 0;
        }

        public static bool TryCollectFullFriendlySquad(
            EntityManager entityManager,
            in Context context,
            NativeList<Entity> squad)
        {
            squad.Clear();
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!IsGuidedFriendly(entityManager, entity, context) ||
                    !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
                    entityManager.HasComponent<UnitTransportPassenger>(entity) ||
                    (entityManager.HasComponent<UnitHealth>(entity) &&
                     entityManager.GetComponentData<UnitHealth>(entity).Current <= 0))
                {
                    continue;
                }

                squad.Add(entity);
            }

            // M01 owns exactly four command-squad soldiers. Returning a partial squad here
            // recreates the observed three-move/one-left-behind failure and must fail closed.
            return squad.Length == ExpectedCommandSquadCount;
        }

        private static bool TryResolveRouteInputs(
            EntityManager entityManager,
            Entity gridEntity,
            Entity entity,
            in Context context,
            out int2 currentCell,
            out int2 footprintSize,
            out byte factionId,
            out NativeArray<GridWalkable> walkable,
            out NativeBitArray blocked,
            out NativeArray<byte> friendlyPassFactionIds)
        {
            currentCell = default;
            footprintSize = default;
            factionId = 0;
            walkable = default;
            blocked = default;
            friendlyPassFactionIds = default;
            if (!entityManager.Exists(entity) || entityManager.HasComponent<UnitAirMovement>(entity) ||
                !entityManager.HasComponent<CampaignMissionUnitRoleComponent>(entity) ||
                !entityManager.HasComponent<UnitGrid>(entity) ||
                !entityManager.HasComponent<UnitMove>(entity) ||
                !entityManager.HasComponent<PathPoolComponent>(gridEntity) ||
                !entityManager.HasBuffer<GridWalkable>(gridEntity) ||
                !entityManager.HasComponent<DynamicBlockerComponent>(gridEntity))
            {
                return false;
            }

            CampaignMissionUnitRoleComponent role =
                entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
            if (!role.SessionToken.Equals(context.SessionToken) || !role.MissionRoleId.Equals(FriendlyRoleId))
                return false;

            factionId = entityManager.HasComponent<Faction>(entity)
                ? entityManager.GetComponentData<Faction>(entity).Id
                : (byte)0;
            if (!FactionIdentity.IsPlayerControlled(factionId))
                return false;

            PathPoolComponent pool = entityManager.GetComponentData<PathPoolComponent>(gridEntity);
            if (!pool.Cells.IsCreated)
                return false;

            // Structural changes are applied only after every squad member preflights. The
            // views are then reacquired for each apply so one soldier cannot invalidate the next.
            walkable = entityManager.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData =
                entityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            blocked = blockerData.Blocked;
            friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            currentCell = entityManager.GetComponentData<UnitGrid>(entity).Cell;
            footprintSize = entityManager.HasComponent<UnitFootprint>(entity)
                ? entityManager.GetComponentData<UnitFootprint>(entity).Size
                : new int2(1, 1);
            return true;
        }

        private static bool TryResolveStraightStreetGoal(
            EntityManager entityManager,
            Entity gridEntity,
            in GridConfig grid,
            UnitMoveOrderSystem moveOrderSystem,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            HashSet<int> selectedCurrentCells,
            MapSurfacePathfindingSnapshot.Context surfaceContext,
            Entity entity,
            in Context context,
            HashSet<int> reservedGoalCells,
            out int2 resolvedGoal)
        {
            resolvedGoal = default;
            int2 start = entityManager.GetComponentData<UnitGrid>(entity).Cell;
            int2 footprintSize = entityManager.HasComponent<UnitFootprint>(entity)
                ? entityManager.GetComponentData<UnitFootprint>(entity).Size
                : new int2(1, 1);
            byte factionId = entityManager.HasComponent<Faction>(entity)
                ? entityManager.GetComponentData<Faction>(entity).Id
                : (byte)0;
            bool advancesAlongZ = math.abs(context.TargetCell.y - start.y) >=
                                  math.abs(context.TargetCell.x - start.x);

            // Finish the authored move as a readable four-soldier firing line across the
            // road. The former all-cells search favored a single-file column on the road
            // axis; in the RTS camera one soldier could be hidden directly behind another
            // and the move looked like a three-soldier order. Every preferred slot remains
            // inside the validated move-target disk and every route is still the direct
            // authored street route.
            if (CampaignMissionGuidedStreetPathUtility.TryResolvePreferredFormationGoal(
                    entityManager,
                    gridEntity,
                    grid,
                    moveOrderSystem,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    selectedCurrentCells,
                    surfaceContext,
                    start,
                    footprintSize,
                    factionId,
                    context,
                    advancesAlongZ,
                    reservedGoalCells,
                    out resolvedGoal))
            {
                return true;
            }

            int bestScore = int.MaxValue;

            using NativeList<int2> route = new(Allocator.Temp);
            int radius = context.TargetRadiusCells;
            for (int zOffset = -radius; zOffset <= radius; zOffset++)
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                int2 offset = new(xOffset, zOffset);
                if (math.lengthsq(offset) > radius * radius)
                    continue;

                int2 candidate = context.TargetCell + offset;
                int lateralChange = advancesAlongZ
                    ? math.abs(candidate.x - start.x)
                    : math.abs(candidate.y - start.y);
                int forwardOffset = advancesAlongZ ? math.abs(zOffset) : math.abs(xOffset);
                int centerOffset = math.abs(xOffset) + math.abs(zOffset);
                int score = lateralChange * 100 + forwardOffset * 10 + centerOffset;
                if (score >= bestScore ||
                    !moveOrderSystem.CanReserveManualMoveGoal(
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        reservedGoalCells,
                        selectedCurrentCells,
                        candidate,
                        footprintSize,
                        0,
                        factionId,
                        surfaceContext,
                        false))
                {
                    continue;
                }

                route.Clear();
                if (!CampaignMissionGuidedStreetPathUtility.TryBuildDirect(
                        entityManager,
                        gridEntity,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        start,
                        candidate,
                        footprintSize,
                        factionId,
                        route))
                {
                    continue;
                }

                bestScore = score;
                resolvedGoal = candidate;
            }

            if (bestScore == int.MaxValue)
                return false;

            moveOrderSystem.ReserveManualMoveGoalFootprint(
                grid, reservedGoalCells, resolvedGoal, footprintSize, 0);
            return true;
        }

        private static bool IsGuidedFriendly(
            EntityManager entityManager,
            Entity entity,
            in Context context)
        {
            if (!entityManager.Exists(entity) || entityManager.HasComponent<UnitAirMovement>(entity) ||
                !entityManager.HasComponent<CampaignMissionUnitRoleComponent>(entity) ||
                !entityManager.HasComponent<UnitGrid>(entity))
            {
                return false;
            }

            CampaignMissionUnitRoleComponent role =
                entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
            return role.SessionToken.Equals(context.SessionToken) &&
                   role.MissionRoleId.Equals(FriendlyRoleId);
        }

    }
}
