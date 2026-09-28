using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    public static class CH04M01AirCorridorRulesValidation
    {
        public static void Run()
        {
            var scenario=UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.ScenarioSetupConfig>(CH04M01AirCorridorConfigBuilder.ScenarioPath);
            Require(scenario!=null&&scenario.MissionRuntime.BaseMissionRoleId=="role.friendly.ground_sensor","Protected radar role must match objective publication role");
            for(int step=1;step<=4;step++)
            {
                var guidance=new CampaignMissionGuidanceProjectionComponent{Active=1,Version=1,GuidanceId=65000+step,Prompt=(CampaignMissionGuidancePromptKind)(74+step)};
                Require(Game.UI.Shell.Ecs.AssistantObjectiveProjectionUtility.TryBuildCampaignGuidanceRecommendation(in guidance,out var recommendation)&&recommendation.TutorialStep==step&&recommendation.TutorialStepCount==4,"Air Corridor must publish four distinct tutorial steps");
            }
            var staleTrailRoot=new GameObject("AirCorridorTrailLifecycleRegression");
            var staleTrail=staleTrailRoot.AddComponent<MissileTrailVfxView>();
            UnityEngine.Object.DestroyImmediate(staleTrailRoot);
            var trailField=typeof(MissileTrailVfxView).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic)??throw new InvalidOperationException("Trail singleton missing");
            trailField.SetValue(null,staleTrail);MissileTrailVfxView.ReleaseAll();
            Require(trailField.GetValue(null)==null,"Destroyed missile trail reference must be released before a new play session");
            using var world=new World("AirCorridorProtectionRules");var em=world.EntityManager;
            var root=em.CreateEntity();var radar=em.CreateEntity(typeof(UnitHealth));
            em.SetComponentData(radar,new UnitHealth{Current=500,Max=500});
            em.AddBuffer<CampaignMissionDefenseMember>(root).Add(new CampaignMissionDefenseMember{Entity=radar,IsSensor=1,HealthInitialized=1});
            var method=typeof(CampaignMissionRuntimeSystem).GetMethod("ProjectAirCorridorProtection",BindingFlags.Static|BindingFlags.NonPublic)??throw new InvalidOperationException("Radar projection missing");
            var facts=Project(method,em,root,default);Require(facts.ForwardPostBound==1&&facts.ForwardPostDestroyed==0&&facts.ForwardPostDamaged==0,"Healthy mobile radar did not bind");
            em.SetComponentData(radar,new UnitHealth{Current=400,Max=500});facts=Project(method,em,root,facts);Require(facts.ForwardPostDamaged==1&&facts.CoreBreached==0,"Radar damage did not remove undamaged star");
            var members=em.GetBuffer<CampaignMissionDefenseMember>(root);var member=members[0];member.Defeated=1;members[0]=member;
            em.SetComponentData(radar,new UnitHealth{Current=0,Max=500});facts=Project(method,em,root,facts);Require(facts.CoreBreached==1&&facts.ForwardPostDestroyed==1,"Destroyed radar did not fail mission");
            member.Defeated=0;members[0]=member;em.SetComponentData(radar,new UnitHealth{Current=500,Max=500});
            facts=Project(method,em,root,new CampaignMissionAttemptFactsComponent{ElapsedMilliseconds=240000});Require(facts.CoreBreached==1,"Mission deadline did not fail closed");
            Debug.Log("[AirCorridorRules] result=Passed mobileRadar=binding,damage,destruction deadline=240s guidance=4steps trailLifecycle=destroyed-reference");
        }
        private static CampaignMissionAttemptFactsComponent Project(MethodInfo method,EntityManager em,Entity root,CampaignMissionAttemptFactsComponent facts)
        {object[] args={em,root,facts};method.Invoke(null,args);return (CampaignMissionAttemptFactsComponent)args[2];}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
