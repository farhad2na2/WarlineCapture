using UnityEngine;
using Game.Components;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Config/Air Missile Launcher")]
    public sealed class AirMissileLauncherConfig : ScriptableObject
    {
        [Header("Detection")]
        [SerializeField, Min(0f)] private float minRange = 8f;
        [SerializeField, Min(0f)] private float baseDetectionRange = 220f;
        [SerializeField, Min(0f)] private float maxDetectionRange = 420f;
        [SerializeField, Min(0f)] private float airTargetPriority = 25f;
        [SerializeField, Min(0f)] private float incomingMissilePriority = 100f;

        [Header("Launcher")]
        [SerializeField, Min(1f)] private float turretYawSpeedDegreesPerSecond = 280f;
        [SerializeField, Min(0.1f)] private float aimToleranceDegrees = 7.5f;
        [SerializeField, Min(0.01f)] private float lockSeconds = 0.35f;
        [SerializeField, Min(0f)] private float launchDelaySeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float reloadSeconds = 1.8f;

        [Header("Missile")]
        [SerializeField, Min(0.01f)] private float missileSpeed = 95f;
        [SerializeField, Min(0f)] private float missileAcceleration = 35f;
        [SerializeField, Min(1f)] private float missileTurnRateDegreesPerSecond = 220f;
        [SerializeField, Min(0.1f)] private float missileLifetimeSeconds = 7f;
        [SerializeField, Min(0.1f)] private float proximityFuseRadius = 4f;
        [SerializeField, Min(0)] private int airTargetDamage = 120;
        [SerializeField, Min(0)] private int incomingMissileDamage = 9999;
        [SerializeField, Range(0f, 1f)] private float trackingQuality = 0.75f;

        [Header("Support")]
        [SerializeField, Min(0f)] private float radarSupportRangeBonus = AirDefenseSupportTuning.RadarRangeBonus;
        [SerializeField, Range(0.1f, 1f)] private float radarLockTimeMultiplier = AirDefenseSupportTuning.RadarLockTimeMultiplier;
        [SerializeField, Range(0f, 1f)] private float radarTrackingBonus = AirDefenseSupportTuning.RadarTrackingBonus;
        [SerializeField, Min(0f)] private float radarTurnRateBonus = AirDefenseSupportTuning.RadarTurnRateBonus;
        [SerializeField, Min(0f)] private float satelliteSupportRangeBonus = AirDefenseSupportTuning.SatelliteRangeBonus;
        [SerializeField, Range(0.1f, 1f)] private float satelliteLockTimeMultiplier = AirDefenseSupportTuning.SatelliteLockTimeMultiplier;
        [SerializeField, Range(0f, 1f)] private float satelliteTrackingBonus = AirDefenseSupportTuning.SatelliteTrackingBonus;
        [SerializeField, Min(0f)] private float satelliteTurnRateBonus = AirDefenseSupportTuning.SatelliteTurnRateBonus;
        [SerializeField, Min(0f)] private float maxSupportRangeBonus = 180f;
        [SerializeField, Range(0f, 1f)] private float maxSupportTrackingBonus = 0.3f;

        [Header("VFX")]
        [SerializeField] private GameObject missileVisualPrefab;
        [SerializeField] private GameObject launchFlashPrefab;
        [SerializeField] private GameObject launchSmokePrefab;
        [SerializeField] private GameObject missileTrailPrefab;
        [SerializeField] private GameObject airburstExplosionPrefab;
        [SerializeField] private GameObject airTargetImpactPrefab;
        [SerializeField] private GameObject interceptExplosionPrefab;

        public float MinRange => Mathf.Max(0f, minRange);
        public float BaseDetectionRange => Mathf.Max(MinRange, baseDetectionRange);
        public float MaxDetectionRange => Mathf.Max(BaseDetectionRange, maxDetectionRange);
        public float AirTargetPriority => Mathf.Max(0f, airTargetPriority);
        public float IncomingMissilePriority => Mathf.Max(0f, incomingMissilePriority);
        public float TurretYawSpeedDegreesPerSecond => Mathf.Max(1f, turretYawSpeedDegreesPerSecond);
        public float AimToleranceDegrees => Mathf.Max(0.1f, aimToleranceDegrees);
        public float LockSeconds => Mathf.Max(0.01f, lockSeconds);
        public float LaunchDelaySeconds => Mathf.Max(0f, launchDelaySeconds);
        public float ReloadSeconds => Mathf.Max(0.01f, reloadSeconds);
        public float MissileSpeed => Mathf.Max(0.01f, missileSpeed);
        public float MissileAcceleration => Mathf.Max(0f, missileAcceleration);
        public float MissileTurnRateDegreesPerSecond => Mathf.Max(1f, missileTurnRateDegreesPerSecond);
        public float MissileLifetimeSeconds => Mathf.Max(0.1f, missileLifetimeSeconds);
        public float ProximityFuseRadius => Mathf.Max(0.1f, proximityFuseRadius);
        public int AirTargetDamage => Mathf.Max(0, airTargetDamage);
        public int IncomingMissileDamage => Mathf.Max(0, incomingMissileDamage);
        public float TrackingQuality => Mathf.Clamp01(trackingQuality);
        public float RadarSupportRangeBonus => Mathf.Max(0f, radarSupportRangeBonus);
        public float RadarLockTimeMultiplier => Mathf.Clamp(radarLockTimeMultiplier, 0.1f, 1f);
        public float RadarTrackingBonus => Mathf.Clamp01(radarTrackingBonus);
        public float RadarTurnRateBonus => Mathf.Max(0f, radarTurnRateBonus);
        public float SatelliteSupportRangeBonus => Mathf.Max(0f, satelliteSupportRangeBonus);
        public float SatelliteLockTimeMultiplier => Mathf.Clamp(satelliteLockTimeMultiplier, 0.1f, 1f);
        public float SatelliteTrackingBonus => Mathf.Clamp01(satelliteTrackingBonus);
        public float SatelliteTurnRateBonus => Mathf.Max(0f, satelliteTurnRateBonus);
        public float MaxSupportRangeBonus => Mathf.Max(0f, maxSupportRangeBonus);
        public float MaxSupportTrackingBonus => Mathf.Clamp01(maxSupportTrackingBonus);
        public GameObject MissileVisualPrefab => missileVisualPrefab;
        public GameObject LaunchFlashPrefab => launchFlashPrefab;
        public GameObject LaunchSmokePrefab => launchSmokePrefab;
        public GameObject MissileTrailPrefab => missileTrailPrefab;
        public GameObject AirburstExplosionPrefab => airburstExplosionPrefab;
        public GameObject AirTargetImpactPrefab => airTargetImpactPrefab;
        public GameObject InterceptExplosionPrefab => interceptExplosionPrefab;
    }
}
