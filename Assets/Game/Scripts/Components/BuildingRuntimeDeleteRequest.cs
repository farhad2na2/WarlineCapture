using Unity.Entities;

namespace Game.Components
{
    public struct BuildingRuntimeDeleteRequest : IBufferElementData
    {
        public int BuildingRuntimeId;
        // Default requests retain normal demolition effects. Attempt cleanup removes
        // its temporary runtime building through the existing final-removal owner.
        public byte ImmediateCleanup;
    }
}
