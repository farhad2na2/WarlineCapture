using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class HelicopterDropSearchDiagnostics
    {
        internal bool searchStarted;
        internal float preferredWorldX;
        internal float preferredWorldY;
        internal float preferredWorldZ;
        internal int preferredCellX;
        internal int preferredCellY;
        internal int gridWidth;
        internal int gridHeight;
        internal float gridCellSize;
        internal float gridOriginX;
        internal float gridOriginY;
        internal float gridOriginZ;
        internal int footprintX;
        internal int footprintY;
        internal int candidateChecks;
        internal int candidateAccepted;
        internal int outOfGrid;
        internal int walkableRejected;
        internal int blockedRejected;
        internal int occupiedRejected;
        internal int reservedRejected;
        internal int surfaceBlockedRejected;
        internal int surfaceReservedRejected;
        internal int surfaceInfantryRejected;
        internal int heightRejected;
        internal float heightRejectedDeltaMin;
        internal float heightRejectedDeltaMax;
        internal float heightLimit;
        internal int heightRejectedRoadLike;
        internal int heightRejectedNonRoad;
    }

    internal static class SharedHelicopterDropKernel
    {
        private const int HelicopterDropSearchRadiusCells = 96;
        private const int HelicopterDropMaxCandidateChecksPerSearch = 128;
        private const int HelicopterDropLandingPaddingCells = 1;
        private const float HelicopterDropMaxNonRoadLandingHeightDelta = 0.75f;

        internal static bool TryFindClearHelicopterDropCell(
            EntityManager em,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,
            NativeBitArray occupied,
            NativeBitArray reserved,
            int2 preferredCell,
            int2 unitFootprint,
            int startRadius,
            out bool exhaustedSearchBudget,
            out int nextSearchRadius,
            out int2 dropCell,
            out Vector3 dropPosition)
        {
            return TryFindClearHelicopterDropCell(em, grid, walkable, blocked, occupied, reserved,
                preferredCell, unitFootprint, startRadius, null, out exhaustedSearchBudget,
                out nextSearchRadius, out dropCell, out dropPosition);
        }

        internal static bool TryFindClearHelicopterDropCell(
            EntityManager em,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,
            NativeBitArray occupied,
            NativeBitArray reserved,
            int2 preferredCell,
            int2 unitFootprint,
            int startRadius,
            HelicopterDropSearchDiagnostics diagnostics,
            out bool exhaustedSearchBudget,
            out int nextSearchRadius,
            out int2 dropCell,
            out Vector3 dropPosition)
        {
            exhaustedSearchBudget = false;
            nextSearchRadius = 0;
            dropCell = default;
            dropPosition = default;
            int candidateChecks = 0;
            int safeStartRadius = math.clamp(startRadius, 0, HelicopterDropSearchRadiusCells);
            for (int radius = safeStartRadius; radius <= HelicopterDropSearchRadiusCells; radius++)
            {
                for (int y = preferredCell.y - radius; y <= preferredCell.y + radius; y++)
                {
                    for (int x = preferredCell.x - radius; x <= preferredCell.x + radius; x++)
                    {
                        if (radius > 0 && math.abs(x - preferredCell.x) != radius &&
                            math.abs(y - preferredCell.y) != radius) continue;
                        candidateChecks++;
                        if (diagnostics != null) diagnostics.candidateChecks++;
                        var candidate = new int2(x, y);
                        if (!TryResolveHelicopterDropCandidate(em, grid, walkable, blocked,
                            occupied, reserved, candidate, unitFootprint, diagnostics, out var position)) continue;
                        dropCell = candidate;
                        dropPosition = position;
                        return true;
                    }
                }
                if (candidateChecks >= HelicopterDropMaxCandidateChecksPerSearch)
                {
                    nextSearchRadius = radius < HelicopterDropSearchRadiusCells ? radius + 1 : 0;
                    exhaustedSearchBudget = nextSearchRadius > 0;
                    return false;
                }
            }
            return false;
        }

        internal static bool TryResolveHelicopterDropCandidate(
            EntityManager em,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,
            NativeBitArray occupied,
            NativeBitArray reserved,
            int2 candidate,
            int2 footprintSize,
            out Vector3 dropPosition)
        {
            return TryResolveHelicopterDropCandidate(em, grid, walkable, blocked, occupied,
                reserved, candidate, footprintSize, null, out dropPosition);
        }

        private static bool TryResolveHelicopterDropCandidate(
            EntityManager em,
            GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,
            NativeBitArray occupied,
            NativeBitArray reserved,
            int2 candidate,
            int2 footprintSize,
            HelicopterDropSearchDiagnostics diagnostics,
            out Vector3 dropPosition)
        {
            dropPosition = default;
            int2 footprint = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(candidate, footprint);
            int2 max = min + footprint;
            int padding = math.max(0, HelicopterDropLandingPaddingCells);
            int2 paddedMin = min - new int2(padding, padding);
            int2 paddedMax = max + new int2(padding, padding);
            if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
            {
                if (diagnostics != null) diagnostics.outOfGrid++;
                return false;
            }

            for (int y = paddedMin.y; y < paddedMax.y; y++)
            {
                int row = y * grid.Width;
                for (int x = paddedMin.x; x < paddedMax.x; x++)
                {
                    int index = row + x;
                    if (walkable[index].Value == 0)
                    {
                        if (diagnostics != null) diagnostics.walkableRejected++;
                        return false;
                    }
                    if (blocked.IsCreated && blocked.IsSet(index))
                    {
                        if (diagnostics != null) diagnostics.blockedRejected++;
                        return false;
                    }
                    if (occupied.IsCreated && occupied.IsSet(index))
                    {
                        if (diagnostics != null) diagnostics.occupiedRejected++;
                        return false;
                    }
                    if (reserved.IsCreated && reserved.IsSet(index))
                    {
                        if (diagnostics != null) diagnostics.reservedRejected++;
                        return false;
                    }
                }
            }

            float3 resolved = GridUtils.CellToWorldCenter(grid, candidate);
            MapSurfaceSpawnGrounding grounding = new();
            if (grounding.TryGroundCellCenter(em, grid, candidate, ref resolved, out MapSurfaceSample sample))
            {
                if (sample.SurfaceType == MapSurfaceType.Blocked)
                {
                    if (diagnostics != null) diagnostics.surfaceBlockedRejected++;
                    return false;
                }
                if ((sample.Flags & MapSurfaceFlags.Reserved) != 0)
                {
                    if (diagnostics != null) diagnostics.surfaceReservedRejected++;
                    return false;
                }
                if ((sample.MovementMask & MapSurfaceMovementMask.Infantry) == 0)
                {
                    if (diagnostics != null) diagnostics.surfaceInfantryRejected++;
                    return false;
                }

                float heightDelta = resolved.y - grid.Origin.y;
                if (heightDelta > HelicopterDropMaxNonRoadLandingHeightDelta && !IsRoadLikeHelicopterDropSurface(sample))
                {
                    if (diagnostics != null)
                    {
                        diagnostics.heightRejected++;
                        diagnostics.heightRejectedDeltaMin = diagnostics.heightRejected == 1
                            ? heightDelta : math.min(diagnostics.heightRejectedDeltaMin, heightDelta);
                        diagnostics.heightRejectedDeltaMax = math.max(diagnostics.heightRejectedDeltaMax, heightDelta);
                        diagnostics.heightLimit = HelicopterDropMaxNonRoadLandingHeightDelta;
                        diagnostics.heightRejectedNonRoad++;
                    }
                    return false;
                }
            }

            if (diagnostics != null) diagnostics.candidateAccepted++;
            dropPosition = new Vector3(resolved.x, resolved.y, resolved.z);
            return true;
        }

        private static bool IsRoadLikeHelicopterDropSurface(MapSurfaceSample sample)
        {
            return sample.SurfaceType == MapSurfaceType.Road ||
                   sample.SurfaceType == MapSurfaceType.DirtRoad ||
                   sample.SurfaceType == MapSurfaceType.Highway ||
                   sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                   sample.SurfaceType == MapSurfaceType.Ramp ||
                   (sample.Flags & (MapSurfaceFlags.Road | MapSurfaceFlags.Bridge | MapSurfaceFlags.Ramp)) != 0;
        }
    }
}
