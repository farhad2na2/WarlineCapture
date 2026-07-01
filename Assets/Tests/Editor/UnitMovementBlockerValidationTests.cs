using NUnit.Framework;
using System.Reflection;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class UnitMovementBlockerValidationTests
{
    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UnitMovementBlockerValidationTests();
            tests.UnitMovementTargetRejectsBuildingBlockerCells();
            tests.EngagedCombatMovementStopsBeforeBuildingBlocker();
            tests.EngagedCombatMovementDoesNotMoveTowardDebugFireTarget();
            tests.AirMovementDoesNotMoveTowardDebugFireTarget();
            tests.AirMovementDoesNotMoveDuringDebugFireStateWithoutEngageTarget();
            tests.InfantryMovementDoesNotStallOnOwnPreviousOccupancySnapshot();
            tests.VehicleConfiguredFootprintOverridesRenderedBounds();
            tests.AuthoredUsaTankPlacementsAreVehicleWalkableInBakedSurface();
            tests.LoggedAuthoredUsaTankPlacementHasVehicleDepartureSurface();
            tests.MapVehiclePlacementReadModelProjectsSourceKeyAndPrefabEntityData();
            tests.MapVehiclePlacementProgressStateTracksEmptyConfigCompletion();
            tests.MapVehiclePlacementClearanceRemovesBlockersUnderVehicleFootprint();
            tests.MapVehiclePlacementDepartureClearanceRemovesPaddedBlockers();
            tests.VehiclePathingCanDepartFromCurrentDynamicBlockedFootprint();
            tests.VehiclePathingStillRejectsNewDynamicBlockedFootprintCells();
            tests.PathRequestIgnoredOccupancyDefaultsToMovingUnitFootprint();
            tests.VehiclePathingCanDepartFromCurrentPaddedClearanceOccupancy();
            tests.VehicleMovementCanDepartFromCurrentPaddedClearanceOccupancy();
            tests.InfantryOpenPathMovementAdvancesEveryFrame();
            Debug.Log("[UnitMovementBlockerValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitMovementBlockerValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunHoldCommandFocusedValidation()
    {
        try
        {
            var tests = new UnitMovementBlockerValidationTests();
            tests.HeldEngagedCombatMovementClearsTargetOutsideAttackRange();
            tests.AirHoldPositionDoesNotAutoReturnHomeWithoutTarget();
            Debug.Log("[HoldCommandMovementValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[HoldCommandMovementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunMapVehiclePlacementFocusedValidation()
    {
        try
        {
            var tests = new UnitMovementBlockerValidationTests();
            tests.MapVehiclePlacementReadModelProjectsSourceKeyAndPrefabEntityData();
            tests.MapVehiclePlacementProgressStateTracksEmptyConfigCompletion();
            tests.MapVehiclePlacementClearanceRemovesBlockersUnderVehicleFootprint();
            tests.MapVehiclePlacementDepartureClearanceRemovesPaddedBlockers();
            Debug.Log("[MapVehiclePlacementValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[MapVehiclePlacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void UnitMovementTargetRejectsBuildingBlockerCells()
    {
        var grid = new GridConfig
        {
            Width = 5,
            Height = 5,
            CellSize = 1f,
            Origin = float3.zero
        };

        var walkable = new NativeArray<GridWalkable>(grid.Width * grid.Height, Allocator.Temp);
        var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var friendlyPassFactionIds = new NativeArray<byte>(grid.Width * grid.Height, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                friendlyPassFactionIds[i] = byte.MaxValue;
            }

            int buildingIndex = GridUtils.CellToIndex(new int2(2, 2), grid.Width);
            blocked.Set(buildingIndex, true);

            Assert.IsFalse(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(2, 2),
                    new int2(1, 1),
                    new int2(1, 2),
                    factionId: 1),
                "Units must not move their path target onto a building or wall blocker cell.");

            Assert.IsTrue(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(1, 1),
                    new int2(1, 1),
                    new int2(1, 2),
                    factionId: 1),
                "Units should still accept an adjacent walkable, unblocked target cell.");

            int gateIndex = GridUtils.CellToIndex(new int2(3, 2), grid.Width);
            blocked.Set(gateIndex, true);
            friendlyPassFactionIds[gateIndex] = 0;

            Assert.IsTrue(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(3, 2),
                    new int2(1, 1),
                    new int2(3, 1),
                    factionId: 0),
                "A gate blocker should allow only its configured friendly faction through.");

            Assert.IsFalse(
                UnitGridMoveJob.CanOccupyMovementTarget(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    new int2(3, 2),
                    new int2(1, 1),
                    new int2(3, 1),
                    factionId: 1),
                "Enemy units must not pass through another faction's gate blocker.");
        }
        finally
        {
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void EngagedCombatMovementStopsBeforeBuildingBlocker()
    {
        using var world = new World("UnitMovementBlockerValidationTests");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 6, 3, out blockerCounts, out blocked, out friendlyPassFactionIds);
            blocked.Set(GridUtils.CellToIndex(new int2(2, 1), 6), true);

            Entity target = em.CreateEntity(
                typeof(UnitHealth),
                typeof(UnitFootprint));
            em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(target, new UnitFootprint { Size = new int2(1, 1) });

            Entity attacker = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(EngageTarget),
                typeof(LocalTransform));
            em.SetComponentData(attacker, new Faction { Id = 1 });
            em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitMove { Speed = 2f, WalkSpeed = 2f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(attacker, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(attacker, new UnitVehicleMovement());
            em.SetComponentData(attacker, new UnitVehicleKinematics());
            em.SetComponentData(attacker, new UnitCombat { AggroRangeCells = 8, ChaseBreakDistance = 20f, CanAttack = 1, AutoEngage = 1 });
            em.SetComponentData(attacker, new UnitAttack { Range = 0.1f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(attacker, new EngageTarget
            {
                Target = target,
                Cell = new int2(4, 1),
                Position = new float3(4.5f, 0f, 1.5f),
                IsCommanded = 1
            });
            em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

            SystemHandle engagedMoveSystem = world.CreateSystem<UnitEngagedMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            engagedMoveSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(attacker).Position;
            Assert.AreEqual(1.5f, position.x, 0.001f, "Engaged combat movement must not step into a blocked building/wall cell.");
            Assert.AreEqual(new int2(1, 1), em.GetComponentData<UnitGrid>(attacker).Cell);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void EngagedCombatMovementDoesNotMoveTowardDebugFireTarget()
    {
        using var world = new World("UnitMovementDebugFireValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 16, 3, out blockerCounts, out blocked, out friendlyPassFactionIds);

            Entity attacker = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(EngageTarget),
                typeof(LocalTransform));
            Entity debugTarget = em.CreateEntity(
                typeof(DebugFireTargetTag),
                typeof(UnitHealth),
                typeof(LocalTransform));

            em.SetComponentData(attacker, new Faction { Id = 1 });
            em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitFootprint { Size = new int2(2, 2) });
            em.SetComponentData(attacker, new UnitMove { Speed = 4f, WalkSpeed = 4f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(attacker, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
            em.SetComponentData(attacker, new UnitVehicleMovement { TurnSpeedDegrees = 360f, Acceleration = 20f, Braking = 20f, RearPivotOffset = 0f });
            em.SetComponentData(attacker, new UnitVehicleKinematics { CurrentSpeed = 1f });
            em.SetComponentData(attacker, new UnitCombat { AggroRangeCells = 8, ChaseBreakDistance = 20f, CanAttack = 1, AutoEngage = 1 });
            em.SetComponentData(attacker, new UnitAttack { Range = 0.1f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

            em.SetComponentData(debugTarget, new DebugFireTargetTag { Source = attacker });
            em.SetComponentData(debugTarget, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(debugTarget, LocalTransform.FromPosition(new float3(12.5f, 0f, 1.5f)));
            em.SetComponentData(attacker, new EngageTarget
            {
                Target = debugTarget,
                Cell = new int2(12, 1),
                Position = new float3(12.5f, 0f, 1.5f),
                IsCommanded = 1
            });

            SystemHandle engagedMoveSystem = world.CreateSystem<UnitEngagedMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            engagedMoveSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(attacker).Position;
            Assert.AreEqual(1.5f, position.x, 0.001f);
            Assert.AreEqual(1.5f, position.z, 0.001f);
            Assert.AreEqual(0f, em.GetComponentData<UnitVehicleKinematics>(attacker).CurrentSpeed, 0.001f);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void HeldEngagedCombatMovementClearsTargetOutsideAttackRange()
    {
        using var world = new World("HeldEngagedCombatMovementValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 16, 3, out blockerCounts, out blocked, out friendlyPassFactionIds);

            Entity target = em.CreateEntity(
                typeof(UnitHealth),
                typeof(UnitFootprint));
            em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(target, new UnitFootprint { Size = new int2(1, 1) });

            Entity attacker = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitCombat),
                typeof(UnitAttack),
                typeof(EngageTarget),
                typeof(HoldPositionOrderTag),
                typeof(LocalTransform));
            em.SetComponentData(attacker, new Faction { Id = 1 });
            em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(attacker, new UnitMove { Speed = 4f, WalkSpeed = 4f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(attacker, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(attacker, new UnitVehicleMovement());
            em.SetComponentData(attacker, new UnitVehicleKinematics { CurrentSpeed = 2f });
            em.SetComponentData(attacker, new UnitCombat { AggroRangeCells = 12, ChaseBreakDistance = 30f, CanAttack = 1, AutoEngage = 1 });
            em.SetComponentData(attacker, new UnitAttack { Range = 1f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(attacker, new EngageTarget
            {
                Target = target,
                Cell = new int2(8, 1),
                Position = new float3(8.5f, 0f, 1.5f),
                IsCommanded = 0
            });
            em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

            SystemHandle engagedMoveSystem = world.CreateSystem<UnitEngagedMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            engagedMoveSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(attacker).Position;
            EngageTarget engage = em.GetComponentData<EngageTarget>(attacker);
            Assert.AreEqual(Entity.Null, engage.Target, "Held units must drop targets outside their effective attack range instead of chasing.");
            Assert.AreEqual(1.5f, position.x, 0.001f);
            Assert.AreEqual(1.5f, position.z, 0.001f);
            Assert.AreEqual(0f, em.GetComponentData<UnitVehicleKinematics>(attacker).CurrentSpeed, 0.001f);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void AirMovementDoesNotMoveTowardDebugFireTarget()
    {
        using var world = new World("AirMovementDebugFireValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 16, 8, out blockerCounts, out blocked, out friendlyPassFactionIds);

            Entity helicopter = em.CreateEntity(
                typeof(UnitGrid),
                typeof(UnitMove),
                typeof(UnitAttack),
                typeof(UnitAirMovement),
                typeof(UnitAirComponent),
                typeof(EngageTarget),
                typeof(LocalTransform));
            Entity debugTarget = em.CreateEntity(
                typeof(DebugFireTargetTag),
                typeof(UnitHealth),
                typeof(LocalTransform));

            float3 startPosition = new(1.5f, 6f, 3.5f);
            float3 targetPosition = new(11.5f, 6f, 3.5f);
            em.SetComponentData(helicopter, new UnitGrid { Cell = new int2(1, 3) });
            em.SetComponentData(helicopter, new UnitMove { Speed = 12f, WalkSpeed = 12f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(helicopter, new UnitAttack { Range = 12f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
            em.SetComponentData(helicopter, new UnitAirComponent
            {
                HomePosition = new float3(1.5f, 0f, 3.5f),
                HomeCell = new int2(1, 3),
                HomeInitialized = 1,
                Airborne = 1,
                AttackRunActive = 1,
                ReturningHome = 1
            });
            em.SetComponentData(helicopter, LocalTransform.FromPosition(startPosition));

            em.SetComponentData(debugTarget, new DebugFireTargetTag { Source = helicopter });
            em.SetComponentData(debugTarget, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(debugTarget, LocalTransform.FromPosition(targetPosition));
            em.SetComponentData(helicopter, new EngageTarget
            {
                Target = debugTarget,
                Cell = new int2(11, 3),
                Position = targetPosition,
                IsCommanded = 1
            });

            SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            airMovementSystem.Update(world.Unmanaged);

            float3 position = em.GetComponentData<LocalTransform>(helicopter).Position;
            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(helicopter);
            Assert.AreEqual(startPosition.x, position.x, 0.001f, "Debug fire must not turn a helicopter's weapon target into a move target.");
            Assert.AreEqual(startPosition.y, position.y, 0.001f);
            Assert.AreEqual(startPosition.z, position.z, 0.001f);
            Assert.AreEqual(0, airState.AttackRunActive, "Debug fire should cancel air attack-run movement while preserving weapon fire.");
            Assert.AreEqual(0, airState.ReturningHome);
            Assert.AreEqual(1, airState.Airborne);
            Assert.IsTrue(em.HasComponent<EngageTarget>(helicopter), "Debug fire still needs EngageTarget so UnitAttackSystem can fire.");
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void AirMovementDoesNotMoveDuringDebugFireStateWithoutEngageTarget()
    {
        using var world = new World("AirMovementDebugFireStateValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 16, 8, out blockerCounts, out blocked, out friendlyPassFactionIds);

            Entity helicopter = em.CreateEntity(
                typeof(UnitGrid),
                typeof(UnitMove),
                typeof(UnitAttack),
                typeof(UnitAirMovement),
                typeof(UnitAirComponent),
                typeof(UnitTarget),
                typeof(ManualMoveOrderTag),
                typeof(SelectedUnitDebugFireState),
                typeof(LocalTransform));
            Entity debugTarget = em.CreateEntity(
                typeof(DebugFireTargetTag),
                typeof(UnitHealth),
                typeof(LocalTransform));

            float3 startPosition = new(1.5f, 6f, 3.5f);
            em.SetComponentData(helicopter, new UnitGrid { Cell = new int2(1, 3) });
            em.SetComponentData(helicopter, new UnitMove { Speed = 12f, WalkSpeed = 12f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(helicopter, new UnitAttack { Range = 12f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(helicopter, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 0f });
            em.SetComponentData(helicopter, new UnitAirComponent
            {
                HomePosition = new float3(1.5f, 0f, 3.5f),
                HomeCell = new int2(1, 3),
                HomeInitialized = 1,
                Airborne = 1,
                AttackRunActive = 1,
                ReturningHome = 1
            });
            em.SetComponentData(helicopter, new UnitTarget { Cell = new int2(11, 3) });
            em.SetComponentData(helicopter, new SelectedUnitDebugFireState { Target = debugTarget });
            em.SetComponentData(helicopter, LocalTransform.FromPosition(startPosition));

            em.SetComponentData(debugTarget, new DebugFireTargetTag { Source = helicopter });
            em.SetComponentData(debugTarget, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(debugTarget, LocalTransform.FromPosition(new float3(11.5f, 6f, 3.5f)));

            SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            airMovementSystem.Update(world.Unmanaged);

            float3 position = em.GetComponentData<LocalTransform>(helicopter).Position;
            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(helicopter);
            Assert.AreEqual(startPosition.x, position.x, 0.001f, "Air missile debug fire uses SelectedUnitDebugFireState without EngageTarget and still must suppress movement.");
            Assert.AreEqual(startPosition.y, position.y, 0.001f);
            Assert.AreEqual(startPosition.z, position.z, 0.001f);
            Assert.AreEqual(0, airState.AttackRunActive);
            Assert.AreEqual(0, airState.ReturningHome);
            Assert.AreEqual(1, airState.Airborne);
            Assert.IsTrue(em.HasComponent<UnitTarget>(helicopter), "Debug fire should pause existing air movement orders instead of consuming them.");
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void AirHoldPositionDoesNotAutoReturnHomeWithoutTarget()
    {
        using var world = new World("AirHoldPositionValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;

        try
        {
            CreateGrid(em, 16, 8, out blockerCounts, out blocked, out friendlyPassFactionIds);

            Entity jet = em.CreateEntity(
                typeof(UnitGrid),
                typeof(UnitMove),
                typeof(UnitAttack),
                typeof(UnitAirMovement),
                typeof(UnitAirComponent),
                typeof(HoldPositionOrderTag),
                typeof(LocalTransform));

            float3 startPosition = new(8.5f, 6f, 3.5f);
            em.SetComponentData(jet, new UnitGrid { Cell = new int2(8, 3) });
            em.SetComponentData(jet, new UnitMove { Speed = 12f, WalkSpeed = 12f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            em.SetComponentData(jet, new UnitAttack { Range = 12f, CooldownSeconds = 1f, Damage = 1 });
            em.SetComponentData(jet, new UnitAirMovement { CruiseHeight = 6f, RunwayTaxiSpeed = 4f });
            em.SetComponentData(jet, new UnitAirComponent
            {
                HomePosition = new float3(1.5f, 0f, 3.5f),
                HomeCell = new int2(1, 3),
                HomeInitialized = 1,
                Airborne = 1,
                UsesRunway = 1,
                AttackRunActive = 1,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                ReturnApproachInitialized = 1,
                RunwayTakeoffPosition = new float3(1.5f, 0f, 3.5f),
                RunwayTakeoffCell = new int2(1, 3),
                RunwayLandingPosition = new float3(5.5f, 0f, 3.5f),
                RunwayLandingCell = new int2(5, 3)
            });
            em.SetComponentData(jet, LocalTransform.FromPosition(startPosition));

            SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
            world.SetTime(new TimeData(0.4d, 0.4f));
            airMovementSystem.Update(world.Unmanaged);

            float3 position = em.GetComponentData<LocalTransform>(jet).Position;
            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(jet);
            Assert.AreEqual(startPosition.x, position.x, 0.001f, "Held airborne units should not auto-return to runway when no target remains.");
            Assert.AreEqual(startPosition.y, position.y, 0.001f);
            Assert.AreEqual(startPosition.z, position.z, 0.001f);
            Assert.AreEqual(1, airState.Airborne);
            Assert.AreEqual(0, airState.ReturningHome);
            Assert.AreEqual(0, airState.AttackRunActive);
            Assert.AreEqual(0, airState.ReturnApproachInitialized);
        }
        finally
        {
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InfantryMovementDoesNotStallOnOwnPreviousOccupancySnapshot()
    {
        using var world = new World("UnitMovementSelfOccupancyValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 4;
            const int height = 1;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            pathPool.Add(new int2(1, 0));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
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
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
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

            int targetIndex = GridUtils.CellToIndex(new int2(1, 0), width);
            occupied.Set(targetIndex, true);

            Entity unit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(unit, new Faction { Id = 0 });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(1, 0) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(unit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(unit, new UnitVehicleMovement());
            em.SetComponentData(unit, new UnitVehicleKinematics());
            em.SetComponentData(unit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(unit, new UnitPathRange { Start = 0, Length = 1 });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(0.5f, 0f, 0.5f)));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            movementSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(unit).Position;
            Assert.Greater(position.x, 0.5f, "Infantry must not spend a frame stalled when the only occupant in the next path cell is itself from the previous occupancy snapshot.");
            Assert.Less(position.x, 1.5f, "This validation should exercise normal in-flight movement, not path completion.");
        }
        finally
        {
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void VehicleConfiguredFootprintOverridesRenderedBounds()
    {
        GameObject testObject = new("ConfiguredFootprintVehicle");
        try
        {
            UnitGridAuthoring authoring = testObject.AddComponent<UnitGridAuthoring>();
            SetPrivateField(authoring, "usesVehicleMotion", true);
            SetPrivateField(authoring, "autoCalculateFootprint", true);
            SetPrivateField(authoring, "footprintCells", new Vector2Int(3, 3));

            System.Type bakerType = typeof(UnitGridAuthoring).GetNestedType("UnitGridBaker", BindingFlags.NonPublic);
            Assert.NotNull(bakerType, "UnitGridAuthoring baker type could not be found.");
            MethodInfo resolveFootprint = bakerType.GetMethod("ResolveFootprint", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(resolveFootprint, "UnitGridAuthoring footprint resolver could not be found.");

            object result = resolveFootprint.Invoke(
                null,
                new object[]
                {
                    authoring,
                    true,
                    new Bounds(Vector3.zero, new Vector3(5f, 2f, 10f))
                });

            Assert.AreEqual(new int2(3, 3), (int2)result, "Configured multi-cell vehicle footprints must stay authoritative over rendered model bounds.");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void MapVehiclePlacementReadModelProjectsSourceKeyAndPrefabEntityData()
    {
        using World world = new("MapVehiclePlacementReadModelProjectionTests");
        EntityManager em = world.EntityManager;
        GameObject vehiclePrefab = new("Unit_Veh_Tank_USA");
        MapVehiclePlacementConfig config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        try
        {
            config.EditorSetPlacements(new System.Collections.Generic.List<MapVehiclePlacementConfigEntry>
            {
                new(
                    "Map/Vehicles/MapVehicle_Tank_USA/SM_Veh_Tank_USA_01",
                    "Unit_Veh_Tank_USA",
                    vehiclePrefab,
                    factionId: 1,
                    worldCenter: new Vector3(10f, 2f, 20f),
                    worldPosition: new Vector3(11f, 1f, 21f),
                    worldEulerAngles: new Vector3(0f, 90f, 0f),
                    worldScale: Vector3.one)
            });

            Entity prefabEntity = em.CreateEntity(
                typeof(Prefab),
                typeof(UnitMove),
                typeof(UnitSourcePrefabKey),
                typeof(UnitFootprint));
            em.SetName(prefabEntity, "Unit_Veh_Tank_USA");
            em.SetComponentData(prefabEntity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("unit_veh_tank_usa") });
            em.SetComponentData(prefabEntity, new UnitFootprint { Size = new int2(3, 3) });

            Entity registryEntity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
            DynamicBuffer<UnitPrefabRegistryEntry> registry = em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            registry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });

            Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            EntityQuery prefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            EntityQuery liveUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<Faction>());

            var spawnPrefabSystem = new BuildingSpawnPrefabSystem();
            var spawnPrefabContext = new BuildingSpawnPrefabSystem.Context(
                registryQuery,
                prefabCandidatesQuery,
                liveUnitsQuery);
            var runtimeUnitPrefabContext = new RuntimeUnitPrefabSystem.Context(
                spawnPrefabSystem,
                TryGetEntityManager,
                null,
                () => spawnPrefabContext);
            var context = new MapVehiclePlacementSpawnPrefabSystemHelper.Context(
                config,
                null,
                new RuntimeUnitPrefabSystem(),
                runtimeUnitPrefabContext,
                null,
                null);

            int projected = MapVehiclePlacementSpawnPrefabSystemHelper.PublishPlacementReadModel(context, em, boundary);

            Assert.AreEqual(1, projected);
            Assert.IsTrue(em.HasBuffer<MapVehiclePlacementReadModel>(boundary));
            DynamicBuffer<MapVehiclePlacementReadModel> placements =
                em.GetBuffer<MapVehiclePlacementReadModel>(boundary, true);
            Assert.AreEqual(1, placements.Length);
            Assert.AreEqual(0, placements[0].PlacementIndex);
            Assert.AreEqual(new FixedString64Bytes("unit_veh_tank_usa"), placements[0].VehicleSourceKey);
            Assert.AreEqual(prefabEntity, placements[0].Prefab);
            Assert.AreEqual(1, placements[0].HasPrefab);
            Assert.AreEqual(new int2(3, 3), placements[0].FootprintCells);
            Assert.AreEqual(1, placements[0].FactionId);
            Assert.AreEqual(new float3(10f, 2f, 20f), placements[0].WorldCenter);
            Assert.AreEqual(new float3(11f, 1f, 21f), placements[0].WorldPosition);
        }
        finally
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(vehiclePrefab);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void MapVehiclePlacementProgressStateTracksEmptyConfigCompletion()
    {
        using World world = new("MapVehiclePlacementProgressStateTests");
        EntityManager em = world.EntityManager;
        MapVehiclePlacementConfig config = ScriptableObject.CreateInstance<MapVehiclePlacementConfig>();
        GameObject authoringRoot = new("MapVehicleAuthoringRoot");
        try
        {
            config.EditorSetPlacements(new System.Collections.Generic.List<MapVehiclePlacementConfigEntry>());
            var runtimeUnitPrefabContext = new RuntimeUnitPrefabSystem.Context(
                default,
                TryGetEntityManager,
                null,
                null);
            var context = new MapVehiclePlacementSpawnPrefabSystemHelper.Context(
                config,
                authoringRoot.transform,
                new RuntimeUnitPrefabSystem(),
                runtimeUnitPrefabContext,
                null,
                null);

            var system = new MapVehiclePlacementSpawnPrefabSystemHelper();
            system.Update(context);

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MapVehiclePlacementProgressState>());
            Assert.AreEqual(1, query.CalculateEntityCount());
            Entity progressEntity = query.GetSingletonEntity();
            MapVehiclePlacementProgressState progress = em.GetComponentData<MapVehiclePlacementProgressState>(progressEntity);
            Assert.AreEqual(1, progress.Queued);
            Assert.AreEqual(1, progress.AuthoringHidden);
            Assert.AreEqual(0, progress.NextPlacementIndex);
            Assert.AreEqual(0, progress.LastClearedBlockerCells);
            Assert.AreEqual(MapVehiclePlacementProgressState.InitialRandomState, progress.RandomState);
            Assert.IsTrue(system.IsComplete);
            Assert.IsFalse(authoringRoot.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(authoringRoot);
            Object.DestroyImmediate(config);
        }

        bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = em;
            return true;
        }
    }

    [Test]
    public void AuthoredUsaTankPlacementsAreVehicleWalkableInBakedSurface()
    {
        const string vehicleConfigPath = "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        const string mapSurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";

        MapVehiclePlacementConfig vehicleConfig = AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(vehicleConfigPath);
        MapSurfaceDataAsset mapSurfaceData = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(mapSurfacePath);

        Assert.NotNull(vehicleConfig, $"Missing map vehicle placement config at {vehicleConfigPath}.");
        Assert.NotNull(mapSurfaceData, $"Missing map surface data at {mapSurfacePath}.");
        Assert.IsTrue(mapSurfaceData.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> surfaceBlob));

        try
        {
            ref MapSurfaceBlob surface = ref surfaceBlob.Value;
            var grid = new GridConfig
            {
                Width = surface.Dimensions.x,
                Height = surface.Dimensions.y,
                CellSize = surface.CellSize,
                Origin = surface.GridOrigin
            };
            var slopeClassifier = new MapSurfaceSlopeClassifier();
            int checkedPlacements = 0;

            for (int placementIndex = 0; placementIndex < vehicleConfig.Placements.Count; placementIndex++)
            {
                MapVehiclePlacementConfigEntry placement = vehicleConfig.Placements[placementIndex];
                if (placement == null || placement.Category != "Unit_Veh_Tank_USA")
                    continue;

                int2 centerCell = GridUtils.WorldToCell(grid, new float3(placement.WorldCenter.x, placement.WorldCenter.y, placement.WorldCenter.z));
                int2 footprintSize = new(3, 3);
                int2 min = UnitFootprintUtility.GetMinCell(centerCell, footprintSize);
                int2 max = min + footprintSize;

                for (int y = min.y; y < max.y; y++)
                {
                    for (int x = min.x; x < max.x; x++)
                    {
                        Assert.IsTrue(
                            x >= 0 && x < surface.Dimensions.x && y >= 0 && y < surface.Dimensions.y,
                            $"Authored tank footprint leaves the baked grid. source={placement.SourcePath} cell=({x},{y})");

                        int cellIndex = (y * surface.Dimensions.x) + x;
                        MapSurfaceCell surfaceCell = surface.Cells[cellIndex];
                        Assert.Greater(
                            surfaceCell.SurfaceCount,
                            0,
                            $"Authored tank footprint has no baked surface. source={placement.SourcePath} cell=({x},{y})");

                        bool allowsTrackedVehicle = false;
                        int firstSurfaceIndex = surfaceCell.SurfaceCount == 1
                            ? surfaceCell.InlineSurfaceIndex
                            : surfaceCell.FirstSurfaceIndex;
                        for (int surfaceOffset = 0; surfaceOffset < surfaceCell.SurfaceCount; surfaceOffset++)
                        {
                            MapSurfaceSample sample = surface.Samples[firstSurfaceIndex + surfaceOffset];
                            if (slopeClassifier.AllowsMovement(sample, MapSurfaceMovementMask.TrackedVehicle))
                            {
                                allowsTrackedVehicle = true;
                                break;
                            }
                        }

                        Assert.IsTrue(
                            allowsTrackedVehicle,
                            $"Authored tank footprint is not tracked-vehicle walkable. source={placement.SourcePath} center={centerCell} cell=({x},{y})");
                    }
                }

                checkedPlacements++;
            }

            Assert.Greater(checkedPlacements, 0, "No Unit_Veh_Tank_USA placements were found in the baked vehicle placement config.");
        }
        finally
        {
            if (surfaceBlob.IsCreated)
                surfaceBlob.Dispose();
        }
    }

    [Test]
    public void LoggedAuthoredUsaTankPlacementHasVehicleDepartureSurface()
    {
        const string vehicleConfigPath = "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        const string mapSurfacePath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        const string loggedSourcePath = "Map/Vehicles/MapVehicle_Tank_USA/SM_Veh_Tank_USA_01 (1)";

        MapVehiclePlacementConfig vehicleConfig = AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(vehicleConfigPath);
        MapSurfaceDataAsset mapSurfaceData = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(mapSurfacePath);

        Assert.NotNull(vehicleConfig, $"Missing map vehicle placement config at {vehicleConfigPath}.");
        Assert.NotNull(mapSurfaceData, $"Missing map surface data at {mapSurfacePath}.");
        Assert.IsTrue(mapSurfaceData.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> surfaceBlob));

        try
        {
            MapVehiclePlacementConfigEntry loggedPlacement = null;
            for (int i = 0; i < vehicleConfig.Placements.Count; i++)
            {
                MapVehiclePlacementConfigEntry placement = vehicleConfig.Placements[i];
                if (placement == null ||
                    placement.FactionId != 1 ||
                    placement.Category != "Unit_Veh_Tank_USA" ||
                    placement.SourcePath != loggedSourcePath)
                {
                    continue;
                }

                loggedPlacement = placement;
                break;
            }

            Assert.NotNull(loggedPlacement, $"Could not find the logged stuck tank placement `{loggedSourcePath}`.");

            ref MapSurfaceBlob surface = ref surfaceBlob.Value;
            var grid = new GridConfig
            {
                Width = surface.Dimensions.x,
                Height = surface.Dimensions.y,
                CellSize = surface.CellSize,
                Origin = surface.GridOrigin
            };
            var surfaceComponent = new MapSurfaceComponent
            {
                SurfaceBlob = surfaceBlob,
                GridOrigin = surface.GridOrigin,
                CellSize = surface.CellSize,
                Dimensions = surface.Dimensions,
                HasSurfaceData = 1,
                HasLayeredCells = 1
            };
            var validation = new MapSurfaceTraversalValidation();
            int2 centerCell = GridUtils.WorldToCell(grid, new float3(loggedPlacement.WorldCenter.x, loggedPlacement.WorldCenter.y, loggedPlacement.WorldCenter.z));
            int2 footprintSize = new(3, 3);
            int2[] offsets =
            {
                new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
                new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
            };

            bool hasDeparture = false;
            int2 firstValidDeparture = default;
            for (int i = 0; i < offsets.Length; i++)
            {
                int2 candidate = centerCell + offsets[i];
                if (validation.CanTraverseFootprint(surfaceComponent, surfaceComponent.HasSurfaceData, grid, candidate, footprintSize, true))
                {
                    hasDeparture = true;
                    firstValidDeparture = candidate;
                    break;
                }
            }

            Assert.IsTrue(
                hasDeparture,
                $"Logged tank placement has no adjacent tracked-vehicle surface footprint. source={loggedSourcePath} center={centerCell}");
            Assert.AreNotEqual(centerCell, firstValidDeparture);
        }
        finally
        {
            if (surfaceBlob.IsCreated)
                surfaceBlob.Dispose();
        }
    }

    [Test]
    public void MapVehiclePlacementClearanceRemovesBlockersUnderVehicleFootprint()
    {
        var grid = new GridConfig
        {
            Width = 8,
            Height = 8,
            CellSize = 1f,
            Origin = float3.zero
        };
        int gridSize = grid.Width * grid.Height;
        var blockerCounts = new NativeArray<int>(gridSize, Allocator.Temp);
        var blocked = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Temp);

        try
        {
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = 7;

            int2 center = new(4, 4);
            int2 footprint = new(3, 3);
            int2 min = UnitFootprintUtility.GetMinCell(center, footprint);
            int2 max = min + footprint;
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    blocked.Set(index, true);
                    blockerCounts[index] = 2;
                }
            }

            int adjacentIndex = GridUtils.CellToIndex(new int2(1, 1), grid.Width);
            blocked.Set(adjacentIndex, true);
            blockerCounts[adjacentIndex] = 3;

            var blockerData = new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            };

            int cleared = MapVehiclePlacementSpawnPrefabSystemHelper.ClearRuntimeBlockersInFootprint(
                grid,
                ref blockerData,
                center,
                footprint);

            Assert.AreEqual(9, cleared, "The authored vehicle footprint should be cleared as a valid departure pad.");
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    Assert.IsFalse(blocked.IsSet(index), $"Vehicle footprint blocker was not cleared at {x},{y}.");
                    Assert.AreEqual(0, blockerCounts[index], $"Vehicle footprint blocker count was not cleared at {x},{y}.");
                    Assert.AreEqual(byte.MaxValue, friendlyPassFactionIds[index], $"Vehicle footprint pass id was not reset at {x},{y}.");
                }
            }

            Assert.IsTrue(blocked.IsSet(adjacentIndex), "Adjacent blocker cells must not be cleared.");
            Assert.AreEqual(3, blockerCounts[adjacentIndex]);
            Assert.AreEqual(7, friendlyPassFactionIds[adjacentIndex]);
        }
        finally
        {
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            blockerCounts.Dispose();
        }
    }

    [Test]
    public void MapVehiclePlacementDepartureClearanceRemovesPaddedBlockers()
    {
        var grid = new GridConfig
        {
            Width = 8,
            Height = 8,
            CellSize = 1f,
            Origin = float3.zero
        };
        int gridSize = grid.Width * grid.Height;
        var blockerCounts = new NativeArray<int>(gridSize, Allocator.Temp);
        var blocked = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Temp);

        try
        {
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = 7;

            int2 center = new(4, 4);
            int2 footprint = new(3, 3);
            int2 min = UnitFootprintUtility.GetMinCell(center, footprint) - new int2(1, 1);
            int2 max = min + footprint + new int2(2, 2);
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    blocked.Set(index, true);
                    blockerCounts[index] = 2;
                }
            }

            int adjacentOutsidePaddingIndex = GridUtils.CellToIndex(new int2(1, 1), grid.Width);
            blocked.Set(adjacentOutsidePaddingIndex, true);
            blockerCounts[adjacentOutsidePaddingIndex] = 3;

            var blockerData = new DynamicBlockerComponent
            {
                GridSize = gridSize,
                Counts = blockerCounts,
                Blocked = blocked,
                FriendlyPassFactionIds = friendlyPassFactionIds
            };

            int cleared = MapVehiclePlacementSpawnPrefabSystemHelper.ClearRuntimeBlockersInFootprint(
                grid,
                ref blockerData,
                center,
                footprint,
                UnitPathPlacementValidation.VehicleOccupancyPaddingCells);

            Assert.AreEqual(25, cleared, "Map-authored vehicles need a one-cell blocker-free departure pad around their footprint.");
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    Assert.IsFalse(blocked.IsSet(index), $"Vehicle departure blocker was not cleared at {x},{y}.");
                    Assert.AreEqual(0, blockerCounts[index], $"Vehicle departure blocker count was not cleared at {x},{y}.");
                    Assert.AreEqual(byte.MaxValue, friendlyPassFactionIds[index], $"Vehicle departure pass id was not reset at {x},{y}.");
                }
            }

            Assert.IsTrue(blocked.IsSet(adjacentOutsidePaddingIndex), "Blockers outside the one-cell departure pad must not be cleared.");
            Assert.AreEqual(3, blockerCounts[adjacentOutsidePaddingIndex]);
            Assert.AreEqual(7, friendlyPassFactionIds[adjacentOutsidePaddingIndex]);
        }
        finally
        {
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            blockerCounts.Dispose();
        }
    }

    [Test]
    public void VehiclePathingCanDepartFromCurrentPaddedClearanceOccupancy()
    {
        var grid = new GridConfig
        {
            Width = 10,
            Height = 5,
            CellSize = 1f,
            Origin = float3.zero
        };
        int gridSize = grid.Width * grid.Height;
        var walkable = new NativeArray<GridWalkable>(gridSize, Allocator.Temp);
        var blocked = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Temp);
        var occupied = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var liveEntities = new NativeArray<Entity>(0, Allocator.Temp);
        var liveGrids = new NativeArray<UnitGrid>(0, Allocator.Temp);
        var liveFootprints = new NativeArray<UnitFootprint>(0, Allocator.Temp);
        var liveManualGroupMembers = new NativeArray<byte>(0, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                friendlyPassFactionIds[i] = byte.MaxValue;
            }

            occupied.Set(GridUtils.CellToIndex(new int2(3, 0), grid.Width), true);

            Assert.IsTrue(
                UnitPathPlacementValidation.CanPlaceForPathing(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveEntities,
                    liveGrids,
                    liveFootprints,
                    liveManualGroupMembers,
                    Entity.Null,
                    new int2(4, 2),
                    new int2(3, 3),
                    new int2(3, 2),
                    isVehicle: true,
                    manualMove: true,
                    factionId: 1),
                "Vehicle pathing must allow departure when an occupied cell is already inside the current padded clearance but outside the actual footprint.");
        }
        finally
        {
            liveManualGroupMembers.Dispose();
            liveFootprints.Dispose();
            liveGrids.Dispose();
            liveEntities.Dispose();
            occupied.Dispose();
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void VehiclePathingCanDepartFromCurrentDynamicBlockedFootprint()
    {
        var grid = new GridConfig
        {
            Width = 10,
            Height = 5,
            CellSize = 1f,
            Origin = float3.zero
        };
        int gridSize = grid.Width * grid.Height;
        var walkable = new NativeArray<GridWalkable>(gridSize, Allocator.Temp);
        var blocked = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Temp);
        var occupied = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var liveEntities = new NativeArray<Entity>(0, Allocator.Temp);
        var liveGrids = new NativeArray<UnitGrid>(0, Allocator.Temp);
        var liveFootprints = new NativeArray<UnitFootprint>(0, Allocator.Temp);
        var liveManualGroupMembers = new NativeArray<byte>(0, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                friendlyPassFactionIds[i] = byte.MaxValue;
            }

            blocked.Set(GridUtils.CellToIndex(new int2(4, 2), grid.Width), true);

            Assert.IsTrue(
                UnitPathPlacementValidation.CanPlaceForPathing(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveEntities,
                    liveGrids,
                    liveFootprints,
                    liveManualGroupMembers,
                    Entity.Null,
                    new int2(4, 2),
                    new int2(3, 3),
                    new int2(3, 2),
                    isVehicle: true,
                    manualMove: true,
                    factionId: 1),
                "Vehicle pathing must allow departure when an authored blocker is already inside the current vehicle footprint.");
        }
        finally
        {
            liveManualGroupMembers.Dispose();
            liveFootprints.Dispose();
            liveGrids.Dispose();
            liveEntities.Dispose();
            occupied.Dispose();
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void VehiclePathingStillRejectsNewDynamicBlockedFootprintCells()
    {
        var grid = new GridConfig
        {
            Width = 10,
            Height = 5,
            CellSize = 1f,
            Origin = float3.zero
        };
        int gridSize = grid.Width * grid.Height;
        var walkable = new NativeArray<GridWalkable>(gridSize, Allocator.Temp);
        var blocked = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Temp);
        var occupied = new NativeBitArray(gridSize, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var liveEntities = new NativeArray<Entity>(0, Allocator.Temp);
        var liveGrids = new NativeArray<UnitGrid>(0, Allocator.Temp);
        var liveFootprints = new NativeArray<UnitFootprint>(0, Allocator.Temp);
        var liveManualGroupMembers = new NativeArray<byte>(0, Allocator.Temp);

        try
        {
            for (int i = 0; i < walkable.Length; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                friendlyPassFactionIds[i] = byte.MaxValue;
            }

            blocked.Set(GridUtils.CellToIndex(new int2(5, 2), grid.Width), true);

            Assert.IsFalse(
                UnitPathPlacementValidation.CanPlaceForPathing(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveEntities,
                    liveGrids,
                    liveFootprints,
                    liveManualGroupMembers,
                    Entity.Null,
                    new int2(4, 2),
                    new int2(3, 3),
                    new int2(3, 2),
                    isVehicle: true,
                    manualMove: true,
                    factionId: 1),
                "Vehicle pathing must still reject blocker cells newly entered by the next footprint.");
        }
        finally
        {
            liveManualGroupMembers.Dispose();
            liveFootprints.Dispose();
            liveGrids.Dispose();
            liveEntities.Dispose();
            occupied.Dispose();
            friendlyPassFactionIds.Dispose();
            blocked.Dispose();
            walkable.Dispose();
        }
    }

    [Test]
    public void PathRequestIgnoredOccupancyDefaultsToMovingUnitFootprint()
    {
        using var world = new World("PathRequestIgnoredOccupancyValidation");
        EntityManager em = world.EntityManager;
        Entity unit = em.CreateEntity(typeof(UnitGrid), typeof(UnitFootprint));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(7, 3) });
        em.SetComponentData(unit, new UnitFootprint { Size = new int2(5, 10) });

        UnitPathIgnoredOccupancy.ResolveIgnoredOccupancy(
            em,
            unit,
            out Entity ignoredEntity,
            out int2 ignoredCell,
            out int2 ignoredSize);

        Assert.AreEqual(unit, ignoredEntity, "Normal path requests must ignore the moving entity's own footprint.");
        Assert.AreEqual(new int2(7, 3), ignoredCell);
        Assert.AreEqual(new int2(5, 10), ignoredSize);
    }

    [Test]
    public void VehicleMovementCanDepartFromCurrentPaddedClearanceOccupancy()
    {
        using var world = new World("VehicleCurrentClearanceDepartureValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 10;
            const int height = 5;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            pathPool.Add(new int2(4, 2));
            pathPool.Add(new int2(5, 2));
            pathPool.Add(new int2(6, 2));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
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
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
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

            occupied.Set(GridUtils.CellToIndex(new int2(3, 0), width), true);

            Entity unit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(unit, new Faction { Id = 1 });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(3, 2) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(3, 3) });
            em.SetComponentData(unit, new UnitMove { Speed = 5f, WalkSpeed = 5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
            em.SetComponentData(unit, new UnitVehicleMovement { TurnSpeedDegrees = 720f, Acceleration = 20f, Braking = 20f, RearPivotOffset = 0f });
            em.SetComponentData(unit, new UnitVehicleKinematics());
            em.SetComponentData(unit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(unit, new UnitPathRange { Start = 0, Length = pathPool.Length });
            em.SetComponentData(unit, LocalTransform.FromPositionRotation(new float3(3.5f, 0f, 2.5f), quaternion.RotateY(math.radians(90f))));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            movementSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            float3 position = em.GetComponentData<LocalTransform>(unit).Position;
            Assert.Greater(
                position.x,
                3.5f,
                "A vehicle must be able to depart when an occupied cell is already inside its current padded clearance but outside its actual footprint.");
        }
        finally
        {
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    [Test]
    public void InfantryOpenPathMovementAdvancesEveryFrame()
    {
        using var world = new World("UnitMovementOpenPathContinuityValidation");
        EntityManager em = world.EntityManager;

        NativeArray<int> blockerCounts = default;
        NativeBitArray blocked = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray occupied = default;
        NativeList<int2> pathPool = default;

        try
        {
            const int width = 12;
            const int height = 1;
            int gridSize = width * height;
            blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
            blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
            for (int i = 0; i < friendlyPassFactionIds.Length; i++)
                friendlyPassFactionIds[i] = byte.MaxValue;
            occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pathPool = new NativeList<int2>(Allocator.Persistent);
            for (int x = 1; x <= 10; x++)
                pathPool.Add(new int2(x, 0));

            var grid = new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero };
            Entity gridEntity = em.CreateEntity(
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
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
            em.SetComponentData(gridEntity, new PathPoolComponent { Cells = pathPool });

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);
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

            Entity unit = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitFootprint),
                typeof(UnitMove),
                typeof(UnitMovementBehavior),
                typeof(UnitVehicleMovement),
                typeof(UnitVehicleKinematics),
                typeof(UnitPathFollow),
                typeof(UnitPathRange),
                typeof(LocalTransform));
            em.SetComponentData(unit, new Faction { Id = 0 });
            em.SetComponentData(unit, new UnitGrid { Cell = new int2(0, 0) });
            em.SetComponentData(unit, new UnitFootprint { Size = new int2(1, 1) });
            em.SetComponentData(unit, new UnitMove { Speed = 3f, WalkSpeed = 3f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.01f });
            em.SetComponentData(unit, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            em.SetComponentData(unit, new UnitVehicleMovement());
            em.SetComponentData(unit, new UnitVehicleKinematics());
            em.SetComponentData(unit, new UnitPathFollow { PathIndex = 0 });
            em.SetComponentData(unit, new UnitPathRange { Start = 0, Length = pathPool.Length });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(0.5f, 0f, 0.5f)));

            world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            SystemHandle movementSystem = world.CreateSystem<UnitGridMovementSystem>();
            float previousX = em.GetComponentData<LocalTransform>(unit).Position.x;
            for (int frame = 1; frame <= 40; frame++)
            {
                world.SetTime(new TimeData(frame / 60d, 1f / 60f));
                movementSystem.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();

                float currentX = em.GetComponentData<LocalTransform>(unit).Position.x;
                Assert.Greater(
                    currentX,
                    previousX + 0.0001f,
                    $"Infantry ECS position must advance every frame on an open path before visual animation is considered. frame={frame}");
                previousX = currentX;
            }
        }
        finally
        {
            if (pathPool.IsCreated)
                pathPool.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
        }
    }

    private static void CreateGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeBitArray blocked,
        out NativeArray<byte> friendlyPassFactionIds)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });

        em.AddBuffer<GridWalkable>(gridEntity);
        em.AddBuffer<GridRoad>(gridEntity);
        em.AddBuffer<GridRoadSidewalk>(gridEntity);
        em.AddBuffer<GridRoadDirt>(gridEntity);

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
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing private field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
