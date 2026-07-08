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
