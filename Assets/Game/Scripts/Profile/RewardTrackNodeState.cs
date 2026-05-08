public readonly struct RewardTrackNodeState
{
    public readonly RewardTrackNodeConfig Config;
    public readonly bool IsUnlocked;
    public readonly bool IsClaimed;

    public RewardTrackNodeState(RewardTrackNodeConfig config, bool isUnlocked, bool isClaimed)
    {
        Config = config;
        IsUnlocked = isUnlocked;
        IsClaimed = isClaimed;
    }

    public bool CanClaim => IsUnlocked && !IsClaimed;
}
