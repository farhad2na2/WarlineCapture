using Game.Narrative.Contracts;

namespace Game.Narrative.Runtime
{
    public enum FirstLaunchNarrativeRouteAction
    {
        None = 0,
        RecordReviewerHandoff = 1,
        CompleteWatchedAndRequestMenu = 2,
        StartSkippedDebrief = 3,
        RequestSkipConfirmation = 4,
        CompleteSkippedAndRequestMenu = 5,
        ContinueReviewerAfterConfirmedSkip = 6,
    }

    public readonly struct FirstLaunchNarrativeRouteDecision
    {
        public FirstLaunchNarrativeRouteDecision(
            FirstLaunchNarrativeRouteAction action,
            string nextStateId = null)
        {
            Action = action;
            NextStateId = nextStateId;
        }

        public FirstLaunchNarrativeRouteAction Action { get; }
        public string NextStateId { get; }
    }

    public static class FirstLaunchNarrativeRouteUtilitySystemHelper
    {
        public static FirstLaunchNarrativeRouteDecision EvaluateHandoff(
            in NarrativeHandoffResult result,
            bool reviewerMode)
        {
            if (reviewerMode)
            {
                string nextStateId = result.RouteRole == NarrativeRouteRole.MissionHandoff
                    ? result.ReviewerContinueStateId
                    : null;
                return new FirstLaunchNarrativeRouteDecision(
                    FirstLaunchNarrativeRouteAction.RecordReviewerHandoff,
                    nextStateId);
            }

            if (result.RouteRole == NarrativeRouteRole.DebriefArrival)
                return default;

            return new FirstLaunchNarrativeRouteDecision(
                FirstLaunchNarrativeRouteAction.CompleteWatchedAndRequestMenu);
        }

        public static FirstLaunchNarrativeRouteDecision EvaluateSkipRequest(
            in NarrativeRouteRequest request,
            bool reviewerMode,
            bool hasCommittedCommanderIdentity,
            bool confirmationPending)
        {
            if (request.RouteRole == NarrativeRouteRole.DebriefArrival)
            {
                return new FirstLaunchNarrativeRouteDecision(
                    FirstLaunchNarrativeRouteAction.StartSkippedDebrief,
                    request.DestinationId);
            }

            if (confirmationPending || request.RouteRole != NarrativeRouteRole.MissionHandoff)
                return default;

            if (!reviewerMode && hasCommittedCommanderIdentity)
            {
                return new FirstLaunchNarrativeRouteDecision(
                    FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMenu);
            }

            return new FirstLaunchNarrativeRouteDecision(
                FirstLaunchNarrativeRouteAction.RequestSkipConfirmation,
                request.ReviewerContinueStateId);
        }

        public static FirstLaunchNarrativeRouteDecision EvaluateConfirmedSkip(
            bool reviewerMode,
            string reviewerContinueStateId)
        {
            return reviewerMode
                ? new FirstLaunchNarrativeRouteDecision(
                    FirstLaunchNarrativeRouteAction.ContinueReviewerAfterConfirmedSkip,
                    reviewerContinueStateId)
                : new FirstLaunchNarrativeRouteDecision(
                    FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMenu);
        }

    }
}
