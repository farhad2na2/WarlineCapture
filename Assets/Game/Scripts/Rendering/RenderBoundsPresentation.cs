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
        private static EntityQuery AuthoredBuildingGeometry(EntityManager manager) => manager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<MaterialMeshInfo>(), ComponentType.ReadOnly<RenderMeshArray>() },
            None = new[] { ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>(), ComponentType.ReadOnly<Prefab>() }
        });
        public int CountAuthoredBuildingGeometry(EntityManager manager)
        {
            using var query = AuthoredBuildingGeometry(manager);
            using var entities = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var entity in entities)
                if (TryReadPlacementSceneryBounds(manager, entity, out _)) count++;
            return count;
        }
        public void CollectAuthoredBuildingBounds(EntityManager manager, ref NativeList<Bounds> results)
        {
            using var query = AuthoredBuildingGeometry(manager);
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (TryReadPlacementSceneryBounds(manager, entities[i], out var bounds)) results.Add(bounds);
            }
        }

        private static bool TryReadPlacementSceneryBounds(EntityManager manager, Entity entity, out Bounds bounds)
        {
            bounds = default;
            var materialMesh = manager.GetComponentData<MaterialMeshInfo>(entity);
            var array = manager.GetSharedComponentManaged<RenderMeshArray>(entity);
            var matrix = manager.GetComponentData<LocalToWorld>(entity).Value;
            bool found = false;
            if (materialMesh.HasMaterialMeshIndexRange)
            {
                var range = materialMesh.MaterialMeshIndexRange;
                // GetMesh returns only the first mesh of a range. Reserve every
                // standing part, including trees behind a flat ground-cover mesh.
                for (int i = range.start; i < range.end; i++)
                    AppendPlacementSceneryBounds(array.MeshReferences[array.MaterialMeshIndices[i].MeshIndex], matrix, ref bounds, ref found);
            }
            else AppendPlacementSceneryBounds(array.GetMesh(materialMesh), matrix, ref bounds, ref found);
            return found;
        }

        private static void AppendPlacementSceneryBounds(Mesh mesh, float4x4 matrix, ref Bounds bounds, ref bool found)
        {
            if (mesh == null || mesh.name.Contains("Destroyed")) return;
            string name = mesh.name;
            if (!(name.StartsWith("SM_Bld_", System.StringComparison.Ordinal) ||
                 name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("Palm", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.StartsWith("SM_Prop_", System.StringComparison.Ordinal))) return;
            var local = mesh.bounds;
            var world = AABB.Transform(matrix, new AABB { Center = local.center, Extents = local.extents });
            if (world.Extents.y < .5f || !math.all(math.isfinite(world.Center)) || !math.all(math.isfinite(world.Extents))) return;
            var current = new Bounds(world.Center, world.Extents * 2f);
            if (found) bounds.Encapsulate(current); else bounds = current;
            found = true;
        }

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
