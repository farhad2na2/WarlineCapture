using System;

public static class WarlineCaptureMissionSession
{
    public static MissionConfig ActiveMission { get; private set; }
    public static WarlineCaptureRoute ReturnRoute { get; private set; } = WarlineCaptureRoute.SagaMap;
    public static bool HasActiveMission => ActiveMission != null;
    public static string ActiveMissionId => ActiveMission?.MissionId ?? string.Empty;
    public static string ActiveScenarioSetupId => ActiveMission?.ScenarioSetupId ?? string.Empty;
    public static string ActiveLevelId => ActiveMission?.LevelId ?? string.Empty;
    public static string ActiveIsoMapId => ActiveMission?.IsoMapId ?? string.Empty;
    public static string ActiveMapPreviewArtId => ActiveMission?.MapPreviewArtId ?? string.Empty;
    public static string ActiveMinimapArtId => ActiveMission?.MinimapArtId ?? string.Empty;

    public static void BeginMission(string missionId, WarlineCaptureRoute returnRoute)
    {
        ActiveMission = ChapterOneMissionCatalog.GetMission(missionId);
        ReturnRoute = returnRoute;
    }

#if UNITY_EDITOR
    public static void BeginMissionForTests(MissionConfig mission, WarlineCaptureRoute returnRoute)
    {
        ActiveMission = mission ?? throw new ArgumentNullException(nameof(mission));
        ReturnRoute = returnRoute;
    }
#endif

    public static void Clear()
    {
        ActiveMission = null;
        ReturnRoute = WarlineCaptureRoute.SagaMap;
    }

    public static MissionResultData BuildCurrentResult(GameRuntimeStats.Snapshot snapshot)
    {
        if (ActiveMission == null)
            throw new InvalidOperationException("No active mission session is available.");

        return MissionResultBuilder.Build(ActiveMission, snapshot);
    }

    public static MissionResultData CompleteCurrentMission(GameRuntimeStats.Snapshot snapshot)
    {
        MissionResultData result = BuildCurrentResult(snapshot);
        SagaProgressStore.ApplyMissionResult(result);
        return result;
    }
}
