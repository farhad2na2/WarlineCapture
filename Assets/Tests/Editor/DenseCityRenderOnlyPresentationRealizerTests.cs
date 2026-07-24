using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityRenderOnlyPresentationRealizerTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityRenderOnlyPresentationRealizerTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string MaterialPath = TempRoot + "/material.mat";
    private const string OtherMaterialPath = TempRoot + "/other.mat";
    private const string ThirdMaterialPath = TempRoot + "/third.mat";
    private const string PrefabPath = TempRoot + "/prop.prefab";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

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
    public void Realize_UsesRecordedPrefabMaterialTransformAndSemanticParent()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            GameObject prefab = CreatePrefab(MaterialPath);
            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(11f, 3f, -7f),
                Quaternion.Euler(0f, 37f, 0f),
                new Vector3(2f, 1.5f, 0.75f));
            DenseCityPresentationBakeRecord record = CreateRecord(metadata, matrix);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);

            Transform realized = DenseCityRenderOnlyPresentationRealizer.Realize(record, hierarchy);

            Assert.That(
                realized.parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop)));
            AssertMatrix(record.WorldMatrix, realized.localToWorldMatrix);
            Assert.That(
                PrefabUtility.GetCorrespondingObjectFromSource(realized.gameObject),
                Is.SameAs(prefab));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_AppliesRecordedSingleMaterialOverride()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            GameObject prefab = CreatePrefab(MaterialPath);
            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
            Material other = CreateMaterial(OtherMaterialPath);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(other, out string otherGuid, out long _),
                Is.True);
            var record = new DenseCityPresentationBakeRecord(
                new DenseCityRecordIdentity(
                    "dense-city-v1",
                    42,
                    0,
                    "prop-visual",
                    0,
                    metadata.PrefabAssetGuid,
                    metadata.PrefabLocalId),
                DenseCityPresentationCategory.Prop,
                metadata.PrefabAssetGuid,
                null,
                new[] { otherGuid },
                Matrix4x4.identity,
                true,
                true,
                1);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform realized = DenseCityRenderOnlyPresentationRealizer.Realize(record, hierarchy);

            Assert.That(realized.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(other));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_AmbiguousMaterialIdentityMismatchFailsAndRemovesPartialInstance()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            GameObject prefab = CreatePrefab(MaterialPath);
            DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
            Material other = CreateMaterial(OtherMaterialPath);
            Material third = CreateMaterial(ThirdMaterialPath);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(other, out string otherGuid, out long _),
                Is.True);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(third, out string thirdGuid, out long _),
                Is.True);
            var record = new DenseCityPresentationBakeRecord(
                new DenseCityRecordIdentity(
                    "dense-city-v1",
                    42,
                    0,
                    "prop-visual",
                    0,
                    metadata.PrefabAssetGuid,
                    metadata.PrefabLocalId),
                DenseCityPresentationCategory.Prop,
                metadata.PrefabAssetGuid,
                null,
                new[] { otherGuid, thirdGuid },
                Matrix4x4.identity,
                true,
                true,
                1);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform props = hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop);

            Assert.That(
                () => DenseCityRenderOnlyPresentationRealizer.Realize(record, hierarchy),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("material identity"));
            Assert.That(props.childCount, Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void RequireMatrixParity_AcceptsOneUlpAtLargeCoordinatesButRejectsDrift()
    {
        GameObject prefab = CreatePrefab(MaterialPath);
        DenseCityVisualAssetMetadata metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
        Matrix4x4 expected = Matrix4x4.identity;
        expected[12] = 1704.95789f;
        DenseCityPresentationBakeRecord record = CreateRecord(metadata, expected);
        Matrix4x4 oneUlp = expected;
        oneUlp[12] = BitConverter.Int32BitsToSingle(
            BitConverter.SingleToInt32Bits(expected[12]) + 1);

        Assert.That(
            () => DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(oneUlp, record),
            Throws.Nothing);

        Matrix4x4 drifted = expected;
        drifted[12] += 0.01f;
        Assert.That(
            () => DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(drifted, record),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("matrix[12]"));
    }

    private static GameObject CreatePrefab(string materialPath)
    {
        Material material = CreateMaterial(materialPath);
        var source = new GameObject("PropSource");
        var filter = source.AddComponent<MeshFilter>();
        filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        source.AddComponent<MeshRenderer>().sharedMaterial = material;
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

    private static DenseCityPresentationBakeRecord CreateRecord(
        DenseCityVisualAssetMetadata metadata,
        Matrix4x4 matrix) =>
        DenseCityRenderOnlyPresentationRecordFactory.Create(
            new DenseCityRenderOnlyPresentationRecordInput(
                "dense-city-v1",
                42,
                0,
                0,
                "prop-visual",
                DenseCityPresentationCategory.Prop,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId,
                metadata.MaterialAssetGuids,
                matrix,
                true,
                true,
                1));

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

    private static void AssertMatrix(Matrix4x4 expected, Matrix4x4 actual)
    {
        for (int index = 0; index < 16; index++)
            Assert.That(actual[index], Is.EqualTo(expected[index]).Within(0.0001f), $"matrix[{index}]");
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
