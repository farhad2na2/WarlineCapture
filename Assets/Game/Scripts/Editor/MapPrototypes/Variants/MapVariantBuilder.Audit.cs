using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    [Serializable]
    internal sealed class MapVariantAuditReport
    {
        public string mapId;
        public int seed;
        public int placements;
        public int reservedPlacements;
        public int attachedPlacements;
        public int roadCells;
        public int rejectedCandidates;
        public int floating;
        public int buried;
        public int detachedParts;
        public int unsupportedAttachments;
        public int overlaps;
        public int roadIntrusions;
        public int transformDrift;
        public int outsideWorld;
        public List<string> issues = new();

        public bool Passed =>
            floating == 0 && buried == 0 && detachedParts == 0 && unsupportedAttachments == 0 &&
            overlaps == 0 && roadIntrusions == 0 && transformDrift == 0 && outsideWorld == 0;
    }

    internal sealed partial class MapVariantBuilder
    {
        private const float FloatTolerance = 0.1f;
        private const float BuryTolerance = 1.2f;
        private const float OverlapShrink = 0.15f;
        private const int MaxIssueLines = 60;

        public MapVariantAuditReport Audit()
        {
            var report = new MapVariantAuditReport
            {
                mapId = MapId,
                seed = Seed,
                placements = Placements.Count,
                reservedPlacements = Placements.Count(p => p.Reserved),
                attachedPlacements = Placements.Count(p => p.Attached),
                roadCells = RoadCells.Count,
                rejectedCandidates = Rejected
            };

            var actualBounds = new Dictionary<MapVariantPlacement, Bounds>();
            foreach (MapVariantPlacement p in Placements)
            {
                if (p.Instance == null || !TryGetRenderBounds(p.Instance, out Bounds b))
                {
                    Issue(report, ref report.transformDrift, $"missing-instance {p.PrefabPath}");
                    continue;
                }

                actualBounds[p] = b;
                if (p.Backdrop)
                    continue;

                bool axisAligned = Mathf.Abs(Mathf.DeltaAngle(p.Yaw, Mathf.Round(p.Yaw / 90f) * 90f)) < 0.5f;
                if (axisAligned && Vector2.Distance(new Vector2(b.center.x, b.center.z), p.Center) > 0.25f)
                    Issue(report, ref report.transformDrift, $"drift {Describe(p)} actual={b.center}");

                if (!p.Attached && !World.Overlaps(new Rect(b.min.x, b.min.z, b.size.x, b.size.z)))
                    Issue(report, ref report.outsideWorld, $"outside {Describe(p)}");

                var footprint = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
                Height.SampleRange(footprint, out float groundMin, out float groundMax);
                if (p.Afloat)
                {
                    float water = WaterLevel ?? float.NegativeInfinity;
                    if (groundMax > water - 0.3f || b.min.y > water || b.max.y < water)
                        Issue(report, ref report.floating, $"afloat {Describe(p)} ground={groundMax:F2} hull={b.min.y:F2}..{b.max.y:F2}");
                }
                else if (p.Spanning)
                {
                    float support = Mathf.Max(WaterLevel ?? float.NegativeInfinity, groundMin);
                    if (b.min.y > support + FloatTolerance)
                        Issue(report, ref report.floating, $"span {Describe(p)} gap={b.min.y - support:F2}");
                }
                else if (!p.Attached)
                {
                    if (b.min.y > groundMin + FloatTolerance)
                        Issue(report, ref report.floating, $"floating {Describe(p)} gap={b.min.y - groundMin:F2}");
                    if (b.min.y < groundMin - Mathf.Max(BuryTolerance, p.FoundationDepth + 0.1f))
                        Issue(report, ref report.buried, $"buried {Describe(p)} depth={groundMin - b.min.y:F2}");
                }

                if (!AllPartsSupported(p, p.Afloat || p.Spanning ? b.min.y : groundMin))
                    Issue(report, ref report.detachedParts, $"detached-part {Describe(p)}");
            }

            foreach (MapVariantPlacement p in Placements.Where(p => p.Attached))
            {
                if (!actualBounds.TryGetValue(p, out Bounds b))
                    continue;
                bool supported = actualBounds.Any(kv =>
                    kv.Key != p &&
                    kv.Value.min.x - 0.05f <= b.center.x && kv.Value.max.x + 0.05f >= b.center.x &&
                    kv.Value.min.z - 0.05f <= b.center.z && kv.Value.max.z + 0.05f >= b.center.z &&
                    Mathf.Abs(kv.Value.max.y - b.min.y) <= 0.2f);
                if (!supported)
                    Issue(report, ref report.unsupportedAttachments, $"unsupported {Describe(p)}");
            }

            AuditOverlaps(report);
            AuditRoadIntrusions(report);
            return report;
        }

        private void AuditOverlaps(MapVariantAuditReport report)
        {
            const float bucket = 24f;
            var buckets = new Dictionary<Vector2Int, List<MapVariantPlacement>>();
            List<MapVariantPlacement> reserved = Placements.Where(p => p.Reserved && !p.Backdrop && !p.Attached && !p.Spanning).ToList();
            foreach (MapVariantPlacement p in reserved)
            {
                Rect r = FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f);
                for (int z = Mathf.FloorToInt(r.yMin / bucket); z <= Mathf.FloorToInt(r.yMax / bucket); z++)
                for (int x = Mathf.FloorToInt(r.xMin / bucket); x <= Mathf.FloorToInt(r.xMax / bucket); x++)
                {
                    var key = new Vector2Int(x, z);
                    if (!buckets.TryGetValue(key, out List<MapVariantPlacement> list))
                        buckets[key] = list = new List<MapVariantPlacement>();
                    list.Add(p);
                }
            }

            var seen = new HashSet<(MapVariantPlacement, MapVariantPlacement)>();
            foreach (List<MapVariantPlacement> list in buckets.Values)
            {
                for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    MapVariantPlacement a = list[i];
                    MapVariantPlacement b = list[j];
                    if (a.Group != 0 && a.Group == b.Group)
                        continue;
                    if (!seen.Add((a, b)))
                        continue;
                    if (ObbIntersects(a.Center, Shrink(a.HalfSize), a.Yaw, b.Center, Shrink(b.HalfSize), b.Yaw))
                        Issue(report, ref report.overlaps, $"overlap {Describe(a)} x {Describe(b)}");
                }
            }
        }

        private void AuditRoadIntrusions(MapVariantAuditReport report)
        {
            foreach (MapVariantPlacement p in Placements.Where(p => p.Reserved && !p.Backdrop && !p.Attached && !p.Spanning))
            {
                Rect r = FootprintAabb(p.Center, p.HalfSize, p.Yaw, 0f);
                Vector2Int min = RoadCell(r.min);
                Vector2Int max = RoadCell(r.max);
                for (int z = min.y; z <= max.y; z++)
                for (int x = min.x; x <= max.x; x++)
                {
                    var cell = new Vector2Int(x, z);
                    if (!RoadCells.ContainsKey(cell))
                        continue;
                    Rect road = RoadCellRect(cell);
                    if (ObbIntersects(p.Center, Shrink(p.HalfSize), p.Yaw, road.center, road.size * 0.5f, 0f))
                    {
                        Issue(report, ref report.roadIntrusions, $"on-road {Describe(p)} cell={cell}");
                        goto next;
                    }
                }

                next: ;
            }
        }

        // Every connected cluster of renderers inside one prefab must reach the ground or rest on another cluster.
        private static bool AllPartsSupported(MapVariantPlacement p, float groundMin)
        {
            List<Bounds> parts = p.Instance.GetComponentsInChildren<Renderer>(false)
                .Where(IsGeometryRenderer)
                .Select(r => r.bounds)
                .ToList();
            if (parts.Count <= 1)
                return true;

            float baseY = p.Attached ? parts.Min(b => b.min.y) : groundMin;
            var supported = new bool[parts.Count];
            var queue = new Queue<int>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].min.y <= baseY + 0.25f)
                {
                    supported[i] = true;
                    queue.Enqueue(i);
                }
            }

            while (queue.Count > 0)
            {
                Bounds current = parts[queue.Dequeue()];
                current.Expand(0.3f);
                for (int i = 0; i < parts.Count; i++)
                {
                    if (supported[i] || !current.Intersects(parts[i]))
                        continue;
                    supported[i] = true;
                    queue.Enqueue(i);
                }
            }

            return supported.All(s => s);
        }

        private static Vector2 Shrink(Vector2 half) =>
            new(Mathf.Max(0.01f, half.x - OverlapShrink), Mathf.Max(0.01f, half.y - OverlapShrink));

        private static bool ObbIntersects(Vector2 ca, Vector2 ha, float yawA, Vector2 cb, Vector2 hb, float yawB)
        {
            Vector2[] axes =
            {
                Axis(yawA, 0), Axis(yawA, 1), Axis(yawB, 0), Axis(yawB, 1)
            };
            Vector2 d = cb - ca;
            foreach (Vector2 axis in axes)
            {
                float ra = Project(ha, yawA, axis);
                float rb = Project(hb, yawB, axis);
                if (Mathf.Abs(Vector2.Dot(d, axis)) > ra + rb)
                    return false;
            }

            return true;
        }

        // Unity yaw maps local +X to (cos, -sin) and local +Z to (sin, cos) in world XZ.
        private static Vector2 Axis(float yaw, int index)
        {
            float r = yaw * Mathf.Deg2Rad;
            return index == 0 ? new Vector2(Mathf.Cos(r), -Mathf.Sin(r)) : new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        private static float Project(Vector2 half, float yaw, Vector2 axis) =>
            half.x * Mathf.Abs(Vector2.Dot(Axis(yaw, 0), axis)) + half.y * Mathf.Abs(Vector2.Dot(Axis(yaw, 1), axis));

        private static string Describe(MapVariantPlacement p) =>
            $"{System.IO.Path.GetFileNameWithoutExtension(p.PrefabPath)}@({p.Center.x:F1},{p.Center.y:F1})";

        private static void Issue(MapVariantAuditReport report, ref int counter, string line)
        {
            counter++;
            if (report.issues.Count < MaxIssueLines)
                report.issues.Add(line);
        }
    }
}
