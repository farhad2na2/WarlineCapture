using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
public sealed class M05BreachAssaultIntegrationTests
{
    public static void RunFocusedValidation()
    {
        try {var t=new M05BreachAssaultIntegrationTests();t.AuthoredMissionHasBilingualPlayableContract();t.FirstClearAndReplayPersistWithoutDuplicateUnlocks();t.TargetIdentityExcludesAuthoredScenery();t.VoicesCoverEveryLessonInBothLanguages();Debug.Log("[M05BreachIntegration] result=Passed tests=4");ValidationExit.Passed();}
        catch(Exception e){Debug.LogException(e);ValidationExit.Failed();}
    }
    [Test] public void AuthoredMissionHasBilingualPlayableContract()
    {
        var scenario=AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M05BreachAssaultConfigBuilder.ScenarioPath);
        Assert.IsTrue(scenario.TryValidate(out string error),error);
        Assert.IsTrue(scenario.Breach.Enabled);Assert.IsFalse(scenario.Defense.Enabled);Assert.IsFalse(scenario.Extraction.Enabled);
        int rifles=0;foreach(var group in scenario.UnitGroups)foreach(var unit in group.Units)if(group.FactionIndex==1 && unit.MissionRoleId=="role.friendly.command_squad")rifles+=unit.Count;
        Assert.AreEqual(8,rifles);
        Assert.AreEqual(20000,scenario.Breach.SecureHoldMilliseconds);
        var mission=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M05BreachAssaultConfigBuilder.MissionPath);
        Assert.IsTrue(MissionDefinitionContractValidation.TryValidateDefinition(mission,out error),error);
        string old=GameLocalization.CurrentLocaleCode;
        try {foreach(var locale in new[]{"en","fa-IR"}) {GameLocalization.SetLocale(locale,false);for(int i=1;i<=8;i++)foreach(string part in new[]{"title","body"}){string key=$"mission.m05.tutorial.{i}.{part}";string value=GameText.Get(key);Assert.IsNotEmpty(value);Assert.AreNotEqual(key,value);if(locale=="fa-IR")Assert.IsTrue(value.Any(c=>c>='\u0600'&&c<='\u06ff'),key);}}}
        finally {GameLocalization.SetLocale(old,false);}
        try {foreach(var locale in new[]{"en","fa-IR"})
        {
            GameLocalization.SetLocale(locale,false);
            for(int i=0;i<4;i++) {string key="mission.m05.guide.availability."+i;string value=GameText.Get(key);Assert.AreNotEqual(key,value);Assert.IsFalse(value.Contains("M3"));}
            string count=string.Format(GameText.Get("mission.guide.lesson_count"),8);Assert.IsTrue(count.Contains("8"));Assert.IsFalse(count.Contains("12"));
        }
        } finally {GameLocalization.SetLocale(old,false);}
    }
    [Test] public void FirstClearAndReplayPersistWithoutDuplicateUnlocks()
    {
        string path=Path.Combine(Path.GetTempPath(),"warline-m05-save-"+Guid.NewGuid().ToString("N"));
        const string mission="saga.ch01.m05.breach_assault";
        try
        {
            var save=new SaveService(new JsonSaveRepository(path));var store=new CampaignMissionProgressStore(save);
            save.SaveProfile(new PlayerProfileSaveData{credits=100,commanderXp=50});
            var grants=new[]{new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.commander_xp",750),new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",4000),
                new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch01.m05.ghillie_unlock",1),new CampaignMissionRewardGrant(MissionRewardKind.None,"reward.ch01.m05.apc_armor_parts",35)};
            Assert.IsTrue(store.SettleWithRewards(mission,"first",1,true,3,200000,null,grants).Applied);
            Assert.IsTrue(store.SettleWithRewards(mission,"first",1,true,3,200000,null,grants).IsDuplicate);
            var first=save.LoadProfile();Assert.AreEqual(4100,first.credits);Assert.AreEqual(800,first.commanderXp);
            CollectionAssert.Contains(first.ownedUnitUnlocks,"Unit_Chr_Ghillie_Male_01");Assert.AreEqual(35,first.blueprintParts.Single(x=>x.targetItemId=="Unit_Veh_APC_Heavy").amount);
            var replay=new[]{new CampaignMissionRewardGrant(MissionRewardKind.Credits,"",300)};
            Assert.IsTrue(store.SettleWithRewards(mission,"replay",2,false,2,220000,null,replay).Applied);
            Assert.AreEqual(4400,save.LoadProfile().credits);Assert.AreEqual(35,save.LoadProfile().blueprintParts.Single(x=>x.targetItemId=="Unit_Veh_APC_Heavy").amount);
            Assert.IsTrue(new CampaignMissionProgressStore(save).ReadAll().Single(x=>x.missionId==mission).firstClearCompleted);
            Assert.Throws<ArgumentException>(()=>store.SettleWithRewards("saga.ch01.m04.airlift","foreign",1,true,3,100,null,grants));
        }
        finally {if(Directory.Exists(path))Directory.Delete(path,true);}
    }
    [Test] public void VoicesCoverEveryLessonInBothLanguages()
    {
        Assert.IsTrue(Game.UI.Runtime.MatchHudAssistantUiSystemHelper.CanUseTutorialNarration(8));
        M05BreachAssaultEditorProbe.ValidateVoiceCatalog();
        foreach(var language in new[]{Game.Narrative.Contracts.FirstLaunchNarrativeLanguage.English,Game.Narrative.Contracts.FirstLaunchNarrativeLanguage.Persian})
            for(byte step=1;step<=8;step++)
            {
                string expected=$"vo.aria.tutorial.m05.{step:00}."+(language==Game.Narrative.Contracts.FirstLaunchNarrativeLanguage.Persian?"fa":"en");
                var actual=Game.UI.Shell.Ecs.UiShellEcsGateway.ResolveTutorialAudioEventId(step,8,Game.UI.Contracts.UiTutorialNarrationPhase.PrimaryAction,language);
                Assert.AreEqual(expected,actual.ToString());
            }
    }
    [Test] public void TargetIdentityExcludesAuthoredScenery()
    {
        var origin=new int2(1021,425);var target=new RuntimeBuildingCombatInfo{RuntimeBuildingId=1,OwnerFactionId=2,OriginCell=origin};
        Assert.IsTrue(CampaignMissionRuntimeSystem.MatchesBreachTarget(in target,1,origin));
        target.OwnerFactionId=1;Assert.IsFalse(CampaignMissionRuntimeSystem.MatchesBreachTarget(in target,1,origin));
        target.OwnerFactionId=2;target.OriginCell=new int2(928,344);Assert.IsFalse(CampaignMissionRuntimeSystem.MatchesBreachTarget(in target,1,origin));
    }
}
