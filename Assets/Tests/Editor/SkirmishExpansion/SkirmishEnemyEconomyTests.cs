using System;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Actual native service semantics in disposable ECS records. These tests do
    // not claim native prefab flight/haul delivery or normal mission acceptance.
    public sealed class SkirmishEnemyEconomyTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly World World = new World("EnemyEconomyNativeServices");
            public EntityManager Em => World.EntityManager;
            public readonly Entity Session, Boundary, Barracks, Ground;
            public readonly SkirmishResolvedSetup Setup;
            public readonly SkirmishProductionCatalog Catalog;
            public readonly SkirmishArmyProfileConfig Army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.CombinedArms);
            public Fixture(int materials = 800, bool truck = true, int seed = 17)
            {
                Setup = new SkirmishResolvedSetup {CatalogId="economy.fixture",DefinitionId="economy.fixture",
                    ContentHash="economy.fixture",Seed=seed,DifficultyId=SkirmishDifficultyId.Regular,
                    ObjectiveKind=SkirmishObjectiveKind.BaseAssault,ArmyProfileId=SkirmishArmyProfileId.CombinedArms,
                    Readiness=SkirmishReadinessStage.Field,DeadlineSeconds=1080,LogisticsSupportCapEach=2,
                    RoleOverlays=SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice()};
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(Setup,null,out var policy));Setup.EnemyPolicy=policy;
                Session=Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),
                    typeof(SkirmishEnemyStockComponent),typeof(SkirmishEconomyStockComponent),typeof(SkirmishEnemyCapacityComponent));
                Em.SetComponentData(Session,new SkirmishExpandedSessionComponent {SessionId="economy.fixture.attempt",Phase=SkirmishSessionPhase.Playing,SpawnComplete=1});
                Em.SetComponentData(Session,new SkirmishObjectiveClockComponent {Playing=1,DeadlineSeconds=1080});
                Em.SetComponentData(Session,new SkirmishEnemyStockComponent {Materials=materials,Fuel=350});
                Em.SetComponentData(Session,new SkirmishEconomyStockComponent {FactionId=1,Materials=450,Oil=120,Fuel=350});
                Em.SetComponentData(Session,new SkirmishEnemyCapacityComponent {InfantryCap=48,GroundCap=8,AirCap=2,SupplyCap=100});
                Em.AddComponentObject(Session,new SkirmishResolvedSetupRecord {Setup=Setup});
                Em.AddBuffer<SkirmishProductionReservation>(Session);
                Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(Em,Session,Setup));
                SkirmishResearchService.EnsureSession(Em,Session,Setup);
                var baseline=UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Skirmish/Construction.asset");
                var scene=UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                Catalog=SkirmishProductionCatalog.Create(Setup,baseline,scene,ReadNativeTiming);
                Em.AddComponentObject(Session,new SkirmishProductionCatalogRecord {Catalog=Catalog});
                Boundary=Em.CreateEntity(typeof(BuildingRuntimeStateTag));Em.AddBuffer<BuildingFactionUnitProductionRequest>(Boundary);
                Barracks=Producer(SkirmishProducerKind.Barracks,"barracks");Ground=Producer(SkirmishProducerKind.GroundStaging,"ground");
                if(truck)Truck();
            }
            public Entity Producer(SkirmishProducerKind kind,string stable)
            {
                var e=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth));
                Em.SetComponentData(e,new SkirmishAttemptOwnedComponent {SessionId="economy.fixture.attempt",FactionId=2,StableObjectId=stable,IsStructure=1});
                Em.SetComponentData(e,new SkirmishStructureIdentityComponent {Producer=kind});Em.SetComponentData(e,new UnitHealth {Current=800,Max=800});return e;
            }
            public Entity Truck(byte faction=2,string attempt="economy.fixture.attempt",bool alive=true)
            {
                var e=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth));
                Em.SetComponentData(e,new SkirmishAttemptOwnedComponent {SessionId=new FixedString64Bytes(attempt),FactionId=faction,StableObjectId=new FixedString64Bytes("truck."+e.Index)});
                Em.SetComponentData(e,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.LogisticsTruck,Category=SkirmishPopulationCategory.LogisticsSupport});
                Em.SetComponentData(e,new UnitHealth {Current=alive?160:0,Max=160});return e;
            }
            public void Clock(float elapsed=0,bool paused=false)
                =>Em.SetComponentData(Session,new SkirmishObjectiveClockComponent {Playing=1,Paused=paused?(byte)1:(byte)0,ElapsedSeconds=elapsed,DeadlineSeconds=1080});
            public void Money(int materials)
            {var s=Em.GetComponentData<SkirmishEnemyStockComponent>(Session);s.Materials=materials;Em.SetComponentData(Session,s);}
            public SkirmishEnemyEconomyService.Decision Tick(SkirmishRoleKind combat=SkirmishRoleKind.Rifle,bool urgent=false,
                SkirmishRoleKind goal=SkirmishRoleKind.Tank,byte faction=2)
                =>SkirmishEnemyEconomyService.Tick(Em,Session,faction,combat,urgent,goal,1);
            public DynamicBuffer<SkirmishResearchQueueItem> Research=>Em.GetBuffer<SkirmishResearchQueueItem>(Session);
            public DynamicBuffer<BuildingFactionUnitProductionRequest> Requests=>Em.GetBuffer<BuildingFactionUnitProductionRequest>(Boundary);
            static bool ReadNativeTiming(GameObject prefab,out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata)
            {
                metadata=default;if(prefab==null||!prefab.TryGetComponent(out UnitGridAuthoring a))return false;
                metadata=new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(a.ProductionDurationSeconds,
                    a.ProductionTransportPrefab,a.IsAirUnit,a.ProductionTransportArrivalSeconds,a.ProductionTransportHoldForNextReadySeconds,
                    a.ProductionTransportMaxConcurrent,a.ProductionTransportRequiresAirportRunway,a.ProductionTransportUsesRunwayLanding,a.GetConfiguredFootprintCells());return true;
            }
            public void Dispose(){Catalog.Dispose();World.Dispose();}
        }
        [Test] public void PaidEnemyReadinessUsesRealCostPositiveTimeAndIndependentPlayerState()
        {
            foreach(int seed in new[]{17,104734})
            {
                using var f=new Fixture(seed:seed);var d=f.Tick();Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,d.Action);
                Assert.AreNotEqual(0u,d.ResearchId);Assert.AreEqual(560,SkirmishMaterialsService.Read(f.Em,f.Session,2));
                Assert.AreEqual(450,SkirmishMaterialsService.Read(f.Em,f.Session,1));Assert.AreEqual(1,f.Research.Length);
                Assert.AreEqual(240,f.Research[0].MaterialsPaid);Assert.AreEqual(45,f.Research[0].RemainingSeconds);
                Assert.AreEqual(0,SkirmishResearchService.Tick(f.Em,f.Session,100,true));
                Assert.AreEqual(0,SkirmishResearchService.Tick(f.Em,f.Session,44,false));
                Assert.AreEqual(1,SkirmishResearchService.Tick(f.Em,f.Session,1,false));
                Assert.IsTrue(SkirmishResearchService.TryReadState(f.Em,f.Session,2,out var enemy));Assert.AreEqual(SkirmishReadinessStage.Established,enemy.Readiness);
                Assert.IsTrue(SkirmishResearchService.TryReadState(f.Em,f.Session,1,out var player));Assert.AreEqual(SkirmishReadinessStage.Field,player.Readiness);
                Assert.AreEqual(1u,player.NextResearchId,"Native global ID allocator legitimately advances for enemy research");
                var stocks=f.Em.GetComponentData<SkirmishEconomyStockComponent>(f.Session);Assert.AreEqual(120,stocks.Oil);Assert.AreEqual(350,stocks.Fuel);
                Assert.AreEqual(SkirmishRoleKind.Tank,f.Tick().SuggestedCombatRole);
            }
        }
        [Test] public void OpenResearchRecordsPreventSecondDebitAndProducerLossDoesNotRepeat()
        {
            using var f=new Fixture();var first=f.Tick();for(int i=1;i<30;i++){f.Clock(i);Assert.AreEqual(first.ResearchId,f.Tick().ResearchId);}
            Assert.AreEqual(560,SkirmishMaterialsService.Read(f.Em,f.Session,2));Assert.AreEqual(1,f.Research.Length);
            // Native queue/state are the registered authorities; reconstructed caller has no private cache.
            Assert.AreEqual(SkirmishEnemyEconomyAction.ResearchPending,f.Tick().Action);
            Assert.AreEqual(1,SkirmishResearchService.NotifyProducerDestroyed(f.Em,f.Session,SkirmishProducerKind.Barracks,2));
            for(int i=30;i<60;i++){f.Clock(i);f.Tick();}Assert.AreEqual(1,f.Research.Length);Assert.AreEqual(560,SkirmishMaterialsService.Read(f.Em,f.Session,2));
            Assert.AreEqual(SkirmishResearchPhase.Lost,f.Research[0].Phase);
        }
        [Test] public void ExactPaidRebuildEvidencePermitsOneRetryWithoutClearingOrRefundingLoss()
        {
            using var f=new Fixture();uint failed=f.Tick().ResearchId;
            Assert.AreEqual(1,SkirmishResearchService.NotifyProducerDestroyed(f.Em,f.Session,SkirmishProducerKind.Barracks,2));
            f.Producer(SkirmishProducerKind.Barracks,"rebuilt.barracks");
            var proof=new SkirmishEnemyEconomyService.ResearchRecoveryEvidence("economy.fixture.attempt",failed,"barracks","rebuilt.barracks");
            SkirmishEnemyEconomyService.Tick(f.Em,f.Session,2,SkirmishRoleKind.Rifle,false,SkirmishRoleKind.Tank,1,in proof);
            Assert.AreEqual(1,f.Research.Length,"Old producer still alive invalidates replacement proof");
            f.Em.SetComponentData(f.Barracks,new UnitHealth {Current=0});
            var stale=new SkirmishEnemyEconomyService.ResearchRecoveryEvidence("old.attempt",failed,"barracks","rebuilt.barracks");
            SkirmishEnemyEconomyService.Tick(f.Em,f.Session,2,SkirmishRoleKind.Rifle,false,SkirmishRoleKind.Tank,1,in stale);Assert.AreEqual(1,f.Research.Length);
            var retry=SkirmishEnemyEconomyService.Tick(f.Em,f.Session,2,SkirmishRoleKind.Rifle,false,SkirmishRoleKind.Tank,1,in proof);
            Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,retry.Action);Assert.AreNotEqual(failed,retry.ResearchId);
            Assert.AreEqual(2,f.Research.Length);Assert.AreEqual(SkirmishResearchPhase.Lost,f.Research[0].Phase);
            Assert.AreEqual(320,SkirmishMaterialsService.Read(f.Em,f.Session,2));Assert.AreEqual(450,SkirmishMaterialsService.Read(f.Em,f.Session,1));
            for(int i=1;i<20;i++){f.Clock(i);SkirmishEnemyEconomyService.Tick(f.Em,f.Session,2,SkirmishRoleKind.Rifle,false,SkirmishRoleKind.Tank,1,in proof);}
            Assert.AreEqual(2,f.Research.Length);Assert.AreEqual(320,SkirmishMaterialsService.Read(f.Em,f.Session,2));
        }
        [Test] public void ImmediateDefenseAndDynamicReplacementReserveBeatInvestment()
        {
            using var f=new Fixture();var d=f.Tick(urgent:true);Assert.AreEqual(SkirmishEnemyEconomyAction.PreserveDefense,d.Action);
            Assert.IsTrue(d.PermitCombatPurchase);Assert.AreEqual(0,f.Research.Length);Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2));
            f.Money(419);Assert.AreNotEqual(SkirmishEnemyEconomyAction.ReadinessQueued,f.Tick().Action);
            f.Money(420);Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,f.Tick().Action);
            Assert.IsFalse(SkirmishEnemyEconomyService.CanSpend(int.MaxValue,int.MaxValue,1));
            using var changed=new Fixture();
            for(int i=0;i<changed.Setup.RoleOverlays.Length;i++)
                if(changed.Setup.RoleOverlays[i].RoleKind==SkirmishRoleKind.LogisticsTruck)
                {var row=changed.Setup.RoleOverlays[i];row.MaterialsCost=150;changed.Setup.RoleOverlays[i]=row;}
            changed.Money(469);Assert.AreNotEqual(SkirmishEnemyEconomyAction.ReadinessQueued,changed.Tick().Action);
            changed.Money(470);Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,changed.Tick().Action);
        }
        [Test] public void MissingTruckUsesSingleNativeIntentAndAuthoritativePaidQueue()
        {
            using var f=new Fixture(truck:false);Assert.AreEqual(SkirmishEnemyEconomyAction.LogisticsRequested,f.Tick().Action);
            Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(2,f.Requests[0].FactionId);
            Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2),"Native request emission is not payment or delivery");
            int token=f.Requests[0].RequestId;for(int i=1;i<40;i++){f.Clock(i);f.Tick();}Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(token,f.Requests[0].RequestId);
            // Exercise the actual transaction owner separately: this is a paid
            // queue/cap test, not an invented processor callback or native delivery.
            Assert.IsTrue(SkirmishProductionService.TryQueue(f.Em,f.Session,SkirmishRoleIds.LogisticsTruck,1,f.Army,2,out var paid));
            Assert.AreEqual(100,paid.MaterialsCost);Assert.AreEqual(700,SkirmishMaterialsService.Read(f.Em,f.Session,2));
            Assert.AreEqual(1,SkirmishEnemyEconomyService.CountOwnedOrPending(f.Em,f.Session,2,SkirmishRoleKind.LogisticsTruck));
            Assert.AreEqual(450,SkirmishMaterialsService.Read(f.Em,f.Session,1));
        }
        [Test] public void ActualOwnedPendingAndReleasedTruckCountsDoNotDuplicateOrCrossAttempts()
        {
            using var f=new Fixture(truck:false);f.Truck(1);f.Truck(2,"old.attempt");f.Truck(alive:false);
            var hidden=f.Truck();f.Em.AddComponent<Disabled>(hidden);
            Assert.AreEqual(1,SkirmishEnemyEconomyService.CountOwnedOrPending(f.Em,f.Session,2,SkirmishRoleKind.LogisticsTruck));
            f.Tick(goal:SkirmishRoleKind.None);Assert.AreEqual(0,f.Requests.Length);
            var owner=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(hidden);owner.CapacityReleased=1;f.Em.SetComponentData(hidden,owner);
            f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Add(new SkirmishProductionReservation {FactionId=2,Role=SkirmishRoleKind.LogisticsTruck,
                Category=SkirmishPopulationCategory.LogisticsSupport,MemberCount=1,Phase=SkirmishReservationPhase.Producing});
            Assert.AreEqual(1,SkirmishEnemyEconomyService.CountOwnedOrPending(f.Em,f.Session,2,SkirmishRoleKind.LogisticsTruck));f.Tick(goal:SkirmishRoleKind.None);Assert.AreEqual(0,f.Requests.Length);
        }
        [Test] public void CapacityProducerQueueAndArmyRestrictionsPreventWastefulResearch()
        {
            using var f=new Fixture();var cap=f.Em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session);cap.GroundCap=0;f.Em.SetComponentData(f.Session,cap);
            f.Tick();Assert.AreEqual(0,f.Research.Length);cap.GroundCap=8;f.Em.SetComponentData(f.Session,cap);
            f.Em.SetComponentData(f.Ground,new UnitHealth {Current=0});f.Tick();Assert.AreEqual(0,f.Research.Length);
            f.Em.SetComponentData(f.Ground,new UnitHealth {Current=800});
            f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Add(new SkirmishProductionReservation {FactionId=2,Producer=SkirmishProducerKind.Barracks,Phase=SkirmishReservationPhase.Producing});
            f.Tick();Assert.AreEqual(0,f.Research.Length);f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Clear();
            f.Setup.ArmyProfileId=SkirmishArmyProfileId.GroundManeuver;f.Tick(goal:SkirmishRoleKind.Fighter);Assert.AreEqual(0,f.Research.Length);
            Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2));
        }
        [Test] public void PauseResultLegacyBadPolicyAndWrongFactionCannotSpend()
        {
            using var f=new Fixture(truck:false);f.Clock(paused:true);f.Tick();Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(0,f.Research.Length);
            f.Clock();f.Tick(faction:1);Assert.AreEqual(0,f.Requests.Length);
            var state=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);state.Phase=SkirmishSessionPhase.Finished;f.Em.SetComponentData(f.Session,state);f.Tick();Assert.AreEqual(0,f.Requests.Length);
            state.Phase=SkirmishSessionPhase.Playing;state.IsLegacy=1;f.Em.SetComponentData(f.Session,state);f.Tick();Assert.AreEqual(0,f.Requests.Length);
            state.IsLegacy=0;f.Em.SetComponentData(f.Session,state);var planner=f.Em.GetComponentData<SkirmishEnemyPlannerV1Component>(f.Session);planner.PolicyHash++;f.Em.SetComponentData(f.Session,planner);f.Tick();Assert.AreEqual(0,f.Requests.Length);
            Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2));
        }
        [Test] public void TimeAndExplicitNeedGateInvestmentAndPureQuoteDoesNotUnlock()
        {
            using var f=new Fixture();f.Tick(goal:SkirmishRoleKind.None);Assert.AreEqual(0,f.Research.Length);
            Assert.IsTrue(f.Catalog.TryReadProductionDeliverySeconds(SkirmishRoleKind.Tank,out float nativeSeconds));Assert.Greater(nativeSeconds,0);
            f.Clock(1080 - 45 - nativeSeconds);f.Tick();Assert.AreEqual(0,f.Research.Length);f.Clock();
            var savedCatalog=f.Em.GetComponentObject<SkirmishProductionCatalogRecord>(f.Session);
            f.Em.RemoveComponent<SkirmishProductionCatalogRecord>(f.Session);f.Tick();Assert.AreEqual(0,f.Research.Length);
            f.Em.AddComponentObject(f.Session,savedCatalog);
            var quote=SkirmishProductionService.EvaluateQueueAtReadiness(f.Em,f.Session,SkirmishRoleIds.Tank,1,f.Army,2,SkirmishReadinessStage.Established);
            Assert.IsTrue(quote.Accepted);Assert.AreEqual(360,quote.MaterialsCost);
            Assert.IsFalse(SkirmishProductionService.TryQueue(f.Em,f.Session,SkirmishRoleIds.Tank,1,f.Army,2,out var actual));Assert.AreEqual(SkirmishReasonCode.MissingReadiness,actual.Reason);
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Length);Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2));
        }
        [Test] public void FullArsenalGoalRequiresBothRemainingNativeResearchStepsAndDeliveryTime()
        {
            using var f=new Fixture();f.Producer(SkirmishProducerKind.Airport,"airport");
            Assert.IsTrue(f.Catalog.TryReadProductionDeliverySeconds(SkirmishRoleKind.Fighter,out float delivery));
            Assert.IsTrue(SkirmishResearchService.CanQueue(f.Em,f.Session,SkirmishResearchKind.Readiness,2,
                out var next,out float nextSeconds));
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedSeconds,nextSeconds);
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedMaterials,next.MaterialsCost);
            var projected=SkirmishProductionService.EvaluateQueueAtReadiness(f.Em,f.Session,SkirmishRoleIds.Fighter,
                1,f.Army,2,SkirmishReadinessStage.FullArsenal);
            Assert.IsTrue(projected.Accepted,"The goal is otherwise genuinely feasible at native required readiness.");
            float total=SkirmishResearchCosts.ReadinessEstablishedSeconds+SkirmishResearchCosts.ReadinessFullArsenalSeconds+delivery;
            f.Clock(1080-nextSeconds-delivery-SkirmishResearchCosts.ReadinessFullArsenalSeconds*.5f);
            Assert.AreNotEqual(SkirmishEnemyEconomyAction.ReadinessQueued,f.Tick(combat:SkirmishRoleKind.None,goal:SkirmishRoleKind.Fighter).Action);
            Assert.AreEqual(0,f.Research.Length);Assert.AreEqual(800,SkirmishMaterialsService.Read(f.Em,f.Session,2));
            f.Clock(1080-total);f.Tick(combat:SkirmishRoleKind.None,goal:SkirmishRoleKind.Fighter);Assert.AreEqual(0,f.Research.Length);
            f.Clock(1080-total-1);var first=f.Tick(combat:SkirmishRoleKind.None,goal:SkirmishRoleKind.Fighter);
            Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,first.Action);
            Assert.AreEqual(1,f.Research.Length);Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedMaterials,f.Research[0].MaterialsPaid);
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedSeconds,f.Research[0].RemainingSeconds);
            Assert.AreEqual(1,SkirmishResearchService.Tick(f.Em,f.Session,nextSeconds,false));
            Assert.IsTrue(SkirmishResearchService.TryReadState(f.Em,f.Session,2,out var state));
            Assert.AreEqual(SkirmishReadinessStage.Established,state.Readiness);
            // Disclosed isolated fixture funds for the second genuine native step;
            // no readiness fields are set and no unlock is injected.
            f.Money(800);f.Clock(1080-SkirmishResearchCosts.ReadinessFullArsenalSeconds-delivery);
            f.Tick(combat:SkirmishRoleKind.None,goal:SkirmishRoleKind.Fighter);Assert.AreEqual(1,f.Research.Length);
            f.Clock(1080-SkirmishResearchCosts.ReadinessFullArsenalSeconds-delivery-1);
            var second=f.Tick(combat:SkirmishRoleKind.None,goal:SkirmishRoleKind.Fighter);
            Assert.AreEqual(SkirmishEnemyEconomyAction.ReadinessQueued,second.Action);
            Assert.AreEqual(2,f.Research.Length);Assert.AreNotEqual(first.ResearchId,second.ResearchId);
            Assert.AreEqual(SkirmishResearchCosts.ReadinessFullArsenalMaterials,f.Research[1].MaterialsPaid);
            Assert.AreEqual(SkirmishResearchCosts.ReadinessFullArsenalSeconds,f.Research[1].RemainingSeconds);
            Assert.AreEqual(450,SkirmishMaterialsService.Read(f.Em,f.Session,1));
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Length);
        }

        [Test] public void OpenNativeResearchExcludesUrgentAndRetryPurchaseUntilCompletion()
        {
            foreach(bool retry in new[]{false,true})
            {
                using var f=new Fixture();
                if(retry)
                {
                    var cap=f.Em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session);cap.GroundCap=0;f.Em.SetComponentData(f.Session,cap);
                    Assert.AreEqual(SkirmishEnemyProductionIntentState.Backoff,SkirmishEnemyPaidProductionAdapter.TryRequest(f.Em,f.Session,
                        new FixedString64Bytes("economy.control.car"),SkirmishRoleKind.Car,1,f.Army,0));
                    cap.GroundCap=8;f.Em.SetComponentData(f.Session,cap);
                }
                Assert.IsTrue(SkirmishResearchService.TryQueue(f.Em,f.Session,SkirmishResearchKind.Readiness,2,out var research));
                Assert.AreEqual(560,SkirmishMaterialsService.Read(f.Em,f.Session,2));
                f.Clock(2);
                Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Car),
                    "Different native GroundStaging producer is genuinely otherwise eligible during Barracks research.");
                var before=f.Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(f.Session);
                foreach(bool urgent in new[]{false,true})
                {
                    var decision=f.Tick(combat:SkirmishRoleKind.Car,urgent:urgent,goal:SkirmishRoleKind.None);
                    Assert.AreEqual(SkirmishEnemyEconomyAction.ResearchPending,decision.Action);Assert.AreEqual(research.ResearchId,decision.ResearchId);
                    // This is the same admission guard used by the commander; no hidden purchase call.
                    if(decision.PermitCombatPurchase)SkirmishEnemyPaidProductionAdapter.TryRequest(f.Em,f.Session,
                        new FixedString64Bytes("economy.control.car"),SkirmishRoleKind.Car,1,f.Army,2);
                    Assert.IsFalse(decision.PermitCombatPurchase);
                    Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(0,f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Length);
                    Assert.AreEqual(560,SkirmishMaterialsService.Read(f.Em,f.Session,2));
                    Assert.AreEqual(before.State,f.Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(f.Session).State);
                    Assert.AreEqual(before.RequestId,f.Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(f.Session).RequestId);
                }
                Assert.AreEqual(1,SkirmishResearchService.Tick(f.Em,f.Session,SkirmishResearchCosts.ReadinessEstablishedSeconds,false));
                var after=f.Tick(combat:SkirmishRoleKind.Car,urgent:true,goal:SkirmishRoleKind.None);
                if(!retry)
                {
                    Assert.IsTrue(after.PermitCombatPurchase);
                    SkirmishEnemyPaidProductionAdapter.TryRequest(f.Em,f.Session,new FixedString64Bytes("economy.control.car"),SkirmishRoleKind.Car,1,f.Army,2);
                }
                Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(2,f.Requests[0].FactionId);
                var quote=SkirmishProductionService.EvaluateQueue(f.Em,f.Session,SkirmishRoleIds.Car,1,f.Army,2);
                Assert.IsTrue(quote.Accepted);Assert.Greater(quote.MaterialsCost,0);
                Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(f.Setup.RoleOverlays,SkirmishRoleKind.Car,out var car));
                Assert.AreEqual(car.MaterialsCost,quote.MaterialsCost,"Native single-Car quote must match the configured authoritative role price.");
                int materialsBefore=SkirmishMaterialsService.Read(f.Em,f.Session,2);
                Assert.IsTrue(SkirmishProductionService.TryQueue(f.Em,f.Session,SkirmishRoleIds.Car,1,f.Army,2,out var paid));
                Assert.AreEqual(quote.MaterialsCost,paid.MaterialsCost);
                Assert.AreEqual(materialsBefore-quote.MaterialsCost,SkirmishMaterialsService.Read(f.Em,f.Session,2),"Exact native paid debit, not a guessed historical price.");
                var receipts=f.Em.GetBuffer<SkirmishProductionReservation>(f.Session);Assert.AreEqual(1,receipts.Length);
                Assert.AreEqual(paid.ReservationId,receipts[0].ReservationId);Assert.AreEqual(SkirmishRoleKind.Car,receipts[0].Role);
                Assert.AreEqual((byte)2,receipts[0].FactionId);Assert.AreEqual(quote.MemberCount,receipts[0].MemberCount);
                Assert.AreEqual(quote.MaterialsCost,receipts[0].MaterialsPaid);
                Assert.AreEqual(450,SkirmishMaterialsService.Read(f.Em,f.Session,1));
            }
        }

        public static void RunFocusedValidation()
        {
            var t=new SkirmishEnemyEconomyTests();t.PaidEnemyReadinessUsesRealCostPositiveTimeAndIndependentPlayerState();
            t.OpenResearchRecordsPreventSecondDebitAndProducerLossDoesNotRepeat();t.ExactPaidRebuildEvidencePermitsOneRetryWithoutClearingOrRefundingLoss();t.ImmediateDefenseAndDynamicReplacementReserveBeatInvestment();
            t.MissingTruckUsesSingleNativeIntentAndAuthoritativePaidQueue();t.ActualOwnedPendingAndReleasedTruckCountsDoNotDuplicateOrCrossAttempts();
            t.CapacityProducerQueueAndArmyRestrictionsPreventWastefulResearch();t.PauseResultLegacyBadPolicyAndWrongFactionCannotSpend();
            t.TimeAndExplicitNeedGateInvestmentAndPureQuoteDoesNotUnlock();t.FullArsenalGoalRequiresBothRemainingNativeResearchStepsAndDeliveryTime();t.OpenNativeResearchExcludesUrgentAndRetryPurchaseUntilCompletion();Debug.Log("[SkirmishEnemyEconomyTests] result=Passed tests=11");
        }
    }
}
