using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Composition;

namespace Game.Editor
{
    #if UNITY_EDITOR
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;

    [InitializeOnLoad]
    public static class MapSurfacePreviewOverlaySystem
    {
        private const int MaxWalkablePreviewMeshes = 4096;
        private const int MaxBlockerPreviewMeshes = 20000;
        private const int MaxVehicleFootprintPreviewRuns = 120000;
        private const float PreviewSurfaceLift = 0.08f;
        private const float VehicleFootprintPreviewLift = 0.16f;

        private static PreviewMeshItem[] previewMeshes = new PreviewMeshItem[0];
        private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode;
        private static string previewLabel = string.Empty;
        private static float minHeight;
        private static float maxHeight;
        private static int vehicleFootprintInvalidCellCount;
        private static int vehicleFootprintInvalidRunCount;
        private static int vehicleFootprintBlockedCellCount;
        private static int vehicleFootprintBlockedRunCount;
        private static int vehicleFootprintValidCellCount;
        private static int vehicleFootprintValidRunCount;
        private static bool vehicleFootprintPreviewRunLimitHit;
        private static bool hasVehicleFootprintPreviewBounds;
        private static Bounds vehicleFootprintPreviewBounds;
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
            previewLabel = BuildPreviewLabel(authoring.name, previewMeshes, mode);
            SceneView.RepaintAll();
        }

