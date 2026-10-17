using System;
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
    // Disposable ECS records exercise the real adapter. These are correlation and
    // transaction-boundary regressions, not a normal-input paid mission proof.
    public sealed class SkirmishEnemyPaidProductionAdapterTests
    {
        public static void RunFocusedValidation()
        {
            var t=new SkirmishEnemyPaidProductionAdapterTests();
            t.OnePendingIntentIsNotFailureOrReplacement();
            t.AcceptedReceiptIsNotDeliveryAndNeverRequeues();
            t.ExactCallbackRejectsWrongFactionRoleTokenOwnerAttempt();
            t.UnrelatedCompletedRowsRemainUntouched();
            t.SuccessWithoutAtomicWitnessAbandonsWithoutRepeatPayment();
            t.ThreeIdenticalNativeRejectsBackOffAndStop();
            t.ChangedNativeRejectResetsIdenticalBudget();
            t.FeasibilityRejectionsUseActualSessionArmyAndStocks();
            t.PauseAndInactiveSimulationFreezeRetry();
            t.MissingTransientPendingRowReusesPersistedToken();
            t.ExactGroupCohortAndArrivalAreRequired();
            t.DisabledBoardedMembersStillBelongToDeliveredCohort();
            t.DuplicateStableMemberFailsClosed();
            t.LostReceiptDoesNotReplaceUntilNewAction();
            t.DuplicateRequestAndMissingReceiptFailClosed();
            t.LegacyOrMissingIntentNeverDispatches();
            t.PureDispatchGuardRetainsCapacityAndQueueLocksAcrossUnrelatedIncome();
            Debug.Log("[SkirmishEnemyPaidProductionAdapterTests] result=Passed tests=17");
        }
        sealed class Fixture:IDisposable
        {
            public readonly World World=new World("EnemyPaidProductionIntent");
            public EntityManager Em=>World.EntityManager;
            public readonly Entity Session,Boundary,Producer;
            public readonly SkirmishArmyProfileConfig Army=SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.AirMobile);
            public Fixture(bool producer=true)
            {
                Session=Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent),
                    typeof(SkirmishEnemyProductionIntentV1Component),typeof(SkirmishEnemyStockComponent),typeof(SkirmishEnemyCapacityComponent));
                Em.SetComponentData(Session,new SkirmishExpandedSessionComponent {SessionId="paid.intent.attempt",Phase=SkirmishSessionPhase.Playing,SpawnComplete=1});
                Em.SetComponentData(Session,new SkirmishObjectiveClockComponent {Playing=1});
                Em.SetComponentData(Session,new SkirmishEnemyProductionIntentV1Component {Version=1,NextRequestId=1});
                Em.SetComponentData(Session,new SkirmishEnemyStockComponent {Materials=1800,Fuel=700});
                Em.SetComponentData(Session,new SkirmishEnemyCapacityComponent {InfantryCap=48,GroundCap=8,AirCap=2,SupplyCap=100});
                Em.AddComponentObject(Session,new SkirmishResolvedSetupRecord {Setup=new SkirmishResolvedSetup {
                    ArmyProfileId=SkirmishArmyProfileId.AirMobile,Readiness=SkirmishReadinessStage.Field,
                    RoleOverlays=SkirmishRoleOverlayCatalog.CreateAirMobileSlice(),LogisticsSupportCapEach=2 }});
                Em.AddBuffer<SkirmishProductionReservation>(Session);
                Boundary=Em.CreateEntity(typeof(BuildingRuntimeStateTag));Em.AddBuffer<BuildingFactionUnitProductionRequest>(Boundary);
                if(producer)
                {
                    Producer=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth));
                    Em.SetComponentData(Producer,new SkirmishAttemptOwnedComponent {SessionId="paid.intent.attempt",StableObjectId="enemy.barracks",FactionId=2,IsStructure=1});
                    Em.SetComponentData(Producer,new SkirmishStructureIdentityComponent {Producer=SkirmishProducerKind.Barracks});
                    Em.SetComponentData(Producer,new RuntimeBuildingCombatInfo {RuntimeBuildingId=77,OwnerFactionId=2});
                    Em.SetComponentData(Producer,new UnitHealth {Current=800});
                }
            }
            public SkirmishEnemyProductionIntentV1Component Intent=>Em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(Session);
            public DynamicBuffer<BuildingFactionUnitProductionRequest> Requests=>Em.GetBuffer<BuildingFactionUnitProductionRequest>(Boundary);
            public SkirmishEnemyProductionIntentState Request(string key="action.1",SkirmishRoleKind role=SkirmishRoleKind.Rifle)
                =>SkirmishEnemyPaidProductionAdapter.TryRequest(Em,Session,key,role,1,Army,Now);
            public float Now=>Em.GetComponentData<SkirmishObjectiveClockComponent>(Session).ElapsedSeconds;
            public SkirmishEnemyProductionIntentState Tick(float now)
            {var c=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);c.ElapsedSeconds=now;Em.SetComponentData(Session,c);return SkirmishEnemyPaidProductionAdapter.Tick(Em,Session,Army,now);}
            public void Status(byte status,byte code=0)
            {var buffer=Requests;var row=buffer[0];row.Status=status;row.ResultCode=code;buffer[0]=row;}
            public void AddReceipt()
            {
                Em.GetBuffer<SkirmishProductionReservation>(Session).Add(new SkirmishProductionReservation {
                    ReservationId=12,FactionId=2,Role=SkirmishRoleKind.Rifle,Category=SkirmishPopulationCategory.Infantry,
                    MemberCount=4,RemainingMembers=4,ProducerRuntimeId=77,Producer=SkirmishProducerKind.Barracks,Phase=SkirmishReservationPhase.Reserved });
            }
            public bool Accept()
            { AddReceipt();var r=Requests[0];return SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(Em,in r,SkirmishRoleKind.Rifle,Session,r.RequestAttempt,12); }
            public void Phase(SkirmishReservationPhase phase,int delivered=0,int remaining=4)
            {var b=Em.GetBuffer<SkirmishProductionReservation>(Session);var r=b[0];r.Phase=phase;r.DeliveredMembers=delivered;r.RemainingMembers=remaining;r.ProductionGroupId=41;b[0]=r;}
            public Entity Member(int ordinal,bool transit=false,uint group=41,string stable=null)
            {
                var a=Em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(SkirmishSharedActorTag),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth));
                Em.SetComponentData(a,new SkirmishAttemptOwnedComponent {SessionId="paid.intent.attempt",StableObjectId=stable??("production.12."+ordinal),FactionId=2,ReservationId=12});
                Em.SetComponentData(a,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.Rifle,Category=SkirmishPopulationCategory.Infantry});
                Em.SetComponentData(a,new SkirmishArmyGroupMembershipComponent {GroupId=group,Domain=SkirmishPopulationCategory.Infantry});
                Em.SetComponentData(a,new UnitHealth {Current=90});if(transit)Em.AddComponent<UnitSpawnTransitTag>(a);return a;
            }
            public void Dispose()=>World.Dispose();
        }
        [Test] public void OnePendingIntentIsNotFailureOrReplacement()
        {
            using var f=new Fixture();Assert.AreEqual(SkirmishEnemyProductionIntentState.RequestPending,f.Request());
            var first=f.Intent;for(int i=1;i<=100;i++){f.Tick(i);f.Request("action.new",SkirmishRoleKind.Gunner);}
            Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(first.RequestId,f.Intent.RequestId);
            Assert.AreEqual(first.ActionKey,f.Intent.ActionKey);Assert.AreEqual(0,f.Intent.IdenticalRejectCount);
            Assert.AreEqual(1800,f.Em.GetComponentData<SkirmishEnemyStockComponent>(f.Session).Materials);
            Assert.AreEqual(0,f.Em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session).InfantryReserved);
        }
        [Test] public void AcceptedReceiptIsNotDeliveryAndNeverRequeues()
        {
            using var f=new Fixture();f.Request();Assert.IsTrue(f.Accept());f.Status(BuildingFactionUnitProductionRequest.Succeeded);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.AcceptedWaiting,f.Tick(1));Assert.AreEqual(0,f.Requests.Length);
            Assert.AreEqual(new FixedString64Bytes("enemy.barracks"),f.Intent.ProducerStableObjectId);
            for(int i=2;i<50;i++)f.Tick(i);Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(12u,f.Intent.ReservationId);
        }
        [Test] public void ExactCallbackRejectsWrongFactionRoleTokenOwnerAttempt()
        {
            using var f=new Fixture();f.Request();var good=f.Requests[0];f.AddReceipt();
            var captured=f.Intent;
            var r=good;r.FactionId=1;Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Rifle,f.Session,r.RequestAttempt,12));
            r=good;r.RequestId++;Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Rifle,f.Session,r.RequestAttempt,12));
            r=good;Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Gunner,f.Session,r.RequestAttempt,12));
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Rifle,Entity.Null,r.RequestAttempt,12));
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Rifle,f.Session,"other.attempt",12));
            Assert.AreEqual(captured.ReservationId,f.Intent.ReservationId);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.RequestPending,f.Intent.State);
            r=good;Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.RecordAcceptedDispatch(f.Em,in r,SkirmishRoleKind.Rifle,f.Session,r.RequestAttempt,12));
        }
        [Test] public void UnrelatedCompletedRowsRemainUntouched()
        {
            using var f=new Fixture();f.Request();var other=f.Requests[0];other.RequestId=19;other.Status=BuildingFactionUnitProductionRequest.Succeeded;f.Requests.Add(other);
            other.RequestId=20;other.RequestAttempt="old.attempt";f.Requests.Add(other);other.RequestId=21;other.FactionId=1;f.Requests.Add(other);
            Assert.IsTrue(f.Accept());f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Tick(1);
            Assert.AreEqual(3,f.Requests.Length);Assert.AreEqual(19,f.Requests[0].RequestId);Assert.AreEqual(20,f.Requests[1].RequestId);Assert.AreEqual(21,f.Requests[2].RequestId);
        }
        [Test] public void SuccessWithoutAtomicWitnessAbandonsWithoutRepeatPayment()
        {
            using var f=new Fixture();f.Request();f.Status(BuildingFactionUnitProductionRequest.Succeeded);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Tick(1));
            for(int i=2;i<20;i++){f.Tick(i);f.Request();}Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(0u,f.Intent.ReservationId);
        }
        [Test] public void ThreeIdenticalNativeRejectsBackOffAndStop()
        {
            using var f=new Fixture();f.Request();f.Status(BuildingFactionUnitProductionRequest.Failed,3);f.Tick(0);
            Assert.AreEqual(1,f.Intent.IdenticalRejectCount);f.Tick(.9f);Assert.AreEqual(0,f.Requests.Length);
            f.Tick(1);f.Status(BuildingFactionUnitProductionRequest.Failed,3);f.Tick(1);Assert.AreEqual(2,f.Intent.IdenticalRejectCount);
            f.Tick(2.9f);Assert.AreEqual(0,f.Requests.Length);f.Tick(3);f.Status(BuildingFactionUnitProductionRequest.Failed,3);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Tick(3));f.Tick(100);Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(3,f.Intent.IdenticalRejectCount);
        }
        [Test] public void ChangedNativeRejectResetsIdenticalBudget()
        {
            using var f=new Fixture();f.Request();f.Status(BuildingFactionUnitProductionRequest.Failed,2);f.Tick(0);
            f.Tick(1);f.Status(BuildingFactionUnitProductionRequest.Failed,3);f.Tick(1);Assert.AreEqual(1,f.Intent.IdenticalRejectCount);
            Assert.AreEqual(3,f.Intent.LastRejectCode);Assert.AreEqual(1,f.Intent.RequestId);Assert.AreEqual(2,f.Intent.NextRequestId);
        }
        [Test] public void FeasibilityRejectionsUseActualSessionArmyAndStocks()
        {
            using var f=new Fixture(false);Assert.AreEqual(SkirmishEnemyProductionIntentState.Backoff,f.Request());
            Assert.AreEqual((ushort)SkirmishReasonCode.MissingProducer,f.Intent.LastRejectCode);f.Tick(1);f.Tick(3);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Intent.State);Assert.AreEqual(0,f.Requests.Length);
            Assert.AreEqual(1800,f.Em.GetComponentData<SkirmishEnemyStockComponent>(f.Session).Materials);
            var setup=f.Em.GetComponentObject<SkirmishResolvedSetupRecord>(f.Session).Setup;setup.ArmyProfileId=SkirmishArmyProfileId.GroundManeuver;
            SkirmishEnemyPaidProductionAdapter.TryRequest(f.Em,f.Session,"offensive",SkirmishRoleKind.Fighter,2,SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.CombinedArms),f.Now);
            Assert.AreEqual((ushort)SkirmishReasonCode.UnsupportedRole,f.Intent.LastRejectCode);Assert.AreEqual(0,f.Requests.Length);
        }
        [Test] public void PauseAndInactiveSimulationFreezeRetry()
        {
            using var f=new Fixture(false);f.Request();var before=f.Intent;var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.Paused=1;f.Em.SetComponentData(f.Session,clock);
            f.Tick(100);Assert.AreEqual(before.IdenticalRejectCount,f.Intent.IdenticalRejectCount);Assert.AreEqual(before.RetryNotBeforeElapsed,f.Intent.RetryNotBeforeElapsed);
            clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.Paused=0;f.Em.SetComponentData(f.Session,clock);
            var gate=f.Em.CreateEntity(typeof(RuntimeGameplayStateComponent));f.Tick(101);Assert.AreEqual(before.IdenticalRejectCount,f.Intent.IdenticalRejectCount);
            f.Em.SetComponentData(gate,new RuntimeGameplayStateComponent {SimulationActive=1});f.Tick(102);Assert.AreEqual(2,f.Intent.IdenticalRejectCount);
        }
        [Test] public void MissingTransientPendingRowReusesPersistedToken()
        {
            using var f=new Fixture();f.Request();var captured=f.Intent;f.Requests.Clear();
            f.Em.SetComponentData(f.Session,captured);Assert.AreEqual(SkirmishEnemyProductionIntentState.RequestPending,f.Tick(10));
            Assert.AreEqual(captured.RequestId,f.Requests[0].RequestId);Assert.AreEqual(captured.NextRequestId,f.Intent.NextRequestId);
            Assert.AreEqual(1,f.Requests.Length);Assert.AreEqual(1800,f.Em.GetComponentData<SkirmishEnemyStockComponent>(f.Session).Materials);
        }
        [Test] public void ExactGroupCohortAndArrivalAreRequired()
        {
            using var f=new Fixture();f.Request();f.Accept();f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Phase(SkirmishReservationPhase.Live,4);
            f.Member(0);f.Member(1);f.Member(2);var last=f.Member(3,true);f.Member(9,false,99);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.AcceptedWaiting,f.Tick(1));Assert.AreEqual(3,SkirmishEnemyPaidProductionAdapter.ReadDeliveredAliveCount(f.Em,f.Session,f.Intent));
            f.Em.RemoveComponent<UnitSpawnTransitTag>(last);Assert.AreEqual(SkirmishEnemyProductionIntentState.Delivered,f.Tick(2));
            f.Request();Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(2,f.Intent.NextRequestId);
        }
        [Test] public void DisabledBoardedMembersStillBelongToDeliveredCohort()
        {
            using var f=new Fixture();f.Request();f.Accept();f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Phase(SkirmishReservationPhase.Live,4);
            for(int i=0;i<4;i++){var a=f.Member(i);f.Em.AddComponent<Disabled>(a);}Assert.AreEqual(SkirmishEnemyProductionIntentState.Delivered,f.Tick(1));
        }
        [Test] public void DuplicateStableMemberFailsClosed()
        {
            using var f=new Fixture();f.Request();f.Accept();f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Phase(SkirmishReservationPhase.Live,4);
            for(int i=0;i<4;i++)f.Member(i);f.Member(5,false,41,"production.12.0");Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Tick(1));
        }
        [Test] public void LostReceiptDoesNotReplaceUntilNewAction()
        {
            using var f=new Fixture();f.Request();f.Accept();f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Phase(SkirmishReservationPhase.Lost);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Lost,f.Tick(1));f.Request();Assert.AreEqual(0,f.Requests.Length);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.RequestPending,f.Request("replacement.2"));Assert.AreEqual(2,f.Intent.RequestId);Assert.AreEqual(3,f.Intent.NextRequestId);
        }
        [Test] public void DuplicateRequestAndMissingReceiptFailClosed()
        {
            using(var f=new Fixture()){f.Request();f.Requests.Add(f.Requests[0]);Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Tick(1));}
            using(var f=new Fixture()){f.Request();f.Accept();f.Status(BuildingFactionUnitProductionRequest.Succeeded);f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Clear();Assert.AreEqual(SkirmishEnemyProductionIntentState.Abandoned,f.Tick(1));Assert.AreEqual(0,f.Requests.Length);}
        }
        [Test] public void PureDispatchGuardRetainsCapacityAndQueueLocksAcrossUnrelatedIncome()
        {
            using var f=new Fixture();Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            var cap=f.Em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session);cap.InfantryCap=0;f.Em.SetComponentData(f.Session,cap);
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            var stock=f.Em.GetComponentData<SkirmishEnemyStockComponent>(f.Session);stock.Materials+=100;f.Em.SetComponentData(f.Session,stock);
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            cap.InfantryCap=48;f.Em.SetComponentData(f.Session,cap);f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Add(new SkirmishProductionReservation {
                ReservationId=99,FactionId=2,Role=SkirmishRoleKind.Gunner,Category=SkirmishPopulationCategory.Infantry,
                MemberCount=4,RemainingMembers=4,ProducerRuntimeId=77,Producer=SkirmishProducerKind.Barracks,Phase=SkirmishReservationPhase.Producing });
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            stock.Materials+=100;f.Em.SetComponentData(f.Session,stock);
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            var reservations=f.Em.GetBuffer<SkirmishProductionReservation>(f.Session);var receipt=reservations[0];receipt.Phase=SkirmishReservationPhase.Released;reservations[0]=receipt;
            Assert.IsTrue(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            f.Em.AddBuffer<SkirmishResearchQueueItem>(f.Session).Add(new SkirmishResearchQueueItem {FactionId=2,Producer=SkirmishProducerKind.Barracks,Phase=SkirmishResearchPhase.Researching});
            Assert.IsFalse(SkirmishEnemyPaidProductionAdapter.CanDispatchNow(f.Em,f.Session,SkirmishRoleKind.Rifle));
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Idle,f.Intent.State);Assert.AreEqual(1,f.Intent.NextRequestId);
            Assert.AreEqual(0,f.Requests.Length);Assert.AreEqual(stock.Materials,f.Em.GetComponentData<SkirmishEnemyStockComponent>(f.Session).Materials);
        }
        [Test] public void LegacyOrMissingIntentNeverDispatches()
        {
            using var f=new Fixture();var s=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);s.IsLegacy=1;f.Em.SetComponentData(f.Session,s);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Idle,f.Request());f.Em.RemoveComponent<SkirmishEnemyProductionIntentV1Component>(f.Session);
            Assert.AreEqual(SkirmishEnemyProductionIntentState.Idle,f.Request());Assert.AreEqual(0,f.Requests.Length);
        }
    }
}
