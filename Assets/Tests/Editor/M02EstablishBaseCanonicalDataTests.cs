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
        "[M02EstablishBaseCanonicalDataValidation] result=Passed tests=15";
    private static readonly string[] CanonicalPaths =
    {
        M02EstablishBaseConfigBuilder.MissionPath,
        M02EstablishBaseConfigBuilder.ScenarioPath,
        M02EstablishBaseForwardPostWindowValidation.DefinitionPath,
        M02EstablishBaseConfigBuilder.MissionCatalogPath,
        M02EstablishBaseConfigBuilder.OperationMapCatalogPath
    };

    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseConfigBuilder.BuildCanonicalData();
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
            tests.RestrictionsAndAirfieldPersonnelMatchTheMissionScope();
            tests.CatalogsValidateAndResolveExactChapterSet();
            tests.MissionScenarioAndMapIdentitiesClose();
            tests.ContentPacksMirrorLogicalMapVersionsAndHashes();
            tests.CatalogsRejectMissingDuplicateAndStaleIdentities();
            tests.M01CatalogRefreshPreservesM02AndCanonicalBytes();
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
        string[] before = HashCanonicalAssets();
        M02EstablishBaseConfigBuilder.BuildCanonicalData();
        CollectionAssert.AreEqual(before, HashCanonicalAssets());
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

        Assert.AreEqual(15, anchors.Count);
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
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.BuildLotSize.x,
            zone.HalfWidthCells * 2);
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.BuildLotSize.y,
            zone.HalfHeightCells * 2);
        Assert.GreaterOrEqual(zone.HalfWidthCells * 2, footprint.x);
        Assert.GreaterOrEqual(zone.HalfHeightCells * 2, footprint.y);
        Assert.GreaterOrEqual(zone.HalfWidthCells * 2 - footprint.x, 4,
            "The Barracks lot must preserve two clear cells on each horizontal side.");
        Assert.GreaterOrEqual(zone.HalfHeightCells * 2 - footprint.y, 4,
            "The Barracks lot must preserve two clear cells on each vertical side.");
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
    public void RestrictionsAndAirfieldPersonnelMatchTheMissionScope()
    {
        ScenarioSetupConfig scenario = Scenario();
        Assert.IsFalse(scenario.Restrictions.BuildingDisabled);
        Assert.IsFalse(scenario.Restrictions.ProductionDisabled);
        Assert.IsFalse(scenario.Restrictions.EconomyDisabled);
        Assert.IsTrue(scenario.Restrictions.TransportDisabled);
        Assert.IsTrue(scenario.Restrictions.AirDisabled);
        Assert.AreEqual(1, scenario.AmbientPresentations.Length);
        ScenarioAmbientPresentationConfig personnel = scenario.AmbientPresentations[0];
        Assert.AreEqual("ambient.ch01.m02.base_personnel", personnel.PresentationId);
        Assert.AreEqual("anchor.ch01.m02.airfield_personnel_a", personnel.AnchorId);
        Assert.AreEqual("route.ch01.m02.base_patrol", personnel.RouteId);
        Assert.AreEqual(8, personnel.InstanceCount);
    }

    [Test]
    public void CatalogsValidateAndResolveExactChapterSet()
    {
        MissionDefinitionCatalogConfig missions = Load<MissionDefinitionCatalogConfig>(
            M02EstablishBaseConfigBuilder.MissionCatalogPath);
        OperationMapCatalogConfig maps = Load<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
        Assert.IsTrue(MissionDefinitionContractValidation.TryValidateCatalog(missions, out string error), error);
        Assert.IsTrue(maps.TryValidate(out error), error);
        Assert.AreEqual(2, missions.Entries.Length);
        Assert.AreEqual("saga.ch01.m01.first_contact", missions.Entries[0].MissionId);
        Assert.AreEqual(M02EstablishBaseConfigBuilder.MissionId, missions.Entries[1].MissionId);
        Assert.IsTrue(missions.TryResolve(M02EstablishBaseConfigBuilder.MissionId, out var mission));
        Assert.AreEqual(M02EstablishBaseConfigBuilder.MissionPath, AssetDatabase.GetAssetPath(mission));
        Assert.AreEqual(2, maps.Definitions.Length);
        Assert.AreEqual("opmap.ch01.district_edge_01", maps.Definitions[0].OperationMapId);
        Assert.AreEqual("opmap.ch01.forward_post_01", maps.Definitions[1].OperationMapId);
        Assert.IsTrue(maps.TryResolve("opmap.ch01.forward_post_01", out var map));
        Assert.AreEqual(M02EstablishBaseForwardPostWindowValidation.DefinitionPath,
            AssetDatabase.GetAssetPath(map));
    }

    [Test]
    public void MissionScenarioAndMapIdentitiesClose()
    {
        MissionDefinitionConfig mission = Load<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        ScenarioSetupConfig scenario = Scenario();
        OperationMapDefinition map = Load<OperationMapDefinition>(
            M02EstablishBaseForwardPostWindowValidation.DefinitionPath);
        Assert.AreEqual(mission.ScenarioId, scenario.ScenarioId);
        Assert.AreEqual(mission.OperationMapId, scenario.OperationMapId);
        Assert.AreEqual(scenario.OperationMapId, map.OperationMapId);
    }

    [Test]
    public void ContentPacksMirrorLogicalMapVersionsAndHashes()
    {
        OperationMapCatalogConfig catalog = Load<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
        for (int index = 0; index < catalog.Definitions.Length; index++)
        {
            OperationMapDefinition map = catalog.Definitions[index];
            OperationMapCatalogEntryConfig entry = catalog.Entries[index];
            Assert.AreSame(map, entry.Definition);
            Assert.AreEqual("opmap-pack." + map.OperationMapId.Substring(6),
                entry.ContentPack.ContentPackId);
            Assert.AreEqual(OperationMapDeliveryKind.BuiltInLocal, entry.ContentPack.DeliveryKind);
            Assert.AreEqual(map.ContentVersion, entry.ContentPack.ContentVersion);
            Assert.AreEqual(map.ContentHash, entry.ContentPack.ContentHash);
        }
    }

    [Test]
    public void CatalogsRejectMissingDuplicateAndStaleIdentities()
    {
        MissionDefinitionConfig m01 = Load<MissionDefinitionConfig>(
            M01FirstContactConfigBuilder.MissionPath);
        MissionDefinitionCatalogConfig missionCatalog =
            ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        OperationMapCatalogConfig mapCatalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        try
        {
            SerializedObject missions = new(missionCatalog);
            SerializedProperty entries = missions.FindProperty("entries");
            entries.arraySize = 2;
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("missionId").stringValue = m01.MissionId;
                entry.FindPropertyRelative("definition").objectReferenceValue = m01;
            }
            missions.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateCatalog(
                missionCatalog, out string duplicateError));
            StringAssert.Contains("Duplicate", duplicateError);

            entries.arraySize = 1;
            SerializedProperty stale = entries.GetArrayElementAtIndex(0);
            stale.FindPropertyRelative("missionId").stringValue = M02EstablishBaseConfigBuilder.MissionId;
            stale.FindPropertyRelative("definition").objectReferenceValue = m01;
            missions.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateCatalog(
                missionCatalog, out string staleError));
            StringAssert.Contains("does not match", staleError);

            SerializedObject maps = new(mapCatalog);
            SerializedProperty definitions = maps.FindProperty("definitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = null;
            maps.FindProperty("entries").arraySize = 1;
            maps.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(mapCatalog.TryValidate(out string missingError));
            StringAssert.Contains("missing", missingError);

            OperationMapDefinition m01Map = Load<OperationMapDefinition>(
                M01FirstContactConfigBuilder.OperationMapPath);
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = m01Map;
            SerializedProperty mapEntry = maps.FindProperty("entries").GetArrayElementAtIndex(0);
            mapEntry.FindPropertyRelative("definition").objectReferenceValue = m01Map;
            SerializedProperty contentPack = mapEntry.FindPropertyRelative("contentPack");
            contentPack.FindPropertyRelative("contentPackId").stringValue =
                "opmap-pack.ch01.district_edge_01";
            contentPack.FindPropertyRelative("deliveryKind").intValue =
                (int)OperationMapDeliveryKind.BuiltInLocal;
            contentPack.FindPropertyRelative("contentVersion").intValue = m01Map.ContentVersion;
            contentPack.FindPropertyRelative("contentHash").stringValue = "stale";
            maps.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(mapCatalog.TryValidate(out string stalePackError));
            StringAssert.Contains("version and hash", stalePackError);

            MissionDefinitionCatalogConfig liveMissions = Load<MissionDefinitionCatalogConfig>(
                M02EstablishBaseConfigBuilder.MissionCatalogPath);
            OperationMapCatalogConfig liveMaps = Load<OperationMapCatalogConfig>(
                M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
            Assert.IsFalse(liveMissions.TryResolve("saga.ch01.m03.stale", out _));
            Assert.IsFalse(liveMaps.TryResolve("opmap.ch01.stale", out _));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(missionCatalog);
            UnityEngine.Object.DestroyImmediate(mapCatalog);
        }
    }

    [Test]
    public void M01CatalogRefreshPreservesM02AndCanonicalBytes()
    {
        string[] before = HashCatalogs();
        M01FirstContactConfigBuilder.RefreshChapterCatalogs();
        CollectionAssert.AreEqual(before, HashCatalogs());
        Assert.IsTrue(Load<MissionDefinitionCatalogConfig>(
            M02EstablishBaseConfigBuilder.MissionCatalogPath).TryResolve(
                M02EstablishBaseConfigBuilder.MissionId, out _));
        Assert.IsTrue(Load<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath).TryResolve(
                "opmap.ch01.forward_post_01", out _));
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

    private static string[] HashCanonicalAssets()
    {
        string[] hashes = new string[CanonicalPaths.Length];
        for (int index = 0; index < CanonicalPaths.Length; index++)
            hashes[index] = HashFile(CanonicalPaths[index]);
        return hashes;
    }

    private static string[] HashCatalogs() => new[]
    {
        HashFile(M02EstablishBaseConfigBuilder.MissionCatalogPath),
        HashFile(M02EstablishBaseConfigBuilder.OperationMapCatalogPath)
    };

    private static string HashFile(string path)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(
            sha.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty);
    }
}
