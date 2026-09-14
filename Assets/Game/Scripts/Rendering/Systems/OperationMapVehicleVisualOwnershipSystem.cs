using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    public struct OperationMapVehicleVisualOwnershipApplied : IComponentData { }

    // Packed maps can retain the original prefab render tree beside the canonical
    // detailed tree. A dormant gameplay root does not disable its render children.
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(FactionVisualSystem))]
    [UpdateBefore(typeof(UnitRenderVisualExclusivitySystem))]
    public partial struct OperationMapVehicleVisualOwnershipSystem : ISystem
    {
        private EntityQuery pending;

        public void OnCreate(ref SystemState state)
        {
            pending = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                    ComponentType.ReadOnly<UnitDetailedVisualReference>(), ComponentType.ReadOnly<Child>() },
                None = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<OperationMapVehicleVisualOwnershipApplied>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
            state.RequireForUpdate(pending);
        }

        public void OnUpdate(ref SystemState state)
        {
            using var units = pending.ToEntityArray(Allocator.Temp);
            foreach (var unit in units) Repair(state.EntityManager, unit);
        }

        public static void Repair(EntityManager em, Entity unit)
        {
            Entity detail = em.GetComponentData<UnitDetailedVisualReference>(unit).Root;
            if (detail == Entity.Null || !em.Exists(detail)) return;
            var canonical = new HashSet<Entity>();
            Collect(em, detail, canonical);
            var renderers = new Dictionary<Mesh, List<Entity>>();
            foreach (Entity e in canonical)
            {
                Mesh mesh = MeshOf(em, e);
                if (mesh == null) continue;
                if (!renderers.TryGetValue(mesh, out var targets)) renderers.Add(mesh, targets = new List<Entity>());
                targets.Add(e);
            }
            if (renderers.Count == 0) return; // Wait for the streamed render tree.
            var wholeTree = new HashSet<Entity>();
            Collect(em, unit, wholeTree);
            foreach (Entity e in wholeTree)
            {
                if (canonical.Contains(e)) continue;
                Mesh mesh = MeshOf(em, e);
                if (mesh == null || !renderers.TryGetValue(mesh, out var targets)) continue;
                foreach (Entity target in targets)
                {
                    // Only retire coincident copies, never another attachment or a marker.
                    if (!Coincident(em, e, target)) continue;
                    if (!em.HasComponent<DisableRendering>(e)) em.AddComponent<DisableRendering>(e);
                    if (em.HasComponent<FactionTintColor>(e))
                    {
                        // One ECS owner for _BaseColor; keeping both property components
                        // makes the colour depend on which batch metadata wins.
                        if (em.HasComponent<URPMaterialPropertyBaseColor>(target)) em.RemoveComponent<URPMaterialPropertyBaseColor>(target);
                        em.AddComponentData(target, em.GetComponentData<FactionTintColor>(e));
                        em.AddComponent<FactionTintTarget>(target);
                        em.AddComponent<FactionUnitModelTintTarget>(target);
                    }
                }
            }
            em.AddComponent<OperationMapVehicleVisualOwnershipApplied>(unit);
        }

        private static bool Coincident(EntityManager em, Entity a, Entity b)
        {
            if (!em.HasComponent<LocalToWorld>(a) || !em.HasComponent<LocalToWorld>(b)) return false;
            var x = em.GetComponentData<LocalToWorld>(a).Value;
            var y = em.GetComponentData<LocalToWorld>(b).Value;
            return Unity.Mathematics.math.cmax(Unity.Mathematics.math.abs(x.c0 - y.c0)) < .001f &&
                Unity.Mathematics.math.cmax(Unity.Mathematics.math.abs(x.c1 - y.c1)) < .001f &&
                Unity.Mathematics.math.cmax(Unity.Mathematics.math.abs(x.c2 - y.c2)) < .001f &&
                Unity.Mathematics.math.cmax(Unity.Mathematics.math.abs(x.c3 - y.c3)) < .001f;
        }

        private static Mesh MeshOf(EntityManager em, Entity e)
        {
            if (!em.HasComponent<MaterialMeshInfo>(e) || !em.HasComponent<RenderMeshArray>(e) ||
                em.HasComponent<SelectionObjectOutlineTag>(e) || em.HasComponent<SelectionMarkerTag>(e)) return null;
            var info = em.GetComponentData<MaterialMeshInfo>(e);
            if (info.HasMaterialMeshIndexRange || info.Mesh >= 0) return null;
            return em.GetSharedComponentManaged<RenderMeshArray>(e).GetMesh(info);
        }

        private static void Collect(EntityManager em, Entity e, HashSet<Entity> result)
        {
            if (!em.Exists(e) || !result.Add(e) || !em.HasBuffer<Child>(e)) return;
            // Snapshot before any structural changes in Repair.
            var children = em.GetBuffer<Child>(e);
            for (int i = 0; i < children.Length; i++) Collect(em, children[i].Value, result);
        }
    }
}
