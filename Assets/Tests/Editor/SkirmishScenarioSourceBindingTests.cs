using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class SkirmishScenarioSourceBindingTests
{
    public static string RunFocusedValidation()
    {
        var tests = new SkirmishScenarioSourceBindingTests();
        tests.ReusesOnlySelectedExactPhysicalSource(false, false, true);
        tests.ReusesOnlySelectedExactPhysicalSource(true, false, false);
        tests.ReusesOnlySelectedExactPhysicalSource(false, true, false);
        tests.DesertBaseStandaloneSourceRequiresExactSelectedMetadata(false, false, true);
        tests.DesertBaseStandaloneSourceRequiresExactSelectedMetadata(true, false, false);
        tests.DesertBaseStandaloneSourceRequiresExactSelectedMetadata(false, true, false);
        return "[SkirmishScenarioSourceBinding] result=Passed cases=6";
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    public void DesertBaseStandaloneSourceRequiresExactSelectedMetadata(bool staleContent, bool wrongScenario, bool expected)
    {
        var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
        Assert.NotNull(definition);
        var loaded = Object.Instantiate(definition);
        using var world = new World("S004 standalone source validation");
        using var bootstrap = new OperationMapRuntimeBootstrapSceneSystemHelper(world);
        try
        {
            var em = world.EntityManager;
            var match = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetComponentData(match, new SkirmishMatchState { ScenarioIndex = 6 });
            FixedString64Bytes mission = SkirmishLaunchProjection.MissionId;
            FixedString64Bytes scenario = wrongScenario ? "scenario.skirmish.wrong" : SkirmishLaunchProjection.ScenarioId;
            Assert.IsTrue(bootstrap.TryPublish(definition, in scenario, in mission, 1,
                OperationMapReadinessFlags.Metadata, OperationMapReadinessFlags.Metadata, out var root, out var error), error);
            if (staleContent)
            {
                var serialized = new SerializedObject(loaded);
                serialized.FindProperty("contentHash").stringValue = new string('a', 64);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.AreEqual(expected, CampaignMissionOperationMapReuseUtility.TryValidateStandalonePhysicalSource(
                em, loaded, root, out error), error);
            Assert.AreEqual(expected ? 1 : 0, em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated);
        }
        finally { Object.DestroyImmediate(loaded); }
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    public void ReusesOnlySelectedExactPhysicalSource(bool staleContent, bool wrongScenario, bool expected)
    {
        var preset = SkirmishPresetConfig.Load(1);
        Assert.NotNull(preset);
        var logical = Object.Instantiate(preset.operationMap);
        var physical = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset");
        using var world = new World("Skirmish source binding validation");
        BlobAssetReference<OperationMapBlob> blob = default;
        try
        {
            if (staleContent)
            {
                var so = new SerializedObject(logical);
                so.FindProperty("sourceBinding").FindPropertyRelative("sourceContentHash").stringValue = new string('a',64);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.IsTrue(logical.TryCreatePersistentMetadataBlob(out blob,out string error),error);
            var em = world.EntityManager;
            var match = em.CreateEntity(typeof(SkirmishMatchState));
            em.SetComponentData(match,new SkirmishMatchState { ScenarioIndex = 1 });
            var root = em.CreateEntity(typeof(OperationMapRootComponent),typeof(ActiveOperationMapComponent),typeof(OperationMapMetadataComponent));
            em.SetComponentData(root,new ActiveOperationMapComponent
            {
                OperationMapId = new FixedString64Bytes(logical.OperationMapId),
                MissionId = new FixedString64Bytes("skirmish.city_crossroads"),
                ScenarioId = new FixedString64Bytes(wrongScenario ? "scenario.skirmish.wrong" : "scenario.skirmish.city_crossroads"),
                Generation = 3, SchemaVersion = logical.SchemaVersion, ContentVersion = logical.ContentVersion
            });
            em.SetComponentData(root,new OperationMapMetadataComponent { Blob=blob,Generation=3 });
            bool accepted = CampaignMissionOperationMapReuseUtility.TryReuse(em,physical,out var resolved,out error);
            Assert.AreEqual(expected,accepted,error);
            Assert.AreEqual(expected ? root : Entity.Null,resolved);
            Assert.AreEqual(expected ? 1 : 0,em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated);
        }
        finally { if(blob.IsCreated)blob.Dispose(); Object.DestroyImmediate(logical); }
    }
}
