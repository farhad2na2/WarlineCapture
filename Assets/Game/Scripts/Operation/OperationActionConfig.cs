using System;

[Serializable]
public sealed class OperationActionConfig
{
    public OperationActionType actionType;
    public int stabilityDelta;
    public int threatDelta;
    public int intelDelta;
    public int supplyCost;
    public int supplyReward;
    public bool startsRaidMission;
    public OperationEventSeverity eventSeverity;
    public int trustDelta;
    public int securityDelta;
    public int infrastructureDelta;
    public int enemyInfluenceDelta;
    public int heatDelta;
    public int civilianRiskDelta;
    public string eventTitle;
    public string eventBody;

    public int SupplyDelta => supplyReward - supplyCost;

    public OperationActionConfig()
    {
    }

    public OperationActionConfig(
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        int supplyCost,
        int supplyReward,
        bool startsRaidMission,
        string eventTitle,
        string eventBody,
        OperationEventSeverity eventSeverity = OperationEventSeverity.Info,
        int trustDelta = 0,
        int securityDelta = 0,
        int infrastructureDelta = 0,
        int enemyInfluenceDelta = 0,
        int heatDelta = 0,
        int civilianRiskDelta = 0)
    {
        this.actionType = actionType;
        this.stabilityDelta = stabilityDelta;
        this.threatDelta = threatDelta;
        this.intelDelta = intelDelta;
        this.supplyCost = Math.Max(0, supplyCost);
        this.supplyReward = Math.Max(0, supplyReward);
        this.startsRaidMission = startsRaidMission;
        this.eventSeverity = eventSeverity;
        this.trustDelta = trustDelta;
        this.securityDelta = securityDelta;
        this.infrastructureDelta = infrastructureDelta;
        this.enemyInfluenceDelta = enemyInfluenceDelta;
        this.heatDelta = heatDelta;
        this.civilianRiskDelta = civilianRiskDelta;
        this.eventTitle = eventTitle;
        this.eventBody = eventBody;
    }

    public static OperationActionConfig[] CreateDefaults()
    {
        return new[]
        {
            new OperationActionConfig(OperationActionType.Patrol, 3, -5, 2, 0, 0, false, "Patrol Sweep", "Ground patrol reduced visible enemy pressure.", trustDelta: 1, securityDelta: 5, enemyInfluenceDelta: -5, heatDelta: 2, civilianRiskDelta: -2),
            new OperationActionConfig(OperationActionType.Scan, 0, 0, 12, 1, 0, false, "Drone Scan", "New intel evidence is ready for review.", enemyInfluenceDelta: -1, heatDelta: 1),
            new OperationActionConfig(OperationActionType.Aid, 7, -1, 0, 1, 0, false, "Aid Convoy", "Civilian support convoy improved district stability.", trustDelta: 8, infrastructureDelta: 3, civilianRiskDelta: -5),
            new OperationActionConfig(OperationActionType.Raid, -4, -14, -6, 2, 0, true, "Raid Authorized", "Raid team committed to a tactical mission route.", OperationEventSeverity.Warning, trustDelta: -4, securityDelta: 8, infrastructureDelta: -3, enemyInfluenceDelta: -12, heatDelta: 8, civilianRiskDelta: 6),
            new OperationActionConfig(OperationActionType.Repair, 6, -2, 0, 1, 0, false, "Infrastructure Repair", "Engineering crews restored critical district services.", trustDelta: 3, securityDelta: 2, infrastructureDelta: 12, heatDelta: -2, civilianRiskDelta: -4),
            new OperationActionConfig(OperationActionType.Evacuate, -3, 1, 0, 1, 0, false, "Civilian Evacuation", "Civilian evacuation reduced immediate harm but strained local trust.", OperationEventSeverity.Warning, trustDelta: -5, securityDelta: 1, heatDelta: 3, civilianRiskDelta: -15),
            new OperationActionConfig(OperationActionType.BuildOutpost, 4, -7, 1, 2, 0, false, "Forward Outpost Built", "A forward outpost improved district security and response readiness.", trustDelta: 1, securityDelta: 14, infrastructureDelta: 4, enemyInfluenceDelta: -8, heatDelta: 5, civilianRiskDelta: -3)
        };
    }
}
