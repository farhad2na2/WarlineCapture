using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Packaged expanded-skirmish catalog. Lives under Resources so normal launches load
    /// authored definition assets as the source of truth instead of building an in-memory
    /// set or reading repository planning files. Authored by the Editor definition builder;
    /// validated against the in-memory factory so the two cannot drift.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Expansion Catalog")]
    public sealed class SkirmishExpansionCatalogConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishExpansionCatalog";

        [SerializeField] private SkirmishObjectiveConfig objectiveBa;
        [SerializeField] private SkirmishArmyProfileConfig armyGround;
        [SerializeField] private SkirmishArmyProfileConfig armyAir;
        [SerializeField] private SkirmishStartPackageConfig startField;
        [SerializeField] private SkirmishStartPackageConfig startEstablished;
        [SerializeField] private SkirmishSizeConfig sizeStandard;
        [SerializeField] private SkirmishSizeConfig sizeWar;
        [SerializeField] private SkirmishSizeConfig sizeLargeWar;
        [SerializeField] private SkirmishDifficultyConfig difficultyRecruit;
        [SerializeField] private SkirmishDifficultyConfig difficultyRegular;
        [SerializeField] private SkirmishDifficultyConfig difficultyVeteran;
        [SerializeField] private SkirmishDifficultyConfig difficultyCommander;
        [SerializeField] private SkirmishEconomyConfig economy;
        [SerializeField] private SkirmishUpgradeConfig upgrades;
        [SerializeField] private SkirmishIntelConfig intel;
        [SerializeField] private SkirmishRoleCatalogConfig roles;
        [SerializeField] private SkirmishGroundStagingConfig groundStaging;
        [SerializeField] private SkirmishMapLayoutConfig layoutDbBa;
        [SerializeField] private SkirmishPublicationConfig publication;
        [SerializeField] private SkirmishScenarioDefinitionConfig[] definitions =
            Array.Empty<SkirmishScenarioDefinitionConfig>();

        // Explicit archived references preserve saved versions without adding duplicate current cards.
        [SerializeField] private SkirmishScenarioDefinitionConfig[] historicalDefinitions =
            Array.Empty<SkirmishScenarioDefinitionConfig>();
        public IReadOnlyList<SkirmishScenarioDefinitionConfig> Definitions => definitions;
        public IReadOnlyList<SkirmishScenarioDefinitionConfig> HistoricalDefinitions => historicalDefinitions;

        public bool TryGetDefinition(string catalogId, int version, out SkirmishScenarioDefinitionConfig definition) =>
            SkirmishDefinitionVersionResolver.TryResolve(definitions, historicalDefinitions, catalogId, version, out definition);

        public static SkirmishExpansionCatalogConfig Load()
        {
#if UNITY_EDITOR
            if (editorValidationCatalog != null) return editorValidationCatalog;
#endif
            return Resources.Load<SkirmishExpansionCatalogConfig>(ResourceName);
        }
#if UNITY_EDITOR
        private static SkirmishExpansionCatalogConfig editorValidationCatalog;
        public static IDisposable UseEditorValidationCatalog(SkirmishExpansionCatalogConfig candidate)
        {
            if (candidate == null || editorValidationCatalog != null) throw new InvalidOperationException("One explicit Editor authored candidate required.");
            editorValidationCatalog = candidate; return new EditorValidationScope(candidate);
        }
        private sealed class EditorValidationScope : IDisposable
        {
            private SkirmishExpansionCatalogConfig value;
            internal EditorValidationScope(SkirmishExpansionCatalogConfig candidate) => value = candidate;
            public void Dispose(){if(editorValidationCatalog == value)editorValidationCatalog=null;value=null;}
        }
#endif

        public bool TryGetDefinition(string catalogId, out SkirmishScenarioDefinitionConfig definition)
        {
            definition = null; int matches = 0;
            if (definitions == null || string.IsNullOrEmpty(catalogId)) return false;
            foreach (var candidate in definitions)
                if (candidate != null && string.Equals(candidate.CatalogId,catalogId,StringComparison.Ordinal))
                { definition=candidate; matches++; }
            if (matches==1) return true;
            definition=null;return false;
        }

        public SkirmishDifficultyConfig GetDifficulty(Game.Skirmish.Contracts.SkirmishDifficultyId difficulty)
        {
            switch (difficulty)
            {
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Recruit: return difficultyRecruit;
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Regular: return difficultyRegular;
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Veteran: return difficultyVeteran;
                case Game.Skirmish.Contracts.SkirmishDifficultyId.Commander: return difficultyCommander;
                default: return null;
            }
        }

        public SkirmishExpansionAuthoredSet ToAuthoredSet()
        {
            var set = new SkirmishExpansionAuthoredSet
            {
                Definitions = definitions == null
                    ? Array.Empty<SkirmishScenarioDefinitionConfig>()
                    : (SkirmishScenarioDefinitionConfig[])definitions.Clone(),
                HistoricalDefinitions = historicalDefinitions == null
                    ? Array.Empty<SkirmishScenarioDefinitionConfig>()
                    : (SkirmishScenarioDefinitionConfig[])historicalDefinitions.Clone(),
                ObjectiveBa = objectiveBa,
                ArmyGround = armyGround,
                ArmyAir = armyAir,
                StartField = startField,
                StartEstablished = startEstablished,
                SizeStandard = sizeStandard,
                SizeWar = sizeWar,
                SizeLargeWar = sizeLargeWar,
                DifficultyRecruit = difficultyRecruit,
                DifficultyRegular = difficultyRegular,
                DifficultyVeteran = difficultyVeteran,
                DifficultyCommander = difficultyCommander,
                Economy = economy,
                Upgrades = upgrades,
                Intel = intel,
                Roles = roles,
                GroundStaging = groundStaging,
                LayoutDbBa = layoutDbBa,
                Publication = publication
            };
            for (int i = 0; i < set.Definitions.Count; i++)
            {
                SkirmishScenarioDefinitionConfig definition = set.Definitions[i];
                if (definition == null)
                    continue;
                switch (definition.CatalogId)
                {
                    case SkirmishBattleCatalogConfig.DesertBaseEstablishedScenarioId:
                        set.DefinitionS002 = definition;
                        break;
                    case "S003":
                        set.DefinitionS003 = definition;
                        break;
                    case "S005":
                        set.DefinitionS005 = definition;
                        set.ArmyCombined = definition.ArmyProfileConfig;
                        break;
                    case "S004":
                        set.DefinitionS004 = definition;
                        break;
                }
            }

            return set;
        }

        public bool TryValidate(out string error)
        {
            if (objectiveBa == null || armyGround == null || armyAir == null ||
                startField == null || startEstablished == null ||
                sizeStandard == null || sizeWar == null || sizeLargeWar == null ||
                difficultyRecruit == null || difficultyRegular == null ||
                difficultyVeteran == null || difficultyCommander == null ||
                economy == null || upgrades == null || intel == null || roles == null ||
                groundStaging == null || layoutDbBa == null || publication == null)
            {
                error = "Skirmish expansion catalog is missing one or more shared references.";
                return false;
            }

            if (definitions.Length == 0)
            {
                error = "Skirmish expansion catalog has no scenario definitions.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] == null)
                {
                    error = $"Skirmish expansion catalog definition at index {i} is missing.";
                    return false;
                }

                if (!seen.Add(definitions[i].CatalogId))
                {
                    error = $"Duplicate skirmish expansion definition id: '{definitions[i].CatalogId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
