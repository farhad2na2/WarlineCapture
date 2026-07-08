using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

public sealed class AudioPlaybackPresentationSystemHelperTests
{
    private readonly List<AudioClip> _clips = new();

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.Helper_PrewarmsPoolWithoutPlaying());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_ReusesPoolAfterStopAll());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_CullsWhenEventMaxInstancesReached());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_ReturnsMissingClipForEmptyCatalogEntry());
            passed++;
            RunCase(test => test.ResolveLinearVolume_AppliesMasterAndBusSettings());
            passed++;

            Debug.Log($"[AudioPlaybackPresentationHelperValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioPlaybackPresentationHelperValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AudioPlaybackPresentationSystemHelperTests> testCase)
    {
        var tests = new AudioPlaybackPresentationSystemHelperTests();
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
    public void Helper_PrewarmsPoolWithoutPlaying()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 2, maxPoolSize: 4);

        Assert.AreEqual(2, helper.PoolSize);
        Assert.AreEqual(4, helper.MaxPoolSize);
        Assert.AreEqual(2, helper.CreatedSourceCount);
        Assert.AreEqual(0, helper.ActiveSourceCount);
    }

    [Test]
    public void PlayAcceptedRequest_ReusesPoolAfterStopAll()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            busId: "UI",
            maxInstances: 1,
            clip: CreateClip("ui_click"));
        AudioPlaybackRequestElement request = CreateAcceptedRequest(1, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash);
        AudioSettingsComponent settings = CreateSettings();

        AudioPlaybackPresentationResult first = helper.PlayAcceptedRequest(request, entry, bus: null, settings: settings);
        helper.StopAll();
        AudioPlaybackPresentationResult second = helper.PlayAcceptedRequest(request, entry, bus: null, settings: settings);

        Assert.IsTrue(first.Played);
        Assert.IsTrue(second.Played);
        Assert.AreEqual(1, helper.CreatedSourceCount);
        Assert.AreEqual(first.SourceIndex, second.SourceIndex);
    }

    [Test]
    public void PlayAcceptedRequest_CullsWhenEventMaxInstancesReached()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 2, maxPoolSize: 2);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            busId: "UI",
            maxInstances: 1,
            clip: CreateClip("ui_click"));
        AudioSettingsComponent settings = CreateSettings();

        AudioPlaybackPresentationResult first = helper.PlayAcceptedRequest(
            CreateAcceptedRequest(1, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash),
            entry,
            bus: null,
            settings: settings);
        AudioPlaybackPresentationResult second = helper.PlayAcceptedRequest(
            CreateAcceptedRequest(2, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash),
            entry,
            bus: null,
            settings: settings);

        Assert.IsTrue(first.Played);
        Assert.IsFalse(second.Played);
        Assert.AreEqual(AudioPlaybackRequestStatus.Culled, second.Status);
        Assert.AreEqual("MaxInstances", second.Reason);
        Assert.AreEqual(1, helper.ActiveSourceCount);
    }

    [Test]
    public void PlayAcceptedRequest_ReturnsMissingClipForEmptyCatalogEntry()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            busId: "UI",
            maxInstances: 1,
            clip: null);

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(
            CreateAcceptedRequest(1, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash),
            entry,
            bus: null,
            settings: CreateSettings());

        Assert.IsFalse(result.Played);
        Assert.AreEqual(AudioPlaybackRequestStatus.MissingClip, result.Status);
        Assert.AreEqual(0, helper.ActiveSourceCount);
    }

    [Test]
    public void ResolveLinearVolume_AppliesMasterAndBusSettings()
    {
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.UIButtonPrimaryClick,
            busId: "UI",
            maxInstances: 1,
            clip: CreateClip("ui_click"),
            volumeDecibels: -6f);
        AudioPlaybackRequestElement request = CreateAcceptedRequest(1, AudioEventIds.UIButtonPrimaryClick, AudioEventIds.UIButtonPrimaryClickHash);
        request.VolumeDecibels = 0f;
        AudioSettingsComponent settings = CreateSettings();
        settings.MasterVolume = 0.5f;
        settings.UiVolume = 0.5f;

        float volume = AudioPlaybackPresentationSystemHelper.ResolveLinearVolume(request, entry, bus: null, settings);

        Assert.That(volume, Is.EqualTo(0.125297f).Within(0.001f));
    }

    private AudioClip CreateClip(string name)
    {
        AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
        _clips.Add(clip);
        return clip;
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
        AudioClip clip,
        float volumeDecibels = 0f)
    {
        AudioEventCatalogEntry entry = new();
        AudioPlaybackConfig playback = new();
        SetPrivateField(playback, "maxInstances", maxInstances);
        SetPrivateField(entry, "eventId", eventId);
        SetPrivateField(entry, "busId", busId);
        SetPrivateField(entry, "volumeDecibels", volumeDecibels);
        SetPrivateField(entry, "playback", playback);

        if (clip != null)
        {
            AudioClipWeightEntry weightEntry = new();
            SetPrivateField(weightEntry, "clip", clip);
            SetPrivateField(weightEntry, "weight", 1);
            SetPrivateField(entry, "clips", new List<AudioClipWeightEntry> { weightEntry });
        }

        return entry;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{typeof(TTarget).Name}.{fieldName} must exist.");
        field.SetValue(target, value);
    }
}
