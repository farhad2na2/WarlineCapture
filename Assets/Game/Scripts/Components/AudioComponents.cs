using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum AudioPlaybackPriority : byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public enum AudioPlaybackRequestKind : byte
    {
        OneShot = 0,
        MusicState = 1,
        AmbienceState = 2,
        StopEvent = 3,
        StopBus = 4
    }

    public enum AudioPlaybackRequestStatus : byte
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        CooldownSkipped = 3,
        MissingEvent = 4,
        MissingClip = 5,
        Culled = 6
    }

    public struct AudioPlaybackRequestQueueComponent : IComponentData
    {
        public int LastRequestId;
        public uint Version;
    }

    public struct AudioPlaybackRequestElement : IBufferElementData
    {
        public int RequestId;
        public int Frame;
        public AudioPlaybackRequestKind Kind;
        public AudioPlaybackPriority Priority;
        public uint EventHash;
        public FixedString64Bytes EventId;
        public FixedString32Bytes BusId;
        public Entity SourceEntity;
        public float3 WorldPosition;
        public float VolumeDecibels;
        public float PitchMultiplier;
        public float RequestedAt;
        public byte HasWorldPosition;
        public byte Spatial;
        public byte InterruptsLowerPriority;
    }

    public struct AudioPlaybackResultElement : IBufferElementData
    {
        public int RequestId;
        public AudioPlaybackRequestStatus Status;
        public uint EventHash;
        public FixedString64Bytes EventId;
        public FixedString64Bytes Reason;
        public float ProcessedAt;
    }

    public struct AudioSettingsComponent : IComponentData
    {
        public uint Version;
        public float MasterVolume;
        public float UiVolume;
        public float SfxVolume;
        public float AlertsVolume;
        public float MusicVolume;
        public float AmbienceVolume;
        public float VoiceVolume;
        public byte MasterMuted;
        public byte UiMuted;
        public byte SfxMuted;
        public byte AlertsMuted;
        public byte MusicMuted;
        public byte AmbienceMuted;
        public byte VoiceMuted;
    }

    public struct AudioMusicStateComponent : IComponentData
    {
        public uint Version;
        public uint CurrentEventHash;
        public uint RequestedEventHash;
        public FixedString64Bytes CurrentEventId;
        public FixedString64Bytes RequestedEventId;
        public float Intensity;
        public float TransitionSeconds;
        public float RequestedAt;
        public byte Loop;
        public byte IsTransitioning;
    }

    public struct AudioListenerStateComponent : IComponentData
    {
        public uint Version;
        public float3 Position;
        public float3 Forward;
        public float MaxAudibleDistance;
        public byte HasListener;
    }
}
