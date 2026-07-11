using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class AudioPlaybackPresentationRuntimeView : MonoBehaviour
    {
        [SerializeField] private AudioEventCatalogConfig eventCatalog;
        [SerializeField] private AudioMixerBusConfig mixerBusConfig;
        [SerializeField, Min(0)] private int initialPoolSize = 8;
        [SerializeField, Min(1)] private int maxPoolSize = 32;

        private readonly AudioPlaybackPresentationBridgeSystemHelper _bridge = new();
        private AudioPlaybackPresentationSystemHelper _playbackHelper;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private Aph405AndroidVoicePilotProbe _voicePilotProbe;
#endif

        public int LastPresentedRequestId => _bridge.LastPresentedRequestId;
        public int ActiveSourceCount => _playbackHelper?.ActiveSourceCount ?? 0;
        public int PoolSize => _playbackHelper?.PoolSize ?? 0;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (!EnsureInitialized())
                return;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _voicePilotProbe?.Tick();
#endif
            _playbackHelper.UpdatePool(Time.unscaledTime);

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            _bridge.DrainAcceptedRequests(
                world.EntityManager,
                eventCatalog,
                mixerBusConfig,
                _playbackHelper,
                Time.unscaledTime);
        }

        private void OnDisable()
        {
            _playbackHelper?.StopAll();
        }

        private void OnDestroy()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _voicePilotProbe?.Dispose();
            _voicePilotProbe = null;
#endif
            _playbackHelper?.Dispose();
            _playbackHelper = null;
        }

        private bool EnsureInitialized()
        {
            if (eventCatalog == null)
                return false;

            if (_playbackHelper != null)
                return true;

            int resolvedMaxPoolSize = Mathf.Max(1, maxPoolSize);
            int resolvedInitialPoolSize = Mathf.Clamp(initialPoolSize, 0, resolvedMaxPoolSize);
            _playbackHelper = new AudioPlaybackPresentationSystemHelper(
                transform,
                resolvedInitialPoolSize,
                resolvedMaxPoolSize);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Application.isPlaying && ShouldRunVoicePilotProbe())
                _voicePilotProbe = new Aph405AndroidVoicePilotProbe(eventCatalog, transform);
#endif
            return true;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static bool ShouldRunVoicePilotProbe()
        {
            return Aph405VoicePilotProbeContract.HasCommandLineArgument(
                System.Environment.GetCommandLineArgs(),
                Aph405VoicePilotProbeContract.EditorCommandLineArgument);
        }
#endif
    }
}
