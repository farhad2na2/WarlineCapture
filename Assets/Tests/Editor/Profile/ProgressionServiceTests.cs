using NUnit.Framework;

public sealed class ProgressionServiceTests
{
    [Test]
    public void ProgressionService_AddXpLevelsUpCommander()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData();

        int levelsGained = ProgressionService.GrantCommanderXp(profile, 450);
        CommanderProgression progression = ProgressionService.GetCommanderProgression(profile);

        Assert.AreEqual(3, profile.commanderLevel);
        Assert.AreEqual(450, profile.commanderXp);
        Assert.AreEqual(2, levelsGained);
        Assert.AreEqual("LV. 3", progression.FormatLevel());
        Assert.AreEqual("450 / 820", progression.FormatXpProgress());
        Assert.AreEqual(0f, progression.Progress01, 0.001f);
    }

    [Test]
    public void ProgressionService_PreservesHigherSavedCommanderLevel()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData
        {
            commanderLevel = 5,
            commanderXp = 220
        };

        CommanderProgression progression = ProgressionService.GetCommanderProgression(profile);

        Assert.AreEqual(5, progression.Level);
        Assert.AreEqual("220 / 1,900", progression.FormatXpProgress());
    }

    [Test]
    public void ProgressionService_AccumulatesMissionResultAccountStats()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData();
        MissionResultData result = new MissionResultData(
            "test.profile.m01",
            "Profile Result",
            true,
            3,
            12,
            2,
            4,
            350,
            System.Array.Empty<ObjectiveRuntimeState>());

        ProgressionService.AccumulateAccountStats(profile, result, 2);

        Assert.AreEqual(1, profile.victories);
        Assert.AreEqual(1, profile.missionsCompleted);
        Assert.AreEqual(2, profile.starsEarned);
        Assert.AreEqual(12, profile.enemiesDefeated);
        Assert.AreEqual(2, profile.unitsLost);
        Assert.AreEqual(4, profile.buildingsBuilt);
        Assert.AreEqual(350, profile.resourcesEarned);
    }
}
