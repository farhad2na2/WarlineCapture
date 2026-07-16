using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using RoutePolicy = Game.Runtime.ResourceHaulerAutomaticRoutePolicySystemHelper;

namespace Game.Runtime
{
    internal sealed class BuildingResourceHaulerBridgeCompositionSystemHelper
    {
        private static readonly bool VerboseResourceHaulerLogs = false;
        private static readonly FixedString64Bytes TrayTruckSourceKey = new("Unit_Veh_Truck_Tray");
        private static readonly FixedString64Bytes TankerTruckSourceKey = new("Unit_Veh_Truck_Tanker");
        private const float AutomaticAssignmentStableRefreshSeconds = 2f;
        private readonly List<Entity> _haulerEntities = new();
        private readonly HashSet<Entity> _invalidCapacityWarningEntities = new();
        private readonly ResourceHaulerAIOilAllocationPolicySystemHelper _aiOilAllocationPolicy = new();
        private readonly FactionFuelLogisticsTelemetryBridgeCompositionSystemHelper _fuelLogisticsTelemetry = new();
        private readonly WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent> _moveOrderQueueQueryCache = new(readOnly: true);
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
        public delegate bool TryResolveFactionAIOilAllocationInputDelegate(
            EntityManager entityManager,
            byte factionId,
            out FactionAIOilAllocationInput input);

        internal readonly struct FactionAIOilAllocationInput
        {
            public readonly int PlannedMaterialsCost;
            public readonly int AvailableMaterials;
            public readonly int MaterialsCapacity;
            public readonly float StoredFuelBarrels;
            public readonly int FuelStorageCapacity;

            public FactionAIOilAllocationInput(
                int plannedMaterialsCost,
                int availableMaterials,
                int materialsCapacity,
                float storedFuelBarrels,
                int fuelStorageCapacity)
            {
                PlannedMaterialsCost = math.max(0, plannedMaterialsCost);
                AvailableMaterials = math.max(0, availableMaterials);
                MaterialsCapacity = math.max(0, materialsCapacity);
                StoredFuelBarrels = math.max(0f, storedFuelBarrels);
                FuelStorageCapacity = math.max(0, fuelStorageCapacity);
            }
        }

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
            public readonly TryResolveFactionAIOilAllocationInputDelegate TryResolveFactionAIOilAllocationInput;

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
                GetEffectivePlacementRectDelegate getEffectivePlacementRect,
                TryResolveFactionAIOilAllocationInputDelegate tryResolveFactionAIOilAllocationInput = null)
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
                TryResolveFactionAIOilAllocationInput = tryResolveFactionAIOilAllocationInput;
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
            if (haulerUnitsQuery.IsEmptyIgnoreFilter)
                return;

            int haulerCount = haulerUnitsQuery.CalculateEntityCount();
            if (haulerCount == 0)
                return;

