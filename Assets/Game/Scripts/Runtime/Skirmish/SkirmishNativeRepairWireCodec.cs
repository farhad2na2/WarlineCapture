using System;
using UnityEngine;

namespace Game.Runtime
{
    // Version one remains in its existing owner byte-for-byte. Version two adds
    // a separately versioned repair DTO under one checksum with the base JSON.
    internal static class SkirmishNativeRepairWireCodec
    {
        internal const int Version=2;
        [Serializable] private sealed class Wire
        {
            public int Version;
            public string Payload,RepairPayload,Checksum;
        }
        internal static string Encode(string nativePayload,SkirmishNativeRepairCheckpoint.Payload repairs)
        {
            if(string.IsNullOrEmpty(nativePayload)||repairs==null)return null;
            string repairPayload=JsonUtility.ToJson(repairs);
            return JsonUtility.ToJson(new Wire {Version=Version,Payload=nativePayload,RepairPayload=repairPayload,
                Checksum=SkirmishNativeRepairWireHash.Compute(nativePayload,repairPayload)});
        }
        internal static bool TryDecode(string json,out string nativePayload,out SkirmishNativeRepairCheckpoint.Payload repairs)
        {
            nativePayload=null;repairs=null;
            try
            {
                var wire=JsonUtility.FromJson<Wire>(json);
                if(wire==null||wire.Version!=Version||string.IsNullOrEmpty(wire.Payload)||string.IsNullOrEmpty(wire.RepairPayload)||
                    !string.Equals(wire.Checksum,SkirmishNativeRepairWireHash.Compute(wire.Payload,wire.RepairPayload),StringComparison.Ordinal))return false;
                var decoded=JsonUtility.FromJson<SkirmishNativeRepairCheckpoint.Payload>(wire.RepairPayload);
                if(decoded==null)return false;
                nativePayload=wire.Payload;repairs=decoded;return true;
            }
            catch(ArgumentException){return false;}
        }
    }
}
