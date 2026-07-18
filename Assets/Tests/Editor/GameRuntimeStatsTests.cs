using NUnit.Framework;
using Game.Runtime;

public sealed class GameRuntimeStatsTests
{
    [Test]
    public void ValuePreservesBalanceReportInputsWithoutGlobalState()
    {
        var stats = new GameRuntimeStats(
            oilExtracted: 1,
            fuelProduced: 2,
            vehiclesOrdered: 3,
            soldiersOrdered: 4,
            ammoOrdered: 5,
            buildingsBuilt: 6,
            matchElapsedSeconds: 7,
            civiliansProtected: 8,
            capturedOrDestroyedBuildings: 9,
            ownSoldiersDead: 10,
            enemySoldiersDead: 11);

        Assert.AreEqual(1, stats.OilExtracted);
        Assert.AreEqual(2, stats.FuelProduced);
        Assert.AreEqual(3, stats.VehiclesOrdered);
        Assert.AreEqual(4, stats.SoldiersOrdered);
        Assert.AreEqual(5, stats.AmmoOrdered);
        Assert.AreEqual(6, stats.BuildingsBuilt);
        Assert.AreEqual(7, stats.MatchElapsedSeconds);
        Assert.AreEqual(8, stats.CiviliansProtected);
        Assert.AreEqual(9, stats.CapturedOrDestroyedBuildings);
        Assert.AreEqual(10, stats.OwnSoldiersDead);
        Assert.AreEqual(11, stats.EnemySoldiersDead);
    }
}
