#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class Aph808MissingSerializedReferenceValidator
    {
        internal const string RuntimeUiContentFolder = "Assets/Game/Prefabs/UI/Shell/Content";
        internal const int RequiredEnabledBuildSceneCount = 2;

        public static void Run()
        {
            try
            {
                IReadOnlyList<string> issues = ValidateCurrentProject();
                if (issues.Count != 0)
                    throw new InvalidOperationException(BuildFailureMessage(issues));

                Debug.Log(
                    $"[APH-808 MissingSerializedReferenceValidation] result=Passed " +
                    $"buildScenes={RequiredEnabledBuildSceneCount} " +
                    $"runtimeUiContentPrefabs={GetRuntimeUiContentPrefabPaths().Count}");
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[APH-808 MissingSerializedReferenceValidation] result=Failed");
                Exit(1);
            }
        }

        public static IReadOnlyList<string> ValidateCurrentProject()
        {
            IReadOnlyList<string> targets = ResolveTargetPaths(
                EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path),
                GetRuntimeUiContentPrefabPaths());

            var issues = new List<string>();
            SceneSetup[] priorSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string targetPath in targets)
                {
                    if (targetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                        InspectScene(targetPath, issues);
                    else
                        InspectPrefab(targetPath, issues);
                }
            }
            finally
            {
                if (HasRestorableSceneSetup(priorSetup))
                    EditorSceneManager.RestoreSceneManagerSetup(priorSetup);
            }

            issues.Sort(StringComparer.Ordinal);
            return issues;
        }

        internal static IReadOnlyList<string> ResolveTargetPaths(
            IEnumerable<string> enabledBuildScenePaths,
            IEnumerable<string> runtimeUiContentPrefabPaths)
        {
            if (enabledBuildScenePaths == null)
                throw new InvalidOperationException("Enabled build scene paths are unavailable.");
            if (runtimeUiContentPrefabPaths == null)
                throw new InvalidOperationException("Runtime UI content prefab paths are unavailable.");

            string[] scenes = enabledBuildScenePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (scenes.Length != RequiredEnabledBuildSceneCount)
            {
                throw new InvalidOperationException(
                    $"APH-808 requires exactly {RequiredEnabledBuildSceneCount} enabled build scenes; " +
                    $"found {scenes.Length}: {string.Join(", ", scenes)}");
            }

            foreach (string scenePath in scenes)
            {
                if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Enabled build-scene path is not a Unity scene: {scenePath}");
            }

            string[] prefabs = runtimeUiContentPrefabPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (prefabs.Length == 0)
                throw new InvalidOperationException($"No runtime UI content prefabs found under {RuntimeUiContentFolder}.");

            foreach (string prefabPath in prefabs)
            {
                if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                    !IsPathInRuntimeUiContentFolder(prefabPath))
                {
                    throw new InvalidOperationException(
                        $"Runtime UI content target is outside the explicit prefab scope: {prefabPath}");
                }
            }

            return scenes.Concat(prefabs).ToArray();
        }

        internal static bool IsBrokenObjectReference(UnityEngine.Object value, EntityId entityId)
        {
            return value == null && entityId.IsValid();
        }

        internal static bool HasRestorableSceneSetup(IReadOnlyList<SceneSetup> setup)
        {
            return setup != null && setup.Any(scene => scene.isLoaded && scene.isActive);
        }

        internal static string BuildFailureMessage(IReadOnlyList<string> issues)
        {
            if (issues == null || issues.Count == 0)
                return "APH-808 found no missing serialized references.";

            return $"APH-808 found {issues.Count} missing serialized reference issue(s):\n" +
                   string.Join("\n", issues.Select(issue => "- " + issue));
        }

        private static IReadOnlyList<string> GetRuntimeUiContentPrefabPaths()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { RuntimeUiContentFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsPathInRuntimeUiContentFolder)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsPathInRuntimeUiContentFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalized = path.Replace('\\', '/');
            string directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            return string.Equals(directory, RuntimeUiContentFolder, StringComparison.Ordinal);
        }

        private static void InspectScene(string scenePath, ICollection<string> issues)
        {
            if (!File.Exists(scenePath))
            {
                issues.Add($"{scenePath}: asset file is missing");
                return;
            }

            try
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects().OrderBy(root => root.name, StringComparer.Ordinal))
                    InspectHierarchy(scenePath, root, issues);
            }
            catch (Exception exception)
            {
                issues.Add($"{scenePath}: inspection failed ({exception.GetType().Name}: {exception.Message})");
            }
        }

        private static void InspectPrefab(string prefabPath, ICollection<string> issues)
        {
            if (!File.Exists(prefabPath))
            {
                issues.Add($"{prefabPath}: asset file is missing");
                return;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null)
                {
                    issues.Add($"{prefabPath}: prefab contents could not be loaded");
                    return;
                }

                InspectHierarchy(prefabPath, root, issues);
            }
            catch (Exception exception)
            {
                issues.Add($"{prefabPath}: inspection failed ({exception.GetType().Name}: {exception.Message})");
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void InspectHierarchy(string assetPath, GameObject root, ICollection<string> issues)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            Array.Sort(transforms, (left, right) =>
                string.CompareOrdinal(GetDiagnosticPath(left), GetDiagnosticPath(right)));

            foreach (Transform transform in transforms)
            {
                string objectPath = GetDiagnosticPath(transform);
                Component[] components = transform.gameObject.GetComponents<Component>();
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    Component component = components[componentIndex];
                    if (component == null)
                    {
                        issues.Add($"{assetPath} :: {objectPath}: missing script at component index {componentIndex}");
                        continue;
                    }

                    InspectSerializedObject(assetPath, objectPath, component, issues);
                }
            }
        }

        private static void InspectSerializedObject(
            string assetPath,
            string objectPath,
            Component component,
            ICollection<string> issues)
        {
            try
            {
                var serializedObject = new SerializedObject(component);
                SerializedProperty property = serializedObject.GetIterator();
                bool visitChildren = true;
                while (property.Next(visitChildren))
                {
                    visitChildren = true;
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        !IsBrokenObjectReference(
                            property.objectReferenceValue,
                            property.objectReferenceEntityIdValue))
                    {
                        continue;
                    }

                    issues.Add(
                        $"{assetPath} :: {objectPath} :: {component.GetType().FullName}.{property.propertyPath}: " +
                        "broken object reference");
                }
            }
            catch (Exception exception)
            {
                issues.Add(
                    $"{assetPath} :: {objectPath} :: {component.GetType().FullName}: " +
                    $"serialized inspection failed ({exception.GetType().Name}: {exception.Message})");
            }
        }

        private static string GetDiagnosticPath(Transform transform)
        {
            var segments = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                segments.Push($"{current.name}[{current.GetSiblingIndex()}]");
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
}

#endif
