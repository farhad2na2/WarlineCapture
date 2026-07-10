using System;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AudioEcsDataContractTests
{
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AudioComponents_AreUnmanagedEcsData());
            passed++;
            RunCase(test => test.AudioBuffers_CanBeCreatedAndPopulatedWithoutManagedReferences());
            passed++;
            RunCase(test => test.AudioEnums_KeepExpectedStableValues());
            passed++;
            RunCase(test => test.AudioSettings_DefaultWritableValuesAreValid());
            passed++;

            Debug.Log($"[AudioEcsDataContractValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioEcsDataContractValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AudioEcsDataContractTests> testCase)
    {
        var tests = new AudioEcsDataContractTests();
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
        _world = new World("AudioEcsDataContractTests");
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AudioComponents_AreUnmanagedEcsData()
    {
        AssertComponent<AudioPlaybackRequestQueueComponent>();
        AssertComponent<AudioPlaybackResultQueueComponent>();
        AssertComponent<AudioSettingsComponent>();
        AssertComponent<AudioMusicStateComponent>();
        AssertComponent<AudioListenerStateComponent>();
    }

    [Test]
    public void AudioBuffers_CanBeCreatedAndPopulatedWithoutManagedReferences()
    {
        AssertBuffer<AudioPlaybackRequestElement>();
        AssertBuffer<AudioPlaybackResultElement>();
        AssertBuffer<AudioCooldownStateElement>();

        Entity audioEntity = _entityManager.CreateEntity(
            typeof(AudioPlaybackRequestQueueComponent),
            typeof(AudioSettingsComponent),
            typeof(AudioMusicStateComponent),
            typeof(AudioListenerStateComponent));

        _entityManager.SetComponentData(audioEntity, new AudioPlaybackRequestQueueComponent
        {
            LastRequestId = 7,
            Version = 2
        });

        _entityManager.SetComponentData(audioEntity, new AudioSettingsComponent
        {
            Version = 3,
            MasterVolume = 1f,
            UiVolume = 0.8f,
            SfxVolume = 0.9f,
            AlertsVolume = 1f,
            MusicVolume = 0.65f,
            AmbienceVolume = 0.5f,
            VoiceVolume = 0.9f
        });

        _entityManager.SetComponentData(audioEntity, new AudioMusicStateComponent
        {
            Version = 4,
            RequestedEventHash = AudioEventIds.MusicMatchCalmLoopHash,
            RequestedEventId = new FixedString64Bytes(AudioEventIds.MusicMatchCalmLoop),
            TransitionSeconds = 0.75f,
            Intensity = 0.2f,
            Loop = 1
        });

        _entityManager.SetComponentData(audioEntity, new AudioListenerStateComponent
        {
            Version = 5,
            Position = new float3(10f, 34f, -8f),
            Forward = new float3(0f, -0.5f, 1f),
            MaxAudibleDistance = 120f,
            HasListener = 1
        });

        _entityManager.AddBuffer<AudioPlaybackRequestElement>(audioEntity).Add(new AudioPlaybackRequestElement
        {
            RequestId = 8,
            Frame = 99,
            Kind = AudioPlaybackRequestKind.OneShot,
            Priority = AudioPlaybackPriority.High,
            EventHash = AudioEventIds.GameplayCommandMoveAcceptedHash,
            EventId = new FixedString64Bytes(AudioEventIds.GameplayCommandMoveAccepted),
            BusId = new FixedString32Bytes("SFX"),
            WorldPosition = new float3(12f, 0f, 18f),
            VolumeDecibels = -3f,
            PitchMultiplier = 1f,
            RequestedAt = 12.5f,
            HasWorldPosition = 1,
            Spatial = 1
        });

        _entityManager.AddBuffer<AudioPlaybackResultElement>(audioEntity).Add(new AudioPlaybackResultElement
        {
            RequestId = 8,
            Status = AudioPlaybackRequestStatus.Accepted,
            EventHash = AudioEventIds.GameplayCommandMoveAcceptedHash,
            EventId = new FixedString64Bytes(AudioEventIds.GameplayCommandMoveAccepted),
            Reason = new FixedString64Bytes("Accepted")
        });
        _entityManager.AddBuffer<AudioCooldownStateElement>(audioEntity).Add(new AudioCooldownStateElement
        {
            EventHash = AudioEventIds.GameplayCommandMoveAcceptedHash,
            LastAcceptedAt = 12.5f
        });

        Assert.AreEqual(1, _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity).Length);
        Assert.AreEqual(1, _entityManager.GetBuffer<AudioCooldownStateElement>(audioEntity).Length);
        Assert.AreEqual(AudioEventIds.GameplayCommandMoveAcceptedHash, _entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity)[0].EventHash);
    }

    [Test]
    public void AudioEnums_KeepExpectedStableValues()
    {
        Assert.AreEqual(0, (byte)AudioPlaybackPriority.Low);
        Assert.AreEqual(3, (byte)AudioPlaybackPriority.Critical);
        Assert.AreEqual(0, (byte)AudioPlaybackRequestKind.OneShot);
        Assert.AreEqual(1, (byte)AudioPlaybackRequestKind.MusicState);
        Assert.AreEqual(0, (byte)AudioPlaybackRequestStatus.Pending);
        Assert.AreEqual(3, (byte)AudioPlaybackRequestStatus.CooldownSkipped);
        Assert.AreEqual(7, (byte)AudioPlaybackRequestStatus.Presented);
    }

    [Test]
    public void AudioSettings_DefaultWritableValuesAreValid()
    {
        AudioSettingsComponent settings = new()
        {
            MasterVolume = 1f,
            UiVolume = 1f,
            SfxVolume = 1f,
            AlertsVolume = 1f,
            MusicVolume = 0.75f,
            AmbienceVolume = 0.75f,
            VoiceVolume = 1f
        };

        AssertVolume(settings.MasterVolume);
        AssertVolume(settings.UiVolume);
        AssertVolume(settings.SfxVolume);
        AssertVolume(settings.AlertsVolume);
        AssertVolume(settings.MusicVolume);
        AssertVolume(settings.AmbienceVolume);
        AssertVolume(settings.VoiceVolume);
    }

    private static void AssertVolume(float value)
    {
        Assert.GreaterOrEqual(value, 0f);
        Assert.LessOrEqual(value, 1f);
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
