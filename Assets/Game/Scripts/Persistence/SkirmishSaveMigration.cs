using Game.Configs;

namespace Game.Runtime
{
    public static class SkirmishSaveMigration
    {
        public const int Version = 2;

        public static QuickGameSaveData Normalize(QuickGameSaveData data)
        {
            data ??= new QuickGameSaveData();
            data.configuration = data.schemaVersion >= 1 && data.schemaVersion <= Version
                ? data.configuration.NormalizeForBaseAssault()
                : QuickGameConfig.Defaults;
            data.schemaVersion = Version;
            data.presetId = data.configuration.ScenarioIndex == SkirmishPresetConfig.StressScaleProbeScenarioIndex
                ? "stress_scale_probe"
                : data.configuration.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex
                    ? "industrial_basin"
                    : data.configuration.ScenarioIndex == SkirmishPresetConfig.CityCrossroadsScenarioIndex
                        ? "city_crossroads"
                        : "base_assault";
            data.enemyCount = 1;
            data.difficulty = "Normal";
            data.fogOfWar = false;
            return data;
        }
    }
}