        public static PreviewMeshItem[] BuildPreviewMeshes(
            MapSurfaceAuthoring authoring,
            MapSurfaceEditorOverlaySystem.OverlayMode mode)
        {
            if (authoring == null)
                return new PreviewMeshItem[0];

            if (mode == MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint)
                return BuildVehicle3x3FootprintPreviewMeshes(authoring);

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

        private static PreviewMeshItem[] BuildWalkableBlockedPreviewMeshes(MapSurfaceAuthoring authoring, Bounds? previewBounds = null)
        {
            var walkableSurfaces = new List<PreviewMeshItem>(256);
            var roads = new List<PreviewMeshItem>(256);
            var blockers = new List<PreviewMeshItem>(256);
            if (!TryGetGridBounds(authoring, out Bounds gridBounds))
                return new PreviewMeshItem[0];

            Bounds filterBounds = previewBounds ?? gridBounds;
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
                    AddMeshItem(filters[filterIndex], group, group.Role, target, limit, filterBounds);
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

        private static PreviewMeshItem[] BuildVehicle3x3FootprintPreviewMeshes(MapSurfaceAuthoring authoring)
        {
            vehicleFootprintInvalidCellCount = 0;
            vehicleFootprintInvalidRunCount = 0;
            vehicleFootprintBlockedCellCount = 0;
            vehicleFootprintBlockedRunCount = 0;
            vehicleFootprintValidCellCount = 0;
            vehicleFootprintValidRunCount = 0;
            vehicleFootprintPreviewRunLimitHit = false;
            hasVehicleFootprintPreviewBounds = false;

            Bounds previewBounds = ResolveVehicleFootprintPreviewBounds(authoring);
            vehicleFootprintPreviewBounds = previewBounds;
            hasVehicleFootprintPreviewBounds = true;

            var items = new List<PreviewMeshItem>(4);
            if (authoring.GridConfig == null ||
                authoring.BakedSurfaceData == null ||
                !authoring.BakedSurfaceData.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
            {
                return BuildWalkableBlockedPreviewMeshes(authoring, previewBounds);
            }

            try
            {
                GridAuthoringConfig gridAsset = authoring.GridConfig;
                GridConfig grid = new()
                {
                    Width = gridAsset.Width,
                    Height = gridAsset.Height,
                    CellSize = gridAsset.CellSize,
                    Origin = (float3)gridAsset.Origin
                };
                MapSurfaceComponent surface = new()
                {
                    SurfaceBlob = surfaceBlob,
                    GridOrigin = surfaceBlob.Value.GridOrigin,
                    CellSize = surfaceBlob.Value.CellSize,
                    Dimensions = surfaceBlob.Value.Dimensions,
                    HasSurfaceData = 1
                };

                if (TryBuildVehicleFootprintCellMesh(
                        surface,
                        grid,
                        previewBounds,
                        VehicleFootprintPreviewCellKind.ValidFullFootprint,
                        out PreviewMeshItem validCenters))
                {
                    items.Add(validCenters);
                }

                if (TryBuildVehicleFootprintCellMesh(
                        surface,
                        grid,
                        previewBounds,
                        VehicleFootprintPreviewCellKind.SingleCellBlocked,
                        out PreviewMeshItem blockedCenters))
                {
                    items.Add(blockedCenters);
                }

                if (TryBuildVehicleFootprintCellMesh(
                        surface,
                        grid,
                        previewBounds,
                        VehicleFootprintPreviewCellKind.InvalidFullFootprint,
                        out PreviewMeshItem invalidCenters))
                {
                    items.Add(invalidCenters);
                }
            }
            finally
            {
                surfaceBlob.Dispose();
            }

            return items.ToArray();
        }

        private static bool TryBuildVehicleFootprintCellMesh(
            MapSurfaceComponent surface,
            GridConfig grid,
            Bounds previewBounds,
            VehicleFootprintPreviewCellKind kind,
            out PreviewMeshItem item)
        {
            item = default;
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
                return false;

            MapSurfaceTraversalValidation validation = new();
            int2 footprint = new(3, 3);
            ResolveCellRange(grid, surface.Dimensions, previewBounds, out int2 minCell, out int2 maxCell);
            List<Vector3> vertices = new(4096);
            List<int> indices = new(6144);
            Bounds bounds = default;
            bool hasBounds = false;

            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                int runStart = -1;
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    int2 cell = new(x, y);
                    bool shouldAddCell = ShouldAddVehicleFootprintPreviewCell(
                        surface,
                        grid,
                        validation,
                        cell,
                        footprint,
                        kind);
                    if (shouldAddCell)
                    {
                        IncrementVehicleFootprintCellCount(kind);
                        if (runStart < 0)
                            runStart = x;
                        continue;
                    }

                    AddVehicleFootprintRun(grid, kind, y, runStart, x - 1, vertices, indices, ref bounds, ref hasBounds);
                    runStart = -1;
                }

                AddVehicleFootprintRun(grid, kind, y, runStart, maxCell.x, vertices, indices, ref bounds, ref hasBounds);
            }

            if (vertices.Count == 0)
                return false;

            Mesh mesh = new()
            {
                name = ResolveVehicleFootprintMeshName(kind),
                hideFlags = HideFlags.HideAndDontSave,
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            item = new PreviewMeshItem(
                mesh,
                Matrix4x4.identity,
                hasBounds ? bounds : mesh.bounds,
                ResolveVehicleFootprintPreviewRole(kind),
                ResolveVehicleFootprintMovementMask(kind),
                ResolveVehicleFootprintPreviewMeshKind(kind));
            return true;
        }

        private static bool ShouldAddVehicleFootprintPreviewCell(
            MapSurfaceComponent surface,
            GridConfig grid,
            MapSurfaceTraversalValidation validation,
            int2 cell,
            int2 footprint,
            VehicleFootprintPreviewCellKind kind)
        {
            bool singleVehicleCell = validation.CanTraverse(
                surface,
                surface.HasSurfaceData,
                cell,
                MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle);

            if (kind == VehicleFootprintPreviewCellKind.SingleCellBlocked)
                return !singleVehicleCell;

            if (!singleVehicleCell)
                return false;

            bool validFullFootprint = validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, true);
            if (kind == VehicleFootprintPreviewCellKind.ValidFullFootprint)
                return validFullFootprint;

            return !validFullFootprint;
        }

        private static void IncrementVehicleFootprintCellCount(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    vehicleFootprintValidCellCount++;
                    break;
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    vehicleFootprintBlockedCellCount++;
                    break;
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    vehicleFootprintInvalidCellCount++;
                    break;
            }
        }

