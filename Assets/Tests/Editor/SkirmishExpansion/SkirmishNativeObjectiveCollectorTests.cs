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
    public sealed class SkirmishNativeObjectiveCollectorTests
    {
        // Disposable ECS actors and logical synthetic area fixtures only; no map,
        // terrain, roads, physical source assets or source-fit claims.
        private sealed class Fixture : IDisposable
        {
            public readonly World World=new World("ObjectiveCollectorLogicalFixture");
            public EntityManager Em=>World.EntityManager;
            public Entity Session;
            public SkirmishCompiledObjectiveAreas Areas;
            public SkirmishNativeObjectiveCollector Collector;
            public readonly System.Collections.Generic.List<Entity> Originals=new();
            public Fixture(SkirmishObjectiveKind kind)
            {
                Session=Em.CreateEntity();
                Em.AddComponentData(Session,new SkirmishExpandedSessionComponent{SessionId="attempt.fixture",DefinitionId="definition.fixture",
                    ObjectiveKind=kind,Phase=SkirmishSessionPhase.Playing,SpawnComplete=1,ContentVersion=1,SetupHash=123});
                Em.AddComponentData(Session,new SkirmishObjectiveClockComponent{Playing=1,DeadlineSeconds=1200});
                Areas=AreaFixture(kind);
                if(kind==SkirmishObjectiveKind.FrontlineControl)
                {
                    Originals.Add(Base(1));Originals.Add(Base(2));
                }
                else for(int i=0;i<(kind==SkirmishObjectiveKind.Breakthrough?12:3);i++)
                {
                    Entity actor=Actor("original."+i,1,kind==SkirmishObjectiveKind.Breakthrough?SkirmishRoleKind.Rifle:SkirmishRoleKind.ConvoyLogic,
                        kind==SkirmishObjectiveKind.Breakthrough?new float3(45,0,5):new float3(5,0,5));
                    var owned=Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    Em.AddComponentData(actor,new SkirmishObjectiveRoleComponent{Role=kind==SkirmishObjectiveKind.Breakthrough?
                        SkirmishObjectiveRoleKind.BreakthroughDesignated:SkirmishObjectiveRoleKind.ConvoyTruck,
                        StableObjectId=owned.StableObjectId,FactionId=1});Originals.Add(actor);
                }
                Assert.IsTrue(SkirmishNativeObjectiveCollector.TrySeal(Em,Session,Areas,Areas.LogicalFingerprint,out Collector,out string error),error);
            }
            public Entity Actor(string id,byte faction,SkirmishRoleKind role,float3 position)
            {
                Entity e=Em.CreateEntity();
                Em.AddComponentData(e,new SkirmishAttemptOwnedComponent{SessionId="attempt.fixture",StableObjectId=new FixedString64Bytes(id),FactionId=faction});
                Em.AddComponentData(e,new Faction{Id=faction});
                Em.AddComponentData(e,new SkirmishUnitRoleComponent{Role=role,Category=role==SkirmishRoleKind.ConvoyLogic?
                    SkirmishPopulationCategory.ObjectiveSupport:SkirmishRoleIds.Category(role)});
                Em.AddComponentData(e,new UnitHealth{Current=100,Max=100});Em.AddComponent<SkirmishSharedActorTag>(e);
                Em.AddComponentData(e,LocalTransform.FromPosition(position));return e;
            }
            private Entity Base(byte faction)
            {
                Entity e=Em.CreateEntity();string id="base."+faction;
                Em.AddComponentData(e,new SkirmishAttemptOwnedComponent{SessionId="attempt.fixture",StableObjectId=new FixedString64Bytes(id),FactionId=faction,IsStructure=1});
                Em.AddComponentData(e,new Faction{Id=faction});
                Em.AddComponentData(e,new SkirmishObjectiveRoleComponent{Role=faction==1?SkirmishObjectiveRoleKind.PlayerBase:SkirmishObjectiveRoleKind.EnemyBase,
                    StableObjectId=new FixedString64Bytes("base-role."+faction),FactionId=faction});
                Em.AddComponentData(e,new SkirmishStructureIdentityComponent{DesignatedBase=1,StructureId=SkirmishStructureIds.Barracks});
                Em.AddComponentData(e,new UnitHealth{Current=800,Max=800});Em.AddComponent<SkirmishSharedActorTag>(e);return e;
            }
            public void Time(float seconds)
            {var c=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);c.ElapsedSeconds=seconds;Em.SetComponentData(Session,c);}
            public SkirmishNativeObjectiveFacts Collect(SkirmishObjectiveCollectorClock cursor=default)
            {Assert.IsTrue(Collector.TryCollect(Em,Session,cursor,out var facts,out string error),error);return facts;}
            public void Dispose()=>World.Dispose();
        }

        private static SkirmishCompiledObjectiveAreas AreaFixture(SkirmishObjectiveKind kind)
        {
            var binding=new SkirmishObjectiveAreaBinding{DefinitionId="definition.fixture",SourceConfigId="source.fixture",SourceAssetGuid=new string('a',32),
                SourceContentHash="fixture-only",SourceSemanticFingerprint=new string('b',64),LayoutId="layout.fixture",LayoutBindingFingerprint=new string('c',64),
                SourceMinX=-100,SourceMinZ=-100,SourceMaxX=200,SourceMaxZ=200};
            var roles=kind==SkirmishObjectiveKind.FrontlineControl ? new[]{SkirmishObjectiveAreaRole.FrontlineA,SkirmishObjectiveAreaRole.FrontlineB,SkirmishObjectiveAreaRole.FrontlineC}:
                kind==SkirmishObjectiveKind.Breakthrough?new[]{SkirmishObjectiveAreaRole.BreakthroughCorridorA,SkirmishObjectiveAreaRole.BreakthroughCorridorB,SkirmishObjectiveAreaRole.BreakthroughExit}:
                new[]{SkirmishObjectiveAreaRole.ConvoyDestination};
            var config=new SkirmishObjectiveAreaConfig{AreaSetId="synthetic-logical-unit-test",ObjectiveKind=kind,Binding=binding,Areas=new SkirmishObjectiveArea[roles.Length]};
            for(int i=0;i<roles.Length;i++)config.Areas[i]=new SkirmishObjectiveArea{StableAreaId="area."+i,Role=roles[i],Shape=SkirmishObjectiveAreaShape.AxisAlignedRectangle,
                Points=new[]{new SkirmishObjectiveAreaPoint(i*20,0),new SkirmishObjectiveAreaPoint(i*20+10,10)}};
            config.Measurement=new SkirmishObjectiveAreaEvidence{ArtifactId="synthetic-logical-unit-test-only",ArtifactSha256=new string('d',64),
                GeometryFingerprint=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config)};
            Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(config,binding,kind,out var areas,out string error),error);return areas;
        }

        [Test] public void ClockRetainsFractionAcrossRepeatedCollectionAndCheckpointRoundtrip()
        {
            var cursor=new SkirmishObjectiveCollectorClock{FractionalTicks=.75};
            var clock=new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=.125f};
            Assert.IsTrue(SkirmishNativeObjectiveCollector.TryProjectClock(cursor,clock,out var next,out long ticks,out _));
            Assert.AreEqual(1250000,ticks);Assert.AreEqual(.75,next.FractionalTicks);
            var restored=JsonUtility.FromJson<SkirmishObjectiveCollectorClock>(JsonUtility.ToJson(next));
            Assert.IsTrue(SkirmishNativeObjectiveCollector.TryProjectClock(restored,clock,out var repeated,out ticks,out _));
            Assert.AreEqual(0,ticks);Assert.AreEqual(.75,repeated.FractionalTicks);
        }
        [Test] public void RegressingNonfiniteAndPausedAdvancingClocksFailWithoutMutation()
        {
            var cursor=new SkirmishObjectiveCollectorClock{LastAuthoritativeSeconds=1,FractionalTicks=.25};
            foreach(float seconds in new[]{.5f,float.NaN,float.PositiveInfinity})
            {Assert.IsFalse(SkirmishNativeObjectiveCollector.TryProjectClock(cursor,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=seconds},out var next,out _,out _));Assert.AreEqual(1,next.LastAuthoritativeSeconds);}
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryProjectClock(cursor,new SkirmishObjectiveClockComponent{Playing=1,Paused=1,ElapsedSeconds=2},out _,out _,out _));
            Assert.IsTrue(SkirmishNativeObjectiveCollector.TryProjectClock(cursor,new SkirmishObjectiveClockComponent{Playing=1,Paused=1,ElapsedSeconds=1},out _,out long ticks,out _));Assert.AreEqual(0,ticks);
        }
        [Test] public void ZoneCountsOnlyOwnLivingDismountedCombatInfantryAcrossBothFactions()
        {
            using var f=new Fixture(SkirmishObjectiveKind.FrontlineControl);
            f.Actor("player",1,SkirmishRoleKind.Rifle,new float3(5,0,5));f.Actor("enemy",2,SkirmishRoleKind.Rocketeer,new float3(5,0,5));
            f.Actor("tank",1,SkirmishRoleKind.Tank,new float3(5,0,5));
            Entity passenger=f.Actor("passenger",1,SkirmishRoleKind.Rifle,new float3(5,0,5));f.Em.AddComponentData(passenger,new UnitTransportPassenger{Transport=Entity.Null});
            Entity disabled=f.Actor("disabled",1,SkirmishRoleKind.Rifle,new float3(5,0,5));f.Em.AddComponent<Disabled>(disabled);
            Entity dead=f.Actor("dead",1,SkirmishRoleKind.Rifle,new float3(5,0,5));f.Em.SetComponentData(dead,new UnitHealth{Current=0,Max=100});
            Entity foreign=f.Actor("foreign",1,SkirmishRoleKind.Rifle,new float3(5,0,5));var owner=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(foreign);owner.SessionId="different";f.Em.SetComponentData(foreign,owner);
            var facts=f.Collect();Assert.AreEqual(1,facts.Frontline.ZoneA.PlayerEligibleInfantry);Assert.AreEqual(1,facts.Frontline.ZoneA.EnemyEligibleInfantry);
            Assert.IsTrue(facts.Frontline.OriginalBaseFactsPresent);Assert.IsTrue(facts.Frontline.PlayerOriginalBaseAlive);
        }
        [Test] public void CurrentParentTransformPreventsLocalCoordinatesCountingAsWorldOccupancy()
        {
            using var f=new Fixture(SkirmishObjectiveKind.FrontlineControl);
            Entity parent=f.Em.CreateEntity();f.Em.AddComponentData(parent,LocalTransform.FromPosition(100,0,0));
            Entity child=f.Actor("child",1,SkirmishRoleKind.Rifle,new float3(5,0,5));f.Em.AddComponentData(child,new Parent{Value=parent});
            Assert.AreEqual(0,f.Collect().Frontline.ZoneA.PlayerEligibleInfantry);
        }
        [Test] public void DuplicateStableIdsAndUnknownMissingOriginalsFailClosed()
        {
            using var f=new Fixture(SkirmishObjectiveKind.Breakthrough);
            f.Em.DestroyEntity(f.Originals[0]);Assert.IsFalse(f.Collector.TryCollect(f.Em,f.Session,default,out var facts,out _));Assert.IsNull(facts);
            using var other=new Fixture(SkirmishObjectiveKind.FrontlineControl);
            other.Actor("duplicate",1,SkirmishRoleKind.Rifle,new float3(5,0,5));other.Actor("duplicate",2,SkirmishRoleKind.Rifle,new float3(5,0,5));
            Assert.IsFalse(other.Collector.TryCollect(other.Em,other.Session,default,out facts,out _));Assert.IsNull(facts);
        }
        [Test] public void DestroyedOriginalTombstoneSurvivesNativeEntityRemoval()
        {
            using var f=new Fixture(SkirmishObjectiveKind.Breakthrough);Entity dead=f.Originals[0];
            f.Em.SetComponentData(dead,new UnitHealth{Current=0,Max=100});
            Assert.IsTrue(f.Collector.TryRecordDestroyedBeforeRemoval(f.Em,f.Session,dead,out string error),error);
            Assert.IsTrue(f.Collector.TryRecordDestroyedBeforeRemoval(f.Em,f.Session,dead,out error),error);f.Em.DestroyEntity(dead);
            var facts=f.Collect();bool found=false;foreach(var row in facts.NextTombstones)if(row.StableOriginalId=="original.0")
            {found=true;Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Destroyed,row.Disposition);}Assert.IsTrue(found);
            Assert.IsFalse(facts.Breakthrough.OriginalSubjects[0].Alive);
        }
        [Test] public void EvacuationIsDistinctFromDestructionAndCannotRepeatOrCaptureAfterRemoval()
        {
            using var f=new Fixture(SkirmishObjectiveKind.Breakthrough);f.Time(3);var facts=f.Collect();
            var state=SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard,"a","b",f.Collector.OriginalStableIds());state.ExitOpen=true;
            var events=SkirmishBreakthroughReducer.Step(ref state,facts.Breakthrough);Assert.AreEqual(12,events.Length);
            Assert.IsTrue(f.Collector.TryCommitCollectedTombstones(facts,out _));
            string id=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Originals[0]).StableObjectId.ToString();
            Assert.IsTrue(f.Collector.TryRecordEvacuated(f.Em,f.Session,id,state,out string error),error);
            Assert.IsFalse(f.Collector.TryRecordEvacuated(f.Em,f.Session,id,state,out _));
            f.Em.DestroyEntity(f.Originals[0]);var after=f.Collect(facts.NextClock);
            Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Evacuated,after.NextTombstones[0].Disposition);
            Assert.IsFalse(after.Breakthrough.OriginalSubjects[0].Alive);
        }
        [Test] public void DeliveredTruckKeepsSuccessTombstoneAndOrdinaryLogisticsCannotSubstitute()
        {
            using var f=new Fixture(SkirmishObjectiveKind.ConvoyEscort);f.Time(5);var facts=f.Collect();
            var state=SkirmishConvoyReducer.Create(SkirmishSizeId.Standard,"destination",f.Collector.OriginalStableIds());
            Assert.AreEqual(3,SkirmishConvoyReducer.Step(ref state,facts.Convoy).Length);
            Assert.IsTrue(f.Collector.TryCommitCollectedTombstones(facts,out _));
            string id=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Originals[0]).StableObjectId.ToString();
            Assert.IsTrue(f.Collector.TryRecordDelivered(f.Em,f.Session,id,state,out string error),error);f.Em.DestroyEntity(f.Originals[0]);
            f.Actor("ordinary.logistics",1,SkirmishRoleKind.LogisticsTruck,new float3(5,0,5));
            var after=f.Collect(facts.NextClock);Assert.AreEqual(3,after.Convoy.OriginalTrucks.Length);
            Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Delivered,after.NextTombstones[0].Disposition);
        }
        [Test] public void ForgedTombstoneAndChangedAttemptCannotCommit()
        {
            using var f=new Fixture(SkirmishObjectiveKind.FrontlineControl);var facts=f.Collect();
            facts.NextTombstones[0].Disposition=SkirmishOriginalObjectiveDisposition.Destroyed;
            Assert.IsFalse(f.Collector.TryCommitCollectedTombstones(facts,out _));
            var session=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);session.SetupHash++;
            f.Em.SetComponentData(f.Session,session);Assert.IsFalse(f.Collector.TryCollect(f.Em,f.Session,default,out _,out _));
        }
        [Test] public void MissingAreaBindingOrLateOriginalSealDoesNotInferFallbacks()
        {
            using var f=new Fixture(SkirmishObjectiveKind.ConvoyEscort);
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TrySeal(f.Em,f.Session,null,"",out _,out _));
            f.Time(1);Assert.IsFalse(SkirmishNativeObjectiveCollector.TrySeal(f.Em,f.Session,f.Areas,f.Areas.LogicalFingerprint,out _,out _));
        }
        [Test] public void DeathTombstoneRejectsMutatedSealedIdentityInEveryNativeOwnershipField()
        {
            for(int mutation=0;mutation<8;mutation++)
            {
                using var f=new Fixture(SkirmishObjectiveKind.Breakthrough);Entity actor=f.Originals[0];
                MutateIdentity(f,actor,mutation);f.Em.SetComponentData(actor,new UnitHealth{Current=0,Max=100});
                Assert.IsFalse(f.Collector.TryRecordDestroyedBeforeRemoval(f.Em,f.Session,actor,out _),"mutation "+mutation);
            }
        }
        [Test] public void SuccessfulRemovalRejectsMutatedIdentityAndPendingSnapshot()
        {
            for(int mutation=0;mutation<8;mutation++)
            {
                using var f=new Fixture(SkirmishObjectiveKind.ConvoyEscort);f.Time(5);var facts=f.Collect();
                var state=SkirmishConvoyReducer.Create(SkirmishSizeId.Standard,"destination",f.Collector.OriginalStableIds());
                SkirmishConvoyReducer.Step(ref state,facts.Convoy);
                Assert.IsFalse(f.Collector.TryRecordDelivered(f.Em,f.Session,"original.0",state,out _),"pending snapshot");
                Assert.IsTrue(f.Collector.TryCommitCollectedTombstones(facts,out _));
                MutateIdentity(f,f.Originals[0],mutation);
                Assert.IsFalse(f.Collector.TryRecordDelivered(f.Em,f.Session,"original.0",state,out _),"mutation "+mutation);
            }
        }
        [Test] public void CheckpointRowsRebindFreshWorldAfterClockAdvancesAndPreserveDistinctBaseIds()
        {
            using var before=new Fixture(SkirmishObjectiveKind.FrontlineControl);
            Assert.IsTrue(before.Collector.TryExportCheckpointRows(before.Em,before.Session,out var rows,out string error),error);
            Assert.AreNotEqual(rows[0].StableOriginalId,rows[0].ObjectiveStableId);
            using var after=new Fixture(SkirmishObjectiveKind.FrontlineControl);after.Time(17);
            Assert.IsTrue(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out var rebound,out error),error);
            Assert.IsTrue(rebound.TryExportCheckpointRows(after.Em,after.Session,out var actual,out error),error);
            Assert.AreEqual(rows[0].ObjectiveStableId,actual[0].ObjectiveStableId);
            var owner=after.Em.GetComponentData<SkirmishAttemptOwnedComponent>(after.Originals[0]);owner.CapacityReleased=1;
            after.Em.SetComponentData(after.Originals[0],owner);
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out _,out _),"Healthy original cannot resume with released capacity");
            owner.CapacityReleased=0;after.Em.SetComponentData(after.Originals[0],owner);
            var changed=rows;changed[0].ObjectiveStableId="wrong.base.role";
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,changed,out _,out _));
        }
        [Test] public void CheckpointRowsRejectPendingSnapshotAndResurrectedSuccessfulOriginal()
        {
            using var before=new Fixture(SkirmishObjectiveKind.ConvoyEscort);var facts=before.Collect();
            Assert.IsFalse(before.Collector.TryExportCheckpointRows(before.Em,before.Session,out _,out _));
            Assert.IsTrue(before.Collector.TryCommitCollectedTombstones(facts,out _));
            Assert.IsTrue(before.Collector.TryExportCheckpointRows(before.Em,before.Session,out var rows,out _));
            rows[0].Disposition=SkirmishOriginalObjectiveDisposition.Delivered;
            using var after=new Fixture(SkirmishObjectiveKind.ConvoyEscort);after.Time(9);
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out _,out _));
            after.Em.DestroyEntity(after.Originals[0]);
            Assert.IsTrue(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out var rebound,out string error),error);
            Assert.IsTrue(rebound.TryExportCheckpointRows(after.Em,after.Session,out var actual,out error),error);
            Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Delivered,actual[0].Disposition);
            rows[1].Disposition=SkirmishOriginalObjectiveDisposition.Destroyed;
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out _,out _),"Healthy reconstructed actor cannot satisfy destroyed tombstone");
            rows[1].Disposition=SkirmishOriginalObjectiveDisposition.Live;
            var extra=after.Actor("unplanned.original",1,SkirmishRoleKind.ConvoyLogic,new float3(5,0,5));
            after.Em.AddComponentData(extra,new SkirmishObjectiveRoleComponent{Role=SkirmishObjectiveRoleKind.ConvoyTruck,FactionId=1,StableObjectId="unplanned.original"});
            Assert.IsFalse(SkirmishNativeObjectiveCollector.TryRebindCheckpoint(after.Em,after.Session,after.Areas,
                after.Areas.LogicalFingerprint,rows,out _,out _),"Unplanned original actors must not bypass exact count through omitted payload rows");
        }
        [Test] public void SuccessfulNativeDepartureReleasesCapacityWithoutDeathAndKeepsOriginalTombstone()
        {
            using var f=new Fixture(SkirmishObjectiveKind.Breakthrough);
            foreach(var actor in f.Originals){var role=f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor);role.SupplyCost=1;f.Em.SetComponentData(actor,role);}
            f.Em.AddComponentData(f.Session,new SkirmishCapacityComponent{InfantryLive=12,SupplyLive=12,InfantryCap=96,SupplyCap=200});
            var tracked=f.Em.AddBuffer<SkirmishTrackedUnit>(f.Session);tracked.Add(new SkirmishTrackedUnit{Entity=f.Originals[0],FactionId=1});
            f.Time(3);var facts=f.Collect();
            var state=SkirmishBreakthroughReducer.Create(SkirmishSizeId.Standard,"a","b",f.Collector.OriginalStableIds());state.ExitOpen=true;
            SkirmishBreakthroughReducer.Step(ref state,facts.Breakthrough);Assert.IsTrue(f.Collector.TryCommitCollectedTombstones(facts,out _));
            f.Em.AddComponentObject(f.Session,new SkirmishNativeObjectiveRecord{Collector=f.Collector,Areas=f.Areas,Breakthrough=state,Clock=facts.NextClock});
            Assert.IsTrue(SkirmishNativeObjectiveDeparture.TryRemoveSuccessfulOriginal(f.Em,f.Session,"original.0",out string error),error);
            Assert.IsFalse(f.Em.Exists(f.Originals[0]));Assert.AreEqual(0,f.Em.GetBuffer<SkirmishTrackedUnit>(f.Session).Length);
            var capacity=f.Em.GetComponentData<SkirmishCapacityComponent>(f.Session);Assert.AreEqual(11,capacity.InfantryLive);Assert.AreEqual(11,capacity.SupplyLive);
            Assert.IsFalse(SkirmishNativeObjectiveDeparture.TryRemoveSuccessfulOriginal(f.Em,f.Session,"original.0",out _));
            Assert.IsTrue(f.Collector.TryExportCheckpointRows(f.Em,f.Session,out var rows,out error),error);
            Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Evacuated,rows[0].Disposition);
        }
        private static void MutateIdentity(Fixture f,Entity actor,int mutation)
        {
            var owned=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            if(mutation==0)owned.FactionId=2;
            if(mutation==1)owned.IsStructure=1;
            if(mutation==2)owned.ReservationId=123;
            f.Em.SetComponentData(actor,owned);
            if(mutation==3){var role=f.Em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);role.StableObjectId="mutated";f.Em.SetComponentData(actor,role);}
            if(mutation==4){var role=f.Em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);role.Role=SkirmishObjectiveRoleKind.None;f.Em.SetComponentData(actor,role);}
            if(mutation==5)f.Em.RemoveComponent<SkirmishSharedActorTag>(actor);
            if(mutation==6){var role=f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor);role.Role=SkirmishRoleKind.Tank;f.Em.SetComponentData(actor,role);}
            if(mutation==7)f.Em.SetComponentData(actor,new Faction{Id=2});
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishNativeObjectiveCollectorTests();
            t.ClockRetainsFractionAcrossRepeatedCollectionAndCheckpointRoundtrip();t.RegressingNonfiniteAndPausedAdvancingClocksFailWithoutMutation();
            t.ZoneCountsOnlyOwnLivingDismountedCombatInfantryAcrossBothFactions();t.CurrentParentTransformPreventsLocalCoordinatesCountingAsWorldOccupancy();
            t.DuplicateStableIdsAndUnknownMissingOriginalsFailClosed();t.DestroyedOriginalTombstoneSurvivesNativeEntityRemoval();
            t.EvacuationIsDistinctFromDestructionAndCannotRepeatOrCaptureAfterRemoval();t.DeliveredTruckKeepsSuccessTombstoneAndOrdinaryLogisticsCannotSubstitute();
            t.ForgedTombstoneAndChangedAttemptCannotCommit();t.MissingAreaBindingOrLateOriginalSealDoesNotInferFallbacks();
            t.DeathTombstoneRejectsMutatedSealedIdentityInEveryNativeOwnershipField();t.SuccessfulRemovalRejectsMutatedIdentityAndPendingSnapshot();
            t.CheckpointRowsRebindFreshWorldAfterClockAdvancesAndPreserveDistinctBaseIds();t.CheckpointRowsRejectPendingSnapshotAndResurrectedSuccessfulOriginal();
            t.SuccessfulNativeDepartureReleasesCapacityWithoutDeathAndKeepsOriginalTombstone();
            Debug.Log("[SkirmishNativeObjectiveCollectorTests] result=Passed tests=15 nativeMissionReadiness=Pending");
        }
    }
}
#endif
