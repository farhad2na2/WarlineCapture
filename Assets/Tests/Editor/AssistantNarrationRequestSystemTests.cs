using System;
using System.Reflection;
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
    private SystemHandle _narrationAudioResultSystem;
    private Entity _matchStartBoundary;

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
            RunCase(test => test.TutorialNarration_RetiresPriorCueBeforeSelectingTheNextCue());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_ThrottlesLowPriorityButAllowsCriticalInterruption());
            passed++;
            RunCase(test => test.AssistantNarrationRequestSystem_ThreatSuppressionExpiresAfterEightSeconds());
            passed++;
            RunCase(test => test.AssistantNarrationAudioRequestSystem_QueuesAriaVoicePlayback());
            passed++;
            RunCase(test => test.AssistantNarrationAudioRequestSystem_PreservesTextOnlyTruth());
            passed++;
            RunCase(test => test.AssistantNarrationSystems_GateAndClearOutsideStartedMatch());
            passed++;
            RunCase(test => test.AssistantNarrationAudioResultProjection_UsesNewestCorrelatedResult());
            passed++;
            RunCase(test => test.AssistantNarrationAudioTruth_ResolvesOnlyObservableStates());
            passed++;
            RunCase(test => test.AssistantNarrationPresentedPulse_EndsAtPointEightSeconds());
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
        _narrationAudioResultSystem = _world.CreateSystem<AssistantNarrationAudioResultProjectionSystem>();
        _matchStartBoundary = _entityManager.CreateEntity(
            typeof(MatchStartStateComponent),
            typeof(MatchStartQueueComponent));
        _entityManager.SetComponentData(_matchStartBoundary, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
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
        Assert.AreEqual(requests[0].RequestId, narrationState.ActiveNarrationId);
        Assert.AreEqual(0, narrationState.LastSpokenMessageId);
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
    public void TutorialNarration_RetiresPriorCueBeforeSelectingTheNextCue()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(
            boundary,
            1000001,
            AssistantMessagePriority.High,
            "Tap MOVE to select the move command.",
            requiresNarration: 1);

        _narrationSystem.Update(_world.Unmanaged);

        AddMessage(
            boundary,
            9001,
            AssistantMessagePriority.High,
            "Unrelated tactical alert",
            requiresNarration: 0);
        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        MethodInfo retire = typeof(UiShellEcsGateway).GetMethod(
            "RetirePreviousTutorialMessages",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(retire);
        Assert.AreEqual(1, retire.Invoke(null, new object[] { messages }));
        Assert.AreEqual(1, messages[0].Acknowledged);
        Assert.AreEqual(0, messages[0].RequiresNarration);
        Assert.AreEqual(0, messages[1].Acknowledged);

        AddMessage(
            boundary,
            1000002,
            AssistantMessagePriority.High,
            "Tap the highlighted destination to move your squad.",
            requiresNarration: 1);
        _narrationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantNarrationRequestElement> requests =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary);
        Assert.AreEqual(2, requests.Length);
        Assert.AreEqual(1000002, requests[1].MessageId);
        Assert.AreEqual(
            "Tap the highlighted destination to move your squad.",
            requests[1].Text.ToString());
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
    public void AssistantNarrationRequestSystem_ThreatSuppressionExpiresAfterEightSeconds()
    {
        AssistantNarrationRequestElement request = new()
        {
            MessageId = 810123,
            RequestedAt = 2f
        };
        AssistantMessageElement threat = new()
        {
            MessageId = 810123
        };
        AssistantMessageElement report = new()
        {
            MessageId = 820123
        };

        Assert.IsTrue(AssistantNarrationRequestSystem.IsRequestSuppressed(request, threat, 9.999f));
        Assert.IsFalse(AssistantNarrationRequestSystem.IsRequestSuppressed(request, threat, 10f));
        Assert.IsTrue(AssistantNarrationRequestSystem.IsRequestSuppressed(request, report, 100f));
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
        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, narrationRequests[0].Status);
        Assert.Greater(narrationRequests[0].AudioPlaybackRequestId, 0);
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, narrationRequests[0].AudioEventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash, narrationRequests[0].AudioEventHash);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> playbackRequests =
            _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, playbackRequests.Length);
        Assert.AreEqual(narrationRequests[0].AudioPlaybackRequestId, playbackRequests[0].RequestId);
        Assert.AreEqual(AudioPlaybackRequestKind.OneShot, playbackRequests[0].Kind);
        Assert.AreEqual(AudioPlaybackPriority.High, playbackRequests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, playbackRequests[0].Status);
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackType, playbackRequests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash, playbackRequests[0].EventHash);
        Assert.AreEqual("Voice", playbackRequests[0].BusId.ToString());
        Assert.AreEqual(1, playbackRequests[0].InterruptsLowerPriority);

        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);
        playbackRequests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, playbackRequests[0].Status);
    }

    [Test]
    public void AssistantNarrationAudioRequestSystem_PreservesTextOnlyTruth()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(boundary, 1351, AssistantMessagePriority.High, "Hostile cell spotted", requiresNarration: 1);
        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        AssistantMessageElement message = messages[0];
        message.AudioEventId = default;
        messages[0] = message;

        _narrationSystem.Update(_world.Unmanaged);
        _narrationAudioSystem.Update(_world.Unmanaged);

        AssistantNarrationRequestElement request =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary)[0];
        AssistantNarrationStateComponent narrationState =
            _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);

        Assert.AreEqual(AssistantCommandIntentStatus.Accepted, request.Status);
        Assert.AreEqual(0, request.AudioPlaybackRequestId);
        Assert.AreEqual(0, _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity).Length);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, narrationState.LastAudioStatus);
        Assert.AreEqual(0, narrationState.LastAudioFailureReason.Length);
        Assert.AreEqual(0, narrationState.IsSpeaking);
    }

    [Test]
    public void AssistantNarrationSystems_GateAndClearOutsideStartedMatch()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(boundary, 1401, AssistantMessagePriority.High, "Hostile patrol", requiresNarration: 1);

        MatchStartQueueComponent matchStart =
            _entityManager.GetComponentData<MatchStartQueueComponent>(_matchStartBoundary);
        matchStart.HasStarted = 0;
        matchStart.IsStartPending = 1;
        matchStart.LastStatus = MatchStartStatusKind.Starting;
        _entityManager.SetComponentData(_matchStartBoundary, matchStart);

        _narrationSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary).Length);

        matchStart.HasStarted = 1;
        matchStart.IsStartPending = 0;
        matchStart.LastStatus = MatchStartStatusKind.Started;
        _entityManager.SetComponentData(_matchStartBoundary, matchStart);
        _narrationSystem.Update(_world.Unmanaged);
        _narrationAudioSystem.Update(_world.Unmanaged);
        Assert.AreEqual(1, _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary).Length);

        UiShellStateComponent shellState = _entityManager.GetComponentData<UiShellStateComponent>(boundary);
        shellState.ActiveRoute = UIRoute.MainMenu;
        shellState.CurrentMode = UiShellMode.MainMenu;
        _entityManager.SetComponentData(boundary, shellState);
        _narrationSystem.Update(_world.Unmanaged);

        AssistantNarrationStateComponent narrationState =
            _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary).Length);
        Assert.AreEqual(0, narrationState.ActiveNarrationId);
        Assert.AreEqual(0, narrationState.ActiveAudioPlaybackRequestId);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, narrationState.LastAudioStatus);
        Assert.AreEqual(0f, narrationState.LastPresentedAt);
        Assert.AreEqual(0, narrationState.IsSpeaking);
    }

    [Test]
    public void AssistantNarrationAudioResultProjection_UsesNewestCorrelatedResult()
    {
        Entity boundary = CreateBoundary(AssistantNarrationMode.Important);
        AddMessage(
            boundary,
            1501,
            AssistantMessagePriority.High,
            "Hostile cell spotted",
            requiresNarration: 1,
            audioEventId: AudioEventIds.VOARIAMessageWarningGroundAttackType);

        _narrationSystem.Update(_world.Unmanaged);
        _narrationAudioSystem.Update(_world.Unmanaged);

        AssistantNarrationRequestElement narrationRequest =
            _entityManager.GetBuffer<AssistantNarrationRequestElement>(boundary)[0];
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);
        _narrationAudioResultSystem.Update(_world.Unmanaged);

        AssistantNarrationStateComponent narrationState =
            _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, narrationState.LastAudioStatus);
        Assert.AreEqual(0, narrationState.IsSpeaking);

        uint acceptedVersion = narrationState.Version;
        AudioEventRequestSystem.AppendPlaybackResult(_entityManager, audioEntity, new AudioPlaybackResultElement
        {
            RequestId = narrationRequest.AudioPlaybackRequestId + 1000,
            Status = AudioPlaybackRequestStatus.MissingClip,
            Reason = new FixedString64Bytes("Unrelated"),
            ProcessedAt = 1.05f
        });
        _narrationAudioResultSystem.Update(_world.Unmanaged);
        narrationState = _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(acceptedVersion, narrationState.Version);
        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, narrationState.LastAudioStatus);

        float presentedAt = Mathf.Max(0.01f, Time.unscaledTime);
        AudioEventRequestSystem.AppendPlaybackResult(_entityManager, audioEntity, new AudioPlaybackResultElement
        {
            RequestId = narrationRequest.AudioPlaybackRequestId,
            Status = AudioPlaybackRequestStatus.Presented,
            EventHash = narrationRequest.AudioEventHash,
            EventId = narrationRequest.AudioEventId,
            Reason = new FixedString64Bytes("Played"),
            ProcessedAt = presentedAt
        });
        _narrationAudioResultSystem.Update(_world.Unmanaged);

        narrationState = _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(AudioPlaybackRequestStatus.Presented, narrationState.LastAudioStatus);
        Assert.AreEqual(presentedAt, narrationState.LastPresentedAt);
        Assert.AreEqual(1501, narrationState.LastSpokenMessageId);
        Assert.AreEqual(0, narrationState.IsSpeaking);

        AudioEventRequestSystem.AppendPlaybackResult(_entityManager, audioEntity, new AudioPlaybackResultElement
        {
            RequestId = narrationRequest.AudioPlaybackRequestId,
            Status = AudioPlaybackRequestStatus.MissingClip,
            EventHash = narrationRequest.AudioEventHash,
            EventId = narrationRequest.AudioEventId,
            Reason = new FixedString64Bytes("debug-only-reason"),
            ProcessedAt = presentedAt + 0.1f
        });
        _narrationAudioResultSystem.Update(_world.Unmanaged);

        narrationState = _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
        Assert.AreEqual(AudioPlaybackRequestStatus.MissingClip, narrationState.LastAudioStatus);
        Assert.AreEqual("Voice clip unavailable", narrationState.LastAudioFailureReason.ToString());
        Assert.AreEqual(0f, narrationState.LastPresentedAt);
        Assert.AreEqual(0, narrationState.IsSpeaking);
    }

    [Test]
    public void AssistantNarrationAudioTruth_ResolvesOnlyObservableStates()
    {
        AssistantSettingsComponent assistantSettings = new()
        {
            NarrationMode = AssistantNarrationMode.Important
        };
        AudioSettingsComponent audioSettings = new()
        {
            MasterVolume = 1f,
            VoiceVolume = 1f
        };
        AssistantNarrationRequestElement request = new()
        {
            Text = new FixedString128Bytes("Hostile cell spotted"),
            AudioEventId = new FixedString64Bytes(AudioEventIds.VOARIAMessageWarningGroundAttackType),
            AudioPlaybackRequestId = 7
        };
        AssistantNarrationStateComponent narrationState = new()
        {
            Mode = AssistantNarrationMode.Important,
            LastAudioStatus = AudioPlaybackRequestStatus.Pending
        };

        Assert.AreEqual(
            UiAssistantNarrationStateKind.Queued,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));

        narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Accepted;
        Assert.AreEqual(
            UiAssistantNarrationStateKind.Accepted,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));

        narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Presented;
        Assert.AreEqual(
            UiAssistantNarrationStateKind.Presented,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));

        narrationState.LastAudioStatus = AudioPlaybackRequestStatus.MissingEvent;
        Assert.AreEqual(
            UiAssistantNarrationStateKind.Failed,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));

        narrationState.LastAudioStatus = AudioPlaybackRequestStatus.Pending;
        request.AudioEventId = default;
        request.AudioPlaybackRequestId = 0;
        Assert.AreEqual(
            UiAssistantNarrationStateKind.TextOnly,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));

        audioSettings.VoiceMuted = 1;
        Assert.AreEqual(
            UiAssistantNarrationStateKind.Off,
            AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                assistantSettings,
                audioSettings,
                request,
                narrationState));
    }

    [Test]
    public void AssistantNarrationPresentedPulse_EndsAtPointEightSeconds()
    {
        AssistantNarrationStateComponent narrationState = new()
        {
            LastAudioStatus = AudioPlaybackRequestStatus.Presented,
            LastPresentedAt = 10f,
            IsSpeaking = 0
        };

        Assert.IsTrue(AssistantNarrationAudioResultProjectionSystem.IsPresentationPulseActive(narrationState, 10f));
        Assert.IsTrue(AssistantNarrationAudioResultProjectionSystem.IsPresentationPulseActive(narrationState, 10.799f));
        Assert.IsFalse(AssistantNarrationAudioResultProjectionSystem.IsPresentationPulseActive(narrationState, 10.8f));
        Assert.IsFalse(AssistantNarrationAudioResultProjectionSystem.IsPresentationPulseActive(narrationState, 11f));
        Assert.AreEqual(0, narrationState.IsSpeaking);
    }

    private Entity CreateBoundary(AssistantNarrationMode mode)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent),
            typeof(AssistantSettingsComponent));
        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        _entityManager.SetComponentData(boundary, DefaultStatus());
        _entityManager.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            FuelText = new FixedString32Bytes("9,750"),
            MaterialsText = new FixedString32Bytes("92/120"),
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
