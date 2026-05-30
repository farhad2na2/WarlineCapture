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
    private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode = MapSurfaceEditorOverlaySystem.OverlayMode.Walkable;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Map Surface Bake", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bakes from MapBakeGroupAuthoring classifications under this MapSurfaceAuthoring into a MapSurfaceDataAsset. " +
            "This uses the current single-layer mesh bake path; bridge/highway multi-layer authoring can be added behind this same button.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            previewMode = (MapSurfaceEditorOverlaySystem.OverlayMode)EditorGUILayout.EnumPopup("Preview Mode", previewMode);
            if (GUILayout.Button("Preview Authoring In Scene View (No Bake)", GUILayout.Height(30f)))
                PreviewSelectedAuthoring((MapSurfaceAuthoring)target);
            if (MapSurfacePreviewOverlaySystem.HasPreview && GUILayout.Button("Clear Bake Preview", GUILayout.Height(24f)))
                MapSurfacePreviewOverlaySystem.ClearPreview();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Bake Map Surface Data", GUILayout.Height(30f)))
                BakeSelectedAuthoring((MapSurfaceAuthoring)target);
        }
    }

    private void PreviewSelectedAuthoring(MapSurfaceAuthoring authoring)
    {
        MapSurfacePreviewOverlaySystem.ShowAuthoringPreview(authoring, previewMode);
    }

    private void BakeSelectedAuthoring(MapSurfaceAuthoring authoring)
    {
        MapSurfaceDataAsset asset = ResolveOrCreateDataAsset(authoring);
        if (asset == null)
            return;

        if (!TryBuildSurfaceBlob(
                authoring,
                out MapSurfaceBakeRequest request,
                out BlobAssetReference<MapSurfaceBlob> surfaceBlob,
                out int sourceCount))
            return;

        MapSurfaceDataAsset previewAsset = ScriptableObject.CreateInstance<MapSurfaceDataAsset>();
        previewAsset.ConfigureBakedSurface(
            new Vector3(request.GridOrigin.x, request.GridOrigin.y, request.GridOrigin.z),
            request.CellSize,
            new Vector2Int(request.Dimensions.x, request.Dimensions.y),
            surfaceBlob,
            sourceCount == 0);

        if (previewAsset.CompressedPayloadBytes > MapSurfaceDataAsset.GitFriendlyPayloadByteLimit)
        {
            int payloadMb = Mathf.CeilToInt(previewAsset.CompressedPayloadBytes / (1024f * 1024f));
            int limitMb = Mathf.CeilToInt(MapSurfaceDataAsset.GitFriendlyPayloadByteLimit / (1024f * 1024f));
            UnityEngine.Object.DestroyImmediate(previewAsset);
            surfaceBlob.Dispose();
            EditorUtility.DisplayDialog(
                "Map Surface Bake",
                $"Bake produced a compact payload of {payloadMb} MB, above the {limitMb} MB Git-friendly limit. " +
                "Use a lower-resolution bake, chunked surface assets, or a sparse authoring pass before saving.",
                "OK");
            return;
        }

        Undo.RecordObject(asset, "Bake Map Surface Data");
        asset.ConfigureBakedSurface(
            new Vector3(request.GridOrigin.x, request.GridOrigin.y, request.GridOrigin.z),
            request.CellSize,
            new Vector2Int(request.Dimensions.x, request.Dimensions.y),
            surfaceBlob,
            sourceCount == 0);
        UnityEngine.Object.DestroyImmediate(previewAsset);
        surfaceBlob.Dispose();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[MapSurfaceBake] Baked {asset.SurfaceCount} surfaces, {asset.ConnectionCount} connections " +
            $"to {AssetDatabase.GetAssetPath(asset)} compactBytes={asset.CompressedPayloadBytes} uncompressedBytes={asset.UncompressedPayloadBytes}");
    }

    private static bool TryBuildSurfaceBlob(
        MapSurfaceAuthoring authoring,
        out MapSurfaceBakeRequest request,
        out BlobAssetReference<MapSurfaceBlob> surfaceBlob,
        out int sourceCount)
    {
        request = default;
        surfaceBlob = default;
        sourceCount = 0;

        if (authoring == null)
            return false;

        if (authoring.GridConfig == null)
        {
            EditorUtility.DisplayDialog(
                "Map Surface Bake",
                "Assign Grid Config before baking map surface data.",
                "OK");
            return false;
        }

        request = CreateBakeRequest(authoring);
        List<MapSurfaceMeshBakeSource> sources = new();
        AddAuthoringGroupSources(authoring.transform, sources);
        sourceCount = sources.Count;

        var bakeSystem = new MapSurfaceBakeSystem();
        bool baked = sourceCount > 0
            ? bakeSystem.TryBuildSingleLayerTerrain(request, sources.ToArray(), Allocator.Persistent, out surfaceBlob)
            : bakeSystem.TryBuildFlatEquivalent(request, Allocator.Persistent, out surfaceBlob);

        if (baked && surfaceBlob.IsCreated)
            return true;

        EditorUtility.DisplayDialog(
            "Map Surface Bake",
            "Bake failed. Check grid dimensions, cell size, and source meshes.",
            "OK");
        return false;
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

    private static MapSurfaceBakeRequest CreateBakeRequest(MapSurfaceAuthoring authoring)
    {
        GridAuthoringConfig grid = authoring.GridConfig;
        return new MapSurfaceBakeRequest(
            (float3)grid.Origin,
            grid.CellSize,
            new int2(grid.Width, grid.Height));
    }


    private static void AddAuthoringGroupSources(Transform root, List<MapSurfaceMeshBakeSource> sources)
    {
        if (root == null)
            return;

        MapBakeGroupAuthoring[] groups = root.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            MapBakeGroupAuthoring group = groups[i];
            if (group == null)
                continue;

            if (!TryResolveSurfaceSettings(group, out MapSurfaceType type, out MapSurfaceFlags flags, out MapSurfaceMovementMask movementMask, out int layerId))
                continue;

            AddMeshSources(
                group,
                sources,
                type,
                flags,
                movementMask,
                layerId,
                group.IncludeInactiveChildren);
        }
    }

    private static bool TryResolveSurfaceSettings(
        MapBakeGroupAuthoring group,
        out MapSurfaceType type,
        out MapSurfaceFlags flags,
        out MapSurfaceMovementMask movementMask,
        out int layerId)
    {
        type = MapSurfaceType.Terrain;
        flags = MapSurfaceFlags.None;
        movementMask = group.MovementMask;
        layerId = group.LayerId;

        switch (group.Role)
        {
            case MapBakeGroupRole.Terrain:
                type = MapSurfaceType.Terrain;
                movementMask = ResolveMovementMaskOrDefault(
                    movementMask,
                    MapSurfaceMovementMask.AllGroundUnits |
                    MapSurfaceMovementMask.AirGrounded |
                    MapSurfaceMovementMask.BuildingPlacement);
                return true;
            case MapBakeGroupRole.Road:
                type = MapSurfaceType.Road;
                flags = MapSurfaceFlags.Road;
                movementMask = ResolveMovementMaskOrDefault(
                    movementMask,
                    MapSurfaceMovementMask.AllGroundUnits |
                    MapSurfaceMovementMask.AirGrounded);
                return true;
            case MapBakeGroupRole.Bridge:
                type = MapSurfaceType.BridgeDeck;
                flags = MapSurfaceFlags.Road | MapSurfaceFlags.Bridge;
                layerId = Mathf.Max(1, layerId);
                movementMask = ResolveMovementMaskOrDefault(movementMask, MapSurfaceMovementMask.AllGroundUnits);
                return true;
            case MapBakeGroupRole.Ramp:
                type = MapSurfaceType.Ramp;
                flags = MapSurfaceFlags.Road | MapSurfaceFlags.Ramp;
                movementMask = ResolveMovementMaskOrDefault(movementMask, MapSurfaceMovementMask.AllGroundUnits);
                return true;
            case MapBakeGroupRole.Blocker:
            case MapBakeGroupRole.IgnoredDecoration:
            default:
                movementMask = MapSurfaceMovementMask.None;
                return false;
        }
    }

    private static MapSurfaceMovementMask ResolveMovementMaskOrDefault(
        MapSurfaceMovementMask authoredMask,
        MapSurfaceMovementMask defaultMask)
    {
        return authoredMask == MapSurfaceMovementMask.None
            ? defaultMask
            : authoredMask;
    }

    private static void AddMeshSources(
        MapBakeGroupAuthoring ownerGroup,
        List<MapSurfaceMeshBakeSource> sources,
        MapSurfaceType surfaceType,
        MapSurfaceFlags flags,
        MapSurfaceMovementMask movementMask,
        int layerId,
        bool includeInactiveChildren)
    {
        if (ownerGroup == null)
            return;

        MeshFilter[] filters = ownerGroup.GetComponentsInChildren<MeshFilter>(includeInactiveChildren);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            if (!IsOwnedByGroup(filter, ownerGroup))
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

    private static bool IsOwnedByGroup(MeshFilter filter, MapBakeGroupAuthoring ownerGroup)
    {
        if (filter == null || ownerGroup == null)
            return false;

        MapBakeGroupAuthoring nearestGroup = filter.GetComponentInParent<MapBakeGroupAuthoring>(true);
        return nearestGroup == ownerGroup;
    }
}
#endif
