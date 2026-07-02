using System.Collections.Generic;
using UnityEngine;
using Game.Components;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Custom Game/Faction Config")]
    public sealed class CustomGameFactionConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class UnitSpawnEntry
        {
            [SerializeField] private string sourceKey;
            [SerializeField, Min(0)] private int count = 1;
            [SerializeField] private Vector2Int spawnOffset;

            public string SourceKey => sourceKey;
            public int Count => count;
            public Vector2Int SpawnOffset => spawnOffset;
        }

        [System.Serializable]
        public sealed class BuildingSpawnEntry
        {
            [SerializeField] private string lookupKey;
            [SerializeField] private GameObject legacyBuildingPrefab;
            [SerializeField] private Vector2Int originOffset;

            public string LookupKey => lookupKey;
            public GameObject LegacyBuildingPrefab => legacyBuildingPrefab;
            public Vector2Int OriginOffset => originOffset;
        }

        [System.Serializable]
        public sealed class FactionEntry
        {
            [SerializeField, Min(0)] private int factionId;
            [SerializeField] private string displayName;
            [SerializeField] private Vector2Int spawnCell = new(10, 10);
            [SerializeField] private List<UnitSpawnEntry> units = new();
            [SerializeField] private List<BuildingSpawnEntry> buildings = new();

            public int FactionId => factionId;
            public string DisplayName => displayName;
            public Vector2Int SpawnCell => spawnCell;
            public IReadOnlyList<UnitSpawnEntry> Units => units;
            public IReadOnlyList<BuildingSpawnEntry> Buildings => buildings;
        }

        [SerializeField] private List<FactionEntry> factions = new();

        public IReadOnlyList<FactionEntry> Factions => factions;
    }
}
