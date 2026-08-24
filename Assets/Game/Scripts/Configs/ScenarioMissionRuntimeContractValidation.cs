using System;
using Game.Components;

namespace Game.Configs
{
    internal static class ScenarioMissionRuntimeContractValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario, out string error)
        {
            ScenarioMissionRuntimeConfig runtime = scenario.MissionRuntime;
            if (!runtime.Enabled)
            {
                error = null;
                return true;
            }

            ReadOnlySpan<ScenarioMissionBuildEntryConfig> buildCatalog = runtime.BuildCatalog;
            ScenarioRestrictionConfig restrictions = scenario.Restrictions;
            if (runtime.StartingCredits <= 0 || runtime.StartingMaterials <= 0 ||
                restrictions.BuildingDisabled || restrictions.ProductionDisabled || restrictions.EconomyDisabled ||
                buildCatalog.Length == 0 ||
                !IsGameplayConfigId(runtime.RequiredProducerConfigId, "Building_") ||
                !IsGameplayConfigId(runtime.RequiredUnitConfigId, "Unit_") ||
                !IsScopedId(runtime.BaseMissionRoleId, "role") ||
                !ContainsRequiredAnchor(scenario, runtime.BaseAnchorId, OperationMapAnchorKind.Base) ||
                !ContainsRequiredAnchor(scenario, runtime.BuildZone.AnchorId, OperationMapAnchorKind.Build) ||
                runtime.BuildZone.HalfWidthCells < 1 || runtime.BuildZone.HalfHeightCells < 1)
            {
                error = $"Campaign scenario '{scenario.ScenarioId}' has invalid mission economy, build, or base data.";
                return false;
            }

            bool producerFound = false;
            for (int index = 0; index < buildCatalog.Length; index++)
            {
                ScenarioMissionBuildEntryConfig entry = buildCatalog[index];
                if (!IsGameplayConfigId(entry.BuildingConfigId, "Building_") || entry.MaxCount < 1)
                {
                    error = $"Campaign scenario '{scenario.ScenarioId}' has invalid mission build entry at index {index}.";
                    return false;
                }

                if (entry.BuildingConfigId == runtime.RequiredProducerConfigId)
                    producerFound = true;

                for (int previous = 0; previous < index; previous++)
                {
                    if (buildCatalog[previous].BuildingConfigId == entry.BuildingConfigId)
                    {
                        error = $"Campaign scenario '{scenario.ScenarioId}' has duplicate mission build entry " +
                            $"'{entry.BuildingConfigId}'.";
                        return false;
                    }
                }
            }

            ScenarioDelayedWaveConfig wave = runtime.DelayedWave;
            if (!producerFound || !ContainsGroup(scenario, wave.UnitGroupId) ||
                !ContainsRoute(scenario, wave.RouteId, wave.UnitGroupId) ||
                wave.TargetMissionRoleId != runtime.BaseMissionRoleId ||
                wave.WarningAtMilliseconds < 0 || wave.ActivationAtMilliseconds <= wave.WarningAtMilliseconds)
            {
                error = $"Campaign scenario '{scenario.ScenarioId}' has invalid required producer or delayed wave.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool ContainsGroup(ScenarioSetupConfig scenario, string groupId)
        {
            foreach (ScenarioUnitGroupConfig group in scenario.UnitGroups)
                if (group.GroupId == groupId) return true;
            return false;
        }

        private static bool ContainsRoute(ScenarioSetupConfig scenario, string routeId, string groupId)
        {
            foreach (ScenarioPatrolRouteConfig route in scenario.PatrolRoutes)
                if (route.RouteId == routeId && route.UnitGroupId == groupId) return true;
            return false;
        }

        private static bool ContainsRequiredAnchor(
            ScenarioSetupConfig scenario,
            string anchorId,
            OperationMapAnchorKind kind)
        {
            foreach (ScenarioAnchorRequirementConfig requirement in scenario.RequiredAnchors)
                if (requirement.AnchorId == anchorId && requirement.Kind == kind) return true;
            return false;
        }

        private static bool IsGameplayConfigId(string value, string requiredPrefix)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 96 ||
                !value.StartsWith(requiredPrefix, StringComparison.Ordinal))
                return false;
            foreach (char character in value)
                if (!char.IsLetterOrDigit(character) && character != '_') return false;
            return true;
        }

        private static bool IsScopedId(string value, string prefix) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= 60 &&
            value.StartsWith(prefix + ".", StringComparison.Ordinal);
    }
}
