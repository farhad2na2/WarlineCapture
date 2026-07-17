using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    internal static class OperationMapCurrentInfrastructureAnchorAuthoring
    {
        private const string AirportCategory = "Building_Airport";
        private const string HelipadCategory = "Building_Helipad";
        private const string HelipadSpawnMarker = "Spawn_01";
        private const int ExpectedAirportCount = 1;
        private const int ExpectedHelipadCount = 3;

        public static OperationMapAnchorConfig[] BuildInfrastructureAnchors(
            MapBuildingPlacementConfig placementConfig)
        {
            if (placementConfig == null)
                throw new ArgumentNullException(nameof(placementConfig));

            List<MapBuildingPlacementConfigEntry> airports = CollectPlacements(
                placementConfig,
                AirportCategory,
                ExpectedAirportCount);
            List<MapBuildingPlacementConfigEntry> helipads = CollectPlacements(
                placementConfig,
                HelipadCategory,
                ExpectedHelipadCount);
            var anchors = new OperationMapAnchorConfig[airports.Count + helipads.Count];
            int anchorIndex = 0;

            for (int index = 0; index < airports.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = airports[index];
                int laneIndex = CountEarlierFactionPlacements(airports, index);
                anchors[anchorIndex++] = BuildRunwayAnchor(placement, laneIndex);
            }

            for (int index = 0; index < helipads.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = helipads[index];
                int laneIndex = CountEarlierFactionPlacements(helipads, index);
                anchors[anchorIndex++] = BuildHelipadAnchor(placement, laneIndex);
            }

            return anchors;
        }

        public static string ComputeGeneratedMetadataHash(
            string baseMetadataHash,
            IReadOnlyList<OperationMapAnchorConfig> anchors)
        {
            if (string.IsNullOrWhiteSpace(baseMetadataHash))
                throw new ArgumentException("Base metadata hash is required.", nameof(baseMetadataHash));
            if (anchors == null)
                throw new ArgumentNullException(nameof(anchors));

            var canonical = new StringBuilder(baseMetadataHash.Length + anchors.Count * 160);
            canonical.Append(baseMetadataHash).Append('\n');
            for (int index = 0; index < anchors.Count; index++)
            {
                OperationMapAnchorConfig anchor = anchors[index];
                canonical.Append(anchor.AnchorId).Append('|')
                    .Append((byte)anchor.Kind).Append('|');
                AppendVector(canonical, anchor.Position);
                canonical.Append('|');
                AppendVector(canonical, anchor.EulerAngles);
                canonical.Append('|')
                    .Append(anchor.Radius.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(anchor.FactionId).Append('|')
                    .Append(anchor.LaneIndex).Append('\n');
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(canonical.ToString());
            return ToLowerHex(sha256.ComputeHash(bytes));
        }

        private static List<MapBuildingPlacementConfigEntry> CollectPlacements(
            MapBuildingPlacementConfig placementConfig,
            string category,
            int expectedCount)
        {
            var matches = new List<MapBuildingPlacementConfigEntry>(expectedCount);
            IReadOnlyList<MapBuildingPlacementConfigEntry> placements = placementConfig.Placements;
            for (int index = 0; index < placements.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = placements[index];
                if (placement != null && string.Equals(placement.Category, category, StringComparison.Ordinal))
                    matches.Add(placement);
            }

            matches.Sort((left, right) => string.CompareOrdinal(left.SourcePath, right.SourcePath));
            if (matches.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Current compatibility map requires exactly {expectedCount} '{category}' placements; found {matches.Count}.");
            }

            for (int index = 0; index < matches.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = matches[index];
                if (placement.BuildingPrefab == null || string.IsNullOrWhiteSpace(placement.SourcePath))
                    throw new InvalidOperationException($"'{category}' placement {index} is missing prefab or source identity.");
                if (index > 0 && string.Equals(matches[index - 1].SourcePath, placement.SourcePath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"'{category}' placement source identity is duplicated: {placement.SourcePath}.");
                RequirePlacementScale(placement);
            }

            return matches;
        }

        private static OperationMapAnchorConfig BuildRunwayAnchor(
            MapBuildingPlacementConfigEntry placement,
            int laneIndex)
        {
            if (!BuildingRunwaySystem.TryResolvePrefabRunwayLocalData(
                    placement.BuildingPrefab,
                    out Vector3 localCenter,
                    out Quaternion localRotation,
                    out Vector3 localHalfExtents))
            {
                throw new InvalidOperationException(
                    $"Airport placement '{placement.SourcePath}' has no valid prefab runway geometry.");
            }

            Matrix4x4 placementMatrix = CreatePlacementMatrix(placement, out Quaternion placementRotation);
            float halfLength = Mathf.Abs(localHalfExtents.z * placement.WorldScale.z);
            if (!IsFinite(halfLength) || halfLength <= 0f)
                throw new InvalidOperationException($"Airport placement '{placement.SourcePath}' has invalid runway half-length.");

            Quaternion worldRotation = placementRotation * localRotation;
            return new OperationMapAnchorConfig(
                BuildAnchorId(OperationMapAnchorKind.Runway, placement.FactionId, laneIndex),
                OperationMapAnchorKind.Runway,
                placementMatrix.MultiplyPoint3x4(localCenter),
                worldRotation.eulerAngles,
                halfLength,
                placement.FactionId,
                laneIndex);
        }

        private static OperationMapAnchorConfig BuildHelipadAnchor(
            MapBuildingPlacementConfigEntry placement,
            int laneIndex)
        {
            Transform spawn = FindUniqueTransform(placement.BuildingPrefab, HelipadSpawnMarker);
            if (!BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(
                    placement.BuildingPrefab,
                    out Bounds localBounds))
            {
                throw new InvalidOperationException(
                    $"Helipad placement '{placement.SourcePath}' has no prefab bounds for clearance geometry.");
            }

            Matrix4x4 placementMatrix = CreatePlacementMatrix(placement, out Quaternion placementRotation);
            float width = Mathf.Abs(localBounds.size.x * placement.WorldScale.x);
            float depth = Mathf.Abs(localBounds.size.z * placement.WorldScale.z);
            float clearanceRadius = Mathf.Min(width, depth) * 0.5f;
            if (!IsFinite(clearanceRadius) || clearanceRadius <= 0f)
                throw new InvalidOperationException($"Helipad placement '{placement.SourcePath}' has invalid clearance radius.");

            Quaternion worldRotation = placementRotation * spawn.localRotation;
            return new OperationMapAnchorConfig(
                BuildAnchorId(OperationMapAnchorKind.Helipad, placement.FactionId, laneIndex),
                OperationMapAnchorKind.Helipad,
                placementMatrix.MultiplyPoint3x4(spawn.localPosition),
                worldRotation.eulerAngles,
                clearanceRadius,
                placement.FactionId,
                laneIndex);
        }

        private static Matrix4x4 CreatePlacementMatrix(
            MapBuildingPlacementConfigEntry placement,
            out Quaternion rotation)
        {
            rotation = Quaternion.Euler(placement.WorldEulerAngles);
            return Matrix4x4.TRS(placement.WorldPosition, rotation, placement.WorldScale);
        }

        private static Transform FindUniqueTransform(GameObject prefab, string name)
        {
            Transform result = null;
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null || !string.Equals(candidate.name, name, StringComparison.Ordinal))
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Prefab '{prefab.name}' has duplicate '{name}' markers.");
                result = candidate;
            }

            return result != null
                ? result
                : throw new InvalidOperationException($"Prefab '{prefab.name}' is missing required '{name}' marker.");
        }

        private static int CountEarlierFactionPlacements(
            IReadOnlyList<MapBuildingPlacementConfigEntry> placements,
            int currentIndex)
        {
            int count = 0;
            byte factionId = placements[currentIndex].FactionId;
            for (int index = 0; index < currentIndex; index++)
            {
                if (placements[index].FactionId == factionId)
                    count++;
            }
            return count;
        }

        private static string BuildAnchorId(OperationMapAnchorKind kind, byte factionId, int laneIndex)
        {
            string kindSegment = kind == OperationMapAnchorKind.Runway ? "runway" : "helipad";
            return $"anchor.skirmish.desert_base_01.{kindSegment}.faction_{factionId}.lane_{laneIndex}";
        }

        private static void RequirePlacementScale(MapBuildingPlacementConfigEntry placement)
        {
            Vector3 scale = placement.WorldScale;
            if (!IsFinite(scale.x) || !IsFinite(scale.y) || !IsFinite(scale.z) ||
                Mathf.Abs(scale.x) <= 0f || Mathf.Abs(scale.y) <= 0f || Mathf.Abs(scale.z) <= 0f)
            {
                throw new InvalidOperationException($"Placement '{placement.SourcePath}' has invalid world scale.");
            }
        }

        private static void AppendVector(StringBuilder target, Vector3 value)
        {
            target.Append(value.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.z.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static string ToLowerHex(byte[] bytes)
        {
            const string digits = "0123456789abcdef";
            char[] result = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                result[index * 2] = digits[bytes[index] >> 4];
                result[index * 2 + 1] = digits[bytes[index] & 0x0f];
            }
            return new string(result);
        }
    }
}
