using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentCompatibilitySceneViewStager
    {
        private const string ViewRootName = "OperationMapSceneView";
        private const string OperationMapId = "opmap.skirmish.desert_base_01";
        private const string GridConfigPath =
            "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";

        [MenuItem("Tools/Warline Capture/Operation Maps/Bind Current Scene View")]
        public static void Stage()
        {
            OperationMapCurrentStagedDefinitionBuilder.Stage();

            Scene scene = OpenStagedScene();
            try
            {
                OperationMapSceneView view = FindOrCreateView(scene);
                Bind(view, scene);
                if (!view.TryValidate(out string error))
                    throw new InvalidOperationException(error);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        OperationMapCurrentCompatibilitySceneStager.DestinationScenePath,
                        saveAsCopy: false))
                {
                    throw new InvalidOperationException("Unity failed to save the staged operation-map scene view.");
                }
            }
            finally
            {
                CloseScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!TryValidate(out string validationError))
                throw new InvalidOperationException(validationError);
            if (!OperationMapCurrentStagedSpatialBindingValidator.TryValidate(out validationError))
                throw new InvalidOperationException(validationError);
        }

        public static void StageForBatch() => Stage();

        public static bool TryValidate(out string error)
        {
            Scene scene;
            try
            {
                scene = OpenStagedScene();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            try
            {
                List<OperationMapSceneView> views = FindViews(scene);
                if (views.Count != 1)
                {
                    error = $"Staged operation map requires exactly one scene view; found {views.Count}.";
                    return false;
                }

                if (!views[0].TryValidate(out error))
                    return false;

                OperationMapSceneView view = views[0];
                if (!string.Equals(view.gameObject.name, ViewRootName, StringComparison.Ordinal) ||
                    !string.Equals(view.OperationMapId, OperationMapId, StringComparison.Ordinal) ||
                    !string.Equals(view.DecorationRoot.name, "Decorations", StringComparison.Ordinal) ||
                    !string.Equals(view.BuildingAuthoringRoot.name, "Buildings", StringComparison.Ordinal) ||
                    !string.Equals(view.VehicleAuthoringRoot.name, "Vehicles", StringComparison.Ordinal) ||
                    view.DecorationCombinedMeshBaker.transform != view.DecorationRoot ||
                    AssetDatabase.GetAssetPath(view.Definition) !=
                    OperationMapCurrentStagedDefinitionBuilder.DefinitionPath ||
                    AssetDatabase.GetAssetPath(view.BuildingPlacements) !=
                    OperationMapCurrentCompatibilityPlacementStager.DestinationBuildingConfigPath ||
                    AssetDatabase.GetAssetPath(view.VehiclePlacements) !=
                    OperationMapCurrentCompatibilityPlacementStager.DestinationVehicleConfigPath ||
                    AssetDatabase.GetAssetPath(view.GridAuthoringConfig) != GridConfigPath ||
                    AssetDatabase.GetAssetPath(view.MapSubScene.SceneAsset) !=
                    OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath ||
                    !string.Equals(
                        view.Definition.NavigationMetadata.AuthoredSubSceneGuid,
                        AssetDatabase.AssetPathToGUID(
                            OperationMapCurrentCompatibilitySubSceneStager.DestinationSubScenePath),
                        StringComparison.Ordinal))
                {
                    error = "Staged operation-map scene view reference identity drifted.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                CloseScene(scene);
            }
        }

        private static void Bind(OperationMapSceneView view, Scene scene)
        {
            GameObject map = FindRequiredRoot(scene, "Map");
            Transform decorations = FindRequiredRoot(scene, "Decorations").transform;
            Transform buildingsRoot = FindRequiredChild(map.transform, "Buildings");
            Transform vehiclesRoot = FindRequiredChild(map.transform, "Vehicles");
            CombinedMeshBaker combinedMeshBaker = decorations.GetComponent<CombinedMeshBaker>();
            if (combinedMeshBaker == null)
                throw new InvalidOperationException("Staged operation map decorations require CombinedMeshBaker.");
            MapSurfaceAuthoring surface = FindSingleComponent<MapSurfaceAuthoring>(scene);
            GridAuthoringConfig gridConfig = LoadRequired<GridAuthoringConfig>(GridConfigPath);
            SubScene subScene = FindSingleComponent<SubScene>(scene);
            OperationMapDefinition definition = LoadRequired<OperationMapDefinition>(
                OperationMapCurrentStagedDefinitionBuilder.DefinitionPath);
            MapBuildingPlacementConfig buildings = LoadRequired<MapBuildingPlacementConfig>(
                OperationMapCurrentCompatibilityPlacementStager.DestinationBuildingConfigPath);
            MapVehiclePlacementConfig vehicles = LoadRequired<MapVehiclePlacementConfig>(
                OperationMapCurrentCompatibilityPlacementStager.DestinationVehicleConfigPath);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("operationMapId").stringValue = OperationMapId;
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("mapRoot").objectReferenceValue = map.transform;
            serialized.FindProperty("decorationCombinedMeshBaker").objectReferenceValue = combinedMeshBaker;
            serialized.FindProperty("decorationRoot").objectReferenceValue = decorations;
            serialized.FindProperty("buildingAuthoringRoot").objectReferenceValue = buildingsRoot;
            serialized.FindProperty("vehicleAuthoringRoot").objectReferenceValue = vehiclesRoot;
            serialized.FindProperty("mapSurfaceAuthoring").objectReferenceValue = surface;
            serialized.FindProperty("gridAuthoringConfig").objectReferenceValue = gridConfig;
            serialized.FindProperty("buildingPlacements").objectReferenceValue = buildings;
            serialized.FindProperty("vehiclePlacements").objectReferenceValue = vehicles;
            serialized.FindProperty("mapSubScene").objectReferenceValue = subScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static OperationMapSceneView FindOrCreateView(Scene scene)
        {
            List<OperationMapSceneView> views = FindViews(scene);
            if (views.Count > 1)
                throw new InvalidOperationException("Staged operation map contains multiple scene views.");
            if (views.Count == 1)
            {
                if (!string.Equals(views[0].gameObject.name, ViewRootName, StringComparison.Ordinal) ||
                    views[0].transform.parent != null)
                    throw new InvalidOperationException("Existing operation-map scene view root identity drifted.");
                return views[0];
            }

            var root = new GameObject(ViewRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.AddComponent<OperationMapSceneView>();
        }

        private static List<OperationMapSceneView> FindViews(Scene scene)
        {
            var views = new List<OperationMapSceneView>(1);
            foreach (GameObject root in scene.GetRootGameObjects())
                views.AddRange(root.GetComponentsInChildren<OperationMapSceneView>(true));
            return views;
        }

        private static T FindSingleComponent<T>(Scene scene) where T : Component
        {
            T found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] candidates = root.GetComponentsInChildren<T>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (found != null)
                        throw new InvalidOperationException($"Staged operation map contains multiple {typeof(T).Name} components.");
                    found = candidates[index];
                }
            }

            return found != null
                ? found
                : throw new InvalidOperationException($"Staged operation map has no {typeof(T).Name} component.");
        }

        private static GameObject FindRequiredRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, StringComparison.Ordinal))
                    return root;
            }
            throw new InvalidOperationException($"Staged operation map has no '{name}' root.");
        }

        private static Transform FindRequiredChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null
                ? child
                : throw new InvalidOperationException(
                    $"Staged operation map root has no direct '{name}' child.");
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException($"Required asset is missing: '{path}'.");
        }

        private static Scene OpenStagedScene()
        {
            string path = OperationMapCurrentCompatibilitySceneStager.DestinationScenePath;
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException($"Close '{path}' before scene-view staging or validation.");
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void CloseScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, removeScene: true);
        }
    }
}
