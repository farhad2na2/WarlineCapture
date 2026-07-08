using System;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class AudioConfigContractTests
{
    private static readonly string[] ConfigPaths =
    {
        "Assets/Game/Scripts/Audio/Config/AudioEventCatalogConfig.cs",
        "Assets/Game/Scripts/Audio/Config/AudioEventCatalogEntry.cs",
        "Assets/Game/Scripts/Audio/Config/AudioMixerBusConfig.cs",
        "Assets/Game/Scripts/Audio/Config/AudioMusicStateConfig.cs"
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
            Debug.Log("[AudioConfigContractValidation] result=Passed tests=6");
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
}
