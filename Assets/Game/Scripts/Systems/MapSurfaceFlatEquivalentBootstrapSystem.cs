using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MapSurfaceFlatEquivalentBootstrapSystem : ISystem
    {
        private EntityQuery _gridQuery;
        private EntityQuery _surfaceQuery;
        private EntityQuery _ownedSurfaceQuery;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _surfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            _ownedSurfaceQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MapSurfaceComponent>(),
                ComponentType.ReadOnly<MapSurfaceFlatEquivalentRuntimeBlobTag>());
            state.RequireForUpdate(_gridQuery);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_ownedSurfaceQuery.IsEmptyIgnoreFilter)
                return;

            ComponentTypeHandle<MapSurfaceComponent> surfaceType = state.GetComponentTypeHandle<MapSurfaceComponent>(true);
            using NativeArray<ArchetypeChunk> chunks = _ownedSurfaceQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<MapSurfaceComponent> surfaces = chunks[chunkIndex].GetNativeArray(ref surfaceType);
                for (int i = 0; i < surfaces.Length; i++)
                {
                    BlobAssetReference<MapSurfaceBlob> blob = surfaces[i].SurfaceBlob;
                    if (blob.IsCreated)
                        blob.Dispose();
                }
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_surfaceQuery.IsEmptyIgnoreFilter || _gridQuery.IsEmptyIgnoreFilter)
            {
                state.Enabled = false;
                return;
            }

            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            if (!TryBuildFlatEquivalent(grid, Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new MapSurfaceComponent
            {
                SurfaceBlob = surfaceBlob,
                GridOrigin = grid.Origin,
                CellSize = grid.CellSize,
                Dimensions = new int2(grid.Width, grid.Height),
                HasSurfaceData = 1,
                HasLayeredCells = 0,
                HasRoadSurfaces = 0,
                HasBridgeSurfaces = 0
            });
            ecb.AddComponent(entity, new MapSurfacePathCostComponent
            {
                EnableSlopeCost = 0,
                GentleSlopeTraversalCost = 0,
                SteepSlopeTraversalCost = 0
            });
            ecb.AddComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            state.Enabled = false;
        }

        private static bool TryBuildFlatEquivalent(
            GridConfig grid,
            Allocator allocator,
            out BlobAssetReference<MapSurfaceBlob> surfaceBlob)
        {
            surfaceBlob = default;

            if (grid.CellSize <= 0f || grid.Width <= 0 || grid.Height <= 0)
                return false;

            int cellCount = grid.Width * grid.Height;
            using var builder = new BlobBuilder(Allocator.Temp);
            ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
            root.GridOrigin = grid.Origin;
            root.CellSize = grid.CellSize;
            root.Dimensions = new int2(grid.Width, grid.Height);
            root.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
            root.CompactMinHeight = grid.Origin.y;
            root.CompactHeightStep = 0.01f;

            builder.Allocate(ref root.Cells, 0);
            builder.Allocate(ref root.Samples, 0);
            builder.Allocate(ref root.Connections, 0);
            BlobBuilderArray<MapSurfaceCompactSample> samples = builder.Allocate(ref root.CompactSamples, cellCount);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int index = x + y * grid.Width;

                    samples[index] = new MapSurfaceCompactSample
                    {
                        LayerId = 0,
                        PackedHeight = 0,
                        NormalX = 0,
                        NormalY = sbyte.MaxValue,
                        NormalZ = 0,
                        SurfaceType = MapSurfaceType.Terrain,
                        MovementMask = MapSurfaceMovementMask.AllGroundUnits |
                                       MapSurfaceMovementMask.AirGrounded |
                                       MapSurfaceMovementMask.BuildingPlacement,
                        Flags = MapSurfaceFlags.None
                    };
                }
            }

            surfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(allocator);
            return surfaceBlob.IsCreated;
        }
    }
}
