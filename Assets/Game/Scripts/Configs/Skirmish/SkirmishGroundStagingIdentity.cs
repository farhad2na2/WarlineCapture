using UnityEngine;

namespace Game.Configs
{
    public sealed class SkirmishGroundStagingIdentity : MonoBehaviour
    {
        [SerializeField] private string structureId = "building.ground_staging";
        [SerializeField] private string displayNameEn = "Ground Staging";
        [SerializeField] private string displayNameFa = "سکوی زمینی";
        [SerializeField] private int vehicleQueues = 1;
        [SerializeField] private int logisticsQueues = 1;
        [SerializeField] private bool expertTent = false;

        public string StructureId => structureId;
        public string DisplayNameEn => displayNameEn;
        public string DisplayNameFa => displayNameFa;
        public int VehicleQueues => vehicleQueues;
        public int LogisticsQueues => logisticsQueues;
        public bool IsExpertTent => expertTent;

        public void ConfigureEstablished()
        {
            structureId = "building.ground_staging";
            displayNameEn = "Ground Staging";
            displayNameFa = "سکوی زمینی";
            vehicleQueues = 1;
            logisticsQueues = 1;
            expertTent = false;
        }
    }
}
