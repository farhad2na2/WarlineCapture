using Unity.Collections;
using Unity.Entities;
using Game.UI.Runtime;

namespace Game.Configs { }
namespace Game.Components { }

namespace Game.UI.Shell.Ecs
{
    public static class UiShellEcsGateway
    {
        public static int RegistrationBoundaryCalls;
        public static void RegisterAsRuntimeGateway() => RegistrationBoundaryCalls++;
    }

    public static class UiAudioEventBridgeSystem
    {
        public static int CapturedRequests;
        public static bool TryEnqueue(World world, UIAudioEventRequest request)
        {
            CapturedRequests++;
            return false; // Boundary observed; no ECS queue mutation or playback is claimed.
        }
    }
}

namespace Game.Runtime
{
    public struct AudioMusicStateComponent
    {
        public uint CurrentEventHash;
        public uint RequestedEventHash;
    }

    public static class AudioEventRequestSystem
    {
        public static int EnsureEntityCalls;
        public static int CapturedMusicRequests;
        public static Entity EnsureAudioEntity(EntityManager manager)
        {
            EnsureEntityCalls++;
            return manager.AudioEntity;
        }
        public static void EnqueueMusicState(EntityManager manager, FixedString64Bytes eventId, uint eventHash,
            float requestedAt, float transitionSeconds, bool loop)
        {
            CapturedMusicRequests++; // Captured boundary only; does not claim request queue mutation.
        }
    }
}

namespace Unity.Entities
{
    public sealed partial class EntityManager
    {
        public Entity AudioEntity { get; set; } = new(99);
    }
}
