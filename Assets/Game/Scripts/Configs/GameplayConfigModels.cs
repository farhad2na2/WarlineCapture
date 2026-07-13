using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Components;

namespace Game.Configs
{
    [Serializable]
    public sealed class BuildingProductionConfigEntry
    { [SerializeField] private GameObject spawnUnitPrefab;

        public GameObject SpawnUnitPrefab
        {
            get => spawnUnitPrefab;
            set => spawnUnitPrefab = value;
        }
    }

    public enum BuildingRole : byte
    {
        None = 0,
        House = 1,
        Shop = 2,
        CityHall = 3,
        TentRefugee = 4,
        MilitaryCamp = 5
    }

    public enum UnitAnimationKind : byte
    {
        Idle = 0,
        Aim = 1,
        Shoot = 2,
        Grenade = 3,
        Walk = 4,
        WalkAim = 5,
        WalkShoot = 6,
        Run = 7,
        RunAim = 8,
        RunShoot = 9,
        Reload = 10,
        Death01 = 11,
        Death02 = 12,
        Death03 = 13
    }

    [Serializable]
    public sealed class GameStringConfigEntry
    {
        [SerializeField] private string key;
        [TextArea, SerializeField] private string value;
        [SerializeField] private string audioEventId;

        public string Key => key;
        public string Value => value;
        public string AudioEventId => audioEventId;
    }

    public enum AIControllerRole : byte
    {
        Enemy = 0,
        PlayerAuto = 1
    }

    public enum AIControllerDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    [CreateAssetMenu(menuName = "Game/Config/AI Controller")]
    public class AIControllerConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private bool enabled = true;
        [SerializeField] private AIControllerRole role = AIControllerRole.Enemy;
        [SerializeField] private AIControllerDifficulty difficulty = AIControllerDifficulty.Normal;
        [SerializeField, Min(0)] private int factionId = 1;
        [SerializeField] private bool autoControlsPlayerFaction;

        [Header("Economy")]
        [SerializeField, Min(0)] private int startingMoney = 50000;
        [SerializeField, Min(0f)] private float incomeMultiplier = 1f;
        [SerializeField, Min(0)] private int oilSellPrice = 150;
        [SerializeField, Min(0)] private int fuelSellPrice = 220;

        [Header("Decision Timing")]
        [SerializeField, Min(0.1f)] private float buildIntervalSeconds = 8f;
        [SerializeField, Min(0.1f)] private float unitProductionIntervalSeconds = 6f;
        [SerializeField, Min(0.1f)] private float attackIntervalSeconds = 45f;

        [Header("Combat Policy")]
        [SerializeField, Min(0)] private int maxActiveAttackGroups = 2;
        [SerializeField, Min(0)] private int defenseRadiusCells = 40;
        [SerializeField, Range(0f, 1f)] private float aggression = 0.6f;

        [Header("Preferences")]
        [SerializeField] private List<string> preferredBuildingIds = new();
        [SerializeField] private List<string> preferredUnitIds = new();
        [SerializeField] private List<string> preferredVehicleIds = new();

