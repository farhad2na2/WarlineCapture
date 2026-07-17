using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class OperationMapRunwayReadModelUtility
    {
        private const int NoActiveMapSignature = 0;
        private const int InvalidActiveMapSignature = int.MinValue;

        public static int ResolveGenerationSignature(EntityManager entityManager)
        {
            if (OperationMapMetadataUtility.TryResolveActiveMetadata(
                    entityManager,
                    out _,
                    out int generation,
                    out bool hasActiveMap,
                    out _))
            {
                return unchecked((generation * 397) ^ 0x4f504d41);
            }

            return hasActiveMap ? InvalidActiveMapSignature : NoActiveMapSignature;
        }

        public static bool TryAppendRunways(
            EntityManager entityManager,
            DynamicBuffer<BuildingFactionRunwayReadModel> destination,
            in GridConfig grid,
            out FixedList512Bytes<byte> mapOwnedFactions,
            out bool hasActiveMap,
            out string error)
        {
            mapOwnedFactions = default;
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
                error = "The runtime grid is invalid for active operation-map runway projection.";
                return false;
            }

            for (int index = 0; index < metadata.Value.Anchors.Length; index++)
            {
                OperationMapAnchorBlob anchor = metadata.Value.Anchors[index];
                if (anchor.Kind != OperationMapAnchorKind.Runway)
                    continue;

                if (anchor.FactionId < 0 || anchor.FactionId > byte.MaxValue || anchor.LaneIndex < 0)
                {
                    error = $"Active operation-map runway `{anchor.Id}` has an invalid faction or lane.";
                    return false;
                }

                for (int priorIndex = 0; priorIndex < index; priorIndex++)
                {
                    OperationMapAnchorBlob prior = metadata.Value.Anchors[priorIndex];
                    if (prior.Kind == OperationMapAnchorKind.Runway &&
                        prior.FactionId == anchor.FactionId &&
                        prior.LaneIndex == anchor.LaneIndex)
                    {
                        error = $"Active operation map has duplicate runway anchors for faction {anchor.FactionId}, lane {anchor.LaneIndex}.";
                        return false;
                    }
                }

                if (!OperationMapMetadataUtility.TryResolveRunwayGeometry(
                        in anchor,
                        out _,
                        out _,
                        out float3 takeoffPosition,
                        out float3 landingPosition))
                {
                    error = $"Active operation-map runway `{anchor.Id}` has invalid geometry.";
                    return false;
                }

                int2 takeoffCell = GridUtils.WorldToCell(in grid, takeoffPosition);
                int2 landingCell = GridUtils.WorldToCell(in grid, landingPosition);
                if (!GridUtils.InBounds(takeoffCell, grid.Width, grid.Height) ||
                    !GridUtils.InBounds(landingCell, grid.Width, grid.Height))
                {
                    error = $"Active operation-map runway `{anchor.Id}` resolves outside the runtime grid.";
                    return false;
                }
            }

            for (int index = 0; index < metadata.Value.Anchors.Length; index++)
            {
                OperationMapAnchorBlob anchor = metadata.Value.Anchors[index];
                if (anchor.Kind != OperationMapAnchorKind.Runway)
                    continue;

                OperationMapMetadataUtility.TryResolveRunwayGeometry(
                    in anchor,
                    out float3 center,
                    out float3 direction,
                    out float3 takeoffPosition,
                    out float3 landingPosition);
                byte factionId = (byte)anchor.FactionId;
                int2 takeoffCell = GridUtils.WorldToCell(in grid, takeoffPosition);
                int2 landingCell = GridUtils.WorldToCell(in grid, landingPosition);
                destination.Add(new BuildingFactionRunwayReadModel
                {
                    FactionId = factionId,
                    BuildingId = anchor.Id,
                    BuildingRuntimeId = 0,
                    TakeoffCell = takeoffCell,
                    LandingCell = landingCell,
                    TakeoffPosition = takeoffPosition,
                    LandingPosition = landingPosition,
                    Center = center,
                    Direction = direction,
                    HalfExtents = new float2(1f, anchor.Radius)
                });

                if (!ContainsFaction(in mapOwnedFactions, factionId))
                    mapOwnedFactions.Add(factionId);
            }

            error = null;
            return true;
        }

        public static bool ContainsFaction(in FixedList512Bytes<byte> factions, byte factionId)
        {
            for (int index = 0; index < factions.Length; index++)
            {
                if (factions[index] == factionId)
                    return true;
            }

            return false;
        }

        public static void RemoveBuildingFallbacks(
            DynamicBuffer<BuildingFactionRunwayReadModel> destination,
            int mapRunwayCount,
            in FixedList512Bytes<byte> mapOwnedFactions)
        {
            for (int index = destination.Length - 1; index >= mapRunwayCount; index--)
            {
                if (ContainsFaction(in mapOwnedFactions, destination[index].FactionId))
                    destination.RemoveAt(index);
            }
        }
    }
}
