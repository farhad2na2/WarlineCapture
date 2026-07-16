using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AssistantCommandIntentGatewayTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.TryEnqueueAssistantCommandIntent_CopiesTopRecommendationIntoRequest());
            passed++;
            RunCase(test => test.TryEnqueueAssistantCommandIntent_MapsExecutableRecommendationKind());
            passed++;
            RunCase(test => test.TryEnqueueAssistantCommandIntent_PreservesTakeoverFlag());
            passed++;
            RunCase(test => test.TryEnqueueAssistantCommandIntent_BlocksTakeoverWhenDisabled());
            passed++;
            RunCase(test => test.TryEnqueueAssistantCommandIntent_QueuesStopWithoutRecommendation());
            passed++;
            RunCase(test => test.TryReadMatchHudAssistantHighlight_ConvertsActivePreviewRow());
            passed++;

            Debug.Log($"[AssistantCommandIntentGatewayValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantCommandIntentGatewayValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantCommandIntentGatewayTests> testCase)
    {
        var tests = new AssistantCommandIntentGatewayTests();
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

    private World _previousWorld;
    private World _world;
    private EntityManager _entityManager;

    [SetUp]
    public void SetUp()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World(nameof(AssistantCommandIntentGatewayTests));
        World.DefaultGameObjectInjectionWorld = _world;
        UiShellEcsGateway.RegisterAsRuntimeGateway();
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        UiShellEcsGateway.RegisterAsRuntimeGateway();
        _world?.Dispose();
    }

    [Test]
    public void TryEnqueueAssistantCommandIntent_CopiesTopRecommendationIntoRequest()
    {
        Entity boundary = CreateActiveBoundary();
        Entity source = _entityManager.CreateEntity();
        Entity target = _entityManager.CreateEntity();
        FixedString64Bytes targetId = new("anchor.objective.destroy_patrol_group");
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3101,
            SourceVersion = 7,
            Kind = AssistantRecommendationKind.Attack,
            Priority = AssistantMessagePriority.Normal,
            TargetKind = AssistantTargetKind.Entity,
            TargetId = targetId,
            SourceEntity = source,
            TargetEntity = target,
            TargetCell = new int2(4, 5),
            WorldPosition = new float3(12f, 3f, 9f),
            Title = new FixedString64Bytes("Assign an attack target"),
            ActionLabel = new FixedString64Bytes("SHOW ME"),
            CanShow = 1
        });

        Assert.IsTrue(UiShellEcsGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ShowRecommendation, false));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentResultElement>(boundary));

        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, requests[0].RequestId);
        Assert.AreEqual(3101, requests[0].RecommendationId);
        Assert.AreEqual(7, requests[0].RecommendationSourceVersion);
        Assert.AreEqual(AssistantCommandIntentKind.ShowRecommendation, requests[0].Kind);
        Assert.AreEqual(AssistantTargetKind.Entity, requests[0].TargetKind);
        Assert.AreEqual(targetId, requests[0].TargetId);
        Assert.AreEqual(source, requests[0].SourceEntity);
        Assert.AreEqual(target, requests[0].TargetEntity);
        Assert.AreEqual(new int2(4, 5), requests[0].TargetCell);
        Assert.AreEqual(new float3(12f, 3f, 9f), requests[0].WorldPosition);
        Assert.AreEqual(0, requests[0].FromTakeover);
    }

    [Test]
    public void TryEnqueueAssistantCommandIntent_MapsExecutableRecommendationKind()
    {
        Entity boundary = CreateActiveBoundary();
        Entity source = _entityManager.CreateEntity();
        Entity target = _entityManager.CreateEntity();
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3101,
            Kind = AssistantRecommendationKind.Attack,
            TargetKind = AssistantTargetKind.Entity,
            SourceEntity = source,
            TargetEntity = target,
            ActionLabel = new FixedString64Bytes("SHOW ME"),
            CanShow = 1,
            CanExecute = 1
        });

        Assert.IsTrue(UiShellEcsGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation, false));

        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AssistantCommandIntentKind.AttackEntity, requests[0].Kind);
        Assert.AreEqual(3101, requests[0].RecommendationId);
        Assert.AreEqual(source, requests[0].SourceEntity);
        Assert.AreEqual(target, requests[0].TargetEntity);
    }

    [Test]
    public void TryEnqueueAssistantCommandIntent_PreservesTakeoverFlag()
    {
        Entity boundary = CreateActiveBoundary();
        _entityManager.AddComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = AssistantNarrationMode.Important,
            AllowTakeover = 1,
            SubtitlesEnabled = 1
        });
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3001,
            Kind = AssistantRecommendationKind.Select,
            TargetKind = AssistantTargetKind.UiSurface,
            CanExecute = 1,
            CanTakeControl = 1
        });

        Assert.IsTrue(UiShellEcsGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation, true));

        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AssistantCommandIntentKind.SelectEntity, requests[0].Kind);
        Assert.AreEqual(1, requests[0].FromTakeover);
    }

    [Test]
    public void TryEnqueueAssistantCommandIntent_BlocksTakeoverWhenDisabled()
    {
        Entity boundary = CreateActiveBoundary();
        _entityManager.AddComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = AssistantNarrationMode.Important,
            AllowTakeover = 0,
            SubtitlesEnabled = 1
        });
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        recommendations.Add(new AssistantRecommendationElement
        {
            RecommendationId = 3001,
            Kind = AssistantRecommendationKind.Select,
            TargetKind = AssistantTargetKind.UiSurface,
            CanExecute = 1,
            CanTakeControl = 1
        });

        Assert.IsFalse(UiShellEcsGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation, true));
        Assert.IsFalse(_entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary));
    }

    [Test]
    public void TryEnqueueAssistantCommandIntent_QueuesStopWithoutRecommendation()
    {
        Entity boundary = CreateActiveBoundary();

        Assert.IsTrue(UiShellEcsGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.StopAssistantControl, false));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentResultElement>(boundary));

        DynamicBuffer<AssistantCommandIntentRequestElement> requests =
            _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1, requests[0].RequestId);
        Assert.AreEqual(0, requests[0].RecommendationId);
        Assert.AreEqual(AssistantCommandIntentKind.StopAssistantControl, requests[0].Kind);
        Assert.AreEqual(AssistantTargetKind.None, requests[0].TargetKind);
        Assert.AreEqual(0, requests[0].FromTakeover);
    }

    [Test]
    public void TryReadMatchHudAssistantHighlight_ConvertsActivePreviewRow()
    {
        Entity boundary = _entityManager.CreateEntity(typeof(UiShellRootComponent));
        DynamicBuffer<AssistantPreviewHighlightElement> highlights =
            _entityManager.AddBuffer<AssistantPreviewHighlightElement>(boundary);
        highlights.Add(new AssistantPreviewHighlightElement
        {
            RequestId = 12,
            RecommendationId = 4501,
            TargetKind = AssistantTargetKind.WorldPosition,
            WorldPosition = new float3(21f, 4f, 13f),
            Strength = 0.75f,
            Active = 1
        });

        Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantHighlight(out UiAssistantHighlightModel highlight));
        Assert.IsTrue(highlight.Active);
        Assert.AreEqual(12, highlight.RequestId);
        Assert.AreEqual(4501, highlight.RecommendationId);
        Assert.AreEqual((byte)AssistantTargetKind.WorldPosition, highlight.TargetKind);
        Assert.AreEqual(21f, highlight.WorldX);
        Assert.AreEqual(4f, highlight.WorldY);
        Assert.AreEqual(13f, highlight.WorldZ);
        Assert.AreEqual(0.75f, highlight.Strength);
        Assert.Greater(highlight.Version, 0u);
    }

    private Entity CreateActiveBoundary()
    {
        Entity boundary = _entityManager.CreateEntity(typeof(UiShellRootComponent), typeof(UiShellStateComponent));
        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            CurrentMode = UiShellMode.MatchHud,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        Entity matchStart = _entityManager.CreateEntity(typeof(MatchStartQueueComponent));
        _entityManager.SetComponentData(matchStart, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
        return boundary;
    }
}
