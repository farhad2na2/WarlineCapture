using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    // Slot is the stable public convoy.1/.2/.3 role. The actual native actor ID
    // remains separately owned by SkirmishAttemptOwnedComponent.
    public struct SkirmishConvoyObjectiveTruckComponent : IComponentData
    {
        public byte Slot;
        public int DefinitionVersion;
        public FixedString128Bytes DefinitionFingerprint;
    }
}
