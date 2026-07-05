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
        private static readonly FixedString64Bytes TrayTruckSourceKey = new("Unit_Veh_Truck_Tray");
        private static readonly FixedString64Bytes TankerTruckSourceKey = new("Unit_Veh_Truck_Tanker");
        private const float AutomaticAssignmentStableRefreshSeconds = 2f;
        private readonly HashSet<Entity> _invalidCapacityWarningEntities = new();
        private uint _lastAutomaticAssignmentSignature;
        private uint _nextReservationId = 1u;
        private float _nextAutomaticAssignmentRefreshAt;
        private bool _hasAutomaticAssignmentSignature;

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

            bool runAutomaticAssignment = ShouldRunAutomaticAssignmentScan(context, em, grid, haulerQuery, now);
            for (int i = 0; i < haulerQuery.Length; i++)
            {
                Entity hauler = haulerQuery[i];
                if (em.HasComponent<UnitResourceHaulOrder>(hauler))
                {
                    UpdateResourceHauler(context, em, grid, hauler, now);
                }
                else
                {
                    ReleaseOrphanedReservation(context, em, hauler);
                    if (runAutomaticAssignment)
                        TryAssignAutomaticHaulerOrder(context, em, grid, hauler);
                }
            }
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
                if (TryFindNearestBuilding(context, clickedBuilding, candidate => context.ResourceHaulerUtilitySystemHelper.HasAvailableFuelForHauler(em, candidate), out source))
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

                UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(unit);
                float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
                ReleaseOrderReservations(context, em, unit);
                if (em.HasComponent<UnitResourceHaulOrder>(unit))
                    em.RemoveComponent<UnitResourceHaulOrder>(unit);
                if (!TryReserveHaulCapacity(context, em, source, destination, resourceKind, loadAmount, out UnitResourceHaulReservation reservation))
                    continue;

                if (!TryIssueHaulerMoveToBuilding(context, em, unit, source, out int2 sourceGoal))
                {
                    ReleaseReservation(context, em, reservation);
                    continue;
                }

                UnitResourceHaulOrder order = context.ResourceHaulerUtilitySystemHelper.CreateOrder(source.Id, destination.Id, sourceGoal, resourceKind);

                if (em.HasComponent<UnitResourceHaulOrder>(unit))
                    em.SetComponentData(unit, order);
                else
                    em.AddComponentData(unit, order);
                SetOrAddResourceHaulReservation(em, unit, reservation);

                assignedAny = true;
            }

            return assignedAny;
        }

        private bool TryAssignAutomaticHaulerOrder(Context context, EntityManager em, GridConfig grid, Entity unit)
        {
            if (context.ResourceHaulerUtilitySystemHelper == null ||
                context.FactionResourceCompositionSystemHelper == null ||
                context.RuntimeBuildings == null ||
                !TryGetAutomaticHaulerKind(em, unit, out ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind))
            {
                return false;
            }

            UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(unit);
            float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
            if (loadAmount <= 0f)
            {
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.HaulerUnavailable,
                    resourceKind);
                return false;
            }

            byte factionId = em.GetComponentData<Faction>(unit).Id;
            int2 unitCell = em.GetComponentData<UnitGrid>(unit).Cell;
            if (!TryFindAutomaticHaulerRoute(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    out RuntimeBuildingEntity source,
                    out RuntimeBuildingEntity destination))
            {
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Blocked,
                    ResolveAutomaticAssignmentBlockReason(context, em, grid, factionId, unitCell, resourceKind, loadAmount),
                    resourceKind);
                return false;
            }

            if (!TryReserveHaulCapacity(context, em, source, destination, resourceKind, loadAmount, out UnitResourceHaulReservation reservation))
            {
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.ReservationFailed,
                    resourceKind);
                return false;
            }

            if (!TryIssueHaulerMoveToBuilding(context, em, unit, source, out int2 sourceGoal))
            {
                ReleaseReservation(context, em, reservation);
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.RouteUnavailable,
                    resourceKind);
                return false;
            }

            UnitResourceHaulOrder order = context.ResourceHaulerUtilitySystemHelper.CreateOrder(
                source.Id,
                destination.Id,
                sourceGoal,
                resourceKind);
            em.AddComponentData(unit, order);
            SetOrAddResourceHaulReservation(em, unit, reservation);
            SetResourceHaulStatus(
                em,
                unit,
                FuelLogisticsTaskStatusCode.Assigned,
                FuelLogisticsBlockReasonCode.None,
                resourceKind);
            return true;
        }

        private bool ShouldRunAutomaticAssignmentScan(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers,
            float now)
        {
            if (_hasAutomaticAssignmentSignature && now < _nextAutomaticAssignmentRefreshAt)
                return false;

            if (context.ResourceHaulerUtilitySystemHelper == null ||
                context.FactionResourceCompositionSystemHelper == null ||
                context.RuntimeBuildings == null ||
                haulers.Length == 0)
            {
                return false;
            }

            uint signature = CalculateAutomaticAssignmentSignature(context, em, grid, haulers);
            if (signature == 0u)
            {
                _hasAutomaticAssignmentSignature = false;
                _lastAutomaticAssignmentSignature = 0u;
                _nextAutomaticAssignmentRefreshAt = 0f;
                return false;
            }

            if (_hasAutomaticAssignmentSignature &&
                signature == _lastAutomaticAssignmentSignature &&
                now < _nextAutomaticAssignmentRefreshAt)
            {
                return false;
            }

            _hasAutomaticAssignmentSignature = true;
            _lastAutomaticAssignmentSignature = signature;
            _nextAutomaticAssignmentRefreshAt = now + AutomaticAssignmentStableRefreshSeconds;
            return true;
        }

        private static uint CalculateAutomaticAssignmentSignature(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers)
        {
            uint hash = 2166136261u;
            int idleCandidateCount = 0;
            for (int i = 0; i < haulers.Length; i++)
            {
                Entity haulerEntity = haulers[i];
                if (em.HasComponent<UnitResourceHaulOrder>(haulerEntity) ||
                    !TryGetAutomaticHaulerKind(em, haulerEntity, out ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind))
                {
                    continue;
                }

                UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(haulerEntity);
                float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
                if (loadAmount <= 0f)
                    continue;

                idleCandidateCount++;
                hash = AppendHash(hash, haulerEntity.Index);
                hash = AppendHash(hash, haulerEntity.Version);
                hash = AppendHash(hash, (int)resourceKind);
                hash = AppendHash(hash, em.GetComponentData<Faction>(haulerEntity).Id);
                hash = AppendHash(hash, QuantizeResource(loadAmount));
                if (em.HasComponent<UnitGrid>(haulerEntity))
                {
                    int2 cell = em.GetComponentData<UnitGrid>(haulerEntity).Cell;
                    hash = AppendHash(hash, cell.x);
                    hash = AppendHash(hash, cell.y);
                }
            }

            if (idleCandidateCount == 0)
                return 0u;

            hash = AppendHash(hash, idleCandidateCount);
            hash = AppendHash(hash, grid.Width);
            hash = AppendHash(hash, grid.Height);
            hash = AppendHash(hash, QuantizeResource(grid.CellSize));
            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null || building.IsDestroyed || !building.HasOwnerFaction)
                    continue;
                if (building.OilStorageCapacity <= 0 &&
                    building.FuelStorageCapacity <= 0 &&
                    building.OilBarrelsPerDay <= 0f &&
                    building.FuelBarrelsPerDay <= 0f)
                {
                    continue;
                }

                hash = AppendHash(hash, building.Id);
                hash = AppendHash(hash, building.OwnerFactionId);
                hash = AppendHash(hash, building.OilStorageCapacity);
                hash = AppendHash(hash, building.FuelStorageCapacity);
                hash = AppendHash(hash, QuantizeResource(building.OilBarrelsPerDay));
                hash = AppendHash(hash, QuantizeResource(building.FuelBarrelsPerDay));
                hash = AppendHash(hash, QuantizeResource(context.ResourceHaulerUtilitySystemHelper.GetStoredResource(
                    em,
                    building,
                    ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)));
                hash = AppendHash(hash, QuantizeResource(context.ResourceHaulerUtilitySystemHelper.GetStoredResource(
                    em,
                    building,
                    ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel)));
                if (building.CombatEntity != Entity.Null &&
                    em.Exists(building.CombatEntity) &&
                    em.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
                {
                    BuildingResourceStorageComponent storage = em.GetComponentData<BuildingResourceStorageComponent>(building.CombatEntity);
                    hash = AppendHash(hash, (int)storage.Version);
                    hash = AppendHash(hash, QuantizeResource(storage.ReservedOilInboundBarrels));
                    hash = AppendHash(hash, QuantizeResource(storage.ReservedOilOutboundBarrels));
                    hash = AppendHash(hash, QuantizeResource(storage.ReservedFuelInboundBarrels));
                    hash = AppendHash(hash, QuantizeResource(storage.ReservedFuelOutboundBarrels));
                }
            }

            return hash;
        }

