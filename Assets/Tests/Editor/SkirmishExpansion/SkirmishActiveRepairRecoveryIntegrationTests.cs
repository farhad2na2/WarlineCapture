#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Synthetic native actors inside the actual generic compiler/provider/queue.
    // Exercises real capture/codec/preparation/commit, never a gameplay win.
    public sealed class SkirmishActiveRepairRecoveryIntegrationTests
    {
        private sealed class Fixture
        {
            internal readonly EntityManager Em;
            internal readonly Entity Session,Tank,Facility,Bank;
            internal readonly SkirmishResolvedSetup Setup;
            internal readonly RuntimeBuildingEntity Building;
            internal Fixture(EntityManager em,Entity session,bool withFacility=true,int runtimeId=10)
            {
                Em=em;Session=session;Setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                // Ordinary queue is not the initialized native session. Reuse the
                // actual lifecycle owner for objective/capacity/research/army and
                // policy checkpoint state; this never spawns fixture force actors.
                em.World.GetOrCreateSystem<SkirmishSessionInitializationSystem>().Update(em.World.Unmanaged);
                Assert.AreEqual(1,em.GetComponentData<SkirmishExpandedSessionComponent>(session).InitializationComplete);
                if(Setup.EnemyPolicy!=null)Assert.IsTrue(em.HasComponent<SkirmishEnemyPlannerV1Component>(session));
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);native.SpawnComplete=1;native.Phase=SkirmishSessionPhase.Playing;em.SetComponentData(session,native);
                Set(em,session,new SkirmishObjectiveClockComponent {Playing=1});
                Set(em,session,new SkirmishObjectiveStateComponent {Kind=Setup.ObjectiveKind,State=SkirmishObjectiveStateKind.Active});
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                Assert.IsTrue(definition.MapLayout.TryGetPad(1,SkirmishLegalPadKind.Service,out var pad));
                Assert.IsTrue(definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z));
                Tank=CreateTank(em,native.SessionId,"repair.integration.tank",new float3(x,0,z));
                if(withFacility)
                {
                    Facility=em.CreateEntity();em.AddComponentData(Facility,new SkirmishAttemptOwnedComponent {SessionId=native.SessionId,StableObjectId="repair.integration.facility",FactionId=1,IsStructure=1});
                    em.AddComponent<SkirmishSharedActorTag>(Facility);em.AddComponentData(Facility,new Faction {Id=1});em.AddComponentData(Facility,new UnitHealth {Current=800,Max=800});
                    em.AddComponentData(Facility,LocalTransform.FromPosition(new float3(x,0,z)));
                    em.AddComponentData(Facility,new UnitSourcePrefabKey {Value="Building_GroundStaging"});
                    em.AddComponentData(Facility,new SkirmishStructureIdentityComponent {StructureId=new FixedString64Bytes(SkirmishStructureIds.GroundStaging)});
                    em.AddComponentData(Facility,new RuntimeBuildingCombatInfo {RuntimeBuildingId=runtimeId,OwnerFactionId=1,OriginCell=new int2((int)x,(int)z)});
                    Building=new RuntimeBuildingEntity {Id=runtimeId,CombatEntity=Facility};
                    em.AddComponentObject(Facility,new SkirmishNativeBuildingCheckpointRecord {Building=Building,TryRestoreProduction=_=>true,RollbackProduction=_=>{}});
                }
                em.AddComponent<SkirmishSharedMaterialsInitialized>(session);
                Bank=CreateBank(em,1);CreateBank(em,2);
            }
            internal void Start(Entity actor=default)
            {if(actor==Entity.Null)actor=Tank;Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(Em,Session,actor,1,VehicleRepairAction.Start,out var error),error);Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(Em,actor,out error),error);}
            internal void Time(float elapsed,Entity actor=default)
            {if(actor==Entity.Null)actor=Tank;var clock=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);clock.ElapsedSeconds=elapsed;Em.SetComponentData(Session,clock);Assert.IsTrue(VehicleFieldRepairService.Tick(Em,actor,out string error),error);}
            internal SkirmishNativeCheckpointSnapshotService.Snapshot Capture()
            {Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(Em,Session,_=>"unused",out var saved,out string error),error);return saved;}
            internal void MakeFresh(string attempt)
            {
                var native=Em.GetComponentData<SkirmishExpandedSessionComponent>(Session);native.SessionId=new FixedString64Bytes(attempt);native.SpawnComplete=0;native.Phase=SkirmishSessionPhase.Spawning;Em.SetComponentData(Session,native);
                using var query=Em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));using var actors=query.ToEntityArray(Allocator.Temp);
                foreach(var actor in actors){var owner=Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);owner.SessionId=native.SessionId;Em.SetComponentData(actor,owner);Em.AddComponent<CampaignMissionCombatSuppressedTag>(actor);}
                Em.SetComponentData(Tank,new UnitHealth {Current=199,Max=200});
            }
        }
        private static void Set<T>(EntityManager em,Entity entity,T value)where T:unmanaged,IComponentData
        {if(em.HasComponent<T>(entity))em.SetComponentData(entity,value);else em.AddComponentData(entity,value);}
        private static Entity CreateBank(EntityManager em,byte faction)
        {var bank=em.CreateEntity();em.AddComponentData(bank,new FactionEconomy {FactionId=faction});em.AddComponentData(bank,new FactionTacticalMaterialsComponent {FactionId=faction,Current=100,Capacity=200,Version=1});return bank;}
        private static Entity CreateTank(EntityManager em,FixedString64Bytes attempt,string id,float3 position)
        {
            var actor=em.CreateEntity();em.AddComponent<SkirmishSharedActorTag>(actor);
            em.AddComponentData(actor,new SkirmishAttemptOwnedComponent {SessionId=attempt,StableObjectId=new FixedString64Bytes(id),FactionId=1});
            em.AddComponentData(actor,new Faction {Id=1});em.AddComponentData(actor,new UnitHealth {Current=100,Max=200});
            em.AddComponentData(actor,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.Tank,Category=SkirmishPopulationCategory.Ground});
            em.AddComponentData(actor,new UnitSourcePrefabKey {Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Tank))});
            em.AddComponentData(actor,new UnitMovementBehavior {UsesVehicleMotion=1});em.AddComponentData(actor,LocalTransform.FromPosition(position));return actor;
        }
        private static void With(Action<Fixture> callback,bool facility=true,int runtimeId=10)
            =>SkirmishGenericMissionDispatchTests.WithViewBoundQueuedSession((em,session)=>callback(new Fixture(em,session,facility,runtimeId)));
        private static SkirmishNativeCheckpointRecoveryPreparation.Prepared Prepare(Fixture fresh,SkirmishNativeCheckpointSnapshotService.Snapshot saved)
        {
            Assert.AreEqual(saved.SetupHash,fresh.Setup.SetupHash,"Independent actual compiler must produce identical setup.");
            fresh.MakeFresh(saved.SessionId);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh.Em,fresh.Session,saved,fresh.Setup,_=>null,out var prepared,out string error),error);
            return prepared;
        }
        [Test] public void ActualWireTwoFreshRecoveryResumesPaidWorkWithoutRechargeOrHealing()
        {
            With(source=>
            {
                source.Start();source.Time(5.25f);var paid=source.Em.GetComponentData<VehicleRepairStateComponent>(source.Tank);
                // Queueing does not initialize the native active-objective owner.
                // The disposable fixture performs that normal spawn lifecycle step;
                // capture must still refuse a missing owner without losing paid work.
                var objective=source.Em.GetComponentData<SkirmishObjectiveStateComponent>(source.Session);
                Assert.AreEqual(source.Setup.ObjectiveKind,objective.Kind);
                Assert.AreEqual(SkirmishObjectiveStateKind.Active,objective.State);
                source.Em.RemoveComponent<SkirmishObjectiveStateComponent>(source.Session);
                Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(source.Em,source.Session,_=>"unused",out var incomplete,out _));
                Assert.IsNull(incomplete);
                source.Em.AddComponentData(source.Session,objective);
                Assert.AreEqual(paid.PaidMaterials,source.Em.GetComponentData<VehicleRepairStateComponent>(source.Tank).PaidMaterials);
                var saved=source.Capture();Assert.IsNotNull(saved.Repairs);
                Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishObjectiveStateComponent savedObjective));
                Assert.AreEqual(source.Setup.ObjectiveKind,savedObjective.Kind);Assert.AreEqual(SkirmishObjectiveStateKind.Active,savedObjective.State);
                string wire=SkirmishNativeCheckpointCodec.Encode(saved,source.Setup);Assert.IsNotNull(wire);
                StringAssert.StartsWith("{\"Version\":2,",wire);
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire,source.Setup,out saved));
                With(fresh=>
                {
                    var prepared=Prepare(fresh,saved);Assert.AreEqual(199,fresh.Em.GetComponentData<UnitHealth>(fresh.Tank).Current);
                    Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out string error),error);
                    var restored=fresh.Em.GetComponentData<VehicleRepairStateComponent>(fresh.Tank);
                    Assert.AreEqual(fresh.Session,restored.Session);Assert.AreEqual(fresh.Facility,restored.ServiceFacility);Assert.AreEqual(fresh.Bank,restored.MaterialsBank);
                    Assert.AreEqual(paid.RemainingTicks,restored.RemainingTicks);Assert.AreEqual(paid.FractionalTicks,restored.FractionalTicks);Assert.AreEqual(paid.LastClockSeconds,restored.LastClockSeconds);
                    Assert.AreEqual(100,fresh.Em.GetComponentData<UnitHealth>(fresh.Tank).Current);Assert.AreEqual(80,fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank).Current);
                    fresh.Time(6.25f);Assert.AreEqual(paid.RemainingTicks-TimeSpan.TicksPerSecond,fresh.Em.GetComponentData<VehicleRepairStateComponent>(fresh.Tank).RemainingTicks);
                    fresh.Time(20);Assert.AreEqual(150,fresh.Em.GetComponentData<UnitHealth>(fresh.Tank).Current);Assert.AreEqual(80,fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank).Current);
                    Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out _));
                },runtimeId:20);
            });
        }
        [Test] public void ActualRefundDebtResumesWithMissingFacilityAndRefundsOnce()
        {
            With(source=>
            {
                source.Start();var bank=source.Em.GetComponentData<FactionTacticalMaterialsComponent>(source.Bank);bank.Current=bank.Capacity;source.Em.SetComponentData(source.Bank,bank);
                VehicleFieldRepairService.NotifyNewMovement(source.Em,source.Tank);
                var clock=source.Em.GetComponentData<SkirmishObjectiveClockComponent>(source.Session);clock.ElapsedSeconds=1;source.Em.SetComponentData(source.Session,clock);
                Assert.IsFalse(VehicleFieldRepairService.Tick(source.Em,source.Tank,out _));
                Assert.AreEqual(VehicleRepairPhase.AccountingFailure,source.Em.GetComponentData<VehicleRepairStateComponent>(source.Tank).Phase);
                source.Em.DestroyEntity(source.Facility);var saved=source.Capture();string wire=SkirmishNativeCheckpointCodec.Encode(saved,source.Setup);Assert.IsNotNull(wire);
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire,source.Setup,out saved));
                With(fresh=>
                {
                    var prepared=Prepare(fresh,saved);Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out string error),error);
                    var restored=fresh.Em.GetComponentData<VehicleRepairStateComponent>(fresh.Tank);Assert.AreEqual(VehicleRepairPhase.AccountingFailure,restored.Phase);
                    Assert.AreEqual(15,restored.RefundDue-restored.RefundedMaterials);Assert.AreEqual(Entity.Null,restored.ServiceFacility);
                    Assert.AreEqual(200,fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank).Current);Assert.AreEqual(100,fresh.Em.GetComponentData<UnitHealth>(fresh.Tank).Current);
                    var materials=fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank);
                    Assert.AreEqual(FactionTacticalMaterialsMutationResult.Applied,FactionTacticalMaterialsUtilitySystemHelper.TrySpend(ref materials,20,FactionTacticalMaterialsSpendKind.Construction));fresh.Em.SetComponentData(fresh.Bank,materials);
                    Assert.IsTrue(VehicleFieldRepairService.Tick(fresh.Em,fresh.Tank,out _));Assert.IsTrue(VehicleFieldRepairService.Tick(fresh.Em,fresh.Tank,out _));
                    Assert.AreEqual(195,fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank).Current);
                },facility:false);
            });
        }
        [Test] public void PendingPublicRepairRequestStillRefusesActualNativeCapture()
        {With(f=>{Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Tank,1,VehicleRepairAction.Start,out _));Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(f.Em,f.Session,_=>"unused",out var saved,out _));Assert.IsNull(saved);Assert.AreEqual(100,f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank).Current);});}
        [Test] public void NativeCommitFailureRollsBackRepairComponentsAndPreviouslyAppliedParts()
        {
            With(source=>
            {
                source.Start();var native=source.Em.GetComponentData<SkirmishExpandedSessionComponent>(source.Session);
                var second=CreateTank(source.Em,native.SessionId,"repair.integration.tank.second",source.Em.GetComponentData<LocalTransform>(source.Tank).Position);source.Start(second);
                var saved=source.Capture();With(fresh=>
                {
                    var secondFresh=CreateTank(fresh.Em,fresh.Em.GetComponentData<SkirmishExpandedSessionComponent>(fresh.Session).SessionId,"repair.integration.tank.second",fresh.Em.GetComponentData<LocalTransform>(fresh.Tank).Position);
                    var prepared=Prepare(fresh,saved);
                    var record=fresh.Em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(fresh.Facility);
                    record.TryRestoreProduction=_=>{fresh.Em.RemoveComponent<SkirmishSharedActorTag>(secondFresh);return true;};
                    record.RollbackProduction=_=>{if(!fresh.Em.HasComponent<SkirmishSharedActorTag>(secondFresh))fresh.Em.AddComponent<SkirmishSharedActorTag>(secondFresh);};
                    Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out _));Assert.IsFalse(prepared.Committed);
                    Assert.IsFalse(fresh.Em.HasComponent<VehicleRepairStateComponent>(fresh.Tank));Assert.IsFalse(fresh.Em.HasComponent<VehicleRepairStateComponent>(secondFresh));
                    Assert.AreEqual(199,fresh.Em.GetComponentData<UnitHealth>(fresh.Tank).Current);Assert.AreEqual(100,fresh.Em.GetComponentData<FactionTacticalMaterialsComponent>(fresh.Bank).Current);
                    Assert.IsTrue(fresh.Em.HasComponent<CampaignMissionCombatSuppressedTag>(fresh.Tank));Assert.IsTrue(fresh.Em.HasComponent<SkirmishSharedActorTag>(secondFresh));
                },runtimeId:20);
            });
        }
        private static void RunCase(string name,Action action,System.Collections.Generic.List<Exception> failures)
        {
            try{action();Debug.Log("[SkirmishActiveRepairRecoveryIntegrationCase] case="+name+" result=Passed");}
            catch(Exception error)
            {
                Debug.LogError("[SkirmishActiveRepairRecoveryIntegrationCase] case="+name+" result=Failed error="+error);
                failures.Add(new InvalidOperationException(name,error));
            }
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishActiveRepairRecoveryIntegrationTests();
            var failures=new System.Collections.Generic.List<Exception>();
            RunCase(nameof(ActualWireTwoFreshRecoveryResumesPaidWorkWithoutRechargeOrHealing),t.ActualWireTwoFreshRecoveryResumesPaidWorkWithoutRechargeOrHealing,failures);
            RunCase(nameof(ActualRefundDebtResumesWithMissingFacilityAndRefundsOnce),t.ActualRefundDebtResumesWithMissingFacilityAndRefundsOnce,failures);
            RunCase(nameof(PendingPublicRepairRequestStillRefusesActualNativeCapture),t.PendingPublicRepairRequestStillRefusesActualNativeCapture,failures);
            RunCase(nameof(NativeCommitFailureRollsBackRepairComponentsAndPreviouslyAppliedParts),t.NativeCommitFailureRollsBackRepairComponentsAndPreviouslyAppliedParts,failures);
            if(failures.Count!=0)throw new AggregateException("Active repair recovery integration failed cases="+failures.Count+" total=4",failures);
            Debug.Log("[SkirmishActiveRepairRecoveryIntegrationTests] result=Passed tests=4 normalMissionAcceptance=0");
        }
    }
}
#endif
