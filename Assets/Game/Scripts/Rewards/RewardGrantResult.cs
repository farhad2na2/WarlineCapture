using System;
using UnityEngine;

[Serializable]
public readonly struct RewardGrantResult
{
    [SerializeField] private readonly string rewardId;
    [SerializeField] private readonly RewardType type;
    [SerializeField] private readonly string targetItemId;
    [SerializeField] private readonly int amount;
    [SerializeField] private readonly bool granted;
    [SerializeField] private readonly string reason;

    public string RewardId => rewardId;
    public RewardType Type => type;
    public string TargetItemId => targetItemId;
    public int Amount => amount;
    public bool Granted => granted;
    public string Reason => reason;

    public RewardGrantResult(string rewardId, RewardType type, string targetItemId, int amount, bool granted, string reason)
    {
        this.rewardId = rewardId ?? string.Empty;
        this.type = type;
        this.targetItemId = targetItemId ?? string.Empty;
        this.amount = Mathf.Max(0, amount);
        this.granted = granted;
        this.reason = reason ?? string.Empty;
    }
}
