using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using Game.UI.Runtime;
namespace Game.Composition
{
    internal static class FirstLaunchNarrativeModelUtilitySystemHelper
    {
        internal static List<FirstLaunchNarrativeSequenceStateDefinition> CreatePlaybackDefinitions(
            NarrativeSequenceConfig config, bool storyOnly)
        {
            var lookup = new Dictionary<string, NarrativeStateRecord>(StringComparer.Ordinal);
            foreach (var state in config.States) lookup.Add(state.StateId, state);
            var definitions = new List<FirstLaunchNarrativeSequenceStateDefinition>();
            bool Omit(NarrativeStateRecord state) => state.Kind == NarrativeStateKind.InteractiveGuidance ||
                (storyOnly && state.Kind == NarrativeStateKind.InteractiveIdentity);
            string Next(string id)
            {
                // Also supports older authored sequences until their assets are migrated.
                for (int i = 0; i < lookup.Count && lookup.TryGetValue(id ?? string.Empty, out var next) && Omit(next); i++)
                    id = next.ContinueStateId;
                return id;
            }
            foreach (var state in config.States)
            {
                if (Omit(state)) continue;
                bool endOfOpening = storyOnly && state.RouteRole == NarrativeRouteRole.MissionHandoff;
                var timings = new float[state.Lines.Count];
                for (int i = 0; i < timings.Length; i++) timings[i] = state.Lines[i].StartSeconds;
                definitions.Add(new FirstLaunchNarrativeSequenceStateDefinition(state.StateId, state.Kind,
                    endOfOpening ? string.Empty : Next(state.ContinueStateId), Next(state.SkipStateId),
                    state.DurationSeconds, timings));
                if (endOfOpening) break;
            }
            return definitions;
        }

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
