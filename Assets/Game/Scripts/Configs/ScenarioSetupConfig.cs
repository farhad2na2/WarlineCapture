using System;
using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct ScenarioAnchorRequirementConfig
    {
        [SerializeField] private string anchorId;
        [SerializeField] private OperationMapAnchorKind kind;

        public ScenarioAnchorRequirementConfig(string anchorId, OperationMapAnchorKind kind)
        {
            this.anchorId = anchorId;
            this.kind = kind;
        }

        public string AnchorId => anchorId;
        public OperationMapAnchorKind Kind => kind;

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidAnchorId(anchorId))
            {
                error = $"Invalid required operation-map anchor id: '{anchorId ?? "<null>"}'.";
                return false;
            }

            if (kind == OperationMapAnchorKind.None)
            {
                error = $"Required operation-map anchor '{anchorId}' must declare a kind.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public struct ScenarioUnitEntryConfig
    {
        [SerializeField] private string unitConfigKey;
        [SerializeField] private string runtimePrefabSourceKey;
        [SerializeField] private string expectedAssetGuid;
        [SerializeField] private string spawnAnchorId;
        [SerializeField] private string missionRoleId;
        [SerializeField, Min(1)] private int count;

        public ScenarioUnitEntryConfig(
            string unitConfigKey, string runtimePrefabSourceKey, string expectedAssetGuid,
            string spawnAnchorId, string missionRoleId, int count)
        {
            this.unitConfigKey = unitConfigKey;
            this.runtimePrefabSourceKey = runtimePrefabSourceKey;
            this.expectedAssetGuid = expectedAssetGuid;
            this.spawnAnchorId = spawnAnchorId;
            this.missionRoleId = missionRoleId;
            this.count = count;
        }
        public string UnitConfigKey => unitConfigKey;
        public string RuntimePrefabSourceKey => runtimePrefabSourceKey;
        public string ExpectedAssetGuid => expectedAssetGuid;
        public string SpawnAnchorId => spawnAnchorId;
        public string MissionRoleId => missionRoleId;
        public int Count => count;
    }

    [Serializable]
    public struct ScenarioUnitGroupConfig
    {
        [SerializeField] private string groupId;
        [SerializeField] private byte factionIndex;
        [SerializeField] private ScenarioUnitEntryConfig[] units;

        public ScenarioUnitGroupConfig(string groupId, byte factionIndex, ScenarioUnitEntryConfig[] units)
        {
            this.groupId = groupId;
            this.factionIndex = factionIndex;
            this.units = units;
        }
        public string GroupId => groupId;
        public byte FactionIndex => factionIndex;
        public ReadOnlySpan<ScenarioUnitEntryConfig> Units => units;
    }

    [Serializable]
    public struct ScenarioPatrolRouteConfig
    {
        [SerializeField] private string routeId;
        [SerializeField] private string unitGroupId;
        [SerializeField] private string[] anchorIds;
        [SerializeField, Min(0)] private int startDelayMilliseconds;

        public ScenarioPatrolRouteConfig(
            string routeId, string unitGroupId, string[] anchorIds, int startDelayMilliseconds)
        {
            this.routeId = routeId;
            this.unitGroupId = unitGroupId;
            this.anchorIds = anchorIds;
            this.startDelayMilliseconds = startDelayMilliseconds;
        }

        public string RouteId => routeId;
        public string UnitGroupId => unitGroupId;
        public ReadOnlySpan<string> AnchorIds => anchorIds;
        public int StartDelayMilliseconds => startDelayMilliseconds;
    }

    [Serializable]
    public struct ScenarioRestrictionConfig
    {
        [SerializeField] private bool buildingDisabled;
        [SerializeField] private bool productionDisabled;
        [SerializeField] private bool economyDisabled;
        [SerializeField] private bool transportDisabled;
        [SerializeField] private bool airDisabled;

        public ScenarioRestrictionConfig(
            bool buildingDisabled, bool productionDisabled, bool economyDisabled,
            bool transportDisabled, bool airDisabled)
        {
            this.buildingDisabled = buildingDisabled;
            this.productionDisabled = productionDisabled;
            this.economyDisabled = economyDisabled;
            this.transportDisabled = transportDisabled;
            this.airDisabled = airDisabled;
        }

        public bool BuildingDisabled => buildingDisabled;
        public bool ProductionDisabled => productionDisabled;
        public bool EconomyDisabled => economyDisabled;
        public bool TransportDisabled => transportDisabled;
        public bool AirDisabled => airDisabled;
    }

    [Serializable]
    public struct ScenarioAmbientPresentationConfig
    {
        [SerializeField] private string presentationId;
        [SerializeField] private string anchorId;
        [SerializeField] private string routeId;
        [SerializeField, Min(1)] private int instanceCount;

        public ScenarioAmbientPresentationConfig(
            string presentationId, string anchorId, string routeId, int instanceCount)
        {
            this.presentationId = presentationId;
            this.anchorId = anchorId;
            this.routeId = routeId;
            this.instanceCount = instanceCount;
        }

        public string PresentationId => presentationId;
        public string AnchorId => anchorId;
        public string RouteId => routeId;
        public int InstanceCount => instanceCount;
    }

    [CreateAssetMenu(menuName = "Game/Operation Maps/Scenario Setup")]
    public sealed class ScenarioSetupConfig : ScriptableObject
    {
        [SerializeField] private string scenarioId;
        [SerializeField] private string operationMapId;
        [SerializeField] private ScenarioAnchorRequirementConfig[] requiredAnchors =
            Array.Empty<ScenarioAnchorRequirementConfig>();
        [Header("Campaign scenario (default-safe for Skirmish)")]
        [SerializeField] private int deterministicSeed;
        [SerializeField, Min(0)] private int encounterStartMilliseconds;
        [SerializeField] private ScenarioUnitGroupConfig[] unitGroups = Array.Empty<ScenarioUnitGroupConfig>();
        [SerializeField] private ScenarioPatrolRouteConfig[] patrolRoutes = Array.Empty<ScenarioPatrolRouteConfig>();
        [SerializeField] private ScenarioRestrictionConfig restrictions;
        [SerializeField] private ScenarioAmbientPresentationConfig[] ambientPresentations =
            Array.Empty<ScenarioAmbientPresentationConfig>();

        public string ScenarioId => scenarioId;
        public string OperationMapId => operationMapId;
        public ReadOnlySpan<ScenarioAnchorRequirementConfig> RequiredAnchors => requiredAnchors;
        public int DeterministicSeed => deterministicSeed;
        public int EncounterStartMilliseconds => encounterStartMilliseconds;
        public ReadOnlySpan<ScenarioUnitGroupConfig> UnitGroups => unitGroups;
        public ReadOnlySpan<ScenarioPatrolRouteConfig> PatrolRoutes => patrolRoutes;
        public ScenarioRestrictionConfig Restrictions => restrictions;
        public ReadOnlySpan<ScenarioAmbientPresentationConfig> AmbientPresentations => ambientPresentations;

        public bool TryValidateIdentity(out string error)
        {
            if (!OperationMapIdentityRules.IsValidScenarioId(scenarioId))
            {
                error = $"Invalid scenario id: '{scenarioId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidate(out string error)
        {
            if (!TryValidateIdentity(out error))
                return false;

            if (requiredAnchors == null)
            {
                error = $"Scenario '{scenarioId}' required-anchor collection is missing.";
                return false;
            }

            for (int index = 0; index < requiredAnchors.Length; index++)
            {
                if (!requiredAnchors[index].TryValidate(out error))
                    return false;

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                            requiredAnchors[index].AnchorId,
                            requiredAnchors[previous].AnchorId,
                            StringComparison.Ordinal))
                    {
                        error = $"Scenario '{scenarioId}' has duplicate required anchor " +
                                $"'{requiredAnchors[index].AnchorId}'.";
                        return false;
                    }
                }
            }

            if (!IsCampaignScenario())
            {
                error = null;
                return true;
            }

            if (deterministicSeed == 0 || encounterStartMilliseconds < 0 ||
                unitGroups == null || unitGroups.Length < 2 || patrolRoutes == null ||
                ambientPresentations == null)
            {
                error = $"Campaign scenario '{scenarioId}' is missing deterministic setup or force groups.";
                return false;
            }

            if (!TryValidateUnitGroups(out error) || !TryValidatePatrolRoutes(out error) ||
                !TryValidateAmbientPresentations(out error))
                return false;

            error = null;
            return true;
        }

        private bool IsCampaignScenario() =>
            scenarioId != null && scenarioId.StartsWith("scenario.ch", StringComparison.Ordinal);

        private bool TryValidateUnitGroups(out string error)
        {
            for (int groupIndex = 0; groupIndex < unitGroups.Length; groupIndex++)
            {
                ScenarioUnitGroupConfig group = unitGroups[groupIndex];
                if (!IsScopedId(group.GroupId, "group") || group.FactionIndex == 0 || group.Units.Length == 0)
                {
                    error = $"Campaign scenario '{scenarioId}' has invalid unit group at index {groupIndex}.";
                    return false;
                }
                for (int previous = 0; previous < groupIndex; previous++)
                {
                    if (unitGroups[previous].GroupId == group.GroupId)
                    {
                        error = $"Campaign scenario '{scenarioId}' has duplicate group '{group.GroupId}'.";
                        return false;
                    }
                }
                foreach (ScenarioUnitEntryConfig unit in group.Units)
                {
                    if (!IsScopedId(unit.UnitConfigKey, "unit") || string.IsNullOrWhiteSpace(unit.RuntimePrefabSourceKey) ||
                        unit.RuntimePrefabSourceKey.Length > 63 || !IsLowerHexGuid(unit.ExpectedAssetGuid) ||
                        !OperationMapIdentityRules.IsValidAnchorId(unit.SpawnAnchorId) ||
                        !IsScopedId(unit.MissionRoleId, "role") || unit.Count < 1)
                    {
                        error = $"Campaign scenario '{scenarioId}' group '{group.GroupId}' has an invalid unit.";
                        return false;
                    }
                }
            }
            error = null;
            return true;
        }

        private bool TryValidatePatrolRoutes(out string error)
        {
            for (int index = 0; index < patrolRoutes.Length; index++)
            {
                ScenarioPatrolRouteConfig route = patrolRoutes[index];
                if (!IsScopedId(route.RouteId, "route") || !ContainsGroup(route.UnitGroupId) ||
                    route.AnchorIds.Length < 2 || route.StartDelayMilliseconds < 0)
                {
                    error = $"Campaign scenario '{scenarioId}' has invalid patrol route at index {index}.";
                    return false;
                }
                foreach (string anchorId in route.AnchorIds)
                {
                    if (!OperationMapIdentityRules.IsValidAnchorId(anchorId))
                    {
                        error = $"Campaign scenario '{scenarioId}' patrol route '{route.RouteId}' has invalid anchors.";
                        return false;
                    }
                }
            }
            error = null;
            return true;
        }

        private bool TryValidateAmbientPresentations(out string error)
        {
            foreach (ScenarioAmbientPresentationConfig ambient in ambientPresentations)
            {
                if (!IsScopedId(ambient.PresentationId, "ambient") ||
                    !OperationMapIdentityRules.IsValidAnchorId(ambient.AnchorId) ||
                    (!string.IsNullOrEmpty(ambient.RouteId) && !IsScopedId(ambient.RouteId, "route")) ||
                    ambient.InstanceCount < 1)
                {
                    error = $"Campaign scenario '{scenarioId}' has invalid ambient presentation.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private bool ContainsGroup(string groupId)
        {
            foreach (ScenarioUnitGroupConfig group in unitGroups)
                if (group.GroupId == groupId) return true;
            return false;
        }

        private static bool IsScopedId(string value, string prefix) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= 60 &&
            value.StartsWith(prefix + ".", StringComparison.Ordinal);

        private static bool IsLowerHexGuid(string value)
        {
            if (value == null || value.Length != 32) return false;
            foreach (char character in value)
                if (!(character is >= '0' and <= '9' or >= 'a' and <= 'f')) return false;
            return true;
        }
    }
}
