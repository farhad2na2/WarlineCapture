using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public static class GameRuntimeStats
{
    public enum UnitOrderKind
    {
        Soldier,
        Vehicle,
        Ammo
    }

    public delegate UnitOrderKind ClassifyUnitPrefabDelegate(GameObject prefab);

    public readonly struct Snapshot
    {
        public readonly int OilExtracted;
        public readonly int FuelProduced;
        public readonly int VehiclesOrdered;
        public readonly int SoldiersOrdered;
        public readonly int AmmoOrdered;
        public readonly int BuildingsBuilt;
        public readonly int MatchElapsedSeconds;
        public readonly int CiviliansProtected;
        public readonly int CapturedOrDestroyedBuildings;
        public readonly int OwnSoldiersDead;
        public readonly int EnemySoldiersDead;

        public Snapshot(
            int oilExtracted,
            int fuelProduced,
            int vehiclesOrdered,
            int soldiersOrdered,
            int ammoOrdered,
            int buildingsBuilt,
            int matchElapsedSeconds,
            int civiliansProtected,
            int capturedOrDestroyedBuildings,
            int ownSoldiersDead,
            int enemySoldiersDead)
        {
            OilExtracted = oilExtracted;
            FuelProduced = fuelProduced;
            VehiclesOrdered = vehiclesOrdered;
            SoldiersOrdered = soldiersOrdered;
            AmmoOrdered = ammoOrdered;
            BuildingsBuilt = buildingsBuilt;
            MatchElapsedSeconds = matchElapsedSeconds;
            CiviliansProtected = civiliansProtected;
            CapturedOrDestroyedBuildings = capturedOrDestroyedBuildings;
            OwnSoldiersDead = ownSoldiersDead;
            EnemySoldiersDead = enemySoldiersDead;
        }
    }

    private static float _oilExtracted;
    private static float _fuelProduced;
    private static int _vehiclesOrdered;
    private static int _soldiersOrdered;
    private static int _ammoOrdered;
    private static int _buildingsBuilt;
    private static float _matchElapsedSeconds;
    private static int _civiliansProtected;
    private static int _capturedOrDestroyedBuildings;
    private static int _ownSoldiersDead;
    private static int _enemySoldiersDead;
    private static ClassifyUnitPrefabDelegate _classifyUnitPrefab;

    public static void ConfigureUnitPrefabClassifier(ClassifyUnitPrefabDelegate classifyUnitPrefab)
    {
        _classifyUnitPrefab = classifyUnitPrefab;
    }

    public static void Reset()
    {
        _oilExtracted = 0f;
        _fuelProduced = 0f;
        _vehiclesOrdered = 0;
        _soldiersOrdered = 0;
        _ammoOrdered = 0;
        _buildingsBuilt = 0;
        _matchElapsedSeconds = 0f;
        _civiliansProtected = 0;
        _capturedOrDestroyedBuildings = 0;
        _ownSoldiersDead = 0;
        _enemySoldiersDead = 0;
    }

    public static Snapshot GetSnapshot()
    {
        return new Snapshot(
            Mathf.Max(0, Mathf.FloorToInt(_oilExtracted)),
            Mathf.Max(0, Mathf.FloorToInt(_fuelProduced)),
            _vehiclesOrdered,
            _soldiersOrdered,
            _ammoOrdered,
            _buildingsBuilt,
            Mathf.Max(0, Mathf.FloorToInt(_matchElapsedSeconds)),
            _civiliansProtected,
            _capturedOrDestroyedBuildings,
            _ownSoldiersDead,
            _enemySoldiersDead);
    }

    public static void RecordOilExtracted(float amount)
    {
        if (amount > 0f)
            _oilExtracted += amount;
    }

    public static void RecordFuelProduced(float amount)
    {
        if (amount > 0f)
            _fuelProduced += amount;
    }

    public static void RecordBuildingBuilt()
    {
        _buildingsBuilt++;
    }

    public static void RecordMatchElapsed(float seconds)
    {
        if (seconds > 0f)
            _matchElapsedSeconds += seconds;
    }

    public static void RecordCiviliansProtected(int count = 1)
    {
        if (count > 0)
            _civiliansProtected += count;
    }

    public static void RecordCapturedOrDestroyedBuilding()
    {
        _capturedOrDestroyedBuildings++;
    }

    public static void RecordUnitOrdered(GameObject prefab)
    {
        if (prefab == null)
            return;

        switch (ClassifyUnitPrefab(prefab))
        {
            case UnitOrderKind.Ammo:
                _ammoOrdered++;
                return;

            case UnitOrderKind.Vehicle:
                _vehiclesOrdered++;
                return;

            default:
                _soldiersOrdered++;
                return;
        }
    }

    public static void RecordMilitaryDeath(byte factionId)
    {
        if (FactionIdentitySystem.IsPlayerControlled(factionId))
            _ownSoldiersDead++;
        else
            _enemySoldiersDead++;
    }

    public static bool IsVehiclePrefab(GameObject prefab)
    {
        return prefab != null && ClassifyUnitPrefab(prefab) == UnitOrderKind.Vehicle;
    }

    public static bool IsAmmoPrefab(GameObject prefab)
    {
        return prefab != null && ClassifyUnitPrefab(prefab) == UnitOrderKind.Ammo;
    }

    private static UnitOrderKind ClassifyUnitPrefab(GameObject prefab)
    {
        return _classifyUnitPrefab != null
            ? _classifyUnitPrefab(prefab)
            : ClassifyUnitPrefabByName(prefab);
    }

    private static UnitOrderKind ClassifyUnitPrefabByName(GameObject prefab)
    {
        if (prefab == null)
            return UnitOrderKind.Soldier;

        if (prefab.name.IndexOf("Ammo", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return UnitOrderKind.Ammo;

        if (prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            prefab.name.IndexOf("Vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return UnitOrderKind.Vehicle;
        }

        return UnitOrderKind.Soldier;
    }

    public static bool IsMilitarySoldierEntity(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || em.HasComponent<CivilianUnitTag>(entity))
            return false;

        if (em.HasComponent<UnitAirComponent>(entity))
            return false;

        if (em.HasComponent<UnitFootprint>(entity))
        {
            int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
            if (size.x > 1 || size.y > 1)
                return false;
        }

        return true;
    }
}
