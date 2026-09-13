using System;
namespace Game.Runtime
{
    public sealed partial class CampaignMissionProgressStore
    {
        private const string M05MissionId="saga.ch01.m05.breach_assault";
        private const string ReconRewardId="reward.ch01.m05.ghillie_unlock",ArmorRewardId="reward.ch01.m05.apc_armor_parts";
        private static bool IsBreachReward(string missionId,bool firstClear,CampaignMissionRewardGrant reward) =>
            missionId==M05MissionId && firstClear && (reward.RewardConfigId==ReconRewardId && reward.Amount==1 || reward.RewardConfigId==ArmorRewardId && reward.Amount==35);
        private static bool TryApplyBreachReward(PlayerProfileSaveData profile,CampaignMissionRewardGrant reward)
        {
            if(reward.RewardConfigId==ReconRewardId) {AddAirliftUnitUnlock(profile,"Unit_Chr_Ghillie_Male_01");return true;}
            if(reward.RewardConfigId!=ArmorRewardId) return false;
            profile.blueprintParts=AddBlueprintParts(profile.blueprintParts,"Unit_Veh_APC_Heavy",reward.Amount);return true;
        }
    }
}
