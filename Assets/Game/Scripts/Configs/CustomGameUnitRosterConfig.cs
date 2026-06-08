using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Custom Game/Unit Roster Config")]
public sealed class CustomGameUnitRosterConfig : ScriptableObject
{
    [System.Serializable]
    public sealed class UnitEntry
    {
        [SerializeField] private string sourceKey;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject legacyUnitPrefab;
        [SerializeField] private GameObject visualPrefab;

        public string SourceKey => sourceKey;
        public string DisplayName => displayName;
        public GameObject LegacyUnitPrefab => legacyUnitPrefab;
        public GameObject VisualPrefab => visualPrefab;
    }

    [SerializeField] private List<UnitEntry> units = new();

    public IReadOnlyList<UnitEntry> Units => units;
}
