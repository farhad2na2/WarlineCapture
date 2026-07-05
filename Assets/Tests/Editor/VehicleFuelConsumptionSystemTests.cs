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
