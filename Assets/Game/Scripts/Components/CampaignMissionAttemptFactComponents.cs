using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct CampaignMissionAttemptFactsComponent : IComponentData
    {
        public int ElapsedMilliseconds;
        public int SquadLossCount;
        public int HostileTotalCount;
        public int HostileDefeatedCount;
        public int RequiredBuildingPlacedCount;
        public int RequiredBuildingCompletedCount;
        public int RequiredUnitProducedCount;
        public byte DefenseWaveWarningIssued;
        public byte DefenseWaveActivated;
        public byte CommandSquadSpawned;
        public byte CommandSquadAlive;
        public byte MoveToCoverComplete;
        public byte ThreatConfirmed;
        public byte AttackIssued;
        public byte FinalePresentationRequired;
        public byte FinalePresentationComplete;
    }

    public struct CampaignMissionAttemptFactProjectionStateComponent : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public int BuildingRequestBaselineId;
        public int ProducedUnitReadModelBaselineCount;
        public uint SourceVersion;
        public byte Initialized;
    }
}
