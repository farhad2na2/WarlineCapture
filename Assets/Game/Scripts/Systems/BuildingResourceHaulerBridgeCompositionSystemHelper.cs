using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingResourceHaulerBridgeCompositionSystemHelper
    {
        private static readonly bool VerboseResourceHaulerLogs = false;
        private readonly HashSet<Entity> _invalidCapacityWarningEntities = new();

        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
        public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
        public delegate void EnsureEntityQueriesDelegate(EntityManager entityManager);
        public delegate EntityQuery GetEntityQueryDelegate();
        public delegate bool TryGetRuntimeBuildingDelegate(int id, out RuntimeBuildingEntity building);
        public delegate Vector3 ResolveBuildingFocusWorldPositionDelegate(RuntimeBuildingEntity building);
        public delegate RectInt GetEffectivePlacementRectDelegate(RuntimeBuildingEntity building, GridConfig grid);

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly ResourceHaulerUtilitySystemHelper ResourceHaulerUtilitySystemHelper;
            public readonly FactionResourceCompositionSystemHelper FactionResourceCompositionSystemHelper;
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly EnsureEntityQueriesDelegate EnsureEntityQueries;
            public readonly GetEntityQueryDelegate GetHaulerUnitsQuery;
            public readonly GetEntityQueryDelegate GetSelectedUnitsQuery;
            public readonly TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
            public readonly ResolveBuildingFocusWorldPositionDelegate ResolveBuildingFocusWorldPosition;
            public readonly GetEffectivePlacementRectDelegate GetEffectivePlacementRect;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                ResourceHaulerUtilitySystemHelper resourceHaulerSystem,
                FactionResourceCompositionSystemHelper factionResourceSystem,
                TryGetEntityManagerDelegate tryGetEntityManager,
                TryGetGridDataDelegate tryGetGridData,
                EnsureEntityQueriesDelegate ensureEntityQueries,
                GetEntityQueryDelegate getHaulerUnitsQuery,
                GetEntityQueryDelegate getSelectedUnitsQuery,
                TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
                ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
                GetEffectivePlacementRectDelegate getEffectivePlacementRect)
            {
                RuntimeBuildings = runtimeBuildings;
                ResourceHaulerUtilitySystemHelper = resourceHaulerSystem;
                FactionResourceCompositionSystemHelper = factionResourceSystem;
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                EnsureEntityQueries = ensureEntityQueries;
                GetHaulerUnitsQuery = getHaulerUnitsQuery;
                GetSelectedUnitsQuery = getSelectedUnitsQuery;
                TryGetRuntimeBuilding = tryGetRuntimeBuilding;
                ResolveBuildingFocusWorldPosition = resolveBuildingFocusWorldPosition;
                GetEffectivePlacementRect = getEffectivePlacementRect;
            }
        }

        public void UpdateResourceHaulers(Context context, bool hasPendingPathJob, float now)
        {
            if (hasPendingPathJob)
                return;
            if (context.ResourceHaulerUtilitySystemHelper == null)
                return;
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return;
            context.EnsureEntityQueries?.Invoke(em);
            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return;

            EntityQuery haulerUnitsQuery = context.GetHaulerUnitsQuery != null
                ? context.GetHaulerUnitsQuery()
                : default;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = haulerUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var haulerQuery = new NativeList<Entity>(haulerUnitsQuery.CalculateEntityCount(), Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                haulerQuery.AddRange(entities);
            }

            if (haulerQuery.Length == 0)
                return;

            for (int i = 0; i < haulerQuery.Length; i++)
                UpdateResourceHauler(context, em, grid, haulerQuery[i], now);
        }

        public bool TryAssignSelectedHaulerOrders(Context context, int clickedBuildingId)
        {
            if (context.ResourceHaulerUtilitySystemHelper == null || context.FactionResourceCompositionSystemHelper == null)
                return false;
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return false;
            if (context.TryGetRuntimeBuilding == null || !context.TryGetRuntimeBuilding(clickedBuildingId, out RuntimeBuildingEntity clickedBuilding))
                return false;

            context.EnsureEntityQueries?.Invoke(em);
            EntityQuery selectedUnitsQuery = context.GetSelectedUnitsQuery != null
                ? context.GetSelectedUnitsQuery()
                : default;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var selected = new NativeList<Entity>(selectedUnitsQuery.CalculateEntityCount(), Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                selected.AddRange(entities);
            }

            if (selected.Length == 0)
                return false;

            bool clickedIsOilSource = context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(clickedBuilding);
            bool clickedIsFuelBuilding = context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(clickedBuilding);
            bool clickedIsStorage = context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(clickedBuilding);
            if (!clickedIsOilSource && !clickedIsFuelBuilding && !clickedIsStorage)
                return false;

            RuntimeBuildingEntity source = clickedBuilding;
            RuntimeBuildingEntity destination = clickedBuilding;
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
            if (clickedIsOilSource)
            {
                if (!TryFindNearestBuilding(context, clickedBuilding, candidate => context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(candidate), out destination))
                    return false;
                resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
            }
            else if (clickedIsFuelBuilding)
            {
                if (!TryFindNearestBuilding(context, clickedBuilding, candidate => context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(candidate), out source))
                    return false;
                destination = clickedBuilding;
                resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
            }
            else
            {
                destination = clickedBuilding;
                if (TryFindNearestBuilding(context, clickedBuilding, candidate => context.ResourceHaulerUtilitySystemHelper.HasAvailableFuelForHauler(candidate), out source))
                    resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel;
                else if (TryFindNearestBuilding(context, clickedBuilding, candidate => context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(candidate), out source))
                    resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
                else
                    return false;
            }

            bool assignedAny = false;
            for (int i = 0; i < selected.Length; i++)
            {
                Entity unit = selected[i];
                if (!em.Exists(unit) || !em.HasComponent<UnitResourceHauler>(unit) || em.HasComponent<UnitAirMovement>(unit))
                    continue;

                if (!TryIssueHaulerMoveToBuilding(context, em, unit, source, out int2 sourceGoal))
                    continue;

                UnitResourceHaulOrder order = context.ResourceHaulerUtilitySystemHelper.CreateOrder(source.Id, destination.Id, sourceGoal, resourceKind);

                if (em.HasComponent<UnitResourceHaulOrder>(unit))
                    em.SetComponentData(unit, order);
                else
                    em.AddComponentData(unit, order);

                assignedAny = true;
            }

            return assignedAny;
        }

        public bool TryGetRuntimeBuildingApproachCell(
            Context context,
            RuntimeBuildingEntity building,
            int2 unitFootprint,
            int2 referenceCell,
            out int2 goal)
        {
            goal = default;
            if (building == null || building.IsDestroyed)
                return false;
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return false;
            if (context.TryGetGridData == null || !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerComponent blockerData))
                return false;

            var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            return TryFindBuildingApproachCell(
                grid,
                walkable,
                blockerData.Blocked,
                occupied,
                building.OriginCell,
                building.Definition.FootprintCells,
                unitFootprint,
                referenceCell,
                out goal);
        }

        public bool IsRuntimeBuildingApproachCell(Context context, RuntimeBuildingEntity building, int2 currentCell, int2 unitFootprint)
        {
            if (building == null || building.IsDestroyed)
                return false;
            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return false;

            return IsHaulerAtBuildingApproach(context, currentCell, unitFootprint, building, grid);
        }

        private void UpdateResourceHauler(Context context, EntityManager em, GridConfig grid, Entity entity, float now)
        {
            if (!em.Exists(entity))
                return;

            UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(entity);
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(entity);
            int2 footprintSize = em.HasComponent<UnitFootprint>(entity)
                ? em.GetComponentData<UnitFootprint>(entity).Size
                : new int2(1, 1);
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind = (ResourceHaulerUtilitySystemHelper.ResourceHaulKind)order.ResourceKind;

            if (context.TryGetRuntimeBuilding == null ||
                !context.TryGetRuntimeBuilding(order.SourceBuildingId, out RuntimeBuildingEntity source) ||
                !context.TryGetRuntimeBuilding(order.DestinationBuildingId, out RuntimeBuildingEntity destination))
            {
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                return;
            }

            int2 currentCell = em.GetComponentData<UnitGrid>(entity).Cell;
            switch ((ResourceHaulerUtilitySystemHelper.ResourceHaulPhase)order.Phase)
            {
                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.None:
                    UpdateNonePhase(context, em, entity, source, ref order);
                    break;

                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource:
                    UpdateTravelToSourcePhase(context, em, grid, entity, source, currentCell, footprintSize, ref order);
                    break;

                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Loading:
                    UpdateLoadingPhase(context, em, entity, source, destination, resourceKind, ref hauler, ref order, now);
                    break;

                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination:
                    UpdateTravelToDestinationPhase(context, em, grid, entity, destination, currentCell, footprintSize, ref order);
                    break;

                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Unloading:
                    UpdateUnloadingPhase(context, em, entity, source, destination, resourceKind, ref hauler, ref order, now);
                    break;
            }
        }

        private static void UpdateNonePhase(Context context, EntityManager em, Entity entity, RuntimeBuildingEntity source, ref UnitResourceHaulOrder order)
        {
            if (!TryIssueHaulerMoveToBuilding(context, em, entity, source, out int2 goal))
                return;

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, goal);
            em.SetComponentData(entity, order);
        }

        private static void UpdateTravelToSourcePhase(
            Context context,
            EntityManager em,
            GridConfig grid,
            Entity entity,
            RuntimeBuildingEntity source,
            int2 currentCell,
            int2 footprintSize,
            ref UnitResourceHaulOrder order)
        {
            if (!IsHaulerAtBuildingApproach(context, currentCell, footprintSize, source, grid))
            {
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} phase=ToSource current={currentCell} target={order.TargetCell} source={source.Id} sourceOrigin={source.OriginCell}");
                if (!HasGoalOrPathRequest(em, entity, order.TargetCell))
                {
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} reissuing-source-move source={source.Id}");
                    TryIssueHaulerMoveToBuilding(context, em, entity, source, out _);
                }
                return;
            }

            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} arrived-source source={source.Id} current={currentCell}");
            context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Loading);
            em.SetComponentData(entity, order);
        }

        private void UpdateLoadingPhase(
            Context context,
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            ref UnitResourceHauler hauler,
            ref UnitResourceHaulOrder order,
            float now)
        {
            float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
            if (loadAmount <= 0f)
            {
                if (_invalidCapacityWarningEntities.Add(entity))
                    Debug.LogWarning($"[ResourceHauler] entity={entity} invalid-capacity capacity={hauler.BarrelCapacity}");
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                return;
            }

            _invalidCapacityWarningEntities.Remove(entity);
            float sourceStored = context.ResourceHaulerUtilitySystemHelper.GetStoredResource(em, source, resourceKind);
            float currentCargo = resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel ? hauler.CargoFuelBarrels : hauler.CargoOilBarrels;
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} phase=Loading resource={resourceKind} current={em.GetComponentData<UnitGrid>(entity).Cell} source={source.Id} stored={sourceStored:0.##} cargo={currentCargo:0.##}/{loadAmount:0.##} actionEndsAt={order.ActionEndsAt:0.##} now={now:0.##}");
            if (!context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(em, source, resourceKind, loadAmount))
            {
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} waiting-for-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                return;
            }

            ResourceHaulerUtilitySystemHelper.TimedActionState loadTimer = context.ResourceHaulerUtilitySystemHelper.AdvanceTimedAction(ref order, now, hauler.FillDurationSeconds);
            if (loadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Started)
            {
                em.SetComponentData(entity, order);
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} loading-started source={source.Id} fillDuration={hauler.FillDurationSeconds:0.##} completeAt={order.ActionEndsAt:0.##}");
                return;
            }
            if (loadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Waiting)
            {
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} loading-in-progress source={source.Id} remaining={order.ActionEndsAt - now:0.##}");
                return;
            }

            sourceStored = context.ResourceHaulerUtilitySystemHelper.GetStoredResource(em, source, resourceKind);
            if (!context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(em, source, resourceKind, loadAmount))
            {
                context.ResourceHaulerUtilitySystemHelper.ResetActionTimer(ref order);
                em.SetComponentData(entity, order);
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} loading-reset-insufficient-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                return;
            }

            if (!context.ResourceHaulerUtilitySystemHelper.TryCompleteLoad(em, source, resourceKind, loadAmount, ref hauler))
                return;
            em.SetComponentData(entity, hauler);
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} loading-complete resource={resourceKind} source={source.Id} loaded={loadAmount:0.##}");

            if (!TryIssueHaulerMoveToBuilding(context, em, entity, destination, out int2 destinationGoal))
            {
                context.ResourceHaulerUtilitySystemHelper.RevertLoad(em, source, resourceKind, loadAmount, ref hauler);
                em.SetComponentData(entity, hauler);
                if (VerboseResourceHaulerLogs)
                    Debug.LogWarning($"[ResourceHauler] entity={entity} failed-destination-move destination={destination.Id} revertedLoad={loadAmount:0.##}");
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, destinationGoal);
            em.SetComponentData(entity, order);
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} to-destination destination={destination.Id} target={destinationGoal}");
        }

        private static void UpdateTravelToDestinationPhase(
            Context context,
            EntityManager em,
            GridConfig grid,
            Entity entity,
            RuntimeBuildingEntity destination,
            int2 currentCell,
            int2 footprintSize,
            ref UnitResourceHaulOrder order)
        {
            if (!IsHaulerAtBuildingApproach(context, currentCell, footprintSize, destination, grid))
            {
                if (!HasGoalOrPathRequest(em, entity, order.TargetCell))
                    TryIssueHaulerMoveToBuilding(context, em, entity, destination, out _);
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Unloading);
            em.SetComponentData(entity, order);
        }

        private static void UpdateUnloadingPhase(
            Context context,
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            ref UnitResourceHauler hauler,
            ref UnitResourceHaulOrder order,
            float now)
        {
            float cargo = context.ResourceHaulerUtilitySystemHelper.GetCargo(hauler, resourceKind);
            if (cargo <= 0f)
            {
                context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.None);
                em.SetComponentData(entity, order);
                return;
            }

            if (!context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(em, destination, resourceKind, cargo))
                return;

            ResourceHaulerUtilitySystemHelper.TimedActionState unloadTimer = context.ResourceHaulerUtilitySystemHelper.AdvanceTimedAction(ref order, now, hauler.UnloadDurationSeconds);
            if (unloadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Started ||
                unloadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Waiting)
            {
                em.SetComponentData(entity, order);
                return;
            }

            if (!context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(em, destination, resourceKind, cargo))
            {
                context.ResourceHaulerUtilitySystemHelper.ResetActionTimer(ref order);
                em.SetComponentData(entity, order);
                return;
            }

            if (!context.ResourceHaulerUtilitySystemHelper.TryCompleteUnload(em, destination, resourceKind, ref hauler))
                return;
            em.SetComponentData(entity, hauler);

            if (!TryIssueHaulerMoveToBuilding(context, em, entity, source, out int2 sourceGoal))
            {
                context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.None);
                em.SetComponentData(entity, order);
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, sourceGoal);
            em.SetComponentData(entity, order);
        }

        private static bool IsHaulerAtBuildingApproach(Context context, int2 currentCell, int2 footprintSize, RuntimeBuildingEntity building, GridConfig grid)
        {
            if (building?.Definition == null || context.GetEffectivePlacementRect == null)
                return false;

            int2 clampedFootprint = UnitFootprintUtility.ClampSize(footprintSize);
            int2 unitMin = UnitFootprintUtility.GetMinCell(currentCell, clampedFootprint);
            RectInt unitRect = new(unitMin.x, unitMin.y, clampedFootprint.x, clampedFootprint.y);
            RectInt buildingRect = context.GetEffectivePlacementRect(building, grid);
            if (unitRect.Overlaps(buildingRect))
                return false;

            int distanceX = AxisDistance(unitRect.xMin, unitRect.xMax, buildingRect.xMin, buildingRect.xMax);
            int distanceY = AxisDistance(unitRect.yMin, unitRect.yMax, buildingRect.yMin, buildingRect.yMax);
            int approachDistance = math.max(distanceX, distanceY);
            return approachDistance <= 2;
        }

        private static int AxisDistance(int minA, int maxA, int minB, int maxB)
        {
            if (maxA <= minB)
                return minB - maxA;

            if (maxB <= minA)
                return minA - maxB;

            return 0;
        }

        private static bool TryFindNearestBuilding(Context context, RuntimeBuildingEntity originBuilding, System.Predicate<RuntimeBuildingEntity> predicate, out RuntimeBuildingEntity result)
        {
            result = null;
            if (originBuilding == null || predicate == null || context.RuntimeBuildings == null || context.ResolveBuildingFocusWorldPosition == null)
                return false;

            Vector3 origin = context.ResolveBuildingFocusWorldPosition(originBuilding);
            float bestDistanceSq = float.MaxValue;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null || candidate == originBuilding || candidate.IsDestroyed || !predicate(candidate))
                    continue;

                Vector3 candidatePosition = context.ResolveBuildingFocusWorldPosition(candidate);
                float distanceSq = (candidatePosition - origin).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                result = candidate;
            }

            return result != null;
        }

        private static bool TryIssueHaulerMoveToBuilding(Context context, EntityManager em, Entity unit, RuntimeBuildingEntity building, out int2 goal)
        {
            goal = default;
            if (building == null || building.IsDestroyed || !em.Exists(unit) || context.TryGetGridData == null ||
                !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerComponent blockerData))
            {
                return false;
            }

            var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            int2 referenceCell = em.GetComponentData<UnitGrid>(unit).Cell;
            int2 unitFootprint = em.HasComponent<UnitFootprint>(unit)
                ? em.GetComponentData<UnitFootprint>(unit).Size
                : new int2(1, 1);
            if (!TryFindBuildingApproachCell(grid, walkable, blockerData.Blocked, occupied, building.OriginCell, building.Definition.FootprintCells, unitFootprint, referenceCell, out goal))
                return false;

            if (em.HasComponent<EngageTarget>(unit))
                em.RemoveComponent<EngageTarget>(unit);
            if (em.HasComponent<UnitPathFollow>(unit))
                em.RemoveComponent<UnitPathFollow>(unit);
            if (em.HasComponent<UnitPathRange>(unit))
                em.RemoveComponent<UnitPathRange>(unit);
            if (em.HasComponent<AutoWanderMoveTag>(unit))
                em.RemoveComponent<AutoWanderMoveTag>(unit);

            UnitMoveOrderRequestSystem.EnqueueAndProcessTargetPathMoveOrder(em, unit, goal);

            if (!em.HasComponent<ManualMoveOrderTag>(unit))
                em.AddComponent<ManualMoveOrderTag>(unit);

            return true;
        }

        private static bool HasGoalOrPathRequest(EntityManager em, Entity entity, int2 goal)
        {
            bool sameTarget = em.HasComponent<UnitTarget>(entity) && em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
            bool sameRequest = em.HasComponent<UnitPathRequest>(entity) && em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
            return sameTarget || sameRequest;
        }

        private static bool TryFindBuildingApproachCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            Vector2Int originCell,
            Vector2Int footprintCells,
            int2 unitFootprint,
            int2 referenceCell,
            out int2 goal)
        {
            goal = default;
            int maxRadius = math.max(grid.Width, grid.Height);
            int bestScore = int.MaxValue;
            bool found = false;
            RectInt buildingRect = new(originCell, footprintCells);
            int2 clampedUnitFootprint = UnitFootprintUtility.ClampSize(unitFootprint);

            for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
            {
                int minX = originCell.x - extraRadius;
                int minY = originCell.y - extraRadius;
                int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
                int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

                for (int x = minX; x <= maxX; x++)
                {
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                    if (maxY != minY)
                        TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
                }

                for (int y = minY + 1; y < maxY; y++)
                {
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                    if (maxX != minX)
                        TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, buildingRect, clampedUnitFootprint, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
                }

                if (found)
                    return true;
            }

            return false;
        }

        private static void TryScoreBuildingApproachCandidate(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            RectInt buildingRect,
            int2 unitFootprint,
            int2 referenceCell,
            int x,
            int y,
            ref int bestScore,
            ref int2 bestCell,
            ref bool found)
        {
            if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
                return;

            int2 candidateCell = new(x, y);
            int2 candidateMin = UnitFootprintUtility.GetMinCell(candidateCell, unitFootprint);
            RectInt unitRect = new(candidateMin.x, candidateMin.y, unitFootprint.x, unitFootprint.y);
            if (unitRect.Overlaps(buildingRect))
                return;

            if (!UnitFootprintUtility.CanPlace(grid, walkable, blocked, default, occupied, candidateCell, unitFootprint, referenceCell, 0))
                return;

            int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
            if (!found || score < bestScore)
            {
                bestScore = score;
                bestCell = candidateCell;
                found = true;
            }
        }
    }
}
