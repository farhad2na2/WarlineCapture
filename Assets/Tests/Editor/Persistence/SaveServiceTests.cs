using System.IO;
using NUnit.Framework;

public sealed class SaveServiceTests
{
    private string _saveRoot;
    private JsonSaveRepository _repository;
    private SaveService _service;

    [SetUp]
    public void SetUp()
    {
        _saveRoot = Path.Combine(Path.GetTempPath(), "SaveServiceTests", System.Guid.NewGuid().ToString("N"));
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
        var saveData = new SaveDataModel
        {
            profile = new PlayerProfileSaveData { commanderName = "Mandel", commanderLevel = 3, starsEarned = 5 },
            settings = new SettingsSaveData { largeText = true },
            quickGame = new QuickGameSaveData { enemyCount = 4, difficulty = "Hard" }
        };

        _service.SaveProject(saveData);

        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.ProfileFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.SettingsFileName)));
        Assert.IsTrue(File.Exists(_repository.GetPath(SaveService.QuickGameFileName)));
    }

    [Test]
    public void LoadProject_RestoresSplitSaveData()
    {
        _service.SaveProfile(new PlayerProfileSaveData { commanderName = "Mandel", commanderLevel = 7 });
        _service.SaveQuickGame(new QuickGameSaveData { presetId = "rush", fogOfWar = false });

        SaveDataModel loaded = _service.LoadProject();

        Assert.AreEqual(SaveMigration.CurrentVersion, loaded.saveVersion);
        Assert.AreEqual("Mandel", loaded.profile.commanderName);
        Assert.AreEqual(7, loaded.profile.commanderLevel);
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
    public void SaveMigration_NormalizesProfileUnlockArrays()
    {
        SaveDataModel data = new SaveDataModel
        {
            profile = new PlayerProfileSaveData
            {
                ownedUnitUnlocks = null
            }
        };

        SaveDataModel migrated = SaveMigration.Migrate(data);

        Assert.NotNull(migrated.profile.ownedUnitUnlocks);
        Assert.AreEqual(0, migrated.profile.ownedUnitUnlocks.Length);
    }
}
