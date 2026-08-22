using Game.Components;
using SnivelerCode.GpuAnimation.Scripts.Components;
using SnivelerCode.GpuAnimation.Scripts.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MaterialAnimatorProcess))]
    [UpdateBefore(typeof(MaterialAnimateChildProcess))]
    public partial struct UnitDeathPoseFreezeSystem : ISystem
    {
        private ComponentLookup<MaterialAnimatorBlobData> _animatorLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _animatorLookup = state.GetComponentLookup<MaterialAnimatorBlobData>(true);
            state.RequireForUpdate<UnitDeathPoseFreezeTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _animatorLookup.Update(ref state);
            state.Dependency = new FreezeFinalDeathPoseJob
            {
                AnimatorLookup = _animatorLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(UnitDeathPoseFreezeTag))]
        private partial struct FreezeFinalDeathPoseJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<MaterialAnimatorBlobData> AnimatorLookup;

            private void Execute(
                in MaterialAnimationIndex animationIndex,
                in MaterialAnimatorLink animatorLink,
                ref MaterialAnimationData animationData)
            {
                if (!AnimatorLookup.HasComponent(animatorLink.Value))
                    return;

                BlobAssetReference<MaterialAnimatorBlobAsset> blobReference =
                    AnimatorLookup[animatorLink.Value].Value;
                if (!blobReference.IsCreated)
                    return;

                ref MaterialAnimatorBlobAsset animator = ref blobReference.Value;
                if (animator.Animations.Length == 0 || animator.BoneCount <= 0)
                    return;

                byte targetIndex = (byte)(animationIndex.Value % animator.Animations.Length);
                ref MaterialAnimationBlobAsset animation = ref animator.Animations[targetIndex];
                animationData.AnimationIndex = targetIndex;
                animationData.TransitionIndex = targetIndex;
                animationData.TransitionTime = 0f;
                animationData.RenderConfig = ResolveFinalRenderConfig(
                    animation.Start,
                    animation.Frames,
                    animator.BoneCount);
            }
        }

        internal static float3 ResolveFinalRenderConfig(
            int animationStart,
            int animationFrameCount,
            int boneCount)
        {
            int finalFrame = animationStart + (math.max(1, animationFrameCount) - 1) * math.max(1, boneCount);
            return new float3(finalFrame, finalFrame, 0f);
        }
    }
}
