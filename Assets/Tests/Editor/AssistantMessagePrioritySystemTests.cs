using System;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
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
            RunCase(test => test.PublishesVerifiedThreatMessage()); passed++;
            RunCase(test => test.RemovesExpiredThreatMessage()); passed++;
            RunCase(test => test.UpdatesThreatWithoutDuplicating()); passed++;
            RunCase(test => test.UsesAirWarningVoiceForAirThreat()); passed++;
            RunCase(test => test.SuppressesMessagesOutsideActiveMatch()); passed++;
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
        try { testCase(tests); }
        finally { tests.TearDown(); }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World(nameof(AssistantMessagePrioritySystemTests));
        _entityManager = _world.EntityManager;
        _messageSystem = _world.CreateSystem<AssistantMessagePrioritySystem>();
    }

    [TearDown]
    public void TearDown() => _world?.Dispose();

    [Test]
    public void PublishesVerifiedThreatMessage()
    {
        Entity boundary = CreateBoundary();
        AddThreat(boundary, AssistantThreatKind.GroundAttack, 18, 4f);

        _messageSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages = _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(1, messages.Length);
        Assert.AreEqual(AssistantMessagePriority.High, messages[0].Priority);
        Assert.AreEqual(AssistantRecommendationKind.DefensiveAlert, messages[0].RelatedKind);
        StringAssert.Contains("RIFLE SQUAD", messages[0].Text.ToString());
        StringAssert.Contains("HOSTILE CAR", messages[0].Text.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, messages[0].AudioEventId.ToString());
        Assert.AreEqual(1, messages[0].RequiresNarration);
        Assert.AreEqual(1u, _entityManager.GetComponentData<AssistantMessageReadModelComponent>(boundary).Version);
    }

    [Test]
    public void RemovesExpiredThreatMessage()
    {
        Entity boundary = CreateBoundary();
        AddThreat(boundary, AssistantThreatKind.GroundAttack, 18, 0.5f);
        _world.SetTime(new TimeData(1d, 0.1f));

        _messageSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantMessageElement>(boundary).Length);
    }

    [Test]
    public void UpdatesThreatWithoutDuplicating()
    {
        Entity boundary = CreateBoundary();
        DynamicBuffer<AssistantThreatReadModelElement> threats = AddThreat(
            boundary,
            AssistantThreatKind.GroundAttack,
            18,
            4f);
        _messageSystem.Update(_world.Unmanaged);

        threats = _entityManager.GetBuffer<AssistantThreatReadModelElement>(boundary);
        AssistantThreatReadModelElement updated = threats[0];
        updated.SourceEventId = 12;
        updated.Damage = 33;
        threats[0] = updated;
        _messageSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages = _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(1, messages.Length);
        StringAssert.Contains("damage 33", messages[0].Text.ToString());
    }

    [Test]
    public void UsesAirWarningVoiceForAirThreat()
    {
        Entity boundary = CreateBoundary();
        AddThreat(boundary, AssistantThreatKind.AirAttack, 10, 4f);

        _messageSystem.Update(_world.Unmanaged);

        AssistantMessageElement message = _entityManager.GetBuffer<AssistantMessageElement>(boundary)[0];
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningAirAttackType, message.AudioEventId.ToString());
    }

    [Test]
    public void SuppressesMessagesOutsideActiveMatch()
    {
        Entity boundary = CreateBoundary(UIRoute.MainMenu, UiShellMode.MainMenu);
        AddThreat(boundary, AssistantThreatKind.GroundAttack, 10, 4f);

        _messageSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantMessageElement>(boundary).Length);
    }

    private Entity CreateBoundary(
        UIRoute route = UIRoute.Match,
        UiShellMode mode = UiShellMode.MatchHud)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudHeaderComponent));
        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = mode,
            ActiveRoute = route,
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

    private DynamicBuffer<AssistantThreatReadModelElement> AddThreat(
        Entity boundary,
        AssistantThreatKind kind,
        int damage,
        float expiresAt)
    {
        DynamicBuffer<AssistantThreatReadModelElement> threats =
            _entityManager.AddBuffer<AssistantThreatReadModelElement>(boundary);
        threats.Add(new AssistantThreatReadModelElement
        {
            ThreatId = 44,
            SourceEventId = 11,
            Kind = kind,
            Priority = AssistantMessagePriority.High,
            Damage = damage,
            FriendlyName = new FixedString64Bytes("RIFLE SQUAD"),
            HostileName = new FixedString64Bytes("HOSTILE CAR"),
            ExpiresAt = expiresAt,
            Reason = new FixedString128Bytes("Verified combat damage")
        });
        return threats;
    }
}
