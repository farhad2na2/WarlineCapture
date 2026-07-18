using NUnit.Framework;
using Game.Components;
using Game.Configs;

public sealed class AISettingsValidationTests
{
    [Test]
    public void AISettingsSnapshot_AppliesDifficultyEconomyAndCadenceMultipliers()
    {
        AISettingsSnapshot snapshot = AISettingsSnapshot.Defaults;
        snapshot.Difficulty = AIDifficultySetting.Brutal;
        snapshot.StartingMoney = AIStartingMoneySetting.High;
        snapshot.IncomeMultiplier = 2f;
        snapshot.BuildSpeed = AISpeedSetting.Fast;
        snapshot.UnitProductionSpeed = AISpeedSetting.Fast;
        snapshot.Expansion = AIExpansionSetting.Fast;

        Assert.AreEqual(120000, snapshot.ApplyStartingMoney(50000, AIControllerRole.Enemy));
        Assert.AreEqual(3f, snapshot.ApplyIncomeMultiplier(1f, AIControllerRole.Enemy), 0.001f);
        Assert.Less(snapshot.ApplyBuildInterval(8f, AIControllerRole.Enemy), 4f);
        Assert.Less(snapshot.ApplyProductionInterval(6f, AIControllerRole.Enemy), 3f);
    }
}
