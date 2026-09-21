using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public struct SkirmishPublicationEvidence
    {
        public string CatalogId;
        public string DefinitionId;
        public string ContentHash;
        public uint SetupHash;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public bool ManualWin;
        public bool AriaMatrix;
        public bool EdgeFixtures;
        public bool RecoveryEvidence;
        public bool DeviceEvidence;
        public bool AssetExists;
    }

    public struct SkirmishPlayableFlipRequest
    {
        public string CatalogId;
        public string DefinitionId;
        public string ContentHash;
        public uint SetupHash;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public string EvidenceDirectory;
        public string[] RequiredRelativeFiles;
        public bool ConfirmWrite;
    }

    public static class SkirmishPublicationValidator
    {
        public const int LegacyPrototypeCompatibilityVersion = 1;
        public const string S002CatalogId = "S002";

        public static bool IsFirstVisitAllowed(SkirmishDifficultyId difficulty, SkirmishSizeId size) =>
            difficulty == SkirmishDifficultyId.Regular && size == SkirmishSizeId.Standard;

        public static bool TryEvaluate(
            SkirmishPublicationRowConfig row,
            SkirmishScenarioDefinitionConfig definition,
            SkirmishResolvedSetup setup,
            in SkirmishPublicationEvidence evidence,
            out SkirmishPublicationStatus status,
            out List<SkirmishCompileReason> reasons)
        {
            reasons = new List<SkirmishCompileReason>();
            status = SkirmishPublicationStatus.Planned;
            if (string.IsNullOrEmpty(row.CatalogId) && definition == null && setup == null)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingDefinition, "catalogId", "Publication row is empty."));
                return false;
            }

            string catalogId = FirstNonEmpty(row.CatalogId, definition?.CatalogId, setup?.CatalogId);
            string definitionId = FirstNonEmpty(row.DefinitionId, definition?.DefinitionId, setup?.DefinitionId);
            if (catalogId != S002CatalogId)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "catalogId", catalogId));
                return false;
            }

            if (definition != null && definition.CatalogId != catalogId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "definition.catalogId", definition.CatalogId));
            if (setup != null && setup.CatalogId != catalogId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "setup.catalogId", setup.CatalogId));
            if (!string.IsNullOrEmpty(definitionId) && definition != null && definition.DefinitionId != definitionId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "definitionId", definition.DefinitionId));
            if (definition != null && setup != null && definition.ContentHash != setup.ContentHash)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "contentHash", setup.ContentHash));
            if (!string.IsNullOrEmpty(evidence.ContentHash) &&
                setup != null &&
                evidence.ContentHash != setup.ContentHash)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "evidence.contentHash", evidence.ContentHash));
            }

            if (!string.IsNullOrEmpty(evidence.DefinitionId) && evidence.DefinitionId != definitionId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "evidence.definitionId", evidence.DefinitionId));
            if (setup != null && evidence.SetupHash != 0 && evidence.SetupHash != setup.SetupHash)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MatrixMismatch, "evidence.setupHash", evidence.SetupHash.ToString("X8")));

            SkirmishDifficultyId difficulty = evidence.DifficultyId != SkirmishDifficultyId.None
                ? evidence.DifficultyId
                : setup != null ? setup.DifficultyId : SkirmishDifficultyId.None;
            SkirmishSizeId size = evidence.SizeId != SkirmishSizeId.None
                ? evidence.SizeId
                : setup != null ? setup.SizeId : SkirmishSizeId.None;
            if (!IsFirstVisitAllowed(difficulty, size))
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.UnsupportedSize,
                    "difficulty+size",
                    "First visit remains Regular Standard."));
            }

            if (reasons.Count > 0)
            {
                status = row.Status == SkirmishPublicationStatus.InProgress
                    ? SkirmishPublicationStatus.InProgress
                    : SkirmishPublicationStatus.Planned;
                return false;
            }

            status = SkirmishPublicationStatus.InProgress;
            if (evidence.AssetExists && !HasCompleteEvidence(in evidence))
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.MissingReadiness,
                    "evidence",
                    "An asset's existence cannot set Playable."));
                return true;
            }

            if (!HasCompleteEvidence(in evidence))
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.MissingReadiness,
                    "evidence",
                    "Manual, ARIA, edge, recovery and device evidence are required."));
                return true;
            }

            status = SkirmishPublicationStatus.Playable;
            return true;
        }

        public static bool HasCompleteEvidence(in SkirmishPublicationEvidence evidence) =>
            evidence.ManualWin &&
            evidence.AriaMatrix &&
            evidence.EdgeFixtures &&
            evidence.RecoveryEvidence &&
            evidence.DeviceEvidence;

        public static bool TryEvaluatePlayableFlip(
            SkirmishPublicationRowConfig row,
            SkirmishScenarioDefinitionConfig definition,
            SkirmishResolvedSetup setup,
            in SkirmishPlayableFlipRequest request,
            Func<string, bool> fileExists,
            out SkirmishPublicationStatus status,
            out List<SkirmishCompileReason> reasons)
        {
            var evidence = new SkirmishPublicationEvidence
            {
                CatalogId = request.CatalogId,
                DefinitionId = request.DefinitionId,
                ContentHash = request.ContentHash,
                SetupHash = request.SetupHash,
                DifficultyId = request.DifficultyId,
                SizeId = request.SizeId,
                AssetExists = true
            };
            if (!TryEvaluate(row, definition, setup, in evidence, out status, out reasons))
                return false;

            reasons.Clear();
            if (string.IsNullOrEmpty(request.EvidenceDirectory))
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.MissingReadiness,
                    "evidenceDirectory",
                    "Game View evidence path is required."));
                status = SkirmishPublicationStatus.InProgress;
                return true;
            }

            if (request.RequiredRelativeFiles == null || request.RequiredRelativeFiles.Length == 0)
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.MissingReadiness,
                    "evidenceFiles",
                    "Required Game View evidence files are missing from the flip request."));
                status = SkirmishPublicationStatus.InProgress;
                return true;
            }

            if (fileExists == null)
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.MissingReadiness,
                    "evidenceFiles",
                    "Evidence path probe is required."));
                status = SkirmishPublicationStatus.InProgress;
                return true;
            }

            for (int i = 0; i < request.RequiredRelativeFiles.Length; i++)
            {
                string relative = request.RequiredRelativeFiles[i];
                string path = CombineEvidencePath(request.EvidenceDirectory, relative);
                if (!fileExists(path))
                {
                    reasons.Add(new SkirmishCompileReason(
                        SkirmishReasonCode.MissingReadiness,
                        "evidenceFiles",
                        path));
                    status = SkirmishPublicationStatus.InProgress;
                    return true;
                }
            }

            status = SkirmishPublicationStatus.Playable;
            return true;
        }

        public static string CombineEvidencePath(string directory, string relativeFile)
        {
            if (string.IsNullOrEmpty(directory))
                return relativeFile ?? string.Empty;
            if (string.IsNullOrEmpty(relativeFile))
                return directory;
            if (directory.EndsWith("/", StringComparison.Ordinal) || directory.EndsWith("\\", StringComparison.Ordinal))
                return directory + relativeFile;
            return directory + "/" + relativeFile;
        }

        public static void ApplyLegacyPrototypeCompatibility(
            SkirmishBattleCatalogEntry[] entries,
            int compatibilityVersion)
        {
            if (entries == null || compatibilityVersion != LegacyPrototypeCompatibilityVersion)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                SkirmishBattleCatalogEntry entry = entries[i];
                if (entry.ScenarioId == SkirmishBattleCatalogConfig.DesertBaseScenarioId)
                {
                    entry.Status = SkirmishBattleCatalogStatus.Playable;
                    entry.PlayableScenarioIndex = SkirmishPresetConfig.DesertBaseScenarioIndex;
                    entry.TitleKey = "ui.skirmish.base_assault_map";
                    entry.TitleEnglish = "DESERT BASE";
                    entry.DescriptionEnglish =
                        "Advance from the western base. Fight through the city or approach around its edge.";
                    entry.DescriptionFarsi =
                        "از پایگاه غربی جلو برو؛ از وسط شهر یا مسیر کنار شهر به دشمن برس.";
                    entries[i] = entry;
                }
                else if (entry.ScenarioId == SkirmishBattleCatalogConfig.CityCrossroadsScenarioId)
                {
                    entry.Status = SkirmishBattleCatalogStatus.Playable;
                    entry.PlayableScenarioIndex = SkirmishPresetConfig.CityCrossroadsScenarioIndex;
                    entry.TitleKey = "ui.skirmish.city_crossroads_map";
                    entry.TitleEnglish = "CITY CROSSROADS";
                    entry.DescriptionEnglish =
                        "Attack from the north toward the southern base. Use city streets or flank around the blocks.";
                    entry.DescriptionFarsi =
                        "از شمال شهر به پایگاه جنوبی حمله کن؛ از خیابون‌ها یا مسیرهای کناری جلو برو.";
                    entries[i] = entry;
                }
                else if (entry.ScenarioId == SkirmishBattleCatalogConfig.IndustrialBasinScenarioId)
                {
                    entry.Status = SkirmishBattleCatalogStatus.Playable;
                    entry.PlayableScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
                    entry.TitleKey = "ui.skirmish.industrial_basin_map";
                    entry.TitleEnglish = "INDUSTRIAL BASIN";
                    entry.DescriptionEnglish =
                        "Deploy in the northern clearing and push southeast along the industrial approach. Defend your base before advancing.";
                    entry.DescriptionFarsi =
                        "در محوطه باز شمالی مستقر شو و از مسیر صنعتی به سمت جنوب‌شرق پیش برو. قبل از پیشروی از پایگاهت دفاع کن.";
                    entries[i] = entry;
                }
                else
                {
                    entry.Status = SkirmishBattleCatalogStatus.Planned;
                    entry.PlayableScenarioIndex = -1;
                    SkirmishExpandedCopyProjection.ApplyLibraryCopy(ref entry);
                    entries[i] = entry;
                }
            }
        }

        private static string FirstNonEmpty(string a, string b, string c)
        {
            if (!string.IsNullOrEmpty(a))
                return a;
            if (!string.IsNullOrEmpty(b))
                return b;
            return c ?? string.Empty;
        }
    }
}
