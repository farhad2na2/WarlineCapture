using System;

namespace Game.Narrative.Contracts
{
    public enum NarrativeRouteRole
    {
        None = 0,
        MissionHandoff = 1,
        ReviewerGameplay = 2,
        DebriefOpening = 3,
        DebriefArrival = 4,
        CommanderIdentity = 5,
        GuidanceChoice = 6
    }

    public enum NarrativeGuidanceMode
    {
        Full = 0,
        Contextual = 1,
        Minimal = 2
    }

    [Serializable]
    public struct NarrativeCommanderIdentityData
    {
        public string Callsign;
        public string DisplayName;
    }

    [Serializable]
    public struct NarrativeCompletionPayload
    {
        public string PayloadId;
        public bool Watched;
        public bool Skipped;
        public string LastCompletedStateId;
        public string[] EvidenceIds;
        public string[] MissionContextFlags;
    }

    [Serializable]
    public struct NarrativeHandoffResult
    {
        public string DestinationId;
        public NarrativeRouteRole RouteRole;
        public string ReviewerContinueStateId;
        public NarrativeCommanderIdentityData Commander;
        public NarrativeGuidanceMode Guidance;
        public NarrativeCompletionPayload Completion;
        public ulong TransitionToken;
    }

    [Serializable]
    public struct NarrativeRouteRequest
    {
        public string DestinationId;
        public NarrativeRouteRole RouteRole;
        public string ReviewerContinueStateId;
    }
}
