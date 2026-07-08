using System;
using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantControlOwnerSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _ownerSystem;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantControlOwnerSystem_MirrorsPreviewState());
            passed++;
            RunCase(test => test.AssistantControlOwnerSystem_StartsBoundedTakeover());
            passed++;
            RunCase(test => test.AssistantControlOwnerSystem_CancelsExpiredTakeover());
            passed++;
            RunCase(test => test.AssistantControlOwnerSystem_CancelsAfterMaxActionCount());
            passed++;

            Debug.Log($"[AssistantControlOwnerSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantControlOwnerSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantControlOwnerSystemTests> testCase)
    {
        var tests = new AssistantControlOwnerSystemTests();
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
        _world = new World(nameof(AssistantControlOwnerSystemTests));
        _entityManager = _world.EntityManager;
        _ownerSystem = _world.CreateSystem<AssistantControlOwnerSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AssistantControlOwnerSystem_MirrorsPreviewState()
    {
        Entity boundary = CreateBoundary(new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantPreview,
            ActiveRecommendationId = 3101
        });

        _ownerSystem.Update(_world.Unmanaged);

        AssistantControlOwnerComponent owner =
            _entityManager.GetComponentData<AssistantControlOwnerComponent>(boundary);
        Assert.AreEqual(AssistantControlState.AssistantPreview, owner.State);
        Assert.AreEqual(3101, owner.ActiveRecommendationId);
        Assert.AreEqual(0, owner.ActionCount);
        Assert.AreEqual(0, owner.MaxActionCount);
        Assert.AreEqual(0f, owner.TimeoutAt);
    }

    [Test]
    public void AssistantControlOwnerSystem_StartsBoundedTakeover()
    {
        Entity boundary = CreateBoundary(new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101
        });
        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        results.Add(new AssistantCommandIntentResultElement
        {
            RequestId = 7,
            Kind = AssistantCommandIntentKind.ShowRecommendation,
            Status = AssistantCommandIntentStatus.Completed
        });

        _ownerSystem.Update(_world.Unmanaged);

        AssistantControlOwnerComponent owner =
            _entityManager.GetComponentData<AssistantControlOwnerComponent>(boundary);
        Assert.AreEqual(AssistantControlState.AssistantTakeover, owner.State);
        Assert.AreEqual(4101, owner.ActiveRecommendationId);
        Assert.AreEqual(7, owner.ActiveIntentRequestId);
        Assert.AreEqual(0, owner.ActionCount);
        Assert.AreEqual(3, owner.MaxActionCount);
        Assert.GreaterOrEqual(owner.TimeoutAt, owner.StartedAt + 29.9f);
    }

    [Test]
    public void AssistantControlOwnerSystem_CancelsExpiredTakeover()
    {
        Entity boundary = CreateBoundary(new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101
        });
        _entityManager.AddComponentData(boundary, new AssistantControlOwnerComponent
        {
            State = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101,
            MaxActionCount = 3,
            TimeoutAt = -1f
        });

        _ownerSystem.Update(_world.Unmanaged);

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        AssistantControlOwnerComponent owner =
            _entityManager.GetComponentData<AssistantControlOwnerComponent>(boundary);
        Assert.AreEqual(AssistantControlState.Player, assistantState.ControlState);
        Assert.AreEqual(0, assistantState.ActiveRecommendationId);
        Assert.AreEqual(1, assistantState.UiDirty);
        Assert.AreEqual(AssistantControlState.Player, owner.State);
        Assert.AreEqual(0, owner.ActiveRecommendationId);
    }

    [Test]
    public void AssistantControlOwnerSystem_CancelsAfterMaxActionCount()
    {
        Entity boundary = CreateBoundary(new AssistantStateComponent
        {
            ControlState = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101
        });
        _entityManager.AddComponentData(boundary, new AssistantControlOwnerComponent
        {
            State = AssistantControlState.AssistantTakeover,
            ActiveRecommendationId = 4101,
            ActiveIntentRequestId = 2,
            MaxActionCount = 1,
            TimeoutAt = UnityEngine.Time.time + 30f
        });
        DynamicBuffer<AssistantCommandIntentResultElement> results =
            _entityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
        results.Add(new AssistantCommandIntentResultElement
        {
            RequestId = 3,
            Kind = AssistantCommandIntentKind.SelectEntity,
            Status = AssistantCommandIntentStatus.Accepted
        });

        _ownerSystem.Update(_world.Unmanaged);

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        AssistantControlOwnerComponent owner =
            _entityManager.GetComponentData<AssistantControlOwnerComponent>(boundary);
        Assert.AreEqual(AssistantControlState.Player, assistantState.ControlState);
        Assert.AreEqual(0, assistantState.ActiveRecommendationId);
        Assert.AreEqual(AssistantControlState.Player, owner.State);
        Assert.AreEqual(0, owner.ActionCount);
    }

    private Entity CreateBoundary(AssistantStateComponent assistantState)
    {
        Entity boundary = _entityManager.CreateEntity(typeof(UiShellRootComponent));
        _entityManager.AddComponentData(boundary, assistantState);
        _entityManager.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        return boundary;
    }
}
