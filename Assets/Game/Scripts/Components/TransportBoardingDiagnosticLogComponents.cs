using Unity.Collections;
using Unity.Entities;

public struct TransportBoardingDiagnosticLogQueueComponent : IComponentData
{
}

public struct TransportBoardingDiagnosticLogComponent : IBufferElementData
{
    public FixedString512Bytes Message;
}
