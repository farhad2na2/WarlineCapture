using System;

[Serializable]
public sealed class WarlineCaptureSaveData
{
    public int saveVersion = SaveMigration.CurrentVersion;
    public PlayerProfileSaveData profile = new();
    public SagaSaveData saga = new();
    public OperationSaveData operation = new();
    public SettingsSaveData settings = new();
    public QuickGameSaveData quickGame = new();
}

[Serializable]
public sealed class PlayerProfileSaveData
{
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
    public string[] claimedRewardTrackNodes = Array.Empty<string>();
    public MissionHistoryEntrySaveData[] missionHistory = Array.Empty<MissionHistoryEntrySaveData>();
}

[Serializable]
public sealed class BlueprintPartSaveData
{
    public string targetItemId;
    public int amount;
}

[Serializable]
public sealed class MissionHistoryEntrySaveData
{
    public string missionId;
    public string missionName;
    public bool victory;
    public int starsEarned;
    public int enemiesDefeated;
    public int unitsLost;
    public int buildingsBuilt;
    public int resourcesEarned;
    public string summary;
}

[Serializable]
public sealed class SagaSaveData
{
    public SagaMissionProgressData[] missions = Array.Empty<SagaMissionProgressData>();
}

[Serializable]
public sealed class SagaMissionProgressData
{
    public string missionId;
    public bool completed;
    public int stars;
}

[Serializable]
public sealed class OperationSaveData
{
    public int operationDay = 1;
    public int operationSupplies = 4;
    public int completedActions;
    public DistrictStateData[] districts = Array.Empty<DistrictStateData>();
    public OperationEventData[] pendingEvents = Array.Empty<OperationEventData>();
    public OperationIntelEvidenceData[] intelEvidence = Array.Empty<OperationIntelEvidenceData>();
}

[Serializable]
public sealed class DistrictStateData
{
    public string districtId;
    public int stability;
    public int threat;
    public int intel;
    public int trust;
    public int security;
    public int infrastructure;
    public int enemyInfluence;
    public int heat;
    public int civilianRisk;
}

[Serializable]
public sealed class OperationEventData
{
    public string eventId;
    public string title;
    public string body;
    public string districtId;
    public OperationActionType actionType;
    public OperationEventCategory category;
    public OperationEventSeverity severity;
    public int operationDay;
    public string sourceMetric;
    public int metricValue;
    public bool unread = true;
}

[Serializable]
public sealed class OperationIntelEvidenceData
{
    public string evidenceId;
    public string districtId;
    public string sourceEventId;
    public string title;
    public string body;
    public int confidence;
    public int operationDay;
    public bool unread = true;
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
