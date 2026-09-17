using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Skirmish/Base Assault Preset")]
    public sealed class SkirmishPresetConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishBaseAssault";
        public const float MatchDurationSeconds = 900f;
        public const int InfantryLimitPerFaction = 24;
        [UnityEngine.Min(1)] public float supplyDaySeconds=120f;
        public BuildingPlacementSystemConfig buildingPlacement;
        public AIControllerConfig[] aiControllers;
        public AIPlanEntryStartupConfig aiPlan;
        public MapVehiclePlacementConfig mapVehicles;
    }
}
