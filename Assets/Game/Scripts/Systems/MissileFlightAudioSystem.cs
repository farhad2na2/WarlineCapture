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
        private const float MissileFlightIntervalSeconds = 0.45f;
        private EntityQuery _projectileQuery;

        public void OnCreate(ref SystemState state)
        {
            _projectileQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<LocalTransform>()
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<GroundMissileProjectileComponent>(),
                    ComponentType.ReadOnly<AirMissileProjectileComponent>()
                }
            });
            state.RequireForUpdate(_projectileQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float now = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = new(Unity.Collections.Allocator.Temp);

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

            CombatAudioEventUtility.EmitMissileFlight(em, projectile, position, now);
            audioState.NextFlightAudioAt = now + MissileFlightIntervalSeconds;

            if (em.HasComponent<MissileFlightAudioState>(projectile))
                em.SetComponentData(projectile, audioState);
            else
                ecb.AddComponent(projectile, audioState);
        }
    }
}
