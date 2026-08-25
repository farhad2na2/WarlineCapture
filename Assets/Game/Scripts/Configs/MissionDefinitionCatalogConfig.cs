using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionDefinitionCatalogEntryConfig
    {
        [SerializeField] private string missionId;
        [SerializeField] private MissionDefinitionConfig definition;
        [SerializeField] private ScenarioSetupConfig scenario;

        public MissionDefinitionCatalogEntryConfig(string missionId, MissionDefinitionConfig definition)
            : this(missionId, definition, null)
        {
        }

        public MissionDefinitionCatalogEntryConfig(
            string missionId,
            MissionDefinitionConfig definition,
            ScenarioSetupConfig scenario)
        {
            this.missionId = missionId;
            this.definition = definition;
            this.scenario = scenario;
        }

        public string MissionId => missionId;
        public MissionDefinitionConfig Definition => definition;
        public ScenarioSetupConfig Scenario => scenario;
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

        public bool TryResolve(
            string missionId,
            out MissionDefinitionConfig definition,
            out ScenarioSetupConfig scenario)
        {
            definition = null;
            scenario = null;
            if (entries == null || !MissionDefinitionContractValidation.IsValidMissionId(missionId))
                return false;

            for (int index = 0; index < entries.Length; index++)
            {
                if (!string.Equals(entries[index].MissionId, missionId, StringComparison.Ordinal))
                    continue;
                definition = entries[index].Definition;
                scenario = entries[index].Scenario;
                return definition != null && scenario != null;
            }

            return false;
        }
    }
}
