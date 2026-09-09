using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct MissionCameraTourBlob
    {
        public int StartHoldMilliseconds,PostHoldMilliseconds,ApproachHoldMilliseconds,ReturnHoldMilliseconds;
        public float SmoothTimeSeconds;
        public float4 PostPerspective,ApproachPerspective;
    }
    public struct CampaignMissionCameraTourState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal,StageStartedAtMilliseconds,OpeningWatchdogMilliseconds;
        public uint SourceVersion;
        public byte Captured,SkipRequested,ReducedMotion,ReturnSettled,FinaleSkipRequested;
        public float3 StartFocus;
        public float4 StartPerspective;
    }
}
