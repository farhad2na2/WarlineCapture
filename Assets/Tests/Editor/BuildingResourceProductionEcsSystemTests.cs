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
            tests.ApplyTick_ConvertsOilIntoFuel();
            tests.ApplyTick_DoesNotConvertOilWhenFuelStorageIsFull();
            tests.UpdateResourceProduction_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale();
            tests.ProductionRuntimeTick_UsesProvidedDeltaTimeForThrottledResourceProduction();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterProductionUpdate();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterHaulerUpdate();
            tests.AutomaticFuelLogisticsRoute_PairsTrayWithFactionOilAndRefinery();
            tests.AutomaticFuelLogisticsRoute_PairsTankerWithFactionRefineryAndFuelStorage();
            tests.AutomaticFuelLogisticsSignature_ChangesOnlyWhenRelevantStateChanges();
            tests.AutomaticFuelLogisticsReservation_ReservesSourceAndDestinationCapacity();
            tests.AutomaticFuelLogisticsCycle_TrayTransfersOilWithoutManualCommand();
            Debug.Log("[BuildingResourceProductionEcsFocusedValidation] result=Passed tests=12");
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
        System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
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
            GetEffectivePlacementRect);

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
