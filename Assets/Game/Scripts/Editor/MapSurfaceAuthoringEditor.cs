#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MapSurfaceAuthoring))]
public sealed class MapSurfaceAuthoringEditor : Editor
{
    private const string DefaultAssetDirectory = "Assets/Game/Data/MapSurfaces";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Map Surface Bake", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bakes from explicit MapSurfaceAuthoring roots into a MapSurfaceDataAsset. " +
            "This uses the current single-layer mesh bake path; bridge/highway multi-layer authoring can be added behind this same button.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Bake Map Surface Data", GUILayout.Height(30f)))
                BakeSelectedAuthoring((MapSurfaceAuthoring)target);
        }
    }

    private void BakeSelectedAuthoring(MapSurfaceAuthoring authoring)
    {
        if (authoring == null)
            return;

        if (authoring.GridAuthoring == null)
        {
            EditorUtility.DisplayDialog(
                "Map Surface Bake",
                "Assign Grid Authoring before baking map surface data.",
                "OK");
            return;
        }

        MapSurfaceDataAsset asset = ResolveOrCreateDataAsset(authoring);
        if (asset == null)
            return;

        MapSurfaceBakeRequest request = CreateBakeRequest(authoring.GridAuthoring);
        List<MapSurfaceMeshBakeSource> sources = new();
        AddMeshSources(authoring.TerrainRoot, sources, MapSurfaceType.Terrain, MapSurfaceFlags.None, MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded | MapSurfaceMovementMask.BuildingPlacement, 0);
        AddMeshSources(authoring.RoadRoot, sources, MapSurfaceType.Road, MapSurfaceFlags.Road, MapSurfaceMovementMask.AllGroundUnits | MapSurfaceMovementMask.AirGrounded, 0);
        AddMeshSources(authoring.BridgeRoot, sources, MapSurfaceType.BridgeDeck, MapSurfaceFlags.Road | MapSurfaceFlags.Bridge, MapSurfaceMovementMask.AllGroundUnits, 1);
        AddMeshSources(authoring.RampRoot, sources, MapSurfaceType.Ramp, MapSurfaceFlags.Road | MapSurfaceFlags.Ramp, MapSurfaceMovementMask.AllGroundUnits, 0);

        var bakeSystem = new MapSurfaceBakeSystem();
        bool baked;
        BlobAssetReference<MapSurfaceBlob> surfaceBlob;
        if (sources.Count > 0)
        {
            baked = bakeSystem.TryBuildSingleLayerTerrain(
                request,
                sources.ToArray(),
                Allocator.Persistent,
                out surfaceBlob);
        }
        else
        {
            baked = bakeSystem.TryBuildFlatEquivalent(
                request,
                Allocator.Persistent,
                out surfaceBlob);
        }

        if (!baked || !surfaceBlob.IsCreated)
        {
            EditorUtility.DisplayDialog(
                "Map Surface Bake",
                "Bake failed. Check grid dimensions, cell size, and source meshes.",
                "OK");
            return;
        }

        Undo.RecordObject(asset, "Bake Map Surface Data");
        asset.ConfigureBakedSurface(
            new Vector3(request.GridOrigin.x, request.GridOrigin.y, request.GridOrigin.z),
            request.CellSize,
            new Vector2Int(request.Dimensions.x, request.Dimensions.y),
            surfaceBlob,
            sources.Count == 0);
        surfaceBlob.Dispose();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MapSurfaceBake] Baked {asset.SurfaceCount} surfaces, {asset.ConnectionCount} connections to {AssetDatabase.GetAssetPath(asset)}");
    }

    private MapSurfaceDataAsset ResolveOrCreateDataAsset(MapSurfaceAuthoring authoring)
    {
        SerializedProperty dataProperty = serializedObject.FindProperty("bakedSurfaceData");
        MapSurfaceDataAsset asset = dataProperty.objectReferenceValue as MapSurfaceDataAsset;
        if (asset != null)
            return asset;

        Directory.CreateDirectory(DefaultAssetDirectory);
        string sceneName = SceneManager.GetActiveScene().isLoaded
            ? SceneManager.GetActiveScene().name
            : "Map";
        string safeName = string.IsNullOrWhiteSpace(authoring.name) ? "Map" : authoring.name.Replace(' ', '_');
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultAssetDirectory}/{sceneName}_{safeName}_MapSurfaceData.asset");
        asset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        AssetDatabase.CreateAsset(asset, path);

        serializedObject.Update();
        dataProperty.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(authoring);
        return asset;
    }

    private static MapSurfaceBakeRequest CreateBakeRequest(GridAuthoring grid)
    {
        return new MapSurfaceBakeRequest(
            (float3)grid.transform.position,
            grid.CellSize,
            new int2(grid.Width, grid.Height));
    }

    private static void AddMeshSources(
        Transform root,
        List<MapSurfaceMeshBakeSource> sources,
        MapSurfaceType surfaceType,
        MapSurfaceFlags flags,
        MapSurfaceMovementMask movementMask,
        int layerId)
    {
        if (root == null)
            return;

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            sources.Add(new MapSurfaceMeshBakeSource(
                filter.sharedMesh,
                filter.transform.localToWorldMatrix,
                surfaceType,
                flags,
                movementMask,
                layerId));
        }
    }
}
#endif
