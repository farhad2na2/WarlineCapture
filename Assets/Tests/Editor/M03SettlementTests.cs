using System;
using System.IO;
using System.Linq;
using Game.Runtime;
using Game.Missions.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class M03SettlementTests
{
    private const string Mission="saga.ch01.m03.radar_warning",Next="saga.ch01.m04.airlift";
    public static void RunFocusedValidation()
    {
        try
        {
            var tests=new M03SettlementTests();
            tests.GrantsSettleAtomicallyOnceAndReplayOnlyPaysReplayCredits();
            tests.PreownedUnlocksConvertAndForeignOrReplayGrantsReject();
            Debug.Log("[M03SettlementValidation] result=Passed tests=2"); ValidationExit.Passed();
        }
        catch(Exception e) {Debug.LogException(e); Debug.LogError("[M03SettlementValidation] result=Failed"); ValidationExit.Failed();}
    }
    private static CampaignMissionRewardGrant[] Rewards()=>new[]
    {
        new CampaignMissionRewardGrant(MissionRewardKind.None," reward.commander_xp ",400),
        new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",2000),
        new CampaignMissionRewardGrant(MissionRewardKind.None," reward.ch01.m03.guard_tower_unlock ",1),
        new CampaignMissionRewardGrant(MissionRewardKind.None," reward.ch01.m03.radar_ping_unlock ",1)
    };
    [Test] public void GrantsSettleAtomicallyOnceAndReplayOnlyPaysReplayCredits()=>WithStore((service,store)=>
    {
        service.SaveProfile(new PlayerProfileSaveData{credits=100,commanderXp=50});
        Assert.IsFalse(store.SettleWithRewards(Mission,"early",1,false,3,200000,null,new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",300)}).Applied);
        Assert.IsTrue(store.SettleWithRewards(Mission," first ",1,true,3,200000,Next,Rewards()).Applied);
        var first=service.LoadProfile();
        Assert.AreEqual(2100,first.credits); Assert.AreEqual(450,first.commanderXp);
        CollectionAssert.Contains(first.ownedBuildingUnlocks,"Building_GuardTower");
        CollectionAssert.Contains(first.ownedSupportAbilityUnlocks,"ability.radar_ping");
        Assert.IsTrue(store.ReadAll().Single(p=>p.missionId==Next).available);
        Assert.IsTrue(store.SettleWithRewards(Mission,"first",1,true,3,190000,Next,Rewards()).IsDuplicate);
        Assert.IsTrue(store.SettleWithRewards(Mission,"second-first",2,true,3,190000,Next,Rewards()).IsDuplicate);
        var replay=new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",300)};
        Assert.IsTrue(store.SettleWithRewards(Mission,"replay",2,false,2,220000,null,replay).Applied);
        Assert.IsTrue(store.SettleWithRewards(Mission,"replay",2,false,3,180000,null,replay).IsDuplicate);
        var saved=service.LoadProfile(); Assert.AreEqual(2400,saved.credits); Assert.AreEqual(450,saved.commanderXp);
        var entry=new CampaignMissionProgressStore(service).ReadAll().Single(p=>p.missionId==Mission);
        Assert.AreEqual(1,entry.successfulReplayCount); Assert.AreEqual(3,entry.bestStars); Assert.AreEqual(200000,entry.bestCompletionMilliseconds);
    });
    [Test] public void PreownedUnlocksConvertAndForeignOrReplayGrantsReject()=>WithStore((service,store)=>
    {
        service.SaveProfile(new PlayerProfileSaveData{ownedBuildingUnlocks=new[]{"Building_GuardTower"},ownedSupportAbilityUnlocks=new[]{"ability.radar_ping"}});
        Assert.Throws<ArgumentException>(()=>store.SettleWithRewards("saga.ch01.m02.establish_base","foreign",1,true,3,100,null,Rewards()));
        Assert.Throws<ArgumentException>(()=>store.SettleWithRewards(Mission,"replay",1,false,3,100,null,Rewards()));
        Assert.IsTrue(store.SettleWithRewards(Mission,"first",1,true,2,220000,Next,Rewards()).Applied);
        var saved=service.LoadProfile();
        Assert.AreEqual(1,saved.ownedBuildingUnlocks.Count(x=>x=="Building_GuardTower"));
        Assert.AreEqual(1,saved.ownedSupportAbilityUnlocks.Count(x=>x=="ability.radar_ping"));
        Assert.AreEqual(1,saved.blueprintParts.Single(x=>x.targetItemId=="upgrade.building.base_defense").amount);
        Assert.AreEqual(1,saved.blueprintParts.Single(x=>x.targetItemId=="ability.radar_ping").amount);
    });
    private static void WithStore(Action<SaveService,CampaignMissionProgressStore> test)
    {
        string root=Path.Combine(Path.GetTempPath(),"warline-m03-settlement-"+Guid.NewGuid().ToString("N"));
        try {var service=new SaveService(new JsonSaveRepository(root)); test(service,new CampaignMissionProgressStore(service));}
        finally {if(Directory.Exists(root)) Directory.Delete(root,true);}
    }
}
