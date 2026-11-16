using Game.Components;
using Game.Configs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ProductionDeliveryLogicalBasis = Game.Simulation.SharedProductionDeliveryKernel.ProductionDeliveryLogicalBasis;

namespace Game.Runtime
{
    // Native-only source binding. Headless/source hosts may own the shared
    // placement basis without claiming loaded-scene physical validation.
    public struct SkirmishProductionDeliveryBasisProvenanceComponent : IComponentData
    {
        public FixedString128Bytes OperationMapId;
        public FixedString128Bytes SourceIdentityHash;
        public int MapGeneration;
    }

    internal enum SkirmishProductionDeliveryBasisStartupResult : byte
    {
        Ready,
        Pending,
        Invalid
    }

    internal static class SkirmishProductionDeliveryBasisStartup
    {
        const OperationMapReadinessFlags GridOwnerReady = OperationMapReadinessFlags.SubScene;
        const float SurfaceFrameTolerance = 0.0001f;

        // Keep cleanup of this Simulation.Mathematics-backed component in the
        // Runtime assembly that owns it. Composition can call this seam without
        // instantiating RemoveIfPresent<T> across the assembly boundary.
        internal static void RemoveSessionComponents(EntityManager em, Entity session)
        {
            if (em == default || !em.Exists(session)) return;
            if (em.HasComponent<SkirmishProductionDeliveryBasisComponent>(session))
                em.RemoveComponent<SkirmishProductionDeliveryBasisComponent>(session);
            if (em.HasComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(session))
                em.RemoveComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
        }

        internal static SkirmishProductionDeliveryBasisStartupResult TryInitialize(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            Entity selectedPlacementGrid,
            out string failure)
        {
            failure = null;
            if (em == default || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) || setup == null)
                return Invalid(out failure, "Expanded Skirmish session or resolved setup is unavailable.");

            SkirmishExpandedSessionComponent sessionState =
                em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (sessionState.IsLegacy != 0 || sessionState.SetupHash != setup.SetupHash)
                return Invalid(out failure, "Delivery-basis startup identity does not match an expanded Skirmish setup.");

            SkirmishProductionDeliveryBasisStartupResult resolution = TryResolveForSetup(
                em, setup, selectedPlacementGrid, mapReadyForSession: true,
                out SkirmishProductionDeliveryBasisComponent resolved,
                out SkirmishProductionDeliveryBasisProvenanceComponent provenance, out failure);
            if (resolution != SkirmishProductionDeliveryBasisStartupResult.Ready) return resolution;
            bool hasBasis = em.HasComponent<SkirmishProductionDeliveryBasisComponent>(session);
            bool hasProvenance = em.HasComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
            if (hasBasis != hasProvenance)
                return Invalid(out failure, "Expanded Skirmish delivery basis and native provenance must be stored together.");
            if (hasBasis)
            {
                var existing = em.GetComponentData<SkirmishProductionDeliveryBasisComponent>(session);
                if (!Same(in existing, in resolved))
                    return Invalid(out failure, "Expanded Skirmish session delivery basis changed after startup binding.");
                var existingProvenance = em.GetComponentData<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
                if (!Same(in existingProvenance, in provenance))
                    return Invalid(out failure, "Expanded Skirmish native map provenance changed after startup binding.");
            }
            else
            {
                em.AddComponentData(session, resolved);
                em.AddComponentData(session, provenance);
            }
            return SkirmishProductionDeliveryBasisStartupResult.Ready;
        }

        // Read-only shared resolver. Checkpoint restoration can validate the exact
        // map/grid basis before queuing or mutating a session, then attach this
        // already-resolved value to the returned session.
        internal static SkirmishProductionDeliveryBasisStartupResult TryResolveForSetup(
            EntityManager em,
            SkirmishResolvedSetup setup,
            Entity selectedPlacementGrid,
            bool mapReadyForSession,
            out SkirmishProductionDeliveryBasisComponent resolved,
            out SkirmishProductionDeliveryBasisProvenanceComponent provenance,
            out string failure)
        {
            failure = null;
            resolved = default;
            provenance = default;
            if (em == default || setup == null)
                return Invalid(out failure, "Resolved setup is unavailable for delivery-basis preflight.");

            using var roots = em.CreateEntityQuery(
                typeof(OperationMapRootComponent),
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent),
                typeof(OperationMapReadinessComponent));
            int rootCount = roots.CalculateEntityCount();
            // The caller only reaches this owner in play mode after map, surface,
            // and baked registry publication. A missing complete map root is a
            // ready-session contract failure, not a condition to retry forever.
            if (rootCount == 0)
            {
                if (!mapReadyForSession) return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Active operation-map root is missing after player map readiness.");
            }
            if (rootCount != 1) return Invalid(out failure, "Expected exactly one active operation-map root for Skirmish delivery basis.");

