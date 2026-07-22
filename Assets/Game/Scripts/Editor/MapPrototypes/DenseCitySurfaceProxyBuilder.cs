using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    internal readonly struct DenseCitySurfaceProxyBuildResult
    {
        internal DenseCitySurfaceProxyBuildResult(int partitions, int records, int vertices, int triangles)
        {
            Partitions = partitions;
            Records = records;
            Vertices = vertices;
            Triangles = triangles;
        }

        internal int Partitions { get; }
        internal int Records { get; }
        internal int Vertices { get; }
        internal int Triangles { get; }
    }

    internal static class DenseCitySurfaceProxyBuilder
    {
        private readonly struct PartitionKey : IComparable<PartitionKey>
        {
            internal PartitionKey(DenseCitySurfaceBakeRecord record)
            {
                Role = ResolveRole(record.Kind);
                Layer = record.Layer;
                MovementMask = record.MovementMask;
                Chunk = record.Chunk;
            }

            internal MapBakeGroupRole Role { get; }
            internal int Layer { get; }
            internal uint MovementMask { get; }
            internal Vector2Int Chunk { get; }

            public int CompareTo(PartitionKey other)
            {
                int comparison = Role.CompareTo(other.Role);
                if (comparison != 0) return comparison;
                comparison = Layer.CompareTo(other.Layer);
                if (comparison != 0) return comparison;
                comparison = MovementMask.CompareTo(other.MovementMask);
                if (comparison != 0) return comparison;
                comparison = Chunk.x.CompareTo(other.Chunk.x);
                return comparison != 0 ? comparison : Chunk.y.CompareTo(other.Chunk.y);
            }
        }

        private readonly struct PreparedPolygon
        {
            internal PreparedPolygon(DenseCitySurfaceBakeRecord record, Vector2[] points)
                : this(record.Elevation, points)
            {
            }

            internal PreparedPolygon(float elevation, Vector2[] points)
            {
                Elevation = elevation;
                Points = points;
            }

            internal float Elevation { get; }
            internal Vector2[] Points { get; }
        }

        private readonly struct RectangleSpan : IComparable<RectangleSpan>
        {
            internal RectangleSpan(float elevation, float minX, float minY, float maxX, float maxY)
            {
                Elevation = elevation;
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            internal float Elevation { get; }
            internal float MinX { get; }
            internal float MinY { get; }
            internal float MaxX { get; }
            internal float MaxY { get; }

            public int CompareTo(RectangleSpan other)
            {
                int comparison = Elevation.CompareTo(other.Elevation);
                if (comparison != 0) return comparison;
                comparison = MinX.CompareTo(other.MinX);
                if (comparison != 0) return comparison;
                comparison = MinY.CompareTo(other.MinY);
                if (comparison != 0) return comparison;
                comparison = MaxX.CompareTo(other.MaxX);
                return comparison != 0 ? comparison : MaxY.CompareTo(other.MaxY);
            }
        }

        private readonly struct HorizontalMergeKey : IComparable<HorizontalMergeKey>
        {
            internal HorizontalMergeKey(RectangleSpan rectangle)
            {
                Elevation = rectangle.Elevation;
                MinY = rectangle.MinY;
                MaxY = rectangle.MaxY;
            }

            private float Elevation { get; }
            private float MinY { get; }
            private float MaxY { get; }

            public int CompareTo(HorizontalMergeKey other)
            {
                int comparison = Elevation.CompareTo(other.Elevation);
                if (comparison != 0) return comparison;
                comparison = MinY.CompareTo(other.MinY);
                return comparison != 0 ? comparison : MaxY.CompareTo(other.MaxY);
            }
        }

        private readonly struct VerticalMergeKey : IComparable<VerticalMergeKey>
        {
            internal VerticalMergeKey(RectangleSpan rectangle)
            {
                Elevation = rectangle.Elevation;
                MinX = rectangle.MinX;
                MaxX = rectangle.MaxX;
            }

            private float Elevation { get; }
            private float MinX { get; }
            private float MaxX { get; }

            public int CompareTo(VerticalMergeKey other)
            {
                int comparison = Elevation.CompareTo(other.Elevation);
                if (comparison != 0) return comparison;
                comparison = MinX.CompareTo(other.MinX);
                return comparison != 0 ? comparison : MaxX.CompareTo(other.MaxX);
            }
        }

        internal static DenseCitySurfaceProxyBuildResult Build(
            DenseCityGenerationRecordSet records,
            DenseCityGeneratedRootAuthoring mapBakeRoot,
            string operationMapId,
            Rect mapSurfaceBounds,
            string candidateAssetFolder)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (mapBakeRoot == null || mapBakeRoot.Role != DenseCityGeneratedRootRole.MapBakeSource)
                throw new ArgumentException("A map-bake generated root is required.", nameof(mapBakeRoot));
            if (!mapBakeRoot.TryValidate(out string rootError))
                throw new InvalidOperationException(rootError);
            RequireFiniteMapBounds(mapSurfaceBounds);
            RequireNewCandidateFolder(operationMapId, mapBakeRoot.DeterministicGenerationHash, candidateAssetFolder);

            IReadOnlyList<DenseCitySurfaceBakeRecord> surfaces = records.Surfaces;
            if (surfaces.Count == 0)
                throw new InvalidOperationException("Dense-city surface proxy output requires surface records.");
            var partitions = new SortedDictionary<PartitionKey, List<PreparedPolygon>>();
            for (int index = 0; index < surfaces.Count; index++)
            {
                DenseCitySurfaceBakeRecord surface = surfaces[index];
                Vector2[] points = PreparePolygon(surface, mapSurfaceBounds);
                var key = new PartitionKey(surface);
                if (!partitions.TryGetValue(key, out List<PreparedPolygon> polygons))
                {
                    polygons = new List<PreparedPolygon>();
                    partitions.Add(key, polygons);
                }
                polygons.Add(new PreparedPolygon(surface, points));
            }

            var createdRoots = new List<GameObject>(partitions.Count);
            bool folderCreated = false;
            int vertexCount = 0;
            int triangleCount = 0;
            try
            {
                EnsureFolder(candidateAssetFolder);
                folderCreated = true;
                int partitionIndex = 0;
                foreach (KeyValuePair<PartitionKey, List<PreparedPolygon>> entry in partitions)
                {
                    PartitionKey key = entry.Key;
                    Transform roleRoot = ResolveRoleRoot(mapBakeRoot.transform, key.Role);
                    string name = CreatePartitionName(key, partitionIndex++);
                    List<PreparedPolygon> mergedPolygons = MergeCompatibleRectangles(entry.Value);
                    Mesh mesh = CreateMesh(name, mergedPolygons, out int vertices, out int triangles);
                    string assetPath = candidateAssetFolder + "/" + name + ".asset";
                    AssetDatabase.CreateAsset(mesh, assetPath);

                    var owner = new GameObject(name);
                    owner.transform.SetParent(roleRoot, false);
                    Configure(owner.AddComponent<MapBakeGroupAuthoring>(), key);
                    owner.AddComponent<MeshFilter>().sharedMesh = mesh;
                    createdRoots.Add(owner);
                    vertexCount += vertices;
                    triangleCount += triangles;
                }

                RequireCleanProxyHierarchy(mapBakeRoot.gameObject, surfaces.Count, createdRoots.Count);
                AssetDatabase.SaveAssets();
                return new DenseCitySurfaceProxyBuildResult(
                    createdRoots.Count,
                    surfaces.Count,
                    vertexCount,
                    triangleCount);
            }
            catch
            {
                for (int index = createdRoots.Count - 1; index >= 0; index--)
                {
                    if (createdRoots[index] != null)
                        UnityEngine.Object.DestroyImmediate(createdRoots[index]);
                }
                if (folderCreated)
                    AssetDatabase.DeleteAsset(candidateAssetFolder);
                throw;
            }
        }

        private static Vector2[] PreparePolygon(DenseCitySurfaceBakeRecord record, Rect mapSurfaceBounds)
        {
            Vector2[] points = record.Polygon.ToArray();
            for (int index = 0; index < points.Length; index++)
            {
                Vector2 point = points[index];
                if (point.x < mapSurfaceBounds.xMin || point.x > mapSurfaceBounds.xMax ||
                    point.y < mapSurfaceBounds.yMin || point.y > mapSurfaceBounds.yMax)
                {
                    throw new InvalidOperationException(
                        $"Surface polygon exceeds approved map bounds: '{record.Identity.StableKey}'.");
                }
            }
            float signedArea = SignedArea(points);
            if (!float.IsFinite(signedArea) || Mathf.Abs(signedArea) <= 0.0001f)
                throw new InvalidOperationException($"Surface polygon has zero area: '{record.Identity.StableKey}'.");
            if (signedArea > 0f)
                Array.Reverse(points);

            int first = 0;
            for (int index = 1; index < points.Length; index++)
            {
                if (points[index].x < points[first].x ||
                    (Mathf.Approximately(points[index].x, points[first].x) &&
                     points[index].y < points[first].y))
                {
                    first = index;
                }
            }
            if (first != 0)
            {
                var rotated = new Vector2[points.Length];
                for (int index = 0; index < points.Length; index++)
                    rotated[index] = points[(first + index) % points.Length];
                points = rotated;
            }
            RequireConvex(points, record.Identity.StableKey);
            return points;
        }

        private static List<PreparedPolygon> MergeCompatibleRectangles(
            IReadOnlyList<PreparedPolygon> polygons)
        {
            var rectangles = new List<RectangleSpan>(polygons.Count);
            var otherPolygons = new List<PreparedPolygon>();
            for (int index = 0; index < polygons.Count; index++)
            {
                PreparedPolygon polygon = polygons[index];
                if (TryCreateRectangle(polygon, out RectangleSpan rectangle))
                    rectangles.Add(rectangle);
                else
                    otherPolygons.Add(polygon);
            }

            int previousCount;
            do
            {
                previousCount = rectangles.Count;
                rectangles = MergeHorizontally(rectangles);
                rectangles = MergeVertically(rectangles);
            } while (rectangles.Count < previousCount);

            rectangles.Sort();
            var result = new List<PreparedPolygon>(rectangles.Count + otherPolygons.Count);
            for (int index = 0; index < rectangles.Count; index++)
            {
                RectangleSpan rectangle = rectangles[index];
                result.Add(new PreparedPolygon(
                    rectangle.Elevation,
                    new[]
                    {
                        new Vector2(rectangle.MinX, rectangle.MinY),
                        new Vector2(rectangle.MinX, rectangle.MaxY),
                        new Vector2(rectangle.MaxX, rectangle.MaxY),
                        new Vector2(rectangle.MaxX, rectangle.MinY)
                    }));
            }
            result.AddRange(otherPolygons);
            return result;
        }

        private static bool TryCreateRectangle(PreparedPolygon polygon, out RectangleSpan rectangle)
        {
            rectangle = default;
            if (polygon.Points.Length != 4)
                return false;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            for (int index = 0; index < polygon.Points.Length; index++)
            {
                Vector2 point = polygon.Points[index];
                minX = Mathf.Min(minX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x);
                maxY = Mathf.Max(maxY, point.y);
            }
            if (minX >= maxX || minY >= maxY)
                return false;

            int cornerMask = 0;
            for (int index = 0; index < polygon.Points.Length; index++)
            {
                Vector2 point = polygon.Points[index];
                int corner;
                if (point.x == minX && point.y == minY) corner = 1;
                else if (point.x == minX && point.y == maxY) corner = 2;
                else if (point.x == maxX && point.y == maxY) corner = 4;
                else if (point.x == maxX && point.y == minY) corner = 8;
                else return false;
                if ((cornerMask & corner) != 0)
                    return false;
                cornerMask |= corner;
            }
            if (cornerMask != 15)
                return false;

            rectangle = new RectangleSpan(polygon.Elevation, minX, minY, maxX, maxY);
            return true;
        }

        private static List<RectangleSpan> MergeHorizontally(IReadOnlyList<RectangleSpan> input)
        {
            var groups = new SortedDictionary<HorizontalMergeKey, List<RectangleSpan>>();
            for (int index = 0; index < input.Count; index++)
            {
                RectangleSpan rectangle = input[index];
                var key = new HorizontalMergeKey(rectangle);
                if (!groups.TryGetValue(key, out List<RectangleSpan> group))
                {
                    group = new List<RectangleSpan>();
                    groups.Add(key, group);
                }
                group.Add(rectangle);
            }

            var result = new List<RectangleSpan>(input.Count);
            foreach (List<RectangleSpan> group in groups.Values)
            {
                group.Sort((left, right) =>
                {
                    int comparison = left.MinX.CompareTo(right.MinX);
                    return comparison != 0 ? comparison : left.MaxX.CompareTo(right.MaxX);
                });
                RectangleSpan current = group[0];
                for (int index = 1; index < group.Count; index++)
                {
                    RectangleSpan next = group[index];
                    if (current.MaxX == next.MinX)
                    {
                        current = new RectangleSpan(
                            current.Elevation,
                            current.MinX,
                            current.MinY,
                            next.MaxX,
                            current.MaxY);
                    }
                    else
                    {
                        result.Add(current);
                        current = next;
                    }
                }
                result.Add(current);
            }
            return result;
        }

        private static List<RectangleSpan> MergeVertically(IReadOnlyList<RectangleSpan> input)
        {
            var groups = new SortedDictionary<VerticalMergeKey, List<RectangleSpan>>();
            for (int index = 0; index < input.Count; index++)
            {
                RectangleSpan rectangle = input[index];
                var key = new VerticalMergeKey(rectangle);
                if (!groups.TryGetValue(key, out List<RectangleSpan> group))
                {
                    group = new List<RectangleSpan>();
                    groups.Add(key, group);
                }
                group.Add(rectangle);
            }

            var result = new List<RectangleSpan>(input.Count);
            foreach (List<RectangleSpan> group in groups.Values)
            {
                group.Sort((left, right) =>
                {
                    int comparison = left.MinY.CompareTo(right.MinY);
                    return comparison != 0 ? comparison : left.MaxY.CompareTo(right.MaxY);
                });
                RectangleSpan current = group[0];
                for (int index = 1; index < group.Count; index++)
                {
                    RectangleSpan next = group[index];
                    if (current.MaxY == next.MinY)
                    {
                        current = new RectangleSpan(
                            current.Elevation,
                            current.MinX,
                            current.MinY,
                            current.MaxX,
                            next.MaxY);
                    }
                    else
                    {
                        result.Add(current);
                        current = next;
                    }
                }
                result.Add(current);
            }
            return result;
        }

        private static void RequireConvex(IReadOnlyList<Vector2> points, string stableKey)
        {
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 a = points[index];
                Vector2 b = points[(index + 1) % points.Count];
                Vector2 c = points[(index + 2) % points.Count];
                float cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
                if (cross > 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Surface polygon must be convex with deterministic winding: '{stableKey}'.");
                }
            }
        }

        private static Mesh CreateMesh(
            string name,
            IReadOnlyList<PreparedPolygon> polygons,
            out int vertexCount,
            out int triangleCount)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++)
            {
                PreparedPolygon polygon = polygons[polygonIndex];
                int start = vertices.Count;
                for (int pointIndex = 0; pointIndex < polygon.Points.Length; pointIndex++)
                {
                    Vector2 point = polygon.Points[pointIndex];
                    vertices.Add(new Vector3(point.x, polygon.Elevation, point.y));
                }
                for (int pointIndex = 1; pointIndex < polygon.Points.Length - 1; pointIndex++)
                {
                    indices.Add(start);
                    indices.Add(start + pointIndex);
                    indices.Add(start + pointIndex + 1);
                }
            }

            var mesh = new Mesh
            {
                name = name,
                indexFormat = vertices.Count <= ushort.MaxValue ? IndexFormat.UInt16 : IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0, true);
            vertexCount = vertices.Count;
            triangleCount = indices.Count / 3;
            return mesh;
        }

        private static void RequireCleanProxyHierarchy(GameObject root, int records, int partitions)
        {
            if (root.GetComponentsInChildren<MeshFilter>(true).Length != partitions ||
                root.GetComponentsInChildren<MeshRenderer>(true).Length != 0 ||
                root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Collider2D>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody2D>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city proxy hierarchy validation failed for {records} records.");
            }
        }

        private static void Configure(MapBakeGroupAuthoring group, PartitionKey key)
        {
            var serialized = new SerializedObject(group);
            serialized.FindProperty("role").enumValueIndex = (int)key.Role;
            serialized.FindProperty("layerId").intValue = key.Layer;
            serialized.FindProperty("movementMask").intValue = unchecked((int)key.MovementMask);
            serialized.FindProperty("includeInactiveChildren").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform ResolveRoleRoot(Transform mapRoot, MapBakeGroupRole role)
        {
            string name = role switch
            {
                MapBakeGroupRole.Terrain => "Terrain",
                MapBakeGroupRole.Road => "Roads",
                MapBakeGroupRole.Bridge => "Bridges",
                MapBakeGroupRole.Ramp => "Ramps",
                MapBakeGroupRole.Blocker => "Blockers",
                _ => throw new ArgumentOutOfRangeException(nameof(role))
            };
            Transform result = mapRoot.Find("BakeSources/" + name);
            if (result == null)
                throw new InvalidOperationException($"Dense-city proxy role root is missing: '{name}'.");
            return result;
        }

        private static MapBakeGroupRole ResolveRole(DenseCitySurfaceRecordKind kind) => kind switch
        {
            DenseCitySurfaceRecordKind.Terrain => MapBakeGroupRole.Terrain,
            DenseCitySurfaceRecordKind.Road => MapBakeGroupRole.Road,
            DenseCitySurfaceRecordKind.Bridge => MapBakeGroupRole.Bridge,
            DenseCitySurfaceRecordKind.Ramp => MapBakeGroupRole.Ramp,
            DenseCitySurfaceRecordKind.Blocker => MapBakeGroupRole.Blocker,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        private static string CreatePartitionName(PartitionKey key, int index) =>
            $"Proxy_{index:D4}_{key.Role}_L{key.Layer:D2}_M{key.MovementMask:x8}_" +
            $"X{FormatCoordinate(key.Chunk.x)}_Y{FormatCoordinate(key.Chunk.y)}";

        private static string FormatCoordinate(int value) =>
            value < 0 ? "N" + Math.Abs((long)value).ToString("D4") : "P" + value.ToString("D4");

        private static float SignedArea(IReadOnlyList<Vector2> points)
        {
            double area = 0d;
            for (int index = 0; index < points.Count; index++)
            {
                Vector2 current = points[index];
                Vector2 next = points[(index + 1) % points.Count];
                area += (double)current.x * next.y - (double)next.x * current.y;
            }
            return (float)(area * 0.5d);
        }

        private static void RequireNewCandidateFolder(
            string operationMapId,
            string deterministicGenerationHash,
            string path)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
                throw new ArgumentException("A valid operation-map id is required.", nameof(operationMapId));
            string requiredScope = "/" + operationMapId + "/Candidate/" +
                                   deterministicGenerationHash + "/SurfaceProxies";
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(requiredScope, StringComparison.Ordinal) ||
                AssetDatabase.IsValidFolder(path) || File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Dense-city proxy output requires a new map-scoped candidate Assets folder.");
            }
        }

        private static void RequireFiniteMapBounds(Rect bounds)
        {
            if (!float.IsFinite(bounds.xMin) || !float.IsFinite(bounds.yMin) ||
                !float.IsFinite(bounds.xMax) || !float.IsFinite(bounds.yMax) ||
                bounds.width <= 0f || bounds.height <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bounds),
                    "Dense-city proxy map bounds must be finite with positive dimensions.");
            }
        }

        private static void EnsureFolder(string path)
        {
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent))
                throw new InvalidOperationException($"Invalid dense-city proxy asset folder: '{path}'.");
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
