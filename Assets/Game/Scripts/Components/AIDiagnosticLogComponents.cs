using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct AIDiagnosticLogQueueComponent : IComponentData
    {
    }

    public struct AIDiagnosticLogComponent : IBufferElementData
    {
        public const byte LogSeverity = 0;
        public const byte WarningSeverity = 1;

        public FixedString512Bytes Message;
        public byte Severity;
    }
}
