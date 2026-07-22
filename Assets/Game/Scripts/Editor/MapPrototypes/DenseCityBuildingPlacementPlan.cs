using System;
using Game.Components;
using UnityEngine;

namespace Game.Editor
{
    internal enum DenseCityFrontageEdge
    {
        None,
        MinimumX,
        MaximumX,
        MinimumZ,
        MaximumZ
    }

    internal readonly struct DenseCityBuildingPlacementPlan
    {
        internal DenseCityBuildingPlacementPlan(
            Vector2Int originCell,
            Vector2Int footprintCells,
            Matrix4x4 worldMatrix,
            Bounds blockerBounds,
            Vector2 footprintSize,
            float foundationElevation,
            Vector3 frontageDirection,
            Vector2Int chunk,
            Vector3 realizationLocalPosition,
            Quaternion realizationLocalRotation)
        {
            OriginCell = originCell;
            FootprintCells = footprintCells;
            WorldMatrix = worldMatrix;
            BlockerBounds = blockerBounds;
            FootprintSize = footprintSize;
            FoundationElevation = foundationElevation;
            FrontageDirection = frontageDirection;
            Chunk = chunk;
            RealizationLocalPosition = realizationLocalPosition;
            RealizationLocalRotation = realizationLocalRotation;
        }

        internal Vector2Int OriginCell { get; }
        internal Vector2Int FootprintCells { get; }
        internal Matrix4x4 WorldMatrix { get; }
        internal Bounds BlockerBounds { get; }
        internal Vector2 FootprintSize { get; }
        internal float FoundationElevation { get; }
        internal Vector3 FrontageDirection { get; }
        internal Vector2Int Chunk { get; }
        internal Vector3 RealizationLocalPosition { get; }
        internal Quaternion RealizationLocalRotation { get; }
    }

    internal static class DenseCityBuildingPlacementPlanner
    {
        internal const float PresentationChunkSize = 32f;
        private const float MatrixTolerance = 0.0001f;
        private const float BoundsTolerance = 0.05f;

        internal static DenseCityBuildingPlacementPlan Create(
            Vector3 requestedCenter,
            float rotationDegrees,
            float localWidth,
            float localDepth,
            float localHeight,
            float visualScale,
            float foundationElevation,
            GridConfig grid,
            DenseCityFrontageEdge frontageEdge,
            float frontageBoundary)
        {
            return Create(
                requestedCenter,
                rotationDegrees,
                localWidth,
                localDepth,
                localHeight,
                visualScale,
                foundationElevation,
                grid,
                frontageEdge,
                frontageBoundary,
                Matrix4x4.identity,
                Matrix4x4.identity);
        }

        internal static DenseCityBuildingPlacementPlan Create(
            Vector3 requestedCenter,
            float rotationDegrees,
            float localWidth,
            float localDepth,
            float localHeight,
            float visualScale,
            float foundationElevation,
            GridConfig grid,
            DenseCityFrontageEdge frontageEdge,
            float frontageBoundary,
            Matrix4x4 realizationParentLocalToWorld,
            Matrix4x4 realizationParentWorldToLocal)
        {
            RequireFinite(requestedCenter, nameof(requestedCenter));
            RequireFinite(rotationDegrees, nameof(rotationDegrees));
            RequirePositiveFinite(localWidth, nameof(localWidth));
            RequirePositiveFinite(localDepth, nameof(localDepth));
            RequirePositiveFinite(localHeight, nameof(localHeight));
            RequirePositiveFinite(visualScale, nameof(visualScale));
            RequireFinite(foundationElevation, nameof(foundationElevation));
            RequireFinite(frontageBoundary, nameof(frontageBoundary));
            RequireFinite(new Vector3(grid.Origin.x, grid.Origin.y, grid.Origin.z), nameof(grid));
            RequirePositiveFinite(grid.CellSize, nameof(grid));
            if (grid.Width <= 0 || grid.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(grid));

            int quarterTurns = Mathf.RoundToInt(rotationDegrees / 90f);
            if (Mathf.Abs(rotationDegrees - quarterTurns * 90f) > 0.001f)
                throw new ArgumentOutOfRangeException(nameof(rotationDegrees), "Dense-city buildings require quarter-turn rotation.");

            bool quarterTurn = Mathf.Abs(quarterTurns) % 2 != 0;
            float worldWidth = quarterTurn ? localDepth : localWidth;
            float worldDepth = quarterTurn ? localWidth : localDepth;
            int footprintX = Mathf.Max(1, Mathf.CeilToInt(worldWidth));
            int footprintZ = Mathf.Max(1, Mathf.CeilToInt(worldDepth));
            if (footprintX > grid.Width || footprintZ > grid.Height)
                throw new ArgumentOutOfRangeException(nameof(grid), "Dense-city building footprint exceeds the grid.");

            int originX = Mathf.Clamp(
                Mathf.RoundToInt(requestedCenter.x - grid.Origin.x - footprintX * 0.5f),
                0,
                grid.Width - footprintX);
            int originZ = Mathf.Clamp(
                Mathf.RoundToInt(requestedCenter.z - grid.Origin.z - footprintZ * 0.5f),
                0,
                grid.Height - footprintZ);
            var originCell = new Vector2Int(originX, originZ);
            var footprintCells = new Vector2Int(footprintX, footprintZ);
            var position = new Vector3(
                grid.Origin.x + (originX + footprintX * 0.5f) * grid.CellSize,
                foundationElevation,
                grid.Origin.z + (originZ + footprintZ * 0.5f) * grid.CellSize);

            Vector3 scaledSize = new(worldWidth, localHeight, worldDepth);
            ApplyFrontageSnap(ref position, scaledSize, frontageEdge, frontageBoundary);
            Quaternion worldRotation = Quaternion.Euler(0f, rotationDegrees, 0f);
            Vector3 localPosition = realizationParentWorldToLocal.MultiplyPoint3x4(position);
            Quaternion localRotation = Quaternion.Inverse(realizationParentLocalToWorld.rotation) * worldRotation;
            Matrix4x4 worldMatrix = realizationParentLocalToWorld * Matrix4x4.TRS(
                localPosition,
                localRotation,
                Vector3.one * visualScale);
            Vector3 realizablePosition = worldMatrix.GetColumn(3);
            var blockerBounds = new Bounds(
                realizablePosition + Vector3.up * scaledSize.y * 0.5f,
                scaledSize);
            var chunk = new Vector2Int(
                Mathf.FloorToInt(realizablePosition.x / PresentationChunkSize),
                Mathf.FloorToInt(realizablePosition.z / PresentationChunkSize));
            return new DenseCityBuildingPlacementPlan(
                originCell,
                footprintCells,
                worldMatrix,
                blockerBounds,
                new Vector2(worldWidth, worldDepth),
                foundationElevation,
                ResolveFrontageDirection(frontageEdge, rotationDegrees),
                chunk,
                localPosition,
                localRotation);
        }

