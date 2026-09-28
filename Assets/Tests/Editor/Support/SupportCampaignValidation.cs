#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using Game.Missions.Contracts;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportCampaignValidation
    {
        public static void RunFocusedValidation()
        {
            try {var t=new SupportCampaignValidation();t.FirstClearUnlockIsIdempotent();t.MigrationOnlyFromCompletedMilestone();t.WrongMissionReplayAndAmountReject();t.EarlyReplayStillHidden();t.AuthoredSteelPushRewardContract();t.AllScheduledGrantsAndMigrationPersist();Debug.Log("[SupportCampaignValidation] result=Passed tests=6 scope=four-abilities future-mission-content=pending");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportCampaignValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        private static SaveService Service()=>new SaveService(new JsonSaveRepository(Path.Combine(Path.GetTempPath(),"support-profile-tests",Guid.NewGuid().ToString("N"))));
        private static CampaignMissionRewardGrant[] Grants()=>new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch04.m02.smoke_screen_unlock",1)};
        [Test] public void FirstClearUnlockIsIdempotent()
        {
            var service=Service();var profile=service.LoadProfile();profile.ownedSupportAbilityUnlocks=new[]{"legacy.support","ability.smoke_screen"};service.SaveProfile(profile);
            var store=new CampaignMissionProgressStore(service);
            Assert.IsTrue(store.SettleWithRewards(CampaignMissionSequence.SteelPush,"first",1,true,3,90000,null,Grants()).Applied);
            var after=service.LoadProfile();CollectionAssert.AreEqual(profile.ownedSupportAbilityUnlocks,after.ownedSupportAbilityUnlocks);
            Assert.AreEqual(profile.blueprintParts.Length,after.blueprintParts.Length);
            Assert.IsFalse(store.SettleWithRewards(CampaignMissionSequence.SteelPush,"first",1,true,3,90000,null,Grants()).Applied);
            CollectionAssert.AreEqual(after.ownedSupportAbilityUnlocks,service.LoadProfile().ownedSupportAbilityUnlocks);
        }
        [Test] public void MigrationOnlyFromCompletedMilestone()
        {
            var profile=new PlayerProfileSaveData {ownedSupportAbilityUnlocks=new[]{"ability.radar_ping","legacy.support"},campaignMissionProgress=new[]{
                new CampaignMissionProgressSaveData {missionId=CampaignMissionSequence.SteelPush,available=true,firstClearCompleted=false}}};
            SupportProfileMigration.Normalize(profile);Assert.AreEqual(2,profile.ownedSupportAbilityUnlocks.Length);
            profile.campaignMissionProgress[0].firstClearCompleted=true;SupportProfileMigration.Normalize(profile);SupportProfileMigration.Normalize(profile);
            CollectionAssert.AreEqual(new[]{"ability.radar_ping","legacy.support","ability.smoke_screen"},profile.ownedSupportAbilityUnlocks);
        }
        [Test] public void WrongMissionReplayAndAmountReject()
        {
            var store=new CampaignMissionProgressStore(Service());
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(CampaignMissionSequence.AirCorridor,"wrong",1,true,3,1000,null,Grants()));
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(CampaignMissionSequence.SteelPush,"replay",1,false,3,1000,null,Grants()));
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(CampaignMissionSequence.SteelPush,"wrong",1,true,3,1000,null,new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch04.m02.smoke_screen_unlock",2)}));
        }
        [Test] public void AuthoredSteelPushRewardContract()
        {
            var config=UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.MissionDefinitionConfig>("Assets/Game/Configs/Missions/Chapter04/MissionDefinition_Ch04_M02_SteelPush.asset");
            Assert.IsNotNull(config);Assert.IsTrue(Game.Configs.MissionDefinitionContractValidation.TryValidateDefinition(config,out var error),error);
            bool found=false;foreach(var reward in config.FirstClearRewards)if(reward.RewardConfigId=="reward.ch04.m02.smoke_screen_unlock")
            {found=true;Assert.AreEqual("mission.reward.smoke_screen",reward.DisplayTextKey);Assert.AreEqual(1,reward.Amount);}
            Assert.IsTrue(found,"Actual first-clear asset must contain Smoke unlock.");
        }
        [Test] public void AllScheduledGrantsAndMigrationPersist()
        {
            string[] missions={"saga.ch04.m02.steel_push","saga.ch04.m03.split_front","saga.ch04.m04.grounded_signal","saga.ch05.m02.trust_under_fire"};
            var service=Service();var profile=service.LoadProfile();profile.ownedSupportAbilityUnlocks=new[]{"ability.radar_ping","legacy.support"};service.SaveProfile(profile);
            var store=new CampaignMissionProgressStore(service);
            foreach(var mission in missions)
            {
                string reward=SupportProfileMigration.RewardForMission(mission);var grants=new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,reward,1)};
                Assert.Throws<ArgumentException>(()=>store.SettleWithRewards("saga.ch01.m01.first_contact","wrong-"+mission,1,true,3,1000,null,grants));
                Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(mission,"wrong-amount",1,true,3,1000,null,new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,reward,2)}));
                Assert.IsTrue(store.SettleWithRewards(mission,"clear-"+mission,1,true,3,1000,null,grants).Applied);
                Assert.IsFalse(store.SettleWithRewards(mission,"clear-"+mission,1,true,3,1000,null,grants).Applied);
                CollectionAssert.Contains(service.LoadProfile().ownedSupportAbilityUnlocks,SupportProfileMigration.AbilityForReward(reward));
            }
            var after=service.LoadProfile();Assert.AreEqual(6,after.ownedSupportAbilityUnlocks.Length);Assert.AreEqual(0,after.blueprintParts.Length);
            var old=new PlayerProfileSaveData {ownedSupportAbilityUnlocks=new[]{"ability.radar_ping","legacy.support"},campaignMissionProgress=Array.ConvertAll(missions,m=>new CampaignMissionProgressSaveData {missionId=m,firstClearCompleted=true})};
            SupportProfileMigration.Normalize(old);SupportProfileMigration.Normalize(old);Assert.AreEqual(6,old.ownedSupportAbilityUnlocks.Length);Assert.AreEqual(SupportProfileMigration.CurrentVersion,old.supportMigrationVersion);
        }
        [Test] public void EarlyReplayStillHidden()
        {
            using var f=new SupportValidationFixture();var p=f.Em.GetComponentData<SupportMissionPolicyComponent>(f.Root);p.OwnedMask=1;p.AllowedMask=0;p.TestGrantMask=0;f.Em.SetComponentData(f.Root,p);
            Assert.AreEqual(SupportRejectionReason.MissionRestricted,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,f.Request()));
        }
    }
}
#endif
