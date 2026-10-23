using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using Game.Runtime.Spawning;

namespace Game.Runtime
{
    public readonly struct MapSurfaceSpawnGrounding
    {

        public bool TryGroundCellCenter(
            EntityManager entityManager,
            GridConfig grid,
            int2 cell,
            ref float3 worldPosition,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            sample = default;
            if (!TryGetSurface(entityManager, out MapSurfaceComponent surface, out Entity surfaceEntity) ||
                !TryGetSample(surface, cell, out sample))
            {
                return false;
            }

            if (TryGetHighestInfantrySupport(surface, cell, sample, out MapSurfaceSample supportSample) &&
                SpawnGroundingKernel.ShouldLiftToSupport(sample.Height, supportSample.Height))
            {
                sample = supportSample;
            }

            if(entityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
            {
                var overlays=entityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity,true);
                var triangles=entityManager.HasBuffer<MapSurfaceMeshTriangle>(surfaceEntity)?entityManager.GetBuffer<MapSurfaceMeshTriangle>(surfaceEntity,true).AsNativeArray():default;
                var instances=entityManager.HasBuffer<MapSurfaceMeshInstance>(surfaceEntity)?entityManager.GetBuffer<MapSurfaceMeshInstance>(surfaceEntity,true).AsNativeArray():default;
                float bestHeight=float.NegativeInfinity;
                for(int i=0;i<overlays.Length;i++)
                {
                    var overlay=overlays[i];
                    if(!SpawnGroundingKernel.CanSampleOverlay((ushort)overlay.MovementMask) ||
                        !MapSurfaceSceneOverlaySampling.TrySample(overlay,worldPosition,triangles,instances,out float height,out float3 normal) || !SpawnGroundingKernel.AcceptOverlayHeight(height,ref bestHeight))continue;
                    sample.Height=height;sample.Normal=normal;
                    sample.LayerId=overlay.LayerId;sample.SurfaceType=overlay.SurfaceType;
                    sample.Flags=overlay.Flags;sample.MovementMask=overlay.MovementMask;
                }
            }
            worldPosition.y = SpawnGroundingKernel.FinalHeight(sample.Height, groundOffset);
            return true;
        }

        public bool TryGroundWorldPosition(
            EntityManager entityManager,
            GridConfig grid,
            ref float3 worldPosition,
            out int2 cell,
            out MapSurfaceSample sample,
            float groundOffset = 0f)
        {
            cell = GridUtils.WorldToCell(grid, worldPosition);
            return TryGroundCellCenter(entityManager, grid, cell, ref worldPosition, out sample, groundOffset);
        }

        private bool TryGetSurface(EntityManager entityManager, out MapSurfaceComponent surface, out Entity surfaceEntity)
        {
            surface = default; surfaceEntity=Entity.Null;

            using EntityQuery surfaceQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.IsEmptyIgnoreFilter)
                return false;

            surfaceEntity=surfaceQuery.GetSingletonEntity();
            surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            return surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
        }

        private bool TryGetSample(MapSurfaceComponent surface, int2 cell, out MapSurfaceSample sample)
        {
            sample = default;

            if ((uint)cell.x >= (uint)surface.Dimensions.x ||
                (uint)cell.y >= (uint)surface.Dimensions.y ||
                !surface.SurfaceBlob.IsCreated)
            {
                return false;
            }

            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, cell, out sample);
        }

        private bool TryGetHighestInfantrySupport(
            MapSurfaceComponent surface,
            int2 centerCell,
            MapSurfaceSample anchor,
            out MapSurfaceSample supportSample)
        {
            var context = new SupportSamples { Surface = surface };
            return SpawnGroundingKernel.TryGetHighestInfantrySupport<SupportSamples, MapSurfaceSample>(
                ref context, centerCell.x, centerCell.y, anchor, out supportSample);
        }

        private struct SupportSamples : ISpawnGroundingSamples<MapSurfaceSample>
        {
            public MapSurfaceComponent Surface;
            public bool TryGetSample(int x, int z, out MapSurfaceSample sample)
                => new MapSurfaceSpawnGrounding().TryGetSample(Surface, new int2(x, z), out sample);
            public SpawnGroundingFacts Facts(MapSurfaceSample sample) => new SpawnGroundingFacts
            {
                Height = sample.Height, LayerId = sample.LayerId, SurfaceType = (byte)sample.SurfaceType,
                Flags = (ushort)sample.Flags, MovementMask = (ushort)sample.MovementMask
            };
        }
    }
}
