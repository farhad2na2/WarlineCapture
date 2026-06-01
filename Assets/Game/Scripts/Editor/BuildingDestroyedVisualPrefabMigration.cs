#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildingDestroyedVisualPrefabMigration
{
    private const string BuildingPrefabRoot = "Assets/Game/Prefabs/Buildings";
    private const string DestroyedPrefabRoot = "Assets/Game/Prefabs/Buildings/Destroyed";

    public static void ExtractDestroyedVisualPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(DestroyedPrefabRoot))
            AssetDatabase.CreateFolder(BuildingPrefabRoot, "Destroyed");

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { BuildingPrefabRoot });
        int extracted = 0;
        int assigned = 0;
        int removed = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!ShouldProcessPrefab(prefabPath))
                continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BuildingDefinitionAuthoring authoring = prefabRoot.GetComponent<BuildingDefinitionAuthoring>();
                if (authoring == null)
                    continue;

                Transform destroyed = FindDirectOrNestedByName(prefabRoot.transform, "Destroyed");
                GameObject destroyedPrefab = null;
                if (destroyed != null)
                {
                    destroyedPrefab = SaveDestroyedVisualPrefab(prefabPath, destroyed);
                    extracted++;
                    destroyed.SetParent(null, false);
                    Object.DestroyImmediate(destroyed.gameObject, true);
                    removed++;
                }
                else
                {
                    string expectedPath = BuildDestroyedPrefabPath(prefabPath);
                    destroyedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
                }

                bool prefabDirty = destroyed != null;
                if (destroyedPrefab != null && AssignDestroyedVisual(authoring, destroyedPrefab, out bool authoringChanged))
                {
                    assigned++;
                    prefabDirty |= authoringChanged;
                }

                if (prefabDirty)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BuildingDestroyedVisualPrefabMigration] extracted={extracted} assigned={assigned} removed={removed}");
    }

    private static bool ShouldProcessPrefab(string prefabPath)
    {
        if (string.IsNullOrWhiteSpace(prefabPath))
            return false;
        if (!prefabPath.StartsWith(BuildingPrefabRoot + "/", System.StringComparison.Ordinal))
            return false;
        if (prefabPath.StartsWith(DestroyedPrefabRoot + "/", System.StringComparison.Ordinal))
            return false;
        if (Path.GetFileName(prefabPath) == "BuildingSelectionMarker.prefab")
            return false;
        return Path.GetExtension(prefabPath) == ".prefab";
    }

    private static GameObject SaveDestroyedVisualPrefab(string sourcePrefabPath, Transform destroyed)
    {
        GameObject root = new($"{Path.GetFileNameWithoutExtension(sourcePrefabPath)}_Destroyed");
        try
        {
            GameObject copy = Object.Instantiate(destroyed.gameObject);
            copy.name = "Destroyed";
            copy.SetActive(true);
            copy.transform.SetParent(root.transform, false);
            copy.transform.localPosition = destroyed.localPosition;
            copy.transform.localRotation = destroyed.localRotation;
            copy.transform.localScale = destroyed.localScale;

            return PrefabUtility.SaveAsPrefabAsset(root, BuildDestroyedPrefabPath(sourcePrefabPath));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static string BuildDestroyedPrefabPath(string sourcePrefabPath)
    {
        return $"{DestroyedPrefabRoot}/{Path.GetFileNameWithoutExtension(sourcePrefabPath)}_Destroyed.prefab";
    }

    private static bool AssignDestroyedVisual(
        BuildingDefinitionAuthoring authoring,
        GameObject destroyedPrefab,
        out bool authoringChanged)
    {
        authoringChanged = false;
        if (authoring == null || destroyedPrefab == null)
            return false;

        SerializedObject authoringObject = new(authoring);
        SerializedProperty configProperty = authoringObject.FindProperty("config");
        Object config = configProperty != null ? configProperty.objectReferenceValue : null;
        if (config != null)
        {
            SerializedObject configObject = new(config);
            SerializedProperty configDestroyed = configObject.FindProperty("destroyedVisualPrefab");
            if (configDestroyed != null)
            {
                configDestroyed.objectReferenceValue = destroyedPrefab;
                configObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                return true;
            }
        }

        SerializedProperty authoringDestroyed = authoringObject.FindProperty("destroyedVisualPrefab");
        if (authoringDestroyed == null)
            return false;

        authoringDestroyed.objectReferenceValue = destroyedPrefab;
        authoringObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(authoring);
        authoringChanged = true;
        return true;
    }

    private static Transform FindDirectOrNestedByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
                return child;

            Transform nested = FindDirectOrNestedByName(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
#endif
