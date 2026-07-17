using System;
using System.Collections.Generic;
using Game.Rendering;
using UnityEngine;

namespace Game.Composition
{
    internal readonly struct StaticMapPresentationChunkCoordinate : IEquatable<StaticMapPresentationChunkCoordinate>
    {
        internal readonly int X;
        internal readonly int Z;

        internal StaticMapPresentationChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(StaticMapPresentationChunkCoordinate other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) =>
            obj is StaticMapPresentationChunkCoordinate other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Z);
    }

    internal readonly struct StaticMapPresentationChunk
    {
        internal readonly string Path;
        internal readonly StaticMapPresentationChunkCoordinate Coordinate;

        internal StaticMapPresentationChunk(
            string path,
            StaticMapPresentationChunkCoordinate coordinate)
        {
            Path = path;
            Coordinate = coordinate;
        }
    }

    internal static class StaticMapPresentationManifestIndex
    {
        private const float RayEpsilon = 0.00001f;

        internal static bool TryCreate(
            StaticMapPresentationManifest manifest,
            Camera camera,
            out StaticMapPresentationChunk[] chunks,
            out float chunkSize,
            out string error)
        {
            chunks = Array.Empty<StaticMapPresentationChunk>();
            chunkSize = 0f;
            if (manifest == null)
            {
                error = "Static map presentation manifest is missing.";
                return false;
            }

            if (camera == null)
            {
                error = "Static map presentation camera is missing.";
                return false;
            }

            if (!StaticMapPresentationManifest.IsSchemaReadable(manifest.SchemaVersion))
            {
                error = "Static map presentation manifest schema is unsupported.";
                return false;
            }

            if (!StaticMapPresentationManifest.HasRequiredIdentity(
                    manifest.SchemaVersion,
                    manifest.OperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath))
            {
                error = "Static map presentation manifest identity is incomplete.";
                return false;
            }

            if (!IsFinite(manifest.ChunkSize) || manifest.ChunkSize <= 0f)
            {
                error = "Static map presentation chunk size must be finite and positive.";
                return false;
            }

            if (manifest.Chunks == null || manifest.Sources == null ||
                manifest.Chunks.Count == 0 || manifest.Sources.Count == 0)
            {
                error = "Static map presentation manifest must contain chunks and sources.";
                return false;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<string> paths = new(StringComparer.Ordinal);
            HashSet<string> sourceIds = new(StringComparer.Ordinal);
            HashSet<StaticMapPresentationChunkCoordinate> coordinates = new();
            StaticMapPresentationChunk[] result = new StaticMapPresentationChunk[manifest.Chunks.Count];
            int expectedStart = 0;
            for (int i = 0; i < manifest.Chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry entry = manifest.Chunks[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ChunkId) || !ids.Add(entry.ChunkId))
                {
                    error = $"Static map presentation chunk {i} has an empty or duplicate ID.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.ScenePath) || !paths.Add(entry.ScenePath))
                {
                    error = $"Static map presentation chunk {i} has an empty or duplicate scene path.";
                    return false;
                }

                if (!IsFinite(entry.WorldBounds.center) || !IsFinite(entry.WorldBounds.size))
                {
                    error = $"Static map presentation chunk {i} has invalid bounds.";
                    return false;
                }

                if (entry.SourceStartIndex != expectedStart || entry.SourceCount <= 0 ||
                    entry.SourceStartIndex < 0 ||
                    entry.SourceStartIndex > manifest.Sources.Count - entry.SourceCount)
                {
                    error = $"Static map presentation chunk {i} has an invalid source range.";
                    return false;
                }

                int end = entry.SourceStartIndex + entry.SourceCount;
                for (int sourceIndex = entry.SourceStartIndex; sourceIndex < end; sourceIndex++)
                {
                    StaticMapPresentationSourceEntry source = manifest.Sources[sourceIndex];
                    if (source == null ||
                        !string.Equals(source.ChunkId, entry.ChunkId, StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(source.SourceGlobalObjectId) ||
                        !sourceIds.Add(source.SourceGlobalObjectId) ||
                        !IsFinite(source.WorldBounds.center) ||
                        !IsFinite(source.WorldBounds.size))
                    {
                        error = $"Static map presentation chunk {i} has an invalid source.";
                        return false;
                    }

                    if (!TryGetCoordinate(source.WorldBounds.center.x, manifest.ChunkSize, out int sourceX) ||
                        !TryGetCoordinate(source.WorldBounds.center.z, manifest.ChunkSize, out int sourceZ))
                    {
                        error = $"Static map presentation chunk {i} source coordinate is out of range.";
                        return false;
                    }

                    if (sourceIndex == entry.SourceStartIndex)
                    {
                        StaticMapPresentationChunkCoordinate coordinate = new(sourceX, sourceZ);
                        if (!coordinates.Add(coordinate))
                        {
                            error = $"Static map presentation chunk {i} duplicates a grid coordinate.";
                            return false;
                        }

                        result[i] = new StaticMapPresentationChunk(entry.ScenePath, coordinate);
                    }
                    else if (result[i].Coordinate.X != sourceX || result[i].Coordinate.Z != sourceZ)
                    {
                        error = $"Static map presentation chunk {i} spans multiple grid coordinates.";
                        return false;
                    }
                }

                expectedStart = end;
            }

            if (expectedStart != manifest.Sources.Count)
            {
                error = "Static map presentation chunk ranges do not cover every source.";
                return false;
            }

            chunkSize = manifest.ChunkSize;
            chunks = result;
            error = null;
            return true;
        }

        internal static bool TryGetCoordinate(float value, float chunkSize, out int coordinate)
        {
            double floored = Math.Floor((double)value / chunkSize);
            if (double.IsNaN(floored) || floored < int.MinValue || floored > int.MaxValue)
            {
                coordinate = 0;
                return false;
            }

            coordinate = (int)floored;
            return true;
        }

        internal static bool InsideExpandedRange(
            StaticMapPresentationChunkCoordinate coordinate,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            int ring)
        {
            return (long)coordinate.X >= (long)minX - ring &&
                   (long)coordinate.X <= (long)maxX + ring &&
                   (long)coordinate.Z >= (long)minZ - ring &&
                   (long)coordinate.Z <= (long)maxZ + ring;
        }

        internal static bool TryGetFootprint(
            Camera camera,
            float chunkSize,
            out int minX,
            out int maxX,
            out int minZ,
            out int maxZ)
        {
            minX = minZ = int.MaxValue;
            maxX = maxZ = int.MinValue;
            return TryProjectCorner(camera, 0f, 0f, chunkSize, ref minX, ref maxX, ref minZ, ref maxZ) &&
                   TryProjectCorner(camera, 0f, 1f, chunkSize, ref minX, ref maxX, ref minZ, ref maxZ) &&
                   TryProjectCorner(camera, 1f, 0f, chunkSize, ref minX, ref maxX, ref minZ, ref maxZ) &&
                   TryProjectCorner(camera, 1f, 1f, chunkSize, ref minX, ref maxX, ref minZ, ref maxZ);
        }

        private static bool TryProjectCorner(
            Camera camera,
            float viewportX,
            float viewportY,
            float chunkSize,
            ref int minX,
            ref int maxX,
            ref int minZ,
            ref int maxZ)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, viewportY));
            if (Mathf.Abs(ray.direction.y) < RayEpsilon)
                return false;

            float distance = -ray.origin.y / ray.direction.y;
            if (!IsFinite(distance) || distance < 0f)
                return false;

            Vector3 point = ray.GetPoint(distance);
            if (!TryGetCoordinate(point.x, chunkSize, out int x) ||
                !TryGetCoordinate(point.z, chunkSize, out int z))
            {
                return false;
            }

            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minZ = Math.Min(minZ, z);
            maxZ = Math.Max(maxZ, z);
            return true;
        }

        internal static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
