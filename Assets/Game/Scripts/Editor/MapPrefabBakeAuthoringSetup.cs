#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapPrefabBakeAuthoringSetup
{
    private const string MapPrefabPath = "Assets/Game/Prefabs/Maps/Map.prefab";
    private const string MapRootName = "Map";

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

            if (mapRoot.GetComponent<MapSurfaceAuthoring>() == null)
                mapRoot.gameObject.AddComponent<MapSurfaceAuthoring>();

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

    private static IReadOnlyDictionary<string, MapBakeGroupRole> DefaultRoles()
    {
        return new Dictionary<string, MapBakeGroupRole>(StringComparer.Ordinal)
        {
            { "Ground", MapBakeGroupRole.Terrain },
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
