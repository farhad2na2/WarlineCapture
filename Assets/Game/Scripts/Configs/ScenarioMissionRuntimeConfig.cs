using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct ScenarioMissionBuildEntryConfig
    {
        [SerializeField] private string buildingConfigId;
        [SerializeField, Min(1)] private int maxCount;

        public ScenarioMissionBuildEntryConfig(string buildingConfigId, int maxCount)
        {
            this.buildingConfigId = buildingConfigId;
            this.maxCount = maxCount;
        }

        public string BuildingConfigId => buildingConfigId;
        public int MaxCount => maxCount;
    }

    [Serializable]
    public struct ScenarioMissionBuildZoneConfig
    {
        [SerializeField] private string anchorId;
        [SerializeField, Min(1)] private int halfWidthCells;
        [SerializeField, Min(1)] private int halfHeightCells;

        public ScenarioMissionBuildZoneConfig(string anchorId, int halfWidthCells, int halfHeightCells)
        {
            this.anchorId = anchorId;
            this.halfWidthCells = halfWidthCells;
            this.halfHeightCells = halfHeightCells;
        }

        public string AnchorId => anchorId;
        public int HalfWidthCells => halfWidthCells;
        public int HalfHeightCells => halfHeightCells;
    }

    [Serializable]
    public struct ScenarioDelayedWaveConfig
    {
        [SerializeField] private string unitGroupId;
        [SerializeField] private string routeId;
        [SerializeField] private string targetMissionRoleId;
        [SerializeField, Min(0)] private int warningAtMilliseconds;
        [SerializeField, Min(1)] private int activationAtMilliseconds;

        public ScenarioDelayedWaveConfig(
            string unitGroupId,
            string routeId,
            string targetMissionRoleId,
            int warningAtMilliseconds,
            int activationAtMilliseconds)
        {
            this.unitGroupId = unitGroupId;
            this.routeId = routeId;
            this.targetMissionRoleId = targetMissionRoleId;
            this.warningAtMilliseconds = warningAtMilliseconds;
            this.activationAtMilliseconds = activationAtMilliseconds;
        }

        public string UnitGroupId => unitGroupId;
        public string RouteId => routeId;
        public string TargetMissionRoleId => targetMissionRoleId;
        public int WarningAtMilliseconds => warningAtMilliseconds;
        public int ActivationAtMilliseconds => activationAtMilliseconds;
    }

    [Serializable]
    public struct ScenarioMissionRuntimeConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(0)] private int startingCredits;
        [SerializeField, Min(0)] private int startingMaterials;
        [SerializeField] private ScenarioMissionBuildEntryConfig[] buildCatalog;
        [SerializeField] private string requiredProducerConfigId;
        [SerializeField] private string requiredUnitConfigId;
        [SerializeField] private string baseMissionRoleId;
        [SerializeField] private string baseAnchorId;
        [SerializeField] private ScenarioMissionBuildZoneConfig buildZone;
        [SerializeField] private ScenarioDelayedWaveConfig delayedWave;

        public ScenarioMissionRuntimeConfig(
            bool enabled,
            int startingCredits,
            int startingMaterials,
            ScenarioMissionBuildEntryConfig[] buildCatalog,
            string requiredProducerConfigId,
            string requiredUnitConfigId,
            string baseMissionRoleId,
            string baseAnchorId,
            ScenarioMissionBuildZoneConfig buildZone,
            ScenarioDelayedWaveConfig delayedWave)
        {
            this.enabled = enabled;
            this.startingCredits = startingCredits;
            this.startingMaterials = startingMaterials;
            this.buildCatalog = buildCatalog;
            this.requiredProducerConfigId = requiredProducerConfigId;
            this.requiredUnitConfigId = requiredUnitConfigId;
            this.baseMissionRoleId = baseMissionRoleId;
            this.baseAnchorId = baseAnchorId;
            this.buildZone = buildZone;
            this.delayedWave = delayedWave;
        }

        public bool Enabled => enabled;
        public int StartingCredits => startingCredits;
        public int StartingMaterials => startingMaterials;
        public ReadOnlySpan<ScenarioMissionBuildEntryConfig> BuildCatalog => buildCatalog;
        public string RequiredProducerConfigId => requiredProducerConfigId;
        public string RequiredUnitConfigId => requiredUnitConfigId;
        public string BaseMissionRoleId => baseMissionRoleId;
        public string BaseAnchorId => baseAnchorId;
        public ScenarioMissionBuildZoneConfig BuildZone => buildZone;
        public ScenarioDelayedWaveConfig DelayedWave => delayedWave;
    }
}
