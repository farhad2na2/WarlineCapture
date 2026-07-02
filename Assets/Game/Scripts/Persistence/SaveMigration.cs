namespace Game.Runtime
{
    public static class SaveMigration
    {
        public const int CurrentVersion = 1;

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
            return data;
        }
    }
}
