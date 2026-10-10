using System.Collections.Generic;
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
            NativeList<Bounds> unused = default;
            return ReadAuthoredBuildingBounds(manager, ref unused, false);
        }
        public void CollectAuthoredBuildingBounds(EntityManager manager, ref NativeList<Bounds> results)
        {
            ReadAuthoredBuildingBounds(manager, ref results, true);
        }

        private static int ReadAuthoredBuildingBounds(EntityManager manager, ref NativeList<Bounds> results, bool collect)
        {
            using var query = AuthoredBuildingGeometry(manager);
            query.CompleteDependency();
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            var meshArrayType = manager.GetSharedComponentTypeHandle<RenderMeshArray>();
            var materialMeshType = manager.GetComponentTypeHandle<MaterialMeshInfo>(true);
            var transformType = manager.GetComponentTypeHandle<LocalToWorld>(true);
            int count = 0;
            // Call-local metadata only: each subsequent read still observes mesh
            // renames, edited mesh bounds and every entity's current transform.
            var meshMetadata = new Dictionary<Mesh, PlacementSceneryMesh>();
            var meshArrays = new Dictionary<UnityObjectRef<Mesh>[], PlacementSceneryMesh[]>();
            foreach (var chunk in chunks)
            {
                // Resolve the shared mesh array once per chunk. Per-entity managed
                // component lookups made a dense Campaign placement census take
                // over 130 ms, even when none of its scenery changed.
                var array = chunk.GetSharedComponentManaged(meshArrayType, manager);
                var references = array.MeshReferences;
                PlacementSceneryMesh[] meshes = System.Array.Empty<PlacementSceneryMesh>();
                if (references != null && !meshArrays.TryGetValue(references, out meshes))
                {
                    meshes = new PlacementSceneryMesh[references.Length];
                    for (int i = 0; i < references.Length; i++)
                        meshes[i] = ReadPlacementSceneryMesh(references[i], meshMetadata);
                    meshArrays.Add(references, meshes);
                }
                var materials = chunk.GetNativeArray(ref materialMeshType);
                var transforms = chunk.GetNativeArray(ref transformType);
                var enabled = chunk.GetEnabledMask(ref materialMeshType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    // ToEntityArray filtered disabled MaterialMeshInfo instances;
                    // chunk iteration must preserve that same query boundary.
                    if (!enabled[i] || !TryReadPlacementSceneryBounds(array, meshes, materials[i], transforms[i].Value, out var bounds)) continue;
                    count++;
                    if (collect) results.Add(bounds);
                }
            }
            return count;
        }

        private static bool TryReadPlacementSceneryBounds(RenderMeshArray array, PlacementSceneryMesh[] meshes, MaterialMeshInfo materialMesh, float4x4 matrix, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            if (materialMesh.HasMaterialMeshIndexRange)
            {
                var range = materialMesh.MaterialMeshIndexRange;
                // GetMesh returns only the first mesh of a range. Reserve every
                // standing part, including trees behind a flat ground-cover mesh.
                for (int i = range.start; i < range.end; i++)
                    AppendPlacementSceneryBounds(meshes[array.MaterialMeshIndices[i].MeshIndex], matrix, ref bounds, ref found);
            }
            // RenderMeshArray.GetMesh returns null for a runtime mesh. Static
            // negative indices use the package's public array-index conversion.
            else if (materialMesh.Mesh < 0)
                AppendPlacementSceneryBounds(meshes[MaterialMeshInfo.StaticIndexToArrayIndex(materialMesh.Mesh)], matrix, ref bounds, ref found);
            return found;
        }

        private struct PlacementSceneryMesh
        {
            public bool Eligible;
            public Bounds LocalBounds;
        }

        private static PlacementSceneryMesh ReadPlacementSceneryMesh(Mesh mesh, Dictionary<Mesh, PlacementSceneryMesh> metadata)
        {
            if (ReferenceEquals(mesh, null)) return default;
            if (metadata.TryGetValue(mesh, out var descriptor)) return descriptor;
            descriptor = default;
            if (mesh != null)
            {
                string name = mesh.name;
                descriptor.Eligible = !name.Contains("Destroyed") &&
                    (name.StartsWith("SM_Bld_", System.StringComparison.Ordinal) ||
                     name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("Palm", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.StartsWith("SM_Prop_", System.StringComparison.Ordinal));
                if (descriptor.Eligible) descriptor.LocalBounds = mesh.bounds;
            }
            metadata.Add(mesh, descriptor);
            return descriptor;
        }

        private static void AppendPlacementSceneryBounds(PlacementSceneryMesh mesh, float4x4 matrix, ref Bounds bounds, ref bool found)
        {
            if (!mesh.Eligible) return;
            var local = mesh.LocalBounds;
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
