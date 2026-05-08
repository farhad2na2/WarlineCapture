using System;
using UnityEngine;

[Serializable]
public sealed class StarGoalConfig
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private ObjectiveType type;
    [SerializeField] private int targetAmount = 1;

    public string Id => id;
    public string DisplayName => displayName;
    public ObjectiveType Type => type;
    public int TargetAmount => Mathf.Max(0, targetAmount);

    public StarGoalConfig(string id, string displayName, ObjectiveType type, int targetAmount)
    {
        this.id = id;
        this.displayName = displayName;
        this.type = type;
        this.targetAmount = targetAmount;
    }
}
