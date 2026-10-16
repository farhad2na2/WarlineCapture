using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    public struct SkirmishMedalDifficultyTotals
    {
        public int Recruit;
        public int Regular;
        public int Veteran;
        public int Commander;
    }

    public readonly struct SkirmishMedalAwardResult
    {
        public SkirmishMedalAwardResult(string scenarioId, SkirmishDifficultyId difficulty, bool isFirstAward)
        {
            ScenarioId = scenarioId ?? string.Empty;
            Difficulty = difficulty;
            IsFirstAward = isFirstAward;
        }

        public string ScenarioId { get; }
        public SkirmishDifficultyId Difficulty { get; }
        public bool IsFirstAward { get; }
    }

    /// <summary>
    /// Pure medal book. One row per catalog id and difficulty. The first Victory wins;
    /// later victories, including a newer contentVersion, leave the original row in place.
    /// </summary>
    public sealed class SkirmishMedalLedger
    {
        private readonly List<SkirmishMedalSaveData> _rows = new();
        private readonly List<SkirmishBattleCatalogEntry> _catalog = new();

        public SkirmishMedalLedger(SkirmishMedalSaveData[] saved, IReadOnlyList<SkirmishBattleCatalogEntry> catalog = null)
        {
            if (saved != null)
            {
                for (int i = 0; i < saved.Length; i++)
                {
                    SkirmishMedalSaveData row = saved[i];
                    if (row == null || string.IsNullOrEmpty(row.catalogId) || !IsMedalDifficulty((SkirmishDifficultyId)row.difficulty))
                        continue;
                    if (IndexOf(row.catalogId, (SkirmishDifficultyId)row.difficulty) >= 0)
                        continue;
                    _rows.Add(Clone(row));
                }
            }

            if (catalog != null)
            {
                for (int i = 0; i < catalog.Count; i++)
                    _catalog.Add(catalog[i]);
            }
        }

        public void Replace(SkirmishMedalSaveData[] saved)
        {
            _rows.Clear();
            if (saved == null)
                return;
            for (int i = 0; i < saved.Length; i++)
            {
                SkirmishMedalSaveData row = saved[i];
                if (row == null || string.IsNullOrEmpty(row.catalogId) || !IsMedalDifficulty((SkirmishDifficultyId)row.difficulty))
                    continue;
                if (IndexOf(row.catalogId, (SkirmishDifficultyId)row.difficulty) >= 0)
                    continue;
                _rows.Add(Clone(row));
            }
        }

        public bool TryAward(SkirmishResultReceipt receipt, out SkirmishMedalAwardResult award) =>
            TryAward(receipt, DateTime.UtcNow, out award);

        public bool TryAward(SkirmishResultReceipt receipt, DateTime earnedUtc, out SkirmishMedalAwardResult award)
        {
            award = default;
            if (!IsEligible(receipt))
                return false;
            if (!TryFindPlayable(receipt.CatalogId, out SkirmishBattleCatalogEntry entry))
                return false;

            int existing = IndexOf(entry.ScenarioId, receipt.DifficultyId);
            if (existing >= 0)
            {
                award = new SkirmishMedalAwardResult(entry.ScenarioId, receipt.DifficultyId, false);
                return true;
            }

            _rows.Add(new SkirmishMedalSaveData
            {
                catalogId = entry.ScenarioId,
                difficulty = (byte)receipt.DifficultyId,
                contentVersion = receipt.ContentVersion,
                firstEarnedUtc = earnedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                sessionId = receipt.SessionId ?? string.Empty
            });
            award = new SkirmishMedalAwardResult(entry.ScenarioId, receipt.DifficultyId, true);
            return true;
        }

        public bool Has(string catalogId, SkirmishDifficultyId difficulty) =>
            IndexOf(catalogId, difficulty) >= 0;

        public int CountFor(string mapId) => CountFor(mapId, null);

        public int CountFor(string mapId, string objectiveId)
        {
            if (string.IsNullOrEmpty(mapId))
                return 0;
            int count = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalog[i];
                if (!string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(objectiveId) &&
                    !string.Equals(entry.ObjectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!seen.Add(entry.ScenarioId))
                    continue;
                if (HasAny(entry.ScenarioId))
                    count++;
            }

            return count;
        }

        public SkirmishMedalDifficultyTotals TotalsByDifficulty()
        {
            var totals = new SkirmishMedalDifficultyTotals();
            for (int i = 0; i < _rows.Count; i++)
            {
                switch ((SkirmishDifficultyId)_rows[i].difficulty)
                {
                    case SkirmishDifficultyId.Recruit:
                        totals.Recruit++;
                        break;
                    case SkirmishDifficultyId.Regular:
                        totals.Regular++;
                        break;
                    case SkirmishDifficultyId.Veteran:
                        totals.Veteran++;
                        break;
                    case SkirmishDifficultyId.Commander:
                        totals.Commander++;
                        break;
                }
            }

            return totals;
        }

        public int BattlesWon()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _rows.Count; i++)
            {
                if (!string.IsNullOrEmpty(_rows[i].catalogId))
                    seen.Add(_rows[i].catalogId);
            }

            return seen.Count;
        }

        public SkirmishMedalSaveData[] ToSaveData()
        {
            var copy = new SkirmishMedalSaveData[_rows.Count];
            for (int i = 0; i < _rows.Count; i++)
                copy[i] = Clone(_rows[i]);
            return copy;
        }

        public static bool IsEligible(SkirmishResultReceipt receipt)
        {
            if (receipt == null)
                return false;
            if (receipt.IsCustom != 0 || receipt.IsLegacy != 0)
                return false;
            if (receipt.Outcome != SkirmishOutcomeKind.Victory)
                return false;
            if (receipt.Reason == SkirmishEndReasonKind.Surrender)
                return false;
            if (!IsMedalDifficulty(receipt.DifficultyId))
                return false;
            if (string.IsNullOrEmpty(receipt.CatalogId) || string.IsNullOrEmpty(receipt.SessionId))
                return false;
            return true;
        }

        private bool HasAny(string catalogId)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (string.Equals(_rows[i].catalogId, catalogId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool TryFindPlayable(string catalogId, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!string.Equals(_catalog[i].ScenarioId, catalogId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_catalog[i].IsPlayable)
                {
                    entry = _catalog[i];
                    return true;
                }

                entry = default;
                return false;
            }

            entry = default;
            return false;
        }

        private int IndexOf(string catalogId, SkirmishDifficultyId difficulty)
        {
            if (string.IsNullOrEmpty(catalogId) || !IsMedalDifficulty(difficulty))
                return -1;
            byte raw = (byte)difficulty;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].difficulty == raw &&
                    string.Equals(_rows[i].catalogId, catalogId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static bool IsMedalDifficulty(SkirmishDifficultyId difficulty) =>
            difficulty >= SkirmishDifficultyId.Recruit && difficulty <= SkirmishDifficultyId.Commander;

        private static SkirmishMedalSaveData Clone(SkirmishMedalSaveData row) =>
            new()
            {
                catalogId = row.catalogId ?? string.Empty,
                difficulty = row.difficulty,
                contentVersion = row.contentVersion,
                firstEarnedUtc = row.firstEarnedUtc ?? string.Empty,
                sessionId = row.sessionId ?? string.Empty
            };
    }
}
