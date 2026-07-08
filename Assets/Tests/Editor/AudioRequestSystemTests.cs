using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AudioRequestSystemTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.EnsureAudioEntity_CreatesRequestStateAndBuffers());
            passed++;
            RunCase(test => test.ProcessPendingRequests_AcceptsFirstRequestAndSkipsCooldownDuplicate());
            passed++;
            RunCase(test => test.ProcessPendingRequests_MarksMissingEvent());
            passed++;
            RunCase(test => test.MusicStateSystem_AppliesRequestedState());
            passed++;
            RunCase(test => test.SettingsSystem_ClampsVolumesAndBumpsVersion());
            passed++;

            Debug.Log($"[AudioRequestSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioRequestSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AudioRequestSystemTests> testCase)
    {
        var tests = new AudioRequestSystemTests();
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
        _world = new World("AudioRequestSystemTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void EnsureAudioEntity_CreatesRequestStateAndBuffers()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);

        Assert.IsTrue(_entityManager.HasComponent<AudioPlaybackRequestQueueComponent>(audioEntity));
        Assert.IsTrue(_entityManager.HasComponent<AudioSettingsComponent>(audioEntity));
        Assert.IsTrue(_entityManager.HasComponent<AudioMusicStateComponent>(audioEntity));
        Assert.IsTrue(_entityManager.HasComponent<AudioListenerStateComponent>(audioEntity));
        Assert.IsTrue(_entityManager.HasBuffer<AudioPlaybackRequestElement>(audioEntity));
        Assert.IsTrue(_entityManager.HasBuffer<AudioPlaybackResultElement>(audioEntity));
        Assert.IsTrue(_entityManager.HasBuffer<AudioCooldownStateElement>(audioEntity));

        AudioSettingsComponent settings = _entityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
        Assert.AreEqual(1f, settings.MasterVolume);
        Assert.AreEqual(0.75f, settings.MusicVolume);
    }

    [Test]
    public void ProcessPendingRequests_AcceptsFirstRequestAndSkipsCooldownDuplicate()
    {
        AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
            AudioEventIds.UIButtonPrimaryClickHash,
            new FixedString32Bytes("UI"),
            AudioPlaybackPriority.Medium,
            requestedAt: 1f,
            cooldownSeconds: 1f);
        AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            new FixedString64Bytes(AudioEventIds.UIButtonPrimaryClick),
            AudioEventIds.UIButtonPrimaryClickHash,
            new FixedString32Bytes("UI"),
            AudioPlaybackPriority.Medium,
            requestedAt: 1.1f,
            cooldownSeconds: 1f);

        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1.1f);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests = _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);
        DynamicBuffer<AudioCooldownStateElement> cooldowns = _entityManager.GetBuffer<AudioCooldownStateElement>(audioEntity);

        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, requests[0].Status);
        Assert.AreEqual(AudioPlaybackRequestStatus.CooldownSkipped, requests[1].Status);
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(AudioPlaybackRequestStatus.Accepted, results[0].Status);
        Assert.AreEqual(AudioPlaybackRequestStatus.CooldownSkipped, results[1].Status);
        Assert.AreEqual(1, cooldowns.Length);
        Assert.AreEqual(AudioEventIds.UIButtonPrimaryClickHash, cooldowns[0].EventHash);
    }

    [Test]
    public void ProcessPendingRequests_MarksMissingEvent()
    {
        AudioEventRequestSystem.EnqueueOneShot(
            _entityManager,
            default,
            0u,
            new FixedString32Bytes("UI"),
            AudioPlaybackPriority.Medium,
            requestedAt: 1f);

        AudioCooldownSystem.ProcessPendingRequests(_entityManager, now: 1f);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        DynamicBuffer<AudioPlaybackResultElement> results = _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(AudioPlaybackRequestStatus.MissingEvent, results[0].Status);
        Assert.AreEqual("MissingEvent", results[0].Reason.ToString());
    }

    [Test]
    public void MusicStateSystem_AppliesRequestedState()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_entityManager);
        AudioMusicStateComponent musicState = _entityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
        musicState.RequestedEventHash = AudioEventIds.MusicMatchCalmLoopHash;
        musicState.RequestedEventId = new FixedString64Bytes(AudioEventIds.MusicMatchCalmLoop);
        musicState.IsTransitioning = 1;

        bool changed = AudioMusicStateSystem.ApplyRequestedMusicState(ref musicState);

        Assert.IsTrue(changed);
        Assert.AreEqual(AudioEventIds.MusicMatchCalmLoopHash, musicState.CurrentEventHash);
        Assert.AreEqual(AudioEventIds.MusicMatchCalmLoop, musicState.CurrentEventId.ToString());
        Assert.AreEqual(0u, musicState.RequestedEventHash);
        Assert.AreEqual(0, musicState.IsTransitioning);
    }

    [Test]
    public void SettingsSystem_ClampsVolumesAndBumpsVersion()
    {
        AudioSettingsComponent settings = new()
        {
            Version = 2,
            MasterVolume = 1.2f,
            UiVolume = -0.1f,
            SfxVolume = 0.5f,
            AlertsVolume = 0.25f,
            MusicVolume = 4f,
            AmbienceVolume = 0.75f,
            VoiceVolume = -2f
        };

        bool changed = AudioSettingsSystem.NormalizeSettings(ref settings);

        Assert.IsTrue(changed);
        Assert.AreEqual(3u, settings.Version);
        Assert.AreEqual(1f, settings.MasterVolume);
        Assert.AreEqual(0f, settings.UiVolume);
        Assert.AreEqual(1f, settings.MusicVolume);
        Assert.AreEqual(0f, settings.VoiceVolume);
    }
}
