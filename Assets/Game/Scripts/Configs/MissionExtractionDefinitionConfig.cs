using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionExtractionDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string passengerRoleId, carrierRoleId, aircraftRoleId;
        [SerializeField] private string rescueAnchorId, landingAnchorId, departureAnchorId;
        [SerializeField] private int requiredPassengers, secureHoldMilliseconds, deadlineMilliseconds;
        [SerializeField] private float landingRadius, departureRadius;
        [SerializeField] private bool vehiclesSelfSupplied, authoredMapDefensesDormant;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public bool Enabled => enabled;
        public string PassengerRoleId => passengerRoleId;
        public string CarrierRoleId => carrierRoleId;
        public string AircraftRoleId => aircraftRoleId;
        public string RescueAnchorId => rescueAnchorId;
        public string LandingAnchorId => landingAnchorId;
        public string DepartureAnchorId => departureAnchorId;
        public int RequiredPassengers => requiredPassengers;
        public int SecureHoldMilliseconds => secureHoldMilliseconds;
        public int DeadlineMilliseconds => deadlineMilliseconds;
        public float LandingRadius => landingRadius;
        public float DepartureRadius => departureRadius;
        public bool VehiclesSelfSupplied => vehiclesSelfSupplied;
        public bool AuthoredMapDefensesDormant => authoredMapDefensesDormant;
        public MissionCameraTourConfig CameraTour => cameraTour;
    }
}
