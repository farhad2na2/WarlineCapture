using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Composition;
using Game.Editor;
using Game.Narrative.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class M02EstablishBaseNarrativeTests
{
    private const string PassMarker =
        "[M02EstablishBaseNarrativeValidation] result=Passed tests=9";

    [MenuItem("Game/Validation/Run M02 Establish Base Narrative Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseNarrativeConfigBuilder.BuildAndInstall();
            BuildIsDeterministicAndContainsThreeSequences();
            MissionReferencesAllThreeSequences();
            BriefEstablishesPostDirectionAndCivicPurpose();
            CommsRecoverPreAttackMunicipalAccessList();
            DebriefClosesPostDaliaAndM03WarningSectorBeats();
            ProvisionalSequencesContainFallbackTextWithoutFinalMedia();
            SkipReducedMotionAndCaptionsAreSupported();
            M02NarrativeIdentityDoesNotBorrowM01OrFirstLaunchContent();
            MenuSceneCarriesAllM02NarrativeBindings();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[M02EstablishBaseNarrativeValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public static void BuildIsDeterministicAndContainsThreeSequences()
    {
        M02EstablishBaseNarrativeConfigBuilder.Build();
        string first = Hash();
        M02EstablishBaseNarrativeConfigBuilder.Build();
        Assert.AreEqual(first, Hash());
        CollectionAssert.AreEquivalent(
            new[]
            {
                M02EstablishBaseNarrativeConfigBuilder.BriefSequenceId,
                M02EstablishBaseNarrativeConfigBuilder.CommsSequenceId,
                M02EstablishBaseNarrativeConfigBuilder.DebriefSequenceId
            },
            Sequences().Select(sequence => sequence.SequenceId));
    }

    [Test]
    public static void MissionReferencesAllThreeSequences()
    {
        EnsureBuilt();
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        Assert.IsNotNull(mission);
        Assert.AreEqual(M02EstablishBaseNarrativeConfigBuilder.BriefSequenceId, mission.BriefingSequenceId);
        Assert.AreEqual(M02EstablishBaseNarrativeConfigBuilder.CommsSequenceId, mission.CommsSequenceId);
        Assert.AreEqual(M02EstablishBaseNarrativeConfigBuilder.DebriefSequenceId, mission.DebriefSequenceId);
    }

    [Test]
    public static void BriefEstablishesPostDirectionAndCivicPurpose()
    {
        EnsureBuilt();
        NarrativeSequenceConfig brief = Sequence(M02EstablishBaseNarrativeConfigBuilder.BriefSequenceId);
        string transcript = Transcript(brief);
        StringAssert.Contains("abandoned JRC forward post", transcript);
        StringAssert.Contains("Restore it", transcript);
        StringAssert.Contains("defense lane", transcript);
        StringAssert.Contains("clinic route", transcript);
        StringAssert.Contains("civic lifeline", transcript);
        Assert.AreEqual(NarrativeSpeakerId.Dalia, brief.States[0].Lines[0].Speaker);
        CollectionAssert.Contains(brief.States[1].MissionContextFlags, "story.m02.forward_post_civic_purpose");
        Assert.AreEqual("request.m02.interactive_brief.complete", brief.States[1].CompletionPayloadId);
    }

    [Test]
    public static void CommsRecoverPreAttackMunicipalAccessList()
    {
        EnsureBuilt();
        NarrativeSequenceConfig comms = Sequence(M02EstablishBaseNarrativeConfigBuilder.CommsSequenceId);
        string transcript = Transcript(comms);
        StringAssert.Contains("municipal access list", transcript);
        StringAssert.Contains("predates the first strike", transcript);
        StringAssert.Contains("stolen before the attack", transcript);
        Assert.AreEqual(NarrativeSpeakerId.Aria, comms.States[0].Lines[1].Speaker);
        Assert.AreEqual(NarrativeSpeakerId.Samira, comms.States[0].Lines[2].Speaker);
        CollectionAssert.Contains(comms.States[1].EvidenceIds, "evidence.m02.municipal_access_list");
        CollectionAssert.Contains(
            comms.States[1].MissionContextFlags,
            "story.m02.access_list_stolen_before_attack");
        Assert.AreEqual(NarrativeStateKind.RouteHandoff, comms.States[1].Kind);
        Assert.AreEqual("request.m02.comms.complete", comms.States[1].CompletionPayloadId);
    }

    [Test]
    public static void DebriefClosesPostDaliaAndM03WarningSectorBeats()
    {
        EnsureBuilt();
        NarrativeSequenceConfig debrief = Sequence(M02EstablishBaseNarrativeConfigBuilder.DebriefSequenceId);
        NarrativeStateRecord route = debrief.States[1];
        string transcript = Transcript(debrief);
        StringAssert.Contains("forward post is operational", transcript);
        StringAssert.Contains("Dalia Rahim", transcript);
        StringAssert.Contains("accept the field-lead role", transcript);
        StringAssert.Contains("warning sector", transcript);
        StringAssert.Contains("gone dark", transcript);
        Assert.AreEqual(NarrativeSpeakerId.Dalia, debrief.States[0].Lines[1].Speaker);
        Assert.AreEqual(NarrativeRouteRole.DebriefArrival, route.RouteRole);
        CollectionAssert.Contains(route.MissionContextFlags, "story.m02.forward_post_operational");
        CollectionAssert.Contains(route.MissionContextFlags, "story.dalia.field_lead_accepted");
        CollectionAssert.Contains(route.MissionContextFlags, "story.m02.warning_sector_dark");
        CollectionAssert.Contains(
            route.MissionContextFlags,
            "campaign.highlight." + M02EstablishBaseNarrativeConfigBuilder.M03MissionId);
        Assert.AreEqual("request.m02.debrief.complete", route.CompletionPayloadId);
    }

    [Test]
    public static void ProvisionalSequencesContainFallbackTextWithoutFinalMedia()
    {
        EnsureBuilt();
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            NarrativeStateRecord dialogue = sequence.States[0];
            Assert.IsNull(dialogue.Panel16x9);
            Assert.IsNull(dialogue.Panel20x9);
            Assert.IsFalse(dialogue.Panel16x9Reference?.RuntimeKeyIsValid() ?? false);
            Assert.IsFalse(dialogue.Panel20x9Reference?.RuntimeKeyIsValid() ?? false);
            foreach (NarrativeDialogueLineRecord line in dialogue.Lines)
            {
                Assert.IsNotEmpty(line.EnglishFallback);
                Assert.That(line.TextKey, Does.StartWith("narrative.m02."));
                Assert.IsNull(line.VoiceClip);
                Assert.IsNull(line.FemaleVoiceClip);
                Assert.IsNull(line.NeutralVoiceClip);
            }
        }
    }

    [Test]
    public static void SkipReducedMotionAndCaptionsAreSupported()
    {
        EnsureBuilt();
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            Assert.AreEqual(sequence.States[1].StateId, sequence.DefaultSkipDestinationId);
            Assert.IsTrue(sequence.States.All(state => state.ReducedMotionSupported));
            Assert.IsTrue(sequence.States[0].Lines.All(line => line.EssentialCaption));
            CollectionAssert.Contains(sequence.States[1].EvidenceIds, "story_archive." + sequence.SequenceId);
        }
    }

    [Test]
    public static void M02NarrativeIdentityDoesNotBorrowM01OrFirstLaunchContent()
    {
        EnsureBuilt();
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            Assert.That(sequence.SequenceId, Does.StartWith("seq.ch01.m02."));
            Assert.IsFalse(sequence.EntryStateId.StartsWith("FL-", StringComparison.Ordinal));
            foreach (NarrativeDialogueLineRecord line in sequence.States.SelectMany(state => state.Lines))
            {
                Assert.That(line.TextKey, Does.StartWith("narrative.m02."));
                Assert.IsFalse(line.TextKey.Contains("m01", StringComparison.OrdinalIgnoreCase));
                Assert.IsFalse(line.TextKey.Contains("first_launch", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Test]
    public static void MenuSceneCarriesAllM02NarrativeBindings()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Single);
        MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(
            FindObjectsInactive.Include);
        Assert.IsNotNull(bootstrap, scene.path);
        Assert.AreEqual(3, bootstrap.CampaignMissionNarrativeConfigs.Length);
        Assert.IsFalse(bootstrap.CampaignMissionNarrativeConfigs.Any(sequence => sequence == null));
        CollectionAssert.IsSubsetOf(
            new[]
            {
                M02EstablishBaseNarrativeConfigBuilder.BriefSequenceId,
                M02EstablishBaseNarrativeConfigBuilder.CommsSequenceId,
                M02EstablishBaseNarrativeConfigBuilder.DebriefSequenceId
            },
            bootstrap.CampaignMissionNarrativeConfigs.Select(sequence => sequence.SequenceId).ToArray());
    }

    private static NarrativeSequenceConfig[] Sequences() =>
        AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
            .OfType<NarrativeSequenceConfig>()
            .ToArray();

    private static void EnsureBuilt() => M02EstablishBaseNarrativeConfigBuilder.Build();

    private static NarrativeSequenceConfig Sequence(string id) =>
        Sequences().Single(sequence => sequence.SequenceId == id);

    private static string Transcript(NarrativeSequenceConfig sequence) =>
        string.Join(" ", sequence.States.SelectMany(state => state.Lines).Select(line => line.EnglishFallback));

    private static string Hash()
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(
                sha.ComputeHash(File.ReadAllBytes(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)))
            .Replace("-", string.Empty);
    }
}
