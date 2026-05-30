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
            { "Military", MapBakeGroupRole.IgnoredDecoration },
            { "Ruins", MapBakeGroupRole.IgnoredDecoration }
        };
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
