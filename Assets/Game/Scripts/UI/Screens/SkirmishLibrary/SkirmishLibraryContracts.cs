using System;
using Game.Skirmish.Contracts;

namespace Game.UI.Runtime
{
    public enum SkirmishLibraryLevel
    {
        Theater,
        Battlefield
    }

    public enum SkirmishBattlefieldLayout
    {
        Map,
        Grid
    }

    public enum SkirmishCardAccess
    {
        Playable,
        NotOwned,
        Unavailable,
        Planned
    }

    [Flags]
    public enum SkirmishMedalMask : byte
    {
        None = 0,
        Recruit = 1,
        Regular = 2,
        Veteran = 4,
        Commander = 8
    }

    public struct SkirmishMedalTotals
    {
        public int Recruit;
        public int Regular;
        public int Veteran;
        public int Commander;
    }

    public struct SkirmishTheaterPinState
    {
        public string MapId;
        public string TitleLocKey;
        public string SubtitleLocKey;
        public string PlatePreviewKey;
        public int PublishedCount;
        public int WonCount;
        public SkirmishMedalTotals Medals;
        public bool HasOwnedPlayable;
        public bool IsRecommended;
    }

    public struct SkirmishTheaterState
    {
        public SkirmishTheaterPinState[] Pins;
        public string SelectedMapId;
        public string RecommendedScenarioId;
        public int OverallWonCount;
        public int OverallPublishedCount;
        public int OverallMedalsEarned;
        public int OverallMedalsPossible;
        public SkirmishMedalTotals OverallMedalTotals;
    }

    public struct SkirmishObjectiveTabState
    {
        public string ObjectiveId;
        public string TitleLocKey;
        public int PublishedCount;
        public int WonCount;
    }

    public struct SkirmishBattleCardState
    {
        public string ScenarioId;
        public string MapId;
        public string ObjectiveId;
        public string ArmyProfile;
        public string StartProfile;
        public string TitleLocKey;
        public string SubtitleLocKey;
        public string ArmyLocKey;
        public string StartLocKey;
        public string PreviewSpriteKey;
        public string PinId;
        public SkirmishMedalMask Medals;
        public SkirmishCardAccess Access;
        public string LockedReasonLocKey;
        public bool IsSelected;
        public bool IsRecommended;
    }

    public struct SkirmishBattlefieldState
    {
        public string MapId;
        public SkirmishBattlefieldLayout Layout;
        public SkirmishObjectiveTabState[] Tabs;
        public string SelectedObjectiveId;
        public SkirmishBattleCardState[] MapCards;
        public SkirmishBattleCardState[] GridCards;
    }

    public struct SkirmishBriefingState
    {
        public string ScenarioId;
        public string TitleLocKey;
        public string DescriptionLocKey;
        public string ArmyLocKey;
        public string StartLocKey;
        public string PurposeLocKey;
        public SkirmishMedalMask Medals;
        public SkirmishDifficultyId[] AvailableDifficulties;
        public SkirmishDifficultyId SelectedDifficulty;
        public SkirmishCardAccess Access;
        public string LockedReasonLocKey;
        public string StoreRouteProductId;
        public bool CanLaunch;
        public string ObjectiveLocKey;
        public int DeadlineSeconds;
        public SkirmishSizeId RecommendedSize;
        public int StartingStructureCount;
        public float MapWidthMetres;
        public float MapDepthMetres;
        public string SizeLocKey;
        public bool ShowSize;
        public bool ShowTime;
    }

    public struct SkirmishLibraryBriefingSource
    {
        public bool Found;
        public int DeadlineSeconds;
        public SkirmishSizeId RecommendedSize;
        public int StartingStructureCount;
        public float MapWidthMetres;
        public float MapDepthMetres;
    }

    public struct SkirmishLibraryFilter
    {
        public string SearchText;
        public string ObjectiveId;
        public string ArmyProfile;
        public string StartProfile;
        public SkirmishDifficultyId? MedalMissing;
    }

    public enum SkirmishLibraryInteractionKind
    {
        Clicked,
        Selected
    }

    public readonly struct SkirmishLibraryInteraction
    {
        public SkirmishLibraryInteraction(
            SkirmishLibraryInteractionKind kind,
            string targetId,
            UIAudioEventKind sound)
        {
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            Sound = sound;
        }

        public SkirmishLibraryInteractionKind Kind { get; }
        public string TargetId { get; }
        public UIAudioEventKind Sound { get; }
    }

    public interface ISkirmishLibraryPrefs
    {
        string Get(string key, string fallback);
        void Set(string key, string value);
        void Save();
    }

    public interface ISkirmishLibraryViewModel
    {
        event Action Changed;
        event Action<SkirmishLibraryInteraction> Interaction;
        SkirmishLibraryLevel Level { get; }
        SkirmishTheaterState Theater { get; }
        SkirmishBattlefieldState Battlefield { get; }
        SkirmishBriefingState Briefing { get; }
        SkirmishLibraryFilter Filter { get; }
        bool SelectMapCollection(string mapId);
        bool SelectObjective(string objectiveId);
        void SetLayout(SkirmishBattlefieldLayout layout);
        bool SelectScenarioId(string scenarioId);
        void SelectDifficulty(SkirmishDifficultyId difficulty);
        void ZoomIn();
        void BackToTheater();
        string QuickBattle();
        void ApplyFilter(SkirmishLibraryFilter filter);
        bool Launch();
        void RestoreFromPrefs();
        void SaveToPrefs();
    }

    public static class SkirmishLibraryControlNames
    {
        public const string ZoomIn = "ZoomInButton";
        public const string QuickBattle = "QuickBattleButton";
        public const string Filters = "FiltersButton";
        public const string TheaterMap = "TheaterMapButton";
        public const string StartBattle = "StartBattleButton";
        public const string LayoutMap = "LayoutMapButton";
        public const string LayoutGrid = "LayoutGridButton";

        public static string TheaterPin(string mapId) => "TheaterPin_" + (mapId ?? string.Empty);
        public static string ObjectiveTab(string objectiveId) => "ObjectiveTab_" + (objectiveId ?? string.Empty);
        public static string BattleCard(string scenarioId) => "BattleCard_" + (scenarioId ?? string.Empty);
    }
}
