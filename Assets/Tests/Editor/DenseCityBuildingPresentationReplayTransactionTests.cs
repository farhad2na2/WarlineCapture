using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityBuildingPresentationReplayTransactionTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityBuildingReplayTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityBuildingPresentationReplayTransactionTests();
        System.Action[] tests =
        {
            suite.Realize_ReplaysBuildingAndAttachmentsUnderDeclaredVisualStates,
            suite.Realize_LateAttachmentMismatchRollsBackCompleteReplay
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

        Debug.Log($"[DenseCityBuildingReplayValidation] result=Passed tests={tests.Length}");
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
    public void Realize_ReplaysBuildingAndAttachmentsUnderDeclaredVisualStates()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            using DenseCityGenerationRecordSet records = CreateRecords(false);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);

            var realized = DenseCityBuildingPresentationReplayTransaction.Realize(
                "opmap.skirmish.building_replay_test",
                records,
                hierarchy,
                DenseCityBuildingDefinitionLibrary.LoadExisting());

            Assert.That(realized, Has.Count.EqualTo(1));
            Assert.That(
                realized[0].Authoring.name,
                Is.EqualTo(DenseCityBuildingPresentationRealizer.SharedBuildingDebugName));
            Assert.That(realized[0].IntactVisualRoot.childCount, Is.EqualTo(1));
            Assert.That(realized[0].DestroyedVisualRoot.childCount, Is.EqualTo(1));
            Assert.That(
                realized[0].IntactVisualRoot.GetChild(0).name,
                Is.EqualTo("intact-attachment"));
            Assert.That(
                realized[0].DestroyedVisualRoot.GetChild(0).name,
                Is.EqualTo("destroyed-attachment"));
            OperationMapBuildingAttachmentAuthoring intactAttachment =
                realized[0].IntactVisualRoot.GetChild(0)
                    .GetComponent<OperationMapBuildingAttachmentAuthoring>();
            OperationMapBuildingAttachmentAuthoring destroyedAttachment =
                realized[0].DestroyedVisualRoot.GetChild(0)
                    .GetComponent<OperationMapBuildingAttachmentAuthoring>();
            Assert.That(intactAttachment, Is.Not.Null);
            Assert.That(intactAttachment.BuildingOwner, Is.SameAs(realized[0].Authoring));
            Assert.That(intactAttachment.VisualState, Is.EqualTo(OperationMapBuildingVisualState.Intact));
            Assert.That(destroyedAttachment, Is.Not.Null);
            Assert.That(destroyedAttachment.BuildingOwner, Is.SameAs(realized[0].Authoring));
            Assert.That(destroyedAttachment.VisualState, Is.EqualTo(OperationMapBuildingVisualState.Destroyed));
            Assert.That(
                realized[0].Authoring.GetComponentsInChildren<Collider>(true),
                Is.Empty);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Realize_LateAttachmentMismatchRollsBackCompleteReplay()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            using DenseCityGenerationRecordSet records = CreateRecords(true);
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            Transform parent = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Shop);

            Assert.That(
                () => DenseCityBuildingPresentationReplayTransaction.Realize(
                    "opmap.skirmish.building_replay_test",
                    records,
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

    private static DenseCityGenerationRecordSet CreateRecords(bool mismatchDestroyedAttachment)
    {
        DenseCityVisualAssetMetadata intact = CreatePrefabMetadata("Intact", "intact-building");
        DenseCityVisualAssetMetadata destroyed = CreatePrefabMetadata("Destroyed", "destroyed-building");
        DenseCityBuildingDefinitionLibrary definitions = DenseCityBuildingDefinitionLibrary.LoadExisting();
        Matrix4x4 buildingMatrix = Matrix4x4.TRS(
            new Vector3(12f, 2f, -8f),
            Quaternion.Euler(0f, 90f, 0f),
            Vector3.one);
        DenseCityBuildingRecordGroup group = DenseCityBuildingRecordFactory.Create(
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
                buildingMatrix,
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

        DenseCityPresentationBakeRecord intactAttachment = CreateAttachment(
            "IntactAttachmentSource",
            "intact-attachment",
            0,
            group.Building.Identity.StableKey,
            DenseCityPresentationCategory.BuildingAttachmentIntact,
            buildingMatrix * Matrix4x4.Translate(new Vector3(0.25f, 1f, 0f)),
            null);
        string[] mismatchGuids = null;
        if (mismatchDestroyedAttachment)
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
            mismatchGuids = new[] { mismatchGuidA, mismatchGuidB };
        }
        DenseCityPresentationBakeRecord destroyedAttachment = CreateAttachment(
            "DestroyedAttachmentSource",
            "destroyed-attachment",
            1,
            group.Building.Identity.StableKey,
            DenseCityPresentationCategory.BuildingAttachmentDestroyed,
            buildingMatrix * Matrix4x4.Translate(new Vector3(-0.25f, 0.5f, 0f)),
            mismatchGuids);

        var records = new DenseCityGenerationRecordSet(1, 2, 4);
        DenseCityBuildingRecordFactory.Add(records, group);
        records.AddBuildingAttachment(intactAttachment);
        records.AddBuildingAttachment(destroyedAttachment);
        records.Seal();
        return records;
    }

    private static DenseCityPresentationBakeRecord CreateAttachment(
        string sourceName,
        string kind,
        int sequence,
        string ownerStableKey,
        DenseCityPresentationCategory category,
        Matrix4x4 matrix,
        string[] overrideMaterialGuids)
    {
        DenseCityVisualAssetMetadata metadata = CreatePrefabMetadata(sourceName, kind);
        return new DenseCityPresentationBakeRecord(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                3,
                kind,
                100 + sequence,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId),
            category,
            metadata.PrefabAssetGuid,
            null,
            overrideMaterialGuids == null
                ? metadata.MaterialAssetGuids.ToArray()
                : overrideMaterialGuids,
            matrix,
            true,
            true,
            2,
            ownerStableKey);
    }

    private static DenseCityVisualAssetMetadata CreatePrefabMetadata(string sourceName, string key)
    {
        string materialPath = TempRoot + "/" + key + ".mat";
        string prefabPath = TempRoot + "/" + key + ".prefab";
        Material material = CreateMaterial(materialPath);
        var source = new GameObject(sourceName);
        source.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        source.AddComponent<MeshRenderer>().sharedMaterial = material;
        source.AddComponent<BoxCollider>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        Object.DestroyImmediate(source);
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
