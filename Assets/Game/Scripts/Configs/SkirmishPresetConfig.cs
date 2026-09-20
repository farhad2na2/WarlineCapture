using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Skirmish/Base Assault Preset")]
    public sealed class SkirmishPresetConfig : ScriptableObject
    {
        public const string ResourceName = "SkirmishBaseAssault";
        public const string SecondResourceName = "SkirmishCityCrossroads";
        public OperationMapDefinition operationMap;
        public static SkirmishPresetConfig Load(int scenarioIndex) =>
            Resources.Load<SkirmishPresetConfig>(scenarioIndex == 1 ? SecondResourceName : ResourceName);
        public const float MatchDurationSeconds = 900f;
        public const int InfantryLimitPerFaction = 24;
        [UnityEngine.Min(1)] public float supplyDaySeconds=120f;
        [Min(1)] public float rifleRange=32f;
        [Min(1)] public int rifleDamage=6;
        [Min(.1f)] public float rifleCooldown=.8f;
        [Min(1)] public float armoredCarRange=40f;
        [Min(1)] public int armoredCarDamage=18;
        [Min(.1f)] public float armoredCarCooldown=1.2f;
        [Min(1)] public float watchtowerRange = 55f;
        [Min(1)] public int watchtowerDamage = 10;
        [Min(.1f)] public float watchtowerCooldown = .8f;
        [Min(0)] public float firstAttackSeconds=60f;
        public Vector3 playerReinforcementRallyOffset = new(40, 0, 10);
        [Min(4)] public int reinforcementInfantryTarget=16;
        [Range(4, 8)] public int attackSquadSize=8;
        [Min(1)] public float baseDefenseRadius=70f;
        [Min(1)] public int aiMaterialsReserve = 80;
        [Min(1)] public int aiFuelReserve = 160;
        public BuildingPlacementSystemConfig buildingPlacement;
        public AIControllerConfig[] aiControllers;
        public AIPlanEntryStartupConfig aiPlan;
        public MapVehiclePlacementConfig mapVehicles;
    }
}
