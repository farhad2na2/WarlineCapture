using System;
using System.IO;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class OperationMapPhysicsFreeBuildingDefinitionResolver
    {
        internal const string OutputRoot =
            "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreeBuildingDefinitions";

        internal static BuildingDefinitionAuthoring Resolve(
            BuildingDefinitionAuthoring source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    source.gameObject,
                    out _))
            {
                return source;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source.gameObject);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(sourceGuid))
                throw new InvalidOperationException("Building definition source must be a persistent prefab.");

            EnsureFolder(OutputRoot);
            string outputPath =
                $"{OutputRoot}/{source.gameObject.name}_{sourceGuid.Substring(0, 8)}.prefab";
            string provenance =
                $"warline.physics-free-building-definition.v3|{sourcePath}|" +
                AssetDatabase.GetAssetDependencyHash(sourcePath);
            BuildingDefinitionAuthoring existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(outputPath)?
                    .GetComponent<BuildingDefinitionAuthoring>();
            AssetImporter existingImporter = AssetImporter.GetAtPath(outputPath);
            if (existing != null && existingImporter != null &&
                string.Equals(existingImporter.userData, provenance, StringComparison.Ordinal) &&
                DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    existing.gameObject,
                    out _))
            {
                return existing;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source.gameObject);
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate building definition: {sourcePath}.");
            try
            {
                PrefabUtility.UnpackPrefabInstance(
                    instance,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(instance);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
                if (saved == null)
                    throw new InvalidOperationException($"Could not save physics-free definition: {outputPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetImporter importer = AssetImporter.GetAtPath(outputPath);
            if (importer == null)
                throw new InvalidOperationException($"Physics-free definition importer is missing: {outputPath}.");
            importer.userData = provenance;
            importer.SaveAndReimport();
            BuildingDefinitionAuthoring result =
                AssetDatabase.LoadAssetAtPath<GameObject>(outputPath)?
                    .GetComponent<BuildingDefinitionAuthoring>();
            string validationError = null;
            if (result == null ||
                !DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    result.gameObject,
                    out validationError))
            {
                throw new InvalidOperationException(
                    validationError ?? $"Physics-free definition is invalid: {outputPath}.");
            }
            return result;
        }

        [MenuItem("Game/Operation Maps/Repair Dense City Physics-Free Building Definitions")]
        public static void RepairCurrentProduction()
        {
            string scenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                OperationMapBuildingAuthoring[] authorings =
                    UnityEngine.Object.FindObjectsByType<OperationMapBuildingAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                int remapped = 0;
                int uniqueGeneratedDefinitions = 0;
                var generatedPaths = new System.Collections.Generic.HashSet<string>(
                    StringComparer.Ordinal);
                foreach (OperationMapBuildingAuthoring authoring in authorings)
                {
                    BuildingDefinitionAuthoring source = ResolveOriginalSource(authoring.Definition);
                    BuildingDefinitionAuthoring resolved = Resolve(source);
                    string resolvedPath = AssetDatabase.GetAssetPath(resolved);
                    if (resolvedPath.StartsWith(OutputRoot + "/", StringComparison.Ordinal) &&
                        generatedPaths.Add(resolvedPath))
                    {
                        uniqueGeneratedDefinitions++;
                    }
                    if (resolved == authoring.Definition)
                        continue;
                    var serialized = new SerializedObject(authoring);
                    serialized.FindProperty("definition").objectReferenceValue = resolved;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    remapped++;
                }

                if (uniqueGeneratedDefinitions != 3)
                {
                    throw new InvalidOperationException(
                        $"Expected three physics-bearing Tent definitions; " +
                        $"found remapped={remapped}, definitions={uniqueGeneratedDefinitions}.");
                }
                bool lightingDataCleared = Lightmapping.lightingDataAsset != null;
                if (lightingDataCleared)
                {
                    Lightmapping.lightingDataAsset = null;
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                if (!EditorSceneManager.SaveScene(scene, scenePath, false))
                    throw new InvalidOperationException("Physics-free definition repair did not save.");
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    scenePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                Debug.Log(
                    "[OperationMapPhysicsFreeBuildingDefinitionRepair] result=Passed " +
                    $"authorings={authorings.Length} remapped={remapped} " +
                    $"generatedDefinitions={uniqueGeneratedDefinitions} " +
                    $"lightingDataCleared={(lightingDataCleared ? 1 : 0)} " +
                    "sourcePrefabsMutated=0");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
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

        private static BuildingDefinitionAuthoring ResolveOriginalSource(
            BuildingDefinitionAuthoring definition)
        {
            if (definition == null)
                throw new InvalidOperationException("Operation-map building definition is missing.");
            string path = AssetDatabase.GetAssetPath(definition.gameObject);
            if (!path.StartsWith(OutputRoot + "/", StringComparison.Ordinal))
                return definition;

            string userData = AssetImporter.GetAtPath(path)?.userData ?? string.Empty;
            string[] fields = userData.Split('|');
            if (fields.Length < 3 ||
                fields[0] != "warline.physics-free-building-definition.v1" &&
                fields[0] != "warline.physics-free-building-definition.v2" &&
                fields[0] != "warline.physics-free-building-definition.v3")
            {
                throw new InvalidOperationException(
                    $"Generated physics-free definition provenance is invalid: {path}.");
            }
            string originalPath = fields[0] == "warline.physics-free-building-definition.v3"
                ? fields[1]
                : AssetDatabase.GUIDToAssetPath(fields[1]);
            BuildingDefinitionAuthoring original =
                AssetDatabase.LoadAssetAtPath<GameObject>(originalPath)?
                    .GetComponent<BuildingDefinitionAuthoring>();
            return original ?? throw new InvalidOperationException(
                $"Original building definition is missing for generated definition: {path}.");
        }
    }
}
