using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public sealed partial class AIPlanEntryStartupSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void WriteBuildPlanEntries(
        DynamicBuffer<AIBuildPlanEntry> entries,
        IReadOnlyList<string> preferredBuildingIds,
        AIPlanEntryStartupConfig config)
    {
        WritePreferredBuildPlanEntries(entries, preferredBuildingIds);

        if (entries.Length > 0)
            return;

        WritePreferredBuildPlanEntries(entries, config != null ? config.FallbackBuildingIds : null);
    }

    public void WriteProductionPlanEntries(
        DynamicBuffer<AIProductionPlanEntry> entries,
        IReadOnlyList<string> preferredUnitIds,
        IReadOnlyList<string> preferredVehicleIds,
        AIPlanEntryStartupConfig config)
    {
        WritePreferredProductionPlanEntries(entries, preferredUnitIds);
        WritePreferredProductionPlanEntries(entries, preferredVehicleIds);

        if (entries.Length > 0)
            return;

        WritePreferredProductionPlanEntries(entries, config != null ? config.FallbackProductionUnitIds : null);
    }

    private void WritePreferredBuildPlanEntries(
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

            entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes(buildingId) });
        }
    }

    private void WritePreferredProductionPlanEntries(
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

            entries.Add(new AIProductionPlanEntry { UnitId = new FixedString64Bytes(BuildingDefinitionSystem.NormalizeSpawnableKey(unitId)) });
        }
    }
}
