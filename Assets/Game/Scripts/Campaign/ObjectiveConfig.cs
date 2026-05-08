using System;
using UnityEngine;

[Serializable]
public sealed class ObjectiveConfig
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private ObjectiveType type;
    [SerializeField] private int targetAmount = 1;
    [SerializeField] private bool required = true;
    [SerializeField] private bool starGoal;

    public string Id => id;
    public string DisplayName => displayName;
    public ObjectiveType Type => type;
    public int TargetAmount => Mathf.Max(0, targetAmount);
    public bool Required => required;
    public bool StarGoal => starGoal;

    public ObjectiveConfig(string id, string displayName, ObjectiveType type, int targetAmount, bool required = true, bool starGoal = false)
    {
        this.id = id;
        this.displayName = displayName;
        this.type = type;
        this.targetAmount = targetAmount;
        this.required = required;
        this.starGoal = starGoal;
    }
}
