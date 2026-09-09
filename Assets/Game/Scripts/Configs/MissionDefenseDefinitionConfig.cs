using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionConvoyElementConfig
    {
        [SerializeField] private string elementId;
        [SerializeField] private string unitGroupId;
        [SerializeField] private string routeId;
        [SerializeField] private string contactAnchorId;
        [SerializeField] private int warningAtMilliseconds;
        [SerializeField] private int activationAtMilliseconds;
        [SerializeField] private int contactAtMilliseconds;
        public string ElementId => elementId;
        public string UnitGroupId => unitGroupId;
        public string RouteId => routeId;
        public string ContactAnchorId => contactAnchorId;
        public int WarningAtMilliseconds => warningAtMilliseconds;
        public int ActivationAtMilliseconds => activationAtMilliseconds;
        public int ContactAtMilliseconds => contactAtMilliseconds;
    }

    [Serializable]
    public struct MissionDefenseDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private bool vehiclesSelfSupplied;
        [SerializeField] private bool authoredMapDefensesDormant;
        [SerializeField] private string forwardPostStableId;
        [SerializeField] private string innerCoreAnchorId;
        [SerializeField] private float innerCoreRadius;
        [SerializeField] private string sensorMissionRoleId;
        [SerializeField] private string initialProducerAnchorId;
        [SerializeField] private int radarPingCharges;
        [SerializeField] private int radarPingCooldownMilliseconds;
        [SerializeField] private MissionConvoyElementConfig[] convoyElements;
        [SerializeField] private MissionGuidanceStepConfig[] guidanceSteps;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public MissionCameraTourConfig CameraTour=>cameraTour;
        public bool Enabled => enabled;
        public bool VehiclesSelfSupplied => vehiclesSelfSupplied;
        public bool AuthoredMapDefensesDormant => authoredMapDefensesDormant;
        public string ForwardPostStableId => forwardPostStableId;
        public string InnerCoreAnchorId => innerCoreAnchorId;
        public float InnerCoreRadius => innerCoreRadius;
        public string SensorMissionRoleId => sensorMissionRoleId;
        public string InitialProducerAnchorId => initialProducerAnchorId;
        public int RadarPingCharges => radarPingCharges;
        public int RadarPingCooldownMilliseconds => radarPingCooldownMilliseconds;
        public ReadOnlySpan<MissionConvoyElementConfig> ConvoyElements => convoyElements;
        public ReadOnlySpan<MissionGuidanceStepConfig> GuidanceSteps => guidanceSteps;
    }
}
