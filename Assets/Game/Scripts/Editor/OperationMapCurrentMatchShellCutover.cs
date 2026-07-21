using System;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Authoring;
using Game.Rendering;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapCurrentMatchShellCutover
    {
        public const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        public const string MatchRuntimeSubScenePath =
            "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity";
        private const string MatchSourceSubScenePath =
            "Assets/Game/Scenes/Match/MatchSubScene.unity";
        private const string MatchRuntimeSubSceneRootName = "MatchRuntimeSubScene";
        private const string UnitPrefabRegistryRootName = "UnitPrefabRegistryAuthoring";

        private static readonly string[] MapRootNames =
        {
            "MatchSubScene",
            "Start",
            "End",
            "Decorations",
            "Reflection Probe",
            "Map",
            "Faction2",
            "Faction3",
            "Faction4",
            "Faction5",
            "Faction1"
        };

        private static readonly string[] LegacyShellRootNames =
        {
            "Bootstrap",
            "Main Camera",
            "Global Volume",
            "Directional Light",
            "Directional Light (1)"
        };

        private static readonly string[] ShellRootNames =
        {
            "Bootstrap",
            "Main Camera",
            "Global Volume",
            "Directional Light",
            "Directional Light (1)",
            MatchRuntimeSubSceneRootName
        };

        private static readonly string[] MapReferencePropertyNames =
        {
            "staticMapPresentationManifest",
            "decorationCombinedMeshBaker",
            "decorationRoot",
            "mapBuildingAuthoringRoot",
            "mapVehicleAuthoringRoot",
            "mapSurfaceAuthoring",
            "mapBuildingPlacementConfig",
            "mapVehiclePlacementConfig",
            "runtimeGridConfig"
        };

        [MenuItem("Game/Operation Maps/Cut Over Current Match Shell")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            RequireStagedMapReady();
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene matchScene = default;
            using var journal = MatchShellCutoverJournal.Begin();
            try
            {
                EnsureMatchRuntimeSubSceneAsset();
                StaticMapPresentationBaker.BakeCurrentStagedOperationMapPresentation();
                RequireStagedManifest();

                matchScene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
                StripMapOwnership(matchScene);
                if (!EditorSceneManager.SaveScene(matchScene, MatchScenePath, saveAsCopy: false))
                    throw new InvalidOperationException("Unity failed to save the thin Match shell.");

                if (!TryValidateThinShell(matchScene, out string error))
                    throw new InvalidOperationException(error);

                journal.Commit();
                AssetDatabase.SaveAssets();
                Debug.Log("[OperationMapCurrentMatchShellCutover] result=Passed roots=6 clearedMapReferences=10");
            }
            catch
            {
                CloseMatchScene(matchScene);
                journal.Rollback();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                throw;
            }
            finally
            {
                CloseMatchScene(matchScene);
                if (!Application.isBatchMode && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        public static void ApplyForBatch() => Apply();

        public static bool TryValidateThinShell(out string error)
        {
            Scene loaded = SceneManager.GetSceneByPath(MatchScenePath);
            bool opened = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = opened
                ? EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                return TryValidateThinShell(scene, out error);
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        internal static bool IsMapRootName(string value) =>
            Array.IndexOf(MapRootNames, value) >= 0;

        private static void RequireStagedMapReady()
        {
            if (!OperationMapCurrentCompatibilityRootExtractor.TryValidate(out string error) ||
                !OperationMapCurrentCompatibilitySceneViewStager.TryValidate(out error) ||
                !OperationMapCurrentStagedSpatialBindingValidator.TryValidate(out error) ||
                !OperationMapCurrentStagedDefinitionBuilder.TryValidate(out error))
            {
                throw new InvalidOperationException(
                    $"Staged operation map is not ready for Match cutover: {error}");
            }
        }

        private static void RequireStagedManifest()
        {
            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(StaticMapPresentationBaker.ManifestPath);
            string expectedScene = OperationMapCurrentCompatibilitySceneStager.DestinationScenePath;
            if (manifest == null ||
                manifest.SchemaVersion != StaticMapPresentationManifest.CurrentSchemaVersion ||
                !string.Equals(manifest.OperationMapId, StaticMapPresentationBaker.CurrentOperationMapId, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalScenePath, expectedScene, StringComparison.Ordinal) ||
                !string.Equals(manifest.CanonicalSceneGuid, AssetDatabase.AssetPathToGUID(expectedScene), StringComparison.Ordinal) ||
                manifest.Chunks.Count != 514 || manifest.Sources.Count == 0)
            {
                throw new InvalidOperationException(
                    "Staged static-presentation manifest identity or content is incomplete after publication.");
            }
        }

        private static void StripMapOwnership(Scene scene)
        {
            List<GameObject> roots = GetRoots(scene);
            if (HasExactRootNames(roots, ShellRootNames))
            {
                if (!TryValidateThinShell(scene, out string alreadyThinError))
                    throw new InvalidOperationException(alreadyThinError);
                return;
            }

            if (HasExactRootNames(roots, LegacyShellRootNames))
            {
                EnsureMatchRuntimeSubSceneRoot(scene);
                return;
            }

            RequireFullMatchRoots(roots);
            MatchSceneView view = FindSingleMatchSceneView(roots);
            var serialized = new SerializedObject(view);
            for (int index = 0; index < MapReferencePropertyNames.Length; index++)
            {
                SerializedProperty property = serialized.FindProperty(MapReferencePropertyNames[index]) ??
                    throw new InvalidOperationException(
                        $"MatchSceneView map-reference property is missing: {MapReferencePropertyNames[index]}");
                property.objectReferenceValue = null;
            }

            SerializedProperty debugViews = serialized.FindProperty("runtimeGridDebugViews") ??
                throw new InvalidOperationException("MatchSceneView runtimeGridDebugViews property is missing.");
            debugViews.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            for (int index = roots.Count - 1; index >= 0; index--)
            {
                if (IsMapRootName(roots[index].name))
                    UnityEngine.Object.DestroyImmediate(roots[index]);
            }

            EnsureMatchRuntimeSubSceneRoot(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool TryValidateThinShell(Scene scene, out string error)
        {
            List<GameObject> roots = GetRoots(scene);
            if (!HasExactRootNames(roots, ShellRootNames))
            {
                error = "Match shell does not contain the exact accepted shell-root set.";
                return false;
            }

            MatchSceneView view;
            try
            {
                view = FindSingleMatchSceneView(roots);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            var serialized = new SerializedObject(view);
            for (int index = 0; index < MapReferencePropertyNames.Length; index++)
            {
                SerializedProperty property = serialized.FindProperty(MapReferencePropertyNames[index]);
                if (property == null || property.objectReferenceValue != null)
                {
                    error = $"Match shell retains map-reference property '{MapReferencePropertyNames[index]}'.";
                    return false;
                }
            }

            SerializedProperty debugViews = serialized.FindProperty("runtimeGridDebugViews");
            if (debugViews == null || debugViews.arraySize != 0)
            {
                error = "Match shell retains map-owned runtime grid debug views.";
                return false;
            }

            if (!HasObjectReference(serialized, "worldCamera") ||
                !HasObjectReference(serialized, "directionalLight") ||
                !HasObjectReference(serialized, "globalVolume") ||
                !HasObjectReference(serialized, "operationMapCatalog"))
            {
                error = "Match shell lost required camera, lighting, volume, or operation-map catalog ownership.";
                return false;
            }

            GameObject runtimeSubSceneRoot = roots[^1];
            SubScene runtimeSubScene = runtimeSubSceneRoot.GetComponent<SubScene>();
            error = null;
            if (runtimeSubScene == null || !runtimeSubScene.AutoLoadScene ||
                AssetDatabase.GetAssetPath(runtimeSubScene.SceneAsset) != MatchRuntimeSubScenePath ||
                !TryValidateMatchRuntimeSubSceneAsset(out error))
            {
                if (string.IsNullOrEmpty(error))
                    error = "Match shell shared runtime subscene reference is missing or invalid.";
                return false;
            }

            error = null;
            return true;
        }

        private static void EnsureMatchRuntimeSubSceneAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MatchRuntimeSubScenePath) != null)
            {
                if (!TryValidateMatchRuntimeSubSceneAsset(out string existingError))
                    throw new InvalidOperationException(existingError);
                return;
            }

            Scene sourceScene = EditorSceneManager.OpenScene(MatchSourceSubScenePath, OpenSceneMode.Single);
            Scene runtimeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject sourceRoot = null;
                GameObject[] sourceRoots = sourceScene.GetRootGameObjects();
                for (int index = 0; index < sourceRoots.Length; index++)
                {
                    if (!string.Equals(sourceRoots[index].name, UnitPrefabRegistryRootName, StringComparison.Ordinal))
                        continue;
                    if (sourceRoot != null)
                        throw new InvalidOperationException("Match source subscene contains duplicate unit registry roots.");
                    sourceRoot = sourceRoots[index];
                }

                if (sourceRoot == null || sourceRoot.GetComponent<UnitPrefabRegistryAuthoring>() == null)
                    throw new InvalidOperationException("Match source subscene has no unit prefab registry authoring root.");

                GameObject clone = UnityEngine.Object.Instantiate(sourceRoot);
                clone.name = UnitPrefabRegistryRootName;
                SceneManager.MoveGameObjectToScene(clone, runtimeScene);
                if (!EditorSceneManager.SaveScene(runtimeScene, MatchRuntimeSubScenePath, saveAsCopy: false))
                    throw new InvalidOperationException("Unity failed to save the shared Match runtime subscene.");
            }
            finally
            {
                if (runtimeScene.IsValid() && runtimeScene.isLoaded)
                    EditorSceneManager.CloseScene(runtimeScene, removeScene: true);
                if (sourceScene.IsValid() && sourceScene.isLoaded)
                    EditorSceneManager.CloseScene(sourceScene, removeScene: true);
            }

            AssetDatabase.ImportAsset(
                MatchRuntimeSubScenePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (!TryValidateMatchRuntimeSubSceneAsset(out string error))
                throw new InvalidOperationException(error);
        }

        private static void EnsureMatchRuntimeSubSceneRoot(Scene scene)
        {
            SceneAsset runtimeSceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(MatchRuntimeSubScenePath) ??
                throw new InvalidOperationException("Shared Match runtime subscene asset is missing.");
            var root = new GameObject(MatchRuntimeSubSceneRootName);
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            SubScene subScene = root.AddComponent<SubScene>();
            subScene.SceneAsset = runtimeSceneAsset;
            subScene.AutoLoadScene = true;
            root.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool TryValidateMatchRuntimeSubSceneAsset(out string error)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MatchRuntimeSubScenePath) == null)
            {
                error = "Shared Match runtime subscene asset is missing.";
                return false;
            }

            Scene loaded = SceneManager.GetSceneByPath(MatchRuntimeSubScenePath);
            bool opened = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = opened
                ? EditorSceneManager.OpenScene(MatchRuntimeSubScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                if (roots.Length != 1 ||
                    !string.Equals(roots[0].name, UnitPrefabRegistryRootName, StringComparison.Ordinal))
                {
                    error = "Shared Match runtime subscene must contain only the unit prefab registry root.";
                    return false;
                }

                UnitPrefabRegistryAuthoring authoring = roots[0].GetComponent<UnitPrefabRegistryAuthoring>();
                if (authoring == null)
                {
                    error = "Shared Match runtime subscene unit registry authoring is missing.";
                    return false;
                }

                var serialized = new SerializedObject(authoring);
                SerializedProperty config = serialized.FindProperty("config");
                SerializedProperty prefabs = serialized.FindProperty("unitSpawnPrefabs");
                if (config == null || config.objectReferenceValue == null ||
                    prefabs == null || prefabs.arraySize == 0)
                {
                    error = "Shared Match runtime subscene unit registry data is incomplete.";
                    return false;
                }

                error = null;
                return true;
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static bool HasObjectReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null && property.objectReferenceValue != null;
        }

        private static void CloseMatchScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            if (SceneManager.sceneCount == 1)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        private static void RequireFullMatchRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots.Count != LegacyShellRootNames.Length + MapRootNames.Length)
                throw new InvalidOperationException("Match root count drifted before thin-shell cutover.");
            for (int index = 0; index < roots.Count; index++)
            {
                string name = roots[index].name;
                if (Array.IndexOf(LegacyShellRootNames, name) < 0 && !IsMapRootName(name))
                    throw new InvalidOperationException($"Unclassified Match root blocks cutover: '{name}'.");
            }
        }

        private static MatchSceneView FindSingleMatchSceneView(IReadOnlyList<GameObject> roots)
        {
            MatchSceneView found = null;
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                MatchSceneView[] candidates = roots[rootIndex].GetComponentsInChildren<MatchSceneView>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Match scene contains multiple MatchSceneView components.");
                    found = candidates[index];
                }
            }
            return found != null
                ? found
                : throw new InvalidOperationException("Match scene has no MatchSceneView component.");
        }

        private static List<GameObject> GetRoots(Scene scene)
        {
            var roots = new List<GameObject>(ShellRootNames.Length + MapRootNames.Length);
            scene.GetRootGameObjects(roots);
            return roots;
        }

        private static bool HasExactRootNames(IReadOnlyList<GameObject> roots, IReadOnlyList<string> expected)
        {
            if (roots.Count != expected.Count)
                return false;
            for (int index = 0; index < expected.Count; index++)
            {
                if (!string.Equals(roots[index].name, expected[index], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private sealed class MatchShellCutoverJournal : IDisposable
        {
            private readonly string backupRoot;
            private readonly string projectRoot;
            private readonly bool outputExisted;
            private readonly bool runtimeSubSceneExisted;
            private readonly bool runtimeSubSceneMetaExisted;
            private bool completed;

            private MatchShellCutoverJournal(
                string backupRoot,
                string projectRoot,
                bool outputExisted,
                bool runtimeSubSceneExisted,
                bool runtimeSubSceneMetaExisted)
            {
                this.backupRoot = backupRoot;
                this.projectRoot = projectRoot;
                this.outputExisted = outputExisted;
                this.runtimeSubSceneExisted = runtimeSubSceneExisted;
                this.runtimeSubSceneMetaExisted = runtimeSubSceneMetaExisted;
            }

            public static MatchShellCutoverJournal Begin()
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string backupRoot = Path.Combine(
                    projectRoot,
                    "Library",
                    "OperationMapMatchShellCutover",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(backupRoot);
                File.Copy(Path.Combine(projectRoot, MatchScenePath), Path.Combine(backupRoot, "Match.unity"));

                string output = Path.Combine(projectRoot, StaticMapPresentationBaker.OutputRoot);
                bool outputExisted = Directory.Exists(output);
                if (outputExisted)
                    CopyDirectory(output, Path.Combine(backupRoot, "Presentation"));

                string runtimeSubScene = Path.Combine(projectRoot, MatchRuntimeSubScenePath);
                string runtimeSubSceneMeta = runtimeSubScene + ".meta";
                bool runtimeSubSceneExisted = File.Exists(runtimeSubScene);
                bool runtimeSubSceneMetaExisted = File.Exists(runtimeSubSceneMeta);
                if (runtimeSubSceneExisted)
                    File.Copy(runtimeSubScene, Path.Combine(backupRoot, "MatchRuntimeSubScene.unity"));
                if (runtimeSubSceneMetaExisted)
                    File.Copy(runtimeSubSceneMeta, Path.Combine(backupRoot, "MatchRuntimeSubScene.unity.meta"));
                return new MatchShellCutoverJournal(
                    backupRoot,
                    projectRoot,
                    outputExisted,
                    runtimeSubSceneExisted,
                    runtimeSubSceneMetaExisted);
            }

            public void Commit()
            {
                ThrowIfCompleted();
                completed = true;
                Directory.Delete(backupRoot, recursive: true);
            }

            public void Rollback()
            {
                if (completed)
                    return;

                File.Copy(Path.Combine(backupRoot, "Match.unity"), Path.Combine(projectRoot, MatchScenePath), true);
                RestoreFile(
                    Path.Combine(backupRoot, "MatchRuntimeSubScene.unity"),
                    Path.Combine(projectRoot, MatchRuntimeSubScenePath),
                    runtimeSubSceneExisted);
                RestoreFile(
                    Path.Combine(backupRoot, "MatchRuntimeSubScene.unity.meta"),
                    Path.Combine(projectRoot, MatchRuntimeSubScenePath) + ".meta",
                    runtimeSubSceneMetaExisted);
                string output = Path.Combine(projectRoot, StaticMapPresentationBaker.OutputRoot);
                if (Directory.Exists(output))
                    Directory.Delete(output, recursive: true);
                if (outputExisted)
                    CopyDirectory(Path.Combine(backupRoot, "Presentation"), output);
                completed = true;
                Directory.Delete(backupRoot, recursive: true);
            }

            public void Dispose()
            {
                if (!completed)
                    Rollback();
            }

            private void ThrowIfCompleted()
            {
                if (completed)
                    throw new InvalidOperationException("Match shell cutover journal is already complete.");
            }

            private static void CopyDirectory(string source, string destination)
            {
                Directory.CreateDirectory(destination);
                foreach (string file in Directory.GetFiles(source))
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
                foreach (string directory in Directory.GetDirectories(source))
                    CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }

            private static void RestoreFile(string backup, string destination, bool existed)
            {
                if (existed)
                    File.Copy(backup, destination, true);
                else if (File.Exists(destination))
                    File.Delete(destination);
            }
        }
    }
}
