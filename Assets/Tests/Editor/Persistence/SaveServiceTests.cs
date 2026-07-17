using System.IO;
using NUnit.Framework;
using Game.Runtime;
using Game.Narrative.Contracts;

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

    [Test]
    public void LoadProfile_NewProfileStartsFirstLaunchAndRoundTripsHandoffState()
    {
        PlayerProfileSaveData fresh = _service.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.NotStarted, fresh.firstLaunchStatus);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Unselected.ToString(), fresh.firstLaunchLanguage);
        Assert.AreEqual(FirstLaunchProfileState.CurrentSchemaVersion, fresh.profileSchemaVersion);

        fresh.firstLaunchStatus = FirstLaunchProfileState.HandoffPending;
        fresh.firstLaunchCommanderCallsign = "RAVEN";
        fresh.firstLaunchCommanderDisplayName = "Alex Morgan";
        fresh.firstLaunchCommanderPortraitIndex = 2;
        fresh.firstLaunchGuidance = "Contextual";
        fresh.firstLaunchLastCompletedStateId = "FL-P18";
        fresh.firstLaunchWatched = true;
        fresh.firstLaunchLanguage = FirstLaunchNarrativeLanguage.Persian.ToString();
        _service.SaveProfile(fresh);

        PlayerProfileSaveData loaded = _service.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.HandoffPending, loaded.firstLaunchStatus);
        Assert.AreEqual("RAVEN", loaded.firstLaunchCommanderCallsign);
        Assert.AreEqual("Alex Morgan", loaded.firstLaunchCommanderDisplayName);
        Assert.AreEqual(2, loaded.firstLaunchCommanderPortraitIndex);
        Assert.AreEqual("Contextual", loaded.firstLaunchGuidance);
        Assert.AreEqual("FL-P18", loaded.firstLaunchLastCompletedStateId);
        Assert.IsTrue(loaded.firstLaunchWatched);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Persian.ToString(), loaded.firstLaunchLanguage);
    }

    [Test]
    public void LoadProfile_LegacyProfileIsTreatedAsEstablishedPlayer()
    {
        Directory.CreateDirectory(_saveRoot);
        File.WriteAllText(_repository.GetPath(SaveService.ProfileFileName), "{\"commanderName\":\"Legacy\",\"commanderLevel\":9}");

        PlayerProfileSaveData loaded = _service.LoadProfile();

        Assert.AreEqual("Legacy", loaded.commanderName);
        Assert.AreEqual(9, loaded.commanderLevel);
        Assert.AreEqual(FirstLaunchProfileState.Completed, loaded.firstLaunchStatus);
        Assert.IsTrue(loaded.firstLaunchWatched);
    }

    [Test]
    public void LoadProfile_UnknownFirstLaunchStateFallsBackSafely()
    {
        Directory.CreateDirectory(_saveRoot);
        File.WriteAllText(_repository.GetPath(SaveService.ProfileFileName), "{\"firstLaunchStatus\":\"Corrupt\"}");
        PlayerProfileSaveData loaded = _service.LoadProfile();
        Assert.AreEqual(FirstLaunchProfileState.NotStarted, loaded.firstLaunchStatus);
        Assert.AreEqual("COMMANDER", loaded.firstLaunchCommanderCallsign);
        Assert.AreEqual("Full", loaded.firstLaunchGuidance);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Unselected.ToString(), loaded.firstLaunchLanguage);
    }

    [Test]
    public void ResetFirstLaunchProgress_PreservesUnrelatedProfileProgress()
    {
        _service.SaveProfile(new PlayerProfileSaveData
        {
            commanderName = "Mandel",
            commanderLevel = 12,
            credits = 4500,
            ownedUnitUnlocks = new[] { "unit.raven" },
            firstLaunchStatus = FirstLaunchProfileState.Completed,
            firstLaunchLastCompletedStateId = "first_launch.command_base_reveal",
            firstLaunchCommanderCallsign = "RAVEN",
            firstLaunchCommanderDisplayName = "Alex Morgan",
            firstLaunchCommanderPortraitIndex = 2,
            firstLaunchGuidance = "Contextual",
            firstLaunchLanguage = FirstLaunchNarrativeLanguage.Persian.ToString(),
            firstLaunchWatched = true,
            firstLaunchSkipped = true
        });

        _service.ResetFirstLaunchProgress();

        PlayerProfileSaveData loaded = _service.LoadProfile();
        Assert.AreEqual("Mandel", loaded.commanderName);
        Assert.AreEqual(12, loaded.commanderLevel);
        Assert.AreEqual(4500, loaded.credits);
        CollectionAssert.AreEqual(new[] { "unit.raven" }, loaded.ownedUnitUnlocks);
        Assert.AreEqual(FirstLaunchProfileState.NotStarted, loaded.firstLaunchStatus);
        Assert.AreEqual(string.Empty, loaded.firstLaunchLastCompletedStateId);
        Assert.AreEqual("COMMANDER", loaded.firstLaunchCommanderCallsign);
        Assert.AreEqual("Commander", loaded.firstLaunchCommanderDisplayName);
        Assert.AreEqual(0, loaded.firstLaunchCommanderPortraitIndex);
        Assert.AreEqual("Full", loaded.firstLaunchGuidance);
        Assert.AreEqual(FirstLaunchNarrativeLanguage.Unselected.ToString(), loaded.firstLaunchLanguage);
        Assert.IsFalse(loaded.firstLaunchWatched);
        Assert.IsFalse(loaded.firstLaunchSkipped);
    }

    [Test]
    public void DeleteAllSaveData_RemovesEverySaveFile()
    {
        _service.SaveProject(new SaveDataModel
        {
            profile = new PlayerProfileSaveData { commanderLevel = 8 },
            settings = new SettingsSaveData { largeText = true },
            quickGame = new QuickGameSaveData { enemyCount = 5 }
        });

        _service.DeleteAllSaveData();

        Assert.IsFalse(_repository.Exists(SaveService.ProfileFileName));
        Assert.IsFalse(_repository.Exists(SaveService.SettingsFileName));
        Assert.IsFalse(_repository.Exists(SaveService.QuickGameFileName));
    }
}
