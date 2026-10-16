#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeObjectiveCheckpointTests
    {
        [Serializable] private sealed class Wire { public int Version=1; public string Payload, Checksum; }
        private sealed class Fixture : IDisposable
        {
            public readonly World World = new World("ObjectiveNativeSaveValidation");
            public readonly SkirmishResolvedSetup Setup = new SkirmishResolvedSetup {
                ObjectiveKind=SkirmishObjectiveKind.FrontlineControl, SizeId=SkirmishSizeId.Standard,
                LayoutBindingFingerprint=new string('c',64) };
            public SkirmishNativeCheckpointSnapshotService.Snapshot Saved;
            public SkirmishNativeObjectiveCheckpoint.Payload Payload;
            public Entity Session;
            public readonly Entity[] Actors=new Entity[2];
            public Fixture()
            {
                var em=World.EntityManager; var session=em.CreateEntity();Session=session;
                Setup.Structures=new[] {
                    new SkirmishResolvedStructureEntry { StructureId="base",FactionId=1,DesignatedBase=true,ObjectiveRoleId="objective.0" },
                    new SkirmishResolvedStructureEntry { StructureId="base",FactionId=2,DesignatedBase=true,ObjectiveRoleId="objective.1" } };
                em.AddComponentData(session,new SkirmishExpandedSessionComponent {
                    ObjectiveKind=SkirmishObjectiveKind.FrontlineControl, Phase=SkirmishSessionPhase.Playing, SpawnComplete=1 });
                em.AddComponentData(session,new SkirmishObjectiveClockComponent { ElapsedSeconds=1,Playing=1 });
                em.AddComponentData(session,new SkirmishObjectiveStateComponent { State=SkirmishObjectiveStateKind.Active });
                Saved=new SkirmishNativeCheckpointSnapshotService.Snapshot { Version=8,
                    SessionParts=SkirmishNativeCheckpointParts.Capture(em,session,_=>null),
                    Actors=new SkirmishNativeCheckpointSnapshotService.Actor[2] };
                Payload=new SkirmishNativeObjectiveCheckpoint.Payload { Kind=Setup.ObjectiveKind,
                    LogicalFingerprint=new string('a',64),GeometryFingerprint=new string('b',64),
                    LayoutBindingFingerprint=Setup.LayoutBindingFingerprint,MissionViewProfileFingerprint=new string('d',64),
                    Clock=new SkirmishObjectiveCollectorClock { LastAuthoritativeSeconds=1 },
                    Originals=new SkirmishObjectiveCollectorOriginalRow[2] };
                for(int i=0;i<2;i++)
                {
                    var actor=em.CreateEntity();string id="base."+(i+1);
                    Actors[i]=actor;
                    em.AddComponentData(actor,new SkirmishAttemptOwnedComponent { StableObjectId=id,FactionId=(byte)(i+1),IsStructure=1 });
                    em.AddComponentData(actor,new UnitHealth { Current=100,Max=100 });
                    em.AddComponentData(actor,new Faction { Id=(byte)(i+1) });
                    em.AddComponent<SkirmishSharedActorTag>(actor);
                    em.AddComponentData(actor,new SkirmishObjectiveRoleComponent {
                        StableObjectId="objective."+i,FactionId=(byte)(i+1),
                        Role=i==0?SkirmishObjectiveRoleKind.PlayerBase:SkirmishObjectiveRoleKind.EnemyBase });
                    Saved.Actors[i]=new SkirmishNativeCheckpointSnapshotService.Actor { Id=id,IsStructure=1,
                        Parts=SkirmishNativeCheckpointParts.Capture(em,actor,_=>null) };
                    Payload.Originals[i]=new SkirmishObjectiveCollectorOriginalRow { StableOriginalId=id,ObjectiveStableId="objective."+i,
                        Faction=(byte)(i+1),IsStructure=1,Role=i==0?SkirmishObjectiveRoleKind.PlayerBase:SkirmishObjectiveRoleKind.EnemyBase,
                        Disposition=SkirmishOriginalObjectiveDisposition.Live };
                }
                var reducer=SkirmishFrontlineReducer.Create(Setup.SizeId,"zone.a","zone.b","zone.c");
                reducer.ElapsedTicks=TimeSpan.TicksPerSecond;Payload.Reducer=JsonUtility.ToJson(reducer);
                Saved.Objective=Payload;
            }
            public bool Valid()=>SkirmishNativeObjectiveCheckpoint.Validate(Payload,Saved,Setup,out _);
            public void Dispose()=>World.Dispose();
        }

        [Test] public void SchemaPreservesBaseAssaultV7AndRejectsUnownedVersionCrossovers()
        {
            Assert.IsTrue(SkirmishNativeObjectiveCheckpoint.Schema(7,null,SkirmishObjectiveKind.BaseAssault));
            Assert.IsFalse(SkirmishNativeObjectiveCheckpoint.Schema(8,null,SkirmishObjectiveKind.BaseAssault));
            using var f=new Fixture();Assert.IsTrue(f.Valid());
            f.Saved.Version=7;Assert.IsFalse(f.Valid());
        }

        [Test] public void NativePayloadRejectsClockIdentityRosterAndReducerMutations()
        {
            using var f=new Fixture();Assert.IsTrue(f.Valid());
            var original=f.Payload.Clock;f.Payload.Clock.LastAuthoritativeSeconds=2;Assert.IsFalse(f.Valid());f.Payload.Clock=original;
            f.Payload.Clock.FractionalTicks=double.NaN;Assert.IsFalse(f.Valid());f.Payload.Clock=original;
            f.Payload.LayoutBindingFingerprint=new string('e',64);Assert.IsFalse(f.Valid());f.Payload.LayoutBindingFingerprint=f.Setup.LayoutBindingFingerprint;
            var rows=f.Payload.Originals;f.Payload.Originals=new[]{rows[0],rows[0]};Assert.IsFalse(f.Valid());f.Payload.Originals=rows;
            var row=rows[0];rows[0].Disposition=SkirmishOriginalObjectiveDisposition.Evacuated;Assert.IsFalse(f.Valid());rows[0]=row;
            var em=f.World.EntityManager;var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Actors[0]);
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(f.Session);
            objective.Terminal=1;objective.Outcome=SkirmishOutcomeKind.TechnicalFailure;em.SetComponentData(f.Session,objective);
            var sessionParts=f.Saved.SessionParts;f.Saved.SessionParts=SkirmishNativeCheckpointParts.Capture(em,f.Session,_=>null);
            Assert.IsFalse(f.Valid());f.Saved.SessionParts=sessionParts;
            owner.CapacityReleased=1;em.SetComponentData(f.Actors[0],owner);
            var parts=f.Saved.Actors[0].Parts;f.Saved.Actors[0].Parts=SkirmishNativeCheckpointParts.Capture(em,f.Actors[0],_=>null);
            Assert.IsFalse(f.Valid());f.Saved.Actors[0].Parts=parts;
            em.SetComponentData(f.Actors[0],new Faction { Id=2 });
            f.Saved.Actors[0].Parts=SkirmishNativeCheckpointParts.Capture(em,f.Actors[0],_=>null);Assert.IsFalse(f.Valid());f.Saved.Actors[0].Parts=parts;
            var state=JsonUtility.FromJson<SkirmishFrontlineState>(f.Payload.Reducer);state.DeadlineTicks++;f.Payload.Reducer=JsonUtility.ToJson(state);Assert.IsFalse(f.Valid());
        }

        [Test] public void NativeDescriptorChecksumCoversActualObjectivePayloadBytes()
        {
            using var f=new Fixture();string payload=JsonUtility.ToJson(f.Saved);
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant();
            string directory=Path.Combine(Path.GetTempPath(),"SkirmishObjectiveWire-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"native.json");
            try
            {
                File.WriteAllText(path,JsonUtility.ToJson(new Wire { Payload=payload,Checksum=hash }));
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryReadDescriptor(path,out var read));Assert.AreEqual(f.Payload.Reducer,read.Objective.Reducer);
                f.Payload.MissionViewProfileFingerprint=new string('f',64);
                File.WriteAllText(path,JsonUtility.ToJson(new Wire { Payload=JsonUtility.ToJson(f.Saved),Checksum=hash }));
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryReadDescriptor(path,out _));
            }
            finally { File.Delete(path);Directory.Delete(directory); }
        }

        [Test] public void MissingFreshBindingRejectsPreparationWithoutNativeMutation()
        {
            using var f=new Fixture();var em=f.World.EntityManager;
            int count=em.UniversalQuery.CalculateEntityCount();
            Assert.IsFalse(SkirmishNativeObjectiveCheckpoint.TryPrepare(em,f.Session,f.Saved,f.Setup,out var prepared,out _));
            Assert.IsNull(prepared);Assert.AreEqual(count,em.UniversalQuery.CalculateEntityCount());
            Assert.IsFalse(em.HasComponent<SkirmishNativeObjectiveRecord>(f.Session));
        }

        [Test] public void RealNativeCommitRollsBackSessionPartsWhenObjectiveBindingDisappears()
        {
            using var f=new Fixture();var em=f.World.EntityManager;
            var fresh=em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
            fresh.SpawnComplete=0;fresh.Phase=SkirmishSessionPhase.Spawning;em.SetComponentData(f.Session,fresh);
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=0;clock.Playing=0;em.SetComponentData(f.Session,clock);
            var staged=new SkirmishNativeCheckpointRecoveryPreparation.Prepared {
                WorldSequence=em.WorldUnmanaged.SequenceNumber,Session=f.Session,
                Objective=new SkirmishNativeObjectiveCheckpoint.Prepared(f.Payload,
                    new SkirmishNativeObjectiveRecoveryBinding(null,f.Payload.MissionViewProfileFingerprint)) };
            staged.Identities.Add("$session",f.Session);staged.Parts.Add(f.Session,f.Saved.SessionParts);
            // Exercise the actual native transaction and catch/Parts rollback,
            // after saved Playing/clock fields have been applied. No fixture save.
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryCommit.TryCommit(em,staged,out _));
            var after=em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
            Assert.AreEqual(SkirmishSessionPhase.Spawning,after.Phase);Assert.AreEqual(0,after.SpawnComplete);
            Assert.AreEqual(0,em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session).ElapsedSeconds);
            Assert.IsFalse(em.HasComponent<SkirmishNativeObjectiveRecord>(f.Session));Assert.IsFalse(staged.Committed);
        }

        [Test] public void ObjectiveMarkerCannotSpliceOntoOrdinarySavedActorOrCrossIntoBaseAssault()
        {
            using var world=new World("ObjectiveTruckClassification");var em=world.EntityManager;var actor=em.CreateEntity();
            em.AddComponentData(actor,new SkirmishUnitRoleComponent { Role=SkirmishRoleKind.Rifle });
            var saved=new SkirmishNativeCheckpointSnapshotService.Actor { Parts=SkirmishNativeCheckpointParts.Capture(em,actor,_=>null) };
            var ba=new SkirmishResolvedSetup { ObjectiveKind=SkirmishObjectiveKind.BaseAssault };
            Assert.IsTrue(SkirmishNativeConvoyCheckpoint.TryValidateClassification(ba,saved,out _));
            em.AddComponentData(actor,new SkirmishConvoyObjectiveTruckComponent { Slot=1 });
            saved.Parts=SkirmishNativeCheckpointParts.Capture(em,actor,_=>null);
            Assert.IsFalse(SkirmishNativeConvoyCheckpoint.TryValidateClassification(ba,saved,out _));
            var ce=new SkirmishResolvedSetup { ObjectiveKind=SkirmishObjectiveKind.ConvoyEscort };
            Assert.IsFalse(SkirmishNativeConvoyCheckpoint.TryValidateClassification(ce,saved,out _));
            em.RemoveComponent<SkirmishConvoyObjectiveTruckComponent>(actor);
            em.SetComponentData(actor,new SkirmishUnitRoleComponent { Role=SkirmishRoleKind.ConvoyLogic });
            saved.Parts=SkirmishNativeCheckpointParts.Capture(em,actor,_=>null);
            Assert.IsFalse(SkirmishNativeConvoyCheckpoint.TryValidateClassification(ba,saved,out _));
            Assert.IsFalse(SkirmishNativeConvoyCheckpoint.TryValidateClassification(ce,saved,out _));
        }
    }
}
#endif
