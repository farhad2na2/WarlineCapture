using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public interface IRuntimeActorBirthSourceProvider
    {
        bool TryReadBirthSource(World world, Entity actualGrid, out RuntimeActorSourceIdentity source);
    }
    public sealed class RuntimeActorBirthSourceProvider : IComponentData
    {
        public readonly IRuntimeActorBirthSourceProvider Provider;
        public RuntimeActorBirthSourceProvider() { }
        public RuntimeActorBirthSourceProvider(IRuntimeActorBirthSourceProvider provider) { Provider = provider; }
    }
    public readonly struct RuntimeActorSourceIdentity
    {
        public readonly ulong WorldSequence;
        public readonly Entity MapRoot, Grid, SceneRoot;
        public readonly GridConfig Layout;
        public readonly int Generation, ViewId, SourceViewId;
        public readonly FixedString128Bytes SourceIdentity, SourceContent, GeneratedMetadata;
        internal RuntimeActorSourceIdentity(ulong world, Entity map, Entity grid, Entity scene, int generation,
            int view, int sourceView, string identity, string content, string generated, GridConfig layout)
        { WorldSequence = world; MapRoot = map; Grid = grid; SceneRoot = scene; Generation = generation;
          ViewId = view; SourceViewId = sourceView; Layout = layout; SourceIdentity = new FixedString128Bytes(identity);
          SourceContent = new FixedString128Bytes(content); GeneratedMetadata = new FixedString128Bytes(generated); }
        internal bool SameIdentity(in RuntimeActorSourceIdentity other) => WorldSequence == other.WorldSequence &&
            MapRoot == other.MapRoot && Grid == other.Grid && SceneRoot == other.SceneRoot && Generation == other.Generation &&
            ViewId == other.ViewId && SourceViewId == other.SourceViewId && RuntimeGridBlockerEpoch.SameLayout(Layout, other.Layout) && SourceIdentity.Equals(other.SourceIdentity) &&
            SourceContent.Equals(other.SourceContent) && GeneratedMetadata.Equals(other.GeneratedMetadata);
    }

    // Diagnostic provenance only. This never owns actors, changes placement, or grants delivery.
    // A default/missing context is deliberately harmless to gameplay and unusable for evidence.
    public readonly struct RuntimeActorBirthContext
    {
        internal enum Kind : byte { None, Scenario, BuildingRequest, NativeReceipt, Reconstruction }
        internal readonly RuntimeActorSourceIdentity Source;
        internal readonly Entity Session;
        internal readonly FixedString64Bytes SessionId;
        internal readonly Kind Origin;
        internal readonly int EntryIndex, RequestId, ProducerId;
        internal readonly uint ReceiptId;
        internal readonly BuildingRuntimeSpawnRequest BuildingRequest;
        internal readonly FixedString64Bytes StableId;
        private RuntimeActorBirthContext(RuntimeActorSourceIdentity source, Entity session, FixedString64Bytes id,
            Kind origin, int entry = 0, int request = 0, int producer = 0, uint receipt = 0,
            BuildingRuntimeSpawnRequest building = default, FixedString64Bytes stableId = default)
        { Source = source; Session = session; SessionId = id; Origin = origin; EntryIndex = entry;
          RequestId = request; ProducerId = producer; ReceiptId = receipt; BuildingRequest = building; StableId = stableId; }

        internal static RuntimeActorBirthContext CaptureBuilding(EntityManager em, Entity actualGrid, GridConfig layout, in BuildingRuntimeSpawnRequest request)
        {
            if (!IsOwnedBuildingRequest(em, request) || !TrySource(em, actualGrid, request.PlanEntity, out var source, out var id) || !RuntimeGridBlockerEpoch.SameLayout(layout, source.Layout)) return default;
            return new RuntimeActorBirthContext(source, request.PlanEntity, id, Kind.BuildingRequest,
                request.EntryIndex, request.RequestId, building: request);
        }
        internal static bool IsOwnedBuildingRequest(EntityManager em, in BuildingRuntimeSpawnRequest r)
        {
            if (r.RequestId <= 0 || r.Status != BuildingRuntimeSpawnRequest.Pending || r.PlanEntity == Entity.Null ||
                !em.Exists(r.PlanEntity) || !em.HasComponent<SkirmishExpandedSessionComponent>(r.PlanEntity)) return false;
            if (SkirmishEnemyConstructionAdapter.IsOwnedRequest(em, r)) return true;
            if (r.AuthoredStartingGrant != 1 || !em.HasComponent<BuildingStartingGrantOwner>(r.PlanEntity) ||
                r.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding || r.HasOwnerFaction != 1) return false;
            if (em.HasBuffer<SkirmishStartingBuildingRequest>(r.PlanEntity) && em.HasComponent<SkirmishResolvedSetupRecord>(r.PlanEntity))
            {
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(r.PlanEntity).Setup;
                if (setup?.Structures != null && r.EntryIndex >= 0 && r.EntryIndex < setup.Structures.Length)
                {
                    var structure = setup.Structures[r.EntryIndex];
                    if (r.BuildingId.ToString() == SkirmishStructureIds.VisualKey(structure.StructureId) && r.FactionId == structure.FactionId)
                        foreach (var row in em.GetBuffer<SkirmishStartingBuildingRequest>(r.PlanEntity))
                            if (row.StructureIndex == r.EntryIndex && row.RequestId == r.RequestId && row.BoundEntity == Entity.Null) return true;
                }
            }
            // Reconstruction has its own freshly issued request correlation; saved entity pointers are never accepted.
            if (em.HasComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(r.PlanEntity))
            {
                var rows = em.GetComponentObject<SkirmishNativeCheckpointStructureReconstructionRecord>(r.PlanEntity).Requests;
                if (r.EntryIndex < 0 || r.EntryIndex >= rows.Count) return false;
                var row = rows[r.EntryIndex];
                return row.Id == r.RequestId && row.BoundEntity == Entity.Null && row.Actor != null &&
                    row.Actor.PrefabKey == r.BuildingId.ToString() &&
                    SkirmishNativeCheckpointParts.TryReadComponent(row.Actor.Parts, out RuntimeBuildingCombatInfo info) &&
                    info.OwnerFactionId == r.FactionId && info.OriginCell.Equals(r.PreferredOrigin);
            }
            return false;
        }
        internal static RuntimeActorBirthContext CaptureScenario(EntityManager em, Entity actualGrid,
            FixedString64Bytes sessionId, FixedString64Bytes stableId, GridConfig layout)
        {
            Entity session = Entity.Null;
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                if (em.GetComponentData<SkirmishExpandedSessionComponent>(entity).SessionId.Equals(sessionId))
                { if (session != Entity.Null) return default; session = entity; }
            if (stableId.IsEmpty || !TrySource(em, actualGrid, session, out var source, out var id) || !RuntimeGridBlockerEpoch.SameLayout(layout, source.Layout)) return default;
            return new RuntimeActorBirthContext(source, session, id, Kind.Scenario, stableId: stableId);
        }
        internal static RuntimeActorBirthContext CaptureNative(EntityManager em, Entity actualGrid, Entity session,
            uint receipt, int producer, byte faction, FixedString64Bytes key, FixedString64Bytes attempt, GridConfig layout)
        {
            if (!SkirmishProductionService.CanDeliverNative(em, session, receipt, producer, faction, key, attempt) ||
                !TrySource(em, actualGrid, session, out var source, out var id) || !id.Equals(attempt) || !RuntimeGridBlockerEpoch.SameLayout(layout, source.Layout)) return default;
            return new RuntimeActorBirthContext(source, session, id, Kind.NativeReceipt, producer: producer, receipt: receipt);
        }
        // Only the actual reconstruction owner calls this with its newly selected real grid, before Instantiate.
        internal static RuntimeActorBirthContext CaptureReconstruction(EntityManager em, Entity actualGrid, Entity session,
            FixedString64Bytes stableId)
        {
            if (stableId.IsEmpty || !em.Exists(session) || !em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(session) ||
                !TrySource(em, actualGrid, session, out var source, out var id)) return default;
            return new RuntimeActorBirthContext(source, session, id, Kind.Reconstruction, stableId: stableId);
        }
        private static bool TrySource(EntityManager em, Entity grid, Entity session, out RuntimeActorSourceIdentity source, out FixedString64Bytes id)
        {
            source = default; id = default;
            if (em == default || session == Entity.Null || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) || !em.Exists(grid) || !em.HasComponent<GridConfig>(grid)) return false;
            id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if (id.IsEmpty) return false;
            using var providers = em.CreateEntityQuery(typeof(RuntimeActorBirthSourceProvider), typeof(OperationMapRootComponent));
            if (providers.CalculateEntityCount() != 1) return false;
            var root = providers.GetSingletonEntity();
            var provider = em.GetComponentObject<RuntimeActorBirthSourceProvider>(root)?.Provider;
            return provider != null && provider.TryReadBirthSource(em.World, grid, out source) &&
                source.MapRoot == root && source.Grid == grid && source.WorldSequence == em.WorldUnmanaged.SequenceNumber &&
                RuntimeGridBlockerEpoch.SameLayout(source.Layout, em.GetComponentData<GridConfig>(grid));
        }
        internal bool IsCurrent(EntityManager em, Entity grid) => Origin != Kind.None && Source.Grid == grid &&
            TrySource(em, grid, Session, out var current, out var id) && id.Equals(SessionId) && Source.SameIdentity(current);
        // Called only by real creation owners, never by adoption or an observer. Always remove inherited prefab provenance.
        internal void StampFresh(EntityManager em, Entity actor, Entity actualGrid, bool pathingBlocker = false)
        {
            if (!em.Exists(actor)) return;
            if (em.HasComponent<RuntimeActorBirthRecord>(actor)) em.RemoveComponent<RuntimeActorBirthRecord>(actor);
            if (IsCurrent(em, actualGrid)) em.AddComponentData(actor, new RuntimeActorBirthRecord(this, actor, em.WorldUnmanaged.SequenceNumber, pathingBlocker));
        }
        internal static void VerifyNativeAdoption(EntityManager em, Entity actor, Entity session, uint receipt, int producer, FixedString64Bytes attempt)
        {
            if (!em.HasComponent<RuntimeActorBirthRecord>(actor)) return;
            var record = em.GetComponentData<RuntimeActorBirthRecord>(actor);
            if (!record.MatchesActor(em, actor) || record.Origin != Kind.NativeReceipt || record.Session != session ||
                record.ReceiptId != receipt || record.ProducerId != producer || !record.SessionId.Equals(attempt) || !record.IsCurrent(em, record.Grid))
                em.RemoveComponent<RuntimeActorBirthRecord>(actor);
        }
        internal static void VerifyBuildingAdoption(EntityManager em, Entity actor, in BuildingRuntimeSpawnRequest request)
        {
            if (em.Exists(actor) && em.HasComponent<SkirmishNativeBuildingCheckpointRecord>(actor))
            {
                var building = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(actor).Building;
                if (building != null && building.BlockerEntity != Entity.Null && em.Exists(building.BlockerEntity))
                    VerifyBuildingRecord(em, building.BlockerEntity, request);
            }
            VerifyBuildingRecord(em, actor, request);
        }
        private static void VerifyBuildingRecord(EntityManager em, Entity actor, in BuildingRuntimeSpawnRequest request)
        {
            if (!em.HasComponent<RuntimeActorBirthRecord>(actor)) return;
            var record = em.GetComponentData<RuntimeActorBirthRecord>(actor);
            if (!record.MatchesActor(em, actor) || record.Origin != Kind.BuildingRequest || record.Session != request.PlanEntity ||
                record.RequestId != request.RequestId || record.EntryIndex != request.EntryIndex ||
                !record.BuildingRequestMatches(request) || !record.IsCurrent(em, record.Grid))
                em.RemoveComponent<RuntimeActorBirthRecord>(actor);
        }
        public static string ContributorUnavailableReason(EntityManager em, Entity actor, Entity grid, Entity sourceRoot, FixedString64Bytes attempt)
        {
            if (!em.HasComponent<RuntimeActorBirthRecord>(actor)) return "AttemptWriterGridSourceBindingUnavailable";
            var record = em.GetComponentData<RuntimeActorBirthRecord>(actor);
            if (!record.MatchesActor(em, actor) || !record.SessionId.Equals(attempt)) return "ForeignOrClonedWriterBirth";
            if (!record.IsCurrent(em, grid) || record.SceneRoot != sourceRoot) return "WriterBirthSourceLifetimeChanged";
            if (em.HasComponent<SkirmishAttemptOwnedComponent>(actor))
            {
                var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!own.SessionId.Equals(record.SessionId) || (!record.StableId.IsEmpty && !own.StableObjectId.Equals(record.StableId)) ||
                    (record.Origin == Kind.NativeReceipt && own.ReservationId != record.ReceiptId)) return "WriterBirthAttemptChanged";
            }
            else if (record.Origin != Kind.BuildingRequest || !record.PathingBlocker || !em.HasComponent<StaticGridBlocker>(actor))
                return "WriterBirthActorOwnershipMissing";
            return null;
        }
        public static bool HasRecord(EntityManager em, Entity actor) => em.HasComponent<RuntimeActorBirthRecord>(actor);

        // Unmanaged evidence: never put World/View managed graphs on a clonable ECS component.
        // Self identity uses primitives, so Instantiate's Entity remapper cannot bless a copied record.
        internal readonly struct RuntimeActorBirthRecord : IComponentData
        {
            private readonly int ActorIndex, ActorVersion, MapIndex, MapVersion, GridIndex, GridVersion,
                SceneIndex, SceneVersion, SessionIndex, SessionVersion, ViewId, SourceViewId, Generation;
            private readonly ulong WorldSequence;
            private readonly FixedString128Bytes SourceIdentity, SourceContent, GeneratedMetadata;
            internal readonly FixedString64Bytes SessionId, StableId;
            internal readonly Kind Origin;
            internal readonly int EntryIndex, RequestId, ProducerId;
            internal readonly uint ReceiptId;
            internal readonly bool PathingBlocker;
            private readonly BuildingRuntimeSpawnRequest Request;
            private readonly GridConfig Layout;
            internal Entity Session => new Entity { Index = SessionIndex, Version = SessionVersion };
            internal Entity Grid => new Entity { Index = GridIndex, Version = GridVersion };
            internal Entity SceneRoot => new Entity { Index = SceneIndex, Version = SceneVersion };
            internal RuntimeActorBirthRecord(RuntimeActorBirthContext c, Entity actor, ulong world, bool pathingBlocker)
            {
                ActorIndex = actor.Index; ActorVersion = actor.Version; WorldSequence = world;
                MapIndex = c.Source.MapRoot.Index; MapVersion = c.Source.MapRoot.Version;
                GridIndex = c.Source.Grid.Index; GridVersion = c.Source.Grid.Version;
                SceneIndex = c.Source.SceneRoot.Index; SceneVersion = c.Source.SceneRoot.Version;
                SessionIndex = c.Session.Index; SessionVersion = c.Session.Version;
                ViewId = c.Source.ViewId; SourceViewId = c.Source.SourceViewId; Generation = c.Source.Generation;
                SourceIdentity = c.Source.SourceIdentity; SourceContent = c.Source.SourceContent;
                GeneratedMetadata = c.Source.GeneratedMetadata;
                SessionId = c.SessionId; StableId = c.StableId; Origin = c.Origin; EntryIndex = c.EntryIndex;
                RequestId = c.RequestId; ProducerId = c.ProducerId; ReceiptId = c.ReceiptId; PathingBlocker = pathingBlocker; Request = c.BuildingRequest; Layout = c.Source.Layout;
            }
            internal bool MatchesActor(EntityManager em, Entity actor) => WorldSequence == em.WorldUnmanaged.SequenceNumber &&
                ActorIndex == actor.Index && ActorVersion == actor.Version;
            internal bool IsCurrent(EntityManager em, Entity grid)
            {
                if (Origin == Kind.None || WorldSequence != em.WorldUnmanaged.SequenceNumber || Grid != grid ||
                    !TrySource(em, grid, Session, out var current, out var id) || !id.Equals(SessionId)) return false;
                return RuntimeGridBlockerEpoch.SameLayout(Layout, current.Layout) && current.MapRoot.Index == MapIndex && current.MapRoot.Version == MapVersion && current.SceneRoot == SceneRoot &&
                    current.ViewId == ViewId && current.SourceViewId == SourceViewId && current.Generation == Generation &&
                    current.SourceIdentity.Equals(SourceIdentity) && current.SourceContent.Equals(SourceContent) &&
                    current.GeneratedMetadata.Equals(GeneratedMetadata);
            }
            internal bool BuildingRequestMatches(in BuildingRuntimeSpawnRequest r) => Request.RequestKind == r.RequestKind &&
                Request.BuildingId.Equals(r.BuildingId) && Request.FactionId == r.FactionId && Request.HasOwnerFaction == r.HasOwnerFaction &&
                Request.AuthoredStartingGrant == r.AuthoredStartingGrant && Request.PreferredOrigin.Equals(r.PreferredOrigin) &&
                Request.RotateVertical == r.RotateVertical && Request.MaterialsCost == r.MaterialsCost;
        }
    }
}
