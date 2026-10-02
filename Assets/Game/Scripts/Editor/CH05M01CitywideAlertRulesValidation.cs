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
    public static class CH05M01CitywideAlertRulesValidation
    {
        public static void Run()
        {
            var runtime=new CampaignMissionRuntimeComponent { MissionId=CampaignMissionSequence.CitywideAlert,ScenarioId="scenario.ch05.m01.citywide_alert",OperationMapId="opmap.ch05.citywide_alert",SessionToken="citywide-rules",AttemptOrdinal=1,SourceVersion=3,Version=1,DeterministicSeed=1,Phase=MissionPhaseKind.Engage };
            var mission=new CampaignMissionCitywideAlertState { SessionToken=runtime.SessionToken,AttemptOrdinal=1,SourceVersion=3,Initialized=1,Ready=1,
                CoverageReady=1,ClinicRecovered=1,UtilityRecovered=1,ClinicThreatsCleared=1,UtilityThreatsCleared=1,AirCleared=1,
                ReinforcementProduced=1,ReinforcementReady=1,MilitaryCleared=1,StableMilliseconds=6000 };
            var facts=new CampaignMissionAttemptFactsComponent { HostileTotalCount=11,HostileDefeatedCount=11,CivilianTotalCount=4 };
            Require(CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in mission,true,out var victory)&&victory.Outcome==MissionOutcomeKind.Victory,"Qualified two-service victory rejected");
            Require(!CampaignMissionCitywideAlertRuleUtility.TryAdvance(in victory,in facts,in mission,true,out _),"Terminal outcome repeated");
            var invalidRuntime=runtime;invalidRuntime.DeterministicSeed=0;Require(!CampaignMissionCitywideAlertRuleUtility.TryAdvance(in invalidRuntime,in facts,in mission,true,out _),"Invalid launch provenance settled");
            var incomplete=mission;incomplete.ReinforcementReady=0;Require(!CampaignMissionCitywideAlertRuleUtility.VictoryQualified(in incomplete,in facts),"Supplied original substituted for actual reinforcement");
            incomplete=mission;incomplete.UtilityRecovered=0;Require(!CampaignMissionCitywideAlertRuleUtility.VictoryQualified(in incomplete,in facts),"Clinic substituted for second service");
            var surviving=facts;surviving.HostileDefeatedCount=10;Require(!CampaignMissionCitywideAlertRuleUtility.VictoryQualified(in mission,in surviving),"Original hostile survivor ignored");
            foreach (CitywideAlertFailure failure in Enum.GetValues(typeof(CitywideAlertFailure)))
            { if (failure==CitywideAlertFailure.None) continue;var failed=mission;failed.Failure=failure;Require(CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in failed,true,out var defeat)&&defeat.Outcome==MissionOutcomeKind.Defeat,"Failure lost precedence: "+failure); }
            var stale=mission;stale.AttemptOrdinal--;Require(!CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Prior retry settled");
            stale=mission;stale.SourceVersion--;Require(!CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Stale source settled");
            stale=mission;stale.SessionToken="old";Require(!CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in stale,true,out _),"Stale session settled");
            Require(CampaignMissionCitywideAlertRuleUtility.AdvanceHold(5000,1000,false)==0,"Contested recovery did not reset");
            Require(CampaignMissionCitywideAlertRuleUtility.AdvanceHold(5000,int.MaxValue,true)==6000,"Recovery hold overflowed");
            ValidateInitialization(runtime);ValidateStartingLots(runtime);ValidateGrantIdentity();ValidateProducerReadiness(runtime,mission);ValidateMaterialsPolicy(runtime);ValidateResponseFallback();ValidateEngineerRecovery();ValidateProductionAndFuel(runtime,mission);ValidateConsent();ValidateResultSettlement(runtime);
            Debug.Log("[CitywideRules] result=Passed services=independent originals=11 production=actual-producer fuel=two-reserves consent=bounded settlement=idempotent");
        }
        private static void ValidateInitialization(CampaignMissionRuntimeComponent runtime)
        {
            using var world=new World("Citywide original roster initialization");var em=world.EntityManager;
            var root=em.CreateEntity();
            var groups=new (string role,string group,int count)[] {
                ("role.friendly.response.clinic","response.clinic",4),("role.friendly.response.utility","response.utility",4),
                ("role.friendly.engineer.clinic","engineer.clinic",1),("role.friendly.engineer.utility","engineer.utility",1),
                ("role.friendly.g2a","g2a",1),("role.friendly.radar","radar",1),
                ("role.hostile.clinic","hostile.clinic",5),("role.hostile.utility","hostile.utility",4),("role.hostile.air","hostile.air",2),
                ("role.civilian.protected","staff.clinic",2),("role.civilian.protected","staff.utility",2) };
            foreach(var group in groups)for(int i=0;i<group.count;i++)
            {
                var entity=em.CreateEntity(typeof(CampaignMissionUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));
                em.SetComponentData(entity,new CampaignMissionUnitRoleComponent { SessionToken=runtime.SessionToken,
                    MissionRoleId=new FixedString64Bytes(group.role),UnitGroupId=new FixedString64Bytes("group.ch05.m01."+group.group) });
                em.SetComponentData(entity,new UnitHealth { Current=100,Max=100 });
            }
            using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();
            var names=new[]{"clinic","utility","clinic_defense","utility_defense","coverage","reinforcement_perimeter","clinic_recovery","utility_recovery"};
            var anchors=builder.Allocate(ref map.Anchors,names.Length);
            for(int i=0;i<names.Length;i++)anchors[i]=new OperationMapAnchorBlob { Id=new FixedString64Bytes("anchor.ch05.m01."+names[i]),Radius=12 };
            using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
            CampaignMissionRuntimeSystem.InitializeCitywideAlert(em,root,in runtime,ref blob.Value);
            var initialized=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
            Require(initialized.Initialized!=0&&initialized.Failure==CitywideAlertFailure.None,"Actual complete roster initialization failed");
            Require(em.GetBuffer<CampaignMissionCitywideAlertMember>(root).Length==27&&em.GetBuffer<CampaignMissionCitywideBuildingRequest>(root).Length==6,"Initialization lost original actors or actual building requests");
            using var staff=em.CreateEntityQuery(typeof(CampaignMissionStationaryUnitTag));
            Require(staff.CalculateEntityCount()==4,"Protected staff qualification did not apply structural tags");
        }
        private static void ValidateStartingLots(CampaignMissionRuntimeComponent runtime)
        {
            using var world=new World("Citywide six real starting lots");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(BuildingStartingGrantOwner));em.SetComponentData(root,runtime);
            using var builder=new BlobBuilder(Allocator.Temp);ref var map=ref builder.ConstructRoot<OperationMapBlob>();map.OperationMapId=runtime.OperationMapId;
            map.Grid.Origin=new float3(860,0,300);map.Grid.CellSize=1;
            var names=new[]{"clinic_service","utility_service","producer_clinic","producer_utility","reserve_clinic","reserve_utility"};
            var origins=new[]{new Vector2Int(39,144),new Vector2Int(336,142),new Vector2Int(146,30),new Vector2Int(306,30),new Vector2Int(200,52),new Vector2Int(360,52)};
            var sizes=new[]{new Vector2Int(14,12),new Vector2Int(20,16),new Vector2Int(28,15),new Vector2Int(28,15),new Vector2Int(26,25),new Vector2Int(26,25)};
            var anchors=builder.Allocate(ref map.Anchors,6);
            for(int i=0;i<6;i++)anchors[i]=new OperationMapAnchorBlob { Id=new FixedString64Bytes("anchor.ch05.m01."+names[i]),Kind=OperationMapAnchorKind.Build,Position=new float3(origins[i].x+860,0,origins[i].y+300) };
            using var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Temp);
            var entity=em.CreateEntity(typeof(ActiveOperationMapComponent),typeof(OperationMapMetadataComponent));
            em.SetComponentData(entity,new ActiveOperationMapComponent { MissionId=runtime.MissionId,ScenarioId=runtime.ScenarioId,OperationMapId=runtime.OperationMapId,Generation=1 });
            em.SetComponentData(entity,new OperationMapMetadataComponent { Blob=blob,Generation=1 });
            using var maps=em.CreateEntityQuery(typeof(ActiveOperationMapComponent),typeof(OperationMapMetadataComponent));
            for(int i=0;i<6;i++)
            {
                var prefab=new GameObject(i==0?"Building_Citywide_Clinic":i==1?"Building_Citywide_Utility":i<4?"Building_Barrack":"Building_Citywide_ReserveDepot");
                try
                {
                    var definition=new BuildingDefinition { Prefab=prefab,FootprintCells=sizes[i] };var placement=new RectInt(origins[i],sizes[i]);
                    Require(CampaignMissionBuildingPlacementPolicy.IsAllowedCitywideStartingLot(em,maps,in runtime,definition,placement),"Authored Citywide starting lot rejected: "+names[i]);
                    placement.position+=Vector2Int.one;Require(!CampaignMissionBuildingPlacementPolicy.IsAllowedCitywideStartingLot(em,maps,in runtime,definition,placement),"Unauthored Citywide lot accepted");
                    placement=new RectInt(origins[i],Vector2Int.one);Require(!CampaignMissionBuildingPlacementPolicy.IsAllowedCitywideStartingLot(em,maps,in runtime,definition,placement),"Shrunken service reservation accepted");
                    var stale=runtime;stale.AttemptOrdinal++;Require(!CampaignMissionBuildingPlacementPolicy.IsAllowedCitywideStartingLot(em,maps,in stale,definition,new RectInt(origins[i],sizes[i])),"Old grant owner accepted another attempt");
                }
                finally { UnityEngine.Object.DestroyImmediate(prefab); }
            }
        }
        private static void ValidateGrantIdentity()
        {
            using var world=new World("Citywide prepared and supplied building ID collision");var em=world.EntityManager;
            var request=new BuildingRuntimeSpawnRequest { Status=BuildingRuntimeSpawnRequest.Succeeded,BuildingRuntimeId=2,
                BuildingId="Building_Citywide_Utility",ActualOrigin=new int2(336,142),ActualFootprint=new int2(20,16) };
            var supplied=em.CreateEntity(typeof(RuntimeBuildingCombatInfo),typeof(UnitSourcePrefabKey),typeof(UnitHealth));
            em.SetComponentData(supplied,new RuntimeBuildingCombatInfo { RuntimeBuildingId=2,OwnerFactionId=1,OriginCell=request.ActualOrigin,FootprintCells=request.ActualFootprint });
            em.SetComponentData(supplied,new UnitSourcePrefabKey { Value="Building_Citywide_Utility" });em.SetComponentData(supplied,new UnitHealth { Current=700,Max=700 });
            var authored=em.CreateEntity(typeof(RuntimeBuildingCombatInfo),typeof(UnitSourcePrefabKey),typeof(UnitHealth),typeof(OperationMapBuildingComponent));
            em.SetComponentData(authored,em.GetComponentData<RuntimeBuildingCombatInfo>(supplied));em.SetComponentData(authored,em.GetComponentData<UnitSourcePrefabKey>(supplied));
            em.SetComponentData(authored,new UnitHealth { Current=900,Max=900 });
            Require(CampaignMissionRuntimeSystem.IsCitywideRequestedBuilding(em,supplied,in request),"Succeeded supplied service identity rejected");
            Require(!CampaignMissionRuntimeSystem.IsCitywideRequestedBuilding(em,authored,in request),"Prepared map owner with colliding runtime ID bound as supplied service");
            var displaced=em.GetComponentData<RuntimeBuildingCombatInfo>(supplied);displaced.OriginCell+=new int2(1,0);em.SetComponentData(supplied,displaced);
            Require(!CampaignMissionRuntimeSystem.IsCitywideRequestedBuilding(em,supplied,in request),"Same ID at wrong actual origin bound");
        }
        private static void ValidateProducerReadiness(CampaignMissionRuntimeComponent runtime,CampaignMissionCitywideAlertState mission)
        {
            using var world=new World("Citywide real supplied producer catalog readiness");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionDefenseStateComponent));
            em.SetComponentData(root,new CampaignMissionDefenseStateComponent { SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion });
            mission.ClinicProducer=em.CreateEntity(typeof(UnitHealth),typeof(RuntimeBuildingCombatInfo),typeof(UnitSourcePrefabKey));mission.ClinicProducerRuntimeId=3;
            em.SetComponentData(mission.ClinicProducer,new UnitHealth { Current=500,Max=500 });
            em.SetComponentData(mission.ClinicProducer,new RuntimeBuildingCombatInfo { RuntimeBuildingId=3,OwnerFactionId=1 });
            em.SetComponentData(mission.ClinicProducer,new UnitSourcePrefabKey { Value="Building_Barrack" });
            CampaignMissionRuntimeSystem.ProjectCitywideProducerReadiness(em,root,in runtime,in mission);
            Require(em.GetComponentData<CampaignMissionDefenseStateComponent>(root).InitialProducerReady==1,"Actual bound Barracks did not expose ordinary Soldiers catalog");
            em.SetComponentData(mission.ClinicProducer,new UnitHealth { Current=0,Max=500 });
            CampaignMissionRuntimeSystem.ProjectCitywideProducerReadiness(em,root,in runtime,in mission);
            Require(em.GetComponentData<CampaignMissionDefenseStateComponent>(root).InitialProducerReady==0,"Lost producer retained recruitment readiness");
            em.SetComponentData(mission.ClinicProducer,new UnitHealth { Current=500,Max=500 });
            var stale=mission;stale.AttemptOrdinal++;
            CampaignMissionRuntimeSystem.ProjectCitywideProducerReadiness(em,root,in runtime,in stale);
            Require(em.GetComponentData<CampaignMissionDefenseStateComponent>(root).InitialProducerReady==0,"Stale attempt exposed recruitment catalog");
            var info=em.GetComponentData<RuntimeBuildingCombatInfo>(mission.ClinicProducer);info.OwnerFactionId=2;em.SetComponentData(mission.ClinicProducer,info);
            CampaignMissionRuntimeSystem.ProjectCitywideProducerReadiness(em,root,in runtime,in mission);
            Require(em.GetComponentData<CampaignMissionDefenseStateComponent>(root).InitialProducerReady==0,"Hostile producer exposed friendly recruitment");
        }
        private static void ValidateMaterialsPolicy(CampaignMissionRuntimeComponent runtime)
        {
            byte policy=MissionConstructionCostPolicy.ForMission(CampaignMissionSequence.CitywideAlert);
            Require(policy==MissionConstructionCostPolicy.DefenseMaterialsV1&&MissionConstructionCostPolicy.TryResolve(policy,10000,20,out int credits,out int materials)&&credits==0&&materials==30,"Canonical rifle recipe did not use authored Citywide Materials price");
            Require(!MissionConstructionCostPolicy.TryResolve(policy,11000,20,out _,out _),"Unconfigured rifle Credit price silently waived");
            using var world=new World("Citywide actual attempt Materials initialization");var em=world.EntityManager;
            using var builder=new BlobBuilder(Allocator.Temp);ref var catalog=ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            var definitions=builder.Allocate(ref catalog.Missions,1);definitions[0]=new CampaignMissionDefinitionBlob { MissionId=runtime.MissionId,ScenarioId=runtime.ScenarioId,OperationMapId=runtime.OperationMapId,MissionRuntimeEnabled=1,StartingMaterials=180,StartingCredits=0 };
            using var blob=builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Temp);
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionAttemptResourceInitializationComponent),typeof(CampaignMissionAttemptFactsComponent));
            em.SetComponentData(root,runtime);em.SetComponentData(root,new CampaignMissionCatalogComponent { Blob=blob,SourceVersion=runtime.SourceVersion });em.SetComponentData(root,new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1 });
            var gameplay=em.CreateEntity(typeof(RuntimeGameplayStateComponent));em.SetComponentData(gameplay,new RuntimeGameplayStateComponent { PlayRequested=1 });
            var resources=em.CreateEntity(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
            em.SetComponentData(resources,new FactionEconomy { FactionId=1,Money=1000000,Oil=100,Fuel=100 });em.SetComponentData(resources,new FactionTacticalMaterialsComponent { FactionId=1,Current=999,Capacity=1000,Version=1 });
            var initializer=world.GetOrCreateSystem<CampaignMissionAttemptResourceInitializationSystem>();
            world.Unmanaged.GetUnsafeSystemRef<CampaignMissionAttemptResourceInitializationSystem>(initializer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(initializer));
            var economy=em.GetComponentData<FactionEconomy>(resources);var budget=em.GetComponentData<FactionTacticalMaterialsComponent>(resources);
            Require(economy.MaterialsOnlyConstruction==policy&&economy.Money==0&&economy.Oil==0&&economy.Fuel==0&&budget.Current==180&&budget.Capacity==180,"Actual initialization diverged from player recipe policy or inherited resource budget");
            Require(em.GetComponentData<CampaignMissionAttemptResourceInitializationComponent>(root).Applied!=0,"Attempt budget was not applied");
        }
        private static void ValidateResponseFallback()
        {
            using var world=new World("Citywide surviving response group and actual selection");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionCitywideAlertState));em.AddBuffer<CampaignMissionCitywideAlertMember>(root);
            Entity Actor(CitywideAlertMemberKind kind,float x,bool live)
            {
                var entity=em.CreateEntity(typeof(UnitHealth),typeof(UnitCombat),typeof(UnitAttack),typeof(LocalTransform));
                em.SetComponentData(entity,new UnitHealth { Current=live?100:0,Max=100 });em.SetComponentData(entity,new UnitCombat { CanAttack=1 });em.SetComponentData(entity,new UnitAttack { Range=80,Damage=30 });em.SetComponentData(entity,LocalTransform.FromPosition(x,0,426));
                em.GetBuffer<CampaignMissionCitywideAlertMember>(root).Add(new CampaignMissionCitywideAlertMember { Entity=entity,Kind=kind });return entity;
            }
            var clinic=Actor(CitywideAlertMemberKind.ClinicResponse,920,false);var utility=Actor(CitywideAlertMemberKind.UtilityResponse,1190,true);var second=Actor(CitywideAlertMemberKind.UtilityResponse,1198,true);
            Require(CampaignMissionGuidanceProjectionSystem.ResolveCitywideResponseActor(em,root,CitywideAlertMemberKind.ClinicResponse)==utility,"Lost clinic force did not use actual armed utility survivors");
            var guidance=new CampaignMissionGuidanceProjectionComponent { GuidanceId=68003,SourceEntity=utility,RecommendationKind=AssistantRecommendationKind.Attack };
            object[] args={em,root,guidance,null};var resolver=typeof(UiShellEcsGateway).GetMethod("ResolveCitywideAlertTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Require(resolver!=null&&(bool)resolver.Invoke(null,args),"Fallback response target unavailable");var target=(UiMissionTutorialTarget)args[3];
            Require(target.DragSelection&&target.RequiredSelectionCount==2&&target.SelectionMin.x>1100,"Fallback selection still targets the dead clinic force");
            em.SetComponentData(clinic,new UnitHealth { Current=100,Max=100 });Require(CampaignMissionGuidanceProjectionSystem.ResolveCitywideResponseActor(em,root,CitywideAlertMemberKind.ClinicResponse)==clinic,"Alternate force superseded surviving preferred response");
            em.SetComponentData(clinic,new UnitCombat { CanAttack=0 });em.SetComponentData(utility,new UnitHealth { Current=0,Max=100 });em.SetComponentData(second,new UnitHealth { Current=0,Max=100 });
            Require(CampaignMissionGuidanceProjectionSystem.ResolveCitywideResponseActor(em,root,CitywideAlertMemberKind.ClinicResponse)==Entity.Null,"Unarmed or dead response actor recommended for combat");
        }
        private static void ValidateEngineerRecovery()
        {
            using var world=new World("Citywide original engineer safety and required relocation");var em=world.EntityManager;
            var engineer=em.CreateEntity(typeof(UnitHealth),typeof(UnitCombat),typeof(ArmorBreakUnitActivationState),typeof(LocalTransform));
            em.SetComponentData(engineer,new UnitHealth { Current=125,Max=125 });em.SetComponentData(engineer,new UnitCombat { CanAttack=1,AutoEngage=1 });
            em.SetComponentData(engineer,new ArmorBreakUnitActivationState { OriginalAutoEngage=1 });em.SetComponentData(engineer,LocalTransform.FromPosition(970,0,426));
            CampaignMissionRuntimeSystem.QualifyCitywideEngineer(em,engineer);
            var combat=em.GetComponentData<UnitCombat>(engineer);var health=em.GetComponentData<UnitHealth>(engineer);
            Require(combat.CanAttack==0&&combat.AutoEngage==0&&em.GetComponentData<ArmorBreakUnitActivationState>(engineer).OriginalAutoEngage==0,"Engineer auto-fire restored after briefing activation");
            Require(health.Current==125&&health.Max==125&&!em.HasComponent<Disabled>(engineer)&&!em.HasComponent<CampaignMissionStationaryUnitTag>(engineer),"Engineer safety changed health or disabled ordinary movement");
            var recovery=new float3(906,0,438);
            Require(!CampaignMissionRuntimeSystem.CitywideStoppedAt(em,engineer,recovery,6),"Rear staging qualified as completed recovery");
            em.SetComponentData(engineer,LocalTransform.FromPosition(recovery));
            Require(CampaignMissionRuntimeSystem.CitywideStoppedAt(em,engineer,recovery,6),"Living dismounted engineer at actual recovery site rejected");
            em.AddComponent<UnitPathRequest>(engineer);
            Require(!CampaignMissionRuntimeSystem.CitywideStoppedAt(em,engineer,recovery,6),"Moving engineer qualified for stationary recovery");
        }
        private static void ValidateProductionAndFuel(CampaignMissionRuntimeComponent runtime,CampaignMissionCitywideAlertState mission)
        {
            using var world=new World("Citywide producer and finite reserves");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionCitywideAlertState),typeof(SupportFuelScopeComponent));
            em.SetComponentData(root,runtime);
            Entity Storage(float stock,float floor)
            { var entity=em.CreateEntity(typeof(BuildingResourceStorageComponent),typeof(UnitHealth));em.SetComponentData(entity,new UnitHealth { Max=500,Current=500 });em.SetComponentData(entity,new BuildingResourceStorageComponent { OwnerFactionId=1,FuelStorageCapacity=240,StoredFuelBarrels=stock,CivilianFuelReserveBarrels=floor });return entity; }
            var unrelated=Storage(1000,0);mission.ClinicReserve=Storage(100,20);mission.UtilityReserve=Storage(100,20);
            mission.ClinicProducerRuntimeId=11;mission.UtilityProducerRuntimeId=12;em.SetComponentData(root,mission);
            em.SetComponentData(root,new SupportFuelScopeComponent { Required=1,Storage=mission.ClinicReserve });
            Require(SupportFuelTransactionSystem.Available(em,1)==160,"Inherited stock leaked into two-service fuel budget");
            Require(!SupportFuelTransactionSystem.TryConsumeImmediate(em,1,161),"Optional consent could consume civic floors");
            Require(SupportFuelTransactionSystem.TryConsumeImmediate(em,1,100)&&SupportFuelTransactionSystem.Available(em,1)==60,"Atomic Support debit did not span both actual reserves");
            CampaignCitywideFuelScope.Drain(em,1000);
            Require(em.GetComponentData<BuildingResourceStorageComponent>(mission.ClinicReserve).StoredFuelBarrels==20&&em.GetComponentData<BuildingResourceStorageComponent>(mission.UtilityReserve).StoredFuelBarrels==20,"Vehicle consumption breached one district floor");
            Require(em.GetComponentData<BuildingResourceStorageComponent>(unrelated).StoredFuelBarrels==1000,"Unrelated map stock was debited");
            var recruit=em.CreateEntity(typeof(Faction),typeof(UnitHealth),typeof(UnitSourcePrefabKey),typeof(LocalTransform));
            em.SetComponentData(recruit,new Faction { Id=1 });em.SetComponentData(recruit,new UnitHealth { Max=125,Current=125 });
            var canonicalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab");
            Require(canonicalPrefab!=null,"Canonical production prefab missing");
            var canonicalKey=new FixedString64Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(canonicalPrefab.name));
            em.SetComponentData(recruit,new UnitSourcePrefabKey { Value=canonicalKey });
            var produced=new BuildingProducedUnitReadModel { Unit=recruit,UnitSourceKey=canonicalKey,HasOwnerFaction=1,OwnerFactionId=1,BuildingRuntimeId=12 };
            Require(CampaignMissionRuntimeSystem.IsCitywideProducedUnit(em,in produced,in mission),"Actual second Barracks recruit rejected");
            produced.BuildingRuntimeId=99;Require(!CampaignMissionRuntimeSystem.IsCitywideProducedUnit(em,in produced,in mission),"Unrelated producer supplied mission reinforcement");
            produced.BuildingRuntimeId=12;produced.UnitSourceKey=new FixedString64Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Unit_Chr_Soldier_Male_02_Alt_02"));
            Require(!CampaignMissionRuntimeSystem.IsCitywideProducedUnit(em,in produced,in mission),"Different actual produced recipe supplied required reinforcement");
            mission.AttemptOrdinal--;em.SetComponentData(root,mission);Require(SupportFuelTransactionSystem.Available(em,1)==0,"Stale attempt inherited reserve access");
        }
        private static void ValidateConsent()
        {
            using var world=new World("Citywide existing bounded Smoke consent");var em=world.EntityManager;
            var root=em.CreateEntity(typeof(SupportSessionComponent),typeof(SupportPreviewComponent),typeof(SupportProposalComponent),typeof(SupportInputStateComponent));em.AddBuffer<SupportRequestElement>(root);
            em.SetComponentData(root,new SupportSessionComponent { SessionToken="citywide-consent",AttemptOrdinal=2,SimulationSeconds=10 });
            em.SetComponentData(root,new SupportProposalComponent { ProposalId=7,ConsentVersion=9,ExpiresAt=25,FuelCost=20 });
            var intent=new AssistantCommandIntentRequestElement { Kind=AssistantCommandIntentKind.DeclineSupport,SupportSessionToken="citywide-consent",SupportAttemptOrdinal=2,SupportProposalId=7,SupportConsentVersion=9 };
            Require(AssistantSupportIntentSystem.Process(em,root,in intent)==SupportRejectionReason.None,"Existing Decline failed");
            Require(em.GetComponentData<SupportProposalComponent>(root).Consumed!=0&&em.GetBuffer<SupportRequestElement>(root).Length==0,"Decline issued a Support request");
            intent.Kind=AssistantCommandIntentKind.ApproveSupport;
            Require(AssistantSupportIntentSystem.Process(em,root,in intent)==SupportRejectionReason.ConsentRequired,"Declined exact action approved later");
            em.SetComponentData(root,new SupportProposalComponent { ProposalId=7,ConsentVersion=9,ExpiresAt=5 });
            Require(AssistantSupportIntentSystem.Process(em,root,in intent)==SupportRejectionReason.ConsentExpired,"Expired consent accepted");
            intent.SupportAttemptOrdinal--;Require(AssistantSupportIntentSystem.Process(em,root,in intent)==SupportRejectionReason.WrongAttempt,"Prior attempt consent accepted");
            Require(em.GetBuffer<SupportRequestElement>(root).Length==0,"Rejected consent queued an action");
        }
        private static void ValidateResultSettlement(CampaignMissionRuntimeComponent runtime)
        {
            var authored=AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(CH05M01CitywideAlertConfigBuilder.MissionPath);
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
            using var world=new World("Citywide Alert actual result and reward settlement");
            var em=world.EntityManager;
            var root=em.CreateEntity(typeof(CampaignMissionRootComponent),typeof(CampaignMissionCatalogComponent),typeof(CampaignMissionRuntimeComponent),typeof(CampaignMissionAttemptFactsComponent));
            runtime.Phase=MissionPhaseKind.Result;runtime.Outcome=MissionOutcomeKind.Victory;
            runtime.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;runtime.RunKind=MissionRunKind.FirstClear;
            runtime.LaunchOrigin=MissionLaunchOriginKind.CampaignOperations;
            // Reproduce the actual mission facts: no generic defense forward post exists.
            var facts=new CampaignMissionAttemptFactsComponent { CommandSquadSpawned=1,CommandSquadAlive=1,HostileTotalCount=11,HostileDefeatedCount=11,
                CivilianTotalCount=4,ElapsedMilliseconds=354968,CitywideClinicRecovered=1,CitywideUtilityRecovered=1,CitywideReinforcementReady=1,
                CitywideCoverageReady=1,CitywideAirCleared=1,CitywideMilitaryCleared=1,CitywideStableMilliseconds=6000 };
            ref var published=ref blob.Value.Missions[0];
            Require(CampaignMissionResultProjectionSystem.TryProject(in runtime,in facts,ref published,out var projected)&&projected.Stars==3,"Qualified Armor victory rejected by generic defense result gate");
            var incomplete=facts;incomplete.CitywideReinforcementReady=0;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed real recruited perimeter");
            incomplete=facts;incomplete.CitywideStableMilliseconds=5999;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result bypassed service stabilization hold");
            incomplete=facts;incomplete.HostileDefeatedCount=10;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored surviving original hostile");
            incomplete=facts;incomplete.CitywideFailure=CitywideAlertFailure.StaffLost;incomplete.CivilianLossCount=1;
            Require(!CampaignMissionResultProjectionSystem.TryProject(in runtime,in incomplete,ref published,out _),"Result ignored original civic staff loss");
            em.SetComponentData(root,runtime);em.SetComponentData(root,facts);
            em.SetComponentData(root,new CampaignMissionCatalogComponent{Blob=blob,SourceVersion=runtime.SourceVersion});
            em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            var writer=world.GetOrCreateSystem<CampaignMissionResultProjectionSystem>();
            void Update()=>world.Unmanaged.GetUnsafeSystemRef<CampaignMissionResultProjectionSystem>(writer).OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
            Update();Update();
            Require(em.HasComponent<CampaignMissionResultComponent>(root)&&em.GetBuffer<CampaignMissionSettlementRequestElement>(root).Length==1,"Real result writer failed or queued duplicate settlement");
            string savePath=Path.Combine(Path.GetTempPath(),"citywide-result-regression-"+Guid.NewGuid().ToString("N"));
            try
            {
                var save=new SaveService(new JsonSaveRepository(savePath));var store=new CampaignMissionProgressStore(save);
                store.EnsureAvailable(authored.MissionId);var before=save.LoadProfile();
                var request=em.GetBuffer<CampaignMissionSettlementRequestElement>(root)[0];
                var result=em.GetComponentData<CampaignMissionResultComponent>(root);
                var stale=request;stale.AttemptOrdinal--;
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in stale,in runtime,in result,ref published).Accepted==0,"Prior retry reward settlement accepted");
                var receipt=CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published);
                Require(receipt.Accepted!=0&&receipt.FirstClear!=0,"Real Citywide first-clear settlement rejected");
                var earned=save.LoadProfile();
                Require(earned.commanderXp>before.commanderXp&&earned.credits>before.credits,"Authored Citywide rewards not granted");
                Require(CampaignMissionProgressSettlementSystem.Settle(store,in request,in runtime,in result,ref published).Accepted!=0,"Duplicate receipt not recognized");
                var duplicate=save.LoadProfile();
                Require(duplicate.commanderXp==earned.commanderXp&&duplicate.credits==earned.credits,"Duplicate result paid rewards twice");
            }
            finally {if(Directory.Exists(savePath))Directory.Delete(savePath,true);}
        }
        private static void Require(bool condition,string error) { if (!condition) throw new InvalidOperationException(error); }
    }
}
