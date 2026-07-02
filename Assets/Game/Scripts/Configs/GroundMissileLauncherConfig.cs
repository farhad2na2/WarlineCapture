using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Config/Ground Missile Launcher")]
    public sealed class GroundMissileLauncherConfig : ScriptableObject
    {
        [Header("Range")]
        [SerializeField, Min(0f)] private float minRange = 35f;
        [SerializeField, Min(0f)] private float maxRange = 600f;

        [Header("Launcher")]
        [SerializeField, Min(0.01f)] private float prepareSeconds = 0.65f;
        [SerializeField, Min(0.01f)] private float reloadSeconds = 3f;
        [SerializeField] private float batteryElevatedAngleDegrees = -30f;

        [Header("Projectile")]
        [SerializeField, Min(0.01f)] private float rocketSpeed = 42f;
        [SerializeField, Min(0f)] private float arcHeight = 28f;
        [SerializeField, Min(0)] private int damage = 90;
        [SerializeField, Min(0f)] private float damageRadius = 8f;

        [Header("VFX")]
        [SerializeField] private GameObject launcherBackfirePrefab;
        [SerializeField] private GameObject rocketTrailPrefab;
        [SerializeField] private GameObject impactExplosionPrefab;
        [SerializeField] private GameObject impactSmokePrefab;

        public float MinRange => Mathf.Max(0f, minRange);
        public float MaxRange => Mathf.Max(MinRange, maxRange);
        public float PrepareSeconds => Mathf.Max(0.01f, prepareSeconds);
        public float ReloadSeconds => Mathf.Max(0.01f, reloadSeconds);
        public float BatteryElevatedAngleDegrees => batteryElevatedAngleDegrees;
        public float RocketSpeed => Mathf.Max(0.01f, rocketSpeed);
        public float ArcHeight => Mathf.Max(0f, arcHeight);
        public int Damage => Mathf.Max(0, damage);
        public float DamageRadius => Mathf.Max(0f, damageRadius);
        public GameObject LauncherBackfirePrefab => launcherBackfirePrefab;
        public GameObject RocketTrailPrefab => rocketTrailPrefab;
        public GameObject ImpactExplosionPrefab => impactExplosionPrefab;
        public GameObject ImpactSmokePrefab => impactSmokePrefab;
    }
}
