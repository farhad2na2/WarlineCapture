using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum SkirmishBattleCatalogStatus : byte
    {
        Planned = 0,
        Playable = 1
    }

    [Serializable]
    public struct SkirmishBattleCatalogEntry
    {
        public string ScenarioId;
        public string MapId;
        public string ObjectiveId;
        public string ArmyProfile;
        public string StartProfile;
        public string TitleKey;
        public string TitleEnglish;
        public string DescriptionEnglish;
        public string DescriptionFarsi;
        public SkirmishBattleCatalogStatus Status;
        /// <summary>Runtime ScenarioIndex when Status is Playable; otherwise -1.</summary>
        public int PlayableScenarioIndex;
        public string DefinitionId;
        public int ContentVersion;
        public string ReadinessManifestId;

        public bool IsPlayable =>
            Status == SkirmishBattleCatalogStatus.Playable && PlayableScenarioIndex >= 0;
    }

    [CreateAssetMenu(menuName = "Game/Skirmish/Battle Catalog")]
    public sealed class SkirmishBattleCatalogConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishBattleCatalog";
        public const string DesertBaseScenarioId = "S001";
        public const string DesertBaseEstablishedScenarioId = "S002";
        public const string DesertBaseEstablishedDefinitionId = "skirmish.s002";
        public const string DesertBaseAirMobileFieldScenarioId = "S003";
        public const string DesertBaseAirMobileFieldDefinitionId = "skirmish.s003";
        public const string DesertBaseAirMobileEstablishedScenarioId = "S004";
        public const string DesertBaseAirMobileEstablishedDefinitionId = "skirmish.s004";
        public const string CityCrossroadsScenarioId = "S025";
        public const string IndustrialBasinScenarioId = "S073";

        [SerializeField] private SkirmishBattleCatalogEntry[] entries =
            Array.Empty<SkirmishBattleCatalogEntry>();

        public IReadOnlyList<SkirmishBattleCatalogEntry> Entries => entries;

        // Non-Standard choices are validation candidates only until per-size publication lands.
        // Authority is the existing disposable catalog scope, never a caller/environment flag.
        public static bool IsMissionSizeSelectionAvailable(string catalogId,string definitionId,int version,
            Game.Skirmish.Contracts.SkirmishSizeId size)
        {
            if(size==Game.Skirmish.Contracts.SkirmishSizeId.Standard)return true;
            if(size!=Game.Skirmish.Contracts.SkirmishSizeId.War&&size!=Game.Skirmish.Contracts.SkirmishSizeId.LargeWar)return false;
#if UNITY_EDITOR
            if(editorValidationCatalog==null)return false;
            var authored=SkirmishExpansionCatalogConfig.Load();
            if(authored==null||!authored.TryGetDefinition(catalogId,out var definition)||
                definition.DefinitionId!=definitionId||definition.ContentVersion!=version)return false;
            int matches=0;
            foreach(var row in editorValidationCatalog.Entries)
                if(row.ScenarioId==catalogId)
                {
                    if(!row.IsPlayable||row.DefinitionId!=definitionId||row.ContentVersion!=version)return false;
                    matches++;
                }
            return matches==1;
#else
            return false;
#endif
        }

        public static SkirmishBattleCatalogConfig Load()
        {
#if UNITY_EDITOR
            if (editorValidationCatalog != null) return editorValidationCatalog;
#endif
            return Resources.Load<SkirmishBattleCatalogConfig>(ResourceName);
        }

#if UNITY_EDITOR
        private static SkirmishBattleCatalogConfig editorValidationCatalog;

        /// <summary>Expose an unpublished candidate to native Editor input tests without changing the shipped catalog.</summary>
        public static IDisposable UseEditorValidationCatalog(SkirmishBattleCatalogConfig candidate)
        {
            if (candidate == null || editorValidationCatalog != null)
                throw new InvalidOperationException("A single non-null Editor validation catalog is required.");
            editorValidationCatalog = candidate;
            return new EditorValidationCatalogScope(candidate);
        }

        private sealed class EditorValidationCatalogScope : IDisposable
        {
            private SkirmishBattleCatalogConfig candidate;
            public EditorValidationCatalogScope(SkirmishBattleCatalogConfig value) => candidate = value;
            public void Dispose()
            {
                if (candidate == null) return;
                if (editorValidationCatalog == candidate) editorValidationCatalog = null;
                candidate = null;
            }
        }
