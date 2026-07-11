using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public static bool IsWithinTransportBoardingCommandRange(EntityManager em, Entity transport, Entity passenger)
        {
            if (transport == Entity.Null ||
                passenger == Entity.Null ||
                !em.Exists(transport) ||
                !em.Exists(passenger) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitGrid>(passenger))
            {
                return false;
            }

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            int distance = math.abs(passengerCell.x - transportCell.x) + math.abs(passengerCell.y - transportCell.y);
            return distance <= TransportBoardingCommandMaxDistanceCells;
        }

        public bool IsBoardablePlayerTransportClick(
            EntityManager em,
            Vector2 screenPosition,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell)
        {
            EnsureEntityQueries(em);
            return TryGetClickedOrNearbyBoardableTransport(
                screenPosition,
                em,
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out _,
                false);
        }

        public bool TryResolveBoardablePlayerTransportClick(
            EntityManager em,
            Vector2 screenPosition,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell,
            out Entity transport)
        {
            EnsureEntityQueries(em);
            return TryGetClickedOrNearbyBoardableTransport(
                screenPosition,
                em,
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out transport,
                false);
        }

        private int CollectSelectedBoardingSourceEntities(
            EntityManager em,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            List<Entity> selectedEntities,
            out int selectedTagCount,
            out int selectedMoveCount,
            out bool usedCachedSelection)
        {
            selectedEntities.Clear();
            selectedTagCount = 0;
            selectedMoveCount = 0;
            usedCachedSelection = false;

            EnsureEntityQueries(em);
            selectedMoveCount = _selectedMoveQuery.CalculateEntityCount();
            if (selectedMoveCount > 0)
            {
                selectionStateSystem.CachedSelectedMoveEntities.Clear();
                CollectEntities(em, _selectedMoveQuery, selectedEntities);
                for (int i = 0; i < selectedEntities.Count; i++)
                {
                    Entity entity = selectedEntities[i];
                    if (SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, entity))
                        selectionStateSystem.CachedSelectedMoveEntities.Add(entity);
                }

                selectedTagCount = selectedMoveCount;
                return selectedEntities.Count;
            }

            selectedTagCount = _selectedTagQuery.CalculateEntityCount();
            if (selectedTagCount > 0)
            {
                CollectEntities(em, _selectedTagQuery, selectedEntities);
                return selectedEntities.Count;
            }

            List<Entity> cachedSelectedMoveEntities = selectionStateSystem.CachedSelectedMoveEntities;
            for (int i = cachedSelectedMoveEntities.Count - 1; i >= 0; i--)
            {
                Entity entity = cachedSelectedMoveEntities[i];
                if (!SelectionStateCompositionSystemHelper.IsCacheableSelectedMoveEntity(em, entity))
                {
                    cachedSelectedMoveEntities.RemoveAt(i);
                    continue;
                }

                selectedEntities.Add(entity);
            }

            if (selectedEntities.Count > 0)
                usedCachedSelection = true;
            return selectedEntities.Count;
        }

        private static void BuildSelectedBoardingPassengerIgnoreSets(
            EntityManager em,
            in GridConfig grid,
            Entity transport,
            List<Entity> selectedBoardingSourceEntities,
            int availableSoldierSeats,
            int availableVehicleSlots,
            HashSet<Entity> ignoredEntities,
            HashSet<int> ignoredOccupiedCells)
        {
            if (selectedBoardingSourceEntities == null ||
                ignoredEntities == null ||
                ignoredOccupiedCells == null)
            {
                return;
            }

            int plannedSoldierSeats = 0;
            int plannedVehicleSlots = 0;
            for (int i = 0; i < selectedBoardingSourceEntities.Count; i++)
            {
                Entity passenger = selectedBoardingSourceEntities[i];
                if (passenger == transport ||
                    !em.Exists(passenger) ||
                    !em.HasComponent<UnitGrid>(passenger) ||
                    !em.HasComponent<UnitFootprint>(passenger) ||
                    !TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out _))
                {
                    continue;
                }

                if (!TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
                        passengerKind,
                        availableSoldierSeats,
                        availableVehicleSlots,
                        ref plannedSoldierSeats,
                        ref plannedVehicleSlots))
                {
                    continue;
                }

                ignoredEntities.Add(passenger);
                ReserveFootprintCells(
                    grid,
                    em.GetComponentData<UnitGrid>(passenger).Cell,
                    em.GetComponentData<UnitFootprint>(passenger).Size,
                    ignoredOccupiedCells);
            }
        }

        private static void CollectEntities(EntityManager em, EntityQuery query, List<Entity> entities)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < chunkEntities.Length; i++)
                    entities.Add(chunkEntities[i]);
            }
        }

        private void CollectPathingLiveUnits(
            EntityManager em,
            NativeList<Entity> entities,
            NativeList<UnitGrid> grids,
            NativeList<UnitFootprint> footprints)
        {
            entities.Clear();
            grids.Clear();
            footprints.Clear();

            EnsureEntityQueries(em);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> gridType = em.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
            using NativeArray<ArchetypeChunk> chunks = _pathingLiveUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> chunkEntities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> chunkGrids = chunk.GetNativeArray(ref gridType);
                NativeArray<UnitFootprint> chunkFootprints = chunk.GetNativeArray(ref footprintType);
                for (int i = 0; i < chunkEntities.Length; i++)
                {
                    entities.Add(chunkEntities[i]);
                    grids.Add(chunkGrids[i]);
                    footprints.Add(chunkFootprints[i]);
                }
            }
        }

        private bool TryGetClickedOrNearbyBoardableTransport(
            Vector2 screenPosition,
            EntityManager em,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
            TryGetClickedCellDelegate tryGetClickedCell,
            out Entity transport,
            bool logDiagnostics = true)
        {
            transport = Entity.Null;
            bool shouldLogTransportBoarding = logDiagnostics && TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            Entity clickedEntity = Entity.Null;
            bool hasClickedEntity = tryGetClickedUnitEntity(screenPosition, em, out clickedEntity);
            if (hasClickedEntity && IsBoardablePlayerTransport(em, clickedEntity))
            {
                transport = clickedEntity;
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedTransport transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                return true;
            }

            if (!tryGetClickedCell(screenPosition, em, out int2 clickedCell, out _))
            {
                if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoClickedCell clicked={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, clickedEntity)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, clickedEntity)}");
                return false;
            }

            if (TryFindNearbyBoardableTransport(em, clickedCell, out transport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NearbyTransport clickedCell={clickedCell} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                return true;
            }

            if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
            {
                TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=ClickedTransportRejected clicked={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, clickedEntity)} " +
                    $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, clickedEntity)}");
            }

            if (hasClickedEntity &&
                em.Exists(clickedEntity) &&
                em.HasComponent<UnitMove>(clickedEntity) &&
                !em.HasComponent<RuntimeBuildingCombatTag>(clickedEntity) &&
                !em.HasComponent<StaticGridBlocker>(clickedEntity))
            {
                return false;
            }

            return false;
        }

        private bool TryFindNearbyBoardableTransport(
            EntityManager em,
            int2 clickedCell,
            out Entity transport)
        {
            transport = Entity.Null;
            EnsureEntityQueries(em);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _allSelectableQuery.ToArchetypeChunkArray(Allocator.Temp);
            int bestScore = int.MaxValue;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (!IsBoardablePlayerTransport(em, candidate))
                        continue;

                    int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                    int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
                    int clickPaddingCells = GetTransportBoardingClickPaddingCells(em, candidate, footprint);
                    if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, clickPaddingCells))
                        continue;

                    int2 delta = clickedCell - cell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    transport = candidate;
                }
            }

            return transport != Entity.Null;
        }

        internal static bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
        {
            if (!em.HasComponent<UnitAirMovement>(transport))
                return true;

            if (!em.HasComponent<UnitAirComponent>(transport) || !em.HasComponent<LocalTransform>(transport))
                return false;

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            bool physicallyGrounded = transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
            return airState.Airborne == 0 &&
                   airState.TakeoffRolling == 0 &&
                   airState.LandingRolling == 0 &&
                   physicallyGrounded &&
                   !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
        }

        internal static int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
        {
            return em.HasComponent<UnitAirMovement>(transport)
                ? TransportBoardingData.AirBoardingClearanceCells
                : TransportBoardingData.BoardingClearanceCells;
        }

        private static int GetTransportBoardingClickPaddingCells(EntityManager em, Entity transport, int2 footprint)
        {
            int footprintMax = math.max(footprint.x, footprint.y);
            if (em.Exists(transport) && em.HasComponent<UnitAirMovement>(transport))
                return math.max(AirTransportClickPaddingCells, footprintMax + AirTransportClickPaddingCells);

            return math.max(GroundTransportClickPaddingMinCells, footprintMax + GroundTransportClickPaddingExtraCells);
        }

        public static bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.IsBoardablePlayerTransport(em, transport);
        }

        public static bool IsBoardingCandidateForTransport(EntityManager em, Entity transport, Entity passenger)
        {
            return TryResolveBoardingPassengerKind(em, transport, passenger, out _, out _);
        }

        public static bool TryResolveBoardingPassengerKind(
            EntityManager em,
            Entity transport,
            Entity passenger,
            out byte passengerKind,
            out int cargoWeight)
        {
            return TransportBoardingCapacitySystemHelper.TryResolveBoardingPassengerKind(
                em,
                transport,
                passenger,
                out passengerKind,
                out cargoWeight);
        }

        public static bool HasAvailableTransportBoardingSlot(
            EntityManager em,
            Entity transport,
            byte passengerKind,
            out int occupied,
            out int capacity)
        {
            return TransportBoardingCapacitySystemHelper.HasAvailableTransportBoardingSlot(
                em,
                transport,
                passengerKind,
                out occupied,
                out capacity);
        }

        public static bool HasAnyAvailableTransportBoardingSlot(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.HasAnyAvailableTransportBoardingSlot(em, transport);
        }

        public static bool IsPotentialVehicleCargoPassenger(EntityManager em, Entity entity)
        {
            return TransportBoardingCapacitySystemHelper.IsPotentialVehicleCargoPassenger(em, entity);
        }

        private static bool IsPotentialVehicleCargoPassenger(EntityManager em, Entity entity, bool allowLoadedPassenger)
        {
            return TransportBoardingCapacitySystemHelper.IsPotentialVehicleCargoPassenger(
                em,
                entity,
                allowLoadedPassenger);
        }

        public static bool IsVehicleBoardingCandidateForTransport(EntityManager em, Entity transport, Entity passenger)
        {
            return TransportBoardingCapacitySystemHelper.IsVehicleBoardingCandidateForTransport(em, transport, passenger);
        }

        public static bool IsCargoPlaneTransport(EntityManager em, Entity transport)
        {
            return TransportBoardingCapacitySystemHelper.IsCargoPlaneTransport(em, transport);
        }

        public static bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
        {
            return TransportBoardingCapacitySystemHelper.IsSoldierBoardingCandidate(em, entity);
        }


    }
}
