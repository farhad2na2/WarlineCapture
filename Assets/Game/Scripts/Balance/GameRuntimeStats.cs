namespace Game.Runtime
{
    public readonly struct GameRuntimeStats
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

        public GameRuntimeStats(
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
}
