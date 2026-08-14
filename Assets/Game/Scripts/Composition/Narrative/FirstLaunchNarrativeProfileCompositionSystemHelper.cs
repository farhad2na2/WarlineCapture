using System;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class FirstLaunchNarrativeProfileCompositionSystemHelper
    {
        private string commanderIdentityStateId=string.Empty;
        private string guidanceChoiceStateId=string.Empty;
        private SaveService store;
        private PlayerProfileSaveData profile;
        public bool IsInitialized=>profile!=null;
        public NarrativeCommanderIdentityData CommanderIdentity=>new()
        { Callsign = profile?.firstLaunchCommanderCallsign ?? "COMMANDER",
          DisplayName = profile?.firstLaunchCommanderDisplayName ?? "Commander" };
        public int CommanderPortraitIndex=>Math.Max(0,profile?.firstLaunchCommanderPortraitIndex??0);
        public FirstLaunchNarrativeLanguage Language=>Enum.TryParse(profile?.firstLaunchLanguage,true,
            out FirstLaunchNarrativeLanguage language) ? language : FirstLaunchNarrativeLanguage.Unselected;
        public bool RequiresLanguageSelection=>Language==FirstLaunchNarrativeLanguage.Unselected;
        public NarrativeGuidanceMode Guidance=>Enum.TryParse(profile?.firstLaunchGuidance,true,
            out NarrativeGuidanceMode guidance) ? guidance : NarrativeGuidanceMode.Full;
        public void Initialize(SaveService persistence, string commanderStateId, string guidanceStateId)
        {
            store = persistence ?? SaveService.CreateDefault();
            profile = store.LoadProfile();
            commanderIdentityStateId = commanderStateId ?? string.Empty;
            guidanceChoiceStateId = guidanceStateId ?? string.Empty;
        }
        public bool ShouldEnterMenu(bool bypassForDiagnostics, bool reviewerMode) => !reviewerMode &&
            (bypassForDiagnostics || profile.firstLaunchStatus == FirstLaunchProfileState.Completed);

        public bool ShouldResumeHandoff(bool reviewerMode) => !reviewerMode &&
            profile.firstLaunchStatus == FirstLaunchProfileState.HandoffPending;

        public void MarkInProgress(bool reviewerMode)
        {
            if (reviewerMode)
                return;

            profile.firstLaunchStatus = FirstLaunchProfileState.InProgress;
            Save();
        }

        public void CommitLanguage(FirstLaunchNarrativeLanguage language, bool persist)
        {
            if (language != FirstLaunchNarrativeLanguage.English &&
                language != FirstLaunchNarrativeLanguage.Persian)
            {
                return;
            }

            if (language == FirstLaunchNarrativeLanguage.Persian)
            {
                if (string.Equals(profile.firstLaunchCommanderCallsign, "COMMANDER", StringComparison.OrdinalIgnoreCase))
                    profile.firstLaunchCommanderCallsign = "فرمانده";
                if (string.Equals(profile.firstLaunchCommanderDisplayName, "Commander", StringComparison.OrdinalIgnoreCase))
                    profile.firstLaunchCommanderDisplayName = "فرمانده";
            }

            profile.firstLaunchLanguage = language.ToString();
            if (persist)
                Save();
        }

        public void CommitCommanderIdentity(in NarrativeCommanderIdentityData identity, int portraitIndex, bool persist)
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

        public bool HasCommittedCommanderIdentity() =>
            profile.firstLaunchLastCompletedStateId == commanderIdentityStateId ||
            profile.firstLaunchLastCompletedStateId == guidanceChoiceStateId;

        public MissionLaunchPayload PrepareMissionHandoff(ulong transitionToken)
        {
            MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(profile, transitionToken, Guidance);
            Save(); return payload;
        }

        public bool MarkMissionAccepted(in MissionLaunchPayload payload)
        {
            if (profile == null || profile.firstLaunchStatus != FirstLaunchProfileState.HandoffPending ||
                !FirstLaunchMissionHandoffOperation.Matches(profile, payload)) return false;
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed; Save(); return true;
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

        public void Reset()
        { profile = null; store = null; commanderIdentityStateId = guidanceChoiceStateId = string.Empty; }

        private void EnsureValidDefaults()
        {
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderCallsign))
                profile.firstLaunchCommanderCallsign = "COMMANDER";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchCommanderDisplayName))
                profile.firstLaunchCommanderDisplayName = "Commander";
            if (string.IsNullOrWhiteSpace(profile.firstLaunchGuidance))
                profile.firstLaunchGuidance = NarrativeGuidanceMode.Full.ToString();
        }

        private void Save()=>store.SaveProfile(profile);
    }
}
