using System;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyEconomicCoordinatorTests
    {
        // Native services/real catalog in disposable Worlds. No full terrain,
        // delivery, balance, player journey or device certification claim.
        sealed class Fixture:IDisposable
        {
            public readonly SkirmishEnemyConstructionAdapterTests.Fixture Native=new(true);
            public EntityManager Em=>Native.Em;
            public Entity Session=>Native.Session;
            public SkirmishResolvedSetup Setup=>Native.Setup;
            public readonly SkirmishProductionCatalog Catalog;
            public SkirmishEnemyPlannerV1Component Planner;
            public Fixture(bool barracks=true,bool ground=true,bool truck=true,bool timings=true)
            {
                Setup.ArmyProfileId=SkirmishArmyProfileId.CombinedArms;Setup.DeadlineSeconds=1080;
                Setup.RoleOverlays=SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice();Setup.LogisticsSupportCapEach=2;
                SkirmishResearchService.EnsureSession(Em,Session,Setup);
                Em.AddComponentData(Session,new SkirmishEnemyCapacityComponent{InfantryCap=48,GroundCap=8,AirCap=2,SupplyCap=100});
                Em.AddComponentData(Session,new SkirmishEnemyStockComponent{Fuel=350});
                Em.AddBuffer<BuildingFactionUnitProductionRequest>(Native.Boundary);
                var rows=Em.GetBuffer<BuildingConfiguredSpawnableReadModel>(Native.Boundary);
                rows.Add(new BuildingConfiguredSpawnableReadModel{BuildingId=new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(SkirmishStructureIds.VisualKey(SkirmishStructureIds.GroundStaging))),CanRequest=1,MaterialsCost=200,FootprintCells=new int2(4,4)});
                rows.Add(new BuildingConfiguredSpawnableReadModel{BuildingId=new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(SkirmishStructureIds.VisualKey(SkirmishStructureIds.OilPump))),CanRequest=1,MaterialsCost=100,FootprintCells=new int2(2,2)});
                if(barracks)Producer(SkirmishProducerKind.Barracks,SkirmishStructureIds.Barracks);
                if(ground)Producer(SkirmishProducerKind.GroundStaging,SkirmishStructureIds.GroundStaging);
                if(truck)
                {
                    var actor=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth));
                    Em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="construction.attempt",StableObjectId="truck",FactionId=2});
                    Em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.LogisticsTruck,Category=SkirmishPopulationCategory.LogisticsSupport});
                    Em.SetComponentData(actor,new UnitHealth{Current=160,Max=160});
                }
                var baseline=UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Skirmish/Construction.asset");
                var scene=UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
                Catalog=SkirmishProductionCatalog.Create(Setup,baseline,scene,timings?ReadNativeTiming:null);
                Em.AddComponentObject(Session,new SkirmishProductionCatalogRecord{Catalog=Catalog});
                Planner=Em.GetComponentData<SkirmishEnemyPlannerV1Component>(Session);
            }
            void Producer(SkirmishProducerKind kind,string id)
            {
                var actor=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
                Em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="construction.attempt",FactionId=2,StableObjectId=new FixedString64Bytes(id),IsStructure=1});
                Em.SetComponentData(actor,new SkirmishStructureIdentityComponent{Producer=kind});Em.SetComponentData(actor,new UnitHealth{Current=800,Max=800});
                Em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishStructureIds.VisualKey(id))});
            }
            public SkirmishEnemyEconomicCoordinator.Decision Tick(SkirmishRoleKind role=SkirmishRoleKind.Rifle,bool urgent=false)
            {
                var result=SkirmishEnemyEconomicCoordinator.Tick(Em,Session,role,urgent,SkirmishRoleKind.None,ref Planner);
                Em.SetComponentData(Session,Planner);return result;
            }
            static bool ReadNativeTiming(GameObject prefab,out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata)
            {
                metadata=default;if(prefab==null || !prefab.TryGetComponent(out UnitGridAuthoring a))return false;
                metadata=new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(a.ProductionDurationSeconds,
                    a.ProductionTransportPrefab,a.IsAirUnit,a.ProductionTransportArrivalSeconds,a.ProductionTransportHoldForNextReadySeconds,
                    a.ProductionTransportMaxConcurrent,a.ProductionTransportRequiresAirportRunway,a.ProductionTransportUsesRunwayLanding,a.GetConfiguredFootprintCells());return true;
            }
            public void Dispose(){Catalog.Dispose();Native.Dispose();}
        }
        [Test]public void PendingConstructionBlocksSpendsAndReconcileSettlesBeforeStrategicProbe()
        {
            using var f=new Fixture(barracks:false);Assert.IsTrue(f.Native.Request());
            Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionPending,f.Tick().Gate);Assert.AreEqual(800,f.Native.Materials);
            Assert.AreEqual(0,f.Em.GetBuffer<BuildingFactionUnitProductionRequest>(f.Native.Boundary).Length);
            f.Native.Failed();SkirmishEnemyEconomicCoordinator.Reconcile(f.Em,f.Session);
            Assert.AreEqual(1000,f.Native.Materials);Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.Rejected,f.Native.Intent.Phase);
        }
        [Test]public void FeasibleUrgentDefenderBeatsOriginalSupplyRecovery()
        {
            using var f=new Fixture();f.Setup.Structures=new[]{new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.OilPump}};
            Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            var d=f.Tick(urgent:true);Assert.AreEqual(SkirmishEnemyEconomyAction.PreserveDefense,d.Economy.Action);Assert.IsTrue(d.Economy.PermitCombatPurchase);
            Assert.AreEqual(SkirmishEnemyEconomicGate.None,d.Gate);Assert.AreEqual(1000,f.Native.Materials);Assert.AreEqual(0,f.Native.Requests.Length);
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishResearchQueueItem>(f.Session).Length);
        }
        [Test]public void MissingWantedProducerUsesSinglePaidNativeConstructionBeforeResearchOrUnits()
        {
            using var f=new Fixture(barracks:false);var key=f.Planner.NextActionKey;
            var d=f.Tick();Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionRequested,d.Gate);Assert.AreEqual(SkirmishStructureIds.Barracks,d.StructureId);
            Assert.AreEqual(key+1,f.Planner.NextActionKey);Assert.AreEqual(800,f.Native.Materials);Assert.AreEqual(1,f.Native.Requests.Length);
            Assert.AreEqual(0,f.Em.GetBuffer<BuildingFactionUnitProductionRequest>(f.Native.Boundary).Length);
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishResearchQueueItem>(f.Session).Length);
            Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionPending,f.Tick().Gate);Assert.AreEqual(800,f.Native.Materials);
        }
        [Test]public void MissingLogisticsProducerHasPriorityOverOriginalSupplyFacility()
        {
            using var f=new Fixture(ground:false,truck:false);f.Setup.Structures=new[]{new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.OilPump}};
            var d=f.Tick();Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionRequested,d.Gate);Assert.AreEqual(SkirmishStructureIds.GroundStaging,d.StructureId);
            Assert.AreEqual(800,f.Native.Materials);Assert.AreEqual(1,f.Native.Requests.Length);
        }
        [Test]public void OnlyOriginalOwnMissingSupplyFacilitiesAreRecoveredWithoutExpansion()
        {
            using var absent=new Fixture();absent.Tick();Assert.AreEqual(0,absent.Native.Requests.Length);Assert.AreEqual(1000,absent.Native.Materials);
            using var f=new Fixture();f.Setup.Structures=new[]{new SkirmishResolvedStructureEntry{FactionId=1,StructureId=SkirmishStructureIds.Refinery},
                new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.OilPump}};
            var d=f.Tick();Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionRequested,d.Gate);Assert.AreEqual(SkirmishStructureIds.OilPump,d.StructureId);
            Assert.AreEqual(900,f.Native.Materials);Assert.AreEqual(1,f.Native.Intent.DesiredCount);
        }
        [Test]public void UnknownTimingAndClosedNativeWindowPreventConstructionWithoutInventedRates()
        {
            using var unknown=new Fixture(barracks:false,timings:false);Assert.AreEqual(SkirmishEnemyEconomicGate.NativeFitUnknown,unknown.Tick().Gate);
            Assert.AreEqual(1000,unknown.Native.Materials);Assert.AreEqual(0,unknown.Native.Requests.Length);
            using var closed=new Fixture(barracks:false);
            Assert.IsTrue(SkirmishEnemyEconomicCoordinator.TryReadNativeFit(closed.Em,closed.Session,SkirmishRoleKind.Rifle,SkirmishReadinessStage.Field,out float seconds,out int cost));
            Assert.AreEqual(0,cost);closed.Native.At(1080-seconds);
            Assert.AreEqual(SkirmishEnemyEconomicGate.FitWindowClosed,closed.Tick().Gate);Assert.AreEqual(1000,closed.Native.Materials);
        }
        [Test]public void FullArsenalFitIncludesBothNativeReadinessStepsAndDoesNotUnlockState()
        {
            using var f=new Fixture();Assert.IsTrue(f.Catalog.TryReadProductionDeliverySeconds(SkirmishRoleKind.Fighter,out float delivery));
            Assert.IsTrue(SkirmishEnemyEconomicCoordinator.TryReadNativeFit(f.Em,f.Session,SkirmishRoleKind.Fighter,SkirmishReadinessStage.Field,out float seconds,out int cost));
            Assert.AreEqual(delivery+SkirmishResearchCosts.ReadinessEstablishedSeconds+SkirmishResearchCosts.ReadinessFullArsenalSeconds,seconds);
            Assert.AreEqual(SkirmishResearchCosts.ReadinessEstablishedMaterials+SkirmishResearchCosts.ReadinessFullArsenalMaterials,cost);
            Assert.IsTrue(SkirmishResearchService.TryReadState(f.Em,f.Session,2,out var state));Assert.AreEqual(SkirmishReadinessStage.Field,state.Readiness);
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishResearchQueueItem>(f.Session).Length);Assert.AreEqual(1000,f.Native.Materials);
        }
        [Test]public void FullBankMissingProducerRebuildSettlesOldDebtWithoutOtherFeasiblePurchases()
        {
            using var f=new Fixture(barracks:false,ground:false,truck:false);f.Native.Request();
            var bank=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Native.Bank);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref bank,bank.Capacity-bank.Current,FactionTacticalMaterialsSourceKind.Fabrication));f.Em.SetComponentData(f.Native.Bank,bank);
            f.Native.Failed();SkirmishEnemyEconomicCoordinator.Reconcile(f.Em,f.Session);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.RefundPending,f.Native.Intent.Phase);
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.LogisticsTruck));
            var previous=f.Native.Intent;var d=f.Tick();Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionRequested,d.Gate);
            Assert.AreEqual(1200,f.Native.Materials);Assert.AreEqual(200,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Native.Bank).LifetimeConstructionSpent);
            Assert.Greater(f.Native.Intent.CandidateIndex,previous.CandidateIndex);Assert.AreEqual(1,f.Native.Requests.Length);
        }
        [Test]public void ExistingActualResearchAndUnitReceiptsBlockSecondPaidIntentEvenDuringDefense()
        {
            using var research=new Fixture();
            Assert.IsTrue(SkirmishResearchService.TryQueue(research.Em,research.Session,SkirmishResearchKind.Readiness,2,out var paid));
            var d=research.Tick(urgent:true);Assert.AreEqual(SkirmishEnemyEconomicGate.OtherPaidPending,d.Gate);
            Assert.IsFalse(d.Economy.PermitCombatPurchase);Assert.AreEqual(paid.ResearchId,d.Economy.ResearchId);
            Assert.AreEqual(760,research.Native.Materials);Assert.AreEqual(0,research.Native.Requests.Length);
            using var unit=new Fixture();
            Assert.IsTrue(SkirmishProductionService.TryQueue(unit.Em,unit.Session,SkirmishRoleIds.Rifle,1,
                SkirmishArmyProfileConfig.ResolveCached(unit.Setup.ArmyProfileId),2,out var accepted));
            d=unit.Tick(urgent:true);Assert.AreEqual(SkirmishEnemyEconomicGate.OtherPaidPending,d.Gate);Assert.IsFalse(d.Economy.PermitCombatPurchase);
            Assert.AreEqual(1000-accepted.MaterialsCost,unit.Native.Materials);Assert.AreEqual(0,unit.Native.Requests.Length);
        }
        [Test]public void PendingPaidDeliveryReconcilesThroughNativeAdoptionBeforeNextAdmission()
        {
            using var f=new Fixture();var army=SkirmishArmyProfileConfig.ResolveCached(f.Setup.ArmyProfileId);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.RequestPending,SkirmishEnemyPaidProductionAdapter.TryRequest(
                f.Em,f.Session,"unit.before.build",SkirmishRoleKind.Rifle,0,army,0));
            var request=f.Em.GetBuffer<BuildingFactionUnitProductionRequest>(f.Native.Boundary)[0];
            Assert.IsTrue(SkirmishProductionService.TryQueue(f.Em,f.Session,SkirmishRoleIds.Rifle,1,army,2,out var paid));
            Assert.IsTrue(SkirmishProductionService.BindNativeQueue(f.Em,f.Session,paid.ReservationId,17));
            Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in request,SkirmishRoleKind.Rifle,
                f.Session,request.RequestAttempt,paid.ReservationId));
            SkirmishProductionService.StartNativeQueue(f.Em,f.Session,paid.ReservationId,request.RequestAttempt);
            Assert.AreEqual(SkirmishEnemyEconomicGate.OtherPaidPending,f.Tick().Gate);
            for(int n=0;n<paid.MemberCount;n++)
            {
                var actor=f.Em.CreateEntity(typeof(Faction),typeof(UnitSourcePrefabKey),typeof(UnitHealth));
                f.Em.SetComponentData(actor,new Faction{Id=2});f.Em.SetComponentData(actor,new UnitHealth{Current=100,Max=100});
                f.Em.SetComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle))});
                Assert.IsTrue(SkirmishProductionService.AdoptNativeMember(f.Em,f.Session,paid.ReservationId,17,actor,request.RequestAttempt));
            }
            SkirmishEnemyEconomicCoordinator.Reconcile(f.Em,f.Session);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Delivered,f.Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(f.Session).State);
            f.Setup.Structures=new[]{new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.OilPump}};
            Assert.AreEqual(SkirmishEnemyEconomicGate.ConstructionRequested,f.Tick().Gate);
            Assert.AreEqual(1000-paid.MaterialsCost-100,f.Native.Materials);
        }
        [Test]public void KnownFullNativeCapacityPreventsInvestingInUnusableProducer()
        {
            using var f=new Fixture(barracks:false);var cap=f.Em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session);
            cap.InfantryLive=cap.InfantryCap;f.Em.SetComponentData(f.Session,cap);
            Assert.AreEqual(SkirmishEnemyEconomicGate.CurrentRoleBlocked,f.Tick().Gate);Assert.AreEqual(1000,f.Native.Materials);
            Assert.AreEqual(0,f.Native.Requests.Length);
            using var logistics=new Fixture(ground:false,truck:false);
            for(int n=0;n<2;n++)
            {
                var actor=logistics.Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth));
                logistics.Em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="construction.attempt",FactionId=2,StableObjectId=new FixedString64Bytes("tanker."+n)});
                logistics.Em.SetComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Tanker,Category=SkirmishPopulationCategory.LogisticsSupport});
                logistics.Em.SetComponentData(actor,new UnitHealth{Current=160,Max=160});
            }
            Assert.AreEqual(SkirmishEnemyEconomicGate.CurrentRoleBlocked,logistics.Tick().Gate);
            Assert.AreEqual(1000,logistics.Native.Materials);Assert.AreEqual(0,logistics.Native.Requests.Length);
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishEnemyEconomicCoordinatorTests();
            t.PendingConstructionBlocksSpendsAndReconcileSettlesBeforeStrategicProbe();t.FeasibleUrgentDefenderBeatsOriginalSupplyRecovery();
            t.MissingWantedProducerUsesSinglePaidNativeConstructionBeforeResearchOrUnits();t.MissingLogisticsProducerHasPriorityOverOriginalSupplyFacility();
            t.OnlyOriginalOwnMissingSupplyFacilitiesAreRecoveredWithoutExpansion();t.UnknownTimingAndClosedNativeWindowPreventConstructionWithoutInventedRates();
            t.FullArsenalFitIncludesBothNativeReadinessStepsAndDoesNotUnlockState();t.FullBankMissingProducerRebuildSettlesOldDebtWithoutOtherFeasiblePurchases();
            t.ExistingActualResearchAndUnitReceiptsBlockSecondPaidIntentEvenDuringDefense();
            t.PendingPaidDeliveryReconcilesThroughNativeAdoptionBeforeNextAdmission();t.KnownFullNativeCapacityPreventsInvestingInUnusableProducer();
            Debug.Log("[SkirmishEnemyEconomicCoordinatorTests] result=Passed tests=11 scope=IsolatedActualCoordinatorNotNormalMission");
        }
    }
}
