using System;
using Game.Narrative.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class SaveService
    {
        public const string ProfileFileName = "profile.json";
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

        public SaveDataModel LoadProject()
        {
            var data = new SaveDataModel
            {
                profile = LoadProfile(),
                settings = LoadSettings(),
                quickGame = LoadQuickGame()
            };

            return SaveMigration.Migrate(data);
        }

        public void SaveProject(SaveDataModel data)
        {
            SaveDataModel migrated = SaveMigration.Migrate(data);
            SaveProfile(migrated.profile);
            SaveSettings(migrated.settings);
            SaveQuickGame(migrated.quickGame);
        }

        public PlayerProfileSaveData LoadProfile()
        {
            bool exists = _repository.Exists(ProfileFileName);
            string raw = exists ? _repository.ReadRaw(ProfileFileName) : string.Empty;
            PlayerProfileSaveData profile;
            try
            {
                profile = _repository.Load<PlayerProfileSaveData>(ProfileFileName);
            }
            catch (ArgumentException)
            {
                profile = new PlayerProfileSaveData();
            }
            bool legacyProfile = exists && raw.IndexOf("\"firstLaunchStatus\"", StringComparison.Ordinal) < 0;
            bool futureProfile = profile.profileSchemaVersion > FirstLaunchProfileState.CurrentSchemaVersion;
            return NormalizeProfile(profile, legacyProfile, futureProfile);
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
            _repository.SaveAtomic(ProfileFileName, NormalizeProfile(data, false, false));
        }

        public void ResetFirstLaunchProgress()
        {
            PlayerProfileSaveData profile = LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.NotStarted;
            profile.firstLaunchLastCompletedStateId = string.Empty;
            profile.firstLaunchCommanderCallsign = "COMMANDER";
            profile.firstLaunchCommanderDisplayName = "Commander";
            profile.firstLaunchCommanderPortraitIndex = 0;
            profile.firstLaunchGuidance = "Full";
            profile.firstLaunchLanguage = FirstLaunchNarrativeLanguage.Unselected.ToString();
            profile.firstLaunchWatched = false;
            profile.firstLaunchSkipped = false;
            SaveProfile(profile);
        }

        public void DeleteAllSaveData()
        {
            _repository.Delete(ProfileFileName);
            _repository.Delete(SettingsFileName);
            _repository.Delete(QuickGameFileName);
        }

        private static PlayerProfileSaveData NormalizeProfile(
            PlayerProfileSaveData profile,
            bool legacyProfile,
            bool futureProfile = false)
        {
            profile ??= new PlayerProfileSaveData();
            profile.profileSchemaVersion = FirstLaunchProfileState.CurrentSchemaVersion;
            if (legacyProfile)
            {
                profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
                profile.firstLaunchWatched = true;
                profile.firstLaunchSkipped = false;
            }
            else if (!FirstLaunchProfileState.IsKnown(profile.firstLaunchStatus))
            {
                profile.firstLaunchStatus = FirstLaunchProfileState.NotStarted;
            }

            profile.firstLaunchLastCompletedStateId ??= string.Empty;
            profile.firstLaunchCommanderCallsign = string.IsNullOrWhiteSpace(profile.firstLaunchCommanderCallsign)
                ? "COMMANDER"
                : profile.firstLaunchCommanderCallsign.Trim();
            profile.firstLaunchCommanderDisplayName = string.IsNullOrWhiteSpace(profile.firstLaunchCommanderDisplayName)
                ? "Commander"
                : profile.firstLaunchCommanderDisplayName.Trim();
            profile.firstLaunchGuidance = string.IsNullOrWhiteSpace(profile.firstLaunchGuidance)
                ? "Full"
                : profile.firstLaunchGuidance.Trim();
            if (!Enum.TryParse(profile.firstLaunchLanguage, true, out FirstLaunchNarrativeLanguage language) ||
                language == FirstLaunchNarrativeLanguage.Unselected &&
                profile.firstLaunchStatus != FirstLaunchProfileState.NotStarted)
            {
                profile.firstLaunchLanguage = profile.firstLaunchStatus == FirstLaunchProfileState.NotStarted
                    ? FirstLaunchNarrativeLanguage.Unselected.ToString()
                    : FirstLaunchNarrativeLanguage.English.ToString();
            }
            else
            {
            profile.firstLaunchLanguage = language.ToString();
            }
            profile.campaignMissionProgress = futureProfile
                ? Array.Empty<CampaignMissionProgressSaveData>()
                : profile.campaignMissionProgress ?? Array.Empty<CampaignMissionProgressSaveData>();
            return profile;
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
}
