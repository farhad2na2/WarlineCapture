using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitMoveVisualStateSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateAfter(typeof(AudioEventRequestSystem))]
    public partial struct UnitMotionAudioSystem : ISystem
    {
        private const float VehicleEngineIntervalSeconds = 0.55f;
        private const float AircraftFlightIntervalSeconds = 0.65f;
        private const float AircraftTakeoffIntervalSeconds = 1.1f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitMoveVisualComponent>();
            state.RequireForUpdate<UnitMovementBehavior>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float now = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transform, visual, movement, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<UnitMoveVisualComponent>, RefRO<UnitMovementBehavior>>()
                         .WithNone<StaticGridBlocker>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                    continue;

                bool isAircraft = em.HasComponent<UnitAirMovement>(entity);
                bool isVehicle = movement.ValueRO.UsesVehicleMotion != 0;
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
                    bool takeoffRolling = air.TakeoffRolling != 0;
                    bool aircraftActive =
                        visual.ValueRO.IsMoving != 0 ||
                        air.Airborne != 0 ||
                        air.TakeoffRolling != 0 ||
                        air.LandingRolling != 0 ||
                        air.AttackRunActive != 0 ||
                        air.ReturningHome != 0;

                    if (takeoffRolling && (audioState.WasTakeoffRolling == 0 || now >= audioState.NextAircraftTakeoffAt))
                    {
                        GameplayAudioFeedbackSystemHelper.TryEmitAircraftTakeoffAudio(em, entity, now, transform.ValueRO.Position);
                        audioState.NextAircraftTakeoffAt = now + AircraftTakeoffIntervalSeconds;
                        changed = true;
                    }

                    if (aircraftActive && now >= audioState.NextAircraftFlightAt)
                    {
                        GameplayAudioFeedbackSystemHelper.TryEmitAircraftFlightAudio(em, entity, now, transform.ValueRO.Position);
                        audioState.NextAircraftFlightAt = now + AircraftFlightIntervalSeconds;
                        changed = true;
                    }

                    audioState.WasAircraftActive = (byte)(aircraftActive ? 1 : 0);
                    audioState.WasTakeoffRolling = (byte)(takeoffRolling ? 1 : 0);
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
    }
}
