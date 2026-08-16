using System;
using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class AssistantCommandIntentSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _intentSystem;
    private BlobAssetReference<OperationMapBlob> _operationMapBlob;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantCommandIntentSystem_QueuesCameraPreviewFromEntityTarget());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesCameraPreviewFromOperationMapObjectiveAnchor());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesObjectivePreviewFromNamedHostileAnchor());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_RejectsPreviewWithoutWorldTarget());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesSelectionModeFromUiSurfaceDoIt());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesTakeoverSelectionModeFromUiSurfaceDoIt());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesSelectAllSoldiersFromSquadDoIt());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesFocusUnitFromEntityDoIt());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_WritesRecoveryMessageForInvalidSelectTarget());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_CancelsPreviewRequest());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_StopsAssistantControlRequest());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_TimesOutStaleRequest());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesVersionedMoveOrder());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesVersionedAttackOrder());
            passed++;

            Debug.Log($"[AssistantCommandIntentSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantCommandIntentSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantCommandIntentSystemTests> testCase)
    {
        var tests = new AssistantCommandIntentSystemTests();
        tests.SetUp();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(AssistantCommandIntentSystemTests));
        _entityManager = _world.EntityManager;
        _intentSystem = _world.CreateSystem<AssistantCommandIntentSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
        if (_operationMapBlob.IsCreated)
            _operationMapBlob.Dispose();
    }

    [Test]
    public void AssistantCommandIntentSystem_CancelsPreviewRequest()
    {
        Entity boundary = CreateBoundary();
        _entityManager.AddComponentData(boundary, new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantPreview,
            ActiveRecommendationId = 3101
        });
        _entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary).Add(new AssistantPreviewHighlightElement
        {
            RequestId = 4,
            RecommendationId = 3101,
            WorldPosition = new float3(1f, 0f, 2f),
            Strength = 1f,
            Active = 1
        });
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 16,
            Frame = UnityEngine.Time.frameCount,
            RecommendationId = 3101,
            Kind = AssistantCommandIntentKind.CancelPreview,
            TargetKind = AssistantTargetKind.Entity
        });

        _intentSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length);
        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary).Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Cancelled, results[0].Status);
        Assert.AreEqual("Preview cancelled.", results[0].Message.ToString());

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(AssistantControlState.Player, assistantState.ControlState);
        Assert.AreEqual(0, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);
    }

    [Test]
    public void AssistantCommandIntentSystem_StopsAssistantControlRequest()
    {
        Entity boundary = CreateBoundary();
        _entityManager.AddComponentData(boundary, new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101
        });
        _entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary).Add(new AssistantPreviewHighlightElement
        {
            RequestId = 8,
            RecommendationId = 4101,
            WorldPosition = new float3(5f, 0f, 6f),
            Strength = 1f,
            Active = 1
        });
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 18,
            Frame = UnityEngine.Time.frameCount,
            RecommendationId = 0,
            Kind = AssistantCommandIntentKind.StopAssistantControl,
            TargetKind = AssistantTargetKind.None
        });

        _intentSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length);
        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary).Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(18, results[0].RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Cancelled, results[0].Status);
        Assert.AreEqual("Assistant control stopped.", results[0].Message.ToString());

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(AssistantControlState.Player, assistantState.ControlState);
        Assert.AreEqual(0, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);
    }

    [Test]
    public void AssistantCommandIntentSystem_TimesOutStaleRequest()
    {
        Entity boundary = CreateBoundary();
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 17,
            Frame = UnityEngine.Time.frameCount - 1000,
            RecommendationId = 3301,
            Kind = AssistantCommandIntentKind.ShowRecommendation,
            TargetKind = AssistantTargetKind.WorldPosition,
            WorldPosition = new float3(10f, 0f, 12f)
        });

        _intentSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.TimedOut, results[0].Status);
        Assert.AreEqual("Intent timed out.", results[0].Message.ToString());

        using EntityQuery cameraQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsCameraRequestQueueComponent>(),
            ComponentType.ReadOnly<RtsCameraRequestElement>());
        Assert.IsTrue(cameraQuery.IsEmptyIgnoreFilter);
    }

    [Test]
    public void AssistantCommandIntentSystem_WritesRecoveryMessageForInvalidSelectTarget()
    {
        Entity boundary = CreateBoundary();
        Entity destroyedTarget = CreateTarget(new float3(2f, 0f, 5f));
        _entityManager.DestroyEntity(destroyedTarget);
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 15,
            Frame = 31,
            RecommendationId = 3202,
            Kind = AssistantCommandIntentKind.SelectEntity,
            TargetKind = AssistantTargetKind.Entity,
            TargetEntity = destroyedTarget
        });

        _intentSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(15, results[0].RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Rejected, results[0].Status);
        Assert.AreEqual("No selectable target is available.", results[0].Message.ToString());

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(1, messages.Length);
        Assert.AreEqual(700015, messages[0].MessageId);
        Assert.AreEqual(AssistantMessagePriority.High, messages[0].Priority);
        Assert.AreEqual(AssistantRecommendationKind.Explain, messages[0].RelatedKind);
        Assert.AreEqual("ARIA could not find a selectable unit for that action.", messages[0].Text.ToString());
        Assert.AreEqual(0, messages[0].Acknowledged);

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(1, assistantState.UiDirty);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesSelectionModeFromUiSurfaceDoIt()
    {
        Entity boundary = CreateBoundary();
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 11,
            Frame = 25,
            RecommendationId = 3001,
            Kind = AssistantCommandIntentKind.SelectEntity,
            TargetKind = AssistantTargetKind.UiSurface
        });

        _intentSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);
        Assert.AreEqual("Selection queued.", results[0].Message.ToString());

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(AssistantControlState.Guided, assistantState.ControlState);
        Assert.AreEqual(3001, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);

        using EntityQuery selectionQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsSelectionInputStateComponent>(),
            ComponentType.ReadOnly<RtsSelectionCommandIntentRequestElement>());
        Entity selectionEntity = selectionQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> selectionRequests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionEntity);
        Assert.AreEqual(1, selectionRequests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterSelectionMode, selectionRequests[0].Kind);
        Assert.AreEqual(1, selectionRequests[0].RequestId);
        Assert.AreEqual(25, selectionRequests[0].Frame);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesTakeoverSelectionModeFromUiSurfaceDoIt()
    {
        Entity boundary = CreateBoundary();
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 19,
            Frame = 35,
            RecommendationId = 3001,
            Kind = AssistantCommandIntentKind.SelectEntity,
            TargetKind = AssistantTargetKind.UiSurface,
            FromTakeover = 1
        });

        _intentSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(AssistantControlState.AssistantTakeover, assistantState.ControlState);
        Assert.AreEqual(3001, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);

        using EntityQuery selectionQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsSelectionInputStateComponent>(),
            ComponentType.ReadOnly<RtsSelectionCommandIntentRequestElement>());
        Entity selectionEntity = selectionQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> selectionRequests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionEntity);
        Assert.AreEqual(1, selectionRequests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterSelectionMode, selectionRequests[0].Kind);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesFocusUnitFromEntityDoIt()
    {
        Entity boundary = CreateBoundary();
        Entity target = CreateTarget(new float3(4f, 0f, 9f));
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 12,
            Frame = 26,
            RecommendationId = 3201,
            Kind = AssistantCommandIntentKind.SelectEntity,
            TargetKind = AssistantTargetKind.Entity,
            TargetEntity = target,
            WorldPosition = new float3(4f, 0f, 9f)
        });

        _intentSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);
        Assert.AreEqual("Selection queued.", results[0].Message.ToString());

        using EntityQuery selectionQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsSelectionInputStateComponent>(),
            ComponentType.ReadOnly<RtsSelectionCommandIntentRequestElement>());
        Entity selectionEntity = selectionQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> selectionRequests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionEntity);
        Assert.AreEqual(1, selectionRequests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.FocusUnit, selectionRequests[0].Kind);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Entity, selectionRequests[0].TargetKind);
        Assert.AreEqual(target, selectionRequests[0].TargetEntity);
        Assert.AreEqual(1, selectionRequests[0].HasTargetEntity);
        Assert.AreEqual(new float3(4f, 0f, 9f), selectionRequests[0].WorldPosition);
        Assert.AreEqual(1, selectionRequests[0].HasWorldPosition);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesSelectAllSoldiersFromSquadDoIt()
    {
        Entity boundary = CreateBoundary();
        Entity target = CreateTarget(new float3(2f, 0f, 6f));
        _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Add(
            new AssistantCommandIntentRequestElement
            {
                RequestId = 21,
                Frame = 38,
                RecommendationId = 3301,
                Kind = AssistantCommandIntentKind.SelectEntity,
                TargetKind = AssistantTargetKind.Squad,
                TargetEntity = target
            });

        _intentSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);

        using EntityQuery selectionQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsSelectionInputStateComponent>(),
            ComponentType.ReadOnly<RtsSelectionCommandIntentRequestElement>());
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> selectionRequests =
            _entityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionQuery.GetSingletonEntity());
        Assert.AreEqual(1, selectionRequests.Length);
        Assert.AreEqual(RtsSelectionCommandIntentKind.FocusSquad, selectionRequests[0].Kind);
        Assert.AreEqual(target, selectionRequests[0].TargetEntity);
        Assert.AreEqual(1, selectionRequests[0].HasTargetEntity);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesCameraPreviewFromEntityTarget()
    {
        Entity boundary = CreateBoundary();
        Entity target = CreateTarget(new float3(18f, 2f, 32f));
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 7,
            Frame = 19,
            RecommendationId = 3101,
            Kind = AssistantCommandIntentKind.ShowRecommendation,
            TargetKind = AssistantTargetKind.Entity,
            TargetEntity = target
        });

        _intentSystem.Update(_world.Unmanaged);

        requests = _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(0, requests.Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(7, results[0].RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);
        Assert.AreEqual("Preview queued.", results[0].Message.ToString());
        Assert.AreEqual(AssistantCommandIntentStatus.Completed, results[1].Status);
        Assert.AreEqual("Preview active.", results[1].Message.ToString());

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(AssistantControlState.AssistantPreview, assistantState.ControlState);
        Assert.AreEqual(3101, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);

        DynamicBuffer<AssistantPreviewHighlightElement> highlights =
            _entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary);
        Assert.AreEqual(1, highlights.Length);
        Assert.AreEqual(7, highlights[0].RequestId);
        Assert.AreEqual(3101, highlights[0].RecommendationId);
        Assert.AreEqual(AssistantTargetKind.Entity, highlights[0].TargetKind);
        Assert.AreEqual(new float3(18f, 2f, 32f), highlights[0].WorldPosition);
        Assert.AreEqual(1, highlights[0].Active);

        using EntityQuery cameraQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsCameraRequestQueueComponent>(),
            ComponentType.ReadOnly<RtsCameraRequestElement>());
        Entity cameraEntity = cameraQuery.GetSingletonEntity();
        DynamicBuffer<RtsCameraRequestElement> cameraRequests =
            _entityManager.GetBuffer<RtsCameraRequestElement>(cameraEntity);
        Assert.AreEqual(2, cameraRequests.Length);
        Assert.AreEqual(RtsCameraRequestKind.SetSmoothFocusTarget, cameraRequests[0].Kind);
        Assert.AreEqual(new float3(18f, 2f, 32f), cameraRequests[0].WorldPosition);
        Assert.AreEqual(1, cameraRequests[0].Flag);
        Assert.AreEqual(RtsCameraRequestKind.ClearDragging, cameraRequests[1].Kind);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesCameraPreviewFromOperationMapObjectiveAnchor()
    {
        Entity boundary = CreateBoundary();
        FixedString64Bytes anchorId = new("anchor.skirmish.objective.alpha");
        CreateActiveOperationMap(anchorId, new float3(44f, 3f, 71f));
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3201,
            SourceVersion = 9,
            Kind = AssistantRecommendationKind.CameraFocus,
            TargetKind = AssistantTargetKind.Objective,
            TargetId = anchorId,
            CanShow = 1
        });
        _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Add(
            new AssistantCommandIntentRequestElement
            {
                RequestId = 8,
                Frame = UnityEngine.Time.frameCount,
                RecommendationId = 3201,
                RecommendationSourceVersion = 9,
                Kind = AssistantCommandIntentKind.ShowRecommendation,
                TargetKind = AssistantTargetKind.Objective,
                TargetId = anchorId
            });

        _intentSystem.Update(_world.Unmanaged);

        using EntityQuery cameraQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsCameraRequestQueueComponent>(),
            ComponentType.ReadOnly<RtsCameraRequestElement>());
        DynamicBuffer<RtsCameraRequestElement> cameraRequests =
            _entityManager.GetBuffer<RtsCameraRequestElement>(cameraQuery.GetSingletonEntity());
        Assert.AreEqual(2, cameraRequests.Length);
        Assert.AreEqual(RtsCameraRequestKind.SetSmoothFocusTarget, cameraRequests[0].Kind);
        Assert.AreEqual(new float3(44f, 3f, 71f), cameraRequests[0].WorldPosition);
        DynamicBuffer<AssistantPreviewHighlightElement> highlights =
            _entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary);
        Assert.AreEqual(1, highlights.Length);
        Assert.AreEqual(AssistantTargetKind.Objective, highlights[0].TargetKind);
        Assert.AreEqual(new float3(44f, 3f, 71f), highlights[0].WorldPosition);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesObjectivePreviewFromNamedHostileAnchor()
    {
        Entity boundary = CreateBoundary();
        FixedString64Bytes anchorId = new("anchor.ch01.m01.patrol_objective");
        CreateActiveOperationMap(anchorId, new float3(1784f, 0.009f, 754f), OperationMapAnchorKind.Hostile);
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3202,
            SourceVersion = 10,
            Kind = AssistantRecommendationKind.CameraFocus,
            TargetKind = AssistantTargetKind.Objective,
            TargetId = anchorId,
            CanShow = 1
        });
        _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Add(
            new AssistantCommandIntentRequestElement
            {
                RequestId = 9,
                Frame = UnityEngine.Time.frameCount,
                RecommendationId = 3202,
                RecommendationSourceVersion = 10,
                Kind = AssistantCommandIntentKind.ShowRecommendation,
                TargetKind = AssistantTargetKind.Objective,
                TargetId = anchorId
            });

        _intentSystem.Update(_world.Unmanaged);

        using EntityQuery cameraQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsCameraRequestQueueComponent>(),
            ComponentType.ReadOnly<RtsCameraRequestElement>());
        DynamicBuffer<RtsCameraRequestElement> cameraRequests =
            _entityManager.GetBuffer<RtsCameraRequestElement>(cameraQuery.GetSingletonEntity());
        Assert.AreEqual(2, cameraRequests.Length);
        Assert.AreEqual(new float3(1784f, 0.009f, 754f), cameraRequests[0].WorldPosition);
    }

    [Test]
    public void AssistantCommandIntentSystem_RejectsPreviewWithoutWorldTarget()
    {
        Entity boundary = CreateBoundary();
        _entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary).Add(new AssistantPreviewHighlightElement
        {
            RequestId = 1,
            RecommendationId = 2001,
            WorldPosition = new float3(2f, 0f, 3f),
            Strength = 1f,
            Active = 1
        });
        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        requests.Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 3,
            Frame = 11,
            RecommendationId = 3001,
            Kind = AssistantCommandIntentKind.ShowRecommendation,
            TargetKind = AssistantTargetKind.UiSurface
        });

        _intentSystem.Update(_world.Unmanaged);

        requests = _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(0, requests.Length);

        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(3, results[0].RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Rejected, results[0].Status);
        Assert.AreEqual("No preview target is available.", results[0].Message.ToString());
        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary).Length);

        using EntityQuery cameraQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RtsCameraRequestQueueComponent>(),
            ComponentType.ReadOnly<RtsCameraRequestElement>());
        Assert.IsTrue(cameraQuery.IsEmptyIgnoreFilter);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesVersionedMoveOrder()
    {
        Entity boundary = CreateBoundary();
        Entity source = CreatePlayerSource(canAttack: false);
        Entity grid = _entityManager.CreateEntity(typeof(GridConfig));
        _entityManager.SetComponentData(grid, new GridConfig
        {
            Width = 32,
            Height = 24,
            CellSize = 1f
        });
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 5101,
            SourceVersion = 17,
            Kind = AssistantRecommendationKind.Move,
            TargetKind = AssistantTargetKind.Cell,
            SourceEntity = source,
            TargetCell = new int2(6, 7),
            HasTargetCell = 1,
            CanExecute = 1
        });
        _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Add(
            new AssistantCommandIntentRequestElement
            {
                RequestId = 51,
                RecommendationId = 5101,
                RecommendationSourceVersion = 17,
                Kind = AssistantCommandIntentKind.MoveToWorldPosition,
                TargetKind = AssistantTargetKind.Cell,
                SourceEntity = source,
                TargetCell = new int2(6, 7)
            });

        _intentSystem.Update(_world.Unmanaged);

        using EntityQuery query = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMoveOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitMoveOrderRequestElement>());
        DynamicBuffer<UnitMoveOrderRequestElement> requests =
            _entityManager.GetBuffer<UnitMoveOrderRequestElement>(query.GetSingletonEntity());
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(source, requests[0].Entity);
        Assert.AreEqual(new int2(6, 7), requests[0].Goal);
        AssistantCommandDispatchElement dispatch =
            _entityManager.GetBuffer<AssistantCommandDispatchElement>(boundary)[0];
        Assert.AreEqual(AssistantDownstreamCommandKind.MoveOrder, dispatch.DownstreamKind);
        Assert.Greater(dispatch.DownstreamRequestId, 0);
    }

    [Test]
    public void AssistantCommandIntentSystem_QueuesVersionedAttackOrder()
    {
        Entity boundary = CreateBoundary();
        Entity source = CreatePlayerSource(canAttack: true);
        Entity target = _entityManager.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(UnitGrid), typeof(LocalTransform));
        _entityManager.SetComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        _entityManager.SetComponentData(target, new UnitHealth { Current = 80, Max = 80 });
        _entityManager.SetComponentData(target, new UnitGrid { Cell = new int2(9, 5) });
        _entityManager.SetComponentData(target, LocalTransform.FromPosition(new float3(9f, 0f, 5f)));
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 5201,
            SourceVersion = 19,
            Kind = AssistantRecommendationKind.Attack,
            TargetKind = AssistantTargetKind.Entity,
            SourceEntity = source,
            TargetEntity = target,
            CanExecute = 1
        });
        _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary).Add(
            new AssistantCommandIntentRequestElement
            {
                RequestId = 52,
                RecommendationId = 5201,
                RecommendationSourceVersion = 19,
                Kind = AssistantCommandIntentKind.AttackEntity,
                TargetKind = AssistantTargetKind.Entity,
                SourceEntity = source,
                TargetEntity = target
            });

        _intentSystem.Update(_world.Unmanaged);

        using EntityQuery query = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitAttackOrderQueueComponent>(),
            ComponentType.ReadOnly<UnitAttackOrderRequestElement>());
        DynamicBuffer<UnitAttackOrderRequestElement> requests =
            _entityManager.GetBuffer<UnitAttackOrderRequestElement>(query.GetSingletonEntity());
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(source, requests[0].SourceEntity);
        Assert.AreEqual(target, requests[0].TargetEntity);
        AssistantCommandDispatchElement dispatch =
            _entityManager.GetBuffer<AssistantCommandDispatchElement>(boundary)[0];
        Assert.AreEqual(AssistantDownstreamCommandKind.AttackOrder, dispatch.DownstreamKind);
    }

    private Entity CreateBoundary()
    {
        Entity boundary = _entityManager.CreateEntity(typeof(UiShellRootComponent));
        _entityManager.AddBuffer<AssistantCommandIntentRequestElement>(boundary);
        _entityManager.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        return boundary;
    }

    private Entity CreateTarget(float3 position)
    {
        Entity target = _entityManager.CreateEntity(typeof(LocalTransform));
        _entityManager.SetComponentData(target, new LocalTransform
        {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });
        return target;
    }

    private Entity CreatePlayerSource(bool canAttack)
    {
        Entity source = canAttack
            ? _entityManager.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(UnitAttack))
            : _entityManager.CreateEntity(typeof(Faction), typeof(UnitHealth));
        _entityManager.SetComponentData(source, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(source, new UnitHealth { Current = 100, Max = 100 });
        return source;
    }

    private void CreateActiveOperationMap(
        FixedString64Bytes anchorId,
        float3 position,
        OperationMapAnchorKind kind = OperationMapAnchorKind.Objective)
    {
        FixedString64Bytes operationMapId = new("opmap.skirmish.desert_base_01");
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapBlob root = ref builder.ConstructRoot<OperationMapBlob>();
        root.OperationMapId = operationMapId;
        BlobBuilderArray<OperationMapAnchorBlob> anchors = builder.Allocate(ref root.Anchors, 1);
        anchors[0] = new OperationMapAnchorBlob
        {
            Id = anchorId,
            Kind = kind,
            Position = position,
            Rotation = quaternion.identity,
            FactionId = -1,
            LaneIndex = -1
        };
        _operationMapBlob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);

        Entity operationMap = _entityManager.CreateEntity(
            typeof(ActiveOperationMapComponent),
            typeof(OperationMapMetadataComponent),
            typeof(OperationMapReadinessComponent));
        _entityManager.SetComponentData(operationMap, new ActiveOperationMapComponent
        {
            OperationMapId = operationMapId,
            Generation = 1
        });
        _entityManager.SetComponentData(operationMap, new OperationMapMetadataComponent
        {
            Blob = _operationMapBlob,
            Generation = 1
        });
        _entityManager.SetComponentData(operationMap, new OperationMapReadinessComponent
        {
            Generation = 1,
            ReadyFlags = OperationMapReadinessFlags.Metadata,
            RequiredFlags = OperationMapReadinessFlags.Metadata
        });
    }
}