#if UNITY_INCLUDE_TESTS
        internal bool ShouldRunAutomaticAssignmentScanForTests(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers,
            float now)
        {
            return ShouldRunAutomaticAssignmentScan(context, em, grid, haulers, now);
        }

        internal static uint CalculateAutomaticAssignmentSignatureForTests(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers)
        {
            return CalculateAutomaticAssignmentSignature(context, em, grid, haulers);
        }
#endif

        private static bool TryGetAutomaticHaulerKind(
            EntityManager em,
            Entity unit,
            out ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            resourceKind = default;
            if (!em.Exists(unit) ||
                !em.HasComponent<UnitResourceHauler>(unit) ||
                !em.HasComponent<UnitSourcePrefabKey>(unit) ||
                !em.HasComponent<Faction>(unit) ||
                !em.HasComponent<UnitGrid>(unit) ||
                em.HasComponent<UnitAirMovement>(unit))
            {
                return false;
            }

            UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(unit);
            if (sourceKey.Value.Equals(TrayTruckSourceKey))
            {
                resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
                return true;
            }

            if (sourceKey.Value.Equals(TankerTruckSourceKey))
            {
                resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel;
                return true;
            }

            return false;
        }

        private static uint AppendHash(uint hash, int value)
        {
            unchecked
            {
                return (hash ^ (uint)value) * 16777619u;
            }
        }

        private static int QuantizeResource(float value)
        {
            return Mathf.RoundToInt(Mathf.Max(0f, value) * 100f);
        }

        private static bool TryFindAutomaticHaulerRoute(
            Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out RuntimeBuildingEntity source,
            out RuntimeBuildingEntity destination)
        {
            source = null;
            destination = null;
            if (context.RuntimeBuildings == null)
                return false;

            if (!TryFindNearestAutomaticSourceToCell(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    out source))
            {
                return false;
            }

            return TryFindNearestAutomaticDestination(
                context,
                em,
                source,
                factionId,
                resourceKind,
                loadAmount,
                out destination);
        }

        private static FuelLogisticsBlockReasonCode ResolveAutomaticAssignmentBlockReason(
            Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount)
        {
            if (!TryFindNearestAutomaticSourceToCell(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    out RuntimeBuildingEntity source))
            {
                return FuelLogisticsBlockReasonCode.SourceUnavailable;
            }

            if (!HasAutomaticDestinationCandidate(context, em, source, factionId, resourceKind))
                return FuelLogisticsBlockReasonCode.DestinationUnavailable;

            if (!TryFindNearestAutomaticDestination(
                    context,
                    em,
                    source,
                    factionId,
                    resourceKind,
                    loadAmount,
                    out _))
            {
                return FuelLogisticsBlockReasonCode.DestinationFull;
            }

            return FuelLogisticsBlockReasonCode.RouteUnavailable;
        }

