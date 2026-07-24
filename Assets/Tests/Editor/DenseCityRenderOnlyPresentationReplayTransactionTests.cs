using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityRenderOnlyPresentationReplayTransactionTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityRenderOnlyPresentationReplayTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string MaterialPath = TempRoot + "/material.mat";
    private const string OtherMaterialPath = TempRoot + "/other.mat";
    private const string ThirdMaterialPath = TempRoot + "/third.mat";
    private const string PrefabPath = TempRoot + "/prop.prefab";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityRenderOnlyPresentationReplayTransactionTests();
        Action[] tests =
        {
            suite.Realize_ReplaysSealedRenderOnlyRecordsInStableIdentityOrder,
            suite.Realize_LaterFailureRemovesEveryObjectCreatedByReplay
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

        Debug.Log($"[DenseCityRenderOnlyReplayValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void Realize_ReplaysSealedRenderOnlyRecordsInStableIdentityOrder()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        using var records = new DenseCityGenerationRecordSet(1, 1, 4);
        try
        {
            GameObject prefab = CreatePrefab(MaterialPath);
            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
            records.AddRenderOnlyPresentation(CreateRecord(
                metadata,
                1,
                DenseCityPresentationCategory.Vegetation,
                new Vector3(9f, 0f, 3f)));
            records.AddRenderOnlyPresentation(CreateRecord(
                metadata,
                0,
                DenseCityPresentationCategory.Prop,
                new Vector3(2f, 0f, 4f)));
            records.Seal();
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);

            IReadOnlyList<Transform> realized =
                DenseCityRenderOnlyPresentationReplayTransaction.Realize(records, hierarchy);

            Assert.That(realized, Has.Count.EqualTo(2));
            Assert.That(realized[0].name, Does.EndWith("000000"));
            Assert.That(realized[1].name, Does.EndWith("000001"));
            Assert.That(
                realized[0].parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop)));
            Assert.That(
                realized[1].parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Vegetation)));
            Assert.That(realized[0].GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(realized[1].GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_LaterFailureRemovesEveryObjectCreatedByReplay()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        using var records = new DenseCityGenerationRecordSet(1, 1, 4);
        try
        {
            GameObject prefab = CreatePrefab(MaterialPath);
            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
            records.AddRenderOnlyPresentation(CreateRecord(
                metadata,
                0,
                DenseCityPresentationCategory.Prop,
                Vector3.zero));
            Material other = CreateMaterial(OtherMaterialPath);
            Material third = CreateMaterial(ThirdMaterialPath);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(other, out string otherGuid, out long _),
                Is.True);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(third, out string thirdGuid, out long _),
                Is.True);
            records.AddRenderOnlyPresentation(new DenseCityPresentationBakeRecord(
                new DenseCityRecordIdentity(
                    "dense-city-v1",
                    42,
                    0,
                    "prop-visual",
                    1,
                    metadata.PrefabAssetGuid,
                    metadata.PrefabLocalId),
                DenseCityPresentationCategory.Prop,
                metadata.PrefabAssetGuid,
                null,
                new[] { otherGuid, thirdGuid },
                Matrix4x4.TRS(Vector3.right, Quaternion.identity, Vector3.one),
                true,
                true,
                1));
            records.Seal();
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform props = hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop);

            Assert.That(
                () => DenseCityRenderOnlyPresentationReplayTransaction.Realize(records, hierarchy),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("material identity"));
            Assert.That(props.childCount, Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static DenseCityPresentationBakeRecord CreateRecord(
        DenseCityVisualAssetMetadata metadata,
        int sequence,
        DenseCityPresentationCategory category,
        Vector3 position) =>
        DenseCityRenderOnlyPresentationRecordFactory.Create(
            new DenseCityRenderOnlyPresentationRecordInput(
                "dense-city-v1",
                42,
                0,
                sequence,
                "prop-visual",
                category,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId,
                metadata.MaterialAssetGuids,
                Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
                true,
                true,
                1));

    private static GameObject CreatePrefab(string materialPath)
    {
        Material material = CreateMaterial(materialPath);
        var source = new GameObject("PropSource");
        source.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        source.AddComponent<MeshRenderer>().sharedMaterial = material;
        source.AddComponent<BoxCollider>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
        UnityEngine.Object.DestroyImmediate(source);
        return prefab;
    }

    private static Material CreateMaterial(string path)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static DenseCityPresentationHierarchyContext CreateHierarchy(
        Scene mapScene,
        Scene entityScene)
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
