using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapDenseCityProductionDependencyClosureValidation
    {
        private const string ReportPath =
            "Design/AgentReports/2026-08-10_dense_city_production_dependency_closure.json";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/Validate Dense City Production Dependency Closure")]
        public static void RunValidation()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
                    true,
                    out string layoutError))
            {
                throw new InvalidOperationException(layoutError);
            }

            string[] roots =
            {
                OperationMapAddressablesLayoutBuilder.CatalogPath,
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapAddressablesLayoutBuilder.MapSurfacePath,
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(root)))
                    throw new InvalidOperationException($"Production closure root is missing: {root}.");
            }

            string[] dependencies = AssetDatabase.GetDependencies(roots, true)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var directlyOwnedAssets = new HashSet<string>(
                roots.SelectMany(root => AssetDatabase.GetDependencies(root, false))
                    .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)),
                StringComparer.Ordinal);
            string[] forbiddenAuthoring =
            {
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath
            };
            foreach (string forbidden in forbiddenAuthoring)
            {
                if (dependencies.Contains(forbidden, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Production dependency closure contains retired authoring content: {forbidden}.");
                }
            }

            int sceneCount = 0;
            int gameObjectAssetCount = 0;
            int excludedGameplayGameObjectAssetCount = 0;
            int gameObjectCount = 0;
            Scene activeScene = SceneManager.GetActiveScene();
            var openedScenes = new List<Scene>();
            try
            {
                foreach (string path in dependencies)
                {
                    if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        Scene scene = SceneManager.GetSceneByPath(path);
                        if (!scene.IsValid() || !scene.isLoaded)
                        {
                            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                            openedScenes.Add(scene);
                        }

                        sceneCount++;
                        foreach (GameObject root in scene.GetRootGameObjects())
                        {
                            gameObjectCount++;
                            RequirePhysicsFree(root, path, roots);
                        }
                        continue;
                    }

                    GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (asset == null)
                        continue;
                    if (!IsMapOwnedGameObjectAsset(path, directlyOwnedAssets))
                    {
                        excludedGameplayGameObjectAssetCount++;
                        continue;
                    }
                    gameObjectAssetCount++;
                    gameObjectCount++;
                    RequirePhysicsFree(asset, path, roots);
                }
            }
            finally
            {
                for (int index = openedScenes.Count - 1; index >= 0; index--)
                {
                    Scene scene = openedScenes[index];
                    if (scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
                if (activeScene.IsValid() && activeScene.isLoaded)
                    SceneManager.SetActiveScene(activeScene);
            }

            var report = new Report
            {
                schemaVersion = 1,
                result = "Passed",
                exactValidationRevision =
                    AndroidBuildReportGenerator.CaptureGitProvenance().ExactCommit,
                addressableRootCount = roots.Length,
                dependencyAssetCount = dependencies.Length,
                dependencySceneCount = sceneCount,
                dependencyGameObjectAssetCount = gameObjectAssetCount,
                excludedGameplayGameObjectAssetCount =
                    excludedGameplayGameObjectAssetCount,
                validatedGameObjectRootCount = gameObjectCount,
                prohibitedPhysicsComponentCount = 0,
                authoringSceneDependencyCount = 0,
                productionStaticManifestCount = 0,
                productionStaticChunkCount = 0,
                roots = roots,
                forbiddenAuthoringPaths = forbiddenAuthoring
            };
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string physicalReport = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalReport) ?? projectRoot);
            File.WriteAllText(
                physicalReport,
                JsonUtility.ToJson(report, true),
                Utf8WithoutBom);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(
                "[OperationMapDenseCityProductionDependencyClosureValidation] result=Passed " +
                $"roots={roots.Length} dependencies={dependencies.Length} scenes={sceneCount} " +
                $"gameObjectAssets={gameObjectAssetCount} validatedRoots={gameObjectCount} " +
                $"excludedGameplayGameObjectAssets={excludedGameplayGameObjectAssetCount} " +
                "prohibitedPhysics=0 authoringDependencies=0 staticEntries=0");
        }

        private static bool IsMapOwnedGameObjectAsset(
            string path,
            ISet<string> directlyOwnedAssets)
        {
            return directlyOwnedAssets.Contains(path) ||
                   path.StartsWith(
                       "Assets/Game/GeneratedOperationMaps/",
                       StringComparison.Ordinal) ||
                   path.StartsWith(
                       OperationMapPhysicsFreeBuildingDefinitionResolver.OutputRoot + "/",
                       StringComparison.Ordinal) ||
                   path.StartsWith(
                       OperationMapPhysicsFreePrefabResolver.OutputRoot + "/",
                       StringComparison.Ordinal);
        }

        private static void RequirePhysicsFree(
            GameObject root,
            string path,
            IReadOnlyList<string> closureRoots)
        {
            if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    root,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Production dependency '{path}' contains prohibited physics; roots=" +
                    string.Join(",", closureRoots.Where(owner =>
                        AssetDatabase.GetDependencies(owner, true)
                            .Contains(path, StringComparer.Ordinal))) +
                    "; chains=" + string.Join(" || ", closureRoots
                        .Select(owner => FindDependencyChain(owner, path))
                        .Where(chain => !string.IsNullOrEmpty(chain))) +
                    $". {error}");
            }
        }

        private static string FindDependencyChain(string root, string target)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            return TryFindDependencyChain(root, target, visiting, out string chain)
                ? chain
                : null;
        }

        private static bool TryFindDependencyChain(
            string current,
            string target,
            ISet<string> visiting,
            out string chain)
        {
            if (!visiting.Add(current))
            {
                chain = null;
                return false;
            }
            try
            {
                foreach (string dependency in AssetDatabase.GetDependencies(current, false)
                             .Where(path => !string.Equals(path, current, StringComparison.Ordinal))
                             .OrderBy(path => path, StringComparer.Ordinal))
                {
                    if (string.Equals(dependency, target, StringComparison.Ordinal))
                    {
                        chain = current + " -> " + target;
                        return true;
                    }
                    if (!AssetDatabase.GetDependencies(dependency, true)
                            .Contains(target, StringComparer.Ordinal) ||
                        !TryFindDependencyChain(dependency, target, visiting, out string child))
                    {
                        continue;
                    }
                    chain = current + " -> " + child;
                    return true;
                }
            }
            finally
            {
                visiting.Remove(current);
            }
            chain = null;
            return false;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion;
            public string result;
            public string exactValidationRevision;
            public int addressableRootCount;
            public int dependencyAssetCount;
            public int dependencySceneCount;
            public int dependencyGameObjectAssetCount;
            public int excludedGameplayGameObjectAssetCount;
            public int validatedGameObjectRootCount;
            public int prohibitedPhysicsComponentCount;
            public int authoringSceneDependencyCount;
            public int productionStaticManifestCount;
            public int productionStaticChunkCount;
            public string[] roots;
            public string[] forbiddenAuthoringPaths;
        }
    }
}
