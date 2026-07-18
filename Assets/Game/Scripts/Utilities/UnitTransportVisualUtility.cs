using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    public static class UnitTransportVisualUtility
    {
        public static void SetPassengerVisible(EntityManager em, Entity entity, bool visible)
        {
            if (!em.Exists(entity))
                return;

            if (visible)
                RestoreStoredVisuals(em, entity);
            else
                HideVisualTree(em, entity, default, false);

            ApplyKnownVisualReferences(em, entity, visible);

            if (em.HasComponent<UnitMoveVisualComponent>(entity))
                em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        }

        public static void SetPassengerHidden(EntityManager em, Entity entity, EntityCommandBuffer ecb)
        {
            if (!em.Exists(entity))
                return;

            HideVisualTree(em, entity, ecb, true);

            if (em.HasComponent<UnitMoveVisualComponent>(entity))
                em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        }

        private static void HideVisualTree(EntityManager em, Entity entity, EntityCommandBuffer ecb, bool useCommandBuffer)
        {
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(entity))
                return;

            DynamicBuffer<UnitTransportHiddenVisualScale> hiddenScales = em.GetBuffer<UnitTransportHiddenVisualScale>(entity);
            hiddenScales.Clear();

            using var visualEntities = new NativeList<Entity>(16, Allocator.Temp);
            CollectVisualTree(em, entity, visualEntities);

            if (em.HasComponent<UnitDestroyedVisualReference>(entity))
            {
                UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
                CollectVisualTree(em, visualRef.AliveVisual, visualEntities);
                CollectVisualTree(em, visualRef.DestroyedVisual, visualEntities);
            }

            if (em.HasComponent<UnitModelInstanceReference>(entity))
            {
                UnitModelInstanceReference model = em.GetComponentData<UnitModelInstanceReference>(entity);
                CollectVisualTree(em, model.Instance, visualEntities);
            }
            for (int i = 0; i < visualEntities.Length; i++)
            {
                Entity visual = visualEntities[i];
                if (!em.Exists(visual))
                    continue;

                bool wasDisabled = em.HasComponent<Disabled>(visual);
                float previousScale = 1f;
                if (em.HasComponent<LocalTransform>(visual))
                {
                    LocalTransform transform = em.GetComponentData<LocalTransform>(visual);
                    previousScale = transform.Scale;
                    if (transform.Scale != 0f)
                    {
                        transform.Scale = 0f;
                        em.SetComponentData(visual, transform);
                    }
                }

                hiddenScales.Add(new UnitTransportHiddenVisualScale
                {
                    Visual = visual,
                    PreviousScale = previousScale,
                    WasDisabled = (byte)(wasDisabled ? 1 : 0)
                });

                if (visual != entity && !wasDisabled)
                {
                    if (useCommandBuffer)
                        ecb.AddComponent<Disabled>(visual);
                    else
                        em.AddComponent<Disabled>(visual);
                }
            }
        }

        private static void RestoreStoredVisuals(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(entity))
                return;

            DynamicBuffer<UnitTransportHiddenVisualScale> hiddenScales = em.GetBuffer<UnitTransportHiddenVisualScale>(entity);
            using NativeArray<UnitTransportHiddenVisualScale> restoreEntries = hiddenScales.ToNativeArray(Allocator.Temp);
            hiddenScales.Clear();

            for (int i = 0; i < restoreEntries.Length; i++)
            {
                UnitTransportHiddenVisualScale hidden = restoreEntries[i];
                if (!em.Exists(hidden.Visual))
                    continue;

                if (hidden.WasDisabled == 0 && em.HasComponent<Disabled>(hidden.Visual))
                    em.RemoveComponent<Disabled>(hidden.Visual);

                if (em.HasComponent<LocalTransform>(hidden.Visual))
                {
                    LocalTransform transform = em.GetComponentData<LocalTransform>(hidden.Visual);
                    transform.Scale = hidden.PreviousScale;
                    em.SetComponentData(hidden.Visual, transform);
                }
            }

        }

        private static void ApplyKnownVisualReferences(EntityManager em, Entity entity, bool visible)
        {
            if (em.HasComponent<UnitDestroyedVisualReference>(entity))
            {
                UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
                UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.AliveVisual, visible, visualRef.AliveVisibleScale);
                UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.DestroyedVisual, false, visualRef.DestroyedVisibleScale);
                return;
            }

            if (em.HasComponent<UnitModelInstanceReference>(entity))
            {
                UnitModelInstanceReference model = em.GetComponentData<UnitModelInstanceReference>(entity);
                UnitDestroyedVisualSystem.SetChildVisible(em, model.Instance, visible);
            }
        }

        private static void CollectVisualTree(EntityManager em, Entity root, NativeList<Entity> results)
        {
            if (root == Entity.Null || !em.Exists(root) || Contains(results, root))
                return;

            results.Add(root);
            if (!em.HasBuffer<Child>(root))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
            for (int i = 0; i < children.Length; i++)
                CollectVisualTree(em, children[i].Value, results);
        }

        private static bool Contains(NativeList<Entity> entities, Entity entity)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == entity)
                    return true;
            }

            return false;
        }
    }
}
