using System;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AudioSettingsUiProjectionTests
{
    private World _world;
    private World _previousDefaultWorld;
    private UISettingsModel _previousSettings;
    private int _previousTargetFrameRate;
    private int _previousVSyncCount;
    private int _previousQualityLevel;
    private float _previousListenerVolume;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.SettingsService_DefaultsIncludeAllAudioBuses());
            passed++;
            RunCase(test => test.SettingsService_PersistsAudioBusVolumesAndToggles());
            passed++;
            RunCase(test => test.SettingsService_ResetRestoresEnabledAudioBuses());
            passed++;
            RunCase(test => test.UiAudioSettingsProjection_MapsPercentValuesToEcsAudioSettings());
            passed++;
            RunCase(test => test.UiAudioSettingsProjection_MapsAudioTogglesToMuteFlags());
            passed++;
            RunCase(test => test.SettingsServiceApplyRuntime_UpdatesDefaultWorldAudioSettings());
            passed++;
            RunCase(test => test.MenuStartup_AppliesPersistedSettingsWithoutOpeningPopup());
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
        _previousTargetFrameRate = Application.targetFrameRate;
        _previousVSyncCount = QualitySettings.vSyncCount;
        _previousQualityLevel = QualitySettings.GetQualityLevel();
        _previousListenerVolume = AudioListener.volume;
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
        Application.targetFrameRate = _previousTargetFrameRate;
        QualitySettings.vSyncCount = _previousVSyncCount;
        if (QualitySettings.names.Length > 0)
            QualitySettings.SetQualityLevel(_previousQualityLevel, true);
        AudioListener.volume = _previousListenerVolume;
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
        Assert.IsTrue(defaults.Audio.MusicEnabled);
        Assert.IsTrue(defaults.Audio.SoundEnabled);
        Assert.IsTrue(defaults.Audio.VoiceEnabled);
    }

    [Test]
    public void SettingsService_PersistsAudioBusVolumesAndToggles()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.AlertsVolume = 37f;
        model.Audio.VoiceVolume = 42f;
        model.Audio.MusicEnabled = false;
        model.Audio.SoundEnabled = false;
        model.Audio.VoiceEnabled = false;

        SettingsService.Save(model);
        UISettingsModel loaded = SettingsService.Load();

        Assert.AreEqual(37f, loaded.Audio.AlertsVolume);
        Assert.AreEqual(42f, loaded.Audio.VoiceVolume);
        Assert.IsFalse(loaded.Audio.MusicEnabled);
        Assert.IsFalse(loaded.Audio.SoundEnabled);
        Assert.IsFalse(loaded.Audio.VoiceEnabled);
    }

    [Test]
    public void SettingsService_ResetRestoresEnabledAudioBuses()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MusicEnabled = false;
        model.Audio.SoundEnabled = false;
        model.Audio.VoiceEnabled = false;
        SettingsService.Save(model);

        UISettingsModel reset = SettingsService.ResetToDefaults();
        UISettingsModel loaded = SettingsService.Load();

        Assert.IsTrue(reset.Audio.MusicEnabled);
        Assert.IsTrue(reset.Audio.SoundEnabled);
        Assert.IsTrue(reset.Audio.VoiceEnabled);
        Assert.IsTrue(loaded.Audio.MusicEnabled);
        Assert.IsTrue(loaded.Audio.SoundEnabled);
        Assert.IsTrue(loaded.Audio.VoiceEnabled);
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
        Assert.AreEqual(0, projected.MusicMuted);
        Assert.AreEqual(0.75f, projected.SfxVolume);
        Assert.AreEqual(0.75f, projected.UiVolume);
        Assert.AreEqual(1f, projected.AlertsVolume);
        Assert.AreEqual(0.1f, projected.VoiceVolume);
    }

    [Test]
    public void UiAudioSettingsProjection_MapsAudioTogglesToMuteFlags()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MusicEnabled = false;
        model.Audio.SoundEnabled = false;
        model.Audio.VoiceEnabled = false;

        AudioSettingsComponent projected = UiAudioSettingsProjectionSystem.ToAudioSettingsComponent(model, version: 3u);

        Assert.AreEqual(0, projected.MasterMuted);
        Assert.AreEqual(1, projected.MusicMuted);
        Assert.AreEqual(1, projected.SfxMuted);
        Assert.AreEqual(1, projected.UiMuted);
        Assert.AreEqual(1, projected.AlertsMuted);
        Assert.AreEqual(1, projected.AmbienceMuted);
        Assert.AreEqual(1, projected.VoiceMuted);
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
        model.Audio.MusicEnabled = false;
        model.Audio.SoundEnabled = false;
        model.Audio.VoiceEnabled = false;

        SettingsService.ApplyRuntime(model);

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioSettingsComponent settings = _world.EntityManager.GetComponentData<AudioSettingsComponent>(audioEntity);

        Assert.AreEqual(0.4f, settings.MasterVolume);
        Assert.AreEqual(0.2f, settings.MusicVolume);
        Assert.AreEqual(1, settings.MusicMuted);
        Assert.AreEqual(0.7f, settings.SfxVolume);
        Assert.AreEqual(1, settings.SfxMuted);
        Assert.AreEqual(1, settings.UiMuted);
        Assert.AreEqual(0.3f, settings.AlertsVolume);
        Assert.AreEqual(1, settings.AlertsMuted);
        Assert.AreEqual(0.6f, settings.VoiceVolume);
        Assert.AreEqual(1, settings.VoiceMuted);
        Assert.AreEqual(0.4f, AudioListener.volume);
    }

    [Test]
    public void MenuStartup_AppliesPersistedSettingsWithoutOpeningPopup()
    {
        _world.CreateSystem<UiShellStateSystem>();
        _world.CreateSystem<UiAudioSettingsProjectionSystem>();
        _world.CreateSystem<AssistantSettingsPersistenceSystem>();

        using EntityQuery shellQuery =
            _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
        Entity boundary = shellQuery.GetSingletonEntity();

        UISettingsModel persisted = SettingsService.Defaults;
        persisted.Audio.MasterVolume = 31f;
        persisted.Audio.MusicVolume = 17f;
        persisted.Audio.MusicEnabled = false;
        persisted.Graphics.Quality = UIGraphicsQuality.Low;
        persisted.Graphics.FrameRateMode = UIFrameRateMode.Sixty;
        persisted.Accessibility.HighContrastUi = true;
        persisted.Assistant.AssistanceLevel = UIAssistanceLevel.Off;
        persisted.Assistant.NarrationMode = UIAssistantNarrationMode.Off;
        persisted.Assistant.AllowTakeover = false;
        SettingsService.Save(persisted);

        int runtimeApplyCount = 0;
        UISettingsModel applied = default;
        void CaptureApplied(UISettingsModel model)
        {
            runtimeApplyCount++;
            applied = model;
        }

        SettingsService.RuntimeApplied += CaptureApplied;
        GameObject bootstrapObject = new("SettingsStartupBootstrap");
        MenuBootstrapView bootstrap = bootstrapObject.AddComponent<MenuBootstrapView>();
        try
        {
            InvokeLifecycle(bootstrap, "Awake");

            Assert.AreEqual(1, runtimeApplyCount, "Menu startup should apply persisted settings once during Awake.");
            Assert.AreEqual(31f, applied.Audio.MasterVolume);
            Assert.AreEqual(17f, applied.Audio.MusicVolume);
            Assert.IsFalse(applied.Audio.MusicEnabled);
            Assert.AreEqual(UIGraphicsQuality.Low, applied.Graphics.Quality);
            Assert.AreEqual(UIFrameRateMode.Sixty, applied.Graphics.FrameRateMode);
            Assert.AreEqual(UIAssistanceLevel.Off, applied.Assistant.AssistanceLevel);

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
            AudioSettingsComponent audio = _world.EntityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
            Assert.AreEqual(0.31f, audio.MasterVolume, 0.0001f);
            Assert.AreEqual(0.17f, audio.MusicVolume, 0.0001f);
            Assert.AreEqual(1, audio.MusicMuted);
            Assert.AreEqual(0.31f, AudioListener.volume, 0.0001f);

            AssistantSettingsComponent assistant =
                _world.EntityManager.GetComponentData<AssistantSettingsComponent>(boundary);
            Assert.AreEqual(AssistantGuidanceLevel.Off, assistant.GuidanceLevel);
            Assert.AreEqual(AssistantNarrationMode.Off, assistant.NarrationMode);
            Assert.AreEqual(0, assistant.AllowTakeover);
            Assert.AreEqual(1, assistant.HighContrastEnabled);

            DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
                _world.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
            Assert.AreEqual(0, popupRequests.Length, "Startup settings application must not open the Settings popup.");
            Assert.IsNull(
                bootstrapObject.GetComponentInChildren<SettingsPopupView>(true),
                "Startup must not instantiate a Settings popup to apply persisted values.");
        }
        finally
        {
            SettingsService.RuntimeApplied -= CaptureApplied;
            InvokeLifecycle(bootstrap, "OnDisable");
            UnityEngine.Object.DestroyImmediate(bootstrapObject);
        }
    }

    private static void InvokeLifecycle(MenuBootstrapView view, string methodName)
    {
        MethodInfo method = typeof(MenuBootstrapView).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, $"MenuBootstrapView lifecycle method '{methodName}' is missing.");
        method.Invoke(view, null);
    }
}