        internal static bool MatchesRealization(
            DenseCityBuildingPlacementPlan plan,
            Matrix4x4 realizedWorldMatrix,
            Bounds realizedBounds)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    if (Mathf.Abs(plan.WorldMatrix[row, column] - realizedWorldMatrix[row, column]) >
                        MatrixTolerance)
                    {
                        return false;
                    }
                }
            }

            return Mathf.Abs(plan.BlockerBounds.center.x - realizedBounds.center.x) <= BoundsTolerance &&
                   Mathf.Abs(plan.BlockerBounds.center.z - realizedBounds.center.z) <= BoundsTolerance &&
                   Mathf.Abs(plan.BlockerBounds.min.y - realizedBounds.min.y) <= BoundsTolerance &&
                   realizedBounds.min.x >= plan.BlockerBounds.min.x - BoundsTolerance &&
                   realizedBounds.max.x <= plan.BlockerBounds.max.x + BoundsTolerance &&
                   realizedBounds.max.y <= plan.BlockerBounds.max.y + BoundsTolerance &&
                   realizedBounds.min.z >= plan.BlockerBounds.min.z - BoundsTolerance &&
                   realizedBounds.max.z <= plan.BlockerBounds.max.z + BoundsTolerance;
        }

        private static void ApplyFrontageSnap(
            ref Vector3 position,
            Vector3 size,
            DenseCityFrontageEdge edge,
            float boundary)
        {
            switch (edge)
            {
                case DenseCityFrontageEdge.None:
                    return;
                case DenseCityFrontageEdge.MinimumX:
                    position.x = boundary + size.x * 0.5f;
                    return;
                case DenseCityFrontageEdge.MaximumX:
                    position.x = boundary - size.x * 0.5f;
                    return;
                case DenseCityFrontageEdge.MinimumZ:
                    position.z = boundary + size.z * 0.5f;
                    return;
                case DenseCityFrontageEdge.MaximumZ:
                    position.z = boundary - size.z * 0.5f;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge));
            }
        }

        private static Vector3 ResolveFrontageDirection(
            DenseCityFrontageEdge edge,
            float rotationDegrees) => edge switch
        {
            DenseCityFrontageEdge.None => Quaternion.Euler(0f, rotationDegrees, 0f) * Vector3.forward,
            DenseCityFrontageEdge.MinimumX => Vector3.left,
            DenseCityFrontageEdge.MaximumX => Vector3.right,
            DenseCityFrontageEdge.MinimumZ => Vector3.back,
            DenseCityFrontageEdge.MaximumZ => Vector3.forward,
            _ => throw new ArgumentOutOfRangeException(nameof(edge))
        };

        private static void RequirePositiveFinite(float value, string name)
        {
            RequireFinite(value, name);
            if (value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequireFinite(Vector3 value, string name)
        {
            RequireFinite(value.x, name);
            RequireFinite(value.y, name);
            RequireFinite(value.z, name);
        }

    }
}
