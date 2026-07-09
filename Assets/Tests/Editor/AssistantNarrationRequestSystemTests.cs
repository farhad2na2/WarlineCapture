using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantNarrationRequestSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _narrationSystem;
    private SystemHandle _narrationAudioSystem;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantNarrationRequestSystem_CreatesRequestForImportantMessage());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_SuppressesDuplicateRequests());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_RespectsNarrationModeGate());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_CoalescesSameSuppressionKey());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_ThrottlesLowPriorityButAllowsCriticalInterruption());
            passed++;
            RunCase(test => test.AssistantNarrationAudioRequestSystem_QueuesAriaVoicePlayback());
            passed++;

            Debug.Log($"[AssistantNarrationRequestSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantNarrationRequestSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantNarrationRequestSystemTests> testCase)
    {
        var tests = new AssistantNarrationRequestSystemTests();
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
        _world = new World(nameof(AssistantNarrationRequestSystemTests));
        _entityManager = _world.EntityManager;
        _narrationSystem = _world.CreateSystem<AssistantNarrationRequestSystem>();
        _narrationAudioSystem = _world.CreateSystem<AssistantNarrationAudioRequestSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AssistantNarrationRequestSystem_CreatesRequestForImportantMessage()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(boundary, 1001, AssistantMessagePriority.High, "Hostile patrol near base", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1001, requests[0].MessageId);
        Assert.AreEqual(AssistantMessagePriority.High, requests[0].Priority);
        Assert.AreEqual(AssistantCommandIntentStatus.Pending, requests[0].Status);
        Assert.AreEqual("Hostile patrol near base", requests[0].Text.ToString());
        Assert.AreEqual(1, requests[0].InterruptsLowerPriority);

        AssistantNarrationStateComponent narrationState =
            _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(1001, narrationState.LastSpokenMessageId);
        Assert.AreEqual(AssistantNarrationMode.Important, narrationState.Mode);
        Assert.AreEqual(1, narrationState.UiDirty);
    }

    [Test]
    public void AssistantNarrationRequestSystem_SuppressesDuplicateRequests()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.All);
        AddMessage(boundary, 1002, AssistantMessagePriority.Normal, "Oil trucks are idle", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);
        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1002, requests[0].MessageId);
        Assert.AreEqual(0, requests[0].InterruptsLowerPriority);
    }

    [Test]
    public void AssistantNarrationRequestSystem_RespectsNarrationModeGate()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.CriticalOnly);
        AddMessage(boundary, 1003, AssistantMessagePriority.High, "Fuel low", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(0, requests.Length);

        AddMessage(boundary, 1004, AssistantMessagePriority.Critical, "Base under attack", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        requests = _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1004, requests[0].MessageId);

        AssistantSettingsComponent settings =
            _entityManager.GetComponentData<AssistantSettingsComponent>(boundary);
        settings.NarrationMode = AssistantNarrationMode.Off;
        _entityManager.SetComponentData(boundary, settings);
        AddMessage(boundary, 1005, AssistantMessagePriority.Critical, "Command center critical", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        requests = _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
    }

    [Test]
    public void AssistantNarrationRequestSystem_CoalescesSameSuppressionKey()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(
            boundary,
            1101,
            AssistantMessagePriority.High,
            "Hostile armor spotted",
            requiresNarration: 1,
            suppressionKey: "threat.cluster");

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        AssistantMessageElement first = messages[0];
        first.Acknowledged = 1;
        messages[0] = first;
        AddMessage(
            boundary,
            1102,
            AssistantMessagePriority.High,
            "Hostile armor still near base",
            requiresNarration: 1,
            suppressionKey: "threat.cluster");

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1101, requests[0].MessageId);
    }

    [Test]
    public void AssistantNarrationRequestSystem_ThrottlesLowPriorityButAllowsCriticalInterruption()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.All);
        AddMessage(boundary, 1201, AssistantMessagePriority.Normal, "Oil trucks are idle", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        AssistantMessageElement normal = messages[0];
        normal.Acknowledged = 1;
        messages[0] = normal;
        AddMessage(boundary, 1202, AssistantMessagePriority.Normal, "Supply trucks are idle", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(1201, requests[0].MessageId);

        AddMessage(boundary, 1203, AssistantMessagePriority.Critical, "Base under attack", requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        requests = _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(2, requests.Length);
        Assert.AreEqual(1203, requests[1].MessageId);
        Assert.AreEqual(1, requests[1].InterruptsLowerPriority);
    }

    [Test]
    public void AssistantNarrationAudioRequestSystem_QueuesAriaVoicePlayback()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(
            boundary,
            1301,
            AssistantMessagePriority.High,
            "Hostile cell spotted",
            requiresNarration: 1,
            audioEventId: AudioEventIds.VOARIAMessageWarningGroundAttackType);

        _narrationSystem.Update(_world.Unmanaged);
        _narrationAudioSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> narrationRequests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(1, narrationRequests.Length);
        Assert.AreEqual(AssistantCommandIntentStatus.Completed, narrationRequests[0].Status);
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, narrationRequests[0].AudioEventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash, narrationRequests[0].AudioEventHash);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> playbackRequests =
            _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, playbackRequests.Length);
        Assert.AreEqual(AudioPlaybackRequestKind.OneShot, playbackRequests[0].Kind);
        Assert.AreEqual(AudioPlaybackPriority.High, playbackRequests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, playbackRequests[0].Status);
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, playbackRequests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash, playbackRequests[0].EventHash);
        Assert.AreEqual("Voice", playbackRequests[0].BusId.ToString());

        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);
        playbackRequests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, playbackRequests[0].Status);
    }

    private Entity CreateBoundary(AssistantNarrationMode mode)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent),
            typeof(AssistantSettingsComponent));
        _entityManager.SetComponentData(boundary, DefaultStatus());
        _entityManager.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("9,750"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        });
        _entityManager.SetComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = mode,
            AllowTakeover = 1,
            SubtitlesEnabled = 1
        });
        _entityManager.AddBuffer<AssistantMessageElement>(boundary);
        return boundary;
    }

    private void AddMessage(
        Entity boundary,
        int messageId,
        AssistantMessagePriority priority,
        string text,
        byte requiresNarration,
        string suppressionKey = null,
        string audioEventId = null)
    {
        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        messages.Add(new AssistantMessageElement
        {
            MessageId = messageId,
            SourceVersion = messageId,
            Priority = priority,
            RelatedKind = AssistantRecommendationKind.Explain,
            SuppressionKey = new FixedString64Bytes(suppressionKey ?? $"test.{messageId}"),
            Text = new FixedString128Bytes(text),
            AudioEventId = new FixedString64Bytes(audioEventId ?? $"aria.{messageId}"),
            RequiresNarration = requiresNarration,
            Acknowledged = 0
        });
    }

    private static UiMatchHudStatusSurfacesComponent DefaultStatus()
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
            ElapsedText = new FixedString32Bytes("00:30")
        };
    }
}
