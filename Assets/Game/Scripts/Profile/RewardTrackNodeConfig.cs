using System;

public readonly struct RewardTrackNodeConfig
{
    public readonly string NodeId;
    public readonly string Title;
    public readonly string Description;
    public readonly int RequiredCommanderLevel;
    public readonly RewardItemConfig[] Rewards;

    public RewardTrackNodeConfig(string nodeId, string title, string description, int requiredCommanderLevel, RewardItemConfig[] rewards)
    {
        NodeId = nodeId ?? string.Empty;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        RequiredCommanderLevel = Math.Max(1, requiredCommanderLevel);
        Rewards = rewards ?? Array.Empty<RewardItemConfig>();
    }
}
