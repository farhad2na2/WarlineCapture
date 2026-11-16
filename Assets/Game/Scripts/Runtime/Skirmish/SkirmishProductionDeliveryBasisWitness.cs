#if UNITY_EDITOR
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ProductionDeliveryLogicalBasis = Game.Simulation.SharedProductionDeliveryKernel.ProductionDeliveryLogicalBasis;

namespace Game.Runtime
{
    // This readback is an Editor-only witness. Its pass means the actual active
    // ECS map/session owners agree; it does not establish Burst or native player parity.
    internal static class SkirmishProductionDeliveryBasisWitness
    {
        internal static bool Record(EntityManager em, Entity session, Entity selectedGrid, string stage)
        {
            bool valid = TryRead(em, session, selectedGrid, stage, out var identity);
            Debug.Log("[SkirmishProductionDeliveryBasis] result=" + (valid ? "Passed" : "Failed") +
                " stage=" + stage + " session=" + identity.SessionId + " setup=" + identity.SetupHash +
                " map=" + identity.MapId + " generation=" + identity.Generation +
                " grid=" + identity.Grid + " gridFrame=" + identity.GridFrame +
                " sourceIdentity=" + identity.SourceIdentity + " right=" + identity.Right +
                " forward=" + identity.Forward + " scope=ActualActiveExpandedSkirmishECSReadback burst_verified=0");
            return valid;
        }

        private static bool TryRead(EntityManager em, Entity session, Entity selectedGrid, string stage,
            out (string SessionId, uint SetupHash, string MapId, int Generation, string Grid,
                string GridFrame, string SourceIdentity, string Right, string Forward) identity)
        {
            identity = default;
            if (em == default || !em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishProductionDeliveryBasisComponent>(session) ||
                !em.HasComponent<SkirmishProductionDeliveryBasisProvenanceComponent>(session)) return false;
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var basisComponent = em.GetComponentData<SkirmishProductionDeliveryBasisComponent>(session);
            var provenance = em.GetComponentData<SkirmishProductionDeliveryBasisProvenanceComponent>(session);
            bool preCommit = stage == "PreCommit";
            if (state.IsLegacy != 0 || setup == null || state.SetupHash != setup.SetupHash ||
                basisComponent.SetupHash != setup.SetupHash ||
                (preCommit ? state.SpawnComplete != 0 || state.Phase == SkirmishSessionPhase.Playing
                           : state.SpawnComplete == 0 || state.Phase != SkirmishSessionPhase.Playing)) return false;

            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1 || selectedGrid == Entity.Null ||
                grids.GetSingletonEntity() != selectedGrid || !em.Exists(selectedGrid) ||
                em.HasComponent<RuntimeGridBootstrapGridTag>(selectedGrid)) return false;
            GridConfig grid = em.GetComponentData<GridConfig>(selectedGrid);

            using var roots = em.CreateEntityQuery(typeof(OperationMapRootComponent), typeof(ActiveOperationMapComponent),
                typeof(OperationMapMetadataComponent), typeof(OperationMapReadinessComponent));
            if (roots.CalculateEntityCount() != 1) return false;
            Entity root = roots.GetSingletonEntity();
            var active = em.GetComponentData<ActiveOperationMapComponent>(root);
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(root);
            var readiness = em.GetComponentData<OperationMapReadinessComponent>(root);
            if (active.Generation != metadata.Generation || readiness.Generation != active.Generation ||
                metadata.PhysicalSourceValidated != 1 || !metadata.Blob.IsCreated ||
                (readiness.ReadyFlags & OperationMapReadinessFlags.SubScene) == 0) return false;
            ref OperationMapBlob map = ref metadata.Blob.Value;
            if (active.OperationMapId.ToString() != setup.OperationMapId ||
                map.OperationMapId.ToString() != active.OperationMapId.ToString() ||
                !OperationMapMetadataUtility.MatchesGridFrame(in map.Grid, in grid) ||
                provenance.OperationMapId.ToString() != active.OperationMapId.ToString() ||
                provenance.MapGeneration != active.Generation ||
                !provenance.SourceIdentityHash.Equals(map.SourceIdentityHash) ||
                basisComponent.PlacementGrid != selectedGrid ||
                !SharedProductionDeliveryKernel.IsValidProductionDeliveryLogicalBasis(in basisComponent.Basis) ||
                !math.all(basisComponent.Basis.Right == ProductionDeliveryLogicalBasis.WorldAxes.Right) ||
                !math.all(basisComponent.Basis.Forward == ProductionDeliveryLogicalBasis.WorldAxes.Forward)) return false;

            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (surfaces.CalculateEntityCount() != 1) return false;
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated ||
                (readiness.ReadyFlags & OperationMapReadinessFlags.MapSurface) == 0 ||
                surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height ||
                math.abs(surface.CellSize - grid.CellSize) > 0.0001f ||
                math.any(math.abs(surface.GridOrigin - grid.Origin) > new float3(0.0001f))) return false;
            ref MapSurfaceBlob surfaceBlob = ref surface.SurfaceBlob.Value;
            if (surfaceBlob.Dimensions.x != grid.Width || surfaceBlob.Dimensions.y != grid.Height ||
                math.abs(surfaceBlob.CellSize - grid.CellSize) > 0.0001f ||
                math.any(math.abs(surfaceBlob.GridOrigin - grid.Origin) > new float3(0.0001f))) return false;

            identity = (state.SessionId.ToString(), state.SetupHash, active.OperationMapId.ToString(), active.Generation,
                selectedGrid.Index + ":" + selectedGrid.Version,
                grid.Width + "x" + grid.Height + "@" + grid.CellSize + ":" + grid.Origin,
                map.SourceIdentityHash.ToString(), basisComponent.Basis.Right.ToString(), basisComponent.Basis.Forward.ToString());
            return true;
        }
    }
}
#endif
