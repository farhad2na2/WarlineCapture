using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class M02EstablishBaseScenarioTests
{
    private const string Marker = "[M02EstablishBaseScenarioValidation] result=Passed tests=12";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseScenarioTests tests = new();
            tests.LegacyScenarioDefaultsKeepMissionRuntimeDisabled();
            tests.ValidMissionRuntimePasses();
            tests.MissionRuntimeRoundTripsDeterministically();
            tests.DisabledBuildingFailsClosed();
            tests.NonPositiveStartingResourcesFailClosed();
            tests.DuplicateBuildEntriesFailClosed();
            tests.MissingRequiredProducerFailsClosed();
            tests.WrongRequiredUnitPrefixFailsClosed();
            tests.MissingBuildAnchorFailsClosed();
            tests.WrongBaseAnchorKindFailsClosed();
            tests.WarningMustPrecedeActivation();
            tests.DelayedWaveMustUseMatchingGroupAndRoute();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseScenarioValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void LegacyScenarioDefaultsKeepMissionRuntimeDisabled()
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        try
        {
            JsonUtility.FromJsonOverwrite(
                "{\"scenarioId\":\"scenario.skirmish.default\",\"operationMapId\":\"opmap.skirmish.desert_base_01\",\"requiredAnchors\":[]}",
                scenario);
            Assert.IsFalse(scenario.MissionRuntime.Enabled);
            Assert.AreEqual(0, scenario.MissionRuntime.StartingCredits);
            Assert.AreEqual(0, scenario.MissionRuntime.StartingMaterials);
            Assert.AreEqual(0, scenario.MissionRuntime.BuildCatalog.Length);
            Assert.IsTrue(scenario.TryValidate(out string error), error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(scenario);
        }
    }

    [Test]
    public void ValidMissionRuntimePasses()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            Assert.IsTrue(scenario.TryValidate(out string error), error);
            Assert.AreEqual(50000, scenario.MissionRuntime.StartingCredits);
            Assert.AreEqual(120, scenario.MissionRuntime.StartingMaterials);
            Assert.AreEqual("Building_Barrack", scenario.MissionRuntime.RequiredProducerConfigId);
            Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", scenario.MissionRuntime.RequiredUnitConfigId);
            Assert.AreEqual(1, scenario.MissionRuntime.BuildCatalog.Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(scenario);
        }
    }

    [Test]
    public void MissionRuntimeRoundTripsDeterministically()
    {
        ScenarioSetupConfig source = CreateValidScenario();
        ScenarioSetupConfig copy = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        try
        {
            string first = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(first, copy);
            string second = JsonUtility.ToJson(copy);
            Assert.AreEqual(first, second);
            Assert.IsTrue(copy.TryValidate(out string error), error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(copy);
        }
    }

    [Test]
    public void DisabledBuildingFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetField(scenario, "restrictions", new ScenarioRestrictionConfig(true, false, false, true, true));
            AssertRejected(scenario, "mission economy, build, or base data");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void NonPositiveStartingResourcesFailClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(startingCredits: 0));
            AssertRejected(scenario, "mission economy, build, or base data");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void DuplicateBuildEntriesFailClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            ScenarioMissionBuildEntryConfig entry = new("Building_Barrack", 1);
            SetRuntime(scenario, Runtime(buildCatalog: new[] { entry, entry }));
            AssertRejected(scenario, "duplicate mission build entry");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void MissingRequiredProducerFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(requiredProducer: "Building_Helipad"));
            AssertRejected(scenario, "required producer or delayed wave");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void WrongRequiredUnitPrefixFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(requiredUnit: "Building_Barrack"));
            AssertRejected(scenario, "mission economy, build, or base data");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void MissingBuildAnchorFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(buildZone: new ScenarioMissionBuildZoneConfig(
                "anchor.ch01.m02.missing", 5, 4)));
            AssertRejected(scenario, "mission economy, build, or base data");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void WrongBaseAnchorKindFailsClosed()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(baseAnchor: "anchor.ch01.m02.build_lot"));
            AssertRejected(scenario, "mission economy, build, or base data");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void WarningMustPrecedeActivation()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(wave: new ScenarioDelayedWaveConfig(
                "group.ch01.m02.hostile_patrol", "route.ch01.m02.hostile_patrol",
                "role.friendly.forward_post", 30000, 30000)));
            AssertRejected(scenario, "required producer or delayed wave");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [Test]
    public void DelayedWaveMustUseMatchingGroupAndRoute()
    {
        ScenarioSetupConfig scenario = CreateValidScenario();
        try
        {
            SetRuntime(scenario, Runtime(wave: new ScenarioDelayedWaveConfig(
                "group.ch01.m02.friendly", "route.ch01.m02.hostile_patrol",
                "role.friendly.forward_post", 20000, 30000)));
            AssertRejected(scenario, "required producer or delayed wave");
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    private static ScenarioSetupConfig CreateValidScenario()
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        SetField(scenario, "scenarioId", "scenario.ch01.m02.establish_base");
        SetField(scenario, "operationMapId", "opmap.ch01.forward_post_01");
        SetField(scenario, "requiredAnchors", new[]
        {
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.friendly_spawn", OperationMapAnchorKind.Deployment),
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.hostile_spawn", OperationMapAnchorKind.Spawn),
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.lane_a", OperationMapAnchorKind.Lane),
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.lane_b", OperationMapAnchorKind.Lane),
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.forward_post", OperationMapAnchorKind.Base),
            new ScenarioAnchorRequirementConfig("anchor.ch01.m02.build_lot", OperationMapAnchorKind.Build)
        });
        SetField(scenario, "deterministicSeed", 2002001);
        SetField(scenario, "encounterStartMilliseconds", 0);
        SetField(scenario, "unitGroups", new[] { FriendlyGroup(), HostileGroup() });
        SetField(scenario, "patrolRoutes", new[]
        {
            new ScenarioPatrolRouteConfig(
                "route.ch01.m02.hostile_patrol",
                "group.ch01.m02.hostile_patrol",
                new[] { "anchor.ch01.m02.lane_a", "anchor.ch01.m02.lane_b" },
                30000)
        });
        SetField(scenario, "restrictions", new ScenarioRestrictionConfig(false, false, false, true, true));
        SetField(scenario, "ambientPresentations", Array.Empty<ScenarioAmbientPresentationConfig>());
        SetRuntime(scenario, Runtime());
        return scenario;
    }

    private static ScenarioMissionRuntimeConfig Runtime(
        int startingCredits = 50000,
        ScenarioMissionBuildEntryConfig[] buildCatalog = null,
        string requiredProducer = "Building_Barrack",
        string requiredUnit = "Unit_Chr_Soldier_Male_02_Alt_04",
        string baseAnchor = "anchor.ch01.m02.forward_post",
        ScenarioMissionBuildZoneConfig? buildZone = null,
        ScenarioDelayedWaveConfig? wave = null) => new(
            true,
            startingCredits,
            120,
            buildCatalog ?? new[] { new ScenarioMissionBuildEntryConfig("Building_Barrack", 1) },
            requiredProducer,
            requiredUnit,
            "role.friendly.forward_post",
            baseAnchor,
            buildZone ?? new ScenarioMissionBuildZoneConfig("anchor.ch01.m02.build_lot", 5, 4),
            wave ?? new ScenarioDelayedWaveConfig(
                "group.ch01.m02.hostile_patrol",
                "route.ch01.m02.hostile_patrol",
                "role.friendly.forward_post",
                20000,
                30000));

    private static ScenarioUnitGroupConfig FriendlyGroup() => new(
        "group.ch01.m02.friendly", 1, new[]
        {
            new ScenarioUnitEntryConfig(
                "unit.jrc.rifle", "Unit_Chr_Soldier_Male_02_Alt_04",
                "a3d36fd8847164cc596e0b5ba7bd9bb9", "anchor.ch01.m02.friendly_spawn",
                "role.friendly.command_squad", 4)
        });

    private static ScenarioUnitGroupConfig HostileGroup() => new(
        "group.ch01.m02.hostile_patrol", 2, new[]
        {
            new ScenarioUnitEntryConfig(
                "unit.ash.patrol", "Unit_Chr_Insurgent_Male_03",
                "fe23cbf9678344f4b182169b49fe68b6", "anchor.ch01.m02.hostile_spawn",
                "role.hostile.patrol", 3)
        });

    private static void AssertRejected(ScenarioSetupConfig scenario, string expected)
    {
        Assert.IsFalse(scenario.TryValidate(out string error));
        StringAssert.Contains(expected, error);
    }

    private static void SetRuntime(ScenarioSetupConfig scenario, ScenarioMissionRuntimeConfig value) =>
        SetField(scenario, "missionRuntime", value);

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{name}'.");
        field.SetValue(target, value);
    }
}