            Entity root = roots.GetSingletonEntity();
            ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(root);
            OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(root);
            OperationMapReadinessComponent readiness = em.GetComponentData<OperationMapReadinessComponent>(root);
            bool gridOwnerReady = (readiness.ReadyFlags & GridOwnerReady) == GridOwnerReady;
            if (readiness.Generation != active.Generation || metadata.Generation != active.Generation)
            {
                if (!gridOwnerReady) return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Operation-map generation is inconsistent after its scene owner became ready.");
            }
            if (!metadata.Blob.IsCreated)
            {
                if (!gridOwnerReady) return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Active operation-map metadata is missing after its scene owner became ready.");
            }

            ref OperationMapBlob map = ref metadata.Blob.Value;
            if (metadata.PhysicalSourceValidated != 1 ||
                active.OperationMapId.ToString() != setup.OperationMapId ||
                map.OperationMapId.ToString() != active.OperationMapId.ToString() ||
                map.SourceIdentityHash.Length == 0 || map.SourceContentHash.Length == 0)
            {
                if (!gridOwnerReady) return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Expanded Skirmish delivery basis requires the validated active physical map identity.");
            }

            if (selectedPlacementGrid == Entity.Null || !em.Exists(selectedPlacementGrid) ||
                !em.HasComponent<GridConfig>(selectedPlacementGrid) ||
                em.HasComponent<RuntimeGridBootstrapGridTag>(selectedPlacementGrid))
            {
                if (!gridOwnerReady) return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "The selected authored placement grid is unavailable after its scene owner became ready.");
            }

            GridConfig grid = em.GetComponentData<GridConfig>(selectedPlacementGrid);
            if (!OperationMapMetadataUtility.MatchesGridFrame(in map.Grid, in grid))
                return Invalid(out failure, "Selected placement grid differs from the active physical map frame.");

            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            int surfaceCount = surfaces.CalculateEntityCount();
            if (surfaceCount == 0)
            {
                if ((readiness.ReadyFlags & OperationMapReadinessFlags.MapSurface) == 0)
                    return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Active map surface is missing after map-surface readiness was published.");
            }
            if (surfaceCount != 1) return Invalid(out failure, "Expected exactly one active runtime map surface.");
            MapSurfaceComponent surface = surfaces.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
            {
                if ((readiness.ReadyFlags & OperationMapReadinessFlags.MapSurface) == 0)
                    return SkirmishProductionDeliveryBasisStartupResult.Pending;
                return Invalid(out failure, "Active map surface data is unavailable after map-surface readiness was published.");
            }
            ref MapSurfaceBlob surfaceBlob = ref surface.SurfaceBlob.Value;
            if (surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height ||
                math.abs(surface.CellSize - grid.CellSize) > SurfaceFrameTolerance ||
                math.any(math.abs(surface.GridOrigin - grid.Origin) > new float3(SurfaceFrameTolerance)) ||
                surfaceBlob.Dimensions.x != grid.Width || surfaceBlob.Dimensions.y != grid.Height ||
                math.abs(surfaceBlob.CellSize - grid.CellSize) > SurfaceFrameTolerance ||
                math.any(math.abs(surfaceBlob.GridOrigin - grid.Origin) > new float3(SurfaceFrameTolerance)))
                return Invalid(out failure, "Active map surface frame differs from the selected placement grid.");

            if (!SharedProductionDeliveryKernel.TryResolveAxisAlignedMapFrame(
                    grid.Width, grid.Height, grid.CellSize, grid.Origin,
                    out ProductionDeliveryLogicalBasis basis) ||
                !SharedProductionDeliveryKernel.IsValidProductionDeliveryLogicalBasis(in basis))
                return Invalid(out failure, "The active map frame cannot resolve a finite production delivery basis.");

            resolved = new SkirmishProductionDeliveryBasisComponent
            {
                Basis = basis,
                SetupHash = setup.SetupHash,
                PlacementGrid = selectedPlacementGrid
            };
            provenance = new SkirmishProductionDeliveryBasisProvenanceComponent
            {
                OperationMapId = new FixedString128Bytes(active.OperationMapId.ToString()),
                SourceIdentityHash = map.SourceIdentityHash,
                MapGeneration = active.Generation
            };
            return SkirmishProductionDeliveryBasisStartupResult.Ready;
        }

        internal static bool TryAttachResolved(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            in SkirmishProductionDeliveryBasisComponent resolved,
            in SkirmishProductionDeliveryBasisProvenanceComponent provenance)
        {
            if (em == default || !em.Exists(session) || setup == null ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return false;
            SkirmishExpandedSessionComponent state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (state.IsLegacy != 0 || state.SetupHash != setup.SetupHash ||
                !CanAttachResolved(em, session, setup, in resolved, in provenance)) return false;
            bool hasBasis = em.HasComponent<SkirmishProductionDeliveryBasisComponent>(session);
            bool hasProvenance = em.HasComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
            if (hasBasis != hasProvenance) return false;
            if (hasBasis)
            {
                var existing = em.GetComponentData<SkirmishProductionDeliveryBasisComponent>(session);
                var existingProvenance = em.GetComponentData<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
                return Same(in existing, in resolved) && Same(in existingProvenance, in provenance);
            }
            em.AddComponentData(session, resolved);
            em.AddComponentData(session, provenance);
            return true;
        }

        // Preflight a reused launch entity before TryQueue mutates it. Existing
        // stale or partially written basis state rejects without destroying it.
        internal static bool CanAttachResolved(
            EntityManager em,
            Entity candidate,
            SkirmishResolvedSetup setup,
            in SkirmishProductionDeliveryBasisComponent resolved,
            in SkirmishProductionDeliveryBasisProvenanceComponent provenance)
        {
            if (em == default || !em.Exists(candidate) || setup == null ||
                resolved.SetupHash != setup.SetupHash || resolved.PlacementGrid == Entity.Null ||
                !em.Exists(resolved.PlacementGrid) || !em.HasComponent<GridConfig>(resolved.PlacementGrid) ||
                em.HasComponent<RuntimeGridBootstrapGridTag>(resolved.PlacementGrid) ||
                !SharedProductionDeliveryKernel.IsValidProductionDeliveryLogicalBasis(in resolved.Basis)) return false;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1 || grids.GetSingletonEntity() != resolved.PlacementGrid) return false;
            bool hasBasis = em.HasComponent<SkirmishProductionDeliveryBasisComponent>(candidate);
            bool hasProvenance = em.HasComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(candidate);
            if (hasBasis != hasProvenance) return false;
            if (!hasBasis) return true;
            var existing = em.GetComponentData<SkirmishProductionDeliveryBasisComponent>(candidate);
            var existingProvenance = em.GetComponentData<SkirmishProductionDeliveryBasisProvenanceComponent>(candidate);
            return Same(in existing, in resolved) && Same(in existingProvenance, in provenance);
        }

        static bool Same(in SkirmishProductionDeliveryBasisComponent a, in SkirmishProductionDeliveryBasisComponent b) =>
            a.SetupHash == b.SetupHash && a.PlacementGrid == b.PlacementGrid &&
            math.all(a.Basis.Right == b.Basis.Right) && math.all(a.Basis.Forward == b.Basis.Forward);

        static bool Same(in SkirmishProductionDeliveryBasisProvenanceComponent a,
            in SkirmishProductionDeliveryBasisProvenanceComponent b) =>
            a.OperationMapId.Equals(b.OperationMapId) && a.SourceIdentityHash.Equals(b.SourceIdentityHash) &&
            a.MapGeneration == b.MapGeneration;

        static SkirmishProductionDeliveryBasisStartupResult Invalid(out string failure, string message)
        {
            failure = message;
            return SkirmishProductionDeliveryBasisStartupResult.Invalid;
        }
    }
}
