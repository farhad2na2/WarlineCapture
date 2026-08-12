using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionDefinitionCatalogEntryConfig
    {
        [SerializeField] private string missionId;
        [SerializeField] private MissionDefinitionConfig definition;

        public MissionDefinitionCatalogEntryConfig(string missionId, MissionDefinitionConfig definition)
        {
            this.missionId = missionId;
            this.definition = definition;
        }

        public string MissionId => missionId;
        public MissionDefinitionConfig Definition => definition;
    }

    [CreateAssetMenu(menuName = "Game/Missions/Mission Definition Catalog", fileName = "MissionDefinitionCatalog")]
    public sealed class MissionDefinitionCatalogConfig : ScriptableObject
    {
        [SerializeField] private MissionDefinitionCatalogEntryConfig[] entries =
            Array.Empty<MissionDefinitionCatalogEntryConfig>();

        public ReadOnlySpan<MissionDefinitionCatalogEntryConfig> Entries => entries;

        public bool TryResolve(string missionId, out MissionDefinitionConfig definition)
        {
            definition = null;
            if (entries == null || !MissionDefinitionContractValidation.IsValidMissionId(missionId))
                return false;

            for (int index = 0; index < entries.Length; index++)
            {
                if (string.Equals(entries[index].MissionId, missionId, StringComparison.Ordinal))
                {
                    definition = entries[index].Definition;
                    return definition != null;
                }
            }

            return false;
        }
    }
}
