#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MapVehicleHierarchyEditor
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string MapRootName = "Map";
    private const string VehiclesRootName = "Vehicles";
    private const string UnmappedGroupName = "_UnmappedVehicleSources";
    private const string PendingRequestRelativePath = "Temp/RunMapVehicleHierarchyMigration.flag";
    private const string ReportPath = "/private/tmp/map-vehicle-hierarchy-report.json";

    private static readonly Dictionary<string, string> RawSourceToGameplayGroup = new(StringComparer.Ordinal)
    {
        ["SM_Veh_APC_Heavy_01"] = "Unit_Veh_APC_Heavy",
        ["SM_Veh_Drone_01"] = "Unit_Veh_Drone",
        ["SM_Veh_Helicopter_Attack_01"] = "Unit_Veh_Helicopter_Attack",
        ["SM_Veh_Helicopter_Attack_02"] = "Unit_Veh_Helicopter_Attack_Small",
        ["SM_Veh_Helicopter_Transport_01"] = "Unit_Veh_Helicopter_Transport",
        ["SM_Veh_Jet_01"] = "Unit_Veh_Jet_01",
        ["SM_Veh_Jet_02"] = "Unit_Veh_Jet_02",
        ["SM_Veh_Light_Armored_Car_01"] = "Unit_Veh_Light_Armored_Car",
        ["SM_Veh_Radar_Tank_01"] = "Unit_Veh_Radar_Tank",
        ["SM_Veh_Rocket_Truck_01"] = "Unit_Veh_Missle_Launcher_Ground",
        ["SM_Veh_Tank_USA_01"] = "Unit_Veh_Tank_USA",
        ["SM_Veh_TransportPlane_01"] = "Unit_Veh_Plane_Transport",
        ["SM_Veh_Truck_01"] = "Unit_Veh_Truck_Tray",
        ["SM_Veh_Truck_01_Canopy"] = "Unit_Veh_Truck_Canopy",
        ["SM_Veh_Truck_01_Tanker"] = "Unit_Veh_Truck_Tanker",
        ["SM_Veh_Truck_01_Tray"] = "Unit_Veh_Truck_Tray",
    };

    [Serializable]
    private sealed class GroupReport
    {
        public string groupName;
        public int movedCount;
    }

    [Serializable]
    private sealed class MigrationReport
    {
        public int movedCount;
        public int groupCount;
        public List<GroupReport> groups = new();
        public List<string> unmappedSources = new();
        public List<string> skipped = new();
        public List<string> errors = new();
    }

    [InitializeOnLoadMethod]
    private static void RunPendingRequestIfNeeded()
    {
        EditorApplication.delayCall += () =>
        {
            string pendingRequestPath = GetProjectPath(PendingRequestRelativePath);
            if (!File.Exists(pendingRequestPath))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.Log("[MapVehicleHierarchy] pending request found, but Play Mode is active. It will run after scripts reload outside Play Mode.");
                return;
            }

            File.Delete(pendingRequestPath);
            OrganizeMatchVehicleFolders();
        };
    }

    [MenuItem("Game/Map/Organize Match Vehicle Folders")]
    public static void OrganizeMatchVehicleFolders()
    {
        MigrationReport report = new();
        try
        {
            Scene scene = SceneManager.GetSceneByPath(MatchScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);

            Transform vehiclesRoot = FindVehiclesRoot(scene);
            if (vehiclesRoot == null)
                throw new InvalidOperationException("Map/Vehicles root not found in Match scene.");

            Dictionary<string, Transform> groups = CollectExistingGroups(vehiclesRoot);
            List<Transform> sourceChildren = CollectSourceChildren(vehiclesRoot);
            Dictionary<string, int> movedByGroup = new(StringComparer.Ordinal);
            HashSet<string> unmappedSources = new(StringComparer.Ordinal);

            for (int i = 0; i < sourceChildren.Count; i++)
            {
                Transform child = sourceChildren[i];
                if (child == null)
                    continue;

                string rawSourceName = ResolveRawSourceName(child);
                string groupName = ResolveGameplayGroupName(rawSourceName);
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    report.skipped.Add(GetHierarchyPath(child));
                    continue;
                }

                if (groupName == UnmappedGroupName && !string.IsNullOrEmpty(rawSourceName))
                    unmappedSources.Add(rawSourceName);

                Transform group = GetOrCreateGroup(vehiclesRoot, groups, groupName);
                if (child.parent != group)
                    child.SetParent(group, false);

                report.movedCount++;

                if (!movedByGroup.TryAdd(groupName, 1))
                    movedByGroup[groupName]++;
            }

            DeleteEmptyFolders(vehiclesRoot);

            foreach (KeyValuePair<string, Transform> pair in groups)
            {
                if (pair.Value == null)
                    continue;

                int movedCount = movedByGroup.TryGetValue(pair.Key, out int count) ? count : 0;
                report.groups.Add(new GroupReport
                {
                    groupName = pair.Key,
                    movedCount = movedCount
                });
            }

            report.groups.Sort((a, b) => string.CompareOrdinal(a.groupName, b.groupName));
            report.unmappedSources.AddRange(unmappedSources);
            report.unmappedSources.Sort(StringComparer.Ordinal);
            report.groupCount = report.groups.Count;

            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MapVehicleHierarchy] moved {report.movedCount} vehicle authoring object(s) into {report.groupCount} group(s). Report: {ReportPath}");
        }
        catch (Exception ex)
        {
            report.errors.Add(ex.Message);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Debug.LogError($"[MapVehicleHierarchy] failed: {ex.Message}. Report: {ReportPath}");
            throw;
        }
    }

    private static Transform FindVehiclesRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform map = FindChildRecursive(roots[i].transform, MapRootName);
            Transform vehicles = map != null ? map.Find(VehiclesRootName) : null;
            if (vehicles != null)
                return vehicles;
        }

        return null;
    }

    private static Dictionary<string, Transform> CollectExistingGroups(Transform vehiclesRoot)
    {
        Dictionary<string, Transform> groups = new(StringComparer.Ordinal);
        for (int i = 0; i < vehiclesRoot.childCount; i++)
        {
            Transform child = vehiclesRoot.GetChild(i);
            if (child == null || IsPrefabSourceChild(child))
                continue;

            string groupName = child.name.Trim();
            if (!string.IsNullOrEmpty(groupName) && !groups.ContainsKey(groupName))
                groups.Add(groupName, child);
        }

        return groups;
    }

    private static List<Transform> CollectSourceChildren(Transform vehiclesRoot)
    {
        List<Transform> result = new();
        CollectSourceChildrenRecursive(vehiclesRoot, result);
        return result;
    }

    private static void CollectSourceChildrenRecursive(Transform parent, List<Transform> result)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
                continue;

            if (IsPrefabSourceChild(child))
            {
                result.Add(child);
                continue;
            }

            CollectSourceChildrenRecursive(child, result);
        }
    }

    private static bool IsPrefabSourceChild(Transform child)
    {
        return child != null && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) != null;
    }

    private static Transform GetOrCreateGroup(
        Transform vehiclesRoot,
        Dictionary<string, Transform> groups,
        string groupName)
    {
        if (groups.TryGetValue(groupName, out Transform existing) && existing != null)
            return existing;

        var groupObject = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(groupObject, "Create vehicle type group");
        Transform group = groupObject.transform;
        group.SetParent(vehiclesRoot, false);
        group.localPosition = Vector3.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;
        groups[groupName] = group;
        return group;
    }

    private static string ResolveRawSourceName(Transform child)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
        string sourceName = source != null ? source.name : child.name;
        return StripDuplicateSuffix(sourceName);
    }

    private static string ResolveGameplayGroupName(string rawSourceName)
    {
        if (string.IsNullOrWhiteSpace(rawSourceName))
            return string.Empty;

        return RawSourceToGameplayGroup.TryGetValue(rawSourceName, out string gameplayName)
            ? gameplayName
            : UnmappedGroupName;
    }

    private static void DeleteEmptyFolders(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == null || IsPrefabSourceChild(child))
                continue;

            DeleteEmptyFolders(child);
            if (child.childCount > 0)
                continue;

            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    private static string StripDuplicateSuffix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();
        int open = trimmed.LastIndexOf(" (", StringComparison.Ordinal);
        if (open < 0 || !trimmed.EndsWith(")", StringComparison.Ordinal))
            return trimmed;

        string suffix = trimmed.Substring(open + 2, trimmed.Length - open - 3);
        return int.TryParse(suffix, out _) ? trimmed.Substring(0, open) : trimmed;
    }

    private static string GetProjectPath(string relativePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? relativePath
            : Path.Combine(projectRoot, relativePath);
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        Stack<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
#endif
