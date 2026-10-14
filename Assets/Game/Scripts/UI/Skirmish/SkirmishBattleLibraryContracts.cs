using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.UI.Runtime
{
    /// <summary>
    /// SCN-13 battle library levels. Grid is a plate mode of the battlefield level, not a route.
    /// </summary>
    public enum SkirmishLibraryLevel : byte
    {
        Theater = 0,
        Battlefield = 1
    }

    public enum SkirmishLibraryPlateMode : byte
    {
        Map = 0,
        Grid = 1
    }

    /// <summary>
    /// Presentation access. Medals never change this. Programmer 1 does not own the screen;
    /// the host evaluator maps <c>ContentAccessRuntime</c> onto these values.
    /// </summary>
    public enum SkirmishLibraryCardAccess : byte
    {
        OwnedPlayable = 0,
        NotOwned = 1,
        Unavailable = 2,
        Planned = 3
    }

    /// <summary>
    /// One published or development card. Filled by <see cref="ISkirmishBattleLibraryProjection"/>.
    /// </summary>
    public sealed class SkirmishLibraryCardModel
    {
        public string CatalogId = string.Empty;
        public string MapId = string.Empty;
        public string ObjectiveId = string.Empty;
        public string ArmyProfile = string.Empty;
        public string StartProfile = string.Empty;
        public string Title = string.Empty;
        public string Purpose = string.Empty;
        public string ArmyLabel = string.Empty;
        public string StartLabel = string.Empty;
        public string PreviewKey = string.Empty;
        public bool MedalRecruit;
        public bool MedalRegular;
        public bool MedalVeteran;
        public bool MedalCommander;
        public SkirmishLibraryCardAccess Access;
        public string UnavailableReason = string.Empty;

        public bool HasMedal(SkirmishDifficultyId difficulty)
        {
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: return MedalRecruit;
                case SkirmishDifficultyId.Regular: return MedalRegular;
                case SkirmishDifficultyId.Veteran: return MedalVeteran;
                case SkirmishDifficultyId.Commander: return MedalCommander;
                default: return false;
            }
        }
    }

    public sealed class SkirmishLibraryObjectiveModel
    {
        public string ObjectiveId = string.Empty;
        public string Title = string.Empty;
        public string ShortLabel = string.Empty;
        public int Won;
        public int Published;
    }

    public sealed class SkirmishLibraryBattlefieldModel
    {
        public string MapId = string.Empty;
        public string Title = string.Empty;
        public string Geography = string.Empty;
        public int Won;
        public int Published;
        public int MedalRecruit;
        public int MedalRegular;
        public int MedalVeteran;
        public int MedalCommander;
    }

    /// <summary>
    /// Inputs the screen already knows. WP2 may add fields; keep existing fields stable.
    /// </summary>
    public struct SkirmishLibraryQuery
    {
        public bool RightToLeft;
        public bool DevelopmentBuild;
        public string SelectedCatalogId;
        public string MapId;
        public string ObjectiveId;
        public string Search;
        public string ArmyFilter;
        public string StartFilter;
        public bool MissingMedalOnly;
        public SkirmishDifficultyId MedalDifficulty;
    }

    /// <summary>
    /// Immutable-by-convention snapshot. The screen reads it and does not award medals.
    /// </summary>
    public sealed class SkirmishLibrarySnapshot
    {
        public SkirmishLibraryBattlefieldModel[] Battlefields = Array.Empty<SkirmishLibraryBattlefieldModel>();
        public SkirmishLibraryObjectiveModel[] Objectives = Array.Empty<SkirmishLibraryObjectiveModel>();
        public SkirmishLibraryCardModel[] Cards = Array.Empty<SkirmishLibraryCardModel>();
        public SkirmishLibraryCardModel Selected;
        public bool HasSelection;
        public string RecommendedCatalogId = string.Empty;
        public string RecommendedTitle = string.Empty;
        public string FocusCatalogId = string.Empty;
        public int TheaterWon;
        public int TheaterPublished;
        public int MedalRecruit;
        public int MedalRegular;
        public int MedalVeteran;
        public int MedalCommander;
    }

    /// <summary>
    /// WP2 projection. Programmer 1 replaces <see cref="SkirmishBattleLibraryCatalogAdapter"/>.
    /// The adapter reads the catalog and <see cref="ISkirmishMedalReadModel"/> only.
    /// It does not award medals, persist them, or choose Recommended Next.
    /// </summary>
    public interface ISkirmishBattleLibraryProjection
    {
        SkirmishLibrarySnapshot Project(in SkirmishLibraryQuery query);
    }

    /// <summary>
    /// WP1 read model. The empty model is the seam until the ledger lands.
    /// <see cref="BattlesWon"/> counts scenarios with at least one medal, not medal rows.
    /// </summary>
    public interface ISkirmishMedalReadModel
    {
        bool Has(string catalogId, SkirmishDifficultyId difficulty);
        int BattlesWon(string mapId);
        int BattlesWon(string mapId, string objectiveId);
        void TotalsByDifficulty(out int recruit, out int regular, out int veteran, out int commander);
    }

    /// <summary>
    /// WP2 decisions the screen must not invent.
    /// Recommended Next and Quick Battle stay unimplemented until Programmer 1 assigns an advisor.
    /// </summary>
    public interface ISkirmishBattleLibraryAdvisor
    {
        bool TryRecommendNext(in SkirmishLibraryQuery query, IReadOnlyList<SkirmishLibraryCardModel> cards, out string catalogId);
        bool TryQuickBattle(in SkirmishLibraryQuery query, int seed, out string catalogId);
    }

    public sealed class SkirmishMedalReadModelEmpty : ISkirmishMedalReadModel
    {
        public static readonly SkirmishMedalReadModelEmpty Instance = new SkirmishMedalReadModelEmpty();
        public bool Has(string catalogId, SkirmishDifficultyId difficulty) => false;
        public int BattlesWon(string mapId) => 0;
        public int BattlesWon(string mapId, string objectiveId) => 0;
        public void TotalsByDifficulty(out int recruit, out int regular, out int veteran, out int commander)
        {
            recruit = regular = veteran = commander = 0;
        }
    }

    public sealed class SkirmishBattleLibraryAdvisorUnimplemented : ISkirmishBattleLibraryAdvisor
    {
        public static readonly SkirmishBattleLibraryAdvisorUnimplemented Instance = new SkirmishBattleLibraryAdvisorUnimplemented();
        public bool TryRecommendNext(in SkirmishLibraryQuery query, IReadOnlyList<SkirmishLibraryCardModel> cards, out string catalogId)
        {
            catalogId = string.Empty;
            return false;
        }

        public bool TryQuickBattle(in SkirmishLibraryQuery query, int seed, out string catalogId)
        {
            catalogId = string.Empty;
            return false;
        }
    }

    public delegate SkirmishLibraryCardAccess SkirmishLibraryAccessEvaluator(string catalogId, bool catalogPlayable);

    /// <summary>
    /// Replacement points for Programmer 1. The screen reads these and does not construct a ledger.
    /// </summary>
    public static class SkirmishBattleLibraryServices
    {
        public static ISkirmishMedalReadModel Medals = SkirmishMedalReadModelEmpty.Instance;
        public static ISkirmishBattleLibraryAdvisor Advisor = SkirmishBattleLibraryAdvisorUnimplemented.Instance;
        public static SkirmishLibraryAccessEvaluator Access = DefaultAccess;

        public static SkirmishLibraryCardAccess DefaultAccess(string catalogId, bool catalogPlayable)
        {
            return catalogPlayable ? SkirmishLibraryCardAccess.OwnedPlayable : SkirmishLibraryCardAccess.Planned;
        }
    }

    /// <summary>
    /// PlayerPrefs keys beside <c>Warline.Skirmish.LastScenarioId</c>.
    /// The screen writes them so a return from a match can restore the plate.
    /// Programmer 1 may replace the storage; the key names are the contract.
    /// </summary>
    public static class SkirmishLibrarySessionKeys
    {
        public const string Scenario = "Warline.Skirmish.LastScenarioId";
        public const string Level = "Warline.Skirmish.LibraryLevel";
        public const string Map = "Warline.Skirmish.LibraryMap";
        public const string Objective = "Warline.Skirmish.LibraryObjective";
        public const string Plate = "Warline.Skirmish.LibraryPlate";
        public const string Difficulty = "Warline.Skirmish.LibraryDifficulty";
    }
}
