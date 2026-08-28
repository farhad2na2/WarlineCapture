using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Catalog.Contracts;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class M02EstablishBaseNarrativeTests
{
    private const string PassMarker =
        "[M02EstablishBaseNarrativeValidation] result=Passed tests=22";

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
            FinalSequencesBindReviewedPanelsAndEnglishVoice();
            PersianLocaleMatchesEveryFinalNarrativeLine();
            TutorialVoiceAssetsMatchEveryDisplayedInstruction();
            FinalComicDialogueRequiresItsPanelBinding();
            FinalComicDirectPanelPresentsImmediately();
            AuthoredComicDialogueStillRequiresItsPanel();
            SkipReducedMotionAndCaptionsAreSupported();
            M02NarrativeIdentityDoesNotBorrowM01OrFirstLaunchContent();
            MenuSceneCarriesAllM02NarrativeBindings();
            InteractiveBriefSelectsBriefSequenceStage();
            BriefHandoffAcknowledgesExactAttempt();
            WarningSelectsCommsOnlyBeforeActivation();
            ConsumedStagesDoNotRepeatWithinAttempt();
            RetryAttemptRearmsBriefAndComms();
            M01NeverSelectsM02Narrative();
            BriefAndCommsPauseWhileOnlyDebriefReturnsToCampaign();
            OpeningBriefClaimsEnteringMatchBeforeHudIsExposed();
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
        StringAssert.Contains("forward post is abandoned", transcript);
        StringAssert.Contains("Restore it", transcript);
        StringAssert.Contains("Build a Barracks", transcript);
        StringAssert.Contains("clinic road", transcript);
        StringAssert.Contains("route open", transcript);
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
        StringAssert.Contains("city access list", transcript);
        StringAssert.Contains("copied before the first strike", transcript);
        StringAssert.Contains("stole it before the attack", transcript);
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
        StringAssert.Contains("post is active again", transcript);
        StringAssert.Contains("Dalia Rahim", transcript);
        StringAssert.Contains("lead the ground response", transcript);
        StringAssert.Contains("warning network", transcript);
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
    public static void FinalSequencesBindReviewedPanelsAndEnglishVoice()
    {
        EnsureBuilt();
        M02EstablishBaseNarrativeVoiceImporter.ValidateImports();
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            NarrativeStateRecord dialogue = sequence.States[0];
            Assert.IsNotNull(dialogue.Panel16x9);
            Assert.AreSame(dialogue.Panel16x9, dialogue.Panel20x9);
            Assert.That(
                AssetDatabase.GetAssetPath(dialogue.Panel16x9),
                Does.StartWith(M02EstablishBaseNarrativeArtImporter.PanelRoot + "/"));
            Assert.IsFalse(dialogue.Panel16x9Reference?.RuntimeKeyIsValid() ?? false);
            Assert.IsFalse(dialogue.Panel20x9Reference?.RuntimeKeyIsValid() ?? false);
            foreach (NarrativeDialogueLineRecord line in dialogue.Lines)
            {
                Assert.IsNotEmpty(line.EnglishFallback);
                Assert.That(line.TextKey, Does.StartWith("narrative.m02."));
                Assert.IsNotNull(line.VoiceClip);
                Assert.That(
                    AssetDatabase.GetAssetPath(line.VoiceClip),
                    Does.StartWith(M02EstablishBaseNarrativeVoiceImporter.EnglishRoot + "/"));
                Assert.GreaterOrEqual(line.DeadlineSeconds - line.StartSeconds, line.VoiceClip.length);
                Assert.IsNull(line.FemaleVoiceClip);
                Assert.IsNull(line.NeutralVoiceClip);
            }
        }
    }

    [Test]
    public static void PersianLocaleMatchesEveryFinalNarrativeLine()
    {
        EnsureBuilt();
        VoiceManifest manifest = LoadVoiceManifest(
            "Assets/Game/Audio/Narrative/M02EstablishBase/m02_narrative_voice_manifest.json",
            expectedClipCount: M02EstablishBaseNarrativeVoiceImporter.ExpectedClipCount);
        NarrativeLocaleConfig locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(
            M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
        Assert.IsNotNull(locale);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Persian, locale.Language);
        Assert.IsTrue(locale.RightToLeft);
        Dictionary<string, NarrativeLocaleTextRecord> text = locale.Text
            .Where(entry => entry.Key.StartsWith("narrative.m02.", StringComparison.Ordinal))
            .ToDictionary(entry => entry.Key);
        Dictionary<string, NarrativeLocaleVoiceRecord> voices = locale.Voices
            .Where(entry => entry.LineId.StartsWith("m02-", StringComparison.Ordinal))
            .ToDictionary(entry => entry.LineId);

        M02NarrativeLocalizedLine[] expected = M02EstablishBaseNarrativeVoiceImporter.AllLines().ToArray();
        Assert.AreEqual(9, expected.Length);
        Assert.AreEqual(expected.Length, text.Count);
        Assert.AreEqual(expected.Length, voices.Count);
        foreach (M02NarrativeLocalizedLine line in expected)
        {
            Assert.AreEqual(line.Persian, text[line.TextKey].Value, line.TextKey);
            Assert.IsNotNull(voices[line.LineId].VoiceClip, line.LineId);
            Assert.AreEqual(
                M02EstablishBaseNarrativeVoiceImporter.GetPersianClipPath(line.LineId),
                AssetDatabase.GetAssetPath(voices[line.LineId].VoiceClip));
            AssertManifestClip(
                manifest,
                M02EstablishBaseNarrativeVoiceImporter.GetEnglishClipPath(line.LineId),
                line.Speaker.ToString().ToUpperInvariant(),
                "en-US",
                line.English);
            AssertManifestClip(
                manifest,
                M02EstablishBaseNarrativeVoiceImporter.GetPersianClipPath(line.LineId),
                line.Speaker.ToString().ToUpperInvariant(),
                "fa-IR",
                line.Persian);
        }
    }

    [Test]
    public static void TutorialVoiceAssetsMatchEveryDisplayedInstruction()
    {
        M02TutorialNarrationAudioImporter.ValidateImports();
        VoiceManifest manifest = LoadVoiceManifest(
            "Assets/Game/Audio/Voice/Tutorial/tutorial_m02_aria_voice_manifest.json",
            expectedClipCount: M02TutorialNarrationAudioImporter.StableClipPaths.Length);
        string catalog = File.ReadAllText(
            "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json");
        for (byte step = 2; step <= 8; step++)
        {
            Assert.IsTrue(M02EstablishBaseLocalizedText.TryGetTutorial(
                step, FirstLaunchNarrativeLanguage.English, out _, out string english));
            Assert.IsTrue(M02EstablishBaseLocalizedText.TryGetTutorial(
                step, FirstLaunchNarrativeLanguage.Persian, out _, out string persian));
            int index = step - 2;
            AssertManifestClip(
                manifest,
                M02TutorialNarrationAudioImporter.StableClipPaths[index],
                "ARIA",
                "en-US",
                english);
            AssertManifestClip(
                manifest,
                M02TutorialNarrationAudioImporter.StableClipPaths[index + 7],
                "ARIA",
                "fa-IR",
                persian);
        }
        Assert.AreEqual(14, Count(catalog, "VO.ARIA.Tutorial.M02."));
    }

    [Test]
    public static void FinalComicDialogueRequiresItsPanelBinding()
    {
        EnsureBuilt();
        foreach (NarrativeSequenceConfig sequence in Sequences())
        {
            NarrativeStateRecord dialogue = sequence.States[0];
            Assert.AreEqual(NarrativeStateKind.PanelDialogue, dialogue.Kind);
            Assert.IsTrue(dialogue.HasPanelBinding);
            Assert.IsTrue(FirstLaunchNarrativePanelPresentationSystemHelper.RequiresPanel(dialogue));
        }
    }

    [Test]
    public static void FinalComicDirectPanelPresentsImmediately()
    {
        EnsureBuilt();
        NarrativeStateRecord dialogue = Sequence(
            M02EstablishBaseNarrativeConfigBuilder.BriefSequenceId).States[0];
        GameObject root = new("M02DirectPanelPresentationTest", typeof(CanvasGroup));
        GameObject panelObject = new("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panelObject.transform.SetParent(root.transform, false);
        NarrativeSequenceView view = root.AddComponent<NarrativeSequenceView>();
        SerializedObject serializedView = new(view);
        serializedView.FindProperty("rootGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        serializedView.FindProperty("panelImage").objectReferenceValue =
            panelObject.GetComponent<UnityEngine.UI.Image>();
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        FirstLaunchNarrativePanelPresentationSystemHelper panels = new();
        panels.Initialize(
            view,
            new Dictionary<string, NarrativeStateRecord>(StringComparer.Ordinal)
            {
                [dialogue.StateId] = dialogue
            });

        try
        {
            Assert.IsTrue(panels.Present(dialogue, transitionToken: 1));
            Assert.AreSame(dialogue.Panel16x9, view.CurrentPanelSprite);
        }
        finally
        {
            panels.Clear();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public static void AuthoredComicDialogueStillRequiresItsPanel()
    {
        NarrativeSequenceConfig firstLaunch = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(
            "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSequence.asset");
        Assert.IsNotNull(firstLaunch);
        NarrativeStateRecord authoredPanel = firstLaunch.States.First(state => state.HasPanelBinding);
        Assert.AreEqual(NarrativeStateKind.PanelDialogue, authoredPanel.Kind);
        Assert.IsTrue(FirstLaunchNarrativePanelPresentationSystemHelper.RequiresPanel(authoredPanel));
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

    [Test]
    public static void InteractiveBriefSelectsBriefSequenceStage()
    {
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.InteractiveBrief);
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in runtime, default, briefConsumed: false, commsConsumed: false));
    }

    [Test]
    public static void BriefHandoffAcknowledgesExactAttempt()
    {
        using World world = new(nameof(BriefHandoffAcknowledgesExactAttempt));
        Entity root = world.EntityManager.CreateEntity(
            typeof(CampaignMissionRuntimeComponent), typeof(CampaignMissionAttemptFactsComponent));
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.InteractiveBrief);
        world.EntityManager.SetComponentData(root, runtime);
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
            ComponentType.ReadWrite<CampaignMissionAttemptFactsComponent>());

        Assert.IsFalse(CampaignMissionRuntimeProgressUtility.TryCompleteBrief(
            world.EntityManager, query, new FixedString64Bytes("wrong-session"), runtime.AttemptOrdinal));
        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryCompleteBrief(
            world.EntityManager, query, runtime.SessionToken, runtime.AttemptOrdinal));
        Assert.AreEqual(1, world.EntityManager
            .GetComponentData<CampaignMissionAttemptFactsComponent>(root).InteractiveBriefCompleted);
    }

    [Test]
    public static void WarningSelectsCommsOnlyBeforeActivation()
    {
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent warning = new() { DefenseWaveWarningIssued = 1 };
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in runtime, in warning, briefConsumed: true, commsConsumed: false));
        warning.DefenseWaveActivated = 1;
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in runtime, in warning, briefConsumed: true, commsConsumed: false));
    }

    [Test]
    public static void ConsumedStagesDoNotRepeatWithinAttempt()
    {
        CampaignMissionRuntimeComponent brief = Runtime(MissionPhaseKind.InteractiveBrief);
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in brief, default, briefConsumed: true, commsConsumed: false));
        CampaignMissionRuntimeComponent combat = Runtime(MissionPhaseKind.Engage);
        CampaignMissionAttemptFactsComponent warning = new() { DefenseWaveWarningIssued = 1 };
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in combat, in warning, briefConsumed: true, commsConsumed: true));
    }

    [Test]
    public static void RetryAttemptRearmsBriefAndComms()
    {
        CampaignMissionRuntimeComponent retry = Runtime(MissionPhaseKind.InteractiveBrief);
        retry.AttemptOrdinal = 2;
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in retry, default, briefConsumed: false, commsConsumed: false));
        retry.Phase = MissionPhaseKind.Engage;
        CampaignMissionAttemptFactsComponent warning = new() { DefenseWaveWarningIssued = 1 };
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in retry, in warning, briefConsumed: true, commsConsumed: false));
    }

    [Test]
    public static void M01NeverSelectsM02Narrative()
    {
        CampaignMissionRuntimeComponent runtime = Runtime(MissionPhaseKind.InteractiveBrief);
        runtime.MissionId = new Unity.Collections.FixedString64Bytes("saga.ch01.m01.first_contact");
        Assert.AreEqual(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(
                in runtime, default, briefConsumed: false, commsConsumed: false));
    }

    [Test]
    public static void BriefAndCommsPauseWhileOnlyDebriefReturnsToCampaign()
    {
        Assert.IsTrue(CampaignMissionDebriefCompositionSystemHelper.RequiresSimulationPause(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief));
        Assert.IsTrue(CampaignMissionDebriefCompositionSystemHelper.RequiresSimulationPause(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms));
        Assert.IsFalse(CampaignMissionDebriefCompositionSystemHelper.RequiresSimulationPause(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Debrief));
        Assert.IsFalse(CampaignMissionDebriefCompositionSystemHelper.ReturnsToCampaign(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief));
        Assert.IsFalse(CampaignMissionDebriefCompositionSystemHelper.ReturnsToCampaign(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms));
        Assert.IsTrue(CampaignMissionDebriefCompositionSystemHelper.ReturnsToCampaign(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Debrief));
    }

    [Test]
    public static void OpeningBriefClaimsEnteringMatchBeforeHudIsExposed()
    {
        UiShellStateComponent shell = new()
        {
            CurrentMode = UiShellMode.Loading,
            ActiveRoute = UIRoute.Match
        };
        Assert.IsFalse(CampaignMissionNarrativeCompositionUtility.IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief, in shell));
        shell.CurrentMode = UiShellMode.MatchHud;
        shell.IsTransitionRunning = 1;
        Assert.IsTrue(CampaignMissionNarrativeCompositionUtility.IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief, in shell));
        Assert.IsFalse(CampaignMissionNarrativeCompositionUtility.IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms, in shell));
        Assert.IsFalse(CampaignMissionNarrativeCompositionUtility.IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Debrief, in shell));
        shell.IsTransitionRunning = 0;
        Assert.IsTrue(CampaignMissionNarrativeCompositionUtility.IsPresentationReady(
            CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Comms, in shell));
    }

    private static CampaignMissionRuntimeComponent Runtime(MissionPhaseKind phase) => new()
    {
        MissionId = new Unity.Collections.FixedString64Bytes("saga.ch01.m02.establish_base"),
        SessionToken = new Unity.Collections.FixedString64Bytes("m02-narrative-attempt"),
        AttemptOrdinal = 1,
        Phase = phase
    };

    private static NarrativeSequenceConfig[] Sequences() =>
        AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
            .OfType<NarrativeSequenceConfig>()
            .ToArray();

    private static void EnsureBuilt() => M02EstablishBaseNarrativeConfigBuilder.Build();

    private static NarrativeSequenceConfig Sequence(string id) =>
        Sequences().Single(sequence => sequence.SequenceId == id);

    private static string Transcript(NarrativeSequenceConfig sequence) =>
        string.Join(" ", sequence.States.SelectMany(state => state.Lines).Select(line => line.EnglishFallback));

    private static int Count(string source, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static VoiceManifest LoadVoiceManifest(string path, int expectedClipCount)
    {
        VoiceManifest manifest = JsonUtility.FromJson<VoiceManifest>(File.ReadAllText(path));
        Assert.IsNotNull(manifest, path);
        Assert.AreEqual("ElevenLabs", manifest.provider, path);
        Assert.AreEqual("eleven_v3", manifest.model, path);
        Assert.AreEqual(M02EstablishBaseNarrativeVoiceImporter.RightsStatus, manifest.license, path);
        Assert.IsFalse(manifest.runtimeNetworkTts, path);
        Assert.IsNotNull(manifest.clips, path);
        Assert.AreEqual(expectedClipCount, manifest.clips.Length, path);
        CollectionAssert.AllItemsAreUnique(manifest.clips.Select(clip => clip.assetPath).ToArray());
        return manifest;
    }

    private static void AssertManifestClip(
        VoiceManifest manifest,
        string assetPath,
        string expectedSpeaker,
        string locale,
        string expectedText)
    {
        VoiceManifestClip clip = manifest.clips.Single(entry => entry.assetPath == assetPath);
        Assert.AreEqual(expectedSpeaker, clip.speaker, assetPath);
        Assert.AreEqual(locale, clip.locale, assetPath);
        Assert.AreEqual(expectedText, clip.text, assetPath);
        Assert.AreEqual(Sha256(assetPath), clip.sha256, assetPath);
    }

    private static string Sha256(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private static string Hash()
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(
                sha.ComputeHash(File.ReadAllBytes(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)))
            .Replace("-", string.Empty);
    }

    [Serializable]
    private sealed class VoiceManifest
    {
        public string provider;
        public string license;
        public bool runtimeNetworkTts;
        public string model;
        public VoiceManifestClip[] clips;
    }

    [Serializable]
    private sealed class VoiceManifestClip
    {
        public string speaker;
        public string locale;
        public string text;
        public string assetPath;
        public string sha256;
    }
}
