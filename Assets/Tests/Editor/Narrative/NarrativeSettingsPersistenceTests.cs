using System;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class NarrativeSettingsPersistenceTests
{
    private const string Prefix = "Game.Settings.";
    private const string ReducedMotionKey = Prefix + "Accessibility.ReducedMotion";
    private const string LegacyReducedMotionKey = "Game.ReducedMotion";
    private const string NarrativeSubtitlesEnabledKey = Prefix + "Narrative.SubtitlesEnabled";
    private const string NarrativeSubtitleSizeKey = Prefix + "Narrative.SubtitleSize";
    private const string NarrativeBackgroundOpacityKey = Prefix + "Narrative.BackgroundOpacity";
    private const string NarrativeInstantTextKey = Prefix + "Narrative.InstantText";
    private const string NarrativeAutoAdvanceKey = Prefix + "Narrative.AutoAdvance";

    private static readonly string[] IntKeys =
    {
        Prefix + "Audio.MusicEnabled",
        Prefix + "Audio.SoundEnabled",
        Prefix + "Audio.VoiceEnabled",
        Prefix + "Graphics.Quality",
        Prefix + "Graphics.FrameRateMode",
        Prefix + "Notifications.ThreatWarnings",
        Prefix + "Accessibility.HighContrastUi",
        Prefix + "Accessibility.LargeText",
        Prefix + "Accessibility.ColorblindMode",
        ReducedMotionKey,
        Prefix + "Localization.Language",
        Prefix + "Assistant.AssistanceLevel",
        Prefix + "Assistant.NarrationMode",
        Prefix + "Assistant.AllowTakeover",
        Prefix + "Assistant.SubtitlesEnabled",
        NarrativeSubtitlesEnabledKey,
        NarrativeSubtitleSizeKey,
        NarrativeBackgroundOpacityKey,
        NarrativeInstantTextKey,
        NarrativeAutoAdvanceKey,
        LegacyReducedMotionKey
    };

    private static readonly string[] FloatKeys =
    {
        Prefix + "Audio.MasterVolume",
        Prefix + "Audio.MusicVolume",
        Prefix + "Audio.SfxVolume",
        Prefix + "Audio.AlertsVolume",
        Prefix + "Audio.VoiceVolume",
        Prefix + "Controls.CameraSensitivity"
    };

    private IntPreferenceSnapshot[] _intSnapshots;
    private FloatPreferenceSnapshot[] _floatSnapshots;

    [SetUp]
    public void SetUp()
    {
        _intSnapshots = new IntPreferenceSnapshot[IntKeys.Length];
        for (int i = 0; i < IntKeys.Length; i++)
            _intSnapshots[i] = new IntPreferenceSnapshot(IntKeys[i]);

        _floatSnapshots = new FloatPreferenceSnapshot[FloatKeys.Length];
        for (int i = 0; i < FloatKeys.Length; i++)
            _floatSnapshots[i] = new FloatPreferenceSnapshot(FloatKeys[i]);

        DeleteFocusedKeys();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (IntPreferenceSnapshot snapshot in _intSnapshots)
            snapshot.Restore();

        foreach (FloatPreferenceSnapshot snapshot in _floatSnapshots)
            snapshot.Restore();

        PlayerPrefs.Save();
    }

    [Test]
    public void Defaults_IncludeDocumentedNarrativeAccessibilityPresets()
    {
        UISettingsModel defaults = SettingsService.Defaults;

        Assert.IsTrue(defaults.Narrative.SubtitlesEnabled);
        Assert.AreEqual(UISubtitleSize.Standard, defaults.Narrative.SubtitleSize);
        Assert.AreEqual(
            UISubtitleBackgroundOpacity.SeventyFivePercent,
            defaults.Narrative.BackgroundOpacity);
        Assert.IsFalse(defaults.Narrative.InstantText);
        Assert.IsTrue(defaults.Narrative.AutoAdvance);
        Assert.IsFalse(defaults.Accessibility.ReducedMotion);
        Assert.AreEqual(4, Enum.GetValues(typeof(UISubtitleSize)).Length);
        Assert.AreEqual(4, Enum.GetValues(typeof(UISubtitleBackgroundOpacity)).Length);
    }

    [Test]
    public void SaveAndLoad_RoundTripsNarrativeSettingsIndependently()
    {
        UISettingsModel model = SettingsService.Load();
        bool assistantSubtitles = model.Assistant.SubtitlesEnabled;
        model.Narrative.SubtitlesEnabled = false;
        model.Narrative.SubtitleSize = UISubtitleSize.ExtraLarge;
        model.Narrative.BackgroundOpacity = UISubtitleBackgroundOpacity.OneHundredPercent;
        model.Narrative.InstantText = true;
        model.Narrative.AutoAdvance = false;

        SettingsService.Save(model);
        UISettingsModel loaded = SettingsService.Load();

        Assert.IsFalse(loaded.Narrative.SubtitlesEnabled);
        Assert.AreEqual(UISubtitleSize.ExtraLarge, loaded.Narrative.SubtitleSize);
        Assert.AreEqual(
            UISubtitleBackgroundOpacity.OneHundredPercent,
            loaded.Narrative.BackgroundOpacity);
        Assert.IsTrue(loaded.Narrative.InstantText);
        Assert.IsFalse(loaded.Narrative.AutoAdvance);
        Assert.AreEqual(assistantSubtitles, loaded.Assistant.SubtitlesEnabled);
    }

    [Test]
    public void Load_InvalidNarrativeEnumsFallBackToDefaults()
    {
        PlayerPrefs.SetInt(NarrativeSubtitleSizeKey, 99);
        PlayerPrefs.SetInt(NarrativeBackgroundOpacityKey, -1);

        UISettingsModel loaded = SettingsService.Load();

        Assert.AreEqual(UISubtitleSize.Standard, loaded.Narrative.SubtitleSize);
        Assert.AreEqual(
            UISubtitleBackgroundOpacity.SeventyFivePercent,
            loaded.Narrative.BackgroundOpacity);
    }

    [Test]
    public void Load_MigratesLegacyReducedMotionOnlyWhenCanonicalKeyIsAbsent()
    {
        PlayerPrefs.SetInt(LegacyReducedMotionKey, 1);

        UISettingsModel migrated = SettingsService.Load();

        Assert.IsTrue(migrated.Accessibility.ReducedMotion);
        Assert.IsTrue(PlayerPrefs.HasKey(ReducedMotionKey));
        Assert.AreEqual(1, PlayerPrefs.GetInt(ReducedMotionKey));

        PlayerPrefs.SetInt(ReducedMotionKey, 0);
        UISettingsModel canonical = SettingsService.Load();

        Assert.IsFalse(canonical.Accessibility.ReducedMotion);
        Assert.AreEqual(1, PlayerPrefs.GetInt(LegacyReducedMotionKey));
    }

    [Test]
    public void Save_SynchronizesCanonicalAndLegacyReducedMotionKeys()
    {
        UISettingsModel model = SettingsService.Load();
        model.Accessibility.ReducedMotion = true;

        SettingsService.Save(model);

        Assert.AreEqual(1, PlayerPrefs.GetInt(ReducedMotionKey));
        Assert.AreEqual(1, PlayerPrefs.GetInt(LegacyReducedMotionKey));

        model.Accessibility.ReducedMotion = false;
        SettingsService.Save(model);

        Assert.AreEqual(0, PlayerPrefs.GetInt(ReducedMotionKey));
        Assert.AreEqual(0, PlayerPrefs.GetInt(LegacyReducedMotionKey));
    }

    private static void DeleteFocusedKeys()
    {
        PlayerPrefs.DeleteKey(ReducedMotionKey);
        PlayerPrefs.DeleteKey(LegacyReducedMotionKey);
        PlayerPrefs.DeleteKey(NarrativeSubtitlesEnabledKey);
        PlayerPrefs.DeleteKey(NarrativeSubtitleSizeKey);
        PlayerPrefs.DeleteKey(NarrativeBackgroundOpacityKey);
        PlayerPrefs.DeleteKey(NarrativeInstantTextKey);
        PlayerPrefs.DeleteKey(NarrativeAutoAdvanceKey);
        PlayerPrefs.Save();
    }

    private readonly struct IntPreferenceSnapshot
    {
        private readonly string _key;
        private readonly bool _exists;
        private readonly int _value;

        public IntPreferenceSnapshot(string key)
        {
            _key = key;
            _exists = PlayerPrefs.HasKey(key);
            _value = PlayerPrefs.GetInt(key);
        }

        public void Restore()
        {
            if (_exists)
                PlayerPrefs.SetInt(_key, _value);
            else
                PlayerPrefs.DeleteKey(_key);
        }
    }

    private readonly struct FloatPreferenceSnapshot
    {
        private readonly string _key;
        private readonly bool _exists;
        private readonly float _value;

        public FloatPreferenceSnapshot(string key)
        {
            _key = key;
            _exists = PlayerPrefs.HasKey(key);
            _value = PlayerPrefs.GetFloat(key);
        }

        public void Restore()
        {
            if (_exists)
                PlayerPrefs.SetFloat(_key, _value);
            else
                PlayerPrefs.DeleteKey(_key);
        }
    }
}
