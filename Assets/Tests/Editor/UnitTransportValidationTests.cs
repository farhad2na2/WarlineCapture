using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;
using Game.Composition;

public sealed class UnitTransportValidationTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;
    private NativeList<int2> _pathPool;

    public static void RunBatchValidation()
    {
        try
        {
            RunTest(test => test.GroundPersonnelTransport_BoardsSoldierLikeApc());
            RunTest(test => test.TransportPlaneCapacity_RecognizesPlaneSourceNames());
            RunTest(test => test.TransportPlaneCapacity_AddsCargoCapacityAndPassengerBuffer());
            RunTest(test => test.TransportPlaneCapacity_PreservesAuthoredCargoCapacity());
            RunTest(test => test.TransportPlaneConfig_ContainsCargoCapacityAndAirdropVisualSources());
            RunTest(test => test.TransportPlanePrefab_UsesConfiguredAirdropVisualPrefabAssets());
            RunTest(test => test.TransportPlaneSelectionMetadata_ResolvesPortraitAndSelectionReferences());
            RunTest(test => test.TransportPlaneFixedWingClassifier_TreatsTransportPlaneAsRunwayAircraft());
            RunTest(test => test.TransportPlaneDoorSystem_InterpolatesBakedDoorRotation());
            RunTest(test => test.TransportPlaneBoardingCommand_AllowsSelectedVehiclePassenger());
            RunTest(test => test.TransportPlaneBoardingCommand_UsesRearRampApproachForSoldierPassenger());
            RunTest(test => test.TransportPlaneBoardingCommand_QueuesPassengerMoveToBoardingGoal());
            RunTest(test => test.TransportPlaneBoardingCommand_SelectedFarSoldierUsesRampWithoutNearbyRequirement());
            RunTest(test => test.TransportPlaneBoardingCommand_NoDoorMetadataStillBoardsSelectedFarSoldier());
            RunTest(test => test.TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockSelectedPassengerGoal());
            RunTest(test => test.TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockPathfindingToRamp());
            RunTest(test => test.TransportPlaneBoardingCommand_RetryReplacesStalePendingOrdersWithoutCountingThemAsSeats());
            RunTest(test => test.TransportPlaneBoardingCommand_RejectsAirbornePlanePickupBoarding());
            RunTest(test => test.TransportPlaneBoardingSystem_BoardsVehicleIntoCargoSlot());
            RunTest(test => test.TransportPlaneBoardingCommand_LoadingCargoMessageForVehiclePassenger());
            RunTest(test => test.TransportPlaneBoardingCommand_FullVehicleSlotsReportsTransportFull());
            RunTest(test => test.TransportPlaneBoardPreview_OnlyCargoPlaneAcceptsVehiclePassenger());
            RunTest(test => test.HelicopterBoardingCommand_RejectsVehiclePassenger());
            RunTest(test => test.TransportPlaneDisembarkCommand_UsesRearRampForLoadedVehicle());
            RunTest(test => test.TransportPlaneDisembarkCommand_LandedWithoutDoorMetadataUsesGroundExit());
            RunTest(test => test.TransportPlaneDisembarkCommand_BlockedRampReportsNoDisembarkCell());
            RunTest(test => test.TransportPlaneDoorOpenRequest_OpensThenExpires());
            RunTest(test => test.TransportPlaneDisembarkCommand_AirborneStartsAirdropRequest());
            RunTest(test => test.TransportPlaneDisembarkCommand_AirdropResultReportsInProgress());
            RunTest(test => test.TransportPlaneDisembarkCommand_AirdropInProgressActuallyDropsPassenger());
            RunTest(test => test.TransportPlaneDisembarkCommand_RegistryVisualStartsAirdropRequest());
            RunTest(test => test.TransportPlaneDisembarkCommand_MissingAirdropVisualRejectsWithReason());
            RunTest(test => test.TransportPlaneDisembarkCommand_NoLandingCellRejectsWithReason());
            RunTest(test => test.TransportPlaneDisembarkCommand_BlockedAirdropReportsCargoDropBlocked());
            RunTest(test => test.TransportPlaneDisembarkCommand_LandedTargetCellStartsAirdropRequest());
            RunTest(test => test.TransportPlaneAirdropPass_WaitsForFixedWingPassBeforeReleaseAndReturnsHome());
            RunTest(test => test.TransportPlaneAirdropPass_DoesNotRequireUnitAttackComponent());
            RunTest(test => test.TransportPlaneAirdropPass_PhysicallyAirborneWithStaleGroundedFlagDropsPassengers());
            RunTest(test => test.TransportPlaneAirdropSystem_ReleasesSoldierWithParachuteVisualAndRestoresOnTouchdown());
            RunTest(test => test.TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromSourceKeyBeforeDrop());
            RunTest(test => test.TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromRegistryBeforeDrop());
            RunTest(test => test.TransportPlaneAirdropSystem_ReleasesVehicleCargoWithEmergencyDropVisual());
            RunTest(test => test.TransportPlaneAirdropSystem_KeepsDoorOpenBrieflyAfterFinalDropRelease());
            RunTest(test => test.TransportPlaneAirdropSystem_ParachuteVisualTracksSoldierDuringDescent());
            RunTest(test => test.TransportPlaneAirdropSystem_CargoVisualTracksVehicleDuringDescent());
            RunTest(test => test.TransportPlaneAirdropSystem_SoldierSettlesAfterTouchdown());
            RunTest(test => test.TransportPlaneAirdropSystem_VehicleRollsOutAfterCargoTouchdown());
            RunTest(test => test.LoadedTransportAttackOrder_CreatesDeployOrderInsteadOfEngageTarget());
            RunTest(test => test.LoadedTransportAttackOrder_RejectsTransportWithoutAttackPayload());
            RunTest(test => test.LoadedTransportAttackOrder_AcceptsNestedAttackPayloadCarrier());
            RunTest(test => test.TransportDeployOrderSystem_CargoPlaneStartsAirdropNearBlockedTarget());
            RunTest(test => test.TransportDeployOrderSystem_AttackDeployAirdropsOnlyAttackPayload());
            RunTest(test => test.TransportDeployOrderSystem_AttackDeployHelicopterRopesOnlyAttackPayload());
            RunTest(test => test.TransportDeployAttackSystem_WaitsUntilPassengerSettledBeforeEngage());
            RunTest(test => test.TransportDeployAttackSystem_NestedCarrierReceivesDeployOrderForArmedPayload());
            RunTest(test => test.InitialUnitSpawnApplySystem_CopiesTransportAirdropVisualPrefabRefs());
            RunTest(test => test.TransportPlanePureEcs_StaticGuardRejectsManagedRuntimeBridgePatterns());
            RunTest(test => test.GroundPersonnelTransport_BoardOrderCapsAtAvailableSeats());
            RunTest(test => test.BoardTransportCommandSystem_OnUpdateConsumesPreResolvedTransportRequest());
            RunTest(test => test.BoardTransportCommandSystem_EmptyQueueSteadyStateAllocatesNoManagedMemory());
            RunTest(test => test.BoardAllSelectedTransportCommand_ConsumesRequestAndOrdersNearestSoldiers());
            RunTest(test => test.BoardAllSelectedTransportCommand_RetryRefreshesPendingPassengersForSameTransport());
            RunTest(test => test.BoardAllSelectedTransportCommand_IgnoresDistantPassengers());
            RunTest(test => test.AirTransport_DoesNotBoardSoldierUntilLanded());
            RunTest(test => test.AirTransport_BoardsWhenLandedOnRaisedHelipad());
            RunTest(test => test.TransportBoardingSystem_DuplicatePassengerEntriesConsumeCapacityPerEntry());
            RunTest(test => test.TransportBoardingSystem_IgnoresMissingPassengerEntitiesWhenCountingOccupancy());
            RunTest(test => test.TransportBoardingSystem_VehicleRequiresCargoCapacity());
            RunTest(test => test.TransportBoardingSystem_CargoSoldierCapacityOverridesLegacyCapacity());
            RunTest(test => test.TransportBoardingSystem_BoardingTargetMetadataClassifiesVehicleOccupancy());
            RunTest(test => test.TransportBoardingSystem_MultipleGridConfigsRejectAmbiguousSingleton());
            RunTest(test => test.AirTransport_DoesNotBoardAtOldWideClearanceDistance());
            RunTest(test => test.AirTransport_DoesNotBoardWhenStoppedOneCellShortOfCloseGoal());
            RunTest(test => test.AirTransport_DoesNotBoardAtFarEdgeOfLargeHelicopterFootprint());
            RunTest(test => test.AirTransportPickup_ClickingFlyingHelicopterCommandsLandingNearPassengerBeforeBoarding());
            RunTest(test => test.AirTransportPickup_FindingLandingCellDoesNotInvalidateGridArrays());
            RunTest(test => test.AirTransport_DoesNotBoardWhenAirFlagsGroundedButModelStillFlying());
            RunTest(test => test.AirTransport_BoardsAllPassengersThatReachedCloseHelicopterGoals());
            RunTest(test => test.TransportPlane_BoardsPassengerBesideRearRampGoalWithStaleMovementState());
            RunTest(test => test.TransportPlane_BoardsClusterAroundResolvedRearRampWithStaleMovementState());
            RunTest(test => test.Transport_DoesNotBoardPassengerThatOnlyReachedFarBoardingGoal());
            RunTest(test => test.HelicopterRopeDisembark_ReleasesPassengersOneAtATime());
            RunTest(test => test.HelicopterRopeDisembark_DropsStraightDownOntoFreeLandingCell());
            RunTest(test => test.HelicopterRopeDisembark_TenPassengersDisperseToDistinctFreeCells());
            RunTest(test => test.FocusedTransportExitButton_StartsRopeDisembarkWithoutLosingPassenger());
            RunTest(test => test.SelectionFallback_FindsNearbyTransportHelicopterWhenHelipadCellWasClicked());
            RunTest(test => test.FocusedTransportReadModel_PublishesPassengerCapacityAndRows());
            RunTest(test => test.FocusedTransportReadModel_PublishesPlaneCargoCapacityBreakdown());
            Debug.Log("[UnitTransportValidation] result=Passed tests=88");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitTransportValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunBoardMoveValidation()
    {
        try
        {
            RunTest(test => test.TransportPlaneBoardingCommand_QueuesPassengerMoveToBoardingGoal());
            RunTest(test => test.BoardTransportCommandSystem_OnUpdateConsumesPreResolvedTransportRequest());
            RunTest(test => test.BoardTransportCommandFlush_ConsumesPreResolvedTransportRequestAndOrdersMove());
            Debug.Log("[UnitTransportBoardMoveValidation] result=Passed tests=3");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitTransportBoardMoveValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunBoardingArchitectureCharacterizationValidation()
    {
        try
        {
            RunTest(test => test.TransportBoardingSystem_DuplicatePassengerEntriesConsumeCapacityPerEntry());
            RunTest(test => test.TransportBoardingSystem_IgnoresMissingPassengerEntitiesWhenCountingOccupancy());
            RunTest(test => test.TransportBoardingSystem_VehicleRequiresCargoCapacity());
            RunTest(test => test.TransportBoardingSystem_CargoSoldierCapacityOverridesLegacyCapacity());
            RunTest(test => test.TransportBoardingSystem_BoardingTargetMetadataClassifiesVehicleOccupancy());
            RunTest(test => test.TransportBoardingSystem_MultipleGridConfigsRejectAmbiguousSingleton());
            Debug.Log("[TransportBoardingArchitectureCharacterization] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[TransportBoardingArchitectureCharacterization] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static void RunTest(Action<UnitTransportValidationTests> test)
    {
        var fixture = new UnitTransportValidationTests();
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
    public void GroundPersonnelTransport_BoardsSoldierLikeApc()
    {
        using var world = new World("GroundPersonnelTransport_BoardsSoldierLikeApc");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity passenger = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(passenger, passengers[0].Passenger);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void TransportPlaneCapacity_RecognizesPlaneSourceNames()
    {
        var capacitySystem = new UnitTransportCapacitySystem();

        Assert.IsTrue(capacitySystem.IsTransportPlaneName("Unit_Veh_Plane_Transport"));
        Assert.IsTrue(capacitySystem.IsTransportPlaneName("SM_Veh_TransportPlane_01"));
        Assert.IsFalse(capacitySystem.IsTransportPlaneName("Unit_Veh_Jet_Fighter"));
    }

    [Test]
    public void TransportPlaneCapacity_AddsCargoCapacityAndPassengerBuffer()
    {
        using var world = new World("TransportPlaneCapacity_AddsCargoCapacityAndPassengerBuffer");
        EntityManager em = world.EntityManager;
        Entity transport = em.CreateEntity(typeof(UnitSourcePrefabKey));
        em.SetComponentData(transport, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });

        var capacitySystem = new UnitTransportCapacitySystem();

        Assert.IsTrue(capacitySystem.TryEnsureTransportCapacity(em, transport));
        Assert.AreEqual(24, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        Assert.IsTrue(em.HasComponent<UnitTransportCargoCapacity>(transport));
        UnitTransportCargoCapacity cargoCapacity = em.GetComponentData<UnitTransportCargoCapacity>(transport);
        Assert.AreEqual(24, cargoCapacity.SoldierCapacity);
        Assert.AreEqual(2, cargoCapacity.VehicleCapacity);
        Assert.AreEqual(0, cargoCapacity.CargoWeightCapacity);
        Assert.IsTrue(em.HasBuffer<UnitTransportPassengerElement>(transport));
    }

    [Test]
    public void TransportPlaneCapacity_PreservesAuthoredCargoCapacity()
    {
        using var world = new World("TransportPlaneCapacity_PreservesAuthoredCargoCapacity");
        EntityManager em = world.EntityManager;
        Entity transport = em.CreateEntity(typeof(UnitTransportCapacity), typeof(UnitTransportCargoCapacity));
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 12 });
        em.SetComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 24,
            VehicleCapacity = 3,
            CargoWeightCapacity = 40
        });

        UnitTransportCargoCapacity capacity = new UnitTransportCapacitySystem().ResolveTransportCargoCapacity(em, transport);

        Assert.AreEqual(24, capacity.SoldierCapacity);
        Assert.AreEqual(3, capacity.VehicleCapacity);
        Assert.AreEqual(40, capacity.CargoWeightCapacity);
    }

    [Test]
    public void TransportPlaneConfig_ContainsCargoCapacityAndAirdropVisualSources()
    {
        const string ConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Plane_Transport_Config.asset";
        const string ParachutePath = "Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab";
        const string EmergencyDropPath = "Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_01.prefab";

        UnitGridAuthoringConfig config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(ConfigPath);

        Assert.IsNotNull(config);
        Assert.AreEqual(24, config.SoldierTransportCapacity);
        Assert.AreEqual(2, config.VehicleTransportCapacity);
        Assert.AreEqual(0, config.CargoWeightCapacity);
        Assert.AreEqual(55f, config.TransportCruiseHeight);
        Assert.IsNotNull(config.SoldierParachuteVisualPrefab);
        Assert.IsNotNull(config.VehicleEmergencyDropVisualPrefab);
        AssertValidPrefabAssetReference(config.SoldierParachuteVisualPrefab, ParachutePath);
        AssertValidPrefabAssetReference(config.VehicleEmergencyDropVisualPrefab, EmergencyDropPath);
        Assert.IsNotNull(config.PortraitSprite);
        Assert.IsNotNull(config.PortraitCardSprite);
        Assert.IsNotNull(config.PortraitActionSprite);
        Assert.IsNotNull(config.UnitSelectionMarkerPrefab);
        Assert.IsNotNull(config.VehicleSelectionMarkerPrefab);
        Assert.IsNotNull(config.UnitHealthBarPrefab);
        Assert.IsNotNull(config.VehicleHealthBarPrefab);
    }

    [Test]
    public void TransportPlanePrefab_UsesConfiguredAirdropVisualPrefabAssets()
    {
        const string PrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";
        const string ParachutePath = "Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab";
        const string EmergencyDropPath = "Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_01.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.IsNotNull(prefab);
        Assert.IsTrue(prefab.TryGetComponent(out UnitGridAuthoring authoring));
        Assert.IsTrue(authoring.IsAirUnit);
        Assert.IsTrue(authoring.ProductionTransportUsesRunwayLanding);
        Assert.AreEqual(24, authoring.SoldierTransportCapacity);
        Assert.AreEqual(2, authoring.VehicleTransportCapacity);
        Assert.IsNotNull(authoring.SoldierParachuteVisualPrefab);
        Assert.IsNotNull(authoring.VehicleEmergencyDropVisualPrefab);
        AssertValidPrefabAssetReference(authoring.SoldierParachuteVisualPrefab, ParachutePath);
        AssertValidPrefabAssetReference(authoring.VehicleEmergencyDropVisualPrefab, EmergencyDropPath);
    }

    [Test]
    public void TransportPlaneSelectionMetadata_ResolvesPortraitAndSelectionReferences()
    {
        const string PrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.IsNotNull(prefab);
        Assert.IsTrue(prefab.TryGetComponent(out UnitGridAuthoring authoring));
        Assert.AreEqual("Transport Plane", authoring.ConfiguredDisplayName);
        Assert.IsTrue(authoring.IsAirUnit);
        Assert.AreEqual(24, authoring.SoldierTransportCapacity);
        Assert.AreEqual(2, authoring.VehicleTransportCapacity);
        Assert.IsNotNull(authoring.PortraitSprite);
        Assert.IsNotNull(authoring.PortraitCardSprite);
        Assert.IsNotNull(authoring.PortraitActionSprite);
        Assert.IsNotNull(SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionPortraitSprite(prefab));
        Assert.IsNotNull(SelectionPortraitSpriteResolverUiSystemHelper.ResolveSelectionCardPortraitSprite(prefab));
        Assert.IsNotNull(authoring.UnitSelectionMarkerPrefab);
        Assert.IsNotNull(authoring.VehicleSelectionMarkerPrefab);
        Assert.IsNotNull(authoring.UnitHealthBarPrefab);
        Assert.IsNotNull(authoring.VehicleHealthBarPrefab);

        var definitionSystem = new BuildingDefinitionPrefabSystemHelper();
        definitionSystem.ConfigureAuthoringMetadataResolvers(null, TryGetTestUnitDefinitionMetadata);
        definitionSystem.RebuildSpawnablesLookup(null, new List<GameObject> { prefab });
        Assert.IsTrue(definitionSystem.TryResolveConfiguredUnitSpawnPrefab("Unit_Veh_Plane_Transport", out GameObject resolvedByPrefabName));
        Assert.AreSame(prefab, resolvedByPrefabName);
        Assert.IsTrue(definitionSystem.TryResolveConfiguredUnitSpawnPrefab("Transport Plane", out GameObject resolvedByDisplayName));
        Assert.AreSame(prefab, resolvedByDisplayName);
    }

    private static bool TryGetTestUnitDefinitionMetadata(GameObject prefab, out BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata metadata)
    {
        metadata = default;
        if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
            return false;

        metadata = new BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata
        {
            DisplayName = authoring.ConfiguredDisplayName,
            Description = authoring.ConfiguredDescription,
            FootprintCells = authoring.GetConfiguredFootprintCells(),
            CanRequest = authoring.CanRequest,
            Price = authoring.Price
        };
        return true;
    }

    [Test]
    public void TransportPlaneFixedWingClassifier_TreatsTransportPlaneAsRunwayAircraft()
    {
        Assert.IsTrue(FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(new FixedString64Bytes("Unit_Veh_Plane_Transport")));
        Assert.IsTrue(FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(new FixedString64Bytes("Unit_Veh_Jet_02")));
        Assert.IsTrue(FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(new FixedString64Bytes("Unit_Veh_Drone")));
        Assert.IsFalse(FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(new FixedString64Bytes("Unit_Veh_Helicopter_Transport")));
    }

    [Test]
    public void TransportPlaneDoorSystem_InterpolatesBakedDoorRotation()
    {
        using var world = new World("TransportPlaneDoorSystem_InterpolatesBakedDoorRotation");
        EntityManager em = world.EntityManager;
        Entity transport = em.CreateEntity(typeof(UnitTransportPlaneDoorReference), typeof(UnitTransportPlaneDoorState));
        Entity door = em.CreateEntity(typeof(LocalTransform));
        Entity passenger = em.CreateEntity(typeof(UnitTransportBoardingTarget), typeof(LocalTransform));
        quaternion closedRotation = quaternion.identity;
        quaternion openRotation = quaternion.EulerXYZ(math.radians(90f), 0f, 0f);
        em.SetComponentData(door, LocalTransform.FromPositionRotationScale(float3.zero, closedRotation, 1f));
        em.SetComponentData(passenger, new UnitTransportBoardingTarget
        {
            Transport = transport,
            Goal = int2.zero
        });
        em.SetComponentData(passenger, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(transport, new UnitTransportPlaneDoorReference
        {
            DoorEntity = door,
            ClosedLocalRotation = closedRotation,
            OpenLocalRotation = openRotation,
            OpenSeconds = 1f,
            CloseSeconds = 0.5f
        });
        em.SetComponentData(transport, new UnitTransportPlaneDoorState
        {
            Open01 = 0f,
            TargetOpen = 0
        });

        SystemHandle system = world.CreateSystem<UnitTransportPlaneDoorSystem>();
        world.SetTime(new TimeData(1d, 0.5f));
        system.Update(world.Unmanaged);

        UnitTransportPlaneDoorState openedState = em.GetComponentData<UnitTransportPlaneDoorState>(transport);
        Assert.AreEqual(1, openedState.TargetOpen);
        Assert.AreEqual(0.5f, openedState.Open01, 0.001f);
        Assert.That(math.dot(em.GetComponentData<LocalTransform>(door).Rotation, closedRotation), Is.LessThan(0.999f));

        em.RemoveComponent<UnitTransportBoardingTarget>(passenger);
        world.SetTime(new TimeData(2d, 0.5f));
        system.Update(world.Unmanaged);

        UnitTransportPlaneDoorState closedState = em.GetComponentData<UnitTransportPlaneDoorState>(transport);
        Assert.AreEqual(0, closedState.TargetOpen);
        Assert.AreEqual(0f, closedState.Open01, 0.001f);
        Assert.That(math.dot(em.GetComponentData<LocalTransform>(door).Rotation, closedRotation), Is.GreaterThan(0.999f));
    }

    [Test]
    public void TransportPlaneBoardingCommand_AllowsSelectedVehiclePassenger()
    {
        using var world = new World("TransportPlaneBoardingCommand_AllowsSelectedVehiclePassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(vehicle));
        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(vehicle);
        Assert.AreEqual(transport, boarding.Transport);
        Assert.AreEqual(UnitTransportPassengerKind.Vehicle, boarding.PassengerKind);
        Assert.AreEqual(new int2(12, 7), boarding.Goal);
    }

    [Test]
    public void TransportPlaneBoardingCommand_UsesRearRampApproachForSoldierPassenger()
    {
        using var world = new World("TransportPlaneBoardingCommand_UsesRearRampApproachForSoldierPassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual("Boarding transport plane.", result.Message.ToString());
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, boarding.Transport);
        Assert.AreEqual(UnitTransportPassengerKind.Soldier, boarding.PassengerKind);
        Assert.AreEqual(new int2(12, 7), boarding.Goal);
    }

    [Test]
    public void TransportPlaneBoardingCommand_QueuesPassengerMoveToBoardingGoal()
    {
        using var world = new World("TransportPlaneBoardingCommand_QueuesPassengerMoveToBoardingGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted);
        AssertBoardingMoveOrder(em, passenger, transport);
    }

    [Test]
    public void TransportPlaneBoardingCommand_SelectedFarSoldierUsesRampWithoutNearbyRequirement()
    {
        using var world = new World("TransportPlaneBoardingCommand_SelectedFarSoldierUsesRampWithoutNearbyRequirement");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 96, 96);

        Entity transport = CreateTransportPlane(em, new int2(70, 70));
        em.RemoveComponent<UnitTransportCargoCapacity>(transport);
        em.RemoveComponent<UnitSourcePrefabKey>(transport);
        Entity passenger = CreateSelectablePassenger(em, new int2(5, 5));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual("Boarding transport plane.", result.Message.ToString());
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, boarding.Transport);
        Assert.AreEqual(UnitTransportPassengerKind.Soldier, boarding.PassengerKind);
        Assert.AreEqual(new int2(70, 65), boarding.Goal);
    }

    [Test]
    public void TransportPlaneBoardingCommand_NoDoorMetadataStillBoardsSelectedFarSoldier()
    {
        using var world = new World("TransportPlaneBoardingCommand_NoDoorMetadataStillBoardsSelectedFarSoldier");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateGrid(em, 96, 96);

            Entity transport = CreateTransportPlane(em, new int2(70, 70));
            em.RemoveComponent<UnitTransportPlaneDoorReference>(transport);
            em.RemoveComponent<UnitTransportPlaneDoorState>(transport);
            Entity passenger = CreateSelectablePassenger(em, new int2(5, 5));
            var commandSystem = new TransportBoardingCommandSystem();

            TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
                em,
                transport,
                new UnitTransportAirPickupSystem(),
                new UnitMoveOrderSystem(),
                new SelectionStateCompositionSystemHelper());

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual("Boarding transport plane.", result.Message.ToString());
            Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
            UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
            Assert.AreEqual(transport, boarding.Transport);
            Assert.AreEqual(UnitTransportPassengerKind.Soldier, boarding.PassengerKind);
            Assert.LessOrEqual(math.distancesq(new float2(boarding.Goal.x, boarding.Goal.y), new float2(70, 70)), 25f);

            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            for (int i = 0; i < 256 && !em.HasComponent<UnitPathRange>(passenger); i++)
            {
                world.SetTime(new TimeData((i + 1) * 0.016d, 0.016f));
                pathSystem.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                System.Threading.Thread.Sleep(1);
            }

            Assert.IsTrue(em.HasComponent<UnitPathRange>(passenger), "Selected far soldier should receive a real path to board the specific transport plane even when door metadata is missing.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockSelectedPassengerGoal()
    {
        using var world = new World("TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockSelectedPassengerGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));
        Entity rampSoldier = CreateSelectablePassenger(em, new int2(12, 7));
        em.RemoveComponent<SelectedUnitTag>(passenger);
        em.RemoveComponent<SelectedUnitTag>(rampSoldier);

        int2 rampCell = new(12, 7);
        for (int y = rampCell.y - 8; y <= rampCell.y + 8; y++)
        {
            for (int x = rampCell.x - 8; x <= rampCell.x + 8; x++)
            {
                int2 cell = new(x, y);
                if (!GridUtils.InBounds(cell, 30, 30) || cell.Equals(rampCell))
                    continue;

                _blocked.Set(GridUtils.CellToIndex(cell, 30), true);
            }
        }

        _occupied.Set(GridUtils.CellToIndex(rampCell, 30), true);
        var selectionStateSystem = new SelectionStateCompositionSystemHelper();
        selectionStateSystem.CacheSelectedMoveEntities(em, new[] { rampSoldier, passenger });
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            selectionStateSystem);

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, boarding.Transport);
        Assert.AreEqual(new int2(12, 7), boarding.Goal);
    }

    [Test]
    public void TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockPathfindingToRamp()
    {
        using var world = new World("TransportPlaneBoardingCommand_SelectedRampSoldierDoesNotBlockPathfindingToRamp");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
            CreateGrid(em, 30, 30);

            Entity transport = CreateTransportPlane(em, new int2(12, 12));
            Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));
            Entity rampSoldier = CreateSelectablePassenger(em, new int2(12, 7));
            em.RemoveComponent<SelectedUnitTag>(passenger);
            em.RemoveComponent<SelectedUnitTag>(rampSoldier);

            var selectionStateSystem = new SelectionStateCompositionSystemHelper();
            selectionStateSystem.CacheSelectedMoveEntities(em, new[] { rampSoldier, passenger });
            var commandSystem = new TransportBoardingCommandSystem();

            TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
                em,
                transport,
                new UnitTransportAirPickupSystem(),
                new UnitMoveOrderSystem(),
                selectionStateSystem);

            Assert.IsTrue(result.Accepted);
            Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
            UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
            Assert.AreEqual(new int2(12, 7), boarding.Goal);
            Assert.IsTrue(em.HasComponent<ManualMoveGroupMemberTag>(passenger));
            Assert.IsTrue(em.HasComponent<ManualMoveGroupMemberTag>(rampSoldier));

            SystemHandle pathSystem = world.CreateSystem<UnitPathfindingSystem>();
            for (int i = 0; i < 128 && !em.HasComponent<UnitPathRange>(passenger); i++)
            {
                world.SetTime(new TimeData((i + 1) * 0.016d, 0.016f));
                pathSystem.Update(world.Unmanaged);
                em.CompleteAllTrackedJobs();
                System.Threading.Thread.Sleep(1);
            }

            Assert.IsTrue(em.HasComponent<UnitPathRange>(passenger), "Boarding passenger should receive a real path to the ramp even when another selected boarding soldier occupies the ramp cell.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void TransportPlaneBoardingCommand_RetryReplacesStalePendingOrdersWithoutCountingThemAsSeats()
    {
        using var world = new World("TransportPlaneBoardingCommand_RetryReplacesStalePendingOrdersWithoutCountingThemAsSeats");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 6 });
        em.SetComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 6,
            VehicleCapacity = 0,
            CargoWeightCapacity = 0
        });
        Entity loadedPassenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = loadedPassenger });

        Entity[] passengers =
        {
            CreateSelectablePassenger(em, new int2(17, 10)),
            CreateSelectablePassenger(em, new int2(17, 11)),
            CreateSelectablePassenger(em, new int2(17, 12)),
            CreateSelectablePassenger(em, new int2(17, 13)),
            CreateSelectablePassenger(em, new int2(17, 14))
        };
        int2[] staleGoals =
        {
            new(2, 2),
            new(3, 2),
            new(4, 2),
            new(5, 2),
            new(6, 2)
        };
        for (int i = 0; i < passengers.Length; i++)
        {
            em.AddComponentData(passengers[i], new UnitTransportBoardingTarget
            {
                Transport = transport,
                Goal = staleGoals[i],
                PassengerKind = UnitTransportPassengerKind.Soldier
            });
        }

        var commandSystem = new TransportBoardingCommandSystem();
        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsTrue(result.Accepted, "Retrying a selected board command must replace stale pending reservations instead of treating them as occupied seats.");
        for (int i = 0; i < passengers.Length; i++)
        {
            Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passengers[i]));
            UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passengers[i]);
            Assert.AreEqual(transport, boarding.Transport);
            Assert.AreNotEqual(staleGoals[i], boarding.Goal);
        }
    }

    [Test]
    public void TransportPlaneBoardingCommand_RejectsAirbornePlanePickupBoarding()
    {
        using var world = new World("TransportPlaneBoardingCommand_RejectsAirbornePlanePickupBoarding");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(12.5f, 8f, 12.5f)));
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.Airborne = 1;
        em.SetComponentData(transport, airState);
        Entity passenger = CreateSelectablePassenger(em, new int2(17, 12));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsFalse(result.Accepted);
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void TransportPlaneBoardingSystem_BoardsVehicleIntoCargoSlot()
    {
        using var world = new World("TransportPlaneBoardingSystem_BoardsVehicleIntoCargoSlot");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity vehicle = CreateVehiclePassenger(em, new int2(13, 12), transport, new int2(13, 12));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(vehicle, passengers[0].Passenger);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        UnitTransportCargoPassenger cargoPassenger = em.GetComponentData<UnitTransportCargoPassenger>(vehicle);
        Assert.AreEqual(UnitTransportPassengerKind.Vehicle, cargoPassenger.PassengerKind);
        Assert.AreEqual(transport, cargoPassenger.Transport);
    }

    [Test]
    public void TransportPlaneBoardingCommand_LoadingCargoMessageForVehiclePassenger()
    {
        using var world = new World("TransportPlaneBoardingCommand_LoadingCargoMessageForVehiclePassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            TargetEntity = transport,
            SecondaryTargetEntity = vehicle,
            HasTargetEntity = 1,
            HasSecondaryTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Loading cargo.", results[0].Message.ToString());
    }

    [Test]
    public void TransportPlaneBoardingCommand_FullVehicleSlotsReportsTransportFull()
    {
        using var world = new World("TransportPlaneBoardingCommand_FullVehicleSlotsReportsTransportFull");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity loadedA = CreateLoadedVehiclePassenger(em, transport);
        Entity loadedB = CreateLoadedVehiclePassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = loadedA });
        passengers.Add(new UnitTransportPassengerElement { Passenger = loadedB });
        Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            TargetEntity = transport,
            SecondaryTargetEntity = vehicle,
            HasTargetEntity = 1,
            HasSecondaryTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TransportFull, results[0].ReasonCode);
        Assert.AreEqual("Transport is full.", results[0].Message.ToString());
    }

    [Test]
    public void TransportPlaneBoardPreview_OnlyCargoPlaneAcceptsVehiclePassenger()
    {
        using var world = new World("TransportPlaneBoardPreview_OnlyCargoPlaneAcceptsVehiclePassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transportPlane = CreateTransportPlane(em, new int2(12, 12));
        Entity helicopter = CreateTransport(em, new int2(20, 20), air: true, airborne: false, sourcePrefabKey: "Unit_Veh_Helicopter_Transport");
        Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
        Entity soldier = CreateSelectablePassenger(em, new int2(18, 12));
        var pointerSystem = new RtsSelectionPointerTargetCommandCompositionSystemHelper();
        RtsSelectionPointerTargetCommandCompositionSystemHelper.Context context = default;

        Assert.IsTrue(pointerSystem.IsValidBoardPassengerPreviewTarget(context, em, transportPlane, vehicle));
        Assert.IsTrue(pointerSystem.IsValidBoardTransportPreviewTarget(context, em, vehicle, transportPlane));
        Assert.IsTrue(pointerSystem.IsValidBoardPassengerPreviewTarget(context, em, transportPlane, soldier));
        Assert.IsFalse(pointerSystem.IsValidBoardPassengerPreviewTarget(context, em, helicopter, vehicle));
        Assert.IsFalse(pointerSystem.IsValidBoardTransportPreviewTarget(context, em, vehicle, helicopter));
    }

    [Test]
    public void HelicopterBoardingCommand_RejectsVehiclePassenger()
    {
        using var world = new World("HelicopterBoardingCommand_RejectsVehiclePassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransport(em, new int2(12, 12), air: true, airborne: false, sourcePrefabKey: "Unit_Veh_Helicopter_Transport");
        Entity vehicle = CreateSelectableVehiclePassenger(em, new int2(17, 12));
        var commandSystem = new TransportBoardingCommandSystem();

        TransportBoardingCommandSystem.Result result = commandSystem.TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper());

        Assert.IsFalse(result.Accepted);
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(vehicle));
    }

    [Test]
    public void TransportPlaneDisembarkCommand_UsesRearRampForLoadedVehicle()
    {
        using var world = new World("TransportPlaneDisembarkCommand_UsesRearRampForLoadedVehicle");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);
        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<Disabled>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        Assert.AreEqual(new int2(12, 7), em.GetComponentData<UnitGrid>(vehicle).Cell);
        Assert.IsTrue(em.HasComponent<UnitTarget>(vehicle));
        Assert.AreEqual(new int2(12, 3), em.GetComponentData<UnitTarget>(vehicle).Cell);
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(vehicle));
        Assert.AreEqual(new int2(12, 3), em.GetComponentData<UnitPathRequest>(vehicle).Goal);
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_LandedWithoutDoorMetadataUsesGroundExit()
    {
        using var world = new World("TransportPlaneDisembarkCommand_LandedWithoutDoorMetadataUsesGroundExit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        em.RemoveComponent<UnitTransportPlaneDoorReference>(transport);
        em.RemoveComponent<UnitTransportPlaneDoorState>(transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });

        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.IsFalse(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_BlockedRampReportsNoDisembarkCell()
    {
        using var world = new World("TransportPlaneDisembarkCommand_BlockedRampReportsNoDisembarkCell");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);
        for (int i = 0; i < _blocked.Length; i++)
            _blocked.Set(i, true);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);
        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<Disabled>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoDisembarkCell, results[0].ReasonCode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual("No clear exit point for passengers.", results[0].Message.ToString());
    }

    [Test]
    public void TransportPlaneDoorOpenRequest_OpensThenExpires()
    {
        using var world = new World("TransportPlaneDoorOpenRequest_OpensThenExpires");
        EntityManager em = world.EntityManager;
        Entity transport = em.CreateEntity(
            typeof(UnitTransportPlaneDoorReference),
            typeof(UnitTransportPlaneDoorState),
            typeof(UnitTransportPlaneDoorOpenRequest));
        em.SetComponentData(transport, new UnitTransportPlaneDoorReference
        {
            DoorEntity = Entity.Null,
            ClosedLocalRotation = quaternion.identity,
            OpenLocalRotation = quaternion.identity,
            OpenSeconds = 1f,
            CloseSeconds = 1f
        });
        em.SetComponentData(transport, new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = 0.2f });

        SystemHandle doorSystem = world.CreateSystem<UnitTransportPlaneDoorSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        doorSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen);

        world.SetTime(new TimeData(1.1d, 0.2f));
        doorSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen);

        world.SetTime(new TimeData(1.3d, 0.1f));
        doorSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_AirborneStartsAirdropRequest()
    {
        using var world = new World("TransportPlaneDisembarkCommand_AirborneStartsAirdropRequest");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        passengers.Add(new UnitTransportPassengerElement { Passenger = vehicle });

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            TargetCell = new int2(15, 15),
            HasTargetEntity = 1,
            HasTargetCell = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);
        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(2, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<Disabled>(soldier));
        Assert.IsTrue(em.HasComponent<Disabled>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport));
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropRequest>(transport));
        UnitTransportAirdropRequest airdrop = em.GetComponentData<UnitTransportAirdropRequest>(transport);
        Assert.AreEqual(new int2(15, 15), airdrop.DropReferenceCell);
        Assert.AreEqual(2, airdrop.DropCount);
        Assert.AreEqual(1, airdrop.SoldierDropCount);
        Assert.AreEqual(1, airdrop.VehicleDropCount);
        Assert.AreEqual(UnitTransportAirdropMode.Mixed, airdrop.DropMode);
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_AirdropResultReportsInProgress()
    {
        using var world = new World("TransportPlaneDisembarkCommand_AirdropResultReportsInProgress");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Airdrop in progress.", results[0].Message.ToString());
    }

    [Test]
    public void TransportPlaneDisembarkCommand_AirdropInProgressActuallyDropsPassenger()
    {
        using var world = new World("TransportPlaneDisembarkCommand_AirdropInProgressActuallyDropsPassenger");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: true);
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Airdrop in progress.", results[0].Message.ToString());

        for (int i = 0; i < 8 && em.GetBuffer<UnitTransportPassengerElement>(transport).Length > 0; i++)
        {
            world.SetTime(new TimeData(1.1d + (i * 0.1d), 0.1f));
            airMovementSystem.Update(world.Unmanaged);
            airdropSystem.Update(world.Unmanaged);
        }

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
    }

    [Test]
    public void TransportPlaneDisembarkCommand_MissingAirdropVisualRejectsWithReason()
    {
        using var world = new World("TransportPlaneDisembarkCommand_MissingAirdropVisualRejectsWithReason");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.CommandUnavailable, results[0].ReasonCode);
        Assert.AreEqual("Parachute visual missing.", results[0].Message.ToString());
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_RegistryVisualStartsAirdropRequest()
    {
        using var world = new World("TransportPlaneDisembarkCommand_RegistryVisualStartsAirdropRequest");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "RegistryCommandParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "RegistryCommandCargoVisual");
        AddAirdropVisualPrefabRegistry(em, "Unit_Veh_Plane_Transport", parachutePrefab, cargoPrefab);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            HasTargetEntity = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Airdrop in progress.", results[0].Message.ToString());
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_NoLandingCellRejectsWithReason()
    {
        using var world = new World("TransportPlaneDisembarkCommand_NoLandingCellRejectsWithReason");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        int2 dropCell = new(15, 15);
        BlockAirdropLandingArea(new GridConfig { Width = 30, Height = 30, CellSize = 1f, Origin = float3.zero }, dropCell, 16);

        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            TargetCell = dropCell,
            HasTargetEntity = 1,
            HasTargetCell = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TargetBlocked, results[0].ReasonCode);
        Assert.AreEqual("No clear airdrop landing zone.", results[0].Message.ToString());
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
    }

    [Test]
    public void TransportPlaneDisembarkCommand_BlockedAirdropReportsCargoDropBlocked()
    {
        using var world = new World("TransportPlaneDisembarkCommand_BlockedAirdropReportsCargoDropBlocked");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity passenger = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        using EntityQuery gridQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>());
        Entity gridEntity = gridQuery.GetSingletonEntity();
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        int2 blockedDropCell = new(15, 15);
        walkable[GridUtils.CellToIndex(blockedDropCell, 30)] = new GridWalkable { Value = 0 };
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            TargetCell = blockedDropCell,
            HasTargetEntity = 1,
            HasTargetCell = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TargetBlocked, results[0].ReasonCode);
        Assert.AreEqual("Cargo drop blocked.", results[0].Message.ToString());
    }

    [Test]
    public void TransportPlaneDisembarkCommand_LandedTargetCellStartsAirdropRequest()
    {
        using var world = new World("TransportPlaneDisembarkCommand_LandedTargetCellStartsAirdropRequest");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
            TargetEntity = transport,
            TargetCell = new int2(15, 15),
            HasTargetEntity = 1,
            HasTargetCell = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropRequest>(transport));
        UnitTransportAirdropRequest request = em.GetComponentData<UnitTransportAirdropRequest>(transport);
        Assert.AreEqual(new int2(15, 15), request.DropReferenceCell);
        Assert.AreEqual(0, request.PassReady);
        Assert.IsTrue(em.HasComponent<Disabled>(soldier));
    }

    [Test]
    public void TransportPlaneAirdropPass_WaitsForFixedWingPassBeforeReleaseAndReturnsHome()
    {
        using var world = new World("TransportPlaneAirdropPass_WaitsForFixedWingPassBeforeReleaseAndReturnsHome");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransportPlane(em, new int2(4, 15));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: true);
        SetTransportPlaneAirborne(em, transport, new float3(4.5f, 55f, 15.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 0
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "Airdrop must wait until air movement marks the pass ready.");
        Assert.IsTrue(em.HasComponent<Disabled>(soldier));
        Assert.IsFalse(em.HasComponent<UnitTransportParachuteDropComponent>(soldier));

        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        for (int i = 0; i < 60 && em.GetBuffer<UnitTransportPassengerElement>(transport).Length > 0; i++)
        {
            world.SetTime(new TimeData(1.1d + (i * 0.25d), 0.25f));
            airMovementSystem.Update(world.Unmanaged);
            airdropSystem.Update(world.Unmanaged);
        }

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(soldier));
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        Assert.AreEqual(1, airState.ReturningHome);
        Assert.AreEqual(0, airState.AttackRunActive);
    }

    [Test]
    public void TransportPlaneAirdropPass_DoesNotRequireUnitAttackComponent()
    {
        using var world = new World("TransportPlaneAirdropPass_DoesNotRequireUnitAttackComponent");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransportPlane(em, new int2(4, 15));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: true);
        em.RemoveComponent<UnitAttack>(transport);
        SetTransportPlaneAirborne(em, transport, new float3(4.5f, 55f, 15.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 0
        });

        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        for (int i = 0; i < 80 && em.GetBuffer<UnitTransportPassengerElement>(transport).Length > 0; i++)
        {
            world.SetTime(new TimeData(1d + (i * 0.25d), 0.25f));
            airMovementSystem.Update(world.Unmanaged);
            airdropSystem.Update(world.Unmanaged);
        }

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(soldier));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(soldier);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
    }

    [Test]
    public void TransportPlaneAirdropPass_PhysicallyAirborneWithStaleGroundedFlagDropsPassengers()
    {
        using var world = new World("TransportPlaneAirdropPass_PhysicallyAirborneWithStaleGroundedFlagDropsPassengers");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransportPlane(em, new int2(15, 15));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        float3 airbornePosition = new(15.5f, 55f, 15.5f);
        em.SetComponentData(transport, LocalTransform.FromPosition(airbornePosition));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(airbornePosition) });
        em.SetComponentData(transport, new UnitGrid { Cell = new int2(15, 15) });
        UnitAirComponent staleAirState = em.GetComponentData<UnitAirComponent>(transport);
        staleAirState.Airborne = 0;
        staleAirState.TakeoffRolling = 0;
        staleAirState.LandingRolling = 0;
        staleAirState.AttackRunActive = 0;
        em.SetComponentData(transport, staleAirState);
        AddAirdropVisualPrefabs(em, transport);

        Entity soldier = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 0
        });

        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        for (int i = 0; i < 4 && em.GetBuffer<UnitTransportPassengerElement>(transport).Length > 0; i++)
        {
            world.SetTime(new TimeData(1d + (i * 0.1d), 0.1f));
            airMovementSystem.Update(world.Unmanaged);
            airdropSystem.Update(world.Unmanaged);
        }

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        UnitAirComponent correctedAirState = em.GetComponentData<UnitAirComponent>(transport);
        Assert.AreEqual(1, correctedAirState.Airborne);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(soldier));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(soldier);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
    }

    [Test]
    public void TransportPlaneAirdropSystem_ReleasesSoldierWithParachuteVisualAndRestoresOnTouchdown()
    {
        using var world = new World("TransportPlaneAirdropSystem_ReleasesSoldierWithParachuteVisualAndRestoresOnTouchdown");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
        Entity passenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportHiddenVisualScale> hiddenVisuals = em.GetBuffer<UnitTransportHiddenVisualScale>(passenger);
        hiddenVisuals.Add(new UnitTransportHiddenVisualScale
        {
            Visual = passenger,
            PreviousScale = 1f,
            WasDisabled = 0
        });
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 1
        });
        em.AddComponentData(transport, new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = 5f });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(0.75f, em.GetComponentData<UnitTransportPlaneDoorOpenRequest>(transport).RemainingSeconds, 0.001f);
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        Assert.AreEqual(0, em.GetBuffer<UnitTransportHiddenVisualScale>(passenger).Length);
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        Assert.AreEqual(new int2(15, 15), drop.LandingCell);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualCleanup>(drop.VisualEntity));

        world.SetTime(new TimeData(6d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        Assert.AreEqual(new int2(15, 15), em.GetComponentData<UnitGrid>(passenger).Cell);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(passenger).Position.y, 0.001f);
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropSettleComponent>(passenger));
        Assert.IsTrue(em.Exists(drop.VisualEntity), "Visual should linger briefly after touchdown.");

        world.SetTime(new TimeData(8d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.Exists(drop.VisualEntity), "Parachute visual should be destroyed after cleanup delay.");
    }

    [Test]
    public void TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromSourceKeyBeforeDrop()
    {
        using var world = new World("TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromSourceKeyBeforeDrop");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "SourceParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "SourceCargoVisual");
        Entity sourcePrefab = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitSourcePrefabKey),
            typeof(UnitTransportAirdropVisualPrefabs));
        em.SetComponentData(sourcePrefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });
        em.SetComponentData(sourcePrefab, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });

        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
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

        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport));
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualCleanup>(drop.VisualEntity));
    }

    [Test]
    public void TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromRegistryBeforeDrop()
    {
        using var world = new World("TransportPlaneAirdropSystem_BackfillsVisualPrefabsFromRegistryBeforeDrop");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "RegistryParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "RegistryCargoVisual");
        AddAirdropVisualPrefabRegistry(em, "Unit_Veh_Plane_Transport", parachutePrefab, cargoPrefab);

        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
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

        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport));
        UnitTransportAirdropVisualPrefabs backfilled = em.GetComponentData<UnitTransportAirdropVisualPrefabs>(transport);
        Assert.AreEqual(parachutePrefab, backfilled.SoldierParachuteVisualPrefab);
        Assert.AreEqual(cargoPrefab, backfilled.VehicleEmergencyDropVisualPrefab);
        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportParachuteDropComponent>(passenger));
        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualCleanup>(drop.VisualEntity));
    }

    [Test]
    public void TransportPlaneAirdropSystem_ReleasesVehicleCargoWithEmergencyDropVisual()
    {
        using var world = new World("TransportPlaneAirdropSystem_ReleasesVehicleCargoWithEmergencyDropVisual");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.VehicleOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<Disabled>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(vehicle));
        Assert.IsTrue(em.HasComponent<UnitTransportCargoDropComponent>(vehicle));
        UnitTransportCargoDropComponent drop = em.GetComponentData<UnitTransportCargoDropComponent>(vehicle);
        Assert.AreEqual(new int2(15, 15), drop.LandingCell);
        Assert.AreNotEqual(Entity.Null, drop.VisualEntity);
        Assert.IsTrue(em.Exists(drop.VisualEntity));

        world.SetTime(new TimeData(7d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportCargoDropComponent>(vehicle));
        Assert.AreEqual(new int2(15, 15), em.GetComponentData<UnitGrid>(vehicle).Cell);
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(vehicle).Position.y, 0.001f);
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropSettleComponent>(vehicle));
    }

    [Test]
    public void TransportPlaneAirdropSystem_KeepsDoorOpenBrieflyAfterFinalDropRelease()
    {
        using var world = new World("TransportPlaneAirdropSystem_KeepsDoorOpenBrieflyAfterFinalDropRelease");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            SoldierDropCount = 1,
            DropMode = UnitTransportAirdropMode.SoldierOnly,
            PassReady = 1
        });
        em.AddComponentData(transport, new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = 5f });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportAirdropRequest>(transport));
        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(0.75f, em.GetComponentData<UnitTransportPlaneDoorOpenRequest>(transport).RemainingSeconds, 0.001f);

        SystemHandle doorSystem = world.CreateSystem<UnitTransportPlaneDoorSystem>();
        world.SetTime(new TimeData(1.3d, 0.3f));
        doorSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(1, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen);

        world.SetTime(new TimeData(2.1d, 0.8f));
        doorSystem.Update(world.Unmanaged);
        world.SetTime(new TimeData(2.2d, 0.1f));
        doorSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport));
        Assert.AreEqual(0, em.GetComponentData<UnitTransportPlaneDoorState>(transport).TargetOpen);
    }

    [Test]
    public void TransportPlaneAirdropSystem_ParachuteVisualTracksSoldierDuringDescent()
    {
        using var world = new World("TransportPlaneAirdropSystem_ParachuteVisualTracksSoldierDuringDescent");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
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

        UnitTransportParachuteDropComponent drop = em.GetComponentData<UnitTransportParachuteDropComponent>(passenger);
        AssertVisualTracksPassenger(em, passenger, drop.VisualEntity, 2.2f, 1.2f);

        world.SetTime(new TimeData(2.7d, 1.7f));
        airdropSystem.Update(world.Unmanaged);

        LocalTransform passengerTransform = em.GetComponentData<LocalTransform>(passenger);
        Assert.Greater(passengerTransform.Position.y, 0.1f);
        Assert.Less(passengerTransform.Position.y, drop.StartPosition.y);
        AssertVisualTracksPassenger(em, passenger, drop.VisualEntity, 2.2f, 1.2f);
    }

    [Test]
    public void TransportPlaneAirdropSystem_CargoVisualTracksVehicleDuringDescent()
    {
        using var world = new World("TransportPlaneAirdropSystem_CargoVisualTracksVehicleDuringDescent");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        em.AddComponentData(transport, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.VehicleOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        UnitTransportCargoDropComponent drop = em.GetComponentData<UnitTransportCargoDropComponent>(vehicle);
        AssertVisualTracksPassenger(em, vehicle, drop.VisualEntity, 1.6f);

        world.SetTime(new TimeData(3.4d, 2.4f));
        airdropSystem.Update(world.Unmanaged);

        LocalTransform vehicleTransform = em.GetComponentData<LocalTransform>(vehicle);
        Assert.Greater(vehicleTransform.Position.y, 0.1f);
        Assert.Less(vehicleTransform.Position.y, drop.StartPosition.y);
        AssertVisualTracksPassenger(em, vehicle, drop.VisualEntity, 1.6f);
    }

    [Test]
    public void TransportPlaneAirdropSystem_SoldierSettlesAfterTouchdown()
    {
        using var world = new World("TransportPlaneAirdropSystem_SoldierSettlesAfterTouchdown");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
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
        world.SetTime(new TimeData(6d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportAirdropSettleComponent>(passenger));
        UnitTransportAirdropSettleComponent settle = em.GetComponentData<UnitTransportAirdropSettleComponent>(passenger);
        Assert.AreNotEqual(new int2(15, 15), settle.EndCell);

        world.SetTime(new TimeData(7d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportAirdropSettleComponent>(passenger));
        Assert.AreEqual(settle.EndCell, em.GetComponentData<UnitGrid>(passenger).Cell);
        Assert.Less(math.distance(settle.EndPosition, em.GetComponentData<LocalTransform>(passenger).Position), 0.001f);
        Assert.AreEqual(0, em.GetComponentData<UnitMoveVisualComponent>(passenger).IsMoving);
    }

    [Test]
    public void TransportPlaneAirdropSystem_VehicleRollsOutAfterCargoTouchdown()
    {
        using var world = new World("TransportPlaneAirdropSystem_VehicleRollsOutAfterCargoTouchdown");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        SetTransportPlaneAirborne(em, transport, new float3(12.5f, 55f, 12.5f));
        AddAirdropVisualPrefabs(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
        em.AddComponentData(transport, new UnitTransportAirdropRequest
        {
            DropReferenceCell = new int2(15, 15),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f,
            DropCount = 1,
            VehicleDropCount = 1,
            DropMode = UnitTransportAirdropMode.VehicleOnly,
            PassReady = 1
        });

        SystemHandle airdropSystem = world.CreateSystem<UnitTransportAirdropSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        airdropSystem.Update(world.Unmanaged);
        world.SetTime(new TimeData(7d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportAirdropSettleComponent>(vehicle));
        UnitTransportAirdropSettleComponent settle = em.GetComponentData<UnitTransportAirdropSettleComponent>(vehicle);
        Assert.AreNotEqual(new int2(15, 15), settle.EndCell);

        world.SetTime(new TimeData(9d, 0.1f));
        airdropSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportAirdropSettleComponent>(vehicle));
        Assert.AreEqual(settle.EndCell, em.GetComponentData<UnitGrid>(vehicle).Cell);
        Assert.Less(math.distance(settle.EndPosition, em.GetComponentData<LocalTransform>(vehicle).Position), 0.001f);
        Assert.AreEqual(0, em.GetComponentData<UnitMoveVisualComponent>(vehicle).IsMoving);
    }

    [Test]
    public void LoadedTransportAttackOrder_CreatesDeployOrderInsteadOfEngageTarget()
    {
        using var world = new World("LoadedTransportAttackOrder_CreatesDeployOrderInsteadOfEngageTarget");
        EntityManager em = world.EntityManager;

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        em.AddComponentData(transport, new UnitCombat { CanAttack = 0, AutoEngage = 0 });
        Entity passenger = CreateLoadedPassenger(em, transport);
        AddAttackCapability(em, passenger);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity target = CreateHostileTarget(em, new int2(20, 20));

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = transport;
            var targetOrderSystem = new UnitTargetOrderSystem();
            result = targetOrderSystem.IssueAttackTarget(em, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.IsTrue(em.HasComponent<UnitTransportDeployOrder>(transport));
        Assert.IsFalse(em.HasComponent<EngageTarget>(transport), "Loaded transports should deploy passengers instead of attacking like a gunship.");
        UnitTransportDeployOrder deployOrder = em.GetComponentData<UnitTransportDeployOrder>(transport);
        Assert.AreEqual(target, deployOrder.TargetEntity);
        Assert.AreEqual(new int2(20, 20), deployOrder.TargetCell);
        Assert.AreEqual(1, deployOrder.AttackAfterDeploy);
    }

    [Test]
    public void LoadedTransportAttackOrder_RejectsTransportWithoutAttackPayload()
    {
        using var world = new World("LoadedTransportAttackOrder_RejectsTransportWithoutAttackPayload");
        EntityManager em = world.EntityManager;

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        em.AddComponentData(transport, new UnitCombat { CanAttack = 0, AutoEngage = 0 });
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity target = CreateHostileTarget(em, new int2(20, 20));

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = transport;
            var targetOrderSystem = new UnitTargetOrderSystem();
            result = targetOrderSystem.IssueAttackTarget(em, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsFalse(result.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, result.CommandResult.ReasonCode);
        Assert.AreEqual(UnitTransportAttackPayloadUtility.ResolveNoAttackPayloadFeedback(), result.CommandResult.Message);
        Assert.IsFalse(em.HasComponent<UnitTransportDeployOrder>(transport));
        Assert.IsFalse(em.HasComponent<EngageTarget>(transport));
    }

    [Test]
    public void LoadedTransportAttackOrder_AcceptsNestedAttackPayloadCarrier()
    {
        using var world = new World("LoadedTransportAttackOrder_AcceptsNestedAttackPayloadCarrier");
        EntityManager em = world.EntityManager;

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        em.AddComponentData(transport, new UnitCombat { CanAttack = 0, AutoEngage = 0 });
        Entity carrier = CreateLoadedVehiclePassenger(em, transport);
        em.AddComponentData(carrier, new UnitTransportCapacity { SoldierCapacity = 4 });
        em.AddBuffer<UnitTransportPassengerElement>(carrier);
        Entity nestedSoldier = CreateLoadedPassenger(em, carrier);
        AddAttackCapability(em, nestedSoldier);
        em.GetBuffer<UnitTransportPassengerElement>(carrier).Add(new UnitTransportPassengerElement { Passenger = nestedSoldier });
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = carrier });
        Entity target = CreateHostileTarget(em, new int2(20, 20));

        NativeArray<Entity> selected = new(1, Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            selected[0] = transport;
            var targetOrderSystem = new UnitTargetOrderSystem();
            result = targetOrderSystem.IssueAttackTarget(em, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.IsTrue(em.HasComponent<UnitTransportDeployOrder>(transport), "The top-level transport should deploy the carrier because it contains an attack-capable passenger.");
    }

    [Test]
    public void TransportDeployOrderSystem_CargoPlaneStartsAirdropNearBlockedTarget()
    {
        using var world = new World("TransportDeployOrderSystem_CargoPlaneStartsAirdropNearBlockedTarget");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        AddAirdropVisualPrefabs(em, transport);
        Entity passenger = CreateLoadedPassenger(em, transport);
        AddAttackCapability(em, passenger);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });
        Entity target = CreateHostileTarget(em, new int2(20, 20));

        using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<GridWalkable>());
        Entity gridEntity = gridQuery.GetSingletonEntity();
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        walkable[GridUtils.CellToIndex(new int2(20, 20), 32)] = new GridWalkable { Value = 0 };

        em.AddComponentData(transport, new UnitTransportDeployOrder
        {
            TargetEntity = target,
            TargetCell = new int2(20, 20),
            TargetPosition = em.GetComponentData<LocalTransform>(target).Position,
            AttackAfterDeploy = 1
        });

        SystemHandle deploySystem = world.CreateSystem<UnitTransportDeployOrderSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        deploySystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportDeployOrder>(transport));
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropRequest>(transport));
        UnitTransportAirdropRequest request = em.GetComponentData<UnitTransportAirdropRequest>(transport);
        Assert.AreNotEqual(new int2(20, 20), request.DropReferenceCell, "Deploy should pick a nearby walkable cell when the target cell is blocked.");
        Assert.IsTrue(GridUtils.InBounds(request.DropReferenceCell, 32, 32));
        walkable = em.GetBuffer<GridWalkable>(gridEntity);
        Assert.AreEqual(1, walkable[GridUtils.CellToIndex(request.DropReferenceCell, 32)].Value);
        Assert.IsTrue(em.HasComponent<UnitTransportDeployAttackTarget>(passenger));
    }

    [Test]
    public void TransportDeployOrderSystem_AttackDeployAirdropsOnlyAttackPayload()
    {
        using var world = new World("TransportDeployOrderSystem_AttackDeployAirdropsOnlyAttackPayload");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        PrepareRunwayTransportPlaneForAirdropMovement(em, transport, airborne: false);
        AddAirdropVisualPrefabs(em, transport);
        Entity unarmedVehicle = CreateLoadedVehiclePassenger(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        AddAttackCapability(em, soldier);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = unarmedVehicle });
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        Entity target = CreateHostileTarget(em, new int2(20, 20));

        em.AddComponentData(transport, new UnitTransportDeployOrder
        {
            TargetEntity = target,
            TargetCell = new int2(20, 20),
            TargetPosition = em.GetComponentData<LocalTransform>(target).Position,
            AttackAfterDeploy = 1
        });

        SystemHandle deploySystem = world.CreateSystem<UnitTransportDeployOrderSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        deploySystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTransportAirdropRequest>(transport));
        UnitTransportAirdropRequest request = em.GetComponentData<UnitTransportAirdropRequest>(transport);
        Assert.AreEqual(1, request.DropCount);
        Assert.AreEqual(1, request.SoldierDropCount);
        Assert.AreEqual(0, request.VehicleDropCount);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(soldier, passengers[0].Passenger, "Attack payload should be first so the airdrop system releases it before deferred cargo.");
        Assert.AreEqual(unarmedVehicle, passengers[1].Passenger, "Unarmed cargo with no attack payload should stay loaded after the attack payload.");
        Assert.IsTrue(em.HasComponent<UnitTransportDeployAttackTarget>(soldier));
        Assert.IsFalse(em.HasComponent<UnitTransportDeployAttackTarget>(unarmedVehicle));
    }

    [Test]
    public void TransportDeployOrderSystem_AttackDeployHelicopterRopesOnlyAttackPayload()
    {
        using var world = new World("TransportDeployOrderSystem_AttackDeployHelicopterRopesOnlyAttackPayload");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(12, 12), air: true, airborne: true, "Unit_Veh_Helicopter_Transport");
        em.AddComponentData(transport, new UnitMove { Speed = 12f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        Entity unarmedPassenger = CreateLoadedPassenger(em, transport);
        Entity soldier = CreateLoadedPassenger(em, transport);
        AddAttackCapability(em, soldier);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = unarmedPassenger });
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        Entity target = CreateHostileTarget(em, new int2(13, 12));

        em.AddComponentData(transport, new UnitTransportDeployOrder
        {
            TargetEntity = target,
            TargetCell = new int2(13, 12),
            TargetPosition = em.GetComponentData<LocalTransform>(target).Position,
            AttackAfterDeploy = 1
        });

        SystemHandle deploySystem = world.CreateSystem<UnitTransportDeployOrderSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        deploySystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportDeployOrder>(transport));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport));
        UnitTransportRopeDisembarkRequest request = em.GetComponentData<UnitTransportRopeDisembarkRequest>(transport);
        Assert.AreEqual(1, request.TotalDropCount);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(soldier, passengers[0].Passenger, "Attack payload should be first so the rope system releases it before deferred cargo.");
        Assert.AreEqual(unarmedPassenger, passengers[1].Passenger, "Unarmed cargo should stay loaded after the attack payload.");
        Assert.IsTrue(em.HasComponent<UnitTransportDeployAttackTarget>(soldier));
        Assert.IsFalse(em.HasComponent<UnitTransportDeployAttackTarget>(unarmedPassenger));

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        world.SetTime(new TimeData(1.2d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);

        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(unarmedPassenger, passengers[0].Passenger, "The deferred unarmed passenger should remain loaded.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(soldier));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(unarmedPassenger));

        UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(soldier);
        world.SetTime(new TimeData(dropState.StartedAt + dropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        disembarkSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport));
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(unarmedPassenger, passengers[0].Passenger);
    }

    [Test]
    public void TransportDeployAttackSystem_WaitsUntilPassengerSettledBeforeEngage()
    {
        using var world = new World("TransportDeployAttackSystem_WaitsUntilPassengerSettledBeforeEngage");
        EntityManager em = world.EntityManager;
        Entity target = CreateHostileTarget(em, new int2(20, 20));
        Entity passenger = CreateSelectablePassenger(em, new int2(18, 20));
        em.AddComponentData(passenger, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.AddComponentData(passenger, new UnitAttack { Range = 8f, CooldownSeconds = 1f, Damage = 10 });
        em.AddComponentData(passenger, new UnitTransportDeployAttackTarget
        {
            TargetEntity = target,
            TargetCell = new int2(20, 20),
            TargetPosition = em.GetComponentData<LocalTransform>(target).Position
        });
        em.AddComponentData(passenger, new UnitTransportAirdropSettleComponent
        {
            StartPosition = new float3(18.5f, 3f, 20.5f),
            EndPosition = new float3(18.5f, 0f, 20.5f),
            EndCell = new int2(18, 20),
            StartedAt = 0f,
            DurationSeconds = 1f
        });

        SystemHandle deployAttackSystem = world.CreateSystem<UnitTransportDeployAttackSystem>();
        world.SetTime(new TimeData(0.25d, 0.1f));
        deployAttackSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<EngageTarget>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportDeployAttackTarget>(passenger));

        em.RemoveComponent<UnitTransportAirdropSettleComponent>(passenger);
        world.SetTime(new TimeData(1.25d, 0.1f));
        deployAttackSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportDeployAttackTarget>(passenger));
        Assert.IsTrue(em.HasComponent<EngageTarget>(passenger));
        Assert.AreEqual(target, em.GetComponentData<EngageTarget>(passenger).Target);
    }

    [Test]
    public void TransportDeployAttackSystem_NestedCarrierReceivesDeployOrderForArmedPayload()
    {
        using var world = new World("TransportDeployAttackSystem_NestedCarrierReceivesDeployOrderForArmedPayload");
        EntityManager em = world.EntityManager;
        Entity target = CreateHostileTarget(em, new int2(20, 20));
        Entity carrier = CreateTransport(em, new int2(18, 20), air: false, airborne: false);
        em.SetComponentData(carrier, new UnitTransportCapacity { SoldierCapacity = 4 });
        Entity nestedSoldier = CreateLoadedPassenger(em, carrier);
        AddAttackCapability(em, nestedSoldier);
        em.GetBuffer<UnitTransportPassengerElement>(carrier).Add(new UnitTransportPassengerElement { Passenger = nestedSoldier });
        em.AddComponentData(carrier, new UnitTransportDeployAttackTarget
        {
            TargetEntity = target,
            TargetCell = new int2(20, 20),
            TargetPosition = em.GetComponentData<LocalTransform>(target).Position
        });

        SystemHandle deployAttackSystem = world.CreateSystem<UnitTransportDeployAttackSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        deployAttackSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitTransportDeployAttackTarget>(carrier));
        Assert.IsFalse(em.HasComponent<EngageTarget>(carrier), "An unarmed carrier should not attack directly.");
        Assert.IsTrue(em.HasComponent<UnitTransportDeployOrder>(carrier), "An unarmed carrier with armed cargo should continue the deploy attack chain.");
        UnitTransportDeployOrder deployOrder = em.GetComponentData<UnitTransportDeployOrder>(carrier);
        Assert.AreEqual(target, deployOrder.TargetEntity);
        Assert.AreEqual(1, deployOrder.AttackAfterDeploy);
    }

    [Test]
    public void InitialUnitSpawnApplySystem_CopiesTransportAirdropVisualPrefabRefs()
    {
        using var world = new World("InitialUnitSpawnApplySystem_CopiesTransportAirdropVisualPrefabRefs");
        EntityManager em = world.EntityManager;
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "InitialSpawnParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "InitialSpawnCargoVisual");
        Entity prefab = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitSourcePrefabKey),
            typeof(UnitTransportAirdropVisualPrefabs));
        em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });
        em.SetComponentData(prefab, new UnitTransportAirdropVisualPrefabs
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });

        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            _ = new InitialUnitSpawnApplySystem().InstantiateAndConfigureSpawnedUnit(
                em,
                ecb,
                prefab,
                hasPrefab: true,
                faction: FactionIdentity.PlayerFactionId,
                cell: new int2(4, 5),
                pos: new float3(4.5f, 0f, 5.5f));
            ecb.Playback(em);
        }
        finally
        {
            ecb.Dispose();
        }

        using EntityQuery spawnedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitTransportAirdropVisualPrefabs>(),
            ComponentType.Exclude<Prefab>());
        using NativeArray<Entity> spawnedEntities = spawnedQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, spawnedEntities.Length);
        Entity instance = spawnedEntities[0];
        Assert.IsTrue(em.HasComponent<UnitTransportAirdropVisualPrefabs>(instance));
        UnitTransportAirdropVisualPrefabs copied = em.GetComponentData<UnitTransportAirdropVisualPrefabs>(instance);
        Assert.AreEqual(parachutePrefab, copied.SoldierParachuteVisualPrefab);
        Assert.AreEqual(cargoPrefab, copied.VehicleEmergencyDropVisualPrefab);
    }

    [Test]
    public void TransportPlanePureEcs_StaticGuardRejectsManagedRuntimeBridgePatterns()
    {
        string[] runtimeFiles =
        {
            "Assets/Game/Scripts/Components/GridComponents.cs",
            "Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandSystem.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandRouting.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandBoardTransport.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandBoardPassenger.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandPlanning.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandCandidates.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandApproach.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandDisembarkPlanning.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandAirdropPlanning.cs",
            "Assets/Game/Scripts/Systems/TransportBoardingCommandDisembarkApplication.cs",
            "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportPassengerStateSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportPlaneDoorSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportDeployOrderSystem.cs",
            "Assets/Game/Scripts/Systems/UnitTransportDeployAttackSystem.cs"
        };
        string[] forbidden =
        {
            "SystemBase",
            "MonoBehaviour",
            "Object.Instantiate",
            "Object.Destroy",
            "Resources.Load",
            "FindObjectOfType",
            "GameObject.Find",
            "AddComponentObject(entity, new UnitTransport",
            "TransportPlaneManager",
            "AirdropController",
            "TransportFacade"
        };

        for (int i = 0; i < runtimeFiles.Length; i++)
        {
            string source = File.ReadAllText(runtimeFiles[i]);
            for (int j = 0; j < forbidden.Length; j++)
            {
                Assert.IsFalse(
                    source.Contains(forbidden[j], StringComparison.Ordinal),
                    $"{runtimeFiles[i]} must stay pure ECS and must not contain `{forbidden[j]}`.");
            }
        }
    }

    [Test]
    public void GroundPersonnelTransport_BoardOrderCapsAtAvailableSeats()
    {
        using var world = new World("GroundPersonnelTransport_BoardOrderCapsAtAvailableSeats");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(10, 10), air: false, airborne: false);
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 2 });
        Entity loadedPassenger = CreateLoadedPassenger(em, transport);
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = loadedPassenger });

        Entity passengerA = CreateSelectablePassenger(em, new int2(6, 10));
        Entity passengerB = CreateSelectablePassenger(em, new int2(6, 12));
        Entity passengerC = CreateSelectablePassenger(em, new int2(6, 14));

        var boardingSystem = new TransportBoardingCommandSystem();
        TransportBoardingCommandSystem.Result result = boardingSystem.TryRequestBoardTransportOrderToClickedUnit(
            em,
            Vector2.zero,
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper(),
            (Vector2 _screenPosition, EntityManager _em, out Entity entity) =>
            {
                entity = transport;
                return true;
            },
            TryGetNoClickedCell);

        Assert.IsTrue(result.Accepted);
        Assert.AreEqual(1, CountBoardingTargets(em, passengerA, passengerB, passengerC), "Only one free seat remains, so only one selected passenger may receive a boarding order.");
        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "Existing passengers stay loaded while new passengers are ordered.");
    }

    [Test]
    public void BoardTransportCommandSystem_OnUpdateConsumesPreResolvedTransportRequest()
    {
        using var world = new World("BoardTransportCommandSystem_OnUpdateConsumesPreResolvedTransportRequest");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(10, 10), air: false, airborne: false);
        Entity passenger = CreateSelectablePassenger(em, new int2(6, 10));
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            TargetEntity = transport,
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            HasTargetEntity = 1,
            HasScreenPosition = 1
        });

        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        transportCommandSystem.Update(world.Unmanaged);

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardTransport, results[0].Kind);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual(1, CountBoardingTargets(em, passenger), "The resolved board request should produce the same passenger boarding target as the old screen-click command path.");
        AssertBoardingMoveOrder(em, passenger, transport);
    }

    [Test]
    public void BoardTransportCommandSystem_EmptyQueueSteadyStateAllocatesNoManagedMemory()
    {
        using var world = new World("BoardTransportCommandSystem_EmptyQueueAllocationTest");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();

        transportCommandSystem.Update(world.Unmanaged);
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
            transportCommandSystem.Update(world.Unmanaged);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.AreEqual(0L, allocatedBytes, "Warm empty transport command updates must not construct managed routing helpers.");
    }

    [Test]
    public void BoardTransportCommandFlush_ConsumesPreResolvedTransportRequestAndOrdersMove()
    {
        using var world = new World("BoardTransportCommandFlush_ConsumesPreResolvedTransportRequestAndOrdersMove");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(10, 10), air: false, airborne: false);
        Entity passenger = CreateSelectablePassenger(em, new int2(6, 10));
        Entity commandEntity = CreateCommandEntity(em);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            TargetEntity = transport,
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            HasTargetEntity = 1,
            HasScreenPosition = 1
        });

        var transportCommandSystem = new TransportBoardingCommandSystem();
        bool handled = transportCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            new SelectionStateCompositionSystemHelper(),
            TryGetNoClickedUnit,
            TryGetNoClickedCell);

        Assert.IsTrue(handled);
        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardTransport, results[0].Kind);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        AssertBoardingMoveOrder(em, passenger, transport);
    }

    [Test]
    public void BoardAllSelectedTransportCommand_ConsumesRequestAndOrdersNearestSoldiers()
    {
        using var world = new World("BoardAllSelectedTransportCommand_ConsumesRequestAndOrdersNearestSoldiers");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(10, 10), air: false, airborne: false);
        Entity nearSoldier = CreateBoardAllSoldier(em, new int2(7, 10));
        Entity farSoldier = CreateBoardAllSoldier(em, new int2(7, 13));
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardAllSelectedTransport,
            RequestId = 17,
            Frame = 90
        });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(transport);
        var transportCommandSystem = new TransportBoardingCommandSystem();

        bool handled = transportCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            selectionState,
            TryGetNoClickedUnit,
            TryGetNoClickedCell);

        Assert.IsTrue(handled);
        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardAllSelectedTransport, results[0].Kind);
        Assert.AreEqual(17, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual("Boarding 2 units.", results[0].Message.ToString());
        Assert.AreEqual(2, CountBoardingTargets(em, nearSoldier, farSoldier));
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(nearSoldier).Transport);
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(farSoldier).Transport);
    }

    [Test]
    public void BoardAllSelectedTransportCommand_RetryRefreshesPendingPassengersForSameTransport()
    {
        using var world = new World("BoardAllSelectedTransportCommand_RetryRefreshesPendingPassengersForSameTransport");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        GridConfig grid = em.GetComponentData<GridConfig>(GetGridEntity(em));
        int2 rampCell = TransportBoardingCommandSystem.ResolvePlaneRampApproachCell(em, grid, transport);
        int2 passengerCellA = rampCell;
        int2 passengerCellB = rampCell + new int2(1, 0);
        int2 passengerCellC = rampCell + new int2(-1, 0);
        Entity passengerA = CreateBoardAllSoldier(em, passengerCellA);
        Entity passengerB = CreateBoardAllSoldier(em, passengerCellB);
        Entity passengerC = CreateBoardAllSoldier(em, passengerCellC);
        int2[] allowedApproachCells = { passengerCellA, passengerCellB, passengerCellC };
        for (int y = rampCell.y - 8; y <= rampCell.y + 8; y++)
        {
            for (int x = rampCell.x - 8; x <= rampCell.x + 8; x++)
            {
                int2 cell = new(x, y);
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    continue;

                bool allowed = false;
                for (int i = 0; i < allowedApproachCells.Length; i++)
                    allowed |= allowedApproachCells[i].Equals(cell);
                if (!allowed)
                    _blocked.Set(GridUtils.CellToIndex(cell, grid.Width), true);
            }
        }

        for (int i = 0; i < allowedApproachCells.Length; i++)
            _occupied.Set(GridUtils.CellToIndex(allowedApproachCells[i], grid.Width), true);

        int2 staleGoal = new(1, 1);
        em.AddComponentData(passengerA, new UnitTransportBoardingTarget { Transport = transport, Goal = staleGoal });
        em.AddComponentData(passengerB, new UnitTransportBoardingTarget { Transport = transport, Goal = staleGoal + new int2(1, 0) });
        em.AddComponentData(passengerC, new UnitTransportBoardingTarget { Transport = transport, Goal = staleGoal + new int2(2, 0) });

        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardAllSelectedTransport,
            RequestId = 170,
            Frame = 900
        });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(transport);
        var transportCommandSystem = new TransportBoardingCommandSystem();

        bool handled = transportCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            selectionState,
            TryGetNoClickedUnit,
            TryGetNoClickedCell);

        Assert.IsTrue(handled);
        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardAllSelectedTransport, results[0].Kind);
        Assert.AreEqual(170, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Boarding 3 units.", results[0].Message.ToString());
        Assert.AreEqual(3, CountBoardingTargets(em, passengerA, passengerB, passengerC));
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(passengerA).Transport);
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(passengerB).Transport);
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(passengerC).Transport);
        Assert.AreNotEqual(staleGoal, em.GetComponentData<UnitTransportBoardingTarget>(passengerA).Goal);
        Assert.AreNotEqual(staleGoal + new int2(1, 0), em.GetComponentData<UnitTransportBoardingTarget>(passengerB).Goal);
        Assert.AreNotEqual(staleGoal + new int2(2, 0), em.GetComponentData<UnitTransportBoardingTarget>(passengerC).Goal);
    }

    [Test]
    public void BoardAllSelectedTransportCommand_IgnoresDistantPassengers()
    {
        using var world = new World("BoardAllSelectedTransportCommand_IgnoresDistantPassengers");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 128, 128);

        Entity transport = CreateTransport(em, new int2(10, 10), air: false, airborne: false);
        Entity nearSoldier = CreateBoardAllSoldier(em, new int2(7, 10));
        Entity distantSoldier = CreateBoardAllSoldier(em, new int2(80, 80));
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardAllSelectedTransport,
            RequestId = 18,
            Frame = 91
        });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(transport);
        var transportCommandSystem = new TransportBoardingCommandSystem();

        bool handled = transportCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            selectionState,
            TryGetNoClickedUnit,
            TryGetNoClickedCell);

        Assert.IsTrue(handled);
        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardAllSelectedTransport, results[0].Kind);
        Assert.AreEqual(18, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual("Boarding 1 unit.", results[0].Message.ToString());
        Assert.AreEqual(1, CountBoardingTargets(em, nearSoldier, distantSoldier));
        Assert.AreEqual(transport, em.GetComponentData<UnitTransportBoardingTarget>(nearSoldier).Transport);
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(distantSoldier));
    }

    [Test]
    public void AirTransport_DoesNotBoardSoldierUntilLanded()
    {
        using var world = new World("AirTransport_DoesNotBoardSoldierUntilLanded");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: true, airborne: true);
        Entity passenger = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));

        UnitAirComponent landedState = em.GetComponentData<UnitAirComponent>(transport);
        landedState.Airborne = 0;
        landedState.ReturningHome = 0;
        em.SetComponentData(transport, landedState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(5.5f, 0f, 5.5f)));

        world.SetTime(new TimeData(2d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransport_BoardsWhenLandedOnRaisedHelipad()
    {
        using var world = new World("AirTransport_BoardsWhenLandedOnRaisedHelipad");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 2.25f, 8.5f)));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(new float3(8.5f, 2.25f, 8.5f)) });

        Entity passenger = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A helicopter visibly landed on a raised helipad should accept nearby soldiers.");
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void TransportBoardingSystem_DuplicatePassengerEntriesConsumeCapacityPerEntry()
    {
        using var world = new World("TransportBoardingSystem_DuplicatePassengerEntries");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 2 });
        Entity loadedPassenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = loadedPassenger });
        passengers.Add(new UnitTransportPassengerElement { Passenger = loadedPassenger });
        Entity candidate = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> currentPassengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(2, currentPassengers.Length, "Current occupancy counts each live buffer entry, including duplicate entity entries.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(candidate));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(candidate),
            "A duplicate entry can consume the final seat and causes the pending boarding target to be cancelled.");
    }

    [Test]
    public void TransportBoardingSystem_IgnoresMissingPassengerEntitiesWhenCountingOccupancy()
    {
        using var world = new World("TransportBoardingSystem_MissingPassengerEntries");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 1 });
        Entity destroyedPassenger = em.CreateEntity();
        em.DestroyEntity(destroyedPassenger);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = Entity.Null });
        passengers.Add(new UnitTransportPassengerElement { Passenger = destroyedPassenger });
        Entity candidate = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> currentPassengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(candidate));
        Assert.IsTrue(em.HasComponent<Disabled>(candidate));
        Assert.AreEqual(3, currentPassengers.Length);
        Assert.AreEqual(Entity.Null, currentPassengers[0].Passenger);
        Assert.AreEqual(destroyedPassenger, currentPassengers[1].Passenger);
        Assert.IsTrue(TransportPassengerBufferContains(currentPassengers, candidate),
            "Null and destroyed passenger entries are retained but ignored by occupancy counting.");
    }

    [Test]
    public void TransportBoardingSystem_VehicleRequiresCargoCapacity()
    {
        using var world = new World("TransportBoardingSystem_VehicleRequiresCargoCapacity");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity vehicle = CreateVehiclePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(vehicle),
            "Vehicle capacity is zero when UnitTransportCargoCapacity is absent, regardless of legacy soldier capacity.");
    }

    [Test]
    public void TransportBoardingSystem_CargoSoldierCapacityOverridesLegacyCapacity()
    {
        using var world = new World("TransportBoardingSystem_CargoSoldierCapacityOverride");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 4 });
        em.AddComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 1,
            VehicleCapacity = 0,
            CargoWeightCapacity = 0
        });
        Entity loadedPassenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = loadedPassenger });
        Entity candidate = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(candidate));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(candidate),
            "A positive cargo soldier capacity overrides the legacy UnitTransportCapacity value.");
    }

    [Test]
    public void TransportBoardingSystem_BoardingTargetMetadataClassifiesVehicleOccupancy()
    {
        using var world = new World("TransportBoardingSystem_BoardingTargetKindFallback");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        em.AddComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 0,
            VehicleCapacity = 1,
            CargoWeightCapacity = 0
        });
        Entity reservedVehicle = em.CreateEntity(typeof(UnitTransportBoardingTarget));
        em.SetComponentData(reservedVehicle, new UnitTransportBoardingTarget
        {
            Transport = transport,
            Goal = new int2(6, 5),
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = reservedVehicle });
        Entity candidate = CreateVehiclePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(candidate));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(candidate),
            "When cargo metadata is absent, matching boarding-target metadata classifies a live buffered entity as vehicle occupancy.");
    }

    [Test]
    public void TransportBoardingSystem_MultipleGridConfigsRejectAmbiguousSingleton()
    {
        using var world = new World("TransportBoardingSystem_MultipleGridConfigs");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 12, 12);
        Entity duplicateGrid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(duplicateGrid, new GridConfig { Width = 12, Height = 12, CellSize = 1f, Origin = float3.zero });
        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity passenger = CreatePassenger(em, new int2(6, 5), transport, new int2(6, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));

        void UpdateBoardingSystemDirectly()
        {
            world.Unmanaged.GetUnsafeSystemRef<UnitTransportBoardingSystem>(boardingSystem)
                .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(boardingSystem));
        }

        Assert.Throws<InvalidOperationException>(UpdateBoardingSystemDirectly,
            "UnitTransportBoardingSystem requires exactly one GridConfig before processing passengers.");

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger),
            "Ambiguous GridConfig state aborts the update before the current boarding target is consumed.");
    }

    [Test]
    public void AirTransport_DoesNotBoardAtOldWideClearanceDistance()
    {
        using var world = new World("AirTransport_DoesNotBoardAtOldWideClearanceDistance");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreatePassenger(em, new int2(13, 8), transport, new int2(13, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier outside the tightened helicopter boarding clearance must keep walking instead of boarding from far away.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardWhenStoppedOneCellShortOfCloseGoal()
    {
        using var world = new World("AirTransport_DoesNotBoardWhenStoppedOneCellShortOfCloseGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        Entity passenger = CreatePassenger(em, new int2(10, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A helicopter passenger must reach the close boarding goal instead of boarding from the old two-cell fallback distance.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_DoesNotBoardAtFarEdgeOfLargeHelicopterFootprint()
    {
        using var world = new World("AirTransport_DoesNotBoardAtFarEdgeOfLargeHelicopterFootprint");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransport(em, new int2(20, 20), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(17, 21) });
        Entity passenger = CreatePassenger(em, new int2(28, 30), transport, new int2(28, 30));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier at the far edge of a large helicopter footprint must not board until it reaches the compact center boarding area.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransportPickup_ClickingFlyingHelicopterCommandsLandingNearPassengerBeforeBoarding()
    {
        using var world = new World("AirTransportPickup_ClickingFlyingHelicopterCommandsLandingNearPassengerBeforeBoarding");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(4, 4), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        UnitAirComponent visuallyFlyingAirState = em.GetComponentData<UnitAirComponent>(transport);
        visuallyFlyingAirState.Airborne = 0;
        visuallyFlyingAirState.HomeInitialized = 1;
        visuallyFlyingAirState.HomePosition = new float3(4.5f, 0f, 4.5f);
        em.SetComponentData(transport, visuallyFlyingAirState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(4.5f, 8f, 4.5f)));
        Entity passenger = CreatePassenger(em, new int2(16, 16), transport, new int2(16, 16));
        Entity gridEntity = GetGridEntity(em);
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
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
            walkable.AsNativeArray(),
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

        Assert.IsTrue(prepared, "Clicking a flying transport helicopter with a selected soldier should command a pickup landing.");
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passenger).Cell, pickupCell, "The helicopter must land on a free nearby cell, not on top of the soldier.");
        Assert.LessOrEqual(
            math.max(math.abs(pickupCell.x - em.GetComponentData<UnitGrid>(passenger).Cell.x), math.abs(pickupCell.y - em.GetComponentData<UnitGrid>(passenger).Cell.y)),
            10,
            "The pickup landing should stay near the selected soldier.");
        Assert.IsTrue(em.HasComponent<UnitTarget>(transport));
        Assert.AreEqual(pickupCell, em.GetComponentData<UnitTarget>(transport).Cell);
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        Assert.AreEqual(1, airState.Airborne, "A helicopter that is physically above the ground must be marked airborne for pickup landing, even if stale flags said grounded.");
        Assert.AreEqual(pickupCell, airState.HomeCell);

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "The soldier must not board while the helicopter is still airborne and moving to the pickup landing.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
    }

    [Test]
    public void AirTransportPickup_FindingLandingCellDoesNotInvalidateGridArrays()
    {
        using var world = new World("AirTransportPickup_FindingLandingCellDoesNotInvalidateGridArrays");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(4, 4), air: true, airborne: true, "Unit_Veh_Helicopter_Transport");
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(1, 1) });
        Entity passenger = CreatePassenger(em, new int2(16, 16), transport, new int2(16, 16));
        Entity gridEntity = GetGridEntity(em);
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkableBuffer = em.GetBuffer<GridWalkable>(gridEntity);
        NativeArray<GridWalkable> walkable = walkableBuffer.AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancyData = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);

        using EntityQuery liveUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using NativeArray<Entity> liveEntities = liveUnitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveGrids = liveUnitQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveFootprints = liveUnitQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        var airPickupSystem = new UnitTransportAirPickupSystem();
        bool found = airPickupSystem.TryFindAirTransportPickupForBoarding(
            em,
            transport,
            grid,
            walkable,
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
            out _);

        Assert.IsTrue(found, "Finding an airborne pickup landing cell should succeed.");
        Assert.IsFalse(em.HasComponent<UnitTarget>(transport), "Finding the pickup cell must not make structural ECS changes while grid NativeArrays are still in use.");
        Assert.AreEqual(1, walkable[GridUtils.CellToIndex(new int2(0, 0), grid.Width)].Value, "The held GridWalkable NativeArray should remain valid after pickup-cell search.");
    }

    [Test]
    public void AirTransport_DoesNotBoardWhenAirFlagsGroundedButModelStillFlying()
    {
        using var world = new World("AirTransport_DoesNotBoardWhenAirFlagsGroundedButModelStillFlying");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 8f, 8.5f)));

        Entity passenger = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A soldier must not board while the helicopter model is still visibly flying, even if stale air flags say grounded.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void AirTransport_BoardsAllPassengersThatReachedCloseHelicopterGoals()
    {
        using var world = new World("AirTransport_BoardsAllPassengersThatReachedCloseHelicopterGoals");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 20, 20);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passengerA = CreatePassenger(em, new int2(7, 8), transport, new int2(7, 8));
        Entity passengerB = CreatePassenger(em, new int2(9, 8), transport, new int2(9, 8));
        Entity passengerC = CreatePassenger(em, new int2(8, 7), transport, new int2(8, 7));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(3, passengers.Length, "Every passenger that reached a valid close helicopter boarding goal should board in the same update.");
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerA));
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerB));
        Assert.IsTrue(TransportPassengerBufferContains(passengers, passengerC));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerA));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerB));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerC));
    }

    [Test]
    public void TransportPlane_BoardsPassengerBesideRearRampGoalWithStaleMovementState()
    {
        using var world = new World("TransportPlane_BoardsPassengerBesideRearRampGoalWithStaleMovementState");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 30, 30);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        int2 rampGoal = new(12, 2);
        Entity passenger = CreatePassenger(em, new int2(13, 2), transport, rampGoal);
        em.AddComponentData(passenger, new UnitTarget { Cell = rampGoal });
        em.AddComponentData(passenger, new UnitPathRequest { Goal = rampGoal });
        em.AddComponentData(passenger, new UnitPathFollow { PathIndex = 0 });

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "A cargo-plane soldier beside the rear-ramp goal should board even if stale movement components have not been cleaned up yet.");
        Assert.AreEqual(passenger, passengers[0].Passenger);
        Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsTrue(em.HasComponent<Disabled>(passenger));
        Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passenger));
    }

    [Test]
    public void TransportPlane_BoardsClusterAroundResolvedRearRampWithStaleMovementState()
    {
        using var world = new World("TransportPlane_BoardsClusterAroundResolvedRearRampWithStaleMovementState");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 40, 40);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        GridConfig grid = em.GetComponentData<GridConfig>(GetGridEntity(em));
        int2 rampCell = TransportBoardingCommandSystem.ResolvePlaneRampApproachCell(em, grid, transport);
        int2[] passengerCells =
        {
            rampCell,
            rampCell + new int2(1, 0),
            rampCell + new int2(-1, 0),
            rampCell + new int2(0, 1),
            rampCell + new int2(0, -1),
            rampCell + new int2(2, 0),
            rampCell + new int2(-2, 0),
            rampCell + new int2(1, 1)
        };
        Entity[] passengers = new Entity[passengerCells.Length];
        for (int i = 0; i < passengerCells.Length; i++)
        {
            int2 staleReservedGoal = new int2(2 + i, 2);
            passengers[i] = CreatePassenger(em, passengerCells[i], transport, staleReservedGoal);
            em.AddComponentData(passengers[i], new UnitTarget { Cell = staleReservedGoal });
            em.AddComponentData(passengers[i], new UnitPathRequest { Goal = staleReservedGoal });
            em.AddComponentData(passengers[i], new UnitPathFollow { PathIndex = 0 });
        }

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        DynamicBuffer<UnitTransportPassengerElement> loadedPassengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(passengers.Length, loadedPassengers.Length, "Every soldier clustered around the cargo plane rear ramp should board in the same update, even when stale movement components remain.");
        for (int i = 0; i < passengers.Length; i++)
        {
            Assert.IsTrue(TransportPassengerBufferContains(loadedPassengers, passengers[i]));
            Assert.IsTrue(em.HasComponent<UnitTransportPassenger>(passengers[i]));
            Assert.IsTrue(em.HasComponent<Disabled>(passengers[i]));
            Assert.IsFalse(em.HasComponent<UnitTransportBoardingTarget>(passengers[i]));
        }
    }

    [Test]
    public void Transport_DoesNotBoardPassengerThatOnlyReachedFarBoardingGoal()
    {
        using var world = new World("Transport_DoesNotBoardPassengerThatOnlyReachedFarBoardingGoal");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 32, 32);

        Entity transport = CreateTransport(em, new int2(5, 5), air: false, airborne: false);
        Entity passenger = CreatePassenger(em, new int2(20, 5), transport, new int2(20, 5));

        SystemHandle boardingSystem = world.CreateSystem<UnitTransportBoardingSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        boardingSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "A passenger must not board just because it reached a stale/far boarding goal.");
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
        Assert.IsFalse(em.HasComponent<Disabled>(passenger));
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger), "The order should remain active until the passenger actually reaches the transport clearance.");
    }

    [Test]
    public void HelicopterRopeDisembark_ReleasesPassengersOneAtATime()
    {
        using var world = new World("HelicopterRopeDisembark_ReleasesPassengersOneAtATime");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: true);
        Entity passengerA = CreateLoadedPassenger(em, transport);
        Entity passengerB = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerA });
        passengers.Add(new UnitTransportPassengerElement { Passenger = passengerB });
        em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
        {
            ReferenceCell = new int2(8, 8),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        });

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();

        world.SetTime(new TimeData(1d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);

        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "Only one passenger should leave per rope interval.");
        Assert.IsFalse(em.HasComponent<Disabled>(passengerA));
        Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passengerA));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passengerA));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA));
        Assert.IsTrue(em.HasComponent<Disabled>(passengerB));
        float3 firstStart = em.GetComponentData<LocalTransform>(passengerA).Position;
        Assert.Greater(firstStart.y, 1f);

        world.SetTime(new TimeData(1.4d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "The second passenger must wait for the configured drop interval.");

        world.SetTime(new TimeData(2d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(1, passengers.Length, "The second passenger must not start descending before the first passenger has reached the ground.");
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passengerB));

        UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passengerA);
        world.SetTime(new TimeData(dropState.StartedAt + dropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDropComponent>(passengerA));
        Assert.That(em.GetComponentData<LocalTransform>(passengerA).Position.y, Is.EqualTo(dropState.EndPosition.y).Within(0.001f));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA), "A passenger should receive a direct free-cell disperse after reaching the ground.");
        Assert.IsFalse(em.HasComponent<UnitTarget>(passengerA), "Rope exit disperse must not depend on pathfinding in the tight landing area.");
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(passengerA));
        Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passengerA).IsMoving);
        UnitTransportRopeDisperseComponent passengerADisperse = em.GetComponentData<UnitTransportRopeDisperseComponent>(passengerA);
        int2 passengerATarget = passengerADisperse.EndCell;
        Assert.AreNotEqual(em.GetComponentData<UnitGrid>(passengerA).Cell, passengerATarget);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA), "Starting the move-away should immediately free the rope landing slot for the next passenger.");
        Assert.LessOrEqual(
            math.max(
                math.abs(passengerATarget.x - em.GetComponentData<UnitGrid>(passengerA).Cell.x),
                math.abs(passengerATarget.y - em.GetComponentData<UnitGrid>(passengerA).Cell.y)),
            12,
            "The post-rope move-away target should remain near the landing point.");

        world.SetTime(new TimeData(2.6d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        Assert.AreEqual(0, passengers.Length, "The second passenger should start once the previous passenger has started moving away, without waiting for the full disperse move.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "The helicopter should keep the rope request active while the second passenger is descending.");
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA), "The first passenger can still be moving away while the second starts descending.");
        Assert.IsFalse(em.HasComponent<Disabled>(passengerB));
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passengerB));

        world.SetTime(new TimeData(passengerADisperse.StartedAt + passengerADisperse.DurationSeconds + 0.1f, 0.1f));
        disperseSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerA));
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerA));
        Assert.AreEqual(passengerATarget, em.GetComponentData<UnitGrid>(passengerA).Cell);

        UnitTransportRopeDropComponent passengerBDropState = em.GetComponentData<UnitTransportRopeDropComponent>(passengerB);
        world.SetTime(new TimeData(passengerBDropState.StartedAt + passengerBDropState.DurationSeconds + 0.1f, 0.1f));
        dropSystem.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passengerB), "Each passenger should receive a direct free-cell disperse after landing.");
        UnitTransportRopeDisperseComponent passengerBDisperse = em.GetComponentData<UnitTransportRopeDisperseComponent>(passengerB);
        int2 passengerBTarget = passengerBDisperse.EndCell;
        Assert.AreNotEqual(passengerATarget, passengerBTarget, "Consecutive rope exits should target different free cells instead of stacking on one target.");
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerB), "The final passenger should also free the rope landing slot as soon as it starts moving away.");

        world.SetTime(new TimeData(passengerBDisperse.StartedAt + passengerBDisperse.DurationSeconds + 0.1f, 0.1f));
        disperseSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passengerB));
        Assert.AreEqual(passengerBTarget, em.GetComponentData<UnitGrid>(passengerB).Cell);

        world.SetTime(new TimeData(3.0d, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "The helicopter may finish disembark only after the last passenger leaves the rope landing cell.");
    }

    [Test]
    public void HelicopterRopeDisembark_DropsStraightDownOntoFreeLandingCell()
    {
        using var world = new World("HelicopterRopeDisembark_DropsStraightDownOntoFreeLandingCell");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);
        int2 elevatedTentLikeCell = new(6, 6);
        BlobAssetReference<MapSurfaceBlob> surfaceBlob = CreateFlatSurfaceWithElevatedCell(new int2(24, 24), elevatedTentLikeCell, 3.25f);
        try
        {
            AddMapSurface(em, surfaceBlob, new int2(24, 24));

            Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: true);
            em.AddComponentData(transport, new UnitModelLocalTransform
            {
                Position = new float3(3f, 0f, -2f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            Entity passenger = CreateLoadedPassenger(em, transport);
            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
            em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
            {
                ReferenceCell = new int2(12, 8),
                NextDropAt = 0f,
                DropIntervalSeconds = 0.8f
            });

            SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
            SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
            UnitTransportRopeLandingClearance clearance = em.GetComponentData<UnitTransportRopeLandingClearance>(passenger);
            LocalTransform transportAfterDropStart = em.GetComponentData<LocalTransform>(transport);
            float3 ropeAnchorAfterReposition = transportAfterDropStart.Position + new float3(3f, 0f, -2f);
            Assert.That(ropeAnchorAfterReposition.x, Is.EqualTo(dropState.EndPosition.x).Within(0.001f), "Helicopter rope anchor must be above the selected free landing cell in X.");
            Assert.That(ropeAnchorAfterReposition.z, Is.EqualTo(dropState.EndPosition.z).Within(0.001f), "Helicopter rope anchor must be above the selected free landing cell in Z.");
            Assert.That(dropState.EndPosition.x, Is.EqualTo(dropState.StartPosition.x).Within(0.001f), "Rope drop must stay vertical in X.");
            Assert.That(dropState.EndPosition.z, Is.EqualTo(dropState.StartPosition.z).Within(0.001f), "Rope drop must stay vertical in Z.");
            Assert.That(dropState.EndPosition.y, Is.LessThan(dropState.StartPosition.y), "Rope drop must descend to the ground.");
            Assert.AreNotEqual(elevatedTentLikeCell, clearance.LandingCell, "Rope drop must reject an elevated scenery-like surface even if the grid blocker buffer does not mark it blocked.");
            Assert.That(dropState.EndPosition.y, Is.EqualTo(0f).Within(0.001f), "Rope landing should resolve to terrain height, not to the elevated scenery surface.");
            Assert.IsFalse(_blocked.IsSet(GridUtils.CellToIndex(clearance.LandingCell, 24)), "Rope landing clearance must reserve an unblocked cell.");
            Assert.AreEqual(clearance.LandingCell, em.GetComponentData<UnitGrid>(passenger).Cell, "Passenger grid cell should match the reserved landing cell.");

            world.SetTime(new TimeData(dropState.StartedAt + dropState.DurationSeconds + 0.1f, 0.1f));
            dropSystem.Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger));
            UnitTransportRopeDisperseComponent disperseState = em.GetComponentData<UnitTransportRopeDisperseComponent>(passenger);
            int2 disperseTarget = disperseState.EndCell;
            int2 landingCell = GridUtils.WorldToCell(em.GetComponentData<GridConfig>(GetGridEntity(em)), dropState.EndPosition);
            Assert.AreNotEqual(landingCell, disperseTarget, "The post-landing move target should give the passenger somewhere to move away from the rope.");
            Assert.LessOrEqual(
                math.max(math.abs(disperseTarget.x - landingCell.x), math.abs(disperseTarget.y - landingCell.y)),
                12,
                "The post-landing move target should stay near the rope landing cell.");
            Assert.IsFalse(em.HasComponent<UnitPathRequest>(passenger));
            Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passenger).IsMoving);
        }
        finally
        {
            if (surfaceBlob.IsCreated)
                surfaceBlob.Dispose();
        }
    }

    [Test]
    public void HelicopterRopeDisembark_TenPassengersDisperseToDistinctFreeCells()
    {
        using var world = new World("HelicopterRopeDisembark_TenPassengersDisperseToDistinctFreeCells");
        EntityManager em = world.EntityManager;
        const int width = 40;
        CreateGrid(em, width, 40);

        Entity transport = CreateTransport(em, new int2(20, 20), air: true, airborne: true);
        Entity[] passengersToDrop = new Entity[10];
        for (int i = 0; i < passengersToDrop.Length; i++)
            passengersToDrop[i] = CreateLoadedPassenger(em, transport);

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengersToDrop.Length; i++)
            passengers.Add(new UnitTransportPassengerElement { Passenger = passengersToDrop[i] });

        em.AddComponentData(transport, new UnitTransportRopeDisembarkRequest
        {
            ReferenceCell = new int2(20, 20),
            NextDropAt = 0f,
            DropIntervalSeconds = 0.1f
        });

        SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
        SystemHandle dropSystem = world.CreateSystem<UnitTransportRopeDropSystem>();
        SystemHandle disperseSystem = world.CreateSystem<UnitTransportRopeDisperseSystem>();
        HashSet<int> disperseTargets = new();
        double time = 1d;

        for (int i = 0; i < passengersToDrop.Length; i++)
        {
            Entity passenger = passengersToDrop[i];
            world.SetTime(new TimeData(time, 0.1f));
            disembarkSystem.Update(world.Unmanaged);
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger), $"Passenger {i} should start a rope drop.");

            UnitTransportRopeDropComponent dropState = em.GetComponentData<UnitTransportRopeDropComponent>(passenger);
            time = dropState.StartedAt + dropState.DurationSeconds + 0.1f;
            world.SetTime(new TimeData(time, 0.1f));
            dropSystem.Update(world.Unmanaged);

            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisperseComponent>(passenger), $"Passenger {i} should start moving away after landing.");
            UnitTransportRopeDisperseComponent disperseState = em.GetComponentData<UnitTransportRopeDisperseComponent>(passenger);
            Assert.IsTrue(
                disperseTargets.Add(GridUtils.CellToIndex(disperseState.EndCell, width)),
                $"Passenger {i} should get a unique free disperse cell instead of stacking on another exited soldier.");
            Assert.AreEqual(1, em.GetComponentData<UnitMoveVisualComponent>(passenger).IsMoving, $"Passenger {i} should use the run/move visual while dispersing.");

            time = disperseState.StartedAt + disperseState.DurationSeconds + 0.1f;
            world.SetTime(new TimeData(time, 0.1f));
            disperseSystem.Update(world.Unmanaged);

            Assert.IsFalse(em.HasComponent<UnitTransportRopeLandingClearance>(passenger), $"Passenger {i} should clear the rope landing cell after moving away.");
            Assert.AreEqual(disperseState.EndCell, em.GetComponentData<UnitGrid>(passenger).Cell);
            time += 0.1d;
        }

        world.SetTime(new TimeData(time, 0.1f));
        disembarkSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport));
    }

    [Test]
    public void FocusedTransportExitButton_StartsRopeDisembarkWithoutLosingPassenger()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("FocusedTransportExitButton_StartsRopeDisembarkWithoutLosingPassenger");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        Entity passenger = CreateLoadedPassenger(em, transport);
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });

        try
        {
            Entity queue = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            em.AddBuffer<RtsSelectionCommandResultElement>(queue);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                HasTargetEntity = 1
            });

            SystemHandle transportCommandSystem = world.CreateSystem<TransportBoardingCommandSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            transportCommandSystem.Update(world.Unmanaged);
            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(queue);
            DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(queue);

            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(1, results[0].Accepted);
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDisembarkRequest>(transport), "Exit button should start the rope disembark flow for transport helicopters.");
            Assert.AreEqual(1, em.GetBuffer<UnitTransportPassengerElement>(transport).Length, "Passenger must remain in the helicopter buffer until the rope system drops it.");

            SystemHandle disembarkSystem = world.CreateSystem<UnitTransportRopeDisembarkSystem>();
            world.SetTime(new TimeData(1d, 0.1f));
            disembarkSystem.Update(world.Unmanaged);

            Assert.AreEqual(0, em.GetBuffer<UnitTransportPassengerElement>(transport).Length);
            Assert.IsFalse(em.HasComponent<Disabled>(passenger));
            Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(passenger));
            Assert.IsTrue(em.HasComponent<UnitTransportRopeDropComponent>(passenger));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void SelectionFallback_FindsNearbyTransportHelicopterWhenHelipadCellWasClicked()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("SelectionFallback_FindsNearbyTransportHelicopterWhenHelipadCellWasClicked");
        World.DefaultGameObjectInjectionWorld = world;
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: true, airborne: false, "Unit_Veh_Helicopter_Transport");
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomeInitialized = 1;
        airState.HomePosition = new float3(8.5f, 0f, 8.5f);
        airState.Airborne = 0;
        em.SetComponentData(transport, airState);
        em.SetComponentData(transport, LocalTransform.FromPosition(new float3(8.5f, 2.25f, 8.5f)));
        em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(new float3(8.5f, 2.25f, 8.5f)) });
        try
        {
            var transportCommandSystem = new TransportBoardingCommandSystem();
            bool found = transportCommandSystem.IsBoardablePlayerTransportClick(
                em,
                Vector2.zero,
                TryGetNoClickedUnit,
                TryGetNearbyHelipadCell);

            Assert.IsTrue(found, "Clicking the helipad/ground beside the landed transport helicopter should still resolve the boardable helicopter.");
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void FocusedTransportReadModel_PublishesPassengerCapacityAndRows()
    {
        using var world = new World("FocusedTransportReadModel_PublishesPassengerCapacityAndRows");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 16, 16);

        Entity transport = CreateTransport(em, new int2(8, 8), air: false, airborne: false);
        Entity passenger = CreateLoadedPassenger(em, transport);
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 4 });
        em.AddComponentData(passenger, new UnitHealth { Current = 7, Max = 10 });
        em.AddComponentData(passenger, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes("Rifle Passenger"),
            Description = new FixedString128Bytes("Passenger")
        });
        em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = passenger });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(transport);
        var readModelSystem = new FocusedUnitUiReadModelUiSystemHelper();
        readModelSystem.Publish(
            em,
            selectionState,
            new SelectionUiReadModelLookup(),
            new UnitTransportCapacitySystem(),
            1f);

        Assert.IsTrue(readModelSystem.TryRead(em, out FocusedUnitUiReadModelComponent model, out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers));
        Assert.AreEqual(transport, model.FocusedUnit);
        Assert.AreEqual(1, model.PassengerCount);
        Assert.AreEqual(4, model.TransportPassengerCapacity);
        Assert.AreEqual(1, model.TransportSoldierPassengerCount);
        Assert.AreEqual(4, model.TransportSoldierPassengerCapacity);
        Assert.AreEqual(0, model.TransportVehiclePassengerCount);
        Assert.AreEqual(0, model.TransportVehiclePassengerCapacity);
        Assert.AreEqual(1, passengers.Length);
        Assert.AreEqual(passenger, passengers[0].Passenger);
        Assert.AreEqual("Rifle Passenger", passengers[0].DisplayName.ToString());
        Assert.AreEqual(7, passengers[0].HealthCurrent);
        Assert.AreEqual(10, passengers[0].HealthMax);
    }

    [Test]
    public void FocusedTransportReadModel_PublishesPlaneCargoCapacityBreakdown()
    {
        using var world = new World("FocusedTransportReadModel_PublishesPlaneCargoCapacityBreakdown");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);

        Entity transport = CreateTransportPlane(em, new int2(12, 12));
        Entity soldier = CreateLoadedPassenger(em, transport);
        Entity vehicle = CreateLoadedVehiclePassenger(em, transport);
        em.AddComponentData(soldier, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes("Rifle Passenger"),
            Description = new FixedString128Bytes("Passenger")
        });
        em.AddComponentData(vehicle, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes("Tank Cargo"),
            Description = new FixedString128Bytes("Vehicle cargo")
        });
        DynamicBuffer<UnitTransportPassengerElement> transportPassengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        transportPassengers.Add(new UnitTransportPassengerElement { Passenger = soldier });
        transportPassengers.Add(new UnitTransportPassengerElement { Passenger = vehicle });

        var selectionState = new SelectionStateCompositionSystemHelper();
        selectionState.SetFocusedUnit(transport);
        var readModelSystem = new FocusedUnitUiReadModelUiSystemHelper();
        readModelSystem.Publish(
            em,
            selectionState,
            new SelectionUiReadModelLookup(),
            new UnitTransportCapacitySystem(),
            1f);

        Assert.IsTrue(readModelSystem.TryRead(em, out FocusedUnitUiReadModelComponent model, out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers));
        Assert.AreEqual(transport, model.FocusedUnit);
        Assert.AreEqual(2, model.PassengerCount);
        Assert.AreEqual(26, model.TransportPassengerCapacity);
        Assert.AreEqual(1, model.TransportSoldierPassengerCount);
        Assert.AreEqual(24, model.TransportSoldierPassengerCapacity);
        Assert.AreEqual(1, model.TransportVehiclePassengerCount);
        Assert.AreEqual(2, model.TransportVehiclePassengerCapacity);
        Assert.AreEqual(2, passengers.Length);
        Assert.AreEqual(soldier, passengers[0].Passenger);
        Assert.AreEqual(vehicle, passengers[1].Passenger);
        Assert.AreEqual("Rifle Passenger", passengers[0].DisplayName.ToString());
        Assert.AreEqual("Tank Cargo", passengers[1].DisplayName.ToString());
    }

    private static bool TryGetNoClickedUnit(Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static Entity CreateCommandEntity(EntityManager em)
    {
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        return commandEntity;
    }

    private static bool TryGetNoClickedCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;
        return false;
    }

    private static void AssertVisualTracksPassenger(
        EntityManager em,
        Entity passenger,
        Entity visual,
        float expectedHeightOffset,
        float expectedScale = 1f)
    {
        Assert.AreNotEqual(Entity.Null, visual);
        Assert.IsTrue(em.Exists(visual));
        Assert.IsTrue(em.HasComponent<LocalTransform>(visual));

        LocalTransform passengerTransform = em.GetComponentData<LocalTransform>(passenger);
        LocalTransform visualTransform = em.GetComponentData<LocalTransform>(visual);
        Assert.AreEqual(passengerTransform.Position.x, visualTransform.Position.x, 0.001f);
        Assert.AreEqual(passengerTransform.Position.z, visualTransform.Position.z, 0.001f);
        Assert.AreEqual(passengerTransform.Position.y + expectedHeightOffset, visualTransform.Position.y, 0.001f);
        Assert.AreEqual(expectedScale, visualTransform.Scale, 0.001f);
    }

    private static bool TryGetNearbyHelipadCell(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint)
    {
        cell = new int2(10, 8);
        worldPoint = default;
        return true;
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
        for (int i = 0; i < walkable.Length; i++)
        {
            walkable[i] = new GridWalkable { Value = 1 };
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }
    }

    private static void AddMapSurface(EntityManager em, BlobAssetReference<MapSurfaceBlob> surfaceBlob, int2 dimensions)
    {
        Entity surfaceEntity = em.CreateEntity(typeof(MapSurfaceComponent));
        em.SetComponentData(surfaceEntity, new MapSurfaceComponent
        {
            SurfaceBlob = surfaceBlob,
            GridOrigin = float3.zero,
            CellSize = 1f,
            Dimensions = dimensions,
            HasSurfaceData = 1
        });
    }

    private static BlobAssetReference<MapSurfaceBlob> CreateFlatSurfaceWithElevatedCell(int2 dimensions, int2 elevatedCell, float elevatedHeight)
    {
        int cellCount = dimensions.x * dimensions.y;
        var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.GridOrigin = float3.zero;
        root.CellSize = 1f;
        root.Dimensions = dimensions;
        root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;

        BlobBuilderArray<MapSurfaceCell> cells = builder.Allocate(ref root.Cells, cellCount);
        BlobBuilderArray<MapSurfaceSample> samples = builder.Allocate(ref root.Samples, cellCount);
        for (int y = 0; y < dimensions.y; y++)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                int index = x + y * dimensions.x;
                int2 cell = new(x, y);
                cells[index] = new MapSurfaceCell
                {
                    FirstSurfaceIndex = index,
                    SurfaceCount = 1,
                    InlineSurfaceIndex = 0
                };
                samples[index] = new MapSurfaceSample
                {
                    Cell = cell,
                    SurfaceId = index,
                    LayerId = 0,
                    Height = cell.x == elevatedCell.x && cell.y == elevatedCell.y ? elevatedHeight : 0f,
                    Normal = new float3(0f, 1f, 0f),
                    SlopeDegrees = 0f,
                    SurfaceType = MapSurfaceType.Terrain,
                    MovementMask = MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded,
                    Flags = MapSurfaceFlags.None,
                    FirstConnectionIndex = 0,
                    ConnectionCount = 0
                };
            }
        }

        builder.Allocate(ref root.Connections, 0);
        builder.Allocate(ref root.CompactSamples, 0);
        BlobAssetReference<MapSurfaceBlob> blob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        builder.Dispose();
        return blob;
    }

    private void BlockAirdropLandingArea(in GridConfig grid, int2 centerCell, int radius)
    {
        for (int y = centerCell.y - radius; y <= centerCell.y + radius; y++)
        {
            for (int x = centerCell.x - radius; x <= centerCell.x + radius; x++)
            {
                int2 cell = new(x, y);
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    continue;

                _blocked.Set(GridUtils.CellToIndex(cell, grid.Width), true);
            }
        }
    }

    private static Entity CreateHostileTarget(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static Entity CreateTransport(EntityManager em, int2 cell, bool air, bool airborne, string sourcePrefabKey = null)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitTransportCapacity),
            typeof(LocalToWorld),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 10 });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f)));
        em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(new float3(cell.x + 0.5f, airborne ? 8f : 0f, cell.y + 0.5f)) });
        em.AddBuffer<UnitTransportPassengerElement>(entity);
        if (!string.IsNullOrWhiteSpace(sourcePrefabKey))
            em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourcePrefabKey) });

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
        Entity entity = CreateTransport(em, cell, air: true, airborne: false, sourcePrefabKey: "Unit_Veh_Plane_Transport");
        em.SetComponentData(entity, new UnitTransportCapacity { SoldierCapacity = 24 });
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
        if (em.HasComponent<LocalToWorld>(transport))
            em.SetComponentData(transport, new LocalToWorld { Value = float4x4.Translate(position) });
        if (em.HasComponent<UnitGrid>(transport))
            em.SetComponentData(transport, new UnitGrid { Cell = new int2((int)math.floor(position.x), (int)math.floor(position.z)) });
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.Airborne = 1;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.ReturningHome = 0;
        airState.AttackRunActive = 0;
        em.SetComponentData(transport, airState);
    }

    private static void PrepareRunwayTransportPlaneForAirdropMovement(EntityManager em, Entity transport, bool airborne)
    {
        if (!em.HasComponent<UnitMove>(transport))
        {
            em.AddComponentData(transport, new UnitMove
            {
                Speed = 24f,
                WalkSpeed = 1.5f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
        }
        else
        {
            em.SetComponentData(transport, new UnitMove
            {
                Speed = 24f,
                WalkSpeed = 1.5f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
        }

        if (!em.HasComponent<UnitAttack>(transport))
            em.AddComponentData(transport, new UnitAttack { Range = 30f, CooldownSeconds = 1f, Damage = 10 });

        UnitAirMovement movement = em.GetComponentData<UnitAirMovement>(transport);
        movement.CruiseHeight = 55f;
        movement.RunwayTaxiSpeed = 8f;
        em.SetComponentData(transport, movement);

        UnitGrid grid = em.GetComponentData<UnitGrid>(transport);
        float3 home = new(grid.Cell.x + 0.5f, 0f, grid.Cell.y + 0.5f);
        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        airState.HomePosition = home;
        airState.HomeCell = grid.Cell;
        airState.HomeInitialized = 1;
        airState.ReturningHome = 0;
        airState.Airborne = (byte)(airborne ? 1 : 0);
        airState.UsesRunway = 1;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        airState.RunwayTakeoffPosition = home;
        airState.RunwayTakeoffCell = grid.Cell;
        airState.RunwayLandingPosition = home + new float3(6f, 0f, 0f);
        airState.RunwayLandingCell = grid.Cell + new int2(6, 0);
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
            typeof(UnitTransportBoardingTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitTransportBoardingTarget { Transport = transport, Goal = boardingGoal });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateVehiclePassenger(EntityManager em, int2 cell, Entity transport, int2 boardingGoal)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(UnitTransportBoardingTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.SetComponentData(entity, new UnitTransportBoardingTarget
        {
            Transport = transport,
            Goal = boardingGoal,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
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
            typeof(UnitTransportPassenger),
            typeof(UnitMoveVisualComponent),
            typeof(LocalTransform),
            typeof(Disabled));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitTransportPassenger { Transport = transport });
        em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        em.AddBuffer<UnitTransportHiddenVisualScale>(entity);
        return entity;
    }

    private static Entity CreateLoadedVehiclePassenger(EntityManager em, Entity transport)
    {
        Entity entity = CreateLoadedPassenger(em, transport);
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.AddComponentData(entity, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 9
        });
        return entity;
    }

    private static void AddAttackCapability(EntityManager em, Entity entity)
    {
        UnitCombat combat = em.HasComponent<UnitCombat>(entity)
            ? em.GetComponentData<UnitCombat>(entity)
            : new UnitCombat { AggroRangeCells = 8, ChaseBreakDistance = 16f, AutoEngage = 0 };
        combat.CanAttack = 1;
        if (em.HasComponent<UnitCombat>(entity))
            em.SetComponentData(entity, combat);
        else
            em.AddComponentData(entity, combat);

        UnitAttack attack = new()
        {
            Range = 8f,
            CooldownSeconds = 1f,
            Damage = 10
        };
        if (em.HasComponent<UnitAttack>(entity))
            em.SetComponentData(entity, attack);
        else
            em.AddComponentData(entity, attack);
    }

    private static Entity CreateSelectablePassenger(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(SelectedUnitTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
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
            typeof(UnitSourcePrefabKey),
            typeof(SelectedUnitTag),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 3) });
        em.SetComponentData(entity, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static Entity CreateAirdropVisualPrefab(EntityManager em, string name)
    {
        Entity entity = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetName(entity, name);
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        return entity;
    }

    private static void AssertValidPrefabAssetReference(GameObject prefab, string expectedPath)
    {
        Assert.IsNotNull(prefab);
        Assert.DoesNotThrow(() => { _ = prefab.scene; });
        Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(prefab));
        Assert.AreSame(AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath), prefab);
        Assert.AreNotEqual(PrefabAssetType.NotAPrefab, PrefabUtility.GetPrefabAssetType(prefab));
    }

    private static void AddAirdropVisualPrefabs(EntityManager em, Entity transport)
    {
        Entity parachutePrefab = CreateAirdropVisualPrefab(em, "ParachuteVisual");
        Entity cargoPrefab = CreateAirdropVisualPrefab(em, "CargoVisual");
        UnitTransportAirdropVisualPrefabs prefabs = new()
        {
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        };
        if (em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport))
            em.SetComponentData(transport, prefabs);
        else
            em.AddComponentData(transport, prefabs);
    }

    private static void AddAirdropVisualPrefabRegistry(
        EntityManager em,
        string sourceKey,
        Entity parachutePrefab,
        Entity cargoPrefab)
    {
        Entity registryEntity = em.CreateEntity();
        DynamicBuffer<UnitTransportAirdropVisualPrefabRegistryEntry> registry =
            em.AddBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(registryEntity);
        registry.Add(new UnitTransportAirdropVisualPrefabRegistryEntry
        {
            SourceKey = new FixedString64Bytes(sourceKey),
            SoldierParachuteVisualPrefab = parachutePrefab,
            VehicleEmergencyDropVisualPrefab = cargoPrefab
        });
    }

    private static Entity CreateBoardAllSoldier(EntityManager em, int2 cell)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitMove),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Rifle") });
        em.SetComponentData(entity, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return entity;
    }

    private static int CountBoardingTargets(EntityManager em, params Entity[] passengers)
    {
        int count = 0;
        for (int i = 0; i < passengers.Length; i++)
        {
            if (em.HasComponent<UnitTransportBoardingTarget>(passengers[i]))
                count++;
        }

        return count;
    }

    private static void AssertBoardingMoveOrder(EntityManager em, Entity passenger, Entity transport)
    {
        Assert.IsTrue(em.HasComponent<UnitTransportBoardingTarget>(passenger), "Passenger should keep a boarding target after the board command is accepted.");
        UnitTransportBoardingTarget boarding = em.GetComponentData<UnitTransportBoardingTarget>(passenger);
        Assert.AreEqual(transport, boarding.Transport);
        Assert.IsTrue(em.HasComponent<UnitTarget>(passenger), "Boarding passenger should receive a movement target to the transport approach point.");
        Assert.AreEqual(boarding.Goal, em.GetComponentData<UnitTarget>(passenger).Cell);
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(passenger), "Boarding passenger should receive a path request so it starts moving toward the transport.");
        Assert.AreEqual(boarding.Goal, em.GetComponentData<UnitPathRequest>(passenger).Goal);
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(passenger), "Boarding passenger should remain under manual movement while walking to the transport.");
        Assert.IsTrue(em.HasComponent<UnitVehicleMovement>(passenger), "Boarding passenger should keep or restore UnitVehicleMovement so UnitGridMoveJob can advance it.");
        Assert.IsTrue(em.HasComponent<UnitVehicleKinematics>(passenger), "Boarding passenger should keep or restore UnitVehicleKinematics so UnitGridMoveJob can advance it.");
    }

    private static bool TransportPassengerBufferContains(DynamicBuffer<UnitTransportPassengerElement> passengers, Entity passenger)
    {
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i].Passenger == passenger)
                return true;
        }

        return false;
    }

    private static Entity GetGridEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        return query.GetSingletonEntity();
    }
}
