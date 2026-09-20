using Unity.Entities;

namespace Game.Components
{
    public struct SkirmishStressLaunchRequest : IComponentData
    {
        public int Seed;
        public int Scale;
        public SkirmishStressPhaseCode Phase;
        public SkirmishStressLayoutCode Layout;
        public byte Sequence;
    }

    public struct SkirmishStressSession : IComponentData
    {
        public int Seed;
        public int Scale;
        public SkirmishStressPhaseCode ActivePhase;
        public SkirmishStressLayoutCode Layout;
        public byte Sequence;
        public byte ForceProjected;
        public byte OrdersIssued;
        public byte SpawnComplete;
        public byte SpawnStalled;
        public float PhaseElapsed;
        public float SpawnStallSeconds;
        public int LastObservedSpawned;
        public int RequestedCombat;
        public int RequestedAir;
        public int RequestedSupport;
        public int RequestedBuildings;
    }

    public struct SkirmishStressCensusSample : IBufferElementData
    {
        public SkirmishStressPhaseCode Phase;
        public int RequestedCombat;
        public int RequestedAir;
        public int RequestedSupport;
        public int RequestedBuildings;
        public int SpawnedCombat;
        public int SpawnedAir;
        public int SpawnedSupport;
        public int SpawnedBuildings;
        public int AliveCombat;
        public int AliveAir;
        public int AliveSupport;
        public int AliveBuildings;
        public int DestroyedCombat;
        public int DestroyedAir;
        public int DestroyedSupport;
        public int DestroyedBuildings;
        public int LiveProjectiles;
        public int MissingSpawnCombat;
        public float ElapsedSeconds;
    }

    public enum SkirmishStressPhaseCode : byte
    {
        Warmup = 0,
        Idle = 1,
        MassMove = 2,
        DenseCombat = 3,
        AirTransport = 4,
        Destruction = 5
    }

    public enum SkirmishStressLayoutCode : byte
    {
        Spread = 0,
        Concentrated = 1
    }
}
