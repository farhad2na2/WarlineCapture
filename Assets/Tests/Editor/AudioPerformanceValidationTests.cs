using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AudioPerformanceValidationTests
{
    private readonly List<AudioClip> _clips = new();

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.UiSpamCooldownProcessing_KeepsCooldownStateBounded());
            passed++;
            RunCase(test => test.AlertBurstCooldownProcessing_KeepsCooldownStatePerEvent());
            passed++;
            RunCase(test => test.PlaybackPoolStress_ReusesPrewarmedSource());
            passed++;
            RunCase(test => test.GameplayAudioArchitecture_AvoidsRuntimeLoadsAndDirectPlayback());
            passed++;

            Debug.Log($"[AudioPerformanceValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioPerformanceValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AudioPerformanceValidationTests> testCase)
    {
        var tests = new AudioPerformanceValidationTests();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _clips.Count; i++)
        {
            if (_clips[i] != null)
                UnityEngine.Object.DestroyImmediate(_clips[i]);
        }

        _clips.Clear();
    }

    [Test]
    public void UiSpamCooldownProcessing_KeepsCooldownStateBounded()
    {
        using World world = new("AudioUiSpamPerformanceValidation");
        EntityManager em = world.EntityManager;
        const int SpamCount = 512;

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        em.GetBuffer<AudioPlaybackRequestElement>(audioEntity).EnsureCapacity(SpamCount);
        em.GetBuffer<AudioPlaybackResultElement>(audioEntity).EnsureCapacity(SpamCount);
        em.GetBuffer<AudioCooldownStateElement>(audioEntity).EnsureCapacity(1);

        for (int i = 0; i < SpamCount; i++)
        {
            AudioEventRequestSystem.EnqueueOneShot(
                em,
                new FixedString64Bytes(AudioEventIds.UIButtonDisabledTap),
                AudioEventIds.UIButtonDisabledTapHash,
                new FixedString32Bytes("UI"),
                AudioPlaybackPriority.Low,
                requestedAt: 1f + (i * 0.001f),
                cooldownSeconds: 0.25f);
        }

        AudioCooldownSystem.ProcessPendingRequests(em, now: 1.1f);

        DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        DynamicBuffer<AudioPlaybackResultElement> results = em.GetBuffer<AudioPlaybackResultElement>(audioEntity);
        DynamicBuffer<AudioCooldownStateElement> cooldowns = em.GetBuffer<AudioCooldownStateElement>(audioEntity);

        Assert.AreEqual(AudioEventRequestSystem.MaxTerminalRequestHistory + 1, requests.Length);
        Assert.AreEqual(AudioEventRequestSystem.MaxResultHistory, results.Length);
        Assert.AreEqual(1, cooldowns.Length);
        Assert.AreEqual(1, CountRequestsWithStatus(requests, AudioPlaybackRequestStatus.Accepted));
        Assert.AreEqual(
            AudioEventRequestSystem.MaxTerminalRequestHistory,
            CountRequestsWithStatus(requests, AudioPlaybackRequestStatus.CooldownSkipped));
        Assert.AreEqual(SpamCount - AudioEventRequestSystem.MaxResultHistory + 1, results[0].RequestId);
    }

    [Test]
    public void AlertBurstCooldownProcessing_KeepsCooldownStatePerEvent()
    {
        using World world = new("AudioAlertBurstPerformanceValidation");
        EntityManager em = world.EntityManager;
        const int BurstCount = 256;

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        em.GetBuffer<AudioPlaybackRequestElement>(audioEntity).EnsureCapacity(BurstCount);
        em.GetBuffer<AudioPlaybackResultElement>(audioEntity).EnsureCapacity(BurstCount);
        em.GetBuffer<AudioCooldownStateElement>(audioEntity).EnsureCapacity(2);

        for (int i = 0; i < BurstCount; i++)
        {
            bool critical = (i & 1) == 0;
            AudioEventRequestSystem.EnqueueOneShot(
                em,
                new FixedString64Bytes(critical ? AudioEventIds.AlertThreatCritical : AudioEventIds.AlertUnitUnderAttack),
                critical ? AudioEventIds.AlertThreatCriticalHash : AudioEventIds.AlertUnitUnderAttackHash,
                new FixedString32Bytes("Alerts"),
                critical ? AudioPlaybackPriority.Critical : AudioPlaybackPriority.High,
                requestedAt: 2f + (i * 0.001f),
                cooldownSeconds: critical ? 4f : 2.5f);
        }

        AudioCooldownSystem.ProcessPendingRequests(em, now: 2.25f);

        DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        DynamicBuffer<AudioCooldownStateElement> cooldowns = em.GetBuffer<AudioCooldownStateElement>(audioEntity);

        Assert.AreEqual(BurstCount, requests.Length);
        Assert.AreEqual(2, cooldowns.Length);
        Assert.AreEqual(2, CountRequestsWithStatus(requests, AudioPlaybackRequestStatus.Accepted));
        Assert.AreEqual(BurstCount - 2, CountRequestsWithStatus(requests, AudioPlaybackRequestStatus.CooldownSkipped));
    }

    [Test]
    public void PlaybackPoolStress_ReusesPrewarmedSource()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            busId: "UI",
            maxInstances: 1,
            clip: CreateClip("audio_perf_ui_click"));
        AudioSettingsComponent settings = CreateSettings();

        for (int i = 0; i < 128; i++)
        {
            AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(
                CreateAcceptedRequest(i + 1, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash),
                entry,
                bus: null,
                settings: settings);
            Assert.IsTrue(result.Played, $"Playback iteration {i} should use the prewarmed source.");
            Assert.AreEqual(0, result.SourceIndex);
            helper.StopAll();
        }

        Assert.AreEqual(1, helper.PoolSize);
        Assert.AreEqual(1, helper.CreatedSourceCount);
        Assert.AreEqual(0, helper.ActiveSourceCount);
    }

    [Test]
    public void GameplayAudioArchitecture_AvoidsRuntimeLoadsAndDirectPlayback()
    {
        string[] roots =
        {
            "Assets/Game/Scripts/Systems",
            "Assets/Game/Scripts/UI/Shell/Ecs",
            "Assets/Game/Scripts/UI/Components"
        };
        string[] forbiddenTokens =
        {
            "Resources.Load",
            "AudioSource",
            "PlayOneShot",
            ".Play()"
        };

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            string root = roots[rootIndex];
            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);
                for (int tokenIndex = 0; tokenIndex < forbiddenTokens.Length; tokenIndex++)
                {
                    string token = forbiddenTokens[tokenIndex];
                    StringAssert.DoesNotContain(
                        token,
                        source,
                        $"{path} must publish semantic audio requests instead of direct runtime loading/playback.");
                }
            }
        }
    }

    private AudioClip CreateClip(string name)
    {
        AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
        _clips.Add(clip);
        return clip;
    }

    private static int CountRequestsWithStatus(
        DynamicBuffer<AudioPlaybackRequestElement> requests,
        AudioPlaybackRequestStatus status)
    {
        int count = 0;
        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].Status == status)
                count++;
        }

        return count;
    }

    private static AudioPlaybackRequestElement CreateAcceptedRequest(int requestId, string eventId, uint eventHash)
    {
        return new AudioPlaybackRequestElement
        {
            RequestId = requestId,
            Kind = AudioPlaybackRequestKind.OneShot,
            Priority = AudioPlaybackPriority.Medium,
            Status = AudioPlaybackRequestStatus.Accepted,
            EventId = new FixedString64Bytes(eventId),
            EventHash = eventHash,
            BusId = new FixedString32Bytes("UI"),
            PitchMultiplier = 1f
        };
    }

    private static AudioSettingsComponent CreateSettings()
    {
        return new AudioSettingsComponent
        {
            MasterVolume = 1f,
            UiVolume = 1f,
            SfxVolume = 1f,
            AlertsVolume = 1f,
            MusicVolume = 1f,
            AmbienceVolume = 1f,
            VoiceVolume = 1f
        };
    }

    private static AudioEventCatalogEntry CreateEntry(
        string eventId,
        string busId,
        int maxInstances,
        AudioClip clip)
    {
        AudioEventCatalogEntry entry = new();
        AudioPlaybackConfig playback = new();
        SetPrivateField(playback, "maxInstances", maxInstances);
        SetPrivateField(entry, "eventId", eventId);
        SetPrivateField(entry, "busId", busId);
        SetPrivateField(entry, "playback", playback);

        AudioClipWeightEntry weightEntry = new();
        SetPrivateField(weightEntry, "clip", clip);
        SetPrivateField(weightEntry, "weight", 1);
        SetPrivateField(entry, "clips", new List<AudioClipWeightEntry> { weightEntry });
        return entry;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{typeof(TTarget).Name}.{fieldName} must exist.");
        field.SetValue(target, value);
    }
}
