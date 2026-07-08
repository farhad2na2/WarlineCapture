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

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantCommandIntentSystem_QueuesCameraPreviewFromEntityTarget());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_RejectsPreviewWithoutWorldTarget());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesSelectionModeFromUiSurfaceDoIt());
            passed++;
            RunCase(test => test.AssistantCommandIntentSystem_QueuesFocusUnitFromEntityDoIt());
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
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(7, results[0].RequestId);
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, results[0].Status);
        Assert.AreEqual("Preview queued.", results[0].Message.ToString());

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
}
