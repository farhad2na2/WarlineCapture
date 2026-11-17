using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    /// <summary>
    /// Pure rules for the Editor-only validation library (WARLINE_VALIDATION_LIBRARY=1). Authored missions stay
    /// Planned (hidden) in the shipped library until their acceptance evidence exists, so the real menu path cannot
    /// launch them. The validation library presents those Planned authored rows as launchable cards in an Editor-memory
    /// catalog only: nothing here writes the shipped catalog asset, publishes a mission or grants readiness.
    /// </summary>
    public static class SkirmishValidationLibraryPlan
    {
        public const string Marker = "[SkirmishValidationLibrary]";
        public const string EnvironmentVariable = "WARLINE_VALIDATION_LIBRARY";

        /// <summary>Transport token for candidate rows; the same base the Editor availability scope uses (1000 + mission number).</summary>
        public const int IndexBase = 1000;

        public const string TitleBadge = "[VALIDATION] ";
        public const string DescriptionBadge = "Validation candidate, not published. ";

        public readonly struct AuthoredSource
        {
            public readonly string CatalogId, DefinitionId;
            public readonly int ContentVersion;
            public readonly bool HasCampaignMap, HasMapLayout;

            public AuthoredSource(string catalogId, string definitionId, int contentVersion, bool hasCampaignMap, bool hasMapLayout)
            {
                CatalogId = catalogId;
                DefinitionId = definitionId;
                ContentVersion = contentVersion;
                HasCampaignMap = hasCampaignMap;
                HasMapLayout = hasMapLayout;
            }
        }

        public sealed class Result
        {
            /// <summary>Every shipped row in the shipped order; flipped rows are Playable with a transport index and a validation badge.</summary>
            public SkirmishBattleCatalogEntry[] Rows;
            /// <summary>Catalog IDs that were Planned and are now presented, in shipped order.</summary>
            public readonly List<string> Planned = new();
            /// <summary>Planned rows that have an authored definition but cannot be presented, as "id:reason".</summary>
            public readonly List<string> Rejected = new();
            /// <summary>Planned rows with no authored definition at all (nothing to validate yet).</summary>
            public int WithoutDefinition;
        }

        public static int IndexFor(string catalogId) =>
            IndexBase + int.Parse(catalogId.Substring(1), CultureInfo.InvariantCulture);

        public static Result Build(IReadOnlyList<SkirmishBattleCatalogEntry> shipped, IReadOnlyList<AuthoredSource> authored)
        {
            var result = new Result();
            int count = shipped?.Count ?? 0;
            result.Rows = new SkirmishBattleCatalogEntry[count];
            var rowCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var takenIndexes = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                result.Rows[i] = shipped[i];
                if (shipped[i].ScenarioId != null)
                    rowCounts[shipped[i].ScenarioId] = rowCounts.TryGetValue(shipped[i].ScenarioId, out int seen) ? seen + 1 : 1;
                if (shipped[i].IsPlayable) takenIndexes.Add(shipped[i].PlayableScenarioIndex);
            }

            for (int i = 0; i < count; i++)
            {
                var row = result.Rows[i];
                if (row.IsPlayable) continue; // already visible: not a validation candidate
                string id = row.ScenarioId;
                if (!SkirmishMissionDispatch.Canonical(id)) { result.Rejected.Add((id ?? "?") + ":notCanonicalId"); continue; }
                if (!TryFindSingle(authored, id, out var source, out string ambiguity))
                {
                    if (ambiguity != null) result.Rejected.Add(id + ":" + ambiguity);
                    else result.WithoutDefinition++;
                    continue;
                }
                string reason = Reject(row, source, rowCounts[id], takenIndexes);
                if (reason != null) { result.Rejected.Add(id + ":" + reason); continue; }

                int index = IndexFor(id);
                takenIndexes.Add(index);
                row.Status = SkirmishBattleCatalogStatus.Playable;
                row.PlayableScenarioIndex = index;
                row.DefinitionId = source.DefinitionId;
                row.ContentVersion = source.ContentVersion;
                // The card shows it is a validation candidate. A localization key would hide the English badge, so
                // the key is cleared and the English title carries the badge in every locale.
                string title = string.IsNullOrEmpty(row.TitleEnglish) ? id : row.TitleEnglish;
                row.TitleEnglish = title.StartsWith(TitleBadge, StringComparison.Ordinal) ? title : TitleBadge + title;
                row.TitleKey = string.Empty;
                row.DescriptionEnglish = Badge(row.DescriptionEnglish);
                row.DescriptionFarsi = Badge(row.DescriptionFarsi);
                result.Rows[i] = row;
                result.Planned.Add(id);
            }
            return result;
        }

        private static string Badge(string description)
        {
            if (string.IsNullOrEmpty(description)) return DescriptionBadge.TrimEnd();
            return description.StartsWith(DescriptionBadge, StringComparison.Ordinal) ? description : DescriptionBadge + description;
        }

        private static bool TryFindSingle(IReadOnlyList<AuthoredSource> authored, string id, out AuthoredSource source, out string ambiguity)
        {
            source = default;
            ambiguity = null;
            int matches = 0;
            if (authored != null)
                for (int i = 0; i < authored.Count; i++)
                    if (authored[i].CatalogId == id) { source = authored[i]; matches++; }
            if (matches > 1) ambiguity = "ambiguousAuthoredDefinition";
            return matches == 1;
        }

        private static string Reject(in SkirmishBattleCatalogEntry row, in AuthoredSource source, int rowsWithId, HashSet<int> takenIndexes)
        {
            if (rowsWithId != 1) return "duplicateLibraryRow";
            if (row.Status != SkirmishBattleCatalogStatus.Planned || row.PlayableScenarioIndex >= 0) return "malformedPlannedRow";
            if (SkirmishMissionDispatch.TryGetLegacyIndex(row.ScenarioId, out _)) return "legacyIndexMission";
            if (source.ContentVersion <= 0) return "contentVersionNotPositive";
            if (source.DefinitionId != SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(row.ScenarioId)).Value)
                return "definitionIdMismatch";
            if (!source.HasCampaignMap || !source.HasMapLayout) return "mapOrLayoutNotBound";
            if (takenIndexes.Contains(IndexFor(row.ScenarioId))) return "transportIndexCollision";
            return null;
        }

        public static string FormatEnabled(Result result, string trigger) =>
            Marker + " enabled=1 planned=" + (result.Planned.Count == 0 ? "none" : string.Join(",", result.Planned)) +
            " rejected=" + (result.Rejected.Count == 0 ? "none" : string.Join(",", result.Rejected)) +
            " withoutDefinition=" + result.WithoutDefinition + " trigger=" + trigger +
            " scope=EditorMemoryOnly shippedCatalogModified=0 published=0 readinessGranted=0";

        public static string FormatDisabled(string reason) => Marker + " enabled=0 reason=" + reason;
    }
}
