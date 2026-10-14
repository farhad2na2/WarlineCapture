using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Skirmish;

namespace Game.Composition
{
    internal sealed class SkirmishMedalLedgerReader : ISkirmishMedalReader
    {
        private readonly SkirmishMedalLedger _ledger;
        private readonly Func<SkirmishMedalSaveData[]> _reload;

        public SkirmishMedalLedgerReader(SkirmishMedalLedger ledger, Func<SkirmishMedalSaveData[]> reload)
        {
            _ledger = ledger ?? new SkirmishMedalLedger(null);
            _reload = reload;
        }

        public bool Has(string catalogId, SkirmishDifficultyId difficulty) =>
            _ledger.Has(catalogId, difficulty);

        public int CountFor(string mapId) => _ledger.CountFor(mapId);

        public int CountFor(string mapId, string objectiveId) =>
            _ledger.CountFor(mapId, objectiveId);

        public void Refresh()
        {
            if (_reload == null)
                return;
            _ledger.Replace(_reload());
        }

        public static SkirmishMedalLedgerReader FromProfile(IReadOnlyList<SkirmishBattleCatalogEntry> catalog) =>
            new(new SkirmishMedalLedger(LoadProfileMedals(), catalog), LoadProfileMedals);

        private static SkirmishMedalSaveData[] LoadProfileMedals()
        {
            try
            {
                return SaveService.CreateDefault().LoadProfile().skirmishMedals ?? Array.Empty<SkirmishMedalSaveData>();
            }
            catch (Exception)
            {
                return Array.Empty<SkirmishMedalSaveData>();
            }
        }
    }

    internal static class SkirmishLibraryComposition
    {
        public static SkirmishLibraryViewModel Create(IReadOnlyList<SkirmishBattleCatalogEntry> catalog) =>
            new(
                catalog,
                SkirmishMedalLedgerReader.FromProfile(catalog),
                canonicalId => ContentAccessRuntime.Evaluate(canonicalId),
                showPlanned: SkirmishLibraryViewModel.ShowPlannedCards,
                setLaunchDifficulty: difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);
    }
}
