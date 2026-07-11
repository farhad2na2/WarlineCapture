using System;

namespace Game.Runtime
{
    [Serializable]
    public sealed class SaveDataModel
    {
        public int saveVersion = SaveMigration.CurrentVersion;
        public PlayerProfileSaveData profile = new();
        public SettingsSaveData settings = new();
        public QuickGameSaveData quickGame = new();
    }

    [Serializable]
    public sealed class PlayerProfileSaveData
    {
        public int profileSchemaVersion = FirstLaunchProfileState.CurrentSchemaVersion;
        public string commanderName = "Commander";
        public int commanderLevel = 1;
        public int commanderXp;
        public int credits;
        public int materials;
        public int fuel;
        public int intel;
        public int commandAuthority;
        public int rushTickets;
        public int victories;
        public int defeats;
        public int missionsCompleted;
        public int starsEarned;
        public int enemiesDefeated;
        public int unitsLost;
        public int buildingsBuilt;
        public int resourcesEarned;
        public BlueprintPartSaveData[] blueprintParts = Array.Empty<BlueprintPartSaveData>();
        public string[] ownedUnitUnlocks = Array.Empty<string>();
        public string[] ownedBuildingUnlocks = Array.Empty<string>();
        public string[] ownedSupportAbilityUnlocks = Array.Empty<string>();
        public string[] ownedCosmetics = Array.Empty<string>();
        public string firstLaunchStatus = FirstLaunchProfileState.NotStarted;
        public string firstLaunchLastCompletedStateId = string.Empty;
        public string firstLaunchCommanderCallsign = "COMMANDER";
        public string firstLaunchCommanderDisplayName = "Commander";
        public int firstLaunchCommanderPortraitIndex;
        public string firstLaunchGuidance = "Full";
        public bool firstLaunchWatched;
        public bool firstLaunchSkipped;
    }

    public static class FirstLaunchProfileState
    {
        public const int CurrentSchemaVersion = 1;
        public const string NotStarted = "NotStarted";
        public const string InProgress = "InProgress";
        public const string HandoffPending = "HandoffPending";
        public const string Completed = "Completed";

        public static bool IsKnown(string value)
        {
            return value == NotStarted || value == InProgress || value == HandoffPending || value == Completed;
        }
    }

    [Serializable]
    public sealed class BlueprintPartSaveData
    {
        public string targetItemId;
        public int amount;
    }

    [Serializable]
    public sealed class SettingsSaveData
    {
        public float masterVolume = 80f;
        public float musicVolume = 70f;
        public float sfxVolume = 80f;
        public bool highContrastUi;
        public bool largeText;
        public string language = "English";
    }

    [Serializable]
    public sealed class QuickGameSaveData
    {
        public string presetId = "balanced";
        public int enemyCount = 2;
        public string difficulty = "Normal";
        public bool fogOfWar = true;
    }
}
