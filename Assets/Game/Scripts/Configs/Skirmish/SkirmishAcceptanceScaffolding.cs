using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public readonly struct SkirmishAriaAcceptancePayload
    {
        public readonly string CatalogId;
        public readonly string DefinitionId;
        public readonly int ContentVersion;
        public readonly SkirmishSizeId SizeId;
        public readonly SkirmishDifficultyId DifficultyId;
        public readonly int Seed;
        public readonly string Locale;

        public SkirmishAriaAcceptancePayload(
            string catalogId,
            string definitionId,
            int contentVersion,
            SkirmishSizeId sizeId,
            SkirmishDifficultyId difficultyId,
            int seed,
            string locale)
        {
            CatalogId = catalogId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            ContentVersion = contentVersion;
            SizeId = sizeId;
            DifficultyId = difficultyId;
            Seed = seed;
            Locale = locale ?? string.Empty;
        }

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrEmpty(CatalogId) || string.IsNullOrEmpty(DefinitionId))
            {
                error = "Definition identity is required.";
                return false;
            }

            if (SizeId == SkirmishSizeId.None || DifficultyId == SkirmishDifficultyId.None)
            {
                error = "Size and difficulty are required.";
                return false;
            }

            if (Seed <= 0)
            {
                error = "Seed must be a positive integer.";
                return false;
            }

            if (!string.Equals(Locale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) &&
                !string.Equals(Locale, GameLocalization.PersianLocaleCode, StringComparison.Ordinal))
            {
                error = "Use catalog locale en or fa-IR.";
                return false;
            }

            error = null;
            return true;
        }

        public bool IsFirstVisitRegularStandard =>
            string.Equals(CatalogId, SkirmishAcceptanceCensusCapture.CatalogId, StringComparison.Ordinal) &&
            string.Equals(DefinitionId, SkirmishAcceptanceCensusCapture.DefinitionId, StringComparison.Ordinal) &&
            SizeId == SkirmishSizeId.Standard &&
            DifficultyId == SkirmishDifficultyId.Regular;

        public string FormatSelectedConfiguration()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "catalog={0} definition={1} version={2} size={3} difficulty={4} seed={5} locale={6}",
                CatalogId,
                DefinitionId,
                ContentVersion.ToString(CultureInfo.InvariantCulture),
                SkirmishIdParsing.ToCode(SizeId),
                SkirmishIdParsing.ToCode(DifficultyId),
                Seed.ToString(CultureInfo.InvariantCulture),
                Locale);
        }

        public static bool TryCreate(
            string catalogId,
            string definitionId,
            int contentVersion,
            SkirmishSizeId sizeId,
            SkirmishDifficultyId difficultyId,
            int seed,
            string locale,
            out SkirmishAriaAcceptancePayload payload,
            out string error)
        {
            payload = new SkirmishAriaAcceptancePayload(
                catalogId,
                definitionId,
                contentVersion,
                sizeId,
                difficultyId,
                seed,
                locale);
            return payload.TryValidate(out error);
        }

        public static bool TryCreateFirstVisitS002(
            int seed,
            string locale,
            out SkirmishAriaAcceptancePayload payload,
            out string error)
        {
            if (!SkirmishAcceptanceCensusCapture.IsRegularStandardAriaSeed(seed))
            {
                payload = default;
                error = "First-visit ARIA seed must be 104731, 130365 or 155923.";
                return false;
            }

            if (!TryCreate(
                    SkirmishAcceptanceCensusCapture.CatalogId,
                    SkirmishAcceptanceCensusCapture.DefinitionId,
                    SkirmishAcceptanceCensusCapture.DefinitionVersion,
                    SkirmishSizeId.Standard,
                    SkirmishDifficultyId.Regular,
                    seed,
                    locale,
                    out payload,
                    out error))
                return false;

            return true;
        }

        public static bool TryCreateFirstVisitS003(
            int seed,
            string locale,
            out SkirmishAriaAcceptancePayload payload,
            out string error)
        {
            if (!SkirmishAcceptanceCensusCapture.IsS003RegularStandardSeed(seed))
            {
                payload = default;
                error = "First-visit ARIA seed must be 104732, 130366 or 155924.";
                return false;
            }

            return TryCreate(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishAcceptanceCensusCapture.S003DefinitionId,
                SkirmishAcceptanceCensusCapture.S003DefinitionVersion,
                SkirmishSizeId.Standard,
                SkirmishDifficultyId.Regular,
                seed,
                locale,
                out payload,
                out error);
        }

        public static bool TryCreateFirstVisitS004(
            int seed,
            string locale,
            out SkirmishAriaAcceptancePayload payload,
            out string error)
        {
            if (!SkirmishAcceptanceCensusCapture.IsS004RegularStandardSeed(seed))
            {
                payload = default;
                error = "First-visit ARIA seed must be 104733, 130367 or 155925.";
                return false;
            }

            return TryCreate(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishAcceptanceCensusCapture.S004DefinitionId,
                SkirmishAcceptanceCensusCapture.S004DefinitionVersion,
                SkirmishSizeId.Standard,
                SkirmishDifficultyId.Regular,
                seed,
                locale,
                out payload,
                out error);
        }
    }

    public sealed class SkirmishAcceptanceCensus
    {
        public string CatalogId;
        public string DefinitionId;
        public int DefinitionVersion;
        public string ContentHash;
        public uint SetupHash;
        public string ConfigHash;
        public string CodeHash;
        public string Size;
        public string Difficulty;
        public int Seed;
        public int PlayerInfantry;
        public int PlayerGround;
        public int PlayerCombat;
        public int PlayerStartingStructures;
        public int EnemyInfantry;
        public int EnemyGround;
        public int EnemyStartingStructures;
        public int MaterialsEach;
        public int OilEach;
        public int UsableFuelEach;
        public int DeadlineSeconds;
        public bool MeasuredLayoutBound;
        public bool PlayableMarked;
        public string Report;
    }

    public readonly struct SkirmishAcceptanceMatrixSlot
    {
        public readonly string RunId;
        public readonly string Kind;
        public readonly string Size;
        public readonly string Difficulty;
        public readonly int Seed;
        public readonly string Locale;
        public readonly string Executor;
        public readonly bool Placeholder;

        public SkirmishAcceptanceMatrixSlot(
            string runId,
            string kind,
            string size,
            string difficulty,
            int seed,
            string locale,
            string executor,
            bool placeholder)
        {
            RunId = runId;
            Kind = kind;
            Size = size;
            Difficulty = difficulty;
            Seed = seed;
            Locale = locale;
            Executor = executor;
            Placeholder = placeholder;
        }
    }

    public static class SkirmishAcceptanceCensusCapture
    {
        public const string CatalogId = "S002";
        public const string DefinitionId = "skirmish.s002";
        public const int DefinitionVersion = 1;
        public const string S003CatalogId = "S003";
        public const string S003DefinitionId = "skirmish.s003";
        public const int S003DefinitionVersion = 1;
        public const string S004CatalogId = "S004";
        public const string S004DefinitionId = "skirmish.s004";
        public const int S004DefinitionVersion = 1;
        public const int CodeHashSchemaVersion = 1;
        public const int FirstVisitSeed = 104731;
        public const string CodeHashIdentity =
            "skirmish.acceptance.code.v1|Game.Configs.SkirmishSetupCompiler.ComputeHash|fnv1a-2166136261-16777619|Game.Configs.SkirmishAcceptanceCensusCapture";

        public static readonly int[] RegularStandardAriaSeeds = { 104731, 130365, 155923 };

        public static bool IsRegularStandardAriaSeed(int seed)
        {
            for (int i = 0; i < RegularStandardAriaSeeds.Length; i++)
            {
                if (RegularStandardAriaSeeds[i] == seed)
                    return true;
            }

            return false;
        }

        public static bool IsS003RegularStandardSeed(int seed)
        {
            int[] seeds = SkirmishS003FirstVisit.RegularStandardSeeds;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (seeds[i] == seed)
                    return true;
            }

            return false;
        }

        public static bool IsS004RegularStandardSeed(int seed)
        {
            int[] seeds = SkirmishS004FirstVisit.RegularStandardSeeds;
            for (int i = 0; i < seeds.Length; i++)
            {
                if (seeds[i] == seed)
                    return true;
            }

            return false;
        }

        public static bool TryCaptureRegularStandard(
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            out SkirmishAcceptanceCensus census,
            out List<SkirmishCompileReason> reasons)
        {
            return TryCapture(
                authored,
                matrix,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                FirstVisitSeed,
                out census,
                out reasons);
        }

        public static bool TryCaptureS003RegularStandard(
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            out SkirmishAcceptanceCensus census,
            out List<SkirmishCompileReason> reasons)
        {
            return TryCaptureDefinition(
                authored,
                authored == null ? null : authored.DefinitionS003,
                S003CatalogId,
                matrix,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS003FirstVisit.SeedA,
                out census,
                out reasons);
        }

        public static bool TryCaptureS004RegularStandard(
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            out SkirmishAcceptanceCensus census,
            out List<SkirmishCompileReason> reasons)
        {
            return TryCaptureDefinition(
                authored,
                authored == null ? null : authored.DefinitionS004,
                S004CatalogId,
                matrix,
                SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard,
                SkirmishS004FirstVisit.SeedA,
                out census,
                out reasons);
        }

        public static bool TryCapture(
            SkirmishExpansionAuthoredSet authored,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            int seed,
            out SkirmishAcceptanceCensus census,
            out List<SkirmishCompileReason> reasons)
        {
            return TryCaptureDefinition(
                authored,
                authored == null ? null : authored.DefinitionS002,
                CatalogId,
                matrix,
                difficulty,
                size,
                seed,
                out census,
                out reasons);
        }

        private static bool TryCaptureDefinition(
            SkirmishExpansionAuthoredSet authored,
            SkirmishScenarioDefinitionConfig definition,
            string missingCatalogId,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            SkirmishDifficultyId difficulty,
            SkirmishSizeId size,
            int seed,
            out SkirmishAcceptanceCensus census,
            out List<SkirmishCompileReason> reasons)
        {
            census = null;
            reasons = new List<SkirmishCompileReason>();
            if (authored == null || definition == null)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingDefinition, "definition", missingCatalogId));
                return false;
            }

            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = definition.RequiredFeatureIds
            };
            if (!SkirmishSetupCompiler.TryCompile(
                    definition,
                    difficulty,
                    size,
                    seed,
                    manifest,
                    matrix,
                    out SkirmishResolvedSetup setup,
                    out reasons))
                return false;

            census = FromSetup(setup);
            return true;
        }

        public static SkirmishAcceptanceCensus FromSetup(SkirmishResolvedSetup setup)
        {
            if (setup == null)
                throw new ArgumentNullException(nameof(setup));

            return new SkirmishAcceptanceCensus
            {
                CatalogId = setup.CatalogId,
                DefinitionId = setup.DefinitionId,
                DefinitionVersion = setup.ContentVersion,
                ContentHash = setup.ContentHash,
                SetupHash = setup.SetupHash,
                ConfigHash = ComputeConfigHash(setup),
                CodeHash = ComputeCodeHash(),
                Size = SkirmishIdParsing.ToCode(setup.SizeId),
                Difficulty = SkirmishIdParsing.ToCode(setup.DifficultyId),
                Seed = setup.Seed,
                PlayerInfantry = setup.PlayerInfantry,
                PlayerGround = setup.PlayerGround,
                PlayerCombat = setup.PlayerCombat,
                PlayerStartingStructures = setup.PlayerStartingStructures,
                EnemyInfantry = setup.EnemyInfantry,
                EnemyGround = setup.EnemyGround,
                EnemyStartingStructures = setup.EnemyStartingStructures,
                MaterialsEach = setup.MaterialsEach,
                OilEach = setup.OilEach,
                UsableFuelEach = setup.UsableFuelEach,
                DeadlineSeconds = setup.DeadlineSeconds,
                MeasuredLayoutBound = setup.MeasuredLayoutBound,
                PlayableMarked = false,
                Report = setup.Report
            };
        }

        public static string ComputeCodeHash() => Sha256Hex(CodeHashIdentity);

        public static string ComputeConfigHash(SkirmishResolvedSetup setup)
        {
            if (setup == null)
                throw new ArgumentNullException(nameof(setup));

            string canonical = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4:X8}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}",
                setup.CatalogId,
                setup.DefinitionId,
                setup.ContentVersion,
                setup.ContentHash,
                setup.SetupHash,
                SkirmishIdParsing.ToCode(setup.SizeId),
                SkirmishIdParsing.ToCode(setup.DifficultyId),
                setup.Seed,
                setup.PlayerInfantry,
                setup.PlayerGround,
                setup.PlayerStartingStructures,
                setup.MaterialsEach,
                setup.DeadlineSeconds,
                setup.MeasuredLayoutBound ? 1 : 0,
                setup.LayoutId);
            return Sha256Hex(canonical);
        }

        public static string FormatLog(SkirmishAcceptanceCensus census)
        {
            if (census == null)
                throw new ArgumentNullException(nameof(census));

            return string.Format(
                CultureInfo.InvariantCulture,
                "[SkirmishAcceptanceCensus] result=Passed catalog={0} definition={1} version={2} code={3} config={4} setup={5:X8} seed={6} playable=0",
                census.CatalogId,
                census.DefinitionId,
                census.DefinitionVersion,
                census.CodeHash,
                census.ConfigHash,
                census.SetupHash,
                census.Seed);
        }

        private static string Sha256Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }
    }

    public static class SkirmishAcceptanceScaffold
    {
        public const string RelativeDirectory = "Design/AgentReports/SkirmishExpansion/S002";
        public const string RelativeEvidenceDirectory = "_Evidence";
        public const string RelativeReportEvidenceDirectory = "Design/AgentReports/SkirmishExpansion/S002/_Evidence";
        public const string PlayingPngFileName = "s002-regular-standard-104731-playing.png";
        public const string GameViewSidecarFileName = "s002-regular-standard-104731-gameview.json";
        public const string RunsFileName = "runs.csv";
        public const string AcceptanceFileName = "acceptance.md";

        public static readonly string S003PlayingPngFileName =
            FormatRegularStandardPlayingPng(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA);

        public static readonly string S003GameViewSidecarFileName =
            FormatRegularStandardGameViewSidecar(
                SkirmishAcceptanceCensusCapture.S003CatalogId,
                SkirmishS003FirstVisit.SeedA);

        public static readonly string S003RelativeReportEvidenceDirectory =
            RelativeReportEvidenceDirectoryFor(SkirmishAcceptanceCensusCapture.S003CatalogId);

        public static readonly string[] RequiredGameViewEvidenceFiles =
        {
            PlayingPngFileName,
            GameViewSidecarFileName
        };

        public static readonly string[] S003RequiredGameViewEvidenceFiles =
        {
            S003PlayingPngFileName,
            S003GameViewSidecarFileName
        };

        public static readonly string S004PlayingPngFileName =
            FormatRegularStandardPlayingPng(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA);

        public static readonly string S004GameViewSidecarFileName =
            FormatRegularStandardGameViewSidecar(
                SkirmishAcceptanceCensusCapture.S004CatalogId,
                SkirmishS004FirstVisit.SeedA);

        public static readonly string S004RelativeReportEvidenceDirectory =
            RelativeReportEvidenceDirectoryFor(SkirmishAcceptanceCensusCapture.S004CatalogId);

        public static readonly string[] S004RequiredGameViewEvidenceFiles =
        {
            S004PlayingPngFileName,
            S004GameViewSidecarFileName
        };

        public static readonly string[] EvidenceDirectoryCandidates =
        {
            RelativeEvidenceDirectory,
            RelativeReportEvidenceDirectory
        };

        public static readonly string[] S003EvidenceDirectoryCandidates =
        {
            RelativeEvidenceDirectory,
            S003RelativeReportEvidenceDirectory
        };

        public static readonly string[] S004EvidenceDirectoryCandidates =
        {
            RelativeEvidenceDirectory,
            S004RelativeReportEvidenceDirectory
        };

        public static string FormatRegularStandardPlayingPng(string catalogId, int seed)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-regular-standard-{1}-playing.png",
                NormalizeCatalogToken(catalogId),
                seed);
        }

        public static string FormatRegularStandardGameViewSidecar(string catalogId, int seed)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-regular-standard-{1}-gameview.json",
                NormalizeCatalogToken(catalogId),
                seed);
        }

        public static string RelativeReportEvidenceDirectoryFor(string catalogId)
        {
            return "Design/AgentReports/SkirmishExpansion/" + (catalogId ?? string.Empty) + "/_Evidence";
        }

        public static string ResolveEvidenceDirectory(string projectRoot, Func<string, bool> fileExists)
        {
            return ResolveEvidenceDirectory(
                projectRoot,
                EvidenceDirectoryCandidates,
                RequiredGameViewEvidenceFiles,
                fileExists);
        }

        public static string ResolveS003EvidenceDirectory(string projectRoot, Func<string, bool> fileExists)
        {
            return ResolveEvidenceDirectory(
                projectRoot,
                S003EvidenceDirectoryCandidates,
                S003RequiredGameViewEvidenceFiles,
                fileExists);
        }

        public static string ResolveS004EvidenceDirectory(string projectRoot, Func<string, bool> fileExists)
        {
            return ResolveEvidenceDirectory(
                projectRoot,
                S004EvidenceDirectoryCandidates,
                S004RequiredGameViewEvidenceFiles,
                fileExists);
        }

        public static string ResolveEvidenceDirectory(
            string projectRoot,
            string[] directoryCandidates,
            string[] requiredRelativeFiles,
            Func<string, bool> fileExists)
        {
            if (string.IsNullOrEmpty(projectRoot))
                return RelativeEvidenceDirectory;
            if (directoryCandidates != null)
            {
                for (int i = 0; i < directoryCandidates.Length; i++)
                {
                    string directory = Path.Combine(projectRoot, directoryCandidates[i]);
                    if (HasRequiredFiles(directory, requiredRelativeFiles, fileExists))
                        return directory;
                }
            }

            return Path.Combine(projectRoot, RelativeEvidenceDirectory);
        }

        public static bool HasRequiredGameViewEvidence(string directory, Func<string, bool> fileExists)
        {
            return HasRequiredFiles(directory, RequiredGameViewEvidenceFiles, fileExists);
        }

        public static bool HasRequiredFiles(string directory, string[] requiredRelativeFiles, Func<string, bool> fileExists)
        {
            if (fileExists == null || string.IsNullOrEmpty(directory) || requiredRelativeFiles == null)
                return false;
            for (int i = 0; i < requiredRelativeFiles.Length; i++)
            {
                if (!fileExists(Path.Combine(directory, requiredRelativeFiles[i])))
                    return false;
            }

            return true;
        }

        private static string NormalizeCatalogToken(string catalogId)
        {
            return string.IsNullOrEmpty(catalogId) ? string.Empty : catalogId.ToLowerInvariant();
        }
        public const string RequiredHeader =
            "run_id,catalog_id,definition_version,code_hash,config_hash,size,difficulty,seed,locale,device,executor,normal_speed,started_at,result,end_reason,duration_seconds,input_violations,human_interventions,trace_path,log_path";

        public static readonly string[] RequiredHeaderFields = RequiredHeader.Split(',');

        public static string RelativeRunsPath => Path.Combine(RelativeDirectory, RunsFileName);
        public static string RelativeAcceptancePath => Path.Combine(RelativeDirectory, AcceptanceFileName);

        public static bool HeaderMatches(string header)
        {
            if (string.IsNullOrEmpty(header))
                return false;
            return string.Equals(header.Trim(), RequiredHeader, StringComparison.Ordinal);
        }

        public static bool TryReadRunsHeader(string projectRoot, out string header, out string error)
        {
            header = null;
            if (string.IsNullOrEmpty(projectRoot))
            {
                error = "projectRoot is required.";
                return false;
            }

            string path = Path.Combine(projectRoot, RelativeRunsPath);
            if (!File.Exists(path))
            {
                error = "runs.csv is missing.";
                return false;
            }

            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            {
                error = "runs.csv has no header.";
                return false;
            }

            header = lines[0].Trim();
            if (!HeaderMatches(header))
            {
                error = "runs.csv header does not match the mandated fields.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryReadAcceptanceMarkdown(string projectRoot, out string markdown, out string error)
        {
            markdown = null;
            if (string.IsNullOrEmpty(projectRoot))
            {
                error = "projectRoot is required.";
                return false;
            }

            string path = Path.Combine(projectRoot, RelativeAcceptancePath);
            if (!File.Exists(path))
            {
                error = "acceptance.md is missing.";
                return false;
            }

            markdown = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(markdown))
            {
                error = "acceptance.md is empty.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool ListsRequiredMatrix(string markdown, out string error)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                error = "acceptance.md is empty.";
                return false;
            }

            if (markdown.IndexOf("Manual victory", StringComparison.OrdinalIgnoreCase) < 0 ||
                markdown.IndexOf("S002-manual-rs-104731-en", StringComparison.Ordinal) < 0)
            {
                error = "acceptance.md must list the manual victory slot.";
                return false;
            }

            if (markdown.IndexOf("ARIA sample", StringComparison.OrdinalIgnoreCase) < 0 ||
                markdown.IndexOf("104731", StringComparison.Ordinal) < 0 ||
                markdown.IndexOf("130365", StringComparison.Ordinal) < 0 ||
                markdown.IndexOf("155923", StringComparison.Ordinal) < 0)
            {
                error = "acceptance.md must list the Regular Standard ARIA sample seeds.";
                return false;
            }

            if (markdown.IndexOf("Edge and recovery placeholders", StringComparison.OrdinalIgnoreCase) < 0 ||
                markdown.IndexOf("S002-edge-replacement-barracks", StringComparison.Ordinal) < 0 ||
                markdown.IndexOf("S002-recovery-checkpoint", StringComparison.Ordinal) < 0)
            {
                error = "acceptance.md must list edge and recovery placeholders.";
                return false;
            }

            if (markdown.IndexOf("How to fill", StringComparison.OrdinalIgnoreCase) < 0 ||
                markdown.IndexOf("runs.csv", StringComparison.Ordinal) < 0)
            {
                error = "acceptance.md must explain how to fill runs.csv.";
                return false;
            }

            if (markdown.IndexOf("Do not force", StringComparison.OrdinalIgnoreCase) < 0 &&
                markdown.IndexOf("do not force", StringComparison.OrdinalIgnoreCase) < 0)
            {
                error = "acceptance.md must forbid forced Victory.";
                return false;
            }

            error = null;
            return true;
        }

        public static SkirmishAcceptanceMatrixSlot[] RequiredFirstVisitSlots()
        {
            var slots = new List<SkirmishAcceptanceMatrixSlot>
            {
                new SkirmishAcceptanceMatrixSlot(
                    "S002-manual-rs-104731-en",
                    "manual",
                    "Standard",
                    "Regular",
                    FirstVisitSeed(),
                    GameLocalization.EnglishLocaleCode,
                    "human",
                    true)
            };

            int[] seeds = SkirmishAcceptanceCensusCapture.RegularStandardAriaSeeds;
            string[] locales = { GameLocalization.EnglishLocaleCode, GameLocalization.PersianLocaleCode };
            for (int s = 0; s < seeds.Length; s++)
            {
                for (int l = 0; l < locales.Length; l++)
                {
                    string localeCode = locales[l] == GameLocalization.EnglishLocaleCode ? "en" : "fa";
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        string runId = string.Format(
                            CultureInfo.InvariantCulture,
                            "S002-aria-rs-{0}-{1}-{2}",
                            seeds[s],
                            localeCode,
                            attempt);
                        slots.Add(new SkirmishAcceptanceMatrixSlot(
                            runId,
                            "aria",
                            "Standard",
                            "Regular",
                            seeds[s],
                            locales[l],
                            "aria",
                            true));
                    }
                }
            }

            slots.Add(Edge("S002-edge-replacement-barracks"));
            slots.Add(Edge("S002-edge-same-tick-bases"));
            slots.Add(Edge("S002-edge-deadline-draw"));
            slots.Add(Edge("S002-edge-field-army-wipe"));
            slots.Add(Edge("S002-edge-hidden-health"));
            slots.Add(new SkirmishAcceptanceMatrixSlot(
                "S002-recovery-checkpoint", "recovery", "Standard", "Regular", FirstVisitSeed(),
                GameLocalization.EnglishLocaleCode, "fixture", true));
            slots.Add(new SkirmishAcceptanceMatrixSlot(
                "S002-recovery-replay", "recovery", "Standard", "Regular", FirstVisitSeed(),
                GameLocalization.EnglishLocaleCode, "fixture", true));
            slots.Add(new SkirmishAcceptanceMatrixSlot(
                "S002-recovery-os-interrupt", "recovery", "Standard", "Regular", FirstVisitSeed(),
                GameLocalization.EnglishLocaleCode, "fixture", true));
            slots.Add(new SkirmishAcceptanceMatrixSlot(
                "S002-device-review", "device", "Standard", "Regular", FirstVisitSeed(),
                GameLocalization.EnglishLocaleCode, "fixture", true));
            return slots.ToArray();
        }

        public static string FormatPendingRow(in SkirmishAcceptanceMatrixSlot slot, string definitionVersion, string codeHash, string configHash)
        {
            string[] fields =
            {
                slot.RunId ?? string.Empty,
                SkirmishAcceptanceCensusCapture.CatalogId,
                definitionVersion ?? string.Empty,
                codeHash ?? string.Empty,
                configHash ?? string.Empty,
                slot.Size ?? string.Empty,
                slot.Difficulty ?? string.Empty,
                slot.Seed > 0 ? slot.Seed.ToString(CultureInfo.InvariantCulture) : string.Empty,
                slot.Locale ?? string.Empty,
                string.Empty,
                slot.Executor ?? string.Empty,
                "1",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };
            return string.Join(",", fields);
        }

        public static int CountPendingResultColumns(string row)
        {
            if (string.IsNullOrEmpty(row))
                return 0;
            return row.Split(',').Length;
        }

        private static int FirstVisitSeed() => SkirmishAcceptanceCensusCapture.FirstVisitSeed;

        private static SkirmishAcceptanceMatrixSlot Edge(string runId) =>
            new SkirmishAcceptanceMatrixSlot(
                runId,
                "edge",
                "Standard",
                "Regular",
                FirstVisitSeed(),
                GameLocalization.EnglishLocaleCode,
                "fixture",
                true);
    }
}
