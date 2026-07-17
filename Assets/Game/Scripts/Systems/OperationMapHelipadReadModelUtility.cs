using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class OperationMapHelipadReadModelUtility
    {
        private const float PositionToleranceSq = 0.0625f;
        private static readonly FixedString128Bytes HelipadBuildingId = new("building_helipad");

        public static void Bind(
            EntityManager entityManager,
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> destination,
            in GridConfig grid)
        {
            if (!TryBind(entityManager, destination, in grid, out _, out _))
                destination.Clear();
        }

        public static bool TryBind(
            EntityManager entityManager,
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> destination,
            in GridConfig grid,
            out bool hasActiveMap,
            out string error)
        {
            if (!OperationMapMetadataUtility.TryResolveActiveMetadata(
                    entityManager,
                    out BlobAssetReference<OperationMapBlob> metadata,
                    out hasActiveMap,
                    out error))
            {
                return !hasActiveMap && error == null;
            }

            if (grid.Width <= 0 || grid.Height <= 0 || !math.isfinite(grid.CellSize) || grid.CellSize <= 0f)
            {
                error = "The runtime grid is invalid for active operation-map helipad binding.";
                return false;
            }

            FixedList4096Bytes<int> boundBuildings = default;
            FixedList512Bytes<byte> mapOwnedFactions = default;
            for (int anchorIndex = 0; anchorIndex < metadata.Value.Anchors.Length; anchorIndex++)
            {
                OperationMapAnchorBlob anchor = metadata.Value.Anchors[anchorIndex];
                if (anchor.Kind != OperationMapAnchorKind.Helipad)
                    continue;

                if (anchor.FactionId < 0 || anchor.FactionId > byte.MaxValue || anchor.LaneIndex < 0)
                {
                    error = $"Active operation-map helipad `{anchor.Id}` has an invalid faction or lane.";
                    return false;
                }

                for (int priorIndex = 0; priorIndex < anchorIndex; priorIndex++)
                {
                    OperationMapAnchorBlob prior = metadata.Value.Anchors[priorIndex];
                    if (prior.Kind == OperationMapAnchorKind.Helipad &&
                        prior.FactionId == anchor.FactionId &&
                        prior.LaneIndex == anchor.LaneIndex)
                    {
                        error = $"Active operation map has duplicate helipad anchors for faction {anchor.FactionId}, lane {anchor.LaneIndex}.";
                        return false;
                    }
                }

                if (!OperationMapMetadataUtility.TryResolveHelipadGeometry(
                        in anchor,
                        out float3 center,
                        out _,
                        out _))
                {
                    error = $"Active operation-map helipad `{anchor.Id}` has invalid geometry.";
                    return false;
                }

                int2 cell = GridUtils.WorldToCell(in grid, center);
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                {
                    error = $"Active operation-map helipad `{anchor.Id}` resolves outside the runtime grid.";
                    return false;
                }

                int rowIndex = FindMatchingRow(destination, (byte)anchor.FactionId, cell, center);
                if (rowIndex < 0)
                {
                    error = $"Active operation-map helipad `{anchor.Id}` has no unique runtime production-slot owner.";
                    return false;
                }

                int buildingRuntimeId = destination[rowIndex].BuildingRuntimeId;
                if (ContainsBuilding(in boundBuildings, buildingRuntimeId))
                {
                    error = $"Active operation-map helipad `{anchor.Id}` resolves to an already-bound runtime building.";
                    return false;
                }

                if (boundBuildings.Length >= boundBuildings.Capacity)
                {
                    error = "Active operation-map helipad count exceeds the bounded runtime binding capacity.";
                    return false;
                }

                boundBuildings.Add(buildingRuntimeId);
                if (!ContainsFaction(in mapOwnedFactions, (byte)anchor.FactionId))
                    mapOwnedFactions.Add((byte)anchor.FactionId);
            }

            for (int anchorIndex = 0; anchorIndex < metadata.Value.Anchors.Length; anchorIndex++)
            {
                OperationMapAnchorBlob anchor = metadata.Value.Anchors[anchorIndex];
                if (anchor.Kind != OperationMapAnchorKind.Helipad)
                    continue;

                OperationMapMetadataUtility.TryResolveHelipadGeometry(
                    in anchor,
                    out float3 center,
                    out _,
                    out _);
                int2 cell = GridUtils.WorldToCell(in grid, center);
                int rowIndex = FindMatchingRow(destination, (byte)anchor.FactionId, cell, center);
                BuildingFactionProductionSpawnPointReadModel row = destination[rowIndex];
                row.Cell = cell;
                row.WorldPosition = center;
                destination[rowIndex] = row;
            }

            for (int rowIndex = destination.Length - 1; rowIndex >= 0; rowIndex--)
            {
                BuildingFactionProductionSpawnPointReadModel row = destination[rowIndex];
                if (!row.BuildingId.Equals(HelipadBuildingId) ||
                    !ContainsFaction(in mapOwnedFactions, row.FactionId) ||
                    ContainsBuilding(in boundBuildings, row.BuildingRuntimeId))
                {
                    continue;
                }

                destination.RemoveAt(rowIndex);
            }

            error = null;
            return true;
        }

        private static int FindMatchingRow(
            DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> destination,
            byte factionId,
            int2 cell,
            float3 center)
        {
            int match = -1;
            for (int rowIndex = 0; rowIndex < destination.Length; rowIndex++)
            {
                BuildingFactionProductionSpawnPointReadModel row = destination[rowIndex];
                if (row.FactionId != factionId ||
                    row.BuildingRuntimeId <= 0 ||
                    row.SlotIndex < 0 ||
                    !row.BuildingId.Equals(HelipadBuildingId) ||
                    !row.Cell.Equals(cell) ||
                    math.distancesq(row.WorldPosition, center) > PositionToleranceSq)
                {
                    continue;
                }

                if (match >= 0)
                    return -1;
                match = rowIndex;
            }

            return match;
        }

        private static bool ContainsBuilding(
            in FixedList4096Bytes<int> buildingRuntimeIds,
            int buildingRuntimeId)
        {
            for (int index = 0; index < buildingRuntimeIds.Length; index++)
            {
                if (buildingRuntimeIds[index] == buildingRuntimeId)
                    return true;
            }

            return false;
        }

        private static bool ContainsFaction(in FixedList512Bytes<byte> factions, byte factionId)
        {
            for (int index = 0; index < factions.Length; index++)
            {
                if (factions[index] == factionId)
                    return true;
            }

            return false;
        }
    }
}
