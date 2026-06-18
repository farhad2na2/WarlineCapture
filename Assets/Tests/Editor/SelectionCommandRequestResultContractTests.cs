#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionCommandRequestResultContractTests
{
    public static void RunBatchValidation()
    {
        try
        {
            var tests = new SelectionCommandRequestResultContractTests();
            tests.CommandIntentRequest_CarriesPreResolvedTargetData();
            tests.CommandResult_CarriesFeedbackMarkerAndLifetimeData();
            tests.TacticalCommandReasonCodes_IncludeTransportFailureCodes();
            tests.ScanCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.MoveCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.MoveCommandProcessor_ReacquiresResultBufferAfterMoveOrderStructuralWrites();
            tests.MoveCommandProcessor_ReacquiresCommandBuffersAfterCallerStructuralChange();
            tests.AttackCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.AttackCommandSystem_OnUpdateConsumesPreResolvedEntityRequest();
            tests.ScanCommandSystem_OnUpdateConsumesPreResolvedCellRequest();
            tests.ScanCommandSystem_RevealsEnemyBuildingInsideRadius();
            tests.ScanCommandSystem_SelectedScannerQueuesUnitScanOrder();
            tests.ScanCommandSystem_SelectedCombatUnitQueuesReducedRadiusScanOrder();
            tests.UnitScanOrderExecutionSystem_RevealsWhenScannerReachesScanArea();
            tests.UnitScanOrderExecutionSystem_GroundScannerPatrolsScanAreaAfterArrival();
            tests.UnitAirMovementSystem_LandedRunwayScannerBeginsTakeoffBeforeRecon();
            tests.UnitAirMovementSystem_AirborneScannerLoitersInsteadOfLandingDuringActiveScan();
            tests.UnitScanOrderExecutionSystem_ReturnsAirScannerHomeWhenScanExpires();
            tests.UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea();
            tests.UnitEngagedMovementSystem_ClearsScanTargetsOutsideScanArea();
            tests.TransportCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.ScanCommandFlush_DrainsResultsOnceAndDoesNotDuplicateFeedback();
            tests.ScanCommandFlush_DeferredSelectedScannerFeedbackSaysScannerEnRoute();
            tests.ScanCommandFlush_AcceptedOneShotScanClearsCommandMode();
            tests.ScanCommandFlush_RejectedOneShotScanClearsCommandMode();
            tests.MoveCommandFlush_ShowsAcceptedWorldMarkerFromResult();
            tests.AttackCommandFlush_ShowsAcceptedTargetMarkerFromResult();
            tests.MoveCommandFlush_ReacquiresCommandBuffersAfterQuerySetupStructuralChange();
            tests.ImmediateDestroyFallback_DeletesSelectedBuildingThroughResultBoundary();
            tests.ImmediateSelectedUnitFlush_AcceptedHoldCleansPresentationAndRefreshesFocus();
            tests.ImmediateSelectedUnitFlush_RejectedHoldClearsCommandFeedbackOnly();
            tests.ImmediateSelectedUnitFlush_DestroyFocusedUnitClearsFocusedPresentation();
            tests.SelectionModeFlush_EnterAppliesPresentationCleanup();
            tests.SelectionModeFlush_ExitAppliesPresentationCleanup();
            tests.ScanTargetModeFlush_AppliesPresentationCleanup();
            tests.AttackTargetModeFlush_AppliesAcceptedEnterPresentationCleanup();
            tests.AttackTargetModeFlush_AppliesRejectedEnterPresentationCleanup();
            tests.AttackTargetModeFlush_AppliesAirDefenseAutoEngagePresentationCleanup();
            tests.AttackTargetModeFlush_AppliesAcceptedTogglePresentationCleanup();
            tests.FocusedMissileLauncherRadarAttackFlush_AppliesPresentationCleanup();
            tests.BoardTargetModeFlush_AppliesAcceptedPresentationCleanup();
            tests.BoardTargetModeFlush_AppliesRejectedPresentationCleanup();
            tests.BoardTargetModeFlush_AppliesToggleOffPresentationCleanup();
            tests.SelectAllFlush_DrainsRectangleBoundaryAndPresentationCleanup();
            tests.CancelActiveCommandModeFlush_ClearsPresentationWithoutPersistentFeedback();
            tests.MoveTargetModeFlush_AppliesAcceptedPresentationCleanup();
            tests.MoveTargetModeFlush_AppliesRejectedPresentationCleanup();
            tests.DeselectAllFlush_ClearsManagedSelectionCacheAndPresentation();
            UnityEngine.Debug.Log("[SelectionCommandRequestResultContractValidation] result=Passed tests=48");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[SelectionCommandRequestResultContractValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ImmediateSelectedUnitFlush_AcceptedHoldCleansPresentationAndRefreshesFocus()
    {
        using World world = new("ImmediateSelectedUnitFlush_AcceptedHoldCleansPresentationAndRefreshesFocus");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 110,
            Frame = 129
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.HoldPosition,
            RequestId = 111,
            Frame = 130
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Attack,
            ActiveCommandModeFrame = 32,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            HasQueuedMoveOrder = 1,
            QueuedMoveOrderFrame = 32,
            QueuedMoveOrderScreenPosition = new float2(16f, 17f)
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        Entity selectedUnit = CreateSelectedMovableUnit(em);

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int exitBuildModeCount = 0;
        int cancelBuildingPlacementCount = 0;
        int clearBuildingCount = 0;
        string clearBuildingReason = string.Empty;
        int commandModeCount = 0;
        TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int refreshFocusedCount = 0;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            () => cancelBuildingPlacementCount++,
            null,
            null,
            reason =>
            {
                clearBuildingCount++;
                clearBuildingReason = reason;
            },
            () => exitBuildModeCount++,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            applyHudCommandMode: mode =>
            {
                commandModeCount++;
                appliedCommandMode = mode;
            },
            clearHudCommandMode: () => clearCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            refreshFocusedUnit: (_, _) => refreshFocusedCount++);

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessImmediateSelectedUnitCommandRequests(
            flushContext,
            focusedUnit: Entity.Null);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(0, inputState.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.IsTrue(em.HasComponent<HoldPositionOrderTag>(selectedUnit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(selectedUnit));
        Assert.AreEqual(1, commandModeCount);
        Assert.AreEqual(TacticalCommandMode.Hold, appliedCommandMode);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, exitBuildModeCount);
        Assert.AreEqual(1, cancelBuildingPlacementCount);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual("SelectionUiCommandSystem.Hold", clearBuildingReason);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsTrue(commandResult.Accepted);
        Assert.AreEqual("Holding current position.", commandResult.Message);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, refreshFocusedCount);
    }

    [Test]
    public void ImmediateSelectedUnitFlush_RejectedHoldClearsCommandFeedbackOnly()
    {
        using World world = new("ImmediateSelectedUnitFlush_RejectedHoldClearsCommandFeedbackOnly");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.HoldPosition,
            RequestId = 112,
            Frame = 131
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Attack,
            ActiveCommandModeFrame = 33,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });

        int explicitAttackModeCount = 0;
        int commandModeCount = 0;
        TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        int cameraDraggingCount = 0;
        int refreshFocusedCount = 0;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            applyHudCommandMode: mode =>
            {
                commandModeCount++;
                appliedCommandMode = mode;
            },
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: _ => explicitAttackModeCount++,
            setHudWorldMarkersVisible: _ => worldMarkerVisibilityCount++,
            setCameraDragging: _ => cameraDraggingCount++,
            refreshFocusedUnit: (_, _) => refreshFocusedCount++);

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessImmediateSelectedUnitCommandRequests(
            flushContext,
            focusedUnit: Entity.Null);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, commandModeCount);
        Assert.AreEqual(TacticalCommandMode.Hold, appliedCommandMode);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsFalse(commandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, commandResult.ReasonCode);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(0, explicitAttackModeCount);
        Assert.AreEqual(0, worldMarkerVisibilityCount);
        Assert.AreEqual(0, cameraDraggingCount);
        Assert.AreEqual(0, refreshFocusedCount);
    }

    [Test]
    public void ImmediateSelectedUnitFlush_DestroyFocusedUnitClearsFocusedPresentation()
    {
        using World world = new("ImmediateSelectedUnitFlush_DestroyFocusedUnitClearsFocusedPresentation");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DestroyFocusedUnit,
            RequestId = 113,
            Frame = 132
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent());
        em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        Entity focusedUnit = em.CreateEntity(typeof(Faction));
        em.SetComponentData(focusedUnit, new Faction { Id = FactionIdentity.PlayerFactionId });

        int clearFocusedCount = 0;
        int clearSelectionCount = 0;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int explicitAttackModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        int cameraDraggingCount = 0;
        var selectionStateSystem = new SelectionStateSystem();
        selectionStateSystem.SetFocusedUnit(focusedUnit);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            selectionStateSystem: selectionStateSystem,
            clearHudSelection: () => clearSelectionCount++,
            setExplicitAttackTargetModeActive: _ => explicitAttackModeCount++,
            setHudWorldMarkersVisible: _ => worldMarkerVisibilityCount++,
            setCameraDragging: _ => cameraDraggingCount++,
            clearFocusedUnit: state =>
            {
                clearFocusedCount++;
                state.ClearFocusedUnit();
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessImmediateSelectedUnitCommandRequests(
            flushContext,
            focusedUnit);

        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.IsFalse(em.Exists(focusedUnit));
        Assert.AreEqual(Entity.Null, selectionStateSystem.FocusedUnit);
        Assert.AreEqual(1, clearFocusedCount);
        Assert.AreEqual(1, clearSelectionCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsTrue(commandResult.Accepted);
        Assert.AreEqual("Destroyed selected unit.", commandResult.Message);
        Assert.AreEqual(0, explicitAttackModeCount);
        Assert.AreEqual(0, worldMarkerVisibilityCount);
        Assert.AreEqual(0, cameraDraggingCount);
    }

    [Test]
    public void SelectionModeFlush_EnterAppliesPresentationCleanup()
    {
        using World world = new("SelectionModeFlush_EnterAppliesPresentationCleanup");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                RequestId = 95,
                Frame = 114
            });
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.EnterSelectionMode,
                RequestId = 96,
                Frame = 115
            });
            em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
            {
                ActiveCommandMode = (int)TacticalCommandMode.Attack,
                ActiveCommandModeFrame = 22,
                ActiveCommandModeOneShot = 1,
                ActiveCommandModeRequiresWorldTarget = 1,
                LastKnownPointerPosition = new float2(45f, 67f),
                HasLastKnownPointerPosition = 1
            });
            Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
            {
                SelectionModeActive = 0,
                SuppressNextWorldClick = 0
            });

            int explicitAttackModeCount = 0;
            bool explicitAttackModeActive = true;
            int clearBuildingCount = 0;
            int commandModeCount = 0;
            TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
            int clearCommandModeCount = 0;
            int worldMarkerVisibilityCount = 0;
            bool worldMarkersVisible = true;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            int diagnosticCount = 0;
            string lastDiagnostic = string.Empty;
            var buildingInteraction = new BuildingPlacementInteractionSystem();
            var buildingContext = new BuildingPlacementInteractionSystem.Context(
                null,
                null,
                () => true,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                _ => clearBuildingCount++,
                null,
                null,
                null);
            var inputSystem = new RtsSelectionInputSystem();
            RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
                inputSystem,
                default,
                default,
                default,
                null,
                em,
                applyHudCommandMode: mode =>
                {
                    commandModeCount++;
                    appliedCommandMode = mode;
                },
                clearHudCommandMode: () => clearCommandModeCount++,
                buildingPlacementInteractionSystem: buildingInteraction,
                buildingPlacementInteractionContext: buildingContext,
                setExplicitAttackTargetModeActive: active =>
                {
                    explicitAttackModeCount++;
                    explicitAttackModeActive = active;
                },
                setHudWorldMarkersVisible: visible =>
                {
                    worldMarkerVisibilityCount++;
                    worldMarkersVisible = visible;
                },
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                logSelectionClickDiagnostic: message =>
                {
                    diagnosticCount++;
                    lastDiagnostic = message;
                });

            bool handled = new RtsSelectionCommandResultFlushSystem().ProcessSelectionModeCommandRequests(
                flushContext,
                currentFrame: 320);

            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
            Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
            Assert.AreEqual(1, runtimeState.SelectionModeActive);
            Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
            Assert.AreEqual(1, explicitAttackModeCount);
            Assert.IsFalse(explicitAttackModeActive);
            Assert.AreEqual(1, clearBuildingCount);
            Assert.AreEqual(1, commandModeCount);
            Assert.AreEqual(TacticalCommandMode.Select, appliedCommandMode);
            Assert.AreEqual(0, clearCommandModeCount);
            Assert.AreEqual(1, worldMarkerVisibilityCount);
            Assert.IsFalse(worldMarkersVisible);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
            Assert.AreEqual(1, diagnosticCount);
            StringAssert.Contains("selectionModeEntered source=ui", lastDiagnostic);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void SelectionModeFlush_ExitAppliesPresentationCleanup()
    {
        using World world = new("SelectionModeFlush_ExitAppliesPresentationCleanup");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.ExitSelectionMode,
                RequestId = 97,
                Frame = 116
            });
            em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
            {
                ActiveCommandMode = (int)TacticalCommandMode.Select,
                ActiveCommandModeFrame = 23,
                ActiveCommandModeOneShot = 1,
                LastKnownPointerPosition = new float2(75f, 88f),
                HasLastKnownPointerPosition = 1
            });
            Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
            {
                SelectionModeActive = 1,
                SuppressNextWorldClick = 0
            });

            int explicitAttackModeCount = 0;
            int clearBuildingCount = 0;
            int commandModeCount = 0;
            int clearCommandModeCount = 0;
            int worldMarkerVisibilityCount = 0;
            bool worldMarkersVisible = true;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            int diagnosticCount = 0;
            string lastDiagnostic = string.Empty;
            var buildingInteraction = new BuildingPlacementInteractionSystem();
            var buildingContext = new BuildingPlacementInteractionSystem.Context(
                null,
                null,
                () => true,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                _ => clearBuildingCount++,
                null,
                null,
                null);
            var inputSystem = new RtsSelectionInputSystem();
            RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
                inputSystem,
                default,
                default,
                default,
                null,
                em,
                applyHudCommandMode: _ => commandModeCount++,
                clearHudCommandMode: () => clearCommandModeCount++,
                buildingPlacementInteractionSystem: buildingInteraction,
                buildingPlacementInteractionContext: buildingContext,
                setExplicitAttackTargetModeActive: _ => explicitAttackModeCount++,
                setHudWorldMarkersVisible: visible =>
                {
                    worldMarkerVisibilityCount++;
                    worldMarkersVisible = visible;
                },
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                logSelectionClickDiagnostic: message =>
                {
                    diagnosticCount++;
                    lastDiagnostic = message;
                });

            bool handled = new RtsSelectionCommandResultFlushSystem().ProcessSelectionModeCommandRequests(
                flushContext,
                currentFrame: 321);

            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
            Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
            Assert.AreEqual(0, runtimeState.SelectionModeActive);
            Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
            Assert.AreEqual(0, explicitAttackModeCount);
            Assert.AreEqual(0, clearBuildingCount);
            Assert.AreEqual(0, commandModeCount);
            Assert.AreEqual(1, clearCommandModeCount);
            Assert.AreEqual(1, worldMarkerVisibilityCount);
            Assert.IsFalse(worldMarkersVisible);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
            Assert.AreEqual(1, diagnosticCount);
            StringAssert.Contains("selectionModeExited source=ui", lastDiagnostic);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ScanTargetModeFlush_AppliesPresentationCleanup()
    {
        using World world = new("ScanTargetModeFlush_AppliesPresentationCleanup");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                RequestId = 98,
                Frame = 117
            });
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.EnterScanTargetMode,
                RequestId = 99,
                Frame = 118
            });
            em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
            {
                ActiveCommandMode = (int)TacticalCommandMode.Move,
                ActiveCommandModeFrame = 24,
                ActiveCommandModeOneShot = 1,
                ActiveCommandModeRequiresWorldTarget = 1,
                HasQueuedMoveOrder = 1,
                QueuedMoveOrderFrame = 24,
                QueuedMoveOrderScreenPosition = new float2(10f, 11f),
                LastKnownPointerPosition = new float2(42f, 24f),
                HasLastKnownPointerPosition = 1
            });
            Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
            {
                SelectionModeActive = 1,
                SuppressNextWorldClick = 0
            });

            int explicitAttackModeCount = 0;
            bool explicitAttackModeActive = true;
            int exitBuildModeCount = 0;
            int cancelBuildingPlacementCount = 0;
            int clearBuildingCount = 0;
            string clearBuildingReason = string.Empty;
            int commandModeCount = 0;
            TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
            int worldMarkerVisibilityCount = 0;
            bool worldMarkersVisible = true;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            int diagnosticCount = 0;
            string lastDiagnostic = string.Empty;
            var buildingInteraction = new BuildingPlacementInteractionSystem();
            var buildingContext = new BuildingPlacementInteractionSystem.Context(
                null,
                null,
                () => true,
                null,
                null,
                null,
                null,
                null,
                null,
                () => cancelBuildingPlacementCount++,
                null,
                null,
                reason =>
                {
                    clearBuildingCount++;
                    clearBuildingReason = reason;
                },
                () => exitBuildModeCount++,
                null,
                null);
            var inputSystem = new RtsSelectionInputSystem();
            RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
                inputSystem,
                default,
                default,
                default,
                null,
                em,
                applyHudCommandMode: mode =>
                {
                    commandModeCount++;
                    appliedCommandMode = mode;
                },
                buildingPlacementInteractionSystem: buildingInteraction,
                buildingPlacementInteractionContext: buildingContext,
                setExplicitAttackTargetModeActive: active =>
                {
                    explicitAttackModeCount++;
                    explicitAttackModeActive = active;
                },
                setHudWorldMarkersVisible: visible =>
                {
                    worldMarkerVisibilityCount++;
                    worldMarkersVisible = visible;
                },
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                logSelectionClickDiagnostic: message =>
                {
                    diagnosticCount++;
                    lastDiagnostic = message;
                });

            bool handled = new RtsSelectionCommandResultFlushSystem().ProcessScanTargetModeCommandRequests(
                flushContext,
                currentFrame: 322);

            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
            Assert.AreEqual((int)TacticalCommandMode.Scan, inputState.ActiveCommandMode);
            Assert.AreEqual(322, inputState.ActiveCommandModeFrame);
            Assert.AreEqual(1, inputState.ActiveCommandModeOneShot);
            Assert.AreEqual(1, inputState.ActiveCommandModeRequiresWorldTarget);
            Assert.AreEqual(0, inputState.HasQueuedMoveOrder);
            Assert.AreEqual(0, runtimeState.SelectionModeActive);
            Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
            Assert.AreEqual(1, explicitAttackModeCount);
            Assert.IsFalse(explicitAttackModeActive);
            Assert.AreEqual(1, exitBuildModeCount);
            Assert.AreEqual(1, cancelBuildingPlacementCount);
            Assert.AreEqual(1, clearBuildingCount);
            Assert.AreEqual("SelectionUiCommandSystem.EnterScanTargetMode", clearBuildingReason);
            Assert.AreEqual(1, commandModeCount);
            Assert.AreEqual(TacticalCommandMode.Scan, appliedCommandMode);
            Assert.AreEqual(1, worldMarkerVisibilityCount);
            Assert.IsFalse(worldMarkersVisible);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
            Assert.AreEqual(1, diagnosticCount);
            StringAssert.Contains("scanModeEntered result=True", lastDiagnostic);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void AttackTargetModeFlush_AppliesAcceptedEnterPresentationCleanup()
    {
        using World world = new("AttackTargetModeFlush_AppliesAcceptedEnterPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 104,
            Frame = 123
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterAttackTargetMode,
            RequestId = 105,
            Frame = 124
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 28,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            HasQueuedMoveOrder = 1,
            QueuedMoveOrderFrame = 28,
            QueuedMoveOrderScreenPosition = new float2(14f, 15f),
            LastKnownPointerPosition = new float2(64f, 74f),
            HasLastKnownPointerPosition = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        CreateSelectedAttackUnit(em);

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        string clearBuildingReason = string.Empty;
        int commandModeCount = 0;
        TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
        int commandResultCount = 0;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = false;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            reason =>
            {
                clearBuildingCount++;
                clearBuildingReason = reason;
            },
            null,
            null,
            null);
        var inputSystem = new RtsSelectionInputSystem();
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            inputSystem,
            default,
            default,
            default,
            _ => commandResultCount++,
            em,
            applyHudCommandMode: mode =>
            {
                commandModeCount++;
                appliedCommandMode = mode;
            },
            clearHudCommandMode: () => clearCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessAttackTargetModeCommandRequests(
            flushContext,
            currentFrame: 326,
            focusedUnit: Entity.Null);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.Attack, inputState.ActiveCommandMode);
        Assert.AreEqual(326, inputState.ActiveCommandModeFrame);
        Assert.AreEqual(1, inputState.ActiveCommandModeOneShot);
        Assert.AreEqual(1, inputState.ActiveCommandModeRequiresWorldTarget);
        Assert.AreEqual(0, inputState.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(2, explicitAttackModeCount);
        Assert.IsTrue(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual("SelectionUiCommandSystem.EnterAttackTargetMode", clearBuildingReason);
        Assert.AreEqual(1, commandModeCount);
        Assert.AreEqual(TacticalCommandMode.Attack, appliedCommandMode);
        Assert.AreEqual(0, commandResultCount);
        Assert.AreEqual(0, clearCommandModeCount);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsTrue(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("attackModeEntered result=True", lastDiagnostic);
    }

    [Test]
    public void AttackTargetModeFlush_AppliesRejectedEnterPresentationCleanup()
    {
        using World world = new("AttackTargetModeFlush_AppliesRejectedEnterPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterAttackTargetMode,
            RequestId = 106,
            Frame = 125
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 29,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        CreateSelectedNonAttackUnit(em);

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        int commandModeCount = 0;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _ => clearBuildingCount++,
            null,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            applyHudCommandMode: _ => commandModeCount++,
            clearHudCommandMode: () => clearCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessAttackTargetModeCommandRequests(
            flushContext,
            currentFrame: 327,
            focusedUnit: Entity.Null);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual(0, commandModeCount);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsFalse(commandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, commandResult.ReasonCode);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("attackModeEntered result=False reason=TargetNotAttackable", lastDiagnostic);
    }

    [Test]
    public void AttackTargetModeFlush_AppliesAirDefenseAutoEngagePresentationCleanup()
    {
        using World world = new("AttackTargetModeFlush_AppliesAirDefenseAutoEngagePresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterAttackTargetMode,
            RequestId = 107,
            Frame = 126
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 30,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            HasQueuedMoveOrder = 1,
            QueuedMoveOrderFrame = 30
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        CreateSelectedAirDefenseLauncher(em);

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        int commandModeCount = 0;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _ => clearBuildingCount++,
            null,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            applyHudCommandMode: _ => commandModeCount++,
            clearHudCommandMode: () => clearCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessAttackTargetModeCommandRequests(
            flushContext,
            currentFrame: 328,
            focusedUnit: Entity.Null);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(0, inputState.HasQueuedMoveOrder);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual(0, commandModeCount);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsTrue(commandResult.Accepted);
        Assert.AreEqual("Air defense auto-engages aircraft and incoming missiles.", commandResult.Message);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("attackModeEntered result=False reason=AirDefenseAutoEngage", lastDiagnostic);
    }

    [Test]
    public void AttackTargetModeFlush_AppliesAcceptedTogglePresentationCleanup()
    {
        using World world = new("AttackTargetModeFlush_AppliesAcceptedTogglePresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 108,
            Frame = 127
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.ToggleAttackTargetMode,
            RequestId = 109,
            Frame = 128
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 31,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            HasQueuedMoveOrder = 1,
            QueuedMoveOrderFrame = 31,
            LastKnownPointerPosition = new float2(84f, 94f),
            HasLastKnownPointerPosition = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        Entity focusedUnit = CreateFocusedAttackUnit(em);

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = false;
        int clearBuildingCount = 0;
        int commandModeCount = 0;
        TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
        int commandResultCount = 0;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = false;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            new RtsSelectionInputSystem(),
            default,
            default,
            default,
            _ => commandResultCount++,
            em,
            applyHudCommandMode: mode =>
            {
                commandModeCount++;
                appliedCommandMode = mode;
            },
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessAttackTargetModeCommandRequests(
            flushContext,
            currentFrame: 329,
            focusedUnit);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(1, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity)[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Attack, inputState.ActiveCommandMode);
        Assert.AreEqual(329, inputState.ActiveCommandModeFrame);
        Assert.AreEqual(1, inputState.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsTrue(explicitAttackModeActive);
        Assert.AreEqual(0, clearBuildingCount);
        Assert.AreEqual(1, commandModeCount);
        Assert.AreEqual(TacticalCommandMode.Attack, appliedCommandMode);
        Assert.AreEqual(0, commandResultCount);
        Assert.AreEqual(0, clearCommandModeCount);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsTrue(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("attackModeToggled result=True", lastDiagnostic);
    }

    [Test]
    public void FocusedMissileLauncherRadarAttackFlush_AppliesPresentationCleanup()
    {
        using World world = new("FocusedMissileLauncherRadarAttackFlush_AppliesPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.ToggleAttackTargetMode,
            RequestId = 130,
            Frame = 150
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Attack,
            ActiveCommandModeFrame = 149,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1
        });
        Entity launcher = CreateRadarAttackLauncher(em, new int2(10, 10));
        CreateThreatDetector(em, FactionIdentity.PlayerFactionId, ThreatDetectionKind.Ground, new int2(20, 20), 40);
        Entity groundTarget = CreateRadarAttackTarget(em, FactionIdentity.EnemyFactionId, new int2(35, 20));
        var selectionState = new SelectionStateSystem();

        int clearSelectionCount = 0;
        string clearReason = string.Empty;
        int setFocusedUnitCount = 0;
        Entity focusedUnit = Entity.Null;
        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = false;
        int applySelectionCount = 0;
        Entity appliedSelection = Entity.Null;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            selectionStateSystem: selectionState,
            clearCurrentSelection: (_, reason) =>
            {
                clearSelectionCount++;
                clearReason = reason;
            },
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            applyHudSelection: (_, entity) =>
            {
                applySelectionCount++;
                appliedSelection = entity;
            },
            setFocusedUnit: (state, entity) =>
            {
                setFocusedUnitCount++;
                focusedUnit = entity;
                state.SetFocusedUnit(entity);
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessFocusedMissileLauncherRadarAttack(
            flushContext,
            launcher);

        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));
        EngageTarget engage = em.GetComponentData<EngageTarget>(launcher);
        Assert.AreEqual(groundTarget, engage.Target);
        Assert.AreEqual(new int2(35, 20), engage.Cell);
        Assert.AreEqual(1, engage.IsCommanded);
        Assert.AreEqual(1, clearSelectionCount);
        Assert.AreEqual("MissileLauncherRadarAttack", clearReason);
        Assert.AreEqual(1, setFocusedUnitCount);
        Assert.AreEqual(launcher, focusedUnit);
        Assert.AreEqual(launcher, selectionState.FocusedUnit);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsTrue(commandResult.Accepted);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsTrue(worldMarkersVisible);
        Assert.AreEqual(1, applySelectionCount);
        Assert.AreEqual(launcher, appliedSelection);
    }

    [Test]
    public void BoardTargetModeFlush_AppliesAcceptedPresentationCleanup()
    {
        using World world = new("BoardTargetModeFlush_AppliesAcceptedPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 100,
            Frame = 119
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterBoardTargetMode,
            RequestId = 101,
            Frame = 120
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 25,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            HasQueuedMoveOrder = 1,
            QueuedMoveOrderFrame = 25,
            QueuedMoveOrderScreenPosition = new float2(12f, 13f),
            LastKnownPointerPosition = new float2(52f, 62f),
            HasLastKnownPointerPosition = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });
        Entity transport = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(LocalTransform),
            typeof(UnitTransportCapacity));
        em.SetComponentData(transport, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(transport, new UnitGrid { Cell = new int2(4, 5) });
        em.SetComponentData(transport, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(transport, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 3 });

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        string clearBuildingReason = string.Empty;
        int boardCommandModeCount = 0;
        BoardCommandModeDirection appliedDirection = BoardCommandModeDirection.None;
        bool appliedBoardAllInteractable = false;
        int commandResultCount = 0;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = false;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            reason =>
            {
                clearBuildingCount++;
                clearBuildingReason = reason;
            },
            null,
            null,
            null);
        var inputSystem = new RtsSelectionInputSystem();
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            inputSystem,
            default,
            default,
            default,
            _ => commandResultCount++,
            em,
            clearHudCommandMode: () => clearCommandModeCount++,
            applyHudBoardCommandMode: (direction, boardAllInteractable) =>
            {
                boardCommandModeCount++;
                appliedDirection = direction;
                appliedBoardAllInteractable = boardAllInteractable;
            },
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessBoardTargetModeCommandRequests(
            flushContext,
            currentFrame: 323);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.Board, inputState.ActiveCommandMode);
        Assert.AreEqual(323, inputState.ActiveCommandModeFrame);
        Assert.AreEqual(1, inputState.ActiveCommandModeOneShot);
        Assert.AreEqual(1, inputState.ActiveCommandModeRequiresWorldTarget);
        Assert.AreEqual((byte)BoardCommandModeDirection.TransportToPassenger, inputState.ActiveBoardCommandDirection);
        Assert.AreEqual(transport, inputState.ActiveBoardTransport);
        Assert.AreEqual(0, inputState.HasQueuedMoveOrder);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual("SelectionUiCommandSystem.EnterBoardTargetMode", clearBuildingReason);
        Assert.AreEqual(1, boardCommandModeCount);
        Assert.AreEqual(BoardCommandModeDirection.TransportToPassenger, appliedDirection);
        Assert.IsTrue(appliedBoardAllInteractable);
        Assert.AreEqual(0, commandResultCount);
        Assert.AreEqual(0, clearCommandModeCount);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsTrue(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("boardModeEntered result=True", lastDiagnostic);
    }

    [Test]
    public void BoardTargetModeFlush_AppliesRejectedPresentationCleanup()
    {
        using World world = new("BoardTargetModeFlush_AppliesRejectedPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterBoardTargetMode,
            RequestId = 102,
            Frame = 121
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Move,
            ActiveCommandModeFrame = 26,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        int boardCommandModeCount = 0;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _ => clearBuildingCount++,
            null,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            clearHudCommandMode: () => clearCommandModeCount++,
            applyHudBoardCommandMode: (_, _) => boardCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessBoardTargetModeCommandRequests(
            flushContext,
            currentFrame: 324);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual(0, boardCommandModeCount);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsFalse(commandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, commandResult.ReasonCode);
        Assert.AreEqual("Select units to board.", commandResult.Message);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("boardModeEntered result=False reason=NoSelection", lastDiagnostic);
    }

    [Test]
    public void BoardTargetModeFlush_AppliesToggleOffPresentationCleanup()
    {
        using World world = new("BoardTargetModeFlush_AppliesToggleOffPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterBoardTargetMode,
            RequestId = 103,
            Frame = 122
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Board,
            ActiveCommandModeFrame = 27,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            ActiveBoardCommandDirection = (byte)BoardCommandModeDirection.TransportToPassenger,
            ActiveBoardTransport = new Entity { Index = 12, Version = 1 }
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });

        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int clearBuildingCount = 0;
        int boardCommandModeCount = 0;
        int commandResultCount = 0;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _ => clearBuildingCount++,
            null,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            _ => commandResultCount++,
            em,
            clearHudCommandMode: () => clearCommandModeCount++,
            applyHudBoardCommandMode: (_, _) => boardCommandModeCount++,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessBoardTargetModeCommandRequests(
            flushContext,
            currentFrame: 325);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(0, inputState.ActiveBoardCommandDirection);
        Assert.AreEqual(Entity.Null, inputState.ActiveBoardTransport);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearBuildingCount);
        Assert.AreEqual(0, boardCommandModeCount);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(0, commandResultCount);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("boardModeToggledOff", lastDiagnostic);
    }

    [Test]
    public void MoveTargetModeFlush_AppliesAcceptedPresentationCleanup()
    {
        using World world = new("MoveTargetModeFlush_AppliesAcceptedPresentationCleanup");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.EnterMoveTargetMode,
                RequestId = 93,
                Frame = 112
            });
            em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
            {
                LastKnownPointerPosition = new float2(12f, 34f),
                HasLastKnownPointerPosition = 1
            });
            Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent { SelectionModeActive = 1 });
            Entity selectedUnit = em.CreateEntity(
                typeof(SelectedUnitTag),
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitMove));
            em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(selectedUnit, new UnitGrid { Cell = new int2(2, 3) });
            em.SetComponentData(selectedUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, ArriveDistance = 0.1f });

            int explicitAttackModeCount = 0;
            bool explicitAttackModeActive = true;
            int clearBuildingCount = 0;
            int commandModeCount = 0;
            TacticalCommandMode appliedCommandMode = TacticalCommandMode.None;
            int commandResultCount = 0;
            int worldMarkerVisibilityCount = 0;
            bool worldMarkersVisible = true;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            int diagnosticCount = 0;
            string lastDiagnostic = string.Empty;
            var buildingInteraction = new BuildingPlacementInteractionSystem();
            var buildingContext = new BuildingPlacementInteractionSystem.Context(
                null,
                null,
                () => true,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                _ => clearBuildingCount++,
                null,
                null,
                null);
            var inputSystem = new RtsSelectionInputSystem();
            RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
                inputSystem,
                default,
                default,
                default,
                _ => commandResultCount++,
                em,
                applyHudCommandMode: mode =>
                {
                    commandModeCount++;
                    appliedCommandMode = mode;
                },
                buildingPlacementInteractionSystem: buildingInteraction,
                buildingPlacementInteractionContext: buildingContext,
                setExplicitAttackTargetModeActive: active =>
                {
                    explicitAttackModeCount++;
                    explicitAttackModeActive = active;
                },
                setHudWorldMarkersVisible: visible =>
                {
                    worldMarkerVisibilityCount++;
                    worldMarkersVisible = visible;
                },
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                logSelectionClickDiagnostic: message =>
                {
                    diagnosticCount++;
                    lastDiagnostic = message;
                });

            bool handled = new RtsSelectionCommandResultFlushSystem().ProcessMoveTargetModeCommandRequests(
                flushContext,
                currentFrame: 260);

            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
            Assert.AreEqual((int)TacticalCommandMode.Move, inputState.ActiveCommandMode);
            Assert.AreEqual(0, runtimeState.SelectionModeActive);
            Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
            Assert.AreEqual(1, explicitAttackModeCount);
            Assert.IsFalse(explicitAttackModeActive);
            Assert.AreEqual(1, clearBuildingCount);
            Assert.AreEqual(1, commandModeCount);
            Assert.AreEqual(TacticalCommandMode.Move, appliedCommandMode);
            Assert.AreEqual(0, commandResultCount);
            Assert.AreEqual(1, worldMarkerVisibilityCount);
            Assert.IsFalse(worldMarkersVisible);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
            Assert.AreEqual(1, diagnosticCount);
            StringAssert.Contains("moveModeEntered result=True", lastDiagnostic);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MoveTargetModeFlush_AppliesRejectedPresentationCleanup()
    {
        using World world = new("MoveTargetModeFlush_AppliesRejectedPresentationCleanup");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.EnterMoveTargetMode,
            RequestId = 94,
            Frame = 113
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent { SelectionModeActive = 1 });

        int clearCommandModeCount = 0;
        int commandModeCount = 0;
        int commandResultCount = 0;
        TacticalCommandResult commandResult = default;
        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int worldMarkerVisibilityCount = 0;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int diagnosticCount = 0;
        string lastDiagnostic = string.Empty;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                commandResultCount++;
                commandResult = result;
            },
            em,
            applyHudCommandMode: _ => commandModeCount++,
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: _ => worldMarkerVisibilityCount++,
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            logSelectionClickDiagnostic: message =>
            {
                diagnosticCount++;
                lastDiagnostic = message;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessMoveTargetModeCommandRequests(
            flushContext,
            currentFrame: 300);

        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(1, runtimeState.SelectionModeActive);
        Assert.AreEqual(0, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(0, commandModeCount);
        Assert.AreEqual(1, commandResultCount);
        Assert.IsFalse(commandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.NoSelection, commandResult.ReasonCode);
        Assert.AreEqual(0, worldMarkerVisibilityCount);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(1, diagnosticCount);
        StringAssert.Contains("moveModeEntered result=False reason=NoSelection", lastDiagnostic);
    }

    [Test]
    public void CancelActiveCommandModeFlush_ClearsPresentationWithoutPersistentFeedback()
    {
        using World world = new("CancelActiveCommandModeFlush_ClearsPresentationWithoutPersistentFeedback");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.CancelActiveCommandMode,
            RequestId = 92,
            Frame = 111
        });
        em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
        {
            ActiveCommandMode = (int)TacticalCommandMode.Board,
            ActiveCommandModeFrame = 55,
            ActiveCommandModeOneShot = 1,
            ActiveCommandModeRequiresWorldTarget = 1,
            ActiveBoardCommandDirection = (byte)BoardCommandModeDirection.TransportToPassenger,
            ActiveBoardTransport = new Entity { Index = 8, Version = 1 }
        });
        Entity runtimeStateEntity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(runtimeStateEntity, new RuntimeGameplayStateComponent
        {
            SelectionModeActive = 1,
            SuppressNextWorldClick = 0
        });

        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        int commandResultCount = 0;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            _ => commandResultCount++,
            em,
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessCancelActiveCommandModeRequests(flushContext);

        Assert.IsTrue(handled);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
        RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeStateEntity);
        Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
        Assert.AreEqual(0, runtimeState.SelectionModeActive);
        Assert.AreEqual(1, runtimeState.SuppressNextWorldClick);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
        Assert.AreEqual(0, commandResultCount);
    }

    [Test]
    public void SelectAllFlush_DrainsRectangleBoundaryAndPresentationCleanup()
    {
        using World world = new("SelectAllFlush_DrainsRectangleBoundaryAndPresentationCleanup");
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            Entity commandEntity = em.CreateEntity(
                typeof(RtsSelectionInputRequestQueueComponent),
                typeof(RtsSelectionInputStateComponent));
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.SelectAllSoldiers,
                RequestId = 91,
                Frame = 110,
                ScreenPosition = new float2(20f, 30f),
                DragStart = new float2(0f, 0f),
                DragCurrent = new float2(40f, 60f),
                TargetKind = RtsSelectionCommandTargetKind.ScreenRect,
                HasScreenRect = 1
            });
            em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
            em.AddBuffer<RtsSelectionPointerRequestElement>(commandEntity);
            em.SetComponentData(commandEntity, new RtsSelectionInputRequestQueueComponent { LastRequestId = 100 });
            em.SetComponentData(commandEntity, new RtsSelectionInputStateComponent
            {
                ActiveCommandMode = (int)TacticalCommandMode.Attack,
                ActiveCommandModeFrame = 44,
                ActiveCommandModeOneShot = 1,
                ActiveCommandModeRequiresWorldTarget = 1,
                ActiveBoardCommandDirection = (byte)BoardCommandModeDirection.TransportToPassenger,
                ActiveBoardTransport = new Entity { Index = 7, Version = 1 }
            });

            int clearCommandModeCount = 0;
            int worldMarkerVisibilityCount = 0;
            bool worldMarkersVisible = true;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            int explicitAttackModeCount = 0;
            bool explicitAttackModeActive = true;
            int rectangleDrainCount = 0;
            var inputSystem = new RtsSelectionInputSystem();
            RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
                inputSystem,
                default,
                default,
                default,
                null,
                em,
                clearHudCommandMode: () => clearCommandModeCount++,
                setExplicitAttackTargetModeActive: active =>
                {
                    explicitAttackModeCount++;
                    explicitAttackModeActive = active;
                },
                setHudWorldMarkersVisible: visible =>
                {
                    worldMarkerVisibilityCount++;
                    worldMarkersVisible = visible;
                },
                processSelectionRectangleRequests: () =>
                {
                    rectangleDrainCount++;
                    em.GetBuffer<RtsSelectionPointerRequestElement>(commandEntity).Clear();
                },
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                });

            bool handled = new RtsSelectionCommandResultFlushSystem().ProcessSelectAllCommandRequests(flushContext);

            Assert.IsTrue(handled);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionPointerRequestElement>(commandEntity).Length);
            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            Assert.AreEqual((int)TacticalCommandMode.None, inputState.ActiveCommandMode);
            Assert.AreEqual(1, clearCommandModeCount);
            Assert.AreEqual(1, explicitAttackModeCount);
            Assert.IsFalse(explicitAttackModeActive);
            Assert.AreEqual(1, worldMarkerVisibilityCount);
            Assert.IsFalse(worldMarkersVisible);
            Assert.AreEqual(1, rectangleDrainCount);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ImmediateDestroyFallback_DeletesSelectedBuildingThroughResultBoundary()
    {
        using World world = new("ImmediateDestroyFallback_DeletesSelectedBuildingThroughResultBoundary");
        int deleteCount = 0;
        int clearHudSelectionCount = 0;
        TacticalCommandResult feedback = default;
        bool hasFeedback = false;
        var buildingInteraction = new BuildingPlacementInteractionSystem();
        var buildingContext = new BuildingPlacementInteractionSystem.Context(
            null,
            null,
            () => true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            () => deleteCount++,
            null,
            null,
            null,
            null);
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            result =>
            {
                feedback = result;
                hasFeedback = true;
            },
            world.EntityManager,
            buildingPlacementInteractionSystem: buildingInteraction,
            buildingPlacementInteractionContext: buildingContext,
            clearHudSelection: () => clearHudSelectionCount++);

        bool handled = new RtsSelectionCommandResultFlushSystem().TryProcessSelectedBuildingDestroyFallback(
            flushContext,
            RtsSelectionCommandIntentKind.DestroyFocusedUnit,
            accepted: false,
            rejectionReason: TacticalCommandReasonCode.NoSelection);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, deleteCount);
        Assert.AreEqual(1, clearHudSelectionCount);
        Assert.IsTrue(hasFeedback);
        Assert.IsTrue(feedback.Accepted);
        Assert.AreEqual("Destroyed selected building.", feedback.Message);
    }

    [Test]
    public void DeselectAllFlush_ClearsManagedSelectionCacheAndPresentation()
    {
        using World world = new("DeselectAllFlush_ClearsManagedSelectionCacheAndPresentation");
        EntityManager em = world.EntityManager;
        Entity selectedUnit = em.CreateEntity(typeof(SelectedUnitTag));
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.DeselectAll,
            RequestId = 77,
            Frame = 100
        });

        var selectionState = new SelectionStateSystem();
        selectionState.CachedSelectedMoveEntities.Add(selectedUnit);
        selectionState.SetFocusedUnit(selectedUnit);
        int clearFocusedCount = 0;
        int clearSelectionCount = 0;
        int clearCommandModeCount = 0;
        int worldMarkerVisibilityCount = 0;
        bool worldMarkersVisible = true;
        int cameraDraggingCount = 0;
        bool cameraDragging = true;
        int explicitAttackModeCount = 0;
        bool explicitAttackModeActive = true;
        RtsSelectionCommandResultFlushSystem.Context flushContext = CreateFlushContext(
            null,
            default,
            default,
            default,
            null,
            em,
            selectionStateSystem: selectionState,
            clearHudSelection: () => clearSelectionCount++,
            clearHudCommandMode: () => clearCommandModeCount++,
            setExplicitAttackTargetModeActive: active =>
            {
                explicitAttackModeCount++;
                explicitAttackModeActive = active;
            },
            setHudWorldMarkersVisible: visible =>
            {
                worldMarkerVisibilityCount++;
                worldMarkersVisible = visible;
            },
            setCameraDragging: dragging =>
            {
                cameraDraggingCount++;
                cameraDragging = dragging;
            },
            clearFocusedUnit: state =>
            {
                clearFocusedCount++;
                state.ClearFocusedUnit();
            });

        bool handled = new RtsSelectionCommandResultFlushSystem().ProcessDeselectAllCommandRequests(flushContext);

        Assert.IsTrue(handled);
        Assert.IsFalse(em.HasComponent<SelectedUnitTag>(selectedUnit));
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(0, selectionState.CachedSelectedMoveEntities.Count);
        Assert.AreEqual(Entity.Null, selectionState.FocusedUnit);
        Assert.AreEqual(1, clearFocusedCount);
        Assert.AreEqual(1, clearSelectionCount);
        Assert.AreEqual(1, clearCommandModeCount);
        Assert.AreEqual(1, explicitAttackModeCount);
        Assert.IsFalse(explicitAttackModeActive);
        Assert.AreEqual(1, worldMarkerVisibilityCount);
        Assert.IsFalse(worldMarkersVisible);
        Assert.AreEqual(1, cameraDraggingCount);
        Assert.IsFalse(cameraDragging);
    }

    [Test]
    public void CommandIntentRequest_CarriesPreResolvedTargetData()
    {
        Entity target = new() { Index = 7, Version = 1 };
        Entity secondaryTarget = new() { Index = 8, Version = 1 };

        var request = new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 42,
            Frame = 123,
            TargetEntity = target,
            SecondaryTargetEntity = secondaryTarget,
            TargetCell = new int2(12, 34),
            WorldPosition = new float3(5f, 6f, 7f),
            ScreenPosition = new float2(800f, 450f),
            DragStart = new float2(100f, 200f),
            DragCurrent = new float2(300f, 400f),
            TargetKind = RtsSelectionCommandTargetKind.WorldPosition,
            ExplicitAttackTargetMode = 1,
            HasTargetEntity = 1,
            HasSecondaryTargetEntity = 1,
            HasTargetCell = 1,
            HasWorldPosition = 1,
            HasScreenPosition = 1,
            HasScreenRect = 1
        };

        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, request.Kind);
        Assert.AreEqual(target, request.TargetEntity);
        Assert.AreEqual(secondaryTarget, request.SecondaryTargetEntity);
        Assert.AreEqual(new int2(12, 34), request.TargetCell);
        Assert.AreEqual(new float3(5f, 6f, 7f), request.WorldPosition);
        Assert.AreEqual(RtsSelectionCommandTargetKind.WorldPosition, request.TargetKind);
        Assert.AreEqual(1, request.HasTargetEntity);
        Assert.AreEqual(1, request.HasWorldPosition);
        Assert.AreEqual(1, request.HasScreenPosition);
        Assert.AreEqual(1, request.HasScreenRect);
    }

    [Test]
    public void CommandResult_CarriesFeedbackMarkerAndLifetimeData()
    {
        Entity target = new() { Index = 17, Version = 2 };

        var result = new RtsSelectionCommandResultElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            RequestId = 77,
            Frame = 456,
            TargetEntity = target,
            TargetCell = new int2(21, 43),
            ScreenPosition = new float2(512f, 256f),
            WorldPosition = new float3(9f, 0f, 11f),
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            CommandMode = (int)TacticalCommandMode.Board,
            HasCommandResult = 1,
            Accepted = 0,
            ReasonCode = (int)TacticalCommandReasonCode.TransportFull,
            FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
            FeedbackDurationSeconds = 2.25f,
            EmitScreenMarker = 1,
            MarkerFactionId = 2,
            HasSourceEntity = 1,
            DeferredToSource = 1,
            HasTargetEntity = 1,
            HasTargetCell = 1,
            HasWorldPosition = 1,
            ShowWorldMarkers = 1,
            RevealedCount = 3,
            RadiusCells = 5,
            Message = new FixedString64Bytes("Transport is full.")
        };

        Assert.AreEqual(RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger, result.Kind);
        Assert.AreEqual(target, result.TargetEntity);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, result.TargetKind);
        Assert.AreEqual((int)TacticalCommandMode.Board, result.CommandMode);
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TransportFull, result.ReasonCode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, result.FeedbackLifetime);
        Assert.AreEqual(2.25f, result.FeedbackDurationSeconds);
        Assert.AreEqual(1, result.EmitScreenMarker);
        Assert.AreEqual(1, result.ShowWorldMarkers);
        Assert.AreEqual(1, result.HasSourceEntity);
        Assert.AreEqual(1, result.DeferredToSource);
        Assert.AreEqual("Transport is full.", result.Message.ToString());
    }

    [Test]
    public void TacticalCommandReasonCodes_IncludeTransportFailureCodes()
    {
        Assert.AreEqual("Select a transport vehicle or aircraft first.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidTransport));
        Assert.AreEqual("Select units or cargo that can board.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidPassenger));
        Assert.AreEqual("Transport is full.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.TransportFull));
        Assert.AreEqual("No nearby units can board this transport.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoEligiblePassengers));
        Assert.AreEqual("No clear exit point for passengers.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoDisembarkCell));
        Assert.AreEqual("Passenger is not inside this transport.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.TransportPassengerMissing));
    }

    [Test]
    public void ScanCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandRequestResultContractTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 1,
            Frame = 10,
            HasScreenPosition = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 2,
            Frame = 10,
            HasScreenPosition = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 3,
            Frame = 11,
            HasScreenPosition = 1
        });

        using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        var processor = new ScanIntelCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptyGridConfigQuery,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, requests[0].Kind);
        Assert.AreEqual(2, requests[0].RequestId);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(1, results[0].RequestId);
        Assert.AreEqual(3, results[1].RequestId);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[1].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Scan, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.ScanUnavailable, results[0].ReasonCode);
        Assert.AreEqual((int)TacticalCommandReasonCode.ScanUnavailable, results[1].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptyGridConfigQuery,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(2, results.Length);
    }

    [Test]
    public void MoveCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandMoveProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Move, RequestId = 4, Frame = 20 });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Scan, RequestId = 5, Frame = 20 });

        using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        var processor = new SelectedMoveOrderCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptySelectedMoveQuery,
            emptyGridConfigQuery,
            emptyMapSurfaceQuery,
            null,
            new UnitMoveOrderSystem(),
            null,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Move, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoSelection, results[0].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            emptySelectedMoveQuery,
            emptyGridConfigQuery,
            emptyMapSurfaceQuery,
            null,
            new UnitMoveOrderSystem(),
            null,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void MoveCommandProcessor_ReacquiresResultBufferAfterMoveOrderStructuralWrites()
    {
        using World world = new("SelectionCommandMoveProcessorStructuralWriteTests");
        EntityManager em = world.EntityManager;
        NativeArray<int> blockerCounts = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            Entity commandEntity = em.CreateEntity();
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            DynamicBuffer<RtsSelectionCommandResultElement> results =
                em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                RequestId = 44,
                Frame = 120,
                ScreenPosition = new float2(15f, 25f),
                HasScreenPosition = 1
            });

            Entity selectedUnit = em.CreateEntity(
                typeof(SelectedUnitTag),
                typeof(UnitGrid),
                typeof(UnitMove),
                typeof(Faction));
            em.SetComponentData(selectedUnit, new UnitGrid { Cell = new int2(1, 1) });
            em.SetComponentData(selectedUnit, new UnitMove { Speed = 4f, WalkSpeed = 4f, ArriveDistance = 0.1f });
            em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
            CreateWalkableGrid(em, 8, 8, out blockerCounts, out friendlyPassFactionIds, out blocked, out occupied);

            using EntityQuery selectedMoveQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            var processor = new SelectedMoveOrderCommandSystem();

            bool handled = processor.ProcessCommandIntentRequests(
                em,
                commandEntity,
                requests,
                results,
                selectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                null,
                new UnitMoveOrderSystem(),
                TryGetNoClickedUnit,
                ResolveClickedCell(new int2(3, 3), new UnityEngine.Vector3(3.5f, 0f, 3.5f)));

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(handled);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(1, results[0].Accepted);
            Assert.AreEqual(44, results[0].RequestId);
            Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(selectedUnit));
            Assert.IsTrue(em.HasComponent<UnitTarget>(selectedUnit));
            Assert.IsTrue(em.HasComponent<UnitPathRequest>(selectedUnit));
        }
        finally
        {
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
        }
    }

    [Test]
    public void MoveCommandProcessor_ReacquiresCommandBuffersAfterCallerStructuralChange()
    {
        using World world = new("SelectionCommandMoveProcessorBufferRefreshTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            RequestId = 45,
            Frame = 121,
            ScreenPosition = new float2(15f, 25f),
            HasScreenPosition = 1
        });

        em.CreateEntity(typeof(RtsSelectionInputRequestQueueComponent));

        using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        var processor = new SelectedMoveOrderCommandSystem();
        bool handled = false;

        Assert.DoesNotThrow(() =>
        {
            handled = processor.ProcessCommandIntentRequests(
                em,
                commandEntity,
                emptySelectedMoveQuery,
                emptyGridConfigQuery,
                emptyMapSurfaceQuery,
                null,
                new UnitMoveOrderSystem(),
                null,
                null);
        });

        requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.IsTrue(handled);
        Assert.AreEqual(0, requests.Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandReasonCode.NoSelection, results[0].ReasonCode);
    }

    [Test]
    public void AttackCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandAttackProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 6,
            Frame = 30,
            ExplicitAttackTargetMode = 1
        });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Move, RequestId = 7, Frame = 30 });
        var processor = new AttackOrderCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            TryGetNoClickedUnit,
            null,
            null,
            default);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Move, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Attack, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandTargetKind.None, results[0].TargetKind);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Transient, results[0].FeedbackLifetime);
        Assert.AreEqual((int)TacticalCommandReasonCode.TargetNotAttackable, results[0].ReasonCode);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            TryGetNoClickedUnit,
            null,
            null,
            default);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void AttackCommandSystem_OnUpdateConsumesPreResolvedEntityRequest()
    {
        using World world = new("SelectionCommandAttackSystemOnUpdateTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Entity attacker = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitGrid),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(attacker, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
        em.SetComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(attacker, new UnitAttack { Range = 20f, Damage = 10, CooldownSeconds = 1f });
        em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(7, 8) });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(7.5f, 0f, 8.5f)));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 66,
            Frame = 44,
            TargetEntity = target,
            TargetKind = RtsSelectionCommandTargetKind.Entity,
            HasTargetEntity = 1
        });

        SystemHandle system = world.CreateSystem<AttackOrderCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Attack, results[0].Kind);
        Assert.AreEqual(66, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, results[0].TargetKind);
        Assert.AreEqual(target, results[0].TargetEntity);
        Assert.IsTrue(em.HasComponent<EngageTarget>(attacker));
        EngageTarget engageTarget = em.GetComponentData<EngageTarget>(attacker);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(7, 8), engageTarget.Cell);
        Assert.AreEqual(1, engageTarget.IsCommanded);
    }

    [Test]
    public void ScanCommandSystem_OnUpdateConsumesPreResolvedCellRequest()
    {
        using World world = new("SelectionCommandScanSystemOnUpdateTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(6, 5) });
        em.SetComponentData(target, new UnitHealth { Current = 10, Max = 10 });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(6.5f, 0f, 5.5f)));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 77,
            Frame = 88,
            TargetCell = new int2(5, 5),
            WorldPosition = new float3(5.5f, 0f, 5.5f),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasTargetCell = 1,
            HasWorldPosition = 1
        });

        SystemHandle system = world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(77, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasCommandResult);
        Assert.AreEqual(new int2(5, 5), results[0].TargetCell);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Cell, results[0].TargetKind);
        Assert.AreEqual(1, results[0].HasWorldPosition);
        Assert.AreEqual(1, results[0].RevealedCount);
        Assert.IsTrue(em.HasComponent<ScanIntelRevealedTag>(target));
        Assert.IsTrue(em.HasComponent<ScanIntelLastSeen>(target));
    }

    [Test]
    public void ScanCommandSystem_RevealsEnemyBuildingInsideRadius()
    {
        using World world = new("SelectionCommandScanBuildingRevealTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputStateComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity building = em.CreateEntity(
            typeof(Faction),
            typeof(RuntimeBuildingCombatInfo),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(building, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(building, new RuntimeBuildingCombatInfo
        {
            RuntimeBuildingId = 9,
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            OriginCell = new int2(8, 7),
            FootprintCells = new int2(2, 2)
        });
        em.SetComponentData(building, new UnitHealth { Current = 150, Max = 150 });
        em.SetComponentData(building, LocalTransform.FromPosition(new float3(8.5f, 0f, 7.5f)));

        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 82,
            Frame = 93,
            TargetCell = new int2(7, 7),
            WorldPosition = new float3(7.5f, 0f, 7.5f),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasTargetCell = 1,
            HasWorldPosition = 1
        });

        SystemHandle system = world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].RevealedCount);
        Assert.IsTrue(em.HasComponent<ScanIntelRevealedTag>(building));
        Assert.IsTrue(em.HasComponent<ScanIntelLastSeen>(building));
        ScanIntelLastSeen lastSeen = em.GetComponentData<ScanIntelLastSeen>(building);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, lastSeen.FactionId);
    }

    [Test]
    public void ScanCommandSystem_SelectedScannerQueuesUnitScanOrder()
    {
        using World world = new("SelectionCommandSelectedScannerOrderTests");
        EntityManager em = world.EntityManager;
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
        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(21, 20) });
        em.SetComponentData(target, new UnitHealth { Current = 10, Max = 10 });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(21.5f, 0f, 20.5f)));

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

        SystemHandle system = world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(78, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasSourceEntity);
        Assert.AreEqual(scanner, results[0].SourceEntity);
        Assert.AreEqual(0, results[0].RevealedCount);
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        Assert.IsTrue(em.HasComponent<UnitTarget>(scanner));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(scanner));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(scanner));
        Assert.AreEqual(new int2(20, 20), em.GetComponentData<UnitTarget>(scanner).Cell);
        Assert.IsFalse(em.HasComponent<ScanIntelRevealedTag>(target));
    }

    [Test]
    public void ScanCommandSystem_SelectedCombatUnitQueuesReducedRadiusScanOrder()
    {
        using World world = new("SelectionCommandSelectedCombatScanOrderTests");
        EntityManager em = world.EntityManager;
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

        Entity scanner = CreateSelectedCombatScanUnit(em, new int2(2, 2));
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Scan,
            RequestId = 79,
            Frame = 90,
            TargetCell = new int2(20, 20),
            WorldPosition = new float3(20.5f, 0f, 20.5f),
            TargetKind = RtsSelectionCommandTargetKind.Cell,
            HasTargetCell = 1,
            HasWorldPosition = 1
        });

        SystemHandle system = world.CreateSystem<ScanIntelCommandSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity).Length);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, results[0].Kind);
        Assert.AreEqual(79, results[0].RequestId);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(1, results[0].HasSourceEntity);
        Assert.AreEqual(scanner, results[0].SourceEntity);
        Assert.AreEqual(ScanIntelCommandSystem.DefaultCombatUnitScanRadiusCells, results[0].RadiusCells);
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        Assert.AreEqual(
            ScanIntelCommandSystem.DefaultCombatUnitScanRadiusCells,
            em.GetComponentData<UnitScanOrder>(scanner).RadiusCells);
    }

    [Test]
    public void UnitScanOrderExecutionSystem_RevealsWhenScannerReachesScanArea()
    {
        using World world = new("UnitScanOrderExecutionSystem_RevealsWhenScannerReachesScanArea");
        EntityManager em = world.EntityManager;
        world.SetTime(new TimeData(2d, 0.1f));
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(18, 20));
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 79,
            StartedFrame = 90,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            DurationSeconds = 2f,
            EngageDetectedTargets = 1
        });

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = new int2(21, 20) });
        em.SetComponentData(target, new UnitHealth { Current = 10, Max = 10 });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(21.5f, 0f, 20.5f)));

        SystemHandle orderSystem = world.CreateSystem<UnitScanOrderExecutionSystem>();
        orderSystem.Update(world.Unmanaged);
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        UnitScanOrder activeOrder = em.GetComponentData<UnitScanOrder>(scanner);
        Assert.AreEqual(1, activeOrder.HasStarted);
        Assert.AreEqual(2f, activeOrder.StartedTimeSeconds, 0.001f);

        SystemHandle scanSystem = world.CreateSystem<ScanIntelCommandSystem>();
        scanSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<ScanIntelRevealedTag>(target));
        Assert.IsTrue(em.HasComponent<ScanIntelLastSeen>(target));

        using EntityQuery feedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<ScanIntelFeedQueueTag>(),
            ComponentType.ReadOnly<ScanIntelFeedEntry>());
        Assert.IsFalse(feedQuery.IsEmptyIgnoreFilter);
        Entity feedEntity = feedQuery.GetSingletonEntity();
        DynamicBuffer<ScanIntelFeedEntry> feed = em.GetBuffer<ScanIntelFeedEntry>(feedEntity);
        Assert.AreEqual(1, feed.Length);
        Assert.AreEqual(79, feed[0].RequestId);
        Assert.AreEqual(1, feed[0].HasSourceEntity);
        Assert.AreEqual(scanner, feed[0].SourceEntity);
        Assert.AreEqual(1, feed[0].RevealedCount);

        world.SetTime(new TimeData(4.2d, 0.1f));
        orderSystem.Update(world.Unmanaged);
        Assert.IsFalse(em.HasComponent<UnitScanOrder>(scanner));
    }

    [Test]
    public void UnitScanOrderExecutionSystem_GroundScannerPatrolsScanAreaAfterArrival()
    {
        using World world = new("UnitScanOrderExecutionSystem_GroundScannerPatrolsScanAreaAfterArrival");
        EntityManager em = world.EntityManager;
        world.SetTime(new TimeData(6d, 0.1f));
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(20, 20));
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 84,
            StartedFrame = 95,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            StartedTimeSeconds = 5f,
            NextRevealTimeSeconds = 7f,
            DurationSeconds = 10f,
            EngageDetectedTargets = 1,
            HasStarted = 1
        });

        SystemHandle orderSystem = world.CreateSystem<UnitScanOrderExecutionSystem>();
        orderSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        Assert.IsTrue(em.HasComponent<UnitTarget>(scanner));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(scanner));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(scanner));

        UnitScanOrder order = em.GetComponentData<UnitScanOrder>(scanner);
        int2 targetCell = em.GetComponentData<UnitTarget>(scanner).Cell;
        Assert.AreEqual(new int2(23, 20), targetCell);
        Assert.AreEqual(targetCell, em.GetComponentData<UnitPathRequest>(scanner).Goal);
        Assert.AreEqual(1, order.PatrolWaypointIndex);
        Assert.Greater(order.NextPatrolMoveTimeSeconds, 6f);
        Assert.LessOrEqual(math.cmax(math.abs(targetCell - order.CenterCell)), order.RadiusCells);
    }

    [Test]
    public void UnitAirMovementSystem_LandedRunwayScannerBeginsTakeoffBeforeRecon()
    {
        using World world = new("UnitAirMovementSystem_LandedRunwayScannerBeginsTakeoffBeforeRecon");
        EntityManager em = world.EntityManager;
        world.SetTime(new TimeData(1d, 0.25f));
        CreateAirMovementGrid(em);

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(2, 3));
        em.AddComponentData(scanner, new UnitAttack { Range = 6f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.AddComponentData(scanner, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 5f });
        em.AddComponentData(scanner, new UnitAirComponent
        {
            HomePosition = new float3(2.5f, 0f, 3.5f),
            HomeCell = new int2(2, 3),
            HomeInitialized = 1,
            UsesRunway = 1,
            Airborne = 0,
            TakeoffRolling = 0,
            RunwayTakeoffPosition = new float3(2.5f, 0f, 3.5f),
            RunwayTakeoffCell = new int2(2, 3),
            RunwayLandingPosition = new float3(7.5f, 0f, 3.5f),
            RunwayLandingCell = new int2(7, 3)
        });
        em.SetComponentData(scanner, LocalTransform.FromPosition(new float3(2.5f, 0f, 3.5f)));
        em.AddComponentData(scanner, new UnitTarget { Cell = new int2(20, 20) });
        em.AddComponent<ManualMoveOrderTag>(scanner);
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 85,
            StartedFrame = 96,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            DurationSeconds = 8f
        });

        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        airMovementSystem.Update(world.Unmanaged);

        UnitAirComponent air = em.GetComponentData<UnitAirComponent>(scanner);
        Assert.AreEqual(1, air.TakeoffRolling);
        Assert.AreEqual(0, air.Airborne);
        Assert.AreEqual(0, air.ReturningHome);
        Assert.IsTrue(em.HasComponent<UnitTarget>(scanner));
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
    }

    [Test]
    public void UnitAirMovementSystem_AirborneScannerLoitersInsteadOfLandingDuringActiveScan()
    {
        using World world = new("UnitAirMovementSystem_AirborneScannerLoitersInsteadOfLandingDuringActiveScan");
        EntityManager em = world.EntityManager;
        world.SetTime(new TimeData(2d, 0.25f));
        CreateAirMovementGrid(em);

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(20, 20));
        em.AddComponentData(scanner, new UnitAttack { Range = 6f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.AddComponentData(scanner, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 5f });
        em.AddComponentData(scanner, new UnitAirComponent
        {
            HomePosition = new float3(2.5f, 0f, 3.5f),
            HomeCell = new int2(2, 3),
            HomeInitialized = 1,
            UsesRunway = 1,
            Airborne = 1,
            ReturningHome = 0,
            TakeoffRolling = 0,
            LandingRolling = 0,
            RunwayTakeoffPosition = new float3(2.5f, 0f, 3.5f),
            RunwayTakeoffCell = new int2(2, 3),
            RunwayLandingPosition = new float3(7.5f, 0f, 3.5f),
            RunwayLandingCell = new int2(7, 3)
        });
        float3 startPosition = new(20.5f, 12f, 20.5f);
        em.SetComponentData(scanner, LocalTransform.FromPosition(startPosition));
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 86,
            StartedFrame = 97,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            StartedTimeSeconds = 2f,
            NextRevealTimeSeconds = 3f,
            DurationSeconds = 8f,
            HasStarted = 1,
            ReturnHomeAfterCompletion = 1
        });

        SystemHandle airMovementSystem = world.CreateSystem<UnitAirMovementSystem>();
        airMovementSystem.Update(world.Unmanaged);

        UnitAirComponent air = em.GetComponentData<UnitAirComponent>(scanner);
        LocalTransform transform = em.GetComponentData<LocalTransform>(scanner);
        Assert.AreEqual(1, air.Airborne);
        Assert.AreEqual(0, air.ReturningHome);
        Assert.AreEqual(0, air.LandingRolling);
        Assert.AreEqual(0, air.ReturnApproachInitialized);
        Assert.AreEqual(startPosition, transform.Position);
        Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
    }

    [Test]
    public void UnitScanOrderExecutionSystem_ReturnsAirScannerHomeWhenScanExpires()
    {
        using World world = new("UnitScanOrderExecutionSystem_ReturnsAirScannerHomeWhenScanExpires");
        EntityManager em = world.EntityManager;
        world.SetTime(new TimeData(5d, 0.1f));

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(20, 20));
        em.AddComponentData(scanner, new UnitAirMovement { CruiseHeight = 12f, RunwayTaxiSpeed = 5f });
        em.AddComponentData(scanner, new UnitAirComponent
        {
            HomePosition = new float3(3.5f, 0f, 3.5f),
            HomeCell = new int2(3, 3),
            HomeInitialized = 1,
            ReturningHome = 0,
            Airborne = 1,
            UsesRunway = 1,
            TakeoffRolling = 1,
            AttackRunActive = 1,
            ReturnApproachInitialized = 1,
            RunwayTakeoffPosition = new float3(2.5f, 0f, 3.5f),
            RunwayTakeoffCell = new int2(2, 3),
            RunwayLandingPosition = new float3(7.5f, 0f, 3.5f),
            RunwayLandingCell = new int2(7, 3)
        });
        em.AddComponentData(scanner, new UnitTarget { Cell = new int2(20, 20) });
        em.AddComponentData(scanner, new UnitPathRequest { Goal = new int2(20, 20) });
        em.AddComponent<ManualMoveOrderTag>(scanner);

        Entity target = CreateScanEngagementTarget(em, new int2(22, 20));
        em.AddComponentData(scanner, new EngageTarget
        {
            Target = target,
            Cell = new int2(22, 20),
            Position = em.GetComponentData<LocalTransform>(target).Position,
            IsCommanded = 0
        });
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 83,
            StartedFrame = 94,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            StartedTimeSeconds = 1f,
            DurationSeconds = 2f,
            EngageDetectedTargets = 1,
            ReturnHomeAfterCompletion = 1,
            HasStarted = 1
        });

        SystemHandle orderSystem = world.CreateSystem<UnitScanOrderExecutionSystem>();
        orderSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitScanOrder>(scanner));
        Assert.IsFalse(em.HasComponent<UnitTarget>(scanner));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(scanner));
        Assert.IsFalse(em.HasComponent<ManualMoveOrderTag>(scanner));
        Assert.IsFalse(em.HasComponent<EngageTarget>(scanner));

        UnitAirComponent air = em.GetComponentData<UnitAirComponent>(scanner);
        Assert.AreEqual(1, air.ReturningHome);
        Assert.AreEqual(1, air.Airborne);
        Assert.AreEqual(0, air.AttackRunActive);
        Assert.AreEqual(0, air.TakeoffRolling);
        Assert.AreEqual(0, air.ReturnApproachInitialized);
    }

    [Test]
    public void UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea()
    {
        using World world = new("UnitEngagementSystem_ScanOrderAcquiresOnlyTargetsInsideScanArea");
        EntityManager em = world.EntityManager;
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });

        Entity scanner = CreateSelectedScanCapableUnit(em, new int2(20, 20));
        em.AddComponentData(scanner, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 1 });
        em.AddComponentData(scanner, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.AddComponentData(scanner, new UnitScanOrder
        {
            RequestId = 80,
            StartedFrame = 91,
            SourceEntity = scanner,
            CenterCell = new int2(20, 20),
            CenterWorld = new float3(20.5f, 0f, 20.5f),
            RadiusCells = 4,
            DurationSeconds = 5f,
            EngageDetectedTargets = 1,
            HasStarted = 1
        });

        Entity insideTarget = CreateScanEngagementTarget(em, new int2(23, 20));
        Entity outsideTarget = CreateScanEngagementTarget(em, new int2(30, 20));

        var endSimulation = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        SystemHandle engagementSystem = world.CreateSystem<UnitEngagementSystem>();

        world.SetTime(new TimeData(1d, 0.2f));
        engagementSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();
        endSimulation.Update();

        Assert.IsTrue(em.HasComponent<EngageTarget>(scanner));
        EngageTarget engage = em.GetComponentData<EngageTarget>(scanner);
        Assert.AreEqual(insideTarget, engage.Target);
        Assert.AreNotEqual(outsideTarget, engage.Target);
    }

    [Test]
    public void UnitEngagedMovementSystem_ClearsScanTargetsOutsideScanArea()
    {
        using World world = new("UnitEngagedMovementSystem_ClearsScanTargetsOutsideScanArea");
        EntityManager em = world.EntityManager;
        NativeArray<int> blockerCounts = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        try
        {
            CreateWalkableGrid(em, 64, 64, out blockerCounts, out friendlyPassFactionIds, out blocked, out occupied);

            Entity scanner = CreateSelectedScanCapableUnit(em, new int2(20, 20));
            em.AddComponentData(scanner, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 1, ChaseBreakDistance = 100f });
            em.AddComponentData(scanner, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
            em.AddComponentData(scanner, new UnitFootprint { Size = new int2(1, 1) });
            em.AddComponentData(scanner, new UnitMovementBehavior { UsesVehicleMotion = 1 });
            em.AddComponentData(scanner, new UnitVehicleMovement
            {
                TurnSpeedDegrees = 180f,
                Acceleration = 10f,
                Braking = 10f
            });
            em.AddComponentData(scanner, new UnitVehicleKinematics());
            em.AddComponentData(scanner, new UnitScanOrder
            {
                RequestId = 81,
                StartedFrame = 92,
                SourceEntity = scanner,
                CenterCell = new int2(20, 20),
                CenterWorld = new float3(20.5f, 0f, 20.5f),
                RadiusCells = 4,
                DurationSeconds = 5f,
                EngageDetectedTargets = 1,
                HasStarted = 1
            });

            Entity outsideTarget = CreateScanEngagementTarget(em, new int2(30, 20));
            float3 outsidePosition = em.GetComponentData<LocalTransform>(outsideTarget).Position;
            em.AddComponentData(scanner, new EngageTarget
            {
                Target = outsideTarget,
                Cell = new int2(30, 20),
                Position = outsidePosition,
                IsCommanded = 0
            });

            SystemHandle movementSystem = world.CreateSystem<UnitEngagedMovementSystem>();
            world.SetTime(new TimeData(1d, 0.2f));
            movementSystem.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();

            EngageTarget engage = em.GetComponentData<EngageTarget>(scanner);
            Assert.AreEqual(Entity.Null, engage.Target);
            Assert.AreEqual(default(int2), engage.Cell);
            Assert.AreEqual(default(float3), engage.Position);
        }
        finally
        {
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
        }
    }

    [Test]
    public void TransportCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds()
    {
        using World world = new("SelectionCommandTransportProcessorTests");
        EntityManager em = world.EntityManager;
        Entity commandEntity = em.CreateEntity();
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.DisembarkTransport, RequestId = 8, Frame = 40 });
        requests.Add(new RtsSelectionCommandIntentRequestElement { Kind = RtsSelectionCommandIntentKind.Scan, RequestId = 9, Frame = 40 });
        var processor = new TransportBoardingCommandSystem();

        bool handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            null,
            null,
            null);

        Assert.IsTrue(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.Scan, requests[0].Kind);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.DisembarkTransport, results[0].Kind);
        Assert.AreEqual((int)TacticalCommandMode.Board, results[0].CommandMode);
        Assert.AreEqual(RtsSelectionCommandFeedbackLifetime.Hidden, results[0].FeedbackLifetime);
        Assert.AreEqual(0, results[0].Accepted);

        handled = processor.ProcessCommandIntentRequests(
            em,
            commandEntity,
            requests,
            results,
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            new UnitMoveOrderSystem(),
            null,
            null,
            null);

        Assert.IsFalse(handled);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, results.Length);
    }

    [Test]
    public void ScanCommandFlush_DrainsResultsOnceAndDoesNotDuplicateFeedback()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandScanFlushTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(10f, 20f), 50));
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(30f, 40f), 51));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(2, requests.Length);
            Assert.AreEqual(0, results.Length);

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                emptyGridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em);

            bool processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(2, feedbackCount);

            processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsFalse(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(2, feedbackCount);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ScanCommandFlush_DeferredSelectedScannerFeedbackSaysScannerEnRoute()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandScanDeferredFeedbackTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(10f, 20f), 70));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(0, results.Length);

            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig
            {
                Width = 64,
                Height = 64,
                CellSize = 1f,
                Origin = float3.zero
            });
            Entity scanner = CreateSelectedScanCapableUnit(em, new int2(2, 2));

            using EntityQuery selectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            TacticalCommandResult lastResult = TacticalCommandResult.Success();
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                selectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                result =>
                {
                    feedbackCount++;
                    lastResult = result;
                },
                em,
                tryGetScanClickedCell: (UnityEngine.Vector2 screenPosition, EntityManager entityManager, out int2 cell, out UnityEngine.Vector3 worldPoint) =>
                {
                    cell = new int2(20, 20);
                    worldPoint = new UnityEngine.Vector3(20.5f, 0f, 20.5f);
                    return true;
                });

            bool processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(1, feedbackCount);
            Assert.IsTrue(lastResult.Accepted);
            Assert.AreEqual("SCAN ORDERED: SCANNER EN ROUTE", lastResult.Message);
            Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ScanCommandFlush_AcceptedOneShotScanClearsCommandMode()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandScanAcceptedOneShotClearTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            inputSystem.ArmCommandMode(
                TacticalCommandMode.Scan,
                frame: 90,
                oneShot: true,
                requiresWorldTarget: true);
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(10f, 20f), 91));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(0, results.Length);

            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig
            {
                Width = 64,
                Height = 64,
                CellSize = 1f,
                Origin = float3.zero
            });
            Entity scanner = CreateSelectedScanCapableUnit(em, new int2(2, 2));

            using EntityQuery selectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            TacticalCommandResult lastResult = TacticalCommandResult.Success();
            int clearHudCount = 0;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                selectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                result =>
                {
                    feedbackCount++;
                    lastResult = result;
                },
                em,
                clearHudCommandMode: () => clearHudCount++,
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                tryGetScanClickedCell: (UnityEngine.Vector2 screenPosition, EntityManager entityManager, out int2 cell, out UnityEngine.Vector3 worldPoint) =>
                {
                    cell = new int2(20, 20);
                    worldPoint = new UnityEngine.Vector3(20.5f, 0f, 20.5f);
                    return true;
                });

            bool processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(1, feedbackCount);
            Assert.IsTrue(lastResult.Accepted);
            Assert.AreEqual("SCAN ORDERED: SCANNER EN ROUTE", lastResult.Message);
            Assert.IsTrue(em.HasComponent<UnitScanOrder>(scanner));
            Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
            Assert.AreEqual(1, clearHudCount);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ScanCommandFlush_RejectedOneShotScanClearsCommandMode()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandScanRejectedFlushTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            inputSystem.ArmCommandMode(
                TacticalCommandMode.Scan,
                frame: 80,
                oneShot: true,
                requiresWorldTarget: true);
            Assert.IsTrue(inputSystem.QueueScanCommandRequest(new UnityEngine.Vector2(10f, 20f), 81));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(0, results.Length);

            Entity gridEntity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(gridEntity, new GridConfig
            {
                Width = 16,
                Height = 16,
                CellSize = 1f,
                Origin = float3.zero
            });

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            TacticalCommandResult lastResult = TacticalCommandResult.Success();
            int clearHudCount = 0;
            int cameraDraggingCount = 0;
            bool cameraDragging = true;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                result =>
                {
                    feedbackCount++;
                    lastResult = result;
                },
                em,
                clearHudCommandMode: () => clearHudCount++,
                setCameraDragging: dragging =>
                {
                    cameraDraggingCount++;
                    cameraDragging = dragging;
                },
                tryGetScanClickedCell: (UnityEngine.Vector2 screenPosition, EntityManager entityManager, out int2 cell, out UnityEngine.Vector3 worldPoint) =>
                {
                    cell = default;
                    worldPoint = default;
                    return false;
                });

            bool processed = flushSystem.ProcessScanCommandRequests(context);

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.IsTrue(processed);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(1, feedbackCount);
            Assert.IsFalse(lastResult.Accepted);
            Assert.AreEqual(TacticalCommandReasonCode.TargetOutOfBounds, lastResult.ReasonCode);
            Assert.IsFalse(inputSystem.TryGetActiveCommandMode(out _));
            Assert.AreEqual(1, clearHudCount);
            Assert.AreEqual(1, cameraDraggingCount);
            Assert.IsFalse(cameraDragging);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MoveCommandFlush_ShowsAcceptedWorldMarkerFromResult()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandMoveFlushMarkerTests");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        GameObject movePrefab = CreatePrimitiveMarkerPrefab("MoveFlushMarkerPrefab", PrimitiveType.Quad);
        GameObject attackPrefab = CreatePrimitiveMarkerPrefab("AttackFlushMarkerPrefab", PrimitiveType.Quad);
        GameObject runtimeRoot = new("MarkerRoot");
        var orderMarkers = new SelectionOrderMarkerSystem();
        try
        {
            EntityManager em = world.EntityManager;
            CreateWalkableGrid(em, 8, 8, out blockerCounts, out friendlyPassFactionIds, out blocked, out occupied);
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out _,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            results.Add(new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Move,
                RequestId = 501,
                Frame = 90,
                TargetCell = new int2(3, 4),
                WorldPosition = new float3(3.5f, 0f, 4.5f),
                ScreenPosition = new float2(300f, 220f),
                TargetKind = RtsSelectionCommandTargetKind.Cell,
                CommandMode = (int)TacticalCommandMode.Move,
                HasCommandResult = 1,
                Accepted = 1,
                EmitScreenMarker = 1,
                MarkerFactionId = FactionIdentity.PlayerFactionId,
                HasTargetCell = 1,
                HasWorldPosition = 1,
                ShowWorldMarkers = 1
            });

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            orderMarkers.Initialize(movePrefab, attackPrefab, null, null, 1f, runtimeRoot.transform);
            int feedbackCount = 0;
            bool hudWorldMarkersVisible = false;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em,
                orderMarkerSystem: orderMarkers,
                setHudWorldMarkersVisible: value => hudWorldMarkersVisible = value);

            flushSystem.ProcessMoveCommandRequests(context);

            Transform moveMarker = runtimeRoot.transform.Find("MoveOrderMarkerRuntime");
            Assert.IsNotNull(moveMarker);
            Assert.IsTrue(moveMarker.gameObject.activeSelf);
            Assert.AreEqual(3.5f, moveMarker.position.x, 0.001f);
            Assert.AreEqual(4.5f, moveMarker.position.z, 0.001f);
            Assert.AreEqual(1, feedbackCount);
            Assert.IsTrue(hudWorldMarkersVisible);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(movePrefab);
            UnityEngine.Object.DestroyImmediate(attackPrefab);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void AttackCommandFlush_ShowsAcceptedTargetMarkerFromResult()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandAttackFlushMarkerTests");
        World.DefaultGameObjectInjectionWorld = world;
        NativeArray<int> blockerCounts = default;
        NativeArray<byte> friendlyPassFactionIds = default;
        NativeBitArray blocked = default;
        NativeBitArray occupied = default;
        GameObject movePrefab = CreatePrimitiveMarkerPrefab("MoveFlushMarkerPrefab", PrimitiveType.Quad);
        GameObject attackPrefab = CreatePrimitiveMarkerPrefab("AttackFlushMarkerPrefab", PrimitiveType.Quad);
        GameObject targetPrefab = CreatePrimitiveMarkerPrefab("AttackTargetFlushMarkerPrefab", PrimitiveType.Cube);
        GameObject runtimeRoot = new("MarkerRoot");
        var orderMarkers = new SelectionOrderMarkerSystem();
        try
        {
            EntityManager em = world.EntityManager;
            CreateWalkableGrid(em, 8, 8, out blockerCounts, out friendlyPassFactionIds, out blocked, out occupied);
            Entity target = em.CreateEntity(typeof(LocalTransform), typeof(UnitFootprint), typeof(Faction), typeof(UnitHealth));
            em.SetComponentData(target, LocalTransform.FromPosition(new float3(5.5f, 0f, 6.5f)));
            em.SetComponentData(target, new UnitFootprint { Size = new int2(2, 3) });
            em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
            em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });

            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out _,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            results.Add(new RtsSelectionCommandResultElement
            {
                Kind = RtsSelectionCommandIntentKind.Attack,
                RequestId = 601,
                Frame = 91,
                TargetEntity = target,
                WorldPosition = new float3(5.5f, 0f, 6.5f),
                ScreenPosition = new float2(400f, 260f),
                TargetKind = RtsSelectionCommandTargetKind.Entity,
                CommandMode = (int)TacticalCommandMode.Attack,
                HasCommandResult = 1,
                Accepted = 1,
                EmitScreenMarker = 1,
                HasTargetEntity = 1,
                HasWorldPosition = 1,
                ShowWorldMarkers = 1
            });

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery gridConfigQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>(),
                ComponentType.ReadOnly<DynamicOccupancyComponent>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            orderMarkers.Initialize(movePrefab, attackPrefab, targetPrefab, null, 1f, runtimeRoot.transform);
            int feedbackCount = 0;
            bool hudWorldMarkersVisible = false;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                gridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em,
                orderMarkerSystem: orderMarkers,
                setHudWorldMarkersVisible: value => hudWorldMarkersVisible = value);

            bool issued = flushSystem.ProcessAttackCommandRequests(context, explicitAttackTargetModeActive: false);

            Transform attackMarker = runtimeRoot.transform.Find("AttackTargetSelectionMarkerRuntime");
            Assert.IsTrue(issued);
            Assert.IsNotNull(attackMarker);
            Assert.IsTrue(attackMarker.gameObject.activeSelf);
            Assert.AreEqual(5.5f, attackMarker.position.x, 0.001f);
            Assert.AreEqual(6.5f, attackMarker.position.z, 0.001f);
            Assert.AreEqual(1, feedbackCount);
            Assert.IsTrue(hudWorldMarkersVisible);
            Assert.AreEqual(0, em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(movePrefab);
            UnityEngine.Object.DestroyImmediate(attackPrefab);
            UnityEngine.Object.DestroyImmediate(targetPrefab);
            UnityEngine.Object.DestroyImmediate(runtimeRoot);
            if (blockerCounts.IsCreated)
                blockerCounts.Dispose();
            if (friendlyPassFactionIds.IsCreated)
                friendlyPassFactionIds.Dispose();
            if (blocked.IsCreated)
                blocked.Dispose();
            if (occupied.IsCreated)
                occupied.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MoveCommandFlush_ReacquiresCommandBuffersAfterQuerySetupStructuralChange()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new("SelectionCommandMoveFlushBufferRefreshTests");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager em = world.EntityManager;
            var inputSystem = new RtsSelectionInputSystem();
            Assert.IsTrue(inputSystem.QueueMoveCommandRequest(new UnityEngine.Vector2(10f, 20f), 70));
            Assert.IsTrue(inputSystem.TryGetCommandBuffers(
                out _,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
                out DynamicBuffer<RtsSelectionCommandResultElement> results));
            Assert.AreEqual(1, requests.Length);
            Assert.AreEqual(0, results.Length);

            using EntityQuery emptySelectedMoveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            using EntityQuery emptyGridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            using EntityQuery emptyMapSurfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            int feedbackCount = 0;
            var flushSystem = new RtsSelectionCommandResultFlushSystem();
            RtsSelectionCommandResultFlushSystem.Context context = CreateFlushContext(
                inputSystem,
                emptySelectedMoveQuery,
                emptyGridConfigQuery,
                emptyMapSurfaceQuery,
                _ => feedbackCount++,
                em,
                manager => manager.CreateEntity(typeof(RtsSelectionInputRequestQueueComponent)));

            Assert.DoesNotThrow(() => flushSystem.ProcessMoveCommandRequests(context));

            requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(0, results.Length);
            Assert.AreEqual(1, feedbackCount);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    private static GameObject CreatePrimitiveMarkerPrefab(string name, PrimitiveType primitiveType)
    {
        GameObject marker = GameObject.CreatePrimitive(primitiveType);
        marker.name = name;
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        return marker;
    }

    private static Entity CreateSelectedAttackUnit(EntityManager em)
    {
        Entity attackUnit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(attackUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(attackUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(attackUnit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(attackUnit, new UnitAttack { Range = 100f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.SetComponentData(attackUnit, LocalTransform.FromPosition(float3.zero));
        return attackUnit;
    }

    private static Entity CreateSelectedMovableUnit(EntityManager em)
    {
        Entity unit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitCombat));
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(2, 3) });
        em.SetComponentData(unit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        return unit;
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
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Drone_Recon") });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return unit;
    }

    private static Entity CreateSelectedCombatScanUnit(EntityManager em, int2 cell)
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
        em.SetComponentData(unit, new UnitMove { Speed = 4f, WalkSpeed = 4f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(unit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(unit, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Rifle_Squad") });
        em.SetComponentData(unit, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return unit;
    }

    private static Entity CreateAirMovementGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 64,
            Height = 64,
            CellSize = 1f,
            Origin = float3.zero
        });
        return gridEntity;
    }

    private static Entity CreateScanEngagementTarget(EntityManager em, int2 cell)
    {
        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.SetComponentData(target, new UnitGrid { Cell = cell });
        em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(target, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(target, new UnitAttack { Range = 4f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(cell.x + 0.5f, 0f, cell.y + 0.5f)));
        return target;
    }

    private static Entity CreateRadarAttackLauncher(EntityManager em, int2 cell)
    {
        Entity launcher = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitMove),
            typeof(UnitSourcePrefabKey),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(launcher, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(launcher, new UnitGrid { Cell = cell });
        em.SetComponentData(launcher, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(launcher, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(launcher, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Missle_Launcher_Ground") });
        em.SetComponentData(launcher, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(launcher, new UnitAttack { Range = 600f, CooldownSeconds = 1f, Damage = 100, TraceVisibleSeconds = 0.1f });
        em.SetComponentData(launcher, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        return launcher;
    }

    private static Entity CreateRadarAttackTarget(EntityManager em, byte factionId, int2 cell)
    {
        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(UnitMove),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = factionId });
        em.SetComponentData(target, new UnitGrid { Cell = cell });
        em.SetComponentData(target, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(target, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(target, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        return target;
    }

    private static Entity CreateThreatDetector(EntityManager em, byte factionId, ThreatDetectionKind kind, int2 cell, int radiusCells)
    {
        Entity detector = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitHealth),
            typeof(ThreatDetector));
        em.SetComponentData(detector, new Faction { Id = factionId });
        em.SetComponentData(detector, new UnitGrid { Cell = cell });
        em.SetComponentData(detector, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(detector, new ThreatDetector
        {
            Kind = (byte)kind,
            RadiusCells = radiusCells
        });
        return detector;
    }

    private static Entity CreateFocusedAttackUnit(EntityManager em)
    {
        Entity attackUnit = em.CreateEntity(
            typeof(Faction),
            typeof(UnitMove),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(LocalTransform));
        em.SetComponentData(attackUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(attackUnit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        em.SetComponentData(attackUnit, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(attackUnit, new UnitAttack { Range = 100f, CooldownSeconds = 1f, Damage = 10, TraceVisibleSeconds = 0.1f });
        em.SetComponentData(attackUnit, LocalTransform.FromPosition(float3.zero));
        return attackUnit;
    }

    private static Entity CreateSelectedNonAttackUnit(EntityManager em)
    {
        Entity unit = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitMove));
        em.SetComponentData(unit, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(unit, new UnitMove { Speed = 1f, WalkSpeed = 1f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
        return unit;
    }

    private static Entity CreateSelectedAirDefenseLauncher(EntityManager em)
    {
        Entity launcher = em.CreateEntity(
            typeof(SelectedUnitTag),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent));
        em.SetComponentData(launcher, new Faction { Id = FactionIdentity.PlayerFactionId });
        em.SetComponentData(launcher, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(launcher, new AirMissileLauncherComponent
        {
            MinRange = 8f,
            BaseDetectionRange = 220f,
            MaxDetectionRange = 420f,
            LockSeconds = 0.35f,
            LaunchDelaySeconds = 0.12f,
            ReloadSeconds = 1.8f,
            MissileSpeed = 95f,
            MissileTurnRateDegreesPerSecond = 220f,
            MissileLifetimeSeconds = 7f,
            ProximityFuseRadius = 4f,
            AirTargetDamage = 120,
            IncomingMissileDamage = 9999,
            TrackingQuality = 0.75f
        });
        em.SetComponentData(launcher, new AirMissileLauncherStateComponent { Phase = (byte)AirMissileLauncherPhase.Idle });
        return launcher;
    }

    private static bool TryGetNoClickedUnit(UnityEngine.Vector2 screenPosition, EntityManager em, out Entity entity)
    {
        entity = Entity.Null;
        return false;
    }

    private static SelectedMoveOrderCommandSystem.ClickedCellResolver ResolveClickedCell(
        int2 cell,
        UnityEngine.Vector3 worldPoint)
    {
        return (
            UnityEngine.Vector2 _,
            EntityManager _,
            out int2 resolvedCell,
            out UnityEngine.Vector3 resolvedWorldPoint) =>
        {
            resolvedCell = cell;
            resolvedWorldPoint = worldPoint;
            return true;
        };
    }

    private static Entity CreateWalkableGrid(
        EntityManager em,
        int width,
        int height,
        out NativeArray<int> blockerCounts,
        out NativeArray<byte> friendlyPassFactionIds,
        out NativeBitArray blocked,
        out NativeBitArray occupied)
    {
        int gridSize = width * height;
        blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);
        blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < friendlyPassFactionIds.Length; i++)
            friendlyPassFactionIds[i] = byte.MaxValue;

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
            Counts = blockerCounts,
            Blocked = blocked,
            FriendlyPassFactionIds = friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
            walkable[i] = new GridWalkable { Value = 1 };

        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
        roads.ResizeUninitialized(gridSize);
        sidewalks.ResizeUninitialized(gridSize);
        dirtRoads.ResizeUninitialized(gridSize);
        for (int i = 0; i < gridSize; i++)
        {
            roads[i] = new GridRoad { Value = 0 };
            sidewalks[i] = new GridRoadSidewalk { Value = 0 };
            dirtRoads[i] = new GridRoadDirt { Value = 0 };
        }

        return gridEntity;
    }

    private static RtsSelectionCommandResultFlushSystem.Context CreateFlushContext(
        RtsSelectionInputSystem inputSystem,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        System.Action<TacticalCommandResult> applyHudCommandResult,
        EntityManager em,
        System.Action<EntityManager> ensureEntityQueries = null,
        SelectionOrderMarkerSystem orderMarkerSystem = null,
        System.Action<bool> setHudWorldMarkersVisible = null,
        System.Action<UnityEngine.Vector2> requestMoveOrderScreenMarker = null,
        System.Action<UnityEngine.Vector2> requestAttackOrderScreenMarker = null,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem = null,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        SelectionStateSystem selectionStateSystem = null,
        System.Action clearHudSelection = null,
        System.Action clearHudCommandMode = null,
        System.Action<TacticalCommandMode> applyHudCommandMode = null,
        System.Action<BoardCommandModeDirection, bool> applyHudBoardCommandMode = null,
        System.Action<bool> setExplicitAttackTargetModeActive = null,
        System.Action<bool> setCameraDragging = null,
        System.Action processSelectionRectangleRequests = null,
        System.Action<string> logSelectionClickDiagnostic = null,
        System.Action<SelectionStateSystem> clearFocusedUnit = null,
        RtsSelectionCommandResultFlushSystem.RefreshFocusedUnitAction refreshFocusedUnit = null,
        RtsSelectionCommandResultFlushSystem.SetFocusedUnitAction setFocusedUnit = null,
        RtsSelectionCommandResultFlushSystem.ApplyHudSelectionAction applyHudSelection = null,
        RtsSelectionCommandResultFlushSystem.ClearCurrentSelectionAction clearCurrentSelection = null,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetScanClickedCell = null)
    {
        return new RtsSelectionCommandResultFlushSystem.Context(
            inputSystem,
            new SelectionHudFeedbackBoundary(),
            orderMarkerSystem ?? new SelectionOrderMarkerSystem(),
            new SelectedMoveOrderCommandSystem(),
            new AttackOrderCommandSystem(),
            new ScanIntelCommandSystem(),
            new TransportBoardingCommandSystem(),
            new UnitMoveOrderSystem(),
            new UnitTransportCapacitySystem(),
            new UnitTransportAirPickupSystem(),
            selectionStateSystem ?? new SelectionStateSystem(),
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            selectedMoveQuery,
            selectedMoveQuery,
            gridConfigQuery,
            mapSurfaceQuery,
            TryGetEntityManager,
            ensureEntityQueries,
            clearCurrentSelection,
            applyHudCommandMode,
            applyHudBoardCommandMode,
            applyHudCommandResult,
            clearHudSelection,
            applyHudSelection,
            clearHudCommandMode,
            setExplicitAttackTargetModeActive,
            setHudWorldMarkersVisible,
            processSelectionRectangleRequests,
            logSelectionClickDiagnostic,
            requestMoveOrderScreenMarker,
            requestAttackOrderScreenMarker,
            setCameraDragging,
            clearFocusedUnit,
            refreshFocusedUnit,
            setFocusedUnit,
            null,
            null,
            tryGetScanClickedCell,
            null,
            null,
            null);

        bool TryGetEntityManager(out EntityManager resolvedEntityManager)
        {
            resolvedEntityManager = em;
            return true;
        }
    }
}
#endif
