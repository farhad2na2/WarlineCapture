using System;
using Game.Narrative.Contracts;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeProfileCompositionSystemHelper
    {
        private string commanderIdentityStateId = string.Empty;
        private string guidanceChoiceStateId = string.Empty;
        private SaveService saveService;
        private PlayerProfileSaveData profile;
        public bool IsInitialized => profile != null;
        public NarrativeCommanderIdentityData CommanderIdentity => new()
        {
            Callsign = profile?.firstLaunchCommanderCallsign ?? "COMMANDER",
            DisplayName = profile?.firstLaunchCommanderDisplayName ?? "Commander"
        };
        public int CommanderPortraitIndex => Math.Max(0, profile?.firstLaunchCommanderPortraitIndex ?? 0);
        public NarrativeGuidanceMode Guidance => Enum.TryParse(
            profile?.firstLaunchGuidance,
            true,
            out NarrativeGuidanceMode guidance)
                ? guidance
                : NarrativeGuidanceMode.Full;
        public void Initialize(
            SaveService persistence,
            string commanderStateId,
            string guidanceStateId)
        {
            saveService = persistence ?? SaveService.CreateDefault();
            profile = saveService.LoadProfile();
            commanderIdentityStateId = commanderStateId ?? string.Empty;
            guidanceChoiceStateId = guidanceStateId ?? string.Empty;
        }
        public bool ShouldEnterMenu(bool bypassForDiagnostics, bool reviewerMode)
        {
            return !reviewerMode &&
                   (bypassForDiagnostics || profile.firstLaunchStatus == FirstLaunchProfileState.Completed);
        }

        public bool ShouldResumeHandoff(bool reviewerMode)
        {
            return !reviewerMode && profile.firstLaunchStatus == FirstLaunchProfileState.HandoffPending;
        }

        public void MarkInProgress(bool reviewerMode)
        {
            if (reviewerMode)
                return;

            profile.firstLaunchStatus = FirstLaunchProfileState.InProgress;
            Save();
        }

        public void CommitCommanderIdentity(
            in NarrativeCommanderIdentityData identity,
            int portraitIndex,
            bool persist)
        {
            profile.firstLaunchCommanderCallsign = identity.Callsign;
            profile.firstLaunchCommanderDisplayName = identity.DisplayName;
            profile.firstLaunchCommanderPortraitIndex = Math.Max(0, portraitIndex);
            profile.firstLaunchLastCompletedStateId = commanderIdentityStateId;
            if (persist)
                Save();
        }

        public void CommitGuidance(NarrativeGuidanceMode guidance, bool persist)
        {
            profile.firstLaunchGuidance = guidance.ToString();
            profile.firstLaunchLastCompletedStateId = guidanceChoiceStateId;
            if (persist)
                Save();
        }

        public bool HasCommittedCommanderIdentity()
        {
            return profile.firstLaunchLastCompletedStateId == commanderIdentityStateId ||
                   profile.firstLaunchLastCompletedStateId == guidanceChoiceStateId;
        }

        public void MarkSkipped(string lastCompletedStateId)
        {
            EnsureValidDefaults();
            profile.firstLaunchStatus = FirstLaunchProfileState.HandoffPending;
            profile.firstLaunchSkipped = true;
            profile.firstLaunchWatched = false;
            profile.firstLaunchLastCompletedStateId = lastCompletedStateId ?? string.Empty;
            Save();
        }

        public void MarkWatchedHandoff(in NarrativeHandoffResult result)
        {
            EnsureValidDefaults();
            profile.firstLaunchStatus = FirstLaunchProfileState.HandoffPending;
            profile.firstLaunchWatched = true;
            profile.firstLaunchSkipped = false;
            profile.firstLaunchLastCompletedStateId = result.Completion.LastCompletedStateId ?? string.Empty;
            Save();
        }

        public void MarkHandoffComplete()
        {
            if (profile == null || profile.firstLaunchStatus != FirstLaunchProfileState.HandoffPending)
                return;

            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            Save();
        }

        public void Reset()
        {
            profile = null;
            saveService = null;
            commanderIdentityStateId = string.Empty;
            guidanceChoiceStateId = string.Empty;
        }

        private void EnsureValidDefaults()
        {
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderCallsign))
                profile.firstLaunchCommanderCallsign = "COMMANDER";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderDisplayName))
                profile.firstLaunchCommanderDisplayName = "Commander";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchGuidance))
                profile.firstLaunchGuidance = NarrativeGuidanceMode.Full.ToString();
        }

        private void Save()
        {
            saveService.SaveProfile(profile);
        }
    }
}