            _haulerEntities.Clear();
            if (_haulerEntities.Capacity < haulerCount)
                _haulerEntities.Capacity = haulerCount;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using (NativeArray<ArchetypeChunk> chunks = haulerUnitsQuery.ToArchetypeChunkArray(Allocator.Temp))
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                        _haulerEntities.Add(entities[entityIndex]);
                }
            }

            bool runAutomaticAssignment = ShouldRunAutomaticAssignmentScan(context, em, grid, _haulerEntities, now);
            if (runAutomaticAssignment)
                _aiOilAllocationPolicy.ClearInputCache();
            for (int i = 0; i < _haulerEntities.Count; i++)
            {
                Entity hauler = _haulerEntities[i];
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
            bool clickedIsFabricationInput =
                ResourceHaulerAutomaticRoutePolicySystemHelper.IsEnabledMaterialFabricationInput(em, clickedBuilding);
            bool clickedIsStorage = context.FactionResourceCompositionSystemHelper.IsResourceStorageBuilding(clickedBuilding);
            if (!clickedIsOilSource && !clickedIsFuelBuilding && !clickedIsFabricationInput && !clickedIsStorage)
                return false;

            RuntimeBuildingEntity source = clickedBuilding;
            RuntimeBuildingEntity destination = clickedBuilding;
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
            if (clickedIsOilSource)
            {
                if (!TryFindNearestBuilding(
                        context,
                        clickedBuilding,
                        candidate => ResourceHaulerAutomaticRoutePolicySystemHelper.IsAutomaticOilDestination(
                            context,
                            em,
                            candidate),
                        out destination))
                    return false;
                resourceKind = ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil;
            }
            else if (clickedIsFuelBuilding || clickedIsFabricationInput)
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
                bool hadActiveOrder = em.HasComponent<UnitResourceHaulOrder>(unit);
                UnitResourceHaulOrder previousOrder = hadActiveOrder
                    ? em.GetComponentData<UnitResourceHaulOrder>(unit)
                    : default;
                bool isSameRoute = hadActiveOrder &&
                                   previousOrder.SourceBuildingId == source.Id &&
                                   previousOrder.DestinationBuildingId == destination.Id &&
                                   previousOrder.ResourceKind == (byte)resourceKind;
                ReleaseOrderReservations(context, em, unit);
                if (hadActiveOrder)
                    em.RemoveComponent<UnitResourceHaulOrder>(unit);
                if (loadAmount <= 0f)
                {
                    SetResourceHaulStatus(
                        em,
                        unit,
                        FuelLogisticsTaskStatusCode.Blocked,
                        FuelLogisticsBlockReasonCode.HaulerUnavailable,
                        resourceKind);
                    continue;
                }
                if (!TryReserveHaulCapacity(context, em, source, destination, resourceKind, loadAmount, out UnitResourceHaulReservation reservation))
                {
                    SetResourceHaulStatus(
                        em,
                        unit,
                        FuelLogisticsTaskStatusCode.Blocked,
                        FuelLogisticsBlockReasonCode.ReservationFailed,
                        resourceKind);
                    continue;
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
                    continue;
                }

                UnitResourceHaulOrder order = context.ResourceHaulerUtilitySystemHelper.CreateOrder(source.Id, destination.Id, sourceGoal, resourceKind);

                if (em.HasComponent<UnitResourceHaulOrder>(unit))
                    em.SetComponentData(unit, order);
                else
                    em.AddComponentData(unit, order);
                SetOrAddResourceHaulReservation(em, unit, reservation);
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Assigned,
                    FuelLogisticsBlockReasonCode.None,
                    resourceKind);
                if (!isSameRoute)
                    RecordRouteAssignmentTelemetry(em, unit, resourceKind, hadActiveOrder);

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
            FactionAIOilAllocationInput aiInput = default;
            bool hasAIInput = resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil &&
                              _aiOilAllocationPolicy.TryResolveCachedInput(
                                  context.TryResolveFactionAIOilAllocationInput,
                                  em,
                                  factionId,
                                  out aiInput);
            if (!ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindAutomaticHaulerRoute(
                    context,
                    em,
                    grid,
                    factionId,
                    unitCell,
                    resourceKind,
                    loadAmount,
                    hasAIInput,
                    aiInput,
                    out RuntimeBuildingEntity source,
                    out RuntimeBuildingEntity destination))
            {
                SetResourceHaulStatus(
                    em,
                    unit,
                    FuelLogisticsTaskStatusCode.Blocked,
                    ResourceHaulerAutomaticRoutePolicySystemHelper.ResolveAutomaticAssignmentBlockReason(
                        context,
                        em,
                        grid,
                        factionId,
                        unitCell,
                        resourceKind,
                        loadAmount,
                        hasAIInput,
                        aiInput),
                    resourceKind);
                return false;
            }

            bool allowDeferredSourceReservation = resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil &&
                                                  context.ResourceHaulerUtilitySystemHelper.IsOilSourceBuilding(source);
            if (!TryReserveHaulCapacity(
                    context,
                    em,
                    source,
                    destination,
                    resourceKind,
                    loadAmount,
                    allowDeferredSourceReservation,
                    out UnitResourceHaulReservation reservation))
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
            RecordRouteAssignmentTelemetry(em, unit, resourceKind, isReassignment: false);
            return true;
        }

        private bool ShouldRunAutomaticAssignmentScan(
            Context context,
            EntityManager em,
            GridConfig grid,
            List<Entity> haulers,
            float now)
        {
            if (!CanEvaluateAutomaticAssignmentSignature(context, haulers.Count, now))
                return false;

            uint signature = CalculateAutomaticAssignmentSignature(context, em, grid, haulers);
            return ApplyAutomaticAssignmentSignature(signature, now);
        }

        private bool CanEvaluateAutomaticAssignmentSignature(Context context, int haulerCount, float now)
        {
            if (_hasAutomaticAssignmentSignature && now < _nextAutomaticAssignmentRefreshAt)
                return false;

            return context.ResourceHaulerUtilitySystemHelper != null &&
                   context.FactionResourceCompositionSystemHelper != null &&
                   context.RuntimeBuildings != null &&
                   haulerCount > 0;
        }

        private bool ApplyAutomaticAssignmentSignature(uint signature, float now)
        {
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

#if UNITY_INCLUDE_TESTS
        internal void ResetAIOilAllocationInputCacheForTests()
        {
            _aiOilAllocationPolicy.ClearInputCache();
        }

        internal bool TryResolveCachedFactionAIOilAllocationInputForTests(
            Context context,
            EntityManager em,
            byte factionId,
            out FactionAIOilAllocationInput input)
        {
            return _aiOilAllocationPolicy.TryResolveCachedInput(
                context.TryResolveFactionAIOilAllocationInput,
                em,
                factionId,
                out input);
        }
#endif

        private static uint CalculateAutomaticAssignmentSignature(
            Context context,
            EntityManager em,
            GridConfig grid,
            List<Entity> haulers)
        {
            uint hash = 2166136261u;
            int idleCandidateCount = 0;
            for (int i = 0; i < haulers.Count; i++)
                AccumulateAutomaticHaulerSignature(context, em, haulers[i], ref hash, ref idleCandidateCount);

            return FinalizeAutomaticAssignmentSignature(context, em, grid, hash, idleCandidateCount);
        }

        private static void AccumulateAutomaticHaulerSignature(
            Context context,
            EntityManager em,
            Entity haulerEntity,
            ref uint hash,
            ref int idleCandidateCount)
        {
            if (em.HasComponent<UnitResourceHaulOrder>(haulerEntity) ||
                !TryGetAutomaticHaulerKind(em, haulerEntity, out ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind))
            {
                return;
            }

            UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(haulerEntity);
            float loadAmount = context.ResourceHaulerUtilitySystemHelper.GetLoadAmount(hauler);
            if (loadAmount <= 0f)
                return;

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

        private static uint FinalizeAutomaticAssignmentSignature(
            Context context,
            EntityManager em,
            GridConfig grid,
            uint hash,
            int idleCandidateCount)
        {

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
                if (ResourceHaulerAutomaticRoutePolicySystemHelper.TryGetMaterialFabrication(
                        em,
                        building,
                        out MaterialFabricationComponent fabrication))
                {
                    hash = AppendHash(hash, fabrication.ProductionEnabled);
                    hash = AppendHash(hash, (int)fabrication.Version);
                }
            }

            return hash;
        }

#if UNITY_INCLUDE_TESTS
        private static uint CalculateAutomaticAssignmentSignature(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeArray<Entity> haulers)
        {
            uint hash = 2166136261u;
            int idleCandidateCount = 0;
            for (int i = 0; i < haulers.Length; i++)
                AccumulateAutomaticHaulerSignature(context, em, haulers[i], ref hash, ref idleCandidateCount);

            return FinalizeAutomaticAssignmentSignature(context, em, grid, hash, idleCandidateCount);
        }

        internal bool ShouldRunAutomaticAssignmentScanForTests(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers,
            float now)
        {
            if (!CanEvaluateAutomaticAssignmentSignature(context, haulers.Length, now))
                return false;

            uint signature = CalculateAutomaticAssignmentSignature(context, em, grid, haulers.AsArray());
            return ApplyAutomaticAssignmentSignature(signature, now);
        }

        internal static uint CalculateAutomaticAssignmentSignatureForTests(
            Context context,
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> haulers)
        {
            return CalculateAutomaticAssignmentSignature(context, em, grid, haulers.AsArray());
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
                em.HasComponent<ManualMoveOrderTag>(unit) ||
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
            FactionAIOilAllocationInput aiInput = default;
            bool hasAIInput = resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil &&
                              context.TryResolveFactionAIOilAllocationInput != null &&
                              context.TryResolveFactionAIOilAllocationInput(em, factionId, out aiInput);
            return ResourceHaulerAutomaticRoutePolicySystemHelper.TryFindAutomaticHaulerRoute(
                context,
                em,
                grid,
                factionId,
                unitCell,
                resourceKind,
                loadAmount,
                hasAIInput,
                aiInput,
                out source,
                out destination);
        }
#endif

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
            if (context.GetEffectivePlacementRect == null)
                return false;

            RectInt buildingRect = context.GetEffectivePlacementRect(building, grid);
            return TryFindBuildingApproachCell(
                grid,
                walkable,
                blockerData.Blocked,
                occupied,
                buildingRect,
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

            if (IsHaulerDeadOrUnavailable(em, entity))
            {
                ReleaseOrderReservations(context, em, entity);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.HaulerUnavailable,
                    resourceKind);
                return;
            }

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

            byte haulerFactionId = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id
                : byte.MaxValue;
            if (!RoutePolicy.IsSameFactionResourceBuilding(source, haulerFactionId) ||
                !RoutePolicy.IsSameFactionResourceBuilding(destination, haulerFactionId))
            {
                ReleaseOrderReservations(context, em, entity);
                em.RemoveComponent<UnitResourceHaulOrder>(entity);
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    !RoutePolicy.IsSameFactionResourceBuilding(source, haulerFactionId)
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

        private void UpdateTravelToSourcePhase(
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
                if (!HasActiveGoalOrPathRequest(em, entity, order.TargetCell))
                {
                    if (VerboseResourceHaulerLogs)
                        Debug.Log($"[ResourceHauler] entity={entity} reissuing-source-move source={source.Id}");
                    if (TryIssueHaulerMoveToBuilding(context, em, entity, source, out int2 sourceGoal))
                    {
                        context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, sourceGoal);
                        SetOrAddResourceHaulOrder(em, entity, order);
                    }
                    else
                    {
                        ReleaseOrderReservations(context, em, entity);
                        em.RemoveComponent<UnitResourceHaulOrder>(entity);
                        SetResourceHaulStatus(
                            em,
                            entity,
                            FuelLogisticsTaskStatusCode.Blocked,
                            FuelLogisticsBlockReasonCode.RouteUnavailable,
                            (ResourceHaulerUtilitySystemHelper.ResourceHaulKind)order.ResourceKind);
                    }
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
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.HaulerUnavailable,
                    resourceKind);
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
                SetResourceHaulStatus(
                    em,
                    entity,
                    FuelLogisticsTaskStatusCode.Blocked,
                    FuelLogisticsBlockReasonCode.RouteUnavailable,
                    resourceKind);
                if (VerboseResourceHaulerLogs)
                    Debug.LogWarning($"[ResourceHauler] entity={entity} failed-destination-move destination={destination.Id} revertedLoad={loadAmount:0.##}");
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, destinationGoal);
            SetOrAddResourceHaulOrder(em, entity, order);
            if (VerboseResourceHaulerLogs)
                Debug.Log($"[ResourceHauler] entity={entity} to-destination destination={destination.Id} target={destinationGoal}");
        }

        private void UpdateTravelToDestinationPhase(
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
                if (!HasActiveGoalOrPathRequest(em, entity, order.TargetCell))
                {
                    if (TryIssueHaulerMoveToBuilding(context, em, entity, destination, out int2 destinationGoal))
                    {
                        context.ResourceHaulerUtilitySystemHelper.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, destinationGoal);
                        SetOrAddResourceHaulOrder(em, entity, order);
                    }
                    else
                    {
                        ReleaseOrderReservations(context, em, entity);
                        em.RemoveComponent<UnitResourceHaulOrder>(entity);
                        SetResourceHaulStatus(
                            em,
                            entity,
                            FuelLogisticsTaskStatusCode.Blocked,
                            FuelLogisticsBlockReasonCode.RouteUnavailable,
                            (ResourceHaulerUtilitySystemHelper.ResourceHaulKind)order.ResourceKind);
                    }
                }
                return;
            }

            context.ResourceHaulerUtilitySystemHelper.SetPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Unloading);
            em.SetComponentData(entity, order);
        }

        private void UpdateUnloadingPhase(
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
            RecordOilDeliveryTelemetry(em, entity, destination, resourceKind, cargo);
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

        private bool TryIssueHaulerMoveToBuilding(Context context, EntityManager em, Entity unit, RuntimeBuildingEntity building, out int2 goal)
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
            if (context.GetEffectivePlacementRect == null)
                return false;

            RectInt buildingRect = context.GetEffectivePlacementRect(building, grid);
            if (!TryFindBuildingApproachCell(grid, walkable, blockerData.Blocked, occupied, buildingRect, unitFootprint, referenceCell, out goal))
                return false;

            if (em.HasComponent<EngageTarget>(unit))
                em.RemoveComponent<EngageTarget>(unit);
            if (em.HasComponent<UnitPathFollow>(unit))
                em.RemoveComponent<UnitPathFollow>(unit);
            if (em.HasComponent<UnitPathRange>(unit))
                em.RemoveComponent<UnitPathRange>(unit);
            if (em.HasComponent<AutoWanderMoveTag>(unit))
                em.RemoveComponent<AutoWanderMoveTag>(unit);

            UnitMoveOrderQueueRequest.EnqueueAndProcessTargetPathMoveOrder(
                em,
                unit,
                goal,
                _moveOrderQueueQueryCache.Get(em));

            if (em.HasComponent<ManualMoveOrderTag>(unit))
                em.RemoveComponent<ManualMoveOrderTag>(unit);

            return true;
        }

        private static bool HasActiveGoalOrPathRequest(EntityManager em, Entity entity, int2 goal)
        {
            if (em.HasComponent<UnitPathRequest>(entity) &&
                em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal))
            {
                return true;
            }

            if (!em.HasComponent<UnitTarget>(entity) ||
                !em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal))
            {
                return false;
            }

            return em.HasComponent<UnitPathFollow>(entity) ||
                   em.HasComponent<UnitPathRange>(entity) ||
                   em.HasComponent<UnitPathRetryCooldown>(entity) ||
                   em.HasComponent<UnitLongDistanceMove>(entity);
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

        private void SetResourceHaulStatus(
            EntityManager em,
            Entity entity,
            FuelLogisticsTaskStatusCode statusCode,
            FuelLogisticsBlockReasonCode reasonCode,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            _fuelLogisticsTelemetry.SetResourceHaulStatus(
                em,
                entity,
                statusCode,
                reasonCode,
                resourceKind);
        }

        private void RecordRouteAssignmentTelemetry(
            EntityManager em,
            Entity entity,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            bool isReassignment)
        {
            _fuelLogisticsTelemetry.RecordRouteAssignment(em, entity, resourceKind, isReassignment);
        }

        private void RecordOilDeliveryTelemetry(
            EntityManager em,
            Entity entity,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float deliveredBarrels)
        {
            _fuelLogisticsTelemetry.RecordOilDelivery(
                em,
                entity,
                destination,
                resourceKind,
                deliveredBarrels);
        }

        private static bool IsHaulerDeadOrUnavailable(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitDeathAnimationComponent>(entity))
                return true;
            return em.HasComponent<UnitHealth>(entity) &&
                   em.GetComponentData<UnitHealth>(entity).Current <= 0;
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
            return TryReserveHaulCapacity(
                context,
                em,
                source,
                destination,
                resourceKind,
                loadAmount,
                allowDeferredSourceReservation: false,
                out reservation);
        }

        private bool TryReserveHaulCapacity(
            Context context,
            EntityManager em,
            RuntimeBuildingEntity source,
            RuntimeBuildingEntity destination,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind,
            float loadAmount,
            bool allowDeferredSourceReservation,
            out UnitResourceHaulReservation reservation)
        {
            reservation = default;
            if (context.ResourceHaulerUtilitySystemHelper == null || source == null || destination == null || loadAmount <= 0f)
                return false;

            bool sourceReserved = context.ResourceHaulerUtilitySystemHelper.TryReserveSource(em, source, resourceKind, loadAmount);
            if (!sourceReserved && !allowDeferredSourceReservation)
                return false;

            if (!context.ResourceHaulerUtilitySystemHelper.TryReserveDestination(em, destination, resourceKind, loadAmount))
            {
                if (sourceReserved)
                    context.ResourceHaulerUtilitySystemHelper.ReleaseSourceReservation(em, source, resourceKind, loadAmount);
                return false;
            }

            reservation = new UnitResourceHaulReservation
            {
                SourceBuildingId = source.Id,
                DestinationBuildingId = destination.Id,
                ReservedBarrels = loadAmount,
                ResourceKind = (byte)resourceKind,
                SourceReservationActive = sourceReserved ? (byte)1 : (byte)0,
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
            RectInt buildingRect,
            int2 unitFootprint,
            int2 referenceCell,
            out int2 goal)
        {
            goal = default;
            int maxRadius = math.max(grid.Width, grid.Height);
            int bestScore = int.MaxValue;
            bool found = false;
            int2 clampedUnitFootprint = UnitFootprintUtility.ClampSize(unitFootprint);

            for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
            {
                int minX = buildingRect.xMin - extraRadius;
                int minY = buildingRect.yMin - extraRadius;
                int maxX = buildingRect.xMax - 1 + extraRadius;
                int maxY = buildingRect.yMax - 1 + extraRadius;

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
