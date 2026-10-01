using System;
namespace Game.Runtime
{
    // Permanent ownership only. Charges, tactical stock, flights and Entity handles never enter the profile.
    public static class SupportProfileMigration
    {
        public const int CurrentVersion=1;
        public static string AbilityForReward(string reward) => reward switch
        {
            "reward.ch04.m02.smoke_screen_unlock"=>"ability.smoke_screen",
            "reward.ch04.m03.precision_strike_unlock"=>"ability.precision_strike",
            "reward.ch04.m04.paratrooper_reinforcements_unlock" or "reward.ch04.m04.paratroopers_unlock"=>"ability.paratrooper_reinforcements",
            "reward.ch05.m02.supply_drop_unlock"=>"ability.supply_drop",
            _=>string.Empty
        };
        public static string RewardForMission(string mission) => mission switch
        {
            "saga.ch04.m02.steel_push"=>"reward.ch04.m02.smoke_screen_unlock",
            "saga.ch04.m03.split_front"=>"reward.ch04.m03.precision_strike_unlock",
            "saga.ch04.m04.grounded_signal"=>"reward.ch04.m04.paratrooper_reinforcements_unlock",
            "saga.ch05.m02.trust_under_fire"=>"reward.ch05.m02.supply_drop_unlock",
            _=>string.Empty
        };
        public static string AbilityForGrant(string mission,string reward) =>
            RewardForMission(mission)==reward || mission=="saga.ch04.m04.grounded_signal" && reward=="reward.ch04.m04.paratroopers_unlock"
                ? AbilityForReward(reward) : string.Empty;
        public static void Normalize(PlayerProfileSaveData profile)
        {
            if(profile==null)return;
            profile.ownedSupportAbilityUnlocks??=Array.Empty<string>();
            foreach(var entry in profile.campaignMissionProgress??Array.Empty<CampaignMissionProgressSaveData>())
            {
                if(entry==null||!entry.firstClearCompleted)continue;
                string ability=AbilityForReward(RewardForMission(entry.missionId));
                if(ability.Length==0||Array.IndexOf(profile.ownedSupportAbilityUnlocks,ability)>=0)continue;
                var owned=profile.ownedSupportAbilityUnlocks;
                Array.Resize(ref owned,owned.Length+1);owned[owned.Length-1]=ability;profile.ownedSupportAbilityUnlocks=owned;
            }
            profile.supportMigrationVersion=Math.Max(profile.supportMigrationVersion,CurrentVersion);
        }
    }
}
