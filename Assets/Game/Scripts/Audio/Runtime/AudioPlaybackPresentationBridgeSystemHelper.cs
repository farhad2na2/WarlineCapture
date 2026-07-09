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

    public sealed class AudioPlaybackPresentationBridgeSystemHelper
    {
        private const float SettingsMusicFadeSeconds = 0.35f;

        private readonly Dictionary<uint, AudioEventCatalogEntry> _eventsByHash = new();
        private readonly Dictionary<string, AudioMixerBusEntry> _busesById = new(System.StringComparer.Ordinal);
        private AudioEventCatalogConfig _eventCatalog;
        private AudioMixerBusConfig _mixerBusConfig;
        private int _lastPresentedRequestId;
        private uint _lastAppliedSettingsVersion;

        public int LastPresentedRequestId => _lastPresentedRequestId;

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
            DynamicBuffer<AudioPlaybackResultElement> results = em.GetBuffer<AudioPlaybackResultElement>(audioEntity);

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
                AudioPlaybackPresentationResult result = ShouldCullGameplayOnlyRequestWhileInactive(
                        simulationActive,
                        request,
                        entry)
                    ? new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "GameplayInactive", -1)
                    : playbackHelper.PlayAcceptedRequest(request, entry, bus, settings);
                AppendPresentationResult(results, request, result, now);
                LogPresentationDiagnostic(em, request, entry, result, now);
                request.Status = result.Played
                    ? AudioPlaybackRequestStatus.Presented
                    : result.Status;
                requests[i] = request;

                presented++;
                if (result.Played)
                    played++;
                else
                    failed++;

                if (request.RequestId > _lastPresentedRequestId)
                    _lastPresentedRequestId = request.RequestId;
            }

            return new AudioPlaybackPresentationBridgeResult(presented, played, failed, _lastPresentedRequestId);
        }

        public void ResetCursor()
        {
            _lastPresentedRequestId = 0;
            _lastAppliedSettingsVersion = 0;
        }

        private void RebuildCachesIfNeeded(AudioEventCatalogConfig eventCatalog, AudioMixerBusConfig mixerBusConfig)
        {
            if (_eventCatalog == eventCatalog && _mixerBusConfig == mixerBusConfig)
                return;

            _eventCatalog = eventCatalog;
            _mixerBusConfig = mixerBusConfig;
            _eventsByHash.Clear();
            _busesById.Clear();

            IReadOnlyList<AudioEventCatalogEntry> events = eventCatalog.Events;
            for (int i = 0; i < events.Count; i++)
            {
                AudioEventCatalogEntry entry = events[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.EventId))
                    continue;

                uint hash = AudioEventIds.StableHash(entry.EventId);
                _eventsByHash[hash] = entry;
            }

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

        private AudioMixerBusEntry ResolveBus(AudioEventCatalogEntry entry, AudioPlaybackRequestElement request)
        {
            string busId = !string.IsNullOrWhiteSpace(entry?.BusId)
                ? entry.BusId
                : request.BusId.ToString();

            if (string.IsNullOrWhiteSpace(busId))
                return null;

            return _busesById.TryGetValue(busId, out AudioMixerBusEntry bus) ? bus : null;
        }

        private static bool IsGameplaySimulationActive(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (query.CalculateEntityCount() == 0)
                return false;

            RuntimeGameplayStateComponent state = query.GetSingleton<RuntimeGameplayStateComponent>();
            return state.PlayRequested != 0 && state.SimulationActive != 0;
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
            DynamicBuffer<AudioPlaybackResultElement> results,
            AudioPlaybackRequestElement request,
            AudioPlaybackPresentationResult result,
            float now)
        {
            results.Add(new AudioPlaybackResultElement
            {
                RequestId = request.RequestId,
                Status = result.Status,
                EventHash = request.EventHash,
                EventId = request.EventId,
                Reason = new FixedString64Bytes(result.Reason),
                ProcessedAt = now
            });
        }

#if UNITY_EDITOR
        private const int MaxAudioPresentationDiagnosticLogs = 64;
        private static int s_AudioPresentationDiagnosticLogCount;

        private static void LogPresentationDiagnostic(
            EntityManager em,
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioPlaybackPresentationResult result,
            float now)
        {
            if (!UnityEngine.Application.isPlaying ||
                s_AudioPresentationDiagnosticLogCount >= MaxAudioPresentationDiagnosticLogs)
            {
                return;
            }

            string requestBus = request.BusId.ToString();
            string catalogBus = entry?.BusId;
            string bus = string.IsNullOrWhiteSpace(catalogBus) ? requestBus : catalogBus;
            if (!IsDiagnosticBus(bus))
                return;

            s_AudioPresentationDiagnosticLogCount++;
            UnityEngine.Debug.Log(
                $"[AudioDiag] Playback event={request.EventId} bus={bus} requestId={request.RequestId} " +
                $"played={(result.Played ? 1 : 0)} status={result.Status} reason={result.Reason} at={now:F2} " +
                $"source={DescribeAudioSourceEntity(em, request.SourceEntity)}");
        }

        private static bool IsDiagnosticBus(string bus)
        {
            return string.Equals(bus, "Alerts", System.StringComparison.Ordinal) ||
                   string.Equals(bus, "Voice", System.StringComparison.Ordinal);
        }

        private static string DescribeAudioSourceEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return "null";

            string displayName = em.HasComponent<UnitDisplayInfo>(entity)
                ? em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString()
                : string.Empty;
            string sourceKey = em.HasComponent<UnitSourcePrefabKey>(entity)
                ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                : string.Empty;
            string faction = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id.ToString()
                : "?";
            string cell = em.HasComponent<UnitGrid>(entity)
                ? FormatCell(em.GetComponentData<UnitGrid>(entity).Cell)
                : "(?,?)";
            string health = em.HasComponent<UnitHealth>(entity)
                ? FormatHealth(em.GetComponentData<UnitHealth>(entity))
                : "?";

            return $"entity={entity.Index}:{entity.Version} name='{displayName}' source='{sourceKey}' faction={faction} cell={cell} hp={health}";
        }

        private static string FormatCell(Unity.Mathematics.int2 cell)
        {
            return $"({cell.x},{cell.y})";
        }

        private static string FormatHealth(UnitHealth health)
        {
            return $"{health.Current}/{health.Max}";
        }
#endif
    }
}
