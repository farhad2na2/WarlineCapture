using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
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
            ReadyFlags = S002RuntimeBlockerWitness.MandatorySourceReadiness,
            RequiredFlags = S002RuntimeBlockerWitness.MandatorySourceReadiness };
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
        [Test] public void EveryMandatoryFlagMustBeRequiredAndReady()
        {
            foreach (OperationMapReadinessFlags flag in Enum.GetValues(typeof(OperationMapReadinessFlags)))
            {
                if (flag == OperationMapReadinessFlags.None) continue;
                var ready = Ready(); ready.RequiredFlags &= ~flag; ready.ReadyFlags &= ~flag;
                Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()), "omitted " + flag);
                ready = Ready(); ready.RequiredFlags &= ~flag;
                Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()), "undeclared " + flag);
                ready = Ready(); ready.ReadyFlags &= ~flag;
                Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, ready, Dependency()), "not ready " + flag);
            }
            var metadataOnly = Ready(); metadataOnly.RequiredFlags = metadataOnly.ReadyFlags = OperationMapReadinessFlags.Metadata;
            Assert.IsNotNull(S002RuntimeBlockerWitness.ReadinessUnavailableReason(7, 7, 1, metadataOnly, Dependency()));
        }

        // Synthetic scene ownership only: does not establish SceneSystem-loaded runtime acceptance.
        sealed class OwnershipFixture : IDisposable
        {
            public readonly World World = new World("S002 synthetic ownership controls");
            public EntityManager Em => World.EntityManager;
            public readonly Unity.Entities.Hash128 Guid = new Unity.Entities.Hash128("11111111111111111111111111111111");
            public Entity Root, Section, Grid, Dependency;
            public OwnershipFixture()
            {
                Root = Em.CreateEntity(typeof(SceneReference));
                Em.SetComponentData(Root, new SceneReference { SceneGUID = Guid });
                Section = Em.CreateEntity(typeof(SceneSectionData), typeof(SceneEntityReference));
                Em.SetComponentData(Section, new SceneSectionData { SceneGUID = Guid });
                Em.SetComponentData(Section, new SceneEntityReference { SceneEntity = Root });
                Em.AddBuffer<ResolvedSectionEntity>(Root).Add(new ResolvedSectionEntity { SectionEntity = Section });
                Grid = Em.CreateEntity(); Dependency = Em.CreateEntity();
                Em.AddSharedComponent(Grid, new SceneTag { SceneEntity = Section });
                Em.AddSharedComponent(Dependency, new SceneTag { SceneEntity = Section });
            }
            public string Reason() => S002RuntimeBlockerWitness.OwnershipUnavailableReason(Em, Grid, Dependency, Guid, Root);
            public void Dispose() => World.Dispose();
        }
        [Test] public void GlobalDependencyAndMissingGridProvenanceRefuse()
        {
            using var f = new OwnershipFixture(); Assert.IsNull(f.Reason());
            f.Em.RemoveComponent<SceneTag>(f.Dependency);
            Assert.AreEqual("BlockerDependencySourceOwnership:SceneTagUnavailable", f.Reason());
            f.Em.AddSharedComponent(f.Dependency, new SceneTag { SceneEntity = f.Section });
            f.Em.RemoveComponent<SceneTag>(f.Grid);
            Assert.AreEqual("GridSourceOwnership:SceneTagUnavailable", f.Reason());
        }
        [Test] public void ForeignStaleAndUnownedSourceLifecycleRefuse()
        {
            using var f = new OwnershipFixture();
            var foreignGuid = new Unity.Entities.Hash128("22222222222222222222222222222222");
            f.Em.SetComponentData(f.Section, new SceneSectionData { SceneGUID = foreignGuid });
            Assert.AreEqual("GridSourceOwnership:AuthoredSourceOwnerMismatch", f.Reason());
            f.Em.SetComponentData(f.Section, new SceneSectionData { SceneGUID = f.Guid });
            var foreignRoot = f.Em.CreateEntity(typeof(SceneReference));
            f.Em.SetComponentData(f.Section, new SceneEntityReference { SceneEntity = foreignRoot });
            Assert.AreEqual("GridSourceOwnership:AuthoredSourceOwnerMismatch", f.Reason());
            f.Em.SetComponentData(f.Section, new SceneEntityReference { SceneEntity = f.Root });
            f.Em.GetBuffer<ResolvedSectionEntity>(f.Root).Clear();
            Assert.AreEqual("GridSourceOwnership:SectionNotOwnedByLoadedSource", f.Reason());
            f.Em.GetBuffer<ResolvedSectionEntity>(f.Root).Add(new ResolvedSectionEntity { SectionEntity = f.Section });
            f.Em.SetSharedComponent(f.Dependency, new SceneTag { SceneEntity = foreignRoot });
            Assert.AreEqual("BlockerDependencySourceOwnership:SectionLifecycleUnavailable", f.Reason());
            var foreignSection = f.Em.CreateEntity(typeof(SceneSectionData), typeof(SceneEntityReference));
            f.Em.SetComponentData(foreignSection, new SceneSectionData { SceneGUID = foreignGuid });
            f.Em.SetComponentData(foreignSection, new SceneEntityReference { SceneEntity = f.Root });
            f.Em.SetSharedComponent(f.Dependency, new SceneTag { SceneEntity = foreignSection });
            Assert.AreEqual("BlockerDependencySourceOwnership:AuthoredSourceOwnerMismatch", f.Reason());
            f.Em.SetSharedComponent(f.Dependency, new SceneTag { SceneEntity = f.Section });
            f.Em.SetComponentData(f.Root, new SceneReference { SceneGUID = foreignGuid });
            Assert.AreEqual("AuthoredSourceLifecycleUnavailable", f.Reason());
            f.Em.SetComponentData(f.Root, new SceneReference { SceneGUID = f.Guid });
            f.Em.DestroyEntity(f.Section);
            Assert.AreEqual("GridSourceOwnership:SectionLifecycleUnavailable", f.Reason());
            Assert.AreEqual("AuthoredSourceLifecycleUnavailable", S002RuntimeBlockerWitness.OwnershipUnavailableReason(f.Em, f.Grid, f.Dependency, f.Guid, Entity.Null));
        }
        [Test] public void ChangedFenceStateMustBeReReadFromActualComponents()
        {
            using var world = new World("S002 synthetic fence-state control");
            var em = world.EntityManager;
            var map = em.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent),
                typeof(OperationMapReadinessComponent));
            var grid = em.CreateEntity(typeof(GridConfig));
            var dependency = em.CreateEntity(typeof(RuntimeGridBlockerDependencyComponent));
            var admitted = new S002RuntimeBlockerWitness.CaptureAdmissionState {
                Active = new ActiveOperationMapComponent { Generation = 7 },
                Metadata = new OperationMapMetadataComponent { Generation = 7, PhysicalSourceValidated = 1 },
                Readiness = Ready(), Dependency = Dependency(),
                Grid = new GridConfig { Width = 8, Height = 8, CellSize = 1, Origin = float3.zero } };
            em.SetComponentData(map, admitted.Active); em.SetComponentData(map, admitted.Metadata);
            em.SetComponentData(map, admitted.Readiness); em.SetComponentData(grid, admitted.Grid);
            em.SetComponentData(dependency, admitted.Dependency);
            Assert.IsNull(S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            // Model writes becoming visible across the fence by changing actual ECS components
            // after retaining the pre-fence admission. No scheduled-job or live runtime credit.
            var changedReady = admitted.Readiness; changedReady.ReadyFlags &= ~OperationMapReadinessFlags.MapSurface;
            em.SetComponentData(map, changedReady);
            Assert.AreEqual("MapReadinessUnavailable", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.SetComponentData(map, admitted.Readiness);
            var changedDependency = admitted.Dependency; changedDependency.PendingCity = 1;
            em.SetComponentData(dependency, changedDependency);
            Assert.AreEqual("RuntimeBlockerOwnerNotReady", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.SetComponentData(dependency, admitted.Dependency);
            var changedGrid = admitted.Grid; changedGrid.Origin.x = 1;
            em.SetComponentData(grid, changedGrid);
            Assert.AreEqual("CaptureAdmissionChangedAcrossFenceOrCopy", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.SetComponentData(grid, admitted.Grid);
            changedDependency = admitted.Dependency; changedDependency.FinalizeAfterFrames = 5;
            em.SetComponentData(dependency, changedDependency);
            Assert.AreEqual("CaptureAdmissionChangedAcrossFenceOrCopy", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.SetComponentData(dependency, admitted.Dependency);
            var changedMetadata = admitted.Metadata; changedMetadata.Generation = 8;
            em.SetComponentData(map, changedMetadata);
            Assert.AreEqual("SourceGenerationOrValidationUnavailable", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.SetComponentData(map, admitted.Metadata);
            Assert.IsNull(S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
            em.RemoveComponent<GridConfig>(grid);
            Assert.AreEqual("CaptureAdmissionOwnerUnavailable", S002RuntimeBlockerWitness.RevalidateCaptureAdmission(em, map, grid, dependency, admitted));
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
                suite.EveryMandatoryFlagMustBeRequiredAndReady();
                suite.GlobalDependencyAndMissingGridProvenanceRefuse();
                suite.ForeignStaleAndUnownedSourceLifecycleRefuse();
                suite.ChangedFenceStateMustBeReReadFromActualComponents();
                suite.PendingCityAndBlockerFinalizationCannotLookReady();
                suite.OwnedBlockedFactionAndOccupancyCopiesSurviveSourceMutationAndDisposal();
                suite.EditModeCannotFabricateNormalRuntimeAvailability();
                Debug.Log("[S002RuntimeBlockerWitnessTests] result=Passed controls=10 actualRuntimeObserved=0 sourceFitCertified=0 missionReady=0");
            }
            catch (Exception error) { Debug.LogError("[S002RuntimeBlockerWitnessTests] result=Failed\n" + error); throw; }
        }
    }
}
