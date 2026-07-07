using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public static class TacticalFollowAttackCinematicVfxSystemHelper
    {
        public static void PlayLaunch(in TacticalFollowAttackCinematicStateComponent cinematic)
        {
            GameObject prefab = cinematic.LaunchVfxPrefab.Value;
            if (prefab == null)
                return;

            UnitAttackImpactVfxView.Play(
                prefab,
                (Vector3)cinematic.LaunchPosition,
                ToUnityQuaternion(cinematic.LaunchVfxRotation));
        }

        public static void SyncProjectile(
            Entity cinematicEntity,
            in TacticalFollowAttackCinematicStateComponent cinematic)
        {
            MissileTrailVfxView.Sync(
                cinematicEntity,
                cinematic.ProjectilePosition,
                math.normalizesafe(cinematic.ProjectileDirection, cinematic.AttackDirection));
        }

        public static void PlayImpact(in TacticalFollowAttackCinematicStateComponent cinematic)
        {
            GameObject prefab = cinematic.ImpactVfxPrefab.Value;
            if (prefab == null)
                return;

            UnitAttackImpactVfxView.Play(
                prefab,
                (Vector3)cinematic.ImpactPosition,
                ToUnityQuaternion(cinematic.ImpactVfxRotation));
        }

        public static void ReleaseProjectile(Entity cinematicEntity)
        {
            MissileTrailVfxView.Release(cinematicEntity);
        }

        private static Quaternion ToUnityQuaternion(quaternion rotation) =>
            new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}
