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
    public sealed class SkirmishNativeObjectiveTickTests
    {
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("NativeObjectiveTickLogicalFixture");
            public EntityManager Em=>World.EntityManager;
            public Entity Session;public Entity[] Originals=new Entity[3];public SkirmishNativeObjectiveRecord Record;
            public Fixture()
            {
                Session=Em.CreateEntity();
                Em.AddComponentData(Session,new SkirmishExpandedSessionComponent{SessionId="tick.attempt",DefinitionId="tick.definition",
                    ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort,Phase=SkirmishSessionPhase.Playing,SpawnComplete=1,ContentVersion=1,SetupHash=123});
                Em.AddComponentData(Session,new SkirmishObjectiveStateComponent{Kind=SkirmishObjectiveKind.ConvoyEscort});
                Em.AddComponentData(Session,new SkirmishObjectiveClockComponent{Playing=1,DeadlineSeconds=1080});
                Em.AddComponentData(Session,new SkirmishCapacityComponent{InfantryLive=12,GroundLive=1,SupplyLive=18});
                Em.AddComponentData(Session,new SkirmishMatchState());Em.AddBuffer<SkirmishTrackedUnit>(Session);
                for(int i=0;i<3;i++)
                {
                    var actor=Em.CreateEntity();Originals[i]=actor;var id=new FixedString64Bytes("truck."+i);
                    Em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId="tick.attempt",StableObjectId=id,FactionId=1});
                    Em.AddComponentData(actor,new Faction{Id=1});
                    Em.AddComponentData(actor,new SkirmishObjectiveRoleComponent{Role=SkirmishObjectiveRoleKind.ConvoyTruck,FactionId=1,StableObjectId=id});
                    Em.AddComponentData(actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.ConvoyLogic,Category=SkirmishPopulationCategory.ObjectiveSupport});
                    Em.AddComponent<SkirmishSharedActorTag>(actor);Em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
                    Em.AddComponentData(actor,LocalTransform.FromPosition(new float3(5,0,5)));
                    Em.GetBuffer<SkirmishTrackedUnit>(Session).Add(new SkirmishTrackedUnit{Entity=actor,FactionId=1});
                }
                var binding=new SkirmishObjectiveAreaBinding{DefinitionId="tick.definition",SourceConfigId="logical.fixture",SourceAssetGuid=new string('a',32),
                    SourceContentHash="fixture-only",SourceSemanticFingerprint=new string('b',64),LayoutId="logical.layout",LayoutBindingFingerprint=new string('c',64),
                    SourceMinX=0,SourceMinZ=0,SourceMaxX=100,SourceMaxZ=100};
                var config=new SkirmishObjectiveAreaConfig{AreaSetId="synthetic-only",ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort,Binding=binding,
                    Areas=new[]{new SkirmishObjectiveArea{StableAreaId="destination",Role=SkirmishObjectiveAreaRole.ConvoyDestination,
                        Shape=SkirmishObjectiveAreaShape.AxisAlignedRectangle,Points=new[]{new SkirmishObjectiveAreaPoint(0,0),new SkirmishObjectiveAreaPoint(10,10)}}}};
                config.Measurement=new SkirmishObjectiveAreaEvidence{ArtifactId="synthetic-only",ArtifactSha256=new string('d',64),GeometryFingerprint=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config)};
                Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(config,binding,SkirmishObjectiveKind.ConvoyEscort,out var areas,out var error),error);
                Assert.IsTrue(SkirmishNativeObjectiveCollector.TrySeal(Em,Session,areas,areas.LogicalFingerprint,out var collector,out error),error);
                Record=new SkirmishNativeObjectiveRecord{Collector=collector,Areas=areas,MissionViewProfileFingerprint=new string('e',64),
                    Convoy=SkirmishConvoyReducer.Create(SkirmishSizeId.Standard,"destination",collector.OriginalStableIds())};
                Em.AddComponentObject(Session,Record);
            }
            public void Time(float elapsed,bool paused=false)
            {var clock=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);clock.ElapsedSeconds=elapsed;clock.Paused=(byte)(paused?1:0);Em.SetComponentData(Session,clock);}
            public void Dispose()=>World.Dispose();
        }
        [Test] public void ActualTickRemovesAllDeliveredOriginalsBeforePublishingVictoryAndPreservesBudgets()
        {
            using var f=new Fixture();f.Time(5);
            Assert.IsTrue(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out string error),error);
            foreach(var actor in f.Originals)Assert.IsFalse(f.Em.Exists(actor));
            Assert.AreEqual(0,f.Em.GetBuffer<SkirmishTrackedUnit>(f.Session).Length);
            var capacity=f.Em.GetComponentData<SkirmishCapacityComponent>(f.Session);
            Assert.AreEqual(12,capacity.InfantryLive);Assert.AreEqual(1,capacity.GroundLive);Assert.AreEqual(18,capacity.SupplyLive);
            Assert.AreEqual(SkirmishOutcomeKind.Victory,f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session).Outcome);
            Assert.IsTrue(f.Record.Collector.TryExportCheckpointRows(f.Em,f.Session,out var rows,out error),error);
            foreach(var row in rows)Assert.AreEqual(SkirmishOriginalObjectiveDisposition.Delivered,row.Disposition);
            Assert.AreEqual(0,f.Em.GetComponentData<SkirmishMatchState>(f.Session).PlayerUnitsLost);
        }
        [Test] public void PauseDoesNotAdvanceDwellAndSurrenderUsesExistingNativeRequest()
        {
            using var f=new Fixture();f.Time(0,true);
            Assert.IsTrue(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out _));Assert.AreEqual(0,f.Record.Convoy.ElapsedTicks);
            foreach(var actor in f.Originals)Assert.IsTrue(f.Em.Exists(actor));
            var match=f.Em.GetComponentData<SkirmishMatchState>(f.Session);match.SurrenderRequested=1;f.Em.SetComponentData(f.Session,match);
            Assert.IsTrue(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out _));
            Assert.AreEqual(SkirmishEndReasonKind.Surrender,f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session).Reason);
        }
        [Test] public void MissingNativeOwnerFailsClosedWithoutSyntheticVictory()
        {
            using var f=new Fixture();f.Em.RemoveComponent<SkirmishNativeObjectiveRecord>(f.Session);
            Assert.IsFalse(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out _));
            Assert.AreEqual(SkirmishOutcomeKind.TechnicalFailure,f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session).Outcome);
            foreach(var actor in f.Originals)Assert.IsTrue(f.Em.Exists(actor));
        }
        [Test] public void ExactFailureReasonSurvivesKindAndMatchOwnerFailures()
        {
            using var f=new Fixture();
            var objective=f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session);
            objective.Kind=SkirmishObjectiveKind.Breakthrough;f.Em.SetComponentData(f.Session,objective);
            Assert.IsFalse(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out var error));
            Assert.AreEqual("Objective/session kind changed.",error);
            objective.Kind=SkirmishObjectiveKind.ConvoyEscort;objective.Terminal=0;objective.Outcome=SkirmishOutcomeKind.None;
            f.Em.SetComponentData(f.Session,objective);f.Em.RemoveComponent<SkirmishMatchState>(f.Session);
            Assert.IsFalse(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out error));
            Assert.AreEqual("Exact native match surrender owner missing/ambiguous.",error);
        }
        [Test] public void PartialDepartureFailurePublishesTechnicalFailureAndNeverVictory()
        {
            WithPartialDepartureFailure((em,session)=>Assert.AreEqual(SkirmishOutcomeKind.TechnicalFailure,
                em.GetComponentData<SkirmishObjectiveStateComponent>(session).Outcome));
        }
        // Test composition seam: keeps the actual failed native state alive so
        // checksum/recovery tests can assert their real capture API rejects it.
        public static void WithPartialDepartureFailure(Action<EntityManager,Entity> assertion)
        {
            using var f=new Fixture();f.Time(5);
            // The second original owns an unsafe linked actor. The first departure
            // can complete before that separate hierarchy fails preflight.
            var unrelated=f.Em.CreateEntity(typeof(UnitHealth));
            f.Em.SetComponentData(unrelated,new UnitHealth{Current=100,Max=100});
            f.Em.AddBuffer<LinkedEntityGroup>(f.Originals[1]).Add(new LinkedEntityGroup{Value=unrelated});
            Assert.IsFalse(SkirmishNativeObjectiveTick.TryTick(f.Em,f.Session,out var error));
            StringAssert.Contains("Departure hierarchy",error);
            Assert.IsFalse(f.Em.Exists(f.Originals[0]),"Control must exercise an actually completed earlier removal.");
            Assert.IsTrue(f.Em.Exists(f.Originals[1]));Assert.IsTrue(f.Em.Exists(f.Originals[2]));
            Assert.IsTrue(f.Em.Exists(unrelated));
            var objective=f.Em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session);
            Assert.AreEqual(1,objective.Terminal);Assert.AreEqual(SkirmishOutcomeKind.TechnicalFailure,objective.Outcome);
            Assert.AreEqual(SkirmishObjectiveStateKind.TechnicalFailure,objective.State);
            Assert.AreEqual(2,f.Em.GetBuffer<SkirmishTrackedUnit>(f.Session).Length);
            // The reducer's advanced terminal state is deliberately not a claim
            // that this partially failed native attempt may be checkpointed.
            Assert.AreEqual(SkirmishOutcomeKind.Victory,f.Record.Convoy.Outcome);
            assertion(f.Em,f.Session);
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishNativeObjectiveTickTests();
            t.ActualTickRemovesAllDeliveredOriginalsBeforePublishingVictoryAndPreservesBudgets();
            t.PauseDoesNotAdvanceDwellAndSurrenderUsesExistingNativeRequest();t.MissingNativeOwnerFailsClosedWithoutSyntheticVictory();
            t.ExactFailureReasonSurvivesKindAndMatchOwnerFailures();
            t.PartialDepartureFailurePublishesTechnicalFailureAndNeverVictory();
            Debug.Log("[SkirmishNativeObjectiveTickTests] result=Passed tests=5 nativeMissionReadiness=Pending");
        }
    }
}
#endif
