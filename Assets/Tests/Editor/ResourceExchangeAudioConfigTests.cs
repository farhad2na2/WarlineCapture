using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class ResourceExchangeAudioConfigTests
{
    private const string CatalogJsonPath = "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json";

    private static readonly string[] RequiredEventIds =
    {
        AudioEventIds.GameplayResourceExchangeAccepted,
        AudioEventIds.GameplayResourceExchangeRejected,
        AudioEventIds.GameplayResourceExchangeQueueStarted,
        AudioEventIds.GameplayResourceExchangeRushed,
        AudioEventIds.GameplayResourceExchangeCompleted,
        AudioEventIds.GameplayResourceExchangeCancelled
    };

    private static readonly uint[] RequiredEventHashes =
    {
        AudioEventIds.GameplayResourceExchangeAcceptedHash,
        AudioEventIds.GameplayResourceExchangeRejectedHash,
        AudioEventIds.GameplayResourceExchangeQueueStartedHash,
        AudioEventIds.GameplayResourceExchangeRushedHash,
        AudioEventIds.GameplayResourceExchangeCompletedHash,
        AudioEventIds.GameplayResourceExchangeCancelledHash
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new ResourceExchangeAudioConfigTests();
            tests.ResourceExchangeEventsHaveGeneratedConstantsAndCatalogEntries();
            passed++;
            tests.ResourceExchangeEventsUseConfigDrivenSfxGameplayClips();
            passed++;
            tests.ResourceExchangeEventHashesAreStableAndUnique();
            passed++;

            Debug.Log($"[ResourceExchangeAudioConfigValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[ResourceExchangeAudioConfigValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResourceExchangeEventsHaveGeneratedConstantsAndCatalogEntries()
    {
        Dictionary<string, AudioEventJson> eventsById = ReadCatalogEventsById();

        for (int i = 0; i < RequiredEventIds.Length; i++)
        {
            string eventId = RequiredEventIds[i];
            CollectionAssert.Contains(AudioEventIds.AllEventIds, eventId);
            Assert.IsTrue(eventsById.ContainsKey(eventId), $"Missing Resource Exchange audio catalog event: {eventId}");
        }
    }

    [Test]
    public void ResourceExchangeEventsUseConfigDrivenSfxGameplayClips()
    {
        Dictionary<string, AudioEventJson> eventsById = ReadCatalogEventsById();
        var clipPaths = new HashSet<string>();

        for (int i = 0; i < RequiredEventIds.Length; i++)
        {
            string eventId = RequiredEventIds[i];
            Assert.IsTrue(eventsById.TryGetValue(eventId, out AudioEventJson entry), eventId);

            Assert.AreEqual("SFX", entry.busId, eventId);
            Assert.IsTrue(entry.priority == "Medium" || entry.priority == "High", eventId);
            Assert.GreaterOrEqual(entry.cooldownMs, 100, eventId);
            Assert.LessOrEqual(entry.volumeDb, -8.0f, eventId);

            Assert.NotNull(entry.playback, eventId);
            Assert.IsFalse(entry.playback.loop, eventId);
            Assert.IsFalse(entry.playback.spatial, eventId);
            Assert.Greater(entry.playback.maxInstances, 0, eventId);
            Assert.LessOrEqual(entry.playback.maxInstances, 4, eventId);
            Assert.IsFalse(entry.playback.allowRuntimeLoad, eventId);

            Assert.NotNull(entry.clips, eventId);
            Assert.AreEqual(1, entry.clips.Length, eventId);
            AudioClipJson clip = entry.clips[0];
            StringAssert.StartsWith("Assets/Game/Audio/Gameplay/game_resource_exchange_", clip.assetPath, eventId);
            StringAssert.EndsWith(".wav", clip.assetPath, eventId);
            Assert.AreEqual("placeholder", clip.status, eventId);
            Assert.AreEqual(1, clip.weight, eventId);
            Assert.IsTrue(File.Exists(clip.assetPath), $"{eventId} references missing clip {clip.assetPath}.");
            Assert.IsTrue(clipPaths.Add(clip.assetPath), $"Duplicate Resource Exchange clip path: {clip.assetPath}");
        }
    }

    [Test]
    public void ResourceExchangeEventHashesAreStableAndUnique()
    {
        var hashes = new HashSet<uint>();

        for (int i = 0; i < RequiredEventIds.Length; i++)
        {
            Assert.AreEqual(AudioEventIds.StableHash(RequiredEventIds[i]), RequiredEventHashes[i], RequiredEventIds[i]);
            Assert.IsTrue(hashes.Add(RequiredEventHashes[i]), $"Duplicate Resource Exchange event hash: {RequiredEventIds[i]}");
        }
    }

    private static Dictionary<string, AudioEventJson> ReadCatalogEventsById()
    {
        Assert.IsTrue(File.Exists(CatalogJsonPath), $"Missing audio event catalog: {CatalogJsonPath}");
        CatalogJson catalog = JsonUtility.FromJson<CatalogJson>(File.ReadAllText(CatalogJsonPath));
        Assert.NotNull(catalog);
        Assert.NotNull(catalog.events);

        var eventsById = new Dictionary<string, AudioEventJson>(catalog.events.Length, StringComparer.Ordinal);
        for (int i = 0; i < catalog.events.Length; i++)
        {
            AudioEventJson entry = catalog.events[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.eventId), $"Catalog entry {i} is missing an event id.");
            Assert.IsTrue(eventsById.TryAdd(entry.eventId, entry), $"Duplicate audio event id: {entry.eventId}");
        }

        return eventsById;
    }

    [Serializable]
    private sealed class CatalogJson
    {
        public AudioEventJson[] events;
    }

    [Serializable]
    private sealed class AudioEventJson
    {
        public string eventId;
        public string busId;
        public string priority;
        public int cooldownMs;
        public float volumeDb;
        public PlaybackJson playback;
        public AudioClipJson[] clips;
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
}
