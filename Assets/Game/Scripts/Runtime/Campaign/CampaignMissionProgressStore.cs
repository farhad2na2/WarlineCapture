using System;
using System.Collections.Generic;

namespace Game.Runtime
{
    public sealed class CampaignMissionProgressStore
    {
        public const int CurrentEntrySchemaVersion = 1;
        private readonly SaveService _saveService;

        public CampaignMissionProgressStore(SaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public CampaignMissionProgressSaveData[] ReadAll()
        {
            PlayerProfileSaveData profile = _saveService.LoadProfile();
            CampaignMissionProgressSaveData[] normalized = Normalize(profile.campaignMissionProgress);
            return Clone(normalized);
        }

        public bool EnsureAvailable(string missionId)
        {
            RequireMissionId(missionId);
            PlayerProfileSaveData profile = _saveService.LoadProfile();
            List<CampaignMissionProgressSaveData> entries = ToList(profile.campaignMissionProgress);
            CampaignMissionProgressSaveData entry = FindOrAdd(entries, missionId);
            if (entry.available) return false;
            entry.available = true;
            Save(profile, entries);
            return true;
        }

        public bool SetPendingResume(string missionId, bool pending, int attemptOrdinal)
        {
            RequireMissionId(missionId);
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            PlayerProfileSaveData profile = _saveService.LoadProfile();
            List<CampaignMissionProgressSaveData> entries = ToList(profile.campaignMissionProgress);
            CampaignMissionProgressSaveData entry = FindOrAdd(entries, missionId);
            if (entry.pendingResume == pending && entry.lastAttemptOrdinal == attemptOrdinal) return false;
            entry.pendingResume = pending;
            entry.lastAttemptOrdinal = attemptOrdinal;
            Save(profile, entries);
            return true;
        }

        public bool Settle(
            string missionId,
            string sessionToken,
            int attemptOrdinal,
            bool firstClear,
            byte stars,
            int completionMilliseconds,
            string nextMissionId)
        {
            RequireMissionId(missionId);
            if (string.IsNullOrWhiteSpace(sessionToken)) throw new ArgumentException("Session token is required.", nameof(sessionToken));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            if (completionMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(completionMilliseconds));
            string settlementToken = sessionToken.Trim() + ":" + attemptOrdinal;

            PlayerProfileSaveData profile = _saveService.LoadProfile();
            List<CampaignMissionProgressSaveData> entries = ToList(profile.campaignMissionProgress);
            CampaignMissionProgressSaveData entry = FindOrAdd(entries, missionId);
            if (entry.lastSettledToken == settlementToken) return false;

            entry.available = true;
            entry.firstClearCompleted |= firstClear;
            entry.firstClearRewardSettled |= firstClear;
            if (!firstClear) entry.successfulReplayCount++;
            entry.bestStars = Math.Max(entry.bestStars, stars);
            if (completionMilliseconds > 0 &&
                (entry.bestCompletionMilliseconds == 0 || completionMilliseconds < entry.bestCompletionMilliseconds))
                entry.bestCompletionMilliseconds = completionMilliseconds;
            entry.lastSettledToken = settlementToken;
            entry.pendingResume = false;
            entry.lastAttemptOrdinal = attemptOrdinal;
            if (!string.IsNullOrWhiteSpace(nextMissionId)) FindOrAdd(entries, nextMissionId.Trim()).available = true;
            Save(profile, entries);
            return true;
        }

        private void Save(PlayerProfileSaveData profile, List<CampaignMissionProgressSaveData> entries)
        {
            profile.campaignMissionProgress = Normalize(entries.ToArray());
            _saveService.SaveProfile(profile);
        }

        private static List<CampaignMissionProgressSaveData> ToList(CampaignMissionProgressSaveData[] source) =>
            new(Normalize(source));

        private static CampaignMissionProgressSaveData FindOrAdd(
            List<CampaignMissionProgressSaveData> entries,
            string missionId)
        {
            for (int index = 0; index < entries.Count; index++)
                if (entries[index].missionId == missionId) return entries[index];
            CampaignMissionProgressSaveData entry = new() { missionId = missionId };
            entries.Add(entry);
            return entry;
        }

        private static CampaignMissionProgressSaveData[] Normalize(CampaignMissionProgressSaveData[] source)
        {
            Dictionary<string, CampaignMissionProgressSaveData> unique = new(StringComparer.Ordinal);
            if (source != null)
            {
                for (int index = 0; index < source.Length; index++)
                {
                    CampaignMissionProgressSaveData entry = source[index];
                    if (entry == null || entry.schemaVersion > CurrentEntrySchemaVersion ||
                        string.IsNullOrWhiteSpace(entry.missionId)) continue;
                    entry.schemaVersion = CurrentEntrySchemaVersion;
                    entry.missionId = entry.missionId.Trim();
                    entry.bestStars = Math.Clamp(entry.bestStars, 0, 3);
                    entry.bestCompletionMilliseconds = Math.Max(0, entry.bestCompletionMilliseconds);
                    entry.successfulReplayCount = Math.Max(0, entry.successfulReplayCount);
                    entry.lastAttemptOrdinal = Math.Max(0, entry.lastAttemptOrdinal);
                    entry.lastSettledToken ??= string.Empty;
                    unique.TryAdd(entry.missionId, entry);
                }
            }
            CampaignMissionProgressSaveData[] result = new CampaignMissionProgressSaveData[unique.Count];
            unique.Values.CopyTo(result, 0);
            Array.Sort(result, (left, right) => string.CompareOrdinal(left.missionId, right.missionId));
            return result;
        }

        private static CampaignMissionProgressSaveData[] Clone(CampaignMissionProgressSaveData[] source)
        {
            CampaignMissionProgressSaveData[] result = new CampaignMissionProgressSaveData[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                CampaignMissionProgressSaveData value = source[index];
                result[index] = new CampaignMissionProgressSaveData
                {
                    schemaVersion = value.schemaVersion, missionId = value.missionId, available = value.available,
                    firstClearCompleted = value.firstClearCompleted, bestStars = value.bestStars,
                    bestCompletionMilliseconds = value.bestCompletionMilliseconds,
                    firstClearRewardSettled = value.firstClearRewardSettled,
                    successfulReplayCount = value.successfulReplayCount, lastSettledToken = value.lastSettledToken,
                    pendingResume = value.pendingResume, lastAttemptOrdinal = value.lastAttemptOrdinal
                };
            }
            return result;
        }

        private static void RequireMissionId(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId) || !missionId.StartsWith("saga.", StringComparison.Ordinal))
                throw new ArgumentException("A scoped mission id is required.", nameof(missionId));
        }
    }
}
