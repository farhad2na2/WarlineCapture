using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct CampaignMissionFinalKillCinematicComponent : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public float3 VictimPosition;
        public float ElapsedUnscaledSeconds;
        public uint Version;
        public byte Active;
    }
}
