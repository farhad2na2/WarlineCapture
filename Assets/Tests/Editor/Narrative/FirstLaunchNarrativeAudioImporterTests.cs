using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FirstLaunchNarrativeAudioImporterTests
{
    private static readonly string[] ExpectedClipIds =
    {
        "p02_radio",
        "p03_radio",
        "p04_dalia",
        "p04_samira",
        "p05_aria",
        "p06_aria",
        "p07_aria",
        "p09_aria",
        "p10_aria",
        "p11_dalia",
        "p12_samira",
        "p13_aria",
        "p14_commander",
        "p14_commander_female",
        "p14_commander_neutral",
        "p15_dalia",
        "p16_aria",
        "p17_dalia",
        "p18_aria",
    };

    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeAudioImporterTests tests = new();
            tests.TemporaryVoiceBatch_HasStableMonoClipIdsAndRequiredImportSettings();
            Debug.Log("[FirstLaunchNarrativeAudioImporterValidation] result=Passed tests=1 clips=19");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeAudioImporterValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void TemporaryVoiceBatch_HasStableMonoClipIdsAndRequiredImportSettings()
    {
        FirstLaunchNarrativeAudioImporter.ConfigureTemporaryVoiceImports();

        CollectionAssert.AreEqual(ExpectedClipIds, FirstLaunchNarrativeAudioImporter.StableClipIds);
        Assert.That(
            Directory.GetFiles(FirstLaunchNarrativeAudioImporter.VoiceRoot, "*.wav", SearchOption.TopDirectoryOnly),
            Has.Length.EqualTo(ExpectedClipIds.Length));

        foreach (string clipId in ExpectedClipIds)
        {
            string assetPath = FirstLaunchNarrativeAudioImporter.GetAssetPath(clipId);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            Assert.NotNull(importer, assetPath);
            Assert.NotNull(clip, assetPath);
            Assert.AreEqual(clipId, clip.name, assetPath);
            Assert.AreEqual(1, clip.channels, assetPath);
            Assert.IsTrue(importer.forceToMono, assetPath);
            Assert.IsTrue(importer.loadInBackground, assetPath);
            Assert.IsFalse(importer.ambisonic, assetPath);
            StringAssert.Contains(FirstLaunchNarrativeAudioImporter.RightsStatus, importer.userData, assetPath);
            StringAssert.Contains("runtimeNetworkTts=false", importer.userData, assetPath);

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, settings.loadType, assetPath);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, settings.compressionFormat, assetPath);
            Assert.AreEqual(FirstLaunchNarrativeAudioImporter.VorbisQuality, settings.quality, 0.0001f, assetPath);
            Assert.IsTrue(settings.preloadAudioData, assetPath);
            Assert.AreEqual(AudioSampleRateSetting.PreserveSampleRate, settings.sampleRateSetting, assetPath);

            AssertNormalizeDisabledWhenSupported(importer, assetPath);
        }

        Assert.DoesNotThrow(FirstLaunchNarrativeAudioImporter.ValidateTemporaryVoiceImports);
    }

    private static void AssertNormalizeDisabledWhenSupported(AudioImporter importer, string assetPath)
    {
        SerializedObject serializedImporter = new(importer);
        SerializedProperty normalize = serializedImporter.FindProperty("m_Normalize") ??
                                       serializedImporter.FindProperty("normalize");
        if (normalize != null)
            Assert.IsFalse(normalize.boolValue, assetPath);
    }
}
