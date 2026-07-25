using System;
using System.Collections.Generic;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityPresentationReplayTransactionTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityPresentationReplayTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityPresentationReplayTransactionTests();
        Action[] tests =
        {
            suite.Realize_ReplaysBuildingsAndRenderOnlyPresentationsAsOneSet,
            suite.Realize_LateFailurePreservesAcceptedAndRollsBackNewPresentationSet
        };

        for (int index = 0; index < tests.Length; index++)
        {
            suite.SetUp();
            try
            {
                tests[index]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[DenseCityPresentationReplayValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown() => AssetDatabase.DeleteAsset(TempRoot);

    [Test]
    public void Realize_ReplaysBuildingsAndRenderOnlyPresentationsAsOneSet()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityGenerationRecordSet records = CreateRecords(false);
            DenseCityGenerationRecordSnapshot snapshot = records.CreateSnapshot();
            records.Dispose();
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);

            DenseCityRealizedPresentationSet realized = DenseCityPresentationReplayTransaction.Realize(
                "opmap.skirmish.presentation_replay_test",
                snapshot,
                hierarchy,
                DenseCityBuildingDefinitionLibrary.LoadExisting());

            Assert.That(realized.Buildings, Has.Count.EqualTo(1));
            Assert.That(realized.RenderOnly, Has.Count.EqualTo(2));
            Assert.That(
                realized.RenderOnly[0].parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop)));
            Assert.That(
                realized.RenderOnly[1].parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Vegetation)));

            foreach (DenseCityPresentationIdentityAuthoring identity in
                     entityScene.GetRootGameObjects()
                         .SelectMany(root =>
                             root.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true))
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(identity);
            }
            DenseCityGeneratedRootAuthoring entityRoot = entityScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .Single(root => root.Role == DenseCityGeneratedRootRole.EntityPresentationSource);
            DenseCityCandidatePresentationIdentityBackfill.BackfillResult backfill =
                DenseCityCandidatePresentationIdentityBackfill.Apply(snapshot, hierarchy, entityRoot);

            Assert.That(backfill.Buildings, Is.EqualTo(1));
            Assert.That(backfill.RenderOnly, Is.EqualTo(2));
            Assert.That(backfill.Added, Is.EqualTo(3));
            Assert.That(backfill.Existing, Is.Zero);
            Assert.That(
                entityRoot.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true),
                Has.Length.EqualTo(3));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_LateFailurePreservesAcceptedAndRollsBackNewPresentationSet()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            using DenseCityGenerationRecordSet records = CreateRecords(true);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform buildings = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Shop);
            Transform props = hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop);
            var acceptedBuilding = new GameObject("AcceptedBuildingPresentation");
            acceptedBuilding.transform.SetParent(buildings, false);
            var acceptedProp = new GameObject("AcceptedPropPresentation");
            acceptedProp.transform.SetParent(props, false);

            Assert.That(
                () => DenseCityPresentationReplayTransaction.Realize(
                    "opmap.skirmish.presentation_replay_test",
                    records,
                    hierarchy,
                    DenseCityBuildingDefinitionLibrary.LoadExisting()),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("material identity"));
            Assert.That(buildings.childCount, Is.EqualTo(1));
            Assert.That(buildings.GetChild(0).gameObject, Is.SameAs(acceptedBuilding));
            Assert.That(props.childCount, Is.EqualTo(1));
            Assert.That(props.GetChild(0).gameObject, Is.SameAs(acceptedProp));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static DenseCityGenerationRecordSet CreateRecords(bool mismatchSecondRenderOnly)
    {
        DenseCityVisualAssetMetadata intact = CreatePrefabMetadata("Intact", "intact-building");
        DenseCityVisualAssetMetadata destroyed = CreatePrefabMetadata("Destroyed", "destroyed-building");
        DenseCityBuildingDefinitionLibrary definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();
        DenseCityBuildingRecordGroup building = DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                20,
                intact.PrefabAssetGuid,
                intact.PrefabLocalId,
                destroyed.PrefabAssetGuid,
                destroyed.PrefabLocalId,
                intact.MaterialAssetGuids.ToArray(),
                destroyed.MaterialAssetGuids.ToArray(),
                Matrix4x4.TRS(new Vector3(12f, 2f, -8f), Quaternion.identity, Vector3.one),
                new Vector2Int(10, 20),
                new Vector2Int(8, 6),
                new Vector2(8f, 6f),
                2f,
                new Bounds(new Vector3(12f, 4f, -8f), new Vector3(6f, 4f, 8f)),
                Vector3.right,
                GeneratedCityBuildingRole.Shop,
                definitions.ResolveAssetGuid(GeneratedCityBuildingRole.Shop),
                0,
                500f,
                1,
                0,
                Vector2Int.zero));

        DenseCityVisualAssetMetadata prop = CreatePrefabMetadata("Prop", "prop");
        DenseCityVisualAssetMetadata vegetation = CreatePrefabMetadata("Vegetation", "vegetation");
        IReadOnlyList<string> secondMaterials = vegetation.MaterialAssetGuids;
        if (mismatchSecondRenderOnly)
        {
            Material mismatchA = CreateMaterial(TempRoot + "/mismatch-a.mat");
            Material mismatchB = CreateMaterial(TempRoot + "/mismatch-b.mat");
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    mismatchA,
                    out string mismatchGuidA,
                    out long _),
                Is.True);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    mismatchB,
                    out string mismatchGuidB,
                    out long _),
                Is.True);
            secondMaterials = new[] { mismatchGuidA, mismatchGuidB };
        }

        var records = new DenseCityGenerationRecordSet(1, 2, 4);
        DenseCityBuildingRecordFactory.Add(records, building);
        records.AddRenderOnlyPresentation(CreateRenderOnlyRecord(
            prop,
            prop.MaterialAssetGuids,
            0,
            DenseCityPresentationCategory.Prop));
        records.AddRenderOnlyPresentation(CreateRenderOnlyRecord(
            vegetation,
            secondMaterials,
            1,
            DenseCityPresentationCategory.Vegetation));
        records.Seal();
        return records;
    }

    private static DenseCityPresentationBakeRecord CreateRenderOnlyRecord(
        DenseCityVisualAssetMetadata metadata,
        IReadOnlyList<string> materialGuids,
        int sequence,
        DenseCityPresentationCategory category) =>
        DenseCityRenderOnlyPresentationRecordFactory.Create(
            new DenseCityRenderOnlyPresentationRecordInput(
                "dense-city-v1",
                42,
                0,
                sequence,
                "presentation-visual",
                category,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId,
                materialGuids,
                Matrix4x4.TRS(new Vector3(sequence * 4f, 0f, 3f), Quaternion.identity, Vector3.one),
                true,
                true,
                1));

    private static DenseCityVisualAssetMetadata CreatePrefabMetadata(string sourceName, string key)
    {
        Material material = CreateMaterial(TempRoot + "/" + key + ".mat");
        var source = new GameObject(sourceName);
        source.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        source.AddComponent<MeshRenderer>().sharedMaterial = material;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, TempRoot + "/" + key + ".prefab");
        UnityEngine.Object.DestroyImmediate(source);
        return DenseCityVisualAssetMetadataExtractor.Extract(prefab);
    }

    private static Material CreateMaterial(string path)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static DenseCityPresentationHierarchyContext CreateHierarchy(Scene mapScene, Scene entityScene)
    {
        var roots = DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            "dense-city:test:42",
            "dense-city-v1",
            1,
            42,
            Hash);
        return DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);
    }

    private static (Scene MapScene, Scene EntityScene) CreateScenePair()
    {
        Scene mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(mapScene, MapScenePath), Is.True);
        Scene entityScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Assert.That(EditorSceneManager.SaveScene(entityScene, EntityScenePath), Is.True);
        return (mapScene, entityScene);
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);
            current = next;
        }
    }
}