        public bool Enabled => enabled;
        public AIControllerRole Role => role;
        public AIControllerDifficulty Difficulty => difficulty;
        public int FactionId => factionId;
        public bool AutoControlsPlayerFaction => autoControlsPlayerFaction;
        public int StartingMoney => startingMoney;
        public float IncomeMultiplier => incomeMultiplier;
        public int OilSellPrice => oilSellPrice;
        public int FuelSellPrice => fuelSellPrice;
        public float BuildIntervalSeconds => buildIntervalSeconds;
        public float UnitProductionIntervalSeconds => unitProductionIntervalSeconds;
        public float AttackIntervalSeconds => attackIntervalSeconds;
        public int MaxActiveAttackGroups => maxActiveAttackGroups;
        public int DefenseRadiusCells => defenseRadiusCells;
        public float Aggression => aggression;
        public List<string> PreferredBuildingIds => preferredBuildingIds;
        public List<string> PreferredUnitIds => preferredUnitIds;
        public List<string> PreferredVehicleIds => preferredVehicleIds;
    }

    [CreateAssetMenu(menuName = "Game/Config/Building Placement System")]
    public class BuildingPlacementSystemConfig : ScriptableObject, ICatalogPrefabSource
    { [SerializeField] private Camera worldCamera; [SerializeField] private GameObject roadPreviewPrefab; [SerializeField] private GameObject buildingSelectionMarkerPrefab; [Min(0.1f), SerializeField] private float buildButtonPreviewDistanceMultiplier = 1f; [Min(0.1f), SerializeField] private float unitCommandButtonPreviewDistanceMultiplier = 1f; [Min(0), SerializeField] private int maxQueuedUnitProductions = 25; [SerializeField] private List<GameObject> spawnables = new(); [SerializeField] private UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig; [SerializeField] private InitialUnitsSpawnerAuthoringConfig initialUnitsConfig; [SerializeField] private float buildPlaneY; [SerializeField] private float placementOutlineHeight = 0.15f; [SerializeField] private Color placementValidColor = new(0.15f, 0.85f, 0.2f, 1f); [SerializeField] private Color placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);

        public Camera WorldCamera => worldCamera;
        public GameObject RoadPreviewPrefab => roadPreviewPrefab;
        public GameObject BuildingSelectionMarkerPrefab => buildingSelectionMarkerPrefab;
        public float BuildButtonPreviewDistanceMultiplier => buildButtonPreviewDistanceMultiplier;
        public float UnitCommandButtonPreviewDistanceMultiplier => unitCommandButtonPreviewDistanceMultiplier;
        public int MaxQueuedUnitProductions => maxQueuedUnitProductions;
        public List<GameObject> Spawnables => spawnables;
        public UnitPrefabRegistryAuthoringConfig UnitPrefabRegistryConfig => unitPrefabRegistryConfig;
        public InitialUnitsSpawnerAuthoringConfig InitialUnitsConfig => initialUnitsConfig;
        public float BuildPlaneY => buildPlaneY;
        public float PlacementOutlineHeight => placementOutlineHeight;
        public Color PlacementValidColor => placementValidColor;
        public Color PlacementInvalidColor => placementInvalidColor;

        IReadOnlyList<GameObject> ICatalogPrefabSource.UnitSpawnPrefabs => unitPrefabRegistryConfig != null
            ? unitPrefabRegistryConfig.UnitSpawnPrefabs
            : null;
        IReadOnlyList<GameObject> ICatalogPrefabSource.BuildingSpawnPrefabs => spawnables;
    }

    [CreateAssetMenu(menuName = "Game/Config/Road Build System")]
    public class RoadBuildSystemConfig : ScriptableObject
    { [SerializeField] private Camera worldCamera; [SerializeField] private GameObject straightPrefab; [SerializeField] private GameObject tIntersectionPrefab; [SerializeField] private GameObject intersectionPrefab; [SerializeField] private GameObject endPrefab; [SerializeField] private GameObject cornerPrefab; [SerializeField] private GameObject autobahnPrefab; [SerializeField] private GameObject autobahnConnectPrefab; [SerializeField] private Vector3 gridOrigin = Vector3.zero; [SerializeField] private float buildPlaneY; [SerializeField] private float roadGridSize = 20f; [SerializeField] private int chunkSizeInCells = 8; [SerializeField] private float previewAlpha = 0.65f; [SerializeField] private GameObject soldierBasePrefab; [SerializeField] private Vector2Int soldierBaseFootprintCells = new(20, 20); [SerializeField] private float placementOutlineHeight = 0.15f; [SerializeField] private float placementOutlineWidth = 0.35f; [SerializeField] private Color placementValidColor = new(0.15f, 0.85f, 0.2f, 1f); [SerializeField] private Color placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);

        public Camera WorldCamera => worldCamera;
        public GameObject StraightPrefab => straightPrefab;
        public GameObject TIntersectionPrefab => tIntersectionPrefab;
        public GameObject IntersectionPrefab => intersectionPrefab;
        public GameObject EndPrefab => endPrefab;
        public GameObject CornerPrefab => cornerPrefab;
        public GameObject AutobahnPrefab => autobahnPrefab;
        public GameObject AutobahnConnectPrefab => autobahnConnectPrefab;
        public Vector3 GridOrigin => gridOrigin;
        public float BuildPlaneY => buildPlaneY;
        public float RoadGridSize => roadGridSize;
        public int ChunkSizeInCells => chunkSizeInCells;
        public float PreviewAlpha => previewAlpha;
        public GameObject SoldierBasePrefab => soldierBasePrefab;
        public Vector2Int SoldierBaseFootprintCells => soldierBaseFootprintCells;
        public float PlacementOutlineHeight => placementOutlineHeight;
        public float PlacementOutlineWidth => placementOutlineWidth;
        public Color PlacementValidColor => placementValidColor;
        public Color PlacementInvalidColor => placementInvalidColor;
    }

    [CreateAssetMenu(menuName = "Game/Config/RTS Selection System")]
    public class RTSSelectionSystemConfig : ScriptableObject
    { [SerializeField] private Camera worldCamera; [SerializeField] private GameObject moveOrderMarkerPrefab; [SerializeField, Min(0.01f)] private float orderMarkerVisibleSeconds = 1.25f; [SerializeField] private GameObject attackOrderMarkerPrefab; [SerializeField] private GameObject attackTargetMarkerPrefab; [SerializeField] private Color selectionFill = new(0.2f, 1f, 0.2f, 0.15f); [SerializeField] private Color selectionBorder = new(0.2f, 1f, 0.2f, 0.95f); [SerializeField] private float dragThresholdPixels = 8f; [SerializeField, Min(0.1f)] private float selectionModeHoldSeconds = 1f; [SerializeField] private float panSensitivity = 0.03f; [SerializeField] private float zoomSpeed = 20f; [SerializeField] private float minZoomHeight = 10f; [SerializeField] private float maxZoomHeight = 45f; [SerializeField] private float normalModeZoomHeight = 24f; [SerializeField] private float buildModeZoomHeight = 100f; [SerializeField] private float normalModePitch = 58f; [SerializeField] private float buildModePitch = 64f; [SerializeField] private float normalModeYaw = 10f; [SerializeField] private float buildModeYaw = 10f; [SerializeField] private float normalModeFieldOfView = 36f; [SerializeField] private float buildModeFieldOfView = 32f; [SerializeField] private float fullscreenIsoZoomHeight = 40f; [SerializeField] private float fullscreenIsoPitch = 82f; [SerializeField] private float fullscreenIsoYaw = 10f; [SerializeField] private float fullscreenIsoOrthographicSize = 24f; [SerializeField] private float zoomTransitionSmoothTime = 0.25f;

        public Camera WorldCamera => worldCamera;
        public GameObject MoveOrderMarkerPrefab => moveOrderMarkerPrefab;
        public float OrderMarkerVisibleSeconds => orderMarkerVisibleSeconds;
        public GameObject AttackOrderMarkerPrefab => attackOrderMarkerPrefab;
        public GameObject AttackTargetMarkerPrefab => attackTargetMarkerPrefab;
        public Color SelectionFill => selectionFill;
        public Color SelectionBorder => selectionBorder;
        public float DragThresholdPixels => dragThresholdPixels;
        public float SelectionModeHoldSeconds => selectionModeHoldSeconds;
        public float PanSensitivity => panSensitivity;
        public float ZoomSpeed => zoomSpeed;
        public float MinZoomHeight => minZoomHeight;
        public float MaxZoomHeight => maxZoomHeight;
        public float NormalModeZoomHeight => normalModeZoomHeight;
        public float BuildModeZoomHeight => buildModeZoomHeight;
        public float NormalModePitch => normalModePitch;
        public float BuildModePitch => buildModePitch;
        public float NormalModeYaw => normalModeYaw;
        public float BuildModeYaw => buildModeYaw;
        public float NormalModeFieldOfView => normalModeFieldOfView;
        public float BuildModeFieldOfView => buildModeFieldOfView;
        public float FullscreenIsoZoomHeight => fullscreenIsoZoomHeight;
        public float FullscreenIsoPitch => fullscreenIsoPitch;
        public float FullscreenIsoYaw => fullscreenIsoYaw;
        public float FullscreenIsoOrthographicSize => fullscreenIsoOrthographicSize;
        public float ZoomTransitionSmoothTime => zoomTransitionSmoothTime;
    }

    [CreateAssetMenu(menuName = "Game/Config/Runtime City Spawner")]
    public class RuntimeCitySpawnerSystemConfig : ScriptableObject
    { [SerializeField] private bool spawnOnStart = true; [SerializeField] private bool generateBuildings = true; [SerializeField] private uint randomSeed = 24681357; [SerializeField] private int cityCount = 1; [SerializeField] private Vector2Int startCell = new(180, 180); [SerializeField] private int generationYieldInterval; [SerializeField] private int gasStationCount = 3; [SerializeField] private int shopCount = 20; [SerializeField] private int houseCount = 32; [SerializeField] private int otherBuildingCount = 8; [SerializeField] private int cityDecorationBuildingCount = 16; [SerializeField] private int hallPlazaRadiusRoadCells = 2; [SerializeField] private int extraTownRadiusRoadCells = 5; [SerializeField] private int cityMinSpacingRoadCells = 16; [Range(0f, 1f), SerializeField] private float ruralHouseRatio = 0.35f; [SerializeField] private int gasStationMinSpacingRoadCells = 3; [Range(0f, 1f), SerializeField] private float houseWallChance = 0.5f; [SerializeField] private int houseWallMinDistanceCells = 2; [SerializeField] private int houseWallMaxDistanceCells = 4; [SerializeField] private int landmarkMinDistanceFromHallRoadCells = 3; [SerializeField] private int landmarkClearanceCells = 4; [SerializeField] private int autobahnMinLengthRoadCells = 8; [SerializeField] private int autobahnEdgeMarginRoadCells = 3; [SerializeField] private int defaultBuildingMaxHealth = 300; [SerializeField] private GameObject clockTowerPrefab; [SerializeField] private List<GameObject> fountainPrefabs = new(); [SerializeField] private List<GameObject> monumentPrefabs = new(); [SerializeField] private List<GameObject> pillarPrefabs = new(); [SerializeField] private List<GameObject> hallPrefabs = new(); [SerializeField] private List<GameObject> gasStationPrefabs = new(); [SerializeField] private List<GameObject> shopPrefabs = new(); [SerializeField] private List<GameObject> housePrefabs = new(); [SerializeField] private List<GameObject> otherBuildingPrefabs = new(); [SerializeField] private List<GameObject> cityDecorationPrefabs = new(); [SerializeField] private List<GameObject> houseWallPrefabs = new(); [SerializeField] private GameObject houseWallGatePrefab; [SerializeField] private GameObject houseWallPillarPrefab;

        public bool SpawnOnStart => spawnOnStart;
        public bool GenerateBuildings => generateBuildings;
        public uint RandomSeed => randomSeed;
        public int CityCount => cityCount;
        public Vector2Int StartCell => startCell;
        public int GenerationYieldInterval => generationYieldInterval;
        public int GasStationCount => gasStationCount;
        public int ShopCount => shopCount;
        public int HouseCount => houseCount;
        public int OtherBuildingCount => otherBuildingCount;
        public int CityDecorationBuildingCount => cityDecorationBuildingCount;
        public int HallPlazaRadiusRoadCells => hallPlazaRadiusRoadCells;
        public int ExtraTownRadiusRoadCells => extraTownRadiusRoadCells;
        public int CityMinSpacingRoadCells => cityMinSpacingRoadCells;
        public float RuralHouseRatio => ruralHouseRatio;
        public int GasStationMinSpacingRoadCells => gasStationMinSpacingRoadCells;
        public float HouseWallChance => houseWallChance;
        public int HouseWallMinDistanceCells => houseWallMinDistanceCells;
        public int HouseWallMaxDistanceCells => houseWallMaxDistanceCells;
        public int LandmarkMinDistanceFromHallRoadCells => landmarkMinDistanceFromHallRoadCells;
        public int LandmarkClearanceCells => landmarkClearanceCells;
        public int AutobahnMinLengthRoadCells => autobahnMinLengthRoadCells;
        public int AutobahnEdgeMarginRoadCells => autobahnEdgeMarginRoadCells;
        public int DefaultBuildingMaxHealth => defaultBuildingMaxHealth;
        public GameObject ClockTowerPrefab => clockTowerPrefab;
        public List<GameObject> FountainPrefabs => fountainPrefabs;
        public List<GameObject> MonumentPrefabs => monumentPrefabs;
        public List<GameObject> PillarPrefabs => pillarPrefabs;
        public List<GameObject> HallPrefabs => hallPrefabs;
        public List<GameObject> GasStationPrefabs => gasStationPrefabs;
        public List<GameObject> ShopPrefabs => shopPrefabs;
        public List<GameObject> HousePrefabs => housePrefabs;
        public List<GameObject> OtherBuildingPrefabs => otherBuildingPrefabs;
        public List<GameObject> CityDecorationPrefabs => cityDecorationPrefabs;
        public List<GameObject> HouseWallPrefabs => houseWallPrefabs;
        public GameObject HouseWallGatePrefab => houseWallGatePrefab;
        public GameObject HouseWallPillarPrefab => houseWallPillarPrefab;
    }

    [CreateAssetMenu(menuName = "Game/Config/Runtime Decoration Spawner")]
    public class RuntimeDecorationSpawnerSystemConfig : ScriptableObject
    { [SerializeField] private bool spawnOnStart = true; [SerializeField] private int decorationCount = 150; [SerializeField] private uint randomSeed = 12345; [Range(0f, 1f), SerializeField] private float treeSpawnRatio = 0.3f; [Min(1), SerializeField] private int treeClusterCount = 5; [Min(1), SerializeField] private int treeClusterSpacingMinCells = 2; [Min(1), SerializeField] private int treeClusterSpacingMaxCells = 5; [Min(0), SerializeField] private int treeClusterDistanceMinCells; [Min(1), SerializeField] private int treeClusterDistanceMaxCells = 12; [SerializeField] private float yPosition; [SerializeField] private List<GameObject> prefabs = new();

        public bool SpawnOnStart => spawnOnStart;
        public int DecorationCount => decorationCount;
        public uint RandomSeed => randomSeed;
        public float TreeSpawnRatio => treeSpawnRatio;
        public int TreeClusterCount => treeClusterCount;
        public int TreeClusterSpacingMinCells => treeClusterSpacingMinCells;
        public int TreeClusterSpacingMaxCells => treeClusterSpacingMaxCells;
        public int TreeClusterDistanceMinCells => treeClusterDistanceMinCells;
        public int TreeClusterDistanceMaxCells => treeClusterDistanceMaxCells;
        public float YPosition => yPosition;
        public List<GameObject> Prefabs => prefabs;
    }

    [CreateAssetMenu(menuName = "Game/Config/Runtime Grid Blocker System")]
    public class RuntimeGridBlockerSystemConfig : ScriptableObject
    { [SerializeField] private bool spawnOnStart = true; [SerializeField] private int blockerCount = 80; [SerializeField] private uint randomSeed = 24680; [Range(0f, 1f), SerializeField] private float treeSpawnRatio = 0.4f; [Min(1), SerializeField] private int treeClusterCount = 6; [Min(1), SerializeField] private int treeClusterSpacingMinCells = 2; [Min(1), SerializeField] private int treeClusterSpacingMaxCells = 6; [Min(0), SerializeField] private int treeClusterDistanceMinCells; [Min(1), SerializeField] private int treeClusterDistanceMaxCells = 14; [SerializeField] private float yPosition; [SerializeField] private List<GameObject> prefabs = new();

        public bool SpawnOnStart => spawnOnStart;
        public int BlockerCount => blockerCount;
        public uint RandomSeed => randomSeed;
        public float TreeSpawnRatio => treeSpawnRatio;
        public int TreeClusterCount => treeClusterCount;
        public int TreeClusterSpacingMinCells => treeClusterSpacingMinCells;
        public int TreeClusterSpacingMaxCells => treeClusterSpacingMaxCells;
        public int TreeClusterDistanceMinCells => treeClusterDistanceMinCells;
        public int TreeClusterDistanceMaxCells => treeClusterDistanceMaxCells;
        public float YPosition => yPosition;
        public List<GameObject> Prefabs => prefabs;
    }

    [CreateAssetMenu(menuName = "Game/Config/Day Night System")]
    public class DayNightSystemConfig : ScriptableObject
    { [SerializeField] private float fullDayDurationMinutes = 5f; [SerializeField] private float startHour = 9f; [SerializeField] private Light directionalLight; [SerializeField] private Volume globalVolume; [SerializeField] private float sunYaw = 170f; [SerializeField] private bool animateDirectionalLight; [SerializeField, Range(0f, 24f)] private float nightStartsAtHour = 19f; [SerializeField, Range(0f, 24f)] private float morningStartsAtHour = 6f; [SerializeField, Range(0f, 24f)] private float nightVisionStartHour = 19f; [SerializeField, Range(0f, 24f)] private float nightVisionEndHour = 6f; [SerializeField] private float nightVisionPostExposure = 2.2f; [SerializeField] private Color nightVisionColorFilter = new(0.55f, 1f, 0.58f, 1f); [SerializeField] private float nightVisionTemperature = -80f; [SerializeField] private float nightVisionTint = -55f; [SerializeField, Min(0f)] private float nightVisionBloomIntensity = 0.02f; [SerializeField, Min(0f)] private float nightVisionBloomThreshold = 2f; [SerializeField] private bool affectFog = true; [SerializeField] private bool affectVolume = true; [SerializeField] private bool updateDynamicGI = false; [SerializeField, Min(1f)] private float dynamicGIRefreshIntervalSeconds = 30f;

        public float FullDayDurationMinutes => fullDayDurationMinutes;
        public float StartHour => startHour;
        public Light DirectionalLight => directionalLight;
        public Volume GlobalVolume => globalVolume;
        public float SunYaw => sunYaw;
        public bool AnimateDirectionalLight => animateDirectionalLight;
        public float NightStartsAtHour => nightStartsAtHour;
        public float MorningStartsAtHour => morningStartsAtHour;
        public float NightVisionStartHour => nightVisionStartHour;
        public float NightVisionEndHour => nightVisionEndHour;
        public float NightVisionPostExposure => nightVisionPostExposure;
        public Color NightVisionColorFilter => nightVisionColorFilter;
        public float NightVisionTemperature => nightVisionTemperature;
        public float NightVisionTint => nightVisionTint;
        public float NightVisionBloomIntensity => nightVisionBloomIntensity;
        public float NightVisionBloomThreshold => nightVisionBloomThreshold;
        public bool AffectFog => affectFog;
        public bool AffectVolume => affectVolume;
        public bool UpdateDynamicGI => updateDynamicGI;
        public float DynamicGIRefreshIntervalSeconds => dynamicGIRefreshIntervalSeconds;
    }

    [CreateAssetMenu(menuName = "Game/Config/Unit Attack Trace System")]
    public class UnitAttackTraceSystemConfig : ScriptableObject
    { [SerializeField] private Camera worldCamera; [SerializeField] private float sourceHeightOffset = 0.9f; [SerializeField] private float targetHeightOffset = 0.9f; [SerializeField, Min(0f)] private float sourceForwardOffset = 0.45f; [SerializeField] private Shader traceShader;

        public Camera WorldCamera => worldCamera;
        public float SourceHeightOffset => sourceHeightOffset;
        public float TargetHeightOffset => targetHeightOffset;
        public float SourceForwardOffset => Mathf.Max(0f, sourceForwardOffset);
        public Shader TraceShader => traceShader;
    }

    [CreateAssetMenu(menuName = "Game/Config/Game Strings")]
    public class GameStringsConfig : ScriptableObject
    {
        [SerializeField] private List<GameStringConfigEntry> entries = new();

        public List<GameStringConfigEntry> Entries => entries;
    }

    [CreateAssetMenu(menuName = "Game/Config/Prefab Preview Camera")]
    public class PrefabPreviewCameraConfig : ScriptableObject
    {
        [Header("Character Preview Model")]
        [SerializeField] private Vector3 characterModelPosition = new(-2f, 0f, 0f);
        [SerializeField] private Vector3 characterModelRotationEuler = Vector3.zero;

        [Header("Character Preview Camera")]
        [SerializeField] private Vector3 characterCameraPosition = new(-2.019521f, 1.569489f, 0.6451559f);
        [SerializeField] private Vector3 characterCameraRotationEuler = new(-5.722f, 178.278f, 0f);
        [SerializeField, Min(0f)] private float characterCarouselRadius = 1.2f;
        [SerializeField, Min(0.01f)] private float characterTargetHeight = 1.8f;

        [Header("Vehicle Preview Model")]
        [SerializeField] private Vector3 vehicleModelPosition = Vector3.zero;
        [SerializeField] private Vector3 vehicleModelRotationEuler = Vector3.zero;

        [Header("Vehicle Preview Camera")]
        [SerializeField] private Vector3 vehicleCameraPosition = new(0f, 2f, 6f);
        [SerializeField] private Vector3 vehicleCameraRotationEuler = new(10f, 180f, 0f);
        [SerializeField, Min(0f)] private float vehicleCarouselRadius = 2f;
        [SerializeField, Min(0.01f)] private float vehicleTargetHeight = 2.2f;

        [Header("Building Preview Model")]
        [SerializeField] private Vector3 buildingModelPosition = Vector3.zero;
        [SerializeField] private Vector3 buildingModelRotationEuler = Vector3.zero;

        [Header("Building Preview Camera")]
        [SerializeField] private Vector3 buildingCameraPosition = new(0f, 4f, 10f);
        [SerializeField] private Vector3 buildingCameraRotationEuler = new(18f, 145f, 0f);
        [SerializeField, Min(0f)] private float buildingCarouselRadius = 4f;
        [SerializeField, Min(0.01f)] private float buildingTargetHeight = 3f;

        public Vector3 CharacterModelPosition => characterModelPosition;
        public Quaternion CharacterModelRotation => Quaternion.Euler(characterModelRotationEuler);
        public Vector3 CharacterCameraPosition => characterCameraPosition;
        public Quaternion CharacterCameraRotation => Quaternion.Euler(characterCameraRotationEuler);
        public float CharacterCarouselRadius => characterCarouselRadius;
        public float CharacterTargetHeight => Mathf.Max(0.01f, characterTargetHeight);
        public Vector3 VehicleModelPosition => vehicleModelPosition;
        public Quaternion VehicleModelRotation => Quaternion.Euler(vehicleModelRotationEuler);
        public Vector3 VehicleCameraPosition => vehicleCameraPosition;
        public Quaternion VehicleCameraRotation => Quaternion.Euler(vehicleCameraRotationEuler);
        public float VehicleCarouselRadius => vehicleCarouselRadius;
        public float VehicleTargetHeight => Mathf.Max(0.01f, vehicleTargetHeight);
        public Vector3 BuildingModelPosition => buildingModelPosition;
        public Quaternion BuildingModelRotation => Quaternion.Euler(buildingModelRotationEuler);
        public Vector3 BuildingCameraPosition => buildingCameraPosition;
        public Quaternion BuildingCameraRotation => Quaternion.Euler(buildingCameraRotationEuler);
        public float BuildingCarouselRadius => buildingCarouselRadius;
        public float BuildingTargetHeight => Mathf.Max(0.01f, buildingTargetHeight);
    }

    [CreateAssetMenu(menuName = "Game/Config/Grid Authoring")]
    public class GridAuthoringConfig : ScriptableObject
    { [SerializeField] private int width = 16; [SerializeField] private int height = 16; [SerializeField] private float cellSize = 1f; [SerializeField] private Vector3 origin = Vector3.zero; [SerializeField] private Vector2Int[] blockedCells; [SerializeField] private bool drawGrid = true; [SerializeField] private bool drawWhenNotSelected = true; [SerializeField] private bool drawRuntimeDebugInPlayMode = true; [SerializeField] private bool fillWalkableCells; [SerializeField] private bool fillRoadCells = true; [SerializeField] private bool fillSidewalkCells = true; [SerializeField] private float roadCellDebugScale = 0.35f; [SerializeField] private bool fillBuildingCells = true; [SerializeField] private bool fillRuntimeBlockerCells = true; [SerializeField] private bool fillVehicleFootprintCells = true; [SerializeField] private bool drawUnitPaths = true; [SerializeField] private int maxGridLinesPerAxis = 256; [SerializeField] private int maxFilledDebugCells = 250000; [SerializeField] private Color gridLineColor = new(1f, 1f, 1f, 0.15f); [SerializeField] private Color walkableFillColor = new(0.2f, 1f, 0.2f, 0.05f); [SerializeField] private Color roadFillColor = new(0.2f, 0.7f, 1f, 0.28f); [SerializeField] private Color sidewalkFillColor = new(0.2f, 0.85f, 0.25f, 0.5f); [SerializeField] private Color buildingFillColor = new(1f, 0.65f, 0.2f, 0.3f); [SerializeField] private Color runtimeBlockerFillColor = new(0.18f, 0.18f, 0.18f, 0.55f); [SerializeField] private Color vehicleFootprintFillColor = new(0.08f, 0.5f, 0.82f, 0.4f); [SerializeField] private Color unitPathColor = new(0.15f, 1f, 0.9f, 0.9f); [SerializeField] private Color stuckUnitPathColor = new(1f, 0.15f, 0.15f, 0.95f); [SerializeField] private Color blockedFillColor = new(1f, 0.2f, 0.2f, 0.25f);

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public Vector3 Origin => origin;
        public Vector2Int[] BlockedCells => blockedCells;
        public bool DrawGrid => drawGrid;
        public bool DrawWhenNotSelected => drawWhenNotSelected;
        public bool DrawRuntimeDebugInPlayMode => drawRuntimeDebugInPlayMode;
        public bool FillWalkableCells => fillWalkableCells;
        public bool FillRoadCells => fillRoadCells;
        public bool FillSidewalkCells => fillSidewalkCells;
        public float RoadCellDebugScale => roadCellDebugScale;
        public bool FillBuildingCells => fillBuildingCells;
        public bool FillRuntimeBlockerCells => fillRuntimeBlockerCells;
        public bool FillVehicleFootprintCells => fillVehicleFootprintCells;
        public bool DrawUnitPaths => drawUnitPaths;
        public int MaxGridLinesPerAxis => maxGridLinesPerAxis;
        public int MaxFilledDebugCells => maxFilledDebugCells;
        public Color GridLineColor => gridLineColor;
        public Color WalkableFillColor => walkableFillColor;
        public Color RoadFillColor => roadFillColor;
        public Color SidewalkFillColor => sidewalkFillColor;
        public Color BuildingFillColor => buildingFillColor;
        public Color RuntimeBlockerFillColor => runtimeBlockerFillColor;
        public Color VehicleFootprintFillColor => vehicleFootprintFillColor;
        public Color UnitPathColor => unitPathColor;
        public Color StuckUnitPathColor => stuckUnitPathColor;
        public Color BlockedFillColor => blockedFillColor;
    }

    [CreateAssetMenu(menuName = "Game/Config/Static Grid Blocker Authoring")]
    public class StaticGridBlockerAuthoringConfig : ScriptableObject
    { [SerializeField] private Vector2Int cell = new(5, 5); [SerializeField] private Vector2Int size = new(1, 1);

        public Vector2Int Cell => cell;
        public Vector2Int Size => size;
    }

    public enum MaterialFabricationConfigValidationCode : byte
    {
        Valid = 0,
        MissingOilInputCapacity = 1,
        InvalidOilConsumption = 2,
        InvalidMaterialsOutput = 3,
        InvalidCycleDuration = 4,
        UnsupportedOutputCapacityPolicy = 5
    }

    [CreateAssetMenu(menuName = "Game/Config/Building Definition Authoring")]
    public class BuildingDefinitionAuthoringConfig : ScriptableObject
    { [SerializeField] private string displayName = "Building";
        [TextArea, SerializeField] private string description = "Operational building."; [SerializeField] private Sprite portraitSprite; [SerializeField] private Sprite portraitCardSprite; [SerializeField] private Sprite portraitActionSprite; [SerializeField] private int maxHealth = 500; [SerializeField] private BuildingRole role; [SerializeField] private bool isWall; [SerializeField] private bool canRequest = true; [SerializeField, Min(0)] private int price = 20000; [SerializeField, Min(0)] private int materialsCost; [SerializeField, Min(0.01f)] private float productionDurationSeconds = 30f; [SerializeField, Min(0f)] private float oilBarrelsPerDay; [SerializeField, Min(0)] private int oilStorageCapacity; [SerializeField, Min(0f)] private float fuelBarrelsPerDay; [SerializeField, Min(0)] private int fuelStorageCapacity; [Header("Material Fabrication")] [SerializeField] private bool materialFabricationEnabled; [SerializeField, Min(0f)] private float materialFabricationOilConsumedPerCycle; [SerializeField, Min(0)] private int materialFabricationMaterialsOutputPerCycle; [SerializeField, Min(0.01f)] private float materialFabricationCycleDurationSeconds = 30f; [SerializeField] private MaterialFabricationOutputCapacityPolicyCode materialFabricationOutputCapacityPolicy; [Header("Refugees")] [SerializeField, Min(0)] private int refugeeCapacity; [SerializeField, Min(0)] private int refugeeUpkeepPerCitizenPerDay; [Header("Threat Detection")] [SerializeField] private ThreatDetectionKind threatDetectionKind; [SerializeField, Min(0)] private int threatDetectionRadiusCells; [Header("Defense")] [SerializeField] private bool canAttack; [SerializeField, Min(1)] private int maxConcurrentAttacks = 1; [SerializeField, Min(0f)] private float attackRange; [SerializeField, Min(0.01f)] private float attackCooldownSeconds = 1f; [SerializeField, Min(0)] private int attackDamage; [SerializeField] private GameObject attackImpactPrefab; [SerializeField] private GameObject muzzleFlashPrefab; [SerializeField, Min(0f)] private float muzzleFlashHeightOffset = 0.95f; [SerializeField, Min(0f)] private float muzzleFlashForwardOffset = 0.5f; [SerializeField] private Color attackTraceColor = new(1f, 0.62f, 0.25f, 1f); [SerializeField, Min(0.01f)] private float attackTraceWidth = 0.14f; [SerializeField, Min(0.1f)] private float attackTraceScrollSpeed = 24f; [SerializeField, Min(1f)] private float attackTraceDashDensity = 4f; [SerializeField, Min(0.01f)] private float attackTraceVisibleSeconds = 0.1f; [SerializeField, Min(1)] private int attackTracerEveryNthShot = 3; [Header("Destroyed Visual")] [SerializeField] private GameObject destroyedVisualPrefab; [SerializeField] private List<BuildingProductionConfigEntry> productions = new();

        private void OnValidate()
        {
            if (price <= 0)
                price = 20000;
        }

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite PortraitSprite => portraitSprite;
        public Sprite PortraitCardSprite => portraitCardSprite;
        public Sprite PortraitActionSprite => portraitActionSprite;
        public int MaxHealth => maxHealth;
        public BuildingRole Role => role;
        public bool IsWall => isWall;
        public bool CanRequest => canRequest;
        public int Price => Mathf.Max(0, price);
        public int MaterialsCost => Mathf.Max(0, materialsCost);
        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public float OilBarrelsPerDay => Mathf.Max(0f, oilBarrelsPerDay);
        public int OilStorageCapacity => Mathf.Max(0, oilStorageCapacity);
        public float FuelBarrelsPerDay => Mathf.Max(0f, fuelBarrelsPerDay);
        public int FuelStorageCapacity => Mathf.Max(0, fuelStorageCapacity);
        public bool MaterialFabricationEnabled => materialFabricationEnabled;
        public float MaterialFabricationOilConsumedPerCycle => Mathf.Max(0f, materialFabricationOilConsumedPerCycle);
        public int MaterialFabricationMaterialsOutputPerCycle => Mathf.Max(0, materialFabricationMaterialsOutputPerCycle);
        public float MaterialFabricationCycleDurationSeconds => Mathf.Max(0.01f, materialFabricationCycleDurationSeconds);
        public MaterialFabricationOutputCapacityPolicyCode MaterialFabricationOutputCapacityPolicy => materialFabricationOutputCapacityPolicy;

        public MaterialFabricationConfigValidationCode ValidateMaterialFabricationConfiguration()
        {
            if (!materialFabricationEnabled)
                return MaterialFabricationConfigValidationCode.Valid;
            if (oilStorageCapacity <= 0)
                return MaterialFabricationConfigValidationCode.MissingOilInputCapacity;
            if (!float.IsFinite(materialFabricationOilConsumedPerCycle) ||
                materialFabricationOilConsumedPerCycle <= 0f)
                return MaterialFabricationConfigValidationCode.InvalidOilConsumption;
            if (materialFabricationMaterialsOutputPerCycle <= 0)
                return MaterialFabricationConfigValidationCode.InvalidMaterialsOutput;
            if (!float.IsFinite(materialFabricationCycleDurationSeconds) ||
                materialFabricationCycleDurationSeconds <= 0f)
                return MaterialFabricationConfigValidationCode.InvalidCycleDuration;
            if (materialFabricationOutputCapacityPolicy !=
                MaterialFabricationOutputCapacityPolicyCode.RequireFullCycleCapacity)
                return MaterialFabricationConfigValidationCode.UnsupportedOutputCapacityPolicy;

            return MaterialFabricationConfigValidationCode.Valid;
        }
        public int RefugeeCapacity => Mathf.Max(0, refugeeCapacity);
        public int RefugeeUpkeepPerCitizenPerDay => Mathf.Max(0, refugeeUpkeepPerCitizenPerDay);
        public ThreatDetectionKind ThreatDetectionKind => threatDetectionKind;
        public int ThreatDetectionRadiusCells => Mathf.Max(0, threatDetectionRadiusCells);
        public bool CanAttack => canAttack;
        public int MaxConcurrentAttacks => Mathf.Max(1, maxConcurrentAttacks);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public float AttackCooldownSeconds => Mathf.Max(0.01f, attackCooldownSeconds);
        public int AttackDamage => Mathf.Max(0, attackDamage);
        public GameObject AttackImpactPrefab => attackImpactPrefab;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public float MuzzleFlashHeightOffset => Mathf.Max(0f, muzzleFlashHeightOffset);
        public float MuzzleFlashForwardOffset => Mathf.Max(0f, muzzleFlashForwardOffset);
        public Color AttackTraceColor => attackTraceColor;
        public float AttackTraceWidth => Mathf.Max(0.01f, attackTraceWidth);
        public float AttackTraceScrollSpeed => Mathf.Max(0.1f, attackTraceScrollSpeed);
        public float AttackTraceDashDensity => Mathf.Max(1f, attackTraceDashDensity);
        public float AttackTraceVisibleSeconds => Mathf.Max(0.01f, attackTraceVisibleSeconds);
        public int AttackTracerEveryNthShot => Mathf.Max(1, attackTracerEveryNthShot);
        public GameObject DestroyedVisualPrefab => destroyedVisualPrefab;
        public List<BuildingProductionConfigEntry> Productions => productions;
    }

    [CreateAssetMenu(menuName = "Game/Config/Faction Visual Settings")]
    public class FactionVisualSettingsConfig : ScriptableObject
    { [SerializeField] private Color playerColor = new(0.12f, 0.72f, 1f, 1f); [SerializeField] private Color enemyColor = new(1f, 0.35f, 0.2f, 1f); [SerializeField] private Color neutralColor = new(0.82f, 0.82f, 0.82f, 1f); [Range(0f, 1f), SerializeField] private float buildingFactionTintStrength = 0.45f;

        public Color PlayerColor => playerColor;
        public Color EnemyColor => enemyColor;
        public Color NeutralColor => neutralColor;
        public float BuildingFactionTintStrength => Mathf.Clamp01(buildingFactionTintStrength);
    }

    [CreateAssetMenu(menuName = "Game/Config/Unit Health Bar")]
    public class UnitHealthBarConfig : ScriptableObject
    {
        [Range(0f, 1f), SerializeField] private float defaultFill = 1f;

        public float DefaultFill => defaultFill;
    }

    [CreateAssetMenu(menuName = "Game/Config/Faction Tint Target")]
    public class FactionTintTargetConfig : ScriptableObject
    { [SerializeField] private Color defaultColor = Color.white;

        public Color DefaultColor => defaultColor;
    }

    [CreateAssetMenu(menuName = "Game/Config/Unit Grid Authoring")]
    public class UnitGridAuthoringConfig : ScriptableObject
    {
        [SerializeField] private Sprite portraitSprite; [SerializeField] private Sprite portraitCardSprite; [SerializeField] private Sprite portraitActionSprite; [SerializeField] private Sprite weaponSprite; [SerializeField] private string weaponDisplayName; [SerializeField] private bool allowIdleWander = true; [SerializeField] private bool autoCalculateFootprint; [SerializeField] private Vector2Int footprintCells = new(1, 1); [SerializeField] private bool usesVehicleMotion; [SerializeField] private bool isAirUnit; [SerializeField] private bool canRequest = true; [SerializeField, Min(0)] private int price; [SerializeField] private float productionDurationSeconds = 60f; [SerializeField] private GameObject productionTransportPrefab; [SerializeField] private bool isProductionTransportUnit; [SerializeField] private float productionTransportArrivalSeconds = 5f; [SerializeField] private float productionTransportHoldForNextReadySeconds = 4f; [SerializeField, Min(1)] private int productionTransportMaxConcurrent = 1; [SerializeField] private bool productionTransportRequiresAirportRunway; [SerializeField] private bool productionTransportUsesRunwayLanding; [SerializeField, Min(0)] private int soldierTransportCapacity; [SerializeField, Min(0)] private int vehicleTransportCapacity; [SerializeField, Min(0)] private int cargoWeightCapacity; [SerializeField, Min(0f)] private float transportCruiseHeight; [SerializeField] private GameObject soldierParachuteVisualPrefab; [SerializeField] private GameObject vehicleEmergencyDropVisualPrefab; [SerializeField, Min(0.01f)] private float runwayTaxiSpeed = 5f; [SerializeField] private float speed = 5f; [SerializeField] private float walkSpeed = 2f; [SerializeField] private float roadSpeedMultiplier = 1.2f; [SerializeField] private float arriveDistance = 0.05f; [SerializeField] private float groundOffset; [SerializeField, Min(0f)] private float groundFuelPerCell; [SerializeField, Min(0f)] private float airFuelPerCell; [Header("Identity")] [SerializeField] private string displayName; [TextArea, SerializeField] private string description; [Header("LOD")] [SerializeField] private GameObject midLodPrefab; [SerializeField] private GameObject lowLodPrefab; [Header("Unit Visuals")] [SerializeField] private GameObject unitSelectionMarkerPrefab; [SerializeField] private GameObject unitHealthBarPrefab; [SerializeField] private bool tintUnitModelRenderers = true; [Header("Vehicle Visuals")] [SerializeField] private GameObject vehicleDestroyedVisualPrefab; [SerializeField] private GameObject vehicleSelectionMarkerPrefab; [SerializeField] private GameObject vehicleHealthBarPrefab; [SerializeField] private bool tintVehicleModelRenderers = true; [Header("Resource Hauler")] [SerializeField, Min(0)] private int resourceHaulerBarrelCapacity; [SerializeField, Min(0.01f)] private float resourceHaulerFillDurationSeconds = 2f; [SerializeField, Min(0.01f)] private float resourceHaulerUnloadDurationSeconds = 1.5f; [Header("Threat Detection")] [SerializeField] private ThreatDetectionKind threatDetectionKind; [SerializeField, Min(0)] private int threatDetectionRadiusCells; [SerializeField] private bool canAttack = true; [SerializeField] private bool allowAutoEngage = true; [SerializeField] private bool usesTurretAim; [SerializeField] private int aggroRangeCells = 6; [SerializeField] private float attackRange = 2f; [SerializeField] private float chaseBreakDistance = 8f; [SerializeField] private float attackCooldownSeconds = 1f; [SerializeField] private int attackDamage = 10; [SerializeField] private int maxHealth = 100; [SerializeField] private GameObject attackImpactPrefab; [SerializeField] private GameObject muzzleFlashPrefab; [SerializeField, Min(0f)] private float muzzleFlashHeightOffset = 0.9f; [SerializeField, Min(0f)] private float muzzleFlashForwardOffset = 0.45f; [Header("Missile Launcher")] [SerializeField] private GroundMissileLauncherConfig groundMissileLauncherConfig; [SerializeField] private Color attackTraceColor = new(1f, 0.85f, 0.2f, 1f); [SerializeField] private float attackTraceWidth = 0.18f; [SerializeField] private float attackTraceScrollSpeed = 10f; [SerializeField] private float attackTraceDashDensity = 10f; [SerializeField] private float attackTraceVisibleSeconds = 0.08f; [SerializeField, Min(1)] private int attackTracerEveryNthShot = 1; [SerializeField] private float idleDelayMinSeconds = 5f; [SerializeField] private float idleDelayMaxSeconds = 7f; [SerializeField] private float idleWanderDistanceMin = 3f; [SerializeField] private float idleWanderDistanceMax = 5f; [SerializeField] private float attackAnimationSeconds = 0.25f; [SerializeField] private float deathAnimationSeconds = 1.25f; [SerializeField] private List<UnitAnimationKind> animationOrder = new();
        [SerializeField] private AirMissileLauncherConfig airMissileLauncherConfig;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;
            usesVehicleMotion = ResolveDefaultUsesVehicleMotion(name, isAirUnit, soldierTransportCapacity, resourceHaulerBarrelCapacity);
            if (price <= 0)
                price = ResolveDefaultPrice(name, footprintCells);
            if (soldierTransportCapacity <= 0)
                soldierTransportCapacity = ResolveDefaultSoldierTransportCapacity(name);
            if (vehicleTransportCapacity <= 0)
                vehicleTransportCapacity = ResolveDefaultVehicleTransportCapacity(name);
            if (cargoWeightCapacity <= 0)
                cargoWeightCapacity = ResolveDefaultCargoWeightCapacity(name);
            if (transportCruiseHeight <= 0f)
                transportCruiseHeight = ResolveDefaultTransportCruiseHeight(name);
        }

        private static int ResolveDefaultPrice(string assetName, Vector2Int footprint)
        {
            bool isVehicle = footprint.x > 1 || footprint.y > 1 || (!string.IsNullOrWhiteSpace(assetName) && assetName.IndexOf("Veh", StringComparison.OrdinalIgnoreCase) >= 0);
            return isVehicle ? 15000 : 10000;
        }

        private static int ResolveDefaultSoldierTransportCapacity(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return 0;

            if (IsTransportPlaneAssetName(assetName))
                return 24;

            return assetName.IndexOf("Unit_Veh_APC_Fast", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("Unit_Veh_APC_Heavy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("Unit_Veh_APC_Slow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("Unit_Veh_Truck_Canopy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("Unit_Veh_Helicopter_Transport", StringComparison.OrdinalIgnoreCase) >= 0
                ? 10
                : 0;
        }

        private static int ResolveDefaultVehicleTransportCapacity(string assetName)
        {
            return IsTransportPlaneAssetName(assetName) ? 2 : 0;
        }

        private static int ResolveDefaultCargoWeightCapacity(string assetName)
        {
            return 0;
        }

        private static float ResolveDefaultTransportCruiseHeight(string assetName)
        {
            return IsTransportPlaneAssetName(assetName) ? 55f : 0f;
        }

        private static bool IsTransportPlaneAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return false;

            return assetName.IndexOf("Unit_Veh_Plane_Transport", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("SM_Veh_TransportPlane", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("Plane_Transport", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ResolveDefaultUsesVehicleMotion(string assetName, bool airUnit, int transportCapacity, int haulerCapacity)
        {
            if (airUnit || transportCapacity > 0 || haulerCapacity > 0)
                return true;

            if (string.IsNullOrWhiteSpace(assetName))
                return false;

            return assetName.IndexOf("Unit_Veh_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetName.IndexOf("_Veh_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float ResolveDefaultGroundFuelPerCell(string assetName, bool airUnit, bool vehicleMotion, int transportCapacity, int haulerCapacity)
        {
            if (airUnit)
                return 0f;

            bool isVehicle = vehicleMotion || ResolveDefaultUsesVehicleMotion(assetName, airUnit, transportCapacity, haulerCapacity);
            return isVehicle ? 0.05f : 0f;
        }

        private static float ResolveDefaultAirFuelPerCell(bool airUnit)
        {
            return airUnit ? 0.25f : 0f;
        }

        public Sprite WeaponSprite => weaponSprite;
        public Sprite PortraitSprite => portraitSprite;
        public Sprite PortraitCardSprite => portraitCardSprite;
        public Sprite PortraitActionSprite => portraitActionSprite;
        public string WeaponDisplayName => weaponDisplayName;
        public bool AllowIdleWander => allowIdleWander;
        public bool AutoCalculateFootprint => autoCalculateFootprint;
        public Vector2Int FootprintCells => footprintCells;
        public bool UsesVehicleMotion => usesVehicleMotion || ResolveDefaultUsesVehicleMotion(name, isAirUnit, soldierTransportCapacity, resourceHaulerBarrelCapacity);
        public bool IsAirUnit => isAirUnit;
        public bool CanRequest => canRequest;
        public int Price => Mathf.Max(0, price > 0 ? price : ResolveDefaultPrice(name, footprintCells));
        public float ProductionDurationSeconds => productionDurationSeconds;
        public GameObject ProductionTransportPrefab => productionTransportPrefab;
        public bool IsProductionTransportUnit => isProductionTransportUnit;
        public float ProductionTransportArrivalSeconds => productionTransportArrivalSeconds;
        public float ProductionTransportHoldForNextReadySeconds => productionTransportHoldForNextReadySeconds;
        public int ProductionTransportMaxConcurrent => Mathf.Max(1, productionTransportMaxConcurrent);
        public bool ProductionTransportRequiresAirportRunway => productionTransportRequiresAirportRunway;
        public bool ProductionTransportUsesRunwayLanding => productionTransportUsesRunwayLanding;
        public int SoldierTransportCapacity => Mathf.Max(0, soldierTransportCapacity > 0 ? soldierTransportCapacity : ResolveDefaultSoldierTransportCapacity(name));
        public int VehicleTransportCapacity => Mathf.Max(0, vehicleTransportCapacity > 0 ? vehicleTransportCapacity : ResolveDefaultVehicleTransportCapacity(name));
        public int CargoWeightCapacity => Mathf.Max(0, cargoWeightCapacity > 0 ? cargoWeightCapacity : ResolveDefaultCargoWeightCapacity(name));
        public float TransportCruiseHeight => Mathf.Max(0f, transportCruiseHeight > 0f ? transportCruiseHeight : ResolveDefaultTransportCruiseHeight(name));
        public GameObject SoldierParachuteVisualPrefab => soldierParachuteVisualPrefab;
        public GameObject VehicleEmergencyDropVisualPrefab => vehicleEmergencyDropVisualPrefab;
        public float RunwayTaxiSpeed => Mathf.Max(0.01f, runwayTaxiSpeed);
        public float Speed => speed;
        public float WalkSpeed => walkSpeed;
        public float RoadSpeedMultiplier => roadSpeedMultiplier;
        public float ArriveDistance => arriveDistance;
        public float GroundOffset => groundOffset;
        public float GroundFuelPerCell => Mathf.Max(0f, groundFuelPerCell > 0f ? groundFuelPerCell : ResolveDefaultGroundFuelPerCell(name, isAirUnit, usesVehicleMotion, soldierTransportCapacity, resourceHaulerBarrelCapacity));
        public float AirFuelPerCell => Mathf.Max(0f, airFuelPerCell > 0f ? airFuelPerCell : ResolveDefaultAirFuelPerCell(isAirUnit));
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public GameObject MidLodPrefab => midLodPrefab;
        public GameObject LowLodPrefab => lowLodPrefab != null ? lowLodPrefab : midLodPrefab;
        public GameObject UnitSelectionMarkerPrefab => unitSelectionMarkerPrefab != null ? unitSelectionMarkerPrefab : vehicleSelectionMarkerPrefab;
        public GameObject UnitHealthBarPrefab => unitHealthBarPrefab != null ? unitHealthBarPrefab : vehicleHealthBarPrefab;
        public bool TintUnitModelRenderers => tintUnitModelRenderers || tintVehicleModelRenderers;
        public GameObject VehicleDestroyedVisualPrefab => vehicleDestroyedVisualPrefab;
        public GameObject VehicleSelectionMarkerPrefab => vehicleSelectionMarkerPrefab;
        public GameObject VehicleHealthBarPrefab => vehicleHealthBarPrefab;
        public bool TintVehicleModelRenderers => tintVehicleModelRenderers;
        public int ResourceHaulerBarrelCapacity => Mathf.Max(0, resourceHaulerBarrelCapacity);
        public float ResourceHaulerFillDurationSeconds => Mathf.Max(0.01f, resourceHaulerFillDurationSeconds);
        public float ResourceHaulerUnloadDurationSeconds => Mathf.Max(0.01f, resourceHaulerUnloadDurationSeconds);
        public ThreatDetectionKind ThreatDetectionKind => threatDetectionKind;
        public int ThreatDetectionRadiusCells => Mathf.Max(0, threatDetectionRadiusCells);
        public bool CanAttack => canAttack;
        public bool AllowAutoEngage => allowAutoEngage;
        public bool UsesTurretAim => usesTurretAim;
        public int AggroRangeCells => aggroRangeCells;
        public float AttackRange => attackRange;
        public float ChaseBreakDistance => chaseBreakDistance;
        public float AttackCooldownSeconds => attackCooldownSeconds;
        public int AttackDamage => attackDamage;
        public int MaxHealth => maxHealth;
        public GameObject AttackImpactPrefab => attackImpactPrefab;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public float MuzzleFlashHeightOffset => Mathf.Max(0f, muzzleFlashHeightOffset);
        public float MuzzleFlashForwardOffset => Mathf.Max(0f, muzzleFlashForwardOffset);
        public GroundMissileLauncherConfig GroundMissileLauncherConfig => groundMissileLauncherConfig;
        public AirMissileLauncherConfig AirMissileLauncherConfig => airMissileLauncherConfig;
        public Color AttackTraceColor => attackTraceColor;
        public float AttackTraceWidth => attackTraceWidth;
        public float AttackTraceScrollSpeed => attackTraceScrollSpeed;
        public float AttackTraceDashDensity => attackTraceDashDensity;
        public float AttackTraceVisibleSeconds => attackTraceVisibleSeconds;
        public int AttackTracerEveryNthShot => Mathf.Max(1, attackTracerEveryNthShot);
        public float IdleDelayMinSeconds => idleDelayMinSeconds;
        public float IdleDelayMaxSeconds => idleDelayMaxSeconds;
        public float IdleWanderDistanceMin => idleWanderDistanceMin;
        public float IdleWanderDistanceMax => idleWanderDistanceMax;
        public float AttackAnimationSeconds => attackAnimationSeconds;
        public float DeathAnimationSeconds => deathAnimationSeconds;
        public List<UnitAnimationKind> AnimationOrder => animationOrder;
    }

    [CreateAssetMenu(menuName = "Game/Config/Grid Test Spawner Authoring")]
    public class InitialUnitsSpawnerAuthoringConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class FactionUnitEntry
        {
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(0)] private int count = 1;
            [SerializeField] private Vector2Int spawnOffset;

            public GameObject Prefab => prefab;
            public int Count => count;
            public Vector2Int SpawnOffset => spawnOffset;
        }

        [System.Serializable]
        public sealed class FactionBuildingEntry
        {
            [SerializeField] private GameObject prefab;
            [SerializeField] private Vector2Int originOffset;

            public GameObject Prefab => prefab;
            public Vector2Int OriginOffset => originOffset;
        }

        [System.Serializable]
        public sealed class FactionEntry
        {
            [SerializeField, Min(0)] private int factionId;
            [SerializeField] private Vector2Int spawnCell = new(10, 10);
            [SerializeField] private List<FactionUnitEntry> units = new();
            [SerializeField] private List<FactionBuildingEntry> buildings = new();

            public int FactionId => factionId;
            public Vector2Int SpawnCell => spawnCell;
            public List<FactionUnitEntry> Units => units;
            public List<FactionBuildingEntry> Buildings => buildings;
        }

        [SerializeField] private GameObject blockerPrefab;
        [SerializeField] private GameObject unitSelectionMarkerPrefab;
        [SerializeField] private GameObject unitHealthBarPrefab;
        [SerializeField] private List<FactionEntry> factions = new();
        [SerializeField] private int blockerCount = 2000;
        [SerializeField] private int spawnRadiusCells = 5;
        [SerializeField] private float respawnDelaySeconds = 10f;
        [SerializeField] private uint randomSeed = 1;
        [SerializeField, Min(0)] private int initialDollars;
        [SerializeField, Min(0)] private int initialMaterials;
        [SerializeField, Min(0)] private int materialsCapacity;
        [SerializeField, Min(0)] private int initialOil;
        [SerializeField, Min(0)] private int initialFuel;
        [SerializeField] private bool createFactionBases = true;
        [SerializeField] private GameObject baseWallPrefab;
        [SerializeField] private GameObject baseGatePrefab;
        [SerializeField] private GameObject baseCoreBuildingPrefab;
        [SerializeField, Min(80)] private int baseHalfWidthCells = 120;
        [SerializeField, Min(60)] private int baseHalfHeightCells = 80;
        [SerializeField, Min(8)] private int baseMinimumUnitsPerFaction = 18;
        [SerializeField] private bool enableBlockerChurn = true;
        [SerializeField] private float churnIntervalSeconds = 1.0f;
        [SerializeField] private int addRemovePerInterval = 50;

        public GameObject BlockerPrefab => blockerPrefab;
        public virtual GameObject UnitSelectionMarkerPrefab => unitSelectionMarkerPrefab;
        public virtual GameObject UnitHealthBarPrefab => unitHealthBarPrefab;
        public List<FactionEntry> Factions => factions;
        public int BlockerCount => blockerCount;
        public int SpawnRadiusCells => spawnRadiusCells;
        public float RespawnDelaySeconds => respawnDelaySeconds;
        public uint RandomSeed => randomSeed;
        public int InitialDollars => Mathf.Max(0, initialDollars);
        public int InitialMaterials => Mathf.Max(0, initialMaterials);
        public int MaterialsCapacity => Mathf.Max(InitialMaterials, materialsCapacity);
        public int InitialOil => Mathf.Max(0, initialOil);
        public int InitialFuel => Mathf.Max(0, initialFuel);
        public bool CreateFactionBases => createFactionBases;
        public GameObject BaseWallPrefab => baseWallPrefab;
        public GameObject BaseGatePrefab => baseGatePrefab;
        public GameObject BaseCoreBuildingPrefab => baseCoreBuildingPrefab;
        public int BaseHalfWidthCells => Mathf.Max(80, baseHalfWidthCells);
        public int BaseHalfHeightCells => Mathf.Max(60, baseHalfHeightCells);
        public int BaseMinimumUnitsPerFaction => Mathf.Max(8, baseMinimumUnitsPerFaction);
        public bool EnableBlockerChurn => enableBlockerChurn;
        public float ChurnIntervalSeconds => churnIntervalSeconds;
        public int AddRemovePerInterval => addRemovePerInterval;
    }
}
