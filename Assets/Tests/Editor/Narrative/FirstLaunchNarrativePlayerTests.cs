using System;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class FirstLaunchNarrativePlayerTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchNarrativePlayerTests tests = new();
            tests.Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup();
            tests.Player_EmitsInteractiveSkipAndTypedHandoffOnce();
            tests.Player_PauseStepRestartAndCancelAreDeterministic();
            tests.Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload();
            tests.Player_AutoAdvanceHonorsAuthoredPanelAndLineTiming();
            Debug.Log("[FirstLaunchNarrativePlayerValidation] result=Passed tests=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchNarrativePlayerValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void Player_AdvancesStaticAndDialogueStatesWithoutHierarchyLookup()
    {
        TestContext context = CreateContext();
        Assert.IsTrue(context.Player.Start());
        Assert.AreEqual("FL-P01", context.Player.CurrentStateId);
        context.Player.Tick(15f);
        Assert.AreEqual("FL-P02", context.Player.CurrentStateId);

        Button input = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "InputSurface");
        Assert.NotNull(input);
        context.Player.Tick(0.31f);
        input.onClick.Invoke();
        Assert.AreEqual(NarrativeDialoguePhase.AdvanceReady, context.View.DialogueView.Phase);
        input.onClick.Invoke();
        Assert.AreEqual("FL-P03", context.Player.CurrentStateId);
        context.Dispose();
    }

    [Test]
    public void Player_EmitsInteractiveSkipAndTypedHandoffOnce()
    {
        TestContext context = CreateContext();
        int interactiveCount = 0;
        int skipCount = 0;
        int handoffCount = 0;
        context.Player.InteractiveStateRequested += _ => interactiveCount++;
        context.Player.SkipRequested += destination =>
        {
            Assert.AreEqual("first_launch.m01_handoff", destination);
            skipCount++;
        };
        context.Player.HandoffRequested += result =>
        {
            Assert.AreEqual("first_launch.m01_handoff", result.DestinationId);
            handoffCount++;
        };

        Assert.IsTrue(context.Player.StartAt("first_launch.commander_identity"));
        Assert.AreEqual(1, interactiveCount);
        context.Player.CommitInteractiveState("wrong-state");
        Assert.AreEqual("first_launch.commander_identity", context.Player.CurrentStateId);

        Assert.IsTrue(context.Player.StartAt("FL-P02"));
        Button skip = Array.Find(context.Instance.GetComponentsInChildren<Button>(true), button => button.name == "SkipButton");
        Assert.NotNull(skip);
        skip.onClick.Invoke();
        Assert.AreEqual(1, skipCount);

        Assert.IsTrue(context.Player.StartAt("first_launch.m01_handoff"));
        Assert.AreEqual(1, handoffCount);
        Assert.IsFalse(context.Player.IsRunning);
        context.Dispose();
    }

    [Test]
    public void Player_PauseStepRestartAndCancelAreDeterministic()
    {
        TestContext context = CreateContext();
        context.Player.Start();
        context.Player.Pause();
        context.Player.Tick(100f);
        Assert.AreEqual("FL-P01", context.Player.CurrentStateId);
        context.Player.Resume();
        Assert.IsTrue(context.Player.NextState());
        Assert.AreEqual("FL-P02", context.Player.CurrentStateId);
        Assert.IsTrue(context.Player.PreviousState());
        Assert.AreEqual("FL-P01", context.Player.CurrentStateId);
        Assert.IsTrue(context.Player.Restart());
        Assert.AreEqual("FL-P01", context.Player.CurrentStateId);
        context.Player.Cancel();
        Assert.IsFalse(context.Player.IsRunning);
        Assert.AreEqual(string.Empty, context.Player.CurrentStateId);
        context.Dispose();
    }

    [Test]
    public void Player_DebriefWatchedAndSkippedRoutesShareMandatoryCluePayload()
    {
        TestContext context = CreateContext();
        NarrativeHandoffResult result = default;
        int count = 0;
        context.Player.HandoffRequested += value => { result = value; count++; };

        Assert.IsTrue(context.Player.StartAt("first_launch.command_base_reveal"));
        Assert.AreEqual(1, count);
        Assert.AreEqual("first_launch.m01_debrief_completion", result.Completion.PayloadId);
        Assert.IsTrue(result.Completion.Watched);
        Assert.IsFalse(result.Completion.Skipped);
        CollectionAssert.Contains(result.Completion.EvidenceIds, "evidence.aria.revoked_credential_fragment");

        NarrativeCompletionPayload skipped = FirstLaunchNarrativePlayerSystemHelper.CreateDebriefCompletion(true);
        Assert.IsTrue(context.Player.StartAt("first_launch.command_base_reveal", skipped));
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
        Assert.IsTrue(context.Player.StartAt("FL-P02"));
        Assert.AreEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);

        context.Player.Tick(0.29f);
        Assert.AreEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);
        context.Player.Tick(0.02f);
        Assert.AreNotEqual(NarrativeDialoguePhase.Hidden, context.View.DialogueView.Phase);

        context.Player.Tick(7.68f);
        Assert.AreEqual("FL-P02", context.Player.CurrentStateId);
        context.Player.Tick(0.02f);
        Assert.AreEqual("FL-P03", context.Player.CurrentStateId);

        Assert.IsTrue(context.Player.StartAt("FL-P04"));
        context.Player.Tick(0.51f);
        Assert.AreEqual(0, context.Player.CurrentLineIndex);
        context.Player.Tick(11.98f);
        Assert.AreEqual(0, context.Player.CurrentLineIndex);
        context.Player.Tick(0.02f);
        Assert.AreEqual(1, context.Player.CurrentLineIndex);
        context.Dispose();
    }

    private static TestContext CreateContext()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.NotNull(prefab, "Build the FirstLaunch presentation prefab before running player tests.");
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
        FirstLaunchNarrativePlayerSystemHelper player = new();
        NarrativeSequenceConfig sequence = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        Assert.NotNull(sequence, "Build the FirstLaunch sequence config before running player tests.");
        Assert.Greater(sequence.States.Count, 0, "FirstLaunch sequence config must be serialized before player tests.");
        Assert.IsTrue(player.Initialize(
            sequence,
            AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
            AssetDatabase.LoadAssetAtPath<NarrativePunctuationProfile>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),
            view,
            FallbackGameTextResolver.Instance,
            Game.UI.Runtime.SettingsService.Defaults));
        return new TestContext(instance, view, player);
    }

    private readonly struct TestContext
    {
        public readonly GameObject Instance;
        public readonly NarrativeSequenceView View;
        public readonly FirstLaunchNarrativePlayerSystemHelper Player;
        public TestContext(GameObject instance, NarrativeSequenceView view, FirstLaunchNarrativePlayerSystemHelper player) { Instance = instance; View = view; Player = player; }
        public void Dispose() { Player.Cancel(); UnityEngine.Object.DestroyImmediate(Instance); }
    }
}
