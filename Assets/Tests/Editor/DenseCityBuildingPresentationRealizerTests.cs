using System;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityBuildingPresentationRealizerTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityBuildingPresentationRealizerTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string IntactMaterialPath = TempRoot + "/intact.mat";
    private const string DestroyedMaterialPath = TempRoot + "/destroyed.mat";
    private const string EmbeddedDestroyedMaterialPath = TempRoot + "/embedded-destroyed.mat";
    private const string OtherMaterialPath = TempRoot + "/other.mat";
    private const string IntactPrefabPath = TempRoot + "/intact.prefab";
    private const string DestroyedPrefabPath = TempRoot + "/destroyed.prefab";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string OperationMapId = "opmap.skirmish.building_realizer_test";

    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(
                nameof(Realize_CreatesValidatedOwnerAndPrefabConnectedVisualStates),
                test => test.Realize_CreatesValidatedOwnerAndPrefabConnectedVisualStates());
            RunCase(
                nameof(Realize_LateVisualMismatchRemovesCompleteBuildingOwner),
                test => test.Realize_LateVisualMismatchRemovesCompleteBuildingOwner());
            RunCase(
                nameof(Realize_RemovesEmbeddedDestroyedAlternativeFromIntactVisual),
                test => test.Realize_RemovesEmbeddedDestroyedAlternativeFromIntactVisual());
            Debug.Log("[DenseCityBuildingPresentationRealizerFocusedValidation] result=Passed tests=3");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[DenseCityBuildingPresentationRealizerFocusedValidation] result=Failed");
            throw;
        }
    }

    private static void RunCase(
        string name,
        Action<DenseCityBuildingPresentationRealizerTests> action)
    {
        var test = new DenseCityBuildingPresentationRealizerTests();
        test.SetUp();
        try
        {
            action(test);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Focused case '{name}' failed.", exception);
        }
        finally
        {
            test.TearDown();
        }
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
    public void Realize_CreatesValidatedOwnerAndPrefabConnectedVisualStates()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityBuildingRecordGroup group = CreateGroup();
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            DenseCityBuildingDefinitionLibrary definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();

            DenseCityRealizedBuildingPresentation realized =
                DenseCityBuildingPresentationRealizer.Realize(
                    OperationMapId,
                    group.Building,
                    group.IntactPresentation,
                    group.DestroyedPresentation,
                    hierarchy,
                    definitions,
                    null,
                    432);

            Assert.That(realized.Authoring.TryValidate(out string error), Is.True, error);
            Assert.That(realized.Authoring.PlacementIndex, Is.EqualTo(432));
            Assert.That(realized.Authoring.StableId, Is.EqualTo(group.Building.Identity.CreateBakedStableId()));
            DenseCityPresentationIdentityAuthoring presentationIdentity =
                realized.Authoring.GetComponent<DenseCityPresentationIdentityAuthoring>();
            Assert.That(presentationIdentity, Is.Not.Null);
            Assert.That(presentationIdentity.TryValidate(out error), Is.True, error);
            Assert.That(
                presentationIdentity.StableId,
                Is.EqualTo(group.Building.Identity.CreateBakedStableId()));
            Assert.That(
                presentationIdentity.Role,
                Is.EqualTo(OperationMapEntityPresentationRole.GameplayBuildings));
            Assert.That(realized.Authoring.OriginCell, Is.EqualTo(group.Building.OriginCell));
            Assert.That(realized.Authoring.FootprintCells, Is.EqualTo(group.Building.FootprintCells));
            Assert.That(realized.Authoring.MaxHealth, Is.EqualTo((int)group.Building.MaximumHealth));
            Assert.That(
                realized.Authoring.transform.parent,
                Is.SameAs(hierarchy.ResolveIndependentParent(
                    DenseCityPresentationCategory.GameplayBuildingIntact,
                    GeneratedCityBuildingRole.Shop)));
            Assert.That(realized.IntactVisualRoot.parent, Is.SameAs(realized.Authoring.transform));
            Assert.That(realized.DestroyedVisualRoot.parent, Is.SameAs(realized.Authoring.transform));
            Assert.That(
                PrefabUtility.GetCorrespondingObjectFromSource(realized.IntactVisualRoot.gameObject),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(IntactPrefabPath)));
            Assert.That(
                PrefabUtility.GetCorrespondingObjectFromSource(realized.DestroyedVisualRoot.gameObject),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(DestroyedPrefabPath)));
            AssertMatrix(group.Building.WorldMatrix, realized.Authoring.transform.localToWorldMatrix);
            AssertMatrix(group.IntactPresentation.WorldMatrix, realized.IntactVisualRoot.localToWorldMatrix);
            AssertMatrix(group.DestroyedPresentation.WorldMatrix, realized.DestroyedVisualRoot.localToWorldMatrix);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_LateVisualMismatchRemovesCompleteBuildingOwner()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityBuildingRecordGroup group = CreateGroup();
            Material other = CreateMaterial(OtherMaterialPath);
            Assert.That(
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(other, out string otherGuid, out long _),
                Is.True);
            var mismatchedDestroyed = new DenseCityPresentationBakeRecord(
                group.DestroyedPresentation.Identity,
                group.DestroyedPresentation.Category,
                group.DestroyedPresentation.PrefabAssetGuid,
                null,
                new[] { otherGuid },
                group.DestroyedPresentation.WorldMatrix,
                true,
                true,
                3);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform parent = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Shop);

            Assert.That(
                () => DenseCityBuildingPresentationRealizer.Realize(
                    OperationMapId,
                    group.Building,
                    group.IntactPresentation,
                    mismatchedDestroyed,
                    hierarchy,
                    DenseCityBuildingDefinitionLibrary.LoadExisting()),
                Throws.TypeOf<System.InvalidOperationException>().With.Message.Contains("material identity"));
            Assert.That(parent.childCount, Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_RemovesEmbeddedDestroyedAlternativeFromIntactVisual()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityBuildingRecordGroup group = CreateGroup(embeddedDestroyedAlternative: true);
            DenseCityRealizedBuildingPresentation realized =
                DenseCityBuildingPresentationRealizer.Realize(
                    OperationMapId,
                    group.Building,
                    group.IntactPresentation,
                    group.DestroyedPresentation,
                    CreateHierarchy(mapScene, entityScene),
                    DenseCityBuildingDefinitionLibrary.LoadExisting(),
                    null,
                    433);

            Assert.That(realized.IntactVisualRoot.Find("Model"), Is.Not.Null);
            Assert.That(realized.IntactVisualRoot.Find("Destroyed"), Is.Null);
            Assert.That(
                realized.IntactVisualRoot.GetComponentsInChildren<Renderer>(true).Length,
                Is.EqualTo(1));
            Assert.That(
                realized.DestroyedVisualRoot.GetComponentsInChildren<Renderer>(true).Length,
                Is.EqualTo(1));
            Assert.That(group.IntactPresentation.MaterialAssetGuids.Length, Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static DenseCityBuildingRecordGroup CreateGroup(
        bool embeddedDestroyedAlternative = false)
    {
        GameObject intactPrefab = embeddedDestroyedAlternative
            ? CreateIntactPrefabWithEmbeddedDestroyedAlternative()
            : CreatePrefab(IntactPrefabPath, IntactMaterialPath);
        GameObject destroyedPrefab = CreatePrefab(DestroyedPrefabPath, DestroyedMaterialPath);
        DenseCityVisualAssetMetadata intact = DenseCityVisualAssetMetadataExtractor.Extract(
            intactPrefab,
            null,
            renderer => DenseCityBuildingIntactVisualPolicy.ShouldIncludeRenderer(
                intactPrefab,
                renderer));
        DenseCityVisualAssetMetadata destroyed = DenseCityVisualAssetMetadataExtractor.Extract(destroyedPrefab);
        DenseCityBuildingDefinitionLibrary definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();
        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(15f, 2f, -9f),
            Quaternion.Euler(0f, 90f, 0f),
            new Vector3(1.25f, 1f, 1.25f));
        return DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                25,
                intact.PrefabAssetGuid,
                intact.PrefabLocalId,
                destroyed.PrefabAssetGuid,
                destroyed.PrefabLocalId,
                intact.MaterialAssetGuids.ToArray(),
                destroyed.MaterialAssetGuids.ToArray(),
                matrix,
                new Vector2Int(20, 30),
                new Vector2Int(8, 6),
                new Vector2(8f, 6f),
                2f,
                new Bounds(new Vector3(15f, 4f, -9f), new Vector3(6f, 4f, 8f)),
                Vector3.right,
                GeneratedCityBuildingRole.Shop,
                definitions.ResolveAssetGuid(GeneratedCityBuildingRole.Shop),
                2,
                725f,
                1,
                0,
                new Vector2Int(1, 2)));
    }

    private static GameObject CreateIntactPrefabWithEmbeddedDestroyedAlternative()
    {
        Material intactMaterial = CreateMaterial(IntactMaterialPath);
        Material embeddedDestroyedMaterial = CreateMaterial(EmbeddedDestroyedMaterialPath);
        var source = new GameObject("VisualSource");
        var model = new GameObject("Model");
        model.transform.SetParent(source.transform, false);
        model.AddComponent<MeshFilter>().sharedMesh =
            Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        model.AddComponent<MeshRenderer>().sharedMaterial = intactMaterial;
        var destroyed = new GameObject("Destroyed");
        destroyed.transform.SetParent(source.transform, false);
        destroyed.AddComponent<MeshFilter>().sharedMesh =
            Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        destroyed.AddComponent<MeshRenderer>().sharedMaterial = embeddedDestroyedMaterial;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, IntactPrefabPath);
        UnityEngine.Object.DestroyImmediate(source);
        return prefab;
    }

    private static GameObject CreatePrefab(string prefabPath, string materialPath)
    {
        Material material = CreateMaterial(materialPath);
        var source = new GameObject("VisualSource");
        source.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        source.AddComponent<MeshRenderer>().sharedMaterial = material;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        UnityEngine.Object.DestroyImmediate(source);
        return prefab;
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
