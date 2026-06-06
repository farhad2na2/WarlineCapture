public static class WarlineCaptureMissionRules
{
    public const string M01BuildDisabledMessage = "Building unlocks in the next mission.";

    public static bool IsBuildAllowedForActiveMission()
    {
        return !WarlineCaptureMissionSession.HasActiveMission ||
            WarlineCaptureMissionSession.ActiveMissionId != ChapterOneMissionCatalog.FirstContactMissionId;
    }

    public static bool TryRejectBuildForActiveMission()
    {
        if (IsBuildAllowedForActiveMission())
            return false;

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.MissionDoesNotAllowBuild,
                M01BuildDisabledMessage));
        return true;
    }
}
