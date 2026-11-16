#if UNITY_EDITOR
using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Editor
{
    public static class S073CleanupNativeControls
    {
        private static void Need(bool value, string detail)
        {
            if (!value) throw new InvalidOperationException(detail);
        }

        public static void Run()
        {
            using var world = new World("S073CleanupNativeControls");
            var em = world.EntityManager;
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new Faction { Id = 1 });
            em.AddComponentData(entity, new PreviousParent { Value = entity });
            em.AddBuffer<Child>(entity).Add(new Child { Value = entity });
            using var living = em.CreateEntityQuery(typeof(Faction));
            using var previous = em.CreateEntityQuery(typeof(PreviousParent));
            Need(living.CalculateEntityCount() == 1 && previous.CalculateEntityCount() == 1, "Initial query state changed");
            em.DestroyEntity(entity);
            Need(em.Exists(entity) && !em.HasComponent<Faction>(entity) && em.HasComponent<PreviousParent>(entity) &&
                 em.HasBuffer<Child>(entity) && em.GetBuffer<Child>(entity).Length == 1 &&
                 living.CalculateEntityCount() == 0 && previous.CalculateEntityCount() == 1, "Cleanup residue not retained");
            em.DestroyEntity(entity);
            Need(em.Exists(entity) && em.HasBuffer<Child>(entity), "Repeated destroy removed cleanup residue");
            em.RemoveComponent<PreviousParent>(entity);
            Need(em.Exists(entity) && em.HasBuffer<Child>(entity), "Cleanup retired before final component");
            em.AddComponentData(entity, new Faction { Id = 2 });
            em.RemoveComponent<Child>(entity);
            Need(em.Exists(entity) && em.HasComponent<Faction>(entity) && previous.CalculateEntityCount() == 0,
                 "Reintroduced ordinary component was not retained after cleanup");
            em.DestroyEntity(entity);
            Need(!em.Exists(entity), "Reintroduced ordinary entity did not retire on destruction");

            var residue = em.CreateEntity();
            em.AddComponentData(residue, new Faction { Id = 1 });
            em.AddComponentData(residue, new PreviousParent());
            em.DestroyEntity(residue);
            em.RemoveComponent<PreviousParent>(residue);
            Need(!em.Exists(residue), "Cleanup-only residue did not retire when last cleanup was removed");

            var renewed = em.CreateEntity();
            em.AddComponentData(renewed, new Faction { Id = 1 });
            em.AddComponentData(renewed, new PreviousParent());
            em.DestroyEntity(renewed);
            em.AddComponentData(renewed, new Faction { Id = 2 });
            em.DestroyEntity(renewed);
            Need(em.Exists(renewed) && !em.HasComponent<Faction>(renewed) && em.HasComponent<PreviousParent>(renewed),
                 "Repeated destroy did not remove reintroduced ordinary component");
            em.RemoveComponent<PreviousParent>(renewed);
            Need(!em.Exists(renewed), "Renewed residue did not retire");

            var cleanupOnly = em.CreateEntity();
            em.AddComponentData(cleanupOnly, new PreviousParent());
            em.RemoveComponent<PreviousParent>(cleanupOnly);
            Need(em.Exists(cleanupOnly), "Live cleanup-only entity retired without destroy");
            em.DestroyEntity(cleanupOnly);
            Need(!em.Exists(cleanupOnly), "Ordinary destruction changed");

            var prefab = em.CreateEntity();
            em.AddComponent<Prefab>(prefab);
            em.AddComponentData(prefab, new Faction { Id = 2 });
            em.AddComponentData(prefab, new PreviousParent { Value = prefab });
            em.AddBuffer<Child>(prefab).Add(new Child { Value = prefab });
            var instance = em.Instantiate(prefab);
            Need(em.HasComponent<Faction>(instance) && !em.HasComponent<PreviousParent>(instance) &&
                 !em.HasBuffer<Child>(instance), "Instantiation copied cleanup components");
            var linked = em.CreateEntity();
            var linkedChild = em.CreateEntity();
            var linkedGrandchild = em.CreateEntity();
            var group = em.AddBuffer<LinkedEntityGroup>(linked);
            group.Add(new LinkedEntityGroup { Value = linked });
            group.Add(new LinkedEntityGroup { Value = linkedChild });
            var nested = em.AddBuffer<LinkedEntityGroup>(linkedChild);
            nested.Add(new LinkedEntityGroup { Value = linkedChild });
            nested.Add(new LinkedEntityGroup { Value = linkedGrandchild });
            em.AddComponentData(linked, new PreviousParent());
            em.AddComponentData(linkedGrandchild, new PreviousParent());
            em.DestroyEntity(linked);
            Need(em.Exists(linked) && !em.HasBuffer<LinkedEntityGroup>(linked) && !em.Exists(linkedChild) &&
                 em.Exists(linkedGrandchild) && em.HasComponent<PreviousParent>(linkedGrandchild),
                 "Linked nested destruction and cleanup residue changed");
            em.RemoveComponent<PreviousParent>(linked);
            em.RemoveComponent<PreviousParent>(linkedGrandchild);
            Need(!em.Exists(linked) && !em.Exists(linkedGrandchild), "Linked cleanup residues did not retire");
            var arrayRoot = em.CreateEntity();
            var arrayChild = em.CreateEntity();
            var otherRoot = em.CreateEntity();
            var arrayGroup = em.AddBuffer<LinkedEntityGroup>(arrayRoot);
            arrayGroup.Add(new LinkedEntityGroup { Value = arrayRoot });
            arrayGroup.Add(new LinkedEntityGroup { Value = arrayChild });
            em.AddComponentData(arrayChild, new PreviousParent());
            var batch = new NativeArray<Entity>(2, Allocator.Temp);
            batch[0] = arrayRoot; batch[1] = otherRoot;
            em.DestroyEntity(batch); batch.Dispose();
            Need(!em.Exists(arrayRoot) && !em.Exists(otherRoot) && em.Exists(arrayChild) &&
                 em.HasComponent<PreviousParent>(arrayChild), "Array destruction did not expand linked group");
            em.RemoveComponent<PreviousParent>(arrayChild);
            Need(!em.Exists(arrayChild), "Array-linked cleanup residue did not retire");
            Debug.Log("[S073CleanupNativeControls] result=Passed fixtureOnly=1");
        }
    }
}
#endif
