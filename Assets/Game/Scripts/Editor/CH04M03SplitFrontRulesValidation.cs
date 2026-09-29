using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Editor
{
    public static class CH04M03SplitFrontRulesValidation
    {
        public static void Run()
        {
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SteelPush)==CampaignMissionSequence.SplitFront,"Progression missing");
            using var world=new World("SplitFront safety rules");var em=world.EntityManager;
            var launcher=em.CreateEntity(typeof(UnitHealth),typeof(Faction),typeof(LocalTransform),typeof(GroundMissileLauncherComponent),typeof(GroundMissileLauncherStateComponent),typeof(SplitFrontLauncherCommandState));
            em.SetComponentData(launcher,new UnitHealth{Current=500,Max=500});em.SetComponentData(launcher,LocalTransform.FromPosition(0,0,0));em.SetComponentData(launcher,new Faction{Id=1});
            em.SetComponentData(launcher,new GroundMissileLauncherComponent{MinRange=35,MaxRange=200,DamageRadius=8});
            em.SetComponentData(launcher,new SplitFrontLauncherCommandState{ProtectedCenter=new float3(100,0,60),ProtectedRadius=18});
            em.AddComponentData(launcher,new CampaignMissionUnitRoleComponent{SessionToken="safety-attempt"});
            var target=em.CreateEntity(typeof(UnitHealth),typeof(Faction),typeof(LocalTransform),typeof(CampaignMissionUnitRoleComponent));
            em.SetComponentData(target,new UnitHealth{Current=500,Max=500});em.SetComponentData(target,new Faction{Id=2});em.SetComponentData(target,new CampaignMissionUnitRoleComponent{SessionToken="safety-attempt",MissionRoleId="role.hostile.battery"});
            em.SetComponentData(target,LocalTransform.FromPosition(100,0,0));
            Require(CampaignSplitFrontLauncherSafety.IsSafe(em,launcher,target),"Safe verified battery rejected");
            Require(!CampaignSplitFrontLauncherSafety.MayFire(em,launcher,target),"Autonomous firing allowed");
            new UnitTargetOrderSystem().IssueDirectAttackTarget(em,launcher,target,default,new float3(100,0,0));
            Require(em.HasComponent<EngageTarget>(launcher)&&em.GetComponentData<EngageTarget>(launcher).IsCommanded==1&&CampaignSplitFrontLauncherSafety.MayFire(em,launcher,target),"Normal Attack did not authorize firing directly");
            CampaignSplitFrontLauncherSafety.RecordLaunch(em,launcher);
            Require(CampaignSplitFrontLauncherSafety.MayFire(em,launcher,target),"Attack target lost after a shot; reload must retain the order");
            foreach(float distance in new[]{34f,201f})
            {em.SetComponentData(target,LocalTransform.FromPosition(distance,0,0));Require(!CampaignSplitFrontLauncherSafety.IsSafe(em,launcher,target)&&!CampaignSplitFrontLauncherSafety.TryIssueAttack(em,launcher,target),"Range violation accepted");}
            em.SetComponentData(target,LocalTransform.FromPosition(100,0,60));Require(!CampaignSplitFrontLauncherSafety.IsSafe(em,launcher,target),"Protected civilian area accepted");
            em.SetComponentData(target,LocalTransform.FromPosition(100,0,0));em.AddComponent<CampaignMissionCombatSuppressedTag>(target);Require(!CampaignSplitFrontLauncherSafety.IsSafe(em,launcher,target),"Unreleased target accepted");em.RemoveComponent<CampaignMissionCombatSuppressedTag>(target);
            var other=em.CreateEntity(typeof(UnitHealth),typeof(Faction),typeof(LocalTransform),typeof(CampaignMissionUnitRoleComponent));
            em.SetComponentData(other,new UnitHealth{Current=500,Max=500});em.SetComponentData(other,new Faction{Id=2});em.SetComponentData(other,LocalTransform.FromPosition(120,0,0));em.SetComponentData(other,new CampaignMissionUnitRoleComponent{SessionToken="safety-attempt",MissionRoleId="role.hostile.diversion"});
            Require(CampaignSplitFrontLauncherSafety.IsSafe(em,launcher,other),"Normal launcher restricted to one authored battery target");
            em.SetComponentData(launcher,new GroundMissileLauncherStateComponent{Phase=(byte)GroundMissileLauncherPhase.Preparing,TargetEntity=target,Timer=1});
            var selection=new SelectionUiReadModelLookup();
            Require(selection.GetFocusedUnitUiStatus(em,launcher)==SelectionUiReadModelLookup.FocusedUnitUiStatus.MissilePreparing,"Preparation was presented as a launched missile");
            new UnitMoveOrderSystem().ClearMovementOrderComponents(em,launcher);
            Require(em.GetComponentData<GroundMissileLauncherStateComponent>(launcher).Phase==0&&em.GetComponentData<SplitFrontLauncherCommandState>(launcher).CommandedTarget==Entity.Null&&!CampaignSplitFrontLauncherSafety.MayFire(em,launcher,target),"Normal Hold/Stop clearing failed to stop preparation");
            Require(!em.HasComponent<EngageTarget>(launcher),"Stopped attack order retained its target");
            new UnitTargetOrderSystem().IssueDirectAttackTarget(em,launcher,target,default,new float3(100,0,0));
            em.AddComponent<GroundMissileInFlightComponent>(launcher);em.SetComponentData(launcher,new GroundMissileLauncherStateComponent{Phase=(byte)GroundMissileLauncherPhase.Launching,TargetEntity=target,Timer=1});
            CampaignSplitFrontLauncherSafety.Stop(em,launcher);
            Require(em.HasComponent<GroundMissileInFlightComponent>(launcher)&&em.GetComponentData<GroundMissileLauncherStateComponent>(launcher).Phase==(byte)GroundMissileLauncherPhase.Launching&&em.GetComponentData<GroundMissileLauncherStateComponent>(launcher).Timer==1,"Hold recalled a launched missile or reset reload timing");
            Require(selection.GetFocusedUnitUiStatus(em,launcher)==SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched,"Actual flight not presented as launched");
            var root=em.CreateEntity();em.AddBuffer<CampaignMissionDefenseMember>(root).Add(new CampaignMissionDefenseMember{Entity=target,FactionId=2});
            CampaignMissionDefenseDeathObservationSystem.Observe(em,root,"safety-attempt");
            Require(em.GetBuffer<CampaignMissionDefenseMember>(root)[0].Defeated==0,"Alive target counted as defeated");
            em.SetComponentData(target,new UnitHealth{Current=0,Max=500});
            CampaignMissionDefenseDeathObservationSystem.Observe(em,root,"other-attempt");
            Require(em.GetBuffer<CampaignMissionDefenseMember>(root)[0].Defeated==0,"Stale attempt counted a death");
            CampaignMissionDefenseDeathObservationSystem.Observe(em,root,"safety-attempt");em.DestroyEntity(target);
            Require(em.GetBuffer<CampaignMissionDefenseMember>(root)[0].Defeated==1,"Native death lost during paused presentation cleanup");
            var missing=em.CreateEntity();em.GetBuffer<CampaignMissionDefenseMember>(root).Add(new CampaignMissionDefenseMember{Entity=missing,FactionId=2});em.DestroyEntity(missing);
            CampaignMissionDefenseDeathObservationSystem.Observe(em,root,"safety-attempt");
            Require(em.GetBuffer<CampaignMissionDefenseMember>(root)[1].Defeated==0,"Missing entity fabricated a defeat");
            Debug.Log("[SplitFrontDeathObservation] result=Passed actual-zero-health=durable stale-attempt=rejected missing-entity=not-defeated");
            ValidateProtection(em);
            Debug.Log("[SplitFrontRules] result=Passed progression=18 range=minimum,maximum civilians=protected attack=direct retained-after-shot=true hold=stops-preparation noRecall=in-flight targets=normal-hostiles");
        }
        private static void ValidateProtection(EntityManager em)
        {
            var root=em.CreateEntity(typeof(CampaignMissionSplitFrontState),typeof(CampaignMissionDefenseStateComponent));
            em.AddBuffer<CampaignMissionDefenseMember>(root);
            var baseEntity=em.CreateEntity(typeof(UnitHealth));em.SetComponentData(baseEntity,new UnitHealth{Current=1000,Max=1000});
            var runtime=new CampaignMissionRuntimeComponent{MissionId=CampaignMissionSequence.SplitFront,SessionToken="protection",AttemptOrdinal=1,Phase=MissionPhaseKind.Engage};
            em.SetComponentData(root,new CampaignMissionSplitFrontState{SessionToken=runtime.SessionToken,AttemptOrdinal=1,Initialized=1,Base=baseEntity});
            CampaignMissionAttemptFactsComponent Project(CampaignMissionAttemptFactsComponent facts)
            {
                object[] args={em,root,runtime,facts};typeof(CampaignMissionRuntimeSystem).GetMethod("ProjectSplitFrontProtection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,args);
                return (CampaignMissionAttemptFactsComponent)args[3];
            }
            Require(Project(default).ForwardPostBound==1,"Forward base not bound");
            em.SetComponentData(baseEntity,new UnitHealth{Current=900,Max=1000});
            Require(Project(default).ForwardPostDamaged==1&&Project(default).ForwardPostDestroyed==0,"Base damage became an invented loss");
            em.SetComponentData(baseEntity,new UnitHealth{Current=0,Max=1000});Require(Project(default).ForwardPostDestroyed==1,"Destroyed base did not fail");
            em.SetComponentData(baseEntity,new UnitHealth{Current=1000,Max=1000});
            Require(Project(new CampaignMissionAttemptFactsComponent{CivilianLossCount=1}).CoreBreached==1,"Civilian loss did not fail");
            Require(Project(new CampaignMissionAttemptFactsComponent{ElapsedMilliseconds=240000}).CoreBreached==1,"Deadline did not fail");
            em.DestroyEntity(baseEntity);Require(Project(default).ForwardPostDestroyed==1,"Removed bound base did not fail");
            Debug.Log("[SplitFrontProtection] result=Passed base=binding,damage,destruction,removal civilians=mandatory deadline=240s");
        }
        private static void Require(bool value,string error){if(!value)throw new InvalidOperationException(error);}
    }
}
