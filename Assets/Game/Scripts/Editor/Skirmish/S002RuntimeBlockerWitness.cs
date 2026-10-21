#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Passive observation only. Never initializes blockers, runs gameplay systems,
    // writes a grid/entity, changes presentation or grants source-fit readiness.
    public static class S002RuntimeBlockerWitness
    {
        // MatchSceneView.ResolveInitialOperationMapReadiness(true): physical loaded-map
        // admission cannot be weakened by a caller supplying Metadata-only RequiredFlags.
        public const OperationMapReadinessFlags MandatorySourceReadiness =
            OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene |
            OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface |
            OperationMapReadinessFlags.AuthoredConversion | OperationMapReadinessFlags.PresentationManifest |
            OperationMapReadinessFlags.RequiredPresentationPreload;

        public sealed class OwnedGridSnapshot : IDisposable
        {
            public NativeBitArray Blocked;
            public NativeArray<byte> FriendlyPassFactionIds;
            public NativeArray<int> Counts;
            public NativeBitArray Occupied;
            public void Dispose()
            {
                if (Blocked.IsCreated) Blocked.Dispose();
                if (FriendlyPassFactionIds.IsCreated) FriendlyPassFactionIds.Dispose();
                if (Counts.IsCreated) Counts.Dispose();
                if (Occupied.IsCreated) Occupied.Dispose();
                Blocked = default; FriendlyPassFactionIds = default; Counts = default; Occupied = default;
            }
        }

        public static string StorageUnavailableReason(GridConfig grid, DynamicBlockerComponent blockers, DynamicOccupancyComponent occupancy)
        {
            long cells = (long)grid.Width * grid.Height;
            if (grid.Width <= 0 || grid.Height <= 0 || cells > int.MaxValue || !math.all(math.isfinite(grid.Origin)) ||
                !float.IsFinite(grid.CellSize) || grid.CellSize <= 0) return "InvalidGridShape";
            if (blockers.GridSize != cells || !blockers.Blocked.IsCreated || blockers.Blocked.Length != cells ||
                !blockers.Counts.IsCreated || blockers.Counts.Length != cells || !blockers.FriendlyPassFactionIds.IsCreated ||
                blockers.FriendlyPassFactionIds.Length != cells) return "BlockerStorageUnavailableOrWrongShape";
            if (occupancy.GridSize != cells || !occupancy.Occupied.IsCreated || occupancy.Occupied.Length != cells)
                return "OccupancyStorageUnavailableOrWrongShape";
            return null;
        }

        public static string ReadinessUnavailableReason(int activeGeneration, int metadataGeneration, byte physicalSourceValidated,
            OperationMapReadinessComponent readiness, RuntimeGridBlockerDependencyComponent dependency)
        {
            if (activeGeneration <= 0 || activeGeneration != metadataGeneration || readiness.Generation != activeGeneration ||
                physicalSourceValidated != 1) return "SourceGenerationOrValidationUnavailable";
            if ((readiness.RequiredFlags & MandatorySourceReadiness) != MandatorySourceReadiness ||
                (readiness.ReadyFlags & MandatorySourceReadiness) != MandatorySourceReadiness ||
                readiness.FailedFlags != OperationMapReadinessFlags.None ||
                (readiness.ReadyFlags & readiness.RequiredFlags) != readiness.RequiredFlags) return "MapReadinessUnavailable";
            if (dependency.ReadyForDependents != 1 || dependency.PendingCity != 0 || dependency.CityGenerating != 0 ||
                dependency.SpawnFinalizing != 0 || (dependency.SpawnOnStart != 0 && dependency.Spawned != 1))
                return "RuntimeBlockerOwnerNotReady";
            return null;
        }

        // Read existing ECS lifecycle ownership only. The normal presentation helper currently
        // creates an untagged global dependency entity; it therefore cannot pass this join.
        // Do not add tags/stamps or infer provenance from matching dimensions/readiness.
        public static string OwnershipUnavailableReason(EntityManager em, Entity gridEntity, Entity dependencyEntity,
            Unity.Entities.Hash128 authoredGuid, Entity loadedSceneRoot)
        {
            if (!authoredGuid.IsValid || loadedSceneRoot == Entity.Null || !em.Exists(loadedSceneRoot) ||
                !em.HasComponent<SceneReference>(loadedSceneRoot) ||
                !em.GetComponentData<SceneReference>(loadedSceneRoot).SceneGUID.Equals(authoredGuid) ||
                !em.HasBuffer<ResolvedSectionEntity>(loadedSceneRoot))
                return "AuthoredSourceLifecycleUnavailable";
            string reason = SceneOwnerUnavailableReason(em, gridEntity, authoredGuid, loadedSceneRoot);
            if (reason != null) return "GridSourceOwnership:" + reason;
            reason = SceneOwnerUnavailableReason(em, dependencyEntity, authoredGuid, loadedSceneRoot);
            return reason == null ? null : "BlockerDependencySourceOwnership:" + reason;
        }

        static string SceneOwnerUnavailableReason(EntityManager em, Entity entity,
            Unity.Entities.Hash128 authoredGuid, Entity loadedSceneRoot)
        {
            if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<SceneTag>(entity))
                return "SceneTagUnavailable";
            Entity section = em.GetSharedComponent<SceneTag>(entity).SceneEntity;
            if (section == Entity.Null || !em.Exists(section) || !em.HasComponent<SceneSectionData>(section) ||
                !em.HasComponent<SceneEntityReference>(section)) return "SectionLifecycleUnavailable";
            if (!em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(authoredGuid) ||
                em.GetComponentData<SceneEntityReference>(section).SceneEntity != loadedSceneRoot)
                return "AuthoredSourceOwnerMismatch";
            var sections = em.GetBuffer<ResolvedSectionEntity>(loadedSceneRoot, true);
            for (int i = 0; i < sections.Length; i++)
                if (sections[i].SectionEntity == section) return null;
            return "SectionNotOwnedByLoadedSource";
        }

        public struct CaptureAdmissionState
        {
            public ActiveOperationMapComponent Active;
            public OperationMapMetadataComponent Metadata;
            public OperationMapReadinessComponent Readiness;
            public RuntimeGridBlockerDependencyComponent Dependency;
            public GridConfig Grid;
        }

        // Read actual components again after synchronization and after copying. Do not let
        // pre-fence readiness, dependency or grid values authorize a post-fence snapshot.
        public static string RevalidateCaptureAdmission(EntityManager em, Entity mapEntity, Entity gridEntity,
            Entity dependencyEntity, CaptureAdmissionState admitted)
        {
            if (!em.Exists(mapEntity) || !em.Exists(gridEntity) || !em.Exists(dependencyEntity) ||
                !em.HasComponent<ActiveOperationMapComponent>(mapEntity) ||
                !em.HasComponent<OperationMapMetadataComponent>(mapEntity) ||
                !em.HasComponent<OperationMapReadinessComponent>(mapEntity) ||
                !em.HasComponent<GridConfig>(gridEntity) ||
                !em.HasComponent<RuntimeGridBlockerDependencyComponent>(dependencyEntity))
                return "CaptureAdmissionOwnerUnavailable";
            var active = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(mapEntity);
            var readiness = em.GetComponentData<OperationMapReadinessComponent>(mapEntity);
            var dependency = em.GetComponentData<RuntimeGridBlockerDependencyComponent>(dependencyEntity);
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            string reason = ReadinessUnavailableReason(active.Generation, metadata.Generation,
                metadata.PhysicalSourceValidated, readiness, dependency);
            if (reason != null) return reason;
            // Reject even still-ready identity/dependency changes; compare all admission fields.
            var a = admitted.Active; var m = admitted.Metadata; var r = admitted.Readiness;
            var d = admitted.Dependency; var g = admitted.Grid;
            if (active.Generation != a.Generation || !active.OperationMapId.Equals(a.OperationMapId) ||
                !active.ScenarioId.Equals(a.ScenarioId) || !active.MissionId.Equals(a.MissionId) ||
                active.SchemaVersion != a.SchemaVersion || active.ContentVersion != a.ContentVersion ||
                metadata.Generation != m.Generation || metadata.PhysicalSourceValidated != m.PhysicalSourceValidated ||
                !metadata.Blob.Equals(m.Blob) || !metadata.MetadataHash.Equals(m.MetadataHash) ||
                readiness.Generation != r.Generation || readiness.RequiredFlags != r.RequiredFlags ||
                readiness.ReadyFlags != r.ReadyFlags || readiness.FailedFlags != r.FailedFlags ||
                dependency.ReadyForDependents != d.ReadyForDependents || dependency.SpawnOnStart != d.SpawnOnStart ||
                dependency.Spawned != d.Spawned || dependency.SpawnFinalizing != d.SpawnFinalizing ||
                dependency.FinalizeAfterFrames != d.FinalizeAfterFrames || dependency.PendingCity != d.PendingCity ||
                dependency.CityHasSpawned != d.CityHasSpawned || dependency.CityGenerating != d.CityGenerating ||
                grid.Width != g.Width || grid.Height != g.Height || grid.CellSize != g.CellSize || !math.all(grid.Origin == g.Origin))
                return "CaptureAdmissionChangedAcrossFenceOrCopy";
            return null;
        }

        public static string WriterEpochUnavailableReason(GridConfig grid, DynamicBlockerComponent blockers) =>
            RuntimeGridBlockerEpoch.IsCommitted(grid, blockers) ? null : "ActualWriterStorageEpochNotCommitted";

        public static bool SameWriterEpoch(in DynamicBlockerComponent a, in DynamicBlockerComponent b) =>
            a.StorageEpoch == b.StorageEpoch && a.AppliedStorageEpoch == b.AppliedStorageEpoch &&
            a.AppliedRevision == b.AppliedRevision && a.WriteInProgress == b.WriteInProgress && a.EpochExhausted == b.EpochExhausted;

        // Diagnostic only; never filters live writer inputs or marks a runtime producer source-owned.
        // Current attempts/helpers without a real grid/source binding remain unavailable.
        public static string WriterInputOwnershipUnavailableReason(EntityManager em, Entity gridEntity,
            Unity.Entities.Hash128 authoredGuid, Entity sourceRoot, FixedString64Bytes sessionId)
        {
            if (sourceRoot == Entity.Null || !em.Exists(sourceRoot) || !em.HasComponent<SceneReference>(sourceRoot) ||
                !em.GetComponentData<SceneReference>(sourceRoot).SceneGUID.Equals(authoredGuid) ||
                !em.HasBuffer<ResolvedSectionEntity>(sourceRoot)) return "WriterSourceLifecycleUnavailable";
            string reason = SceneOwnerUnavailableReason(em, gridEntity, authoredGuid, sourceRoot);
            if (reason != null) return "WriterGridOwnership:" + reason;
            var storage = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            using var inputs = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
            using var entities = inputs.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                {
                    if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(sessionId))
                        return "ForeignAttemptWriterContributor";
                    return "AttemptWriterGridSourceBindingUnavailable";
                }
                reason = SceneOwnerUnavailableReason(em, entity, authoredGuid, sourceRoot);
                if (reason != null) return "WriterContributorOwnership:" + reason;
                if (!em.HasComponent<StaticBlockerPrevBounds>(entity)) return "WriterNewContributorNotApplied";
                var previous = em.GetComponentData<StaticBlockerPrevBounds>(entity);
                int2 origin = em.HasComponent<OperationMapBuildingComponent>(entity) && em.HasComponent<RuntimeBuildingCombatInfo>(entity)
                    ? em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OriginCell : em.GetComponentData<UnitGrid>(entity).Cell;
                int2 max = origin + math.max(em.GetComponentData<GridBlockerSize>(entity).Size, new int2(1, 1));
                byte pass = em.HasComponent<FriendlyPassGridBlocker>(entity)
                    ? em.GetComponentData<FriendlyPassGridBlocker>(entity).AllowedFactionId : byte.MaxValue;
                var opening = em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(entity)
                    ? em.GetComponentData<StaticGridBlockerSurfaceOpeningComponent>(entity) : default;
                var oldOpening = em.HasComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(entity)
                    ? em.GetComponentData<StaticGridBlockerPrevSurfaceOpeningComponent>(entity).Value : default;
                if (previous.AppliedGrid != gridEntity || previous.StorageEpoch != storage.StorageEpoch ||
                    !math.all(previous.Min == origin) || !math.all(previous.Max == max) || previous.FriendlyPassFactionId != pass ||
                    !opening.SameGeometry(oldOpening)) return "WriterInputChangedSinceApplication";
            }
            using var histories = em.CreateEntityQuery(typeof(StaticBlockerPrevBounds));
            using var historyEntities = histories.ToEntityArray(Allocator.Temp);
            foreach (var entity in historyEntities)
                if (em.HasComponent<StaticGridBlocker>(entity) &&
                    (!em.HasComponent<UnitGrid>(entity) || !em.HasComponent<GridBlockerSize>(entity)))
                    return "WriterMalformedHistoryNotInCurrentInputDomain";
            using var removals = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<StaticBlockerPrevBounds>() },
                None = new[] { ComponentType.ReadOnly<StaticGridBlocker>() } });
            return removals.IsEmptyIgnoreFilter ? null : "WriterRemovalNotApplied";
        }

        public static OwnedGridSnapshot CopyAvailableStorage(GridConfig grid, DynamicBlockerComponent blockers, DynamicOccupancyComponent occupancy)
        {
            string error = StorageUnavailableReason(grid, blockers, occupancy);
            if (error != null) throw new InvalidOperationException(error);
            var copy = new OwnedGridSnapshot();
            try
            {
                int cells = grid.Width * grid.Height;
                copy.Blocked = new NativeBitArray(cells, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                var bits = blockers.Blocked; copy.Blocked.Copy(0, ref bits, 0, cells);
                copy.FriendlyPassFactionIds = new NativeArray<byte>(blockers.FriendlyPassFactionIds, Allocator.Persistent);
                copy.Counts = new NativeArray<int>(blockers.Counts, Allocator.Persistent);
                copy.Occupied = new NativeBitArray(cells, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                bits = occupancy.Occupied; copy.Occupied.Copy(0, ref bits, 0, cells);
                return copy;
            }
            catch { copy.Dispose(); throw; }
        }

        static bool Unavailable(JObject facts, string reason)
        { facts["available"] = false; facts["unavailableReason"] = reason; return false; }

        public static bool TryCaptureCurrent(out OwnedGridSnapshot snapshot, out JObject facts)
        {
            snapshot = null;
            facts = new JObject { ["scope"] = "PairedNormalRuntimeGridObservation_NotSourceFitOrMissionAcceptance",
                ["available"] = false, ["sourceFitCertified"] = false, ["missionReady"] = false,
                ["liveOccupancySeparatelyAttributed"] = true, ["gridMutated"] = false, ["systemsInitializedOrUpdated"] = false };
            if (!EditorApplication.isPlaying) return Unavailable(facts, "NormalPlaySessionUnavailable");
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return Unavailable(facts, "NormalWorldUnavailable");
            var em = world.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad));
            using var dependencies = em.CreateEntityQuery(typeof(RuntimeGridBlockerDependencyComponent));
            facts["sessionCount"] = sessions.CalculateEntityCount(); facts["mapCount"] = maps.CalculateEntityCount();
            facts["gridCount"] = grids.CalculateEntityCount(); facts["blockerDependencyCount"] = dependencies.CalculateEntityCount();
            if (sessions.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1 || grids.CalculateEntityCount() != 1 ||
                dependencies.CalculateEntityCount() != 1) return Unavailable(facts, "UniqueReadyOwnersUnavailable");
            Entity sessionEntity = sessions.GetSingletonEntity(), mapEntity = maps.GetSingletonEntity(), gridEntity = grids.GetSingletonEntity();
            var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            var setupRecord = em.GetComponentObject<SkirmishResolvedSetupRecord>(sessionEntity);
            var setup = setupRecord?.Setup;
            if (session.CatalogId.ToString() != "S002" || session.IsLegacy != 0 || session.IsCustom != 0 ||
                session.Phase != SkirmishSessionPhase.Playing || session.InitializationComplete != 1 || session.SpawnComplete != 1 ||
                session.SpawnVisualPending != 0 || setup == null || setup.CatalogId != "S002" || session.SetupHash != setup.SetupHash ||
                session.ContentVersion != setup.ContentVersion || session.DefinitionId.ToString() != setup.DefinitionId ||
                session.SizeId != setup.SizeId || session.DifficultyId != setup.DifficultyId || session.Seed != setup.Seed)
                return Unavailable(facts, "ExactNormalS002SessionNotReady");
            var launch = em.HasComponent<SkirmishLaunchDefinitionRecord>(sessionEntity)
                ? em.GetComponentObject<SkirmishLaunchDefinitionRecord>(sessionEntity) : null;
            if (launch == null || !launch.Matches(setup, session))
                return Unavailable(facts, "ExactLaunchDefinitionIdentityUnavailable");
            if (!em.HasComponent<OperationMapReadinessComponent>(mapEntity) || !em.HasComponent<DynamicBlockerComponent>(gridEntity) ||
                !em.HasComponent<DynamicOccupancyComponent>(gridEntity)) return Unavailable(facts, "ReadinessOrMaskComponentUnavailable");
            var active = maps.GetSingleton<ActiveOperationMapComponent>(); var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
            var readiness = em.GetComponentData<OperationMapReadinessComponent>(mapEntity);
            var dependency = dependencies.GetSingleton<RuntimeGridBlockerDependencyComponent>();
            var dependencyEntity = dependencies.GetSingletonEntity();
            facts["runtimeBlockerOwner"] = new JObject { ["entityIndex"] = dependencyEntity.Index, ["entityVersion"] = dependencyEntity.Version,
                ["readyForDependents"] = dependency.ReadyForDependents, ["spawnOnStart"] = dependency.SpawnOnStart,
                ["spawned"] = dependency.Spawned, ["spawnFinalizing"] = dependency.SpawnFinalizing,
                ["pendingCity"] = dependency.PendingCity, ["cityHasSpawned"] = dependency.CityHasSpawned, ["cityGenerating"] = dependency.CityGenerating };
            string reason = ReadinessUnavailableReason(active.Generation, metadata.Generation, metadata.PhysicalSourceValidated, readiness, dependency);
            if (reason != null) return Unavailable(facts, reason);
            if (!metadata.Blob.IsCreated) return Unavailable(facts, "SourceMetadataBlobUnavailable");
            ref var map = ref metadata.Blob.Value;
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            if (!SkirmishLaunchProjection.TryGet(em, out var selectedSession, out var match) || selectedSession != sessionEntity)
                return Unavailable(facts, "NormalLaunchSourceOwnerUnavailable");
            var preset = SkirmishPresetConfig.Load(match.ScenarioIndex);
            var source = SkirmishS004Environment.LoadMapForSession(em) ?? (preset != null ? preset.operationMap : null);
            if (source == null || !source.TryValidateMetadata(out _) || !source.TryValidateLocalContentReferences(out _))
                return Unavailable(facts, "DeclaredNormalSourceUnavailableOrInvalid");
            string expectedIdentity = source.SourceBinding.IsConfigured ? source.SourceBinding.SourceIdentityHash : source.SourceIdentityHash;
            string expectedSourceContent = source.SourceBinding.IsConfigured ? source.SourceBinding.SourceContentHash : source.ContentHash;
            if (active.OperationMapId.ToString() != setup.OperationMapId || active.OperationMapId.ToString() != source.OperationMapId ||
                !active.OperationMapId.Equals(map.OperationMapId) || map.ContentHash.ToString() != source.ContentHash ||
                map.GeneratedMetadataHash.ToString() != source.GeneratedMetadataHash || map.SourceIdentityHash.ToString() != expectedIdentity ||
                map.SourceContentHash.ToString() != expectedSourceContent || map.Navigation.AuthoredSubSceneGuid.ToString() != source.NavigationMetadata.AuthoredSubSceneGuid ||
                grid.Width != source.GridMetadata.Dimensions.x || grid.Height != source.GridMetadata.Dimensions.y ||
                grid.CellSize != source.GridMetadata.CellSize || !math.all(grid.Origin == (float3)source.GridMetadata.Origin))
                return Unavailable(facts, "ActualGridAndNormalSourceJoinMismatch");
            var authoredGuid = new Unity.Entities.Hash128(source.NavigationMetadata.AuthoredSubSceneGuid);
            Entity loadedSceneRoot = SceneSystem.GetSceneEntity(world.Unmanaged, authoredGuid);
            if (loadedSceneRoot == Entity.Null || !SceneSystem.IsSceneLoaded(world.Unmanaged, loadedSceneRoot))
                return Unavailable(facts, "AuthoredSourceSceneNotLoaded");
            reason = OwnershipUnavailableReason(em, gridEntity, dependencyEntity, authoredGuid, loadedSceneRoot);
            facts["sourceOwnership"] = reason ?? "JoinedExistingLoadedAuthoredSceneLifecycle";
            if (reason != null) return Unavailable(facts, reason);
            // Expensive one-shot synchronization and mask allocation happen only after admission.
            // This is not suitable for per-frame polling. No system Update or entity writes.
            var admitted = new CaptureAdmissionState { Active = active, Metadata = metadata,
                Readiness = readiness, Dependency = dependency, Grid = grid };
            em.CompleteAllTrackedJobs();
            reason = RevalidateCaptureAdmission(em, mapEntity, gridEntity, dependencyEntity, admitted);
            if (reason != null) return Unavailable(facts, reason);
            reason = OwnershipUnavailableReason(em, gridEntity, dependencyEntity, authoredGuid, loadedSceneRoot);
            if (reason != null || SceneSystem.GetSceneEntity(world.Unmanaged, authoredGuid) != loadedSceneRoot ||
                !SceneSystem.IsSceneLoaded(world.Unmanaged, loadedSceneRoot))
                return Unavailable(facts, reason ?? "SourceLifecycleChangedAcrossFence");
            var blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            var occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            reason = StorageUnavailableReason(grid, blockers, occupancy); if (reason != null) return Unavailable(facts, reason);
            reason = WriterEpochUnavailableReason(grid, blockers); if (reason != null) return Unavailable(facts, reason);
            reason = WriterInputOwnershipUnavailableReason(em, gridEntity, authoredGuid, loadedSceneRoot, session.SessionId);
            if (reason != null) return Unavailable(facts, reason);
            int frame = Time.frameCount;
            var owned = CopyAvailableStorage(grid, blockers, occupancy);
            try
            {
                var after = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
                if (World.DefaultGameObjectInjectionWorld != world || after.Generation != active.Generation ||
                    !after.OperationMapId.Equals(active.OperationMapId) || Time.frameCount != frame ||
                    !em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity).SessionId.Equals(session.SessionId))
                    return Unavailable(facts, "OwnerOrFrameChangedDuringCopy");
                reason = RevalidateCaptureAdmission(em, mapEntity, gridEntity, dependencyEntity, admitted);
                if (reason != null) return Unavailable(facts, reason);
                reason = OwnershipUnavailableReason(em, gridEntity, dependencyEntity, authoredGuid, loadedSceneRoot);
                if (reason != null || SceneSystem.GetSceneEntity(world.Unmanaged, authoredGuid) != loadedSceneRoot ||
                    !SceneSystem.IsSceneLoaded(world.Unmanaged, loadedSceneRoot))
                    return Unavailable(facts, reason ?? "SourceLifecycleChangedDuringCopy");
                var writerAfter = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
                reason = WriterEpochUnavailableReason(grid, writerAfter);
                if (reason != null || !SameWriterEpoch(blockers, writerAfter))
                    return Unavailable(facts, reason ?? "WriterEpochChangedDuringCopy");
                reason = WriterInputOwnershipUnavailableReason(em, gridEntity, authoredGuid, loadedSceneRoot, session.SessionId);
                if (reason != null) return Unavailable(facts, reason);
                facts["actualWriterStorage"] = new JObject { ["storageEpoch"] = blockers.StorageEpoch,
                    ["appliedStorageEpoch"] = blockers.AppliedStorageEpoch, ["appliedRevision"] = blockers.AppliedRevision,
                    ["scope"] = "ActualDeltaAndReplayCommit_NotPresentationCompletenessOrHelperApplicability" };
                int cells = grid.Width * grid.Height, blockedCount = 0, occupiedCount = 0;
                byte[] blockedBytes = new byte[cells], occupiedBytes = new byte[cells];
                for (int i = 0; i < cells; i++)
                {
                    if (owned.Blocked.IsSet(i)) { blockedBytes[i] = 1; blockedCount++; }
                    if (owned.Occupied.IsSet(i)) { occupiedBytes[i] = 1; occupiedCount++; }
                }
                byte[] countBytes;
                using (var stream = new MemoryStream())
                { using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    for (int i = 0; i < cells; i++) writer.Write(owned.Counts[i]); countBytes = stream.ToArray(); }
                facts["source"] = new JObject { ["operationMapId"] = active.OperationMapId.ToString(), ["contentHash"] = map.ContentHash.ToString(),
                    ["sourceIdentityHash"] = map.SourceIdentityHash.ToString(), ["sourceContentHash"] = map.SourceContentHash.ToString(),
                    ["generatedMetadataHash"] = map.GeneratedMetadataHash.ToString(), ["authoredSubSceneGuid"] = map.Navigation.AuthoredSubSceneGuid.ToString(),
                    ["generation"] = active.Generation, ["metadataGeneration"] = metadata.Generation, ["readinessGeneration"] = readiness.Generation,
                    ["mapEntityIndex"] = mapEntity.Index, ["mapEntityVersion"] = mapEntity.Version, ["physicalSourceValidated"] = metadata.PhysicalSourceValidated,
                    ["readyFlags"] = (int)readiness.ReadyFlags, ["requiredFlags"] = (int)readiness.RequiredFlags, ["failedFlags"] = (int)readiness.FailedFlags };
                facts["worldSequence"] = world.SequenceNumber.ToString(); facts["frame"] = frame;
                facts["grid"] = new JObject { ["entityIndex"] = gridEntity.Index, ["entityVersion"] = gridEntity.Version,
                    ["width"] = grid.Width, ["height"] = grid.Height, ["cellSize"] = grid.CellSize,
                    ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) };
                facts["session"] = new JObject { ["id"] = session.SessionId.ToString(), ["catalog"] = "S002", ["version"] = session.ContentVersion,
                    ["setupHash"] = session.SetupHash.ToString("X8"), ["size"] = session.SizeId.ToString(), ["seed"] = session.Seed };
                facts["masks"] = new JObject { ["blockedCells"] = blockedCount, ["occupiedCells"] = occupiedCount,
                    ["blockedBytePerCellSha256"] = Sha(blockedBytes), ["friendlyPassBytePerCellSha256"] = Sha(owned.FriendlyPassFactionIds.ToArray()),
                    ["countInt32LittleEndianSha256"] = Sha(countBytes), ["occupiedBytePerCellSha256"] = Sha(occupiedBytes) };
                using var contributors = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
                using var entities = contributors.ToEntityArray(Allocator.Temp);
                int authored = 0, attempt = 0, other = 0;
                foreach (var entity in entities)
                {
                    if (em.HasComponent<SkirmishAttemptOwnedComponent>(entity)) attempt++;
                    else if (em.HasComponent<OperationMapBuildingComponent>(entity)) authored++;
                    else other++;
                }
                facts["blockerEntityMembershipCounts"] = new JObject { ["sourceAuthoredBuildings"] = authored,
                    ["attemptOwned"] = attempt, ["other"] = other, ["scope"] = "MembershipOnly_NotPerBitAttributionOrPresentationCompleteness" };
                facts["presentationContributorEquivalence"] = "Unmeasured; absence of entities never proves absence of current rendered scenery";
                facts["ownedCopies"] = true; facts["available"] = true; snapshot = owned; owned = null; return true;
            }
            finally { owned?.Dispose(); }
        }

        static string Sha(byte[] bytes)
        { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
}
#endif
