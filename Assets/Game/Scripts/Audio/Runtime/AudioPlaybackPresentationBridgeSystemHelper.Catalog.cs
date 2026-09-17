using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class AudioPlaybackPresentationBridgeSystemHelper
    {
        public static string ResolveSavedAriaLocaleCode()
        {
            try
            {
                PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
                return ResolveAriaLocaleCode(profile?.firstLaunchLanguage);
            }
            catch (System.Exception)
            {
                return "en-US";
            }
        }

        public static string ResolveAriaLocaleCode(string persistedLanguage)
        {
            return System.Enum.TryParse(
                       persistedLanguage,
                       ignoreCase: true,
                       out FirstLaunchNarrativeLanguage language) &&
                   language == FirstLaunchNarrativeLanguage.Persian
                ? PersianLocaleCode
                : "en-US";
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

        private string ResolvePlaybackLocale(AudioEventCatalogEntry entry)
        {
            // Settings, tutorials and tactical voices share the active locale. A cached
            // first-launch preference can disagree after changing language or mission.
            // Each catalog entry owns its localized variants and source fallback.
            return GameLocalization.CurrentLocaleCode;
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

            string eventId = !string.IsNullOrWhiteSpace(entry?.EventId)
                ? entry.EventId
                : request.EventId.ToString();

            return AudioPlaybackPresentationSystemHelper.IsGameplayOnly(eventId, busId);
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
