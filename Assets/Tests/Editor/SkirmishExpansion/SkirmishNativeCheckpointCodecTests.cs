using System;
using System.IO;
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
    public sealed class SkirmishNativeCheckpointCodecTests
    {
        [Test]
        public void NativeWireChecksIntegrityAndExactMissionCompatibility()
        {
            using var world = new World(nameof(NativeWireChecksIntegrityAndExactMissionCompatibility));
            var snapshot = Capture(world.EntityManager, out var setup, out _);
            string wire = SkirmishNativeCheckpointCodec.Encode(snapshot, setup);
            Assert.IsNotNull(wire);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out var decoded));
            Assert.AreEqual("rifle-1", decoded.Actors[0].Id);
            Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(wire.Replace("rifle-1", "rifle-2"), setup, out _));
            setup.ContentVersion++;
            Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out _));
            setup.ContentVersion--;
            setup.Seed++;
            Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out _));
            setup.Seed--;
            setup.ContentHash = "different-source";
            Assert.IsFalse(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out _));
            setup.ContentHash = "source-fixture";
            snapshot.SessionParts[0].Layout ^= 1;
            Assert.IsNull(SkirmishNativeCheckpointCodec.Encode(snapshot, setup));
        }

        [Test]
        public void AtomicReplacementPreservesPreviousSaveAndRejectsIncompleteCapture()
        {
            using var world = new World(nameof(AtomicReplacementPreservesPreviousSaveAndRejectsIncompleteCapture));
            var em = world.EntityManager;
            var snapshot = Capture(em, out var setup, out var actor);
            string directory = Path.Combine(Path.GetTempPath(), "warline-native-checkpoint-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "match.json");
            try
            {
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryWriteAtomic(path, snapshot, setup));
                string previous = File.ReadAllText(path);
                em.SetComponentData(actor, new UnitHealth { Current = 11, Max = 100 });
                var changed = CaptureExisting(em);
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryWriteAtomic(path, changed, setup));
                Assert.AreEqual(previous, File.ReadAllText(path + ".bak"));
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryRead(path, setup, out var saved));
                Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(saved.Actors[0].Parts, out UnitHealth health));
                Assert.AreEqual(11, health.Current);
                string current = File.ReadAllText(path);
                changed.Actors[0].Parts = null;
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryWriteAtomic(path, changed, setup));
                Assert.AreEqual(current, File.ReadAllText(path));
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
                Assert.IsFalse(SkirmishNativeCheckpointCodec.TryRead(directory, setup, out _));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void PersistentSameCellMapReferencesRoundTripAndRejectChangedOrDuplicateIdentity()
        {
            using var world = new World(nameof(PersistentSameCellMapReferencesRoundTripAndRejectChangedOrDuplicateIdentity));
            var em = world.EntityManager;
            Capture(em, out var setup, out _);
            for (int i = 0; i < 2; i++)
            {
                var wall = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey), typeof(OperationMapBuildingIdentity));
                em.SetComponentData(wall, new RuntimeBuildingCombatInfo
                { RuntimeBuildingId = 44 + i, OriginCell = new Unity.Mathematics.int2(926, 373) });
                em.SetComponentData(wall, new UnitSourcePrefabKey { Value = "Wall_Dirt_Straight" });
                em.SetComponentData(wall, new OperationMapBuildingIdentity
                { OperationMapId = "opmap.skirmish.desert_base_01", StableId = "wall." + i });
            }
            var saved = CaptureExisting(em);
            Assert.AreEqual(2, saved.MapReferences.Length);
            string wire = SkirmishNativeCheckpointCodec.Encode(saved, setup);
            Assert.IsNotNull(wire, "A valid native source census must be persistable.");
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire, setup, out var decoded));
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadComponent(decoded.MapReferences[0].Parts,
                out OperationMapBuildingIdentity source));
            Assert.IsFalse(source.StableId.IsEmpty);
            string original = saved.MapReferences[0].Id;
            saved.MapReferences[0].Id = "map/source/opmap.skirmish.desert_base_01/changed-wall";
            Assert.IsNull(SkirmishNativeCheckpointCodec.Encode(saved, setup));
            saved.MapReferences[0].Id = original;
            saved.MapReferences[1].Id = original;
            Assert.IsNull(SkirmishNativeCheckpointCodec.Encode(saved, setup));

            // Preserve cell-based references for sources without persistent IDs.
            using (var sources = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo)))
                em.DestroyEntity(sources);
            var legacy = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey));
            em.SetComponentData(legacy, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = 77, OriginCell = new Unity.Mathematics.int2(3, 4) });
            em.SetComponentData(legacy, new UnitSourcePrefabKey { Value = "legacy-wall" });
            Assert.IsNotNull(SkirmishNativeCheckpointCodec.Encode(CaptureExisting(em), setup));
        }

        internal static SkirmishNativeCheckpointSnapshotService.Snapshot Capture(EntityManager em,
            out SkirmishResolvedSetup setup, out Entity actor)
        {
            setup = new SkirmishResolvedSetup
            {
                ObjectiveKind = SkirmishObjectiveKind.BaseAssault,
                CatalogId = "S005", DefinitionId = "skirmish.s005", ContentVersion = 2,
                ContentHash = "source-fixture", OperationMapId = "campaign-existing-map", LayoutId = "logical-sector",
                SetupHash = 123, Seed = 104734, SizeId = SkirmishSizeId.Standard, DifficultyId = SkirmishDifficultyId.Regular
            };
            Entity session = em.CreateEntity();
            em.AddComponentData(session, new SkirmishExpandedSessionComponent
            {
                ObjectiveKind = setup.ObjectiveKind,
                SessionId = new FixedString64Bytes("native-codec-fixture"), CatalogId = new FixedString64Bytes(setup.CatalogId),
                DefinitionId = new FixedString64Bytes(setup.DefinitionId), ContentVersion = setup.ContentVersion,
                SetupHash = setup.SetupHash, Seed = setup.Seed, SizeId = setup.SizeId, DifficultyId = setup.DifficultyId
            });
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = setup });
            actor = em.CreateEntity(typeof(SkirmishSharedActorTag));
            em.AddComponentData(actor, new SkirmishAttemptOwnedComponent
            { SessionId = new FixedString64Bytes("native-codec-fixture"), StableObjectId = new FixedString64Bytes("rifle-1") });
            em.AddComponentData(actor, new UnitSourcePrefabKey { Value = new FixedString64Bytes("rifle") });
            em.AddComponentData(actor, new UnitHealth { Current = 37, Max = 100 });
            return CaptureExisting(em);
        }

        private static SkirmishNativeCheckpointSnapshotService.Snapshot CaptureExisting(EntityManager em)
        {
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, sessions.GetSingletonEntity(),
                _ => "prefab", out var snapshot, out var failure), failure);
            Assert.AreEqual(7, snapshot.Version, "The minimal codec fixture explicitly exercises legacy BA7.");
            Assert.IsNull(snapshot.Objective);
            return snapshot;
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointCodecTests();
            suite.NativeWireChecksIntegrityAndExactMissionCompatibility();
            suite.AtomicReplacementPreservesPreviousSaveAndRejectsIncompleteCapture();
            suite.PersistentSameCellMapReferencesRoundTripAndRejectChangedOrDuplicateIdentity();
            Debug.Log("[SkirmishNativeCheckpointCodecTests] result=Passed tests=3 nativeRecoveredSessions=0");
        }
    }
}
