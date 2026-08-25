using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static bool MatchesProjectedCatalog(
            in CampaignMissionCatalogComponent projected,
            MissionDefinitionConfig[] missions,
            ScenarioSetupConfig[] scenarios)
        {
            ref CampaignMissionCatalogBlob catalog = ref projected.Blob.Value;
            if (catalog.Missions.Length != missions.Length ||
                catalog.SchemaVersion != missions[0].SchemaVersion)
                return false;

            for (int index = 0; index < missions.Length; index++)
            {
                if (!MatchesDefinition(ref catalog.Missions[index], missions[index], scenarios[index]))
                    return false;
            }

            return true;
        }

        private static bool MatchesDefinition(
            ref CampaignMissionDefinitionBlob projected,
            MissionDefinitionConfig mission,
            ScenarioSetupConfig scenario)
        {
            if (!Matches(projected.MissionId, mission.MissionId) ||
                !Matches(projected.ScenarioId, scenario.ScenarioId) ||
                !Matches(projected.OperationMapId, mission.OperationMapId) ||
                !Matches(projected.DisplayNameKey, mission.DisplayNameKey) ||
                !Matches(projected.DisplaySummaryKey, mission.DisplaySummaryKey) ||
                !Matches(projected.LocationNameKey, mission.LocationNameKey) ||
                !Matches(projected.BriefingSequenceId, mission.BriefingSequenceId) ||
                projected.SchemaVersion != mission.SchemaVersion ||
                projected.DeterministicSeed != scenario.DeterministicSeed ||
                projected.EncounterStartMilliseconds != scenario.EncounterStartMilliseconds ||
                projected.StartingCredits != scenario.MissionRuntime.StartingCredits ||
                projected.StartingMaterials != scenario.MissionRuntime.StartingMaterials ||
                projected.MissionRuntimeEnabled != Flag(scenario.MissionRuntime.Enabled) ||
                projected.BuildingDisabled != Flag(scenario.Restrictions.BuildingDisabled) ||
                projected.ProductionDisabled != Flag(scenario.Restrictions.ProductionDisabled) ||
                projected.EconomyDisabled != Flag(scenario.Restrictions.EconomyDisabled) ||
                projected.TransportDisabled != Flag(scenario.Restrictions.TransportDisabled) ||
                projected.AirDisabled != Flag(scenario.Restrictions.AirDisabled) ||
                projected.ReplayAllowed != Flag(mission.ReplayAllowed) ||
                projected.ReplayTutorialDefaultEnabled != Flag(mission.ReplayTutorialDefaultEnabled))
                return false;

            return MatchesBuildCatalog(ref projected.BuildZone, ref projected.BuildCatalog, scenario.MissionRuntime) &&
                   MatchesObjectives(ref projected.Objectives, mission.Objectives) &&
                   MatchesForces(ref projected.ForceGroups, scenario.UnitGroups) &&
                   MatchesRoutes(ref projected.PatrolRoutes, scenario.PatrolRoutes) &&
                   MatchesAmbient(ref projected.AmbientPresentations, scenario.AmbientPresentations) &&
                   MatchesStars(ref projected.StarRules, mission.Stars) &&
                   MatchesRewards(ref projected.FirstClearRewards, mission.FirstClearRewards) &&
                   MatchesRewards(ref projected.ReplayRewards, mission.ReplayRewards);
        }

        private static bool MatchesObjectives(
            ref BlobArray<CampaignMissionObjectiveBlob> projected,
            ReadOnlySpan<MissionObjectiveDefinitionConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionObjectiveBlob value = ref projected[index];
                if (!Matches(value.ObjectiveId, source[index].ObjectiveId) ||
                    !Matches(value.DisplayTextKey, source[index].DisplayTextKey) ||
                    !Matches(value.MissionRoleId, source[index].MissionRoleId) ||
                    !Matches(value.TargetConfigId, source[index].TargetConfigId) ||
                    value.Rule != source[index].Rule ||
                    value.RequiredCount != source[index].RequiredCount ||
                    value.FailureOnRuleBreak != Flag(source[index].FailureOnRuleBreak))
                    return false;
            }
            return true;
        }

        private static bool MatchesForces(
            ref BlobArray<CampaignMissionForceGroupBlob> projected,
            ReadOnlySpan<ScenarioUnitGroupConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int groupIndex = 0; groupIndex < source.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref projected[groupIndex];
                ScenarioUnitGroupConfig sourceGroup = source[groupIndex];
                ReadOnlySpan<ScenarioUnitEntryConfig> units = sourceGroup.Units;
                if (!Matches(group.GroupId, sourceGroup.GroupId) ||
                    group.FactionId != sourceGroup.FactionIndex || group.Units.Length != units.Length)
                    return false;
                for (int unitIndex = 0; unitIndex < units.Length; unitIndex++)
                {
                    ref CampaignMissionForceUnitBlob unit = ref group.Units[unitIndex];
                    ScenarioUnitEntryConfig sourceUnit = units[unitIndex];
                    if (!Matches(unit.SourceKey, sourceUnit.UnitConfigKey) ||
                        !Matches(unit.RuntimePrefabSourceKey, sourceUnit.RuntimePrefabSourceKey) ||
                        !Matches(unit.ExpectedAssetGuid, sourceUnit.ExpectedAssetGuid) ||
                        !Matches(unit.SpawnAnchorId, sourceUnit.SpawnAnchorId) ||
                        !Matches(unit.MissionRoleId, sourceUnit.MissionRoleId) ||
                        unit.Count != sourceUnit.Count)
                        return false;
                }
            }
            return true;
        }

        private static bool MatchesRoutes(
            ref BlobArray<CampaignMissionPatrolRouteBlob> projected,
            ReadOnlySpan<ScenarioPatrolRouteConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int routeIndex = 0; routeIndex < source.Length; routeIndex++)
            {
                ref CampaignMissionPatrolRouteBlob route = ref projected[routeIndex];
                ScenarioPatrolRouteConfig sourceRoute = source[routeIndex];
                ReadOnlySpan<string> anchors = sourceRoute.AnchorIds;
                if (!Matches(route.RouteId, sourceRoute.RouteId) ||
                    !Matches(route.UnitGroupId, sourceRoute.UnitGroupId) ||
                    route.StartDelayMilliseconds != sourceRoute.StartDelayMilliseconds ||
                    route.AnchorIds.Length != anchors.Length)
                    return false;
                for (int anchorIndex = 0; anchorIndex < anchors.Length; anchorIndex++)
                    if (!Matches(route.AnchorIds[anchorIndex], anchors[anchorIndex]))
                        return false;
            }
            return true;
        }

        private static bool MatchesAmbient(
            ref BlobArray<CampaignMissionAmbientPresentationBlob> projected,
            ReadOnlySpan<ScenarioAmbientPresentationConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionAmbientPresentationBlob value = ref projected[index];
                if (!Matches(value.PresentationId, source[index].PresentationId) ||
                    !Matches(value.AnchorId, source[index].AnchorId) ||
                    !Matches(value.RouteId, source[index].RouteId) ||
                    value.InstanceCount != source[index].InstanceCount)
                    return false;
            }
            return true;
        }

        private static bool MatchesStars(
            ref BlobArray<CampaignMissionStarRuleBlob> projected,
            ReadOnlySpan<MissionStarDefinitionConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionStarRuleBlob value = ref projected[index];
                if (value.StarIndex != source[index].StarIndex || value.Rule != source[index].Rule ||
                    !Matches(value.DisplayTextKey, source[index].DisplayTextKey) ||
                    value.Threshold != source[index].Threshold)
                    return false;
            }
            return true;
        }

        private static bool MatchesRewards(
            ref BlobArray<CampaignMissionRewardBlob> projected,
            ReadOnlySpan<MissionRewardDefinitionConfig> source)
        {
            if (projected.Length != source.Length)
                return false;
            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionRewardBlob value = ref projected[index];
                if (value.Kind != source[index].Kind ||
                    !Matches(value.RewardConfigId, source[index].RewardConfigId) ||
                    !Matches(value.DisplayTextKey, source[index].DisplayTextKey) ||
                    value.Amount != source[index].Amount)
                    return false;
            }
            return true;
        }

        private static bool Matches(in FixedString64Bytes projected, string source) =>
            projected.Equals(new FixedString64Bytes(source ?? string.Empty));

        private static byte Flag(bool value) => value ? (byte)1 : (byte)0;
    }
}
