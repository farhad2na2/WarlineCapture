using Game.Tactical.Contracts;
using Game.Components;

#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Runtime;

public sealed class HoldStopScanCommandPlayModeTests
{
    private World _previousWorld;
    private World _world;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("HoldStopScanCommandPlayModeTests");
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
            _world.Dispose();

        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world = null;
        _previousWorld = null;
    }

    [Test]
    public void HoldCommand_PlayModeAnchorsSelectedGroundUnitAndClearsOrders()
    {
        EntityManager em = _world.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity target = em.CreateEntity();
        Entity unit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(EngageTarget),
            typeof(UnitTarget),
            typeof(UnitPathRequest));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 3) });
        em.SetComponentData(unit, new UnitMove { Speed = 5f, WalkSpeed = 4f, ArriveDistance = 0.1f });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.SetComponentData(unit, new EngageTarget { Target = target, Cell = new int2(4, 5), IsCommanded = 1 });
        em.SetComponentData(unit, new UnitTarget { Cell = new int2(6, 7) });
        em.SetComponentData(unit, new UnitPathRequest { Goal = new int2(8, 9) });

        var inputSystem = new RtsSelectionInputCompositionSystemHelper(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager);
        inputSystem.ArmCommandMode(TacticalCommandMode.Attack, frame: 10, oneShot: true, requiresWorldTarget: true);
        inputSystem.QueueMoveOrder(new Vector2(10f, 20f), executeFrame: 11);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(11f, 22f), frame: 12));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.HoldPosition, frame: 13));

        bool processed = RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(
            em,
            Entity.Null,
            out RtsSelectionCommandIntentKind processedKind,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason,
            out int issuedCount);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.AreEqual(RtsSelectionCommandIntentKind.HoldPosition, processedKind);
        Assert.IsTrue(accepted);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.AreEqual(1, issuedCount);
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(unit));
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsFalse(em.HasComponent<EngageTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitTarget>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(1, em.GetComponentData<UnitCombat>(unit).AutoEngage);
        Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        AssertNoQueuedCommandIntents(inputSystem);
    }

    [Test]
    public void StopCommand_PlayModeStopsMixedVehicleAndAirSelection()
    {
        EntityManager em = _world.EntityManager;
        Entity runtimeStateEntity = CreateRuntimeGameplayState(em, selectionModeActive: true);
        Entity vehicle = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitVehicleKinematics),
            typeof(HoldPositionOrderTag),
            typeof(UnitTarget),
            typeof(UnitPathRequest));
        em.SetComponentData(vehicle, new UnitGrid { Cell = new int2(3, 4) });
        em.SetComponentData(vehicle, new UnitMove { Speed = 7f, WalkSpeed = 5f, ArriveDistance = 0.1f });
        em.SetComponentData(vehicle, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(vehicle, new UnitVehicleKinematics { CurrentSpeed = 4.5f, StallSeconds = 2f });
        em.SetComponentData(vehicle, new UnitTarget { Cell = new int2(5, 6) });
        em.SetComponentData(vehicle, new UnitPathRequest { Goal = new int2(7, 8) });

        Entity airUnit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitAirMovement),
            typeof(UnitAirComponent),
            typeof(UnitTarget),
            typeof(UnitPathRequest));
        em.SetComponentData(airUnit, new UnitGrid { Cell = new int2(10, 11) });
        em.SetComponentData(airUnit, new UnitMove { Speed = 14f, WalkSpeed = 14f, ArriveDistance = 0.1f });
        em.SetComponentData(airUnit, new UnitAirMovement { CruiseHeight = 14f, RunwayTaxiSpeed = 5f });
        em.SetComponentData(airUnit, new UnitAirComponent
        {
            HomeInitialized = 1,
            HomeCell = new int2(12, 13),
            HomePosition = new float3(12f, 0f, 13f),
            Airborne = 1,
            UsesRunway = 1,
            ReturningHome = 1,
            TakeoffRolling = 1,
            LandingRolling = 1,
            AttackRunActive = 1,
            ReturnApproachInitialized = 1,
            RunwayTakeoffCell = new int2(14, 15),
            RunwayLandingCell = new int2(16, 17)
        });
        em.SetComponentData(airUnit, new UnitTarget { Cell = new int2(18, 19) });
        em.SetComponentData(airUnit, new UnitPathRequest { Goal = new int2(20, 21) });

        var inputSystem = new RtsSelectionInputCompositionSystemHelper(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager);
        inputSystem.ArmCommandMode(TacticalCommandMode.Scan, frame: 20, oneShot: true, requiresWorldTarget: true);
        inputSystem.QueueMoveOrder(new Vector2(12f, 24f), executeFrame: 21);
        Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new Vector2(13f, 26f), frame: 22));
        Assert.IsTrue(inputSystem.QueueCommandIntentRequest(RtsSelectionCommandIntentKind.Stop, frame: 23));

        bool processed = RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(
            em,
            Entity.Null,
            out RtsSelectionCommandIntentKind processedKind,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason,
            out int issuedCount);

        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(processed);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Stop, processedKind);
        Assert.IsTrue(accepted);
        Assert.AreEqual(TacticalCommandReasonCode.None, rejectionReason);
        Assert.AreEqual(2, issuedCount);
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(vehicle));
        Assert.IsTrue(em.HasComponent<SelectedUnitTag>(airUnit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(vehicle));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(airUnit));
        Assert.IsFalse(em.HasComponent<HoldPositionOrderTag>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTarget>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTarget>(airUnit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(airUnit));
        Assert.AreEqual(0, em.GetComponentData<UnitCombat>(vehicle).AutoEngage);
        UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(vehicle);
        Assert.AreEqual(0f, kinematics.CurrentSpeed, 0.0001f);
        Assert.AreEqual(0f, kinematics.StallSeconds, 0.0001f);

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(airUnit);
        Assert.AreEqual(1, airState.HomeInitialized);
        Assert.AreEqual(1, airState.Airborne);
        Assert.AreEqual(1, airState.UsesRunway);
        Assert.AreEqual(0, airState.ReturningHome);
        Assert.AreEqual(0, airState.TakeoffRolling);
        Assert.AreEqual(0, airState.LandingRolling);
        Assert.AreEqual(0, airState.AttackRunActive);
        Assert.AreEqual(0, airState.ReturnApproachInitialized);
        Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
        Assert.IsFalse(inputSystem.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        AssertNoQueuedCommandIntents(inputSystem);
    }

    [Test]
    public void ScanCommand_PlayModeQueuesSelectedScannerOrder()
    {
        EntityManager em = _world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });
        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(2, 2));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 78,
            Frame = 89,
            TargetCell = new int2(20, 20),
            WorldPosition = new float3(20.5f, 0f, 20.5f),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasTargetCell = 1,
            HasWorldPosition = 1
        });

        SystemHandle system = _world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(_world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(78, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasSourceEntity);
        Assert.AreEqual(scanner, results[0].SourceEntity);
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        Assert.IsTrue(em.HasComponent<UnitTarget>(scanner));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(scanner));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(scanner));
        Assert.AreEqual(new int2(20, 20), em.GetComponentData<UnitTarget>(scanner).Cell);
    }

    private static Entity CreateRuntimeGameplayState(EntityManager em, bool selectionModeActive)
    {
        Entity entity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(entity, new RuntimeGameplayStateComponent
        {
            PlayRequested = 1,
            SelectionModeActive = (byte)(selectionModeActive ? 1 : 0)
        });
        return entity;
    }

    private static Entity CreateSelectedScanCapableUnit(EntityManager em, int2 cell)
    {
        Entity unit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitHealth),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(unit, new UnitGrid { Cell = cell });
        em.SetComponentData(unit, new UnitMove { Speed = 8f, WalkSpeed = 8f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Drone_Recon") });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return unit;
    }

    private static void AssertNoQueuedCommandIntents(RtsSelectionInputCompositionSystemHelper inputSystem)
    {
        Assert.IsTrue(inputSystem.TryGetCommandBuffers(
            out _,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            out _));
        Assert.AreEqual(0, requests.Length);
    }
}
#endif
