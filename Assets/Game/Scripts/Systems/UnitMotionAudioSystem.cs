using Game.Components;
using Unity.Collections;
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
        private const float FixedWingAircraftFlightIntervalSeconds = 2.6f;
        private const float HelicopterFlightIntervalSeconds = 0.8f;
        private const double MotionAudioUpdateIntervalSeconds = 0.2d;
        private double _nextMotionAudioUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitMoveVisualComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (GameplayRuntimeUpdateDebugFlags.DisableUnitMotionAudioRuntime)
                return;

            double elapsedTime = SystemAPI.Time.ElapsedTime;
            if (elapsedTime < _nextMotionAudioUpdateTime)
                return;

            _nextMotionAudioUpdateTime = elapsedTime + MotionAudioUpdateIntervalSeconds;

            EntityManager em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
            EntityCommandBuffer ecb = default;
            bool hasEntityCommandBuffer = false;

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
                            audioState.NextAircraftEngineAt = now + HelicopterFlightIntervalSeconds;
                        }
                        else
                        {
                            GameplayAudioFeedbackSystemHelper.TryEmitAircraftFlightAudio(
                                em,
                                entity,
                                now,
                                transform.ValueRO.Position);
                            audioState.NextAircraftEngineAt = now + FixedWingAircraftFlightIntervalSeconds;
                        }

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
                {
                    em.SetComponentData(entity, audioState);
                }
                else
                {
                    if (!hasEntityCommandBuffer)
                    {
                        ecb = new EntityCommandBuffer(Allocator.Temp);
                        hasEntityCommandBuffer = true;
                    }

                    ecb.AddComponent(entity, audioState);
                }
            }

            if (hasEntityCommandBuffer)
            {
                ecb.Playback(em);
                ecb.Dispose();
            }
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

            FixedString64Bytes sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
            return ContainsIgnoreCase(sourceKey, "Helicopter") || ContainsIgnoreCase(sourceKey, "Heli");
        }

        private static bool ContainsIgnoreCase(FixedString64Bytes value, string needle)
        {
            int valueLength = value.Length;
            int needleLength = needle.Length;
            if (needleLength == 0 || valueLength < needleLength)
                return false;

            for (int start = 0; start <= valueLength - needleLength; start++)
            {
                bool matches = true;
                for (int offset = 0; offset < needleLength; offset++)
                {
                    if (ToLowerAscii(value[start + offset]) == ToLowerAscii((byte)needle[offset]))
                        continue;

                    matches = false;
                    break;
                }

                if (matches)
                    return true;
            }

            return false;
        }

        private static byte ToLowerAscii(byte value)
        {
            return value is >= (byte)'A' and <= (byte)'Z'
                ? (byte)(value + ((byte)'a' - (byte)'A'))
                : value;
        }
    }
}
