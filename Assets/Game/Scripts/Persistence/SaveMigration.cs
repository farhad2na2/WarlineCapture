namespace Game.Runtime
{
    public static class SaveMigration
    {
        public const int CurrentVersion = 2;

        public static SaveDataModel Migrate(SaveDataModel saveData)
        {
            SaveDataModel data = saveData ?? new SaveDataModel();
            data.saveVersion = CurrentVersion;
            data.profile ??= new PlayerProfileSaveData();
            data.settings ??= new SettingsSaveData();
            data.quickGame ??= new QuickGameSaveData();
            data.profile.blueprintParts ??= System.Array.Empty<BlueprintPartSaveData>();
            data.profile.ownedUnitUnlocks ??= System.Array.Empty<string>();
            data.profile.ownedBuildingUnlocks ??= System.Array.Empty<string>();
            data.profile.ownedSupportAbilityUnlocks ??= System.Array.Empty<string>();
            data.profile.ownedCosmetics ??= System.Array.Empty<string>();
            data.profile.campaignMissionProgress ??= System.Array.Empty<CampaignMissionProgressSaveData>();
            // Existing profiles gain an empty ledger. QuickGameSaveData.lastResult is not a medal source.
            data.profile.skirmishMedals ??= System.Array.Empty<SkirmishMedalSaveData>();
            return data;
        }
    }
}
