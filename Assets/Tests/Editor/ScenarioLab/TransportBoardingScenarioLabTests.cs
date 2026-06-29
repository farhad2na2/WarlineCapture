#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class TransportBoardingScenarioLabTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private NativeList<int2> _pathPool;

    public static void RunFocusedValidation()
    {
        try
        {
            RunTest(test => test.Tb001_GroundVehicleTransport_BoardsSoldierThenGroundExits());
            RunTest(test => test.Tb002_HelicopterTransport_BoardsSoldierThenStartsAndCompletesRopeExit());
            RunTest(test => test.Tb003_AirborneHelicopterPickup_CommandsLandingAndPreventsMidairBoarding());
            RunTest(test => test.Tb004_HelicopterGroundExit_CurrentlyStartsRopeFlow());
            RunTest(test => test.Tb005_TransportPlane_BoardsSoldierAtRearRampThenGroundExits());
            RunTest(test => test.Tb006_TransportPlane_AirDropsSoldierWithParachuteVisual());
            RunTest(test => test.Tb007_TransportPlane_BoardsVehicleCargoThenGroundExits());
            RunTest(test => test.Tb007Tb008_TransportPlane_BoardsVehicleCargoThenAirDropsCargoVisual());
            RunTest(test => test.Tb009_TransportPlane_MixedLoadAirDropsSoldierAndVehicleCounts());
            RunTest(test => test.Tb010_TransportBoardingRejections_UseProductionReasonCodes());
            RunTest(test => test.TransportBoardingScenarioCatalog_ContainsAllPlannedScenarioIds());
            RunTest(test => test.TransportBoardingRuntimeDispatch_RecognizesTbScenarioIds());
            RunTest(test => test.TransportBoardingVisualPlayback_RecognizesWiredVisualRequiredScenarios());
            RunTest(test => test.TransportBoardingScenarioReportJson_WritesMetricsContract());
            RunTest(test => test.TransportBoardingScenarioDefinitionPaths_CoverCatalog());
            Debug.Log("[TransportBoardingScenarioLab] result=Passed tests=15");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[TransportBoardingScenarioLab] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static void RunTest(Action<TransportBoardingScenarioLabTests> test)
    {
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            test(fixture);
        }
        finally
        {
            fixture.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
        if (_pathPool.IsCreated)
            _pathPool.Dispose();
    }

    [Test]
    public void Tb001_GroundVehicleTransport_BoardsSoldierThenGroundExits()
    {
        using var world = new World("TB-001_GroundVehicleTransport_BoardAndGroundExit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 18, 18);

        Entity transport = CreateTransport(em, new int2(8, 8), air: false, airborne: false, "Unit_Veh_APC_01");
        Entity passenger = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));

        RunBoardingSystem(world, 1d);

        AssertPassengerLoaded(em, transport, passenger, "TB-001 soldier should load into the ground transport.");

        Entity commandEntity = CreateCommandEntity(em);
        em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        RunTransportCommandSystem(world, 1.1d);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        AssertPassengerVisibleAndUnloaded(em, passenger, "TB-001 passenger should be visible and no longer attached to the transport after ground exit.");
        Assert.That(math.distance(em.GetComponentData<LocalTransform>(passenger).Position, em.GetComponentData<LocalTransform>(transport).Position), Is.GreaterThan(0.5f));
    }

    [Test]
    public void Tb002_HelicopterTransport_BoardsSoldierThenStartsAndCompletesRopeExit()
    {
        using var world = new World("TB-002_HelicopterTransport_BoardAndRopeExit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 22, 22);

        Entity transport = CreateTransport(em, new int2(10, 10), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreatePassenger(em, new int2(9, 10), transport, new int2(9, 10));

        RunBoardingSystem(world, 1d);

        AssertPassengerLoaded(em, transport, passenger, "TB-002 soldier should board only after the helicopter is landed.");

        Entity commandEntity = CreateCommandEntity(em);
        em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        RunTransportCommandSystem(world, 1.1d);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "TB-002 helicopter exit should start the production rope disembark request.");
        Assert.AreEqual(1, em.GetComponentData<UnitAirComponent>(transport).Airborne, "TB-002 helicopter should be raised/marked airborne for rope exit.");

        SystemHandle ropeSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        world.SetTime(new TimeData(1.2d, 0.1f));
        ropeSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        UnitTransportRopeDropComponent drop = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
        Assert.That(drop.StartPosition.y, Is.GreaterThan(drop.EndPosition.y));

        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        world.SetTime(new TimeData(drop.StartedAt + drop.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger), "TB-002 passenger should receive the real post-rope disperse component after touchdown.");
    }

    [Test]
    public void Tb003_AirborneHelicopterPickup_CommandsLandingAndPreventsMidairBoarding()
    {
        using var world = new World("TB-003_AirborneHelicopterPickup");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(4, 4), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.Airborne = 0;
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(4.5f, 0f, 4.5f);
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(4.5f, 8f, 4.5f)));
        Entity passenger = CreatePassenger(em, new int2(16, 16), transport, new int2(16, 16));
        Entity gridEntity = GetGridEntity(em);
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkableBuffer = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);

        using EntityQuery liveUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using NativeArray<Entity> liveEntities = liveUnitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveGrids = liveUnitQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveFootprints = liveUnitQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        var airPickupSystem = new UnitTransportAirPickupSystem();
        bool prepared = airPickupSystem.TryPrepareAirTransportPickupForBoarding(
            em,
            transport,
            grid,
            walkableBuffer.AsNativeArray(),
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied,
            em.GetComponentData<UnitGrid>(transport).Cell,
            em.GetComponentData<UnitFootprint>(transport).Size,
            new List<Entity> { passenger },
            1,
            liveEntities,
            liveGrids,
            liveFootprints,
            new UnitMoveOrderSystem(),
            out int2 pickupCell);

        Assert.IsTrue(prepared, "TB-003 airborne helicopter should command a pickup landing near the selected passenger.");
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passenger).Cell, pickupCell);
        Assert.IsTrue(em.HasComponent<UnitTarget>(transport));
        Assert.AreEqual(pickupCell, em.GetComponentData<UnitTarget>(transport).Cell);
        Assert.AreEqual(1, em.GetComponentData<UnitAirComponent>(transport).Airborne);

        RunBoardingSystem(world, 1d);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "TB-003 passenger must not board while the helicopter is still physically airborne.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void Tb004_HelicopterGroundExit_CurrentlyStartsRopeFlow()
    {
        using var world = new World("TB-004_HelicopterGroundExitBehaviorAudit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 22, 22);

        Entity transport = CreateTransport(em, new int2(10, 10), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });

        Entity commandEntity = CreateCommandEntity(em);
        em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        RunTransportCommandSystem(world, 1d);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "TB-004 documents current production behavior: helicopter exit starts rope flow even if the helicopter was initially landed.");
        Assert.AreEqual(1, em.GetComponentData<UnitAirComponent>(transport).Airborne);
        Assert.IsTrue(em.HasComponent<Disabled>(passenger), "TB-004 passenger should remain in the helicopter until the rope system releases it.");
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
    }

    [Test]
    public void Tb005_TransportPlane_BoardsSoldierAtRearRampThenGroundExits()
    {
        using var world = new World("TB-005_TransportPlane_RampBoardAndGroundExit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(14, 14));
        GridConfig grid = em.GetComponentData<GridConfig>(GetGridEntity(em));
        int2 rampCell = TransportBoardingCommandSystem.ResolvePlaneRampApproachCell(em, grid, transport);
        Entity passenger = CreatePassenger(em, rampCell, transport, rampCell);

        RunBoardingSystem(world, 1d);

        AssertPassengerLoaded(em, transport, passenger, "TB-005 soldier should load through the resolved rear-ramp boarding cell.");

        Entity commandEntity = CreateCommandEntity(em);
        em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        RunTransportCommandSystem(world, 1.1d);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport), "TB-005 plane ground exit should request the production rear door/ramp to open.");
        AssertPassengerVisibleAndUnloaded(em, passenger, "TB-005 soldier should be visible and detached after ground/ramp exit.");
    }

    [Test]
    public void Tb006_TransportPlane_AirDropsSoldierWithParachuteVisual()
    {
        using var world = new World("TB-006_TransportPlane_SoldierAirdrop");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(14, 14));
        SetTransportPlaneAirborne(em, transport, new float3(14.5f, 55f, 14.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(17, 17),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        AssertPassengerVisibleAndUnloaded(em, passenger, "TB-006 soldier should leave the transport when the airdrop releases.");
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        Assert.AreEqual(new int2(17, 17), drop.LandingCell);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
    }

    [Test]
    public void Tb007_TransportPlane_BoardsVehicleCargoThenGroundExits()
    {
        using var world = new World("TB-007_TransportPlane_VehicleCargoGroundExit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 34, 34);

        Entity transport = CreateTransportPlane(em, new int2(14, 14));
        Entity vehicle = CreateVehiclePassenger(em, new int2(15, 14), transport, new int2(15, 14));

        RunBoardingSystem(world, 1d);

        AssertPassengerLoaded(em, transport, vehicle, "TB-007 vehicle should board into the plane cargo slot before ground exit.");
        Assert.IsTrue(em.HasComponent<UnitTransportCargoPassenger>(vehicle));

        Entity commandEntity = CreateCommandEntity(em);
        em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        RunTransportCommandSystem(world, 1.1d);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        AssertPassengerVisibleAndUnloaded(em, vehicle, "TB-007 vehicle cargo should be visible and detached after ramp ground exit.");
    }

    [Test]
    public void Tb007Tb008_TransportPlane_BoardsVehicleCargoThenAirDropsCargoVisual()
    {
        using var world = new World("TB-007_TB-008_TransportPlane_VehicleCargoBoardAndAirdrop");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(14, 14));
        Entity vehicle = CreateVehiclePassenger(em, new int2(15, 14), transport, new int2(15, 14));

        RunBoardingSystem(world, 1d);

        AssertPassengerLoaded(em, transport, vehicle, "TB-007 vehicle should load into the plane cargo slot.");
        Assert.IsTrue(em.HasComponent<UnitTransportCargoPassenger>(vehicle));

        SetTransportPlaneAirborne(em, transport, new float3(14.5f, 55f, 14.5f));
        AddAirdropVisualPrefabs(em, transport);
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(18, 18),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.VehicleOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1.2d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        AssertPassengerVisibleAndUnloaded(em, vehicle, "TB-008 vehicle cargo should detach and become visible after cargo drop release.");
        Assert.IsTrue(em.HasComponent<UnitTransportCargoDropComponent>(vehicle));
        UnitTransportCargoDropComponent drop = em.GetComponentData<UnitTransportCargoDropComponent>(vehicle);
        Assert.AreEqual(new int2(18, 18), drop.LandingCell);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
    }

    [Test]
    public void Tb009_TransportPlane_MixedLoadAirDropsSoldierAndVehicleCounts()
    {
        using var world = new World("TB-009_TransportPlane_MixedLoadAirdrop");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 34, 34);

        Entity transport = CreateTransportPlane(em, new int2(14, 14));
        SetTransportPlaneAirborne(em, transport, new float3(14.5f, 55f, 14.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        passengers.Add(new UnitTransportPassengerElement { Passenger = vehicle });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(18, 18),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 2,
            SoldierDropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.Mixed,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);
        UnitTransportAirdropRequest pendingDrop = em.GetComponentData<UnitTransportAirdropRequest>(transport);
        world.SetTime(new TimeData(pendingDrop.NextDropAt + 0.01d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        AssertPassengerVisibleAndUnloaded(em, soldier, "TB-009 soldier should be released from the mixed airdrop.");
        AssertPassengerVisibleAndUnloaded(em, vehicle, "TB-009 vehicle should be released from the mixed airdrop.");
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(soldier));
        Assert.IsTrue(em.HasComponent<UnitTransportCargoDropComponent>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
    }

    [Test]
    public void Tb010_TransportBoardingRejections_UseProductionReasonCodes()
    {
        AssertHelicopterRejectsVehiclePassenger();
        AssertAirbornePlaneRejectsBoardingOrder();
        AssertFullPlaneVehicleSlotsRejectVehiclePassenger();
        AssertBlockedGroundExitReportsNoDisembarkCell();
        AssertMissingAirdropVisualRejectsWithReason();
    }

    [Test]
    public void TransportBoardingScenarioCatalog_ContainsAllPlannedScenarioIds()
    {
        string[] expectedIds =
        {
            TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId,
            TransportBoardingScenarioCatalog.Tb002HelicopterBoardRopeExitId,
            TransportBoardingScenarioCatalog.Tb003HelicopterAirPickupId,
            TransportBoardingScenarioCatalog.Tb004HelicopterGroundExitAuditId,
            TransportBoardingScenarioCatalog.Tb005PlaneRampBoardGroundExitId,
            TransportBoardingScenarioCatalog.Tb006PlaneSoldierAirdropId,
            TransportBoardingScenarioCatalog.Tb007PlaneVehicleCargoGroundExitId,
            TransportBoardingScenarioCatalog.Tb008PlaneVehicleCargoAirdropId,
            TransportBoardingScenarioCatalog.Tb009PlaneMixedLoadAirdropId,
            TransportBoardingScenarioCatalog.Tb010RejectionCasesId,
            TransportBoardingScenarioCatalog.Tb011NextCleanupId,
            TransportBoardingScenarioCatalog.Tb012CameraProofPathId
        };

        Assert.AreEqual(expectedIds.Length, TransportBoardingScenarioCatalog.All.Count);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < expectedIds.Length; i++)
        {
            Assert.IsTrue(TransportBoardingScenarioCatalog.TryGetScenario(expectedIds[i], out TransportBoardingScenarioDescriptor descriptor), expectedIds[i]);
            Assert.IsTrue(seenIds.Add(descriptor.ScenarioId), $"Duplicate transport boarding scenario id: {descriptor.ScenarioId}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.DisplayName), expectedIds[i]);
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.Description), expectedIds[i]);
            Assert.AreEqual(expectedIds[i], descriptor.ScenarioId);
        }

        Assert.IsFalse(TransportBoardingScenarioCatalog.TryGetScenario(BattleScenarioAd001Runner.ScenarioId, out _));
    }

    [Test]
    public void TransportBoardingRuntimeDispatch_RecognizesTbScenarioIds()
    {
        BattleScenarioDefinition tbDefinition = CreateEditorDefinition(TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId);
        BattleScenarioDefinition adDefinition = CreateEditorDefinition(BattleScenarioAd001Runner.ScenarioId);
        try
        {
            BattleScenarioResult result = BattleScenarioLabRuntimeRunner.RunDefinition(tbDefinition);
            Assert.AreEqual(TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId, result.ScenarioId);
            Assert.IsFalse(result.Passed, "TB runtime dispatch should not fake a pass before the production visual runner is wired.");
            Assert.AreEqual(BattleScenarioFailureReason.InvalidSetup, result.FailureReason);
            Assert.AreEqual(1, result.Variants.Length);
            Assert.AreEqual(TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId, result.Variants[0].VariantId);
            Assert.IsFalse(TransportBoardingScenarioRuntimeRunner.CanRunDefinition(adDefinition));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tbDefinition);
            UnityEngine.Object.DestroyImmediate(adDefinition);
        }
    }

    [Test]
    public void TransportBoardingVisualPlayback_RecognizesWiredVisualRequiredScenarios()
    {
        GameObject playbackObject = new("TransportBoardingVisualPlaybackTest");
        var definitions = new List<BattleScenarioDefinition>();
        try
        {
            BattleScenarioLabVisualPlayback playback = playbackObject.AddComponent<BattleScenarioLabVisualPlayback>();
            for (int i = 0; i < TransportBoardingScenarioCatalog.All.Count; i++)
            {
                TransportBoardingScenarioDescriptor descriptor = TransportBoardingScenarioCatalog.All[i];
                BattleScenarioDefinition definition = CreateEditorDefinition(descriptor.ScenarioId);
                definitions.Add(definition);

                if (descriptor.VisualProofRequired)
                {
                    Assert.IsTrue(
                        playback.CanPlay(definition),
                        $"{descriptor.ScenarioId} is marked visual-proof required and must have a playback branch.");
                }
                else
                {
                    Assert.IsFalse(
                        playback.CanPlay(definition),
                        $"{descriptor.ScenarioId} is validation-only and should not pretend to have visual playback.");
                }
            }
        }
        finally
        {
            for (int i = 0; i < definitions.Count; i++)
                UnityEngine.Object.DestroyImmediate(definitions[i]);
            UnityEngine.Object.DestroyImmediate(playbackObject);
        }
    }

    [Test]
    public void TransportBoardingScenarioReportJson_WritesMetricsContract()
    {
        var metrics = new TransportBoardingScenarioMetrics
        {
            ScenarioId = TransportBoardingScenarioCatalog.Tb001GroundVehicleBoardExitId,
            VariantId = "default",
            TransportSourceKey = "Unit_Veh_APC_01",
            PassengerSourceKeys = new[] { "Unit_Inf_Rifleman" },
            BoardCommandAccepted = true,
            BoardingStarted = true,
            BoardingCompleted = true,
            BoardTimeSeconds = 1.25f,
            PassengerHiddenAfterBoard = true,
            TransportPassengerCount = 1,
            ExitCommandAccepted = true,
            ExitStarted = true,
            ExitCompleted = true,
            ExitTimeSeconds = 2.5f,
            PassengerVisibleAfterExit = true,
            HasPassengerFinalCell = true,
            PassengerFinalCellX = 9,
            PassengerFinalCellY = 8,
            DropVisualEntityCreated = false,
            DropVisualCleaned = false,
            ReasonCode = 0,
            FailureReason = BattleScenarioFailureReason.None,
            VisualProofPath = "Design/VisualLockLayered/_TransportBoardingScenarioLab/TB-001.png"
        };

        string json = TransportBoardingScenarioReportJson.ToJson(new TransportBoardingScenarioReport
        {
            GeneratedAtUtc = "2026-06-28T00:00:00.0000000Z",
            Metrics = new[] { metrics },
            Passed = true
        });

        Assert.That(json, Does.Contain("\"ScenarioId\": \"TB-001_GroundVehicleTransport_BoardAndGroundExit\""));
        Assert.That(json, Does.Contain("\"PassengerSourceKeys\""));
        Assert.That(json, Does.Contain("\"BoardingCompleted\": true"));
        Assert.That(json, Does.Contain("\"PassengerFinalCellX\": 9"));
        Assert.That(json, Does.Contain("\"FailureReason\": \"None\""));
        Assert.That(json, Does.Contain("\"Passed\": true"));
    }

    [Test]
    public void TransportBoardingScenarioDefinitionPaths_CoverCatalog()
    {
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < TransportBoardingScenarioCatalog.All.Count; i++)
        {
            TransportBoardingScenarioDescriptor descriptor = TransportBoardingScenarioCatalog.All[i];
            string path = BattleScenarioLabValidationRunner.GetTransportBoardingDefinitionPath(descriptor);
            Assert.That(path, Does.StartWith(BattleScenarioLabValidationRunner.TransportBoardingDefinitionFolder + "/"));
            Assert.That(path, Does.EndWith(".asset"));
            Assert.That(path, Does.Contain(descriptor.ScenarioId));
            Assert.IsTrue(seenPaths.Add(path), $"Duplicate transport boarding definition path: {path}");
        }
    }

    private static void RunBoardingSystem(World world, double timeSeconds)
    {
        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(timeSeconds, 0.1f));
        boardingSystem.Update(world.Unmanaged);
    }

    private static void RunTransportCommandSystem(World world, double timeSeconds)
    {
        SystemHandle commandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(timeSeconds, 0.1f));
        commandSystem.Update(world.Unmanaged);
    }

    private static void AssertPassengerLoaded(EntityManager em, Entity transport, Entity passenger, string message)
    {
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, message);
        Assert.AreEqual(passenger, passengers[0].Passenger);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger), message);
        Assert.IsTrue(em.HasComponent<Disabled>(passenger), message);
    }

    private static void AssertPassengerVisibleAndUnloaded(EntityManager em, Entity passenger, string message)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(passenger), message);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger), message);
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passenger), message);
    }

    private static Entity CreateCommandEntity(EntityManager em)
    {
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        return commandEntity;
    }

    private Entity GetGridEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        return query.GetSingletonEntity();
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        for (int i = 0; i < _friendlyPassFactionIds.Length; i++)
            _friendlyPassFactionIds[i] = byte.MaxValue;
        _pathPool = new NativeList<int2>(1024, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(
            typeof(GridConfig),
            typeof(DynamicBlockerComponent),
            typeof(DynamicOccupancyComponent),
            typeof(PathPoolComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });
        em.SetComponentData(gridEntity, new PathPoolComponent { Cells = _pathPool });

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

    private void BlockAllCells()
    {
        for (int i = 0; i < _blocked.Length; i++)
            _blocked.Set(i, true);
    }

    private static Entity CreateTransport(EntityManager em, int2 cell, bool air, bool airborne, string sourcePrefabKey)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(UnitSourcePrefabKey),
            typeof(LocalToWorld),
            typeof(LocalTransform));
        float3 position = new(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f);
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 10 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourcePrefabKey) });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);

        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            em.AddComponentData(entity, new UnitAirComponent
            {
                HomePosition = new float3(cell.x + 0.5f, 0f, cell.y + 0.5f),
                HomeCell = cell,
                HomeInitialized = 1,
                Airborne = (byte)(airborne ? 1 : 0)
            });
        }

        return entity;
    }

    private static Entity CreateTransportPlane(EntityManager em, int2 cell)
    {
        Entity entity = CreateTransport(em, cell, air: true, airborne: false, "Unit_Veh_Plane_Transport");
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 24 });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(7, 7) });
        em.AddComponentData(entity, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 24,
            VehicleCapacity = 2,
            CargoWeightCapacity = 0
        });
        em.AddComponentData(entity, new UnitTransportPlaneDoorReference
        {
            DoorEntity = Entity.Null,
            ClosedLocalRotation = quaternion.identity,
            OpenLocalRotation = quaternion.identity,
            OpenSeconds = 1.1f,
            CloseSeconds = 0.9f,
            DoorLocalPosition = new float3(0f, 0f, -4f),
            InteriorLocalPosition = new float3(0f, 1.45f, 4f),
            ApproachLocalPosition = new float3(0f, 0f, -5f),
            RolloutLocalPosition = new float3(0f, 0f, -5f)
        });
        em.AddComponentData(entity, new UnitTransportPlaneDoorState());
        return entity;
    }

    private static void SetTransportPlaneAirborne(EntityManager em, Entity transport, float3 position)
    {
        em.SetComponentData(transport, LocalTransform.FromPosition(position));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(position) });
        em.SetComponentData(transport, new UnitGrid { Cell = new int2((int)math.floor(position.x), (int)math.floor(position.z)) });
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.Airborne = 1;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.ReturningHome = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        em.SetComponentData(transport, airState);
    }

    private static Entity CreatePassenger(EntityManager em, int2 cell, Entity transport, int2 boardingGoal)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitTransportBoardingTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitTransportBoardingTarget { Transport = transport, Goal = boardingGoal });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateVehiclePassenger(EntityManager em, int2 cell, Entity transport, int2 boardingGoal)
    {
        Entity entity = CreatePassenger(em, cell, transport, boardingGoal);
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitTransportBoardingTarget
        {
            Transport = transport,
            Goal = boardingGoal,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        return entity;
    }

    private static Entity CreateSelectablePassenger(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(SelectedUnitTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static Entity CreateSelectableVehiclePassenger(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitSourcePrefabKey),
            typeof(SelectedUnitTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static Entity CreateLoadedPassenger(EntityManager em, Entity transport)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitMoveVisualComponent),
            typeof(UnitTransportPassenger),
            typeof(LocalTransform),
            typeof(Disabled));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, new UnitTransportPassenger { Transport = transport });
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateLoadedVehiclePassenger(EntityManager em, Entity transport)
    {
        Entity entity = CreateLoadedPassenger(em, transport);
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.AddComponentData(entity, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        return entity;
    }

    private static Entity CreateAirdropVisualPrefab(EntityManager em, string name)
    {
        Entity entity = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetName(entity, name);
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        return entity;
    }

    private static void AddAirdropVisualPrefabs(EntityManager em, Entity transport)
    {
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ScenarioLabParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "ScenarioLabCargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
    }

    private static void AssertHelicopterRejectsVehiclePassenger()
    {
        using var world = new World("TB-010_HelicopterRejectsVehiclePassenger");
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            EntityManager em = world.EntityManager;
            fixture.CreateGrid(em, 30, 30);

            Entity transport = CreateTransport(em, new int2(12, 12), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
            Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));

            TransportBoardingCommandSystem.Result result = new TransportBoardingCommandSystem().TryIssueBoardTransportOrderToTransport(
                em,
                transport,
                new UnitTransportAirPickupSystem(),
                new UnitMoveOrderSystem(),
                null);

            Assert.IsFalse(result.Accepted, "TB-010 helicopter should reject vehicle cargo passengers through the production board command path.");
            Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(vehicle));
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static void AssertAirbornePlaneRejectsBoardingOrder()
    {
        using var world = new World("TB-010_AirbornePlaneRejectsBoarding");
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            EntityManager em = world.EntityManager;
            fixture.CreateGrid(em, 30, 30);

            Entity transport = CreateTransportPlane(em, new int2(12, 12));
            SetTransportPlaneAirborne(em, transport, new float3(12.5f, 8f, 12.5f));
            Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));

            TransportBoardingCommandSystem.Result result = new TransportBoardingCommandSystem().TryIssueBoardTransportOrderToTransport(
                em,
                transport,
                new UnitTransportAirPickupSystem(),
                new UnitMoveOrderSystem(),
                null);

            Assert.IsFalse(result.Accepted, "TB-010 airborne plane should reject boarding orders until it is landed.");
            Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passenger));
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static void AssertFullPlaneVehicleSlotsRejectVehiclePassenger()
    {
        using var world = new World("TB-010_FullPlaneVehicleSlots");
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            EntityManager em = world.EntityManager;
            fixture.CreateGrid(em, 30, 30);

            Entity transport = CreateTransportPlane(em, new int2(12, 12));
            Entity loadedA = CreateLoadedVehiclePassenger(em, transport);
            Entity loadedB = CreateLoadedVehiclePassenger(em, transport);
            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            passengers.Add(new UnitTransportPassengerElement { Passenger = loadedA });
            passengers.Add(new UnitTransportPassengerElement { Passenger = loadedB });
            Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
            Entity commandEntity = CreateCommandEntity(em);
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
                TargetEntity = transport,
                SecondaryTargetEntity = vehicle,
                HasTargetEntity = 1,
                HasSecondaryTargetEntity = 1
            });

            RunTransportCommandSystem(world, 1d);

            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(0, results[0].Accepted);
            Assert.AreEqual((int)TacticalCommandReasonCode.TransportFull, results[0].ReasonCode);
            Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(vehicle));
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static void AssertBlockedGroundExitReportsNoDisembarkCell()
    {
        using var world = new World("TB-010_BlockedGroundExit");
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            EntityManager em = world.EntityManager;
            fixture.CreateGrid(em, 18, 18);
            fixture.BlockAllCells();

            Entity transport = CreateTransport(em, new int2(8, 8), air: false, airborne: false, "Unit_Veh_APC_01");
            Entity passenger = CreateLoadedPassenger(em, transport);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
            Entity commandEntity = CreateCommandEntity(em);
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                HasTargetEntity = 1
            });

            RunTransportCommandSystem(world, 1d);

            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(0, results[0].Accepted);
            Assert.AreEqual((int)TacticalCommandReasonCode.NoDisembarkCell, results[0].ReasonCode);
            Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
            Assert.IsTrue(em.HasComponent<Disabled>(passenger));
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static void AssertMissingAirdropVisualRejectsWithReason()
    {
        using var world = new World("TB-010_MissingAirdropVisual");
        var fixture = new TransportBoardingScenarioLabTests();
        try
        {
            EntityManager em = world.EntityManager;
            fixture.CreateGrid(em, 30, 30);

            Entity transport = CreateTransportPlane(em, new int2(12, 12));
            SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
            Entity passenger = CreateLoadedPassenger(em, transport);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
            Entity commandEntity = CreateCommandEntity(em);
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                HasTargetEntity = 1
            });

            RunTransportCommandSystem(world, 1d);

            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(0, results[0].Accepted);
            Assert.AreEqual((int)TacticalCommandReasonCode.CommandUnavailable, results[0].ReasonCode);
            Assert.AreEqual("Parachute visual missing.", results[0].Message.ToString());
            Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
            Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        }
        finally
        {
            fixture.TearDown();
        }
    }

    private static BattleScenarioDefinition CreateEditorDefinition(string scenarioId)
    {
        BattleScenarioDefinition definition = ScriptableObject.CreateInstance<BattleScenarioDefinition>();
        SerializedObject serialized = new(definition);
        serialized.FindProperty("scenarioId").stringValue = scenarioId;
        serialized.FindProperty("displayName").stringValue = scenarioId;
        serialized.FindProperty("description").stringValue = "Transport boarding Scenario Lab test definition.";
        serialized.FindProperty("scenarioVariants").arraySize = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return definition;
    }
}
#endif
