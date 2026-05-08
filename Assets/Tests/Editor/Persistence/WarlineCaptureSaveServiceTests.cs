using System.IO;
using NUnit.Framework;

public sealed class WarlineCaptureSaveServiceTests
{
    private string _saveRoot;
    private JsonSaveRepository _repository;
    private SaveService _service;

    [SetUp]
    public void SetUp()
    {
        _saveRoot = Path.Combine(Path.GetTempPath(), "WarlineCaptureSaveServiceTests", System.Guid.NewGuid().ToString("N"));
        _repository = new JsonSaveRepository(_saveRoot);
        _service = new SaveService(_repository);
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(_saveRoot) && Directory.Exists(_saveRoot))
            Directory.Delete(_saveRoot, true);
    }

    [Test]
    public void SaveProject_WritesExpectedSplitFiles()
    {
        var saveData = new WarlineCaptureSaveData
        {
            profile = new PlayerProfileSaveData { commanderName = "Mandel", commanderLevel = 3, starsEarned = 5 },
            saga = new SagaSaveData
            {
                missions = new[]
                {
                    new SagaMissionProgressData { missionId = "saga.ch01.m01", completed = true, stars = 3 }
                }
            },
            operation = new OperationSaveData { operationDay = 4 },
            settings = new SettingsSaveData { largeText = true },
            quickGame = new QuickGameSaveData { enemyCount = 4, difficulty = "Hard" }
        };

        _service.SaveProject(saveData);

        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.ProfileFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.SagaFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.OperationFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.SettingsFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.QuickGameFileName)));
    }

    [Test]
    public void LoadProject_RestoresSplitSaveData()
    {
        _service.SaveProfile(new PlayerProfileSaveData { commanderName = "Mandel", commanderLevel = 7 });
        _service.SaveSaga(new SagaSaveData
        {
            missions = new[]
            {
                new SagaMissionProgressData { missionId = "saga.ch01.m02", completed = true, stars = 2 }
            }
        });
        _service.SaveQuickGame(new QuickGameSaveData { presetId = "rush", fogOfWar = false });

        WarlineCaptureSaveData loaded = _service.LoadProject();

        Assert.AreEqual(SaveMigration.CurrentVersion, loaded.saveVersion);
        Assert.AreEqual("Mandel", loaded.profile.commanderName);
        Assert.AreEqual(7, loaded.profile.commanderLevel);
        Assert.AreEqual("saga.ch01.m02", loaded.saga.missions[0].missionId);
        Assert.IsTrue(loaded.saga.missions[0].completed);
        Assert.AreEqual(2, loaded.saga.missions[0].stars);
        Assert.AreEqual("rush", loaded.quickGame.presetId);
        Assert.IsFalse(loaded.quickGame.fogOfWar);
    }

    [Test]
    public void GetSlotInfo_ReportsExistingFiles()
    {
        Assert.IsFalse(_service.GetSlotInfo("profile", SaveService.ProfileFileName).Exists);

        _service.SaveProfile(new PlayerProfileSaveData());

        Assert.IsTrue(_service.GetSlotInfo("profile", SaveService.ProfileFileName).Exists);
    }

    [Test]
    public void SaveMigration_NormalizesRewardTrackClaimArray()
    {
        WarlineCaptureSaveData data = new WarlineCaptureSaveData
        {
            profile = new PlayerProfileSaveData
            {
                ownedUnitUnlocks = null,
                claimedRewardTrackNodes = null,
                missionHistory = null
            }
        };

        WarlineCaptureSaveData migrated = SaveMigration.Migrate(data);

        Assert.NotNull(migrated.profile.ownedUnitUnlocks);
        Assert.NotNull(migrated.profile.claimedRewardTrackNodes);
        Assert.NotNull(migrated.profile.missionHistory);
        Assert.AreEqual(0, migrated.profile.claimedRewardTrackNodes.Length);
        Assert.AreEqual(0, migrated.profile.missionHistory.Length);
    }
}
