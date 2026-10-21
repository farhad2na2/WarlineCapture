using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class S002RuntimeBlockerWitnessTests
    {
        sealed class Fixture : IDisposable
        {
            public GridConfig Grid = new GridConfig { Width = 8, Height = 8, CellSize = 1, Origin = float3.zero };
            public DynamicBlockerComponent Blockers;
            public DynamicOccupancyComponent Occupancy;
            public Fixture()
            {
                Blockers = new DynamicBlockerComponent { GridSize = 64, Blocked = new NativeBitArray(64, Allocator.Persistent),
                    Counts = new NativeArray<int>(64, Allocator.Persistent), FriendlyPassFactionIds = new NativeArray<byte>(64, Allocator.Persistent) };
                Occupancy = new DynamicOccupancyComponent { GridSize = 64, Occupied = new NativeBitArray(64, Allocator.Persistent) };
            }
            public void Dispose()
            { Blockers.Blocked.Dispose(); Blockers.Counts.Dispose(); Blockers.FriendlyPassFactionIds.Dispose(); Occupancy.Occupied.Dispose(); }
        }
        static OperationMapReadinessComponent Ready() => new OperationMapReadinessComponent { Generation = 7,
            ReadyFlags = OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene | OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface,
            RequiredFlags = OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene | OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface };
        static RuntimeGridBlockerDependencyComponent Dependency() => new RuntimeGridBlockerDependencyComponent { ReadyForDependents = 1, SpawnOnStart = 1, Spawned = 1 };
        [Test] public void MissingMasksNeverBecomeAnEmptyAvailableSnapshot()
        {
            using var f = new Fixture();
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(f.Grid, default, f.Occupancy));
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(f.Grid, f.Blockers, default));
            Assert.Throws<InvalidOperationException>(() => S002RuntimeBlockerWitness.CopyAvailableStorage(f.Grid, default, f.Occupancy));
        }
        [Test] public void ShapeAndFactionMaskMismatchRefuse()
        {
            using var f = new Fixture(); var grid = f.Grid; grid.CellSize = float.NaN;
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(grid, f.Blockers, f.Occupancy));
            grid = f.Grid; grid.Origin = new float3(float.PositiveInfinity, 0, 0);
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(grid, f.Blockers, f.Occupancy));
            using var shortMask = new NativeArray<byte>(63, Allocator.Temp);
            var bad = f.Blockers; bad.FriendlyPassFactionIds = shortMask;
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(f.Grid, bad, f.Occupancy));
            bad = f.Blockers; bad.GridSize = 63;
            Assert.IsNotNull(S002RuntimeBlockerWitness.StorageUnavailableReason(f.Grid, bad, f.Occupancy));
        }
        [Test] public void GenerationsReadinessAndSourceValidationAreMandatory()
        {
            Assert.IsNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, Ready(), Dependency()));
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(0, 0, 1, Ready(), Dependency()));
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 8, 1, Ready(), Dependency()));
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 0, Ready(), Dependency()));
            var ready = Ready(); ready.Generation = 8;
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()));
            ready = Ready(); ready.RequiredFlags = OperationMapReadinessFlags.None;
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()));
            ready = Ready(); ready.ReadyFlags &= ~OperationMapReadinessFlags.MapSurface;
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()));
            ready = Ready(); ready.FailedFlags = OperationMapReadinessFlags.SubScene;
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()));
        }
        [Test] public void PendingCityAndBlockerFinalizationCannotLookReady()
        {
            foreach (string field in new[] { "ReadyForDependents", "PendingCity", "CityGenerating", "SpawnFinalizing", "Spawned" })
            {
                var dependency = Dependency();
                switch (field)
                {
                    case "ReadyForDependents": dependency.ReadyForDependents = 0; break;
                    case "PendingCity": dependency.PendingCity = 1; break;
                    case "CityGenerating": dependency.CityGenerating = 1; break;
                    case "SpawnFinalizing": dependency.SpawnFinalizing = 1; break;
                    case "Spawned": dependency.Spawned = 0; break;
                }
                Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, Ready(), dependency), field);
            }
        }
        [Test] public void OwnedBlockedFactionAndOccupancyCopiesSurviveSourceMutationAndDisposal()
        {
            var f = new Fixture();
            try
            {
                var bits = f.Blockers.Blocked; bits.Set(3, true); f.Blockers.Counts[3] = 2; f.Blockers.FriendlyPassFactionIds[3] = 2;
                bits = f.Occupancy.Occupied; bits.Set(11, true);
                var copy = S002RuntimeBlockerWitness.CopyAvailableStorage(f.Grid, f.Blockers, f.Occupancy);
                try
                {
                    bits = f.Blockers.Blocked; bits.Set(3, false); f.Blockers.Counts[3] = 0; f.Blockers.FriendlyPassFactionIds[3] = 1;
                    bits = f.Occupancy.Occupied; bits.Set(11, false);
                    Assert.IsTrue(copy.Blocked.IsSet(3)); Assert.AreEqual(2, copy.Counts[3]); Assert.AreEqual(2, copy.FriendlyPassFactionIds[3]); Assert.IsTrue(copy.Occupied.IsSet(11));
                    f.Dispose(); f = null;
                    Assert.IsTrue(copy.Blocked.IsSet(3)); Assert.AreEqual(2, copy.FriendlyPassFactionIds[3]); Assert.IsTrue(copy.Occupied.IsSet(11));
                    copy.Dispose(); copy.Dispose();
                }
                finally { copy.Dispose(); }
            }
            finally { f?.Dispose(); }
        }
        [Test] public void EditModeCannotFabricateNormalRuntimeAvailability()
        {
            Assert.IsFalse(S002RuntimeBlockerWitness.TryCaptureCurrent(out var snapshot, out var facts));
            Assert.IsNull(snapshot); Assert.IsFalse((bool)facts["available"]);
            Assert.AreEqual("NormalPlaySessionUnavailable", (string)facts["unavailableReason"]);
            Assert.IsFalse((bool)facts["sourceFitCertified"]); Assert.IsFalse((bool)facts["missionReady"]);
        }
        public static void RunFocusedValidation()
        {
            var suite = new S002RuntimeBlockerWitnessTests();
            try
            {
                suite.MissingMasksNeverBecomeAnEmptyAvailableSnapshot();
                suite.ShapeAndFactionMaskMismatchRefuse();
                suite.GenerationsReadinessAndSourceValidationAreMandatory();
                suite.PendingCityAndBlockerFinalizationCannotLookReady();
                suite.OwnedBlockedFactionAndOccupancyCopiesSurviveSourceMutationAndDisposal();
                suite.EditModeCannotFabricateNormalRuntimeAvailability();
                Debug.Log("[S002RuntimeBlockerWitnessTests] result=Passed controls=6 actualRuntimeObserved=0 sourceFitCertified=0 missionReady=0");
            }
            catch (Exception error) { Debug.LogError("[S002RuntimeBlockerWitnessTests] result=Failed\n" + error); throw; }
        }
    }
}
