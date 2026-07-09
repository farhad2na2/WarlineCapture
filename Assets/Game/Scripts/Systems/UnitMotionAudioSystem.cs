using Game.Components;
using System;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitMoveVisualStateSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateAfter(typeof(AudioEventRequestSystem))]
    public partial struct UnitMotionAudioSystem : ISystem
    {
        private const float VehicleEngineIntervalSeconds = 0.65f;
        private const float AircraftEngineIntervalSeconds = 0.9f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitMoveVisualComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float now = (float)SystemAPI.Time.ElapsedTime;
            EntityCommandBuffer ecb = new(Unity.Collections.Allocator.Temp);

            foreach (var (transform, visual, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<UnitMoveVisualComponent>>()
                         .WithNone<StaticGridBlocker>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                    continue;

                bool isAircraft = em.HasComponent<UnitAirMovement>(entity);
                bool isVehicle = IsGroundVehicle(em, entity);
                if (!isAircraft && !isVehicle)
                    continue;

                UnitMotionAudioState audioState = em.HasComponent<UnitMotionAudioState>(entity)
                    ? em.GetComponentData<UnitMotionAudioState>(entity)
                    : default;

                bool changed = false;
                if (isAircraft)
                {
                    UnitAirComponent air = em.HasComponent<UnitAirComponent>(entity)
                        ? em.GetComponentData<UnitAirComponent>(entity)
                        : default;
                    bool aircraftActive =
                        visual.ValueRO.IsMoving != 0 ||
                        air.Airborne != 0 ||
                        air.TakeoffRolling != 0 ||
                        air.LandingRolling != 0 ||
                        air.AttackRunActive != 0;

                    if (aircraftActive && air.TakeoffRolling != 0 && audioState.WasAircraftActive == 0)
                    {
                        GameplayAudioFeedbackSystemHelper.TryEmitAircraftTakeoffAudio(
                            em,
                            entity,
                            now,
                            transform.ValueRO.Position);
                        changed = true;
                    }

                    if (aircraftActive && now >= audioState.NextAircraftEngineAt)
                    {
                        if (IsHelicopter(em, entity))
                        {
                            GameplayAudioFeedbackSystemHelper.TryEmitHelicopterFlightAudio(
                                em,
                                entity,
                                now,
                                transform.ValueRO.Position);
                        }
                        else
                        {
                            GameplayAudioFeedbackSystemHelper.TryEmitAircraftFlightAudio(
                                em,
                                entity,
                                now,
                                transform.ValueRO.Position);
                        }

                        audioState.NextAircraftEngineAt = now + AircraftEngineIntervalSeconds;
                        changed = true;
                    }

                    audioState.WasAircraftActive = (byte)(aircraftActive ? 1 : 0);
                    changed = true;
                }
                else if (visual.ValueRO.IsMoving != 0 && now >= audioState.NextVehicleEngineAt)
                {
                    GameplayAudioFeedbackSystemHelper.TryEmitVehicleEngineAudio(em, entity, now, transform.ValueRO.Position);
                    audioState.NextVehicleEngineAt = now + VehicleEngineIntervalSeconds;
                    changed = true;
                }

                if (!changed)
                    continue;

                if (em.HasComponent<UnitMotionAudioState>(entity))
                    em.SetComponentData(entity, audioState);
                else
                    ecb.AddComponent(entity, audioState);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static bool IsGroundVehicle(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitVehicleMovement>(entity))
                return true;

            return em.HasComponent<UnitMovementBehavior>(entity) &&
                   em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0;
        }

        private static bool IsHelicopter(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<UnitSourcePrefabKey>(entity))
                return false;

            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            return sourceKey.IndexOf("helicopter", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
