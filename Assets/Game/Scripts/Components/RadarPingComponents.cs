using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum RadarPingResultKind : byte
    { None = 0, Pending = 1, Accepted = 2, Unavailable = 3, Paused = 4, Cooldown = 5, NoCharges = 6, NoSensor = 7, Busy = 8 }

    public struct RadarPingState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public uint LastRequestId;
        public uint PendingRequestId;
        public uint Version;
        public Entity PendingSensor;
        public int Charges;
        public int CooldownMilliseconds;
        public int ReadyAtMilliseconds;
        public int LastContactCount;
        public RadarPingResultKind Result;
    }

    [InternalBufferCapacity(8)]
    public struct RadarPingRequest : IBufferElementData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public uint RequestId;
        public Entity Sensor;
    }
}
