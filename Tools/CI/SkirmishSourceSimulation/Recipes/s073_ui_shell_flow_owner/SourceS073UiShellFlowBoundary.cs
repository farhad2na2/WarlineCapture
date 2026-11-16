using Game.Runtime;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;

// Inert source-host effect sinks. The generated flow copy routes only its
// gateway/audio/music effects through these counters; no runtime queues mutate.
internal static class SourceS073UiShellFlowBoundary
{
    internal static int GatewayCalls;
    internal static int AudioCalls;
    internal static int MusicCalls;
    internal static int EnsureAudioCalls;

    internal static void RegisterGateway() => GatewayCalls++;
    internal static bool TryEnqueue(World world, UIAudioEventRequest request)
    {
        AudioCalls++;
        return false;
    }
    internal static Entity EnsureAudioEntity(EntityManager manager)
    {
        EnsureAudioCalls++;
        return Entity.Null;
    }
    internal static void EnqueueMusicState(EntityManager manager, FixedString64Bytes eventId, uint eventHash,
        float requestedAt, float transitionSeconds, bool loop) => MusicCalls++;
}
