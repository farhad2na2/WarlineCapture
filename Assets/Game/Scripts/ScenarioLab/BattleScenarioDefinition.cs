using System;
using UnityEngine;

namespace Game.Runtime
{
    [CreateAssetMenu(menuName = "Game/Scenario Lab/Battle Scenario Definition", fileName = "BattleScenarioDefinition")]
    public sealed class BattleScenarioDefinition : ScriptableObject
    {
        [SerializeField] private string scenarioId;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField, Min(0.001f)] private float fixedDeltaTime = 0.05f;
        [SerializeField, Min(0.1f)] private float maxDurationSeconds = 12f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private BattleScenarioCameraPreset cameraPreset = BattleScenarioCameraPreset.Default;
        [SerializeField] private Bounds worldBounds = new(Vector3.zero, new Vector3(260f, 80f, 160f));
        [SerializeField] private BattleScenarioSpawnEntry[] spawnEntries = Array.Empty<BattleScenarioSpawnEntry>();
        [SerializeField] private BattleScenarioVariant[] scenarioVariants = Array.Empty<BattleScenarioVariant>();
        [SerializeField] private BattleScenarioSuccessCriteria successCriteria = BattleScenarioSuccessCriteria.Default;

        public string ScenarioId => scenarioId;
        public string DisplayName => displayName;
        public string Description => description;
        public float FixedDeltaTime => Mathf.Max(0.001f, fixedDeltaTime);
        public float MaxDurationSeconds => Mathf.Max(0.1f, maxDurationSeconds);
        public int RandomSeed => randomSeed;
        public BattleScenarioCameraPreset CameraPreset => cameraPreset;
        public Bounds WorldBounds => worldBounds;
        public BattleScenarioSpawnEntry[] SpawnEntries => spawnEntries ?? Array.Empty<BattleScenarioSpawnEntry>();
        public BattleScenarioVariant[] ScenarioVariants => scenarioVariants ?? Array.Empty<BattleScenarioVariant>();
        public BattleScenarioSuccessCriteria SuccessCriteria => successCriteria;
    }
}
