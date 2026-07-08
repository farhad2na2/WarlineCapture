using System;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AudioSettingsUiProjectionTests
{
    private World _world;
    private World _previousDefaultWorld;
    private UISettingsModel _previousSettings;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.SettingsService_DefaultsIncludeAllAudioBuses());
            passed++;
            RunCase(test => test.SettingsService_PersistsAlertsAndVoiceVolumes());
            passed++;
            RunCase(test => test.UiAudioSettingsProjection_MapsPercentValuesToEcsAudioSettings());
            passed++;
            RunCase(test => test.SettingsServiceApplyRuntime_UpdatesDefaultWorldAudioSettings());
            passed++;

            Debug.Log($"[AudioSettingsUiProjectionValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AudioSettingsUiProjectionValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AudioSettingsUiProjectionTests> testCase)
    {
        var tests = new AudioSettingsUiProjectionTests();
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
        _previousSettings = SettingsService.Load();
        _previousDefaultWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("AudioSettingsUiProjectionTests");
        World.DefaultGameObjectInjectionWorld = _world;
    }

    [TearDown]
    public void TearDown()
    {
        SettingsService.Save(_previousSettings);
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousDefaultWorld;
        _world?.Dispose();
    }

    [Test]
    public void SettingsService_DefaultsIncludeAllAudioBuses()
    {
        UISettingsModel defaults = SettingsService.Defaults;

        Assert.AreEqual(80f, defaults.Audio.MasterVolume);
        Assert.AreEqual(60f, defaults.Audio.MusicVolume);
        Assert.AreEqual(85f, defaults.Audio.SfxVolume);
        Assert.AreEqual(90f, defaults.Audio.AlertsVolume);
        Assert.AreEqual(85f, defaults.Audio.VoiceVolume);
    }

    [Test]
    public void SettingsService_PersistsAlertsAndVoiceVolumes()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.AlertsVolume = 37f;
        model.Audio.VoiceVolume = 42f;

        SettingsService.Save(model);
        UISettingsModel loaded = SettingsService.Load();

        Assert.AreEqual(37f, loaded.Audio.AlertsVolume);
        Assert.AreEqual(42f, loaded.Audio.VoiceVolume);
    }

    [Test]
    public void UiAudioSettingsProjection_MapsPercentValuesToEcsAudioSettings()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MasterVolume = 25f;
        model.Audio.MusicVolume = 50f;
        model.Audio.SfxVolume = 75f;
        model.Audio.AlertsVolume = 100f;
        model.Audio.VoiceVolume = 10f;

        AudioSettingsComponent projected = UiAudioSettingsProjectionSystem.ToAudioSettingsComponent(model, version: 8u);

        Assert.AreEqual(8u, projected.Version);
        Assert.AreEqual(0.25f, projected.MasterVolume);
        Assert.AreEqual(0.5f, projected.MusicVolume);
        Assert.AreEqual(1, projected.MusicMuted);
        Assert.AreEqual(0.75f, projected.SfxVolume);
        Assert.AreEqual(0.75f, projected.UiVolume);
        Assert.AreEqual(1f, projected.AlertsVolume);
        Assert.AreEqual(0.1f, projected.VoiceVolume);
    }

    [Test]
    public void SettingsServiceApplyRuntime_UpdatesDefaultWorldAudioSettings()
    {
        _world.CreateSystem<UiAudioSettingsProjectionSystem>();

        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MasterVolume = 40f;
        model.Audio.MusicVolume = 20f;
        model.Audio.SfxVolume = 70f;
        model.Audio.AlertsVolume = 30f;
        model.Audio.VoiceVolume = 60f;

        SettingsService.ApplyRuntime(model);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioSettingsComponent settings = _world.EntityManager.GetComponentData<AudioSettingsComponent>(audioEntity);

        Assert.AreEqual(0.4f, settings.MasterVolume);
        Assert.AreEqual(0.2f, settings.MusicVolume);
        Assert.AreEqual(1, settings.MusicMuted);
        Assert.AreEqual(0.7f, settings.SfxVolume);
        Assert.AreEqual(0.3f, settings.AlertsVolume);
        Assert.AreEqual(0.6f, settings.VoiceVolume);
        Assert.AreEqual(0.4f, AudioListener.volume);
    }
}
