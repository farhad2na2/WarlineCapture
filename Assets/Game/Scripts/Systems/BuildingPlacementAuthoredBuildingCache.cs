using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Game.Rendering.Contracts;
using Game.Simulation;

namespace Game.Runtime
{
    /// <summary>Reserves authored buildings, trees, and solid props before their streamed visuals are loaded.</summary>
    internal sealed class BuildingPlacementAuthoredBuildingCache
    {
        private World world;
        private Entity owner;
        private BlobAssetReference<OperationMapRenderDatabaseBlob> blob;
        private GridConfig grid;
        private bool[] mask;
        private int sceneGeometryCount;

        internal bool IsReady => mask != null;

        internal void Ensure(EntityManager em, GridConfig currentGrid)
        {
            int geometryCount = RenderBoundsGateway.Provider?.CountAuthoredBuildingGeometry(em) ?? 0;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>());
            var entity = query.CalculateEntityCount() == 1 ? query.GetSingletonEntity() : Entity.Null;
            var database = entity != Entity.Null ? em.GetComponentData<OperationMapRenderDatabaseComponent>(entity) : default;
            if (geometryCount == 0 && !database.Blob.IsCreated) { mask = null; world = null; return; }
            if (world == em.World && owner == entity && blob == database.Blob && mask != null &&
                sceneGeometryCount == geometryCount && grid.Width == currentGrid.Width && grid.Height == currentGrid.Height &&
                grid.CellSize == currentGrid.CellSize && math.all(grid.Origin == currentGrid.Origin)) return;
            world = em.World; owner = entity; blob = database.Blob; grid = currentGrid; sceneGeometryCount = geometryCount;
            mask = new bool[grid.Width * grid.Height];
            if (blob.IsCreated)
            {
            ref var data = ref blob.Value;
            for (int i = 0; i < data.Placements.Length; i++)
            {
                ref var placement = ref data.Placements[i];
                var bounds = data.Prototypes[placement.PrototypeIndex].CombinedLocalBounds;
                var matrix = placement.WorldMatrix;
                var worldBounds = SharedPlacementGeometry.TransformBounds(matrix, bounds.Center, bounds.Extents);
                bool building = placement.SemanticCategory == DenseCityPresentationSemanticCategory.GameplayBuildingIntact;
                bool solidScenery = placement.SemanticCategory is DenseCityPresentationSemanticCategory.Vegetation or
                    DenseCityPresentationSemanticCategory.Prop or DenseCityPresentationSemanticCategory.BuildingAttachmentIntact;
                // Ground cover and flat decals can stay beneath a foundation; standing scenery cannot.
                if (!SharedPlacementGeometry.ShouldReservePackedBounds(building, solidScenery, worldBounds.Extents.y)) continue;
                Reserve(worldBounds.Center, worldBounds.Extents);
            }
            }
            // Older operation-map scenes retain authored ECS renderers outside the packed database.
            // Their mesh bounds must reserve the same ground before starting grants are resolved.
            var boundsList = new NativeList<Bounds>(Allocator.Temp);
            try
            {
                RenderBoundsGateway.Provider?.CollectAuthoredBuildingBounds(em, ref boundsList);
                if (geometryCount > 0 && boundsList.Length == 0)
                {
                    mask = null; world = null; return; // Loaded geometry has not received its world transforms yet.
                }
                foreach (var bounds in boundsList) Reserve(bounds.center, bounds.extents);
                Debug.Log("[SkirmishPlacementGeometry] geometry=" + geometryCount + " buildingBounds=" + boundsList.Length + " packed=" + blob.IsCreated);
            }
            finally { boundsList.Dispose(); }
        }
        private void Reserve(float3 center, float3 extents)
        {
            SharedPlacementGeometry.Reserve(mask, grid.Width, grid.Height, grid.CellSize,
                grid.Origin, center, extents);
        }

        internal void AppendTo(bool[] target)
        {
            if (mask == null || mask.Length != target.Length) return;
            for (int i = 0; i < mask.Length; i++) target[i] |= mask[i];
        }
        internal bool Overlaps(GridConfig currentGrid, Vector2Int origin, Vector2Int size)
        {
            if (mask == null) return false;
            if (!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin, size, currentGrid)) return true;
            for (int y = origin.y; y < origin.y + size.y; y++)
                for (int x = origin.x; x < origin.x + size.x; x++) if (mask[y * grid.Width + x]) return true;
            return false;
        }
    }
}
