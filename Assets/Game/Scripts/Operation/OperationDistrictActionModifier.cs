using System;

[Serializable]
public sealed class OperationDistrictActionModifier
{
    public string districtId;
    public OperationActionType actionType;
    public int stabilityDelta;
    public int threatDelta;
    public int intelDelta;
    public int supplyReward;
    public OperationEventSeverity eventSeverity;
    public int trustDelta;
    public int securityDelta;
    public int infrastructureDelta;
    public int enemyInfluenceDelta;
    public int heatDelta;
    public int civilianRiskDelta;
    public string eventTitle;
    public string eventBody;

    public OperationDistrictActionModifier()
    {
    }

    public OperationDistrictActionModifier(
        string districtId,
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        int supplyReward,
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
        this.districtId = districtId;
        this.actionType = actionType;
        this.stabilityDelta = stabilityDelta;
        this.threatDelta = threatDelta;
        this.intelDelta = intelDelta;
        this.supplyReward = Math.Max(0, supplyReward);
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
}
