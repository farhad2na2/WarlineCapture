using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AudioPlaybackPresentationBridgeSystemHelperTests
{
    private readonly List<AudioClip> _clips = new();
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.DrainAcceptedRequests_PlaysAcceptedRequestOnce());
            passed++;
            RunCase(test => test.DrainAcceptedRequests_DoesNotReplayPresentedRequestWithNewBridge());
            passed++;
            RunCase(test => test.DrainAcceptedRequests_SkipsPendingAndCooldownSkippedRequests());
            passed++;
            RunCase(test => test.DrainAcceptedRequests_RecordsMissingEventWithoutPlayback());
            passed++;
            RunCase(test => test.DrainAcceptedRequests_CullsGameplayVoiceWhileSimulationInactive());
            passed++;
            RunCase(test => test.DrainAcceptedRequests_FadesSettingsChangesOnActiveMusicSource());
            passed++;

            Debug.Log($"[AudioPlaybackPresentationBridgeValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioPlaybackPresentationBridgeValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    private static void RunCase(Action<AudioPlaybackPresentationBridgeSystemHelperTests> testCase)
    {
        AudioPlaybackPresentationBridgeSystemHelperTests tests = new();
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
        _world = new World("AudioPlaybackPresentationBridgeSystemHelperTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();

        for (int i = 0; i < _clips.Count; i++)
        {
            if (_clips[i] != null)
                UnityEngine.Object.DestroyImmediate(_clips[i]);
        }

        _clips.Clear();
    }

    [Test]
    public void DrainAcceptedRequests_PlaysAcceptedRequestOnce()
    {
        AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
            AudioEventIds.UIButtonPrimaryClickHash,
            new FixedString32Bytes("UI"),
            AudioPlaybackPriority.Medium,
            requestedAt: 1f);
        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);

        AudioEventCatalogConfig catalog = CreateCatalog(CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            "UI",
            CreateClip("ui_primary_click")));
        AudioMixerBusConfig buses = CreateBuses(CreateBus("UI"));
        using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 2);
        AudioPlaybackPresentationBridgeSystemHelper bridge = new();

        AudioPlaybackPresentationBridgeResult first = bridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            buses,
            playback,
            now: 1.1f);
        AudioPlaybackPresentationBridgeResult second = bridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            buses,
            playback,
            now: 1.2f);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);

        Assert.AreEqual(1, first.PresentedCount);
        Assert.AreEqual(1, first.PlayedCount);
        Assert.AreEqual(0, first.FailedCount);
        Assert.AreEqual(0, second.PresentedCount);
        Assert.AreEqual(1, playback.ActiveSourceCount);
        Assert.AreEqual(1, bridge.LastPresentedRequestId);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual("Played", results[1].Reason.ToString());
        Assert.AreEqual(AudioPlaybackRequestStatus.Presented, requests[0].Status);
    }

    [Test]
    public void DrainAcceptedRequests_DoesNotReplayPresentedRequestWithNewBridge()
    {
        AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
            AudioEventIds.UIButtonPrimaryClickHash,
            new FixedString32Bytes("UI"),
            AudioPlaybackPriority.Medium,
            requestedAt: 1f);
        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);

        AudioEventCatalogConfig catalog = CreateCatalog(CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            "UI",
            CreateClip("ui_primary_click")));
        AudioMixerBusConfig buses = CreateBuses(CreateBus("UI"));
        using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 2);

        AudioPlaybackPresentationBridgeSystemHelper firstBridge = new();
        AudioPlaybackPresentationBridgeResult first = firstBridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            buses,
            playback,
            now: 1.1f);

        AudioPlaybackPresentationBridgeSystemHelper recreatedBridge = new();
        AudioPlaybackPresentationBridgeResult second = recreatedBridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            buses,
            playback,
            now: 1.2f);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);

        Assert.AreEqual(1, first.PlayedCount);
        Assert.AreEqual(0, second.PresentedCount);
        Assert.AreEqual(1, playback.ActiveSourceCount);
        Assert.AreEqual(AudioPlaybackRequestStatus.Presented, requests[0].Status);
        Assert.AreEqual(2, results.Length);
    }

    [Test]
    public void DrainAcceptedRequests_SkipsPendingAndCooldownSkippedRequests()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        requests.Add(CreateRequest(1, AudioPlaybackRequestStatus.Pending));
        requests.Add(CreateRequest(2, AudioPlaybackRequestStatus.CooldownSkipped));

        AudioEventCatalogConfig catalog = CreateCatalog(CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            "UI",
            CreateClip("ui_primary_click")));
        using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioPlaybackPresentationBridgeSystemHelper bridge = new();

        AudioPlaybackPresentationBridgeResult result = bridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            CreateBuses(CreateBus("UI")),
            playback,
            now: 1f);

        Assert.AreEqual(0, result.PresentedCount);
        Assert.AreEqual(0, playback.ActiveSourceCount);
        Assert.AreEqual(0, bridge.LastPresentedRequestId);
    }

    [Test]
    public void DrainAcceptedRequests_RecordsMissingEventWithoutPlayback()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        requests.Add(CreateRequest(1, AudioPlaybackRequestStatus.Accepted));

        AudioPlaybackPresentationBridgeSystemHelper bridge = new();
        using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 1);

        AudioPlaybackPresentationBridgeResult result = bridge.DrainAcceptedRequests(
            _entityManager,
            CreateCatalog(),
            CreateBuses(CreateBus("UI")),
            playback,
            now: 1f);

        DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);

        Assert.AreEqual(1, result.PresentedCount);
        Assert.AreEqual(0, result.PlayedCount);
        Assert.AreEqual(1, result.FailedCount);
        Assert.AreEqual(0, playback.ActiveSourceCount);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AudioPlaybackRequestStatus.MissingEvent, results[0].Status);
        Assert.AreEqual("MissingCatalogEntry", results[0].Reason.ToString());
    }

    [Test]
    public void DrainAcceptedRequests_CullsGameplayVoiceWhileSimulationInactive()
    {
        try
        {
            RuntimeGameplayStateTestHelper.SetPlayRequested(_entityManager, true);
            RuntimeGameplayStateTestHelper.SetSimulationActive(_entityManager, false);
            AudioEventRequestSystem.EnqueueOneShot(
                _entityManager,
                new FixedString64Bytes(AudioEventIds.VOARIAMessageWarningGroundAttackType),
                AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash,
                new FixedString32Bytes("Voice"),
                AudioPlaybackPriority.Critical,
                requestedAt: 1f);
            AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);

            AudioEventCatalogConfig catalog = CreateCatalog(CreateEntry(
                AudioEventIds.VOARIAMessageWarningGroundAttackType,
                "Voice",
                CreateClip("aria_ground_attack")));
            AudioMixerBusConfig buses = CreateBuses(CreateBus("Voice"));
            using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 1);
            AudioPlaybackPresentationBridgeSystemHelper bridge = new();

            AudioPlaybackPresentationBridgeResult result = bridge.DrainAcceptedRequests(
                _entityManager,
                catalog,
                buses,
                playback,
                now: 1.1f);

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
            DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
            DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);
            AudioPlaybackResultElement lastResult = results[results.Length - 1];

            Assert.AreEqual(1, result.PresentedCount);
            Assert.AreEqual(0, result.PlayedCount);
            Assert.AreEqual(1, result.FailedCount);
            Assert.AreEqual(0, playback.ActiveSourceCount);
            Assert.AreEqual(AudioPlaybackRequestStatus.Culled, requests[0].Status);
            Assert.AreEqual(AudioPlaybackRequestStatus.Culled, lastResult.Status);
            Assert.AreEqual("GameplayInactive", lastResult.Reason.ToString());
        }
        finally
        {
            InitialUnitsRuntimeState.PlayRequested = false;
            InitialUnitsRuntimeState.SimulationActive = false;
        }
    }

    [Test]
    public void DrainAcceptedRequests_FadesSettingsChangesOnActiveMusicSource()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        AudioSettingsComponent settings = _entityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
        settings.MusicMuted = 0;
        settings.MusicVolume = 1f;
        settings.Version++;
        _entityManager.SetComponentData(audioEntity, settings);

        int requestId = AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            new FixedString64Bytes(AudioEventIds.MusicMenuLoop),
            AudioEventIds.MusicMenuLoopHash,
            new FixedString32Bytes("Music"),
            AudioPlaybackPriority.High,
            requestedAt: 1f);
        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);

        AudioEventCatalogConfig catalog = CreateCatalog(CreateEntry(
            AudioEventIds.MusicMenuLoop,
            "Music",
            CreateClip("music_menu_loop")));
        AudioMixerBusConfig buses = CreateBuses(CreateBus("Music"));
        using AudioPlaybackPresentationSystemHelper playback = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioPlaybackPresentationBridgeSystemHelper bridge = new();

        bridge.DrainAcceptedRequests(_entityManager, catalog, buses, playback, now: 1.1f);

        Assert.IsTrue(playback.TryGetActiveSource(requestId, out AudioSource source));
        Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));

        settings.MusicMuted = 1;
        settings.Version++;
        _entityManager.SetComponentData(audioEntity, settings);

        AudioPlaybackPresentationBridgeResult second = bridge.DrainAcceptedRequests(
            _entityManager,
            catalog,
            buses,
            playback,
            now: 1.2f);

        Assert.AreEqual(0, second.PresentedCount);
        Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));

        playback.UpdatePool(now: 1.375f);
        Assert.That(source.volume, Is.GreaterThan(0f).And.LessThan(1f));

        playback.UpdatePool(now: 1.55f);
        Assert.That(source.volume, Is.EqualTo(0f).Within(0.001f));
    }

    private AudioClip CreateClip(string name)
    {
        AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
        _clips.Add(clip);
        return clip;
    }

    private static AudioPlaybackRequestElement CreateRequest(int requestId, AudioPlaybackRequestStatus status)
    {
        return new AudioPlaybackRequestElement
        {
            RequestId = requestId,
            Kind = AudioPlaybackRequestKind.OneShot,
            Priority = AudioPlaybackPriority.Medium,
            Status = status,
            EventId = new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
            EventHash = AudioEventIds.UIButtonPrimaryClickHash,
            BusId = new FixedString32Bytes("UI"),
            PitchMultiplier = 1f
        };
    }

    private static AudioEventCatalogConfig CreateCatalog(params AudioEventCatalogEntry[] entries)
    {
        AudioEventCatalogConfig catalog = ScriptableObject.CreateInstance<AudioEventCatalogConfig>();
        SetPrivateField(catalog, "events", new List<AudioEventCatalogEntry>(entries));
        return catalog;
    }

    private static AudioMixerBusConfig CreateBuses(params AudioMixerBusEntry[] buses)
    {
        AudioMixerBusConfig config = ScriptableObject.CreateInstance<AudioMixerBusConfig>();
        SetPrivateField(config, "buses", new List<AudioMixerBusEntry>(buses));
        return config;
    }

    private static AudioEventCatalogEntry CreateEntry(string eventId, string busId, AudioClip clip)
    {
        AudioPlaybackConfig playback = new();
        SetPrivateField(playback, "maxInstances", 2);

        AudioClipWeightEntry clipEntry = new();
        SetPrivateField(clipEntry, "clip", clip);
        SetPrivateField(clipEntry, "weight", 1);

        AudioEventCatalogEntry entry = new();
        SetPrivateField(entry, "eventId", eventId);
        SetPrivateField(entry, "busId", busId);
        SetPrivateField(entry, "playback", playback);
        SetPrivateField(entry, "clips", new List<AudioClipWeightEntry> { clipEntry });
        return entry;
    }

    private static AudioMixerBusEntry CreateBus(string busId)
    {
        AudioMixerBusEntry bus = new();
        SetPrivateField(bus, "busId", busId);
        SetPrivateField(bus, "parentBusId", "Master");
        SetPrivateField(bus, "volumeSettingKey", busId);
        return bus;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{typeof(TTarget).Name}.{fieldName} must exist.");
        field.SetValue(target, value);
    }
}
