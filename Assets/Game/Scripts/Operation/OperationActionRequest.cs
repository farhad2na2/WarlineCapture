using System;

[Serializable]
public readonly struct OperationActionRequest
{
    public string DistrictId { get; }
    public OperationActionType ActionType { get; }

    public OperationActionRequest(string districtId, OperationActionType actionType)
    {
        DistrictId = districtId;
        ActionType = actionType;
    }
}
