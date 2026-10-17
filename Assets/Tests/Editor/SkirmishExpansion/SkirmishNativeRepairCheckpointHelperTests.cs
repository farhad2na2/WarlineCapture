#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{

    public sealed class SkirmishNativeRepairCheckpointHelperTests
    {
        private sealed class Fixture
        {
            public EntityManager Em;public Entity Session,Actor,Facility,Bank;
            public Fixture(EntityManager em,Entity session)
            {
                Em=em;Session=session;
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                native.Phase=SkirmishSessionPhase.Playing;native.SpawnComplete=1;em.SetComponentData(session,native);
                if(!em.HasComponent<SkirmishObjectiveClockComponent>(session))em.AddComponentData(session,new SkirmishObjectiveClockComponent{Playing=1});
                var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);clock.Playing=1;clock.Paused=0;em.SetComponentData(session,clock);
                var objective=new SkirmishObjectiveStateComponent{State=SkirmishObjectiveStateKind.Active};
                if(em.HasComponent<SkirmishObjectiveStateComponent>(session))em.SetComponentData(session,objective);else em.AddComponentData(session,objective);
var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                if(!em.HasComponent<SkirmishEconomyStockComponent>(session))em.AddComponentData(session,new SkirmishEconomyStockComponent{FactionId=1});
                if(!em.HasComponent<SkirmishCapacityComponent>(session))em.AddComponentData(session,new SkirmishCapacityComponent{SupplyLive=18});
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                Assert.IsTrue(definition.MapLayout.TryGetPad(1,SkirmishLegalPadKind.Service,out var pad));
                Assert.IsTrue(definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z));
                Actor=em.CreateEntity();em.AddComponent<SkirmishSharedActorTag>(Actor);
                em.AddComponentData(Actor,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.own.tank",FactionId=1});
                em.AddComponentData(Actor,new Faction{Id=1});em.AddComponentData(Actor,new UnitHealth{Current=100,Max=200});
                em.AddComponentData(Actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Tank,Category=SkirmishPopulationCategory.Ground});
                em.AddComponentData(Actor,new UnitMovementBehavior{UsesVehicleMotion=1});
                em.AddComponentData(Actor,LocalTransform.FromPosition(new float3(x,0,z)));
                Facility=em.CreateEntity();em.AddComponentData(Facility,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.ground.facility",FactionId=1,IsStructure=1});
                em.AddComponent<SkirmishSharedActorTag>(Facility);
                em.AddComponentData(Facility,new Faction{Id=1});em.AddComponentData(Facility,new UnitHealth{Current=800,Max=800});
                em.AddComponentData(Facility,LocalTransform.FromPosition(new float3(x,0,z)));
                em.AddComponentData(Facility,new SkirmishStructureIdentityComponent{StructureId=new FixedString64Bytes(SkirmishStructureIds.GroundStaging)});
                em.AddComponentData(Actor,new UnitSourcePrefabKey{Value="Unit_Tank"});
                em.AddComponentData(Facility,new UnitSourcePrefabKey{Value="Building_GroundStaging"});
                Bank=em.CreateEntity();em.AddComponentData(Bank,new FactionEconomy{FactionId=1});
                em.AddComponentData(Bank,new FactionTacticalMaterialsComponent{FactionId=1,Current=100,Capacity=200,Version=1});
            }
            public void Start()
            {Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(Em,Session,Actor,1,VehicleRepairAction.Start,out var error),error);Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(Em,Actor,out error),error);}
            public void Time(float seconds,bool paused=false)
            {var clock=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);clock.ElapsedSeconds=seconds;clock.Paused=(byte)(paused?1:0);Em.SetComponentData(Session,clock);Assert.IsTrue(VehicleFieldRepairService.Tick(Em,Actor,out string error),error);}
            public VehicleRepairStateComponent State=>Em.GetComponentData<VehicleRepairStateComponent>(Actor);
            public int Money=>Em.GetComponentData<FactionTacticalMaterialsComponent>(Bank).Current;
            public int Health=>Em.GetComponentData<UnitHealth>(Actor).Current;
        }

        static void With(Action<Fixture> test)=>SkirmishGenericMissionDispatchTests.WithViewBoundQueuedSession((em,session)=>test(new Fixture(em,session)));
        static SkirmishResolvedSetup Setup(Fixture f)=>f.Em.GetComponentObject<SkirmishResolvedSetupRecord>(f.Session).Setup;
        static SkirmishNativeCheckpointSnapshotService.Snapshot Saved(Fixture f)
        {
            var setup=Setup(f);var native=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
            SkirmishNativeCheckpointSnapshotService.Actor Actor(Entity entity,string key,byte structure)=>new(){
                Id=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).StableObjectId.ToString(),PrefabKey=key,IsStructure=structure,
                Parts=SkirmishNativeCheckpointParts.Capture(f.Em,entity,_=>null)};
            return new(){SessionId=native.SessionId.ToString(),SetupHash=setup.SetupHash,
                SessionParts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,_=>null),
                Actors=new[]{Actor(f.Actor,"Unit_Tank",0),Actor(f.Facility,"Building_GroundStaging",1)},
                Banks=new[]{new SkirmishNativeCheckpointSnapshotService.Bank{Faction=1,Parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Bank,_=>null)}}};
        }
        static SkirmishNativeRepairCheckpoint.Payload Capture(Fixture f)
        {Assert.IsTrue(SkirmishNativeRepairCheckpoint.TryCapture(f.Em,f.Session,out var value,out string error),error);Assert.IsNotNull(value);return value;}
        [Test] public void ReadonlyCapturePreservesPaidCursorAndRealClockLag()
        {With(f=>{f.Start();f.Time(1.125f);var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=2.5f;f.Em.SetComponentData(f.Session,clock);
            string before=JsonUtility.ToJson(f.State);var value=Capture(f);Assert.AreEqual(before,JsonUtility.ToJson(f.State));Assert.AreEqual(80,f.Money);Assert.AreEqual(100,f.Health);
            Assert.AreEqual(1.125,value.Rows[0].LastClockSeconds);Assert.IsTrue(SkirmishNativeRepairCheckpoint.Validate(value,Saved(f),Setup(f),out var error),error);});}
        [Test] public void InvalidPaidRowsCannotPassNativePayloadValidation()
        {With(f=>{f.Start();var original=Capture(f);var saved=Saved(f);Assert.IsTrue(SkirmishNativeRepairCheckpoint.Validate(original,saved,Setup(f),out var error),error);
            Action<SkirmishNativeRepairCheckpoint.Row>[] mutations={r=>r.PaidMaterials++,r=>r.FractionNumerator=0,r=>r.FractionDenominator=0,r=>r.RemainingTicks=-1,
                r=>r.DurationTicks++,r=>r.FractionalTicks=1,r=>r.LastClockSeconds=1,r=>r.RefundDue=1,r=>r.RefundedMaterials=1,r=>r.ActorId="foreign",
                r=>r.Faction=2,r=>r.Role=SkirmishRoleKind.Rifle,r=>r.Category=SkirmishPopulationCategory.Infantry,r=>r.FacilityId="foreign",
                r=>r.FacilityPrefabKey="wrong",r=>r.FacilityPosition=new float3(float.NaN),r=>r.AcceptedPosition=r.PadPosition+new float3(13,0,0),r=>r.IncomingDamage=2};
            foreach(var mutation in mutations){var value=JsonUtility.FromJson<SkirmishNativeRepairCheckpoint.Payload>(JsonUtility.ToJson(original));mutation(value.Rows[0]);
                Assert.IsFalse(SkirmishNativeRepairCheckpoint.Validate(value,saved,Setup(f),out _));}Assert.AreEqual(80,f.Money);Assert.AreEqual(100,f.Health);});}
        [Test] public void DuplicateRowsAndEmptyOrUnknownPayloadVersionsFailClosed()
        {With(f=>{f.Start();var value=Capture(f);var saved=Saved(f);var row=value.Rows[0];value.Rows=new[]{row,row};Assert.IsFalse(SkirmishNativeRepairCheckpoint.Validate(value,saved,Setup(f),out _));
            value.Rows=Array.Empty<SkirmishNativeRepairCheckpoint.Row>();Assert.IsFalse(SkirmishNativeRepairCheckpoint.Validate(value,saved,Setup(f),out _));
            value.Rows=new[]{row};value.Version=2;Assert.IsFalse(SkirmishNativeRepairCheckpoint.Validate(value,saved,Setup(f),out _));});}
        [Test] public void DebtSurvivesMissingFacilityWithoutInventingRefundOrHealth()
        {With(f=>{f.Start();var money=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);money.Current=money.Capacity;f.Em.SetComponentData(f.Bank,money);
            var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=1;f.Em.SetComponentData(f.Session,clock);VehicleFieldRepairService.NotifyNewMovement(f.Em,f.Actor);
            Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));var value=Capture(f);var saved=Saved(f);f.Em.DestroyEntity(f.Facility);
            Assert.AreEqual(VehicleRepairPhase.AccountingFailure,value.Rows[0].Phase);Assert.AreEqual(15,value.Rows[0].RefundDue);Assert.AreEqual(0,value.Rows[0].RefundedMaterials);
            Assert.IsTrue(SkirmishNativeRepairCheckpoint.Validate(value,saved,Setup(f),out var error),error);Assert.AreEqual(200,f.Money);Assert.AreEqual(100,f.Health);});}
        [Test] public void WrongTypedPadCannotBePublishedAsRecoverablePaidState()
        {With(f=>{f.Start();var state=f.State;state.PadId="stale.pad";f.Em.SetComponentData(f.Actor,state);
            Assert.IsFalse(SkirmishNativeRepairCheckpoint.TryCapture(f.Em,f.Session,out var value,out _));Assert.IsNull(value);Assert.AreEqual(80,f.Money);Assert.AreEqual(100,f.Health);});}
        [Test] public void EmptyCaptureKeepsLegacyWireAndPendingRequestRefusesCapture()
        {With(f=>{Assert.IsTrue(SkirmishNativeRepairCheckpoint.TryCapture(f.Em,f.Session,out var value,out _));Assert.IsNull(value);
            Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Start,out _));Assert.IsFalse(SkirmishNativeRepairCheckpoint.TryCapture(f.Em,f.Session,out value,out _));Assert.IsNull(value);Assert.AreEqual(100,f.Money);});}
        public static void RunFocusedValidation()
        {var t=new SkirmishNativeRepairCheckpointHelperTests();t.ReadonlyCapturePreservesPaidCursorAndRealClockLag();t.InvalidPaidRowsCannotPassNativePayloadValidation();
            t.DuplicateRowsAndEmptyOrUnknownPayloadVersionsFailClosed();t.DebtSurvivesMissingFacilityWithoutInventingRefundOrHealth();t.EmptyCaptureKeepsLegacyWireAndPendingRequestRefusesCapture();t.WrongTypedPadCannotBePublishedAsRecoverablePaidState();
            Debug.Log("[SkirmishNativeRepairCheckpointHelperTests] result=Passed tests=6 rowMutations=18 normalMissionAcceptance=0");}
    }
}
#endif
