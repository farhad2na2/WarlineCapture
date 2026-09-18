using Game.Components;
using Game.Rendering.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    public static class RenderBoundsPresentation
    {
        public static IRenderBoundsPresentation Create() => new Reader();

        private sealed class Reader : IRenderBoundsPresentation
    {
        public void CollectWaterFootprints(EntityManager manager, ref NativeList<RenderSurfaceFootprint> results)
        {
            using var query = manager.CreateEntityQuery(typeof(RenderMeshArray), typeof(MaterialMeshInfo), typeof(LocalToWorld));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var mesh = manager.GetSharedComponentManaged<RenderMeshArray>(entity)
                    .GetMesh(manager.GetComponentData<MaterialMeshInfo>(entity));
                if (mesh == null || !mesh.name.Contains("Water_Plane")) continue;
                var local = mesh.bounds;
                var matrix = manager.GetComponentData<LocalToWorld>(entity).Value;
                var world = AABB.Transform(matrix, new AABB { Center = local.center, Extents = local.extents });
                results.Add(new RenderSurfaceFootprint { WorldToLocal = (Matrix4x4)math.inverse(matrix),
                    LocalBounds = local, WorldBounds = new Bounds(world.Center, world.Extents * 2) });
            }
        }

        public bool TryReadWorldBounds(EntityManager manager, Entity entity, out Bounds bounds)
        {
            bounds = default;
            if (entity == Entity.Null || !manager.Exists(entity)) return false;
            AABB value;
            if (manager.HasComponent<WorldRenderBounds>(entity))
                value = manager.GetComponentData<WorldRenderBounds>(entity).Value;
            else if (manager.HasComponent<RenderBounds>(entity) && manager.HasComponent<LocalToWorld>(entity))
                value = AABB.Transform(manager.GetComponentData<LocalToWorld>(entity).Value,
                    manager.GetComponentData<RenderBounds>(entity).Value);
            else return false;
            if (!math.all(math.isfinite(value.Center)) || !math.all(math.isfinite(value.Extents))) return false;
            bounds = new Bounds(value.Center, value.Extents * 2f);
            return true;
        }

        public void CollectStaticBounds(EntityManager manager, Rect horizontalArea, float minExtent, float maxExtent,
            ref NativeList<Bounds> results)
        {
            using var query = manager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<WorldRenderBounds>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<UnitFootprint>() }
            });
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var type = manager.GetComponentTypeHandle<WorldRenderBounds>(true);
            foreach (var chunk in chunks)
            {
                var bounds = chunk.GetNativeArray(ref type);
                for (int i = 0; i < bounds.Length; i++)
                {
                    AABB value = bounds[i].Value;
                    float extent = math.max(value.Extents.x, value.Extents.z);
                    if (!math.all(math.isfinite(value.Center)) || !math.all(math.isfinite(value.Extents)) ||
                        extent < minExtent || extent > maxExtent) continue;
                    var area = new Rect(value.Center.x - value.Extents.x, value.Center.z - value.Extents.z,
                        value.Extents.x * 2f, value.Extents.z * 2f);
                    if (area.xMax < horizontalArea.xMin || area.yMax < horizontalArea.yMin ||
                        area.xMin >= horizontalArea.xMax || area.yMin >= horizontalArea.yMax) continue;
                    results.Add(new Bounds(value.Center, value.Extents * 2f));
                }
            }
        }
    }
    }
}
