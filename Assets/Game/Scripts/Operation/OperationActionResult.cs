using System;

[Serializable]
public readonly struct OperationActionResult
{
    public string DistrictId { get; }
    public OperationActionType ActionType { get; }
    public int StabilityDelta { get; }
    public int ThreatDelta { get; }
    public int IntelDelta { get; }
    public int SupplyDelta { get; }
    public int TrustDelta { get; }
    public int SecurityDelta { get; }
    public int InfrastructureDelta { get; }
    public int EnemyInfluenceDelta { get; }
    public int HeatDelta { get; }
    public int CivilianRiskDelta { get; }
    public bool StartsRaidMission { get; }
    public bool Applied { get; }
    public string FailureReason { get; }
    public OperationEventData Event { get; }

    public OperationActionResult(
        string districtId,
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        bool startsRaidMission)
        : this(districtId, actionType, stabilityDelta, threatDelta, intelDelta, 0, startsRaidMission, null)
    {
    }

    public OperationActionResult(
        string districtId,
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        int supplyDelta,
        bool startsRaidMission,
        OperationEventData operationEvent)
        : this(districtId, actionType, stabilityDelta, threatDelta, intelDelta, supplyDelta, 0, 0, 0, 0, 0, 0, startsRaidMission, true, string.Empty, operationEvent)
    {
    }

    public OperationActionResult(
        string districtId,
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        int supplyDelta,
        int trustDelta,
        int securityDelta,
        int infrastructureDelta,
        int enemyInfluenceDelta,
        int heatDelta,
        int civilianRiskDelta,
        bool startsRaidMission,
        OperationEventData operationEvent)
        : this(districtId, actionType, stabilityDelta, threatDelta, intelDelta, supplyDelta, trustDelta, securityDelta, infrastructureDelta, enemyInfluenceDelta, heatDelta, civilianRiskDelta, startsRaidMission, true, string.Empty, operationEvent)
    {
    }

    public OperationActionResult(
        string districtId,
        OperationActionType actionType,
        int stabilityDelta,
        int threatDelta,
        int intelDelta,
        int supplyDelta,
        int trustDelta,
        int securityDelta,
        int infrastructureDelta,
        int enemyInfluenceDelta,
        int heatDelta,
        int civilianRiskDelta,
        bool startsRaidMission,
        bool applied,
        string failureReason,
        OperationEventData operationEvent)
    {
        DistrictId = districtId;
        ActionType = actionType;
        StabilityDelta = stabilityDelta;
        ThreatDelta = threatDelta;
        IntelDelta = intelDelta;
        SupplyDelta = supplyDelta;
        TrustDelta = trustDelta;
        SecurityDelta = securityDelta;
        InfrastructureDelta = infrastructureDelta;
        EnemyInfluenceDelta = enemyInfluenceDelta;
        HeatDelta = heatDelta;
        CivilianRiskDelta = civilianRiskDelta;
        StartsRaidMission = startsRaidMission;
        Applied = applied;
        FailureReason = failureReason ?? string.Empty;
        Event = operationEvent;
    }
}
