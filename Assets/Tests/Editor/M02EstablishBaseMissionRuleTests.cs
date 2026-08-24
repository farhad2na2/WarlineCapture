using System;
using System.Reflection;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class M02EstablishBaseMissionRuleTests
{
    private const string PassMarker = "[M02EstablishBaseMissionRuleValidation] result=Passed tests=11";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseMissionRuleTests tests = new();
            tests.NewRuleNumericValuesAppendWithoutRenumberingM01();
            tests.BuildRuleUsesOnlyBuildingConfigIdentity();
            tests.ProduceRuleUsesOnlyUnitConfigIdentity();
            tests.DefendRuleUsesOnlyMissionRoleIdentity();
            tests.NoCivilianLossUsesStableDefaultText();
            tests.ValidM02RuleSetPasses();
            tests.AmbiguousBuildTargetFailsClosed();
            tests.MissingBuildTargetFailsClosed();
            tests.WrongBuildPrefixFailsClosed();
            tests.DefendConfigTargetFailsClosed();
            tests.NoCivilianLossThresholdFailsClosed();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseMissionRuleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void NewRuleNumericValuesAppendWithoutRenumberingM01()
    {
        Assert.AreEqual(2, (int)MissionObjectiveRuleKind.ProtectMissionRole);
        Assert.AreEqual(3, (int)MissionObjectiveRuleKind.BuildStructure);
        Assert.AreEqual(4, (int)MissionObjectiveRuleKind.ProduceUnit);
        Assert.AreEqual(5, (int)MissionObjectiveRuleKind.DefendMissionRole);
        Assert.AreEqual(3, (int)MissionStarRuleKind.CompleteUnderMilliseconds);
        Assert.AreEqual(4, (int)MissionStarRuleKind.NoCivilianLoss);
    }

    [Test]
    public void BuildRuleUsesOnlyBuildingConfigIdentity()
    {
        MissionObjectiveDefinitionConfig objective = BuildObjective();
        Assert.AreEqual(MissionObjectiveRuleKind.BuildStructure, objective.Rule);
        Assert.AreEqual(string.Empty, objective.MissionRoleId);
        Assert.AreEqual("Building_Barrack", objective.TargetConfigId);
        Assert.AreEqual(1, objective.RequiredCount);
    }

    [Test]
    public void ProduceRuleUsesOnlyUnitConfigIdentity()
    {
        MissionObjectiveDefinitionConfig objective = ProduceObjective();
        Assert.AreEqual(MissionObjectiveRuleKind.ProduceUnit, objective.Rule);
        Assert.AreEqual(string.Empty, objective.MissionRoleId);
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", objective.TargetConfigId);
    }

    [Test]
    public void DefendRuleUsesOnlyMissionRoleIdentity()
    {
        MissionObjectiveDefinitionConfig objective = DefendObjective();
        Assert.AreEqual(MissionObjectiveRuleKind.DefendMissionRole, objective.Rule);
        Assert.AreEqual("role.friendly.forward_post", objective.MissionRoleId);
        Assert.AreEqual(string.Empty, objective.TargetConfigId);
        Assert.IsTrue(objective.FailureOnRuleBreak);
    }

    [Test]
    public void NoCivilianLossUsesStableDefaultText()
    {
        MissionStarDefinitionConfig star = new(2, MissionStarRuleKind.NoCivilianLoss);
        Assert.AreEqual("mission.star.no_civilian_loss", star.DisplayTextKey);
        Assert.AreEqual(0, star.Threshold);
    }

    [Test]
    public void ValidM02RuleSetPasses()
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
    public void AmbiguousBuildTargetFailsClosed()
    {
        MissionObjectiveDefinitionConfig objective = new(
            "objective.m02.build_barracks",
            "mission.m02.objective.build_barracks",
            MissionObjectiveRuleKind.BuildStructure,
            "role.friendly.forward_post",
            "Building_Barrack",
            1);
        AssertObjectiveRejected(objective);
    }

    [Test]
    public void MissingBuildTargetFailsClosed()
    {
        MissionObjectiveDefinitionConfig objective = new(
            "objective.m02.build_barracks",
            "mission.m02.objective.build_barracks",
            MissionObjectiveRuleKind.BuildStructure,
            string.Empty,
            1);
        AssertObjectiveRejected(objective);
    }

    [Test]
    public void WrongBuildPrefixFailsClosed()
    {
        MissionObjectiveDefinitionConfig objective = new(
            "objective.m02.build_barracks",
            "mission.m02.objective.build_barracks",
            MissionObjectiveRuleKind.BuildStructure,
            string.Empty,
            "Unit_Chr_Soldier_Male_02_Alt_04",
            1);
        AssertObjectiveRejected(objective);
    }

    [Test]
    public void DefendConfigTargetFailsClosed()
    {
        MissionObjectiveDefinitionConfig objective = new(
            "objective.m02.defend_post",
            "mission.m02.objective.defend_post",
            MissionObjectiveRuleKind.DefendMissionRole,
            string.Empty,
            "Building_Barrack",
            1,
            true);
        AssertObjectiveRejected(objective);
    }

    [Test]
    public void NoCivilianLossThresholdFailsClosed()
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "stars", new[]
            {
                new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteMission),
                new MissionStarDefinitionConfig(2, MissionStarRuleKind.NoCivilianLoss, 1),
                new MissionStarDefinitionConfig(3, MissionStarRuleKind.CompleteUnderMilliseconds, 300000)
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("invalid star rule", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static void AssertObjectiveRejected(MissionObjectiveDefinitionConfig objective)
    {
        MissionDefinitionConfig definition = CreateValidDefinition();
        try
        {
            SetField(definition, "objectives", new[] { objective });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateDefinition(definition, out string error));
            StringAssert.Contains("invalid objective", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static MissionDefinitionConfig CreateValidDefinition()
    {
        MissionDefinitionConfig definition = ScriptableObject.CreateInstance<MissionDefinitionConfig>();
        SetField(definition, "missionId", "saga.ch01.m02.establish_base");
        SetField(definition, "schemaVersion", 1);
        SetField(definition, "displayNameKey", "mission.m02.name");
        SetField(definition, "displaySummaryKey", "mission.m02.summary");
        SetField(definition, "locationNameKey", "mission.m02.location");
        SetField(definition, "scenarioId", "scenario.ch01.m02.establish_base");
        SetField(definition, "operationMapId", "opmap.ch01.forward_post_01");
        SetField(definition, "briefingSequenceId", "seq.ch01.m02.brief");
        SetField(definition, "commsSequenceId", "seq.ch01.m02.comms");
        SetField(definition, "debriefSequenceId", "seq.ch01.m02.debrief");
        SetField(definition, "objectives", new[]
        {
            BuildObjective(),
            ProduceObjective(),
            DefendObjective()
        });
        SetField(definition, "stars", new[]
        {
            new MissionStarDefinitionConfig(1, MissionStarRuleKind.CompleteMission),
            new MissionStarDefinitionConfig(2, MissionStarRuleKind.NoCivilianLoss),
            new MissionStarDefinitionConfig(3, MissionStarRuleKind.CompleteUnderMilliseconds, 300000)
        });
        SetField(definition, "firstClearRewards", new[]
        {
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 1500)
        });
        SetField(definition, "replayRewards", new[]
        {
            new MissionRewardDefinitionConfig(MissionRewardKind.Credits, 300)
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

    private static MissionObjectiveDefinitionConfig BuildObjective() => new(
        "objective.m02.build_barracks",
        "mission.m02.objective.build_barracks",
        MissionObjectiveRuleKind.BuildStructure,
        string.Empty,
        "Building_Barrack",
        1);

    private static MissionObjectiveDefinitionConfig ProduceObjective() => new(
        "objective.m02.produce_rifle",
        "mission.m02.objective.produce_rifle",
        MissionObjectiveRuleKind.ProduceUnit,
        string.Empty,
        "Unit_Chr_Soldier_Male_02_Alt_04",
        1);

    private static MissionObjectiveDefinitionConfig DefendObjective() => new(
        "objective.m02.defend_post",
        "mission.m02.objective.defend_post",
        MissionObjectiveRuleKind.DefendMissionRole,
        "role.friendly.forward_post",
        1,
        true);

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
