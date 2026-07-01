#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MapSurfaceAuthoring))]
public sealed class MapSurfaceAuthoringEditor : Editor
{
    private const string DefaultAssetDirectory = "Assets/Game/Data/MapSurfaces";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode = MapSurfaceEditorOverlaySystem.OverlayMode.Walkable;

    [MenuItem("Game/Map Surface/Bake Active Scene Surface Data")]
    public static void BakeActiveSceneSurfaceData()
    {
        if (!TryFindActiveSceneAuthoring(out MapSurfaceAuthoring authoring))
            throw new MissingReferenceException("No MapSurfaceAuthoring found in the active scene.");

        if (authoring.BakedSurfaceData == null)
            throw new MissingReferenceException($"MapSurfaceAuthoring '{authoring.name}' has no baked surface data asset assigned.");

        BakeAuthoringToAsset(authoring, authoring.BakedSurfaceData, allowOversizeDialog: false);
    }

    [MenuItem("Game/Map Surface/Bake Match Scene Surface Data")]
    public static void BakeMatchSceneSurfaceData()
    {
        if (!File.Exists(MatchScenePath))
            throw new FileNotFoundException($"Match scene not found at {MatchScenePath}.");

        EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        BakeActiveSceneSurfaceData();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Game/Map Surface/Preview Vehicle 3x3 Footprint Around Selection")]
    public static void PreviewVehicle3x3FootprintAroundSelection()
    {
        if (!TryFindActiveSceneAuthoring(out MapSurfaceAuthoring authoring))
            throw new MissingReferenceException("No MapSurfaceAuthoring found in the active scene.");

        MapSurfacePreviewOverlaySystem.ShowAuthoringPreview(
            authoring,
            MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint);
    }

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

        BakeAuthoringToAsset(authoring, asset, allowOversizeDialog: true);
    }

    private static void BakeAuthoringToAsset(MapSurfaceAuthoring authoring, MapSurfaceDataAsset asset, bool allowOversizeDialog)
    {
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
            string message =
                $"Bake produced a compact payload of {payloadMb} MB, above the {limitMb} MB Git-friendly limit. " +
                "Use a lower-resolution bake, chunked surface assets, or a sparse authoring pass before saving.";
            if (allowOversizeDialog)
            {
                EditorUtility.DisplayDialog("Map Surface Bake", message, "OK");
            }
            else
            {
                Debug.LogWarning($"[MapSurfaceBake] {message}");
            }
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

    private static bool TryFindActiveSceneAuthoring(out MapSurfaceAuthoring authoring)
    {
        authoring = null;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            authoring = roots[i].GetComponentInChildren<MapSurfaceAuthoring>(true);
            if (authoring != null)
                return true;
        }

        return false;
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
        bool baked;
        bool[] cancelled = { false };
        bool showProgress = !Application.isBatchMode;
        int lastLoggedBatchRow = -1;
        System.Func<int, int, bool> shouldCancel = showProgress
            ? (completedRows, totalRows) => ShouldCancelBakeProgress(completedRows, totalRows, cancelled)
            : (completedRows, totalRows) => LogBatchBakeProgress(completedRows, totalRows, ref lastLoggedBatchRow);
        try
        {
            baked = sourceCount > 0
                ? bakeSystem.TryBuildSingleLayerTerrain(
                    request,
                    sources.ToArray(),
                    Allocator.Persistent,
                    out surfaceBlob,
                    shouldCancel)
                : bakeSystem.TryBuildFlatEquivalent(request, Allocator.Persistent, out surfaceBlob);
        }
        finally
        {
            if (showProgress)
                EditorUtility.ClearProgressBar();
        }

        if (baked && surfaceBlob.IsCreated)
            return true;
        if (cancelled[0])
            return false;

        const string message = "Bake failed. Check grid dimensions, cell size, and source meshes.";
        if (Application.isBatchMode)
            Debug.LogError($"[MapSurfaceBake] {message}");
        else
            EditorUtility.DisplayDialog(
                "Map Surface Bake",
                message,
                "OK");
        return false;
    }

    private static bool ShouldCancelBakeProgress(int completedRows, int totalRows, bool[] cancelled)
    {
        float progress = totalRows > 0 ? completedRows / (float)totalRows : 0f;
        bool isCancelled = EditorUtility.DisplayCancelableProgressBar(
            "Map Surface Bake",
            $"Sampling configured grid rows {completedRows}/{totalRows}",
            progress);
        if (cancelled != null && cancelled.Length > 0)
            cancelled[0] = isCancelled;
        return isCancelled;
    }

    private static bool LogBatchBakeProgress(int completedRows, int totalRows, ref int lastLoggedRow)
    {
        if (completedRows == 0 || completedRows - lastLoggedRow >= 64)
        {
            Debug.Log($"[MapSurfaceBake] progress rows={completedRows}/{totalRows}");
            lastLoggedRow = completedRows;
        }

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
            new int2(grid.Width, grid.Height),
            authoring.SamplesPerCellAxis,
            authoring.MaxSampleHeightDelta);
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
                type = MapSurfaceType.Blocked;
                flags = MapSurfaceFlags.None;
                movementMask = MapSurfaceMovementMask.None;
                return true;
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
