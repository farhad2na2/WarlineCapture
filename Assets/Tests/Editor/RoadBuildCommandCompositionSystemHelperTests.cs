#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class RoadBuildCommandCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RoadBuildCommandCompositionSystemHelperTests();
            tests.RoadBuildCommandRequest_EnterWritesAcceptedResult();
            tests.RoadBuildCommandRequest_EnterAcceptsDefaultRuntimeState();
            tests.RoadBuildCommandRequest_ConfirmWritesAcceptedResult();
            tests.RoadBuildCommandRequest_CancelWritesAcceptedResult();
            tests.RoadBuildCommandRequest_ExitWritesAcceptedResult();
            tests.RoadBuildRuntimeAction_UpdateProcessesQueuedEnterCommand();
            tests.RoadBuildCommandRequest_EnqueueAndProcessExitWritesAcceptedResult();
            Debug.Log("[RoadBuildCommandRequestValidation] result=Passed tests=7");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[RoadBuildCommandRequestValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [SetUp]
    public void SetUp()
    {
        ResetRuntimeState();
    }

    [TearDown]
    public void TearDown()
    {
        ResetRuntimeState();
    }

    [Test]
    public void RoadBuildCommandRequest_EnterWritesAcceptedResult()
    {
        using World world = new("RoadBuildCommandEnterTest");
        RoadBuildCommandTestState state = new();

        int requestId = state.CommandSystem.EnqueueEnterRoadBuildMode(world.EntityManager);
        state.CommandSystem.ProcessPendingRoadBuildCommands(world.EntityManager, state.CommandContext);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindEnterRoadBuildMode,
            accepted: true);
        Assert.IsTrue(state.RuntimeGameplayStateSystem.BuildModeActive);
        Assert.IsFalse(state.RuntimeGameplayStateSystem.SelectionModeActive);
        Assert.AreEqual(RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road, state.SessionState.ActiveBuildTool);
        Assert.AreSame(state.CapturedSnapshot, state.SessionState.RoadBuildSessionSnapshot);
        Assert.AreEqual(1, state.CaptureSnapshotCount);
        Assert.AreEqual(1, state.ApplyBuildCommandModeCount);
        Assert.AreEqual(1, state.ClearSelectedBuildingCount);
        Assert.AreEqual(1, state.CancelBuildingPlacementCount);
        Assert.AreEqual(1, state.UpdatePreviewCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildCommandRequest_EnterAcceptsDefaultRuntimeState()
    {
        using World world = new("RoadBuildCommandEnterDefaultRuntimeStateTest");
        RoadBuildCommandTestState state = new();
        RoadBuildCommandCompositionSystemHelper.Context context = new(
            new RuntimeGameplayStateSystem(),
            state.SessionSystem,
            state.CommandContext.SessionContext,
            () => state.ClearRoadBuildDragStateCount++);

        int requestId = state.CommandSystem.EnqueueEnterRoadBuildMode(world.EntityManager);
        state.CommandSystem.ProcessPendingRoadBuildCommands(world.EntityManager, context);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindEnterRoadBuildMode,
            accepted: true);
        Assert.AreEqual(RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road, state.SessionState.ActiveBuildTool);
        Assert.AreEqual(1, state.CaptureSnapshotCount);
        Assert.AreEqual(1, state.ApplyBuildCommandModeCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildCommandRequest_ConfirmWritesAcceptedResult()
    {
        using World world = new("RoadBuildCommandConfirmTest");
        RoadBuildCommandTestState state = new();
        state.SessionState.RoadBuildSessionSnapshot = state.CapturedSnapshot;

        int requestId = state.CommandSystem.EnqueueConfirmRoadBuildSession(world.EntityManager);
        state.CommandSystem.ProcessPendingRoadBuildCommands(world.EntityManager, state.CommandContext);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindConfirmRoadBuildSession,
            accepted: true);
        Assert.IsNull(state.SessionState.RoadBuildSessionSnapshot);
        Assert.AreEqual(1, state.RemoveRuntimeBlockersUnderRoadsCount);
        Assert.AreEqual(1, state.NotifyStaticMinimapChangedCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildCommandRequest_CancelWritesAcceptedResult()
    {
        using World world = new("RoadBuildCommandCancelTest");
        RoadBuildCommandTestState state = new();
        state.SessionState.RoadBuildSessionSnapshot = state.CapturedSnapshot;

        int requestId = state.CommandSystem.EnqueueCancelRoadBuildSession(world.EntityManager);
        state.CommandSystem.ProcessPendingRoadBuildCommands(world.EntityManager, state.CommandContext);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindCancelRoadBuildSession,
            accepted: true);
        Assert.IsNull(state.SessionState.RoadBuildSessionSnapshot);
        Assert.AreEqual(1, state.RestoreRoadBuildSessionCount);
        Assert.AreSame(state.CapturedSnapshot, state.RestoredSnapshot);
        Assert.AreEqual(1, state.NotifyStaticMinimapChangedCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildCommandRequest_ExitWritesAcceptedResult()
    {
        using World world = new("RoadBuildCommandExitTest");
        RoadBuildCommandTestState state = new();
        state.RuntimeGameplayStateSystem.BuildModeActive = true;
        state.SessionState.ActiveBuildTool = RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road;
        state.SessionState.PendingDeleteStrokeId = 7;
        state.SessionState.PendingDeleteMessage = "Delete road?";

        int requestId = state.CommandSystem.EnqueueExitBuildMode(world.EntityManager);
        state.CommandSystem.ProcessPendingRoadBuildCommands(world.EntityManager, state.CommandContext);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindExitBuildMode,
            accepted: true);
        Assert.IsFalse(state.RuntimeGameplayStateSystem.BuildModeActive);
        Assert.AreEqual(RoadBuildSessionCompositionSystemHelper.BuildToolMode.None, state.SessionState.ActiveBuildTool);
        Assert.IsNull(state.SessionState.PendingDeleteStrokeId);
        Assert.IsNull(state.SessionState.PendingDeleteMessage);
        Assert.AreEqual(2, state.SessionState.SkipBuildClickFrames);
        Assert.AreEqual(1, state.ClearRoadBuildDragStateCount);
        Assert.AreEqual(1, state.CancelPendingBuildCount);
        Assert.AreEqual(1, state.CancelBuildingPlacementCount);
        Assert.AreEqual(1, state.ClearSelectedBuildingCount);
        Assert.AreEqual(1, state.HidePlacementOutlineCount);
        Assert.AreEqual(1, state.ClearCommandModeCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildRuntimeAction_UpdateProcessesQueuedEnterCommand()
    {
        using World world = new("RoadBuildRuntimeActionCommandQueueTest");
        RoadBuildCommandTestState state = new();
        RoadBuildRuntimeActionCompositionSystemHelper.State runtimeState = RoadBuildRuntimeActionCompositionSystemHelper.CreateState();
        RoadBuildRuntimeActionCompositionSystemHelper.ConfigureCommands(
            runtimeState,
            state.CommandSystem,
            state.CommandContext,
            (out EntityManager entityManager) =>
            {
                entityManager = world.EntityManager;
                return true;
            });

        int requestId = state.CommandSystem.EnqueueEnterRoadBuildMode(world.EntityManager);
        RoadBuildRuntimeActionCompositionSystemHelper.Update(runtimeState);

        AssertRoadBuildResult(
            world.EntityManager,
            state.CommandSystem,
            requestId,
            RoadBuildCommandRequestElement.KindEnterRoadBuildMode,
            accepted: true);
        Assert.IsTrue(state.RuntimeGameplayStateSystem.BuildModeActive);
        Assert.AreEqual(RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road, state.SessionState.ActiveBuildTool);
        Assert.AreEqual(1, state.CaptureSnapshotCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    [Test]
    public void RoadBuildCommandRequest_EnqueueAndProcessExitWritesAcceptedResult()
    {
        using World world = new("RoadBuildCommandEnqueueProcessExitTest");
        RoadBuildCommandTestState state = new();
        state.RuntimeGameplayStateSystem.BuildModeActive = true;
        state.SessionState.ActiveBuildTool = RoadBuildSessionCompositionSystemHelper.BuildToolMode.Road;

        Assert.IsTrue(state.CommandSystem.EnqueueAndProcessExitBuildMode(
            world.EntityManager,
            state.CommandContext));

        Assert.IsFalse(state.RuntimeGameplayStateSystem.BuildModeActive);
        Assert.AreEqual(RoadBuildSessionCompositionSystemHelper.BuildToolMode.None, state.SessionState.ActiveBuildTool);
        Assert.AreEqual(1, state.ClearRoadBuildDragStateCount);
        AssertRequestBufferCleared(world.EntityManager);
    }

    private static void AssertRoadBuildResult(
        EntityManager em,
        RoadBuildCommandCompositionSystemHelper commandSystem,
        int requestId,
        byte requestKind,
        bool accepted,
        byte resultCode = RoadBuildCommandResultElement.Completed)
    {
        Assert.IsTrue(commandSystem.TryGetRoadBuildCommandResult(
            em,
            requestId,
            out RoadBuildCommandResultElement result));
        Assert.AreEqual(requestId, result.RequestId);
        Assert.AreEqual(requestKind, result.RequestKind);
        Assert.AreEqual(accepted ? 1 : 0, result.Accepted);
        Assert.AreEqual(resultCode, result.ResultCode);
    }

    private static void AssertRequestBufferCleared(EntityManager em)
    {
        using EntityQuery queueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RoadBuildCommandQueueComponent>());
        Entity queueEntity = queueQuery.GetSingletonEntity();
        Assert.AreEqual(0, em.GetBuffer<RoadBuildCommandRequestElement>(queueEntity).Length);
    }

    private static void ResetRuntimeState()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
    }

    private sealed class RoadBuildCommandTestState
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
        public readonly RoadBuildSessionCompositionSystemHelper SessionSystem = new();
        public readonly RoadBuildSessionCompositionSystemHelper.State SessionState = new();
        public readonly RoadBuildCommandCompositionSystemHelper CommandSystem = new();
        public readonly RoadNetworkCompositionSystemHelper.Snapshot CapturedSnapshot = new();
        public readonly RoadBuildCommandCompositionSystemHelper.Context CommandContext;
        public RoadNetworkCompositionSystemHelper.Snapshot RestoredSnapshot;
        public int CaptureSnapshotCount;
        public int RestoreRoadBuildSessionCount;
        public int RemoveRuntimeBlockersUnderRoadsCount;
        public int NotifyStaticMinimapChangedCount;
        public int ApplyBuildCommandModeCount;
        public int ClearCommandModeCount;
        public int ClearSelectedBuildingCount;
        public int CancelBuildingPlacementCount;
        public int CancelPendingBuildCount;
        public int HidePlacementOutlineCount;
        public int UpdatePreviewCount;
        public int ClearRoadBuildDragStateCount;

        public RoadBuildCommandTestState()
        {
            RuntimeGameplayStateSystem.PlayRequested = true;
            RoadBuildSessionCompositionSystemHelper.Context sessionContext = new(
                SessionState,
                RuntimeGameplayStateSystem,
                () =>
                {
                    CaptureSnapshotCount++;
                    return CapturedSnapshot;
                },
                snapshot =>
                {
                    RestoreRoadBuildSessionCount++;
                    RestoredSnapshot = snapshot;
                },
                () => RemoveRuntimeBlockersUnderRoadsCount++,
                () => NotifyStaticMinimapChangedCount++,
                () => ApplyBuildCommandModeCount++,
                () => ClearCommandModeCount++,
                () => ClearSelectedBuildingCount++,
                () => CancelBuildingPlacementCount++,
                () => CancelPendingBuildCount++,
                () => HidePlacementOutlineCount++,
                () => UpdatePreviewCount++);

            CommandContext = new RoadBuildCommandCompositionSystemHelper.Context(
                RuntimeGameplayStateSystem,
                SessionSystem,
                sessionContext,
                () => ClearRoadBuildDragStateCount++);
        }
    }
}
#endif
