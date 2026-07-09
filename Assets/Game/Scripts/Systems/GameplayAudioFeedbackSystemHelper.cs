using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class GameplayAudioFeedbackSystemHelper
    {
        private static readonly FixedString32Bytes SfxBus = new("SFX");
        internal const float WeaponFireCooldownSeconds = 0.04f;

        public static bool TryEmitVehicleEngineAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayUnitEngineVehicleMove,
                AudioEventIds.GameplayUnitEngineVehicleMoveHash,
                AudioPlaybackPriority.Low,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitAircraftFlightAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayUnitEngineAircraftFlight,
                AudioEventIds.GameplayUnitEngineAircraftFlightHash,
                AudioPlaybackPriority.Low,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitHelicopterFlightAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayUnitEngineHelicopterFlight,
                AudioEventIds.GameplayUnitEngineHelicopterFlightHash,
                AudioPlaybackPriority.Low,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitAircraftTakeoffAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayUnitEngineAircraftTakeoff,
                AudioEventIds.GameplayUnitEngineAircraftTakeoffHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitWeaponFireAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayWeaponFireSmallArms,
                AudioEventIds.GameplayWeaponFireSmallArmsHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                WeaponFireCooldownSeconds,
                source,
                worldPosition);
        }

        public static bool TryEmitMissileLaunchAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayWeaponMissileLaunch,
                AudioEventIds.GameplayWeaponMissileLaunchHash,
                AudioPlaybackPriority.High,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitMissileFlightAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayWeaponMissileFlight,
                AudioEventIds.GameplayWeaponMissileFlightHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        public static bool TryEmitMissileImpactAudio(
            EntityManager em,
            Entity source,
            float requestedAt,
            float3 worldPosition)
        {
            return EnqueueSpatialSfx(
                em,
                AudioEventIds.GameplayWeaponMissileImpact,
                AudioEventIds.GameplayWeaponMissileImpactHash,
                AudioPlaybackPriority.High,
                requestedAt,
                cooldownSeconds: 0f,
                source,
                worldPosition);
        }

        private static bool EnqueueSpatialSfx(
            EntityManager em,
            FixedString64Bytes eventId,
            uint eventHash,
            AudioPlaybackPriority priority,
            float requestedAt,
            float cooldownSeconds,
            Entity source,
            float3 worldPosition)
        {
            AudioEventRequestSystem.EnqueueOneShot(
                em,
                eventId,
                eventHash,
                SfxBus,
                priority,
                requestedAt,
                cooldownSeconds,
                sourceEntity: source,
                spatial: true,
                worldPosition: worldPosition);
            return true;
        }
    }
}
