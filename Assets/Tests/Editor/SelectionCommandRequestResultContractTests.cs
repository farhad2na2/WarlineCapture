#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
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
            tests.TransportCommandProcessor_ConsumesMatchingRequestsOnceAndLeavesOtherKinds();
            tests.ScanCommandFlush_DrainsResultsOnceAndDoesNotDuplicateFeedback();
            tests.MoveCommandFlush_ShowsAcceptedWorldMarkerFromResult();
            tests.AttackCommandFlush_ShowsAcceptedTargetMarkerFromResult();
            tests.MoveCommandFlush_ReacquiresCommandBuffersAfterQuerySetupStructuralChange();
            tests.ImmediateDestroyFallback_DeletesSelectedBuildingThroughResultBoundary();
            tests.SelectAllFlush_DrainsRectangleBoundaryAndPresentationCleanup();
            tests.CancelActiveCommandModeFlush_ClearsPresentationWithoutPersistentFeedback();
            tests.MoveTargetModeFlush_AppliesAcceptedPresentationCleanup();
            tests.MoveTargetModeFlush_AppliesRejectedPresentationCleanup();
            tests.DeselectAllFlush_ClearsManagedSelectionCacheAndPresentation();
            UnityEngine.Debug.Log("[SelectionCommandRequestResultContractValidation] result=Passed tests=21");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            UnityEngine.Debug.LogError("[SelectionCommandRequestResultContractValidation] result=Failed");
            EditorApplication.Exit(1);
        }
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
            em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
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
        Assert.AreEqual("Transport is full.", result.Message.ToString());
    }

    [Test]
    public void TacticalCommandReasonCodes_IncludeTransportFailureCodes()
    {
        Assert.AreEqual("Select a transport vehicle or aircraft first.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidTransport));
        Assert.AreEqual("Select soldiers that can board.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.InvalidPassenger));
        Assert.AreEqual("Transport is full.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.TransportFull));
        Assert.AreEqual("No nearby soldiers can board this transport.", TacticalCommandFeedbackText.ToDisplayText(TacticalCommandReasonCode.NoEligiblePassengers));
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
            em.SetComponentData(selectedUnit, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
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
        em.SetComponentData(attacker, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(attacker, new UnitGrid { Cell = new int2(1, 1) });
        em.SetComponentData(attacker, new UnitCombat { CanAttack = 1 });
        em.SetComponentData(attacker, new UnitAttack { Range = 20f, Damage = 10, CooldownSeconds = 1f });
        em.SetComponentData(attacker, LocalTransform.FromPosition(new float3(1.5f, 0f, 1.5f)));

        Entity target = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(LocalTransform));
        em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
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
        em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
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
                MarkerFactionId = FactionIdentitySystem.PlayerFactionId,
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
            em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
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
            typeof(GridWalkable));
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
        System.Action<bool> setExplicitAttackTargetModeActive = null,
        System.Action<bool> setCameraDragging = null,
        System.Action processSelectionRectangleRequests = null,
        System.Action<string> logSelectionClickDiagnostic = null,
        System.Action<SelectionStateSystem> clearFocusedUnit = null)
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
            gridConfigQuery,
            mapSurfaceQuery,
            TryGetEntityManager,
            ensureEntityQueries,
            null,
            applyHudCommandMode,
            applyHudCommandResult,
            clearHudSelection,
            clearHudCommandMode,
            setExplicitAttackTargetModeActive,
            setHudWorldMarkersVisible,
            processSelectionRectangleRequests,
            logSelectionClickDiagnostic,
            requestMoveOrderScreenMarker,
            requestAttackOrderScreenMarker,
            setCameraDragging,
            clearFocusedUnit,
            null,
            null,
            null,
            null,
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
