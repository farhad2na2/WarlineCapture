namespace Game.Runtime
{
    public sealed partial class CampaignMissionProgressStore
    {
        private const string M03MissionId = "saga.ch01.m03.radar_warning";
        private const string TowerRewardId = "reward.ch01.m03.guard_tower_unlock";
        private const string PingRewardId = "reward.ch01.m03.radar_ping_unlock";
        private const string TowerBuildingId = "Building_GuardTower";
        private const string RadarPingAbilityId = "ability.radar_ping";

        private static bool IsRadarWarningUnlock(string missionId, bool firstClear, CampaignMissionRewardGrant reward) =>
            missionId == M03MissionId && firstClear && reward.Amount == 1 &&
            (reward.RewardConfigId == TowerRewardId || reward.RewardConfigId == PingRewardId);

        private static bool TryApplyRadarWarningUnlock(PlayerProfileSaveData profile, CampaignMissionRewardGrant reward)
        {
            if (reward.RewardConfigId == TowerRewardId)
            {
                if (Contains(profile.ownedBuildingUnlocks,TowerBuildingId))
                    profile.blueprintParts = AddBlueprintParts(profile.blueprintParts,"upgrade.building.base_defense",reward.Amount);
                else profile.ownedBuildingUnlocks = Append(profile.ownedBuildingUnlocks,TowerBuildingId);
                return true;
            }
            if (reward.RewardConfigId != PingRewardId) return false;
            if (Contains(profile.ownedSupportAbilityUnlocks,RadarPingAbilityId))
                profile.blueprintParts = AddBlueprintParts(profile.blueprintParts,RadarPingAbilityId,reward.Amount);
            else profile.ownedSupportAbilityUnlocks = Append(profile.ownedSupportAbilityUnlocks,RadarPingAbilityId);
            return true;
        }
    }
}
