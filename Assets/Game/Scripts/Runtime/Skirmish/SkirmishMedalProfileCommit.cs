using System;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    public static class SkirmishMedalProfileCommit
    {
        /// <summary>
        /// Edit-mode tests point this at an isolated save. Play mode uses <see cref="SaveService.CreateDefault"/>.
        /// </summary>
        public static SaveService TestSave;

        /// <summary>
        /// When set, a false result refuses the profile write. Settlement must not acknowledge
        /// that attempt, so the same Victory can commit on the next try.
        /// </summary>
        public static Func<bool> AllowCommit;

        public static bool TryPersist(SkirmishResultReceipt receipt, out SkirmishMedalAwardResult award)
        {
            award = default;
            if (!SkirmishMedalLedger.IsEligible(receipt))
                return true;

            SaveService save = TestSave;
            if (save == null)
            {
                if (!UnityEngine.Application.isPlaying)
                    return true;
                save = SaveService.CreateDefault();
            }

            try
            {
                SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
                PlayerProfileSaveData profile = save.LoadProfile();
                var ledger = new SkirmishMedalLedger(profile.skirmishMedals, catalog != null ? catalog.Entries : null);
                if (!ledger.TryAward(receipt, DateTime.UtcNow, out award))
                {
                    award = default;
                    return true;
                }

                if (!award.IsFirstAward)
                    return true;
                if (AllowCommit != null && !AllowCommit())
                {
                    award = default;
                    return false;
                }

                profile.skirmishMedals = ledger.ToSaveData();
                save.SaveProfile(profile);
                return true;
            }
            catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException ||
                                          error is InvalidOperationException || error is ArgumentException)
            {
                award = default;
                return false;
            }
        }
    }

    public static class SkirmishLibraryLaunchContext
    {
        public static SkirmishDifficultyId Difficulty = SkirmishDifficultyId.Regular;
    }
}
