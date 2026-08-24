using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Runtime;
namespace Game.Composition
{
    internal static class FirstLaunchNarrativeModelUtilitySystemHelper
    {
        public static string FindStateId(NarrativeSequenceConfig config, NarrativeRouteRole role)
        {
            if (config == null)
                return string.Empty;
            for (int i = 0; i < config.States.Count; i++)
            {
                if (config.States[i]?.RouteRole == role)
                    return config.States[i].StateId;
            }
            return string.Empty;
        }

        public static NarrativeRouteRequest CreateRouteRequest(
            IReadOnlyDictionary<string, NarrativeStateRecord> states,
            string destinationStateId)
        {
            states.TryGetValue(destinationStateId ?? string.Empty, out NarrativeStateRecord destination);
            return new NarrativeRouteRequest
            {
                DestinationId = destinationStateId,
                RouteRole = destination?.RouteRole ?? NarrativeRouteRole.None,
                ReviewerContinueStateId = destination?.ContinueStateId ?? string.Empty
            };
        }

        public static NarrativeLocationPresentationModel CreateLocation(
            NarrativeStateRecord state,
            Game.UI.Contracts.IGameTextResolver textResolver)
        {
            if (state == null ||
                (string.IsNullOrEmpty(state.LocationTitleKey) && string.IsNullOrEmpty(state.LocationTitleFallback)))
                return default;
            string title = string.IsNullOrEmpty(state.LocationTitleFallback) ? "SAHRIN" : state.LocationTitleFallback;
            string subtitle = string.IsNullOrEmpty(state.LocationSubtitleFallback) ? "OLD MARKET / 10:00 LOCAL" : state.LocationSubtitleFallback;
            string titleKey = string.IsNullOrEmpty(state.LocationTitleKey)
                ? "narrative.first_launch.location.sahrin.name"
                : state.LocationTitleKey;
            string subtitleKey = string.IsNullOrEmpty(state.LocationSubtitleKey)
                ? "narrative.first_launch.location.old_market.context"
                : state.LocationSubtitleKey;
            return new NarrativeLocationPresentationModel
            {
                Visible = true,
                Title = textResolver.Get(titleKey, title),
                Subtitle = textResolver.Get(subtitleKey, subtitle)
            };
        }
        public static NarrativeCompletionPayload CreateRouteCompletion(NarrativeStateRecord state, bool skipped)
        {
            return new NarrativeCompletionPayload
            {
                PayloadId = state?.CompletionPayloadId ?? string.Empty,
                Watched = !skipped,
                Skipped = skipped,
                LastCompletedStateId = state?.StateId ?? string.Empty,
                EvidenceIds = state?.EvidenceIds ?? Array.Empty<string>(),
                MissionContextFlags = state?.MissionContextFlags ?? Array.Empty<string>()
            };
        }
    }
}
