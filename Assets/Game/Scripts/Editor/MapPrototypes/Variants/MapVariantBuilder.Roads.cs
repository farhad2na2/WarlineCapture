using System;
using System.Collections.Generic;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    internal enum MapVariantRoadKind
    {
        Asphalt,
        Dirt,
        DirtSidewalk
    }

    internal sealed partial class MapVariantBuilder
    {
        public const float RoadGridSize = 10f;
        private const int RoadChunkSize = 16;

        private static readonly Dictionary<MapVariantRoadKind, string> RoadFolders = new()
        {
            { MapVariantRoadKind.Asphalt, "Assets/Game/Prefabs/Roads/Road_Asphalt_With_Sidewalk/Road_Asphalt_With_Sidewalk" },
            { MapVariantRoadKind.Dirt, "Assets/Game/Prefabs/Roads/Road_Dirt/Road_Dirt" },
            { MapVariantRoadKind.DirtSidewalk, "Assets/Game/Prefabs/Roads/Road_Dirt_With_Sidewalk/Road_Dirt_With_Sidewalk" }
        };

        public Dictionary<Vector2Int, MapVariantRoadKind> RoadCells { get; } = new();

        // Road cells carried by a bridge: they connect neighbouring tiles but keep the water carved beneath.
        public Dictionary<Vector2Int, float> BridgeCells { get; } = new();

        private const string BridgeDeckA = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Bridge_01.prefab";
        private const string BridgeDeckB = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Bridge_02.prefab";
        private const string BridgeSupport = "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Bridge_Support_01.prefab";

        public void AddBridge(MapVariantRoadKind kind, float deckLevel, Vector2 from, Vector2 to)
        {
            AddRoad(kind, from, to);
            Vector2Int a = RoadCell(from);
            Vector2Int b = RoadCell(to);
            Vector2Int step = new(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
            for (Vector2Int cell = a; ; cell += step)
            {
                BridgeCells[cell] = deckLevel;
                if (cell == b)
                    break;
            }
        }

        public Vector2Int RoadCell(Vector2 world) => new(
            Mathf.FloorToInt((world.x - World.xMin) / RoadGridSize),
            Mathf.FloorToInt((world.y - World.yMin) / RoadGridSize));

        public Rect RoadCellRect(Vector2Int cell) => new(
            World.xMin + cell.x * RoadGridSize,
            World.yMin + cell.y * RoadGridSize,
            RoadGridSize,
            RoadGridSize);

        public Vector2 RoadCellCenter(Vector2Int cell) => RoadCellRect(cell).center;

        public void AddRoad(MapVariantRoadKind kind, params Vector2[] worldPoints)
        {
            for (int i = 0; i + 1 < worldPoints.Length; i++)
            {
                Vector2Int a = RoadCell(worldPoints[i]);
                Vector2Int b = RoadCell(worldPoints[i + 1]);
                if (a.x != b.x && a.y != b.y)
                    throw new ArgumentException($"[MapVariants] Road segment {a}->{b} must be axis aligned.");
                Vector2Int step = new(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
                Vector2Int cell = a;
                while (true)
                {
                    if (!RoadCells.ContainsKey(cell) || kind == MapVariantRoadKind.Asphalt)
                        RoadCells[cell] = kind;
                    if (cell == b)
                        break;
                    cell += step;
                }
            }
        }

        public bool IsRoad(Vector2 world) => RoadCells.ContainsKey(RoadCell(world));

        public float DistanceToRoad(Vector2 world, int searchCells = 4)
        {
            Vector2Int c = RoadCell(world);
            float best = float.PositiveInfinity;
            for (int dz = -searchCells; dz <= searchCells; dz++)
            for (int dx = -searchCells; dx <= searchCells; dx++)
            {
                var cell = new Vector2Int(c.x + dx, c.y + dz);
                if (!RoadCells.ContainsKey(cell))
                    continue;
                Rect r = RoadCellRect(cell);
                float ex = Mathf.Max(r.xMin - world.x, 0f, world.x - r.xMax);
                float ez = Mathf.Max(r.yMin - world.y, 0f, world.y - r.yMax);
                best = Mathf.Min(best, Mathf.Sqrt(ex * ex + ez * ez));
            }

            return best;
        }

        // Roads sit on a flat bed at the height the terrain had under the road before flattening.
        public void FlattenUnderRoads(float blendDistance, Func<float, float, float, bool> protect = null)
        {
            var levels = new Dictionary<Vector2Int, float>();
            foreach (Vector2Int cell in RoadCells.Keys)
            {
                if (BridgeCells.ContainsKey(cell))
                    continue;
                Vector2 c = RoadCellCenter(cell);
                levels[cell] = Height.Sample(c.x, c.y);
            }

            int search = Mathf.CeilToInt(blendDistance / RoadGridSize) + 1;
            Height.Apply((x, z, h) =>
            {
                if (protect != null && protect(x, z, h))
                    return h;
                Vector2Int cell = RoadCell(new Vector2(x, z));
                float bestDistance = float.PositiveInfinity;
                float level = h;
                for (int dz = -search; dz <= search; dz++)
                for (int dx = -search; dx <= search; dx++)
                {
                    var n = new Vector2Int(cell.x + dx, cell.y + dz);
                    if (!levels.TryGetValue(n, out float roadLevel))
                        continue;
                    Rect r = RoadCellRect(n);
                    float ex = Mathf.Max(r.xMin - 1f - x, 0f, x - r.xMax - 1f);
                    float ez = Mathf.Max(r.yMin - 1f - z, 0f, z - r.yMax - 1f);
                    float d = Mathf.Sqrt(ex * ex + ez * ez);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        level = roadLevel;
                    }
                }

                if (bestDistance <= 0f)
                    return level;
                if (bestDistance >= blendDistance)
                    return h;
                float t = Mathf.SmoothStep(0f, 1f, bestDistance / blendDistance);
                return Mathf.Lerp(level, h, t);
            });
        }

        public int BuildRoads()
        {
            var kinds = new Dictionary<MapVariantRoadKind, (Unity.Entities.World world, RoadVisualVariantSystem variants, RoadVisualVariantSystem.Prefabs prefabs)>();
            try
            {
                foreach (MapVariantRoadKind kind in new HashSet<MapVariantRoadKind>(RoadCells.Values))
                {
                    var world = new Unity.Entities.World($"MapVariantRoads_{kind}");
                    RoadVisualVariantSystem variants = world.CreateSystemManaged<RoadVisualVariantSystem>();
                    RoadVisualVariantSystem.Prefabs prefabs = LoadRoadPrefabs(kind);
                    variants.CacheVariants(prefabs);
                    kinds.Add(kind, (world, variants, prefabs));
                }

                var emptyTiles = new Dictionary<Vector2Int, RoadNetworkCompositionSystemHelper.RoadTileData>();
                var emptyCells = new HashSet<Vector2Int>();
                var chunks = new Dictionary<Vector2Int, Transform>();
                int built = 0;
                foreach (KeyValuePair<Vector2Int, MapVariantRoadKind> entry in RoadCells)
                {
                    Vector2Int cell = entry.Key;
                    if (BridgeCells.TryGetValue(cell, out float deckLevel))
                    {
                        BuildBridgeCell(cell, deckLevel);
                        continue;
                    }

                    var (_, variants, prefabs) = kinds[entry.Value];
                    var mask = new RoadNetworkCompositionSystemHelper.TileConnectionMask(
                        RoadCells.ContainsKey(cell + Vector2Int.up),
                        RoadCells.ContainsKey(cell + Vector2Int.right),
                        RoadCells.ContainsKey(cell + Vector2Int.down),
                        RoadCells.ContainsKey(cell + Vector2Int.left));
                    RoadNetworkCompositionSystemHelper.RoadVisualType type = ResolveRoadType(mask);
                    GameObject prefab = variants.GetPrefab(prefabs, type);
                    if (prefab == null || !variants.TryGetVariant(type, mask, out RoadVisualVariantSystem.VariantData variant))
                        throw new InvalidOperationException($"[MapVariants] No {entry.Value} road variant for {type} at {cell}.");

                    Vector2Int chunkKey = new(Mathf.FloorToInt(cell.x / (float)RoadChunkSize), Mathf.FloorToInt(cell.y / (float)RoadChunkSize));
                    if (!chunks.TryGetValue(chunkKey, out Transform chunk))
                    {
                        chunk = new GameObject($"RoadChunk_{chunkKey.x}_{chunkKey.y}").transform;
                        chunk.SetParent(Layer(MapVariantLayer.Roads), false);
                        chunks.Add(chunkKey, chunk);
                    }

                    Vector2 center = RoadCellCenter(cell);
                    float level = Height.Sample(center.x, center.y);
                    var context = new RoadChunkVisualSystem.Context(
                        emptyTiles,
                        variants.VisualData,
                        emptyCells,
                        emptyCells,
                        chunk,
                        new Vector3(World.xMin, level + 0.02f, World.yMin),
                        level + 0.02f,
                        RoadGridSize,
                        RoadChunkSize);
                    Vector3 position = RoadChunkVisualSystem.GetPlacementPosition(context, cell, variant);
                    var road = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chunk);
                    road.transform.SetPositionAndRotation(position, variant.Rotation);
                    road.transform.localScale = variant.Scale;
                    GameObjectUtility.SetStaticEditorFlags(road, StaticEditorFlags.BatchingStatic);
                    Occupy(RoadCellRect(cell));
                    built++;
                }

                return built;
            }
            finally
            {
                foreach (var entry in kinds.Values)
                {
                    entry.variants.DisposeCachedVisualData();
                    entry.world.Dispose();
                }
            }
        }

        private void BuildBridgeCell(Vector2Int cell, float deckLevel)
        {
            bool eastWest = RoadCells.ContainsKey(cell + Vector2Int.left) || RoadCells.ContainsKey(cell + Vector2Int.right);
            Vector2 along = eastWest ? Vector2.right : Vector2.up;
            float yaw = eastWest ? 90f : 0f;
            Vector2 center = RoadCellCenter(cell);
            // The deck pivot is the driving surface; the railings extend above it.
            float deckPivot = deckLevel + 0.08f;
            for (int i = 0; i < 2; i++)
            {
                string deck = (cell.x + cell.y + i) % 2 == 0 ? BridgeDeckA : BridgeDeckB;
                PlaceAt(deck, MapVariantLayer.Roads, center + along * (i == 0 ? -2.5f : 2.5f), yaw, deckPivot, false,
                    deckLevel, reserve: false);
            }

            PlaceAt(BridgeSupport, MapVariantLayer.Roads, center, yaw, deckPivot, false, deckLevel, reserve: false);
            Occupy(RoadCellRect(cell));
        }

        private static RoadNetworkCompositionSystemHelper.RoadVisualType ResolveRoadType(
            RoadNetworkCompositionSystemHelper.TileConnectionMask mask)
        {
            return mask.Count switch
            {
                0 => RoadNetworkCompositionSystemHelper.RoadVisualType.End,
                1 => RoadNetworkCompositionSystemHelper.RoadVisualType.End,
                2 when (mask.North && mask.South) || (mask.East && mask.West) =>
                    RoadNetworkCompositionSystemHelper.RoadVisualType.Straight,
                2 => RoadNetworkCompositionSystemHelper.RoadVisualType.Corner,
                3 => RoadNetworkCompositionSystemHelper.RoadVisualType.TIntersection,
                _ => RoadNetworkCompositionSystemHelper.RoadVisualType.Intersection
            };
        }

        private static RoadVisualVariantSystem.Prefabs LoadRoadPrefabs(MapVariantRoadKind kind)
        {
            string stem = RoadFolders[kind];
            GameObject Load(string suffix) =>
                AssetDatabase.LoadAssetAtPath<GameObject>($"{stem}_{suffix}.prefab") ??
                throw new InvalidOperationException($"[MapVariants] Missing road prefab {stem}_{suffix}.prefab");

            return new RoadVisualVariantSystem.Prefabs(
                Load("End"),
                Load("Straight"),
                Load("Corner"),
                Load("T_Intersection"),
                Load("Intersection"),
                null,
                null);
        }
    }
}