#if UNITY_INCLUDE_TESTS
        internal static bool TryFindAutomaticHaulerRouteForTests(
            Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 unitCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out RuntimeBuildingEntity source,
            out RuntimeBuildingEntity destination)
        {
            return TryFindAutomaticHaulerRoute(
                context,
                em,
                grid,
                factionId,
                unitCell,
                resourceKind,
                loadAmount,
                out source,
                out destination);
        }
#endif

        private static bool TryFindNearestAutomaticSourceToCell(
            Context context,
            EntityManager em,
            GridConfig grid,
            byte factionId,
            int2 originCell,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out RuntimeBuildingEntity result)
        {
            result = null;
            if (context.RuntimeBuildings == null || context.ResolveBuildingFocusWorldPosition == null)
                return false;

            Vector3 origin = grid.Origin + new float3(
                (originCell.x + 0.5f) * grid.CellSize,
                0f,
                (originCell.y + 0.5f) * grid.CellSize);
            float bestDistanceSq = float.MaxValue;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate.IsDestroyed ||
                    !IsAutomaticSource(context, em, candidate, factionId, resourceKind, loadAmount))
                {
                    continue;
                }

                Vector3 candidatePosition = context.ResolveBuildingFocusWorldPosition(candidate);
                float distanceSq = (candidatePosition - origin).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                result = candidate;
            }

            return result != null;
        }

        private static bool TryFindNearestAutomaticDestination(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out RuntimeBuildingEntity result)
        {
            result = null;
            if (source == null || context.RuntimeBuildings == null || context.ResolveBuildingFocusWorldPosition == null)
                return false;

            Vector3 origin = context.ResolveBuildingFocusWorldPosition(source);
            float bestDistanceSq = float.MaxValue;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate == source ||
                    candidate.IsDestroyed ||
                    !IsAutomaticDestination(context, em, candidate, factionId, resourceKind, loadAmount))
                {
                    continue;
                }

                Vector3 candidatePosition = context.ResolveBuildingFocusWorldPosition(candidate);
                float distanceSq = (candidatePosition - origin).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                result = candidate;
            }

            return result != null;
        }

        private static bool IsAutomaticSource(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount)
        {
            if (!IsSameFactionResourceBuilding(candidate, factionId))
                return false;

            if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
            {
                return context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(candidate) &&
                       context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(
                           em,
                           candidate,
                           resourceKind,
                           loadAmount);
            }

            return context.ResourceHaulerUtilitySystemHelper.IsFuelStorageSourceBuilding(candidate) &&
                   context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(
                       em,
                       candidate,
                       resourceKind,
                       loadAmount);
        }

        private static bool IsAutomaticDestination(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity candidate,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount)
        {
            if (!IsSameFactionResourceBuilding(candidate, factionId))
                return false;

            if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
            {
                return context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(candidate) &&
                       context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(
                           em,
                           candidate,
                           resourceKind,
                           loadAmount);
            }

            return context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(candidate) &&
                   candidate.FuelStorageCapacity > 0 &&
                   context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(
                       em,
                       candidate,
                       resourceKind,
                       loadAmount);
        }

        private static bool HasAutomaticDestinationCandidate(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            byte factionId,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            if (source == null || context.RuntimeBuildings == null)
                return false;

            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity candidate = pair.Value;
                if (candidate == null ||
                    candidate == source ||
                    candidate.IsDestroyed ||
                    !IsSameFactionResourceBuilding(candidate, factionId))
                {
                    continue;
                }

                if (resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
                {
                    if (context.ResourceHaulerUtilitySystemHelper.IsFuelBuilding(candidate))
                        return true;
                    continue;
                }

                if (context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(candidate) &&
                    candidate.FuelStorageCapacity > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameFactionResourceBuilding(RuntimeBuildingEntity building, byte factionId)
        {
            return building != null &&
                   !building.IsDestroyed &&
                   building.HasOwnerFaction &&
                   building.OwnerFactionId == factionId;
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
                ReleaseOrderReservations(context, em, entity);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.DestinationUnavailable,
                    resourceKind);
                return;
            }

            if (source == null || source.IsDestroyed || destination == null || destination.IsDestroyed)
            {
                ReleaseOrderReservations(context, em, entity);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    source == null || source.IsDestroyed
                        ? FuelLogisticsBlockReasonCode.SourceUnavailable
                        : FuelLogisticsBlockReasonCode.DestinationUnavailable,
                    resourceKind);
                return;
            }

            int2 currentCell = em.GetComponentData<UnitGrid>(entity).Cell;
            switch ((ResourceHaulerUtilitySystemHelper.ResourceHaulPhase)order.Phase)
            {
                case ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.None:
                    UpdateNonePhase(context, em, entity, source, destination, resourceKind, hauler, ref order);
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

        private void UpdateNonePhase(
            Context context,
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            UnitResourceHauler hauler,
            ref UnitResourceHaulOrder order)
        {
            float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
            if (!TryGetReservation(em, entity, out UnitResourceHaulReservation reservation) ||
                reservation.SourceReservationActive == 0 ||
                reservation.DestinationReservationActive == 0)
            {
                ReleaseOrderReservations(context, em, entity);
                if (!TryReserveHaulCapacity(context, em, source, destination, resourceKind, loadAmount, out reservation))
                    return;
                SetOrAddResourceHaulReservation(em, entity, reservation);
            }

            if (!TryIssueHaulerMoveToBuilding(context, em, entity, source, out int2 goal))
            {
                ReleaseOrderReservations(context, em, entity);
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, goal);
            SetOrAddResourceHaulOrder(em, entity, order);
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
                    if (TryIssueHaulerMoveToBuilding(context, em, entity, source, out _))
                        SetOrAddResourceHaulOrder(em, entity, order);
                }
                return;
            }

            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} arrived-source source={source.Id} current={currentCell}");
            context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Loading);
            SetOrAddResourceHaulOrder(em, entity, order);
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
                ReleaseOrderReservations(context, em, entity);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                return;
            }

            _invalidCapacityWarningEntities.Remove(entity);
            float sourceStored = context.ResourceHaulerUtilitySystemHelper.GetStoredResource(em, source, resourceKind);
            float currentCargo = resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel ? hauler.CargoFuelBarrels : hauler.CargoOilBarrels;
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} phase=Loading resource={resourceKind} current={em.GetComponentData<UnitGrid>(entity).Cell} source={source.Id} stored={sourceStored:0.##} cargo={currentCargo:0.##}/{loadAmount:0.##} actionEndsAt={order.ActionEndsAt:0.##} now={now:0.##}");
            bool hasSourceReservation = TryGetReservation(em, entity, out UnitResourceHaulReservation reservation) &&
                                        reservation.SourceReservationActive != 0 &&
                                        reservation.ReservedBarrels + 0.001f >= loadAmount;
            if (!hasSourceReservation && !context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(em, source, resourceKind, loadAmount))
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
            hasSourceReservation = TryGetReservation(em, entity, out reservation) &&
                                   reservation.SourceReservationActive != 0 &&
                                   reservation.ReservedBarrels + 0.001f >= loadAmount;
            if (!hasSourceReservation && !context.ResourceHaulerUtilitySystemHelper.HasEnoughSourceResource(em, source, resourceKind, loadAmount))
            {
                context.ResourceHaulerUtilitySystemHelper.ResetActionTimer(ref order);
                em.SetComponentData(entity, order);
                if (VerboseResourceHaulerLogs)
                    Debug.Log($"[ResourceHauler] entity={entity} loading-reset-insufficient-resource resource={resourceKind} source={source.Id} stored={sourceStored:0.##} need={loadAmount:0.##}");
                return;
            }

            if (hasSourceReservation)
                ReleaseSourceReservationForOrder(context, em, entity, source, resourceKind, loadAmount);
            if (!context.ResourceHaulerUtilitySystemHelper.TryCompleteLoad(em, source, resourceKind, loadAmount, ref hauler))
                return;
            em.SetComponentData(entity, hauler);
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} loading-complete resource={resourceKind} source={source.Id} loaded={loadAmount:0.##}");

            if (!TryIssueHaulerMoveToBuilding(context, em, entity, destination, out int2 destinationGoal))
            {
                ReleaseDestinationReservationForOrder(context, em, entity, destination, resourceKind, loadAmount);
                context.ResourceHaulerUtilitySystemHelper.RevertLoad(em, source, resourceKind, loadAmount, ref hauler);
                em.SetComponentData(entity, hauler);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                if (VerboseResourceHaulerLogs)
                    Debug.LogWarning($"[ResourceHauler] entity={entity} failed-destination-move destination={destination.Id} revertedLoad={loadAmount:0.##}");
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, destinationGoal);
            SetOrAddResourceHaulOrder(em, entity, order);
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
                {
                    if (TryIssueHaulerMoveToBuilding(context, em, entity, destination, out _))
                        SetOrAddResourceHaulOrder(em, entity, order);
                }
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
                ReleaseOrderReservations(context, em, entity);
                context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.None);
                em.SetComponentData(entity, order);
                return;
            }

            bool hasDestinationReservation = TryGetReservation(em, entity, out UnitResourceHaulReservation reservation) &&
                                             reservation.DestinationReservationActive != 0 &&
                                             reservation.ReservedBarrels + 0.001f >= cargo;
            if (!hasDestinationReservation && !context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(em, destination, resourceKind, cargo))
                return;

            ResourceHaulerUtilitySystemHelper.TimedActionState unloadTimer = context.ResourceHaulerUtilitySystemHelper.AdvanceTimedAction(ref order, now, hauler.UnloadDurationSeconds);
            if (unloadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Started ||
                unloadTimer == ResourceHaulerUtilitySystemHelper.TimedActionState.Waiting)
            {
                em.SetComponentData(entity, order);
                return;
            }

            hasDestinationReservation = TryGetReservation(em, entity, out reservation) &&
                                        reservation.DestinationReservationActive != 0 &&
                                        reservation.ReservedBarrels + 0.001f >= cargo;
            if (!hasDestinationReservation && !context.ResourceHaulerUtilitySystemHelper.HasReceivingCapacity(em, destination, resourceKind, cargo))
            {
                context.ResourceHaulerUtilitySystemHelper.ResetActionTimer(ref order);
                em.SetComponentData(entity, order);
                return;
            }

            if (hasDestinationReservation)
                ReleaseDestinationReservationForOrder(context, em, entity, destination, resourceKind, cargo);
            if (!context.ResourceHaulerUtilitySystemHelper.TryCompleteUnload(em, destination, resourceKind, ref hauler))
                return;
            em.SetComponentData(entity, hauler);
            if (em.HasComponent<UnitResourceHaulReservation>(entity))
                em.RemoveComponent<UnitResourceHaulReservation>(entity);
            em.RemoveComponent<UnitResourceHaulOrder>(entity);
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

        private static void SetOrAddResourceHaulOrder(EntityManager em, Entity entity, UnitResourceHaulOrder order)
        {
            if (em.HasComponent<UnitResourceHaulOrder>(entity))
                em.SetComponentData(entity, order);
            else
                em.AddComponentData(entity, order);
        }

        private static void SetOrAddResourceHaulReservation(EntityManager em, Entity entity, UnitResourceHaulReservation reservation)
        {
            if (em.HasComponent<UnitResourceHaulReservation>(entity))
                em.SetComponentData(entity, reservation);
            else
                em.AddComponentData(entity, reservation);
        }

        private static void SetResourceHaulStatus(
            EntityManager em,
            Entity entity,
            FuelLogisticsTaskStatusCode statusCode,
            FuelLogisticsBlockReasonCode reasonCode,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            UnitResourceHaulStatus status = new()
            {
                StatusCode = (byte)statusCode,
                ReasonCode = (byte)reasonCode,
                ResourceKind = (byte)resourceKind
            };
            if (em.HasComponent<UnitResourceHaulStatus>(entity))
                em.SetComponentData(entity, status);
            else
                em.AddComponentData(entity, status);
        }

        private bool TryReserveHaulCapacity(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out UnitResourceHaulReservation reservation)
        {
            reservation = default;
            if (context.ResourceHaulerUtilitySystemHelper == null || source == null || destination == null || loadAmount <= 0f)
                return false;

            if (!context.ResourceHaulerUtilitySystemHelper.TryReserveSource(em, source, resourceKind, loadAmount))
                return false;

            if (!context.ResourceHaulerUtilitySystemHelper.TryReserveDestination(em, destination, resourceKind, loadAmount))
            {
                context.ResourceHaulerUtilitySystemHelper.ReleaseSourceReservation(em, source, resourceKind, loadAmount);
                return false;
            }

            reservation = new UnitResourceHaulReservation
            {
                SourceBuildingId = source.Id,
                DestinationBuildingId = destination.Id,
                ReservedBarrels = loadAmount,
                ResourceKind = (byte)resourceKind,
                SourceReservationActive = 1,
                DestinationReservationActive = 1,
                ReservationId = NextReservationId()
            };
            return true;
        }

