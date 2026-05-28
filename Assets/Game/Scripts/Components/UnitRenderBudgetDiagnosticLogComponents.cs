using Unity.Collections;
using Unity.Entities;

public struct UnitRenderBudgetDiagnosticLogQueueComponent : IComponentData
{
}

public struct UnitRenderBudgetDiagnosticLogComponent : IBufferElementData
{
    public const byte LogSeverity = 0;
    public const byte WarningSeverity = 1;

    public FixedString4096Bytes Message;
    public byte Severity;
}
