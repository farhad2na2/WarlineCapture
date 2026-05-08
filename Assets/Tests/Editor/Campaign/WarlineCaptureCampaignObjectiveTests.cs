using NUnit.Framework;

public sealed class WarlineCaptureCampaignObjectiveTests
{
    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
        SagaProgressStore.ClearMission("test.m01");
    }

    [TearDown]
    public void TearDown()
    {
        GameRuntimeStats.Reset();
        WarlineCaptureMissionSession.Clear();
        SagaProgressStore.ClearMission("test.m01");
    }

    [Test]
    public void ObjectiveManager_EvaluatesRequiredObjectivesFromRuntimeStats()
    {
        MissionConfig mission = CreateMission();
        var manager = new ObjectiveManager();
        manager.Initialize(mission);

        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordBuildingBuilt();

        var states = manager.Evaluate(GameRuntimeStats.GetSnapshot());

        Assert.IsTrue(manager.HasWon);
        Assert.AreEqual(2, states.Count);
        Assert.IsTrue(states[0].Complete);
        Assert.IsTrue(states[1].Complete);
    }

    [Test]
    public void ObjectiveManager_EvaluatesAllInitialPhaseEightObjectiveTypes()
    {
        MissionConfig mission = new MissionConfig(
            "test.phase8",
            "Phase 8 Coverage",
            new[]
            {
                new ObjectiveConfig("survive", "Survive 60 seconds", ObjectiveType.SurviveDuration, 60),
                new ObjectiveConfig("protect", "Protect civilians", ObjectiveType.ProtectCivilianCount, 4),
                new ObjectiveConfig("capture", "Capture or destroy target", ObjectiveType.CaptureOrDestroyBuilding, 1),
                new ObjectiveConfig("losses", "Keep losses below 1", ObjectiveType.KeepUnitLossesBelow, 1)
            },
            System.Array.Empty<StarGoalConfig>());
        var manager = new ObjectiveManager();
        manager.Initialize(mission);

        GameRuntimeStats.RecordMissionElapsed(60.2f);
        GameRuntimeStats.RecordCiviliansProtected(4);
        GameRuntimeStats.RecordCapturedOrDestroyedBuilding();
        GameRuntimeStats.RecordMilitaryDeath(0);

        var states = manager.Evaluate(GameRuntimeStats.GetSnapshot());

        Assert.IsTrue(manager.HasWon);
        Assert.AreEqual(ObjectiveType.SurviveDuration, states[0].Type);
        Assert.IsTrue(states[0].Complete);
        Assert.AreEqual(ObjectiveType.ProtectCivilianCount, states[1].Type);
        Assert.IsTrue(states[1].Complete);
        Assert.AreEqual(ObjectiveType.CaptureOrDestroyBuilding, states[2].Type);
        Assert.IsTrue(states[2].Complete);
        Assert.IsTrue(states[3].Complete);
    }

    [Test]
    public void MissionResultBuilder_AwardsBaseVictoryAndCompletedStarGoals()
    {
        MissionConfig mission = CreateMission();

        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordBuildingBuilt();
        GameRuntimeStats.RecordOilExtracted(75f);
        GameRuntimeStats.RecordFuelProduced(25f);

        MissionResultData result = MissionResultBuilder.Build(mission, GameRuntimeStats.GetSnapshot());

        Assert.IsTrue(result.Victory);
        Assert.AreEqual(3, result.StarsEarned);
        Assert.AreEqual(2, result.EnemiesDefeated);
        Assert.AreEqual(1, result.BuildingsBuilt);
        Assert.AreEqual(100, result.ResourcesEarned);
    }

    [Test]
    public void SagaProgressStore_PersistsCompletionAndKeepsBestStars()
    {
        MissionConfig mission = CreateMission();

        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordBuildingBuilt();
        MissionResultData firstResult = MissionResultBuilder.Build(mission, GameRuntimeStats.GetSnapshot());
        SagaProgressStore.ApplyMissionResult(firstResult);

        MissionResultData lowerStars = new MissionResultData("test.m01", "Test Mission", true, 1, 2, 0, 1, 0, firstResult.Objectives);
        SagaProgressStore.ApplyMissionResult(lowerStars);

        Assert.IsTrue(SagaProgressStore.IsCompleted("test.m01"));
        Assert.AreEqual(2, SagaProgressStore.GetStars("test.m01"));
    }

    [Test]
    public void MissionSession_TracksActiveMissionAndBuildsResult()
    {
        WarlineCaptureMissionSession.BeginMission("saga.ch01.m02.establish_base", WarlineCaptureRoute.SagaMap);
        GameRuntimeStats.RecordBuildingBuilt();
        for (int i = 0; i < 8; i++)
            GameRuntimeStats.RecordMilitaryDeath(1);

        MissionResultData result = WarlineCaptureMissionSession.BuildCurrentResult(GameRuntimeStats.GetSnapshot());

        Assert.IsTrue(WarlineCaptureMissionSession.HasActiveMission);
        Assert.AreEqual("Establish The Base", WarlineCaptureMissionSession.ActiveMission.DisplayName);
        Assert.IsTrue(result.Victory);
        Assert.AreEqual("saga.ch01.m02.establish_base", result.MissionId);
    }

    [Test]
    public void Chapter01M01_ResultCompletesWhenPatrolIsDefeatedAndCommandSquadSurvives()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m01.first_contact");

        GameRuntimeStats.RecordMilitaryDeath(1);

        MissionResultData result = MissionResultBuilder.Build(mission, GameRuntimeStats.GetSnapshot());

        Assert.IsTrue(result.Victory);
        Assert.AreEqual(1, result.EnemiesDefeated);
        Assert.AreEqual("saga.ch01.m01.first_contact", result.MissionId);
        Assert.AreEqual("destroy_patrol", result.Objectives[0].Id);
        Assert.IsTrue(result.Objectives[0].Complete);
        Assert.AreEqual("command_squad_survives", result.Objectives[1].Id);
        Assert.IsTrue(result.Objectives[1].Complete);
    }

    [Test]
    public void Chapter01M01_ResultFailsWhenCommandSquadIsDestroyed()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m01.first_contact");

        GameRuntimeStats.RecordMilitaryDeath(1);
        GameRuntimeStats.RecordMilitaryDeath(0);

        MissionResultData result = MissionResultBuilder.Build(mission, GameRuntimeStats.GetSnapshot());

        Assert.IsFalse(result.Victory);
        Assert.AreEqual("command_squad_survives", result.Objectives[1].Id);
        Assert.IsFalse(result.Objectives[1].Complete);
    }

    private static MissionConfig CreateMission()
    {
        return new MissionConfig(
            "test.m01",
            "Test Mission",
            new[]
            {
                new ObjectiveConfig("destroy", "Destroy two enemies", ObjectiveType.DestroyAllEnemies, 2),
                new ObjectiveConfig("build", "Build one structure", ObjectiveType.BuildStructure, 1)
            },
            new[]
            {
                new StarGoalConfig("resources", "Earn 100 resources", ObjectiveType.ReachResourceAmount, 100),
                new StarGoalConfig("losses", "Lose no more than one squad", ObjectiveType.KeepUnitLossesBelow, 1)
            });
    }
}
