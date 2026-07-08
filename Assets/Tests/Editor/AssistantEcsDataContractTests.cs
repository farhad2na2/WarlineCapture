using System;
using Game.Components;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AssistantEcsDataContractTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantComponents_AreUnmanagedEcsData());
            passed++;
            RunCase(test => test.AssistantBuffers_CanBeCreatedAndPopulatedWithoutManagedReferences());
            passed++;
            RunCase(test => test.AssistantEnums_KeepExpectedStableValues());
            passed++;

            Debug.Log($"[AssistantEcsDataContractValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantEcsDataContractValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantEcsDataContractTests> testCase)
    {
        var tests = new AssistantEcsDataContractTests();
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
        _world = new World("AssistantEcsDataContractTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AssistantComponents_AreUnmanagedEcsData()
    {
        AssertComponent<AssistantStateComponent>();
        AssertComponent<AssistantControlOwnerComponent>();
        AssertComponent<AssistantRecommendationReadModelComponent>();
        AssertComponent<AssistantNarrationStateComponent>();
        AssertComponent<AssistantSettingsComponent>();
    }

    [Test]
    public void AssistantBuffers_CanBeCreatedAndPopulatedWithoutManagedReferences()
    {
        AssertBuffer<AssistantGoalReadModelElement>();
        AssertBuffer<AssistantRecommendationElement>();
        AssertBuffer<AssistantMessageElement>();
        AssertBuffer<AssistantNarrationRequestElement>();
        AssertBuffer<AssistantCommandIntentRequestElement>();
        AssertBuffer<AssistantCommandIntentResultElement>();
        AssertBuffer<AssistantPreviewHighlightElement>();

        Entity assistant = _entityManager.CreateEntity(
            typeof(AssistantStateComponent),
            typeof(AssistantControlOwnerComponent),
            typeof(AssistantRecommendationReadModelComponent),
            typeof(AssistantNarrationStateComponent),
            typeof(AssistantSettingsComponent));

        _entityManager.AddBuffer<AssistantGoalReadModelElement>(assistant).Add(new AssistantGoalReadModelElement
        {
            GoalId = 1,
            State = AssistantGoalState.Active,
            Priority = AssistantMessagePriority.High,
            Title = new FixedString64Bytes("Protect civilians"),
            Body = new FixedString128Bytes("Keep civilian risk under control."),
            IsPrimary = 1
        });

        _entityManager.AddBuffer<AssistantRecommendationElement>(assistant).Add(new AssistantRecommendationElement
        {
            RecommendationId = 7,
            Kind = AssistantRecommendationKind.CameraFocus,
            Priority = AssistantMessagePriority.Normal,
            TargetKind = AssistantTargetKind.WorldPosition,
            Score = 10f,
            WorldPosition = new float3(12f, 0f, 3f),
            Title = new FixedString64Bytes("Focus objective"),
            Reason = new FixedString128Bytes("The active objective needs attention."),
            ActionLabel = new FixedString64Bytes("SHOW ME"),
            CanShow = 1
        });

        _entityManager.AddBuffer<AssistantMessageElement>(assistant).Add(new AssistantMessageElement
        {
            MessageId = 11,
            Priority = AssistantMessagePriority.Critical,
            RelatedKind = AssistantRecommendationKind.DefensiveAlert,
            SuppressionKey = new FixedString64Bytes("threat.warning"),
            Text = new FixedString128Bytes("Hostile movement detected."),
            AudioEventId = new FixedString64Bytes("VO.Assistant.Threat.Warning.01"),
            RequiresNarration = 1
        });

        _entityManager.AddBuffer<AssistantNarrationRequestElement>(assistant).Add(new AssistantNarrationRequestElement
        {
            RequestId = 12,
            MessageId = 11,
            Priority = AssistantMessagePriority.Critical,
            Status = AssistantCommandIntentStatus.Pending,
            Text = new FixedString128Bytes("Hostile movement detected."),
            AudioEventId = new FixedString64Bytes("VO.Assistant.Threat.Warning.01"),
            InterruptsLowerPriority = 1
        });

        _entityManager.AddBuffer<AssistantCommandIntentRequestElement>(assistant).Add(new AssistantCommandIntentRequestElement
        {
            RequestId = 13,
            RecommendationId = 7,
            Kind = AssistantCommandIntentKind.FocusCamera,
            TargetKind = AssistantTargetKind.WorldPosition,
            WorldPosition = new float3(12f, 0f, 3f)
        });

        _entityManager.AddBuffer<AssistantCommandIntentResultElement>(assistant).Add(new AssistantCommandIntentResultElement
        {
            RequestId = 13,
            RecommendationId = 7,
            Kind = AssistantCommandIntentKind.FocusCamera,
            Status = AssistantCommandIntentStatus.Accepted,
            TargetKind = AssistantTargetKind.WorldPosition,
            WorldPosition = new float3(12f, 0f, 3f),
            Message = new FixedString64Bytes("Camera focus accepted.")
        });

        _entityManager.AddBuffer<AssistantPreviewHighlightElement>(assistant).Add(new AssistantPreviewHighlightElement
        {
            RequestId = 13,
            RecommendationId = 7,
            TargetKind = AssistantTargetKind.WorldPosition,
            WorldPosition = new float3(12f, 0f, 3f),
            Strength = 1f,
            Active = 1
        });

        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantGoalReadModelElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantRecommendationElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantMessageElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantNarrationRequestElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantCommandIntentRequestElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantCommandIntentResultElement>(assistant).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantPreviewHighlightElement>(assistant).Length);
    }

    [Test]
    public void AssistantEnums_KeepExpectedStableValues()
    {
        Assert.AreEqual(0, (byte)AssistantControlState.Player);
        Assert.AreEqual(3, (byte)AssistantControlState.AssistantTakeover);
        Assert.AreEqual(0, (byte)AssistantMessagePriority.Low);
        Assert.AreEqual(1, (byte)AssistantMessagePriority.Normal);
        Assert.AreEqual(2, (byte)AssistantMessagePriority.High);
        Assert.AreEqual(3, (byte)AssistantMessagePriority.Critical);
        Assert.AreEqual(2, (byte)AssistantNarrationMode.Important);
        Assert.AreEqual(5, (byte)AssistantCommandIntentStatus.TimedOut);
        Assert.AreEqual(5, (byte)AssistantCommandIntentKind.FocusCamera);
    }

    private static void AssertComponent<T>()
        where T : unmanaged, IComponentData
    {
        ComponentType componentType = ComponentType.ReadWrite<T>();
        Assert.IsFalse(componentType.IsBuffer, $"{typeof(T).Name} must be an IComponentData component, not a buffer.");
    }

    private static void AssertBuffer<T>()
        where T : unmanaged, IBufferElementData
    {
        ComponentType componentType = ComponentType.ReadWrite<T>();
        Assert.IsTrue(componentType.IsBuffer, $"{typeof(T).Name} must be an IBufferElementData dynamic-buffer row.");
    }
}
