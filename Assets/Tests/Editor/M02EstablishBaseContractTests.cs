using System;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseContractTests
{
    private const string Marker = "[M02EstablishBaseContractValidation] result=Passed tests=8";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseContractTests tests = new();
            tests.IdentityAndReferencesAreCanonical();
            tests.ObjectivesMatchTheDetailedSpec();
            tests.StarsMatchTheDetailedSpec();
            tests.FirstClearRewardsAreBoundedAndExplicit();
            tests.ReplayRewardIsReducedCreditsOnly();
            tests.CommandPolicyAddsBuildWithoutUnavailableCommands();
            tests.ReplayAndReadinessRemainExplicit();
            tests.CanonicalDefinitionPassesTheMissionContract();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseContractValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void IdentityAndReferencesAreCanonical()
    {
        MissionDefinitionConfig mission = LoadMission();
        Assert.AreEqual(M02EstablishBaseConfigBuilder.MissionId, mission.MissionId);
        Assert.AreEqual(1, mission.SchemaVersion);
        Assert.AreEqual("mission.m02.name", mission.DisplayNameKey);
        Assert.AreEqual("mission.m02.summary", mission.DisplaySummaryKey);
        Assert.AreEqual("mission.m02.location", mission.LocationNameKey);
        Assert.AreEqual("scenario.ch01.m02.establish_base", mission.ScenarioId);
        Assert.AreEqual("opmap.ch01.forward_post_01", mission.OperationMapId);
        Assert.AreEqual("seq.ch01.m02.brief", mission.BriefingSequenceId);
        Assert.AreEqual("seq.ch01.m02.comms", mission.CommsSequenceId);
        Assert.AreEqual("seq.ch01.m02.debrief", mission.DebriefSequenceId);
    }

    [Test]
    public void ObjectivesMatchTheDetailedSpec()
    {
        ReadOnlySpan<MissionObjectiveDefinitionConfig> objectives = LoadMission().Objectives;
        Assert.AreEqual(2, objectives.Length);
        AssertObjective(objectives[0], "obj.ch01.m02.build_forward_barracks",
            MissionObjectiveRuleKind.BuildStructure, string.Empty, "Building_Barrack", 1, false);
        AssertObjective(objectives[1], "obj.ch01.m02.produce_rifle_squad",
            MissionObjectiveRuleKind.ProduceUnit, string.Empty,
            "Unit_Chr_Soldier_Male_02_Alt_04", 1, false);
    }

    [Test]
    public void StarsMatchTheDetailedSpec()
    {
        ReadOnlySpan<MissionStarDefinitionConfig> stars = LoadMission().Stars;
        Assert.AreEqual(3, stars.Length);
        AssertStar(stars[0], 1, MissionStarRuleKind.CompleteMission, 0);
        AssertStar(stars[1], 2, MissionStarRuleKind.NoCivilianLoss, 0);
        AssertStar(stars[2], 3, MissionStarRuleKind.CompleteUnderMilliseconds, 300000);
    }

    [Test]
    public void FirstClearRewardsAreBoundedAndExplicit()
    {
        ReadOnlySpan<MissionRewardDefinitionConfig> rewards = LoadMission().FirstClearRewards;
        Assert.AreEqual(3, rewards.Length);
        AssertReward(rewards[0], MissionRewardKind.None, "reward.commander_xp", 320);
        AssertReward(rewards[1], MissionRewardKind.Credits, string.Empty, 1500);
        AssertReward(rewards[2], MissionRewardKind.None, "reward.ch01.m02.production_unlock", 1);
    }

    [Test]
    public void ReplayRewardIsReducedCreditsOnly()
    {
        ReadOnlySpan<MissionRewardDefinitionConfig> rewards = LoadMission().ReplayRewards;
        Assert.AreEqual(1, rewards.Length);
        AssertReward(rewards[0], MissionRewardKind.Credits, string.Empty, 300);
    }

    [Test]
    public void CommandPolicyAddsBuildWithoutUnavailableCommands()
    {
        ReadOnlySpan<TacticalCommandMode> commands = LoadMission().CommandPolicy.AllowedCommands;
        TacticalCommandMode[] expected =
        {
            TacticalCommandMode.Select,
            TacticalCommandMode.Move,
            TacticalCommandMode.Attack,
            TacticalCommandMode.Hold,
            TacticalCommandMode.Stop,
            TacticalCommandMode.Build
        };
        Assert.AreEqual(expected.Length, commands.Length);
        for (int index = 0; index < expected.Length; index++)
            Assert.AreEqual(expected[index], commands[index]);
    }

    [Test]
    public void ReplayAndReadinessRemainExplicit()
    {
        MissionDefinitionConfig mission = LoadMission();
        Assert.IsTrue(mission.ReplayAllowed);
        Assert.IsFalse(mission.ReplayTutorialDefaultEnabled);
        Assert.IsTrue(mission.RequireOperationMapReady);
        Assert.IsTrue(mission.RequireGridReady);
        Assert.IsTrue(mission.RequireUnitCatalogReady);
        ReadOnlySpan<string> features = mission.RequiredFeatureIds;
        Assert.AreEqual(3, features.Length);
        Assert.AreEqual("feature.operation_map", features[0]);
        Assert.AreEqual("feature.unit_catalog", features[1]);
        Assert.AreEqual("feature.tactical_commands", features[2]);
    }

    [Test]
    public void CanonicalDefinitionPassesTheMissionContract()
    {
        Assert.IsTrue(MissionDefinitionContractValidation.TryValidateDefinition(
            LoadMission(), out string error), error);
    }

    private static MissionDefinitionConfig LoadMission()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        Assert.That(mission, Is.Not.Null, $"M02 mission asset is missing: {M02EstablishBaseConfigBuilder.MissionPath}");
        return mission;
    }

    private static void AssertObjective(
        MissionObjectiveDefinitionConfig objective,
        string id,
        MissionObjectiveRuleKind rule,
        string role,
        string config,
        int requiredCount,
        bool failure)
    {
        Assert.AreEqual(id, objective.ObjectiveId);
        Assert.AreEqual(rule, objective.Rule);
        Assert.AreEqual(role, objective.MissionRoleId);
        Assert.AreEqual(config, objective.TargetConfigId);
        Assert.AreEqual(requiredCount, objective.RequiredCount);
        Assert.AreEqual(failure, objective.FailureOnRuleBreak);
    }

    private static void AssertStar(
        MissionStarDefinitionConfig star,
        byte index,
        MissionStarRuleKind rule,
        int threshold)
    {
        Assert.AreEqual(index, star.StarIndex);
        Assert.AreEqual(rule, star.Rule);
        Assert.AreEqual(threshold, star.Threshold);
    }

    private static void AssertReward(
        MissionRewardDefinitionConfig reward,
        MissionRewardKind kind,
        string id,
        int amount)
    {
        Assert.AreEqual(kind, reward.Kind);
        Assert.AreEqual(id, reward.RewardConfigId);
        Assert.AreEqual(amount, reward.Amount);
    }
}