#if UNITY_INCLUDE_TESTS
        internal bool TryReserveHaulCapacityForTests(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            out UnitResourceHaulReservation reservation)
        {
            return TryReserveHaulCapacity(context, em, source, destination, resourceKind, loadAmount, out reservation);
        }
#endif

        private uint NextReservationId()
        {
            uint id = _nextReservationId;
            _nextReservationId = _nextReservationId == uint.MaxValue ? 1u : _nextReservationId + 1u;
            return id == 0u ? 1u : id;
        }

        private static bool TryGetReservation(EntityManager em, Entity entity, out UnitResourceHaulReservation reservation)
        {
            reservation = default;
            if (!em.Exists(entity) || !em.HasComponent<UnitResourceHaulReservation>(entity))
                return false;

            reservation = em.GetComponentData<UnitResourceHaulReservation>(entity);
            return reservation.ReservedBarrels > 0f;
        }

        private static void ReleaseOrphanedReservation(Context context, EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) ||
                em.HasComponent<UnitResourceHaulOrder>(entity) ||
                !em.HasComponent<UnitResourceHaulReservation>(entity))
            {
                return;
            }

            ReleaseOrderReservations(context, em, entity);
        }

        private static void ReleaseOrderReservations(Context context, EntityManager em, Entity entity)
        {
            if (!TryGetReservation(em, entity, out UnitResourceHaulReservation reservation))
                return;

            ReleaseReservation(context, em, reservation);
            if (em.Exists(entity) && em.HasComponent<UnitResourceHaulReservation>(entity))
                em.RemoveComponent<UnitResourceHaulReservation>(entity);
        }

        private static void ReleaseReservation(Context context, EntityManager em, UnitResourceHaulReservation reservation)
        {
            if (context.ResourceHaulerUtilitySystemHelper == null)
                return;

            var resourceKind = (ResourceHaulerUtilitySystemHelper.ResourceHaulKind)reservation.ResourceKind;
            if (reservation.SourceReservationActive != 0 &&
                TryResolveReservationBuilding(context, reservation.SourceBuildingId, out RuntimeBuildingEntity source))
            {
                context.ResourceHaulerUtilitySystemHelper.ReleaseSourceReservation(
                    em,
                    source,
                    resourceKind,
                    reservation.ReservedBarrels);
            }

            if (reservation.DestinationReservationActive != 0 &&
                TryResolveReservationBuilding(context, reservation.DestinationBuildingId, out RuntimeBuildingEntity destination))
            {
                context.ResourceHaulerUtilitySystemHelper.ReleaseDestinationReservation(
                    em,
                    destination,
                    resourceKind,
                    reservation.ReservedBarrels);
            }
        }

        private static bool TryResolveReservationBuilding(
            Context context,
            int buildingId,
            out RuntimeBuildingEntity building)
        {
            building = null;
            if (context.TryGetRuntimeBuilding != null && context.TryGetRuntimeBuilding(buildingId, out building))
                return true;

            return context.RuntimeBuildings != null &&
                   context.RuntimeBuildings.TryGetValue(buildingId, out building) &&
                   building != null;
        }

        private static void ReleaseSourceReservationForOrder(
            Context context,
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity source,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float amount)
        {
            if (!TryGetReservation(em, entity, out UnitResourceHaulReservation reservation) ||
                reservation.SourceReservationActive == 0)
            {
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.ReleaseSourceReservation(em, source, resourceKind, amount);
            reservation.SourceReservationActive = 0;
            SetOrAddResourceHaulReservation(em, entity, reservation);
        }

        private static void ReleaseDestinationReservationForOrder(
            Context context,
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float amount)
        {
            if (!TryGetReservation(em, entity, out UnitResourceHaulReservation reservation) ||
                reservation.DestinationReservationActive == 0)
            {
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.ReleaseDestinationReservation(em, destination, resourceKind, amount);
            reservation.DestinationReservationActive = 0;
            SetOrAddResourceHaulReservation(em, entity, reservation);
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
