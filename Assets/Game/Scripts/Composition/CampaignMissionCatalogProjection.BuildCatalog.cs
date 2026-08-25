using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static void ProjectBuildCatalog(
            ref BlobBuilder builder,
            ref CampaignMissionDefinitionBlob definition,
            ScenarioSetupConfig scenario)
        {
            ScenarioMissionBuildZoneConfig buildZone = scenario.MissionRuntime.BuildZone;
            definition.BuildZone = new CampaignMissionBuildZoneBlob
            {
                AnchorId = new FixedString64Bytes(buildZone.AnchorId ?? string.Empty),
                HalfWidthCells = buildZone.HalfWidthCells,
                HalfHeightCells = buildZone.HalfHeightCells
            };
            ReadOnlySpan<ScenarioMissionBuildEntryConfig> source = scenario.MissionRuntime.BuildCatalog;
            BlobBuilderArray<CampaignMissionBuildEntryBlob> projected =
                builder.Allocate(ref definition.BuildCatalog, source.Length);
            for (int index = 0; index < source.Length; index++)
            {
                projected[index] = new CampaignMissionBuildEntryBlob
                {
                    BuildingConfigId = new FixedString64Bytes(source[index].BuildingConfigId),
                    MaxCount = source[index].MaxCount
                };
            }
        }

        private static bool MatchesBuildCatalog(
            ref CampaignMissionBuildZoneBlob projectedZone,
            ref BlobArray<CampaignMissionBuildEntryBlob> projected,
            in ScenarioMissionRuntimeConfig missionRuntime)
        {
            ScenarioMissionBuildZoneConfig sourceZone = missionRuntime.BuildZone;
            if (!Matches(projectedZone.AnchorId, sourceZone.AnchorId) ||
                projectedZone.HalfWidthCells != sourceZone.HalfWidthCells ||
                projectedZone.HalfHeightCells != sourceZone.HalfHeightCells)
                return false;

            ReadOnlySpan<ScenarioMissionBuildEntryConfig> source = missionRuntime.BuildCatalog;
            if (projected.Length != source.Length)
                return false;

            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionBuildEntryBlob entry = ref projected[index];
                if (!Matches(entry.BuildingConfigId, source[index].BuildingConfigId) ||
                    entry.MaxCount != source[index].MaxCount)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
