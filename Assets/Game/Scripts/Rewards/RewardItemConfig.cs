using System;
using UnityEngine;

[Serializable]
public sealed class RewardItemConfig
{
    [SerializeField] private RewardType type;
    [SerializeField] private string targetItemId;
    [SerializeField] private int amount = 1;

    public RewardType Type => type;
    public string TargetItemId => targetItemId;
    public int Amount => Mathf.Max(0, amount);

    public RewardItemConfig(RewardType type, int amount, string targetItemId = "")
    {
        this.type = type;
        this.amount = amount;
        this.targetItemId = targetItemId ?? string.Empty;
    }
}
