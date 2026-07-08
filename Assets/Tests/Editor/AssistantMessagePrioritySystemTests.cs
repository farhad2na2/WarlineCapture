using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantMessagePrioritySystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _messageSystem;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantMessagePrioritySystem_PublishesThreatAndFeedbackMessages());
            passed++;
            RunCase(test => test.AssistantMessagePrioritySystem_RemovesInactiveStatusMessages());
            passed++;
            RunCase(test => test.AssistantMessagePrioritySystem_UpdatesChangedFeedbackWithoutDuplicating());
            passed++;

            Debug.Log($"[AssistantMessagePrioritySystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantMessagePrioritySystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantMessagePrioritySystemTests> testCase)
    {
        var tests = new AssistantMessagePrioritySystemTests();
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
        _world = new World(nameof(AssistantMessagePrioritySystemTests));
        _entityManager = _world.EntityManager;
        _messageSystem = _world.CreateSystem<AssistantMessagePrioritySystem>();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AssistantMessagePrioritySystem_PublishesThreatAndFeedbackMessages()
    {
        Entity boundary = CreateBoundary(StatusWithThreatAndFeedback());

        _messageSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(2, messages.Length);

        AssistantMessageElement threat =
            FindMessage(messages, AssistantMessagePrioritySystem.ThreatMessageId);
        Assert.AreEqual(AssistantMessagePriority.High, threat.Priority);
        Assert.AreEqual(AssistantRecommendationKind.DefensiveAlert, threat.RelatedKind);
        Assert.AreEqual("assistant.threat", threat.SuppressionKey.ToString());
        Assert.AreEqual("Hostile patrol: North gate", threat.Text.ToString());
        Assert.AreEqual("aria.threat", threat.AudioEventId.ToString());
        Assert.AreEqual(1, threat.RequiresNarration);
        Assert.AreEqual(0, threat.Acknowledged);

        AssistantMessageElement feedback =
            FindMessage(messages, AssistantMessagePrioritySystem.FeedbackMessageId);
        Assert.AreEqual(AssistantMessagePriority.Normal, feedback.Priority);
        Assert.AreEqual(AssistantRecommendationKind.Explain, feedback.RelatedKind);
        Assert.AreEqual("assistant.feedback", feedback.SuppressionKey.ToString());
        Assert.AreEqual("Blocked: civilian zone", feedback.Text.ToString());
        Assert.AreEqual(0, feedback.RequiresNarration);

        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreEqual(1, assistantState.UiDirty);

        _messageSystem.Update(_world.Unmanaged);
        messages = _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(2, messages.Length);
    }

    [Test]
    public void AssistantMessagePrioritySystem_RemovesInactiveStatusMessages()
    {
        Entity boundary = CreateBoundary(StatusWithThreatAndFeedback());
        _messageSystem.Update(_world.Unmanaged);

        UiMatchHudStatusSurfacesComponent status = StatusWithThreatAndFeedback();
        status.ThreatVisible = 0;
        status.FeedbackVisible = 0;
        _entityManager.SetComponentData(boundary, status);

        _messageSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(0, messages.Length);
    }

    [Test]
    public void AssistantMessagePrioritySystem_UpdatesChangedFeedbackWithoutDuplicating()
    {
        Entity boundary = CreateBoundary(StatusWithThreatAndFeedback());
        _messageSystem.Update(_world.Unmanaged);

        UiMatchHudStatusSurfacesComponent status = StatusWithThreatAndFeedback();
        status.FeedbackText = new FixedString64Bytes("Choose a valid destination");
        _entityManager.SetComponentData(boundary, status);

        _messageSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(2, messages.Length);
        AssistantMessageElement feedback =
            FindMessage(messages, AssistantMessagePrioritySystem.FeedbackMessageId);
        Assert.AreEqual("Choose a valid destination", feedback.Text.ToString());
        Assert.AreEqual(0, feedback.Acknowledged);
    }

    private Entity CreateBoundary(UiMatchHudStatusSurfacesComponent status)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent));
        _entityManager.SetComponentData(boundary, status);
        _entityManager.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("9,750"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        });
        return boundary;
    }

    private static AssistantMessageElement FindMessage(
        DynamicBuffer<AssistantMessageElement> messages,
        int messageId)
    {
        for (int i = 0; i < messages.Length; i++)
        {
            if (messages[i].MessageId == messageId)
                return messages[i];
        }

        Assert.Fail($"Missing assistant message {messageId}.");
        return default;
    }

    private static UiMatchHudStatusSurfacesComponent StatusWithThreatAndFeedback()
    {
        return new UiMatchHudStatusSurfacesComponent
        {
            ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
            Objective0Text = new FixedString64Bytes("Neutralize hostile patrol"),
            Objective1Text = new FixedString64Bytes("Protect civilians"),
            Objective2Text = new FixedString64Bytes("Keep losses low"),
            Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
            Objective1IconKind = UiMatchHudObjectiveIconKind.Checked,
            Objective2IconKind = UiMatchHudObjectiveIconKind.Star,
            ElapsedText = new FixedString32Bytes("00:30"),
            ThreatVisible = 1,
            ThreatTitle = new FixedString64Bytes("Hostile patrol"),
            ThreatSubtitle = new FixedString64Bytes("North gate"),
            FeedbackVisible = 1,
            FeedbackText = new FixedString64Bytes("Blocked: civilian zone")
        };
    }
}
