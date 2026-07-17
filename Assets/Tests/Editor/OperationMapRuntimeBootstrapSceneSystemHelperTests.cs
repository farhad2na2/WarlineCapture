using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class OperationMapRuntimeBootstrapSceneSystemHelperTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Test]
    public void CatalogPublish_ResolvesOnceAndPublishesSelectedDefinition()
    {
        using World world = new("OperationMapRuntimeBootstrapCatalog");
        OperationMapDefinition definition = CreateValidDefinition();
        OperationMapCatalogConfig catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        SetCatalogDefinitions(catalog, new[] { definition });
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
            FixedString64Bytes missionId = new("skirmish");
            Assert.That(bootstrap.TryPublish(
                catalog,
                definition.OperationMapId,
                in scenarioId,
                in missionId,
                1,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out Entity root,
                out string error), Is.True, error);

            Assert.That(world.EntityManager.GetComponentData<ActiveOperationMapComponent>(root)
                .OperationMapId.ToString(), Is.EqualTo(definition.OperationMapId));
            Assert.That(bootstrap.TryPublish(
                catalog,
                "opmap.skirmish.missing",
                in scenarioId,
                in missionId,
                2,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out _,
                out string missingError), Is.False);
            StringAssert.Contains("not present", missingError);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void Publish_CreatesOneCompleteGenerationWithoutSurfaceOwnership()
    {
        using World world = new("OperationMapRuntimeBootstrapPublish");
        OperationMapDefinition definition = CreateValidDefinition();
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
            FixedString64Bytes missionId = new("skirmish");
            OperationMapReadinessFlags required = OperationMapReadinessFlags.SourceContent |
                                                  OperationMapReadinessFlags.Metadata |
                                                  OperationMapReadinessFlags.MapSurface;

            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                1,
                required,
                required,
                out Entity root,
                out string error), Is.True, error);

            EntityManager entityManager = world.EntityManager;
            Assert.That(entityManager.Exists(root), Is.True);
            Assert.That(CountRoots(entityManager), Is.EqualTo(1));
            Assert.That(entityManager.HasBuffer<OperationMapLoadRequestElement>(root), Is.True);
            Assert.That(entityManager.HasBuffer<OperationMapLoadResultElement>(root), Is.True);
            Assert.That(entityManager.HasComponent<MapSurfaceComponent>(root), Is.False);

            ActiveOperationMapComponent active = entityManager.GetComponentData<ActiveOperationMapComponent>(root);
            Assert.That(active.OperationMapId.ToString(), Is.EqualTo("opmap.skirmish.desert_base_01"));
            Assert.That(active.ScenarioId, Is.EqualTo(scenarioId));
            Assert.That(active.MissionId, Is.EqualTo(missionId));
            Assert.That(active.Generation, Is.EqualTo(1));

            OperationMapMetadataComponent metadata = entityManager.GetComponentData<OperationMapMetadataComponent>(root);
            Assert.That(metadata.Blob.IsCreated, Is.True);
            Assert.That(metadata.MetadataHash.ToString(), Is.EqualTo(Hash));
            Assert.That(metadata.Generation, Is.EqualTo(1));

            OperationMapLoadStateComponent state = entityManager.GetComponentData<OperationMapLoadStateComponent>(root);
            Assert.That(state.Status, Is.EqualTo(OperationMapLoadStatusKind.Ready));
            Assert.That(state.Progress01, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void Publish_RejectsInvalidOrNonMonotonicGenerationWithoutChangingCurrentState()
    {
        using World world = new("OperationMapRuntimeBootstrapGeneration");
        OperationMapDefinition definition = CreateValidDefinition();
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
            FixedString64Bytes missionId = new("skirmish");
            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                2,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out Entity firstRoot,
                out string firstError), Is.True, firstError);

            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                2,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out Entity rejectedRoot,
                out string duplicateError), Is.False);
            Assert.That(rejectedRoot, Is.EqualTo(Entity.Null));
            StringAssert.Contains("increase monotonically", duplicateError);
            Assert.That(CountRoots(world.EntityManager), Is.EqualTo(1));
            Assert.That(world.EntityManager.GetComponentData<ActiveOperationMapComponent>(firstRoot).Generation, Is.EqualTo(2));

            Set(definition, "contentHash", "invalid");
            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                3,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out _,
                out string validationError), Is.False);
            StringAssert.Contains("content hash", validationError);
            Assert.That(world.EntityManager.GetComponentData<ActiveOperationMapComponent>(firstRoot).Generation, Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void Publish_RejectsMultipleRootsBeforeAllocatingOrMutating()
    {
        using World world = new("OperationMapRuntimeBootstrapMultipleRoots");
        EntityManager entityManager = world.EntityManager;
        entityManager.CreateEntity(typeof(OperationMapRootComponent));
        entityManager.CreateEntity(typeof(OperationMapRootComponent));
        OperationMapDefinition definition = CreateValidDefinition();
        using OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
            FixedString64Bytes missionId = new("skirmish");
            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                1,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out Entity root,
                out string error), Is.False);
            Assert.That(root, Is.EqualTo(Entity.Null));
            StringAssert.Contains("zero or one", error);
            Assert.That(CountRoots(entityManager), Is.EqualTo(2));
            AssertOwnedBlobCreated(bootstrap, false);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void ClearAndDispose_RemovePublishedStateAndReleaseOwnedBlob()
    {
        World world = new("OperationMapRuntimeBootstrapClear");
        OperationMapDefinition definition = CreateValidDefinition();
        OperationMapRuntimeBootstrapSceneSystemHelper bootstrap = new(world);
        try
        {
            FixedString64Bytes scenarioId = new("scenario.skirmish.desert_base_standard");
            FixedString64Bytes missionId = new("skirmish");
            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                1,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out _,
                out string error), Is.True, error);
            AssertOwnedBlobCreated(bootstrap, true);

            bootstrap.ClearPublishedState();
            Assert.That(CountRoots(world.EntityManager), Is.Zero);
            AssertOwnedBlobCreated(bootstrap, false);

            Assert.That(bootstrap.TryPublish(
                definition,
                in scenarioId,
                in missionId,
                2,
                OperationMapReadinessFlags.Metadata,
                OperationMapReadinessFlags.Metadata,
                out _,
                out error), Is.True, error);
            world.Dispose();
            Assert.DoesNotThrow(bootstrap.Dispose);
            AssertOwnedBlobCreated(bootstrap, false);
        }
        finally
        {
            bootstrap.Dispose();
            if (world.IsCreated)
                world.Dispose();
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void Helper_HasNoUpdateMethod()
    {
        Assert.That(typeof(OperationMapRuntimeBootstrapSceneSystemHelper).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void MatchBootstrapCompatibilityPublish_PublishesConfiguredIdentityAndDisposesRoot()
    {
        using World world = new("OperationMapCompatibilityComposition");
        OperationMapDefinition definition = CreateValidDefinition();
        OperationMapCatalogConfig catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        SetCatalogDefinitions(catalog, new[] { definition });
        GameObject owner = new("OperationMapCompatibilityMatchView");
        MatchSceneView view = owner.AddComponent<MatchSceneView>();
        Set(view, "operationMapCatalog", catalog);
        try
        {
            Assert.That(view.TryPublishCompatibilityOperationMapMetadata(
                world,
                out string error), Is.True, error);

            using EntityQuery query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>());
            Entity root = query.GetSingletonEntity();
            ActiveOperationMapComponent active =
                world.EntityManager.GetComponentData<ActiveOperationMapComponent>(root);
            Assert.That(active.OperationMapId.ToString(), Is.EqualTo(definition.OperationMapId));
            Assert.That(active.ScenarioId.ToString(), Is.EqualTo("scenario.skirmish.desert_base_standard"));
            Assert.That(active.MissionId.ToString(), Is.EqualTo("skirmish"));

            view.DisposeOperationMapMetadataBootstrap();
            Assert.That(CountRoots(world.EntityManager), Is.Zero);
        }
        finally
        {
            view.DisposeOperationMapMetadataBootstrap();
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void MatchBootstrapCompatibilityPublish_InvalidIdentityFailsWithoutRoot()
    {
        using World world = new("OperationMapCompatibilityInvalidIdentity");
        OperationMapDefinition definition = CreateValidDefinition();
        OperationMapCatalogConfig catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        SetCatalogDefinitions(catalog, new[] { definition });
        GameObject owner = new("OperationMapCompatibilityInvalidMatchView");
        MatchSceneView view = owner.AddComponent<MatchSceneView>();
        Set(view, "operationMapCatalog", catalog);
        Set(view, "operationMapId", "invalid");
        try
        {
            Assert.That(view.TryPublishCompatibilityOperationMapMetadata(
                world,
                out string error), Is.False);
            StringAssert.Contains("Invalid compatibility operation-map id", error);
            Assert.That(CountRoots(world.EntityManager), Is.Zero);
        }
        finally
        {
            view.DisposeOperationMapMetadataBootstrap();
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void MatchRuntimeBind_MissingCatalogFailsBeforeBootstrap()
    {
        using World world = new("OperationMapCompatibilityMissingCatalog");
        GameObject owner = new("OperationMapCompatibilityMissingCatalogMatchView");
        MatchSceneView view = owner.AddComponent<MatchSceneView>();
        try
        {
            Assert.That(view.TryBindMatchRuntime(world, out string error), Is.False);
            StringAssert.Contains("catalog is required", error);
            Assert.That(CountRoots(world.EntityManager), Is.Zero);
            Assert.That(Get<bool>(view, "matchRuntimeBound"), Is.False);
        }
        finally
        {
            view.DisposeOperationMapMetadataBootstrap();
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void MatchRuntimeBind_MissingDefinitionFailsBeforeBootstrap()
    {
        using World world = new("OperationMapCompatibilityMissingDefinition");
        OperationMapDefinition definition = CreateValidDefinition();
        OperationMapCatalogConfig catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        SetCatalogDefinitions(catalog, new[] { definition });
        GameObject owner = new("OperationMapCompatibilityMissingDefinitionMatchView");
        MatchSceneView view = owner.AddComponent<MatchSceneView>();
        Set(view, "operationMapCatalog", catalog);
        Set(view, "operationMapId", "opmap.skirmish.missing");
        try
        {
            Assert.That(view.TryBindMatchRuntime(world, out string error), Is.False);
            StringAssert.Contains("not present in the catalog", error);
            Assert.That(CountRoots(world.EntityManager), Is.Zero);
            Assert.That(Get<bool>(view, "matchRuntimeBound"), Is.False);
        }
        finally
        {
            view.DisposeOperationMapMetadataBootstrap();
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(definition);
        }
    }

    private static int CountRoots(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<OperationMapRootComponent>());
        return query.CalculateEntityCount();
    }

    private static void AssertOwnedBlobCreated(
        OperationMapRuntimeBootstrapSceneSystemHelper bootstrap,
        bool expected)
    {
        FieldInfo field = typeof(OperationMapRuntimeBootstrapSceneSystemHelper).GetField(
            "ownedMetadataBlob",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        BlobAssetReference<OperationMapBlob> blob = (BlobAssetReference<OperationMapBlob>)field.GetValue(bootstrap);
        Assert.That(blob.IsCreated, Is.EqualTo(expected));
    }

    private static OperationMapDefinition CreateValidDefinition()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "operationMapId", "opmap.skirmish.desert_base_01");
        Set(definition, "schemaVersion", 1);
        Set(definition, "contentVersion", 1);
        Set(definition, "sourceIdentityHash", Hash);
        Set(definition, "contentHash", Hash);
        Set(definition, "generatedMetadataHash", Hash);
        Set(definition, "bounds", new OperationMapBoundsConfig(
            new Vector3(-100f, -10f, -100f),
            new Vector3(100f, 50f, 100f),
            new Vector3(-90f, -5f, -90f),
            new Vector3(90f, 40f, 90f),
            new Vector3(-80f, 10f, -80f),
            new Vector3(80f, 40f, 80f)));
        Set(definition, "gridMetadata", new OperationMapGridMetadataConfig(
            Hash.Substring(0, 32), Hash, new Vector3(-100f, 0f, -100f), new Vector2Int(200, 200), 1f, 0));
        Set(definition, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(
            Hash.Substring(0, 32), Hash, Hash.Substring(0, 32), 40000, 3, 1, -10f, 50f));
        Set(definition, "navigationMetadata", new OperationMapNavigationMetadataConfig(
            Hash.Substring(0, 32), 123, 1, true, true, true));
        Set(definition, "cameras", new[]
        {
            new OperationMapCameraConfig(
                "camera.skirmish.planning",
                new Vector3(0f, 30f, 0f),
                new Vector3(60f, 0f, 0f),
                true,
                60f,
                30f,
                true),
            new OperationMapCameraConfig(
                "camera.skirmish.battle",
                new Vector3(20f, 25f, 30f),
                new Vector3(45f, 90f, 0f),
                false,
                55f,
                20f,
                true)
        });
        Set(definition, "planningCameraId", "camera.skirmish.planning");
        Set(definition, "battleCameraId", "camera.skirmish.battle");
        Set(definition, "minimap", new OperationMapMinimapConfig(
            "minimap.skirmish.projection",
            new Vector3(-100f, 0f, -50f),
            new Vector2(200f, 100f),
            0f));
        Set(definition, "anchors", new[]
        {
            new OperationMapAnchorConfig(
                "anchor.skirmish.objective.alpha",
                OperationMapAnchorKind.Objective,
                Vector3.zero,
                Vector3.zero,
                4f)
        });
        return definition;
    }

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }

    private static void SetCatalogDefinitions(
        OperationMapCatalogConfig catalog,
        OperationMapDefinition[] definitions)
    {
        FieldInfo definitionsField = typeof(OperationMapCatalogConfig).GetField(
            "definitions",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(definitionsField, Is.Not.Null);
        definitionsField.SetValue(catalog, definitions);

        OperationMapCatalogEntryConfig[] entries = new OperationMapCatalogEntryConfig[definitions.Length];
        for (int index = 0; index < definitions.Length; index++)
        {
            OperationMapDefinition definition = definitions[index];
            entries[index] = new OperationMapCatalogEntryConfig(
                definition,
                new OperationMapContentPackConfig(
                    "opmap-pack." + definition.OperationMapId.Substring("opmap.".Length),
                    OperationMapDeliveryKind.BuiltInLocal,
                    definition.ContentVersion,
                    definition.ContentHash));
        }

        FieldInfo entriesField = typeof(OperationMapCatalogConfig).GetField(
            "entries",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(entriesField, Is.Not.Null);
        entriesField.SetValue(catalog, entries);
    }

    private static void Set<T>(MatchSceneView view, string fieldName, T value)
    {
        FieldInfo field = typeof(MatchSceneView).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(view, value);
    }

    private static T Get<T>(MatchSceneView view, string fieldName)
    {
        FieldInfo field = typeof(MatchSceneView).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(view);
    }
}
