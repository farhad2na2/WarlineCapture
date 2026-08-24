using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseCanonicalDataTests
{
    private const string BarracksPrefabPath =
        "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
    private const string RequiredUnitConfigPath =
        "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";
    private const string Marker =
        "[M02EstablishBaseCanonicalDataValidation] result=Passed tests=10";

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseConfigBuilder.BuildScenario();
            M02EstablishBaseCanonicalDataTests tests = new();
            tests.RegenerationIsByteStable();
            tests.ScenarioPassesContractValidation();
            tests.IdentityMatchesMissionDefinition();
            tests.RequiredActionsAreAffordableWithPositiveRemainders();
            tests.StartingSquadUsesApprovedResolvableUnits();
            tests.DelayedPatrolUsesApprovedResolvableUnits();
            tests.AllAuthoredAnchorReferencesCloseOverTheRequiredSet();
            tests.BuildZoneContainsTheCanonicalBarracksFootprint();
            tests.DelayedWaveAndRouteUseOneDeterministicTimeline();
            tests.RestrictionsAndCivilianPresentationMatchTheMissionScope();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseCanonicalDataValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RegenerationIsByteStable()
    {
        string before = HashScenario();
        M02EstablishBaseConfigBuilder.BuildScenario();
        Assert.AreEqual(before, HashScenario());
    }

    [Test]
    public void ScenarioPassesContractValidation()
    {
        ScenarioSetupConfig scenario = Scenario();
        Assert.IsTrue(scenario.TryValidate(out string error), error);
    }

    [Test]
    public void IdentityMatchesMissionDefinition()
    {
        ScenarioSetupConfig scenario = Scenario();
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        Assert.AreEqual("scenario.ch01.m02.establish_base", scenario.ScenarioId);
        Assert.AreEqual("opmap.ch01.forward_post_01", scenario.OperationMapId);
        Assert.AreEqual(scenario.ScenarioId, mission.ScenarioId);
        Assert.AreEqual(scenario.OperationMapId, mission.OperationMapId);
        Assert.AreEqual(2002001, scenario.DeterministicSeed);
    }

    [Test]
    public void RequiredActionsAreAffordableWithPositiveRemainders()
    {
        ScenarioMissionRuntimeConfig runtime = Scenario().MissionRuntime;
        BuildingDefinitionAuthoringConfig barracks = Load<BuildingDefinitionAuthoringConfig>(
            M02EstablishBaseConfigBuilder.BarracksConfigPath);
        UnitGridAuthoringConfig rifle = Load<UnitGridAuthoringConfig>(RequiredUnitConfigPath);
        Assert.AreEqual(55000, runtime.StartingCredits);
        Assert.AreEqual(120, runtime.StartingMaterials);
        Assert.AreEqual(5000, runtime.StartingCredits - barracks.Price - rifle.Price);
        Assert.AreEqual(10, runtime.StartingMaterials - barracks.MaterialsCost - rifle.MaterialsCost);
        Assert.AreEqual("Building_Barrack", runtime.RequiredProducerConfigId);
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", runtime.RequiredUnitConfigId);
        Assert.AreEqual(1, runtime.BuildCatalog.Length);
        Assert.AreEqual(1, runtime.BuildCatalog[0].MaxCount);
    }

    [Test]
    public void StartingSquadUsesApprovedResolvableUnits()
    {
        AssertGroup(
            "group.ch01.m02.command_squad",
            1,
            "anchor.ch01.m02.friendly_spawn",
            "role.friendly.command_squad",
            new[]
            {
                "70d525bdf4894529869cd1402ff5d62e", "a3d36fd8847164cc596e0b5ba7bd9bb9",
                "970069fef3e4437195c3225a1615e384", "b9481cc1ec42499c96846b5d161ac7b2"
            });
    }

    [Test]
    public void DelayedPatrolUsesApprovedResolvableUnits()
    {
        AssertGroup(
            "group.ch01.m02.hostile_patrol",
            2,
            "anchor.ch01.m02.hostile_spawn",
            "role.hostile.patrol",
            new[]
            {
                "fe23cbf9678344f4b182169b49fe68b6", "01045d2a58ec4359b395696309684ffa",
                "8093159068194fc187efe5c356116e9b"
            });
    }

    [Test]
    public void AllAuthoredAnchorReferencesCloseOverTheRequiredSet()
    {
        ScenarioSetupConfig scenario = Scenario();
        HashSet<string> anchors = new(StringComparer.Ordinal);
        foreach (ScenarioAnchorRequirementConfig anchor in scenario.RequiredAnchors)
            Assert.IsTrue(anchors.Add(anchor.AnchorId), $"Duplicate anchor '{anchor.AnchorId}'.");

        Assert.AreEqual(12, anchors.Count);
        foreach (ScenarioUnitGroupConfig group in scenario.UnitGroups)
            foreach (ScenarioUnitEntryConfig unit in group.Units)
                Assert.IsTrue(anchors.Contains(unit.SpawnAnchorId), unit.SpawnAnchorId);
        foreach (ScenarioPatrolRouteConfig route in scenario.PatrolRoutes)
            foreach (string anchorId in route.AnchorIds)
                Assert.IsTrue(anchors.Contains(anchorId), anchorId);
        foreach (ScenarioAmbientPresentationConfig ambient in scenario.AmbientPresentations)
            Assert.IsTrue(anchors.Contains(ambient.AnchorId), ambient.AnchorId);
        Assert.IsTrue(anchors.Contains(scenario.MissionRuntime.BaseAnchorId));
        Assert.IsTrue(anchors.Contains(scenario.MissionRuntime.BuildZone.AnchorId));
        AssertAnchorKind(scenario, "anchor.ch01.m02.forward_post", OperationMapAnchorKind.Base);
        AssertAnchorKind(scenario, "anchor.ch01.m02.build_lot", OperationMapAnchorKind.Build);
    }

    [Test]
    public void BuildZoneContainsTheCanonicalBarracksFootprint()
    {
        BuildingDefinitionAuthoring barracks =
            Load<GameObject>(BarracksPrefabPath).GetComponent<BuildingDefinitionAuthoring>();
        Assert.IsNotNull(barracks);
        Vector2Int footprint = barracks.ConfiguredFootprintCells;
        ScenarioMissionBuildZoneConfig zone = Scenario().MissionRuntime.BuildZone;
        Assert.AreEqual(new Vector2Int(20, 10), footprint);
        Assert.AreEqual(24, zone.HalfWidthCells * 2);
        Assert.AreEqual(14, zone.HalfHeightCells * 2);
        Assert.GreaterOrEqual(zone.HalfWidthCells * 2, footprint.x);
        Assert.GreaterOrEqual(zone.HalfHeightCells * 2, footprint.y);
    }

    [Test]
    public void DelayedWaveAndRouteUseOneDeterministicTimeline()
    {
        ScenarioSetupConfig scenario = Scenario();
        Assert.AreEqual(120000, scenario.EncounterStartMilliseconds);
        Assert.AreEqual(1, scenario.PatrolRoutes.Length);
        ScenarioPatrolRouteConfig route = scenario.PatrolRoutes[0];
        ScenarioDelayedWaveConfig wave = scenario.MissionRuntime.DelayedWave;
        Assert.AreEqual("route.ch01.m02.hostile_patrol", route.RouteId);
        Assert.AreEqual(wave.RouteId, route.RouteId);
        Assert.AreEqual(wave.UnitGroupId, route.UnitGroupId);
        Assert.AreEqual(3, route.AnchorIds.Length);
        Assert.AreEqual(90000, wave.WarningAtMilliseconds);
        Assert.AreEqual(120000, wave.ActivationAtMilliseconds);
        Assert.AreEqual(wave.ActivationAtMilliseconds, route.StartDelayMilliseconds);
        Assert.AreEqual("role.friendly.forward_post", wave.TargetMissionRoleId);
    }

    [Test]
    public void RestrictionsAndCivilianPresentationMatchTheMissionScope()
    {
        ScenarioSetupConfig scenario = Scenario();
        Assert.IsFalse(scenario.Restrictions.BuildingDisabled);
        Assert.IsFalse(scenario.Restrictions.ProductionDisabled);
        Assert.IsFalse(scenario.Restrictions.EconomyDisabled);
        Assert.IsTrue(scenario.Restrictions.TransportDisabled);
        Assert.IsTrue(scenario.Restrictions.AirDisabled);
        Assert.AreEqual(1, scenario.AmbientPresentations.Length);
        ScenarioAmbientPresentationConfig civilians = scenario.AmbientPresentations[0];
        Assert.AreEqual("ambient.ch01.m02.civilians", civilians.PresentationId);
        Assert.AreEqual("anchor.ch01.m02.civilian_edge", civilians.AnchorId);
        Assert.AreEqual("route.ch01.m02.civilian_evacuation", civilians.RouteId);
        Assert.AreEqual(12, civilians.InstanceCount);
    }

    private static void AssertGroup(
        string groupId,
        byte factionIndex,
        string spawnAnchorId,
        string roleId,
        string[] expectedGuids)
    {
        ScenarioUnitGroupConfig group = FindGroup(groupId);
        Assert.AreEqual(factionIndex, group.FactionIndex);
        Assert.AreEqual(expectedGuids.Length, group.Units.Length);
        for (int index = 0; index < expectedGuids.Length; index++)
        {
            ScenarioUnitEntryConfig unit = group.Units[index];
            Assert.AreEqual(expectedGuids[index], unit.ExpectedAssetGuid);
            Assert.AreEqual(spawnAnchorId, unit.SpawnAnchorId);
            Assert.AreEqual(roleId, unit.MissionRoleId);
            Assert.AreEqual(1, unit.Count);
            Assert.IsNotEmpty(AssetDatabase.GUIDToAssetPath(unit.ExpectedAssetGuid));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(
                AssetDatabase.GUIDToAssetPath(unit.ExpectedAssetGuid)));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Game/Prefabs/Characters/{unit.RuntimePrefabSourceKey}.prefab"));
        }
    }

    private static ScenarioUnitGroupConfig FindGroup(string id)
    {
        foreach (ScenarioUnitGroupConfig group in Scenario().UnitGroups)
            if (group.GroupId == id)
                return group;
        Assert.Fail($"Missing unit group '{id}'.");
        return default;
    }

    private static void AssertAnchorKind(
        ScenarioSetupConfig scenario,
        string id,
        OperationMapAnchorKind expectedKind)
    {
        foreach (ScenarioAnchorRequirementConfig anchor in scenario.RequiredAnchors)
        {
            if (anchor.AnchorId != id)
                continue;
            Assert.AreEqual(expectedKind, anchor.Kind);
            return;
        }
        Assert.Fail($"Missing required anchor '{id}'.");
    }

    private static ScenarioSetupConfig Scenario() =>
        Load<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath);

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, $"Missing canonical asset '{path}'.");
        return asset;
    }

    private static string HashScenario()
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(
            sha.ComputeHash(File.ReadAllBytes(M02EstablishBaseConfigBuilder.ScenarioPath)))
            .Replace("-", string.Empty);
    }
}
