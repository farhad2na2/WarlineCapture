using Unity.Collections;
using Unity.Entities;

public struct AIDiagnosticLogQueueComponent : IComponentData
{
}

public struct AIDiagnosticLogComponent : IBufferElementData
{
    public FixedString512Bytes Message;
}
