using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UiAudioEventBridgeSystem : ISystem
    {
        private static int s_SubscriptionCount;

        public void OnCreate(ref SystemState state)
        {
            RegisterRuntimeBridge();
            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            UnregisterRuntimeBridge();
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public static bool TryEnqueue(World world, UIAudioEventRequest request)
        {
            if (world == null || !world.IsCreated || string.IsNullOrEmpty(request.EventId) || request.EventHash == 0u)
                return false;

            AudioEventRequestSystem.EnqueueOneShot(
                world.EntityManager,
                new FixedString64Bytes(request.EventId),
                request.EventHash,
                new FixedString32Bytes(request.BusId),
                AudioPlaybackPriority.Medium,
                requestedAt: 0f,
                cooldownSeconds: request.CooldownSeconds);
            return true;
        }

        private static void RegisterRuntimeBridge()
        {
            if (s_SubscriptionCount++ == 0)
                UIAudioEventGateway.AudioEventRequested += EnqueueIntoDefaultWorld;
        }

        private static void UnregisterRuntimeBridge()
        {
            if (s_SubscriptionCount <= 0)
                return;

            s_SubscriptionCount--;
            if (s_SubscriptionCount == 0)
                UIAudioEventGateway.AudioEventRequested -= EnqueueIntoDefaultWorld;
        }

        private static void EnqueueIntoDefaultWorld(UIAudioEventRequest request)
        {
            TryEnqueue(World.DefaultGameObjectInjectionWorld, request);
        }
    }
}
