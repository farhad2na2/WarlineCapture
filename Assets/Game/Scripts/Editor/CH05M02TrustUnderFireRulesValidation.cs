using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Shell.Ecs;
using Game.UI.Contracts;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH05M02TrustUnderFireRulesValidation
    {
        public static void Run()
        {
            var runtime=new CampaignMissionRuntimeComponent { MissionId=CampaignMissionSequence.TrustUnderFire,ScenarioId="scenario.ch05.m02.trust_under_fire",OperationMapId="opmap.ch05.trust_under_fire",SessionToken="trust-rules",AttemptOrdinal=1,SourceVersion=3,Version=1,DeterministicSeed=1,Phase=MissionPhaseKind.Engage };
            var mission=new CampaignMissionTrustUnderFireState { SessionToken=runtime.SessionToken,AttemptOrdinal=1,SourceVersion=3,Initialized=1,Ready=1,
                NorthCrossed=1,SouthCrossed=1,NorthArrived=1,SouthArrived=1,RelayVerified=1,MilitaryCleared=1,StableMilliseconds=6000 };
            var facts=new CampaignMissionAttemptFactsComponent { HostileTotalCount=9,HostileDefeatedCount=9,CivilianTotalCount=4 };
            Require(CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime,in facts,in mission,true,out var victory)&&victory.Outcome==MissionOutcomeKind.Victory,"Qualified convoy and broadcast victory rejected");
            Require(!CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in victory,in facts,in mission,true,out _),"Terminal outcome repeated");
            var incomplete=mission;incomplete.SouthCrossed=0;Require(!CampaignMissionTrustUnderFireRuleUtility.VictoryQualified(in incomplete,in facts),"Shelter shortcut substituted for real south crossing");
            incomplete=mission;incomplete.SouthArrived=0;Require(!CampaignMissionTrustUnderFireRuleUtility.VictoryQualified(in incomplete,in facts),"North convoy substituted for independent south arrival");
            incomplete=mission;incomplete.RelayVerified=0;Require(!CampaignMissionTrustUnderFireRuleUtility.VictoryQualified(in incomplete,in facts),"Dead guards substituted for verified living broadcast source");
            var survivor=facts;survivor.HostileDefeatedCount=8;Require(!CampaignMissionTrustUnderFireRuleUtility.VictoryQualified(in mission,in survivor),"Original military survivor ignored");
            foreach(TrustUnderFireFailure failure in Enum.GetValues(typeof(TrustUnderFireFailure)))
            {if(failure==TrustUnderFireFailure.None)continue;var failed=mission;failed.Failure=failure;Require(CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime,in facts,in failed,true,out var defeat)&&defeat.Outcome==MissionOutcomeKind.Defeat,"Failure lost precedence: "+failure);}
            var stale=mission;stale.AttemptOrdinal--;Require(!CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Prior retry settled");
            stale=mission;stale.SourceVersion--;Require(!CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Stale source settled");
            stale=mission;stale.SessionToken="old";Require(!CampaignMissionTrustUnderFireRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Stale session settled");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(5000,1000,false)==0,"Interrupted shelter hold did not reset");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvanceHold(5000,int.MaxValue,true)==6000,"Hold overflowed");
            Require(SupportProfileMigration.AbilityForGrant(CampaignMissionSequence.TrustUnderFire,"reward.ch05.m02.supply_drop_unlock")=="ability.supply_drop","Wrong first-clear unlock contract");
            Require(SupportProfileMigration.AbilityForGrant(CampaignMissionSequence.CitywideAlert,"reward.ch05.m02.supply_drop_unlock")==string.Empty,"Prior mission grants Supply");
            Require(!CampaignMissionTrustUnderFireRuleUtility.ShouldFailEscortLoss(0,0,9,0),"Uninitialized combat actors caused defeat");
            Require(!CampaignMissionTrustUnderFireRuleUtility.ShouldFailEscortLoss(25,0,9,8),"Partial original health initialization caused defeat");
            Require(CampaignMissionTrustUnderFireRuleUtility.ShouldFailEscortLoss(26,0,9,8),"All original escort deaths left surviving military deadlock");
            Require(!CampaignMissionTrustUnderFireRuleUtility.ShouldFailEscortLoss(26,1,9,8),"Surviving original escort rejected");
            incomplete=mission;incomplete.RelayApproached=0;incomplete.NorthCleared=incomplete.SouthCleared=incomplete.RelayCleared=1;incomplete.RelayVerified=0;
            Require(CampaignMissionTrustUnderFireRuleUtility.Stage(in incomplete)==7,"Defeated guards forced impossible dead escort approach before engineer verification");
            var crossing = new float3(680,0,505);
            byte passage = CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(0,new float3(710,0,505),crossing);
            Require(passage==0,"Far-side arrival fabricated a designated crossing");
            passage=CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(0,new float3(650,0,505),crossing);
            Require(passage==1,"Qualified west entry rejected");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,new float3(710,0,505),crossing)==1,"Skipped middeck fabricated transit");
            passage=CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,crossing,crossing);
            Require(passage==2,"Actual middeck observation rejected");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,new float3(680,0,385),crossing)==0,"Switching to the other route preserved incomplete proof");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,new float3(640,0,505),crossing)==0,"Leaving the assigned bridge corridor preserved incomplete proof");
            passage=CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,new float3(710,0,505),crossing);
            Require(passage==3,"Complete west/deck/east crossing rejected");
            Require(CampaignMissionTrustUnderFireRuleUtility.AdvancePassage(passage,new float3(930,0,505),crossing)==3,"Completed transit lost at shelter");
            ValidateBroadcastOwnership(); ValidateInitialization(runtime); ValidateResultSettlement(runtime);
            Debug.Log("[TrustRules] result=Passed originals=26 routes=independent crossing=required verification=actual settlement=idempotent supply=first-clear");
        }
        private static void ValidateBroadcastOwnership()
        {
            using var world=new World("Trust real neutral broadcast grant binding");var em=world.EntityManager;
            var broadcast=em.CreateEntity(typeof(RuntimeBuildingCombatInfo),typeof(UnitSourcePrefabKey),typeof(UnitHealth),typeof(Faction));
            var origin=new int2(128,324);var size=new int2(14,12);
            var request=new BuildingRuntimeSpawnRequest { Status=BuildingRuntimeSpawnRequest.Succeeded,BuildingRuntimeId=3,HasOwnerFaction=1,FactionId=0,
                BuildingId="Building_Trust_Broadcast",ActualOrigin=origin,ActualFootprint=size };
            em.SetComponentData(broadcast,new RuntimeBuildingCombatInfo { RuntimeBuildingId=3,OwnerFactionId=0,OriginCell=origin,FootprintCells=size });
            em.SetComponentData(broadcast,new UnitSourcePrefabKey { Value="Building_Trust_Broadcast" });
            em.SetComponentData(broadcast,new UnitHealth { Current=1000,Max=1000 });em.SetComponentData(broadcast,new Faction { Id=0 });
            Require(CampaignMissionRuntimeSystem.IsTrustRequestedBuilding(em,broadcast,in request),"Actual neutral broadcast grant failed strict binding");
            Require(!FactionIdentity.CanAutoTargetForCombat(2,em.GetComponentData<Faction>(broadcast).Id),"Relay guards auto-target their protected neutral transmitter");
            Require(FactionIdentity.CanAutoTargetForCombat(2,1),"Neutral preservation weakened military targeting of player escorts");
            var hostileOwner=em.GetComponentData<RuntimeBuildingCombatInfo>(broadcast);hostileOwner.OwnerFactionId=1;em.SetComponentData(broadcast,hostileOwner);
            Require(!CampaignMissionRuntimeSystem.IsTrustRequestedBuilding(em,broadcast,in request),"Wrong player-owned building substituted for neutral civic broadcast");
            Require(em.GetComponentData<UnitHealth>(broadcast).Current==1000,"Ownership qualification changed real health");
        }
        private static void ValidateInitialization(CampaignMissionRuntimeComponent runtime)
        {
            using var world=new World("Trust original roster initialization");var em=world.EntityManager;
            var root=em.CreateEntity();
            var groups=new (string role,string group,int count)[] {
                ("role.friendly.escort.north","escort.north",5),("role.friendly.escort.south","escort.south",5),
                ("role.friendly.convoy.north","convoy.north",1),("role.friendly.convoy.south","convoy.south",1),("role.friendly.engineer","engineer",1),
                ("role.hostile.north","hostile.north",3),("role.hostile.south","hostile.south",3),("role.hostile.relay","hostile.relay",3),
                ("role.civilian.protected","staff.north",2),("role.civilian.protected","staff.south",2) };
            foreach(var group in groups)for(int i=0;i<group.count;i++)
            {
                var entity=em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));
                em.SetComponentData(entity,new CampaignMissionUnitRoleComponent { SessionToken=runtime.SessionToken,
                    MissionRoleId=new FixedString64Bytes(group.role),UnitGroupId=new FixedString64Bytes("group.ch05.m02."+group.group) });
                em.SetComponentData(entity,new UnitHealth { Current=100,Max=100 });
            }
            using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();
            var names=new[]{"north_crossing","south_crossing","north_arrival","south_arrival","relay_gate","relay_approach"};
            var anchors=builder.Allocate(ref map.Anchors,names.Length);
            for(int i=0;i<names.Length;i++)anchors[i]=new OperationMapAnchorBlob { Id=new FixedString64Bytes("anchor.ch05.m02."+names[i]),Radius=12 };
            using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
            CampaignMissionRuntimeSystem.InitializeTrustUnderFire(em,root,in runtime,ref blob.Value);
            var initialized=em.GetComponentData<CampaignMissionTrustUnderFireState>(root);
            Require(initialized.Initialized!=0&&initialized.Failure==TrustUnderFireFailure.None,"Actual complete roster initialization failed");
            Require(em.GetBuffer<CampaignMissionTrustUnderFireMember>(root).Length==26&&em.GetBuffer<CampaignMissionTrustBuildingRequest>(root).Length==4,"Initialization lost original actors or actual building requests");
            using var staff=em.CreateEntityQuery(typeof(CampaignMissionStationaryUnitTag));
            Require(staff.CalculateEntityCount()==13,"Protected staff qualification did not apply structural tags");
        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M02TrustUnderFireConfigBuilder.MissionPath);
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
            using var world=new World("Trust Under Fire actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=9,HostileDefeatedCount=9,
                CivilianTotalCount=4,ElapsedMilliseconds=300000,TrustNorthArrived=1,TrustSouthArrived=1,TrustRelayVerified=1,
                TrustNorthCrossed=1,TrustSouthCrossed=1,TrustMilitaryCleared=1,TrustStableMilliseconds=6000 };
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Trust victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.TrustSouthCrossed=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed real south crossing");
            incomplete=facts;incomplete.TrustStableMilliseconds=5999;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed protected route stabilization hold");
            incomplete=facts;incomplete.HostileDefeatedCount=8;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.TrustFailure=TrustUnderFireFailure.StaffLost;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original civic staff loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"trust-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Trust first-clear settlement rejected");
                var earned=save.LoadProfile();
                Require(Array.IndexOf(earned.ownedSupportAbilityUnlocks,"ability.supply_drop")>=0,"First clear did not grant future Supply ownership");
                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Trust rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();
                Require(Array.FindAll(duplicate.ownedSupportAbilityUnlocks,x=>x=="ability.supply_drop").Length==1,"Duplicate settlement duplicated Supply ownership");
                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static void Require(bool condition,string error) { if(!condition)throw new InvalidOperationException(error); }
    }
}
