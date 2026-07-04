using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;
using Game.Composition;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BuildingProductionQueueCompositionSystemHelperTests
{
    public static void RunBuildingGameplayCompositionRuntimeSmokeValidation()
    {
        World previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("BuildingGameplayCompositionRuntimeSmokeValidation");
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.BuildingGameplayComposition_InitializesRuntimeDollarsFromInitialUnitsConfig();
            tests.BuildingGameplayComposition_CampBuildingRequestStartsConfiguredPlacement();
            Debug.Log("[BuildingGameplayCompositionRuntimeSmokeValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingGameplayCompositionRuntimeSmokeValidation] result=Failed");
            ValidationExit.Failed();
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousDefaultWorld;
            if (world.IsCreated)
                world.Dispose();
        }
    }

    public static void RunGlobalProductionQueueLimitValidation()
    {
        try
        {
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.BuildingUiCampItemCommandRequest_RejectsGlobalProductionQueueLimitAndRefunds();
            Debug.Log("[BuildingProductionGlobalQueueLimitValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionGlobalQueueLimitValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunProductionCameraFocusValidation()
    {
        try
        {
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn();
            tests.FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed();
            tests.FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput();
            tests.FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction();
            tests.FocusNewestPlayerProducedUnit_UsesProducedUnitReadModel();
            tests.ResolveProducedUnitFaction_DefaultsNeutralOrUnownedProductionToPlayer();
            tests.TryFindFirstFriendlyProducerBuilding_PrefersPlayerProducerOverNeutralFallback();
            tests.TryFindFirstFriendlyProducerBuilding_AllowsNeutralFallbackWhenNoPlayerProducerExists();
            tests.RebuildPendingProductionTimeline_ChainsQueuedItemsAfterActiveProduction();
            tests.RebuildPendingProductionTimeline_AfterActiveRemovalResetsNextActiveProgress();
            Debug.Log("[BuildingProductionCameraFocusValidation] result=Passed tests=10");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionCameraFocusValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunProductionMetadataValidation()
    {
        try
        {
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.ResolveProductionDurationSeconds_UsesUnitAuthoringDuration();
            tests.ResolveProductionTransportSettings_UsesConfiguredTransportAuthoring();
            tests.ResolveProductionTransportSettings_DefaultsLargeVehicleToPlaneTransport();
            Debug.Log("[BuildingProductionMetadataValidation] result=Passed tests=3");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionMetadataValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunProducedUnitStateValidation()
    {
        try
        {
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.PruneProducedUnits_RemovesDeadUnitsAndClearsDeadSlots();
            Debug.Log("[ProducedUnitSourceKeyStateValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[ProducedUnitSourceKeyStateValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunProductionRequestValidation()
    {
        try
        {
            var tests = new BuildingProductionQueueCompositionSystemHelperTests();
            tests.BuildingUiProductionCommandRequest_QueuesSelectedBuildingUnitAndWritesResult();
            tests.BuildingUiProductionCommandRequest_RejectsMissingActiveBuilding();
            tests.BuildingUiProductionCommandRequest_RejectsStaleFrame();
            tests.BuildingUiProductionCommandRequest_RejectsUnavailablePrefab();
            tests.BuildingUiProductionCommandRequest_RejectsQueueFull();
            tests.BuildingUiProductionCommandRequest_CancelsPendingProductionAndWritesResult();
            tests.BuildingUiCampItemCommandRequest_StartsConfiguredPlacementAndWritesResult();
            tests.BuildingUiCampItemCommandRequest_QueuesUnitProductionAndWritesResult();
            tests.BuildingUiCampItemCommandRequest_RejectsFullProductionSlotsAndRefunds();
            tests.BuildingRuntimeState_ProcessesQueuedUiProductionCommand();
            tests.BuildingRuntimeState_ProcessesQueuedCampItemCommand();
            tests.CountRuntimeProducedUnitsForFaction_UsesProducedUnitReadModel();
            tests.BuildingRuntimeState_ProductionSummaryUsesProducedUnitReadModel();
            tests.BuildingRuntimeState_FactionSummarySignatureUsesEcsResourceStorage();
            tests.TryQueuePlayerUnitFromBuilding_UsesProducedUnitReadModelSlotOccupancy();
            tests.BuildingDefinitionProductionSourceKey_UsesSlotKeyBeforePrefabFallback();
            tests.BuildingSpawnCompositionSystemHelper_SpawnsSourceKeyOnlyProductionSlot();
            tests.BuildingSpawnCompositionSystemHelper_ResolvesFactionProductionSpawnPointFromBoundaryReadModel();
            tests.BuildingSpawnCompositionSystemHelper_WritesRecentSpawnReservationToBoundaryBuffer();
            tests.BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForProductionSlotPlacement();
            tests.BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointWithoutManagedSlotArray();
            tests.BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForOverrideHelicopterSlot();
            tests.BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForAutomaticHelicopterSpawn();
            Debug.Log("[BuildingProductionRequestValidation] result=Passed tests=23");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[BuildingProductionRequestValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BuildingDefinitionProductionSourceKey_UsesSlotKeyBeforePrefabFallback()
    {
        var sourceKeyOnlyDefinition = new BuildingDefinition
        {
            ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
            {
                new()
                {
                    SpawnUnitPrefab = null,
                    SpawnUnitSourceKey = new FixedString64Bytes("unit_veh_tank_usa")
                }
            }
        };

        Assert.IsTrue(BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey(sourceKeyOnlyDefinition, 0, out FixedString64Bytes sourceKey));
        Assert.AreEqual(new FixedString64Bytes("unit_veh_tank_usa"), sourceKey);

        GameObject unitPrefab = new("Unit_Veh_APC_Heavy");
        try
        {
            var fallbackDefinition = new BuildingDefinition
            {
                SpawnUnitPrefab = unitPrefab
            };

            Assert.IsTrue(BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey(fallbackDefinition, 0, out FixedString64Bytes fallbackSourceKey));
            Assert.AreEqual(new FixedString64Bytes("unit_veh_apc_heavy"), fallbackSourceKey);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_SpawnsSourceKeyOnlyProductionSlot()
    {
        const int width = 8;
        const int height = 8;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_SourceKeyOnlyProductionSlot");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_veh_helicopter_transport");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(UnitAirMovement),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Veh_Helicopter_Transport");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(3, 3) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                new Dictionary<int, RuntimeBuildingEntity>(),
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity building = new()
            {
                Id = 10,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Source Key Helipad",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                building,
                productionIndex: 0,
                reservedProductionSlotIndex: -1,
                overrideWorldPosition: new Vector3(4.5f, 0f, 4.5f),
                overrideCell: new int2(4, 4),
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            Assert.IsNull(building.ProducedUnits);
            Assert.IsTrue(em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity));
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            Assert.AreEqual(1, producedUnitRows.Length);
            Entity spawned = producedUnitRows[0].Unit;
            Assert.IsTrue(em.Exists(spawned));
            Assert.AreEqual(new int2(4, 4), em.GetComponentData<UnitGrid>(spawned).Cell);
            Assert.AreEqual(sourceKey, em.GetComponentData<UnitSourcePrefabKey>(spawned).Value);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(spawned).Id);
            Assert.IsNull(building.ProducedUnitSourceKeys);
            Assert.IsTrue(building.ProducedUnitPrefabs == null || !building.ProducedUnitPrefabs.ContainsKey(spawned));
            Assert.IsTrue(em.HasBuffer<BuildingProductionSpawnRequest>(boundaryEntity));
            DynamicBuffer<BuildingProductionSpawnRequest> spawnRequests =
                em.GetBuffer<BuildingProductionSpawnRequest>(boundaryEntity, true);
            Assert.AreEqual(1, spawnRequests.Length);
            Assert.AreEqual(10, spawnRequests[0].BuildingRuntimeId);
            Assert.AreEqual(0, spawnRequests[0].ProductionIndex);
            Assert.AreEqual(-1, spawnRequests[0].ReservedProductionSlotIndex);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, spawnRequests[0].OwnerFactionId);
            Assert.AreEqual(1, spawnRequests[0].HasOwnerFaction);
            Assert.AreEqual(1, spawnRequests[0].HasOverrideWorldPosition);
            Assert.AreEqual(1, spawnRequests[0].HasOverrideCell);
            Assert.AreEqual(BuildingProductionSpawnRequest.Succeeded, spawnRequests[0].Status);
            Assert.AreEqual(sourceKey, spawnRequests[0].UnitSourceKey);
            Assert.AreEqual(prefabEntity, spawnRequests[0].PrefabEntity);
            Assert.AreEqual(spawned, spawnRequests[0].ProducedUnit);
            Assert.AreEqual(new int2(4, 4), spawnRequests[0].SpawnCell);
            Assert.AreEqual(new float3(4.5f, 0f, 4.5f), spawnRequests[0].SpawnWorldPosition);
            Assert.AreEqual(10, producedUnitRows[0].BuildingRuntimeId);
            Assert.AreEqual(0, producedUnitRows[0].ProductionIndex);
            Assert.AreEqual(-1, producedUnitRows[0].ProductionSlotIndex);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, producedUnitRows[0].OwnerFactionId);
            Assert.AreEqual(1, producedUnitRows[0].HasOwnerFaction);
            Assert.AreEqual(spawned, producedUnitRows[0].Unit);
            Assert.AreEqual(sourceKey, producedUnitRows[0].UnitSourceKey);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_ResolvesFactionProductionSpawnPointFromBoundaryReadModel()
    {
        using World world = new("BuildingSpawnCompositionSystemHelper_FactionSpawnPointReadModel");
        EntityManager em = world.EntityManager;
        Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
            em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity);
        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = FactionIdentity.PlayerFactionId,
            BuildingId = new FixedString128Bytes("building_helipad"),
            SlotIndex = 0,
            Cell = new int2(2, 3),
            WorldPosition = new float3(2.5f, 0f, 3.5f)
        });
        spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
        {
            FactionId = FactionIdentity.PlayerFactionId,
            BuildingId = new FixedString128Bytes("building_helipad"),
            SlotIndex = 1,
            Cell = new int2(4, 5),
            WorldPosition = new float3(4.5f, 0f, 5.5f)
        });

        var spawnSystem = new BuildingSpawnCompositionSystemHelper();
        var context = new BuildingSpawnCompositionSystemHelper.Context(
            new Dictionary<int, RuntimeBuildingEntity>(),
            default,
            null,
            default,
            default,
            null,
            null,
            null,
            (EntityManager _, out Entity entity) =>
            {
                entity = boundaryEntity;
                return true;
            });
        GridConfig grid = new() { Width = 8, Height = 8, CellSize = 1f, Origin = float3.zero };

        Assert.IsTrue(spawnSystem.TryGetFactionProductionSpawnPoint(
            context,
            FactionIdentity.PlayerFactionId,
            "Building_Helipad",
            1,
            em,
            grid,
            out int2 cell,
            out float3 worldPosition));
        Assert.AreEqual(new int2(4, 5), cell);
        Assert.AreEqual(new float3(4.5f, 0f, 5.5f), worldPosition);
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_WritesRecentSpawnReservationToBoundaryBuffer()
    {
        const int width = 8;
        const int height = 8;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_RecentReservationBoundaryBuffer");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_veh_tank_light");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Veh_Tank_Light");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(2, 2) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                new Dictionary<int, RuntimeBuildingEntity>(),
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity building = new()
            {
                Id = 11,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Source Key Factory",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                building,
                productionIndex: 0,
                reservedProductionSlotIndex: -1,
                overrideWorldPosition: new Vector3(4.5f, 0f, 4.5f),
                overrideCell: new int2(4, 4),
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            Assert.IsTrue(em.HasBuffer<BuildingRecentSpawnReservation>(boundaryEntity));
            DynamicBuffer<BuildingRecentSpawnReservation> reservations =
                em.GetBuffer<BuildingRecentSpawnReservation>(boundaryEntity, true);
            Assert.AreEqual(1, reservations.Length);
            Assert.AreEqual(new int2(4, 4), reservations[0].Cell);
            Assert.AreEqual(new int2(2, 2), reservations[0].Size);
            Assert.Greater(reservations[0].ExpiresAt, 0f);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForProductionSlotPlacement()
    {
        const int width = 8;
        const int height = 8;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_BoundaryProductionSlotPlacement");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_inf_regular");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Inf_Regular");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity);
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_factory"),
                BuildingRuntimeId = 12,
                SlotIndex = 0,
                Cell = new int2(5, 5),
                WorldPosition = new float3(5.5f, 0f, 5.5f)
            });

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                new Dictionary<int, RuntimeBuildingEntity>(),
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity building = new()
            {
                Id = 12,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                ProductionSpawnLocalPositions = new[] { new Vector3(1.5f, 0f, 1.5f) },
                ProducedUnitSlots = new Entity[1],
                Definition = new BuildingDefinition
                {
                    DisplayName = "Boundary Factory",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                building,
                productionIndex: 0,
                reservedProductionSlotIndex: 0,
                overrideWorldPosition: null,
                overrideCell: null,
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            Assert.IsNull(building.ProducedUnits);
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            Assert.AreEqual(1, producedUnitRows.Length);
            Entity spawned = producedUnitRows[0].Unit;
            Assert.AreEqual(new int2(5, 5), em.GetComponentData<UnitGrid>(spawned).Cell);
            Assert.AreEqual(new float3(5.5f, 0f, 5.5f), em.GetComponentData<LocalTransform>(spawned).Position);
            Assert.AreEqual(Entity.Null, building.ProducedUnitSlots[0]);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointWithoutManagedSlotArray()
    {
        const int width = 8;
        const int height = 8;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_BoundaryProductionSlotWithoutManagedArray");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_inf_regular");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Inf_Regular");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity);
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_factory"),
                BuildingRuntimeId = 13,
                SlotIndex = 0,
                Cell = new int2(5, 5),
                WorldPosition = new float3(5.5f, 0f, 5.5f)
            });

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                new Dictionary<int, RuntimeBuildingEntity>(),
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity building = new()
            {
                Id = 13,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Boundary Factory",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                building,
                productionIndex: 0,
                reservedProductionSlotIndex: -1,
                overrideWorldPosition: null,
                overrideCell: null,
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            Assert.IsNull(building.ProducedUnits);
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            Assert.AreEqual(1, producedUnitRows.Length);
            Entity spawned = producedUnitRows[0].Unit;
            Assert.AreEqual(new int2(5, 5), em.GetComponentData<UnitGrid>(spawned).Cell);
            Assert.AreEqual(new float3(5.5f, 0f, 5.5f), em.GetComponentData<LocalTransform>(spawned).Position);
            Assert.IsNull(building.ProducedUnitSlots);

            Assert.AreEqual(13, producedUnitRows[0].BuildingRuntimeId);
            Assert.AreEqual(13, producedUnitRows[0].ProductionSlotBuildingRuntimeId);
            Assert.AreEqual(0, producedUnitRows[0].ProductionSlotIndex);
            Assert.AreEqual(spawned, producedUnitRows[0].Unit);
            Assert.AreEqual(sourceKey, producedUnitRows[0].UnitSourceKey);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForOverrideHelicopterSlot()
    {
        const int width = 8;
        const int height = 8;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_BoundaryOverrideHelicopterSlot");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_veh_helicopter_transport");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(UnitAirMovement),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Veh_Helicopter_Transport");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity);
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_helipad"),
                BuildingRuntimeId = 70,
                SlotIndex = 0,
                Cell = new int2(4, 4),
                WorldPosition = new float3(4.5f, 0f, 4.5f)
            });

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            RuntimeBuildingEntity helipad = new()
            {
                Id = 70,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(3, 3),
                ProducedUnitSlots = new Entity[1],
                Definition = new BuildingDefinition
                {
                    DisplayName = "Boundary Helipad",
                    FootprintCells = new Vector2Int(2, 2)
                }
            };
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
            {
                [helipad.Id] = helipad
            };
            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                runtimeBuildings,
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity sourceBuilding = new()
            {
                Id = 10,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Source Key Air Factory",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                sourceBuilding,
                productionIndex: 0,
                reservedProductionSlotIndex: -1,
                overrideWorldPosition: new Vector3(4.5f, 0f, 4.5f),
                overrideCell: new int2(4, 4),
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            Assert.AreEqual(1, producedUnitRows.Length);
            Assert.IsNull(sourceBuilding.ProducedUnits);
            Entity spawned = producedUnitRows[0].Unit;
            Assert.AreEqual(new int2(4, 4), em.GetComponentData<UnitGrid>(spawned).Cell);
            Assert.AreEqual(Entity.Null, helipad.ProducedUnitSlots[0]);
            Assert.AreEqual(10, producedUnitRows[0].BuildingRuntimeId);
            Assert.AreEqual(70, producedUnitRows[0].ProductionSlotBuildingRuntimeId);
            Assert.AreEqual(0, producedUnitRows[0].ProductionSlotIndex);
            Assert.AreEqual(spawned, producedUnitRows[0].Unit);
            Assert.AreEqual(sourceKey, producedUnitRows[0].UnitSourceKey);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void BuildingSpawnCompositionSystemHelper_UsesBoundarySpawnPointForAutomaticHelicopterSpawn()
    {
        const int width = 10;
        const int height = 10;
        int gridSize = width * height;
        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        World world = new("BuildingSpawnCompositionSystemHelper_BoundaryAutomaticHelicopterSpawn");
        EntityManager em = world.EntityManager;

        try
        {
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

            GridConfig grid = new() { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            em.SetComponentData(gridEntity, grid);
            em.SetComponentData(gridEntity, new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            });
            em.SetComponentData(gridEntity, new DynamicOccupancyComponent
            {
                GridSize = gridSize,
                Occupied = occupied
            });

            DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = new GridWalkable { Value = 1 };

            FixedString64Bytes sourceKey = new("unit_veh_helicopter_transport");
            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitSourcePrefabKey),
                typeof(LocalTransform),
                typeof(UnitAirMovement),
                typeof(Faction));
            em.SetName(prefabEntity, "Unit_Veh_Helicopter_Transport");
            em.SetComponentData(prefabEntity, new UnitGrid { Cell = int2.zero });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = sourceKey });
            em.SetComponentData(prefabEntity, LocalTransform.FromPosition(float3.zero));
            em.SetComponentData(prefabEntity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            em.SetComponentData(prefabEntity, new Faction { Id = FactionIdentity.NeutralFactionId });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints =
                em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity);
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_air_factory"),
                BuildingRuntimeId = 10,
                SlotIndex = 0,
                Cell = new int2(2, 2),
                WorldPosition = new float3(2.5f, 0f, 2.5f)
            });
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_helipad"),
                BuildingRuntimeId = 71,
                SlotIndex = 0,
                Cell = new int2(7, 7),
                WorldPosition = new float3(7.5f, 0f, 7.5f)
            });
            spawnPoints.Add(new BuildingFactionProductionSpawnPointReadModel
            {
                FactionId = FactionIdentity.PlayerFactionId,
                BuildingId = new FixedString128Bytes("building_helipad"),
                BuildingRuntimeId = 70,
                SlotIndex = 0,
                Cell = new int2(4, 4),
                WorldPosition = new float3(4.5f, 0f, 4.5f)
            });

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            using EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());

            RuntimeBuildingEntity helipad = new()
            {
                Id = 70,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(3, 3),
                ProducedUnitSlots = new Entity[1],
                Definition = new BuildingDefinition
                {
                    DisplayName = "Boundary Helipad",
                    FootprintCells = new Vector2Int(2, 2)
                }
            };
            RuntimeBuildingEntity farHelipad = new()
            {
                Id = 71,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(6, 6),
                ProducedUnitSlots = new Entity[1],
                Definition = new BuildingDefinition
                {
                    DisplayName = "Boundary Far Helipad",
                    FootprintCells = new Vector2Int(2, 2)
                }
            };
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
            {
                [helipad.Id] = helipad,
                [farHelipad.Id] = farHelipad
            };
            var spawnSystem = new BuildingSpawnCompositionSystemHelper();
            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var context = new BuildingSpawnCompositionSystemHelper.Context(
                runtimeBuildings,
                liveUnitFootprintQuery,
                null,
                spawnPrefabSystem,
                new BuildingSpawnPrefabSystem.Context(registryQuery, prefabCandidatesQuery, liveUnitsQuery),
                new BuildingProductionSlotUtilitySystemHelper(),
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                BuildingDefinitionPrefabSystemHelper.TryGetProductionSourceKey,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });
            RuntimeBuildingEntity sourceBuilding = new()
            {
                Id = 10,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OriginCell = new Vector2Int(2, 2),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Source Key Air Factory",
                    FootprintCells = new Vector2Int(2, 2),
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitSourceKey = sourceKey }
                    }
                }
            };

            uint randomState = 7u;
            Assert.IsTrue(spawnSystem.TrySpawnPlayerUnitNearBuilding(
                context,
                sourceBuilding,
                productionIndex: 0,
                reservedProductionSlotIndex: -1,
                overrideWorldPosition: null,
                overrideCell: null,
                em,
                gridEntity,
                grid,
                em.GetComponentData<DynamicBlockerComponent>(gridEntity),
                ref randomState));

            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            Assert.AreEqual(1, producedUnitRows.Length);
            Assert.IsNull(sourceBuilding.ProducedUnits);
            Entity spawned = producedUnitRows[0].Unit;
            Assert.AreEqual(new int2(4, 4), em.GetComponentData<UnitGrid>(spawned).Cell);
            Assert.AreEqual(new float3(4.5f, 0f, 4.5f), em.GetComponentData<LocalTransform>(spawned).Position);
            Assert.AreEqual(Entity.Null, helipad.ProducedUnitSlots[0]);
            Assert.AreEqual(Entity.Null, farHelipad.ProducedUnitSlots[0]);
            Assert.AreEqual(10, producedUnitRows[0].BuildingRuntimeId);
            Assert.AreEqual(70, producedUnitRows[0].ProductionSlotBuildingRuntimeId);
            Assert.AreEqual(0, producedUnitRows[0].ProductionSlotIndex);
            Assert.AreEqual(spawned, producedUnitRows[0].Unit);
            Assert.AreEqual(sourceKey, producedUnitRows[0].UnitSourceKey);
        }
        finally
        {
            if (world.IsCreated)
                world.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InitializePendingProduction_SetsReadyTimeAndTransportFields()
    {
        var pending = new TestPendingProduction();
        var system = new BuildingProductionQueueCompositionSystemHelper();

        system.InitializePendingProduction(
            pending,
            productionIndex: 2,
            spawnUnitPrefab: null,
            now: 10f,
            productionDurationSeconds: 4f,
            reservedProductionSlotIndex: 1,
            transportPrefab: null,
            transportArrivalSeconds: 3f,
            transportHoldForNextReadySeconds: 5f,
            transportMaxConcurrent: 2,
            transportMode: BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Plane,
            transportRequiresAirportRunway: true);

        Assert.AreEqual(2, pending.ProductionIndex);
        Assert.AreEqual(10f, pending.StartedAt);
        Assert.AreEqual(14f, pending.ReadyAt);
        Assert.AreEqual(1, pending.ReservedProductionSlotIndex);
        Assert.AreEqual(3f, pending.TransportArrivalSeconds);
        Assert.AreEqual(5f, pending.TransportHoldForNextReadySeconds);
        Assert.AreEqual(2, pending.TransportMaxConcurrent);
        Assert.AreEqual(BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Plane, pending.TransportMode);
        Assert.IsTrue(pending.TransportRequiresAirportRunway);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn()
    {
        using World world = new("FocusNewestPlayerProducedUnit_RequestsCameraMoveToNewestSpawn");
        EntityManager em = world.EntityManager;
        Entity older = em.CreateEntity(typeof(LocalTransform));
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(older, LocalTransform.FromPosition(new float3(1f, 0f, 2f)));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProducedUnits = new List<Entity> { older, newest }
        };

        Vector3? requestedFocus = null;
        BuildingProductionTransportBridgeCompositionSystemHelper.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            worldPosition => requestedFocus = worldPosition);

        Assert.IsTrue(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsTrue(requestedFocus.HasValue);
        Assert.AreEqual(new Vector3(7f, 0f, 9f), requestedFocus.Value);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed()
    {
        using World world = new("FocusNewestPlayerProducedUnit_IgnoresWhenBuildDrawerClosed");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        bool requestedFocus = false;
        BuildingProductionTransportBridgeCompositionSystemHelper.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => false,
            _ => requestedFocus = true);

        Assert.IsFalse(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsFalse(requestedFocus);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction()
    {
        using World world = new("FocusNewestPlayerProducedUnit_IgnoresNonPlayerProduction");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        RuntimeBuildingEntity building = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        bool requestedFocus = false;
        BuildingProductionTransportBridgeCompositionSystemHelper.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            _ => requestedFocus = true);

        Assert.IsFalse(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsFalse(requestedFocus);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput()
    {
        using World world = new("FocusNewestPlayerProducedUnit_AllowsNeutralOrUnownedProducerOutput");
        EntityManager em = world.EntityManager;
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(3f, 0f, 4f)));

        RuntimeBuildingEntity unownedBuilding = new()
        {
            HasOwnerFaction = false,
            ProducedUnits = new List<Entity> { newest }
        };
        RuntimeBuildingEntity neutralBuilding = new()
        {
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.NeutralFactionId,
            ProducedUnits = new List<Entity> { newest }
        };

        int focusCount = 0;
        BuildingProductionTransportBridgeCompositionSystemHelper.Context context = new(
            null,
            null,
            null,
            null,
            default,
            () => true,
            _ => focusCount++);

        Assert.IsTrue(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, unownedBuilding, em));
        Assert.IsTrue(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, neutralBuilding, em));
        Assert.AreEqual(2, focusCount);
    }

    [Test]
    public void FocusNewestPlayerProducedUnit_UsesProducedUnitReadModel()
    {
        using World world = new("FocusNewestPlayerProducedUnit_UsesProducedUnitReadModel");
        EntityManager em = world.EntityManager;
        Entity older = em.CreateEntity(typeof(LocalTransform));
        Entity newest = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(older, LocalTransform.FromPosition(new float3(1f, 0f, 2f)));
        em.SetComponentData(newest, LocalTransform.FromPosition(new float3(7f, 0f, 9f)));

        Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            em.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 27,
            ProductionSlotBuildingRuntimeId = 27,
            ProductionIndex = 0,
            ProductionSlotIndex = 0,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            HasOwnerFaction = 1,
            Unit = older,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 27,
            ProductionSlotBuildingRuntimeId = 27,
            ProductionIndex = 1,
            ProductionSlotIndex = 1,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            HasOwnerFaction = 1,
            Unit = newest,
            UnitSourceKey = new FixedString64Bytes("unit_inf_rifle")
        });

        RuntimeBuildingEntity building = new()
        {
            Id = 27,
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId
        };

        Vector3? requestedFocus = null;
        BuildingSpawnCompositionSystemHelper.Context spawnContext = new(
            new Dictionary<int, RuntimeBuildingEntity>(),
            default,
            null,
            default,
            default,
            null,
            null,
            null,
            (EntityManager _, out Entity entity) =>
            {
                entity = boundaryEntity;
                return true;
            });
        BuildingProductionTransportBridgeCompositionSystemHelper.Context context = new(
            null,
            null,
            null,
            null,
            spawnContext,
            () => true,
            worldPosition => requestedFocus = worldPosition);

        Assert.IsTrue(BuildingProductionTransportBridgeCompositionSystemHelper.FocusNewestPlayerProducedUnit(context, building, em));
        Assert.IsTrue(requestedFocus.HasValue);
        Assert.AreEqual(new Vector3(7f, 0f, 9f), requestedFocus.Value);
        Assert.IsNull(building.ProducedUnits);
    }

    [Test]
    public void ResolveProducedUnitFaction_DefaultsNeutralOrUnownedProductionToPlayer()
    {
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnCompositionSystemHelper.ResolveProducedUnitFaction(new RuntimeBuildingEntity { HasOwnerFaction = false }));
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnCompositionSystemHelper.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.NeutralFactionId
            }));
        Assert.AreEqual(
            FactionIdentity.PlayerFactionId,
            BuildingSpawnCompositionSystemHelper.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId
            }));
        Assert.AreEqual(
            FactionIdentity.EnemyFactionId,
            BuildingSpawnCompositionSystemHelper.ResolveProducedUnitFaction(new RuntimeBuildingEntity
            {
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.EnemyFactionId
            }));
    }

    [Test]
    public void TryFindFirstFriendlyProducerBuilding_PrefersPlayerProducerOverNeutralFallback()
    {
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity neutralProducer = CreateProducerBuilding(
                id: 10,
                displayName: "Neutral Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.NeutralFactionId);
            RuntimeBuildingEntity playerProducer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [neutralProducer.Id] = neutralProducer,
                [playerProducer.Id] = playerProducer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab);

            Assert.IsTrue(requestSystem.TryFindFirstFriendlyProducerBuilding(
                context,
                unitPrefab,
                out int buildingId,
                out int productionIndex,
                out string displayName));
            Assert.AreEqual(playerProducer.Id, buildingId);
            Assert.AreEqual(0, productionIndex);
            Assert.AreEqual("Player Helipad", displayName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void TryFindFirstFriendlyProducerBuilding_AllowsNeutralFallbackWhenNoPlayerProducerExists()
    {
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity neutralProducer = CreateProducerBuilding(
                id: 10,
                displayName: "Neutral Helipad",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.NeutralFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [neutralProducer.Id] = neutralProducer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab);

            Assert.IsTrue(requestSystem.TryFindFirstFriendlyProducerBuilding(
                context,
                unitPrefab,
                out int buildingId,
                out int productionIndex,
                out string displayName));
            Assert.AreEqual(neutralProducer.Id, buildingId);
            Assert.AreEqual(0, productionIndex);
            Assert.AreEqual("Neutral Helipad", displayName);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_QueuesSelectedBuildingUnitAndWritesResult()
    {
        using World world = new("BuildingUiProductionCommandRequestTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Queued, result.ResultCode);
            Assert.AreEqual(producer.Id, result.BuildingId);
            Assert.AreEqual(0, result.ProductionIndex);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);

            using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiProductionCommandQueueComponent>());
            Entity queueEntity = queueQuery.GetSingletonEntity();
            Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiProductionCommandRequestElement>(queueEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsMissingActiveBuilding()
    {
        using World world = new("BuildingUiProductionCommandMissingActiveBuildingTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                new Dictionary<int, RuntimeBuildingEntity>(),
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                null,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.MissingActiveBuilding, result.ResultCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsStaleFrame()
    {
        using World world = new("BuildingUiProductionCommandStaleFrameTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 25,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 41);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.NotArmed, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsUnavailablePrefab()
    {
        using World world = new("BuildingUiProductionCommandUnavailablePrefabTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject contextUnitPrefab = new("Context Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 30,
                displayName: "Player Factory",
                unitPrefab: null,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                contextUnitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.UnavailablePrefab, result.ResultCode);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contextUnitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_RejectsQueueFull()
    {
        using World world = new("BuildingUiProductionCommandQueueFullTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 31,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager,
                tryQueuePlayerUnit: (_, _, _) => false);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.QueueFull, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiProductionCommandRequest_CancelsPendingProductionAndWritesResult()
    {
        using World world = new("BuildingUiProductionCommandCancelProductionTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 20,
                displayName: "Player Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            Assert.IsTrue(requestSystem.EnqueueAndProcessCreateUnitFromSelectedBuilding(
                world.EntityManager,
                context,
                producer.Id,
                productionIndex: 0,
                frameCount: 42));
            Assert.AreEqual(1, producer.PendingProductions.Count);

            int requestId = requestSystem.EnqueueCancelProduction(world.EntityManager, producer.Id, pendingProductionIndex: 0);
            requestSystem.ProcessPendingUiProductionCommands(world.EntityManager, context, frameCount: 0, now: 20f);

            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Cancelled, result.ResultCode);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_StartsConfiguredPlacementAndWritesResult()
    {
        using World world = new("BuildingUiCampItemCommandPlacementTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        GameObject buildingPrefab = new("Requestable Airport");
        try
        {
            BuildingDefinition definition = new()
            {
                DisplayName = "Requestable Airport",
                Prefab = buildingPrefab
            };
            bool beganPlacement = false;
            int activePlacementCost = -1;
            BuildingProductionRequestSystemHelper.Context context = CreateCampItemRequestContext(
                new Dictionary<int, RuntimeBuildingEntity>(),
                new List<BuildingDefinition> { definition },
                new Dictionary<GameObject, BuildingDefinition> { { buildingPrefab, definition } },
                Array.Empty<GameObject>(),
                new Dictionary<string, GameObject>(),
                requestPrefab =>
                {
                    beganPlacement = requestPrefab == buildingPrefab;
                    return true;
                },
                _ => true,
                _ => { },
                amount => activePlacementCost = amount);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                buildingPrefab,
                price: 1234,
                focusProducerOnSuccess: true);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 77);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.PlacementStarted, result.ResultCode);
            Assert.AreEqual(1234, result.Price);
            Assert.AreEqual(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingPrefab.name), result.ItemId.ToString());
            Assert.IsTrue(beganPlacement);
            Assert.AreEqual(1234, activePlacementCost);

            using EntityQuery queueQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiCampItemCommandQueueComponent>());
            Entity queueEntity = queueQuery.GetSingletonEntity();
            Assert.AreEqual(0, world.EntityManager.GetBuffer<BuildingUiCampItemCommandRequestElement>(queueEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buildingPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_QueuesUnitProductionAndWritesResult()
    {
        using World world = new("BuildingUiCampItemCommandUnitProductionTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Vehicle");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 21,
                displayName: "Vehicle Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 5678,
                focusProducerOnSuccess: false);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 88);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueued, result.ResultCode);
            Assert.AreEqual(5678, result.Price);
            Assert.AreEqual(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(unitPrefab.name), result.ItemId.ToString());
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
            Assert.AreEqual(0, producer.PendingProductions[0].ProductionIndex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_RejectsFullProductionSlotsAndRefunds()
    {
        using World world = new("BuildingUiCampItemCommandFullProductionSlotsTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Soldier");
        try
        {
            Entity occupiedUnit = world.EntityManager.CreateEntity(typeof(UnitHealth));
            world.EntityManager.SetComponentData(occupiedUnit, new UnitHealth { Current = 10, Max = 10 });
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 24,
                displayName: "Soldier Tent",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            producer.ProductionSpawnLocalPositions = new[] { Vector3.zero };
            producer.ProducedUnitSlots = new[] { occupiedUnit };
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            int dollars = 10000;
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager,
                trySpendDollars: amount =>
                {
                    if (dollars < amount)
                        return false;

                    dollars -= amount;
                    return true;
                },
                refundDollars: amount => dollars += amount);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 1200,
                focusProducerOnSuccess: false);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 88);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueueFull, result.ResultCode);
            Assert.AreEqual("Soldier Tent", result.RequiredBuildingDisplayName.ToString());
            Assert.AreEqual(10000, dollars);
            Assert.AreEqual(0, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingUiCampItemCommandRequest_RejectsGlobalProductionQueueLimitAndRefunds()
    {
        using World world = new("BuildingUiCampItemCommandGlobalQueueLimitTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Requestable Soldier");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 25,
                displayName: "Soldier Tent",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            producer.PendingProductions.Add(new RuntimeBuildingEntity.PendingProduction { Prefab = unitPrefab });
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            int dollars = 10000;
            BuildingProductionRequestSystemHelper.Context context = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager,
                trySpendDollars: amount =>
                {
                    if (dollars < amount)
                        return false;

                    dollars -= amount;
                    return true;
                },
                refundDollars: amount => dollars += amount,
                maxQueuedUnitProductions: 1);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 1200,
                focusProducerOnSuccess: false);
            requestSystem.ProcessPendingUiCampItemCommands(world.EntityManager, context, frameCount: 88);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(0, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.GlobalProductionQueueFull, result.ResultCode);
            Assert.AreEqual(10000, dollars);
            Assert.AreEqual(1, producer.PendingProductions.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingRuntimeState_ProcessesQueuedUiProductionCommand()
    {
        using World world = new("BuildingRuntimeStateQueuedUiProductionTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        var boundarySystem = new BuildingRuntimeProcessingCompositionSystemHelper();
        var runtimeQuerySystem = new BuildingRuntimeReadModelCompositionSystemHelper();
        GameObject unitPrefab = new("Runtime Boundary Unit");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 42,
                displayName: "Boundary Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            Entity boundaryEntity = world.EntityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
            using EntityQuery boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            BuildingProductionRequestSystemHelper.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                world.EntityManager,
                productionSystem);

            int requestId = requestSystem.EnqueueCreateUnitFromSelectedBuilding(
                world.EntityManager,
                producer.Id,
                productionIndex: 0,
                frameCount: 42);

            boundarySystem.Update(
                new BuildingDefinitionPrefabSystemHelper(),
                new BuildingRuntimeSpawnCompositionSystemHelper(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceCompositionSystemHelper(),
                world.EntityManager,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 42);

            Assert.IsTrue(world.EntityManager.Exists(boundaryEntity));
            Assert.IsTrue(requestSystem.TryGetUiProductionCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiProductionCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiProductionCommandResultElement.Queued, result.ResultCode);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingRuntimeState_ProcessesQueuedCampItemCommand()
    {
        using World world = new("BuildingRuntimeStateQueuedCampItemTest");
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        var boundarySystem = new BuildingRuntimeProcessingCompositionSystemHelper();
        var runtimeQuerySystem = new BuildingRuntimeReadModelCompositionSystemHelper();
        GameObject unitPrefab = new("Runtime Boundary Vehicle");
        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 43,
                displayName: "Boundary Vehicle Factory",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            world.EntityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
            using EntityQuery boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            BuildingProductionRequestSystemHelper.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                world.EntityManager);
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                world.EntityManager,
                productionSystem);

            int requestId = requestSystem.EnqueueCampItemRequest(
                world.EntityManager,
                unitPrefab,
                price: 250,
                focusProducerOnSuccess: false);

            boundarySystem.Update(
                new BuildingDefinitionPrefabSystemHelper(),
                new BuildingRuntimeSpawnCompositionSystemHelper(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceCompositionSystemHelper(),
                world.EntityManager,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 42);

            Assert.IsTrue(requestSystem.TryGetUiCampItemCommandResult(
                world.EntityManager,
                requestId,
                out BuildingUiCampItemCommandResultElement result));
            Assert.AreEqual(1, result.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueued, result.ResultCode);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void CountRuntimeProducedUnitsForFaction_UsesProducedUnitReadModel()
    {
        using World world = new("CountRuntimeProducedUnitsForFaction_UsesProducedUnitReadModel");
        EntityManager em = world.EntityManager;
        var runtimeQuerySystem = new BuildingRuntimeReadModelCompositionSystemHelper();
        Entity producedUnit = em.CreateEntity(typeof(UnitHealth));
        em.SetComponentData(producedUnit, new UnitHealth { Current = 10, Max = 10 });
        Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
            em.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
        producedUnitRows.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 84,
            ProductionSlotBuildingRuntimeId = 84,
            ProductionIndex = 0,
            ProductionSlotIndex = 0,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            HasOwnerFaction = 1,
            Unit = producedUnit,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });

        RuntimeBuildingEntity producer = new()
        {
            Id = 84,
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProducedUnits = null
        };
        Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
        {
            [producer.Id] = producer
        };
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = CreateRuntimeQueryContext(
            runtimeBuildings,
            em,
            new BuildingProductionQueueCompositionSystemHelper(),
            boundaryEntity);

        Assert.AreEqual(
            1,
            runtimeQuerySystem.CountRuntimeProducedUnitsForFaction(
                runtimeQueryContext,
                FactionIdentity.PlayerFactionId,
                "unit_inf_regular"));
        Assert.AreEqual(
            0,
            runtimeQuerySystem.CountRuntimeProducedUnitsForFaction(
                runtimeQueryContext,
                FactionIdentity.PlayerFactionId,
                "unit_inf_rifle"));
        Assert.IsNull(producer.ProducedUnits);
    }

    [Test]
    public void BuildingRuntimeState_ProductionSummaryUsesProducedUnitReadModel()
    {
        using World world = new("BuildingRuntimeState_ProductionSummaryUsesProducedUnitReadModel");
        EntityManager em = world.EntityManager;
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        var boundarySystem = new BuildingRuntimeProcessingCompositionSystemHelper();
        var runtimeQuerySystem = new BuildingRuntimeReadModelCompositionSystemHelper();
        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
        GameObject unitPrefab = new("Unit_Inf_Regular");
        try
        {
            definitionSystem.RebuildSpawnablesLookup(null, new List<GameObject> { unitPrefab });
            Entity producedUnit = em.CreateEntity(typeof(UnitHealth));
            em.SetComponentData(producedUnit, new UnitHealth { Current = 10, Max = 10 });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
            producedUnitRows.Add(new BuildingProducedUnitReadModel
            {
                BuildingRuntimeId = 86,
                ProductionSlotBuildingRuntimeId = 86,
                ProductionIndex = 0,
                ProductionSlotIndex = 0,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                HasOwnerFaction = 1,
                Unit = producedUnit,
                UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
            });

            RuntimeBuildingEntity producer = new()
            {
                Id = 86,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                ProducedUnits = null
            };
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            using EntityQuery boundaryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            BuildingProductionRequestSystemHelper.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                em);
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                em,
                productionSystem,
                boundaryEntity);

            boundarySystem.Update(
                definitionSystem,
                new BuildingRuntimeSpawnCompositionSystemHelper(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceCompositionSystemHelper(),
                em,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 0);

            DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries =
                em.GetBuffer<BuildingRuntimeUnitProductionSummary>(boundaryEntity, true);
            AssertProducedUnitSummary(
                summaries,
                FactionIdentity.PlayerFactionId,
                new FixedString128Bytes("unit_inf_regular"),
                producedCount: 1,
                queuedCount: 0);
            Assert.IsNull(producer.ProducedUnits);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void BuildingRuntimeState_FactionSummarySignatureUsesEcsResourceStorage()
    {
        using World world = new("BuildingRuntimeState_FactionSummarySignatureUsesEcsResourceStorage");
        EntityManager em = world.EntityManager;
        var requestSystem = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        var boundarySystem = new BuildingRuntimeProcessingCompositionSystemHelper();
        var runtimeQuerySystem = new BuildingRuntimeReadModelCompositionSystemHelper();
        GameObject unitPrefab = new("Runtime Resource Signature Unit");
        try
        {
            Entity resourceEntity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(resourceEntity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 92,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 100,
                FuelStorageCapacity = 100,
                OilBarrelsPerDay = 2f,
                FuelBarrelsPerDay = 1f,
                StoredOilBarrels = 10f,
                StoredFuelBarrels = 5f
            });
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 92,
                displayName: "Resource Signature Producer",
                unitPrefab,
                hasOwnerFaction: true,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            producer.CombatEntity = resourceEntity;
            producer.Definition.OilStorageCapacity = 100;
            producer.Definition.FuelStorageCapacity = 100;
            producer.Definition.OilBarrelsPerDay = 2f;
            producer.Definition.FuelBarrelsPerDay = 1f;
            producer.StoredOilBarrels = 0f;
            producer.StoredFuelBarrels = 0f;
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings = new()
            {
                [producer.Id] = producer
            };
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            using EntityQuery boundaryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            BuildingProductionRequestSystemHelper.Context productionContext = CreateProducerSelectionContext(
                runtimeBuildings,
                productionSystem,
                unitPrefab,
                em);
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = CreateRuntimeQueryContext(
                runtimeBuildings,
                em,
                productionSystem,
                boundaryEntity);

            boundarySystem.Update(
                new BuildingDefinitionPrefabSystemHelper(),
                new BuildingRuntimeSpawnCompositionSystemHelper(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceCompositionSystemHelper(),
                em,
                boundaryQuery,
                runtimeBuildings,
                now: 20f,
                frameCount: 0);

            AssertFactionSummaryResourceStorage(em, boundaryEntity, FactionIdentity.PlayerFactionId, 10f, 5f);

            em.SetComponentData(resourceEntity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 92,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 100,
                FuelStorageCapacity = 100,
                OilBarrelsPerDay = 2f,
                FuelBarrelsPerDay = 1f,
                StoredOilBarrels = 25f,
                StoredFuelBarrels = 9f
            });

            boundarySystem.Update(
                new BuildingDefinitionPrefabSystemHelper(),
                new BuildingRuntimeSpawnCompositionSystemHelper(),
                default,
                requestSystem,
                productionContext,
                runtimeQuerySystem,
                runtimeQueryContext,
                new FactionResourceCompositionSystemHelper(),
                em,
                boundaryQuery,
                runtimeBuildings,
                now: 20.25f,
                frameCount: 1);

            AssertFactionSummaryResourceStorage(em, boundaryEntity, FactionIdentity.PlayerFactionId, 25f, 9f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void TryQueuePlayerUnitFromBuilding_UsesProducedUnitReadModelSlotOccupancy()
    {
        using World world = new("TryQueuePlayerUnitFromBuilding_UsesProducedUnitReadModelSlotOccupancy");
        EntityManager em = world.EntityManager;
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject unitPrefab = new("Unit_Inf_Regular");
        try
        {
            Entity producedUnit = em.CreateEntity(typeof(UnitHealth));
            em.SetComponentData(producedUnit, new UnitHealth { Current = 10, Max = 10 });
            Entity boundaryEntity = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            DynamicBuffer<BuildingProducedUnitReadModel> producedUnitRows =
                em.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
            producedUnitRows.Add(new BuildingProducedUnitReadModel
            {
                BuildingRuntimeId = 91,
                ProductionSlotBuildingRuntimeId = 91,
                ProductionIndex = 0,
                ProductionSlotIndex = 0,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                HasOwnerFaction = 1,
                Unit = producedUnit,
                UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
            });

            RuntimeBuildingEntity producer = new()
            {
                Id = 91,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                ProductionSpawnLocalPositions = new[] { Vector3.zero },
                ProducedUnitSlots = new Entity[1],
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>(),
                Definition = new BuildingDefinition
                {
                    DisplayName = "Read Model Factory",
                    ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                    {
                        new() { SpawnUnitPrefab = unitPrefab }
                    }
                }
            };
            BuildingProductionQueueCompositionSystemHelper.QueueContext queueContext = new(
                new[] { unitPrefab },
                new Dictionary<string, GameObject>(),
                new BuildingProductionSlotUtilitySystemHelper(),
                null,
                null,
                (EntityManager _, out Entity entity) =>
                {
                    entity = boundaryEntity;
                    return true;
                });

            Assert.IsFalse(productionSystem.TryQueuePlayerUnitFromBuilding(
                queueContext,
                producer,
                0,
                unitPrefab,
                em,
                10f));
            Assert.AreEqual(0, producer.PendingProductions.Count);
            Assert.AreEqual(Entity.Null, producer.ProducedUnitSlots[0]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(unitPrefab);
        }
    }

    [Test]
    public void ResolveProductionDurationSeconds_UsesUnitAuthoringDuration()
    {
        GameObject prefab = new("Unit_Infantry_Test");
        try
        {
            UnitGridAuthoring authoring = prefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(authoring, "productionDurationSeconds", 12.5f);

            BuildingProductionQueueCompositionSystemHelper system = CreateProductionSystem();

            Assert.AreEqual(12.5f, system.ResolveProductionDurationSeconds(prefab), 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_UsesConfiguredTransportAuthoring()
    {
        GameObject producedPrefab = new("Unit_Infantry_Test");
        GameObject transportPrefab = new("Unit_Veh_Helicopter_Transport");
        try
        {
            UnitGridAuthoring producedAuthoring = producedPrefab.AddComponent<UnitGridAuthoring>();
            UnitGridAuthoring transportAuthoring = transportPrefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(producedAuthoring, "productionTransportPrefab", transportPrefab);
            SetAuthoringField(transportAuthoring, "productionTransportArrivalSeconds", 8f);
            SetAuthoringField(transportAuthoring, "productionTransportHoldForNextReadySeconds", 3f);
            SetAuthoringField(transportAuthoring, "productionTransportMaxConcurrent", 4);

            BuildingProductionQueueCompositionSystemHelper system = CreateProductionSystem();
            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { transportPrefab },
                new Dictionary<string, GameObject> { ["unit_veh_helicopter_transport"] = transportPrefab },
                null);

            Assert.AreSame(transportPrefab, settings.TransportPrefab);
            Assert.AreEqual(8f, settings.ArrivalSeconds, 0.0001f);
            Assert.AreEqual(3f, settings.HoldForNextReadySeconds, 0.0001f);
            Assert.AreEqual(4, settings.MaxConcurrent);
            Assert.AreEqual(BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter, settings.Mode);
            Assert.IsFalse(settings.RequiresAirportRunway);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void ResolveProductionTransportSettings_DefaultsLargeVehicleToPlaneTransport()
    {
        GameObject producedPrefab = new("Unit_Veh_TankHeavy_Test");
        GameObject helicopterPrefab = new("Unit_Veh_Helicopter_Transport");
        GameObject planePrefab = new("Unit_Veh_Plane_Transport");
        try
        {
            producedPrefab.AddComponent<UnitGridAuthoring>();
            helicopterPrefab.AddComponent<UnitGridAuthoring>();
            planePrefab.AddComponent<UnitGridAuthoring>();

            var prefabsByKey = new Dictionary<string, GameObject>
            {
                ["unit_veh_helicopter_transport"] = helicopterPrefab,
                ["unit_veh_plane_transport"] = planePrefab
            };
            BuildingProductionQueueCompositionSystemHelper system = CreateProductionSystem();
            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings settings = system.ResolveProductionTransportSettings(
                producedPrefab,
                new[] { helicopterPrefab, planePrefab },
                prefabsByKey,
                (GameObject _, out Bounds bounds) =>
                {
                    bounds = new Bounds(Vector3.zero, new Vector3(3f, 1f, 2f));
                    return true;
                });

            Assert.AreSame(planePrefab, settings.TransportPrefab);
            Assert.AreEqual(BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Plane, settings.Mode);
            Assert.IsTrue(settings.RequiresAirportRunway);
            Assert.AreEqual(1, settings.MaxConcurrent);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(producedPrefab);
            UnityEngine.Object.DestroyImmediate(helicopterPrefab);
            UnityEngine.Object.DestroyImmediate(planePrefab);
        }
    }

    [Test]
    public void PrewarmConfiguredProductionTransportPools_ParentsSelfArrivingAirUnitsUnderRuntimeRoot()
    {
        GameObject runtimeRoot = new("RuntimeTransports_Test");
        GameObject airPrefab = new("Unit_Veh_Jet_02");
        using var world = new World(nameof(PrewarmConfiguredProductionTransportPools_ParentsSelfArrivingAirUnitsUnderRuntimeRoot));
        try
        {
            UnitGridAuthoring airAuthoring = airPrefab.AddComponent<UnitGridAuthoring>();
            SetAuthoringField(airAuthoring, "isAirUnit", true);
            SetAuthoringField(airAuthoring, "productionTransportArrivalSeconds", 2f);
            SetAuthoringField(airAuthoring, "productionTransportHoldForNextReadySeconds", 1f);
            SetAuthoringField(airAuthoring, "productionTransportMaxConcurrent", 1);
            SetAuthoringField(airAuthoring, "productionTransportRequiresAirportRunway", true);

            BuildingProductionQueueCompositionSystemHelper productionSystem = CreateProductionSystem();
            BuildingProductionTransportPresentationSystemHelper transportSystem = new();
            transportSystem.SetRuntimeRoot(runtimeRoot.transform);

            transportSystem.PrewarmConfiguredProductionTransportPools(
                productionSystem,
                new[] { airPrefab },
                new Dictionary<string, GameObject> { ["unit_veh_jet_02"] = airPrefab },
                null,
                world.GetOrCreateSystemManaged<BuildingVisualSystem>());

            Assert.AreEqual(2, runtimeRoot.transform.childCount, "Self-arriving air transport prewarm should still keep the default warm pool size.");
            for (int i = 0; i < runtimeRoot.transform.childCount; i++)
            {
                Transform child = runtimeRoot.transform.GetChild(i);
                Assert.AreSame(runtimeRoot.transform, child.parent);
                Assert.IsFalse(child.gameObject.activeSelf);
                StringAssert.StartsWith("Unit_Veh_Jet_02", child.name);
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Assert.AreNotEqual(
                    "Unit_Veh_Jet_02(Clone)",
                    root.name,
                    "Prewarmed transport pool instances must be owned by RuntimeTransports, not leaked as scene-root unit clones.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
            UnityEngine.Object.DestroyImmediate(airPrefab);
        }
    }

    [Test]
    public void GetProgress_ComputesRemainingAndCanCapTransportProgress()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = transportPrefab
            };

            var system = new BuildingProductionQueueCompositionSystemHelper();
            BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress = system.GetProgress(pending, 9.9f, true);

            Assert.AreEqual(10f, progress.DurationSeconds);
            Assert.AreEqual(0.1f, progress.RemainingSeconds, 0.0001f);
            Assert.AreEqual(0.97f, progress.Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void TransportLaunchAndDelay_UsesArrivalWindow()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 20f,
                TransportPrefab = transportPrefab,
                TransportArrivalSeconds = 5f
            };

            var system = new BuildingProductionQueueCompositionSystemHelper();

            Assert.AreEqual(15f, system.GetTransportLaunchAt(pending));
            Assert.IsFalse(system.ShouldLaunchTransport(pending, 14.9f));
            Assert.IsTrue(system.ShouldLaunchTransport(pending, 15f));

            system.DelayPendingProduction(pending, 2f);

            Assert.AreEqual(2f, pending.StartedAt);
            Assert.AreEqual(22f, pending.ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void RebuildPendingProductionTimeline_ChainsQueuedItemsAfterActiveProduction()
    {
        var first = new TestPendingProduction { StartedAt = 10f, ReadyAt = 20f };
        var second = new TestPendingProduction { StartedAt = 12f, ReadyAt = 22f };
        var third = new TestPendingProduction { StartedAt = 13f, ReadyAt = 18f };
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionQueueCompositionSystemHelper();
        system.RebuildPendingProductionTimeline(pending, now: 15f, preserveActiveProgress: true);

        Assert.AreEqual(10f, first.StartedAt, 0.0001f);
        Assert.AreEqual(20f, first.ReadyAt, 0.0001f);
        Assert.AreEqual(20f, second.StartedAt, 0.0001f);
        Assert.AreEqual(30f, second.ReadyAt, 0.0001f);
        Assert.AreEqual(30f, third.StartedAt, 0.0001f);
        Assert.AreEqual(35f, third.ReadyAt, 0.0001f);
    }

    [Test]
    public void RebuildPendingProductionTimeline_AfterActiveRemovalResetsNextActiveProgress()
    {
        var next = new TestPendingProduction { StartedAt = 0f, ReadyAt = 10f };
        var later = new TestPendingProduction { StartedAt = 0f, ReadyAt = 5f };
        var pending = new List<TestPendingProduction> { next, later };

        var system = new BuildingProductionQueueCompositionSystemHelper();
        system.RebuildPendingProductionTimeline(pending, now: 50f, preserveActiveProgress: false);
        BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress = system.GetProgress(next, 50f, capTransportProgress: false);

        Assert.AreEqual(50f, next.StartedAt, 0.0001f);
        Assert.AreEqual(60f, next.ReadyAt, 0.0001f);
        Assert.AreEqual(60f, later.StartedAt, 0.0001f);
        Assert.AreEqual(65f, later.ReadyAt, 0.0001f);
        Assert.AreEqual(0f, progress.Progress01, 0.0001f);
    }

    [Test]
    public void ReadinessHelpers_ReportReadyAndSoonStates()
    {
        var pending = new TestPendingProduction
        {
            ReadyAt = 20f
        };

        var system = new BuildingProductionQueueCompositionSystemHelper();

        Assert.IsFalse(system.IsReady(pending, 19.9f));
        Assert.IsTrue(system.IsReady(pending, 20f));
        Assert.IsTrue(system.IsReadyWithin(pending, 18f, 2.5f));
        Assert.IsFalse(system.IsReadyWithin(pending, 16f, 2.5f));
    }

    [Test]
    public void PruneProducedUnits_RemovesDeadUnitsAndClearsDeadSlots()
    {
        using World world = new("BuildingProductionQueueCompositionSystemHelperTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 5, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        var producedUnits = new List<Entity> { alive, dead, Entity.Null };
        GameObject deadPrefab = new("DeadPrefab");
        var producedUnitPrefabs = new Dictionary<Entity, GameObject>
        {
            [dead] = deadPrefab
        };
        var producedUnitSourceKeys = new Dictionary<Entity, FixedString64Bytes>
        {
            [alive] = new FixedString64Bytes("AlivePrefab"),
            [dead] = new FixedString64Bytes("DeadPrefab")
        };
        Entity[] slots = { dead, alive };

        try
        {
            var system = new BuildingProductionQueueCompositionSystemHelper();
            system.PruneProducedUnits(producedUnits, slots, producedUnitPrefabs, entityManager, producedUnitSourceKeys);

            Assert.AreEqual(1, producedUnits.Count);
            Assert.AreEqual(alive, producedUnits[0]);
            Assert.IsFalse(producedUnitPrefabs.ContainsKey(dead));
            Assert.IsFalse(producedUnitSourceKeys.ContainsKey(dead));
            Assert.AreEqual(new FixedString64Bytes("AlivePrefab"), producedUnitSourceKeys[alive]);
            Assert.AreEqual(Entity.Null, slots[0]);
            Assert.AreEqual(alive, slots[1]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(deadPrefab);
        }
    }

    [Test]
    public void TransportPendingQueries_FindReadyAndSoonEntries()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var ready = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 10f
            };
            var soon = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 13f
            };
            var later = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 20f
            };
            var pending = new List<TestPendingProduction> { soon, ready, later };

            var system = new BuildingProductionQueueCompositionSystemHelper();

            Assert.AreSame(ready, system.FindNextReadyTransportPending(pending, transportPrefab, 10f));
            Assert.AreSame(soon, system.FindNextSoonTransportPending(pending, transportPrefab, 10f, 4f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void PendingRemovalHelpers_RemoveByReferenceAndIndex()
    {
        var first = new TestPendingProduction();
        var second = new TestPendingProduction();
        var third = new TestPendingProduction();
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionQueueCompositionSystemHelper();

        Assert.IsTrue(system.RemovePendingProduction(pending, second));
        Assert.AreEqual(2, pending.Count);
        Assert.AreSame(first, pending[0]);
        Assert.AreSame(third, pending[1]);

        Assert.IsTrue(system.RemovePendingAt(pending, 0));
        Assert.AreEqual(1, pending.Count);
        Assert.AreSame(third, pending[0]);
    }

    [Test]
    public void BuildingGameplayComposition_InitializesRuntimeDollarsFromInitialUnitsConfig()
    {
        var placementConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        var initialUnitsConfig = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringSceneConfigAsset>();
        BuildingGameplayResultCompositionSystemHelper.Result result = default;
        try
        {
            SetPrivateField(initialUnitsConfig, "initialDollars", 12345);
            SetPrivateField(placementConfig, "initialUnitsConfig", initialUnitsConfig);

            var composition = new BuildingGameplayCompositionSystemHelper();
            result = composition.Initialize(
                placementConfig,
                worldCamera: null,
                runtimeTransportsRoot: null,
                runtimeUiRoot: null,
                roadFootprintState: default,
                factionVisuals: null,
                dayNight: null,
                resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
                tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);

            Assert.AreEqual(12345, result.UiCommand.CurrentDollars(result.UiCommandContext));
        }
        finally
        {
            result.Dispose?.Invoke();
            UnityEngine.Object.DestroyImmediate(initialUnitsConfig);
            UnityEngine.Object.DestroyImmediate(placementConfig);
        }
    }

    [Test]
    public void BuildingGameplayComposition_CampBuildingRequestStartsConfiguredPlacement()
    {
        var placementConfig = ScriptableObject.CreateInstance<BuildingPlacementSystemConfig>();
        var initialUnitsConfig = ScriptableObject.CreateInstance<InitialUnitsSpawnerAuthoringSceneConfigAsset>();
        var buildingPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        BuildingGameplayResultCompositionSystemHelper.Result result = default;
        try
        {
            buildingPrefab.name = "Soldier Base";
            SetPrivateField(initialUnitsConfig, "initialDollars", 10000);
            SetPrivateField(placementConfig, "initialUnitsConfig", initialUnitsConfig);
            SetPrivateField(placementConfig, "spawnables", new List<GameObject> { buildingPrefab });

            var composition = new BuildingGameplayCompositionSystemHelper();
            result = composition.Initialize(
                placementConfig,
                worldCamera: null,
                runtimeTransportsRoot: null,
                runtimeUiRoot: null,
                roadFootprintState: default,
                factionVisuals: null,
                dayNight: null,
                resolveSpawnableLookupKey: BuildingSpawnPrefabLookupKeyPrefabSystemHelper.ResolveSpawnableLookupKey,
                tryGetBuildingDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                tryGetUnitDefinitionMetadata: BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);

            BuildingUiCommandSystemHelper.CampRequestFailure failure = result.UiCommand.TryRequestCampItem(
                result.UiCommandContext,
                buildingPrefab,
                price: 500,
                out _,
                focusProducerOnSuccess: true);

            Assert.AreEqual(BuildingUiCommandSystemHelper.CampRequestFailure.None, failure);
        }
        finally
        {
            result.Dispose?.Invoke();
            UnityEngine.Object.DestroyImmediate(buildingPrefab);
            UnityEngine.Object.DestroyImmediate(initialUnitsConfig);
            UnityEngine.Object.DestroyImmediate(placementConfig);
        }
    }

    private sealed class TestPendingProduction : BuildingProductionQueueCompositionSystemHelper.IPendingProduction
    {
        public int ProductionIndex { get; set; }
        public GameObject Prefab { get; set; }
        public float StartedAt { get; set; }
        public float ReadyAt { get; set; }
        public int ReservedProductionSlotIndex { get; set; }
        public GameObject TransportPrefab { get; set; }
        public float TransportArrivalSeconds { get; set; }
        public float TransportHoldForNextReadySeconds { get; set; }
        public int TransportMaxConcurrent { get; set; }
        public BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }

    private static void SetAuthoringField<T>(UnitGridAuthoring authoring, string fieldName, T value)
    {
        FieldInfo field = typeof(UnitGridAuthoring).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"{nameof(UnitGridAuthoring)} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(authoring, value);
    }

    private static BuildingProductionQueueCompositionSystemHelper CreateProductionSystem()
    {
        var system = new BuildingProductionQueueCompositionSystemHelper();
        system.ConfigureUnitProductionMetadataResolver(BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata);
        return system;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = FindPrivateField(target.GetType(), fieldName);
        Assert.IsNotNull(field, $"{target.GetType().Name} must expose serialized field '{fieldName}' for this test.");
        field.SetValue(target, value);
    }

    private static RuntimeBuildingEntity CreateProducerBuilding(
        int id,
        string displayName,
        GameObject unitPrefab,
        bool hasOwnerFaction,
        byte ownerFactionId)
    {
        return new RuntimeBuildingEntity
        {
            Id = id,
            HasOwnerFaction = hasOwnerFaction,
            OwnerFactionId = ownerFactionId,
            Definition = new BuildingDefinition
            {
                DisplayName = displayName,
                ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                {
                    new() { SpawnUnitPrefab = unitPrefab }
                }
            },
            ProducedUnits = new List<Entity>(),
            ProducedUnitPrefabs = new Dictionary<Entity, GameObject>(),
            PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>()
        };
    }

    private static BuildingProductionRequestSystemHelper.Context CreateCampItemRequestContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        IReadOnlyList<BuildingDefinition> configuredSpawnableDefinitions,
        IReadOnlyDictionary<GameObject, BuildingDefinition> configuredDefinitionsByPrefab,
        IReadOnlyList<GameObject> unitPrefabs,
        IReadOnlyDictionary<string, GameObject> unitPrefabsByKey,
        BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate beginPlacement,
        BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestSystemHelper.RefundDollarsDelegate refundDollars,
        BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate setActivePlacementCost)
    {
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        BuildingProductionQueueCompositionSystemHelper.QueueContext queueContext = new(
            unitPrefabs,
            unitPrefabsByKey,
            new BuildingProductionSlotUtilitySystemHelper(),
            null,
            null);

        return new BuildingProductionRequestSystemHelper.Context(
            runtimeBuildings,
            configuredSpawnableDefinitions,
            configuredDefinitionsByPrefab,
            unitPrefabs,
            unitPrefabsByKey,
            100000,
            25,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            null,
            beginPlacement,
            trySpendDollars,
            refundDollars,
            setActivePlacementCost,
            null,
            _ => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }

    private static BuildingProductionRequestSystemHelper.Context CreateProducerSelectionContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        GameObject unitPrefab,
        EntityManager entityManager = default,
        BuildingProductionRequestSystemHelper.TryQueuePlayerUnitDelegate tryQueuePlayerUnit = null,
        BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate trySpendDollars = null,
        BuildingProductionRequestSystemHelper.RefundDollarsDelegate refundDollars = null,
        int maxQueuedUnitProductions = 25)
    {
        var unitPrefabs = new List<GameObject> { unitPrefab };
        BuildingProductionQueueCompositionSystemHelper.QueueContext queueContext = new(
            unitPrefabs,
            new Dictionary<string, GameObject>(),
            new BuildingProductionSlotUtilitySystemHelper(),
            null,
            null);

        return new BuildingProductionRequestSystemHelper.Context(
            runtimeBuildings,
            null,
            null,
            unitPrefabs,
            new Dictionary<string, GameObject>(),
            100000,
            maxQueuedUnitProductions,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            null,
            null,
            trySpendDollars ?? (_ => true),
            refundDollars ?? (_ => { }),
            _ => { },
            tryQueuePlayerUnit ?? ((building, productionIndex, spawnUnitPrefab) => productionSystem.TryQueuePlayerUnitFromBuilding(
                queueContext,
                building,
                productionIndex,
                spawnUnitPrefab,
                entityManager,
                10f)),
            _ => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }

    private static BuildingRuntimeReadModelCompositionSystemHelper.Context CreateRuntimeQueryContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        EntityManager entityManager,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        Entity runtimeBoundaryEntity = default)
    {
        bool TryGetEntityManager(out EntityManager em)
        {
            em = entityManager;
            return entityManager.World != null && entityManager.World.IsCreated;
        }

        bool TryGetRuntimeBoundaryEntity(EntityManager em, out Entity boundaryEntity)
        {
            boundaryEntity = runtimeBoundaryEntity;
            return boundaryEntity != Entity.Null &&
                   em.World != null &&
                   em.World.IsCreated &&
                   em.Exists(boundaryEntity);
        }

        return new BuildingRuntimeReadModelCompositionSystemHelper.Context(
            runtimeBuildings,
            TryGetEntityManager,
            TryGetRuntimeBoundaryEntity,
            productionSystem,
            BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey,
            _ => false,
            (building, normalizedId) => BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(building?.Definition, normalizedId),
            (prefab, normalizedId) => BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab != null ? prefab.name : string.Empty) == normalizedId,
            (RuntimeBuildingEntity building, out Vector3 worldPosition) =>
            {
                worldPosition = Vector3.zero;
                return false;
            },
            (RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
            {
                goal = default;
                return false;
            },
            (RuntimeBuildingEntity building, int2 currentCell, int2 unitFootprint) => false,
            definition => false,
            (
                byte attackerFactionId,
                Entity finalTarget,
                int2 finalTargetCell,
                int2 attackerCell,
                out Entity breachTarget,
                out int2 breachCell,
                out float3 breachPosition,
                out string reason) =>
            {
                breachTarget = Entity.Null;
                breachCell = default;
                breachPosition = default;
                reason = string.Empty;
                return false;
            });
    }

    private static void AssertProducedUnitSummary(
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries,
        byte factionId,
        FixedString128Bytes unitId,
        int producedCount,
        int queuedCount)
    {
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeUnitProductionSummary summary = summaries[i];
            if (summary.FactionId != factionId || !summary.UnitId.Equals(unitId))
                continue;

            Assert.AreEqual(producedCount, summary.ProducedCount);
            Assert.AreEqual(queuedCount, summary.QueuedCount);
            return;
        }

        Assert.Fail($"Missing production summary for faction={factionId}, unit={unitId.ToString()}.");
    }

    private static void AssertFactionSummaryResourceStorage(
        EntityManager em,
        Entity boundaryEntity,
        byte factionId,
        float expectedOilBarrels,
        float expectedFuelBarrels)
    {
        DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
            em.GetBuffer<BuildingRuntimeFactionSummary>(boundaryEntity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeFactionSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;

            Assert.AreEqual(expectedOilBarrels, summary.StoredOilBarrels);
            Assert.AreEqual(expectedFuelBarrels, summary.StoredFuelBarrels);
            return;
        }

        Assert.Fail($"Missing faction summary for faction={factionId}.");
    }

    private static FieldInfo FindPrivateField(System.Type type, string fieldName)
    {
        for (System.Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return field;
        }

        return null;
    }
}
#endif
