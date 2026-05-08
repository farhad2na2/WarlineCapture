using System;
using UnityEngine;

[Serializable]
public sealed class MissionConfig
{
    [SerializeField] private string missionId;
    [SerializeField] private string displayName;
    [SerializeField] private string scenarioSetupId;
    [SerializeField] private string levelId;
    [SerializeField] private string isoMapId;
    [SerializeField] private string mapPreviewArtId;
    [SerializeField] private string minimapArtId;
    [SerializeField] private ObjectiveConfig[] objectives = Array.Empty<ObjectiveConfig>();
    [SerializeField] private StarGoalConfig[] starGoals = Array.Empty<StarGoalConfig>();
    [SerializeField] private RewardConfig[] rewards = Array.Empty<RewardConfig>();

    public string MissionId => missionId ?? string.Empty;
    public string DisplayName => displayName ?? string.Empty;
    public string ScenarioSetupId => scenarioSetupId ?? string.Empty;
    public string LevelId => levelId ?? string.Empty;
    public string IsoMapId => isoMapId ?? string.Empty;
    public string MapPreviewArtId => mapPreviewArtId ?? string.Empty;
    public string MinimapArtId => minimapArtId ?? string.Empty;
    public ObjectiveConfig[] Objectives => objectives ?? Array.Empty<ObjectiveConfig>();
    public StarGoalConfig[] StarGoals => starGoals ?? Array.Empty<StarGoalConfig>();
    public RewardConfig[] Rewards => rewards ?? Array.Empty<RewardConfig>();

    public MissionConfig(string missionId, string displayName, ObjectiveConfig[] objectives, StarGoalConfig[] starGoals)
        : this(missionId, displayName, objectives, starGoals, Array.Empty<RewardConfig>())
    {
    }

    public MissionConfig(string missionId, string displayName, ObjectiveConfig[] objectives, StarGoalConfig[] starGoals, RewardConfig[] rewards)
        : this(missionId, displayName, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, objectives, starGoals, rewards)
    {
    }

    public MissionConfig(
        string missionId,
        string displayName,
        string scenarioSetupId,
        string levelId,
        string isoMapId,
        string mapPreviewArtId,
        string minimapArtId,
        ObjectiveConfig[] objectives,
        StarGoalConfig[] starGoals,
        RewardConfig[] rewards)
    {
        this.missionId = missionId;
        this.displayName = displayName;
        this.scenarioSetupId = scenarioSetupId;
        this.levelId = levelId;
        this.isoMapId = isoMapId;
        this.mapPreviewArtId = mapPreviewArtId;
        this.minimapArtId = minimapArtId;
        this.objectives = objectives ?? Array.Empty<ObjectiveConfig>();
        this.starGoals = starGoals ?? Array.Empty<StarGoalConfig>();
        this.rewards = rewards ?? Array.Empty<RewardConfig>();
    }
}
