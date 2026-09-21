using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Intel")]
    public sealed class SkirmishIntelConfig : ScriptableObject
    {
        [SerializeField] private string intelId = "skirmish.intel.shared_fog.v1";
        [SerializeField] private bool sharedFog = true;
        [SerializeField] private bool developmentFullVision;
        [SerializeField] private int lastSeenExpireSeconds = 20;
        [SerializeField] private int contentVersion = 1;

        public string IntelId => intelId;
        public bool SharedFog => sharedFog;
        public bool DevelopmentFullVision => developmentFullVision;
        public int LastSeenExpireSeconds => lastSeenExpireSeconds;
        public int ContentVersion => contentVersion;

        public void ConfigureSharedFog()
        {
            intelId = "skirmish.intel.shared_fog.v1";
            sharedFog = true;
            developmentFullVision = true;
            lastSeenExpireSeconds = 20;
            contentVersion = 1;
        }
    }
}
