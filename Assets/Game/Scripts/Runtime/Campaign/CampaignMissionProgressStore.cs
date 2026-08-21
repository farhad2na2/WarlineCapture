using System;
using System.Collections.Generic;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    public sealed class CampaignMissionProgressStore
    {
        public const int CurrentEntrySchemaVersion = 2;
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
            return SettleWithRewards(
                missionId, sessionToken, attemptOrdinal, firstClear, stars, completionMilliseconds,
                nextMissionId, Array.Empty<CampaignMissionRewardGrant>(), false).Applied;
        }

        public CampaignMissionSettlementReceipt SettleWithRewards(
            string missionId,
            string sessionToken,
            int attemptOrdinal,
            bool firstClear,
            byte stars,
            int completionMilliseconds,
            string nextMissionId,
            CampaignMissionRewardGrant[] rewards,
            bool requirePriorFirstClear = true)
        {
            RequireMissionId(missionId);
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new ArgumentException("Session token is required.", nameof(sessionToken));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));
            if (completionMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(completionMilliseconds));
            rewards ??= Array.Empty<CampaignMissionRewardGrant>();
            ValidateRewards(rewards);
            string settlementToken = sessionToken.Trim() + ":" + attemptOrdinal;

            PlayerProfileSaveData profile = _saveService.LoadProfile();
            List<CampaignMissionProgressSaveData> entries = ToList(profile.campaignMissionProgress);
            CampaignMissionProgressSaveData entry = FindOrAdd(entries, missionId);
            if (Contains(entry.settledTokens, settlementToken))
                return CampaignMissionSettlementReceipt.Duplicate(firstClear);
            if (firstClear && entry.firstClearRewardSettled)
                return CampaignMissionSettlementReceipt.Duplicate(true);
            if (!firstClear && requirePriorFirstClear && !entry.firstClearCompleted)
                return CampaignMissionSettlementReceipt.Rejected("replay-before-first-clear", false);

            int commanderXp = 0;
            int credits = 0;
            int materials = 0;
            int fuel = 0;
            for (int index = 0; index < rewards.Length; index++)
            {
                CampaignMissionRewardGrant reward = rewards[index];
                if (reward.Kind == MissionRewardKind.None)
                    commanderXp = checked(commanderXp + reward.Amount);
                else if (reward.Kind == MissionRewardKind.Credits)
                    credits = checked(credits + reward.Amount);
                else if (reward.Kind == MissionRewardKind.Materials)
                    materials = checked(materials + reward.Amount);
                else if (reward.Kind == MissionRewardKind.Fuel)
                    fuel = checked(fuel + reward.Amount);
            }
            profile.commanderXp = checked(profile.commanderXp + commanderXp);
            profile.credits = checked(profile.credits + credits);
            profile.materials = checked(profile.materials + materials);
            profile.fuel = checked(profile.fuel + fuel);

            entry.available = true;
            entry.firstClearCompleted |= firstClear;
            entry.firstClearRewardSettled |= firstClear;
            if (!firstClear) entry.successfulReplayCount = checked(entry.successfulReplayCount + 1);
            entry.bestStars = Math.Max(entry.bestStars, stars);
            if (completionMilliseconds > 0 &&
                (entry.bestCompletionMilliseconds == 0 || completionMilliseconds < entry.bestCompletionMilliseconds))
                entry.bestCompletionMilliseconds = completionMilliseconds;
            entry.lastSettledToken = settlementToken;
            entry.settledTokens = Append(entry.settledTokens, settlementToken);
            entry.pendingResume = false;
            entry.lastAttemptOrdinal = attemptOrdinal;
            if (firstClear && !string.IsNullOrWhiteSpace(nextMissionId))
                FindOrAdd(entries, nextMissionId.Trim()).available = true;
            Save(profile, entries);
            return CampaignMissionSettlementReceipt.Accepted(firstClear, commanderXp, credits, materials, fuel);
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
                    entry.settledTokens = NormalizeTokens(entry.settledTokens, entry.lastSettledToken);
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
                    settledTokens = (string[])value.settledTokens.Clone(),
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

        private static void ValidateRewards(CampaignMissionRewardGrant[] rewards)
        {
            HashSet<string> identities = new(StringComparer.Ordinal);
            for (int index = 0; index < rewards.Length; index++)
            {
                CampaignMissionRewardGrant reward = rewards[index];
                if (reward.Amount <= 0 || reward.Kind == MissionRewardKind.Intel)
                    throw new ArgumentException("Settlement rewards must be positive and M01 cannot grant Intel.", nameof(rewards));
                string identity = reward.Kind == MissionRewardKind.None
                    ? reward.RewardConfigId?.Trim() ?? string.Empty
                    : reward.Kind.ToString();
                if (reward.Kind == MissionRewardKind.None && identity != "reward.commander_xp")
                    throw new ArgumentException("The only supported custom M01 reward is reward.commander_xp.", nameof(rewards));
                if (!identities.Add(identity))
                    throw new ArgumentException("Duplicate settlement reward identity.", nameof(rewards));
            }
        }

        private static bool Contains(string[] values, string value) =>
            values != null && Array.IndexOf(values, value) >= 0;

        private static string[] Append(string[] values, string value)
        {
            string[] result = new string[(values?.Length ?? 0) + 1];
            if (values != null) Array.Copy(values, result, values.Length);
            result[^1] = value;
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] NormalizeTokens(string[] source, string legacyLastToken)
        {
            SortedSet<string> tokens = new(StringComparer.Ordinal);
            if (source != null)
                for (int index = 0; index < source.Length; index++)
                    if (!string.IsNullOrWhiteSpace(source[index])) tokens.Add(source[index].Trim());
            if (!string.IsNullOrWhiteSpace(legacyLastToken)) tokens.Add(legacyLastToken.Trim());
            string[] result = new string[tokens.Count];
            tokens.CopyTo(result);
            return result;
        }
    }

    public readonly struct CampaignMissionRewardGrant
    {
        public CampaignMissionRewardGrant(MissionRewardKind kind, string rewardConfigId, int amount)
        {
            Kind = kind;
            RewardConfigId = rewardConfigId ?? string.Empty;
            Amount = amount;
        }

        public MissionRewardKind Kind { get; }
        public string RewardConfigId { get; }
        public int Amount { get; }
    }

    public readonly struct CampaignMissionSettlementReceipt
    {
        private CampaignMissionSettlementReceipt(
            bool applied, bool duplicate, bool firstClear, string reason,
            int commanderXp, int credits, int materials, int fuel)
        {
            Applied = applied;
            IsDuplicate = duplicate;
            FirstClear = firstClear;
            Reason = reason;
            CommanderXpGranted = commanderXp;
            CreditsGranted = credits;
            MaterialsGranted = materials;
            FuelGranted = fuel;
        }

        public bool Applied { get; }
        public bool IsDuplicate { get; }
        public bool FirstClear { get; }
        public string Reason { get; }
        public int CommanderXpGranted { get; }
        public int CreditsGranted { get; }
        public int MaterialsGranted { get; }
        public int FuelGranted { get; }

        internal static CampaignMissionSettlementReceipt Accepted(
            bool firstClear, int commanderXp, int credits, int materials, int fuel) =>
            new(true, false, firstClear, "settled", commanderXp, credits, materials, fuel);

        internal static CampaignMissionSettlementReceipt Duplicate(bool firstClear) =>
            new(false, true, firstClear, "already-settled", 0, 0, 0, 0);

        internal static CampaignMissionSettlementReceipt Rejected(string reason, bool firstClear) =>
            new(false, false, firstClear, reason, 0, 0, 0, 0);
    }
}
