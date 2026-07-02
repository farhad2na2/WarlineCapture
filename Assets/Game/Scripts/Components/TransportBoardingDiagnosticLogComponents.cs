using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct TransportBoardingDiagnosticLogQueueComponent : IComponentData
    {
    }

    public struct TransportBoardingDiagnosticLogComponent : IBufferElementData
    {
        public FixedString512Bytes Message;
    }
}
