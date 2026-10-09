using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class VehicleFuelConsumptionSystemTests
{
    public static void RunFuelGridContextValidation()
    {
        try
        {
            var tests = new VehicleFuelConsumptionSystemTests();
            tests.VehicleFuelConsumption_AmbiguousGridKeepsExistingMembershipAccounting(false);
            tests.VehicleFuelConsumption_AmbiguousGridKeepsExistingMembershipAccounting(true);
            tests.VehicleFuelConsumption_AirPositionUsesGridOriginAndCellSize();
            ValidationExit.ClearLastExitCode();
            RunRunwayApproachFuelValidation();
            Assert.AreEqual(0, ValidationExit.LastExitCode, "Recorded approaches and existing Fuel regressions must pass.");
            UnityEngine.Debug.Log("[FuelGridContextValidation] result=Passed cases=3 approachCases=2 existingFuelRegressions=8");
            ValidationExit.Passed();
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[FuelGridContextValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void VehicleFuelConsumption_AmbiguousGridKeepsExistingMembershipAccounting(bool aircraft)
    {
        using World world = new(nameof(VehicleFuelConsumption_AmbiguousGridKeepsExistingMembershipAccounting));
        var em = world.EntityManager;
        for (int i = 0; i < 2; i++)
            em.SetComponentData(em.CreateEntity(typeof(GridConfig)),
                new GridConfig { Width = 100, Height = 100, CellSize = i + 1, Origin = float3.zero });
        var storage = CreateFuelStorage(em, 20, 0);
        var unit = CreateFuelUsingUnit(em, new int2(1, 1), groundFuelPerCell: 1.5f, airFuelPerCell: 2);
        em.AddComponentData(unit, Unity.Transforms.LocalTransform.FromPosition(new float3(70, 0, 80)));
        if (aircraft) em.AddComponentData(unit, new UnitAirMovement { CruiseHeight = 12, RunwayTaxiSpeed = 4 });
        var fuel = world.CreateSystem<VehicleFuelConsumptionSystem>();
        Assert.DoesNotThrow(() => fuel.Update(world.Unmanaged), "Fuel does not require a single physical grid for existing membership accounting.");
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(4, 2) });
        fuel.Update(world.Unmanaged);
        Assert.AreEqual(aircraft ? 12f : 14f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);
        Assert.AreEqual(new int2(4, 2), em.GetComponentData<UnitFuelConsumptionState>(unit).LastCell);
    }

    [Test]
    public void VehicleFuelConsumption_AirPositionUsesGridOriginAndCellSize()
    {
        using World world = new(nameof(VehicleFuelConsumption_AirPositionUsesGridOriginAndCellSize));
        var em = world.EntityManager;
        em.SetComponentData(em.CreateEntity(typeof(GridConfig)),
            new GridConfig { Width = 10, Height = 10, CellSize = 2, Origin = new float3(40, 0, -100) });
        var storage = CreateFuelStorage(em, 20, 0);
        var unit = CreateFuelUsingUnit(em, new int2(0, 0), groundFuelPerCell: 1, airFuelPerCell: 2);
        em.AddComponentData(unit, new UnitAirMovement { CruiseHeight = 12, RunwayTaxiSpeed = 4 });
        em.AddComponentData(unit, Unity.Transforms.LocalTransform.FromPosition(new float3(41, 12, -99)));
        var fuel = world.CreateSystem<VehicleFuelConsumptionSystem>();
        fuel.Update(world.Unmanaged);
        em.SetComponentData(unit, Unity.Transforms.LocalTransform.FromPosition(new float3(47, 12, -103)));
        fuel.Update(world.Unmanaged);
        Assert.AreEqual(new int2(3, -2), em.GetComponentData<UnitFuelConsumptionState>(unit).LastCell);
        Assert.AreEqual(new int2(0, 0), em.GetComponentData<UnitGrid>(unit).Cell, "Fuel accounting must not write external cells into ground-grid membership.");
        Assert.AreEqual(10f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);
    }

    public static void RunRunwayApproachFuelValidation()
    {
        try
        {
            var tests = new VehicleFuelConsumptionSystemTests();
            tests.VehicleFuelConsumption_RunwayApproachAccountsForFlightBeyondGroundGrid(1f / 30f);
            tests.VehicleFuelConsumption_RunwayApproachAccountsForFlightBeyondGroundGrid(.1f);
            RunFocusedValidation();
            UnityEngine.Debug.Log("[RunwayApproachFuelValidation] result=Passed cases=2 existingFuelRegressions=8");
            ValidationExit.Passed();
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[RunwayApproachFuelValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [TestCase(1f / 30f)]
    [TestCase(.1f)]
    public void VehicleFuelConsumption_RunwayApproachAccountsForFlightBeyondGroundGrid(float step)
    {
        // Native791's real runway rejoin passes north of the ground grid.
        // Reproduce its recorded aircraft state in an isolated test world and
        // compare the native fuel drain against every physical movement cell.
        using World world = new(nameof(VehicleFuelConsumption_RunwayApproachAccountsForFlightBeyondGroundGrid));
        var em = world.EntityManager;
        var grid = new GridConfig { Width = 2048, Height = 1024, CellSize = 1, Origin = float3.zero };
        em.SetComponentData(em.CreateEntity(typeof(GridConfig)), grid);
        Entity storage = CreateFuelStorage(em, storedFuel: 2000, fuelRate: 0);
        var stock = em.GetComponentData<BuildingResourceStorageComponent>(storage);
        stock.FuelStorageCapacity = 2000;
        em.SetComponentData(storage, stock);
        Entity aircraft = CreateFuelUsingUnit(em, new int2(1391, 620), groundFuelPerCell: .05f, airFuelPerCell: .25f);
        em.AddComponentData(aircraft, new UnitMove { Speed = 36, WalkSpeed = 36, RoadSpeedMultiplier = 1, ArriveDistance = .05f });
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 28, RunwayTaxiSpeed = 5 });
        em.AddComponentData(aircraft, new UnitAirComponent
        {
            HomeInitialized = 1, HomePosition = new float3(837.5f, .82416415f, 710.5f), HomeCell = new int2(837, 710),
            ReturningHome = 1, Airborne = 1, UsesRunway = 1, AttackRunActive = 4, AttackManeuverTurnSign = -1,
            AttackPassGoalCell = new int2(1402, 635),
            RunwayTakeoffPosition = new float3(832.32257f, .79416418f, 775.67499f), RunwayTakeoffCell = new int2(832, 775),
            RunwayLandingPosition = new float3(832.35736f, .79416418f, 565.56500f), RunwayLandingCell = new int2(832, 565),
            AttackRunExitPosition = new float3(832.38202f, 28.824163f, 416.59598f),
            FixedWingCruiseY = 28.824163f, FixedWingCruiseYInitialized = 1
        });
        em.AddComponentData(aircraft, Unity.Transforms.LocalTransform.FromPositionRotationScale(
            new float3(1391.2417f, 28.824163f, 620.11432f),
            new quaternion(-.23818724f, .88892686f, -.10126279f, .37791789f), 1));
        var movement = world.CreateSystem<UnitAirMovementSystem>();
        var fuel = world.CreateSystem<VehicleFuelConsumptionSystem>();
        fuel.Update(world.Unmanaged);
        int2 lastCell = new(1391, 620);
        float expectedDrain = 0;
        int outsideFrames = 0;
        double elapsed = 0;
        for (int frame = 1; elapsed < 180; frame++)
        {
            elapsed += step;
            world.SetTime(new Unity.Core.TimeData(elapsed, step));
            movement.Update(world.Unmanaged);
            var pose = em.GetComponentData<Unity.Transforms.LocalTransform>(aircraft);
            int2 physicalCell = GridUtils.WorldToCell(grid, pose.Position);
            int2 delta = physicalCell - lastCell;
            expectedDrain += (math.abs(delta.x) + math.abs(delta.y)) * .25f;
            lastCell = physicalCell;
            outsideFrames += GridUtils.InBounds(physicalCell, grid.Width, grid.Height) ? 0 : 1;
            fuel.Update(world.Unmanaged);
            float actualDrain = 2000 - em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels;
            Assert.AreEqual(expectedDrain, actualDrain, .001f,
                "Aircraft Fuel must account for the physical runway approach even beyond the ground grid. cell=" + physicalCell + " grid=" + em.GetComponentData<UnitGrid>(aircraft).Cell);
            var air = em.GetComponentData<UnitAirComponent>(aircraft);
            if (air.Airborne != 0 || air.ReturningHome != 0 || air.LandingRolling != 0) continue;
            Assert.Greater(outsideFrames, 0, "Recorded approach must exercise flight outside the physical ground grid.");
            Assert.Less(math.distance(pose.Position, air.HomePosition), 1.5f);
            UnityEngine.Debug.Log("[RunwayApproachFuelCase] result=Passed step=" + step + " elapsed=" + elapsed + " outsideFrames=" + outsideFrames + " fuelSpent=" + actualDrain);
            return;
        }
        Assert.Fail("Recorded native approach must complete landing and taxi within 180 seconds.");
    }

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new VehicleFuelConsumptionSystemTests();
            tests.VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement();
            tests.VehicleFuelConsumption_DoesNotDrainRefineryOutput();
            tests.VehicleFuelConsumption_UsesAirFuelCostForAirUnits();
            tests.AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome();
            tests.AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders();
            tests.AircraftFuelSafetyReturn_AlreadyReturningPreservesManeuverAndLandingProgress();
            tests.GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics();
            tests.GroundVehicleFuelHold_WithUsableFuelKeepsMovement();
            UnityEngine.Debug.Log("[VehicleFuelConsumptionFocusedValidation] result=Passed tests=8");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[VehicleFuelConsumptionFocusedValidation] result=Failed");
            throw;
        }
    }

    [Test]
    public void VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement()
    {
        using World world = new(nameof(VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<VehicleFuelConsumptionSystem>();
        Entity storage = CreateFuelStorage(em, storedFuel: 20f, fuelRate: 0f);
        Entity unit = CreateFuelUsingUnit(em, new int2(1, 1), groundFuelPerCell: 1.5f);

        system.Update(world.Unmanaged);
        Assert.AreEqual(20f, em.GetComponentData<BuildingResourceStorageComponent>(storage).StoredFuelBarrels);

        UnitGrid grid = em.GetComponentData<UnitGrid>(unit);
        grid.Cell = new int2(4, 2);
        em.SetComponentData(unit, grid);

        system.Update(world.Unmanaged);

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

        system.Update(world.Unmanaged);
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(3, 1) });
        system.Update(world.Unmanaged);

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

        system.Update(world.Unmanaged);
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(0, 3) });
        system.Update(world.Unmanaged);

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
        em.AddComponentData(aircraft, new ManualMoveOrderTag());
        em.AddComponentData(aircraft, new UnitScanOrder());
        em.AddComponentData(aircraft, new UnitTransportAirdropRequest());
        em.AddComponentData(aircraft, new UnitTransportRopeDisembarkRequest());

        system.Update(world.Unmanaged);

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
    public void AircraftFuelSafetyReturn_AlreadyReturningPreservesManeuverAndLandingProgress()
    {
        using World world = new(nameof(AircraftFuelSafetyReturn_AlreadyReturningPreservesManeuverAndLandingProgress));
        var em = world.EntityManager;
        var system = world.CreateSystem<AircraftFuelSafetyReturnSystem>();
        CreateFuelStorage(em, storedFuel: 0f, fuelRate: 0f);
        var aircraft = CreateFuelUsingUnit(em, new int2(4, 5), groundFuelPerCell: 0f, airFuelPerCell: 1f);
        em.AddComponentData(aircraft, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 4f });
        var air = new UnitAirComponent
        {
            HomeInitialized = 1, Airborne = 1, ReturningHome = 1, UsesRunway = 1,
            AttackRunActive = 5, AttackManeuverTurnSign = -1,
            AttackRunExitPosition = new float3(42, 12, 18)
        };
        em.AddComponentData(aircraft, air);
        em.AddComponentData(aircraft, new UnitTarget { Cell = new int2(10, 11) });
        for (int tick = 0; tick < 16; tick++) system.Update(world.Unmanaged);
        var maneuver = em.GetComponentData<UnitAirComponent>(aircraft);
        Assert.AreEqual(5, maneuver.AttackRunActive);
        Assert.AreEqual(-1, maneuver.AttackManeuverTurnSign);
        Assert.AreEqual(air.AttackRunExitPosition, maneuver.AttackRunExitPosition);
        Assert.IsFalse(em.HasComponent<UnitTarget>(aircraft), "Low Fuel still clears active combat orders.");
        air.ReturnApproachInitialized = 1;
        air.AttackRunActive = 0;
        air.LandingRolling = 1;
        em.SetComponentData(aircraft, air);
        for (int tick = 0; tick < 16; tick++) system.Update(world.Unmanaged);
        var landing = em.GetComponentData<UnitAirComponent>(aircraft);
        Assert.AreEqual(1, landing.ReturnApproachInitialized);
        Assert.AreEqual(1, landing.LandingRolling);
        Assert.AreEqual(1, landing.ReturningHome);
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
        em.AddComponentData(aircraft, new ManualMoveOrderTag());

        system.Update(world.Unmanaged);

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(aircraft);
        Assert.AreEqual(0, airState.ReturningHome);
        Assert.AreEqual(1, airState.AttackRunActive);
        Assert.AreEqual(1, airState.ReturnApproachInitialized);
        Assert.IsTrue(em.HasComponent<UnitTarget>(aircraft));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(aircraft));
    }

    [Test]
    public void GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics()
    {
        using World world = new(nameof(GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<GroundVehicleFuelHoldSystem>();
        CreateFuelStorage(em, storedFuel: 0f, fuelRate: 0f);
        Entity vehicle = CreateFuelUsingUnit(em, new int2(6, 7), groundFuelPerCell: 1f);
        em.AddComponentData(vehicle, new UnitTarget { Cell = new int2(10, 11) });
        em.AddComponentData(vehicle, new UnitPathRequest { Goal = new int2(10, 11) });
        em.AddComponentData(vehicle, new UnitPathFollow { PathIndex = 3 });
        em.AddComponentData(vehicle, new UnitPathRange { Start = 2, Length = 6 });
        em.AddComponentData(vehicle, new UnitPathRetryCooldown { ResumeFrame = 99 });
        em.AddComponentData(vehicle, new UnitLongDistanceMove { FinalGoal = new int2(12, 13) });
        em.AddComponentData(vehicle, new ManualMoveOrderTag());
        em.AddComponentData(vehicle, new UnitVehicleKinematics { CurrentSpeed = 8f, StallSeconds = 2f });

        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTarget>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitPathFollow>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitPathRange>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitPathRetryCooldown>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitLongDistanceMove>(vehicle));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(vehicle));
        UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(vehicle);
        Assert.AreEqual(0f, kinematics.CurrentSpeed, 0.0001f);
        Assert.AreEqual(0f, kinematics.StallSeconds, 0.0001f);
    }

    [Test]
    public void GroundVehicleFuelHold_WithUsableFuelKeepsMovement()
    {
        using World world = new(nameof(GroundVehicleFuelHold_WithUsableFuelKeepsMovement));
        EntityManager em = world.EntityManager;
        SystemHandle system = world.CreateSystem<GroundVehicleFuelHoldSystem>();
        CreateFuelStorage(em, storedFuel: 5f, fuelRate: 0f);
        Entity vehicle = CreateFuelUsingUnit(em, new int2(6, 7), groundFuelPerCell: 1f);
        em.AddComponentData(vehicle, new UnitTarget { Cell = new int2(10, 11) });
        em.AddComponentData(vehicle, new UnitPathRequest { Goal = new int2(10, 11) });
        em.AddComponentData(vehicle, new ManualMoveOrderTag());
        em.AddComponentData(vehicle, new UnitVehicleKinematics { CurrentSpeed = 8f, StallSeconds = 2f });

        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTarget>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(vehicle));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(vehicle));
        UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(vehicle);
        Assert.AreEqual(8f, kinematics.CurrentSpeed, 0.0001f);
        Assert.AreEqual(2f, kinematics.StallSeconds, 0.0001f);
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
