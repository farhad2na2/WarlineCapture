using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionBreachDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string gateBuildingId, coreBuildingId;
        [SerializeField] private string approachAnchorId, gateAnchorId, coreAnchorId, archiveAnchorId;
        [SerializeField] private string supportRoleId, counterattackRoleId;
        [SerializeField] private int gateHealth, coreHealth, secureHoldMilliseconds, deadlineMilliseconds, counterattackWarningMilliseconds;
        [SerializeField] private float archiveRadius;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public bool Enabled => enabled;
        public string GateBuildingId => gateBuildingId;
        public string CoreBuildingId => coreBuildingId;
        public string ApproachAnchorId => approachAnchorId;
        public string GateAnchorId => gateAnchorId;
        public string CoreAnchorId => coreAnchorId;
        public string ArchiveAnchorId => archiveAnchorId;
        public string SupportRoleId => supportRoleId;
        public string CounterattackRoleId => counterattackRoleId;
        public int GateHealth => gateHealth;
        public int CoreHealth => coreHealth;
        public int SecureHoldMilliseconds => secureHoldMilliseconds;
        public int DeadlineMilliseconds => deadlineMilliseconds;
        public int CounterattackWarningMilliseconds => counterattackWarningMilliseconds;
        public float ArchiveRadius => archiveRadius;
        public MissionCameraTourConfig CameraTour => cameraTour;
    }
}
