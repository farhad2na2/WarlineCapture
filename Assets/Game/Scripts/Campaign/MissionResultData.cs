using System;
using UnityEngine;

[Serializable]
public sealed class MissionResultData
{
    [SerializeField] private string missionId;
    [SerializeField] private string missionName;
    [SerializeField] private bool victory;
    [SerializeField] private int starsEarned;
    [SerializeField] private int enemiesDefeated;
    [SerializeField] private int unitsLost;
    [SerializeField] private int buildingsBuilt;
    [SerializeField] private int resourcesEarned;
    [SerializeField] private ObjectiveRuntimeState[] objectives = Array.Empty<ObjectiveRuntimeState>();
    [SerializeField] private RewardGrantResult[] rewards = Array.Empty<RewardGrantResult>();

    public string MissionId => missionId;
    public string MissionName => missionName;
    public bool Victory => victory;
    public int StarsEarned => starsEarned;
    public int EnemiesDefeated => enemiesDefeated;
    public int UnitsLost => unitsLost;
    public int BuildingsBuilt => buildingsBuilt;
    public int ResourcesEarned => resourcesEarned;
    public ObjectiveRuntimeState[] Objectives => objectives ?? Array.Empty<ObjectiveRuntimeState>();
    public RewardGrantResult[] Rewards => rewards ?? Array.Empty<RewardGrantResult>();

    public MissionResultData(
        string missionId,
        string missionName,
        bool victory,
        int starsEarned,
        int enemiesDefeated,
        int unitsLost,
        int buildingsBuilt,
        int resourcesEarned,
        ObjectiveRuntimeState[] objectives)
        : this(missionId, missionName, victory, starsEarned, enemiesDefeated, unitsLost, buildingsBuilt, resourcesEarned, objectives, Array.Empty<RewardGrantResult>())
    {
    }

    public MissionResultData(
        string missionId,
        string missionName,
        bool victory,
        int starsEarned,
        int enemiesDefeated,
        int unitsLost,
        int buildingsBuilt,
        int resourcesEarned,
        ObjectiveRuntimeState[] objectives,
        RewardGrantResult[] rewards)
    {
        this.missionId = missionId;
        this.missionName = missionName;
        this.victory = victory;
        this.starsEarned = Mathf.Clamp(starsEarned, 0, 3);
        this.enemiesDefeated = Mathf.Max(0, enemiesDefeated);
        this.unitsLost = Mathf.Max(0, unitsLost);
        this.buildingsBuilt = Mathf.Max(0, buildingsBuilt);
        this.resourcesEarned = Mathf.Max(0, resourcesEarned);
        this.objectives = objectives ?? Array.Empty<ObjectiveRuntimeState>();
        this.rewards = rewards ?? Array.Empty<RewardGrantResult>();
    }

    public MissionResultData WithRewards(RewardGrantResult[] rewards)
    {
        return new MissionResultData(
            MissionId,
            MissionName,
            Victory,
            StarsEarned,
            EnemiesDefeated,
            UnitsLost,
            BuildingsBuilt,
            ResourcesEarned,
            Objectives,
            rewards);
    }
}
