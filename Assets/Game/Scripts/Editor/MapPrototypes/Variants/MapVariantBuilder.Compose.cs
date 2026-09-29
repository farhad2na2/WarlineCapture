using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    internal enum MapVariantSide
    {
        South,
        East,
        North,
        West
    }

    internal readonly struct MapVariantGate
    {
        public readonly MapVariantSide Side;
        public readonly float Center;
        public readonly float Width;

        public MapVariantGate(MapVariantSide side, float center, float width)
        {
            Side = side;
            Center = center;
            Width = width;
        }
    }

    internal sealed partial class MapVariantBuilder
    {
        // Synty building prefabs face their local -Z; buildings are rotated so that face points at the street.
        public const float BuildingFrontYawOffset = 180f;

        public static float YawFacing(Vector2 direction) =>
            Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + BuildingFrontYawOffset;

        public int CityBlock(
            Rect block,
            IReadOnlyList<string> frontage,
            IReadOnlyList<string> infill,
            float setback = 0.8f,
            float gap = 0.6f,
            float palmChance = 0.35f,
            MapVariantSide[] streetSides = null)
        {
            streetSides ??= new[] { MapVariantSide.South, MapVariantSide.East, MapVariantSide.North, MapVariantSide.West };
            int placed = 0;
            foreach (MapVariantSide side in streetSides)
                placed += Frontage(block, side, frontage, setback, gap);

            Rect inner = Inset(block, 6f);
            if (inner.width > 4f && inner.height > 4f)
            {
                placed += Scatter(infill, MapVariantLayer.City, inner, Mathf.CeilToInt(inner.width * inner.height / 60f),
                    PlaceOptions.Structure.WithPadding(0.8f), freeYaw: false);
                if (palmChance > 0f)
                    placed += Scatter(MapVariantKits.Palms, MapVariantLayer.Vegetation, inner,
                        Mathf.CeilToInt(inner.width * inner.height / 220f * palmChance),
                        PlaceOptions.Vegetation, minScale: 0.9f, maxScale: 1.2f);
            }

            return placed;
        }

        public int Frontage(Rect block, MapVariantSide side, IReadOnlyList<string> prefabs, float setback, float gap)
        {
            EdgeFrame(block, side, out Vector2 start, out Vector2 along, out Vector2 inward, out float length);
            float yaw = YawFacing(-inward);
            int placed = 0;
            float t = 0f;
            int misses = 0;
            while (t < length && misses < 40)
            {
                string prefab = Pick(prefabs);
                Vector2 size = Footprint(prefab);
                float width = size.x;
                float depth = size.y;
                if (t + width > length)
                {
                    misses++;
                    t += 1.5f;
                    continue;
                }

                Vector2 center = start + along * (t + width * 0.5f) + inward * (setback + depth * 0.5f);
                if (Place(prefab, MapVariantLayer.City, center, yaw, PlaceOptions.Structure.WithPadding(gap * 0.5f)) != null)
                {
                    placed++;
                    t += width + gap;
                    misses = 0;
                }
                else
                {
                    misses++;
                    t += 1.5f;
                }
            }

            return placed;
        }

        public int FencedCompound(Rect rect, string fencePrefab, params MapVariantGate[] gates)
        {
            int placed = 0;
            foreach (MapVariantSide side in new[] { MapVariantSide.South, MapVariantSide.East, MapVariantSide.North, MapVariantSide.West })
            {
                EdgeFrame(rect, side, out Vector2 start, out Vector2 along, out Vector2 _, out float length);
                var cuts = new List<(float from, float to)>();
                foreach (MapVariantGate gate in gates)
                {
                    if (gate.Side == side)
                        cuts.Add((gate.Center - gate.Width * 0.5f, gate.Center + gate.Width * 0.5f));
                }

                cuts.Sort((a, b) => a.from.CompareTo(b.from));
                float cursor = 0f;
                foreach ((float from, float to) in cuts)
                {
                    if (from > cursor + 1f)
                        placed += PlaceLine(fencePrefab, MapVariantLayer.Props, start + along * cursor, start + along * from, PlaceOptions.Prop);
                    cursor = Mathf.Max(cursor, to);
                }

                if (length > cursor + 1f)
                    placed += PlaceLine(fencePrefab, MapVariantLayer.Props, start + along * cursor, start + along * length, PlaceOptions.Prop);
            }

            return placed;
        }

        public int Grid(string prefab, MapVariantLayer layer, Rect area, int columns, int rows, float yaw, PlaceOptions options)
        {
            int placed = 0;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                var center = new Vector2(
                    Mathf.Lerp(area.xMin, area.xMax, (c + 0.5f) / columns),
                    Mathf.Lerp(area.yMin, area.yMax, (r + 0.5f) / rows));
                if (Place(prefab, layer, center, yaw, options) != null)
                    placed++;
            }

            return placed;
        }

        public int Row(
            IReadOnlyList<string> prefabs,
            MapVariantLayer layer,
            Vector2 from,
            Vector2 to,
            float spacing,
            PlaceOptions options,
            float jitter = 0f,
            bool freeYaw = true,
            float fixedYaw = 0f)
        {
            float length = Vector2.Distance(from, to);
            int count = Mathf.Max(1, Mathf.FloorToInt(length / spacing) + 1);
            Vector2 side = Vector2.Perpendicular((to - from).normalized);
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, count == 1 ? 0.5f : i / (float)(count - 1)) + side * Range(-jitter, jitter);
                float yaw = freeYaw ? Range(0f, 360f) : fixedYaw;
                if (Place(Pick(prefabs), layer, p, yaw, options) != null)
                    placed++;
            }

            return placed;
        }

        // Rings a rect with mountains that sink into the terrain as a skyline; they are scenery, not gameplay.
        public int MountainBackdrop(Rect band, int count, float minScale, float maxScale)
        {
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                var p = new Vector2(Range(band.xMin, band.xMax), Range(band.yMin, band.yMax));
                if (Place(Pick(MapVariantKits.Mountains), MapVariantLayer.Backdrop, p, Range(0f, 360f),
                        PlaceOptions.Scenery.WithScale(Range(minScale, maxScale)).WithSink(4f)) != null)
                    placed++;
            }

            return placed;
        }

        // Final pass that fills leftover open sand with the Demo scene's dressing: dune lips, low mounds,
        // grass clumps, pebbles and flat rocks. Paved pads stay clean; big dunes only frame the playable area.
        public int DesertDressing(Rect area, float density = 1f)
        {
            var pads = new List<Rect>();
            foreach (MapVariantPlacement placement in Placements)
                if (placement.Surface)
                    pads.Add(new Rect(placement.Center - placement.HalfSize - Vector2.one, placement.HalfSize * 2f + Vector2.one * 2f));
            bool OpenSand(Vector2 p)
            {
                foreach (Rect pad in pads)
                    if (pad.Contains(p))
                        return false;
                return true;
            }

            int Count(float squareMetresPerPiece) => Mathf.RoundToInt(area.width * area.height * density / squareMetresPerPiece);
            int placed = 0;
            placed += Scatter(MapVariantKits.SandDunes, MapVariantLayer.Vegetation, area, Count(40000f),
                PlaceOptions.Vegetation.WithGroundDelta(3f).WithSink(0.4f), minScale: 0.7f, maxScale: 1.1f,
                accept: p => !Playable.Contains(p) && OpenSand(p));
            placed += Scatter(MapVariantKits.SandMounds, MapVariantLayer.Vegetation, area, Count(9000f),
                PlaceOptions.Vegetation.WithGroundDelta(0.6f).WithPadding(0.5f), minScale: 0.8f, maxScale: 1.4f, accept: OpenSand);
            placed += Scatter(MapVariantKits.SandEdges, MapVariantLayer.Vegetation, area, Count(900f),
                PlaceOptions.Vegetation.WithGroundDelta(0.6f).WithPadding(0.2f), minScale: 0.8f, maxScale: 1.5f, accept: OpenSand);
            placed += Scatter(MapVariantKits.FlatRocks, MapVariantLayer.Vegetation, area, Count(7000f),
                PlaceOptions.Vegetation.WithGroundDelta(1f), minScale: 0.4f, maxScale: 1f, accept: OpenSand);
            placed += Scatter(MapVariantKits.Pebbles, MapVariantLayer.Vegetation, area, Count(500f),
                PlaceOptions.Vegetation.WithPadding(0.1f), minScale: 0.8f, maxScale: 1.6f, accept: OpenSand);
            placed += Scatter(MapVariantKits.GrassClumps, MapVariantLayer.Vegetation, area, Count(160f),
                PlaceOptions.Vegetation.WithPadding(0.05f).WithSink(0.05f), minScale: 0.8f, maxScale: 1.5f, accept: OpenSand);
            return placed;
        }

        public static Rect Inset(Rect rect, float amount) =>
            new(rect.xMin + amount, rect.yMin + amount, Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));

        public static Rect FromMinMax(float xMin, float zMin, float xMax, float zMax) =>
            Rect.MinMaxRect(xMin, zMin, xMax, zMax);

        private static void EdgeFrame(Rect rect, MapVariantSide side, out Vector2 start, out Vector2 along, out Vector2 inward, out float length)
        {
            switch (side)
            {
                case MapVariantSide.South:
                    start = new Vector2(rect.xMin, rect.yMin); along = Vector2.right; inward = Vector2.up; length = rect.width;
                    break;
                case MapVariantSide.North:
                    start = new Vector2(rect.xMax, rect.yMax); along = Vector2.left; inward = Vector2.down; length = rect.width;
                    break;
                case MapVariantSide.East:
                    start = new Vector2(rect.xMax, rect.yMin); along = Vector2.up; inward = Vector2.left; length = rect.height;
                    break;
                case MapVariantSide.West:
                    start = new Vector2(rect.xMin, rect.yMax); along = Vector2.down; inward = Vector2.right; length = rect.height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }
    }
}
