using System;

namespace Game.Skirmish.Contracts
{
    // Own-roster lineage only. No gameplay Entity, enemy fact or order capability.
    public sealed class SkirmishPublicOriginalTransportGroup
    {
        public uint OriginalGroupId,CarrierGroupId;
        public string CarrierPublicId;
        public string[] LivingOriginalIds=Array.Empty<string>();
        public string[] CarrierOriginalIds=Array.Empty<string>();
        public int MountedOriginalCount;
    }
}
