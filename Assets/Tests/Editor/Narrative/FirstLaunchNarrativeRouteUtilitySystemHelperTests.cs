using Game.Narrative.Contracts;
using Game.Narrative.Runtime;
using NUnit.Framework;

public sealed class FirstLaunchNarrativeRouteUtilitySystemHelperTests
{
    [Test]
    public void HandoffRule_SeparatesProductionReviewerAndDebriefRoutes()
    {
        FirstLaunchNarrativeRouteDecision production =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateHandoff(
                Handoff(NarrativeRouteRole.MissionHandoff),
                reviewerMode: false);
        Assert.AreEqual(
            FirstLaunchNarrativeRouteAction.CompleteWatchedAndRequestMatch,
            production.Action);

        FirstLaunchNarrativeRouteDecision reviewer =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateHandoff(
                Handoff(NarrativeRouteRole.MissionHandoff),
                reviewerMode: true);
        Assert.AreEqual(FirstLaunchNarrativeRouteAction.RecordReviewerHandoff, reviewer.Action);
        Assert.AreEqual("first_launch.gameplay_placeholder", reviewer.NextStateId);

        FirstLaunchNarrativeRouteDecision debrief =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateHandoff(
                Handoff(NarrativeRouteRole.DebriefArrival),
                reviewerMode: false);
        Assert.AreEqual(FirstLaunchNarrativeRouteAction.None, debrief.Action);
    }

    [Test]
    public void SkipRule_RequiresIdentityOrConfirmationAndKeepsDebriefRoute()
    {
        FirstLaunchNarrativeRouteDecision freshProfile =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateSkipRequest(
                Request(NarrativeRouteRole.MissionHandoff),
                reviewerMode: false,
                hasCommittedCommanderIdentity: false,
                confirmationPending: false);
        Assert.AreEqual(FirstLaunchNarrativeRouteAction.RequestSkipConfirmation, freshProfile.Action);

        FirstLaunchNarrativeRouteDecision committedProfile =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateSkipRequest(
                Request(NarrativeRouteRole.MissionHandoff),
                reviewerMode: false,
                hasCommittedCommanderIdentity: true,
                confirmationPending: false);
        Assert.AreEqual(
            FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMatch,
            committedProfile.Action);

        FirstLaunchNarrativeRouteDecision pending =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateSkipRequest(
                Request(NarrativeRouteRole.MissionHandoff),
                reviewerMode: false,
                hasCommittedCommanderIdentity: false,
                confirmationPending: true);
        Assert.AreEqual(FirstLaunchNarrativeRouteAction.None, pending.Action);

        FirstLaunchNarrativeRouteDecision debrief =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateSkipRequest(
                Request(NarrativeRouteRole.DebriefArrival),
                reviewerMode: true,
                hasCommittedCommanderIdentity: false,
                confirmationPending: false);
        Assert.AreEqual(FirstLaunchNarrativeRouteAction.StartSkippedDebrief, debrief.Action);
        Assert.AreEqual("first_launch.command_base_reveal", debrief.NextStateId);
    }

    [Test]
    public void ConfirmedSkipRule_SeparatesReviewerPreviewFromProductionPersistence()
    {
        FirstLaunchNarrativeRouteDecision reviewer =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateConfirmedSkip(
                reviewerMode: true,
                reviewerContinueStateId: "first_launch.gameplay_placeholder");
        Assert.AreEqual(
            FirstLaunchNarrativeRouteAction.ContinueReviewerAfterConfirmedSkip,
            reviewer.Action);
        Assert.AreEqual("first_launch.gameplay_placeholder", reviewer.NextStateId);

        FirstLaunchNarrativeRouteDecision production =
            FirstLaunchNarrativeRouteUtilitySystemHelper.EvaluateConfirmedSkip(
                reviewerMode: false,
                reviewerContinueStateId: "first_launch.gameplay_placeholder");
        Assert.AreEqual(
            FirstLaunchNarrativeRouteAction.CompleteSkippedAndRequestMatch,
            production.Action);
        Assert.IsNull(production.NextStateId);
    }

    private static NarrativeHandoffResult Handoff(NarrativeRouteRole role)
    {
        return new NarrativeHandoffResult
        {
            DestinationId = role == NarrativeRouteRole.DebriefArrival
                ? "first_launch.command_base_reveal"
                : "first_launch.m01_handoff",
            RouteRole = role,
            ReviewerContinueStateId = "first_launch.gameplay_placeholder"
        };
    }

    private static NarrativeRouteRequest Request(NarrativeRouteRole role)
    {
        return new NarrativeRouteRequest
        {
            DestinationId = role == NarrativeRouteRole.DebriefArrival
                ? "first_launch.command_base_reveal"
                : "first_launch.m01_handoff",
            RouteRole = role,
            ReviewerContinueStateId = "first_launch.gameplay_placeholder"
        };
    }
}
