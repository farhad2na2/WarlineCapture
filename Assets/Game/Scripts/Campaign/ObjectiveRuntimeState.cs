using System;
using UnityEngine;

[Serializable]
public readonly struct ObjectiveRuntimeState
{
    [SerializeField] private readonly string id;
    [SerializeField] private readonly string displayName;
    [SerializeField] private readonly ObjectiveType type;
    [SerializeField] private readonly int currentAmount;
    [SerializeField] private readonly int targetAmount;
    [SerializeField] private readonly bool required;
    [SerializeField] private readonly bool complete;

    public string Id => id;
    public string DisplayName => displayName;
    public ObjectiveType Type => type;
    public int CurrentAmount => currentAmount;
    public int TargetAmount => targetAmount;
    public bool Required => required;
    public bool Complete => complete;

    public ObjectiveRuntimeState(string id, string displayName, ObjectiveType type, int currentAmount, int targetAmount, bool required, bool complete)
    {
        this.id = id;
        this.displayName = displayName;
        this.type = type;
        this.currentAmount = Mathf.Max(0, currentAmount);
        this.targetAmount = Mathf.Max(0, targetAmount);
        this.required = required;
        this.complete = complete;
    }
}
