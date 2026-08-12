using System;
using System.Reflection;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class M01FirstContactMissionRuleTests
{
    private const string PassMarker = "[M01FirstContactMissionRuleValidation] result=Passed tests=14";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactMissionRuleTests tests = new();
            tests.ObjectiveProjectsDisplayAndFailureData();
            tests.UnderFourMinutesIsAStarThresholdOnly();
            tests.StarProjectsAuthoredDisplayData();
            tests.TypedRewardProjectsDisplayData();
            tests.RewardConfigReferenceProjectsDisplayData();
            tests.FirstClearAndReplayMayUseSameRewardIndependently();
            tests.AmbiguousRewardIdentityFailsClosed();
            tests.DuplicateFirstClearRewardFailsClosed();
            tests.DuplicateReplayRewardFailsClosed();
            tests.PlaceholderRewardIdentityFailsClosed();
            tests.PlaceholderRewardDisplayFailsClosed();
            tests.M01IntelKindFailsClosed();
            tests.M01IntelConfigReferenceFailsClosed();
            tests.InvalidStarDisplayFailsClosed();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactMissionRuleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void ObjectiveProjectsDisplayAndFailureData()
    {
        MissionObjectiveDefinitionConfig objective = FailureObjective();
        Assert.AreEqual("objective.m01.command_squad_survives", objective.ObjectiveId);
        Assert.AreEqual("mission.m01.failure.command_squad_destroyed", objective.DisplayTextKey);
        Assert.AreEqual(MissionObjectiveRuleKind.ProtectMissionRole, objective.Rule);
        Assert.AreEqual("role.friendly.command_squad", objective.MissionRoleId);
        Assert.AreEqual(1, objective.RequiredCount);
        Assert.IsTrue(objective.FailureOnRuleBreak);
    }

    [Test]
    public void UnderFourMinutesIsAStarThresholdOnly()
    {
        MissionStarDefinitionConfig star = new(
            3,
            MissionStarRuleKind.CompleteUnderMilliseconds,
            "mission.m01.star.under_four_minutes",
            240000);
        Assert.AreEqual(MissionStarRuleKind.CompleteUnderMilliseconds, star.Rule);
        Assert.AreEqual(240000, star.Threshold);
        Assert.IsFalse(FailureObjective().DisplayTextKey.Contains("four_minutes"));
    }

    [Test]
    public void StarProjectsAuthoredDisplayData()
    {
        MissionStarDefinitionConfig star = new(
            2,
            MissionStarRuleKind.NoSquadLoss,
            "mission.m01.star.no_squad_loss");
        Assert.AreEqual("mission.m01.star.no_squad_loss", star.DisplayTextKey);
    }

    [Test]
    public void TypedRewardProjectsDisplayData()
    {
        MissionRewardDefinitionConfig reward = new(MissionRewardKind.Credits, 1200);
        Assert.AreEqual(MissionRewardKind.Credits, reward.Kind);
        Assert.AreEqual(string.Empty, reward.RewardConfigId);
        Assert.AreEqual("mission.reward.credits", reward.DisplayTextKey);
        Assert.AreEqual(1200, reward.Amount);
    }

    [Test]
    public void RewardConfigReferenceProjectsDisplayData()
    {
        MissionRewardDefinitionConfig reward = new(
            "reward.commander_xp",
            "mission.reward.commander_xp",
            260);
        Assert.AreEqual(MissionRewardKind.None, reward.Kind);
        Assert.AreEqual("reward.commander_xp", reward.RewardConfigId);
        Assert.AreEqual("mission.reward.commander_xp", reward.DisplayTextKey);
        Assert.AreEqual(260, reward.Amount);
    }

    [Test]
    public void FirstClearAndReplayMayUseSameRewardIndependently()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            Assert.IsTrue(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error), error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void AmbiguousRewardIdentityFailsClosed()
    {
        MissionRewardDefinitionConfig reward = new(MissionRewardKind.Credits, 100);
        object boxed = reward;
        SetField(boxed, "rewardConfigId", "reward.credits");
        AssertRewardRejected((MissionRewardDefinitionConfig)boxed, "ambiguous settlement");
    }

    [Test]
    public void DuplicateFirstClearRewardFailsClosed()
    {
        MissionRewardDefinitionConfig reward = new(MissionRewardKind.Credits, 100);
        AssertRewardSetRejected("firstClearRewards", new[] { reward, reward }, "duplicate first-clear reward");
    }

    [Test]
    public void DuplicateReplayRewardFailsClosed()
    {
        MissionRewardDefinitionConfig reward = new("reward.credits", "mission.reward.credits", 100);
        AssertRewardSetRejected("replayRewards", new[] { reward, reward }, "duplicate replay reward");
    }

    [Test]
    public void PlaceholderRewardIdentityFailsClosed() =>
        AssertRewardRejected(new MissionRewardDefinitionConfig(
            "reward.placeholder", "mission.reward.credits", 100), "invalid first-clear reward");

    [Test]
    public void PlaceholderRewardDisplayFailsClosed() =>
        AssertRewardRejected(new MissionRewardDefinitionConfig(
            "reward.credits", "mission.reward.placeholder", 100), "invalid first-clear reward");

    [Test]
    public void M01IntelKindFailsClosed() =>
        AssertRewardRejected(new MissionRewardDefinitionConfig(MissionRewardKind.Intel, 1), "invalid first-clear reward");

    [Test]
    public void M01IntelConfigReferenceFailsClosed() =>
        AssertRewardRejected(new MissionRewardDefinitionConfig(
            "reward.intel", "mission.reward.intel", 1), "invalid first-clear reward");

    [Test]
    public void InvalidStarDisplayFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "stars", new[]
            {
                new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteMission, "placeholder")
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("invalid star rule", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static void AssertRewardRejected(MissionRewardDefinitionConfig reward, string expectedError) =>
        AssertRewardSetRejected("firstClearRewards", new[] { reward }, expectedError);

    private static void AssertRewardSetRejected(
        string fieldName,
        MissionRewardDefinitionConfig[] rewards,
        string expectedError)
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, fieldName, rewards);
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains(expectedError, error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static MissionDefinitionConfig CreateValidDefinition()
    {
        MissionDefinitionConfig definition = ScriptableObject.CreateInstance<MissionDefinitionConfig>();
        SetField(definition, "missionId", MissionDefinitionContractValidation.FirstContactMissionId);
        SetField(definition, "schemaVersion", 1);
        SetField(definition, "displayNameKey", "mission.m01.name");
        SetField(definition, "displaySummaryKey", "mission.m01.summary");
        SetField(definition, "locationNameKey", "mission.m01.location");
        SetField(definition, "scenarioId", "scenario.ch01.m01.first_contact");
        SetField(definition, "operationMapId", "opmap.ch01.district_edge_01");
        SetField(definition, "briefingSequenceId", "seq.ch01.m01.brief");
        SetField(definition, "commsSequenceId", "seq.ch01.m01.comms");
        SetField(definition, "debriefSequenceId", "seq.ch01.m01.debrief");
        SetField(definition, "objectives", new[] { VictoryObjective(), FailureObjective() });
        SetField(definition, "stars", new[]
        {
            new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteMission),
            new MissionStarDefinitionConfig(2, MissionStarRuleKind.NoSquadLoss),
            new MissionStarDefinitionConfig(3, MissionStarRuleKind.CompleteUnderMilliseconds, 240000)
        });
        SetField(definition, "firstClearRewards", new[]
        {
            new MissionRewardDefinitionConfig("reward.commander_xp", "mission.reward.commander_xp", 260),
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 1200)
        });
        SetField(definition, "replayRewards", new[]
        {
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 250)
        });
        SetField(definition, "commandPolicy", new MissionCommandPolicyConfig(new[]
        {
            TacticalCommandMode.Select,
            TacticalCommandMode.Move,
            TacticalCommandMode.Attack,
            TacticalCommandMode.Hold,
            TacticalCommandMode.Stop
        }));
        SetField(definition, "replayAllowed", true);
        SetField(definition, "requireOperationMapReady", true);
        SetField(definition, "requireGridReady", true);
        SetField(definition, "requireUnitCatalogReady", true);
        SetField(definition, "requiredFeatureIds", new[]
        {
            "feature.operation_map",
            "feature.unit_catalog",
            "feature.tactical_commands"
        });
        return definition;
    }

    private static MissionObjectiveDefinitionConfig VictoryObjective() => new(
        "objective.m01.secure_corridor",
        "mission.m01.objective.secure_corridor",
        MissionObjectiveRuleKind.DestroyMissionRole,
        "role.hostile.patrol",
        3);

    private static MissionObjectiveDefinitionConfig FailureObjective() => new(
        "objective.m01.command_squad_survives",
        "mission.m01.failure.command_squad_destroyed",
        MissionObjectiveRuleKind.ProtectMissionRole,
        "role.friendly.command_squad",
        1,
        true);

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
