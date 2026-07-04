using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Unity.Mathematics;
using Unity.Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ResourceHaulerUtilitySystemHelperTests
{
    public static void RunFocusedValidation()
    {
        var tests = new ResourceHaulerUtilitySystemHelperTests();
        try
        {
            tests.CreateOrder_InitializesTravelToSource();
            tests.SetTravelPhase_UpdatesTargetAndClearsTimer();
            tests.AdvanceTimedAction_StartsWaitsThenCompletes();
            tests.TryCompleteLoad_MovesOilFromSourceIntoHaulerCargo();
            tests.TryCompleteLoad_DoesNotLoadWhenSourceIsShort();
            tests.RevertLoad_ReturnsCargoToSource();
            tests.TryCompleteUnload_ClampsDestinationCapacityAndClearsCargo();
            tests.TryCompleteUnload_WaitsWhenDestinationCannotFitCargo();
            tests.TryCompleteLoad_ClearsOppositeCargoWhenLoadingFuel();
            tests.TryCompleteUnload_PreservesCargoWhenDestinationCannotFitOil();
            tests.RevertLoad_ReturnsOilAndClearsOnlyMatchingCargo();
            tests.Classification_DetectsHaulerSourceAndDestinationRoles();
            tests.StorageTransfer_TryCompleteLoad_UsesComponentStorage();
            tests.StorageTransfer_TryCompleteUnload_UsesComponentStorage();
            tests.HaulerTransferEcs_TryCompleteLoad_UsesComponentStorage();
            tests.HaulerTransferEcs_TryCompleteUnload_UsesComponentStorage();
            tests.LiveEcsStorage_TryCompleteLoad_PrefersCombatEntityStorage();
            tests.LiveEcsStorage_TryCompleteUnload_PrefersCombatEntityStorage();
            tests.LiveEcsStorage_GetStoredResource_PrefersCombatEntityStorage();
            tests.LiveEcsStorage_HasAvailableFuelForHauler_PrefersCombatEntityStorage();
            Debug.Log("[ResourceHaulerFocusedValidation] result=Passed tests=20");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ResourceHaulerFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CreateOrder_InitializesTravelToSource()
    {
        var system = new ResourceHaulerUtilitySystemHelper();
        UnitResourceHaulOrder order = system.CreateOrder(
            sourceBuildingId: 12,
            destinationBuildingId: 34,
            targetCell: new int2(5, 7),
            resourceKind: ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel);

        Assert.AreEqual(12, order.SourceBuildingId);
        Assert.AreEqual(34, order.DestinationBuildingId);
        Assert.AreEqual(new int2(5, 7), order.TargetCell);
        Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToSource, order.Phase);
        Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, order.ResourceKind);
        Assert.AreEqual(0f, order.ActionEndsAt);
    }

    [Test]
    public void SetTravelPhase_UpdatesTargetAndClearsTimer()
    {
        UnitResourceHaulOrder order = new()
        {
            ActionEndsAt = 25f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        system.SetTravelPhase(ref order, ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, new int2(9, 4));

        Assert.AreEqual((byte)ResourceHaulerUtilitySystemHelper.ResourceHaulPhase.ToDestination, order.Phase);
        Assert.AreEqual(new int2(9, 4), order.TargetCell);
        Assert.AreEqual(0f, order.ActionEndsAt);
    }

    [Test]
    public void AdvanceTimedAction_StartsWaitsThenCompletes()
    {
        UnitResourceHaulOrder order = default;

        var system = new ResourceHaulerUtilitySystemHelper();
        ResourceHaulerUtilitySystemHelper.TimedActionState started = system.AdvanceTimedAction(ref order, now: 10f, durationSeconds: 3f);
        ResourceHaulerUtilitySystemHelper.TimedActionState waiting = system.AdvanceTimedAction(ref order, now: 12f, durationSeconds: 3f);
        ResourceHaulerUtilitySystemHelper.TimedActionState ready = system.AdvanceTimedAction(ref order, now: 13f, durationSeconds: 3f);

        Assert.AreEqual(ResourceHaulerUtilitySystemHelper.TimedActionState.Started, started);
        Assert.AreEqual(13f, order.ActionEndsAt);
        Assert.AreEqual(ResourceHaulerUtilitySystemHelper.TimedActionState.Waiting, waiting);
        Assert.AreEqual(ResourceHaulerUtilitySystemHelper.TimedActionState.Ready, ready);
    }

    [Test]
    public void TryCompleteLoad_MovesOilFromSourceIntoHaulerCargo()
    {
        var source = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            OilBarrelsPerDay = 12f,
            StoredOilBarrels = 30f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 4f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        bool loaded = system.TryCompleteLoad(source, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, 10f, ref hauler);

        Assert.IsTrue(loaded);
        Assert.AreEqual(20f, source.StoredOilBarrels);
        Assert.AreEqual(10f, hauler.CargoOilBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteLoad_DoesNotLoadWhenSourceIsShort()
    {
        var source = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            StoredOilBarrels = 9.5f
        };
        UnitResourceHauler hauler = default;

        var system = new ResourceHaulerUtilitySystemHelper();
        bool loaded = system.TryCompleteLoad(source, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, 10f, ref hauler);

        Assert.IsFalse(loaded);
        Assert.AreEqual(9.5f, source.StoredOilBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
    }

    [Test]
    public void RevertLoad_ReturnsCargoToSource()
    {
        var source = new TestHaulerBuilding
        {
            FuelStorageCapacity = 50,
            FuelBarrelsPerDay = 10f,
            StoredFuelBarrels = 12f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        system.RevertLoad(source, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, 5f, ref hauler);

        Assert.AreEqual(17f, source.StoredFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteUnload_ClampsDestinationCapacityAndClearsCargo()
    {
        var destination = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            StoredFuelBarrels = 15f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        bool unloaded = system.TryCompleteUnload(destination, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, ref hauler);

        Assert.IsTrue(unloaded);
        Assert.AreEqual(20f, destination.StoredFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteUnload_WaitsWhenDestinationCannotFitCargo()
    {
        var destination = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            StoredFuelBarrels = 16f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        bool unloaded = system.TryCompleteUnload(destination, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, ref hauler);

        Assert.IsFalse(unloaded);
        Assert.AreEqual(16f, destination.StoredFuelBarrels);
        Assert.AreEqual(5f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteLoad_ClearsOppositeCargoWhenLoadingFuel()
    {
        var source = new TestHaulerBuilding
        {
            FuelStorageCapacity = 100,
            FuelBarrelsPerDay = 12f,
            StoredFuelBarrels = 30f
        };
        UnitResourceHauler hauler = new()
        {
            CargoOilBarrels = 7f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        bool loaded = system.TryCompleteLoad(source, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel, 10f, ref hauler);

        Assert.IsTrue(loaded);
        Assert.AreEqual(20f, source.StoredFuelBarrels);
        Assert.AreEqual(10f, hauler.CargoFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
    }

    [Test]
    public void TryCompleteUnload_PreservesCargoWhenDestinationCannotFitOil()
    {
        var destination = new TestHaulerBuilding
        {
            OilStorageCapacity = 20,
            StoredOilBarrels = 16f
        };
        UnitResourceHauler hauler = new()
        {
            CargoOilBarrels = 5f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        bool unloaded = system.TryCompleteUnload(destination, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, ref hauler);

        Assert.IsFalse(unloaded);
        Assert.AreEqual(16f, destination.StoredOilBarrels);
        Assert.AreEqual(5f, hauler.CargoOilBarrels);
    }

    [Test]
    public void RevertLoad_ReturnsOilAndClearsOnlyMatchingCargo()
    {
        var source = new TestHaulerBuilding
        {
            OilStorageCapacity = 50,
            OilBarrelsPerDay = 10f,
            StoredOilBarrels = 12f
        };
        UnitResourceHauler hauler = new()
        {
            CargoOilBarrels = 5f,
            CargoFuelBarrels = 3f
        };

        var system = new ResourceHaulerUtilitySystemHelper();
        system.RevertLoad(source, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil, 5f, ref hauler);

        Assert.AreEqual(17f, source.StoredOilBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
        Assert.AreEqual(3f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void Classification_DetectsHaulerSourceAndDestinationRoles()
    {
        var oilSource = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            OilBarrelsPerDay = 10f
        };
        var fuelSource = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            FuelBarrelsPerDay = 5f,
            StoredFuelBarrels = 2f
        };

        var system = new ResourceHaulerUtilitySystemHelper();

        Assert.IsTrue(system.IsOilSourceBuilding(oilSource));
        Assert.IsTrue(system.IsFuelBuilding(fuelSource));
        Assert.IsTrue(system.HasAvailableFuelForHauler(fuelSource));
    }

    [Test]
    public void StorageTransfer_TryCompleteLoad_UsesComponentStorage()
    {
        var storage = new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 40,
            StoredOilBarrels = 18f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 3f
        };

        bool loaded = BuildingResourceStorageTransferSystemHelper.TryCompleteLoad(
            ref storage,
            BuildingResourceStorageTransferSystemHelper.OilResourceKind,
            8f,
            ref hauler);

        Assert.IsTrue(loaded);
        Assert.AreEqual(10f, storage.StoredOilBarrels);
        Assert.AreEqual(8f, hauler.CargoOilBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void StorageTransfer_TryCompleteUnload_UsesComponentStorage()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 25,
            StoredFuelBarrels = 17f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 6f
        };

        bool unloaded = BuildingResourceStorageTransferSystemHelper.TryCompleteUnload(
            ref storage,
            BuildingResourceStorageTransferSystemHelper.FuelResourceKind,
            ref hauler);

        Assert.IsTrue(unloaded);
        Assert.AreEqual(23f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void HaulerTransferEcs_TryCompleteLoad_UsesComponentStorage()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 40,
            StoredFuelBarrels = 22f
        };
        UnitResourceHauler hauler = new()
        {
            CargoOilBarrels = 5f
        };

        bool loaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteLoad(
            ref storage,
            BuildingResourceStorageTransferSystemHelper.FuelResourceKind,
            9f,
            ref hauler);

        Assert.IsTrue(loaded);
        Assert.AreEqual(13f, storage.StoredFuelBarrels);
        Assert.AreEqual(9f, hauler.CargoFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
    }

    [Test]
    public void HaulerTransferEcs_TryCompleteUnload_UsesComponentStorage()
    {
        var storage = new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 25,
            StoredOilBarrels = 14f
        };
        UnitResourceHauler hauler = new()
        {
            CargoOilBarrels = 4f
        };

        bool unloaded = BuildingResourceHaulerTransferEcsSystem.TryCompleteUnload(
            ref storage,
            BuildingResourceStorageTransferSystemHelper.OilResourceKind,
            ref hauler);

        Assert.IsTrue(unloaded);
        Assert.AreEqual(18f, storage.StoredOilBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
    }

    [Test]
    public void LiveEcsStorage_TryCompleteLoad_PrefersCombatEntityStorage()
    {
        var world = new World(nameof(LiveEcsStorage_TryCompleteLoad_PrefersCombatEntityStorage));
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                OilStorageCapacity = 40,
                StoredOilBarrels = 18f
            });
            var source = new RuntimeBuildingEntity
            {
                Id = 44,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 40
                },
                CombatEntity = entity,
                StoredOilBarrels = 30f
            };
            UnitResourceHauler hauler = new()
            {
                CargoFuelBarrels = 3f
            };

            bool loaded = new ResourceHaulerUtilitySystemHelper().TryCompleteLoad(
                em,
                source,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil,
                8f,
                ref hauler);

            BuildingResourceStorageComponent storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
            Assert.IsTrue(loaded);
            Assert.AreEqual(10f, storage.StoredOilBarrels);
            Assert.AreEqual(10f, source.StoredOilBarrels);
            Assert.AreEqual(8f, hauler.CargoOilBarrels);
            Assert.AreEqual(0f, hauler.CargoFuelBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void LiveEcsStorage_TryCompleteUnload_PrefersCombatEntityStorage()
    {
        var world = new World(nameof(LiveEcsStorage_TryCompleteUnload_PrefersCombatEntityStorage));
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                FuelStorageCapacity = 25,
                StoredFuelBarrels = 17f
            });
            var destination = new RuntimeBuildingEntity
            {
                Id = 45,
                Definition = new BuildingDefinition
                {
                    FuelStorageCapacity = 25
                },
                CombatEntity = entity,
                StoredFuelBarrels = 1f
            };
            UnitResourceHauler hauler = new()
            {
                CargoFuelBarrels = 6f
            };

            bool unloaded = new ResourceHaulerUtilitySystemHelper().TryCompleteUnload(
                em,
                destination,
                ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel,
                ref hauler);

            BuildingResourceStorageComponent storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
            Assert.IsTrue(unloaded);
            Assert.AreEqual(23f, storage.StoredFuelBarrels);
            Assert.AreEqual(23f, destination.StoredFuelBarrels);
            Assert.AreEqual(0f, hauler.CargoFuelBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void LiveEcsStorage_GetStoredResource_PrefersCombatEntityStorage()
    {
        var world = new World(nameof(LiveEcsStorage_GetStoredResource_PrefersCombatEntityStorage));
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                OilStorageCapacity = 80,
                FuelStorageCapacity = 50,
                StoredOilBarrels = 33f,
                StoredFuelBarrels = 21f
            });
            var building = new RuntimeBuildingEntity
            {
                Id = 46,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 80,
                    FuelStorageCapacity = 50
                },
                CombatEntity = entity,
                StoredOilBarrels = 2f,
                StoredFuelBarrels = 4f
            };

            var system = new ResourceHaulerUtilitySystemHelper();
            float oil = system.GetStoredResource(em, building, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil);
            float fuel = system.GetStoredResource(em, building, ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel);

            Assert.AreEqual(33f, oil);
            Assert.AreEqual(21f, fuel);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void LiveEcsStorage_HasAvailableFuelForHauler_PrefersCombatEntityStorage()
    {
        var world = new World(nameof(LiveEcsStorage_HasAvailableFuelForHauler_PrefersCombatEntityStorage));
        try
        {
            EntityManager em = world.EntityManager;
            Entity entity = em.CreateEntity(typeof(BuildingResourceStorageComponent));
            em.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                FuelStorageCapacity = 40,
                FuelBarrelsPerDay = 5f,
                StoredFuelBarrels = 0f
            });
            var building = new RuntimeBuildingEntity
            {
                Id = 47,
                Definition = new BuildingDefinition
                {
                    FuelStorageCapacity = 40,
                    FuelBarrelsPerDay = 5f
                },
                CombatEntity = entity,
                StoredFuelBarrels = 12f
            };

            bool hasFuel = new ResourceHaulerUtilitySystemHelper().HasAvailableFuelForHauler(em, building);

            Assert.IsFalse(hasFuel);
        }
        finally
        {
            world.Dispose();
        }
    }

    private sealed class TestHaulerBuilding : FactionResourceCompositionSystemHelper.IResourceBuilding
    {
        public bool IsDestroyed { get; set; }
        public bool HasOwnerFaction { get; set; }
        public byte OwnerFactionId { get; set; }
        public int OilStorageCapacity { get; set; }
        public int FuelStorageCapacity { get; set; }
        public float OilBarrelsPerDay { get; set; }
        public float FuelBarrelsPerDay { get; set; }
        public float StoredOilBarrels { get; set; }
        public float StoredFuelBarrels { get; set; }
    }
}
#endif
