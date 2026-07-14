using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FirstLaunchNarrativeEnvironmentAudioImporterTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeEnvironmentAudioImporter.ConfigureEnvironmentAudioImports();
            Assert.AreEqual(6, FirstLaunchNarrativeEnvironmentAudioImporter.StableStreamingLoopIds.Count);
            Assert.AreEqual(2, FirstLaunchNarrativeEnvironmentAudioImporter.StableResidentEventIds.Count);
            ValidateProfile(FirstLaunchNarrativeEnvironmentAudioImporter.StableStreamingLoopIds, streaming: true);
            ValidateProfile(FirstLaunchNarrativeEnvironmentAudioImporter.StableResidentEventIds, streaming: false);
            Debug.Log("[FirstLaunchNarrativeEnvironmentAudioImporterValidation] result=Passed loops=6 events=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeEnvironmentAudioImporterValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static void ValidateProfile(System.Collections.Generic.IReadOnlyList<string> clipIds, bool streaming)
    {
        foreach (string clipId in clipIds)
        {
            string assetPath = FirstLaunchNarrativeEnvironmentAudioImporter.GetAssetPath(clipId);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            Assert.NotNull(importer, assetPath);
            Assert.NotNull(clip, assetPath);
            Assert.AreEqual(streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType, assetPath);
            Assert.AreEqual(streaming, importer.loadInBackground, assetPath);
            Assert.AreEqual(!streaming, importer.defaultSampleSettings.preloadAudioData, assetPath);
            StringAssert.Contains(FirstLaunchNarrativeEnvironmentAudioImporter.RightsStatus, importer.userData, assetPath);
            StringAssert.Contains("runtimeNetworkGeneration=false", importer.userData, assetPath);
        }
    }
}
