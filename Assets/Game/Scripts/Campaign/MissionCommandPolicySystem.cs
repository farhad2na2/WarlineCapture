public sealed class MissionCommandPolicySystem
{
    public string FirstContactBuildDisabledMessage => "Building unlocks in the next mission.";

    private readonly ActiveMissionSession _activeMissionSession;

    public MissionCommandPolicySystem(ActiveMissionSession activeMissionSession = null)
    {
        _activeMissionSession = activeMissionSession ?? new ActiveMissionSession();
    }

    public bool IsBuildAllowedForActiveOperation()
    {
        return !_activeMissionSession.HasActiveMission ||
            _activeMissionSession.ActiveMissionId != ChapterOneMissionCatalog.FirstContactMissionId;
    }

    public bool TryRejectBuildForActiveOperation()
    {
        if (IsBuildAllowedForActiveOperation())
            return false;

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(
            TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.MissionDoesNotAllowBuild,
                FirstContactBuildDisabledMessage));
        return true;
    }
}
