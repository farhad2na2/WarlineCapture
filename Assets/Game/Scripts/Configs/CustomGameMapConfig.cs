using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Custom Game/Map Config")]
    public sealed class CustomGameMapConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int gridWidth = 2048;
        [SerializeField, Min(1)] private int gridHeight = 2048;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin;
        [SerializeField] private GameObject blockerPrefab;
        [SerializeField, Min(0)] private int blockerCount = 2000;
        [SerializeField, Min(0)] private int spawnRadiusCells = 5;
        [SerializeField, Min(0.01f)] private float respawnDelaySeconds = 10f;
        [SerializeField, Min(1)] private uint randomSeed = 1;
        [SerializeField, Min(0)] private int initialDollars;
        [SerializeField, Min(0)] private int initialOil;
        [SerializeField, Min(0)] private int initialFuel;
        [SerializeField] private bool createFactionBases = true;
        [SerializeField] private string baseWallLookupKey = "Wall_Dirt_Straight";
        [SerializeField] private string baseGateLookupKey = "Building_Road_Barrier";
        [SerializeField] private string baseCoreBuildingLookupKey = "Building_Ammunition_Depot";
        [SerializeField, Min(1)] private int baseHalfWidthCells = 120;
        [SerializeField, Min(1)] private int baseHalfHeightCells = 80;
        [SerializeField, Min(0)] private int baseMinimumUnitsPerFaction = 18;
        [SerializeField] private bool enableBlockerChurn = true;
        [SerializeField, Min(0.01f)] private float churnIntervalSeconds = 1f;
        [SerializeField, Min(0)] private int addRemovePerInterval = 50;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        public Vector3 GridOrigin => gridOrigin;
        public GameObject BlockerPrefab => blockerPrefab;
        public int BlockerCount => blockerCount;
        public int SpawnRadiusCells => spawnRadiusCells;
        public float RespawnDelaySeconds => respawnDelaySeconds;
        public uint RandomSeed => randomSeed;
        public int InitialDollars => initialDollars;
        public int InitialOil => initialOil;
        public int InitialFuel => initialFuel;
        public bool CreateFactionBases => createFactionBases;
        public string BaseWallLookupKey => baseWallLookupKey;
        public string BaseGateLookupKey => baseGateLookupKey;
        public string BaseCoreBuildingLookupKey => baseCoreBuildingLookupKey;
        public int BaseHalfWidthCells => baseHalfWidthCells;
        public int BaseHalfHeightCells => baseHalfHeightCells;
        public int BaseMinimumUnitsPerFaction => baseMinimumUnitsPerFaction;
        public bool EnableBlockerChurn => enableBlockerChurn;
        public float ChurnIntervalSeconds => churnIntervalSeconds;
        public int AddRemovePerInterval => addRemovePerInterval;
    }
}
