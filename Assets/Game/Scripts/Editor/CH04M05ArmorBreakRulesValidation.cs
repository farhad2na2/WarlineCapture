using System;
using System.IO;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Unity.Collections;
using UnityEditor;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Game.Editor
{
    public static class CH04M05ArmorBreakRulesValidation
    {
        public static void Run()
        {
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.GroundedSignal)==CampaignMissionSequence.ArmorBreak,"Campaign progression missing");
            var runtime = new CampaignMissionRuntimeComponent { MissionId=CampaignMissionSequence.ArmorBreak, ScenarioId="scenario.ch04.m05.armor_break", OperationMapId="opmap.ch04.armor_break", DeterministicSeed=4005001, SessionToken="armor-real-attempt", AttemptOrdinal=2, SourceVersion=4, Version=1, Phase=MissionPhaseKind.Engage };
            var state = new CampaignMissionArmorBreakState { SessionToken=runtime.SessionToken, AttemptOrdinal=2, SourceVersion=4, Initialized=1, Ready=1,
                CoverageReady=1, AirCleared=1, BatteryDisabled=1, LauncherShots=1, AircraftUsed=1, ArmoredThreatsCleared=1, ArmorApproached=1, CommandDisabled=1, AuthorityRecovered=1 };
            var facts = new CampaignMissionAttemptFactsComponent { HostileTotalCount=12, HostileDefeatedCount=12 };
            Require(Advance(runtime,facts,state,out var win)&&win.Outcome==MissionOutcomeKind.Victory,"Qualified live roster victory rejected");
            Require(!Advance(win,facts,state,out _),"Terminal victory settled twice");
            foreach(ArmorBreakFailure failure in Enum.GetValues(typeof(ArmorBreakFailure)))
            {
                if(failure==ArmorBreakFailure.None)continue;
                var failed=state;failed.Failure=failure;
                Require(Advance(runtime,facts,failed,out var defeat)&&defeat.Outcome==MissionOutcomeKind.Defeat,"Failure lost precedence: "+failure);
            }
            var incomplete=state;incomplete.LauncherShots=0;Require(!Advance(runtime,facts,incomplete,out _),"Battery death substituted for actual G2G shot");
            incomplete=state;incomplete.AircraftUsed=0;Require(!Advance(runtime,facts,incomplete,out _),"Automatic hostile deaths substituted for helicopter mastery");
            incomplete=state;incomplete.ArmoredThreatsCleared=0;
            Require(CampaignMissionArmorBreakRuleUtility.Stage(in incomplete)==4 && !Advance(runtime,facts,incomplete,out _),"First helicopter shot substituted for clearing original armored threats");
            incomplete=state;incomplete.AuthorityRecovered=0;Require(!Advance(runtime,facts,incomplete,out _),"Hostile defeat substituted for recovery");
            var survivors=facts;survivors.HostileDefeatedCount=11;Require(!Advance(runtime,survivors,state,out _),"Live original military member ignored");
            var stale=state;stale.SessionToken="old-session";Require(!Advance(runtime,facts,stale,out _),"Stale session settled");
            stale=state;stale.AttemptOrdinal--;Require(!Advance(runtime,facts,stale,out _),"Prior retry settled");
            stale=state;stale.SourceVersion--;Require(!Advance(runtime,facts,stale,out _),"Stale source settled");
            Require(CampaignMissionArmorBreakRuleUtility.AdvanceRecovery(5000,1000,false)==0,"Departure/contest failed to cancel custody hold");
            Require(CampaignMissionArmorBreakRuleUtility.AdvanceRecovery(5000,int.MaxValue,true)==6000,"Hold overflowed");
            Require(CampaignMissionArmorBreakRuleUtility.AdvanceRecovery(0,-100,true)==0,"Negative time advanced hold");
            using (var world = new World("Armor Break finite reserve scope validation"))
            {
                var em=world.EntityManager;
                var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionArmorBreakState),typeof(SupportFuelScopeComponent));
                em.SetComponentData(root,runtime);em.SetComponentData(root,state);
                var inherited=em.CreateEntity(typeof(BuildingResourceStorageComponent));
                em.SetComponentData(inherited,new BuildingResourceStorageComponent{OwnerFactionId=1,FuelStorageCapacity=1000,StoredFuelBarrels=1000});
                Require(CampaignSteelPushFuelScope.TryGet(em,out var missing,out _)&&missing==Entity.Null,"Missing dedicated reserve fell back to inherited stock");
                var reserve=em.CreateEntity(typeof(BuildingResourceStorageComponent));
                em.SetComponentData(reserve,new BuildingResourceStorageComponent{OwnerFactionId=1,FuelStorageCapacity=240,StoredFuelBarrels=200,CivilianFuelReserveBarrels=40});
                state.FuelReserve=reserve;em.SetComponentData(root,state);
                em.SetComponentData(root,new SupportFuelScopeComponent{Storage=reserve,Required=1});
                Require(CampaignSteelPushFuelScope.TryGet(em,out var scoped,out _)&&scoped==reserve,"Dedicated consumer missed Armor reserve");
                Require(SupportFuelTransactionSystem.Available(em,1)==160,"Optional Support used unrelated map stock");
                Require(!SupportFuelTransactionSystem.TryConsumeImmediate(em,1,161),"Support consumed protected civilian fuel");
                Require(SupportFuelTransactionSystem.TryConsumeImmediate(em,1,20)&&em.GetComponentData<BuildingResourceStorageComponent>(reserve).StoredFuelBarrels==180,"Actual Support transaction did not consume finite reserve");
                CampaignSteelPushFuelScope.Drain(em,reserve,1000);
                Require(em.GetComponentData<BuildingResourceStorageComponent>(reserve).StoredFuelBarrels==40,"Vehicle drain breached protected floor");
                state.AttemptOrdinal--;em.SetComponentData(root,state);
                Require(CampaignSteelPushFuelScope.TryGet(em,out var retry,out _)&&retry==Entity.Null,"Prior attempt reserve leaked into retry");
            }
            using (var world = new World("Armor Break accepted helicopter order observation"))
            {
                var em=world.EntityManager;
                var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionArmorBreakState));
                em.SetComponentData(root,runtime);
                var actor=em.CreateEntity(typeof(UnitHealth),typeof(UnitAttack),typeof(UnitAttackTraceComponent),typeof(CampaignMissionUnitRoleComponent));
                em.SetComponentData(actor,new UnitHealth{Max=100,Current=100});
                em.SetComponentData(actor,new UnitAttackTraceComponent{ShotCounter=9});
                em.SetComponentData(actor,new CampaignMissionUnitRoleComponent{SessionToken=runtime.SessionToken,MissionRoleId="role.friendly.aircraft"});
                var target=em.CreateEntity(typeof(UnitHealth),typeof(Faction),typeof(CampaignMissionUnitRoleComponent),typeof(LocalTransform));
                em.SetComponentData(target,new UnitHealth{Max=100,Current=100});em.SetComponentData(target,new Faction{Id=2});
                em.SetComponentData(target,new CampaignMissionUnitRoleComponent{SessionToken=runtime.SessionToken,MissionRoleId="role.hostile.armor"});
                var roster=em.AddBuffer<CampaignMissionArmorBreakMember>(root);
                roster.Add(new CampaignMissionArmorBreakMember{Entity=actor,Kind=ArmorBreakMemberKind.Aircraft});
                roster.Add(new CampaignMissionArmorBreakMember{Entity=target,Kind=ArmorBreakMemberKind.HostileArmor});
                var observed=new CampaignMissionArmorBreakState { SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,
                    SourceVersion=runtime.SourceVersion,Initialized=1,Ready=1,Aircraft=actor };
                em.SetComponentData(root,observed);
                var system=world.GetOrCreateSystem<CampaignMissionArmorBreakAircraftOrderSystem>();
                void Observe()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionArmorBreakAircraftOrderSystem>(system).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(system));
                // Automatic engagement is not player consent.
                em.AddComponentData(actor,new EngageTarget{Target=target,IsCommanded=0});Observe();
                Require(em.GetComponentData<CampaignMissionArmorBreakState>(root).AircraftAttackOrdered==0,"Automatic attack counted as consent");
                new UnitTargetOrderSystem().IssueDirectAttackTarget(em,actor,target,default,default);
                Observe();observed=em.GetComponentData<CampaignMissionArmorBreakState>(root);
                Require(observed.AircraftAttackOrdered==1&&observed.AircraftShotBaseline==9,"Accepted normal Attack not captured before combat");
                Require(!CampaignMissionArmorBreakAircraftOrderSystem.HasObservedShot(em,in observed),"Consent alone fabricated a shot");
                // Supply the combat event to this isolated observer test, not a mission outcome.
                em.SetComponentData(actor,new UnitAttackTraceComponent{ShotCounter=10});em.SetComponentData(target,new UnitHealth{Max=100,Current=0});
                Require(CampaignMissionArmorBreakAircraftOrderSystem.HasObservedShot(em,in observed),"First shot was discarded after target death");
                em.SetComponentData(target,new UnitHealth{Max=100,Current=100});
                observed.AircraftAttackOrdered=0;observed.AttemptOrdinal--;em.SetComponentData(root,observed);Observe();
                Require(em.GetComponentData<CampaignMissionArmorBreakState>(root).AircraftAttackOrdered==0,"Prior attempt aircraft order accepted");
            }
            // Use the actual authored threat layout to qualify the normal attack goal.
            var aircraftPosition=new float3(1540,30,379);var firstTank=new float3(1585,0,529);
            var hover=CampaignMissionAircraftStandoffUtility.ResolveGroundAttackGoal(aircraftPosition,firstTank,155,10);
            Require(math.distance(hover.xz,firstTank.xz)<155 && math.distance(hover.xz,firstTank.xz)>120,"Helicopter goal forfeits its weapon range advantage");
            Require(math.distance(hover.xz,new float2(1597,529))>120 && math.distance(hover.xz,new float2(1609,529))>120,"Standoff moves inside adjacent original tank fire range");
            var stable=CampaignMissionAircraftStandoffUtility.ResolveGroundAttackGoal(hover,firstTank,155,10);
            Require(math.distancesq(stable,hover)<.0001f,"Repeated normal Attack drifts toward target center");
            Require(math.all(math.isfinite(CampaignMissionAircraftStandoffUtility.ResolveGroundAttackGoal(firstTank,firstTank,155,10))),"Coincident actor creates invalid hover goal");
            ValidateAssaultSurvivors();
            ValidateResultSettlement(runtime);
            Debug.Log("[ArmorBreakRules] result=Passed mastery=actual-shot,aircraft originals=12 recovery=qualified failures=all stale=session,retry,source settlement=idempotent");
        }
        private static void ValidateAssaultSurvivors()
        {
            using var world=new World("Armor Break armed assault survivors");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionArmorBreakState));
            Entity Actor(bool armed,int health)
            {
                var entity=em.CreateEntity(typeof(UnitCombat),typeof(UnitAttack),typeof(UnitHealth),typeof(LocalTransform));
                em.SetComponentData(entity,new UnitCombat{CanAttack=armed?(byte)1:(byte)0});
                em.SetComponentData(entity,new UnitAttack{Range=155,Damage=armed?240:0});
                em.SetComponentData(entity,new UnitHealth{Max=700,Current=health});
                em.SetComponentData(entity,LocalTransform.FromPosition(new float3(1540,30,379)));return entity;
            }
            var apc=Actor(false,550);var tank=Actor(true,0);var helicopter=Actor(true,700);
            var roster=em.AddBuffer<CampaignMissionArmorBreakMember>(root);
            roster.Add(new CampaignMissionArmorBreakMember{Entity=apc,Kind=ArmorBreakMemberKind.Armor});
            roster.Add(new CampaignMissionArmorBreakMember{Entity=tank,Kind=ArmorBreakMemberKind.Armor});
            roster.Add(new CampaignMissionArmorBreakMember{Entity=helicopter,Kind=ArmorBreakMemberKind.Aircraft});
            var mission=new CampaignMissionArmorBreakState{Aircraft=helicopter,AircraftUsed=1,ArmoredThreatsCleared=1};
            em.SetComponentData(root,mission);
            Require(CampaignMissionGuidanceProjectionSystem.ResolveArmorBreakAssaultActor(em,root,in mission)==helicopter,"Unarmed APC or dead tank blocks qualified helicopter fallback");
            bool ResolveTarget(in CampaignMissionGuidanceProjectionComponent cue,out UiMissionTutorialTarget target)
            {
                var method=typeof(UiShellEcsGateway).GetMethod("ResolveArmorBreakTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
                Require(method!=null,"Production tutorial target resolver missing");
                object[] args={em,root,cue,null};bool resolved=(bool)method.Invoke(null,args);target=(UiMissionTutorialTarget)args[3];return resolved;
            }
            var guidance=new CampaignMissionGuidanceProjectionComponent{GuidanceId=67006,SourceEntity=helicopter,RecommendationKind=AssistantRecommendationKind.Move};
            Require(ResolveTarget(in guidance,out var approach)&&approach.RequiredSelectionCount==1&&!approach.DragSelection&&approach.BattleAction==UiTutorialBattleAction.Move,"Helicopter fallback selects armored group or loses normal approach");
            guidance.RecommendationKind=AssistantRecommendationKind.Attack;
            Require(ResolveTarget(in guidance,out var attack)&&attack.BattleAction==UiTutorialBattleAction.Attack,"Helicopter fallback loses normal Attack action");
            em.SetComponentData(tank,new UnitHealth{Max=1250,Current=1});
            Require(CampaignMissionGuidanceProjectionSystem.ResolveArmorBreakAssaultActor(em,root,in mission)==tank,"Qualified helicopter supersedes surviving armed armor");
            guidance.SourceEntity=tank;
            Require(ResolveTarget(in guidance,out var armored)&&armored.RequiredSelectionCount==1,"Stage6 selection includes unarmed APC");
            guidance.GuidanceId=67005;guidance.SourceEntity=apc;guidance.RecommendationKind=AssistantRecommendationKind.Move;
            Require(ResolveTarget(in guidance,out var move)&&move.RequiredSelectionCount==2,"Stage5 approach dropped surviving APC");
            em.SetComponentData(tank,new UnitHealth{Max=1250,Current=0});
            em.SetComponentData(helicopter,new UnitHealth{Max=700,Current=0});
            Require(CampaignMissionGuidanceProjectionSystem.ResolveArmorBreakAssaultActor(em,root,in mission)==Entity.Null,"Dead helicopter recommended as assault fallback");
        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH04M05ArmorBreakConfigBuilder.MissionPath);
            Require(authored!=null,"Authored result contract missing");
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var definitions=builder.Allocate(ref catalog.Missions,1);
            ref var definition=ref definitions[0];
            definition.MissionId=authored.MissionId;definition.ScenarioId=authored.ScenarioId;
            definition.OperationMapId=authored.OperationMapId;definition.Defense.Enabled=1;
            var objectives=builder.Allocate(ref definition.Objectives,authored.Objectives.Length);
            for(int i=0;i<objectives.Length;i++)
            {
                var objective=authored.Objectives[i];
                objectives[i]=new CampaignMissionObjectiveBlob{ObjectiveId=objective.ObjectiveId,Rule=objective.Rule,
                    RequiredCount=objective.RequiredCount,MissionRoleId=objective.MissionRoleId,TargetConfigId=objective.TargetConfigId??string.Empty};
            }
            var stars=builder.Allocate(ref definition.StarRules,authored.Stars.Length);
            for(int i=0;i<stars.Length;i++)stars[i]=new CampaignMissionStarRuleBlob{StarIndex=authored.Stars[i].StarIndex,
                Rule=authored.Stars[i].Rule,Threshold=authored.Stars[i].Threshold,DisplayTextKey=authored.Stars[i].DisplayTextKey};
            var rewards=builder.Allocate(ref definition.FirstClearRewards,authored.FirstClearRewards.Length);
            for(int i=0;i<rewards.Length;i++)rewards[i]=new CampaignMissionRewardBlob{Kind=authored.FirstClearRewards[i].Kind,
                RewardConfigId=authored.FirstClearRewards[i].RewardConfigId??string.Empty,Amount=authored.FirstClearRewards[i].Amount,
                DisplayTextKey=authored.FirstClearRewards[i].DisplayTextKey};
            using var blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            using var world=new World("Armor Break actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent{CommandSquadSpawned=1,CommandSquadAlive=1,
                HostileTotalCount=12,HostileDefeatedCount=12,CivilianTotalCount=3,ElapsedMilliseconds=354968,
                ArmorBreakCoverageReady=1,ArmorBreakAirCleared=1,ArmorBreakHeavyDisabled=1,ArmorBreakLauncherShots=1,
                ArmorBreakAircraftUsed=1,ArmorBreakArmoredThreatsCleared=1,ArmorBreakArmorApproached=1,
                ArmorBreakCommandDisabled=1,ArmorBreakAuthorityRecovered=1,ArmorBreakRecoveryMilliseconds=6000};
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Armor victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.ArmorBreakLauncherShots=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed actual launcher mastery");
            incomplete=facts;incomplete.ArmorBreakRecoveryMilliseconds=5999;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed authority hold");
            incomplete=facts;incomplete.HostileDefeatedCount=11;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.ArmorBreakReliefLost=1;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original relief loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"armor-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Armor first-clear settlement rejected");
                var earned=save.LoadProfile();
                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Armor rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();
                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static bool Advance(in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts,in CampaignMissionArmorBreakState state,out CampaignMissionRuntimeComponent next)=>CampaignMissionArmorBreakRuleUtility.TryAdvance(in runtime,in facts,in state,true,out next);
        private static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
    }
}
