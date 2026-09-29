using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
namespace Game.Components
{
    public struct CampaignMissionSplitFrontState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal, AttackOrders, StoppedOrders, LaunchedShots;
        public Entity Launcher, Battery, Base;
        public byte Initialized, BatteryDestroyed;
    }
    public struct SplitFrontLauncherCommandState : IComponentData
    {
        public Entity CommandedTarget;
        public float3 ProtectedCenter;
        public float ProtectedRadius;
        public int AttackOrders, Stops, Launches;
    }
}
