using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    internal enum MapVariantLayer
    {
        Ground,
        Roads,
        City,
        Industrial,
        Military,
        Props,
        Vehicles,
        Vegetation,
        Backdrop,
        Zones
    }

    internal struct PlaceOptions
    {
        public float Padding;
        public float MaxGroundDelta;
        public float Sink;
        public float Scale;
        public bool Reserve;
        public bool IgnoreOccupancy;
        public bool Backdrop;
        public bool AnchorBounds;
        public int Group;

        public static PlaceOptions Structure => new()
        {
            Padding = 0.6f, MaxGroundDelta = 0.35f, Sink = 0.06f, Scale = 1f, Reserve = true
        };

        public static PlaceOptions Prop => new()
        {
            Padding = 0.25f, MaxGroundDelta = 0.3f, Sink = 0.03f, Scale = 1f, Reserve = true
        };

        public static PlaceOptions Vegetation => new()
        {
            Padding = 0.3f, MaxGroundDelta = 0.9f, Sink = 0.12f, Scale = 1f, Reserve = true
        };

        public static PlaceOptions Scenery => new()
        {
            Padding = 0f, MaxGroundDelta = 99f, Sink = 0.5f, Scale = 1f, Reserve = true, Backdrop = true
        };

        public PlaceOptions WithScale(float scale) { Scale = scale; return this; }
        public PlaceOptions WithGroup(int group) { Group = group; return this; }
        public PlaceOptions WithPadding(float padding) { Padding = padding; return this; }
        public PlaceOptions WithSink(float sink) { Sink = sink; return this; }
        public PlaceOptions WithGroundDelta(float delta) { MaxGroundDelta = delta; return this; }
        public PlaceOptions WithBoundsAnchor() { AnchorBounds = true; return this; }
    }

    internal sealed class MapVariantPlacement
    {
        public GameObject Instance;
        public string PrefabPath;
        public MapVariantLayer Layer;
        public Vector2 Center;
        public Vector2 HalfSize;
        public float Yaw;
        public bool Reserved;
        public bool Backdrop;
        public bool Attached;
        public bool Surface;
        public bool Afloat;
        public bool Spanning;
        public float SpanDeckTop;
        public float FoundationDepth;
        public int Group;
        // Explicit provenance for separately placed roof details; group numbers are not ownership.
        public MapVariantPlacement Support;
    }

    internal sealed class MapVariantPrefabInfo
    {
        public GameObject Prefab;
        public Material Material;
        public (GameObject prefab, Vector3 offset, Material material)[] Attachments =
            Array.Empty<(GameObject, Vector3, Material)>();
        public Bounds LocalBounds;
    }

    internal sealed partial class MapVariantBuilder
    {
        private const float OccupancyCell = 0.5f;

        private readonly Dictionary<string, MapVariantPrefabInfo> _prefabInfo = new(StringComparer.Ordinal);
        private readonly Dictionary<MapVariantLayer, Transform> _layers = new();
        private readonly bool[] _occupied;
        private readonly int _occupancyX;
        private readonly int _occupancyZ;
        private int _nextGroup = 1;

        public MapVariantBuilder(string mapId, Rect world, Rect playable, float groundCellSize, int seed)
        {
            MapId = mapId;
            World = world;
            Playable = playable;
            Random = new System.Random(seed);
            Seed = seed;
            Height = new MapVariantHeightField(world.min, world.size, groundCellSize);
            _occupancyX = Mathf.CeilToInt(world.width / OccupancyCell);
            _occupancyZ = Mathf.CeilToInt(world.height / OccupancyCell);
            _occupied = new bool[_occupancyX * _occupancyZ];
            Root = new GameObject($"Map_{mapId}").transform;
            foreach (MapVariantLayer layer in Enum.GetValues(typeof(MapVariantLayer)))
            {
                var child = new GameObject(layer.ToString()).transform;
                child.SetParent(Root, false);
                _layers.Add(layer, child);
            }
        }

        public string MapId { get; }
        public float? WaterLevel { get; set; }
        public Rect World { get; }
        public Rect Playable { get; }
        public int Seed { get; }
        public System.Random Random { get; }
        public MapVariantHeightField Height { get; }
        public Transform Root { get; }
        public List<MapVariantPlacement> Placements { get; } = new();
        public int Rejected { get; private set; }
        // Presentation passes place art that must stay out of the placement inventory and its hashes.
        public bool RecordPlacements { get; set; } = true;

        public Transform Layer(MapVariantLayer layer) => _layers[layer];

        public int NewGroup() => _nextGroup++;

        public float Range(float min, float max) => min + (float)Random.NextDouble() * (max - min);

        public int RangeInt(int minInclusive, int maxExclusive) => Random.Next(minInclusive, maxExclusive);

        public T Pick<T>(IReadOnlyList<T> items) => items[Random.Next(items.Count)];

        public bool Chance(float probability) => Random.NextDouble() < probability;

        public MapVariantPrefabInfo Info(string prefabPath)
        {
            if (_prefabInfo.TryGetValue(prefabPath, out MapVariantPrefabInfo info))
                return info;

            // "Base~mat|Child~mat@x,y,z" composes kit pieces that Synty ships separately (truck + cargo,
            // support + pipe); "~mat" swaps in one of the pack's alternate colour atlases.
            string[] parts = prefabPath.Split('|');
            ParsePart(parts[0], out GameObject basePrefab, out Material baseMaterial, out Vector3 _);
            info = new MapVariantPrefabInfo { Prefab = basePrefab, Material = baseMaterial };
            var attachments = new List<(GameObject, Vector3, Material)>();
            for (int i = 1; i < parts.Length; i++)
            {
                ParsePart(parts[i], out GameObject prefab, out Material material, out Vector3 offset);
                attachments.Add((prefab, offset, material));
            }

            info.Attachments = attachments.ToArray();
            GameObject probe = Instantiate(info, null);
            probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            probe.transform.localScale = Vector3.one;
            if (!TryGetRenderBounds(probe, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(probe);
                throw new InvalidOperationException($"[MapVariants] Prefab '{prefabPath}' has no visible renderers.");
            }

            UnityEngine.Object.DestroyImmediate(probe);
            info.LocalBounds = bounds;
            _prefabInfo.Add(prefabPath, info);
            return info;
        }

        private static GameObject LoadPrefab(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
            throw new InvalidOperationException($"[MapVariants] Missing prefab '{path}'.");

        private static void ParsePart(string part, out GameObject prefab, out Material material, out Vector3 offset)
        {
            string[] pathAndOffset = part.Split('@');
            string[] pathAndMaterial = pathAndOffset[0].Split('~');
            prefab = LoadPrefab(pathAndMaterial[0]);
            material = pathAndMaterial.Length > 1
                ? AssetDatabase.LoadAssetAtPath<Material>(pathAndMaterial[1]) ??
                  throw new InvalidOperationException($"[MapVariants] Missing material '{pathAndMaterial[1]}'.")
                : null;
            offset = Vector3.zero;
            if (pathAndOffset.Length > 1)
            {
                string[] xyz = pathAndOffset[1].Split(',');
                offset = new Vector3(
                    float.Parse(xyz[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(xyz[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(xyz[2], System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private static GameObject Instantiate(MapVariantPrefabInfo info, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(info.Prefab, parent);
            ApplyMaterial(instance, info.Material);
            foreach ((GameObject prefab, Vector3 offset, Material material) in info.Attachments)
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, instance.transform);
                child.transform.localPosition = offset;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
                ApplyMaterial(child, material);
            }

            return instance;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            if (material == null)
                return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        public Vector2 Footprint(string prefabPath, float scale = 1f)
        {
            Bounds b = Info(prefabPath).LocalBounds;
            return new Vector2(b.size.x, b.size.z) * scale;
        }

        public MapVariantPlacement Place(
            string prefabPath,
            MapVariantLayer layer,
            Vector2 center,
            float yaw,
            PlaceOptions options)
        {
            MapVariantPrefabInfo info = Info(prefabPath);
            float scale = options.Scale <= 0f ? 1f : options.Scale;
            Vector2 half = new Vector2(info.LocalBounds.extents.x, info.LocalBounds.extents.z) * scale;
            Rect footprint = FootprintAabb(center, half, yaw, 0f);
            Rect reserved = FootprintAabb(center, half, yaw, options.Padding);
            if (options.Backdrop)
                reserved = Clip(reserved, World);

            if (!options.Backdrop && !Contains(World, footprint))
                return Reject();

            Height.SampleRange(footprint, out float groundMin, out float groundMax);
            if (groundMax - groundMin > options.MaxGroundDelta)
                return Reject();
            if (!options.Backdrop && WaterLevel.HasValue && groundMin < WaterLevel.Value + 0.15f)
                return Reject();

            if (!options.IgnoreOccupancy && !IsFree(reserved))
                return Reject();

            GameObject instance = Instantiate(info, Layer(layer));
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 localCenter = info.LocalBounds.center * scale;
            Vector3 offset = rotation * new Vector3(localCenter.x, 0f, localCenter.z);
            float localMin = info.LocalBounds.min.y * scale;
            float y = groundMin - options.Sink - localMin;
            // Prefabs authored with a foundation below the pivot keep their floor at the highest ground point,
            // as long as the foundation still reaches the lowest ground point under the footprint.
            if (!options.AnchorBounds && localMin < 0f)
                y = Mathf.Min(y, groundMax);
            instance.transform.SetPositionAndRotation(
                new Vector3(center.x - offset.x, y, center.y - offset.z),
                rotation);
            instance.transform.localScale = Vector3.one * scale;
            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic);

            if (options.Reserve)
                Occupy(reserved);

            var placement = new MapVariantPlacement
            {
                Instance = instance,
                PrefabPath = prefabPath,
                Layer = layer,
                Center = center,
                HalfSize = half,
                Yaw = yaw,
                Reserved = options.Reserve,
                Backdrop = options.Backdrop,
                FoundationDepth = Mathf.Max(0f, -localMin) + (groundMax - groundMin) + options.Sink,
                Group = options.Group
            };
            if (RecordPlacements)
                Placements.Add(placement);
            return placement;
        }

        // Explicit-height placement for pieces that are not ground-supported: hulls on the water surface,
        // bridge decks and piers carried by their own supports. The audit verifies each against its reference.
        public MapVariantPlacement PlaceAt(
            string prefabPath,
            MapVariantLayer layer,
            Vector2 center,
            float yaw,
            float pivotY,
            bool afloat,
            float deckTop = 0f,
            bool reserve = true)
        {
            MapVariantPrefabInfo info = Info(prefabPath);
            Vector2 half = new(info.LocalBounds.extents.x, info.LocalBounds.extents.z);
            Rect reserved = FootprintAabb(center, half, yaw, 0.2f);
            if (reserve && !IsFree(reserved))
                return Reject();

            GameObject instance = Instantiate(info, Layer(layer));
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 offset = rotation * new Vector3(info.LocalBounds.center.x, 0f, info.LocalBounds.center.z);
            instance.transform.SetPositionAndRotation(new Vector3(center.x - offset.x, pivotY, center.y - offset.z), rotation);
            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic);
            if (reserve)
                Occupy(reserved);
            var placement = new MapVariantPlacement
            {
                Instance = instance,
                PrefabPath = prefabPath,
                Layer = layer,
                Center = center,
                HalfSize = half,
                Yaw = yaw,
                Reserved = reserve,
                Afloat = afloat,
                Spanning = !afloat,
                SpanDeckTop = deckTop
            };
            Placements.Add(placement);
            return placement;
        }

        public MapVariantPlacement Afloat(string prefabPath, Vector2 center, float yaw, float draft)
        {
            if (!WaterLevel.HasValue)
                throw new InvalidOperationException("[MapVariants] Afloat placement requires a water level.");
            MapVariantPrefabInfo info = Info(prefabPath);
            Rect footprint = FootprintAabb(center, new Vector2(info.LocalBounds.extents.x, info.LocalBounds.extents.z), yaw, 1f);
            Height.SampleRange(footprint, out float _, out float groundMax);
            if (groundMax > WaterLevel.Value - 0.6f)
                return Reject();
            return PlaceAt(prefabPath, MapVariantLayer.Vehicles, center, yaw, WaterLevel.Value - draft - info.LocalBounds.min.y, true);
        }

        // Flush slab under yards and plants; its top sits 3 cm above the ground so props still read as grounded.
        public MapVariantPlacement Pad(Rect area, string materialPath, float yaw = 0f)
        {
            Height.SampleRange(area, out float groundMin, out float groundMax);
            if (groundMax - groundMin > 0.15f || !IsFree(area))
                return Reject();

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath) ??
                           throw new InvalidOperationException($"[MapVariants] Missing pad material {materialPath}.");
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.name = $"Pad_{Path.GetFileNameWithoutExtension(materialPath)}";
            pad.transform.SetParent(Layer(MapVariantLayer.Ground), false);
            const float thickness = 0.3f;
            pad.transform.SetPositionAndRotation(
                new Vector3(area.center.x, groundMax + 0.03f - thickness * 0.5f, area.center.y),
                Quaternion.Euler(0f, yaw, 0f));
            pad.transform.localScale = new Vector3(area.width, thickness, area.height);
            var renderer = pad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(pad, StaticEditorFlags.BatchingStatic);
            var placement = new MapVariantPlacement
            {
                Instance = pad,
                PrefabPath = materialPath,
                Layer = MapVariantLayer.Ground,
                Center = area.center,
                HalfSize = area.size * 0.5f,
                Yaw = yaw,
                Surface = true
            };
            Placements.Add(placement);
            return placement;
        }

        public MapVariantPlacement PlaceOnTop(
            string prefabPath,
            MapVariantLayer layer,
            MapVariantPlacement support,
            Vector2 localOffset,
            float yaw,
            float scale = 1f)
        {
            if (support == null)
                return null;
            MapVariantPrefabInfo info = Info(prefabPath);
            if (!TryGetRenderBounds(support.Instance, out Bounds supportBounds))
                return null;

            GameObject instance = Instantiate(info, Layer(layer));
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 localCenter = info.LocalBounds.center * scale;
            Vector3 offset = rotation * new Vector3(localCenter.x, 0f, localCenter.z);
            Vector2 center = new(supportBounds.center.x + localOffset.x, supportBounds.center.z + localOffset.y);
            float y = supportBounds.max.y - 0.02f - info.LocalBounds.min.y * scale;
            instance.transform.SetPositionAndRotation(
                new Vector3(center.x - offset.x, y, center.y - offset.z),
                rotation);
            instance.transform.localScale = Vector3.one * scale;
            var placement = new MapVariantPlacement
            {
                Instance = instance,
                PrefabPath = prefabPath,
                Layer = layer,
                Center = center,
                HalfSize = new Vector2(info.LocalBounds.extents.x, info.LocalBounds.extents.z) * scale,
                Yaw = yaw,
                Attached = true,
                Support = support,
                Group = support.Group
            };
            Placements.Add(placement);
            return placement;
        }

        // Tiles a piece end-to-end along its longest horizontal axis. The run is reserved as one strip,
        // so neighbouring pieces never reject each other on shared edges.
        public int PlaceLine(
            string prefabPath,
            MapVariantLayer layer,
            Vector2 from,
            Vector2 to,
            PlaceOptions options,
            float yawOffset = 0f,
            float gapScale = 1f)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
                return 0;
            float scale = options.Scale <= 0f ? 1f : options.Scale;
            Vector3 size = Info(prefabPath).LocalBounds.size * scale;
            bool alongZ = size.z > size.x;
            float pieceLength = (alongZ ? size.z : size.x) * gapScale;
            float pieceWidth = alongZ ? size.x : size.z;
            int count = Mathf.Max(1, Mathf.FloorToInt(length / pieceLength + 0.001f));
            Vector2 dir = delta / length;
            float yaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + (alongZ ? 90f : 0f) + yawOffset;

            Vector2 mid = (from + to) * 0.5f;
            float lineYaw = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Rect strip = FootprintAabb(mid, new Vector2(length * 0.5f, pieceWidth * 0.5f), lineYaw, options.Padding);
            if (!options.IgnoreOccupancy && !IsFree(strip))
            {
                Rejected++;
                return 0;
            }

            PlaceOptions pieceOptions = options;
            pieceOptions.Group = options.Group == 0 ? NewGroup() : options.Group;
            pieceOptions.IgnoreOccupancy = true;
            pieceOptions.Reserve = false;
            pieceOptions.Padding = 0f;
            int placed = 0;
            float used = count * pieceLength;
            Vector2 start = from + dir * ((length - used) * 0.5f);
            for (int i = 0; i < count; i++)
            {
                Vector2 c = start + dir * (pieceLength * (i + 0.5f));
                MapVariantPlacement piece = Place(prefabPath, layer, c, yaw, pieceOptions);
                if (piece == null)
                    continue;
                piece.Reserved = options.Reserve;
                placed++;
            }

            if (placed > 0 && options.Reserve)
                Occupy(strip);
            return placed;
        }

        public int Scatter(
            IReadOnlyList<string> prefabs,
            MapVariantLayer layer,
            Rect area,
            int attempts,
            PlaceOptions options,
            bool freeYaw = true,
            float minScale = 1f,
            float maxScale = 1f,
            Func<Vector2, bool> accept = null)
        {
            int placed = 0;
            for (int i = 0; i < attempts; i++)
            {
                var p = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax));
                if (accept != null && !accept(p))
                    continue;
                float yaw = freeYaw ? Range(0f, 360f) : RangeInt(0, 4) * 90f;
                if (Place(Pick(prefabs), layer, p, yaw, options.WithScale(Range(minScale, maxScale))) != null)
                    placed++;
            }

            return placed;
        }

        public bool IsFree(Rect rect)
        {
            GetCellRange(rect, out int x0, out int z0, out int x1, out int z1);
            if (x0 < 0 || z0 < 0 || x1 >= _occupancyX || z1 >= _occupancyZ)
                return false;
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (_occupied[z * _occupancyX + x])
                    return false;
            }

            return true;
        }

        public void Occupy(Rect rect)
        {
            GetCellRange(rect, out int x0, out int z0, out int x1, out int z1);
            x0 = Mathf.Max(0, x0);
            z0 = Mathf.Max(0, z0);
            x1 = Mathf.Min(_occupancyX - 1, x1);
            z1 = Mathf.Min(_occupancyZ - 1, z1);
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                _occupied[z * _occupancyX + x] = true;
        }

        public Transform AddZoneMarker(string name, Vector3 position, Vector3 size)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(Layer(MapVariantLayer.Zones), false);
            marker.position = new Vector3(position.x, Height.Sample(position.x, position.z) + position.y, position.z);
            marker.localScale = size;
            return marker;
        }

        public static Rect FootprintAabb(Vector2 center, Vector2 half, float yaw, float padding)
        {
            float r = yaw * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(r));
            float s = Mathf.Abs(Mathf.Sin(r));
            float hx = half.x * c + half.y * s + padding;
            float hz = half.x * s + half.y * c + padding;
            return new Rect(center.x - hx, center.y - hz, hx * 2f, hz * 2f);
        }

        public static bool TryGetRenderBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!IsGeometryRenderer(renderer))
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        public static bool IsGeometryRenderer(Renderer renderer) =>
            renderer.enabled &&
            (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) &&
            renderer.gameObject.activeInHierarchy;

        private static Rect Clip(Rect rect, Rect bounds) =>
            Rect.MinMaxRect(
                Mathf.Max(rect.xMin, bounds.xMin), Mathf.Max(rect.yMin, bounds.yMin),
                Mathf.Min(rect.xMax, bounds.xMax), Mathf.Min(rect.yMax, bounds.yMax));

        private static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin && inner.yMin >= outer.yMin &&
            inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

        private MapVariantPlacement Reject()
        {
            Rejected++;
            return null;
        }

        private void GetCellRange(Rect rect, out int x0, out int z0, out int x1, out int z1)
        {
            x0 = Mathf.FloorToInt((rect.xMin - World.xMin) / OccupancyCell);
            z0 = Mathf.FloorToInt((rect.yMin - World.yMin) / OccupancyCell);
            x1 = Mathf.CeilToInt((rect.xMax - World.xMin) / OccupancyCell) - 1;
            z1 = Mathf.CeilToInt((rect.yMax - World.yMin) / OccupancyCell) - 1;
        }
    }
}
