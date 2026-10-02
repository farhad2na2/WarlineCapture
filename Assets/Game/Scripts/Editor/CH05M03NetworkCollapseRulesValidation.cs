using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M03NetworkCollapseRulesValidation
    {
        public static void Run()
        {
            var runtime=new CampaignMissionRuntimeComponent { MissionId=CampaignMissionSequence.NetworkCollapse,ScenarioId="scenario.ch05.m03.network_collapse",OperationMapId="opmap.ch05.network_collapse",SessionToken="network-rules",AttemptOrdinal=1,SourceVersion=3,Version=1,Phase=MissionPhaseKind.Engage,DeterministicSeed=1 };
            var mission=new CampaignMissionNetworkCollapseState {SessionToken=runtime.SessionToken,AttemptOrdinal=1,SourceVersion=3,Initialized=1,Ready=1,NodeOneVerified=1,NodeTwoVerified=1,NodeThreeVerified=1,NodeOneDisabled=1,NodeTwoDisabled=1,NodeThreeDisabled=1,AuditRecovered=1,EngineerAboard=1,Extracted=1,MilitaryCleared=1,ExtractionMilliseconds=6000};
            var facts=new CampaignMissionAttemptFactsComponent {HostileTotalCount=9,HostileDefeatedCount=9,CivilianTotalCount=4};
            Require(CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in mission,true,out var victory)&&victory.Outcome==MissionOutcomeKind.Victory,"Qualified complete audit extraction rejected");
            Require(!CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in victory,in facts,in mission,true,out _),"Terminal outcome repeated");
            var incomplete=mission;incomplete.NodeTwoVerified=0;Require(!CampaignMissionNetworkCollapseRuleUtility.VictoryQualified(in incomplete,in facts),"Unverified destruction substituted for node evidence");
            incomplete=mission;incomplete.EngineerAboard=0;Require(!CampaignMissionNetworkCollapseRuleUtility.VictoryQualified(in incomplete,in facts),"Carrier alone substituted for original evidence team");
            incomplete=mission;incomplete.ExtractionMilliseconds=5999;Require(!CampaignMissionNetworkCollapseRuleUtility.VictoryQualified(in incomplete,in facts),"Incomplete extraction hold accepted");
            incomplete=mission;incomplete.NodeOneDisabled=0;Require(!CampaignMissionNetworkCollapseRuleUtility.CanVerify(1,in incomplete,0),"Second verification bypassed first node disable");
            Require(!CampaignMissionNetworkCollapseRuleUtility.CanVerify(2,in incomplete,1),"Live guard ignored during recon");
            foreach(NetworkCollapseFailure failure in Enum.GetValues(typeof(NetworkCollapseFailure)))
            {if(failure==NetworkCollapseFailure.None)continue;var failed=mission;failed.Failure=failure;Require(CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in failed,true,out var defeat)&&defeat.Outcome==MissionOutcomeKind.Defeat,"Failure lost priority: "+failure);}
            incomplete=mission;incomplete.AttemptOrdinal--;Require(!CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in incomplete,true,out _),"Stale retry settled");incomplete=mission;incomplete.SessionToken="old";Require(!CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in incomplete,true,out _),"Wrong session settled");incomplete=mission;incomplete.SourceVersion--;Require(!CampaignMissionNetworkCollapseRuleUtility.TryAdvance(in runtime,in facts,in incomplete,true,out _),"Stale source settled");
            Require(CampaignMissionNetworkCollapseRuleUtility.AdvanceHold(5000,1000,false)==0,"Moving/off-zone verification did not reset");
            ValidateAuthoredSpawnRestrictions();ValidateOriginalWorld(runtime);ValidateResultSettlement(runtime);
            Debug.Log("[NetworkCollapseRules] result=Passed originalRoster=21 verification=ordered targetSuppression=actual audit=preserved occupancy=original settlement=idempotent");
        }
        private static void ValidateAuthoredSpawnRestrictions()
        {
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M03NetworkCollapseConfigBuilder.MissionPath);
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(CH05M03NetworkCollapseConfigBuilder.ScenarioPath);
            Require(mission != null && scenario != null, "Authored mission/scenario missing for spawn gate");
            var restrictions = scenario.Restrictions;
            var definition = new CampaignMissionDefinitionBlob
            {
                MissionId = mission.MissionId,
                MissionRuntimeEnabled = (byte)(scenario.MissionRuntime.Enabled ? 1 : 0),
                BuildingDisabled = (byte)(restrictions.BuildingDisabled ? 1 : 0),
                ProductionDisabled = (byte)(restrictions.ProductionDisabled ? 1 : 0),
                EconomyDisabled = (byte)(restrictions.EconomyDisabled ? 1 : 0),
                TransportDisabled = (byte)(restrictions.TransportDisabled ? 1 : 0),
                AirDisabled = (byte)(restrictions.AirDisabled ? 1 : 0)
            };
            definition.Defense.Enabled = (byte)(scenario.Defense.Enabled ? 1 : 0);
            Require(definition.TransportDisabled == 0 && definition.MissionRuntimeEnabled == 0 &&
                CampaignMissionSpawnSystem.HasRequiredRestrictions(ref definition),
                "Actual authored transport-enabled ground raid rejected by spawn gate");
            definition.TransportDisabled = 1;
            Require(!CampaignMissionSpawnSystem.HasRequiredRestrictions(ref definition), "Extraction without transport was accepted");
            definition.TransportDisabled = 0;
            definition.MissionId = CampaignMissionSequence.IdAt(0);
            Require(!CampaignMissionSpawnSystem.HasRequiredRestrictions(ref definition), "Network exception leaked into ordinary mission restrictions");
        }
        private static void ValidateOriginalWorld(CampaignMissionRuntimeComponent runtime)
        {
            using var world=new World("Network actual original actors and target policy");var em=world.EntityManager;var root=em.CreateEntity();
            var groups=new (string role,int count)[]{("role.friendly.escort",6),("role.friendly.engineer",1),("role.friendly.extraction_apc",1),("role.hostile.node.1",1),("role.hostile.node.2",1),("role.hostile.node.3",1),("role.hostile.guard.1",2),("role.hostile.guard.2",2),("role.hostile.guard.3",2),("role.civilian.protected",4)};
            foreach(var group in groups)for(int i=0;i<group.count;i++){var e=em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));em.SetComponentData(e,new CampaignMissionUnitRoleComponent {SessionToken=runtime.SessionToken,MissionRoleId=new FixedString64Bytes(group.role)});em.SetComponentData(e,new UnitHealth {Current=100,Max=100});}
            using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();var names=new[]{"recon_1","recon_2","recon_3","approach_1","approach_2","approach_3","audit_gate","extraction"};var anchors=builder.Allocate(ref map.Anchors,names.Length);for(int i=0;i<names.Length;i++)anchors[i]=new OperationMapAnchorBlob {Id=new FixedString64Bytes("anchor.ch05.m03."+names[i]),Position=new float3(i*20,0,0),Radius=6};using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
            CampaignMissionRuntimeSystem.InitializeNetworkCollapse(em,root,in runtime,ref blob.Value);var mission=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);Require(mission.Failure==NetworkCollapseFailure.None&&em.GetBuffer<CampaignMissionNetworkCollapseMember>(root).Length==21,"Actual original initialization failed");
            var attackGuard=typeof(UnitAttackSystem).GetMethod("IsTargetAvailableForCombat",BindingFlags.NonPublic|BindingFlags.Static);Require(attackGuard!=null,"Actual combat target gate missing");
            Entity node=Entity.Null;foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root))if(member.Kind==NetworkCollapseMemberKind.NodeOne)node=member.Entity;
            Require(!(bool)attackGuard.Invoke(null,new object[]{em,mission.Carrier,node}),"Unverified node accepted by actual ordinary combat target gate");em.RemoveComponent<CampaignMissionCombatSuppressedTag>(node);Require((bool)attackGuard.Invoke(null,new object[]{em,mission.Carrier,node}),"Verified real node remains untargetable");
            em.AddBuffer<UnitTransportPassengerElement>(mission.Carrier);em.AddComponentData(mission.Engineer,new UnitTransportPassenger {Transport=mission.Carrier});
            Require(!CampaignMissionRuntimeSystem.NetworkEngineerAboard(em,in mission),"Orphan passenger component substituted for real carrier manifest");var stranger=em.CreateEntity();em.GetBuffer<UnitTransportPassengerElement>(mission.Carrier).Add(new UnitTransportPassengerElement {Passenger=stranger});Require(!CampaignMissionRuntimeSystem.NetworkEngineerAboard(em,in mission),"Wrong passenger substituted for original engineer");em.GetBuffer<UnitTransportPassengerElement>(mission.Carrier).Add(new UnitTransportPassengerElement {Passenger=mission.Engineer});Require(CampaignMissionRuntimeSystem.NetworkEngineerAboard(em,in mission),"Actual original occupancy rejected");em.SetComponentData(mission.Engineer,new UnitHealth {Current=0,Max=100});Require(!CampaignMissionRuntimeSystem.NetworkEngineerAboard(em,in mission),"Dead evidence team extracted");
        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M03NetworkCollapseConfigBuilder.MissionPath);
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
            using var world=new World("Network Collapse actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=9,HostileDefeatedCount=9,
                CivilianTotalCount=4,ElapsedMilliseconds=300000,NetworkNodesVerified=3,NetworkNodesDisabled=3,NetworkAuditRecovered=1,NetworkEngineerAboard=1,NetworkExtracted=1 };
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Network victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.NetworkNodesVerified=2;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed ordered verification");
            incomplete=facts;incomplete.NetworkAuditRecovered=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed actual complete audit recovery");
            incomplete=facts;incomplete.HostileDefeatedCount=8;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.NetworkFailure=NetworkCollapseFailure.StaffLost;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original civic staff loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"network-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Network first-clear settlement rejected");
                var earned=save.LoadProfile();

                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Network Collapse rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();

                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static void Require(bool condition,string error) { if(!condition)throw new InvalidOperationException(error); }
    }
}
