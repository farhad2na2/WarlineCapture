using System.Collections.Generic;
using UnityEngine;

internal sealed class RuntimeCityConfigCompositionSystemHelper
{
    private readonly List<GameObject> _emptyPrefabs = new();

    public RuntimeCityConfigCompositionSystemHelper()
    {
        Current = Snapshot.Default(_emptyPrefabs);
    }

    public Snapshot Current { get; private set; }

    public Snapshot Apply(RuntimeCitySpawnerSystemConfig config)
    {
        Current = Snapshot.From(config, _emptyPrefabs);
        return Current;
    }

    public readonly struct Snapshot
    {
        public readonly bool SpawnOnStart;
        public readonly bool GenerateBuildings;
        public readonly uint RandomSeed;
        public readonly int CityCount;
        public readonly Vector2Int StartCell;
        public readonly int GenerationYieldInterval;
        public readonly int GasStationCount;
        public readonly int ShopCount;
        public readonly int HouseCount;
        public readonly int OtherBuildingCount;
        public readonly int CityDecorationBuildingCount;
        public readonly int HallPlazaRadiusRoadCells;
        public readonly int ExtraTownRadiusRoadCells;
        public readonly int CityMinSpacingRoadCells;
        public readonly float RuralHouseRatio;
        public readonly int GasStationMinSpacingRoadCells;
        public readonly float HouseWallChance;
        public readonly int HouseWallMinDistanceCells;
        public readonly int HouseWallMaxDistanceCells;
        public readonly int LandmarkMinDistanceFromHallRoadCells;
        public readonly int LandmarkClearanceCells;
        public readonly int AutobahnMinLengthRoadCells;
        public readonly int AutobahnEdgeMarginRoadCells;
        public readonly int DefaultBuildingMaxHealth;
        public readonly GameObject ClockTowerPrefab;
        public readonly List<GameObject> FountainPrefabs;
        public readonly List<GameObject> MonumentPrefabs;
        public readonly List<GameObject> PillarPrefabs;
        public readonly List<GameObject> HallPrefabs;
        public readonly List<GameObject> GasStationPrefabs;
        public readonly List<GameObject> ShopPrefabs;
        public readonly List<GameObject> HousePrefabs;
        public readonly List<GameObject> OtherBuildingPrefabs;
        public readonly List<GameObject> CityDecorationPrefabs;
        public readonly List<GameObject> HouseWallPrefabs;
        public readonly GameObject HouseWallGatePrefab;
        public readonly GameObject HouseWallPillarPrefab;

        public Snapshot(
            bool spawnOnStart,
            bool generateBuildings,
            uint randomSeed,
            int cityCount,
            Vector2Int startCell,
            int generationYieldInterval,
            int gasStationCount,
            int shopCount,
            int houseCount,
            int otherBuildingCount,
            int cityDecorationBuildingCount,
            int hallPlazaRadiusRoadCells,
            int extraTownRadiusRoadCells,
            int cityMinSpacingRoadCells,
            float ruralHouseRatio,
            int gasStationMinSpacingRoadCells,
            float houseWallChance,
            int houseWallMinDistanceCells,
            int houseWallMaxDistanceCells,
            int landmarkMinDistanceFromHallRoadCells,
            int landmarkClearanceCells,
            int autobahnMinLengthRoadCells,
            int autobahnEdgeMarginRoadCells,
            int defaultBuildingMaxHealth,
            GameObject clockTowerPrefab,
            List<GameObject> fountainPrefabs,
            List<GameObject> monumentPrefabs,
            List<GameObject> pillarPrefabs,
            List<GameObject> hallPrefabs,
            List<GameObject> gasStationPrefabs,
            List<GameObject> shopPrefabs,
            List<GameObject> housePrefabs,
            List<GameObject> otherBuildingPrefabs,
            List<GameObject> cityDecorationPrefabs,
            List<GameObject> houseWallPrefabs,
            GameObject houseWallGatePrefab,
            GameObject houseWallPillarPrefab)
        {
            SpawnOnStart = spawnOnStart;
            GenerateBuildings = generateBuildings;
            RandomSeed = randomSeed;
            CityCount = cityCount;
            StartCell = startCell;
            GenerationYieldInterval = generationYieldInterval;
            GasStationCount = gasStationCount;
            ShopCount = shopCount;
            HouseCount = houseCount;
            OtherBuildingCount = otherBuildingCount;
            CityDecorationBuildingCount = cityDecorationBuildingCount;
            HallPlazaRadiusRoadCells = hallPlazaRadiusRoadCells;
            ExtraTownRadiusRoadCells = extraTownRadiusRoadCells;
            CityMinSpacingRoadCells = cityMinSpacingRoadCells;
            RuralHouseRatio = ruralHouseRatio;
            GasStationMinSpacingRoadCells = gasStationMinSpacingRoadCells;
            HouseWallChance = houseWallChance;
            HouseWallMinDistanceCells = houseWallMinDistanceCells;
            HouseWallMaxDistanceCells = houseWallMaxDistanceCells;
            LandmarkMinDistanceFromHallRoadCells = landmarkMinDistanceFromHallRoadCells;
            LandmarkClearanceCells = landmarkClearanceCells;
            AutobahnMinLengthRoadCells = autobahnMinLengthRoadCells;
            AutobahnEdgeMarginRoadCells = autobahnEdgeMarginRoadCells;
            DefaultBuildingMaxHealth = defaultBuildingMaxHealth;
            ClockTowerPrefab = clockTowerPrefab;
            FountainPrefabs = fountainPrefabs;
            MonumentPrefabs = monumentPrefabs;
            PillarPrefabs = pillarPrefabs;
            HallPrefabs = hallPrefabs;
            GasStationPrefabs = gasStationPrefabs;
            ShopPrefabs = shopPrefabs;
            HousePrefabs = housePrefabs;
            OtherBuildingPrefabs = otherBuildingPrefabs;
            CityDecorationPrefabs = cityDecorationPrefabs;
            HouseWallPrefabs = houseWallPrefabs;
            HouseWallGatePrefab = houseWallGatePrefab;
            HouseWallPillarPrefab = houseWallPillarPrefab;
        }

        public static Snapshot Default(List<GameObject> emptyPrefabs)
        {
            return new Snapshot(
                true,
                true,
                24681357,
                1,
                new Vector2Int(180, 180),
                0,
                3,
                20,
                32,
                8,
                16,
                2,
                5,
                16,
                0.35f,
                3,
                0.5f,
                2,
                4,
                3,
                4,
                8,
                3,
                300,
                null,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                emptyPrefabs,
                null,
                null);
        }

        public static Snapshot From(RuntimeCitySpawnerSystemConfig config, List<GameObject> emptyPrefabs)
        {
            if (config == null)
                return Default(emptyPrefabs);

            return new Snapshot(
                config.SpawnOnStart,
                config.GenerateBuildings,
                config.RandomSeed,
                config.CityCount,
                config.StartCell,
                config.GenerationYieldInterval,
                config.GasStationCount,
                config.ShopCount,
                config.HouseCount,
                config.OtherBuildingCount,
                config.CityDecorationBuildingCount,
                config.HallPlazaRadiusRoadCells,
                config.ExtraTownRadiusRoadCells,
                config.CityMinSpacingRoadCells,
                config.RuralHouseRatio,
                config.GasStationMinSpacingRoadCells,
                config.HouseWallChance,
                config.HouseWallMinDistanceCells,
                config.HouseWallMaxDistanceCells,
                config.LandmarkMinDistanceFromHallRoadCells,
                config.LandmarkClearanceCells,
                config.AutobahnMinLengthRoadCells,
                config.AutobahnEdgeMarginRoadCells,
                config.DefaultBuildingMaxHealth,
                config.ClockTowerPrefab,
                PrefabsOrEmpty(config.FountainPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.MonumentPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.PillarPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.HallPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.GasStationPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.ShopPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.HousePrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.OtherBuildingPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.CityDecorationPrefabs, emptyPrefabs),
                PrefabsOrEmpty(config.HouseWallPrefabs, emptyPrefabs),
                config.HouseWallGatePrefab,
                config.HouseWallPillarPrefab);
        }

        private static List<GameObject> PrefabsOrEmpty(List<GameObject> prefabs, List<GameObject> emptyPrefabs)
        {
            return prefabs ?? emptyPrefabs;
        }
    }
}
