using System;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Editor;
using Game.Configs;
using Game.Runtime;
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
            suite.SemanticIdentity_RejectsInvalidRoleCategoryAndOverlapCombinations,
            suite.AuthoringOwnership_RejectsDetailedRendererBeneathProxyRoot,
            suite.AuthoringOwnership_RejectsUnclassifiedGeneratedRenderer,
            suite.AuthoringOwnership_AcceptsExplicitProxyOwner,
            suite.AuthoringOwnership_RejectsInheritedProxyOwner,
            suite.ProtectedRoots_AcceptsStableIdentityAndHierarchy,
            suite.ProtectedRoots_RejectsRenamedRoot,
            suite.ProtectedRoots_RejectsDisabledRoot,
            suite.ProtectedRoots_RejectsMovedRoot,
            suite.ProtectedRoots_RejectsReparentedRoot,
            suite.ProtectedRoots_RejectsDeletedRoot,
            suite.ProtectedBoundsIndex_DetectsOnlyOverlappingFootprints,
            suite.AuthoringOwnership_AcceptsCompleteGeneratedBuildingEcsPresentation,
            suite.AuthoringOwnership_RejectsGeneratedBuildingWithoutDestroyedRoot,
            suite.AuthoringOwnership_RejectsGeneratedBuildingManagedRuntimeLink,
            suite.AuthoringOwnership_RejectsGeneratedBuildingSceneEmbeddedMesh,
            suite.AuthoringOwnership_AcceptsOwnedIntactBuildingAttachment,
            suite.AuthoringOwnership_RejectsAttachmentUnderWrongVisualState,
            suite.AuthoringOwnership_RejectsAttachmentWithIndependentPresentationOwner,
            suite.AuthoringOwnership_RejectsNestedAttachmentOwnership,
            suite.GeneratedDebugNames_RejectsUniqueNameBudgetOverflow
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
            classified.AddComponent<DenseCityPresentationIdentityAuthoring>()
                .ConfigureForEditor(
                    "densecity.0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                    OperationMapEntityPresentationRole.RenderOnly,
                    DenseCityPresentationSemanticCategory.Prop);

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
    public void SemanticIdentity_RejectsInvalidRoleCategoryAndOverlapCombinations()
    {
        var owner = new GameObject("SemanticIdentity");
        try
        {
            DenseCityPresentationIdentityAuthoring identity =
                owner.AddComponent<DenseCityPresentationIdentityAuthoring>();
            const string StableId =
                "densecity.0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

            identity.ConfigureForEditor(
                StableId,
                OperationMapEntityPresentationRole.GameplayBuildings,
                DenseCityPresentationSemanticCategory.Prop);
            Assert.That(identity.TryValidate(out _), Is.False);

            identity.ConfigureForEditor(
                StableId,
                OperationMapEntityPresentationRole.RenderOnly,
                DenseCityPresentationSemanticCategory.Prop,
                true);
            Assert.That(identity.TryValidate(out _), Is.False);

            identity.ConfigureForEditor(
                StableId,
                OperationMapEntityPresentationRole.RenderOnly,
                DenseCityPresentationSemanticCategory.Infrastructure,
                true);
            Assert.That(identity.TryValidate(out string error), Is.True, error);
            Assert.That(identity.AllowsProtectedOverlap, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(owner);
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

    [Test]
    public void AuthoringOwnership_AcceptsExplicitProxyOwner()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        Mesh mesh = null;
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
            var proxy = new GameObject("OwnedTerrainProxy");
            proxy.transform.SetParent(terrain, false);
            MapBakeGroupAuthoring owner = proxy.AddComponent<MapBakeGroupAuthoring>();
            var serialized = new SerializedObject(owner);
            serialized.FindProperty("role").enumValueIndex = (int)MapBakeGroupRole.Terrain;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            mesh = new Mesh { name = "OwnedTerrainProxyMesh" };
            proxy.AddComponent<MeshFilter>().sharedMesh = mesh;

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
            if (mesh != null)
                UnityEngine.Object.DestroyImmediate(mesh);
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void AuthoringOwnership_RejectsInheritedProxyOwner()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        Mesh mesh = null;
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
            var proxy = new GameObject("InheritedTerrainProxy");
            proxy.transform.SetParent(terrain, false);
            mesh = new Mesh { name = "InheritedTerrainProxyMesh" };
            proxy.AddComponent<MeshFilter>().sharedMesh = mesh;

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains("exactly one nearest bake-group owner", error);
            StringAssert.Contains("InheritedTerrainProxy", error);
        }
        finally
        {
            if (mesh != null)
                UnityEngine.Object.DestroyImmediate(mesh);
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void ProtectedRoots_AcceptsStableIdentityAndHierarchy()
    {
        (Scene scene, GameObject owner, DenseCityBakeReadinessValidator.ProtectedRootContract contract) =
            CreateProtectedRootScene();
        try
        {
            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateProtectedRootContracts(
                    scene,
                    new[] { contract },
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            CloseProtectedRootScene(scene);
        }
    }

    [Test]
    public void ProtectedRoots_RejectsRenamedRoot()
    {
        AssertProtectedRootMutationRejected(owner => owner.name = "RenamedProtectedRoot", "renamed");
    }

    [Test]
    public void ProtectedRoots_RejectsDisabledRoot()
    {
        AssertProtectedRootMutationRejected(owner => owner.SetActive(false), "active state changed");
    }

    [Test]
    public void ProtectedRoots_RejectsMovedRoot()
    {
        AssertProtectedRootMutationRejected(
            owner => owner.transform.localPosition = Vector3.right,
            "transform moved");
    }

    [Test]
    public void ProtectedRoots_RejectsReparentedRoot()
    {
        AssertProtectedRootMutationRejected(owner =>
        {
            var parent = new GameObject("UnexpectedParent");
            SceneManager.MoveGameObjectToScene(parent, owner.scene);
            owner.transform.SetParent(parent.transform, false);
        }, "reparented");
    }

    [Test]
    public void ProtectedRoots_RejectsDeletedRoot()
    {
        AssertProtectedRootMutationRejected(UnityEngine.Object.DestroyImmediate, "missing");
    }

    [Test]
    public void ProtectedBoundsIndex_DetectsOnlyOverlappingFootprints()
    {
        var index = new DenseCityBakeReadinessValidator.ProtectedBoundsIndex(8f);
        index.Add(new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f)), 2f, "Map/Protected");

        Assert.That(
            index.TryFindOverlap(
                new Bounds(new Vector3(6f, 100f, 0f), Vector3.one),
                out string sourcePath),
            Is.True);
        Assert.That(sourcePath, Is.EqualTo("Map/Protected"));
        Assert.That(
            index.TryFindOverlap(
                new Bounds(new Vector3(20f, 0f, 0f), Vector3.one),
                out _),
            Is.False);
    }

    [Test]
    public void AuthoringOwnership_AcceptsCompleteGeneratedBuildingEcsPresentation()
    {
        AssertGeneratedBuildingMutationAccepted(_ => { });
    }

    [Test]
    public void AuthoringOwnership_RejectsGeneratedBuildingWithoutDestroyedRoot()
    {
        AssertGeneratedBuildingRejected(
            building => UnityEngine.Object.DestroyImmediate(building.DestroyedVisualRoot),
            "one intact and one destroyed visual root");
    }

    [Test]
    public void AuthoringOwnership_RejectsGeneratedBuildingManagedRuntimeLink()
    {
        AssertGeneratedBuildingRejected(
            building => building.gameObject.AddComponent<RuntimeBuildingEntityLink>(),
            "managed RuntimeBuildingEntityLink");
    }

    [Test]
    public void AuthoringOwnership_RejectsGeneratedBuildingSceneEmbeddedMesh()
    {
        Mesh embeddedMesh = null;
        try
        {
            AssertGeneratedBuildingRejected(building =>
            {
                embeddedMesh = new Mesh { name = "SceneEmbeddedGeneratedBuildingMesh" };
                building.IntactVisualRoot.GetComponent<MeshFilter>().sharedMesh = embeddedMesh;
            }, "persistent shared mesh asset");
        }
        finally
        {
            if (embeddedMesh != null)
                UnityEngine.Object.DestroyImmediate(embeddedMesh);
        }
    }

    [Test]
    public void AuthoringOwnership_AcceptsOwnedIntactBuildingAttachment()
    {
        AssertGeneratedBuildingMutationAccepted(building =>
            CreateAttachment(building, building.IntactVisualRoot.transform));
    }

    [Test]
    public void AuthoringOwnership_RejectsAttachmentUnderWrongVisualState()
    {
        AssertGeneratedBuildingRejected(building =>
        {
            OperationMapBuildingAttachmentAuthoring attachment =
                CreateAttachment(building, building.DestroyedVisualRoot.transform);
            attachment.ConfigureForEditor(building, OperationMapBuildingVisualState.Intact);
        }, "declared visual-state root");
    }

    [Test]
    public void AuthoringOwnership_RejectsAttachmentWithIndependentPresentationOwner()
    {
        AssertGeneratedBuildingRejected(building =>
        {
            OperationMapBuildingAttachmentAuthoring attachment =
                CreateAttachment(building, building.IntactVisualRoot.transform);
            attachment.gameObject.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
        }, "independent or mismatched presentation ownership");
    }

    [Test]
    public void AuthoringOwnership_RejectsNestedAttachmentOwnership()
    {
        AssertGeneratedBuildingRejected(building =>
        {
            OperationMapBuildingAttachmentAuthoring parent =
                CreateAttachment(building, building.IntactVisualRoot.transform);
            var nested = new GameObject("NestedAttachment");
            nested.transform.SetParent(parent.transform, false);
            AddPersistentRenderer(nested);
            OperationMapBuildingAttachmentAuthoring marker =
                nested.AddComponent<OperationMapBuildingAttachmentAuthoring>();
            marker.ConfigureForEditor(building, OperationMapBuildingVisualState.Intact);
        }, "duplicate attachment ownership");
    }

    [Test]
    public void GeneratedDebugNames_RejectsUniqueNameBudgetOverflow()
    {
        var root = new GameObject("GeneratedRoot");
        try
        {
            new GameObject("SharedPrefabName").transform.SetParent(root.transform, false);
            new GameObject("SequenceSpecificName_000001").transform.SetParent(root.transform, false);

            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateGeneratedDebugNameBudget(
                    root.transform,
                    2,
                    out string error),
                Is.False);
            StringAssert.Contains("unique entity debug-name budget", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AssertGeneratedBuildingMutationAccepted(Action<OperationMapBuildingAuthoring> mutate)
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            OperationMapBuildingAuthoring building = CreateGeneratedBuilding(mapScene, entityScene);
            mutate(building);
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

    private static void AssertGeneratedBuildingRejected(
        Action<OperationMapBuildingAuthoring> mutate,
        string expectedError)
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            OperationMapBuildingAuthoring building = CreateGeneratedBuilding(mapScene, entityScene);
            mutate(building);
            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                    mapScene,
                    entityScene,
                    OperationMapId,
                    GenerationId,
                    out string error),
                Is.False);
            StringAssert.Contains(expectedError, error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static OperationMapBuildingAuthoring CreateGeneratedBuilding(
        Scene mapScene,
        Scene entityScene)
    {
        var roots = DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            GenerationId,
            "dense-city-v1",
            1,
            42,
            Hash);
        Transform parent = roots.EntityPresentationSource.transform.Find("GameplayBuildings/Buildings");
        var owner = new GameObject(DenseCityBuildingPresentationRealizer.SharedBuildingDebugName);
        owner.transform.SetParent(parent, false);
        var intact = new GameObject("IntactVisual");
        intact.transform.SetParent(owner.transform, false);
        var destroyed = new GameObject("DestroyedVisual");
        destroyed.transform.SetParent(owner.transform, false);
        AddPersistentRenderer(intact);
        AddPersistentRenderer(destroyed);
        BuildingDefinitionAuthoring definition = owner.AddComponent<BuildingDefinitionAuthoring>();
        OperationMapBuildingAuthoring building = owner.AddComponent<OperationMapBuildingAuthoring>();
        building.ConfigureGeneratedForEditor(
            OperationMapId,
            "densecity.0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            1,
            2,
            new Vector2Int(10, 20),
            new Vector2Int(4, 5),
            500,
            definition,
            intact,
            destroyed);
        return building;
    }

    private static OperationMapBuildingAttachmentAuthoring CreateAttachment(
        OperationMapBuildingAuthoring building,
        Transform visualStateRoot)
    {
        var owner = new GameObject("GeneratedAttachment");
        owner.transform.SetParent(visualStateRoot, false);
        AddPersistentRenderer(owner);
        OperationMapBuildingAttachmentAuthoring attachment =
            owner.AddComponent<OperationMapBuildingAttachmentAuthoring>();
        attachment.ConfigureForEditor(
            building,
            visualStateRoot == building.IntactVisualRoot.transform
                ? OperationMapBuildingVisualState.Intact
                : OperationMapBuildingVisualState.Destroyed);
        return attachment;
    }

    private static void AddPersistentRenderer(GameObject owner)
    {
        owner.AddComponent<MeshFilter>().sharedMesh = LoadProjectAsset<Mesh>();
        owner.AddComponent<MeshRenderer>().sharedMaterial = LoadProjectAsset<Material>();
    }

    private static T LoadProjectAsset<T>() where T : UnityEngine.Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/PolygonMilitary" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAllAssetsAtPath(path).OfType<T>().FirstOrDefault();
            if (asset != null)
                return asset;
        }
        Assert.Fail($"No persistent {typeof(T).Name} test asset was found.");
        return null;
    }

    private static void AssertProtectedRootMutationRejected(
        Action<GameObject> mutate,
        string expectedError)
    {
        (Scene scene, GameObject owner, DenseCityBakeReadinessValidator.ProtectedRootContract contract) =
            CreateProtectedRootScene();
        try
        {
            mutate(owner);
            Assert.That(
                DenseCityBakeReadinessValidator.TryValidateProtectedRootContracts(
                    scene,
                    new[] { contract },
                    out string error),
                Is.False);
            StringAssert.Contains(expectedError, error.ToLowerInvariant());
        }
        finally
        {
            CloseProtectedRootScene(scene);
        }
    }

    private static void CloseProtectedRootScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;
        Scene replacement = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive);
        SceneManager.SetActiveScene(replacement);
        EditorSceneManager.CloseScene(scene, true);
    }

    private static (
        Scene Scene,
        GameObject Owner,
        DenseCityBakeReadinessValidator.ProtectedRootContract Contract)
        CreateProtectedRootScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var owner = new GameObject("ProtectedRoot");
        Assert.That(EditorSceneManager.SaveScene(scene, MapScenePath), Is.True);
        string globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString();
        var contract = new DenseCityBakeReadinessValidator.ProtectedRootContract(
            globalObjectId,
            owner.name,
            "ProtectedRoot[0]",
            true,
            1f);
        return (scene, owner, contract);
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
