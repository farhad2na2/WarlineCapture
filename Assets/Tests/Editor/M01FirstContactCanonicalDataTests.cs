using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class M01FirstContactCanonicalDataTests
{
    private const string PassMarker = "[M01FirstContactCanonicalDataValidation] result=Passed tests=13";
    private static readonly string[] CanonicalPaths =
    {
        M01FirstContactConfigBuilder.CatalogPath,
        M01FirstContactConfigBuilder.MissionPath,
        M01FirstContactConfigBuilder.ScenarioPath
    };

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactConfigBuilder.Build();
            M01FirstContactCanonicalDataTests tests = new();
            tests.RegenerationIsByteStable();
            tests.MissionDefinitionPassesContractValidation();
            tests.ScenarioPassesContractValidation();
            tests.CatalogResolvesOnlyCanonicalM01();
            tests.CrossReferencesUseFrozenIdentities();
            tests.NarrativeReferencesUseFrozenIdentities();
            tests.ObjectivesUseFrozenRulesAndIdentities();
            tests.StarsUseFrozenRulesAndFourMinuteThreshold();
            tests.RewardsUseApprovedFirstClearAndReplayValuesWithoutIntel();
            tests.PatrolContainsExactFirstLaunchContinuityIdentities();
            tests.PatrolExcludesReservedQassemAndHeavyGunnerIdentities();
            tests.FriendlySquadContainsExactApprovedIdentities();
            tests.ScenarioContainsRequiredAnchorsRestrictionsAndAmbientCivilians();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactCanonicalDataValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RegenerationIsByteStable()
    {
        string[] before = HashCanonicalAssets();
        M01FirstContactConfigBuilder.Build();
        CollectionAssert.AreEqual(before, HashCanonicalAssets());
    }

    [Test]
    public void MissionDefinitionPassesContractValidation()
    {
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath);
        Assert.IsTrue(MissionDefinitionContractValidation.TryValidateDefinition(mission, out string error), error);
    }

    [Test]
    public void ScenarioPassesContractValidation()
    {
        ScenarioSetupConfig scenario = Load<ScenarioSetupConfig>(M01FirstContactConfigBuilder.ScenarioPath);
        Assert.IsTrue(scenario.TryValidate(out string error), error);
    }

    [Test]
    public void CatalogResolvesOnlyCanonicalM01()
    {
        MissionDefinitionCatalogConfig catalog =
            Load<MissionDefinitionCatalogConfig>(M01FirstContactConfigBuilder.CatalogPath);
        Assert.IsTrue(MissionDefinitionContractValidation.TryValidateCatalog(catalog, out string error), error);
        Assert.IsTrue(catalog.TryResolve(MissionDefinitionContractValidation.FirstContactMissionId, out var mission));
        Assert.AreEqual(M01FirstContactConfigBuilder.MissionPath, AssetDatabase.GetAssetPath(mission));
        Assert.IsFalse(catalog.TryResolve("saga.ch01.m02.missing", out _));
    }

    [Test]
    public void CrossReferencesUseFrozenIdentities()
    {
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath);
        ScenarioSetupConfig scenario = Load<ScenarioSetupConfig>(M01FirstContactConfigBuilder.ScenarioPath);
        Assert.AreEqual("saga.ch01.m01.first_contact", mission.MissionId);
        Assert.AreEqual("scenario.ch01.m01.first_contact", mission.ScenarioId);
        Assert.AreEqual(mission.ScenarioId, scenario.ScenarioId);
        Assert.AreEqual("opmap.ch01.district_edge_01", mission.OperationMapId);
        Assert.AreEqual(mission.OperationMapId, scenario.OperationMapId);
    }

    [Test]
    public void NarrativeReferencesUseFrozenIdentities()
    {
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath);
        Assert.AreEqual("seq.ch01.m01.brief", mission.BriefingSequenceId);
        Assert.AreEqual("seq.ch01.m01.comms", mission.CommsSequenceId);
        Assert.AreEqual("seq.ch01.m01.debrief", mission.DebriefSequenceId);
    }

    [Test]
    public void ObjectivesUseFrozenRulesAndIdentities()
    {
        ReadOnlySpan<MissionObjectiveDefinitionConfig> objectives =
            Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath).Objectives;
        Assert.AreEqual(2, objectives.Length);
        Assert.AreEqual("obj.ch01.m01.destroy_patrol", objectives[0].ObjectiveId);
        Assert.AreEqual(MissionObjectiveRuleKind.DestroyMissionRole, objectives[0].Rule);
        Assert.AreEqual("role.hostile.patrol", objectives[0].MissionRoleId);
        Assert.AreEqual(3, objectives[0].RequiredCount);
        Assert.IsFalse(objectives[0].FailureOnRuleBreak);
        Assert.AreEqual("obj.ch01.m01.keep_command_squad_alive", objectives[1].ObjectiveId);
        Assert.AreEqual(MissionObjectiveRuleKind.ProtectMissionRole, objectives[1].Rule);
        Assert.AreEqual("role.friendly.command_squad", objectives[1].MissionRoleId);
        Assert.IsTrue(objectives[1].FailureOnRuleBreak);
    }

    [Test]
    public void StarsUseFrozenRulesAndFourMinuteThreshold()
    {
        ReadOnlySpan<MissionStarDefinitionConfig> stars =
            Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath).Stars;
        Assert.AreEqual(3, stars.Length);
        Assert.AreEqual(MissionStarRuleKind.CompleteMission, stars[0].Rule);
        Assert.AreEqual(MissionStarRuleKind.NoSquadLoss, stars[1].Rule);
        Assert.AreEqual(MissionStarRuleKind.CompleteUnderMilliseconds, stars[2].Rule);
        Assert.AreEqual(240000, stars[2].Threshold);
    }

    [Test]
    public void RewardsUseApprovedFirstClearAndReplayValuesWithoutIntel()
    {
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(M01FirstContactConfigBuilder.MissionPath);
        ReadOnlySpan<MissionRewardDefinitionConfig> first = mission.FirstClearRewards;
        ReadOnlySpan<MissionRewardDefinitionConfig> replay = mission.ReplayRewards;
        Assert.AreEqual(2, first.Length);
        Assert.AreEqual(MissionRewardKind.None, first[0].Kind);
        Assert.AreEqual("reward.commander_xp", first[0].RewardConfigId);
        Assert.AreEqual(260, first[0].Amount);
        Assert.AreEqual(MissionRewardKind.Credits, first[1].Kind);
        Assert.AreEqual(1200, first[1].Amount);
        Assert.AreEqual(1, replay.Length);
        Assert.AreEqual(MissionRewardKind.Credits, replay[0].Kind);
        Assert.AreEqual(250, replay[0].Amount);
        AssertNoRewardKind(first, MissionRewardKind.Intel);
        AssertNoRewardKind(replay, MissionRewardKind.Intel);
    }

    [Test]
    public void PatrolContainsExactFirstLaunchContinuityIdentities()
    {
        ScenarioUnitGroupConfig patrol = Group("group.ch01.m01.hostile_patrol");
        AssertExactUnitGuids(patrol.Units, new[]
        {
            "fe23cbf9678344f4b182169b49fe68b6",
            "01045d2a58ec4359b395696309684ffa",
            "8093159068194fc187efe5c356116e9b"
        });
        AssertGuidPath("fe23cbf9678344f4b182169b49fe68b6",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Male_03_Config.asset");
        AssertGuidPath("01045d2a58ec4359b395696309684ffa",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_01_Config.asset");
        AssertGuidPath("8093159068194fc187efe5c356116e9b",
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Insurgent_Female_02_Config.asset");
    }

    [Test]
    public void PatrolExcludesReservedQassemAndHeavyGunnerIdentities()
    {
        ReadOnlySpan<ScenarioUnitEntryConfig> units = Group("group.ch01.m01.hostile_patrol").Units;
        AssertGuidAbsent(units, "5e83cd16726343b3b2ffd9fbb49cfa29");
        AssertGuidAbsent(units, "6095a30179ca49bb9e2759a1ace9da39");
    }

    [Test]
    public void FriendlySquadContainsExactApprovedIdentities()
    {
        AssertExactUnitGuids(Group("group.ch01.m01.command_squad").Units, new[]
        {
            "70d525bdf4894529869cd1402ff5d62e",
            "a3d36fd8847164cc596e0b5ba7bd9bb9",
            "970069fef3e4437195c3225a1615e384",
            "b9481cc1ec42499c96846b5d161ac7b2"
        });
    }

    [Test]
    public void ScenarioContainsRequiredAnchorsRestrictionsAndAmbientCivilians()
    {
        ScenarioSetupConfig scenario = Load<ScenarioSetupConfig>(M01FirstContactConfigBuilder.ScenarioPath);
        Assert.AreEqual(11, scenario.RequiredAnchors.Length);
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.player_spawn");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.camera_start");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.move_target");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.patrol_spawn");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.patrol_route_a");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.patrol_route_b");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.patrol_route_c");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.patrol_objective");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.civilian_safe_zone");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.civilian_evacuation");
        AssertAnchor(scenario.RequiredAnchors, "anchor.ch01.m01.minimap_start");
        Assert.IsTrue(scenario.Restrictions.BuildingDisabled);
        Assert.IsTrue(scenario.Restrictions.ProductionDisabled);
        Assert.IsTrue(scenario.Restrictions.EconomyDisabled);
        Assert.IsTrue(scenario.Restrictions.TransportDisabled);
        Assert.IsTrue(scenario.Restrictions.AirDisabled);
        Assert.AreEqual(1, scenario.AmbientPresentations.Length);
        Assert.AreEqual(24, scenario.AmbientPresentations[0].InstanceCount);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, $"Missing canonical asset '{path}'.");
        return asset;
    }

    private static ScenarioUnitGroupConfig Group(string id)
    {
        foreach (ScenarioUnitGroupConfig group in
                 Load<ScenarioSetupConfig>(M01FirstContactConfigBuilder.ScenarioPath).UnitGroups)
            if (group.GroupId == id) return group;
        Assert.Fail($"Missing unit group '{id}'.");
        return default;
    }

    private static string[] HashCanonicalAssets()
    {
        string[] hashes = new string[CanonicalPaths.Length];
        using SHA256 sha = SHA256.Create();
        for (int index = 0; index < CanonicalPaths.Length; index++)
            hashes[index] = BitConverter.ToString(
                sha.ComputeHash(File.ReadAllBytes(CanonicalPaths[index]))).Replace("-", string.Empty);
        return hashes;
    }

    private static void AssertExactUnitGuids(ReadOnlySpan<ScenarioUnitEntryConfig> units, string[] expected)
    {
        Assert.AreEqual(expected.Length, units.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.AreEqual(expected[index], units[index].ExpectedAssetGuid);
            Assert.AreEqual(1, units[index].Count);
        }
    }

    private static void AssertGuidPath(string guid, string expectedPath) =>
        Assert.AreEqual(expectedPath, AssetDatabase.GUIDToAssetPath(guid));

    private static void AssertGuidAbsent(ReadOnlySpan<ScenarioUnitEntryConfig> units, string guid)
    {
        foreach (ScenarioUnitEntryConfig unit in units)
            Assert.AreNotEqual(guid, unit.ExpectedAssetGuid);
    }

    private static void AssertNoRewardKind(
        ReadOnlySpan<MissionRewardDefinitionConfig> rewards, MissionRewardKind kind)
    {
        foreach (MissionRewardDefinitionConfig reward in rewards) Assert.AreNotEqual(kind, reward.Kind);
    }

    private static void AssertAnchor(ReadOnlySpan<ScenarioAnchorRequirementConfig> anchors, string id)
    {
        foreach (ScenarioAnchorRequirementConfig anchor in anchors)
            if (anchor.AnchorId == id) return;
        Assert.Fail($"Missing required anchor '{id}'.");
    }
}
