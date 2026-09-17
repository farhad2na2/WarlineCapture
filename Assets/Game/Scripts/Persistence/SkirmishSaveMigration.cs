using Game.Configs;

namespace Game.Runtime
{
    public static class SkirmishSaveMigration
    {
        public const int Version = 1;

        public static QuickGameSaveData Normalize(QuickGameSaveData data)
        {
            data ??= new QuickGameSaveData();
            data.configuration = data.schemaVersion == Version
                ? data.configuration.NormalizeForBaseAssault()
                : QuickGameConfig.Defaults;
            data.schemaVersion = Version;
            data.presetId = "base_assault";
            data.enemyCount = 1;
            data.difficulty = "Normal";
            data.fogOfWar = false;
            return data;
        }
    }
}
