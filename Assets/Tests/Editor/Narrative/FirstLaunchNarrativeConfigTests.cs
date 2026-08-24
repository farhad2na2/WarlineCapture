using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
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
            tests.SequenceConfig_AuthorsAudioRouteAndCompletionPolicy();
            tests.LocationIntro_Uses10AmBazaarTimeInEnglishAndPersian();
            tests.PersianLocale_UsesNaturalCommunicationsOutageText();
            tests.DialogueLines_HaveStableKeysSpeakersTimingAndVoiceClips();
            tests.SpeakerCatalog_UsesDistinctPortraitsAndProductionAriaIcon();
            tests.SequenceConfig_DoesNotDirectlyRetainPanelTextures();
            Debug.Log("[FirstLaunchNarrativeConfigValidation] result=Passed tests=7 states=26 panels=22 lines=17 speakers=5");
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
    public void SequenceConfig_AuthorsAudioRouteAndCompletionPolicy()
    {
        NarrativeSequenceConfig config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(
            FirstLaunchNarrativeConfigBuilder.SequencePath);
        Dictionary<string, NarrativeStateRecord> states = new(StringComparer.Ordinal);
        foreach (NarrativeStateRecord state in config.States)
        {
            Assert.IsTrue(states.TryAdd(state.StateId, state), state.StateId);
            Assert.NotNull(state.EvidenceIds, state.StateId);
            Assert.NotNull(state.MissionContextFlags, state.StateId);
        }

        Assert.AreEqual(NarrativeMusicCue.Briefing, states["FL-P01"].MusicCue);
        Assert.AreEqual(NarrativeAmbienceCue.CityDay, states["FL-P01"].AmbienceCue);
        Assert.AreEqual(NarrativeMusicCue.Conflict, states["FL-P02"].MusicCue);
        Assert.AreEqual(NarrativeAmbienceCue.Battlefield, states["FL-P02"].AmbienceCue);
        Assert.AreEqual(NarrativeEventCue.Attack, states["FL-P02"].EventCue);
        Assert.AreEqual(NarrativeVehicleCue.Engine, states["FL-P04"].VehicleCue);
        Assert.AreEqual(NarrativeEventCue.Radio, states["FL-P04"].EventCue);

        NarrativeStateRecord handoff = states["first_launch.m01_handoff"];
        Assert.AreEqual(NarrativeRouteRole.MissionHandoff, handoff.RouteRole);
        Assert.AreEqual("first_launch.m01_handoff_completion", handoff.CompletionPayloadId);
        Assert.AreEqual("first_launch.gameplay_placeholder", handoff.ContinueStateId);
        Assert.AreEqual(
            NarrativeRouteRole.ReviewerGameplay,
            states["first_launch.gameplay_placeholder"].RouteRole);
        Assert.AreEqual(NarrativeRouteRole.DebriefOpening, states["FL-P19"].RouteRole);

        NarrativeStateRecord arrival = states["first_launch.command_base_reveal"];
        Assert.AreEqual(NarrativeRouteRole.DebriefArrival, arrival.RouteRole);
        Assert.AreEqual("first_launch.m01_debrief_completion", arrival.CompletionPayloadId);
        CollectionAssert.Contains(arrival.EvidenceIds, "evidence.aria.revoked_credential_fragment");
        CollectionAssert.Contains(arrival.MissionContextFlags, "story.aria.revoked_credential_clue_found");
    }

    [Test]
    public void LocationIntro_Uses10AmBazaarTimeInEnglishAndPersian()
    {
        FirstLaunchNarrativeConfigBuilder.Build();
        NarrativeSequenceConfig config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(
            FirstLaunchNarrativeConfigBuilder.SequencePath);
        Assert.NotNull(config);
        NarrativeStateRecord opening = null;
        foreach (NarrativeStateRecord state in config.States)
        {
            if (state.StateId == "FL-P01")
            {
                opening = state;
                break;
            }
        }
        Assert.NotNull(opening);
        Assert.AreEqual("OLD MARKET / 10:00 LOCAL", opening.LocationSubtitleFallback);

        NarrativeLocaleConfig persian = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(
            FirstLaunchNarrativeConfigBuilder.PersianLocalePath);
        Assert.NotNull(persian);
        NarrativeLocaleTextRecord localizedTime = null;
        foreach (NarrativeLocaleTextRecord entry in persian.Text)
        {
            if (entry.Key == "narrative.first_launch.location.old_market.context")
            {
                localizedTime = entry;
                break;
            }
        }

        Assert.NotNull(localizedTime);
        Assert.AreEqual("بازار قدیم / ساعت ۱۰:۰۰ محلی", localizedTime.Value);
    }

    [Test]
    public void PersianLocale_UsesNaturalCommunicationsOutageText()
    {
        FirstLaunchNarrativeConfigBuilder.Build();
        NarrativeLocaleConfig persian = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(
            FirstLaunchNarrativeConfigBuilder.PersianLocalePath);
        Assert.NotNull(persian);

        NarrativeLocaleTextRecord dispatchLine = null;
        NarrativeLocaleTextRecord ariaLine = null;
        foreach (NarrativeLocaleTextRecord entry in persian.Text)
        {
            if (entry.Key == "narrative.first_launch.line.p03_radio")
            {
                dispatchLine = entry;
            }
            else if (entry.Key == "narrative.first_launch.line.p05_aria")
            {
                ariaLine = entry;
            }
        }

        Assert.NotNull(dispatchLine);
        Assert.AreEqual(
            "فرماندهی واکنش مشترک، اینجا مرکز اعزام منطقه است. صدای ما را دارید؟ سامانهٔ ارتباطی منطقه از کار افتاده و ارتباط با فرماندهی قطع شده.",
            dispatchLine.Value);
        StringAssert.DoesNotContain("رله", dispatchLine.Value);

        Assert.NotNull(ariaLine);
        Assert.AreEqual(
            "من آریا هستم، دستیار ارتباط شهری. زیرساخت ارتباطی آسیب دیده و ارتباط با فرماندهی هنوز برقرار نشده است.",
            ariaLine.Value);
        StringAssert.DoesNotContain("رله", ariaLine.Value);
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
                if (line.Speaker == NarrativeSpeakerId.Commander)
                {
                    Assert.NotNull(line.FemaleVoiceClip, line.LineId);
                    Assert.NotNull(line.NeutralVoiceClip, line.LineId);
                    StringAssert.EndsWith("p14_commander_female.wav", AssetDatabase.GetAssetPath(line.FemaleVoiceClip));
                    StringAssert.EndsWith("p14_commander_neutral.wav", AssetDatabase.GetAssetPath(line.NeutralVoiceClip));
                }
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
        Assert.AreEqual(FirstLaunchNarrativeDialogueAssetImporter.AriaPortraitPath,
            AssetDatabase.GetAssetPath(byId[NarrativeSpeakerId.Aria].IdentitySprite));
        Assert.AreEqual(FirstLaunchNarrativeDialogueAssetImporter.RadioPortraitPath,
            AssetDatabase.GetAssetPath(byId[NarrativeSpeakerId.Radio].IdentitySprite));
        Assert.NotNull(byId[NarrativeSpeakerId.Commander].IdentitySprite);
        Assert.AreEqual("commander_07_faceless", byId[NarrativeSpeakerId.Commander].IdentitySprite.name);
        Assert.AreEqual(NarrativeSpeakerTreatment.AriaIcon, byId[NarrativeSpeakerId.Aria].Treatment);
    }
}
