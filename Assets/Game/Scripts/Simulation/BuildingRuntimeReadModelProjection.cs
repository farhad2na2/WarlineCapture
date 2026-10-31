using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    /// <summary>Deterministic row aggregation shared by the runtime publisher and headless host.</summary>
    public static class BuildingRuntimeReadModelProjection
    {
        public static void AddOrIncrementOwnedBuildingSummary(
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
