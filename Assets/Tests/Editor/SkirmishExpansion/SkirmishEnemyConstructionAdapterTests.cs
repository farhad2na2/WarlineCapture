using System;
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
    // Real adapter/native request and paid utility in disposable Worlds; no claim
    // of real geometry/prefab delivery or a normal-input mission playthrough.
    public sealed class SkirmishEnemyConstructionAdapterTests
    {
        static void WithConstructionLedger(Action<System.Collections.Generic.List<SkirmishConstructionLedger.Observation>> body)
        {
            Assert.IsNull(SkirmishConstructionLedger.Observer);
            var rows=new System.Collections.Generic.List<SkirmishConstructionLedger.Observation>();
            Action<SkirmishConstructionLedger.Observation> sink=rows.Add;SkirmishConstructionLedger.Observer=sink;
            try{body(rows);}finally{if(SkirmishConstructionLedger.Observer==sink)SkirmishConstructionLedger.Observer=null;}
        }
        [Test] public void ConstructionLedgerPendingDoesNotEmitFrameRows()
        {
            WithConstructionLedger(rows=>{
                RequestPaysOnceAndNeverTreatsPendingAsDelivery();
                Assert.AreEqual(1,rows.Count);Assert.AreEqual("EnemyRequest",rows[0].Operation);
                Assert.AreEqual(1000,rows[0].Before.Bank.Current);Assert.AreEqual(800,rows[0].After.Bank.Current);
                Assert.IsTrue(rows[0].After.HasRequest);Assert.AreEqual(rows[0].TransactionId,rows[0].After.EnemyIntent.RequestId);
            });
        }
        [Test] public void ConstructionLedgerFailureDebtRefundOnce()
        {
            WithConstructionLedger(rows=>{
                using var f=new Fixture();Assert.IsTrue(f.Request());f.Failed();
                SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
                CollectionAssert.AreEqual(new[]{"EnemyRequest","EnemyRefundDebt","EnemyRefund"},rows.ConvertAll(x=>x.Operation));
                Assert.AreEqual(1000,f.Materials);
                SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(3,rows.Count);
            });
        }
        [Test] public void ConstructionLedgerDeliveryAndContinueAreNotNewDebits()
        {
            WithConstructionLedger(rows=>{
                using(var f=new Fixture()){
                    Assert.IsTrue(f.Request());var actor=f.Succeeded();
                    Assert.AreEqual("EnemyDelivered",rows[1].Operation);Assert.AreEqual(actor,rows[1].Actor);
                    Assert.IsTrue(rows[1].HasActorOwner);Assert.AreEqual(800,f.Materials);
                    Assert.AreEqual(rows[1].Before.Bank.Current,rows[1].After.Bank.Current);
                }
                rows.Clear();ContinueRecreatesOnlyPersistedPaidRequestWithoutSecondDebit();
                CollectionAssert.AreEqual(new[]{"EnemyRequest","EnemyReenqueue"},rows.ConvertAll(x=>x.Operation));
                Assert.AreEqual(rows[1].Before.Bank.Current,rows[1].After.Bank.Current);
            });
        }
        [Test] public void ConstructionLedgerDebtReplacementRetainsActualChecks()
        {
            WithConstructionLedger(rows=>{
                FullBankDebtAdmitsLegalNextConstructionAndSettlesExactlyOnce();
                Assert.IsTrue(rows.Exists(x=>x.Operation=="EnemyRefundDebt"));
                Assert.GreaterOrEqual(rows.FindAll(x=>x.Operation=="EnemyRequest").Count,2);
                rows.Clear();FailedDebtReplacementEnqueuePreservesOriginalDebtBankAndRows();
                Assert.AreEqual(1,rows.FindAll(x=>x.Operation=="EnemyRequest").Count);
            });
        }
        [Test] public void ConstructionLedgerSinkFailureDoesNotChangeEnemyDecision()
        {
            Assert.IsNull(SkirmishConstructionLedger.Observer);long faults=SkirmishConstructionLedger.Faults;
            Action<SkirmishConstructionLedger.Observation> sink=_=>throw new InvalidOperationException("injected observer failure");
            SkirmishConstructionLedger.Observer=sink;
            try{RequestPaysOnceAndNeverTreatsPendingAsDelivery();Assert.Greater(SkirmishConstructionLedger.Faults,faults);}
            finally{if(SkirmishConstructionLedger.Observer==sink)SkirmishConstructionLedger.Observer=null;}
            RequestPaysOnceAndNeverTreatsPendingAsDelivery();
        }
        public static void RunConstructionLedgerEnemyControls()
        {
            var t=new SkirmishEnemyConstructionAdapterTests();t.ConstructionLedgerPendingDoesNotEmitFrameRows();
            t.ConstructionLedgerFailureDebtRefundOnce();t.ConstructionLedgerDeliveryAndContinueAreNotNewDebits();
            t.ConstructionLedgerDebtReplacementRetainsActualChecks();t.ConstructionLedgerSinkFailureDoesNotChangeEnemyDecision();
            Debug.Log("[ConstructionLedgerEnemyControls] result=Passed controls=5 nativeGeometryMeasured=0 paidResourcesQualified=0");
        }

        public static void RunFocusedValidation()
        {
            var t=new SkirmishEnemyConstructionAdapterTests();
            t.RequestPaysOnceAndNeverTreatsPendingAsDelivery();
            t.NativeFailureRefundsOnceAndNextQuoteAdvancesCandidate();
            t.PausedPendingRequestCannotSpawnOrRefund();
            t.StaleDeletedOwnerKeepsTypedProvenanceAndIsRejected();
            t.ExactResultAdoptsWithoutObjectivePromotionAndSurvivesCheckpoint();
            t.ContinueRecreatesOnlyPersistedPaidRequestWithoutSecondDebit();
            t.PaidUnitResearchAndReserveAdmissionUseRealState();
            t.NativeEffectiveRectangleMustFitRotatedPad();
            t.LiveDeadProducerWitnessSurvivesDespawnAndOnlyDeliveredRebuildQualifies();
            t.CleanupCancelsExactRequestAndRefundsOnce();
            t.CaptureNormalizesFailedRequestWithoutChangingPausedStock();
            t.UnknownPhaseAndActorOwnedConstructionCheckpointFailClosed();
            t.PermanentInvalidPolicyIsRejectedWithoutIndefiniteDefer();
            t.CompilerCopiesOwnAndNeutralPadsAndPinsTheirIdentity();
            t.FullBankRefundDebtAllowsGenuinePaidSpendAndExactOnceSettlement();
            t.UnchargedEnqueueFailureLeavesValidIdleCheckpointPart();
            t.FullBankDebtAdmitsLegalNextConstructionAndSettlesExactlyOnce();
            t.FailedDebtReplacementEnqueuePreservesOriginalDebtBankAndRows();
            Debug.Log("[SkirmishEnemyConstructionAdapterTests] result=Passed tests=18 scope=IsolatedNativeBoundaryNotNormalMission");
        }
        [Serializable]sealed class SavedParts { public SkirmishNativeCheckpointParts.Part[] Parts; }
        internal sealed class Fixture:IDisposable
        {
            public readonly World World=new World("EnemyConstructionBoundary");
            public EntityManager Em=>World.EntityManager;
            public readonly Entity Session,Boundary,Bank;
            public readonly SkirmishResolvedSetup Setup;
            public Fixture(bool extraPads=false)
            {
                Setup=new SkirmishResolvedSetup{ObjectiveKind=SkirmishObjectiveKind.BaseAssault,EnemyFaction=2,
                    DifficultyId=SkirmishDifficultyId.Regular,Readiness=SkirmishReadinessStage.Field};
                var layout=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
                try
                {
                    // Disposable logical fixture only; exercise the public compiler.
                    typeof(SkirmishMapLayoutConfig).GetField("pads",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                        .SetValue(layout,extraPads?new[]{
                            new SkirmishLegalPadConfig{PadId="own.barracks",Kind=SkirmishLegalPadKind.BaseBarracks,FactionId=2,CenterX=50,CenterZ=50,WidthMetres=40,DepthMetres=40},
                            new SkirmishLegalPadConfig{PadId="own.ground",Kind=SkirmishLegalPadKind.GroundStaging,FactionId=2,CenterX=80,CenterZ=20,WidthMetres=20,DepthMetres=20},
                            new SkirmishLegalPadConfig{PadId="own.supply",Kind=SkirmishLegalPadKind.Supply,FactionId=2,CenterX=80,CenterZ=60,WidthMetres=20,DepthMetres=20}}
                            :new[]{new SkirmishLegalPadConfig{PadId="own.barracks",Kind=SkirmishLegalPadKind.BaseBarracks,FactionId=2,
                            CenterX=50,CenterZ=50,WidthMetres=40,DepthMetres=40}});
                    Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(Setup,layout,out var policy));Setup.EnemyPolicy=policy;
                }
                finally{UnityEngine.Object.DestroyImmediate(layout);}
                Session=Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),typeof(SkirmishSharedMaterialsInitialized));
                Em.SetComponentData(Session,new SkirmishExpandedSessionComponent{SessionId="construction.attempt",Phase=SkirmishSessionPhase.Playing,SpawnComplete=1});
                Em.SetComponentData(Session,new SkirmishObjectiveClockComponent{Playing=1});
                Em.AddComponentObject(Session,new SkirmishResolvedSetupRecord{Setup=Setup});
                Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(Em,Session,Setup));
                Em.AddBuffer<SkirmishProductionReservation>(Session);Em.AddBuffer<SkirmishResearchQueueItem>(Session);
                Boundary=Em.CreateEntity(typeof(BuildingRuntimeStateTag));Em.AddBuffer<BuildingRuntimeSpawnRequest>(Boundary);
                var catalog=Em.AddBuffer<BuildingConfiguredSpawnableReadModel>(Boundary);
                catalog.Add(new BuildingConfiguredSpawnableReadModel{BuildingId=new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Barrack")),CanRequest=1,MaterialsCost=200,FootprintCells=new int2(4,4)});
                Bank=Em.CreateEntity(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
                Em.SetComponentData(Bank,new FactionEconomy{FactionId=2,MaterialsOnlyConstruction=1});
                Em.SetComponentData(Bank,new FactionTacticalMaterialsComponent{FactionId=2,Current=1000,Capacity=1200});
                var grid=Em.CreateEntity(typeof(GridConfig));Em.SetComponentData(grid,new GridConfig{Width=100,Height=100,CellSize=1,Origin=float3.zero});
            }
            public SkirmishEnemyConstructionIntentV1Component Intent=>Em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(Session);
            public int Materials=>Em.GetComponentData<FactionTacticalMaterialsComponent>(Bank).Current;
            public DynamicBuffer<BuildingRuntimeSpawnRequest> Requests=>Em.GetBuffer<BuildingRuntimeSpawnRequest>(Boundary);
            public SkirmishEnemyConstructionAdapter.Quote Quote(int reserve=0)
            {Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryPrepare(Em,Session,SkirmishStructureIds.Barracks,reserve,out var q));return q;}
            public bool Request(string action="build.1")
            {var q=Quote();return SkirmishEnemyConstructionAdapter.TryRequest(Em,Session,q,action,0);}
            public void At(float time,bool paused=false)
            {var c=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);c.ElapsedSeconds=time;c.Paused=paused?(byte)1:(byte)0;Em.SetComponentData(Session,c);}
            public void Failed()
            {var b=Requests;var r=b[0];r.Status=BuildingRuntimeSpawnRequest.Failed;r.ResultCode=BuildingRuntimeSpawnRequest.Blocked;b[0]=r;}
            public Entity Building(int runtimeId=71,string stable=null,int health=800)
            {
                var a=Em.CreateEntity(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth),typeof(UnitSourcePrefabKey));
                Em.SetComponentData(a,new RuntimeBuildingCombatInfo{RuntimeBuildingId=runtimeId,OwnerFactionId=2,OriginCell=Intent.Origin,FootprintCells=Intent.Footprint});
                Em.SetComponentData(a,new UnitHealth{Current=health,Max=800});Em.SetComponentData(a,new UnitSourcePrefabKey{Value="Building_Barrack"});
                if(stable!=null){SkirmishScenarioSpawnSystem.BindStructure(Em,a,"construction.attempt",new SkirmishResolvedStructureEntry{FactionId=2,StructureId=SkirmishStructureIds.Barracks});
                    var o=Em.GetComponentData<SkirmishAttemptOwnedComponent>(a);o.StableObjectId=stable;Em.SetComponentData(a,o);}
                return a;
            }
            public Entity Succeeded()
            {
                var a=Building();var b=Requests;var r=b[0];r.Status=BuildingRuntimeSpawnRequest.Succeeded;r.BuildingRuntimeId=71;r.ActualOrigin=r.PreferredOrigin;r.ActualFootprint=Intent.Footprint;b[0]=r;
                SkirmishEnemyConstructionAdapter.RecordNativeResult(Em,r);return a;
            }
            public void Dispose()=>World.Dispose();
        }
        [Test]public void RequestPaysOnceAndNeverTreatsPendingAsDelivery()
        {
            using var f=new Fixture();Assert.IsTrue(f.Request());Assert.AreEqual(800,f.Materials);
            Assert.AreEqual(200,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).LifetimeConstructionSpent);
            Assert.AreEqual(new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Barrack")),f.Requests[0].BuildingId);
            Assert.AreEqual(new FixedString128Bytes("Building_Barrack"),f.Intent.BuildingId);
            var first=f.Intent;for(int n=0;n<20;n++){f.At(n);SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);}
            Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(first.ActionKey,f.Intent.ActionKey);Assert.AreEqual(first.RequestId,f.Intent.RequestId);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.PaidPending,f.Intent.Phase);Assert.AreEqual(800,f.Materials);
            Assert.AreEqual(BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction,f.Requests[0].RequestKind);
            Assert.AreEqual(1,f.Requests[0].RequirePreferredOrigin);Assert.AreEqual(0,f.Requests[0].AuthoredStartingGrant);Assert.AreEqual(0,f.Requests[0].AllowNonBuildableEnemy);
        }
        [Test]public void NativeFailureRefundsOnceAndNextQuoteAdvancesCandidate()
        {
            using var f=new Fixture();f.Request();var first=f.Intent;f.Failed();SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
            Assert.AreEqual(1000,f.Materials);Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.Rejected,f.Intent.Phase);
            SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(1000,f.Materials);
            var next=f.Quote();Assert.Greater(next.CandidateIndex,first.CandidateIndex);Assert.IsFalse(math.all(next.Origin==first.Origin));
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,next,"build.2",0));
            f.At(4);Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,next,"build.2",0));Assert.AreEqual(800,f.Materials);
        }
        [Test]public void PausedPendingRequestCannotSpawnOrRefund()
        {
            using var f=new Fixture();f.Request();var r=f.Requests[0];f.At(1,true);
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.CanProcess(f.Em,r,out bool deferred));Assert.IsTrue(deferred);
            f.Failed();SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(800,f.Materials);Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.PaidPending,f.Intent.Phase);
            f.At(1);SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(1000,f.Materials);
        }
        [Test]public void StaleDeletedOwnerKeepsTypedProvenanceAndIsRejected()
        {
            using var f=new Fixture();f.Request();var r=f.Requests[0];f.Em.DestroyEntity(f.Session);
            Assert.IsTrue(SkirmishEnemyConstructionAdapter.IsConstructionRequest(r));Assert.IsFalse(SkirmishEnemyConstructionAdapter.CanProcess(f.Em,r,out var defer));Assert.IsFalse(defer);
            r.RequestKind=BuildingRuntimeSpawnRequest.KindBuilding;Assert.IsFalse(SkirmishEnemyConstructionAdapter.IsConstructionRequest(r));
        }
        [Test]public void ExactResultAdoptsWithoutObjectivePromotionAndSurvivesCheckpoint()
        {
            using var f=new Fixture();f.Request();var unrelated=f.Requests[0];unrelated.RequestId+=10;unrelated.RequestKind=BuildingRuntimeSpawnRequest.KindBuilding;f.Requests.Add(unrelated);
            var actor=f.Succeeded();
            // Native result adoption has structurally changed the World; fresh
            // boundary handles must retain the completed and unrelated rows.
            Assert.AreEqual(2,f.Requests.Length);Assert.AreEqual(unrelated.RequestId,f.Requests[1].RequestId);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.Delivered,f.Intent.Phase);
            Assert.AreEqual(new FixedString64Bytes("constructed.71"),f.Intent.ResultStableObjectId);
            Assert.IsTrue(f.Em.HasComponent<SkirmishSharedActorTag>(actor));Assert.IsFalse(f.Em.HasComponent<SkirmishObjectiveRoleComponent>(actor));
            var parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(parts,out SkirmishEnemyConstructionIntentV1Component saved));
            Assert.AreEqual(f.Intent.ResultStableObjectId,saved.ResultStableObjectId);
            parts=JsonUtility.FromJson<SavedParts>(JsonUtility.ToJson(new SavedParts{Parts=parts})).Parts;
            var fresh=f.Em.CreateEntity();SkirmishNativeCheckpointParts.Apply(f.Em,fresh,parts,_=>Entity.Null);
            Assert.AreEqual(saved,f.Em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(fresh));
            SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(unrelated.RequestId,f.Requests[0].RequestId);Assert.AreEqual(800,f.Materials);
        }
        [Test]public void ContinueRecreatesOnlyPersistedPaidRequestWithoutSecondDebit()
        {
            using var f=new Fixture();f.Request();var saved=f.Intent;f.Requests.Clear();
            SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(800,f.Materials);
            Assert.AreEqual(saved.ActionKey,f.Intent.ActionKey);Assert.AreEqual(saved.MaterialsPaid,f.Intent.MaterialsPaid);
            Assert.AreEqual(BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction,f.Requests[0].RequestKind);
        }
        [Test]public void PaidUnitResearchAndReserveAdmissionUseRealState()
        {
            using var f=new Fixture();Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryPrepare(f.Em,f.Session,SkirmishStructureIds.Barracks,801,out _));
            var unit=f.Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(f.Session);unit.State=SkirmishEnemyProductionIntentState.RequestPending;f.Em.SetComponentData(f.Session,unit);
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryPrepare(f.Em,f.Session,SkirmishStructureIds.Barracks,0,out _));
            unit.State=SkirmishEnemyProductionIntentState.Backoff;f.Em.SetComponentData(f.Session,unit);Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryPrepare(f.Em,f.Session,SkirmishStructureIds.Barracks,0,out _));
            f.Em.GetBuffer<SkirmishResearchQueueItem>(f.Session).Add(new SkirmishResearchQueueItem{ResearchId=1,FactionId=2,Kind=SkirmishResearchKind.Readiness,Phase=SkirmishResearchPhase.Researching});
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryPrepare(f.Em,f.Session,SkirmishStructureIds.Barracks,0,out _));Assert.AreEqual(1000,f.Materials);
        }
        [Test]public void NativeEffectiveRectangleMustFitRotatedPad()
        {
            var pad=new SkirmishEnemyConstructionPad("rotated",SkirmishLegalPadKind.Service,2,50,50,10,10,-10,10);
            var grid=new GridConfig{Width=100,Height=100,CellSize=1,Origin=float3.zero};
            Assert.IsTrue(SkirmishEnemyConstructionAdapter.FitsPad(pad,grid,new int2(48,48),new int2(4,4)));
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.FitsPad(pad,grid,new int2(30,30),new int2(4,4)));
            using var f=new Fixture();f.Request();Assert.IsFalse(SkirmishEnemyConstructionAdapter.NativeRectFits(f.Em,f.Requests[0],new int2(29,29),new int2(50,50)));
        }
        [Test]public void LiveDeadProducerWitnessSurvivesDespawnAndOnlyDeliveredRebuildQualifies()
        {
            using var f=new Fixture();f.Request();var old=f.Building(17,"old.barracks",0);
            f.Em.GetBuffer<SkirmishResearchQueueItem>(f.Session).Add(new SkirmishResearchQueueItem{ResearchId=9,FactionId=2,Kind=SkirmishResearchKind.Readiness,Producer=SkirmishProducerKind.Barracks,Phase=SkirmishResearchPhase.Lost});
            SkirmishEnemyConstructionAdapter.RecordProducerLoss(f.Em,f.Session,old);f.Em.DestroyEntity(old);
            Assert.AreEqual(new FixedString64Bytes("old.barracks"),f.Intent.PreviousProducerStableId);
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryReadResearchRecoveryEvidence(f.Em,f.Session,out _,out _,out _,out _));
            f.Succeeded();Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryReadResearchRecoveryEvidence(f.Em,f.Session,out var attempt,out var id,out var previous,out var rebuilt));
            Assert.AreEqual(new FixedString64Bytes("construction.attempt"),attempt);Assert.AreEqual(9u,id);Assert.AreNotEqual(previous,rebuilt);
        }
        [Test]public void CleanupCancelsExactRequestAndRefundsOnce()
        {
            using var f=new Fixture();f.Request();var other=f.Requests[0];other.RequestKind=BuildingRuntimeSpawnRequest.KindBuilding;f.Requests.Add(other);
            SkirmishEnemyConstructionAdapter.CleanupAttempt(f.Em,"construction.attempt");Assert.AreEqual(1000,f.Materials);Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(other.RequestId,f.Requests[0].RequestId);
            SkirmishEnemyConstructionAdapter.CleanupAttempt(f.Em,"construction.attempt");Assert.AreEqual(1000,f.Materials);
        }
        [Test]public void CaptureNormalizesFailedRequestWithoutChangingPausedStock()
        {
            using var f=new Fixture();f.Request();f.Failed();f.At(1,true);var parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(parts,out SkirmishEnemyConstructionIntentV1Component saved));
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.RefundPending,saved.Phase);Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.PaidPending,f.Intent.Phase);Assert.AreEqual(800,f.Materials);
        }
        [Test]public void FullBankRefundDebtAllowsGenuinePaidSpendAndExactOnceSettlement()
        {
            using var f=new Fixture();f.Request();
            var materials=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,materials.Capacity-materials.Current,FactionTacticalMaterialsSourceKind.Fabrication));
            f.Em.SetComponentData(f.Bank,materials);f.Failed();SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.RefundPending,f.Intent.Phase);Assert.AreEqual(200,f.Intent.MaterialsPaid);
            Assert.AreEqual(1200,f.Materials);Assert.IsTrue(SkirmishEnemyConstructionAdapter.IsOutstanding(f.Em,f.Session));
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.BlocksOtherPaidActions(f.Em,f.Session));
            Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryPrepare(f.Em,f.Session,SkirmishStructureIds.Barracks,0,out _));
            // Exact real production spend utility, using the authored existing tank price.
            Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(SkirmishRoleOverlayCatalog.CreateS002GroundSlice(),SkirmishRoleKind.Tank,out var role));
            materials=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);int cost=role.MaterialsCost;Assert.GreaterOrEqual(cost,200);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TrySpend(ref materials,cost,FactionTacticalMaterialsSpendKind.Production));
            f.Em.SetComponentData(f.Bank,materials);SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
            Assert.AreEqual(1200-cost+200,f.Materials);Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.Rejected,f.Intent.Phase);
            Assert.AreEqual(0,f.Intent.MaterialsPaid);Assert.AreEqual(cost,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).LifetimeProductionSpent);
            Assert.AreEqual(0,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).LifetimeConstructionSpent);
            uint version=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).Version;
            SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(version,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).Version);
        }
        [Test]public void FullBankDebtAdmitsLegalNextConstructionAndSettlesExactlyOnce()
        {
            using var f=new Fixture();f.Request();var first=f.Intent;
            var materials=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref materials,materials.Capacity-materials.Current,FactionTacticalMaterialsSourceKind.Fabrication));
            f.Em.SetComponentData(f.Bank,materials);f.Failed();SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.RefundPending,f.Intent.Phase);
            var before=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);var q=f.Quote();
            Assert.Greater(q.CandidateIndex,first.CandidateIndex);Assert.IsFalse(math.all(q.Origin==first.Origin));
            Assert.IsTrue(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,q,"debt.replacement",1));
            var after=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            Assert.AreEqual(1200,after.Current);Assert.AreEqual(200,after.LifetimeConstructionSpent);Assert.AreEqual(before.Version+2,after.Version);
            Assert.AreEqual((byte)SkirmishEnemyConstructionPhase.PaidPending,f.Intent.Phase);Assert.AreEqual(200,f.Intent.MaterialsPaid);
            Assert.AreEqual(new FixedString64Bytes("debt.replacement"),f.Intent.ActionKey);Assert.AreEqual(1,f.Requests.Length);
            Assert.AreEqual(f.Intent.RequestId,f.Requests[0].RequestId);Assert.AreEqual(q.Origin,f.Requests[0].PreferredOrigin);
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,q,"another",2));
            SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);Assert.AreEqual(after,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank));
        }
        [Test]public void FailedDebtReplacementEnqueuePreservesOriginalDebtBankAndRows()
        {
            using var f=new Fixture();f.Request();var m=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TryGrant(
                ref m,m.Capacity-m.Current,FactionTacticalMaterialsSourceKind.Fabrication));
            f.Em.SetComponentData(f.Bank,m);f.Failed();SkirmishEnemyConstructionAdapter.Tick(f.Em,f.Session);
            var debt=f.Intent;var before=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
            var economy=f.Em.GetComponentData<FactionEconomy>(f.Bank);var q=f.Quote();
            f.Requests.Add(new BuildingRuntimeSpawnRequest{RequestId=int.MaxValue,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding});
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,q,"cannot.enqueue",1));
            Assert.AreEqual(debt,f.Intent);Assert.AreEqual(before,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank));
            Assert.AreEqual(economy,f.Em.GetComponentData<FactionEconomy>(f.Bank));Assert.AreEqual(1,f.Requests.Length);
            Assert.AreEqual(int.MaxValue,f.Requests[0].RequestId);Assert.AreEqual(BuildingRuntimeSpawnRequest.KindBuilding,f.Requests[0].RequestKind);
        }
        [Test]public void UnchargedEnqueueFailureLeavesValidIdleCheckpointPart()
        {
            using var f=new Fixture();var q=f.Quote();f.Requests.Add(new BuildingRuntimeSpawnRequest{RequestId=int.MaxValue,RequestKind=BuildingRuntimeSpawnRequest.KindBuilding});
            Assert.IsFalse(SkirmishEnemyConstructionAdapter.TryRequest(f.Em,f.Session,q,"failed.admission",0));Assert.AreEqual(1000,f.Materials);
            var parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            var snapshot=new SkirmishNativeCheckpointSnapshotService.Snapshot{SessionId="construction.attempt",SessionParts=parts,
                Actors=Array.Empty<SkirmishNativeCheckpointSnapshotService.Actor>(),MapReferences=Array.Empty<SkirmishNativeCheckpointSnapshotService.MapReference>(),Banks=Array.Empty<SkirmishNativeCheckpointSnapshotService.Bank>()};
            Assert.IsTrue(SkirmishEnemyConstructionCheckpoint.Validate(snapshot,f.Setup));Assert.AreEqual(1,f.Requests.Length);
        }
        [Test]public void PermanentInvalidPolicyIsRejectedWithoutIndefiniteDefer()
        {
            using var f=new Fixture();f.Request();var row=f.Requests[0];
            Assert.IsTrue(SkirmishEnemyConstructionAdapter.CanProcess(f.Em,row,out var deferred));Assert.IsFalse(deferred);
            f.Setup.EnemyPolicy=null;Assert.IsFalse(SkirmishEnemyConstructionAdapter.CanProcess(f.Em,row,out deferred));Assert.IsFalse(deferred);
        }
        [Test]public void CompilerCopiesOwnAndNeutralPadsAndPinsTheirIdentity()
        {
            var layout=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                var pads=new[]{
                    new SkirmishLegalPadConfig{PadId="enemy",FactionId=2,Kind=SkirmishLegalPadKind.Service,CenterX=50,CenterZ=50,WidthMetres=20,DepthMetres=20},
                    new SkirmishLegalPadConfig{PadId="neutral",FactionId=0,Kind=SkirmishLegalPadKind.Supply,CenterX=80,CenterZ=50,WidthMetres=20,DepthMetres=20},
                    new SkirmishLegalPadConfig{PadId="player",FactionId=1,Kind=SkirmishLegalPadKind.AirReturn,CenterX=10,CenterZ=50,WidthMetres=20,DepthMetres=20}};
                var field=typeof(SkirmishMapLayoutConfig).GetField("pads",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);field.SetValue(layout,pads);
                var setup=new SkirmishResolvedSetup{ObjectiveKind=SkirmishObjectiveKind.BaseAssault,EnemyFaction=2,DifficultyId=SkirmishDifficultyId.Regular,Readiness=SkirmishReadinessStage.Field};
                Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup,layout,out var first));Assert.AreEqual(2,first.ConstructionPads.Count);Assert.AreEqual(2,first.Version);
                pads[0].CenterX=55;Assert.IsTrue(SkirmishEnemyPolicyCompiler.TryCompile(setup,layout,out var second));
                Assert.AreEqual(50,first.ConstructionPads[0].X);Assert.AreEqual(55,second.ConstructionPads[0].X);Assert.AreNotEqual(first.Fingerprint,second.Fingerprint);
            }
            finally{UnityEngine.Object.DestroyImmediate(layout);}
        }
        [Test]public void UnknownPhaseAndActorOwnedConstructionCheckpointFailClosed()
        {
            using var f=new Fixture();f.Request();var parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            var snapshot=new SkirmishNativeCheckpointSnapshotService.Snapshot{SessionId="construction.attempt",SessionParts=parts,
                Actors=Array.Empty<SkirmishNativeCheckpointSnapshotService.Actor>(),MapReferences=Array.Empty<SkirmishNativeCheckpointSnapshotService.MapReference>(),Banks=Array.Empty<SkirmishNativeCheckpointSnapshotService.Bank>()};
            Assert.IsTrue(SkirmishEnemyConstructionCheckpoint.Validate(snapshot,f.Setup));
            var value=f.Intent;value.Phase=255;f.Em.SetComponentData(f.Session,value);snapshot.SessionParts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            Assert.IsFalse(SkirmishEnemyConstructionCheckpoint.Validate(snapshot,f.Setup));
            value.Phase=(byte)SkirmishEnemyConstructionPhase.PaidPending;f.Em.SetComponentData(f.Session,value);
            snapshot.SessionParts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null);
            var impostor=f.Em.CreateEntity(typeof(SkirmishEnemyConstructionIntentV1Component));f.Em.SetComponentData(impostor,value);
            snapshot.Actors=new[]{new SkirmishNativeCheckpointSnapshotService.Actor{Parts=SkirmishNativeCheckpointParts.Capture(f.Em,impostor,_=>null)}};
            Assert.IsFalse(SkirmishEnemyConstructionCheckpoint.Validate(snapshot,f.Setup));
        }
    }
}
