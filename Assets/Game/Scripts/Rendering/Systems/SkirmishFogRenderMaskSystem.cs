using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    public partial class SkirmishFogRenderMaskSystem : SystemBase
    {
        private readonly Dictionary<Entity, bool> masked = new();
        private readonly HashSet<Entity> hidden = new();
        private readonly HashSet<Entity> visited = new();
        private readonly Stack<Entity> pending = new();
        private readonly List<Entity> released = new();

        protected override void OnUpdate()
        {
            EntityManager.CompleteAllTrackedJobs();
            hidden.Clear();
            using var query = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities });
            using var contacts = query.ToEntityArray(Allocator.Temp);
            foreach (var contact in contacts)
            {
                if (SkirmishFogRenderVisibility.PlayerVisible(EntityManager, contact)) continue;
                visited.Clear(); pending.Clear(); pending.Push(contact);
                if (EntityManager.HasComponent<UnitDetailedVisualReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitDetailedVisualReference>(contact).Root);
                if (EntityManager.HasComponent<UnitModelInstanceReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitModelInstanceReference>(contact).Instance);
                if (EntityManager.HasComponent<UnitMidLodInstanceReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitMidLodInstanceReference>(contact).Instance);
                if (EntityManager.HasComponent<UnitLowLodInstanceReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitLowLodInstanceReference>(contact).Instance);
                if (EntityManager.HasComponent<UnitHealthBarInstanceReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitHealthBarInstanceReference>(contact).Instance);
                if (EntityManager.HasComponent<UnitSelectionMarkerInstanceReference>(contact))
                    pending.Push(EntityManager.GetComponentData<UnitSelectionMarkerInstanceReference>(contact).Instance);
                while (pending.Count > 0)
                {
                    Entity node = pending.Pop();
                    if (node == Entity.Null || !EntityManager.Exists(node) || !visited.Add(node)) continue;
                    if (EntityManager.HasComponent<MaterialMeshInfo>(node)) hidden.Add(node);
                    if (EntityManager.HasBuffer<Child>(node))
                        foreach (var child in EntityManager.GetBuffer<Child>(node, true)) pending.Push(child.Value);
                    if (EntityManager.HasBuffer<LinkedEntityGroup>(node))
                        foreach (var linked in EntityManager.GetBuffer<LinkedEntityGroup>(node, true)) pending.Push(linked.Value);
                    if (EntityManager.HasBuffer<SelectionObjectOutlineInstanceElement>(node))
                        foreach (var outline in EntityManager.GetBuffer<SelectionObjectOutlineInstanceElement>(node, true)) pending.Push(outline.Value);
                }
            }
            using var airborne = EntityManager.CreateEntityQuery(typeof(AirMissileFlyingVisualComponent));
            using var airVisuals = airborne.ToEntityArray(Allocator.Temp);
            foreach (var visual in airVisuals)
            {
                Entity source = EntityManager.GetComponentData<AirMissileFlyingVisualComponent>(visual).Launcher;
                CollectVisualTree(visual, !FlightVisible(visual, source, 2));
            }
            using var ground = EntityManager.CreateEntityQuery(typeof(GroundMissileFlyingRocketVisualComponent));
            using var groundVisuals = ground.ToEntityArray(Allocator.Temp);
            foreach (var visual in groundVisuals)
            {
                Entity source = EntityManager.GetComponentData<GroundMissileFlyingRocketVisualComponent>(visual).Launcher;
                CollectVisualTree(visual, !FlightVisible(visual, source, 1));
            }
            using var projectiles = EntityManager.CreateEntityQuery(
                new EntityQueryDesc { All = new[] { ComponentType.ReadOnly<AirMissileProjectileComponent>() } },
                new EntityQueryDesc { All = new[] { ComponentType.ReadOnly<GroundMissileProjectileComponent>() } });
            using var shots = projectiles.ToEntityArray(Allocator.Temp);
            foreach (var shot in shots)
            {
                bool air = EntityManager.HasComponent<AirMissileProjectileComponent>(shot);
                Entity source = air ? EntityManager.GetComponentData<AirMissileProjectileComponent>(shot).Source
                    : EntityManager.GetComponentData<GroundMissileProjectileComponent>(shot).Source;
                CollectVisualTree(shot, !FlightVisible(shot, source, air ? (byte)2 : (byte)1));
            }
            // Never remove another owner's DisableRendering, Disabled, or LOD/death/transport state.
            foreach (var node in hidden)
            {
                if (!masked.ContainsKey(node)) masked.Add(node, EntityManager.IsComponentEnabled<MaterialMeshInfo>(node));
                EntityManager.SetComponentEnabled<MaterialMeshInfo>(node, false);
            }
            released.Clear();
            foreach (var pair in masked)
                if (!hidden.Contains(pair.Key))
                {
                    if (EntityManager.Exists(pair.Key) && EntityManager.HasComponent<MaterialMeshInfo>(pair.Key))
                        EntityManager.SetComponentEnabled<MaterialMeshInfo>(pair.Key, pair.Value);
                    released.Add(pair.Key);
                }
            foreach (var node in released) masked.Remove(node);
        }

        private bool FlightVisible(Entity visual, Entity source, byte detectorDomain)
        {
            if (SkirmishFogRenderVisibility.PlayerVisible(EntityManager, source)) return true;
            if (!EntityManager.Exists(source) && !SkirmishFogObservedPointUtility.HasActiveSharedAttempt(EntityManager))
                return true; // Preserve missing-launcher flights in ordinary Campaign worlds.
            float3 position;
            if (EntityManager.HasComponent<LocalToWorld>(visual))
                position = EntityManager.GetComponentData<LocalToWorld>(visual).Position;
            else if (EntityManager.HasComponent<LocalTransform>(visual))
                position = EntityManager.GetComponentData<LocalTransform>(visual).Position;
            else return false;
            return SkirmishFogRenderVisibility.PositionObserved(EntityManager, source, position, detectorDomain);
        }
        private void CollectVisualTree(Entity root, bool hide)
        {
            visited.Clear(); pending.Clear(); pending.Push(root);
            while (pending.Count > 0)
            {
                Entity node = pending.Pop();
                if (node == Entity.Null || !EntityManager.Exists(node) || !visited.Add(node)) continue;
                if (EntityManager.HasComponent<MaterialMeshInfo>(node))
                {
                    if (hide) hidden.Add(node);
                    else hidden.Remove(node);
                }
                if (EntityManager.HasBuffer<Child>(node))
                    foreach (var child in EntityManager.GetBuffer<Child>(node, true)) pending.Push(child.Value);
                if (EntityManager.HasBuffer<LinkedEntityGroup>(node))
                    foreach (var child in EntityManager.GetBuffer<LinkedEntityGroup>(node, true)) pending.Push(child.Value);
            }
        }
        protected override void OnDestroy()
        {
            foreach (var pair in masked)
                if (EntityManager.Exists(pair.Key) && EntityManager.HasComponent<MaterialMeshInfo>(pair.Key))
                    EntityManager.SetComponentEnabled<MaterialMeshInfo>(pair.Key, pair.Value);
            masked.Clear();
        }
    }
}
