using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Catalog.Contracts;
using Game.UI.Runtime;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public sealed class M01StoryReplayTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchSetupRetirement.Apply();
            var tests = new M01StoryReplayTests();
            tests.CampaignReplayWaitsForStoryAndAdvancesOnce();
            tests.FirstLaunchDoesNotRepeatItsOpeningButRetryDoes();
            tests.ReplayHasEveryStoryPanelAndNoSetupOrPostMissionPages();
            tests.NewPlayerRetainsIdentityButNeverGuidanceChoice();
            Debug.Log("[M01StoryReplay] result=Passed tests=4");
            MissionEditorValidationExit.Complete(true);
        }
        catch (Exception error) { Debug.LogException(error); MissionEditorValidationExit.Complete(false); }
    }

    private static CampaignMissionRuntimeComponent Runtime(MissionLaunchOriginKind origin) => new()
    {
        MissionId = new FixedString64Bytes(FirstLaunchMissionHandoffOperation.MissionId),
        LaunchOrigin = origin, RunKind = MissionRunKind.Replay, Phase = MissionPhaseKind.InteractiveBrief
    };

    [Test] public void CampaignReplayWaitsForStoryAndAdvancesOnce()
    {
        var runtime = Runtime(MissionLaunchOriginKind.CampaignOperations);
        Assert.AreEqual(CampaignMissionDebriefCompositionSystemHelper.SequenceStage.Brief,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(runtime, default, false, false));
        Assert.IsFalse(CampaignMissionRuntimeProgressUtility.TryResolveAutomaticTransition(runtime, default, false, out _, out _, out _));
        var facts = new CampaignMissionAttemptFactsComponent { InteractiveBriefCompleted = 1 };
        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryResolveAutomaticTransition(runtime, facts, false, out var phase, out _, out _));
        Assert.AreEqual(MissionPhaseKind.FindSquad, phase);
        Assert.AreEqual(CampaignMissionDebriefCompositionSystemHelper.SequenceStage.None,
            CampaignMissionDebriefCompositionSystemHelper.ResolveStage(runtime, facts, true, false));
    }

    [Test] public void FirstLaunchDoesNotRepeatItsOpeningButRetryDoes()
    {
        var runtime = Runtime(MissionLaunchOriginKind.FirstLaunch);
        Assert.IsFalse(CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime));
        Assert.IsTrue(CampaignMissionRuntimeProgressUtility.TryResolveAutomaticTransition(runtime, default, false, out _, out _, out _));
        runtime.AttemptOrdinal = 1;
        Assert.IsTrue(CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime));
    }

    [Test] public void ReplayHasEveryStoryPanelAndNoSetupOrPostMissionPages()
    {
        var config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        var definitions = FirstLaunchNarrativeModelUtilitySystemHelper.CreatePlaybackDefinitions(config, true);
        Assert.AreEqual(18, definitions.Count); // 17 voiced/establishing panels plus gameplay handoff.
        Assert.IsFalse(definitions.Any(s => s.Kind is NarrativeStateKind.InteractiveIdentity or NarrativeStateKind.InteractiveGuidance));
        Assert.IsFalse(definitions.Any(s => s.StateId == "FL-P19"));
        var player = new FirstLaunchNarrativeSequenceUtilitySystemHelper();
        Assert.IsTrue(player.Configure(config.EntryStateId, definitions));
        Assert.IsTrue(player.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.Start)));
        foreach (var expected in definitions)
        {
            Assert.AreEqual(expected.StateId, player.CurrentStateId);
            if (expected.StateId != "first_launch.m01_handoff")
                Assert.IsTrue(player.Apply(new FirstLaunchNarrativeSequenceIntent(FirstLaunchNarrativeSequenceIntentKind.ContinueState, player.CurrentStateId, player.TransitionToken)));
        }
    }

    [Test] public void NewPlayerRetainsIdentityButNeverGuidanceChoice()
    {
        var config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
        Assert.IsFalse(config.States.Any(s => s.Kind == NarrativeStateKind.InteractiveGuidance));
        var definitions = FirstLaunchNarrativeModelUtilitySystemHelper.CreatePlaybackDefinitions(config, false);
        Assert.IsTrue(definitions.Any(s => s.Kind == NarrativeStateKind.InteractiveIdentity));
        Assert.IsTrue(new FirstLaunchNarrativeSequenceUtilitySystemHelper().Configure(config.EntryStateId, definitions));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
        Assert.IsNull(prefab.transform.Find("SafeArea/GuidanceChoiceSurface"));
        Assert.IsNull(prefab.GetComponent<NarrativeSequenceView>().GuidanceChoiceView);
    }
}
