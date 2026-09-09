namespace Game.Runtime
{
    public sealed partial class CampaignMissionProgressStore
    {
        private const string M04MissionId = "saga.ch01.m04.airlift";
        private const string LailaRewardId = "reward.ch01.m04.laila_unlock";
        private const string TransportRewardId = "reward.ch01.m04.transport_unlock";

        private static bool IsAirliftUnlock(string missionId, bool firstClear, CampaignMissionRewardGrant reward) =>
            missionId == M04MissionId && firstClear && reward.Amount == 1 &&
            (reward.RewardConfigId == LailaRewardId || reward.RewardConfigId == TransportRewardId);

        private static bool TryApplyAirliftUnlock(PlayerProfileSaveData profile, CampaignMissionRewardGrant reward)
        {
            if (reward.RewardConfigId == LailaRewardId)
            {
                AddAirliftUnitUnlock(profile, "Unit_Chr_Pilot_Female_01");
                return true;
            }
            if (reward.RewardConfigId != TransportRewardId) return false;
            AddAirliftUnitUnlock(profile, "Unit_Veh_APC_Fast");
            AddAirliftUnitUnlock(profile, "Unit_Veh_Helicopter_Transport");
            return true;
        }

        private static void AddAirliftUnitUnlock(PlayerProfileSaveData profile, string unitId)
        {
            if (!Contains(profile.ownedUnitUnlocks, unitId))
                profile.ownedUnitUnlocks = Append(profile.ownedUnitUnlocks, unitId);
        }
    }
}
