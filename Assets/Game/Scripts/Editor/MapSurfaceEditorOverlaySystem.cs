#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public sealed class MapSurfaceEditorOverlaySystem
{
    public enum OverlayMode
    {
        Walkable,
        Vehicle3x3Footprint,
        Height,
        Slope,
        Layer,
        RoadBridgeRamp,
        Blocked
    }

    private readonly MapSurfaceLayerAccess _layeredCellSystem = new();

    public void DrawOverlay(MapSurfaceComponent surface, GridConfig grid, OverlayMode mode, int cellStride)
    {
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            return;

        int stride = math.max(1, cellStride);
        if (mode == OverlayMode.Vehicle3x3Footprint)
        {
            DrawVehicleFootprintOverlay(surface, grid, stride);
            return;
        }

        for (int y = 0; y < surface.Dimensions.y; y += stride)
        {
            for (int x = 0; x < surface.Dimensions.x; x += stride)
            {
                int2 cell = new(x, y);
                if (!_layeredCellSystem.TryGetSurfaceRange(surface, cell, out MapSurfaceCellSurfaceRange range))
                    continue;

                for (int i = 0; i < range.SurfaceCount; i++)
                {
                    if (!_layeredCellSystem.TryGetSurface(surface, range, i, out MapSurfaceSample sample))
                        continue;

                    DrawCell(grid, sample, ResolveOverlayColor(sample, mode), stride);
                }
            }
        }
    }

    private static void DrawCell(GridConfig grid, MapSurfaceSample sample, Color color, int cellStride)
    {
        Vector3 center = GridUtils.CellToWorldCenter(grid, sample.Cell);
        center.y = sample.Height + 0.03f;
        float size = math.max(grid.CellSize, 0.01f) * cellStride;
        Vector3 halfX = new(size * 0.5f, 0f, 0f);
        Vector3 halfZ = new(0f, 0f, size * 0.5f);
        Vector3[] verts =
        {
            center - halfX - halfZ,
            center - halfX + halfZ,
            center + halfX + halfZ,
            center + halfX - halfZ
        };

        Handles.DrawSolidRectangleWithOutline(verts, color, new Color(color.r, color.g, color.b, 0.8f));
    }

    private static void DrawVehicleFootprintOverlay(MapSurfaceComponent surface, GridConfig grid, int cellStride)
    {
        MapSurfaceTraversalValidation validation = new();
        int2 footprint = new(3, 3);
        for (int y = 0; y < surface.Dimensions.y; y += cellStride)
        {
            for (int x = 0; x < surface.Dimensions.x; x += cellStride)
            {
                int2 cell = new(x, y);
                bool singleVehicleCell = validation.CanTraverse(
                    surface,
                    surface.HasSurfaceData,
                    cell,
                    MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle);
                bool footprintValid = singleVehicleCell &&
                    validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, true);

                Color color = singleVehicleCell
                    ? footprintValid
                        ? new Color(0.05f, 0.85f, 0.18f, 0.18f)
                        : new Color(1f, 0.42f, 0.04f, 0.42f)
                    : new Color(0.9f, 0.05f, 0.05f, 0.28f);
                DrawCell(grid, new MapSurfaceSample { Cell = cell, Height = grid.Origin.y }, color, cellStride);
            }
        }
    }

    private static Color ResolveOverlayColor(MapSurfaceSample sample, OverlayMode mode)
    {
        switch (mode)
        {
            case OverlayMode.Walkable:
                return sample.MovementMask == MapSurfaceMovementMask.None
                    ? new Color(0.9f, 0.05f, 0.05f, 0.28f)
                    : new Color(0.05f, 0.85f, 0.18f, 0.24f);
            case OverlayMode.Height:
                return Color.Lerp(new Color(0.05f, 0.25f, 0.9f, 0.18f), new Color(0.9f, 0.85f, 0.1f, 0.22f), math.saturate(sample.Height / 20f));
            case OverlayMode.Slope:
                return Color.Lerp(new Color(0.05f, 0.8f, 0.15f, 0.18f), new Color(0.9f, 0.05f, 0.02f, 0.24f), math.saturate(sample.SlopeDegrees / 45f));
            case OverlayMode.Layer:
                return ResolveLayerColor(sample.LayerId);
            case OverlayMode.RoadBridgeRamp:
                return ResolveRoadBridgeRampColor(sample);
            case OverlayMode.Blocked:
                return sample.SurfaceType == MapSurfaceType.Blocked || sample.MovementMask == MapSurfaceMovementMask.None
                    ? new Color(0.9f, 0.05f, 0.05f, 0.28f)
                    : new Color(0.05f, 0.55f, 0.15f, 0.12f);
            default:
                return new Color(0.2f, 0.6f, 1f, 0.14f);
        }
    }

    private static Color ResolveLayerColor(int layerId)
    {
        switch (math.abs(layerId) % 4)
        {
            case 0:
                return new Color(0.1f, 0.45f, 0.95f, 0.18f);
            case 1:
                return new Color(0.1f, 0.8f, 0.35f, 0.18f);
            case 2:
                return new Color(0.95f, 0.65f, 0.1f, 0.18f);
            default:
                return new Color(0.75f, 0.25f, 0.95f, 0.18f);
        }
    }

    private static Color ResolveRoadBridgeRampColor(MapSurfaceSample sample)
    {
        if ((sample.Flags & MapSurfaceFlags.Bridge) != 0 || sample.SurfaceType == MapSurfaceType.BridgeDeck)
            return new Color(0.1f, 0.55f, 1f, 0.24f);
        if ((sample.Flags & MapSurfaceFlags.Ramp) != 0 || sample.SurfaceType == MapSurfaceType.Ramp)
            return new Color(0.95f, 0.65f, 0.1f, 0.24f);
        if ((sample.Flags & MapSurfaceFlags.Highway) != 0 || sample.SurfaceType == MapSurfaceType.Highway)
            return new Color(0.6f, 0.6f, 0.65f, 0.24f);
        if ((sample.Flags & MapSurfaceFlags.Road) != 0)
            return new Color(0.18f, 0.18f, 0.18f, 0.22f);

        return new Color(0.05f, 0.45f, 0.12f, 0.1f);
    }
}
#endif
