using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum MissionDefenseInteractionKind : byte { ReadWarning = 1, FocusWarning = 2, ContinueExplanation = 3, SkipOptional = 4, ReturnCamera = 12 }

    [InternalBufferCapacity(8)]
    public struct MissionDefenseInteractionRequest : IBufferElementData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public MissionDefenseInteractionKind Kind;
        public int WarningElementIndex;
        public int GuidanceId;
    }
}
