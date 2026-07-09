using Game.Components;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateAfter(typeof(GroundMissileProjectileFlightSystem))]
    [UpdateAfter(typeof(AirMissileHomingProjectileSystem))]
    [UpdateBefore(typeof(GroundMissileImpactSystem))]
    [UpdateBefore(typeof(AirMissileImpactSystem))]
    public partial struct MissileFlightAudioSystem : ISystem
    {
        private const float MissileFlightIntervalSeconds = 0.5f;

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float now = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<GroundMissileProjectileComponent>()
                         .WithEntityAccess())
            {
                EmitMissileFlightAudio(em, ecb, entity, transform.ValueRO.Position, now);
            }

            foreach (var (transform, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>>()
                         .WithAll<AirMissileProjectileComponent>()
                         .WithEntityAccess())
            {
                EmitMissileFlightAudio(em, ecb, entity, transform.ValueRO.Position, now);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static void EmitMissileFlightAudio(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity projectile,
            Unity.Mathematics.float3 position,
            float now)
        {
            MissileFlightAudioState audioState = em.HasComponent<MissileFlightAudioState>(projectile)
                ? em.GetComponentData<MissileFlightAudioState>(projectile)
                : default;

            if (now < audioState.NextFlightAudioAt)
                return;

            GameplayAudioFeedbackSystemHelper.TryEmitMissileFlightAudio(em, projectile, now, position);
            audioState.NextFlightAudioAt = now + MissileFlightIntervalSeconds;

            if (em.HasComponent<MissileFlightAudioState>(projectile))
                em.SetComponentData(projectile, audioState);
            else
                ecb.AddComponent(projectile, audioState);
        }
    }
}