        private static void AddVehicleFootprintRun(
            GridConfig grid,
            VehicleFootprintPreviewCellKind kind,
            int y,
            int runStart,
            int runEnd,
            List<Vector3> vertices,
            List<int> indices,
            ref Bounds bounds,
            ref bool hasBounds)
        {
            if (runStart < 0 || runEnd < runStart)
                return;

            IncrementVehicleFootprintRunCount(kind);
            if (GetVehicleFootprintRunCount(kind) > MaxVehicleFootprintPreviewRuns)
            {
                vehicleFootprintPreviewRunLimitHit = true;
                return;
            }

            float cellSize = Mathf.Max(0.01f, grid.CellSize);
            float minX = grid.Origin.x + runStart * cellSize;
            float maxX = grid.Origin.x + (runEnd + 1) * cellSize;
            float minZ = grid.Origin.z + y * cellSize;
            float maxZ = grid.Origin.z + (y + 1) * cellSize;
            float previewY = grid.Origin.y + ResolveVehicleFootprintPreviewLift(kind);

            int baseIndex = vertices.Count;
            vertices.Add(new Vector3(minX, previewY, minZ));
            vertices.Add(new Vector3(minX, previewY, maxZ));
            vertices.Add(new Vector3(maxX, previewY, maxZ));
            vertices.Add(new Vector3(maxX, previewY, minZ));
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);

