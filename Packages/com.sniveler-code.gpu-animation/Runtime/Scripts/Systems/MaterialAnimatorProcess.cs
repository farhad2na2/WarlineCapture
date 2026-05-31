using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using SnivelerCode.GpuAnimation.Scripts.Components;

namespace SnivelerCode.GpuAnimation.Scripts.Systems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MaterialAnimatorProcess : ISystem
    {
        ComponentLookup<MaterialAnimatorBlobData> m_Animators;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_Animators = state.GetComponentLookup<MaterialAnimatorBlobData>(true);
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MaterialAnimationIndex, MaterialAnimatorLink>()
                .WithAllRW<MaterialAnimationData>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_Animators.Update(ref state);
            state.Dependency = new AnimateProcessJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Animators = m_Animators
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct AnimateProcessJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly]
            public ComponentLookup<MaterialAnimatorBlobData> Animators;

            void Execute(in MaterialAnimationIndex animIndex, in MaterialAnimatorLink link, ref MaterialAnimationData data)
            {
                if (!Animators.HasComponent(link.Value)) return;

                var blobReference = Animators[link.Value].Value;
                ref var animator = ref blobReference.Value;
                if (animator.Animations.Length == 0 || animator.BoneCount <= 0) return;

                data.Time += DeltaTime;

                byte currentIndex = (byte)(data.AnimationIndex % animator.Animations.Length);
                byte targetIndex = (byte)(animIndex.Value % animator.Animations.Length);
                ref var currentAnimation = ref animator.Animations[currentIndex];
                float3 renderConfig = ResolveRenderConfig(ref currentAnimation, animator.BoneCount, data.Time);

                if (targetIndex != currentIndex)
                {
                    const float transitionDuration = 0.5f;
                    if (data.TransitionIndex != targetIndex)
                        data.TransitionTime = 0f;
                    data.TransitionIndex = targetIndex;
                    data.TransitionTime = math.min(data.TransitionTime + DeltaTime, transitionDuration);

                    if (data.TransitionTime < transitionDuration)
                    {
                        ref var targetAnimation = ref animator.Animations[targetIndex];
                        float3 targetConfig = ResolveRenderConfig(ref targetAnimation, animator.BoneCount, data.Time);
                        renderConfig = new float3(renderConfig.x, targetConfig.x, data.TransitionTime / transitionDuration);
                    }
                    else
                    {
                        data.AnimationIndex = targetIndex;
                        data.TransitionIndex = targetIndex;
                        data.TransitionTime = 0f;

                        ref var targetAnimation = ref animator.Animations[targetIndex];
                        renderConfig = ResolveRenderConfig(ref targetAnimation, animator.BoneCount, data.Time);
                    }
                }
                else
                {
                    data.TransitionIndex = currentIndex;
                    data.TransitionTime = 0f;
                }

                data.RenderConfig = renderConfig;
            }

            private static float3 ResolveRenderConfig(ref MaterialAnimationBlobAsset animation, int boneCount, float time)
            {
                int frames = math.max(1, animation.Frames);
                float frameRate = math.max(0.001f, animation.Fps * animation.Speed);
                float floatFrame = math.fmod(time * frameRate, frames);
                if (floatFrame < 0f)
                    floatFrame += frames;

                int frame = (int)math.floor(floatFrame);
                int nextFrame = (frame + 1) % frames;
                float clampValue = floatFrame - frame;

                int finalFrame = animation.Start + frame * boneCount;
                int finalNextFrame = animation.Start + nextFrame * boneCount;
                return new float3(finalFrame, finalNextFrame, clampValue);
            }
        }
    }
}
