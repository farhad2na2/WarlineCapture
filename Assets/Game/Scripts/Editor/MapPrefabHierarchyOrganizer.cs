#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapPrefabHierarchyOrganizer
{
    private const string MapPrefabPath = "Assets/Game/Prefabs/Maps/Map.prefab";
    private const string MapRootName = "Map";
    private const string LeftoverParentName = "Map_Unsorted";

    private static readonly string[] CanonicalParents =
    {
        "Clouds",
        "Bushes",
        "Trees",
        "Rocks",
        "Mountains",
        "Grass",
        "Plants",
        "Ground",
        "Ruins",
        "FX",
        "Items",
        "Skydome",
        "Beaches",
        "Bridges",
        "Docks",
        "Concrete",
        "Roads",
        "Buildings",
        "Vehicles",
        "Characters",
        "Weapons",
        "Lights",
        "Props",
        "Runways",
        "ResourceAreas",
        "Military"
    };

    [MenuItem("WarlineCapture/Maps/Reorganize Map Prefab By Type")]
    public static void ReorganizeMapPrefabByType()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            Transform mapRoot = prefabRoot.name == MapRootName
                ? prefabRoot.transform
                : prefabRoot.transform.Find(MapRootName);

            if (mapRoot == null)
                throw new InvalidOperationException($"Could not find {MapRootName} root in {MapPrefabPath}.");

            Dictionary<string, Transform> canonicalParents = ResolveCanonicalParents(mapRoot);
            int movedFromTypeFolders = MoveNestedTypeFolderContents(mapRoot, canonicalParents);
            int movedFromAreaFolders = MoveAreaFolderLeftovers(mapRoot, canonicalParents);
            int deletedEmptyFolders = DeleteEmptyFolders(mapRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MapPrefabPath);
            Debug.Log(
                $"[MapPrefabHierarchyOrganizer] Reorganized {MapPrefabPath}. " +
                $"MovedFromTypeFolders={movedFromTypeFolders}, " +
                $"MovedFromAreaFolders={movedFromAreaFolders}, " +
                $"DeletedEmptyFolders={deletedEmptyFolders}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Dictionary<string, Transform> ResolveCanonicalParents(Transform mapRoot)
    {
        var canonicalParents = new Dictionary<string, Transform>(StringComparer.Ordinal);
        for (int i = 0; i < CanonicalParents.Length; i++)
        {
            string parentName = CanonicalParents[i];
            Transform existing = mapRoot.Find(parentName);
            if (existing == null)
            {
                var parent = new GameObject(parentName);
                existing = parent.transform;
                existing.SetParent(mapRoot, false);
            }

            canonicalParents[parentName] = existing;
        }

        return canonicalParents;
    }

    private static int MoveNestedTypeFolderContents(
        Transform mapRoot,
        IReadOnlyDictionary<string, Transform> canonicalParents)
    {
        List<Transform> folders = CollectTransforms(mapRoot);
        int moved = 0;

        for (int i = folders.Count - 1; i >= 0; i--)
        {
            Transform folder = folders[i];
            if (folder == null || folder == mapRoot)
                continue;

            string canonicalName = NormalizeFolderName(folder.name);
            if (!canonicalParents.TryGetValue(canonicalName, out Transform targetParent))
                continue;

            if (folder == targetParent)
                continue;

            moved += MoveChildrenPreservingWorldAndActiveState(folder, targetParent);
        }

        return moved;
    }

    private static int MoveAreaFolderLeftovers(
        Transform mapRoot,
        IReadOnlyDictionary<string, Transform> canonicalParents)
    {
        Transform leftoverParent = null;
        int moved = 0;
        string[] areaNames = { "Cities", "Islands" };

        for (int i = 0; i < areaNames.Length; i++)
        {
            Transform area = mapRoot.Find(areaNames[i]);
            if (area == null)
                continue;

            List<Transform> remaining = CollectDirectChildren(area);
            for (int childIndex = 0; childIndex < remaining.Count; childIndex++)
            {
                Transform child = remaining[childIndex];
                if (child == null)
                    continue;

                string canonicalName = NormalizeFolderName(child.name);
                Transform targetParent = canonicalParents.TryGetValue(canonicalName, out Transform canonical)
                    ? canonical
                    : leftoverParent ??= ResolveLeftoverParent(mapRoot);

                MovePreservingWorldAndActiveState(child, targetParent);
                moved++;
            }
        }

        return moved;
    }

    private static Transform ResolveLeftoverParent(Transform mapRoot)
    {
        Transform existing = mapRoot.Find(LeftoverParentName);
        if (existing != null)
            return existing;

        var parent = new GameObject(LeftoverParentName);
        Transform parentTransform = parent.transform;
        parentTransform.SetParent(mapRoot, false);
        return parentTransform;
    }

    private static int DeleteEmptyFolders(Transform mapRoot)
    {
        int deleted = 0;
        bool removed;
        do
        {
            removed = false;
            List<Transform> transforms = CollectTransforms(mapRoot);
            for (int i = transforms.Count - 1; i >= 0; i--)
            {
                Transform transform = transforms[i];
                if (transform == null || transform == mapRoot)
                    continue;

                if (transform.childCount != 0 || !IsFolderName(transform.name) || !HasOnlyTransform(transform.gameObject))
                    continue;

                UnityEngine.Object.DestroyImmediate(transform.gameObject);
                deleted++;
                removed = true;
            }
        }
        while (removed);

        return deleted;
    }

    private static bool IsFolderName(string name)
    {
        string normalized = NormalizeFolderName(name);
        if (normalized == "Cities" || normalized == "City" || normalized == "Islands" || normalized == "Island")
            return true;

        for (int i = 0; i < CanonicalParents.Length; i++)
        {
            if (CanonicalParents[i] == normalized)
                return true;
        }

        return normalized == LeftoverParentName;
    }

    private static bool HasOnlyTransform(GameObject gameObject)
    {
        Component[] components = gameObject.GetComponents<Component>();
        return components.Length == 1 && components[0] is Transform;
    }

    private static int MoveChildrenPreservingWorldAndActiveState(Transform source, Transform targetParent)
    {
        List<Transform> children = CollectDirectChildren(source);
        for (int i = 0; i < children.Count; i++)
            MovePreservingWorldAndActiveState(children[i], targetParent);

        return children.Count;
    }

    private static void MovePreservingWorldAndActiveState(Transform child, Transform targetParent)
    {
        bool wasActiveInHierarchy = child.gameObject.activeInHierarchy;
        child.SetParent(targetParent, true);
        if (!wasActiveInHierarchy && child.gameObject.activeSelf)
            child.gameObject.SetActive(false);
    }

    private static List<Transform> CollectDirectChildren(Transform parent)
    {
        var children = new List<Transform>(parent.childCount);
        for (int i = 0; i < parent.childCount; i++)
            children.Add(parent.GetChild(i));

        return children;
    }

    private static List<Transform> CollectTransforms(Transform root)
    {
        var transforms = new List<Transform>();
        CollectTransforms(root, transforms);
        return transforms;
    }

    private static void CollectTransforms(Transform root, List<Transform> transforms)
    {
        transforms.Add(root);
        for (int i = 0; i < root.childCount; i++)
            CollectTransforms(root.GetChild(i), transforms);
    }

    private static string NormalizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        int suffixIndex = name.LastIndexOf(" (", StringComparison.Ordinal);
        if (suffixIndex > 0 && name.EndsWith(")", StringComparison.Ordinal))
            return name.Substring(0, suffixIndex);

        return name;
    }
}
#endif
