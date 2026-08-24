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
            RunCase(test => test.PlayAcceptedRequest_InterruptibleVoiceReplacesOlderEqualPriorityVoice());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_InterruptibleVoiceCannotReplaceHigherPriorityVoice());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_ReturnsMissingClipForEmptyCatalogEntry());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_ConfiguresMissileSpatialSfxForRtsScaleAudibility());
            passed++;
            RunCase(test => test.ResolveLinearVolume_AppliesMasterAndBusSettings());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_ConfiguresSpatialSfxForCameraLocalAudibility());
            passed++;
            RunCase(test => test.ApplySettingsToActiveSources_UpdatesMusicLoopVolume());
            passed++;
            RunCase(test => test.ApplySettingsToActiveSources_FadesMusicLoopVolumeWhenRequested());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_CrossfadesMusicStatesAndReleasesOutgoingLoop());
            passed++;
            RunCase(test => test.PlayAcceptedRequest_SelectsRequestedLocalizedClip());
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
        Assert.AreEqual(AudioPlaybackRequestStatus.Presented, first.Status);
        Assert.AreEqual(AudioPlaybackRequestStatus.Presented, second.Status);
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
    public void PlayAcceptedRequest_InterruptibleVoiceReplacesOlderEqualPriorityVoice()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 2, maxPoolSize: 2);
        AudioPlaybackRequestElement firstRequest = CreateAcceptedRequest(1, "voice.first", 101u);
        firstRequest.BusId = new FixedString32Bytes("Voice");
        firstRequest.Priority = AudioPlaybackPriority.High;
        firstRequest.InterruptsLowerPriority = 1;
        AudioPlaybackRequestElement secondRequest = CreateAcceptedRequest(2, "voice.second", 102u);
        secondRequest.BusId = new FixedString32Bytes("Voice");
        secondRequest.Priority = AudioPlaybackPriority.High;
        secondRequest.InterruptsLowerPriority = 1;

        Assert.IsTrue(helper.PlayAcceptedRequest(
            firstRequest,
            CreateEntry("voice.first", "Voice", 1, CreateClip("voice_first")),
            null,
            CreateSettings()).Played);
        Assert.IsTrue(helper.PlayAcceptedRequest(
            secondRequest,
            CreateEntry("voice.second", "Voice", 1, CreateClip("voice_second")),
            null,
            CreateSettings()).Played);

        Assert.AreEqual(1, helper.ActiveSourceCount);
        Assert.IsFalse(helper.TryGetActiveSource(firstRequest.RequestId, out _));
        Assert.IsTrue(helper.TryGetActiveSource(secondRequest.RequestId, out _));
    }

    [Test]
    public void PlayAcceptedRequest_InterruptibleVoiceCannotReplaceHigherPriorityVoice()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 2, maxPoolSize: 2);
        AudioPlaybackRequestElement criticalRequest = CreateAcceptedRequest(1, "voice.critical", 201u);
        criticalRequest.BusId = new FixedString32Bytes("Voice");
        criticalRequest.Priority = AudioPlaybackPriority.Critical;
        criticalRequest.InterruptsLowerPriority = 1;
        AudioPlaybackRequestElement highRequest = CreateAcceptedRequest(2, "voice.high", 202u);
        highRequest.BusId = new FixedString32Bytes("Voice");
        highRequest.Priority = AudioPlaybackPriority.High;
        highRequest.InterruptsLowerPriority = 1;

        Assert.IsTrue(helper.PlayAcceptedRequest(
            criticalRequest,
            CreateEntry("voice.critical", "Voice", 1, CreateClip("voice_critical")),
            null,
            CreateSettings()).Played);
        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(
            highRequest,
            CreateEntry("voice.high", "Voice", 1, CreateClip("voice_high")),
            null,
            CreateSettings());

        Assert.IsFalse(result.Played);
        Assert.AreEqual(AudioPlaybackRequestStatus.Culled, result.Status);
        Assert.AreEqual("HigherPriorityBusOwner", result.Reason);
        Assert.AreEqual(1, helper.ActiveSourceCount);
        Assert.IsTrue(helper.TryGetActiveSource(criticalRequest.RequestId, out _));
        Assert.IsFalse(helper.TryGetActiveSource(highRequest.RequestId, out _));
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
    public void PlayAcceptedRequest_ConfiguresMissileSpatialSfxForRtsScaleAudibility()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.GameplayUnitEngineAircraftFlight,
            busId: "SFX",
            maxInstances: 1,
            clip: CreateClip("aircraft_engine"),
            spatial: true);
        AudioPlaybackRequestElement request = CreateAcceptedRequest(
            7,
            AudioEventIds.GameplayUnitEngineAircraftFlight,
            AudioEventIds.GameplayUnitEngineAircraftFlightHash);
        request.BusId = new FixedString32Bytes("SFX");
        request.Spatial = 1;
        request.HasWorldPosition = 1;
        request.WorldPosition = new Unity.Mathematics.float3(300f, 40f, 250f);

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(request, entry, bus: null, settings: CreateSettings());

        Assert.IsTrue(result.Played);
        Assert.IsTrue(helper.TryGetActiveSource(request.RequestId, out AudioSource source));
        Assert.AreEqual(1f, source.spatialBlend);
        Assert.AreEqual(AudioRolloffMode.Linear, source.rolloffMode);
        Assert.AreEqual(AudioPlaybackPresentationSystemHelper.SpatialSfxMinDistance, source.minDistance);
        Assert.AreEqual(AudioPlaybackPresentationSystemHelper.SpatialSfxMaxDistance, source.maxDistance);
        Assert.LessOrEqual(
            AudioPlaybackPresentationSystemHelper.SpatialSfxMaxDistance,
            256f,
            "Combat SFX must stay camera-local on the 2048-unit match map instead of remaining audible across the map.");
        Assert.Greater(
            AudioPlaybackPresentationSystemHelper.SpatialSfxMaxDistance,
            AudioPlaybackPresentationSystemHelper.SpatialSfxMinDistance);
        Assert.AreEqual(0f, source.dopplerLevel);
        Assert.AreEqual(0f, source.spread);
        Assert.AreEqual(300f, source.transform.position.x, 0.001f);
        Assert.AreEqual(40f, source.transform.position.y, 0.001f);
        Assert.AreEqual(250f, source.transform.position.z, 0.001f);
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

    [Test]
    public void PlayAcceptedRequest_ConfiguresSpatialSfxForCameraLocalAudibility()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.GameplayWeaponMissileFlight,
            busId: "SFX",
            maxInstances: 1,
            clip: CreateClip("missile_flight"),
            spatial: true);
        AudioPlaybackRequestElement request = CreateAcceptedRequest(
            7,
            AudioEventIds.GameplayWeaponMissileFlight,
            AudioEventIds.GameplayWeaponMissileFlightHash);
        request.BusId = new FixedString32Bytes("SFX");
        request.Spatial = 1;
        request.HasWorldPosition = 1;
        request.WorldPosition = new Unity.Mathematics.float3(1200f, 35f, -900f);

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(request, entry, bus: null, settings: CreateSettings());

        Assert.IsTrue(result.Played);
        Assert.IsTrue(helper.TryGetActiveSource(request.RequestId, out AudioSource source));
        Assert.AreEqual(1f, source.spatialBlend);
        Assert.AreEqual(AudioRolloffMode.Linear, source.rolloffMode);
        Assert.That(source.minDistance, Is.EqualTo(AudioPlaybackPresentationSystemHelper.SpatialSfxMinDistance).Within(0.001f));
        Assert.That(source.maxDistance, Is.EqualTo(AudioPlaybackPresentationSystemHelper.SpatialSfxMaxDistance).Within(0.001f));
        Assert.AreEqual(0f, source.dopplerLevel);
        Assert.That(source.transform.position.x, Is.EqualTo(1200f).Within(0.001f));
        Assert.That(source.transform.position.y, Is.EqualTo(35f).Within(0.001f));
        Assert.That(source.transform.position.z, Is.EqualTo(-900f).Within(0.001f));
    }

    [Test]
    public void ApplySettingsToActiveSources_UpdatesMusicLoopVolume()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.MusicMenuLoop,
            busId: "Music",
            maxInstances: 1,
            clip: CreateClip("music_menu_loop"));
        AudioPlaybackRequestElement request = CreateAcceptedRequest(1, AudioEventIds.MusicMenuLoop, AudioEventIds.MusicMenuLoopHash);
        AudioSettingsComponent settings = CreateSettings();
        settings.MusicVolume = 0.75f;

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(request, entry, bus: null, settings);

        Assert.IsTrue(result.Played);
        Assert.IsTrue(helper.TryGetActiveSource(request.RequestId, out AudioSource source));
        Assert.That(source.volume, Is.EqualTo(0.75f).Within(0.001f));

        settings.MusicMuted = 1;
        helper.ApplySettingsToActiveSources(settings);
        Assert.That(source.volume, Is.EqualTo(0f).Within(0.001f));

        settings.MusicMuted = 0;
        settings.MusicVolume = 0.5f;
        helper.ApplySettingsToActiveSources(settings);
        Assert.That(source.volume, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void ApplySettingsToActiveSources_FadesMusicLoopVolumeWhenRequested()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.MusicMenuLoop,
            busId: "Music",
            maxInstances: 1,
            clip: CreateClip("music_menu_loop"));
        AudioPlaybackRequestElement request = CreateAcceptedRequest(1, AudioEventIds.MusicMenuLoop, AudioEventIds.MusicMenuLoopHash);
        AudioSettingsComponent settings = CreateSettings();

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(request, entry, bus: null, settings);

        Assert.IsTrue(result.Played);
        Assert.IsTrue(helper.TryGetActiveSource(request.RequestId, out AudioSource source));
        Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));

        settings.MusicMuted = 1;
        helper.ApplySettingsToActiveSources(settings, now: 10f, fadeSeconds: 0.4f);
        Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));

        helper.UpdatePool(now: 10.2f);
        Assert.That(source.volume, Is.EqualTo(0.5f).Within(0.05f));

        helper.UpdatePool(now: 10.4f);
        Assert.That(source.volume, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void PlayAcceptedRequest_CrossfadesMusicStatesAndReleasesOutgoingLoop()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 2, maxPoolSize: 2);
        AudioSettingsComponent settings = CreateSettings();
        AudioEventCatalogEntry menuEntry = CreateEntry(
            AudioEventIds.MusicMenuLoop,
            busId: "Music",
            maxInstances: 1,
            clip: CreateClip("music_menu_loop"));
        AudioPlaybackRequestElement menuRequest = CreateAcceptedRequest(
            1,
            AudioEventIds.MusicMenuLoop,
            AudioEventIds.MusicMenuLoopHash);
        menuRequest.Kind = AudioPlaybackRequestKind.MusicState;

        Assert.IsTrue(helper.PlayAcceptedRequest(menuRequest, menuEntry, null, settings).Played);
        Assert.IsTrue(helper.TryGetActiveSource(menuRequest.RequestId, out AudioSource menuSource));
        Assert.That(menuSource.volume, Is.EqualTo(1f).Within(0.001f));

        AudioEventCatalogEntry matchEntry = CreateEntry(
            AudioEventIds.MusicMatchCalmLoop,
            busId: "Music",
            maxInstances: 1,
            clip: CreateClip("music_match_calm_loop"));
        AudioPlaybackRequestElement matchRequest = CreateAcceptedRequest(
            2,
            AudioEventIds.MusicMatchCalmLoop,
            AudioEventIds.MusicMatchCalmLoopHash);
        matchRequest.Kind = AudioPlaybackRequestKind.MusicState;

        AudioPlaybackPresentationResult transition = helper.PlayAcceptedRequest(
            matchRequest,
            matchEntry,
            null,
            settings,
            now: 10f,
            musicTransitionSeconds: 2f);

        Assert.IsTrue(transition.Played);
        Assert.IsTrue(helper.TryGetActiveSource(matchRequest.RequestId, out AudioSource matchSource));
        Assert.That(matchSource.volume, Is.EqualTo(0f).Within(0.001f));

        helper.UpdatePool(now: 11f);
        Assert.That(menuSource.volume, Is.EqualTo(0.5f).Within(0.05f));
        Assert.That(matchSource.volume, Is.EqualTo(0.5f).Within(0.05f));

        helper.UpdatePool(now: 12f);
        Assert.IsFalse(helper.TryGetActiveSource(menuRequest.RequestId, out _));
        Assert.IsTrue(helper.TryGetActiveSource(matchRequest.RequestId, out matchSource));
        Assert.That(matchSource.volume, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void PlayAcceptedRequest_SelectsRequestedLocalizedClip()
    {
        using AudioPlaybackPresentationSystemHelper helper = new(initialPoolSize: 1, maxPoolSize: 1);
        AudioClip english = CreateClip("aria_english");
        AudioClip persian = CreateClip("aria_persian");
        AudioEventCatalogEntry entry = CreateEntry(
            AudioEventIds.VOARIAMessageWarningGroundAttackType,
            busId: "Voice",
            maxInstances: 1,
            clip: english);
        AddLocalizedClip(entry, "fa-IR", persian);
        AudioPlaybackRequestElement request = CreateAcceptedRequest(
            77,
            AudioEventIds.VOARIAMessageWarningGroundAttackType,
            AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash);
        request.BusId = new FixedString32Bytes("Voice");

        AudioPlaybackPresentationResult result = helper.PlayAcceptedRequest(
            request,
            entry,
            bus: null,
            settings: CreateSettings(),
            localeCode: "fa-IR");

        Assert.IsTrue(result.Played);
        Assert.IsTrue(helper.TryGetActiveSource(request.RequestId, out AudioSource source));
        Assert.AreSame(persian, source.clip);
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
        float volumeDecibels = 0f,
        bool spatial = false)
    {
        AudioEventCatalogEntry entry = new();
        AudioPlaybackConfig playback = new();
        SetPrivateField(playback, "spatial", spatial);
        SetPrivateField(playback, "maxInstances", maxInstances);
        SetPrivateField(playback, "spatial", spatial);
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

    private static void AddLocalizedClip(AudioEventCatalogEntry entry, string localeCode, AudioClip clip)
    {
        AudioClipWeightEntry weightEntry = new();
        SetPrivateField(weightEntry, "clip", clip);
        SetPrivateField(weightEntry, "weight", 1);
        LocalizedAudioClipSet clipSet = new();
        SetPrivateField(clipSet, "localeCode", localeCode);
        SetPrivateField(clipSet, "clips", new List<AudioClipWeightEntry> { weightEntry });
        SetPrivateField(entry, "localizedClips", new List<LocalizedAudioClipSet> { clipSet });
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{typeof(TTarget).Name}.{fieldName} must exist.");
        field.SetValue(target, value);
    }
}
