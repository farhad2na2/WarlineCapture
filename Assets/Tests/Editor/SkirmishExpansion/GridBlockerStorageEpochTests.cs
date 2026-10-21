using System;
using Game.Components;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Isolated actual owner + writer + ECB controls, never normal mission acceptance.
    public sealed class GridBlockerStorageEpochTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly World World = new World("actual-grid-storage-epoch-controls");
            public EntityManager Em => World.EntityManager;
            public Entity Grid, Root, Section;
            public readonly Unity.Entities.Hash128 Guid = new Unity.Entities.Hash128("33333333333333333333333333333333");
            readonly SystemHandle init, writer;
            public Fixture()
            {
                Root = Em.CreateEntity(typeof(SceneReference)); Em.SetComponentData(Root, new SceneReference { SceneGUID = Guid });
                Section = Em.CreateEntity(typeof(SceneSectionData), typeof(SceneEntityReference));
                Em.SetComponentData(Section, new SceneSectionData { SceneGUID = Guid });
                Em.SetComponentData(Section, new SceneEntityReference { SceneEntity = Root });
                Em.AddBuffer<ResolvedSectionEntity>(Root).Add(new ResolvedSectionEntity { SectionEntity = Section });
                Grid = NewGrid();
                World.SetTime(new TimeData(1, 1f / 60));
                init = World.CreateSystem<DynamicBlockerInitSystem>(); writer = World.CreateSystem<StaticGridBlockerUpdateSystem>();
                Ensure();
            }
            public Entity NewGrid()
            {
                var e = Em.CreateEntity(typeof(GridConfig));
                Em.SetComponentData(e, new GridConfig { Width = 8, Height = 8, CellSize = 1, Origin = float3.zero });
                Em.AddSharedComponent(e, new SceneTag { SceneEntity = Section }); return e;
            }
            public Entity Block(int x, int z, int width = 2, int height = 2, byte pass = byte.MaxValue)
            {
                var e = Em.CreateEntity(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
                Em.SetComponentData(e, new UnitGrid { Cell = new int2(x, z) });
                Em.SetComponentData(e, new GridBlockerSize { Size = new int2(width, height) });
                Em.AddSharedComponent(e, new SceneTag { SceneEntity = Section });
                if (pass != byte.MaxValue) Em.AddComponentData(e, new FriendlyPassGridBlocker { AllowedFactionId = pass }); return e;
            }
            public DynamicBlockerComponent Data => Em.GetComponentData<DynamicBlockerComponent>(Grid);
            public GridConfig Config => Em.GetComponentData<GridConfig>(Grid);
            public void Ensure() => init.Update(World.Unmanaged);
            public void Tick() { writer.Update(World.Unmanaged); Em.CompleteAllTrackedJobs(); }
            public void ReplaceStorage()
            {
                var data = Data; data.GridSize = -1; Em.SetComponentData(Grid, data); Ensure();
            }
            public void Expect(Func<int, int, int> count, Func<int, int, byte> pass = null)
            {
                var data = Data;
                for (int z = 0; z < 8; z++) for (int x = 0; x < 8; x++)
                {
                    int expected = count(x, z), i = x + 8 * z;
                    Assert.AreEqual(expected, data.Counts[i], $"count {x},{z}");
                    Assert.AreEqual(expected > 0, data.Blocked.IsSet(i), $"blocked {x},{z}");
                    Assert.AreEqual(pass == null ? byte.MaxValue : pass(x, z), data.FriendlyPassFactionIds[i], $"pass {x},{z}");
                }
            }
            public string Attribution() => S002RuntimeBlockerWitness.WriterInputOwnershipUnavailableReason(Em, Grid, Guid, Root, new FixedString64Bytes("current"));
            public void Dispose() => World.Dispose();
        }
        static bool Rect(int x, int z, int ox, int oz, int w = 2, int h = 2) => x >= ox && x < ox + w && z >= oz && z < oz + h;
        static StaticGridBlockerSurfaceOpeningComponent Opening() => new StaticGridBlockerSurfaceOpeningComponent {
            Center = new float2(3.5f, 3.5f), Right = new float2(1, 0), Forward = new float2(0, 1), HalfExtents = new float2(.5f, .5f) };

        [Test] public void InitialAllocationCommitsOnlyAfterActualWriterAndEcb()
        {
            using var f = new Fixture(); var e = f.Block(1, 2);
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data));
            Assert.IsFalse(f.Em.HasComponent<StaticBlockerPrevBounds>(e)); f.Tick();
            f.Expect((x, z) => Rect(x, z, 1, 2) ? 1 : 0);
            Assert.IsTrue(RuntimeGridBlockerEpoch.IsCommitted(f.Config, f.Data));
            var history = f.Em.GetComponentData<StaticBlockerPrevBounds>(e);
            Assert.AreEqual(f.Grid, history.AppliedGrid); Assert.AreEqual(f.Data.StorageEpoch, history.StorageEpoch);
        }
        [Test] public void SameSizeResetReappliesUnchangedBoundsOpeningAndPass()
        {
            using var f = new Fixture(); var e = f.Block(2, 2, 3, 3, 1); f.Em.AddComponentData(e, Opening()); f.Tick();
            var oldEpoch = f.Data.StorageEpoch; f.ReplaceStorage();
            Assert.IsTrue(f.Em.HasComponent<StaticBlockerPrevBounds>(e)); Assert.Greater(f.Data.StorageEpoch, oldEpoch);
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data));
            f.Tick();
            f.Expect((x, z) => Rect(x, z, 2, 2, 3, 3) && !(x == 3 && z == 3) ? 1 : 0,
                (x, z) => Rect(x, z, 2, 2, 3, 3) ? (byte)1 : byte.MaxValue);
        }
        [Test] public void OldEpochMoveAndRemovalNeverSubtractIntoZeroMasks()
        {
            using var f = new Fixture(); var moved = f.Block(1, 1); var removed = f.Block(4, 4); f.Tick(); f.ReplaceStorage();
            f.Em.SetComponentData(moved, new UnitGrid { Cell = new int2(3, 1) }); f.Em.RemoveComponent<StaticGridBlocker>(removed); f.Tick();
            f.Expect((x, z) => Rect(x, z, 3, 1) ? 1 : 0);
            Assert.IsFalse(f.Em.HasComponent<StaticBlockerPrevBounds>(removed));
        }
        [Test] public void ExistingPhasePassOrderingAndRemovalClearArePreserved()
        {
            using var f = new Fixture(); var a = f.Block(2, 2); var b = f.Block(2, 2); f.Tick();
            f.Em.AddComponentData(a, new FriendlyPassGridBlocker { AllowedFactionId = 1 });
            f.Em.AddComponentData(b, new FriendlyPassGridBlocker { AllowedFactionId = 2 });
            byte last = byte.MaxValue;
            using (var q = f.Em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize), typeof(StaticBlockerPrevBounds)))
            using (var order = q.ToEntityArray(Allocator.Temp))
                foreach (var e in order) last = f.Em.GetComponentData<FriendlyPassGridBlocker>(e).AllowedFactionId;
            f.Tick(); f.Expect((x, z) => Rect(x, z, 2, 2) ? 2 : 0, (x, z) => Rect(x, z, 2, 2) ? last : byte.MaxValue);
            f.Em.RemoveComponent<StaticGridBlocker>(b); f.Tick();
            // Preserve the legacy third-phase clear, even though a surviving pass overlaps it.
            f.Expect((x, z) => Rect(x, z, 2, 2) ? 1 : 0);
            f.Block(2, 2, 2, 2, 2); f.ReplaceStorage(); f.Tick();
            // Replay keeps new phase (pass2) before existing phase (pass1), without sorting.
            f.Expect((x, z) => Rect(x, z, 2, 2) ? 2 : 0,
                (x, z) => Rect(x, z, 2, 2) ? (byte)1 : byte.MaxValue);
        }
        [Test] public void InterruptedPendingPassRebuildsAndExhaustionNeverCertifies()
        {
            using var f = new Fixture(); f.Block(1, 1); f.Tick();
            var data = f.Data; data.Counts[0] = 99; data.WriteInProgress = 1; data.AppliedStorageEpoch = 0; f.Em.SetComponentData(f.Grid, data);
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data)); f.Tick();
            f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0);
            data = f.Data; data.StorageEpoch = ulong.MaxValue; data.WriteInProgress = 1; f.Em.SetComponentData(f.Grid, data); f.Tick();
            f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0);
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data));
        }
        [Test] public void NoOpEnsureKeepsEpochButEqualCountLayoutChangeReplays()
        {
            using var f = new Fixture(); f.Block(1, 1); f.Tick(); var before = f.Data; f.Ensure();
            Assert.AreEqual(before.StorageEpoch, f.Data.StorageEpoch); Assert.AreEqual(before.AppliedRevision, f.Data.AppliedRevision);
            var config = f.Config; config.Origin.x = .25f; f.Em.SetComponentData(f.Grid, config);
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data)); f.Tick();
            Assert.Greater(f.Data.StorageEpoch, before.StorageEpoch); f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0);
        }
        [Test] public void RetiredGridHistoryCannotBecomeNewGridSubtraction()
        {
            using var f = new Fixture(); var e = f.Block(1, 1); f.Tick(); var old = f.Grid;
            f.Em.DestroyEntity(old); f.World.CreateSystem<RuntimeGridStorageCleanupSystem>().Update(f.World.Unmanaged);
            f.Grid = f.NewGrid(); f.Ensure(); f.Tick();
            f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0);
            Assert.AreEqual(f.Grid, f.Em.GetComponentData<StaticBlockerPrevBounds>(e).AppliedGrid);
            Assert.AreNotEqual(old, f.Grid);
        }
        [Test] public void ForeignAttemptContributesToGameplayButCannotQualifySource()
        {
            using var f = new Fixture(); var e = f.Block(4, 4);
            f.Em.AddComponentData(e, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes("prior") }); f.Tick();
            f.Expect((x, z) => Rect(x, z, 4, 4) ? 1 : 0); Assert.AreEqual("ForeignAttemptWriterContributor", f.Attribution());
            f.Em.SetComponentData(e, new SkirmishAttemptOwnedComponent { SessionId = new FixedString64Bytes("current") });
            Assert.AreEqual("AttemptWriterGridSourceBindingUnavailable", f.Attribution());
        }
        [Test] public void HelperAbsenceDoesNotDisableAuthoredWriterOrGrantRuntimeCapture()
        {
            using var f = new Fixture(); f.Block(1, 1); f.Tick();
            using var dependencies = f.Em.CreateEntityQuery(typeof(RuntimeGridBlockerDependencyComponent)); Assert.IsTrue(dependencies.IsEmptyIgnoreFilter);
            f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0); Assert.IsNull(f.Attribution());
            // Synthetic scene tags establish only input attribution, not actual loaded normal Play/policy.
            Assert.IsFalse(S002RuntimeBlockerWitness.TryCaptureCurrent(out var snapshot, out var facts));
            Assert.IsNull(snapshot); Assert.IsFalse((bool)facts["missionReady"]);
        }
        [Test] public void InputAndStorageChangesInvalidateObservationAcrossCopyBoundary()
        {
            using var f = new Fixture(); var e = f.Block(1, 1); f.Tick(); Assert.IsNull(f.Attribution());
            var before = f.Data; var occupancy = f.Em.GetComponentData<DynamicOccupancyComponent>(f.Grid);
            using var copy = S002RuntimeBlockerWitness.CopyAvailableStorage(f.Config, before, occupancy);
            f.Em.SetComponentData(e, new UnitGrid { Cell = new int2(3, 3) });
            Assert.AreEqual("WriterInputChangedSinceApplication", f.Attribution());
            f.ReplaceStorage(); Assert.IsFalse(S002RuntimeBlockerWitness.SameWriterEpoch(before, f.Data));
            Assert.IsNotNull(S002RuntimeBlockerWitness.WriterEpochUnavailableReason(f.Config, f.Data));
            Assert.IsTrue(copy.Blocked.IsSet(9)); f.Tick(); f.Expect((x, z) => Rect(x, z, 3, 3) ? 1 : 0);
        }
        [Test] public void BlockerResetPreservesSeparateOccupancyAndOccupancyResetPreservesBlockers()
        {
            using var f = new Fixture(); f.Block(1, 1); f.Tick();
            var occupancy = f.Em.GetComponentData<DynamicOccupancyComponent>(f.Grid); var bits = occupancy.Occupied; bits.Set(63, true);
            f.ReplaceStorage(); f.Tick(); occupancy = f.Em.GetComponentData<DynamicOccupancyComponent>(f.Grid); Assert.IsTrue(occupancy.Occupied.IsSet(63));
            var epoch = f.Data.StorageEpoch; occupancy.GridSize = -1; f.Em.SetComponentData(f.Grid, occupancy); f.Ensure();
            Assert.AreEqual(epoch, f.Data.StorageEpoch); f.Expect((x, z) => Rect(x, z, 1, 1) ? 1 : 0);
        }
        [Test] public void DestroyedHistoryAndUnknownContributorRemainSeparateFromCertification()
        {
            using var f = new Fixture(); var e = f.Block(1, 1); f.Em.AddComponentData(e, Opening()); f.Tick();
            f.Em.DestroyEntity(e); Assert.AreEqual("WriterRemovalNotApplied", f.Attribution());
            f.Tick(); f.Expect((x, z) => 0); Assert.IsFalse(f.Em.HasComponent<StaticBlockerPrevBounds>(e));
            var unknown = f.Block(4, 4); f.Em.RemoveComponent<SceneTag>(unknown); f.Tick();
            f.Expect((x, z) => Rect(x, z, 4, 4) ? 1 : 0);
            Assert.AreEqual("WriterContributorOwnership:SceneTagUnavailable", f.Attribution());
            f.Em.RemoveComponent<UnitGrid>(unknown); f.Tick();
            Assert.AreEqual("WriterMalformedHistoryNotInCurrentInputDomain", f.Attribution());
        }
        public static void RunFocusedValidation()
        {
            var t = new GridBlockerStorageEpochTests();
            try
            {
                t.InitialAllocationCommitsOnlyAfterActualWriterAndEcb(); t.SameSizeResetReappliesUnchangedBoundsOpeningAndPass();
                t.OldEpochMoveAndRemovalNeverSubtractIntoZeroMasks(); t.ExistingPhasePassOrderingAndRemovalClearArePreserved();
                t.InterruptedPendingPassRebuildsAndExhaustionNeverCertifies(); t.NoOpEnsureKeepsEpochButEqualCountLayoutChangeReplays();
                t.RetiredGridHistoryCannotBecomeNewGridSubtraction(); t.ForeignAttemptContributesToGameplayButCannotQualifySource();
                t.HelperAbsenceDoesNotDisableAuthoredWriterOrGrantRuntimeCapture(); t.InputAndStorageChangesInvalidateObservationAcrossCopyBoundary();
                t.BlockerResetPreservesSeparateOccupancyAndOccupancyResetPreservesBlockers(); t.DestroyedHistoryAndUnknownContributorRemainSeparateFromCertification();
                Debug.Log("[GridBlockerStorageEpochTests] result=Passed controls=12 scope=IsolatedActualWriterNotNormalMission sourceFitCertified=0 missionReady=0");
            }
            catch (Exception error) { Debug.LogError("[GridBlockerStorageEpochTests] result=Failed\n" + error); throw; }
        }
    }
}
