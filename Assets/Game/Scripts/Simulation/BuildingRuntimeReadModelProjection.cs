using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    /// <summary>Deterministic row aggregation shared by the runtime publisher and headless host.</summary>
    internal static class BuildingRuntimeReadModelProjection
    {
        internal static void AddFactionSummary(
            DynamicBuffer<BuildingRuntimeFactionSummary> buffer,
            byte factionId,
            int buildingCount,
            FactionResourceCompositionSystemHelper.ResourceEconomySnapshot economy)
        {
            buffer.Add(new BuildingRuntimeFactionSummary
            {
                FactionId = factionId,
                BuildingCount = buildingCount,
                StoredOilBarrels = economy.StoredOilBarrels,
                StoredFuelBarrels = economy.StoredFuelBarrels,
                OilBarrelsPerDay = economy.OilBarrelsPerDay,
                FuelBarrelsPerDay = economy.FuelBarrelsPerDay
            });
        }

        internal static void PublishOwnedBuildingSummaries(
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Func<RuntimeBuildingEntity, FixedString128Bytes> resolveBoundaryId)
        {
            buffer.Clear();
            if (runtimeBuildings == null)
                return;

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.Definition == null)
                    continue;

                FixedString128Bytes buildingId = resolveBoundaryId(building);
                AddOrIncrementOwnedBuildingSummary(buffer, building.OwnerFactionId, buildingId, 1);
            }
        }

        internal static void AddOrIncrementOwnedBuildingSummary(
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> buffer,
            byte factionId,
            FixedString128Bytes buildingId,
            int countDelta)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                BuildingRuntimeOwnedBuildingSummary summary = buffer[i];
                if (summary.FactionId != factionId || !summary.BuildingId.Equals(buildingId))
                    continue;

                summary.Count += countDelta;
                buffer[i] = summary;
                return;
            }

            buffer.Add(new BuildingRuntimeOwnedBuildingSummary
            {
                FactionId = factionId,
                BuildingId = buildingId,
                Count = countDelta
            });
        }
    }
}
