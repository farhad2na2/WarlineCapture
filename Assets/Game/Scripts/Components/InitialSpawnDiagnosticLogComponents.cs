using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct InitialSpawnDiagnosticLogQueueComponent : IComponentData
    {
    }

    public struct InitialSpawnDiagnosticLogComponent : IBufferElementData
    {
        public const byte LogSeverity = 0;
        public const byte WarningSeverity = 1;

        public FixedString4096Bytes Message;
        public byte Severity;
    }
}
