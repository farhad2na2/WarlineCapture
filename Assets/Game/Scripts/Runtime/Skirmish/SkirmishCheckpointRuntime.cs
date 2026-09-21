using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public struct SkirmishCheckpointRequestComponent : IComponentData
    {
        public SkirmishCheckpointStatus Status;
        public int SimulationTick;
    }

    public sealed class SkirmishCheckpointDocumentRecord : IComponentData
    {
        public SkirmishCheckpointDocument Document;
        public string WritePath;
    }
}