            Bounds runBounds = new(
                new Vector3((minX + maxX) * 0.5f, previewY, (minZ + maxZ) * 0.5f),
                new Vector3(maxX - minX, 0.01f, maxZ - minZ));
            if (!hasBounds)
            {
                bounds = runBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(runBounds);
            }
        }

        private static void IncrementVehicleFootprintRunCount(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    vehicleFootprintValidRunCount++;
                    break;
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    vehicleFootprintBlockedRunCount++;
                    break;
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    vehicleFootprintInvalidRunCount++;
                    break;
            }
        }

        private static int GetVehicleFootprintRunCount(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    return vehicleFootprintValidRunCount;
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    return vehicleFootprintBlockedRunCount;
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    return vehicleFootprintInvalidRunCount;
                default:
                    return 0;
            }
        }

        private static float ResolveVehicleFootprintPreviewLift(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    return PreviewSurfaceLift;
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    return VehicleFootprintPreviewLift + 0.02f;
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    return VehicleFootprintPreviewLift + 0.04f;
                default:
                    return VehicleFootprintPreviewLift;
            }
        }

        private static string ResolveVehicleFootprintMeshName(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    return "Vehicle3x3FootprintValidCentersPreview";
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    return "Vehicle3x3FootprintBlockedCentersPreview";
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    return "Vehicle3x3FootprintInvalidCentersPreview";
                default:
                    return "Vehicle3x3FootprintPreview";
            }
        }

        private static MapBakeGroupRole ResolveVehicleFootprintPreviewRole(VehicleFootprintPreviewCellKind kind)
        {
            return kind == VehicleFootprintPreviewCellKind.ValidFullFootprint
                ? MapBakeGroupRole.Terrain
                : MapBakeGroupRole.Blocker;
        }

        private static MapSurfaceMovementMask ResolveVehicleFootprintMovementMask(VehicleFootprintPreviewCellKind kind)
        {
            return kind == VehicleFootprintPreviewCellKind.ValidFullFootprint
                ? MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle
                : MapSurfaceMovementMask.None;
        }

        private static PreviewMeshKind ResolveVehicleFootprintPreviewMeshKind(VehicleFootprintPreviewCellKind kind)
        {
            switch (kind)
            {
                case VehicleFootprintPreviewCellKind.ValidFullFootprint:
                    return PreviewMeshKind.VehicleFootprintValidCenter;
                case VehicleFootprintPreviewCellKind.SingleCellBlocked:
                    return PreviewMeshKind.VehicleFootprintSingleCellBlocked;
                case VehicleFootprintPreviewCellKind.InvalidFullFootprint:
                    return PreviewMeshKind.VehicleFootprintInvalidCenter;
                default:
                    return PreviewMeshKind.AuthoringMesh;
            }
        }

        private static Bounds ResolveVehicleFootprintPreviewBounds(MapSurfaceAuthoring authoring)
        {
            const float minSize = 80f;
            const float maxSize = 260f;
            if (TryResolveSelectedPreviewBounds(authoring, out Bounds selectedBounds))
                return ExpandPreviewBounds(selectedBounds, minSize, maxSize);

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                float size = Mathf.Clamp(sceneView.size * 2.2f, minSize, maxSize);
                Vector3 center = sceneView.pivot;
                center.y = authoring.GridConfig != null ? authoring.GridConfig.Origin.y : center.y;
                return new Bounds(center, new Vector3(size, 80f, size));
            }

            if (TryGetGridBounds(authoring, out Bounds gridBounds))
            {
                float size = Mathf.Min(maxSize, Mathf.Max(minSize, Mathf.Min(gridBounds.size.x, gridBounds.size.z)));
                return new Bounds(gridBounds.center, new Vector3(size, 80f, size));
            }

            return new Bounds(authoring.transform.position, new Vector3(minSize, 80f, minSize));
        }

        private static bool TryResolveSelectedPreviewBounds(MapSurfaceAuthoring authoring, out Bounds bounds)
        {
            bounds = default;
            GameObject selected = Selection.activeGameObject;
            if (selected == null || selected == authoring.gameObject)
                return false;

            if (selected.GetComponentInParent<MapSurfaceAuthoring>(true) == authoring)
                return false;

            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = new Bounds(selected.transform.position, Vector3.one * 8f);
                return true;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds.size.sqrMagnitude > 0.001f;
        }

        private static Bounds ExpandPreviewBounds(Bounds source, float minSize, float maxSize)
        {
            float xzSize = Mathf.Clamp(Mathf.Max(source.size.x, source.size.z) + 80f, minSize, maxSize);
            Vector3 center = source.center;
            return new Bounds(center, new Vector3(xzSize, 80f, xzSize));
        }

        private static void ResolveCellRange(
            GridConfig grid,
            int2 dimensions,
            Bounds previewBounds,
            out int2 minCell,
            out int2 maxCell)
        {
            float cellSize = Mathf.Max(0.01f, grid.CellSize);
            int minX = Mathf.FloorToInt((previewBounds.min.x - grid.Origin.x) / cellSize) - 1;
            int maxX = Mathf.CeilToInt((previewBounds.max.x - grid.Origin.x) / cellSize) + 1;
            int minY = Mathf.FloorToInt((previewBounds.min.z - grid.Origin.z) / cellSize) - 1;
            int maxY = Mathf.CeilToInt((previewBounds.max.z - grid.Origin.z) / cellSize) + 1;

            int width = math.min(dimensions.x, grid.Width);
            int height = math.min(dimensions.y, grid.Height);
            minCell = new int2(math.clamp(minX, 0, math.max(0, width - 1)), math.clamp(minY, 0, math.max(0, height - 1)));
            maxCell = new int2(math.clamp(maxX, 0, math.max(0, width - 1)), math.clamp(maxY, 0, math.max(0, height - 1)));
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
            if (item.Kind == PreviewMeshKind.VehicleFootprintValidCenter)
                return new Color(0.02f, 0.9f, 0.18f, 0.38f);
            if (item.Kind == PreviewMeshKind.VehicleFootprintSingleCellBlocked)
                return new Color(0.96f, 0.05f, 0.04f, 0.78f);
            if (item.Kind == PreviewMeshKind.VehicleFootprintInvalidCenter)
                return new Color(1f, 0.42f, 0.04f, 0.74f);

            switch (mode)
            {
                case MapSurfaceEditorOverlaySystem.OverlayMode.Walkable:
                case MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint:
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
            vehicleFootprintInvalidCellCount = 0;
            vehicleFootprintInvalidRunCount = 0;
            vehicleFootprintBlockedCellCount = 0;
            vehicleFootprintBlockedRunCount = 0;
            vehicleFootprintValidCellCount = 0;
            vehicleFootprintValidRunCount = 0;
            vehicleFootprintPreviewRunLimitHit = false;
            hasVehicleFootprintPreviewBounds = false;
            vehicleFootprintPreviewBounds = default;
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
                case MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint:
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
            if (IsVehicleFootprintCellKind(item.Kind))
                return true;

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
            if (IsVehicleFootprintCellKind(item.Kind))
                return item.LocalToWorld;

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
            if (item.Kind == PreviewMeshKind.VehicleFootprintValidCenter ||
                item.Kind == PreviewMeshKind.VehicleFootprintInvalidCenter)
            {
                return false;
            }

            if (item.Kind == PreviewMeshKind.VehicleFootprintSingleCellBlocked)
                return true;

            return item.Role == MapBakeGroupRole.Blocker ||
                   item.MovementMask == MapSurfaceMovementMask.None;
        }

        private static bool IsVehicleFootprintCellKind(PreviewMeshKind kind)
        {
            return kind == PreviewMeshKind.VehicleFootprintValidCenter ||
                   kind == PreviewMeshKind.VehicleFootprintSingleCellBlocked ||
                   kind == PreviewMeshKind.VehicleFootprintInvalidCenter;
        }

        private static bool AllowsInfantryOnly(MapSurfaceMovementMask movementMask)
        {
            return (movementMask & MapSurfaceMovementMask.Infantry) != 0 &&
                   (movementMask & (MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle)) == 0;
        }

        private static bool IsBlockerOverlayMode(MapSurfaceEditorOverlaySystem.OverlayMode mode)
        {
            return mode == MapSurfaceEditorOverlaySystem.OverlayMode.Walkable ||
                   mode == MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint ||
                   mode == MapSurfaceEditorOverlaySystem.OverlayMode.Blocked;
        }

        private static string BuildPreviewLabel(
            string authoringName,
            PreviewMeshItem[] meshes,
            MapSurfaceEditorOverlaySystem.OverlayMode mode)
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

            if (mode == MapSurfaceEditorOverlaySystem.OverlayMode.Vehicle3x3Footprint)
            {
                string limit = vehicleFootprintPreviewRunLimitHit
                    ? $", capped at {MaxVehicleFootprintPreviewRuns} runs"
                    : string.Empty;
                string bounds = string.Empty;
                if (hasVehicleFootprintPreviewBounds)
                {
                    Vector3 center = vehicleFootprintPreviewBounds.center;
                    Vector3 size = vehicleFootprintPreviewBounds.size;
                    bounds = $", previewCenter=({center.x:0.#},{center.z:0.#}), previewSize={size.x:0.#}x{size.z:0.#}";
                }

                return $"{authoringName}: tank 3x3 runtime preview{bounds}, greenCells={vehicleFootprintValidCellCount}, redCells={vehicleFootprintBlockedCellCount}, redRuns={vehicleFootprintBlockedRunCount}, amberCells={vehicleFootprintInvalidCellCount}, amberRuns={vehicleFootprintInvalidRunCount}{limit}, red means baked vehicle-blocked, amber means center drivable but full 3x3 tank footprint invalid";
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

        public enum PreviewMeshKind : byte
        {
            AuthoringMesh,
            VehicleFootprintValidCenter,
            VehicleFootprintSingleCellBlocked,
            VehicleFootprintInvalidCenter
        }

        private enum VehicleFootprintPreviewCellKind : byte
        {
            ValidFullFootprint,
            SingleCellBlocked,
            InvalidFullFootprint
        }

        public readonly struct PreviewMeshItem
        {
            public readonly Mesh Mesh;
            public readonly Matrix4x4 LocalToWorld;
            public readonly Bounds Bounds;
            public readonly MapBakeGroupRole Role;
            public readonly MapSurfaceMovementMask MovementMask;
            public readonly PreviewMeshKind Kind;

            public PreviewMeshItem(
                Mesh mesh,
                Matrix4x4 localToWorld,
                Bounds bounds,
                MapBakeGroupRole role,
                MapSurfaceMovementMask movementMask)
                : this(mesh, localToWorld, bounds, role, movementMask, PreviewMeshKind.AuthoringMesh)
            {
            }

            public PreviewMeshItem(
                Mesh mesh,
                Matrix4x4 localToWorld,
                Bounds bounds,
                MapBakeGroupRole role,
                MapSurfaceMovementMask movementMask,
                PreviewMeshKind kind)
            {
                Mesh = mesh;
                LocalToWorld = localToWorld;
                Bounds = bounds;
                Role = role;
                MovementMask = movementMask;
                Kind = kind;
            }
        }
    }
    #endif
}