#endif

        public void Configure(SkirmishBattleCatalogEntry[] configured)
        {
            entries = configured ?? Array.Empty<SkirmishBattleCatalogEntry>();
        }

        public int CountPlayable()
        {
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].IsPlayable)
                    count++;
            return count;
        }

        public void CollectPlayable(List<SkirmishBattleCatalogEntry> destination)
        {
            destination.Clear();
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].IsPlayable)
                    destination.Add(entries[i]);
        }

        public bool TryGet(string scenarioId, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].ScenarioId, scenarioId, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public bool TryGetByScenarioIndex(int scenarioIndex, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].IsPlayable && entries[i].PlayableScenarioIndex == scenarioIndex)
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public bool TryValidate(out string error)
        {
            if (entries == null || entries.Length == 0)
            {
                error = "Skirmish battle catalog requires at least one entry.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            int playable = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.ScenarioId))
                {
                    error = $"Skirmish battle catalog entry at index {i} is missing scenarioId.";
                    return false;
                }

                if (!seen.Add(entry.ScenarioId))
                {
                    error = $"Duplicate skirmish battle catalog id: '{entry.ScenarioId}'.";
                    return false;
                }

                if (entry.IsPlayable)
                    playable++;
                else if (entry.PlayableScenarioIndex >= 0)
                {
                    error = $"Planned entry '{entry.ScenarioId}' must not carry a playable scenario index.";
                    return false;
                }
            }

            if (!TryGet(DesertBaseScenarioId, out SkirmishBattleCatalogEntry desert) ||
                !desert.IsPlayable ||
                desert.PlayableScenarioIndex != SkirmishPresetConfig.DesertBaseScenarioIndex)
            {
                error = "S001 must be Playable at scenario index 0.";
                return false;
            }

            if (!TryGet(CityCrossroadsScenarioId, out SkirmishBattleCatalogEntry city) ||
                !city.IsPlayable ||
                city.PlayableScenarioIndex != SkirmishPresetConfig.CityCrossroadsScenarioIndex)
            {
                error = "S025 must be Playable at scenario index 1.";
                return false;
            }

            if (!TryGet(IndustrialBasinScenarioId, out SkirmishBattleCatalogEntry basin) ||
                !basin.IsPlayable ||
                basin.PlayableScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex)
            {
                error = "S073 must be Playable at scenario index 3.";
                return false;
            }

            if (!TryGet(DesertBaseEstablishedScenarioId, out SkirmishBattleCatalogEntry established) ||
                !established.IsPlayable ||
                established.PlayableScenarioIndex != SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex ||
                established.DefinitionId != DesertBaseEstablishedDefinitionId)
            {
                error = "S002 must be Playable at scenario index 4 with definition skirmish.s002.";
                return false;
            }

            if (!TryGet(DesertBaseAirMobileFieldScenarioId, out SkirmishBattleCatalogEntry airMobile) ||
                !airMobile.IsPlayable ||
                airMobile.PlayableScenarioIndex != SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex ||
                airMobile.DefinitionId != DesertBaseAirMobileFieldDefinitionId)
            {
                error = "S003 must be Playable at scenario index 5 with definition skirmish.s003.";
                return false;
            }

            if (!TryGet(DesertBaseAirMobileEstablishedScenarioId, out SkirmishBattleCatalogEntry establishedAir) ||
                !establishedAir.IsPlayable ||
                establishedAir.PlayableScenarioIndex != SkirmishPresetConfig.DesertBaseAirMobileEstablishedScenarioIndex ||
                establishedAir.DefinitionId != DesertBaseAirMobileEstablishedDefinitionId)
            {
                error = "S004 must be Playable at scenario index 6 with definition skirmish.s004.";
                return false;
            }

            if (playable < 3)
            {
                error = "Skirmish battle catalog requires at least the three playable prototypes.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
