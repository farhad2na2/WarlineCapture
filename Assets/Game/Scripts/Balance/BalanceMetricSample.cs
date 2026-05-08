using System;
using UnityEngine;

[Serializable]
public readonly struct BalanceMetricSample
{
    public readonly int OilExtracted;
    public readonly int FuelProduced;
    public readonly int VehiclesOrdered;
    public readonly int SoldiersOrdered;
    public readonly int AmmoOrdered;
    public readonly int BuildingsBuilt;
    public readonly int OwnSoldiersDead;
    public readonly int EnemySoldiersDead;

    public BalanceMetricSample(
        int oilExtracted,
        int fuelProduced,
        int vehiclesOrdered,
        int soldiersOrdered,
        int ammoOrdered,
        int buildingsBuilt,
        int ownSoldiersDead,
        int enemySoldiersDead)
    {
        OilExtracted = Mathf.Max(0, oilExtracted);
        FuelProduced = Mathf.Max(0, fuelProduced);
        VehiclesOrdered = Mathf.Max(0, vehiclesOrdered);
        SoldiersOrdered = Mathf.Max(0, soldiersOrdered);
        AmmoOrdered = Mathf.Max(0, ammoOrdered);
        BuildingsBuilt = Mathf.Max(0, buildingsBuilt);
        OwnSoldiersDead = Mathf.Max(0, ownSoldiersDead);
        EnemySoldiersDead = Mathf.Max(0, enemySoldiersDead);
    }

    public GameRuntimeStats.Snapshot ToSnapshot(int missionElapsedSeconds = 0)
    {
        return new GameRuntimeStats.Snapshot(
            OilExtracted,
            FuelProduced,
            VehiclesOrdered,
            SoldiersOrdered,
            AmmoOrdered,
            BuildingsBuilt,
            missionElapsedSeconds,
            0,
            0,
            OwnSoldiersDead,
            EnemySoldiersDead);
    }
}
