using System;
using Game.Authoring;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityBakeReadinessValidatorTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityBakeReadinessValidatorTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string GenerationId = "dense-city:test:42";
    private const string OperationMapId = "opmap.skirmish.building_test_01";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityBakeReadinessValidatorTests();
        Action[] tests =
        {
            suite.AuthoringOwnership_AcceptsExplicitScenePairAndUniqueOverride,
            suite.AuthoringOwnership_RejectsDuplicateOverrideId,
            suite.AuthoringOwnership_RejectsDuplicateBuildingStableIdentity,
            suite.AuthoringOwnership_RejectsBuildingInOperationMapScene,
            suite.AuthoringOwnership_RejectsPhysicsInInactiveGeneratedDescendant,
            suite.GenerationState_AcceptsExplicitNotGeneratedScenePair,
            suite.GenerationState_RejectsPartialGeneratedScenePair,
            suite.AuthoringOwnership_RejectsDuplicateGeneratedRoleRoot,
            suite.AuthoringOwnership_RejectsEveryGenerationContractMismatch,
            suite.AuthoringOwnership_AcceptsClassifiedRenderOnlyRenderer,
            suite.AuthoringOwnership_RejectsDetailedRendererBeneathProxyRoot,
            suite.AuthoringOwnership_RejectsUnclassifiedGeneratedRenderer
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

        Debug.Log($"[DenseCityBakeReadinessValidation] result=Passed tests={tests.Length}");
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
    public void AuthoringOwnership_AcceptsExplicitScenePairAndUniqueOverride()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            CreateHierarchy(mapScene, entityScene);
            CreateOverride(mapScene, "military-base-protected-area");

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsDuplicateOverrideId()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            CreateHierarchy(mapScene, entityScene);
            CreateOverride(mapScene, "military-base-protected-area");
            CreateOverride(mapScene, "military-base-protected-area");

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("Duplicate dense-city authored override id", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsDuplicateBuildingStableIdentity()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            Transform buildings = roots.EntityPresentationSource.transform
                .Find("GameplayBuildings/Buildings");
            const string sourceId =
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0";
            CreateBuilding(buildings, sourceId, 4);
            CreateBuilding(buildings, sourceId, 5);

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("Duplicate operation-map building stable id", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsBuildingInOperationMapScene()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            CreateHierarchy(mapScene, entityScene);
            var mapOwner = new GameObject("MisplacedBuildingContainer");
            SceneManager.MoveGameObjectToScene(mapOwner, mapScene);
            CreateBuilding(
                mapOwner.transform,
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0",
                4);

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("entity-presentation scene", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsPhysicsInInactiveGeneratedDescendant()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            var invalid = new GameObject("InactiveInvalidPhysics");
            invalid.transform.SetParent(roots.EntityPresentationSource.transform, false);
            invalid.AddComponent<BoxCollider2D>();
            invalid.SetActive(false);

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("BoxCollider2D", error);
            StringAssert.Contains("InactiveInvalidPhysics", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void GenerationState_AcceptsExplicitNotGeneratedScenePair()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            Assert.That(
                DenseCityBakeReadinessValidator.TryResolveGenerationState(
                    mapScene,
                    entityScene,
                    out bool generated,
                    out string generationId,
                    out string error),
                Is.True,
                error);
            Assert.That(generated, Is.False);
            Assert.That(generationId, Is.Null);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void GenerationState_RejectsPartialGeneratedScenePair()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            Scene temporaryEntityScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                temporaryEntityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            EditorSceneManager.CloseScene(temporaryEntityScene, true);

            Assert.That(
                DenseCityBakeReadinessValidator.TryResolveGenerationState(
                    mapScene,
                    entityScene,
                    out _,
                    out _,
                    out string error),
                Is.False);
            StringAssert.Contains("partial or duplicated", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsDuplicateGeneratedRoleRoot()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            GameObject duplicate = UnityEngine.Object.Instantiate(roots.MapBakeSource.gameObject);
            duplicate.name = "DuplicateMapBakeSource";
            SceneManager.MoveGameObjectToScene(duplicate, mapScene);

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("exactly one marked dense-city root", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsEveryGenerationContractMismatch()
    {
        (string propertyName, Action<SerializedProperty> mutate)[] mismatches =
        {
            ("generationId", property => property.stringValue = "dense-city:test:43"),
            ("generatorSchema", property => property.stringValue = "dense-city-v2"),
            ("generatorSchemaVersion", property => property.intValue = 2),
            ("deterministicSeed", property => property.intValue = 43),
            ("deterministicGenerationHash", property => property.stringValue =
                "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")
        };

        foreach ((string propertyName, Action<SerializedProperty> mutate) in mismatches)
        {
            (Scene mapScene, Scene entityScene) = CreateScenePair();
            try
            {
                var roots = DenseCitySemanticHierarchyBuilder.Create(
                    mapScene,
                    entityScene,
                    GenerationId,
                    "dense-city-v1",
                    1,
                    42,
                    Hash);
                var serialized = new SerializedObject(roots.EntityPresentationSource);
                mutate(serialized.FindProperty(propertyName));
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                        mapScene,
                        entityScene,
                        OperationMapId,
                        GenerationId,
                        out string error),
                    Is.False,
                    propertyName);
                StringAssert.Contains("same deterministic generation set", error, propertyName);
            }
            finally
            {
                EditorSceneManager.CloseScene(entityScene, true);
                EditorSceneManager.CloseScene(mapScene, true);
            }
        }
    }

    [Test]
    public void AuthoringOwnership_AcceptsClassifiedRenderOnlyRenderer()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            Transform props = roots.EntityPresentationSource.transform.Find("RenderOnly/Props");
            var classified = new GameObject("ClassifiedPropRenderer");
            classified.transform.SetParent(props, false);
            classified.AddComponent<MeshRenderer>();

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsDetailedRendererBeneathProxyRoot()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            Transform terrain = roots.MapBakeSource.transform.Find("BakeSources/Terrain");
            var detailed = new GameObject("ForbiddenDetailedRenderer");
            detailed.transform.SetParent(terrain, false);
            detailed.AddComponent<MeshRenderer>();

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("proxy hierarchy contains detailed renderer", error);
            StringAssert.Contains("ForbiddenDetailedRenderer", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsUnclassifiedGeneratedRenderer()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                GenerationId,
                "dense-city-v1",
                1,
                42,
                Hash);
            Transform renderOnly = roots.EntityPresentationSource.transform.Find("RenderOnly");
            var unclassified = new GameObject("UnclassifiedRenderer");
            unclassified.transform.SetParent(renderOnly, false);
            unclassified.AddComponent<MeshRenderer>();

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("generated renderer is unclassified", error);
            StringAssert.Contains("UnclassifiedRenderer", error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static void CreateHierarchy(Scene mapScene, Scene entityScene)
    {
        DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            GenerationId,
            "dense-city-v1",
            1,
            42,
            Hash);
    }

    private static void CreateOverride(Scene scene, string stableId)
    {
        var owner = new GameObject("AuthoredOverride");
        SceneManager.MoveGameObjectToScene(owner, scene);
        DenseCityAuthoredOverrideAuthoring authoredOverride =
            owner.AddComponent<DenseCityAuthoredOverrideAuthoring>();
        var serialized = new SerializedObject(authoredOverride);
        serialized.FindProperty("stableId").stringValue = stableId;
        serialized.FindProperty("localSize").vector3Value = new Vector3(10f, 5f, 12f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBuilding(Transform parent, string sourceGlobalObjectId, int placementIndex)
    {
        var owner = new GameObject($"Building_{placementIndex}");
        owner.transform.SetParent(parent, false);
        BuildingDefinitionAuthoring definition = owner.AddComponent<BuildingDefinitionAuthoring>();
        OperationMapBuildingAuthoring building = owner.AddComponent<OperationMapBuildingAuthoring>();
        var intactVisual = new GameObject("IntactVisual");
        intactVisual.transform.SetParent(owner.transform, false);
        var serialized = new SerializedObject(building);
        serialized.FindProperty("operationMapId").stringValue = OperationMapId;
        serialized.FindProperty("sourceGlobalObjectId").stringValue = sourceGlobalObjectId;
        serialized.FindProperty("placementIndex").intValue = placementIndex;
        serialized.FindProperty("definition").objectReferenceValue = definition;
        serialized.FindProperty("intactVisualRoot").objectReferenceValue = intactVisual;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
        if (AssetDatabase.IsValidFolder(path))
            return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
