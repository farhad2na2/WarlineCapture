using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public sealed class FirstLaunchNarrativeConfigTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeConfigTests tests = new();
            tests.SequenceConfig_HasUniqueConnectedStatesAndAllApprovedPanels();
            tests.DialogueLines_HaveStableKeysSpeakersTimingAndVoiceClips();
            tests.SpeakerCatalog_UsesDistinctPortraitsAndProductionAriaIcon();
            tests.SequenceConfig_DoesNotDirectlyRetainPanelTextures();
            Debug.Log("[FirstLaunchNarrativeConfigValidation] result=Passed tests=4 states=26 panels=22 lines=17 speakers=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeConfigValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void SequenceConfig_HasUniqueConnectedStatesAndAllApprovedPanels()
    {
        FirstLaunchNarrativeConfigBuilder.Build();
        NarrativeSequenceConfig config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        Assert.NotNull(config);
        Assert.AreEqual("FL-P01", config.EntryStateId);
        Assert.AreEqual(26, config.States.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);
        int panelCount = 0;
        foreach (NarrativeStateRecord state in config.States)
        {
            Assert.IsTrue(ids.Add(state.StateId), state.StateId);
            Assert.IsTrue(state.ReducedMotionSupported, state.StateId);
            bool has16x9 = state.Panel16x9Reference != null && state.Panel16x9Reference.RuntimeKeyIsValid();
            bool has20x9 = state.Panel20x9Reference != null && state.Panel20x9Reference.RuntimeKeyIsValid();
            if (has16x9 || has20x9)
            {
                Assert.IsNull(state.Panel16x9, state.StateId);
                Assert.IsNull(state.Panel20x9, state.StateId);
                Assert.IsTrue(has16x9, state.StateId);
                Assert.IsTrue(has20x9, state.StateId);
                StringAssert.Contains("/Panels/16x9/FL-P", AssetDatabase.GUIDToAssetPath(state.Panel16x9Reference.AssetGUID));
                StringAssert.Contains("/Panels/20x9/FL-P", AssetDatabase.GUIDToAssetPath(state.Panel20x9Reference.AssetGUID));
                Assert.NotNull(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(state.Panel16x9Reference.AssetGUID), state.StateId);
                Assert.NotNull(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(state.Panel20x9Reference.AssetGUID), state.StateId);
                panelCount++;
            }
        }
        Assert.AreEqual(22, panelCount);
        foreach (NarrativeStateRecord state in config.States)
        {
            if (!string.IsNullOrEmpty(state.ContinueStateId))
                Assert.IsTrue(ids.Contains(state.ContinueStateId), $"{state.StateId} -> {state.ContinueStateId}");
            if (!string.IsNullOrEmpty(state.SkipStateId))
                Assert.IsTrue(ids.Contains(state.SkipStateId), $"{state.StateId} skip -> {state.SkipStateId}");
        }
    }

    [Test]
    public void SequenceConfig_DoesNotDirectlyRetainPanelTextures()
    {
        string[] dependencies = AssetDatabase.GetDependencies(FirstLaunchNarrativeConfigBuilder.SequencePath, true);
        foreach (string dependency in dependencies)
            StringAssert.DoesNotContain("/Art/Narrative/FirstLaunch/Panels/", dependency);
    }

    [Test]
    public void DialogueLines_HaveStableKeysSpeakersTimingAndVoiceClips()
    {
        NarrativeSequenceConfig config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        int lineCount = 0;
        HashSet<string> lineIds = new(StringComparer.Ordinal);
        foreach (NarrativeStateRecord state in config.States)
        {
            foreach (NarrativeDialogueLineRecord line in state.Lines)
            {
                lineCount++;
                Assert.IsTrue(lineIds.Add(line.LineId), line.LineId);
                Assert.AreEqual($"narrative.first_launch.line.{line.LineId}", line.TextKey);
                Assert.IsNotEmpty(line.EnglishFallback);
                Assert.NotNull(line.VoiceClip, line.LineId);
                Assert.Greater(line.DeadlineSeconds, line.StartSeconds, line.LineId);
                StringAssert.Contains(line.LineId, AssetDatabase.GetAssetPath(line.VoiceClip));
            }
        }
        Assert.AreEqual(17, lineCount);
    }

    [Test]
    public void SpeakerCatalog_UsesDistinctPortraitsAndProductionAriaIcon()
    {
        NarrativeSpeakerCatalog catalog = AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
        Assert.NotNull(catalog);
        Assert.AreEqual(5, catalog.Speakers.Count);
        Dictionary<NarrativeSpeakerId, NarrativeSpeakerRecord> byId = new();
        foreach (NarrativeSpeakerRecord speaker in catalog.Speakers)
            Assert.IsTrue(byId.TryAdd(speaker.SpeakerId, speaker), speaker.SpeakerId.ToString());
        Assert.AreNotSame(byId[NarrativeSpeakerId.Dalia].IdentitySprite, byId[NarrativeSpeakerId.Samira].IdentitySprite);
        Assert.AreEqual(FirstLaunchNarrativeDialogueAssetImporter.AriaIconPath,
            AssetDatabase.GetAssetPath(byId[NarrativeSpeakerId.Aria].IdentitySprite));
        Assert.AreEqual(NarrativeSpeakerTreatment.AriaIcon, byId[NarrativeSpeakerId.Aria].Treatment);
    }
}
