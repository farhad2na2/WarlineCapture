using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public readonly struct AudioPlaybackPresentationBridgeResult
    {
        public AudioPlaybackPresentationBridgeResult(int presentedCount, int playedCount, int failedCount, int lastPresentedRequestId)
        {
            PresentedCount = presentedCount;
            PlayedCount = playedCount;
            FailedCount = failedCount;
            LastPresentedRequestId = lastPresentedRequestId;
        }

        public int PresentedCount { get; }
        public int PlayedCount { get; }
        public int FailedCount { get; }
        public int LastPresentedRequestId { get; }
    }

    public sealed partial class AudioPlaybackPresentationBridgeSystemHelper : System.IDisposable
    {
        private const float SettingsMusicFadeSeconds = 0.35f;
        private const string AriaMatchEventPrefix = "VO.ARIA.Message.";
        internal const string PersianLocaleCode = "fa-IR";

        private readonly Dictionary<uint, AudioEventCatalogEntry> _eventsByHash = new();
        private readonly Dictionary<string, AudioMixerBusEntry> _busesById = new(System.StringComparer.Ordinal);
        private AudioEventCatalogConfig _eventCatalog;
        private AudioMixerBusConfig _mixerBusConfig;
        private int _lastPresentedRequestId;
        private uint _lastAppliedSettingsVersion;
        private bool _hasCompletePersianAriaCatalog;
        private bool _hasResolvedAriaLocale;
        private string _ariaLocaleCode;
        private readonly AudioGameplayStateQueryCache _simulationStateQuery = new();

        public int LastPresentedRequestId => _lastPresentedRequestId;

        public AudioPlaybackPresentationResult ReconcileCurrentMusicState(
            EntityManager em,
            AudioEventCatalogConfig eventCatalog,
            AudioMixerBusConfig mixerBusConfig,
            AudioPlaybackPresentationSystemHelper playbackHelper,
            float now)
        {
            if (eventCatalog == null || playbackHelper == null)
            {
                return new AudioPlaybackPresentationResult(
                    false,
                    AudioPlaybackRequestStatus.MissingEvent,
                    "PresentationUnavailable",
                    -1);
            }

            RebuildCachesIfNeeded(eventCatalog, mixerBusConfig);

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
            AudioMusicStateComponent musicState = em.GetComponentData<AudioMusicStateComponent>(audioEntity);
            if (musicState.CurrentEventHash == 0u)
            {
                return new AudioPlaybackPresentationResult(
                    false,
                    AudioPlaybackRequestStatus.Culled,
                    "NoCurrentMusicState",
                    -1);
            }

            if (playbackHelper.HasActiveSourceForEvent(musicState.CurrentEventHash))
            {
                return new AudioPlaybackPresentationResult(
                    false,
                    AudioPlaybackRequestStatus.Presented,
                    "AlreadyPresented",
                    -1);
            }

            AudioPlaybackRequestElement request = new()
            {
                Kind = AudioPlaybackRequestKind.MusicState,
                Priority = AudioPlaybackPriority.High,
                Status = AudioPlaybackRequestStatus.Accepted,
                EventHash = musicState.CurrentEventHash,
                EventId = musicState.CurrentEventId,
                BusId = new FixedString32Bytes("Music"),
                PitchMultiplier = 1f,
                RequestedAt = now
            };
            AudioEventCatalogEntry entry = ResolveEvent(request);
            AudioMixerBusEntry bus = ResolveBus(entry, request);
            AudioSettingsComponent settings = em.GetComponentData<AudioSettingsComponent>(audioEntity);
            return playbackHelper.PlayAcceptedRequest(
                request,
                entry,
                bus,
                settings,
                now,
                musicState.TransitionSeconds);
        }

        public AudioPlaybackPresentationBridgeResult DrainAcceptedRequests(
            EntityManager em,
            AudioEventCatalogConfig eventCatalog,
            AudioMixerBusConfig mixerBusConfig,
            AudioPlaybackPresentationSystemHelper playbackHelper,
            float now)
        {
            if (eventCatalog == null || playbackHelper == null)
            {
                return new AudioPlaybackPresentationBridgeResult(0, 0, 0, _lastPresentedRequestId);
            }

            RebuildCachesIfNeeded(eventCatalog, mixerBusConfig);

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
            AudioPlaybackRequestQueueComponent queue = em.GetComponentData<AudioPlaybackRequestQueueComponent>(audioEntity);
            if (queue.LastRequestId < _lastPresentedRequestId)
                _lastPresentedRequestId = 0;

            AudioSettingsComponent settings = em.GetComponentData<AudioSettingsComponent>(audioEntity);
            if (settings.Version != _lastAppliedSettingsVersion)
            {
                playbackHelper.ApplySettingsToActiveSources(settings, now, SettingsMusicFadeSeconds);
                _lastAppliedSettingsVersion = settings.Version;
            }

            DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);

            bool simulationActive = IsGameplaySimulationActive(em);
            int presented = 0;
            int played = 0;
            int failed = 0;

            for (int i = 0; i < requests.Length; i++)
            {
                AudioPlaybackRequestElement request = requests[i];
                if (request.RequestId <= _lastPresentedRequestId ||
                    request.Status != AudioPlaybackRequestStatus.Accepted)
                {
                    continue;
                }

                AudioEventCatalogEntry entry = ResolveEvent(request);
                AudioMixerBusEntry bus = ResolveBus(entry, request);
                float musicTransitionSeconds = request.Kind == AudioPlaybackRequestKind.MusicState
                    ? em.GetComponentData<AudioMusicStateComponent>(audioEntity).TransitionSeconds
                    : 0f;
                AudioPlaybackPresentationResult result = ShouldCullGameplayOnlyRequestWhileInactive(
                        simulationActive,
                        request,
                        entry)
                    ? new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "GameplayInactive", -1)
                    : playbackHelper.PlayAcceptedRequest(
                        request,
                        entry,
                        bus,
                        settings,
                        now,
                        musicTransitionSeconds,
                        ResolvePlaybackLocale(entry));
                request.Status = result.Played
                    ? AudioPlaybackRequestStatus.Presented
                    : result.Status;
                requests[i] = request;
                AppendPresentationResult(em, audioEntity, request, result, now);
#if UNITY_EDITOR
                AudioPlaybackPresentationDiagnostics.Log(em, request, entry, result, now);
#endif

                presented++;
                if (result.Played)
                    played++;
                else
                    failed++;

                if (request.RequestId > _lastPresentedRequestId)
                    _lastPresentedRequestId = request.RequestId;
            }

            if (presented > 0)
                AudioEventRequestSystem.PruneTerminalRequestHistory(requests);

            return new AudioPlaybackPresentationBridgeResult(presented, played, failed, _lastPresentedRequestId);
        }

        public void ResetCursor()
        {
            _lastPresentedRequestId = 0;
            _lastAppliedSettingsVersion = 0;
            _hasResolvedAriaLocale = false;
            _ariaLocaleCode = null;
        }
        public void Dispose()
        {
            _simulationStateQuery.Dispose();
        }

    }
}
