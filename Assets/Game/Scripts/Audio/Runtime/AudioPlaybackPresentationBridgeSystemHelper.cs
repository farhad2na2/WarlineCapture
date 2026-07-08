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
        private readonly Dictionary<uint, AudioEventCatalogEntry> _eventsByHash = new();
        private readonly Dictionary<string, AudioMixerBusEntry> _busesById = new(System.StringComparer.Ordinal);
        private AudioEventCatalogConfig _eventCatalog;
        private AudioMixerBusConfig _mixerBusConfig;
        private int _lastPresentedRequestId;

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
            DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
            DynamicBuffer<AudioPlaybackResultElement> results = em.GetBuffer<AudioPlaybackResultElement>(audioEntity);

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
                AudioPlaybackPresentationResult result = playbackHelper.PlayAcceptedRequest(request, entry, bus, settings);
                AppendPresentationResult(results, request, result, now);

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
    }
}
