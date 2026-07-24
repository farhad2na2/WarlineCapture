using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Authoring
{
    [Serializable]
    public struct DenseCityProtectedAutobahnReplacementManifestEntry
    {
        [SerializeField] private string stableId;
        [SerializeField] private string prefabGuid;
        [SerializeField] private int column;
        [SerializeField] private int row;

        public DenseCityProtectedAutobahnReplacementManifestEntry(
            string stableId,
            string prefabGuid,
            Vector2Int cell)
        {
            this.stableId = stableId;
            this.prefabGuid = prefabGuid;
            column = cell.x;
            row = cell.y;
        }

        public string StableId => stableId;
        public string PrefabGuid => prefabGuid;
        public Vector2Int Cell => new(column, row);
    }

    public sealed class DenseCityProtectedAutobahnReplacementManifestAuthoring :
        MonoBehaviour
    {
        [SerializeField]
        private List<DenseCityProtectedAutobahnReplacementManifestEntry> entries = new();

        public IReadOnlyList<DenseCityProtectedAutobahnReplacementManifestEntry> Entries =>
            entries;

        public void Configure(
            IReadOnlyList<DenseCityProtectedAutobahnReplacementManifestEntry> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            entries.Clear();
            for (int index = 0; index < values.Count; index++)
                entries.Add(values[index]);
        }
    }
}
