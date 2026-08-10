using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    internal static class OperationMapPhysicsFreePrefabResolver
    {
        internal const string OutputRoot =
            "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions";

        internal static GameObject Resolve(GameObject source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    source,
                    out _))
            {
                return source;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(sourceGuid) ||
                !sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Physics-free prefab resolution requires a persistent prefab source.");
            }

            EnsureFolder(OutputRoot);
            string outputPath =
                $"{OutputRoot}/{source.name}_{sourceGuid.Substring(0, 8)}.prefab";
            string provenance =
                $"warline.physics-free-prefab.v1|{sourcePath}|" +
                AssetDatabase.GetAssetDependencyHash(sourcePath);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            AssetImporter existingImporter = AssetImporter.GetAtPath(outputPath);
            if (existing != null &&
                existingImporter != null &&
                string.Equals(existingImporter.userData, provenance, StringComparison.Ordinal) &&
                DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    existing,
                    out _))
            {
                return existing;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputPath) != null &&
                !AssetDatabase.DeleteAsset(outputPath))
            {
                throw new InvalidOperationException(
                    $"Could not replace stale physics-free prefab: {outputPath}.");
            }
            if (!AssetDatabase.CopyAsset(sourcePath, outputPath))
            {
                throw new InvalidOperationException(
                    $"Could not copy physics-free prefab source: {sourcePath}.");
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(outputPath);
            try
            {
                DenseCityPhysicsComponentStripper.StripInstanceHierarchy(contents);
                if (PrefabUtility.SaveAsPrefabAsset(contents, outputPath) == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save physics-free prefab: {outputPath}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetImporter importer = AssetImporter.GetAtPath(outputPath);
            if (importer == null)
                throw new InvalidOperationException($"Prefab importer is missing: {outputPath}.");
            importer.userData = provenance;
            importer.SaveAndReimport();
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            string error = null;
            if (result == null ||
                !DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    result,
                    out error))
            {
                throw new InvalidOperationException(
                    error ?? $"Physics-free prefab is invalid: {outputPath}.");
            }
            return result;
        }

        [MenuItem("Game/Operation Maps/Repair Dense City Physics-Free Prefab Dependencies")]
        public static void RepairCurrentProduction()
        {
            string scenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string sceneText = File.ReadAllText(scenePath);
            string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
            var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string dependency in dependencies)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(dependency);
                if (source == null ||
                    !dependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                    DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                        source,
                        out _))
                {
                    continue;
                }

                GameObject resolved = Resolve(source);
                string sourceGuid = AssetDatabase.AssetPathToGUID(dependency);
                string resolvedGuid =
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(resolved));
                if (string.IsNullOrWhiteSpace(sourceGuid) ||
                    string.IsNullOrWhiteSpace(resolvedGuid) ||
                    string.Equals(sourceGuid, resolvedGuid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Invalid physics-free prefab GUID mapping for {dependency}.");
                }
                replacements.Add(sourceGuid, resolvedGuid);
            }

            int beforeLength = sceneText.Length;
            int replacedReferences = 0;
            sceneText = Regex.Replace(
                sceneText,
                @"guid: ([0-9a-f]{32})",
                match =>
                {
                    if (!replacements.TryGetValue(match.Groups[1].Value, out string replacement))
                        return match.Value;
                    replacedReferences++;
                    return "guid: " + replacement;
                },
                RegexOptions.CultureInvariant);
            if (sceneText.Length != beforeLength)
                throw new InvalidOperationException("GUID replacement changed scene byte length.");
            if (replacements.Count == 0 || replacedReferences == 0)
            {
                throw new InvalidOperationException(
                    "Current production scene had no physics-bearing prefab dependencies to repair.");
            }

            MethodInfo releaseCachedFileHandles = typeof(AssetDatabase).GetMethod(
                "ReleaseCachedFileHandles",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (releaseCachedFileHandles == null)
            {
                throw new InvalidOperationException(
                    "Unity AssetDatabase cached-handle release API is unavailable.");
            }
            releaseCachedFileHandles.Invoke(null, null);
            File.WriteAllText(scenePath, sceneText);
            AssetDatabase.ImportAsset(
                scenePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                        root,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }
            }
            if (!EditorSceneManager.SaveScene(scene, scenePath, false))
                throw new InvalidOperationException("Physics-free prefab repair did not save.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[OperationMapPhysicsFreePrefabDependencyRepair] result=Passed " +
                $"prefabMappings={replacements.Count} replacedReferences={replacedReferences} " +
                "sourcePrefabsMutated=0");
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
}
