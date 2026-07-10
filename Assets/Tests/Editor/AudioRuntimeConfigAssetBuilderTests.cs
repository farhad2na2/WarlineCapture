using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AudioRuntimeConfigAssetBuilderTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            AudioRuntimeConfigAssetBuilderTests tests = new();
            tests.BuildDefaultAssets_CreatesCatalogAndMixerAssets();
            tests.GeneratedCatalogAsset_HasLoadedClipReferences();
            tests.GeneratedMixerBusAsset_HasExpectedBusesAndDucking();
            Debug.Log("[AudioRuntimeConfigAssetBuilderValidation] result=Passed tests=3");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[AudioRuntimeConfigAssetBuilderValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BuildDefaultAssets_CreatesCatalogAndMixerAssets()
    {
        AudioRuntimeConfigAssetBuilder.BuildResult result = AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();

        Assert.GreaterOrEqual(result.EventCount, 48);
        Assert.GreaterOrEqual(result.BusCount, 6);
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(
            AudioRuntimeConfigAssetBuilder.EventCatalogAssetPath));
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioMixerBusConfig>(
            AudioRuntimeConfigAssetBuilder.MixerBusAssetPath));
    }

    [Test]
    public void GeneratedCatalogAsset_HasLoadedClipReferences()
    {
        AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();

        AudioEventCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(
            AudioRuntimeConfigAssetBuilder.EventCatalogAssetPath);

        Assert.NotNull(catalog);
        Assert.GreaterOrEqual(catalog.Events.Count, 48);

        bool foundPrimaryClick = false;
        for (int i = 0; i < catalog.Events.Count; i++)
        {
            AudioEventCatalogEntry entry = catalog.Events[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.EventId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.BusId));
            Assert.NotNull(entry.Playback);
            Assert.GreaterOrEqual(entry.Playback.MaxInstances, 1);
            Assert.Greater(entry.Clips.Count, 0, entry.EventId);

            for (int clipIndex = 0; clipIndex < entry.Clips.Count; clipIndex++)
            {
                Assert.NotNull(entry.Clips[clipIndex].Clip, entry.EventId);
                Assert.GreaterOrEqual(entry.Clips[clipIndex].Weight, 0, entry.EventId);
            }

            if (entry.EventId == AudioEventIds.UIButtonPrimaryClick)
                foundPrimaryClick = true;
        }

        Assert.IsTrue(foundPrimaryClick);
    }

    [Test]
    public void GeneratedMixerBusAsset_HasExpectedBusesAndDucking()
    {
        AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();

        AudioMixerBusConfig buses = AssetDatabase.LoadAssetAtPath<AudioMixerBusConfig>(
            AudioRuntimeConfigAssetBuilder.MixerBusAssetPath);

        Assert.NotNull(buses);
        Assert.GreaterOrEqual(buses.Buses.Count, 6);

        bool foundAlerts = false;
        for (int i = 0; i < buses.Buses.Count; i++)
        {
            AudioMixerBusEntry bus = buses.Buses[i];
            Assert.IsFalse(string.IsNullOrWhiteSpace(bus.BusId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bus.ParentBusId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(bus.VolumeSettingKey));

            if (bus.BusId != "Alerts")
                continue;

            foundAlerts = true;
            Assert.IsTrue(bus.CanDuck);
            Assert.IsTrue(Contains(bus.DuckTargetBusIds, "Music"));
            Assert.IsTrue(Contains(bus.DuckTargetBusIds, "Ambience"));
        }

        Assert.IsTrue(foundAlerts);
    }

    private static bool Contains(System.Collections.Generic.IReadOnlyList<string> values, string expected)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == expected)
                return true;
        }

        return false;
    }
}
