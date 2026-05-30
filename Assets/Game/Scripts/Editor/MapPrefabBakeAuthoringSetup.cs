#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapPrefabBakeAuthoringSetup
{
    private const string MapPrefabPath = "Assets/Game/Prefabs/Maps/Map.prefab";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string MapRootName = "Map";
    private const string GroundHillsGroupName = "GroundHills";
    private const string GridConfigGuid = "b201000000000000000000000000000b";
    private const string MapSurfaceDataPath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";

    [MenuItem("WarlineCapture/Maps/Setup Map Bake Authoring")]
    public static void SetupMapBakeAuthoring()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            Transform mapRoot = prefabRoot.name == MapRootName
                ? prefabRoot.transform
                : prefabRoot.transform.Find(MapRootName);

            if (mapRoot == null)
                throw new InvalidOperationException($"Could not find {MapRootName} root in {MapPrefabPath}.");

            MapSurfaceAuthoring surfaceAuthoring = mapRoot.GetComponent<MapSurfaceAuthoring>();
            if (surfaceAuthoring == null)
                surfaceAuthoring = mapRoot.gameObject.AddComponent<MapSurfaceAuthoring>();

            ConfigureSurfaceAuthoring(surfaceAuthoring);

            int configured = 0;
            IReadOnlyDictionary<string, MapBakeGroupRole> roles = DefaultRoles();
            for (int i = 0; i < mapRoot.childCount; i++)
            {
                Transform child = mapRoot.GetChild(i);
                if (!roles.TryGetValue(child.name, out MapBakeGroupRole role))
                    role = MapBakeGroupRole.IgnoredDecoration;

                MapBakeGroupAuthoring group = child.GetComponent<MapBakeGroupAuthoring>();
                if (group == null)
                    group = child.gameObject.AddComponent<MapBakeGroupAuthoring>();

                ConfigureGroup(group, role);
                configured++;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MapPrefabPath);
            Debug.Log($"[MapPrefabBakeAuthoringSetup] Added bake authoring to {configured} Map folders in {MapPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void MoveGroundHillMeshesToTerrainGroup()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            Transform mapRoot = prefabRoot.name == MapRootName
                ? prefabRoot.transform
                : prefabRoot.transform.Find(MapRootName);

            if (mapRoot == null)
                throw new InvalidOperationException($"Could not find {MapRootName} root in {MapPrefabPath}.");

            Transform mountains = mapRoot.Find("Mountains");
            if (mountains == null)
                throw new InvalidOperationException($"Could not find Map/Mountains in {MapPrefabPath}.");

            Transform groundHills = mapRoot.Find(GroundHillsGroupName);
            if (groundHills == null)
            {
                var groupObject = new GameObject(GroundHillsGroupName);
                groundHills = groupObject.transform;
                groundHills.SetParent(mapRoot, false);
                groundHills.localPosition = Vector3.zero;
                groundHills.localRotation = Quaternion.identity;
                groundHills.localScale = Vector3.one;
            }

            MapBakeGroupAuthoring group = groundHills.GetComponent<MapBakeGroupAuthoring>();
            if (group == null)
                group = groundHills.gameObject.AddComponent<MapBakeGroupAuthoring>();

            ConfigureGroup(group, MapBakeGroupRole.Terrain);

            List<Transform> groundHillChildren = new();
            CollectGroundHillChildren(mountains, groundHillChildren);
            for (int i = 0; i < groundHillChildren.Count; i++)
                groundHillChildren[i].SetParent(groundHills, true);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MapPrefabPath);
            Debug.Log($"[MapPrefabBakeAuthoringSetup] Moved {groundHillChildren.Count} ground-hill meshes from Map/Mountains to Map/{GroundHillsGroupName}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void ConfigureMatchSceneWaterGroundAsTerrain()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        try
        {
            Transform mapRoot = FindMapRoot(scene);
            Transform props = mapRoot.Find("Props");
            if (props == null)
                throw new InvalidOperationException($"Could not find Map/Props in {MatchScenePath}.");

            int configured = 0;
            Transform[] children = props.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (!child.name.Equals("Water_Ground", StringComparison.Ordinal))
                    continue;

                MapBakeGroupAuthoring group = child.GetComponent<MapBakeGroupAuthoring>();
                if (group == null)
                    group = child.gameObject.AddComponent<MapBakeGroupAuthoring>();

                ConfigureGroup(group, MapBakeGroupRole.Terrain);
                configured++;
            }

            if (configured == 0)
                throw new InvalidOperationException($"No Map/Props/Water_Ground objects found in {MatchScenePath}.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MapPrefabBakeAuthoringSetup] Configured {configured} Match scene Water_Ground objects as terrain surface sources.");
        }
        finally
        {
            if (previousScene.IsValid() &&
                !string.IsNullOrEmpty(previousScene.path) &&
                previousScene.path != scene.path)
            {
                EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
            }
        }
    }

    public static void AuditMapMilitaryCategory()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            Transform mapRoot = prefabRoot.name == MapRootName
                ? prefabRoot.transform
                : prefabRoot.transform.Find(MapRootName);

            if (mapRoot == null)
                throw new InvalidOperationException($"Could not find {MapRootName} root in {MapPrefabPath}.");

            Transform military = mapRoot.Find("Military");
            if (military == null)
            {
                Debug.Log("[MapPrefabBakeAuthoringSetup] Map/Military does not exist.");
                return;
            }

            for (int i = 0; i < military.childCount; i++)
            {
                Transform child = military.GetChild(i);
                Debug.Log($"[MapMilitaryAudit] child={child.name} children={child.childCount} meshes={child.GetComponentsInChildren<MeshFilter>(true).Length}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void FlattenMapMilitaryCategory()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            Transform mapRoot = prefabRoot.name == MapRootName
                ? prefabRoot.transform
                : prefabRoot.transform.Find(MapRootName);

            if (mapRoot == null)
                throw new InvalidOperationException($"Could not find {MapRootName} root in {MapPrefabPath}.");

            Transform military = mapRoot.Find("Military");
            if (military == null)
            {
                Debug.Log("[MapPrefabBakeAuthoringSetup] Map/Military already removed.");
                return;
            }

            int moved = 0;
            while (military.childCount > 0)
            {
                Transform child = military.GetChild(0);
                Transform destination = ResolveMilitaryChildDestination(mapRoot, child);
                child.SetParent(destination, true);
                moved++;
            }

            UnityEngine.Object.DestroyImmediate(military.gameObject);
            SetupMapBakeAuthoringOnLoadedPrefab(mapRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MapPrefabPath);
            Debug.Log($"[MapPrefabBakeAuthoringSetup] Removed Map/Military and moved {moved} children to existing map categories.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static IReadOnlyDictionary<string, MapBakeGroupRole> DefaultRoles()
    {
        return new Dictionary<string, MapBakeGroupRole>(StringComparer.Ordinal)
        {
            { "Ground", MapBakeGroupRole.Terrain },
            { "GroundHills", MapBakeGroupRole.Terrain },
            { "Grass", MapBakeGroupRole.Terrain },
            { "Beaches", MapBakeGroupRole.Terrain },
            { "Concrete", MapBakeGroupRole.Terrain },
            { "Docks", MapBakeGroupRole.Terrain },
            { "Roads", MapBakeGroupRole.Road },
            { "Runways", MapBakeGroupRole.Road },
            { "Bridges", MapBakeGroupRole.Bridge },
            { "Rocks", MapBakeGroupRole.Blocker },
            { "Mountains", MapBakeGroupRole.Blocker },
            { "Buildings", MapBakeGroupRole.Blocker },
            { "Bushes", MapBakeGroupRole.IgnoredDecoration },
            { "Trees", MapBakeGroupRole.IgnoredDecoration },
            { "Plants", MapBakeGroupRole.IgnoredDecoration },
            { "Clouds", MapBakeGroupRole.IgnoredDecoration },
            { "FX", MapBakeGroupRole.IgnoredDecoration },
            { "Items", MapBakeGroupRole.IgnoredDecoration },
            { "Skydome", MapBakeGroupRole.IgnoredDecoration },
            { "Lights", MapBakeGroupRole.IgnoredDecoration },
            { "Props", MapBakeGroupRole.IgnoredDecoration },
            { "Vehicles", MapBakeGroupRole.IgnoredDecoration },
            { "Characters", MapBakeGroupRole.IgnoredDecoration },
            { "Weapons", MapBakeGroupRole.IgnoredDecoration },
            { "ResourceAreas", MapBakeGroupRole.IgnoredDecoration },
            { "Ruins", MapBakeGroupRole.IgnoredDecoration }
        };
    }

    private static void SetupMapBakeAuthoringOnLoadedPrefab(Transform mapRoot)
    {
        MapSurfaceAuthoring surfaceAuthoring = mapRoot.GetComponent<MapSurfaceAuthoring>();
        if (surfaceAuthoring == null)
            surfaceAuthoring = mapRoot.gameObject.AddComponent<MapSurfaceAuthoring>();

        ConfigureSurfaceAuthoring(surfaceAuthoring);

        IReadOnlyDictionary<string, MapBakeGroupRole> roles = DefaultRoles();
        for (int i = 0; i < mapRoot.childCount; i++)
        {
            Transform child = mapRoot.GetChild(i);
            if (!roles.TryGetValue(child.name, out MapBakeGroupRole role))
                role = MapBakeGroupRole.IgnoredDecoration;

            MapBakeGroupAuthoring group = child.GetComponent<MapBakeGroupAuthoring>();
            if (group == null)
                group = child.gameObject.AddComponent<MapBakeGroupAuthoring>();

            ConfigureGroup(group, role);
        }
    }

    private static Transform ResolveMilitaryChildDestination(Transform mapRoot, Transform child)
    {
        string destinationName = ResolveMilitaryChildDestinationName(child.name);
        Transform destination = mapRoot.Find(destinationName);
        if (destination == null)
        {
            var destinationObject = new GameObject(destinationName);
            destination = destinationObject.transform;
            destination.SetParent(mapRoot, false);
            destination.localPosition = Vector3.zero;
            destination.localRotation = Quaternion.identity;
            destination.localScale = Vector3.one;
        }

        return destination;
    }

    private static string ResolveMilitaryChildDestinationName(string childName)
    {
        if (childName.StartsWith("SM_Env_Ground", StringComparison.OrdinalIgnoreCase))
            return "Ground";
        if (childName.IndexOf("DirtRoad", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Road_", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("_Road_", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Roads";
        }
        if (childName.IndexOf("Runway", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Runways";
        if (childName.IndexOf("Bridge", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Bridges";
        if (childName.IndexOf("Rock", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Rocks";
        if (childName.IndexOf("Mountain", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Mountains";
        if (childName.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Bld", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Tent", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Tower", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Hangar", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Barrack", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Container", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Sandbag", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Barrier", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Gate", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Buildings";
        }
        if (childName.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Veh", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Tank", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Truck", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Helicopter", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Plane", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Jet", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("APC", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Vehicles";
        }
        if (childName.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Soldier", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Characters";
        }
        if (childName.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Missile", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Ammo", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Weapons";
        }
        if (childName.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Palm", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Trees";
        }
        if (childName.IndexOf("Bush", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Bushes";
        if (childName.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
            childName.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Plants";
        }
        if (childName.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Lights";

        return "Props";
    }

    private static void CollectGroundHillChildren(Transform root, List<Transform> results)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("SM_Env_Ground_Hill", StringComparison.Ordinal))
            {
                results.Add(child);
                continue;
            }

            CollectGroundHillChildren(child, results);
        }
    }

    private static Transform FindMapRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform root = roots[i].transform;
            if (root.name == MapRootName)
                return root;

            Transform child = root.Find(MapRootName);
            if (child != null)
                return child;

            MapSurfaceAuthoring authoring = root.GetComponentInChildren<MapSurfaceAuthoring>(true);
            if (authoring != null)
                return authoring.transform;
        }

        throw new InvalidOperationException($"Could not find {MapRootName} root in {scene.path}.");
    }

    private static void ConfigureGroup(MapBakeGroupAuthoring group, MapBakeGroupRole role)
    {
        SerializedObject serialized = new(group);
        serialized.FindProperty("role").enumValueIndex = (int)role;
        serialized.FindProperty("layerId").intValue = role == MapBakeGroupRole.Bridge ? 1 : 0;
        serialized.FindProperty("movementMask").intValue = ResolveMovementMask(role);
        serialized.FindProperty("includeInactiveChildren").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(group);
    }

    private static void ConfigureSurfaceAuthoring(MapSurfaceAuthoring authoring)
    {
        GridAuthoringConfig gridConfig = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(AssetDatabase.GUIDToAssetPath(GridConfigGuid));
        if (gridConfig == null)
            throw new InvalidOperationException($"Could not load GridAuthoringConfig from GUID {GridConfigGuid}.");

        MapSurfaceDataAsset surfaceData = ResolveOrCreateSurfaceData();
        SerializedObject serialized = new(authoring);
        serialized.FindProperty("bakedSurfaceData").objectReferenceValue = surfaceData;
        serialized.FindProperty("gridConfig").objectReferenceValue = gridConfig;
        serialized.FindProperty("gridOrigin").vector3Value = Vector3.zero;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(authoring);
    }

    private static MapSurfaceDataAsset ResolveOrCreateSurfaceData()
    {
        MapSurfaceDataAsset surfaceData = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(MapSurfaceDataPath);
        if (surfaceData != null)
            return surfaceData;

        Directory.CreateDirectory(Path.GetDirectoryName(MapSurfaceDataPath));
        surfaceData = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        AssetDatabase.CreateAsset(surfaceData, MapSurfaceDataPath);
        EditorUtility.SetDirty(surfaceData);
        AssetDatabase.SaveAssets();
        return surfaceData;
    }

    private static int ResolveMovementMask(MapBakeGroupRole role)
    {
        return role switch
        {
            MapBakeGroupRole.Terrain => (int)(MapSurfaceMovementMask.AllGroundUnits |
                                              MapSurfaceMovementMask.AirGrounded |
                                              MapSurfaceMovementMask.BuildingPlacement),
            MapBakeGroupRole.Road => (int)(MapSurfaceMovementMask.AllGroundUnits |
                                           MapSurfaceMovementMask.AirGrounded),
            MapBakeGroupRole.Bridge => (int)MapSurfaceMovementMask.AllGroundUnits,
            MapBakeGroupRole.Ramp => (int)MapSurfaceMovementMask.AllGroundUnits,
            _ => (int)MapSurfaceMovementMask.None
        };
    }
}
#endif
