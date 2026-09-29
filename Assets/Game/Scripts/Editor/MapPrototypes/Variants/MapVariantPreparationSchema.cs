using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    // Source coordinates are retained alongside runtime coordinates. No consumer may apply
    // this translation again to the already-converted fields.
    internal static class MapVariantPreparationSchema
    {
        internal const string Version = "map-preparation-v1";
        internal static Vector3 Offset(string mapId) => mapId == "Frontier"
            ? new Vector3(-176f, 0f, -176f) : Vector3.zero;

        internal static string Hash(string value)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                .Replace("-", "").ToLowerInvariant();
        }

        // No traversal sequence, dressing count, schema or seed participates in owner identity.
        // Same prefab at the same source transform denotes the same owner across rebuilds.
        internal static string PlacementKey(string mapId, string prefabGuid, Matrix4x4 sourceMatrix)
        {
            string matrix = string.Join(",", Enumerable.Range(0, 16)
                .Select(i => sourceMatrix[i].ToString("R", CultureInfo.InvariantCulture)));
            return mapId + ":" + Hash(prefabGuid + ":" + matrix);
        }

        internal static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin &&
            inner.yMin >= outer.yMin && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

        internal static Vector2[] Footprint(Vector2 center, Vector2 half, float yaw, Vector3 offset)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            return new[] { new Vector2(-half.x, -half.y), new Vector2(half.x, -half.y),
                new Vector2(half.x, half.y), new Vector2(-half.x, half.y) }
                .Select(p => rotation * new Vector3(p.x, 0f, p.y) +
                    new Vector3(center.x, 0f, center.y) + offset)
                .Select(p => new Vector2(p.x, p.z)).ToArray();
        }

        internal static bool TrySurface(MapVariantBuilder b, Vector2 source, out float height, out string kind)
        {
            if (!b.Playable.Contains(source))
            {
                height = 0f; kind = "OutsidePlayable"; return false;
            }
            Vector3 runtime = new Vector3(source.x, 0f, source.y) + Offset(b.MapId);
            if (runtime.x < 0f || runtime.z < 0f || runtime.x >= 2048f || runtime.z >= 1024f)
            {
                height = 0f; kind = "OutsideGrid"; return false;
            }
            height = b.Height.Sample(source.x, source.y);
            kind = "Ground";
            // Deck pivots are the driving surface, authored 8 cm above the declared level.
            if (b.BridgeCells.TryGetValue(b.RoadCell(source), out float deck))
            {
                height = deck + 0.08f; kind = "BridgeDeck"; return true;
            }
            foreach (MapVariantPlacement pad in b.Placements.Where(p => p.Surface))
            {
                Vector3 local = Quaternion.Euler(0f, -pad.Yaw, 0f) *
                    new Vector3(source.x - pad.Center.x, 0f, source.y - pad.Center.y);
                if (Mathf.Abs(local.x) <= pad.HalfSize.x && Mathf.Abs(local.z) <= pad.HalfSize.y &&
                    MapVariantBuilder.TryGetRenderBounds(pad.Instance, out Bounds bounds))
                {
                    height = Mathf.Max(height, bounds.max.y); kind = "Pad";
                }
            }
            if (b.WaterLevel.HasValue && height < b.WaterLevel.Value + 0.15f)
            {
                kind = "WaterExcluded"; return false;
            }
            return true;
        }
    }

    [Serializable] internal sealed class MapPreparationManifest
    {
        public string schema = MapVariantPreparationSchema.Version;
        public string mapId, sourceScene, sourceSceneGuid, sourceSceneHash, semanticHash;
        public int seed, placementCount, unresolvedPlacementCount, unresolvedPrefabCount;
        public Vector2 worldMin, worldSize, playableMin, playableSize, runtimePlayableMin;
        public Vector3 sourceToRuntimeTranslation;
        public bool waterPresent;
        public float waterLevel, heightCellSize;
        public Vector2 heightOrigin;
        public int heightCellsX, heightCellsZ;
        public float[] groundVertexHeights;
        public List<MapPreparationPrefab> prefabs = new();
        public List<MapPreparationPlacement> placements = new();
        public List<MapPreparationRoad> roads = new();
        public List<MapPreparationZone> zones = new();
    }

    [Serializable] internal sealed class MapPreparationPrefab
    {
        public string path, guid, dependencyHash, hierarchyStatus;
        public long localId;
        public string[] hierarchy, materials, shaders, physics;
        public string intactBranch, destroyedBranch;
    }

    [Serializable] internal sealed class MapPreparationPlacement
    {
        public string stableKey, sourceRecipe, sourceGuid, category, renderingCategory, qualification;
        public string attachmentOwnerKey, attachmentPolicy, definitionRole, destroyedMapping;
        public Vector3 sourcePosition, runtimePosition, sourceScale;
        public Quaternion rotation;
        public Vector2[] sourceFootprint, runtimeFootprint;
        public string[] attachmentPrefabGuids, materialGuids;
        public bool movementBlocked, buildExcluded, damageEligible, targetEligible;
        public bool artReserved, backdrop, outsidePlayable, attached, intentionalRuin;
        public float foundationDepth;
    }

    [Serializable] internal sealed class MapPreparationRoad
    {
        public Vector2Int cell;
        public string kind;
        public Vector2 sourceMin, runtimeMin;
        public bool bridge;
        public float groundHeight, declaredDeckLevel, renderedDeckHeight;
    }

    [Serializable] internal sealed class MapPreparationZone
    {
        public string id, surfaceKind, boundsDecision;
        public Vector3 sourceCenter, runtimeCenter, size;
        public bool entireFootprintInsidePlayable, centerSurfaceValid;
    }
}
