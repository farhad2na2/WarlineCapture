using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AudioConfigContractTests
{
    private static readonly string[] ConfigPaths =
    {
        "Assets/Game/Scripts/Configs/Audio/AudioEventCatalogConfig.cs",
        "Assets/Game/Scripts/Configs/Audio/AudioEventCatalogEntry.cs",
        "Assets/Game/Scripts/Configs/Audio/AudioMixerBusConfig.cs",
        "Assets/Game/Scripts/Configs/Audio/AudioMusicStateConfig.cs",
        "Assets/Game/Scripts/Configs/Audio/AudioEventIds.cs"
    };

    private const string CatalogJsonPath = "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json";
    private const string RuntimeCatalogAssetPath = "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset";
    private const string ImportProfileJsonPath = "Assets/Game/Audio/Config/audio_import_profiles_v0_1.json";

    private static readonly string[] RequiredCoreEventIds =
    {
        AudioEventIds.UIButtonPrimaryClick,
        AudioEventIds.UIButtonSecondaryClick,
        AudioEventIds.UIButtonNegativeClick,
        AudioEventIds.UIButtonDisabledTap,
        AudioEventIds.UITabSelect,
        AudioEventIds.UICardSelect,
        AudioEventIds.UIPopupOpen,
        AudioEventIds.UIPopupClose,
        AudioEventIds.UIFeedbackToastError,
        AudioEventIds.UIScreenForward,
        AudioEventIds.UIScreenBack,
        AudioEventIds.UIDrawerOpen,
        AudioEventIds.UIDrawerClose,
        AudioEventIds.GameplayCommandMoveAccepted,
        AudioEventIds.GameplayCommandAttackAccepted,
        AudioEventIds.GameplayCommandHoldAccepted,
        AudioEventIds.GameplayCommandStopReturning,
        AudioEventIds.GameplayCommandScanAccepted,
        AudioEventIds.GameplayCommandScanTargeting,
        AudioEventIds.GameplayCommandRejected,
        AudioEventIds.GameplayUnitEngineVehicleMove,
        AudioEventIds.GameplayUnitEngineAircraftTakeoff,
        AudioEventIds.GameplayUnitEngineAircraftFlight,
        AudioEventIds.GameplayUnitEngineHelicopterFlight,
        AudioEventIds.GameplayWeaponFireSmallArms,
        AudioEventIds.GameplayWeaponMissileLaunch,
        AudioEventIds.GameplayWeaponMissileFlight,
        AudioEventIds.GameplayWeaponMissileImpact,
        AudioEventIds.GameplayBuildPlaceValid,
        AudioEventIds.GameplayBuildPlaceInvalid,
        AudioEventIds.GameplayProductionQueued,
        AudioEventIds.GameplayUnitVehicleEngine,
        AudioEventIds.GameplayUnitAircraftFlyby,
        AudioEventIds.GameplayWeaponMissileFlight,
        AudioEventIds.AlertThreatMinor,
        AudioEventIds.AlertThreatCritical,
        AudioEventIds.AlertUnitUnderAttack,
        AudioEventIds.AlertBaseBreached,
        AudioEventIds.GameplayObjectiveProgress,
        AudioEventIds.GameplayObjectiveComplete,
        AudioEventIds.GameplayObjectiveFailed,
        AudioEventIds.MusicMenuLoop,
        AudioEventIds.MusicMatchCalmLoop
    };

    public static void RunFocusedValidation()
    {
        try
        {
            AudioConfigContractTests tests = new();
            tests.AudioConfigContractsExist();
            tests.AudioConfigContractsAreScriptableObjects();
            tests.AudioConfigContractsAreDataOnly();
            tests.AudioEventEntryDefaultsToSafeRuntimeValues();
            tests.AudioMusicStateDefaultsToLoopWithNonNegativeFades();
            tests.AudioConfigAssetsCreateWithEmptyCollections();
            tests.AudioEventIdsMatchCatalogJson();
            tests.AudioEventHashesAreStableAndUnique();
            tests.AudioCatalogRequiredEventsHaveEntries();
            tests.AudioCatalogEntriesReferenceValidBusesAndClips();
            tests.AudioCatalogEntriesUseRuntimeSafePlaybackRules();
            tests.SerializedRuntimeCatalogMatchesSourceJson();
            tests.AudioImportProfileConfigExists();
            tests.CatalogAudioImportSettingsMatchProfiles();
            Debug.Log("[AudioConfigContractValidation] result=Passed tests=14");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[AudioConfigContractValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static readonly string[] ForbiddenRuntimeTokens =
    {
        ": MonoBehaviour",
        "Baker<",
        "void Update(",
        "void Awake(",
        "void Start(",
        "AudioSource",
        "PlayOneShot",
        "Resources.Load",
        "FindObjectOfType",
        "FindObjectsOfType",
        "GameObject.Find",
        "SceneManager"
    };

    [Test]
    public void AudioConfigContractsExist()
    {
        for (int i = 0; i < ConfigPaths.Length; i++)
            Assert.IsTrue(File.Exists(ConfigPaths[i]), $"Missing audio config contract: {ConfigPaths[i]}");
    }

    [Test]
    public void AudioConfigContractsAreScriptableObjects()
    {
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(AudioEventCatalogConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(AudioMixerBusConfig)));
        Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(AudioMusicStateConfig)));
    }

    [Test]
    public void AudioConfigContractsAreDataOnly()
    {
        for (int fileIndex = 0; fileIndex < ConfigPaths.Length; fileIndex++)
        {
            string path = ConfigPaths[fileIndex];
            string source = File.ReadAllText(path);
            for (int tokenIndex = 0; tokenIndex < ForbiddenRuntimeTokens.Length; tokenIndex++)
            {
                string token = ForbiddenRuntimeTokens[tokenIndex];
                StringAssert.DoesNotContain(token, source, $"{path} must stay data-only and must not play audio directly.");
            }
        }
    }

    [Test]
    public void AudioEventEntryDefaultsToSafeRuntimeValues()
    {
        AudioEventCatalogEntry entry = new();

        Assert.AreEqual("SFX", entry.BusId);
        Assert.AreEqual(AudioEventPriority.Medium, entry.Priority);
        Assert.AreEqual(0, entry.CooldownMilliseconds);
        Assert.NotNull(entry.Playback);
        Assert.AreEqual(4, entry.Playback.MaxInstances);
        Assert.IsFalse(entry.Playback.AllowRuntimeLoad);
        Assert.NotNull(entry.Clips);
        Assert.AreEqual(0, entry.Clips.Count);
        Assert.AreEqual(new Vector2(-0.02f, 0.02f), entry.PitchVariance);
    }

    [Test]
    public void AudioMusicStateDefaultsToLoopWithNonNegativeFades()
    {
        AudioMusicStateEntry entry = new();

        Assert.IsTrue(entry.Loop);
        Assert.GreaterOrEqual(entry.FadeInSeconds, 0f);
        Assert.GreaterOrEqual(entry.FadeOutSeconds, 0f);
        Assert.GreaterOrEqual(entry.MinimumPlaySeconds, 0f);
    }

    [Test]
    public void AudioConfigAssetsCreateWithEmptyCollections()
    {
        AudioEventCatalogConfig catalog = ScriptableObject.CreateInstance<AudioEventCatalogConfig>();
        AudioMixerBusConfig buses = ScriptableObject.CreateInstance<AudioMixerBusConfig>();
        AudioMusicStateConfig states = ScriptableObject.CreateInstance<AudioMusicStateConfig>();
        try
        {
            Assert.NotNull(catalog.Events);
            Assert.NotNull(buses.Buses);
            Assert.NotNull(states.States);
            Assert.AreEqual(0, catalog.Events.Count);
            Assert.AreEqual(0, buses.Buses.Count);
            Assert.AreEqual(0, states.States.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
            UnityEngine.Object.DestroyImmediate(buses);
            UnityEngine.Object.DestroyImmediate(states);
        }
    }

    [Test]
    public void AudioEventIdsMatchCatalogJson()
    {
        string[] catalogEventIds = ReadCatalogEventIds();

        CollectionAssert.AreEqual(catalogEventIds, AudioEventIds.AllEventIds);
        Assert.GreaterOrEqual(AudioEventIds.AllEventIds.Length, 48);
        Assert.AreEqual(AudioEventIds.UIButtonPrimaryClick, catalogEventIds[0]);
        CollectionAssert.Contains(catalogEventIds, AudioEventIds.AmbienceBaseDistantLoop);
    }

    [Test]
    public void AudioEventHashesAreStableAndUnique()
    {
        string[] eventIds = AudioEventIds.AllEventIds;
        uint[] hashes = AudioEventIds.AllEventHashes;

        Assert.AreEqual(eventIds.Length, hashes.Length);
        Assert.AreEqual(hashes.Length, new HashSet<uint>(hashes).Count);

        for (int i = 0; i < eventIds.Length; i++)
            Assert.AreEqual(AudioEventIds.StableHash(eventIds[i]), hashes[i], eventIds[i]);
    }

    [Test]
    public void AudioImportProfileConfigExists()
    {
        Assert.IsTrue(File.Exists(ImportProfileJsonPath), $"Missing audio import profile config: {ImportProfileJsonPath}");
    }

    [Test]
    public void AudioCatalogRequiredEventsHaveEntries()
    {
        CatalogJson catalog = ReadCatalog();
        HashSet<string> eventIds = new(catalog.events.Select(entry => entry.eventId));

        for (int i = 0; i < RequiredCoreEventIds.Length; i++)
            CollectionAssert.Contains(eventIds, RequiredCoreEventIds[i]);
    }

    [Test]
    public void AudioCatalogEntriesReferenceValidBusesAndClips()
    {
        CatalogJson catalog = ReadCatalog();
        HashSet<string> busIds = new(catalog.buses.Select(bus => bus.busId));
        var eventIds = new HashSet<string>();
        var clipPaths = new HashSet<string>();

        for (int i = 0; i < catalog.events.Length; i++)
        {
            AudioEventJson entry = catalog.events[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.eventId), $"Catalog event at index {i} must have an event id.");
            Assert.IsTrue(eventIds.Add(entry.eventId), $"Duplicate audio event id: {entry.eventId}");
            Assert.IsTrue(busIds.Contains(entry.busId), $"{entry.eventId} references unknown bus {entry.busId}.");
            Assert.NotNull(entry.clips, $"{entry.eventId} must declare clips.");
            Assert.Greater(entry.clips.Length, 0, $"{entry.eventId} must have at least one clip or fallback.");

            for (int clipIndex = 0; clipIndex < entry.clips.Length; clipIndex++)
            {
                AudioClipJson clip = entry.clips[clipIndex];
                Assert.IsFalse(string.IsNullOrWhiteSpace(clip.assetPath), $"{entry.eventId} clip {clipIndex} must have an asset path.");
                StringAssert.StartsWith("Assets/Game/Audio/", clip.assetPath, clip.assetPath);
                StringAssert.EndsWith(".wav", clip.assetPath, clip.assetPath);
                Assert.IsTrue(File.Exists(clip.assetPath), $"{entry.eventId} references missing clip {clip.assetPath}.");
                Assert.Greater(clip.weight, 0, $"{entry.eventId} clip {clip.assetPath} must have positive weight.");
                Assert.IsTrue(clipPaths.Add(clip.assetPath), $"Duplicate catalog clip path: {clip.assetPath}");
            }
        }
    }

    [Test]
    public void AudioCatalogEntriesUseRuntimeSafePlaybackRules()
    {
        CatalogJson catalog = ReadCatalog();
        var validPriorities = new HashSet<string> { "Low", "Medium", "High", "Critical" };

        for (int i = 0; i < catalog.events.Length; i++)
        {
            AudioEventJson entry = catalog.events[i];
            Assert.IsTrue(validPriorities.Contains(entry.priority), $"{entry.eventId} has invalid priority {entry.priority}.");
            Assert.GreaterOrEqual(entry.cooldownMs, 0, $"{entry.eventId} cooldown must be non-negative.");
            Assert.NotNull(entry.playback, $"{entry.eventId} must have playback settings.");
            Assert.Greater(entry.playback.maxInstances, 0, $"{entry.eventId} maxInstances must be positive.");
            Assert.IsFalse(entry.playback.allowRuntimeLoad, $"{entry.eventId} must not allow runtime loading.");
            Assert.GreaterOrEqual(entry.pitchVariance.max, entry.pitchVariance.min, $"{entry.eventId} pitch variance min/max is invalid.");

            bool isLoopBus = entry.busId == "Music" || entry.busId == "Ambience";
            if (entry.playback.loop)
                Assert.IsTrue(isLoopBus, $"{entry.eventId} loops must stay on Music or Ambience buses.");
        }
    }

    [Test]
    public void SerializedRuntimeCatalogMatchesSourceJson()
    {
        CatalogJson source = ReadCatalog();
        AudioEventCatalogConfig runtime =
            AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(RuntimeCatalogAssetPath);
        Assert.NotNull(runtime, $"Missing serialized runtime catalog: {RuntimeCatalogAssetPath}");
        Assert.AreEqual(source.events.Length, runtime.Events.Count, "Serialized runtime catalog event count drifted from JSON.");

        for (int eventIndex = 0; eventIndex < source.events.Length; eventIndex++)
        {
            AudioEventJson expected = source.events[eventIndex];
            AudioEventCatalogEntry actual = runtime.Events[eventIndex];
            Assert.NotNull(actual, $"Serialized runtime event {eventIndex} is null.");
            Assert.AreEqual(expected.eventId, actual.EventId, $"eventId[{eventIndex}]");
            Assert.AreEqual(expected.busId, actual.BusId, expected.eventId);
            Assert.AreEqual(Enum.Parse<AudioEventPriority>(expected.priority), actual.Priority, expected.eventId);
            Assert.AreEqual(expected.cooldownMs, actual.CooldownMilliseconds, expected.eventId);
            Assert.That(actual.VolumeDecibels, Is.EqualTo(expected.volumeDb).Within(0.001f), expected.eventId);
            Assert.That(actual.PitchVariance.x, Is.EqualTo(expected.pitchVariance.min).Within(0.001f), expected.eventId);
            Assert.That(actual.PitchVariance.y, Is.EqualTo(expected.pitchVariance.max).Within(0.001f), expected.eventId);
            Assert.AreEqual(expected.playback.loop, actual.Playback.Loop, expected.eventId);
            Assert.AreEqual(expected.playback.spatial, actual.Playback.Spatial, expected.eventId);
            Assert.AreEqual(expected.playback.maxInstances, actual.Playback.MaxInstances, expected.eventId);
            Assert.AreEqual(expected.playback.allowRuntimeLoad, actual.Playback.AllowRuntimeLoad, expected.eventId);
            Assert.AreEqual(expected.clips.Length, actual.Clips.Count, expected.eventId);

            for (int clipIndex = 0; clipIndex < expected.clips.Length; clipIndex++)
            {
                AudioClipWeightEntry actualClip = actual.Clips[clipIndex];
                Assert.NotNull(actualClip, $"{expected.eventId} clip {clipIndex} is null.");
                Assert.AreEqual(
                    expected.clips[clipIndex].assetPath,
                    AssetDatabase.GetAssetPath(actualClip.Clip),
                    $"{expected.eventId} clip {clipIndex}");
                Assert.AreEqual(expected.clips[clipIndex].weight, actualClip.Weight, $"{expected.eventId} clip {clipIndex}");
            }
        }
    }

    [Test]
    public void CatalogAudioImportSettingsMatchProfiles()
    {
        string[] clipPaths = ReadCatalogClipPaths();
        ImportProfilesJson importProfiles = ReadImportProfiles();
        Assert.GreaterOrEqual(clipPaths.Length, 48);
        Assert.AreEqual(
            0,
            importProfiles.overrides.Length,
            "The accepted category-level Voice policy must not retain temporary pilot overrides.");
        Assert.NotNull(importProfiles.validationSets, "Audio importer config must preserve validation sets.");
        Assert.NotNull(importProfiles.validationSets.APH405VoicePilot, "APH-405 pilot evidence set is missing.");
        Assert.AreEqual(
            8,
            importProfiles.validationSets.APH405VoicePilot.Length,
            "APH-405 evidence must remain frozen to the eight clips measured on Android.");
        CollectionAssert.IsSubsetOf(importProfiles.validationSets.APH405VoicePilot, clipPaths);

        for (int i = 0; i < clipPaths.Length; i++)
        {
            string path = clipPaths[i];
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            Assert.NotNull(importer, $"{path} must use AudioImporter.");
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            string category = ReadAudioCategory(path);

            Assert.AreEqual(AudioCompressionFormat.Vorbis, settings.compressionFormat, path);
            Assert.AreEqual(AudioSampleRateSetting.PreserveSampleRate, settings.sampleRateSetting, path);
            Assert.AreEqual(44100, settings.sampleRateOverride, path);
            Assert.IsFalse(importer.ambisonic, path);

            if (category == "Voice")
            {
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, settings.loadType, path);
                Assert.IsTrue(importer.forceToMono, path);
                Assert.IsFalse(settings.preloadAudioData, path);
                Assert.IsTrue(importer.loadInBackground, path);
            }
            else if (category == "Music" || category == "Ambience")
            {
                Assert.AreEqual(AudioClipLoadType.Streaming, settings.loadType, path);
                Assert.IsFalse(importer.forceToMono, path);
                Assert.IsFalse(settings.preloadAudioData, path);
                Assert.IsTrue(importer.loadInBackground, path);
            }
            else
            {
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, settings.loadType, path);
                Assert.IsTrue(importer.forceToMono, path);
                Assert.IsTrue(settings.preloadAudioData, path);
                Assert.IsFalse(importer.loadInBackground, path);
            }
        }
    }

    private static string[] ReadCatalogEventIds()
    {
        return ReadCatalog()
            .events
            .Select(entry => entry.eventId)
            .ToArray();
    }

    private static string[] ReadCatalogClipPaths()
    {
        return ReadCatalog()
            .events
            .SelectMany(entry => entry.clips ?? Array.Empty<AudioClipJson>())
            .Select(clip => clip.assetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static CatalogJson ReadCatalog()
    {
        Assert.IsTrue(File.Exists(CatalogJsonPath), $"Missing audio event catalog: {CatalogJsonPath}");
        CatalogJson catalog = JsonUtility.FromJson<CatalogJson>(File.ReadAllText(CatalogJsonPath));
        Assert.NotNull(catalog);
        Assert.NotNull(catalog.buses);
        Assert.NotNull(catalog.events);
        Assert.Greater(catalog.buses.Length, 0);
        Assert.Greater(catalog.events.Length, 0);
        return catalog;
    }

    private static ImportProfilesJson ReadImportProfiles()
    {
        Assert.IsTrue(File.Exists(ImportProfileJsonPath), $"Missing audio import profile config: {ImportProfileJsonPath}");
        ImportProfilesJson profiles = JsonUtility.FromJson<ImportProfilesJson>(File.ReadAllText(ImportProfileJsonPath));
        Assert.NotNull(profiles);
        Assert.NotNull(profiles.overrides);
        return profiles;
    }

    private static string ReadAudioCategory(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        Assert.GreaterOrEqual(parts.Length, 5, assetPath);
        Assert.AreEqual("Assets", parts[0], assetPath);
        Assert.AreEqual("Game", parts[1], assetPath);
        Assert.AreEqual("Audio", parts[2], assetPath);
        return parts[3];
    }

    [Serializable]
    private sealed class CatalogJson
    {
        public BusJson[] buses;
        public AudioEventJson[] events;
    }

    [Serializable]
    private sealed class BusJson
    {
        public string busId;
        public string parentBusId;
    }

    [Serializable]
    private sealed class AudioEventJson
    {
        public string eventId;
        public string busId;
        public string priority;
        public int cooldownMs;
        public float volumeDb;
        public PitchVarianceJson pitchVariance;
        public PlaybackJson playback;
        public AudioClipJson[] clips;
    }

    [Serializable]
    private sealed class PitchVarianceJson
    {
        public float min;
        public float max;
    }

    [Serializable]
    private sealed class PlaybackJson
    {
        public bool loop;
        public bool spatial;
        public int maxInstances;
        public bool allowRuntimeLoad;
    }

    [Serializable]
    private sealed class AudioClipJson
    {
        public string assetPath;
        public string status;
        public int weight;
    }

    [Serializable]
    private sealed class ImportProfilesJson
    {
        public ImportProfileOverrideJson[] overrides;
        public ImportProfileValidationSetsJson validationSets;
    }

    [Serializable]
    private sealed class ImportProfileValidationSetsJson
    {
        public string[] APH405VoicePilot;
    }

    [Serializable]
    private sealed class ImportProfileOverrideJson
    {
        public string assetPath;
        public string profile;
    }
}
