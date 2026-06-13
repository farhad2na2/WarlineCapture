#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class MapSurfacePreviewOverlaySystem
{
    private const int MaxWalkablePreviewMeshes = 4096;
    private const int MaxBlockerPreviewMeshes = 20000;
    private const float PreviewSurfaceLift = 0.08f;

    private static PreviewMeshItem[] previewMeshes = new PreviewMeshItem[0];
    private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode;
    private static string previewLabel = string.Empty;
    private static float minHeight;
    private static float maxHeight;
    private static Material previewMaterial;
    private static Mesh gridPreviewMesh;

    public static bool HasPreview => previewMeshes.Length > 0;

    static MapSurfacePreviewOverlaySystem()
    {
        SceneView.duringSceneGui += DrawPreview;
        EditorApplication.projectChanged += ClearPreview;
        EditorApplication.hierarchyChanged += ClearPreview;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
        EditorApplication.quitting += ClearPreview;
    }

    public static void ShowAuthoringPreview(
        MapSurfaceAuthoring authoring,
        MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        ClearPreview();
        if (authoring == null)
            return;

        if (!TryGetGridBounds(authoring, out _))
        {
            previewLabel = $"{authoring.name}: assign GridAuthoringConfig before previewing map surface data";
            SceneView.RepaintAll();
            return;
        }

        previewMode = mode;
        previewMeshes = BuildPreviewMeshes(authoring, mode);
        CalculateHeightRange(previewMeshes, out minHeight, out maxHeight);
        previewLabel = BuildPreviewLabel(authoring.name, previewMeshes);
        SceneView.RepaintAll();
    }

    public static PreviewMeshItem[] BuildPreviewMeshes(
        MapSurfaceAuthoring authoring,
        MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        if (authoring == null)
            return new PreviewMeshItem[0];

        if (mode == MapSurfaceEditorOverlaySystem.OverlayMode.Walkable ||
            mode == MapSurfaceEditorOverlaySystem.OverlayMode.Blocked)
            return BuildWalkableBlockedPreviewMeshes(authoring);

        TryGetGridBounds(authoring, out Bounds gridBounds);
        var items = new List<PreviewMeshItem>(256);
        MapBakeGroupAuthoring[] groups = authoring.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
        for (int i = 0; i < groups.Length && items.Count < MaxWalkablePreviewMeshes; i++)
        {
            MapBakeGroupAuthoring group = groups[i];
            if (group == null || !ShouldPreviewRole(group.Role, mode))
                continue;

            MeshFilter[] filters = group.GetComponentsInChildren<MeshFilter>(group.IncludeInactiveChildren);
            for (int filterIndex = 0; filterIndex < filters.Length && items.Count < MaxWalkablePreviewMeshes; filterIndex++)
                AddMeshItem(filters[filterIndex], group, group.Role, items, MaxWalkablePreviewMeshes, gridBounds);
        }

        return items.ToArray();
    }

    private static PreviewMeshItem[] BuildWalkableBlockedPreviewMeshes(MapSurfaceAuthoring authoring)
    {
        var walkableSurfaces = new List<PreviewMeshItem>(256);
        var roads = new List<PreviewMeshItem>(256);
        var blockers = new List<PreviewMeshItem>(256);
        if (!TryGetGridBounds(authoring, out Bounds gridBounds))
            return new PreviewMeshItem[0];

        MapBakeGroupAuthoring[] groups = authoring.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            MapBakeGroupAuthoring group = groups[i];
            if (group == null || !ShouldPreviewRole(group.Role, MapSurfaceEditorOverlaySystem.OverlayMode.Blocked))
                continue;
            if (IsUnderMapVehicles(group.transform))
                continue;

            List<PreviewMeshItem> target = ResolveWalkableBlockedTarget(group.Role, walkableSurfaces, roads, blockers);
            int limit = group.Role == MapBakeGroupRole.Blocker ? MaxBlockerPreviewMeshes : MaxWalkablePreviewMeshes;
            if (target.Count >= limit)
                continue;

            MeshFilter[] filters = group.GetComponentsInChildren<MeshFilter>(group.IncludeInactiveChildren);
            for (int filterIndex = 0; filterIndex < filters.Length && target.Count < limit; filterIndex++)
                AddMeshItem(filters[filterIndex], group, group.Role, target, limit, gridBounds);
        }

        var combined = new List<PreviewMeshItem>((walkableSurfaces.Count == 0 ? 1 : walkableSurfaces.Count) + blockers.Count + roads.Count);
        if (walkableSurfaces.Count == 0)
            combined.Add(BuildGridPreviewItem(authoring, gridBounds));
        else
            combined.AddRange(walkableSurfaces);
        combined.AddRange(blockers);
        combined.AddRange(roads);
        return combined.ToArray();
    }

    private static List<PreviewMeshItem> ResolveWalkableBlockedTarget(
        MapBakeGroupRole role,
        List<PreviewMeshItem> walkableSurfaces,
        List<PreviewMeshItem> roads,
        List<PreviewMeshItem> blockers)
    {
        switch (role)
        {
            case MapBakeGroupRole.Road:
            case MapBakeGroupRole.Bridge:
            case MapBakeGroupRole.Ramp:
                return roads;
            case MapBakeGroupRole.Blocker:
                return blockers;
            case MapBakeGroupRole.Terrain:
            default:
                return walkableSurfaces;
        }
    }

    private static bool TryGetGridBounds(MapSurfaceAuthoring authoring, out Bounds bounds)
    {
        bounds = default;
        GridAuthoringConfig grid = authoring != null ? authoring.GridConfig : null;
        if (grid == null || grid.Width <= 0 || grid.Height <= 0 || grid.CellSize <= 0f)
            return false;

        Vector3 size = new(grid.Width * grid.CellSize, 0.01f, grid.Height * grid.CellSize);
        Vector3 center = grid.Origin + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        bounds = new Bounds(center, size);
        return true;
    }

    private static PreviewMeshItem BuildGridPreviewItem(MapSurfaceAuthoring authoring, Bounds gridBounds)
    {
        GridAuthoringConfig grid = authoring.GridConfig;
        Matrix4x4 localToWorld =
            Matrix4x4.Translate(grid.Origin) *
            Matrix4x4.Scale(new Vector3(grid.Width * grid.CellSize, 1f, grid.Height * grid.CellSize));

        return new PreviewMeshItem(
            GetGridPreviewMesh(),
            localToWorld,
            gridBounds,
            MapBakeGroupRole.Terrain,
            MapSurfaceMovementMask.AllGroundUnits |
            MapSurfaceMovementMask.AirGrounded |
            MapSurfaceMovementMask.BuildingPlacement);
    }

    private static Mesh GetGridPreviewMesh()
    {
        if (gridPreviewMesh != null)
            return gridPreviewMesh;

        gridPreviewMesh = new Mesh
        {
            name = "MapSurfaceGridPreviewQuad",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(1f, 0f, 0f)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };
        gridPreviewMesh.RecalculateBounds();
        return gridPreviewMesh;
    }

    public static Color ResolveColorForCapture(
        PreviewMeshItem item,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        float min,
        float max)
    {
        switch (mode)
        {
            case MapSurfaceEditorOverlaySystem.OverlayMode.Walkable:
                return ResolveWalkableColor(item);
            case MapSurfaceEditorOverlaySystem.OverlayMode.Height:
                return Color.Lerp(new Color(0.04f, 0.24f, 0.95f, 0.76f), new Color(1f, 0.86f, 0.03f, 0.82f), Mathf.InverseLerp(min, max, item.Bounds.center.y));
            case MapSurfaceEditorOverlaySystem.OverlayMode.Blocked:
                return IsHardBlocked(item)
                    ? new Color(0.95f, 0.05f, 0.05f, 0.72f)
                    : new Color(0.05f, 0.75f, 0.15f, 0.46f);
            case MapSurfaceEditorOverlaySystem.OverlayMode.RoadBridgeRamp:
                return ResolveRoadColor(item.Role);
            case MapSurfaceEditorOverlaySystem.OverlayMode.Layer:
                return item.Role == MapBakeGroupRole.Bridge
                    ? new Color(0.1f, 0.55f, 1f, 0.7f)
                    : new Color(0.1f, 0.85f, 0.3f, 0.45f);
            case MapSurfaceEditorOverlaySystem.OverlayMode.Slope:
            default:
                return item.Role == MapBakeGroupRole.Bridge || item.Role == MapBakeGroupRole.Ramp
                    ? new Color(0.95f, 0.65f, 0.1f, 0.7f)
                    : new Color(0.1f, 0.75f, 0.25f, 0.45f);
        }
    }

    public static void CalculateHeightRange(PreviewMeshItem[] meshes, out float min, out float max)
    {
        min = 0f;
        max = 0f;
        if (meshes == null || meshes.Length == 0)
            return;

        min = meshes[0].Bounds.min.y;
        max = meshes[0].Bounds.max.y;
        for (int i = 1; i < meshes.Length; i++)
        {
            Bounds bounds = meshes[i].Bounds;
            if (bounds.min.y < min)
                min = bounds.min.y;
            if (bounds.max.y > max)
                max = bounds.max.y;
        }
    }

    public static void ClearPreview()
    {
        previewMeshes = new PreviewMeshItem[0];
        previewLabel = string.Empty;
        minHeight = 0f;
        maxHeight = 0f;
        SceneView.RepaintAll();
    }

    private static void AddMeshItem(
        MeshFilter filter,
        MapBakeGroupAuthoring ownerGroup,
        MapBakeGroupRole role,
        List<PreviewMeshItem> items,
        int limit,
        Bounds gridBounds)
    {
        if (items.Count >= limit || filter == null || filter.sharedMesh == null)
            return;

        if (!IsOwnedByGroup(filter, ownerGroup))
            return;

        Renderer renderer = filter.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Bounds bounds = renderer.bounds;
        if (bounds.size.sqrMagnitude <= 0.0001f)
            return;
        if (!IntersectsGridXZ(bounds, gridBounds))
            return;

        items.Add(new PreviewMeshItem(filter.sharedMesh, filter.transform.localToWorldMatrix, bounds, role, ownerGroup.MovementMask));
    }

    private static bool IntersectsGridXZ(Bounds itemBounds, Bounds gridBounds)
    {
        return itemBounds.max.x >= gridBounds.min.x &&
               itemBounds.min.x <= gridBounds.max.x &&
               itemBounds.max.z >= gridBounds.min.z &&
               itemBounds.min.z <= gridBounds.max.z;
    }

    private static bool IsOwnedByGroup(MeshFilter filter, MapBakeGroupAuthoring ownerGroup)
    {
        if (filter == null || ownerGroup == null)
            return false;

        MapBakeGroupAuthoring nearestGroup = filter.GetComponentInParent<MapBakeGroupAuthoring>(true);
        return nearestGroup == ownerGroup;
    }

    private static bool IsUnderMapVehicles(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == "Vehicles" &&
                current.parent != null &&
                current.parent.name == "Map")
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldPreviewRole(MapBakeGroupRole role, MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        switch (mode)
        {
            case MapSurfaceEditorOverlaySystem.OverlayMode.Walkable:
            case MapSurfaceEditorOverlaySystem.OverlayMode.Blocked:
                return role == MapBakeGroupRole.Blocker ||
                       role == MapBakeGroupRole.Terrain ||
                       role == MapBakeGroupRole.Road ||
                       role == MapBakeGroupRole.Bridge ||
                       role == MapBakeGroupRole.Ramp;
            default:
                return role == MapBakeGroupRole.Terrain ||
                       role == MapBakeGroupRole.Road ||
                       role == MapBakeGroupRole.Bridge ||
                       role == MapBakeGroupRole.Ramp;
        }
    }

    private static void DrawPreview(SceneView sceneView)
    {
        if (previewMeshes.Length == 0)
            return;

        Material material = GetPreviewMaterial();
        if (material == null)
            return;

        for (int i = 0; i < previewMeshes.Length; i++)
            DrawPreviewMesh(previewMeshes[i], material);

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 460f, 92f), EditorStyles.helpBox);
        GUILayout.Label($"Map Surface Walkable Preview: {previewMode}", EditorStyles.boldLabel);
        GUILayout.Label(previewLabel);
        GUILayout.Label($"height {minHeight:0.##} to {maxHeight:0.##}");
        if (GUILayout.Button("Clear Map Surface Preview"))
            ClearPreview();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static void DrawPreviewMesh(PreviewMeshItem item, Material material)
    {
        Color color = ResolveColorForCapture(item, previewMode, minHeight, maxHeight);
        material.SetColor("_Color", color);
        material.SetInt("_ZTest", IsBlockerOverlayMode(previewMode) && IsPriorityWalkableBlockedItem(item)
            ? (int)CompareFunction.Always
            : (int)CompareFunction.LessEqual);
        material.SetPass(0);
        Graphics.DrawMeshNow(item.Mesh, ResolvePreviewDrawMatrix(item, previewMode));
    }

    private static bool IsPriorityWalkableBlockedItem(PreviewMeshItem item)
    {
        return item.Role == MapBakeGroupRole.Terrain ||
               item.Role == MapBakeGroupRole.Blocker ||
               item.Role == MapBakeGroupRole.Road ||
               item.Role == MapBakeGroupRole.Bridge ||
               item.Role == MapBakeGroupRole.Ramp;
    }

    public static Matrix4x4 ResolveDrawMatrixForCapture(
        PreviewMeshItem item,
        MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        return ResolvePreviewDrawMatrix(item, mode);
    }

    private static Matrix4x4 ResolvePreviewDrawMatrix(
        PreviewMeshItem item,
        MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        if (!IsBlockerOverlayMode(mode) || !IsHardBlocked(item))
            return Matrix4x4.Translate(Vector3.up * PreviewSurfaceLift) * item.LocalToWorld;

        return BuildFlattenedFootprintMatrix(item.LocalToWorld, item.Bounds.min.y + PreviewSurfaceLift);
    }

    private static Matrix4x4 BuildFlattenedFootprintMatrix(Matrix4x4 localToWorld, float y)
    {
        Matrix4x4 matrix = localToWorld;
        matrix.m01 = 0f;
        matrix.m10 = 0f;
        matrix.m11 = 0f;
        matrix.m12 = 0f;
        matrix.m13 = y;
        matrix.m21 = 0f;
        return matrix;
    }

    private static Color ResolveWalkableColor(PreviewMeshItem item)
    {
        if (IsHardBlocked(item))
            return new Color(0.95f, 0.05f, 0.05f, 0.72f);

        if (AllowsInfantryOnly(item.MovementMask))
            return new Color(0.05f, 0.55f, 1f, 0.62f);

        return new Color(0.02f, 0.9f, 0.18f, 0.62f);
    }

    private static bool IsHardBlocked(PreviewMeshItem item)
    {
        return item.Role == MapBakeGroupRole.Blocker ||
               item.MovementMask == MapSurfaceMovementMask.None;
    }

    private static bool AllowsInfantryOnly(MapSurfaceMovementMask movementMask)
    {
        return (movementMask & MapSurfaceMovementMask.Infantry) != 0 &&
               (movementMask & (MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle)) == 0;
    }

    private static bool IsBlockerOverlayMode(MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        return mode == MapSurfaceEditorOverlaySystem.OverlayMode.Walkable ||
               mode == MapSurfaceEditorOverlaySystem.OverlayMode.Blocked;
    }

    private static string BuildPreviewLabel(string authoringName, PreviewMeshItem[] meshes)
    {
        int blockers = 0;
        int roads = 0;
        int infantryOnly = 0;
        int walkable = 0;
        if (meshes != null)
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                if (IsHardBlocked(meshes[i]))
                {
                    blockers++;
                }
                else if (meshes[i].Role == MapBakeGroupRole.Road ||
                         meshes[i].Role == MapBakeGroupRole.Bridge ||
                         meshes[i].Role == MapBakeGroupRole.Ramp)
                {
                    roads++;
                    walkable++;
                }
                else
                {
                    if (AllowsInfantryOnly(meshes[i].MovementMask))
                        infantryOnly++;

                    walkable++;
                }
            }
        }

        return $"{authoringName}: walkable={walkable}, infantryOnly={infantryOnly}, roads={roads}, blockers={blockers}, vehicles ignored, blockers drawn as top-down footprints, roads drawn last, no asset saved";
    }

    private static Material GetPreviewMaterial()
    {
        if (previewMaterial != null)
            return previewMaterial;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
            return null;

        previewMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        previewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_Cull", (int)CullMode.Off);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        return previewMaterial;
    }

    private static Color ResolveRoadColor(MapBakeGroupRole role)
    {
        switch (role)
        {
            case MapBakeGroupRole.Road:
                return new Color(0.08f, 0.08f, 0.08f, 0.66f);
            case MapBakeGroupRole.Bridge:
                return new Color(0.1f, 0.55f, 1f, 0.72f);
            case MapBakeGroupRole.Ramp:
                return new Color(0.95f, 0.65f, 0.1f, 0.72f);
            case MapBakeGroupRole.Terrain:
                return new Color(0.05f, 0.65f, 0.12f, 0.4f);
            default:
                return new Color(0.5f, 0.5f, 0.5f, 0.25f);
        }
    }

    public readonly struct PreviewMeshItem
    {
        public readonly Mesh Mesh;
        public readonly Matrix4x4 LocalToWorld;
        public readonly Bounds Bounds;
        public readonly MapBakeGroupRole Role;
        public readonly MapSurfaceMovementMask MovementMask;

        public PreviewMeshItem(
            Mesh mesh,
            Matrix4x4 localToWorld,
            Bounds bounds,
            MapBakeGroupRole role,
            MapSurfaceMovementMask movementMask)
        {
            Mesh = mesh;
            LocalToWorld = localToWorld;
            Bounds = bounds;
            Role = role;
            MovementMask = movementMask;
        }
    }
}
#endif
