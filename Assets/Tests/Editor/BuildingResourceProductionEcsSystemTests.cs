using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingResourceProductionEcsSystemTests
{
    public static void RunFocusedValidation()
    {
        var tests = new BuildingResourceProductionEcsSystemTests();
        try
        {
            tests.ApplyTick_ExtractsOilUpToCapacity();
            tests.ApplyTick_FullOilStorageDoesNotOverflowVersionOrAllocate();
            tests.CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeOilPumps();
            tests.CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeFuelBladders();
            tests.ApplyStorageQuery_WritesOilToEcsStorageWithCapacityAndVersion();
            tests.ApplyStorageQuery_ConvertsRefineryOilIntoFuelWithEfficiencyAndCapacity();
            tests.ApplyStorageQuery_ConvertsStandardAndLargeRefineriesWithDifferentRates();
            tests.ApplyTick_ConvertsOilIntoFuel();
            tests.ApplyTick_DoesNotConvertOilWhenFuelStorageIsFull();
            tests.UpdateResourceProduction_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale();
            tests.ProductionRuntimeTick_UsesProvidedDeltaTimeForThrottledResourceProduction();
            tests.ProductionTickSync_PreservesLiveEcsFuelWhenManagedMirrorIsStale();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterProductionUpdate();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterHaulerUpdate();
            tests.AutomaticFuelLogisticsRoute_PairsTrayWithFactionOilAndRefinery();
            tests.AutomaticFuelLogisticsRoute_PairsTankerWithFactionRefineryAndFuelStorage();
            tests.AutomaticFuelLogisticsSignature_ChangesOnlyWhenRelevantStateChanges();
            tests.AutomaticFuelLogisticsAssignmentScan_SkipsWithinStableRefreshWindow();
            tests.AutomaticFuelLogisticsReservation_ReservesSourceAndDestinationCapacity();
            tests.AutomaticFuelLogisticsTray_DispatchesToEmptyProducingOilPump();
            tests.FuelLogisticsApproach_UsesEffectiveRunwayPlacementRect();
            tests.AutomaticFuelLogisticsSeededTray_StartsOilHaulingWithoutRuntimeBuild();
            tests.AutomaticFuelLogisticsManualMove_IsNotOverridden();
            tests.AutomaticFuelLogisticsTray_ReissuesMoveWhenTargetHasNoActivePath();
            tests.AutomaticFuelLogisticsSeededTanker_StartsFuelHaulingWithoutRuntimeBuild();
            tests.AutomaticFuelLogisticsTray_NoRefineryCapacitySetsTypedIdleReason();
            tests.AutomaticFuelLogisticsTray_DestroyedSourceClearsReservation();
            tests.AutomaticFuelLogisticsTray_DestroyedDestinationClearsReservation();
            tests.AutomaticFuelLogisticsTray_DeadHaulerClearsReservation();
            tests.AutomaticFuelLogisticsTray_RouteInvalidationClearsReservation();
            tests.AutomaticFuelLogisticsSteadyState_DoesNotAllocateManagedMemory();
            tests.AutomaticFuelLogisticsCycle_TrayTransfersOilWithoutManualCommand();
            tests.AutomaticFuelLogisticsCycle_TankerTransfersFuelWithoutManualCommand();
            tests.AutomaticFuelLogisticsEnemyFaction_ProducesAndDeliversFuel();
            tests.AutomaticFuelLogisticsTanker_NoRefineryFuelSetsTypedIdleReason();
            tests.AutomaticFuelLogisticsTanker_NoFuelStorageSetsTypedIdleReason();
            tests.AutomaticFuelLogisticsTanker_FullFuelStorageSetsTypedIdleReason();
            tests.AutomaticFuelLogisticsTanker_NoRouteSetsTypedIdleReason();
            tests.AutomaticFuelLogisticsTanker_NoAvailableTankerDoesNotReserveFuel();
            Debug.Log("[BuildingResourceProductionEcsFocusedValidation] result=Passed tests=38");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingResourceProductionEcsFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ApplyTick_ExtractsOilUpToCapacity()
    {
        var storage = new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 20f,
            StoredOilBarrels = 9f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);

        Assert.AreEqual(10f, storage.StoredOilBarrels);
        Assert.AreEqual(0f, storage.StoredFuelBarrels);
        Assert.AreEqual(1f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
        Assert.AreEqual(1u, storage.Version);
    }

    [Test]
    public void ApplyTick_FullOilStorageDoesNotOverflowVersionOrAllocate()
    {
        var storage = new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 20f,
            StoredOilBarrels = 10f,
            Version = 7u
        };

        BuildingResourceProductionEcsSystem.TickResult warmup =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        float extracted = 0f;
        for (int i = 0; i < 64; i++)
        {
            BuildingResourceProductionEcsSystem.TickResult result =
                BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);
            extracted += result.OilExtractedBarrels;
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();

        Assert.AreEqual(10f, storage.StoredOilBarrels);
        Assert.AreEqual(0f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, warmup.OilExtractedBarrels);
        Assert.AreEqual(0f, extracted);
        Assert.AreEqual(7u, storage.Version);
        Assert.AreEqual(before, after);
    }

    [Test]
    public void CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeOilPumps()
    {
        var world = new World(nameof(CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeOilPumps));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            var helper = new BuildingRuntimeEntityCompositionSystemHelper();
            var context = new BuildingRuntimeEntityCompositionSystemHelper.Context(
                TryGetEntityManager,
                TryGetGridData,
                GetFootprintCenter,
                null,
                default,
                null,
                0f);
            var mapDefinition = new BuildingDefinition
            {
                DisplayName = "Map Oil Pump",
                MaxHealth = 250,
                FootprintCells = new Vector2Int(2, 2),
                OilStorageCapacity = 80,
                OilBarrelsPerDay = 24f
            };
            BuildingDefinition runtimeDefinition =
                BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(
                    mapDefinition,
                    new Vector2Int(3, 2));

            Entity mapEntity = helper.CreateBuildingCombatEntity(
                context,
                runtimeBuildingId: 301,
                originCell: new Vector2Int(4, 6),
                mapDefinition,
                FactionIdentity.PlayerFactionId,
                Quaternion.identity);
            Entity runtimeEntity = helper.CreateBuildingCombatEntity(
                context,
                runtimeBuildingId: 302,
                originCell: new Vector2Int(10, 6),
                runtimeDefinition,
                FactionIdentity.PlayerFactionId,
                Quaternion.identity);

            Assert.IsTrue(em.HasComponent<BuildingResourceStorageComponent>(mapEntity));
            Assert.IsTrue(em.HasComponent<BuildingResourceStorageComponent>(runtimeEntity));
            BuildingResourceStorageComponent mapStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(mapEntity);
            BuildingResourceStorageComponent runtimeStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(runtimeEntity);
            Assert.AreEqual(301, mapStorage.RuntimeBuildingId);
            Assert.AreEqual(302, runtimeStorage.RuntimeBuildingId);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, mapStorage.OwnerFactionId);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, runtimeStorage.OwnerFactionId);
            Assert.AreEqual(mapStorage.OilStorageCapacity, runtimeStorage.OilStorageCapacity);
            Assert.AreEqual(mapStorage.OilBarrelsPerDay, runtimeStorage.OilBarrelsPerDay);
            Assert.AreEqual(0, mapStorage.FuelStorageCapacity);
            Assert.AreEqual(0, runtimeStorage.FuelStorageCapacity);
            Assert.AreEqual(0f, mapStorage.StoredOilBarrels);
            Assert.AreEqual(0f, runtimeStorage.StoredOilBarrels);

            bool TryGetEntityManager(out EntityManager entityManager)
            {
                entityManager = em;
                return true;
            }

            bool TryGetGridData(
                out Entity entity,
                out GridConfig grid,
                out DynamicBuffer<GridRoad> roads,
                out DynamicBlockerComponent blockerData)
            {
                entity = gridEntity;
                grid = em.GetComponentData<GridConfig>(gridEntity);
                roads = em.GetBuffer<GridRoad>(gridEntity);
                blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
                return true;
            }

            static Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
            {
                return new Vector3(
                    grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                    grid.Origin.y,
                    grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
            }
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeFuelBladders()
    {
        var world = new World(nameof(CreateBuildingCombatEntity_UsesSameResourceStorageForMapAndRuntimeFuelBladders));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            var helper = new BuildingRuntimeEntityCompositionSystemHelper();
            var context = new BuildingRuntimeEntityCompositionSystemHelper.Context(
                TryGetEntityManager,
                TryGetGridData,
                GetFootprintCenter,
                null,
                default,
                null,
                0f);
            var mapDefinition = new BuildingDefinition
            {
                DisplayName = "Map Fuel Bladder",
                MaxHealth = 250,
                FootprintCells = new Vector2Int(2, 2),
                FuelStorageCapacity = 120
            };
            BuildingDefinition runtimeDefinition =
                BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(
                    mapDefinition,
                    new Vector2Int(3, 2));

            Entity mapEntity = helper.CreateBuildingCombatEntity(
                context,
                runtimeBuildingId: 311,
                originCell: new Vector2Int(4, 6),
                mapDefinition,
                FactionIdentity.PlayerFactionId,
                Quaternion.identity);
            Entity runtimeEntity = helper.CreateBuildingCombatEntity(
                context,
                runtimeBuildingId: 312,
                originCell: new Vector2Int(10, 6),
                runtimeDefinition,
                FactionIdentity.PlayerFactionId,
                Quaternion.identity);

            Assert.IsTrue(em.HasComponent<BuildingResourceStorageComponent>(mapEntity));
            Assert.IsTrue(em.HasComponent<BuildingResourceStorageComponent>(runtimeEntity));
            BuildingResourceStorageComponent mapStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(mapEntity);
            BuildingResourceStorageComponent runtimeStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(runtimeEntity);
            Assert.AreEqual(311, mapStorage.RuntimeBuildingId);
            Assert.AreEqual(312, runtimeStorage.RuntimeBuildingId);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, mapStorage.OwnerFactionId);
            Assert.AreEqual(FactionIdentity.PlayerFactionId, runtimeStorage.OwnerFactionId);
            Assert.AreEqual(0, mapStorage.OilStorageCapacity);
            Assert.AreEqual(0, runtimeStorage.OilStorageCapacity);
            Assert.AreEqual(120, mapStorage.FuelStorageCapacity);
            Assert.AreEqual(mapStorage.FuelStorageCapacity, runtimeStorage.FuelStorageCapacity);
            Assert.AreEqual(0f, mapStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, runtimeStorage.StoredFuelBarrels);

            bool TryGetEntityManager(out EntityManager entityManager)
            {
                entityManager = em;
                return true;
            }

            bool TryGetGridData(
                out Entity entity,
                out GridConfig grid,
                out DynamicBuffer<GridRoad> roads,
                out DynamicBlockerComponent blockerData)
            {
                entity = gridEntity;
                grid = em.GetComponentData<GridConfig>(gridEntity);
                roads = em.GetBuffer<GridRoad>(gridEntity);
                blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
                return true;
            }

            static Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid)
            {
                return new Vector3(
                    grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                    grid.Origin.y,
                    grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
            }
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void ApplyStorageQuery_WritesOilToEcsStorageWithCapacityAndVersion()
    {
        var world = new World(nameof(ApplyStorageQuery_WritesOilToEcsStorageWithCapacityAndVersion));
        try
        {
            EntityManager em = world.EntityManager;
            Entity oilPump = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(oilPump, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 41,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 10,
                OilBarrelsPerDay = 20f,
                StoredOilBarrels = 9f,
                Version = 3u
            });
            Entity fullOilPump = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(fullOilPump, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 42,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 10,
                OilBarrelsPerDay = 20f,
                StoredOilBarrels = 10f,
                Version = 8u
            });
            using EntityQuery storageQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuildingResourceStorageComponent>());

            BuildingResourceProductionEcsSystem.TickResult result =
                BuildingResourceProductionEcsSystem.ApplyStorageQuery(
                    em,
                    storageQuery,
                    secondsPerDay: 10f,
                    deltaTime: 1f,
                    oilBarrelsPerFuelBarrel: 2f);

            BuildingResourceStorageComponent oilStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump);
            BuildingResourceStorageComponent fullStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(fullOilPump);
            Assert.AreEqual(10f, oilStorage.StoredOilBarrels);
            Assert.AreEqual(4u, oilStorage.Version);
            Assert.AreEqual(10f, fullStorage.StoredOilBarrels);
            Assert.AreEqual(8u, fullStorage.Version);
            Assert.AreEqual(1f, result.OilExtractedBarrels);
            Assert.AreEqual(0f, result.FuelProducedBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void ApplyStorageQuery_ConvertsRefineryOilIntoFuelWithEfficiencyAndCapacity()
    {
        var world = new World(nameof(ApplyStorageQuery_ConvertsRefineryOilIntoFuelWithEfficiencyAndCapacity));
        try
        {
            EntityManager em = world.EntityManager;
            Entity refinery = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(refinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 51,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 10,
                FuelBarrelsPerDay = 20f,
                StoredOilBarrels = 12f,
                StoredFuelBarrels = 9f,
                Version = 5u
            });
            Entity emptyInputRefinery = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(emptyInputRefinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 52,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 10,
                FuelBarrelsPerDay = 20f,
                StoredOilBarrels = 0f,
                StoredFuelBarrels = 4f,
                Version = 6u
            });
            Entity fullOutputRefinery = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(fullOutputRefinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 53,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 6,
                FuelBarrelsPerDay = 60f,
                StoredOilBarrels = 20f,
                StoredFuelBarrels = 6f,
                Version = 7u
            });
            using EntityQuery storageQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuildingResourceStorageComponent>());

            BuildingResourceProductionEcsSystem.TickResult result =
                BuildingResourceProductionEcsSystem.ApplyStorageQuery(
                    em,
                    storageQuery,
                    secondsPerDay: 10f,
                    deltaTime: 1f,
                    oilBarrelsPerFuelBarrel: 2f);

            BuildingResourceStorageComponent converted =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery);
            BuildingResourceStorageComponent emptyInput =
                em.GetComponentData<BuildingResourceStorageComponent>(emptyInputRefinery);
            BuildingResourceStorageComponent fullOutput =
                em.GetComponentData<BuildingResourceStorageComponent>(fullOutputRefinery);
            Assert.AreEqual(10f, converted.StoredOilBarrels);
            Assert.AreEqual(10f, converted.StoredFuelBarrels);
            Assert.AreEqual(6u, converted.Version);
            Assert.AreEqual(0f, emptyInput.StoredOilBarrels);
            Assert.AreEqual(4f, emptyInput.StoredFuelBarrels);
            Assert.AreEqual(6u, emptyInput.Version);
            Assert.AreEqual(20f, fullOutput.StoredOilBarrels);
            Assert.AreEqual(6f, fullOutput.StoredFuelBarrels);
            Assert.AreEqual(7u, fullOutput.Version);
            Assert.AreEqual(0f, result.OilExtractedBarrels);
            Assert.AreEqual(1f, result.FuelProducedBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void ApplyStorageQuery_ConvertsStandardAndLargeRefineriesWithDifferentRates()
    {
        var world = new World(nameof(ApplyStorageQuery_ConvertsStandardAndLargeRefineriesWithDifferentRates));
        try
        {
            EntityManager em = world.EntityManager;
            Entity standardRefinery = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(standardRefinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 61,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 5000,
                FuelStorageCapacity = 5000,
                FuelBarrelsPerDay = 100f,
                StoredOilBarrels = 1000f
            });
            Entity largeRefinery = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(largeRefinery, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 62,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                OilStorageCapacity = 10000,
                FuelStorageCapacity = 10000,
                FuelBarrelsPerDay = 200f,
                StoredOilBarrels = 1000f
            });
            using EntityQuery storageQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuildingResourceStorageComponent>());

            BuildingResourceProductionEcsSystem.TickResult result =
                BuildingResourceProductionEcsSystem.ApplyStorageQuery(
                    em,
                    storageQuery,
                    secondsPerDay: 100f,
                    deltaTime: 1f,
                    oilBarrelsPerFuelBarrel: 2f);

            BuildingResourceStorageComponent standard =
                em.GetComponentData<BuildingResourceStorageComponent>(standardRefinery);
            BuildingResourceStorageComponent large =
                em.GetComponentData<BuildingResourceStorageComponent>(largeRefinery);
            Assert.AreEqual(998f, standard.StoredOilBarrels);
            Assert.AreEqual(1f, standard.StoredFuelBarrels);
            Assert.AreEqual(996f, large.StoredOilBarrels);
            Assert.AreEqual(2f, large.StoredFuelBarrels);
            Assert.AreEqual(3f, result.FuelProducedBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void ApplyTick_ConvertsOilIntoFuel()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 10f,
            StoredOilBarrels = 8f,
            StoredFuelBarrels = 9.5f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);

        Assert.AreEqual(7f, storage.StoredOilBarrels);
        Assert.AreEqual(10f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0.5f, result.FuelProducedBarrels);
    }

    [Test]
    public void ApplyTick_DoesNotConvertOilWhenFuelStorageIsFull()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 6,
            FuelBarrelsPerDay = 60f,
            StoredOilBarrels = 100f,
            StoredFuelBarrels = 6f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 5f, 2f);

        Assert.AreEqual(100f, storage.StoredOilBarrels);
        Assert.AreEqual(6f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    [Test]
    public void UpdateResourceProduction_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale()
    {
        var world = new World(nameof(UpdateResourceProduction_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale));
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity entity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
            entityManager.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 21,
                OilStorageCapacity = 20,
                OilBarrelsPerDay = 20f,
                StoredOilBarrels = 9f
            });

            var building = new RuntimeBuildingEntity
            {
                Id = 21,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 20,
                    OilBarrelsPerDay = 20f
                },
                CombatEntity = entity,
                StoredOilBarrels = 2f
            };
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { building.Id, building }
            };

            FactionResourceCompositionSystemHelper.ResourceProductionTickResult result =
                new FactionResourceCompositionSystemHelper().UpdateResourceProduction(
                    entityManager,
                    runtimeBuildings,
                    10f,
                    1f,
                    2f);

            BuildingResourceStorageComponent storage =
                entityManager.GetComponentData<BuildingResourceStorageComponent>(entity);
            Assert.AreEqual(11f, storage.StoredOilBarrels);
            Assert.AreEqual(11f, building.StoredOilBarrels);
            Assert.AreEqual(2f, result.OilExtractedBarrels);
            Assert.AreEqual(0f, result.FuelProducedBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void ProductionRuntimeTick_UsesProvidedDeltaTimeForThrottledResourceProduction()
    {
        var building = new RuntimeBuildingEntity
        {
            Id = 31,
            Definition = new BuildingDefinition
            {
                OilStorageCapacity = 20,
                OilBarrelsPerDay = 600f
            }
        };
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
        {
            { building.Id, building }
        };
        float recordedOil = -1f;
        var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            runtimeBuildings,
            null,
            new FactionResourceCompositionSystemHelper(),
            null,
            default,
            null,
            default,
            null,
            null,
            null,
            oil => recordedOil = oil,
            null,
            null,
            2f);

        new BuildingProductionRuntimeTickCompositionSystemHelper().UpdateResourceProduction(context, 1f);

        Assert.AreEqual(2f, building.StoredOilBarrels);
        Assert.AreEqual(2f, recordedOil);
    }

    [Test]
    public void ProductionTickSync_PreservesLiveEcsFuelWhenManagedMirrorIsStale()
    {
        var world = new World(nameof(ProductionTickSync_PreservesLiveEcsFuelWhenManagedMirrorIsStale));
        try
        {
            EntityManager em = world.EntityManager;
            Entity storageEntity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(storageEntity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 12,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                FuelStorageCapacity = 100,
                StoredOilBarrels = 3f,
                StoredFuelBarrels = 42f,
                Version = 7u
            });
            var building = new RuntimeBuildingEntity
            {
                Id = 12,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                CombatEntity = storageEntity,
                StoredOilBarrels = 0f,
                StoredFuelBarrels = 0f,
                Definition = new BuildingDefinition
                {
                    FuelStorageCapacity = 100
                }
            };

            BuildingProductionTickCompositionSystemHelper.SyncBuildingResourceStorageFromEcs(em, building);

            BuildingResourceStorageComponent storageAfter =
                em.GetComponentData<BuildingResourceStorageComponent>(storageEntity);
            Assert.AreEqual(3f, storageAfter.StoredOilBarrels);
            Assert.AreEqual(42f, storageAfter.StoredFuelBarrels);
            Assert.AreEqual(7u, storageAfter.Version);
            Assert.AreEqual(3f, building.StoredOilBarrels);
            Assert.AreEqual(42f, building.StoredFuelBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void ProductionRuntimeTick_SyncsResourceStorageMirrorAfterProductionUpdate()
    {
        var building = new RuntimeBuildingEntity
        {
            Id = 12,
            Definition = new BuildingDefinition
            {
                OilStorageCapacity = 20,
                OilBarrelsPerDay = 4f
            },
            StoredOilBarrels = 7f
        };
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
        {
            { building.Id, building }
        };
        int syncCount = 0;
        float syncedOil = -1f;
        var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            runtimeBuildings,
            null,
            new FactionResourceCompositionSystemHelper(),
            null,
            default,
            null,
            default,
            null,
            null,
            null,
            null,
            null,
            null,
            2f,
            syncedBuilding =>
            {
                syncCount++;
                syncedOil = syncedBuilding.StoredOilBarrels;
            });

        new BuildingProductionRuntimeTickCompositionSystemHelper().UpdateResourceProduction(context);

        Assert.AreEqual(1, syncCount);
        Assert.AreEqual(7f, syncedOil);
    }

    [Test]
    public void ProductionRuntimeTick_SyncsResourceStorageMirrorAfterHaulerUpdate()
    {
        var building = new RuntimeBuildingEntity
        {
            Id = 13,
            Definition = new BuildingDefinition
            {
                OilStorageCapacity = 30
            },
            StoredOilBarrels = 11f
        };
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
        {
            { building.Id, building }
        };
        int syncCount = 0;
        float syncedOil = -1f;
        var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            runtimeBuildings,
            null,
            null,
            null,
            default,
            null,
            default,
            null,
            null,
            null,
            null,
            null,
            null,
            2f,
            syncedBuilding =>
            {
                syncCount++;
                syncedOil = syncedBuilding.StoredOilBarrels;
            });

        new BuildingProductionRuntimeTickCompositionSystemHelper().UpdateResourceHaulers(context);

        Assert.AreEqual(1, syncCount);
        Assert.AreEqual(11f, syncedOil);
    }

    [Test]
    public void AutomaticFuelLogisticsRoute_PairsTrayWithFactionOilAndRefinery()
    {
        var world = new World(nameof(AutomaticFuelLogisticsRoute_PairsTrayWithFactionOilAndRefinery));
        try
        {
            EntityManager em = world.EntityManager;
            RuntimeBuildingEntity enemyOilPump = CreateResourceBuilding(
                em,
                1,
                2,
                new Vector2Int(3, 3),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                2,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                3,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { enemyOilPump.Id, enemyOilPump },
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };

            bool found = BuildingResourceHaulerBridgeCompositionSystemHelper.TryFindAutomaticHaulerRouteForTests(
                CreateAutomaticRouteContext(runtimeBuildings),
                em,
                CreateTestGrid(),
                1,
                new int2(0, 0),
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil,
                8f,
                out RuntimeBuildingEntity source,
                out RuntimeBuildingEntity destination);

            Assert.IsTrue(found);
            Assert.AreSame(oilPump, source);
            Assert.AreSame(refinery, destination);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsRoute_PairsTankerWithFactionRefineryAndFuelStorage()
    {
        var world = new World(nameof(AutomaticFuelLogisticsRoute_PairsTankerWithFactionRefineryAndFuelStorage));
        try
        {
            EntityManager em = world.EntityManager;
            RuntimeBuildingEntity enemyRefinery = CreateResourceBuilding(
                em,
                11,
                2,
                new Vector2Int(3, 3),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 40f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                12,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 40f);
            RuntimeBuildingEntity fuelBladder = CreateResourceBuilding(
                em,
                13,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 0,
                fuelCapacity: 5000,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { enemyRefinery.Id, enemyRefinery },
                { refinery.Id, refinery },
                { fuelBladder.Id, fuelBladder }
            };

            bool found = BuildingResourceHaulerBridgeCompositionSystemHelper.TryFindAutomaticHaulerRouteForTests(
                CreateAutomaticRouteContext(runtimeBuildings),
                em,
                CreateTestGrid(),
                1,
                new int2(0, 0),
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel,
                8f,
                out RuntimeBuildingEntity source,
                out RuntimeBuildingEntity destination);

            Assert.IsTrue(found);
            Assert.AreSame(refinery, source);
            Assert.AreSame(fuelBladder, destination);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsSignature_ChangesOnlyWhenRelevantStateChanges()
    {
        var world = new World(nameof(AutomaticFuelLogisticsSignature_ChangesOnlyWhenRelevantStateChanges));
        NativeList<Entity> haulers = default;
        try
        {
            EntityManager em = world.EntityManager;
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                21,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                22,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };

            Entity hauler = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(0, 0));
            haulers = new NativeList<Entity>(1, Allocator.Temp);
            haulers.Add(hauler);
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateAutomaticRouteContext(runtimeBuildings);
            GridConfig grid = CreateTestGrid();

            uint first = BuildingResourceHaulerBridgeCompositionSystemHelper.CalculateAutomaticAssignmentSignatureForTests(
                context,
                em,
                grid,
                haulers);
            uint unchanged = BuildingResourceHaulerBridgeCompositionSystemHelper.CalculateAutomaticAssignmentSignatureForTests(
                context,
                em,
                grid,
                haulers);
            BuildingResourceStorageComponent storage = em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            storage.StoredOilBarrels += 1f;
            em.SetComponentData(oilPump.CombatEntity, storage);
            uint changed = BuildingResourceHaulerBridgeCompositionSystemHelper.CalculateAutomaticAssignmentSignatureForTests(
                context,
                em,
                grid,
                haulers);

            Assert.AreNotEqual(0u, first);
            Assert.AreEqual(first, unchanged);
            Assert.AreNotEqual(first, changed);
        }
        finally
        {
            if (haulers.IsCreated)
                haulers.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsAssignmentScan_SkipsWithinStableRefreshWindow()
    {
        var world = new World(nameof(AutomaticFuelLogisticsAssignmentScan_SkipsWithinStableRefreshWindow));
        NativeList<Entity> haulers = default;
        try
        {
            EntityManager em = world.EntityManager;
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                25,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                26,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity hauler = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(0, 0));
            haulers = new NativeList<Entity>(1, Allocator.Temp);
            haulers.Add(hauler);
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateAutomaticRouteContext(runtimeBuildings);
            GridConfig grid = CreateTestGrid();
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();

            bool firstScan = bridge.ShouldRunAutomaticAssignmentScanForTests(context, em, grid, haulers, now: 0f);
            bool skippedInsideWindow = bridge.ShouldRunAutomaticAssignmentScanForTests(context, em, grid, haulers, now: 0.5f);
            bool refreshedAfterWindow = bridge.ShouldRunAutomaticAssignmentScanForTests(context, em, grid, haulers, now: 2.1f);

            Assert.IsTrue(firstScan);
            Assert.IsFalse(skippedInsideWindow);
            Assert.IsTrue(refreshedAfterWindow);
        }
        finally
        {
            if (haulers.IsCreated)
                haulers.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsReservation_ReservesSourceAndDestinationCapacity()
    {
        var world = new World(nameof(AutomaticFuelLogisticsReservation_ReservesSourceAndDestinationCapacity));
        try
        {
            EntityManager em = world.EntityManager;
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                31,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                32,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();

            bool reserved = bridge.TryReserveHaulCapacityForTests(
                CreateAutomaticRouteContext(runtimeBuildings),
                em,
                oilPump,
                refinery,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil,
                8f,
                out UnitResourceHaulReservation reservation);

            BuildingResourceStorageComponent sourceStorage = em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage = em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            Assert.IsTrue(reserved);
            Assert.AreEqual(oilPump.Id, reservation.SourceBuildingId);
            Assert.AreEqual(refinery.Id, reservation.DestinationBuildingId);
            Assert.AreEqual(8f, reservation.ReservedBarrels);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, reservation.ResourceKind);
            Assert.AreEqual(1, reservation.SourceReservationActive);
            Assert.AreEqual(1, reservation.DestinationReservationActive);
            Assert.AreEqual(8f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedOilInboundBarrels);
            Assert.AreEqual(1u, sourceStorage.Version);
            Assert.AreEqual(1u, destinationStorage.Version);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_DispatchesToEmptyProducingOilPump()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTray_DispatchesToEmptyProducingOilPump));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                33,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                34,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(tray));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(tray));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(tray);
            UnitResourceHaulReservation reservation = em.GetComponentData<UnitResourceHaulReservation>(tray);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);

            Assert.AreEqual(oilPump.Id, order.SourceBuildingId);
            Assert.AreEqual(refinery.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
            Assert.AreEqual(0, reservation.SourceReservationActive);
            Assert.AreEqual(1, reservation.DestinationReservationActive);
            Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedOilInboundBarrels);

            MoveHaulerToOrderTarget(em, tray, order);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);
            order = em.GetComponentData<UnitResourceHaulOrder>(tray);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Loading, order.Phase);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.2f);
            UnitResourceHauler cargo = em.GetComponentData<UnitResourceHauler>(tray);
            Assert.AreEqual(0f, cargo.CargoOilBarrels);
            Assert.AreEqual(0f, order.ActionEndsAt);

            sourceStorage.StoredOilBarrels = 8f;
            em.SetComponentData(oilPump.CombatEntity, sourceStorage);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.4f);
            order = em.GetComponentData<UnitResourceHaulOrder>(tray);
            Assert.Greater(order.ActionEndsAt, 1.4f);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 2.5f);
            order = em.GetComponentData<UnitResourceHaulOrder>(tray);
            cargo = em.GetComponentData<UnitResourceHauler>(tray);
            sourceStorage = em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            destinationStorage = em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);

            Assert.AreEqual(8f, cargo.CargoOilBarrels);
            Assert.AreEqual(0f, sourceStorage.StoredOilBarrels);
            Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedOilInboundBarrels);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, order.Phase);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void FuelLogisticsApproach_UsesEffectiveRunwayPlacementRect()
    {
        var world = new World(nameof(FuelLogisticsApproach_UsesEffectiveRunwayPlacementRect));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity runwayBuilding = CreateResourceBuilding(
                em,
                46,
                1,
                new Vector2Int(20, 20),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { runwayBuilding.Id, runwayBuilding }
            };
            RectInt effectiveRunwayRect = new(16, 17, 12, 8);
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(
                    em,
                    gridEntity,
                    runtimeBuildings,
                    (_, _) => effectiveRunwayRect);
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            int2 footprint = new(2, 2);

            bool found = bridge.TryGetRuntimeBuildingApproachCell(
                context,
                runwayBuilding,
                footprint,
                new int2(10, 21),
                out int2 goal);

            Assert.IsTrue(found);
            int2 unitMin = UnitFootprintUtility.GetMinCell(goal, footprint);
            RectInt unitRect = new(unitMin.x, unitMin.y, footprint.x, footprint.y);
            Assert.IsFalse(unitRect.Overlaps(effectiveRunwayRect));
            Assert.IsTrue(bridge.IsRuntimeBuildingApproachCell(context, runwayBuilding, goal, footprint));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsSeededTray_StartsOilHaulingWithoutRuntimeBuild()
    {
        var world = new World(nameof(AutomaticFuelLogisticsSeededTray_StartsOilHaulingWithoutRuntimeBuild));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                35,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                36,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity seededTray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(seededTray));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(seededTray));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(seededTray);
            UnitResourceHaulReservation reservation = em.GetComponentData<UnitResourceHaulReservation>(seededTray);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);

            Assert.AreEqual(oilPump.Id, order.SourceBuildingId);
            Assert.AreEqual(refinery.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, order.ResourceKind);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
            Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(seededTray));
            Assert.AreEqual(oilPump.Id, reservation.SourceBuildingId);
            Assert.AreEqual(refinery.Id, reservation.DestinationBuildingId);
            Assert.AreEqual(8f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedOilInboundBarrels);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsManualMove_IsNotOverridden()
    {
        var world = new World(nameof(AutomaticFuelLogisticsManualMove_IsNotOverridden));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                47,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                48,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity seededTray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            int2 manualGoal = new(30, 30);
            em.AddComponent<ManualMoveOrderTag>(seededTray);
            em.AddComponentData(seededTray, new UnitTarget { Cell = manualGoal });
            em.AddComponentData(seededTray, new UnitPathRequest { Goal = manualGoal });
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(seededTray));
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(seededTray));
            Assert.AreEqual(manualGoal, em.GetComponentData<UnitTarget>(seededTray).Cell);
            Assert.AreEqual(manualGoal, em.GetComponentData<UnitPathRequest>(seededTray).Goal);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_ReissuesMoveWhenTargetHasNoActivePath()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTray_ReissuesMoveWhenTargetHasNoActivePath));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                37,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                38,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity seededTray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            int2 staleGoal = new(2, 2);
            em.AddComponentData(seededTray, new UnitTarget { Cell = staleGoal });
            em.AddComponentData(seededTray, new UnitResourceHaulOrder
            {
                SourceBuildingId = oilPump.Id,
                DestinationBuildingId = refinery.Id,
                TargetCell = staleGoal,
                Phase = (byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource,
                ResourceKind = (byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil
            });
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(seededTray);
            Assert.AreNotEqual(staleGoal, order.TargetCell);
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(seededTray));
            Assert.AreEqual(order.TargetCell, em.GetComponentData<UnitPathRequest>(seededTray).Goal);
            Assert.AreEqual(order.TargetCell, em.GetComponentData<UnitTarget>(seededTray).Cell);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsSeededTanker_StartsFuelHaulingWithoutRuntimeBuild()
    {
        var world = new World(nameof(AutomaticFuelLogisticsSeededTanker_StartsFuelHaulingWithoutRuntimeBuild));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                37,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 40f);
            RuntimeBuildingEntity fuelBladder = CreateResourceBuilding(
                em,
                38,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 0,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { refinery.Id, refinery },
                { fuelBladder.Id, fuelBladder }
            };
            Entity seededTanker = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tanker",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(seededTanker));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(seededTanker));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(seededTanker);
            UnitResourceHaulReservation reservation = em.GetComponentData<UnitResourceHaulReservation>(seededTanker);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(fuelBladder.CombatEntity);

            Assert.AreEqual(refinery.Id, order.SourceBuildingId);
            Assert.AreEqual(fuelBladder.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, order.ResourceKind);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
            Assert.AreEqual(refinery.Id, reservation.SourceBuildingId);
            Assert.AreEqual(fuelBladder.Id, reservation.DestinationBuildingId);
            Assert.AreEqual(8f, sourceStorage.ReservedFuelOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedFuelInboundBarrels);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_NoRefineryCapacitySetsTypedIdleReason()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTray_NoRefineryCapacitySetsTypedIdleReason));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                37,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity fullRefinery = CreateResourceBuilding(
                em,
                38,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 100f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { fullRefinery.Id, fullRefinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(tray));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(tray));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulStatus>(tray));
            UnitResourceHaulStatus status = em.GetComponentData<UnitResourceHaulStatus>(tray);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(fullRefinery.CombatEntity);

            Assert.AreEqual((byte)FuelLogisticsTaskStatusCode.Blocked, status.StatusCode);
            Assert.AreEqual((byte)FuelLogisticsBlockReasonCode.DestinationFull, status.ReasonCode);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, status.ResourceKind);
            Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(0f, destinationStorage.ReservedOilInboundBarrels);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_DestroyedSourceClearsReservation()
    {
        AssertDestroyedEndpointClearsReservation(destroySource: true);
    }

    [Test]
    public void AutomaticFuelLogisticsTray_DestroyedDestinationClearsReservation()
    {
        AssertDestroyedEndpointClearsReservation(destroySource: false);
    }

    private static void AssertDestroyedEndpointClearsReservation(bool destroySource)
    {
        var world = new World(destroySource
            ? nameof(AutomaticFuelLogisticsTray_DestroyedSourceClearsReservation)
            : nameof(AutomaticFuelLogisticsTray_DestroyedDestinationClearsReservation));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                destroySource ? 39 : 41,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                destroySource ? 40 : 42,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(tray));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(tray));
            if (destroySource)
                oilPump.IsDestroyed = true;
            else
                refinery.IsDestroyed = true;

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);

            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            UnitResourceHaulStatus status = em.GetComponentData<UnitResourceHaulStatus>(tray);
            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(tray));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(tray));
            Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(0f, destinationStorage.ReservedOilInboundBarrels);
            Assert.AreEqual((byte)FuelLogisticsTaskStatusCode.Blocked, status.StatusCode);
            Assert.AreEqual(
                (byte)(destroySource
                    ? FuelLogisticsBlockReasonCode.SourceUnavailable
                    : FuelLogisticsBlockReasonCode.DestinationUnavailable),
                status.ReasonCode);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_DeadHaulerClearsReservation()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTray_DeadHaulerClearsReservation));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                45,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                46,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);
            em.AddComponentData(tray, new UnitHealth { Current = 0, Max = 100 });

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);

            AssertClearedOilReservationWithStatus(
                em,
                tray,
                oilPump,
                refinery,
                FuelLogisticsBlockReasonCode.HaulerUnavailable);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTray_RouteInvalidationClearsReservation()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTray_RouteInvalidationClearsReservation));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                47,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                48,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);
            ClearMovementRequestState(em, tray);
            BlockAllCells(blocked, 48 * 48);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);

            AssertClearedOilReservationWithStatus(
                em,
                tray,
                oilPump,
                refinery,
                FuelLogisticsBlockReasonCode.RouteUnavailable);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    private static void AssertClearedOilReservationWithStatus(
        EntityManager em,
        Entity tray,
        RuntimeBuildingEntity oilPump,
        RuntimeBuildingEntity refinery,
        FuelLogisticsBlockReasonCode expectedReason)
    {
        BuildingResourceStorageComponent sourceStorage =
            em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
        BuildingResourceStorageComponent destinationStorage =
            em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
        UnitResourceHaulStatus status = em.GetComponentData<UnitResourceHaulStatus>(tray);
        Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(tray));
        Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(tray));
        Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
        Assert.AreEqual(0f, destinationStorage.ReservedOilInboundBarrels);
        Assert.AreEqual((byte)FuelLogisticsTaskStatusCode.Blocked, status.StatusCode);
        Assert.AreEqual((byte)expectedReason, status.ReasonCode);
    }

    private static void ClearMovementRequestState(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitTarget>(entity))
            em.RemoveComponent<UnitTarget>(entity);
        if (em.HasComponent<UnitPathRequest>(entity))
            em.RemoveComponent<UnitPathRequest>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            em.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            em.RemoveComponent<UnitPathRange>(entity);
    }

    private static void BlockAllCells(NativeBitArray blocked, int count)
    {
        for (int i = 0; i < count; i++)
            blocked.Set(i, true);
    }

    [Test]
    public void AutomaticFuelLogisticsSteadyState_DoesNotAllocateManagedMemory()
    {
        var world = new World(nameof(AutomaticFuelLogisticsSteadyState_DoesNotAllocateManagedMemory));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 48, 48, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                43,
                1,
                new Vector2Int(8, 10),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                44,
                1,
                new Vector2Int(20, 10),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity tray = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(4, 12));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);
            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(tray));

            for (int i = 0; i < 8; i++)
                bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f + i * 0.01f);

            long allocationStart = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
                bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.5f + i * 0.01f);
            long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocationStart;

            Assert.AreEqual(0L, allocatedBytes, "Unchanged automatic fuel logistics steady state should not allocate managed memory.");
            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(tray));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(tray));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsCycle_TrayTransfersOilWithoutManualCommand()
    {
        var world = new World(nameof(AutomaticFuelLogisticsCycle_TrayTransfersOilWithoutManualCommand));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            RuntimeBuildingEntity oilPump = CreateResourceBuilding(
                em,
                41,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 0,
                oilRate: 80f,
                fuelRate: 0f,
                storedOil: 40f,
                storedFuel: 0f);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                42,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { oilPump.Id, oilPump },
                { refinery.Id, refinery }
            };
            Entity hauler = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tray",
                1,
                new int2(0, 0));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(hauler));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(hauler));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual(oilPump.Id, order.SourceBuildingId);
            Assert.AreEqual(refinery.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);
            order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Loading, order.Phase);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.2f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.3f);

            UnitResourceHauler cargo = em.GetComponentData<UnitResourceHauler>(hauler);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(oilPump.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual(8f, cargo.CargoOilBarrels);
            Assert.AreEqual(32f, sourceStorage.StoredOilBarrels);
            Assert.AreEqual(0f, sourceStorage.ReservedOilOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedOilInboundBarrels);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, order.Phase);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.4f);
            order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.Unloading, order.Phase);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.5f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 2.6f);

            cargo = em.GetComponentData<UnitResourceHauler>(hauler);
            destinationStorage = em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            Assert.AreEqual(0f, cargo.CargoOilBarrels);
            Assert.AreEqual(8f, destinationStorage.StoredOilBarrels);
            Assert.AreEqual(0f, destinationStorage.ReservedOilInboundBarrels);
            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(hauler));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(hauler));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsCycle_TankerTransfersFuelWithoutManualCommand()
    {
        var world = new World(nameof(AutomaticFuelLogisticsCycle_TankerTransfersFuelWithoutManualCommand));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                51,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 40f);
            RuntimeBuildingEntity fuelBladder = CreateResourceBuilding(
                em,
                52,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 0,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { refinery.Id, refinery },
                { fuelBladder.Id, fuelBladder }
            };
            Entity hauler = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tanker",
                1,
                new int2(0, 0));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(hauler));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulReservation>(hauler));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual(refinery.Id, order.SourceBuildingId);
            Assert.AreEqual(fuelBladder.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, order.ResourceKind);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.2f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.3f);

            UnitResourceHauler cargo = em.GetComponentData<UnitResourceHauler>(hauler);
            BuildingResourceStorageComponent sourceStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            BuildingResourceStorageComponent destinationStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(fuelBladder.CombatEntity);
            order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual(8f, cargo.CargoFuelBarrels);
            Assert.AreEqual(32f, sourceStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, sourceStorage.ReservedFuelOutboundBarrels);
            Assert.AreEqual(8f, destinationStorage.ReservedFuelInboundBarrels);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, order.Phase);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.4f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.5f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 2.6f);

            cargo = em.GetComponentData<UnitResourceHauler>(hauler);
            destinationStorage = em.GetComponentData<BuildingResourceStorageComponent>(fuelBladder.CombatEntity);
            Assert.AreEqual(0f, cargo.CargoFuelBarrels);
            Assert.AreEqual(8f, destinationStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, destinationStorage.ReservedFuelInboundBarrels);
            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(hauler));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(hauler));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsEnemyFaction_ProducesAndDeliversFuel()
    {
        var world = new World(nameof(AutomaticFuelLogisticsEnemyFaction_ProducesAndDeliversFuel));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                71,
                FactionIdentity.EnemyFactionId,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 800f,
                storedOil: 20f,
                storedFuel: 0f);
            RuntimeBuildingEntity enemyFuelBladder = CreateResourceBuilding(
                em,
                72,
                FactionIdentity.EnemyFactionId,
                new Vector2Int(16, 8),
                oilCapacity: 0,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            RuntimeBuildingEntity playerFuelBladder = CreateResourceBuilding(
                em,
                73,
                FactionIdentity.PlayerFactionId,
                new Vector2Int(10, 8),
                oilCapacity: 0,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { refinery.Id, refinery },
                { enemyFuelBladder.Id, enemyFuelBladder },
                { playerFuelBladder.Id, playerFuelBladder }
            };

            using EntityQuery storageQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuildingResourceStorageComponent>());
            BuildingResourceProductionEcsSystem.TickResult production =
                BuildingResourceProductionEcsSystem.ApplyStorageQuery(
                    em,
                    storageQuery,
                    secondsPerDay: 100f,
                    deltaTime: 1f,
                    oilBarrelsPerFuelBarrel: 2f);

            Assert.AreEqual(8f, production.FuelProducedBarrels);
            BuildingResourceStorageComponent refineryStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            Assert.AreEqual(FactionIdentity.EnemyFactionId, refineryStorage.OwnerFactionId);
            Assert.AreEqual(4f, refineryStorage.StoredOilBarrels);
            Assert.AreEqual(8f, refineryStorage.StoredFuelBarrels);

            Entity hauler = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tanker",
                FactionIdentity.EnemyFactionId,
                new int2(0, 0));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsTrue(em.HasComponent<UnitResourceHaulOrder>(hauler));
            UnitResourceHaulOrder order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            Assert.AreEqual(refinery.Id, order.SourceBuildingId);
            Assert.AreEqual(enemyFuelBladder.Id, order.DestinationBuildingId);
            Assert.AreNotEqual(playerFuelBladder.Id, order.DestinationBuildingId);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, order.ResourceKind);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.1f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0.2f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.3f);
            order = em.GetComponentData<UnitResourceHaulOrder>(hauler);
            MoveHaulerToOrderTarget(em, hauler, order);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.4f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 1.5f);
            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 2.6f);

            BuildingResourceStorageComponent enemyStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(enemyFuelBladder.CombatEntity);
            BuildingResourceStorageComponent playerStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(playerFuelBladder.CombatEntity);
            Assert.AreEqual(8f, enemyStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, enemyStorage.ReservedFuelInboundBarrels);
            Assert.AreEqual(0f, playerStorage.StoredFuelBarrels);
            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(hauler));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(hauler));
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    [Test]
    public void AutomaticFuelLogisticsTanker_NoRefineryFuelSetsTypedIdleReason()
    {
        AssertAutomaticTankerBlocked(
            nameof(AutomaticFuelLogisticsTanker_NoRefineryFuelSetsTypedIdleReason),
            includeFuelStorage: true,
            fuelStorageIsFull: false,
            refineryFuel: 0f,
            FuelLogisticsBlockReasonCode.SourceUnavailable);
    }

    [Test]
    public void AutomaticFuelLogisticsTanker_NoFuelStorageSetsTypedIdleReason()
    {
        AssertAutomaticTankerBlocked(
            nameof(AutomaticFuelLogisticsTanker_NoFuelStorageSetsTypedIdleReason),
            includeFuelStorage: false,
            fuelStorageIsFull: false,
            refineryFuel: 40f,
            FuelLogisticsBlockReasonCode.DestinationUnavailable);
    }

    [Test]
    public void AutomaticFuelLogisticsTanker_FullFuelStorageSetsTypedIdleReason()
    {
        AssertAutomaticTankerBlocked(
            nameof(AutomaticFuelLogisticsTanker_FullFuelStorageSetsTypedIdleReason),
            includeFuelStorage: true,
            fuelStorageIsFull: true,
            refineryFuel: 40f,
            FuelLogisticsBlockReasonCode.DestinationFull);
    }

    [Test]
    public void AutomaticFuelLogisticsTanker_NoRouteSetsTypedIdleReason()
    {
        AssertAutomaticTankerBlocked(
            nameof(AutomaticFuelLogisticsTanker_NoRouteSetsTypedIdleReason),
            includeFuelStorage: true,
            fuelStorageIsFull: false,
            refineryFuel: 40f,
            FuelLogisticsBlockReasonCode.RouteUnavailable,
            blockAllCells: true);
    }

    [Test]
    public void AutomaticFuelLogisticsTanker_NoAvailableTankerDoesNotReserveFuel()
    {
        var world = new World(nameof(AutomaticFuelLogisticsTanker_NoAvailableTankerDoesNotReserveFuel));
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                71,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: 40f);
            RuntimeBuildingEntity fuelBladder = CreateResourceBuilding(
                em,
                72,
                1,
                new Vector2Int(16, 8),
                oilCapacity: 0,
                fuelCapacity: 20,
                oilRate: 0f,
                fuelRate: 0f,
                storedOil: 0f,
                storedFuel: 0f);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { refinery.Id, refinery },
                { fuelBladder.Id, fuelBladder }
            };
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            BuildingResourceStorageComponent refineryStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(refinery.CombatEntity);
            BuildingResourceStorageComponent bladderStorage =
                em.GetComponentData<BuildingResourceStorageComponent>(fuelBladder.CombatEntity);
            Assert.AreEqual(40f, refineryStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, refineryStorage.ReservedFuelOutboundBarrels);
            Assert.AreEqual(0f, bladderStorage.StoredFuelBarrels);
            Assert.AreEqual(0f, bladderStorage.ReservedFuelInboundBarrels);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    private static void AssertAutomaticTankerBlocked(
        string worldName,
        bool includeFuelStorage,
        bool fuelStorageIsFull,
        float refineryFuel,
        FuelLogisticsBlockReasonCode expectedReason,
        bool blockAllCells = false)
    {
        var world = new World(worldName);
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            EntityManager em = world.EntityManager;
            Entity gridEntity = CreateTestGridEntity(em, 32, 32, out blocked, out occupied);
            if (blockAllCells)
                BlockAllCells(blocked, 32 * 32);
            RuntimeBuildingEntity refinery = CreateResourceBuilding(
                em,
                61,
                1,
                new Vector2Int(8, 8),
                oilCapacity: 100,
                fuelCapacity: 100,
                oilRate: 0f,
                fuelRate: 40f,
                storedOil: 0f,
                storedFuel: refineryFuel);
            var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
            {
                { refinery.Id, refinery }
            };
            if (includeFuelStorage)
            {
                RuntimeBuildingEntity fuelBladder = CreateResourceBuilding(
                    em,
                    62,
                    1,
                    new Vector2Int(16, 8),
                    oilCapacity: 0,
                    fuelCapacity: 20,
                    oilRate: 0f,
                    fuelRate: 0f,
                    storedOil: 0f,
                    storedFuel: fuelStorageIsFull ? 20f : 0f);
                runtimeBuildings.Add(fuelBladder.Id, fuelBladder);
            }

            Entity tanker = CreateFuelLogisticsHauler(
                em,
                "Unit_Veh_Truck_Tanker",
                1,
                new int2(0, 0));
            var bridge = new BuildingResourceHaulerBridgeCompositionSystemHelper();
            BuildingResourceHaulerBridgeCompositionSystemHelper.Context context =
                CreateBridgeCycleContext(em, gridEntity, runtimeBuildings);

            bridge.UpdateResourceHaulers(context, hasPendingPathJob: false, now: 0f);

            Assert.IsFalse(em.HasComponent<UnitResourceHaulOrder>(tanker));
            Assert.IsFalse(em.HasComponent<UnitResourceHaulReservation>(tanker));
            Assert.IsTrue(em.HasComponent<UnitResourceHaulStatus>(tanker));
            UnitResourceHaulStatus status = em.GetComponentData<UnitResourceHaulStatus>(tanker);
            Assert.AreEqual((byte)FuelLogisticsTaskStatusCode.Blocked, status.StatusCode);
            Assert.AreEqual((byte)expectedReason, status.ReasonCode);
            Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, status.ResourceKind);
        }
        finally
        {
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            world.Dispose();
        }
    }

    private static BuildingResourceHaulerBridgeCompositionSystemHelper.Context CreateAutomaticRouteContext(
        System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
    {
        return new BuildingResourceHaulerBridgeCompositionSystemHelper.Context(
            runtimeBuildings,
            new ResourceHaulerUtilitySystemHelper(),
            new FactionResourceCompositionSystemHelper(),
            null,
            null,
            null,
            null,
            null,
            null,
            ResolveBuildingFocusWorldPosition,
            null);
    }

    private static BuildingResourceHaulerBridgeCompositionSystemHelper.Context CreateBridgeCycleContext(
        EntityManager em,
        Entity gridEntity,
        System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect = null)
    {
        EntityQuery haulerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitResourceHauler>(),
            ComponentType.ReadOnly<UnitGrid>());
        return new BuildingResourceHaulerBridgeCompositionSystemHelper.Context(
            runtimeBuildings,
            new ResourceHaulerUtilitySystemHelper(),
            new FactionResourceCompositionSystemHelper(),
            TryGetEntityManager,
            TryGetGridData,
            null,
            () => haulerQuery,
            null,
            TryGetRuntimeBuilding,
            ResolveBuildingFocusWorldPosition,
            getEffectivePlacementRect ?? GetEffectivePlacementRect);

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }

        bool TryGetGridData(out Entity entity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData)
        {
            entity = gridEntity;
            grid = em.GetComponentData<GridConfig>(gridEntity);
            roads = em.GetBuffer<GridRoad>(gridEntity);
            blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            return true;
        }

        bool TryGetRuntimeBuilding(int id, out RuntimeBuildingEntity building)
        {
            return runtimeBuildings.TryGetValue(id, out building);
        }
    }

    private static RuntimeBuildingEntity CreateResourceBuilding(
        EntityManager em,
        int id,
        byte factionId,
        Vector2Int originCell,
        int oilCapacity,
        int fuelCapacity,
        float oilRate,
        float fuelRate,
        float storedOil,
        float storedFuel)
    {
        Entity storageEntity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storageEntity, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = id,
            OwnerFactionId = factionId,
            OilStorageCapacity = oilCapacity,
            FuelStorageCapacity = fuelCapacity,
            OilBarrelsPerDay = oilRate,
            FuelBarrelsPerDay = fuelRate,
            StoredOilBarrels = storedOil,
            StoredFuelBarrels = storedFuel
        });

        return new RuntimeBuildingEntity
        {
            Id = id,
            HasOwnerFaction = true,
            OwnerFactionId = factionId,
            OriginCell = originCell,
            CombatEntity = storageEntity,
            StoredOilBarrels = storedOil,
            StoredFuelBarrels = storedFuel,
            Definition = new BuildingDefinition
            {
                FootprintCells = new Vector2Int(2, 2),
                OilStorageCapacity = oilCapacity,
                FuelStorageCapacity = fuelCapacity,
                OilBarrelsPerDay = oilRate,
                FuelBarrelsPerDay = fuelRate
            }
        };
    }

    private static Entity CreateFuelLogisticsHauler(
        EntityManager em,
        string sourceKey,
        byte factionId,
        int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitResourceHauler),
            typeof(UnitSourcePrefabKey),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint));
        em.SetComponentData(entity, new UnitResourceHauler
        {
            BarrelCapacity = 8,
            FillDurationSeconds = 1f,
            UnloadDurationSeconds = 1f
        });
        em.SetComponentData(entity, new UnitSourcePrefabKey
        {
            Value = new FixedString64Bytes(sourceKey)
        });
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        return entity;
    }

    private static Entity CreateTestGridEntity(
        EntityManager em,
        int width,
        int height,
        out NativeBitArray blocked,
        out NativeBitArray occupied)
    {
        int gridSize = width * height;
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(GridWalkable),
            typeof(GridRoad),
            typeof(GridRoadSidewalk),
            typeof(GridRoadDirt));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = width,
            Height = height,
            CellSize = 1f,
            Origin = float3.zero
        });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Blocked = blocked
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }

        return gridEntity;
    }

    private static GridConfig CreateTestGrid()
    {
        return new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        };
    }

    private static void MoveHaulerToOrderTarget(EntityManager em, Entity hauler, UnitResourceHaulOrder order)
    {
        em.SetComponentData(hauler, new UnitGrid { Cell = order.TargetCell });
    }

    private static RectInt GetEffectivePlacementRect(RuntimeBuildingEntity building, GridConfig grid)
    {
        Vector2Int footprint = building.Definition != null && building.Definition.FootprintCells.x > 0 && building.Definition.FootprintCells.y > 0
            ? building.Definition.FootprintCells
            : Vector2Int.one;
        return new RectInt(building.OriginCell, footprint);
    }

    private static Vector3 ResolveBuildingFocusWorldPosition(RuntimeBuildingEntity building)
    {
        Vector2Int footprint = building.Definition != null && building.Definition.FootprintCells.x > 0 && building.Definition.FootprintCells.y > 0
            ? building.Definition.FootprintCells
            : Vector2Int.one;
        return new Vector3(
            building.OriginCell.x + footprint.x * 0.5f,
            0f,
            building.OriginCell.y + footprint.y * 0.5f);
    }
}
#endif
