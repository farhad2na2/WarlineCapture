using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal static class FirstLaunchNarrativeModelFactory
    {
        public static NarrativeLocationPresentationModel CreateLocation(
            NarrativeStateRecord state,
            IGameTextResolver textResolver)
        {
            bool isOpeningLocation = string.Equals(state.StateId, "FL-P01", StringComparison.Ordinal);
            if (!isOpeningLocation && string.IsNullOrEmpty(state.LocationTitleFallback))
                return default;

            string titleFallback = string.IsNullOrEmpty(state.LocationTitleFallback) ? "SAHRIN" : state.LocationTitleFallback;
            string subtitleFallback = string.IsNullOrEmpty(state.LocationSubtitleFallback) ? "OLD MARKET / 06:42 LOCAL" : state.LocationSubtitleFallback;
            string titleKey = string.IsNullOrEmpty(state.LocationTitleKey)
                ? "narrative.first_launch.location.sahrin.name"
                : state.LocationTitleKey;
            string subtitleKey = string.IsNullOrEmpty(state.LocationSubtitleKey)
                ? "narrative.first_launch.location.old_market.context"
                : state.LocationSubtitleKey;
            return new NarrativeLocationPresentationModel
            {
                Visible = true,
                Title = textResolver.Get(titleKey, titleFallback),
                Subtitle = textResolver.Get(subtitleKey, subtitleFallback)
            };
        }

        public static NarrativeCompletionPayload CreateRouteCompletion(NarrativeStateRecord state, bool skipped)
        {
            if (state != null && state.StateId == "first_launch.command_base_reveal")
                return CreateDebriefCompletion(skipped);
            return new NarrativeCompletionPayload
            {
                PayloadId = "first_launch.m01_handoff_completion",
                Watched = !skipped,
                Skipped = skipped,
                LastCompletedStateId = state?.StateId ?? string.Empty,
                EvidenceIds = Array.Empty<string>(),
                MissionContextFlags = Array.Empty<string>()
            };
        }

        public static NarrativeCompletionPayload CreateDebriefCompletion(bool skipped)
        {
            return new NarrativeCompletionPayload
            {
                PayloadId = "first_launch.m01_debrief_completion",
                Watched = !skipped,
                Skipped = skipped,
                LastCompletedStateId = "first_launch.command_base_reveal",
                EvidenceIds = new[] { "evidence.aria.revoked_credential_fragment" },
                MissionContextFlags = new[] { "story.m01.corridor_secured", "story.aria.revoked_credential_clue_found" }
            };
        }
    }
}
