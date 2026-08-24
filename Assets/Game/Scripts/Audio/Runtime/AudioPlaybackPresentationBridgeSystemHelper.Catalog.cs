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
                _ariaLocaleCode = ResolveSavedAriaLocaleCode();
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
