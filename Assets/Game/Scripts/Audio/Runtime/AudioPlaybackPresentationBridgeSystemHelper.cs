using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class AriaVoiceLanguageResolver
    {
        public const string EnglishLocaleCode = "en-US";
        public const string PersianLocaleCode = "fa-IR";

        public static string ResolveSavedLocaleCode()
        {
            try
            {
                PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
                return ResolveLocaleCode(profile?.firstLaunchLanguage);
            }
            catch (System.Exception)
            {
                return EnglishLocaleCode;
            }
        }

        public static string ResolveLocaleCode(string persistedLanguage)
        {
            return System.Enum.TryParse(
                       persistedLanguage,
                       ignoreCase: true,
                       out FirstLaunchNarrativeLanguage language) &&
                   language == FirstLaunchNarrativeLanguage.Persian
                ? PersianLocaleCode
                : EnglishLocaleCode;
        }
    }

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

    public sealed class AudioPlaybackPresentationBridgeSystemHelper : System.IDisposable
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

        private void RebuildCachesIfNeeded(AudioEventCatalogConfig eventCatalog, AudioMixerBusConfig mixerBusConfig)
        {
            if (_eventCatalog == eventCatalog && _mixerBusConfig == mixerBusConfig)
                return;

            _eventCatalog = eventCatalog;
            _mixerBusConfig = mixerBusConfig;
            _eventsByHash.Clear();
            _busesById.Clear();

            int ariaMatchEventCount = 0;
            int persianAriaMatchEventCount = 0;

            IReadOnlyList<AudioEventCatalogEntry> events = eventCatalog.Events;
            for (int i = 0; i < events.Count; i++)
            {
                AudioEventCatalogEntry entry = events[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.EventId))
                    continue;

                uint hash = AudioEventIds.StableHash(entry.EventId);
                _eventsByHash[hash] = entry;
                if (entry.EventId.StartsWith(AriaMatchEventPrefix, System.StringComparison.Ordinal))
                {
                    ariaMatchEventCount++;
                    if (entry.HasLocalizedClips(PersianLocaleCode))
                        persianAriaMatchEventCount++;
                }
            }

            _hasCompletePersianAriaCatalog =
                ariaMatchEventCount > 0 &&
                persianAriaMatchEventCount == ariaMatchEventCount;

            if (mixerBusConfig == null)
                return;

            IReadOnlyList<AudioMixerBusEntry> buses = mixerBusConfig.Buses;
            for (int i = 0; i < buses.Count; i++)
            {
                AudioMixerBusEntry bus = buses[i];
                if (bus == null || string.IsNullOrWhiteSpace(bus.BusId))
                    continue;

                _busesById[bus.BusId] = bus;
            }
        }

        private AudioEventCatalogEntry ResolveEvent(AudioPlaybackRequestElement request)
        {
            if (request.EventHash != 0u && _eventsByHash.TryGetValue(request.EventHash, out AudioEventCatalogEntry entry))
                return entry;

            if (request.EventId.Length == 0)
                return null;

            uint hash = AudioEventIds.StableHash(request.EventId.ToString());
            return _eventsByHash.TryGetValue(hash, out entry) ? entry : null;
        }

        private string ResolvePlaybackLocale(AudioEventCatalogEntry entry)
        {
            if (!_hasCompletePersianAriaCatalog ||
                entry == null ||
                !entry.EventId.StartsWith(AriaMatchEventPrefix, System.StringComparison.Ordinal))
            {
                return null;
            }

            if (!_hasResolvedAriaLocale)
            {
                _ariaLocaleCode = AriaVoiceLanguageResolver.ResolveSavedLocaleCode();
                _hasResolvedAriaLocale = true;
            }

            return _ariaLocaleCode;
        }

        private AudioMixerBusEntry ResolveBus(AudioEventCatalogEntry entry, AudioPlaybackRequestElement request)
        {
            string busId = !string.IsNullOrWhiteSpace(entry?.BusId)
                ? entry.BusId
                : request.BusId.ToString();

            if (string.IsNullOrWhiteSpace(busId))
                return null;

            return _busesById.TryGetValue(busId, out AudioMixerBusEntry bus) ? bus : null;
        }

        private bool IsGameplaySimulationActive(EntityManager em)
        {
            return _simulationStateQuery.IsSimulationActive(em);
        }

        private static bool ShouldCullGameplayOnlyRequestWhileInactive(
            bool simulationActive,
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry)
        {
            if (simulationActive)
                return false;

            string busId = !string.IsNullOrWhiteSpace(entry?.BusId)
                ? entry.BusId
                : request.BusId.ToString();

            if (string.Equals(busId, "Voice", System.StringComparison.Ordinal) ||
                string.Equals(busId, "Alerts", System.StringComparison.Ordinal))
            {
                return true;
            }

            string eventId = !string.IsNullOrWhiteSpace(entry?.EventId)
                ? entry.EventId
                : request.EventId.ToString();

            return eventId.StartsWith("Gameplay.", System.StringComparison.Ordinal) ||
                   eventId.StartsWith("Alert.", System.StringComparison.Ordinal) ||
                   eventId.StartsWith("VO.ARIA", System.StringComparison.Ordinal);
        }

        private static void AppendPresentationResult(
            EntityManager em,
            Entity audioEntity,
            AudioPlaybackRequestElement request,
            AudioPlaybackPresentationResult result,
            float now)
        {
            AudioEventRequestSystem.AppendPlaybackResult(em, audioEntity, new AudioPlaybackResultElement
            {
                RequestId = request.RequestId,
                Status = result.Played ? AudioPlaybackRequestStatus.Presented : result.Status,
                EventHash = request.EventHash,
                EventId = request.EventId,
                Reason = new FixedString64Bytes(result.Reason),
                ProcessedAt = now
            });
        }

    }
}
