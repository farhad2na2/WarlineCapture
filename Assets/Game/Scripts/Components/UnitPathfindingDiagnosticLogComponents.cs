using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct UnitPathfindingDiagnosticLogQueueComponent : IComponentData
    {
    }

    public struct UnitPathfindingDiagnosticLogComponent : IBufferElementData
    {
        public FixedString4096Bytes Message;
    }
}
