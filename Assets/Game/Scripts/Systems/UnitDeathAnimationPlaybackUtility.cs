using Game.Components;
using Game.Configs;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    internal static class UnitDeathAnimationPlaybackUtility
    {
        private const float MinimumPlaybackSeconds = 0.01f;

        internal static float Prepare(
            EntityManager entityManager,
            Entity unit,
            float configuredDuration)
        {
            float playbackDuration = math.max(MinimumPlaybackSeconds, configuredDuration);
            if (!TryResolveDeathAnimationIndex(entityManager, unit, out byte deathAnimationIndex))
                return playbackDuration;

            using NativeList<Entity> visualEntities = new(256, Allocator.Temp);
            using NativeHashSet<Entity> visited = new(256, Allocator.Temp);
            CollectUnitVisualEntities(entityManager, unit, visualEntities, visited);

            for (int index = 0; index < visualEntities.Length; index++)
            {
                Entity visual = visualEntities[index];
                if (entityManager.HasComponent<MaterialAnimationIndex>(visual))
                {
                    entityManager.SetComponentData(
                        visual,
                        new MaterialAnimationIndex { Value = deathAnimationIndex });
                }

                if (entityManager.HasComponent<MaterialAnimationData>(visual))
                {
                    MaterialAnimationData animationData =
                        entityManager.GetComponentData<MaterialAnimationData>(visual);
                    entityManager.SetComponentData(
                        visual,
                        ResetPlaybackClock(in animationData, deathAnimationIndex));
                }

                if (!entityManager.HasComponent<MaterialAnimatorLink>(visual))
                    continue;

                Entity animatorEntity = entityManager.GetComponentData<MaterialAnimatorLink>(visual).Value;
                if (animatorEntity == Entity.Null || !entityManager.Exists(animatorEntity) ||
                    !entityManager.HasComponent<MaterialAnimatorBlobData>(animatorEntity))
                {
                    continue;
                }

                BlobAssetReference<MaterialAnimatorBlobAsset> animatorReference =
                    entityManager.GetComponentData<MaterialAnimatorBlobData>(animatorEntity).Value;
                if (!animatorReference.IsCreated || animatorReference.Value.Animations.Length == 0)
                    continue;

                ref MaterialAnimatorBlobAsset animator = ref animatorReference.Value;
                int targetIndex = deathAnimationIndex % animator.Animations.Length;
                ref MaterialAnimationBlobAsset animation = ref animator.Animations[targetIndex];
                playbackDuration = math.min(
                    playbackDuration,
                    ResolveSinglePlaybackDuration(ref animation, playbackDuration));
            }

            return math.max(MinimumPlaybackSeconds, playbackDuration);
        }

        internal static MaterialAnimationData ResetPlaybackClock(
            in MaterialAnimationData current,
            byte deathAnimationIndex)
        {
            MaterialAnimationData reset = current;
            reset.AnimationIndex = deathAnimationIndex;
            reset.Time = 0f;
            reset.TransitionIndex = deathAnimationIndex;
            reset.TransitionTime = 0f;
            return reset;
        }

        internal static float ResolveSinglePlaybackDuration(
            ref MaterialAnimationBlobAsset animation,
            float fallbackDuration)
        {
            float safeFallback = math.max(MinimumPlaybackSeconds, fallbackDuration);
            if (animation.Frames <= 1 || animation.Fps == 0 || animation.Speed == 0)
                return safeFallback;

            float frameRate = animation.Fps * animation.Speed;
            float finalFrameTime = (animation.Frames - 1) / frameRate;
            return math.isfinite(finalFrameTime) && finalFrameTime > 0f
                ? math.max(MinimumPlaybackSeconds, finalFrameTime)
                : safeFallback;
        }

        private static bool TryResolveDeathAnimationIndex(
            EntityManager entityManager,
            Entity unit,
            out byte animationIndex)
        {
            animationIndex = 0;
            if (!entityManager.HasBuffer<UnitAnimationOrderEntry>(unit))
                return false;

            DynamicBuffer<UnitAnimationOrderEntry> animationOrder =
                entityManager.GetBuffer<UnitAnimationOrderEntry>(unit);
            if (TryResolveKind(animationOrder, UnitAnimationKind.Death01, out animationIndex) ||
                TryResolveKind(animationOrder, UnitAnimationKind.Death02, out animationIndex) ||
                TryResolveKind(animationOrder, UnitAnimationKind.Death03, out animationIndex))
            {
                return true;
            }

            return false;
        }

        private static bool TryResolveKind(
            DynamicBuffer<UnitAnimationOrderEntry> animationOrder,
            UnitAnimationKind kind,
            out byte animationIndex)
        {
            byte encodedKind = (byte)kind;
            for (int index = 0; index < animationOrder.Length; index++)
            {
                if (animationOrder[index].Kind != encodedKind)
                    continue;

                animationIndex = (byte)(encodedKind + 1);
                return true;
            }

            animationIndex = 0;
            return false;
        }

        private static void CollectUnitVisualEntities(
            EntityManager entityManager,
            Entity unit,
            NativeList<Entity> visualEntities,
            NativeHashSet<Entity> visited)
        {
            CollectVisualEntities(entityManager, unit, visualEntities, visited);
            if (entityManager.HasComponent<UnitDetailedVisualReference>(unit))
            {
                CollectVisualEntities(
                    entityManager,
                    entityManager.GetComponentData<UnitDetailedVisualReference>(unit).Root,
                    visualEntities,
                    visited);
            }
            if (entityManager.HasComponent<UnitModelInstanceReference>(unit))
            {
                CollectVisualEntities(
                    entityManager,
                    entityManager.GetComponentData<UnitModelInstanceReference>(unit).Instance,
                    visualEntities,
                    visited);
            }
            if (entityManager.HasComponent<UnitMidLodInstanceReference>(unit))
            {
                CollectVisualEntities(
                    entityManager,
                    entityManager.GetComponentData<UnitMidLodInstanceReference>(unit).Instance,
                    visualEntities,
                    visited);
            }
            if (entityManager.HasComponent<UnitLowLodInstanceReference>(unit))
            {
                CollectVisualEntities(
                    entityManager,
                    entityManager.GetComponentData<UnitLowLodInstanceReference>(unit).Instance,
                    visualEntities,
                    visited);
            }
        }

        private static void CollectVisualEntities(
            EntityManager entityManager,
            Entity entity,
            NativeList<Entity> visualEntities,
            NativeHashSet<Entity> visited)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity) || !visited.Add(entity))
                return;

            visualEntities.Add(entity);
            if (entityManager.HasBuffer<LinkedEntityGroup>(entity))
            {
                DynamicBuffer<LinkedEntityGroup> linkedEntities =
                    entityManager.GetBuffer<LinkedEntityGroup>(entity);
                for (int index = 0; index < linkedEntities.Length; index++)
                {
                    CollectVisualEntities(
                        entityManager,
                        linkedEntities[index].Value,
                        visualEntities,
                        visited);
                }
            }

            if (!entityManager.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = entityManager.GetBuffer<Child>(entity);
            for (int index = 0; index < children.Length; index++)
                CollectVisualEntities(entityManager, children[index].Value, visualEntities, visited);
        }
    }
}
