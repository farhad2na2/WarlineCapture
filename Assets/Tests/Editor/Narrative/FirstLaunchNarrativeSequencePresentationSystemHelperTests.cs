using System;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class FirstLaunchNarrativeSequencePresentationSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativeSequencePresentationSystemHelperTests tests = new();
            tests.Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup();
            tests.Player_EmitsInteractiveSkipAndTypedHandoffOnce();
            tests.Player_PauseStepRestartAndCancelAreDeterministic();
            tests.Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload();
            tests.Player_AutoAdvanceHonorsAuthoredPanelAndLineTiming();
            tests.Player_UsesSelectedCommanderPortraitAndMatchingVoice();
            Debug.Log("[FirstLaunchNarrativeSequencePresentationSystemHelperValidation] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativeSequencePresentationSystemHelperValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup()
    {
        TestContext context = CreateContext();
        Assert.IsTrue(context.SequencePresentation.Start());
        Assert.AreEqual("FL-P01", context.SequencePresentation.CurrentStateId);
        context.SequencePresentation.Tick(15f);
        Assert.AreEqual("FL-P02", context.SequencePresentation.CurrentStateId);

        Button input = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "InputSurface");
        Assert.NotNull(input);
        context.SequencePresentation.Tick(0.31f);
        input.onClick.Invoke();
        Assert.AreEqual(NarrativeDialoguePhase.AdvanceReady, context.View.DialogueView.Phase);
        input.onClick.Invoke();
        Assert.AreEqual("FL-P03", context.SequencePresentation.CurrentStateId);
        context.Dispose();
    }

    [Test]
    public void Player_EmitsInteractiveSkipAndTypedHandoffOnce()
    {
        TestContext context = CreateContext();
        int interactiveCount = 0;
        int skipCount = 0;
        int handoffCount = 0;
        context.SequencePresentation.InteractiveStateRequested += _ => interactiveCount++;
        context.SequencePresentation.SkipRequested += request =>
        {
            Assert.AreEqual("first_launch.m01_handoff", request.DestinationId);
            Assert.AreEqual(NarrativeRouteRole.MissionHandoff, request.RouteRole);
            skipCount++;
        };
        context.SequencePresentation.HandoffRequested += result =>
        {
            Assert.AreEqual("first_launch.m01_handoff", result.DestinationId);
            handoffCount++;
        };

        Assert.IsTrue(context.SequencePresentation.StartAt("first_launch.commander_identity"));
        Assert.AreEqual(1, interactiveCount);
        context.SequencePresentation.CommitInteractiveState("wrong-state");
        Assert.AreEqual("first_launch.commander_identity", context.SequencePresentation.CurrentStateId);

        Assert.IsTrue(context.SequencePresentation.StartAt("FL-P02"));
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Assert.NotNull(skip);
        skip.onClick.Invoke();
        Assert.AreEqual(1, skipCount);

        Assert.IsTrue(context.SequencePresentation.StartAt("first_launch.m01_handoff"));
        Assert.AreEqual(1, handoffCount);
        Assert.IsFalse(context.SequencePresentation.IsRunning);
        context.Dispose();
    }

    [Test]
    public void Player_PauseStepRestartAndCancelAreDeterministic()
    {
        TestContext context = CreateContext();
        context.SequencePresentation.Start();
        context.SequencePresentation.Pause();
        context.SequencePresentation.Tick(100f);
        Assert.AreEqual("FL-P01", context.SequencePresentation.CurrentStateId);
        context.SequencePresentation.Resume();
        Assert.IsTrue(context.SequencePresentation.NextState());
        Assert.AreEqual("FL-P02", context.SequencePresentation.CurrentStateId);
        Assert.IsTrue(context.SequencePresentation.PreviousState());
        Assert.AreEqual("FL-P01", context.SequencePresentation.CurrentStateId);
        Assert.IsTrue(context.SequencePresentation.Restart());
        Assert.AreEqual("FL-P01", context.SequencePresentation.CurrentStateId);
        context.SequencePresentation.Cancel();
        Assert.IsFalse(context.SequencePresentation.IsRunning);
        Assert.AreEqual(string.Empty, context.SequencePresentation.CurrentStateId);
        context.Dispose();
    }

    [Test]
    public void Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload()
    {
        TestContext context = CreateContext();
        NarrativeHandoffResult result = default;
        int count = 0;
        context.SequencePresentation.HandoffRequested += value => { result = value; count++; };

        Assert.IsTrue(context.SequencePresentation.StartAt("first_launch.command_base_reveal"));
        Assert.AreEqual(1, count);
        Assert.AreEqual("first_launch.m01_debrief_completion", result.Completion.PayloadId);
        Assert.IsTrue(result.Completion.Watched);
        Assert.IsFalse(result.Completion.Skipped);
        CollectionAssert.Contains(result.Completion.EvidenceIds, "evidence.aria.revoked_credential_fragment");

        NarrativeCompletionPayload skipped = context.SequencePresentation.CreateCompletion(
            "first_launch.command_base_reveal",
            true);
        Assert.IsTrue(context.SequencePresentation.StartAt("first_launch.command_base_reveal", skipped));
        Assert.AreEqual(2, count);
        Assert.IsFalse(result.Completion.Watched);
        Assert.IsTrue(result.Completion.Skipped);
        CollectionAssert.Contains(result.Completion.EvidenceIds, "evidence.aria.revoked_credential_fragment");
        CollectionAssert.Contains(result.Completion.MissionContextFlags, "story.aria.revoked_credential_clue_found");
        context.Dispose();
    }

    [Test]
    public void Player_AutoAdvanceHonorsAuthoredPanelAndLineTiming()
    {
        TestContext context = CreateContext();
        Assert.IsTrue(context.SequencePresentation.StartAt("FL-P02"));
        Assert.AreEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);

        context.SequencePresentation.Tick(0.29f);
        Assert.AreEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);
        context.SequencePresentation.Tick(0.02f);
        Assert.AreNotEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);

        context.SequencePresentation.Tick(7.68f);
        Assert.AreEqual("FL-P02", context.SequencePresentation.CurrentStateId);
        context.SequencePresentation.Tick(0.02f);
        Assert.AreEqual("FL-P03", context.SequencePresentation.CurrentStateId);

        Assert.IsTrue(context.SequencePresentation.StartAt("FL-P04"));
        context.SequencePresentation.Tick(0.51f);
        Assert.AreEqual(0, context.SequencePresentation.CurrentLineIndex);
        context.SequencePresentation.Tick(11.98f);
        Assert.AreEqual(0, context.SequencePresentation.CurrentLineIndex);
        context.SequencePresentation.Tick(0.02f);
        Assert.AreEqual(1, context.SequencePresentation.CurrentLineIndex);
        context.Dispose();
    }

    [Test]
    public void Player_UsesSelectedCommanderPortraitAndMatchingVoice()
    {
        AssertCommanderPresentation(0, "p14_commander_female");
        AssertCommanderPresentation(1, "p14_commander");
        AssertCommanderPresentation(6, "p14_commander_neutral");
    }

    private static void AssertCommanderPresentation(int portraitIndex, string expectedClipName)
    {
        TestContext context = CreateContext();
        context.SequencePresentation.ApplyCommanderIdentity(new NarrativeCommanderIdentityData
        {
            Callsign = "NOMAD",
            DisplayName = "TEST COMMANDER"
        }, portraitIndex);

        Sprite selectedPortrait = context.View.CommanderIdentityView.SelectedPortrait;
        Assert.NotNull(selectedPortrait, $"Commander portrait {portraitIndex}");
        Assert.IsTrue(context.SequencePresentation.StartAt("FL-P14"));
        context.SequencePresentation.Tick(0.51f);

        Assert.AreSame(selectedPortrait, context.View.DialogueView.CurrentPortraitSprite);
        Assert.IsTrue(context.View.DialogueView.IsPortraitVisible);
        Assert.NotNull(context.View.VoiceSource.clip);
        Assert.AreEqual(expectedClipName, context.View.VoiceSource.clip.name);
        context.Dispose();
    }

    private static TestContext CreateContext()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab, "Build the FirstLaunch presentation prefab before running sequencePresentation tests.");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
        FirstLaunchNarrativeSequencePresentationSystemHelper sequencePresentation = new();
        NarrativeSequenceConfig sequence = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        Assert.NotNull(sequence, "Build the FirstLaunch sequence config before running sequencePresentation tests.");
        Assert.Greater(sequence.States.Count, 0, "FirstLaunch sequence config must be serialized before sequencePresentation tests.");
        Assert.IsTrue(sequencePresentation.Initialize(
            sequence,
            AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
            AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),
            view,
            FallbackGameTextResolver.Instance,
            Game.UI.Runtime.SettingsService.Defaults));
        return new TestContext(instance, view, sequencePresentation);
    }

    private readonly struct TestContext
    {
        public readonly GameObject Instance;
        public readonly NarrativeSequenceView View;
        public readonly FirstLaunchNarrativeSequencePresentationSystemHelper SequencePresentation;
        public TestContext(GameObject instance, NarrativeSequenceView view, FirstLaunchNarrativeSequencePresentationSystemHelper sequencePresentation) { Instance = instance; View = view; SequencePresentation = sequencePresentation; }
        public void Dispose() { SequencePresentation.Cancel(); UnityEngine.Object.DestroyImmediate(Instance); }
    }
}
