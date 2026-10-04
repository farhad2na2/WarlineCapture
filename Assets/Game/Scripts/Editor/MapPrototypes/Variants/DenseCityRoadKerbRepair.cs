using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    // Subtract adjoining carriageways from the visual kerbs; gameplay surfaces are untouched.
    public static class DenseCityRoadKerbRepair
    {
        private const string Art = "Assets/Game/Art/MapBeautify/DenseCity/";
        private const float Epsilon = .0001f;

        public static void Run()
        {
            int removed = TrimGeneratedKerbs();
            if (TrimGeneratedKerbs() != 0) throw new InvalidOperationException("Kerb clipping did not converge.");
            AssetDatabase.SaveAssets();
            Debug.Log($"[DenseCityRoadKerbRepair] result=Passed clippedKerbTriangles={removed} gameplay=Unchanged");
        }

        internal static int TrimGeneratedKerbs()
        {
            var paving = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "DenseCity_FlatPaving.asset");
            var kerbs = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "DenseCity_KerbPaving.asset");
            if (paving == null || kerbs == null) throw new InvalidOperationException("Dense City road meshes are missing.");
            var roadVertices = paving.vertices;
            if (roadVertices.Length % 4 != 0) throw new InvalidOperationException("Expected authored paving quads.");
            var roads = new List<Vector3[]>();
            for (int i = 0; i < roadVertices.Length; i += 4)
                roads.Add(roadVertices.Skip(i).Take(4).ToArray());
            var roadBounds = roads.Select(BoundsOf).ToArray();
            var source = kerbs.vertices;
            var indices = kerbs.triangles;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int clipped = 0;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var original = new List<Vector3> { source[indices[i]], source[indices[i + 1]], source[indices[i + 2]] };
                var pieces = new List<List<Vector3>> { original };
                var originalBounds = BoundsOf(original);
                for (int roadIndex = 0; roadIndex < roads.Count; roadIndex++)
                {
                    var road = roads[roadIndex];
                    // Parallel streets, different elevations and this tile's own kerbs need no clipping.
                    if (!Overlaps(originalBounds, roadBounds[roadIndex])) continue;
                    var next = new List<List<Vector3>>();
                    foreach (var piece in pieces) Subtract(piece, road, next);
                    pieces = next;
                    if (pieces.Count == 0) break;
                }
                float before = Area(original), after = pieces.Sum(Area);
                if (before - after > Epsilon) clipped++;
                foreach (var piece in pieces)
                {
                    if (Area(piece) < Epsilon) continue;
                    int start = vertices.Count;
                    vertices.AddRange(piece);
                    for (int j = 1; j + 1 < piece.Count; j++) triangles.AddRange(new[] { start, start + j, start + j + 1 });
                }
            }
            if (clipped == 0) return 0;
            kerbs.Clear();
            kerbs.indexFormat = IndexFormat.UInt32;
            kerbs.SetVertices(vertices);
            kerbs.SetTriangles(triangles, 0);
            kerbs.RecalculateNormals();
            kerbs.RecalculateBounds();
            EditorUtility.SetDirty(kerbs);
            return clipped;
        }

        private static Bounds BoundsOf(IReadOnlyList<Vector3> polygon)
        {
            var bounds = new Bounds(polygon[0], Vector3.zero);
            for (int i = 1; i < polygon.Count; i++) bounds.Encapsulate(polygon[i]);
            return bounds;
        }

        private static bool Overlaps(Bounds polygon, Bounds road) =>
            Mathf.Abs(polygon.center.y - road.center.y) < .15f &&
            polygon.min.x < road.max.x - Epsilon && polygon.max.x > road.min.x + Epsilon &&
            polygon.min.z < road.max.z - Epsilon && polygon.max.z > road.min.z + Epsilon;

        // Each outside half-plane is final; only the remainder advances to the next edge.
        private static void Subtract(List<Vector3> polygon, Vector3[] road, List<List<Vector3>> output)
        {
            float orientation = Mathf.Sign(SignedArea(road));
            var inside = polygon;
            for (int i = 0; i < road.Length && inside.Count >= 3; i++)
            {
                Vector3 a = road[i], b = road[(i + 1) % road.Length];
                var outside = Clip(inside, a, b, orientation, false);
                if (outside.Count >= 3 && Area(outside) > Epsilon) output.Add(outside);
                inside = Clip(inside, a, b, orientation, true);
            }
        }

        private static List<Vector3> Clip(List<Vector3> polygon, Vector3 a, Vector3 b, float orientation, bool keepInside)
        {
            var result = new List<Vector3>();
            float Distance(Vector3 p) => orientation * ((b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x));
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 p = polygon[i], q = polygon[(i + 1) % polygon.Count];
                float dp = Distance(p), dq = Distance(q);
                bool pin = keepInside ? dp >= 0 : dp <= 0;
                bool qin = keepInside ? dq >= 0 : dq <= 0;
                if (pin) result.Add(p);
                if (pin != qin) result.Add(Vector3.LerpUnclamped(p, q, dp / (dp - dq)));
            }
            return result;
        }

        private static float Area(List<Vector3> polygon) => Mathf.Abs(SignedArea(polygon));
        private static float SignedArea(IReadOnlyList<Vector3> polygon)
        {
            float area = 0;
            Vector3 origin = polygon[0];
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i] - origin; var b = polygon[(i + 1) % polygon.Count] - origin;
                area += a.x * b.z - b.x * a.z;
            }
            return area * .5f;
        }
    }
}
