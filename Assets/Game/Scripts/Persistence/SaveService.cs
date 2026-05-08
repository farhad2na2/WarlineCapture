using System;
using UnityEngine;

public sealed class SaveService
{
    public const string ProfileFileName = "profile.json";
    public const string SagaFileName = "saga.json";
    public const string OperationFileName = "operation.json";
    public const string SettingsFileName = "settings.json";
    public const string QuickGameFileName = "quickgame.json";

    private readonly JsonSaveRepository _repository;

    public SaveService(JsonSaveRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public static SaveService CreateDefault()
    {
        return new SaveService(new JsonSaveRepository(Application.persistentDataPath));
    }

    public WarlineCaptureSaveData LoadProject()
    {
        var data = new WarlineCaptureSaveData
        {
            profile = LoadProfile(),
            saga = LoadSaga(),
            operation = LoadOperation(),
            settings = LoadSettings(),
            quickGame = LoadQuickGame()
        };

        return SaveMigration.Migrate(data);
    }

    public void SaveProject(WarlineCaptureSaveData data)
    {
        WarlineCaptureSaveData migrated = SaveMigration.Migrate(data);
        SaveProfile(migrated.profile);
        SaveSaga(migrated.saga);
        SaveOperation(migrated.operation);
        SaveSettings(migrated.settings);
        SaveQuickGame(migrated.quickGame);
    }

    public PlayerProfileSaveData LoadProfile()
    {
        return _repository.Load<PlayerProfileSaveData>(ProfileFileName);
    }

    public SagaSaveData LoadSaga()
    {
        return _repository.Load<SagaSaveData>(SagaFileName);
    }

    public OperationSaveData LoadOperation()
    {
        WarlineCaptureSaveData migrated = SaveMigration.Migrate(new WarlineCaptureSaveData
        {
            operation = _repository.Load<OperationSaveData>(OperationFileName)
        });
        return migrated.operation;
    }

    public SettingsSaveData LoadSettings()
    {
        return _repository.Load<SettingsSaveData>(SettingsFileName);
    }

    public QuickGameSaveData LoadQuickGame()
    {
        return _repository.Load<QuickGameSaveData>(QuickGameFileName);
    }

    public void SaveProfile(PlayerProfileSaveData data)
    {
        _repository.Save(ProfileFileName, data);
    }

    public void SaveSaga(SagaSaveData data)
    {
        _repository.Save(SagaFileName, data);
    }

    public void SaveOperation(OperationSaveData data)
    {
        _repository.Save(OperationFileName, data);
    }

    public void SaveSettings(SettingsSaveData data)
    {
        _repository.Save(SettingsFileName, data);
    }

    public void SaveQuickGame(QuickGameSaveData data)
    {
        _repository.Save(QuickGameFileName, data);
    }

    public SaveSlotInfo GetSlotInfo(string slotId, string fileName)
    {
        return new SaveSlotInfo(slotId, fileName, _repository.Exists(fileName));
    }
}
