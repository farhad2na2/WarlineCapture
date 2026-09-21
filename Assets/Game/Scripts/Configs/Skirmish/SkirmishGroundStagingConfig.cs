using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Ground Staging")]
    public sealed class SkirmishGroundStagingConfig : ScriptableObject
    {
        [SerializeField] private string structureId = "building.ground_staging";
        [SerializeField] private int vehicleQueues = 1;
        [SerializeField] private int logisticsQueues = 1;
        [SerializeField] private string playerPadAnchorRole = "staging.player";
        [SerializeField] private string enemyPadAnchorRole = "staging.enemy";
        [SerializeField] private int contentVersion = 1;

        public string StructureId => structureId;
        public int VehicleQueues => vehicleQueues;
        public int LogisticsQueues => logisticsQueues;
        public string PlayerPadAnchorRole => playerPadAnchorRole;
        public string EnemyPadAnchorRole => enemyPadAnchorRole;
        public int ContentVersion => contentVersion;

        public void ConfigureEstablished()
        {
            structureId = "building.ground_staging";
            vehicleQueues = 1;
            logisticsQueues = 1;
            playerPadAnchorRole = "staging.player";
            enemyPadAnchorRole = "staging.enemy";
            contentVersion = 1;
        }
    }
}
