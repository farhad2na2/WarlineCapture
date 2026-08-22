using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class M01TutorialNarrationAudioImporterTests
{
    [MenuItem("Game/Validation/Run M01 Tutorial ARIA Voice Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M01TutorialNarrationAudioImporter.ConfigureImports();
            new M01TutorialNarrationAudioImporterTests().LicensedEnglishAndPersianClipsMatchVoiceProfile();
            Debug.Log("[M01TutorialNarrationVoiceValidation] result=Passed clips=10 locales=2");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M01TutorialNarrationVoiceValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void LicensedEnglishAndPersianClipsMatchVoiceProfile()
    {
        Assert.AreEqual(10, M01TutorialNarrationAudioImporter.StableClipPaths.Length);
        M01TutorialNarrationAudioImporter.ValidateImports();
        for (int i = 0; i < M01TutorialNarrationAudioImporter.StableClipPaths.Length; i++)
        {
            string path = M01TutorialNarrationAudioImporter.StableClipPaths[i];
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), path);
        }
    }
}
