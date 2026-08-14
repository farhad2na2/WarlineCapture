using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class M01FirstContactNarrativeTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            BuildIsDeterministicAndContainsThreeSequences(); MissionReferencesAllThreeSequences();
            BriefContinuesExactThreatProfiles(); CommsRemainMissionScopedAndTyped();
            DebriefIsFragmentaryAndHighlightsCanonicalM02(); SkipReducedMotionAndCaptionsAreSupported();
            ReplayCannotEnterFirstLaunchColdOpen(); RuntimeRejectsStaleInputAfterRestart();
            Debug.Log("[M01FirstContactNarrativeValidation] result=Passed tests=8"); ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception); Debug.LogError("[M01FirstContactNarrativeValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test] public static void BuildIsDeterministicAndContainsThreeSequences()
    {
        M01FirstContactNarrativeConfigBuilder.Build(); string first = Hash();
        M01FirstContactNarrativeConfigBuilder.Build(); Assert.AreEqual(first, Hash());
        CollectionAssert.AreEquivalent(new[] { M01FirstContactNarrativeConfigBuilder.BriefSequenceId,
            M01FirstContactNarrativeConfigBuilder.CommsSequenceId, M01FirstContactNarrativeConfigBuilder.DebriefSequenceId },
            Sequences().Select(value => value.SequenceId));
    }

    [Test] public static void MissionReferencesAllThreeSequences()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath);
        Assert.AreEqual(M01FirstContactNarrativeConfigBuilder.BriefSequenceId, mission.BriefingSequenceId);
        Assert.AreEqual(M01FirstContactNarrativeConfigBuilder.CommsSequenceId, mission.CommsSequenceId);
        Assert.AreEqual(M01FirstContactNarrativeConfigBuilder.DebriefSequenceId, mission.DebriefSequenceId);
    }

    [Test] public static void BriefContinuesExactThreatProfiles()
    {
        NarrativeSequenceConfig brief = Sequence(M01FirstContactNarrativeConfigBuilder.BriefSequenceId);
        string transcript = Transcript(brief); StringAssert.Contains("Courier", transcript);
        StringAssert.Contains("Warden", transcript); StringAssert.Contains("Broker", transcript);
        CollectionAssert.Contains(brief.States[1].EvidenceIds, "evidence.first_launch.ash_patrol_profiles");
        Assert.AreEqual("request.m01.interactive_brief.complete", brief.States[1].CompletionPayloadId);
    }

    [Test] public static void CommsRemainMissionScopedAndTyped()
    {
        NarrativeSequenceConfig comms = Sequence(M01FirstContactNarrativeConfigBuilder.CommsSequenceId);
        Assert.AreEqual(NarrativeStateKind.RouteHandoff, comms.States[1].Kind);
        Assert.AreEqual(NarrativeRouteRole.None, comms.States[1].RouteRole);
        Assert.AreEqual("request.m01.comms.complete", comms.States[1].CompletionPayloadId);
        StringAssert.Contains("verified mission state", Transcript(comms));
    }

    [Test] public static void DebriefIsFragmentaryAndHighlightsCanonicalM02()
    {
        NarrativeSequenceConfig debrief = Sequence(M01FirstContactNarrativeConfigBuilder.DebriefSequenceId);
        NarrativeStateRecord route = debrief.States[1]; string transcript = Transcript(debrief);
        StringAssert.Contains("fragment of a revoked civic-relay credential", transcript);
        foreach (string forbidden in new[] { "qassem", "male_05", "heavy gunner", "complete protocol" })
            Assert.IsFalse(transcript.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(NarrativeRouteRole.DebriefArrival, route.RouteRole);
        CollectionAssert.Contains(route.MissionContextFlags, "campaign.highlight." + M01FirstContactNarrativeConfigBuilder.M02MissionId);
        CollectionAssert.Contains(route.EvidenceIds, "evidence.aria.revoked_credential_fragment");
    }

    [Test] public static void SkipReducedMotionAndCaptionsAreSupported()
    {
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            Assert.AreEqual(sequence.States[1].StateId, sequence.DefaultSkipDestinationId);
            Assert.IsTrue(sequence.States.All(state => state.ReducedMotionSupported));
            Assert.IsTrue(sequence.States[0].Lines.All(line => line.EssentialCaption));
            CollectionAssert.Contains(sequence.States[1].EvidenceIds, "story_archive." + sequence.SequenceId);
        }
    }

    [Test] public static void ReplayCannotEnterFirstLaunchColdOpen()
    {
        string serialized = File.ReadAllText(M01FirstContactNarrativeConfigBuilder.NarrativePath);
        Assert.IsFalse(serialized.Contains("seq.first_launch", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(Sequences().SelectMany(value => value.States).Any(state => state.StateId.StartsWith("FL-", StringComparison.Ordinal)));
    }

    [Test] public static void RuntimeRejectsStaleInputAfterRestart()
    {
        NarrativeSequenceConfig brief = Sequence(M01FirstContactNarrativeConfigBuilder.BriefSequenceId);
        FirstLaunchNarrativeSequenceUtilitySystemHelper runtime = new();
        Assert.IsTrue(runtime.Configure(brief.EntryStateId, brief.States.Select(state => new FirstLaunchNarrativeSequenceStateDefinition(
            state.StateId, state.Kind, state.ContinueStateId, state.SkipStateId, state.DurationSeconds,
            state.Lines.Select(line => line.StartSeconds).ToArray())).ToArray()));
        runtime.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.Start));
        ulong stale = runtime.TransitionToken; runtime.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.Restart));
        Assert.Greater(runtime.TransitionToken, stale);
        Assert.IsFalse(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Skip, runtime.CurrentStateId, stale)));
        Assert.IsTrue(runtime.Apply(new FirstLaunchNarrativeSequenceIntent(
            FirstLaunchNarrativeSequenceIntentKind.Skip, runtime.CurrentStateId, runtime.TransitionToken)));
    }

    private static NarrativeSequenceConfig[] Sequences() => AssetDatabase.LoadAllAssetsAtPath(
        M01FirstContactNarrativeConfigBuilder.NarrativePath).OfType<NarrativeSequenceConfig>().ToArray();
    private static NarrativeSequenceConfig Sequence(string id) => Sequences().Single(value => value.SequenceId == id);
    private static string Transcript(NarrativeSequenceConfig sequence) => string.Join(" ",
        sequence.States.SelectMany(state => state.Lines).Select(line => line.EnglishFallback));
    private static string Hash()
    { using SHA256 sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(
        M01FirstContactNarrativeConfigBuilder.NarrativePath))).Replace("-", string.Empty); }
}
