using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    // Shared native Return implementation; selection/UI authority stays at the caller.
    public static class UnitReturnToBaseCommandService
    {
        public static bool TryIssueOwned(EntityManager em, Entity session, byte actingFaction, Entity actor)
        {
            if (!SkirmishActorCommandAuthority.IsActiveOwnedActor(em, session, actingFaction, actor))
                return false;
            using var queues = em.CreateEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>(), ComponentType.ReadOnly<RespawnQueueComponent>());
            using var buildings = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            if (queues.CalculateEntityCount() > 1) return false;
            return TryIssue(em, queues.IsEmptyIgnoreFilter ? Entity.Null : queues.GetSingletonEntity(), buildings, actor);
        }

        public static bool TryIssue(
            EntityManager em,
            Entity queueEntity,
            EntityQuery buildingRuntimeStateQuery,
            Entity entity)
        {
            byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
            int2 currentCell = em.HasComponent<UnitGrid>(entity)
                ? em.GetComponentData<UnitGrid>(entity).Cell
                : default;

            if (!TryResolveReturnToBaseGoal(em, queueEntity, buildingRuntimeStateQuery, entity, factionId, currentCell, out int2 goal))
                return false;

            // A Return interrupts the logical Skirmish attack as well as the
            // native order. Otherwise a later attack on that same target is
            // mistaken for an order still executing and never launches again.
            if (SkirmishSharedCombatBinding.UsesSharedCommands(em, entity))
                SkirmishWorldMovementService.ClearIntent(em, entity);
            bool initializedAircraftHome = false;
            if (em.HasComponent<UnitAirComponent>(entity))
            {
                var air = em.GetComponentData<UnitAirComponent>(entity);
                if (air.HomeInitialized != 0)
                {
                    goal = air.HomeCell;
                    initializedAircraftHome = true;
                    air.ReturningHome = 1;
                    // Replace an attack/move pass with the native landing
                    // maneuver. Preserve a return already lining up or rolling.
                    if (air.ReturnApproachInitialized == 0 && air.AttackRunActive <= 3)
                    {
                        air.AttackRunActive = 0;
                        air.AttackRunExitPosition = default;
                    }
                    em.SetComponentData(entity, air);
                }
                // Returning aircraft must not reacquire a nearby enemy and
                // abandon the landing. A new AttackMove explicitly arms combat.
                if (em.HasComponent<UnitCombat>(entity))
                {
                    var combat = em.GetComponentData<UnitCombat>(entity);
                    combat.AutoEngage = 0;
                    em.SetComponentData(entity, combat);
                }
            }
            if (initializedAircraftHome)
            {
                // A normal Move to parking makes fixed wings fly over it at
                // cruise height before their runway return can start.
                // ReturningHome already owns landing and the final taxi.
                UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, entity);
                return true;
            }
            return UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, entity, goal);
        }

        private static bool TryResolveReturnToBaseGoal(
            EntityManager em,
            Entity queueEntity,
            EntityQuery buildingRuntimeStateQuery,
            Entity entity,
            byte factionId,
            int2 currentCell,
            out int2 goal)
        {
            if (TryResolveProducedUnitHomeSpawnPoint(em, buildingRuntimeStateQuery, entity, factionId, out goal))
                return true;

            if (TryResolveNearestFactionProductionSpawnPoint(em, buildingRuntimeStateQuery, factionId, currentCell, out goal))
                return true;

            return TryResolveFactionRespawnSpawnPoint(em, queueEntity, factionId, out goal);
        }

        private static bool TryResolveProducedUnitHomeSpawnPoint(
            EntityManager em,
            EntityQuery buildingRuntimeStateQuery,
            Entity entity,
            byte factionId,
            out int2 goal)
        {
            goal = default;
            if (!TryGetBuildingRuntimeBoundaryEntity(em, buildingRuntimeStateQuery, out Entity boundaryEntity) ||
                !em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity) ||
                !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
            {
                return false;
            }

            DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);

            for (int i = 0; i < producedUnits.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnits[i];
                if (producedUnit.Unit != entity ||
                    producedUnit.HasOwnerFaction == 0 ||
                    producedUnit.OwnerFactionId != factionId)
                {
                    continue;
                }

                int buildingRuntimeId = producedUnit.ProductionSlotBuildingRuntimeId > 0
                    ? producedUnit.ProductionSlotBuildingRuntimeId
                    : producedUnit.BuildingRuntimeId;
                int slotIndex = producedUnit.ProductionSlotIndex;
                if (TryFindProductionSpawnPoint(spawnPoints, factionId, buildingRuntimeId, slotIndex, out goal))
                    return true;

                if (TryFindProductionSpawnPoint(spawnPoints, factionId, buildingRuntimeId, -1, out goal))
                    return true;
            }

            return false;
        }

        private static bool TryResolveNearestFactionProductionSpawnPoint(
            EntityManager em,
            EntityQuery buildingRuntimeStateQuery,
            byte factionId,
            int2 currentCell,
            out int2 goal)
        {
            goal = default;
            if (!TryGetBuildingRuntimeBoundaryEntity(em, buildingRuntimeStateQuery, out Entity boundaryEntity) ||
                !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
            {
                return false;
            }

            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
            int bestDistance = int.MaxValue;
            bool found = false;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
                if (spawnPoint.FactionId != factionId)
                    continue;

                int2 delta = spawnPoint.Cell - currentCell;
                int distance = delta.x * delta.x + delta.y * delta.y;
                if (found && distance >= bestDistance)
                    continue;

                bestDistance = distance;
                goal = spawnPoint.Cell;
                found = true;
            }

            return found;
        }

        private static bool TryResolveFactionRespawnSpawnPoint(
            EntityManager em,
            Entity queueEntity,
            byte factionId,
            out int2 goal)
        {
            goal = default;
            if (queueEntity == Entity.Null ||
                !em.Exists(queueEntity) ||
                !em.HasBuffer<RespawnFactionSpawnPoint>(queueEntity))
            {
                return false;
            }

            DynamicBuffer<RespawnFactionSpawnPoint> points = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].FactionId != factionId)
                    continue;

                goal = points[i].SpawnCell;
                return true;
            }

            return false;
        }

        private static bool TryGetBuildingRuntimeBoundaryEntity(
            EntityManager em,
            EntityQuery buildingRuntimeStateQuery,
            out Entity boundaryEntity)
        {
            boundaryEntity = Entity.Null;
            if (buildingRuntimeStateQuery.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = buildingRuntimeStateQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!em.Exists(entity))
                        continue;

                    boundaryEntity = entity;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindProductionSpawnPoint(
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints,
            byte factionId,
            int buildingRuntimeId,
            int slotIndex,
            out int2 goal)
        {
            goal = default;
            if (buildingRuntimeId <= 0)
                return false;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
                if (spawnPoint.FactionId != factionId ||
                    spawnPoint.BuildingRuntimeId != buildingRuntimeId ||
                    (slotIndex >= 0 && spawnPoint.SlotIndex != slotIndex))
                {
                    continue;
                }

                goal = spawnPoint.Cell;
                return true;
            }

            return false;
        }

    }
}
