using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class VehicleFuelConsumptionSystemTests
{
    [Test]
    public void VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement()
    {
        using World world = new(nameof(VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<VehicleFuelConsumptionSystem>();
        Entity storage = CreateFuelStorage(em, storedFuel: 20f, fuelRate: 0f);
        Entity unit = CreateFuelUsingUnit(em, new int2(1, 1), groundFuelPerCell: 1.5f);

        world.Unmanaged.UpdateSystem(system);
        Assert.AreEqual(20f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);

        UnitGrid grid = em.GetComponentData<UnitGrid>(unit);
        grid.Cell = new int2(4, 2);
        em.SetComponentData(unit, grid);

        world.Unmanaged.UpdateSystem(system);

        BuildingResourceStorageComponent storageAfterMove = em.GetComponentData<BuildingResourceStorageComponent>(storage);
        UnitFuelConsumptionState consumptionState = em.GetComponentData<UnitFuelConsumptionState>(unit);
        Assert.AreEqual(14f, storageAfterMove.StoredFuelBarrels);
        Assert.AreEqual(1u, storageAfterMove.Version);
        Assert.AreEqual(new int2(4, 2), consumptionState.LastCell);
    }

    [Test]
    public void VehicleFuelConsumption_DoesNotDrainRefineryOutput()
    {
        using World world = new(nameof(VehicleFuelConsumption_DoesNotDrainRefineryOutput));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<VehicleFuelConsumptionSystem>();
        Entity refinery = CreateFuelStorage(em, storedFuel: 20f, fuelRate: 10f);
        Entity unit = CreateFuelUsingUnit(em, new int2(1, 1), groundFuelPerCell: 1f);

        world.Unmanaged.UpdateSystem(system);
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(3, 1) });
        world.Unmanaged.UpdateSystem(system);

        BuildingResourceStorageComponent refineryAfterMove = em.GetComponentData<BuildingResourceStorageComponent>(refinery);
        Assert.AreEqual(20f, refineryAfterMove.StoredFuelBarrels);
        Assert.AreEqual(0u, refineryAfterMove.Version);
    }

    [Test]
    public void VehicleFuelConsumption_UsesAirFuelCostForAirUnits()
    {
        using World world = new(nameof(VehicleFuelConsumption_UsesAirFuelCostForAirUnits));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<VehicleFuelConsumptionSystem>();
        Entity storage = CreateFuelStorage(em, storedFuel: 20f, fuelRate: 0f);
        Entity unit = CreateFuelUsingUnit(em, new int2(0, 0), groundFuelPerCell: 1f, airFuelPerCell: 2f);
        em.AddComponentData(unit, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 4f });

        world.Unmanaged.UpdateSystem(system);
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(0, 3) });
        world.Unmanaged.UpdateSystem(system);

        BuildingResourceStorageComponent storageAfterMove = em.GetComponentData<BuildingResourceStorageComponent>(storage);
        Assert.AreEqual(14f, storageAfterMove.StoredFuelBarrels);
    }

    [Test]
    public void AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome()
    {
        using World world = new(nameof(AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<AircraftFuelSafetyReturnSystem>();
        CreateFuelStorage(em, storedFuel: 0f, fuelRate: 0f);
        Entity aircraft = CreateFuelUsingUnit(em, new int2(4, 5), groundFuelPerCell: 0f, airFuelPerCell: 1f);
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 4f });
        em.AddComponentData(aircraft, new UnitAirComponent
        {
            HomeInitialized = 1,
            HomeCell = new int2(1, 2),
            HomePosition = new float3(1f, 0f, 2f),
            Airborne = 1,
            ReturningHome = 0,
            AttackRunActive = 1,
            ReturnApproachInitialized = 1
        });
        em.AddComponentData(aircraft, new UnitTarget { Cell = new int2(10, 11) });
        em.AddComponentData(aircraft, new EngageTarget { Target = Entity.Null, Cell = new int2(12, 13), IsCommanded = 1 });
        em.AddComponentData(aircraft, new UnitPathRequest { Goal = new int2(14, 15) });
        em.AddComponentData<ManualMoveOrderTag>(aircraft);
        em.AddComponentData<UnitScanOrder>(aircraft);
        em.AddComponentData<UnitTransportAirdropRequest>(aircraft);
        em.AddComponentData<UnitTransportRopeDisembarkRequest>(aircraft);

        world.Unmanaged.UpdateSystem(system);

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(aircraft);
        Assert.AreEqual(1, airState.ReturningHome);
        Assert.AreEqual(0, airState.AttackRunActive);
        Assert.AreEqual(0, airState.ReturnApproachInitialized);
        Assert.IsFalse(em.HasComponent<UnitTarget>(aircraft));
        Assert.IsFalse(em.HasComponent<EngageTarget>(aircraft));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(aircraft));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(aircraft));
        Assert.IsFalse(em.HasComponent<UnitScanOrder>(aircraft));
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(aircraft));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(aircraft));
    }

    [Test]
    public void AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders()
    {
        using World world = new(nameof(AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<AircraftFuelSafetyReturnSystem>();
        CreateFuelStorage(em, storedFuel: 10f, fuelRate: 0f);
        Entity aircraft = CreateFuelUsingUnit(em, new int2(4, 5), groundFuelPerCell: 0f, airFuelPerCell: 1f);
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 4f });
        em.AddComponentData(aircraft, new UnitAirComponent
        {
            HomeInitialized = 1,
            HomeCell = new int2(1, 2),
            HomePosition = new float3(1f, 0f, 2f),
            Airborne = 1,
            ReturningHome = 0,
            AttackRunActive = 1,
            ReturnApproachInitialized = 1
        });
        em.AddComponentData(aircraft, new UnitTarget { Cell = new int2(10, 11) });
        em.AddComponentData<ManualMoveOrderTag>(aircraft);

        world.Unmanaged.UpdateSystem(system);

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(aircraft);
        Assert.AreEqual(0, airState.ReturningHome);
        Assert.AreEqual(1, airState.AttackRunActive);
        Assert.AreEqual(1, airState.ReturnApproachInitialized);
        Assert.IsTrue(em.HasComponent<UnitTarget>(aircraft));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(aircraft));
    }

    private static Entity CreateFuelUsingUnit(
        EntityManager em,
        int2 cell,
        float groundFuelPerCell,
        float airFuelPerCell = 0f)
    {
        Entity unit = em.CreateEntity(
            typeof(UnitGrid),
            typeof(Faction),
            typeof(UnitMovementBehavior),
            typeof(UnitFuelConsumption),
            typeof(UnitFuelConsumptionState));
        em.SetComponentData(unit, new UnitGrid { Cell = cell });
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(unit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(unit, new UnitFuelConsumption
        {
            Enabled = 1,
            GroundFuelPerCell = groundFuelPerCell,
            AirFuelPerCell = airFuelPerCell
        });
        em.SetComponentData(unit, new UnitFuelConsumptionState());
        return unit;
    }

    private static Entity CreateFuelStorage(EntityManager em, float storedFuel, float fuelRate)
    {
        Entity storage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 7,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            FuelStorageCapacity = 100,
            FuelBarrelsPerDay = fuelRate,
            StoredFuelBarrels = storedFuel
        });
        return storage;
    }
}
