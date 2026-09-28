using Game.Missions.Contracts;
namespace Game.Runtime
{
    public sealed partial class CampaignMissionProgressStore
    {
        private static bool IsSupportUnlock(string missionId,bool firstClear,CampaignMissionRewardGrant reward) =>
            firstClear && reward.Amount==1 && SupportProfileMigration.AbilityForGrant(missionId,reward.RewardConfigId).Length!=0;
        private static bool TryApplySupportUnlock(PlayerProfileSaveData profile,CampaignMissionRewardGrant reward)
        {
            string ability=SupportProfileMigration.AbilityForReward(reward.RewardConfigId);
            if(ability.Length==0)return false;
            if(!Contains(profile.ownedSupportAbilityUnlocks,ability))profile.ownedSupportAbilityUnlocks=Append(profile.ownedSupportAbilityUnlocks,ability);
            return true;
        }
    }
}
