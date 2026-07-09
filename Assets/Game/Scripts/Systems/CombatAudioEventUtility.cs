using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class CombatAudioEventUtility
    {
        private static readonly FixedString32Bytes SfxBus = new("SFX");

        public static bool EmitStandardWeaponFire(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            if (source == Entity.Null || !em.Exists(source))
                return false;

            if (IsAircraft(em, source))
            {
                return EmitSpatialOneShot(
                    em,
                    AudioEventIds.GameplayWeaponMissileLaunch,
                    AudioEventIds.GameplayWeaponMissileLaunchHash,
                    AudioPlaybackPriority.High,
                    requestedAt,
                    0.18f,
                    source,
                    position);
            }

            if (IsVehicle(em, source))
            {
                return EmitSpatialOneShot(
                    em,
                    AudioEventIds.GameplayWeaponVehicleCannonFire,
                    AudioEventIds.GameplayWeaponVehicleCannonFireHash,
                    AudioPlaybackPriority.High,
                    requestedAt,
                    0.16f,
                    source,
                    position);
            }

            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayWeaponRifleFire,
                AudioEventIds.GameplayWeaponRifleFireHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                0.055f,
                source,
                position);
        }

        public static bool EmitGroundMissileLaunch(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayWeaponMissileLaunch,
                AudioEventIds.GameplayWeaponMissileLaunchHash,
                AudioPlaybackPriority.High,
                requestedAt,
                0.18f,
                source,
                position);
        }

        public static bool EmitAirMissileLaunch(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayWeaponAirMissileLaunch,
                AudioEventIds.GameplayWeaponAirMissileLaunchHash,
                AudioPlaybackPriority.High,
                requestedAt,
                0.18f,
                source,
                position);
        }

        public static bool EmitVehicleEngine(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayUnitVehicleEngine,
                AudioEventIds.GameplayUnitVehicleEngineHash,
                AudioPlaybackPriority.Low,
                requestedAt,
                0.25f,
                source,
                position);
        }

        public static bool EmitAircraftEngine(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayUnitEngineAircraftFlight,
                AudioEventIds.GameplayUnitEngineAircraftFlightHash,
                AudioPlaybackPriority.Low,
                requestedAt,
                2.6f,
                source,
                position);
        }

        public static bool EmitMissileFlight(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayWeaponMissileFlight,
                AudioEventIds.GameplayWeaponMissileFlightHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                0.18f,
                source,
                position);
        }

        public static bool EmitBulletImpact(EntityManager em, Entity target, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayImpactBullet,
                AudioEventIds.GameplayImpactBulletHash,
                AudioPlaybackPriority.Medium,
                requestedAt,
                0.045f,
                target,
                position);
        }

        public static bool EmitSmallExplosion(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayExplosionSmall,
                AudioEventIds.GameplayExplosionSmallHash,
                AudioPlaybackPriority.High,
                requestedAt,
                0.12f,
                source,
                position);
        }

        public static bool EmitLargeExplosion(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            return EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayExplosionLarge,
                AudioEventIds.GameplayExplosionLargeHash,
                AudioPlaybackPriority.Critical,
                requestedAt,
                0.18f,
                source,
                position);
        }

        public static bool EmitVehicleDestroyed(EntityManager em, Entity source, float3 position, float requestedAt)
        {
            bool emitted = EmitSpatialOneShot(
                em,
                AudioEventIds.GameplayUnitVehicleDestroyed,
                AudioEventIds.GameplayUnitVehicleDestroyedHash,
                AudioPlaybackPriority.High,
                requestedAt,
                0.4f,
                source,
                position);
            EmitLargeExplosion(em, source, position, requestedAt);
            return emitted;
        }

        private static bool EmitSpatialOneShot(
            EntityManager em,
            string eventId,
            uint eventHash,
            AudioPlaybackPriority priority,
            float requestedAt,
            float cooldownSeconds,
            Entity source,
            float3 position)
        {
            AudioEventRequestSystem.EnqueueOneShot(
                em,
                new FixedString64Bytes(eventId),
                eventHash,
                SfxBus,
                priority,
                requestedAt,
                cooldownSeconds,
                sourceEntity: source,
                spatial: true,
                worldPosition: position);
            return true;
        }

        private static bool IsAircraft(EntityManager em, Entity entity)
        {
            return em.HasComponent<UnitAirComponent>(entity) ||
                   em.HasComponent<UnitAirMovement>(entity) ||
                   HasSourceKeyToken(em, entity, "plane") ||
                   HasSourceKeyToken(em, entity, "jet") ||
                   HasSourceKeyToken(em, entity, "helicopter");
        }

        private static bool IsVehicle(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitVehicleMovement>(entity))
                return true;

            if (em.HasComponent<UnitFootprint>(entity))
            {
                int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
                if (math.max(size.x, size.y) > 1)
                    return true;
            }

            return HasSourceKeyToken(em, entity, "veh") ||
                   HasSourceKeyToken(em, entity, "vehicle") ||
                   HasSourceKeyToken(em, entity, "tank") ||
                   HasSourceKeyToken(em, entity, "apc") ||
                   HasSourceKeyToken(em, entity, "truck");
        }

        private static bool HasSourceKeyToken(EntityManager em, Entity entity, string token)
        {
            return em.HasComponent<UnitSourcePrefabKey>(entity) &&
                   em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                       .IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
