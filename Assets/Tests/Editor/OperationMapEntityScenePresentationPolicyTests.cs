using System.Reflection;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using NUnit.Framework;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapEntityScenePresentationPolicyTests
{
    [Test]
    public void UsesEntityScenePresentation_RequiresPresentationKind()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        try
        {
            Assert.That(
                OperationMapEntityScenePresentationPolicy.UsesEntityScenePresentation(definition),
                Is.False);
            Assert.That(
                OperationMapEntityScenePresentationPolicy.ShouldSkipStaticManifestStreamerAndOwnership(
                    definition),
                Is.False);

            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Assert.That(
                OperationMapEntityScenePresentationPolicy.UsesEntityScenePresentation(definition),
                Is.True);
            Assert.That(
                OperationMapEntityScenePresentationPolicy.ShouldSkipStaticManifestStreamerAndOwnership(
                    definition),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void TryValidateEntitySceneBinding_AllowsEmptyPlacementsAndRequiresSubScene()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        GameObject root = new("EntityScenePolicyRoot");
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Transform mapRoot = root.transform;
            Transform buildings = new GameObject("Buildings").transform;
            buildings.SetParent(mapRoot, false);
            Transform vehicles = new GameObject("Vehicles").transform;
            vehicles.SetParent(mapRoot, false);
            SubScene subScene = root.AddComponent<SubScene>();

            Assert.That(
                OperationMapEntityScenePresentationPolicy.TryValidateEntitySceneBinding(
                    definition,
                    OperationMapCanonicalPresentationMode.EntityScene,
                    mapRoot,
                    buildings,
                    vehicles,
                    subScene,
                    buildingPlacements: null,
                    vehiclePlacements: null,
                    out string error),
                Is.True,
                error);

            Assert.That(
                OperationMapEntityScenePresentationPolicy.TryValidateEntitySceneBinding(
                    definition,
                    OperationMapCanonicalPresentationMode.EntityScene,
                    mapRoot,
                    buildings,
                    vehicles,
                    mapSubScene: null,
                    buildingPlacements: null,
                    vehiclePlacements: null,
                    out error),
                Is.False);
            Assert.That(error, Does.Contain("SubScene"));
        }
        finally
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TryValidateEntitySceneBinding_RejectsRenderersUnderMapRoot()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        GameObject root = new("EntityScenePolicyRendererRoot");
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Transform mapRoot = root.transform;
            Transform buildings = new GameObject("Buildings").transform;
            buildings.SetParent(mapRoot, false);
            Transform vehicles = new GameObject("Vehicles").transform;
            vehicles.SetParent(mapRoot, false);
            GameObject meshObject = new("Mesh");
            meshObject.transform.SetParent(mapRoot, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = new Mesh();
            meshObject.AddComponent<MeshRenderer>();
            SubScene subScene = root.AddComponent<SubScene>();

            Assert.That(
                OperationMapEntityScenePresentationPolicy.TryValidateEntitySceneBinding(
                    definition,
                    OperationMapCanonicalPresentationMode.EntityScene,
                    mapRoot,
                    buildings,
                    vehicles,
                    subScene,
                    buildingPlacements: null,
                    vehiclePlacements: null,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("renderer-free"));
        }
        finally
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SceneView_EmptyPlacementsAcceptedOnlyForEntityScene()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        GridAuthoringConfig grid = ScriptableObject.CreateInstance<GridAuthoringConfig>();
        GameObject viewObject = new("OperationMapSceneView");
        try
        {
            Set(definition, "operationMapId", "opmap.skirmish.entity_scene_view");
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);

            OperationMapSceneView view = viewObject.AddComponent<OperationMapSceneView>();
            Transform mapRoot = new GameObject("Map").transform;
            mapRoot.SetParent(viewObject.transform, false);
            CombinedMeshBaker decoration = new GameObject("Decoration").AddComponent<CombinedMeshBaker>();
            decoration.transform.SetParent(mapRoot, false);
            Transform buildings = new GameObject("Buildings").transform;
            buildings.SetParent(mapRoot, false);
            Transform vehicles = new GameObject("Vehicles").transform;
            vehicles.SetParent(mapRoot, false);
            MapSurfaceAuthoring surface = new GameObject("Surface").AddComponent<MapSurfaceAuthoring>();
            surface.transform.SetParent(viewObject.transform, false);
            SubScene subScene = viewObject.AddComponent<SubScene>();
            Set(
                definition,
                "navigationMetadata",
                new OperationMapNavigationMetadataConfig(
                    subScene.SceneGUID.ToString(),
                    0,
                    0,
                    false,
                    false,
                    false));

            SerializedObject serialized = new(view);
            serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.entity_scene_view";
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("canonicalPresentationMode").enumValueIndex =
                (int)OperationMapCanonicalPresentationMode.EntityScene;
            serialized.FindProperty("mapRoot").objectReferenceValue = mapRoot;
            serialized.FindProperty("decorationCombinedMeshBaker").objectReferenceValue = decoration;
            serialized.FindProperty("decorationRoot").objectReferenceValue = decoration.transform;
            serialized.FindProperty("buildingAuthoringRoot").objectReferenceValue = buildings;
            serialized.FindProperty("vehicleAuthoringRoot").objectReferenceValue = vehicles;
            serialized.FindProperty("mapSurfaceAuthoring").objectReferenceValue = surface;
            serialized.FindProperty("gridAuthoringConfig").objectReferenceValue = grid;
            serialized.FindProperty("mapSubScene").objectReferenceValue = subScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(view.TryValidate(out string error), Is.True, error);

            Set(definition, "presentationKind", OperationMapPresentationKind.StaticSceneChunks);
            serialized.FindProperty("canonicalPresentationMode").enumValueIndex =
                (int)OperationMapCanonicalPresentationMode.SourceRenderersPresent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(view.TryValidate(out error), Is.False);
            Assert.That(error, Does.Contain("non-empty"));
        }
        finally
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(grid);
            Object.DestroyImmediate(viewObject);
        }
    }

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }
}
