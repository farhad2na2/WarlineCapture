using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class FirstLaunchNarrativeTextCatalogTests
{
    private const string CatalogPath = "Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json";
    private const string AudioPlanPath = "Assets/Game/Data/Narrative/FirstLaunch/first_launch_audio_cue_plan.json";

    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeTextCatalogTests tests = new();
            tests.EnglishCatalog_HasStableUniqueKeysAndDistinctSpeakerIdentity();
            tests.AudioCuePlan_ReferencesExistingLocalAssetsAndForbidsRuntimeTts();
            Debug.Log("[FirstLaunchNarrativeTextCatalogValidation] result=Passed tests=2 lines=17 speakers=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeTextCatalogValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void EnglishCatalog_HasStableUniqueKeysAndDistinctSpeakerIdentity()
    {
        TextCatalog catalog = JsonUtility.FromJson<TextCatalog>(File.ReadAllText(CatalogPath));
        Assert.AreEqual(1, catalog.schemaVersion);
        Assert.AreEqual("en", catalog.locale);
        Assert.AreEqual("first_launch", catalog.sequenceId);
        Assert.AreEqual(17, catalog.lines.Length);
        Assert.AreEqual(5, catalog.speakers.Length);
        Assert.GreaterOrEqual(catalog.essentialCaptions.Length, 6);

        HashSet<string> keys = new(StringComparer.Ordinal);
        HashSet<string> lineIds = new(StringComparer.Ordinal);
        foreach (Line line in catalog.lines)
        {
            Assert.IsTrue(lineIds.Add(line.lineId), line.lineId);
            Assert.IsTrue(keys.Add(line.key), line.key);
            StringAssert.StartsWith("narrative.first_launch.line.", line.key);
            Assert.IsNotEmpty(line.speaker);
            Assert.IsNotEmpty(line.text);
        }

        foreach (Speaker speaker in catalog.speakers)
        {
            Assert.IsTrue(keys.Add(speaker.nameKey), speaker.nameKey);
            Assert.IsTrue(keys.Add(speaker.roleKey), speaker.roleKey);
            Assert.IsTrue(keys.Add(speaker.accessibleLabelKey), speaker.accessibleLabelKey);
            Assert.IsNotEmpty(speaker.name);
            Assert.IsNotEmpty(speaker.role);
            Assert.IsNotEmpty(speaker.accessibleLabel);
        }

        Assert.AreNotEqual(FindSpeaker(catalog, "DALIA").name, FindSpeaker(catalog, "SAMIRA").name);
        Assert.AreNotEqual(FindSpeaker(catalog, "ARIA").role, FindSpeaker(catalog, "RADIO").role);
    }

    [Test]
    public void AudioCuePlan_ReferencesExistingLocalAssetsAndForbidsRuntimeTts()
    {
        AudioPlan plan = JsonUtility.FromJson<AudioPlan>(File.ReadAllText(AudioPlanPath));
        Assert.AreEqual("first_launch", plan.sequenceId);
        Assert.IsTrue(File.Exists(plan.score.asset), plan.score.asset);
        Assert.GreaterOrEqual(plan.cues.Length, 7);
        foreach (Cue cue in plan.cues)
        {
            Assert.IsTrue(File.Exists(cue.asset), cue.asset);
            Assert.IsNotEmpty(cue.purpose);
            Assert.IsNotEmpty(cue.bus);
        }

        Assert.IsFalse(plan.mixRules.runtimeNetworkTts);
        Assert.IsTrue(plan.mixRules.voiceHonorsVoiceSetting);
        Assert.IsTrue(plan.mixRules.mutedPlaybackMustRemainClearWithSubtitles);
    }

    private static Speaker FindSpeaker(TextCatalog catalog, string id)
    {
        foreach (Speaker speaker in catalog.speakers)
        {
            if (speaker.id == id)
                return speaker;
        }
        Assert.Fail($"Missing speaker {id}");
        return null;
    }

    [Serializable] private sealed class TextCatalog
    {
        public int schemaVersion;
        public string locale;
        public string sequenceId;
        public Control[] controls;
        public Speaker[] speakers;
        public Line[] lines;
        public Caption[] essentialCaptions;
    }

    [Serializable] private sealed class Control { public string key; public string value; }
    [Serializable] private sealed class Caption { public string key; public string value; }
    [Serializable] private sealed class Line { public string lineId; public string key; public string speaker; public string text; }
    [Serializable] private sealed class Speaker
    {
        public string id;
        public string nameKey;
        public string name;
        public string roleKey;
        public string role;
        public string accessibleLabelKey;
        public string accessibleLabel;
    }

    [Serializable] private sealed class AudioPlan
    {
        public string sequenceId;
        public Score score;
        public Cue[] cues;
        public MixRules mixRules;
    }

    [Serializable] private sealed class Score { public string asset; public string status; public string notes; }
    [Serializable] private sealed class Cue { public string stateId; public string purpose; public string asset; public string bus; }
    [Serializable] private sealed class MixRules
    {
        public bool voiceHonorsVoiceSetting;
        public bool ambienceHonorsSoundSetting;
        public bool scoreHonorsMusicSetting;
        public bool mutedPlaybackMustRemainClearWithSubtitles;
        public bool runtimeNetworkTts;
    }
}
