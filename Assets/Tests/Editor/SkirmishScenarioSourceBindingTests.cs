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
        return "[SkirmishScenarioSourceBinding] result=Passed cases=3";
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
