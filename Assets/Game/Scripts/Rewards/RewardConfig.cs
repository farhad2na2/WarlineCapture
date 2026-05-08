using System;
using UnityEngine;

[Serializable]
public sealed class RewardConfig
{
    [SerializeField] private string rewardId;
    [SerializeField] private string previewTitle;
    [SerializeField] private bool firstClearOnly;
    [SerializeField] private int starThreshold;
    [SerializeField] private RewardItemConfig[] items = Array.Empty<RewardItemConfig>();
    [SerializeField] private RewardConfig duplicateFallback;

    public string RewardId => rewardId;
    public string PreviewTitle => previewTitle;
    public bool FirstClearOnly => firstClearOnly;
    public int StarThreshold => Mathf.Clamp(starThreshold, 0, 3);
    public RewardItemConfig[] Items => items ?? Array.Empty<RewardItemConfig>();
    public RewardConfig DuplicateFallback => duplicateFallback;

    public RewardConfig(
        string rewardId,
        string previewTitle,
        RewardItemConfig[] items,
        bool firstClearOnly = false,
        int starThreshold = 0,
        RewardConfig duplicateFallback = null)
    {
        this.rewardId = rewardId;
        this.previewTitle = previewTitle;
        this.items = items ?? Array.Empty<RewardItemConfig>();
        this.firstClearOnly = firstClearOnly;
        this.starThreshold = Mathf.Clamp(starThreshold, 0, 3);
        this.duplicateFallback = duplicateFallback;
    }
}
