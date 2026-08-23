using System;
using System.IO;
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
            M01TutorialNarrationAudioImporterTests tests = new();
            tests.LicensedEnglishAndPersianClipsMatchVoiceProfile();
            tests.Manifest_TextMatchesEveryCommandAndWorldTargetCue();
            Debug.Log("[M01TutorialNarrationVoiceValidation] result=Passed clips=12 locales=2");
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
        Assert.AreEqual(12, M01TutorialNarrationAudioImporter.StableClipPaths.Length);
        M01TutorialNarrationAudioImporter.ValidateImports();
        for (int i = 0; i < M01TutorialNarrationAudioImporter.StableClipPaths.Length; i++)
        {
            string path = M01TutorialNarrationAudioImporter.StableClipPaths[i];
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(path), path);
        }
    }

    [Test]
    public void Manifest_TextMatchesEveryCommandAndWorldTargetCue()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        Assert.IsFalse(string.IsNullOrWhiteSpace(projectRoot));
        string manifestPath = Path.Combine(
            projectRoot,
            "Assets/Game/Audio/Voice/Tutorial/tutorial_m01_aria_voice_manifest.json");
        string manifest = File.ReadAllText(manifestPath);

        StringAssert.Contains("WarlineCapture.TutorialNarrationVoice.v2", manifest);
        StringAssert.Contains("Tap MOVE to select the move command.", manifest);
        StringAssert.Contains("Tap the highlighted destination to move your squad.", manifest);
        StringAssert.Contains("Tap ATTACK to select the attack command.", manifest);
        StringAssert.Contains("Tap the highlighted enemy to issue the attack.", manifest);
        StringAssert.Contains("برای انتخاب دستور حرکت، روی «حرکت» بزنید.", manifest);
        StringAssert.Contains("برای حرکت گروه، روی مقصد علامت‌گذاری‌شده بزنید.", manifest);
        StringAssert.Contains("برای انتخاب دستور حمله، روی «حمله» بزنید.", manifest);
        StringAssert.Contains("برای صدور دستور حمله، روی دشمن علامت‌گذاری‌شده بزنید.", manifest);
    }
}
