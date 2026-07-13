using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AIPlanEntryStartupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled helper; composition calls its methods directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public void WriteBuildPlanEntries(
            DynamicBuffer<AIBuildPlanEntry> entries,
            IReadOnlyList<string> preferredBuildingIds,
            IReadOnlyList<string> fallbackBuildingIds)
        {
            WritePreferredBuildPlanEntries(entries, preferredBuildingIds);

            if (entries.Length > 0)
                return;

            WritePreferredBuildPlanEntries(entries, fallbackBuildingIds);
        }

        public void WriteProductionPlanEntries(
            DynamicBuffer<AIProductionPlanEntry> entries,
            IReadOnlyList<string> preferredUnitIds,
            IReadOnlyList<string> preferredVehicleIds,
            IReadOnlyList<string> fallbackProductionUnitIds)
        {
            WritePreferredProductionPlanEntries(entries, preferredUnitIds);
            WritePreferredProductionPlanEntries(entries, preferredVehicleIds);

            if (entries.Length > 0)
                return;

            WritePreferredProductionPlanEntries(entries, fallbackProductionUnitIds);
        }

        private static void WritePreferredBuildPlanEntries(
            DynamicBuffer<AIBuildPlanEntry> entries,
            IReadOnlyList<string> preferredBuildingIds)
        {
            if (preferredBuildingIds == null)
                return;

            for (int i = 0; i < preferredBuildingIds.Count; i++)
            {
                string buildingId = preferredBuildingIds[i];
                if (string.IsNullOrWhiteSpace(buildingId))
                    continue;

                entries.Add(new AIBuildPlanEntry
                {
                    BuildingId = new FixedString64Bytes(
                        BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId))
                });
            }
        }

        private static void WritePreferredProductionPlanEntries(
            DynamicBuffer<AIProductionPlanEntry> entries,
            IReadOnlyList<string> preferredUnitIds)
        {
            if (preferredUnitIds == null)
                return;

            for (int i = 0; i < preferredUnitIds.Count; i++)
            {
                string unitId = preferredUnitIds[i];
                if (string.IsNullOrWhiteSpace(unitId))
                    continue;

                entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(unitId)) });
            }
        }
    }
}
