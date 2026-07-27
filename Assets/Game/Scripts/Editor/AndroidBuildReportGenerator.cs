#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    public static class AndroidBuildReportGenerator
    {
        public const int TopAssetLimit = 100;

        public const string ApkJsonReportPath =
            "Design/AgentReports/architecture_performance_android_apk_build_report.json";

        public const string ApkMarkdownReportPath =
            "Design/AgentReports/architecture_performance_android_apk_build_report.md";

        public const string AabJsonReportPath =
            "Design/AgentReports/architecture_performance_android_aab_build_report.json";

        public const string AabMarkdownReportPath =
            "Design/AgentReports/architecture_performance_android_aab_build_report.md";

        private const string ArtifactSizeSemantics =
            "Compressed APK/AAB package file length. This is not a sum of per-asset packed contributions.";

        private const string BuildReportSizeSemantics =
            "Unity BuildReport bytes. Included-asset rows are serialized packed contributions; " +
            "packed file overhead and summary-unaccounted bytes are reported separately.";

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public static AndroidBuildReportDocument GenerateAndWriteReports(
            BuildReport buildReport,
            string packageType)
        {
            return GenerateAndWriteReports(buildReport, packageType, CaptureGitProvenance());
        }

        public static AndroidBuildReportDocument GenerateAndWriteReports(
            BuildReport buildReport,
            string packageType,
            AndroidBuildReportProvenance provenance)
        {
            if (buildReport == null)
                throw new ArgumentNullException(nameof(buildReport));
            if (provenance == null)
                throw new ArgumentNullException(nameof(provenance));

            string normalizedPackageType = NormalizePackageType(packageType);
            BuildSummary summary = buildReport.summary;
            ValidateReleaseAndroidReport(summary, normalizedPackageType);

            PackedAssets[] packedFiles = buildReport.packedAssets ?? Array.Empty<PackedAssets>();
            var contributions = new List<AndroidPackedAssetContribution>();
            var packedFileOverheads = new List<ulong>(packedFiles.Length);

            for (int packedFileIndex = 0; packedFileIndex < packedFiles.Length; packedFileIndex++)
            {
                PackedAssets packedFile = packedFiles[packedFileIndex];
                packedFileOverheads.Add(packedFile.overhead);

                PackedAssetInfo[] contents = packedFile.contents ?? Array.Empty<PackedAssetInfo>();
                for (int contentIndex = 0; contentIndex < contents.Length; contentIndex++)
                {
                    PackedAssetInfo content = contents[contentIndex];
                    contributions.Add(new AndroidPackedAssetContribution
                    {
                        SourceAssetPath = content.sourceAssetPath,
                        ObjectType = content.type?.FullName,
                        PackedBytes = content.packedSize
                    });
                }
            }

            AndroidPackedAssetAggregation aggregation = AggregatePackedAssets(
                contributions,
                packedFileOverheads,
                summary.totalSize,
                TopAssetLimit);

            string artifactFilePath = summary.outputPath;
            if (string.IsNullOrWhiteSpace(artifactFilePath) || !File.Exists(artifactFilePath))
                throw new FileNotFoundException("Android build artifact was not found.", artifactFilePath);

            var evidence = new AndroidBuildReportEvidence
            {
                PackageType = normalizedPackageType,
                ExactCommit = provenance.ExactCommit,
                Dirty = provenance.Dirty,
                UnityVersion = UnityEngine.Application.unityVersion,
                ScriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
                TargetArchitecture = FormatAndroidArchitectures(PlayerSettings.Android.targetArchitectures),
                FrameTimingStatsEnabled = PlayerSettings.enableFrameTimingStats,
                ArtifactPath = NormalizeArtifactPath(artifactFilePath),
                ArtifactBytes = checked((ulong)new FileInfo(artifactFilePath).Length),
                ArtifactSha256 = ComputeSha256(artifactFilePath)
            };

            AndroidBuildReportDocument document = CreateReport(evidence, aggregation);
            WriteReports(document, normalizedPackageType);
            return document;
        }

        public static AndroidBuildReportProvenance CaptureGitProvenance()
        {
            GitEvidence gitEvidence = ReadGitEvidence();
            return new AndroidBuildReportProvenance(gitEvidence.ExactCommit, gitEvidence.Dirty);
        }

        public static AndroidPackedAssetAggregation AggregatePackedAssets(
            IEnumerable<AndroidPackedAssetContribution> contributions,
            IEnumerable<ulong> packedFileOverheads,
            ulong buildReportSummaryTotalSizeBytes,
            int topAssetLimit = TopAssetLimit)
        {
            if (contributions == null)
                throw new ArgumentNullException(nameof(contributions));
            if (packedFileOverheads == null)
                throw new ArgumentNullException(nameof(packedFileOverheads));
            if (topAssetLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(topAssetLimit));

            var assetsByPath = new Dictionary<string, AssetAccumulator>(StringComparer.Ordinal);
            ulong attributedPackedBytes = 0;
            ulong unattributedPackedBytes = 0;
            int packedAssetEntryCount = 0;
            int unattributedPackedEntryCount = 0;

            foreach (AndroidPackedAssetContribution contribution in contributions)
            {
                if (contribution == null)
                    throw new ArgumentException("Packed contribution collection contains a null item.", nameof(contributions));

                packedAssetEntryCount++;
                string normalizedPath = NormalizeSourceAssetPath(contribution.SourceAssetPath);
                if (string.IsNullOrEmpty(normalizedPath))
                {
                    unattributedPackedEntryCount++;
                    unattributedPackedBytes = checked(unattributedPackedBytes + contribution.PackedBytes);
                    continue;
                }

                attributedPackedBytes = checked(attributedPackedBytes + contribution.PackedBytes);
                if (!assetsByPath.TryGetValue(normalizedPath, out AssetAccumulator accumulator))
                {
                    accumulator = new AssetAccumulator(normalizedPath);
                    assetsByPath.Add(normalizedPath, accumulator);
                }

                accumulator.PackedBytes = checked(accumulator.PackedBytes + contribution.PackedBytes);
                accumulator.ObjectTypes.Add(NormalizeObjectType(contribution.ObjectType));
            }

            ulong packedFileOverheadBytes = 0;
            int packedFileCount = 0;
            foreach (ulong overhead in packedFileOverheads)
            {
                packedFileCount++;
                packedFileOverheadBytes = checked(packedFileOverheadBytes + overhead);
            }

            ulong packedContentBytes = checked(attributedPackedBytes + unattributedPackedBytes);
            ulong accountedPackedFileBytes = checked(packedContentBytes + packedFileOverheadBytes);
            long summaryUnaccountedBytes = checked(
                (long)buildReportSummaryTotalSizeBytes - (long)accountedPackedFileBytes);

            List<AndroidBuildIncludedAsset> allAssets = assetsByPath.Values
                .Select(accumulator => new AndroidBuildIncludedAsset
                {
                    SourceAssetPath = accumulator.SourceAssetPath,
                    PackedBytes = accumulator.PackedBytes,
                    ObjectTypes = accumulator.ObjectTypes.OrderBy(value => value, StringComparer.Ordinal).ToList()
                })
                .ToList();

            List<AndroidBuildIncludedAsset> topAssets = allAssets
                .OrderByDescending(asset => asset.PackedBytes)
                .ThenBy(asset => asset.SourceAssetPath, StringComparer.Ordinal)
                .Take(topAssetLimit)
                .ToList();

            List<AndroidBuildIncludedAsset> allIncludedTextures = allAssets
                .Where(asset => asset.ObjectTypes.Contains("UnityEngine.Texture2D", StringComparer.Ordinal))
                .OrderBy(asset => asset.SourceAssetPath, StringComparer.Ordinal)
                .ToList();

            return new AndroidPackedAssetAggregation
            {
                PackedFileCount = packedFileCount,
                PackedAssetEntryCount = packedAssetEntryCount,
                UnattributedPackedEntryCount = unattributedPackedEntryCount,
                TotalIncludedAssetCount = assetsByPath.Count,
                ReportedIncludedAssetCount = topAssets.Count,
                AttributedPackedAssetBytes = attributedPackedBytes,
                UnattributedPackedAssetBytes = unattributedPackedBytes,
                PackedContentBytes = packedContentBytes,
                PackedFileOverheadBytes = packedFileOverheadBytes,
                AccountedPackedFileBytes = accountedPackedFileBytes,
                BuildReportSummaryTotalSizeBytes = buildReportSummaryTotalSizeBytes,
                BuildReportSummaryUnaccountedBytes = summaryUnaccountedBytes,
                TopAssets = topAssets,
                AllIncludedTextures = allIncludedTextures
            };
        }

        public static AndroidBuildReportDocument CreateReport(
            AndroidBuildReportEvidence evidence,
            AndroidPackedAssetAggregation aggregation)
        {
            if (evidence == null)
                throw new ArgumentNullException(nameof(evidence));
            if (aggregation == null)
                throw new ArgumentNullException(nameof(aggregation));

            string packageType = NormalizePackageType(evidence.PackageType);
            RequireEvidence(evidence.ExactCommit, nameof(evidence.ExactCommit));
            RequireEvidence(evidence.UnityVersion, nameof(evidence.UnityVersion));
            RequireEvidence(evidence.ScriptingBackend, nameof(evidence.ScriptingBackend));
            RequireEvidence(evidence.TargetArchitecture, nameof(evidence.TargetArchitecture));
            RequireEvidence(evidence.ArtifactPath, nameof(evidence.ArtifactPath));
            RequireEvidence(evidence.ArtifactSha256, nameof(evidence.ArtifactSha256));
            if (!evidence.FrameTimingStatsEnabled)
                throw new InvalidOperationException(
                    "Release Android build evidence requires Frame Timing Stats.");

            return new AndroidBuildReportDocument
            {
                TaskId = "APH-500",
                SchemaVersion = 1,
                Status = "complete",
                ExactCommit = evidence.ExactCommit.Trim(),
                Dirty = evidence.Dirty,
                UnityVersion = evidence.UnityVersion.Trim(),
                ReleaseBuildType = "release",
                PackageType = packageType,
                BuildTarget = "Android",
                ScriptingBackend = evidence.ScriptingBackend.Trim(),
                TargetArchitecture = evidence.TargetArchitecture.Trim(),
                FrameTimingStatsEnabled = true,
                DetailedBuildReport = true,
                ArtifactPath = NormalizeSourceAssetPath(evidence.ArtifactPath),
                ArtifactBytes = evidence.ArtifactBytes,
                ArtifactSha256 = evidence.ArtifactSha256.Trim().ToLowerInvariant(),
                ArtifactSizeSemantics = ArtifactSizeSemantics,
                BuildReportSizeSemantics = BuildReportSizeSemantics,
                PackedFileCount = aggregation.PackedFileCount,
                PackedAssetEntryCount = aggregation.PackedAssetEntryCount,
                UnattributedPackedEntryCount = aggregation.UnattributedPackedEntryCount,
                TotalIncludedAssetCount = aggregation.TotalIncludedAssetCount,
                ReportedIncludedAssetCount = aggregation.ReportedIncludedAssetCount,
                AttributedPackedAssetBytes = aggregation.AttributedPackedAssetBytes,
                UnattributedPackedAssetBytes = aggregation.UnattributedPackedAssetBytes,
                PackedContentBytes = aggregation.PackedContentBytes,
                PackedFileOverheadBytes = aggregation.PackedFileOverheadBytes,
                AccountedPackedFileBytes = aggregation.AccountedPackedFileBytes,
                BuildReportSummaryTotalSizeBytes = aggregation.BuildReportSummaryTotalSizeBytes,
                BuildReportSummaryUnaccountedBytes = aggregation.BuildReportSummaryUnaccountedBytes,
                BuildReportIncludedAssets = aggregation.TopAssets.Select(CloneAsset).ToList(),
                AllIncludedTexturePathsExported = true,
                BuildReportIncludedTextures = aggregation.AllIncludedTextures.Select(CloneAsset).ToList()
            };
        }

        public static string SerializeReport(AndroidBuildReportDocument report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            return JsonConvert.SerializeObject(report, JsonSettings);
        }

        public static string BuildMarkdown(AndroidBuildReportDocument report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var builder = new StringBuilder(32768);
            builder.Append("# Android ").Append(report.PackageType).Append(" Build Report\n\n");
            builder.Append("- Task: `").Append(report.TaskId).Append("`\n");
            builder.Append("- Status: `").Append(report.Status).Append("`\n");
            builder.Append("- Exact commit: `").Append(report.ExactCommit).Append("`\n");
            builder.Append("- Dirty: `").Append(report.Dirty ? "true" : "false").Append("`\n");
            builder.Append("- Unity: `").Append(report.UnityVersion).Append("`\n");
            builder.Append("- Build: `release ").Append(report.PackageType).Append("`\n");
            builder.Append("- Target: `").Append(report.BuildTarget).Append("`\n");
            builder.Append("- Scripting backend: `").Append(report.ScriptingBackend).Append("`\n");
            builder.Append("- Target architecture: `").Append(report.TargetArchitecture).Append("`\n");
            builder.Append("- Frame Timing Stats: `enabled`\n");
            builder.Append("- Detailed BuildReport: `true`\n");
            builder.Append("- Artifact: `").Append(EscapeMarkdown(report.ArtifactPath)).Append("`\n");
            builder.Append("- Artifact SHA-256: `").Append(report.ArtifactSha256).Append("`\n\n");

            builder.Append("## Size Accounting\n\n");
            builder.Append("| Measure | Bytes | Meaning |\n");
            builder.Append("|---|---:|---|\n");
            AppendAccountingRow(builder, "Attributed packed assets", report.AttributedPackedAssetBytes,
                "Sum of BuildReport packed entries with a normalized sourceAssetPath");
            AppendAccountingRow(builder, "Unattributed packed content", report.UnattributedPackedAssetBytes,
                "Sum of BuildReport packed entries without a sourceAssetPath");
            AppendAccountingRow(builder, "Packed file overhead", report.PackedFileOverheadBytes,
                "Sum of PackedAssets.overhead header bytes");
            AppendAccountingRow(builder, "Accounted packed files", report.AccountedPackedFileBytes,
                "Attributed + unattributed + packed file overhead");
            AppendAccountingRow(builder, "BuildReport summary total size", report.BuildReportSummaryTotalSizeBytes,
                "BuildSummary.totalSize for all build output");
            builder.Append("| BuildReport summary unaccounted | ")
                .Append(report.BuildReportSummaryUnaccountedBytes.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" | Summary total minus accounted packed files; signed |\n");
            AppendAccountingRow(builder, "Compressed package file length", report.ArtifactBytes,
                "APK/AAB artifact file length on disk");

            builder.Append("\nPacked contributions and packed-file overhead come from `BuildReport.packedAssets`. ")
                .Append("The artifact file length is the compressed APK/AAB package size and is not a per-asset compressed-byte attribution.\n\n");

            builder.Append("## Top 100 Included Assets\n\n");
            builder.Append("- Distinct attributed assets: `").Append(report.TotalIncludedAssetCount).Append("`\n");
            builder.Append("- Rows reported: `").Append(report.ReportedIncludedAssetCount).Append("`\n");
            builder.Append("- Packed files: `").Append(report.PackedFileCount).Append("`\n");
            builder.Append("- Packed entries: `").Append(report.PackedAssetEntryCount).Append("`\n\n");
            builder.Append("| Rank | Packed bytes | MiB | Object types | Source asset path |\n");
            builder.Append("|---:|---:|---:|---|---|\n");

            for (int index = 0; index < report.BuildReportIncludedAssets.Count; index++)
            {
                AndroidBuildIncludedAsset asset = report.BuildReportIncludedAssets[index];
                builder.Append("| ").Append(index + 1)
                    .Append(" | ").Append(asset.PackedBytes.ToString("N0", CultureInfo.InvariantCulture))
                    .Append(" | ").Append(FormatMiB(asset.PackedBytes))
                    .Append(" | ").Append(EscapeMarkdown(string.Join(", ", asset.ObjectTypes)))
                    .Append(" | `").Append(EscapeMarkdown(asset.SourceAssetPath)).Append("` |\n");
            }

            return builder.ToString();
        }

        public static string NormalizeSourceAssetPath(string sourceAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sourceAssetPath))
                return string.Empty;

            string normalized = sourceAssetPath.Trim().Replace('\\', '/');
            while (normalized.StartsWith("./", StringComparison.Ordinal))
                normalized = normalized.Substring(2);
            while (normalized.IndexOf("//", StringComparison.Ordinal) >= 0)
                normalized = normalized.Replace("//", "/");
            return normalized;
        }

        private static void ValidateReleaseAndroidReport(BuildSummary summary, string packageType)
        {
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Cannot report an unsuccessful build: {summary.result}.");
            if (summary.platform != BuildTarget.Android)
                throw new InvalidOperationException($"APH-500 only supports Android builds, not {summary.platform}.");
            ValidateReleaseBuildOptions(summary.options);

            string expectedExtension = packageType == "AAB" ? ".aab" : ".apk";
            if (!string.Equals(Path.GetExtension(summary.outputPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Package type {packageType} does not match build output '{summary.outputPath}'.");
            }
        }

        internal static void ValidateReleaseBuildOptions(BuildOptions options)
        {
            if ((options & BuildOptions.DetailedBuildReport) == 0)
                throw new InvalidOperationException("APH-500 requires BuildOptions.DetailedBuildReport.");

            const BuildOptions forbidden =
                BuildOptions.Development |
                BuildOptions.AllowDebugging |
                BuildOptions.ConnectWithProfiler |
                BuildOptions.EnableDeepProfilingSupport;
            BuildOptions enabledForbiddenOptions = options & forbidden;
            if (enabledForbiddenOptions != BuildOptions.None)
            {
                throw new InvalidOperationException(
                    $"APH-500 reports release APK/AAB builds only; forbidden options: {enabledForbiddenOptions}.");
            }
        }

        private static void WriteReports(AndroidBuildReportDocument report, string packageType)
        {
            string jsonPath = packageType == "AAB" ? AabJsonReportPath : ApkJsonReportPath;
            string markdownPath = packageType == "AAB" ? AabMarkdownReportPath : ApkMarkdownReportPath;
            string reportDirectory = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(reportDirectory))
                Directory.CreateDirectory(reportDirectory);

            var encoding = new UTF8Encoding(false);
            File.WriteAllText(jsonPath, SerializeReport(report) + "\n", encoding);
            File.WriteAllText(markdownPath, BuildMarkdown(report), encoding);
            UnityEngine.Debug.Log(
                $"[AndroidBuildReport] result=Passed packageType={packageType} " +
                $"assets={report.TotalIncludedAssetCount} artifactBytes={report.ArtifactBytes} " +
                $"json={jsonPath} markdown={markdownPath}");
        }

        private static GitEvidence ReadGitEvidence()
        {
            string exactCommit = RunGit("rev-parse HEAD").Trim();
            string dirtyOverride = Environment.GetEnvironmentVariable("APH500_GIT_DIRTY");
            if (!string.IsNullOrWhiteSpace(dirtyOverride))
            {
                if (!bool.TryParse(dirtyOverride, out bool overriddenDirty))
                {
                    throw new InvalidOperationException(
                        $"APH500_GIT_DIRTY must be true or false, not '{dirtyOverride}'.");
                }

                return new GitEvidence(exactCommit, overriddenDirty);
            }

            string status = RunGit("status --porcelain --untracked-files=normal");
            return new GitEvidence(exactCommit, !string.IsNullOrWhiteSpace(status));
        }

        private static string RunGit(string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ResolveGitExecutable(
                        Environment.GetEnvironmentVariable("WARLINE_GIT_EXECUTABLE")),
                    Arguments = arguments,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start git {arguments}.");

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000))
                throw new TimeoutException($"git {arguments} timed out.");
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"git {arguments} failed with exit code {process.ExitCode}: {standardError.Trim()}");
            }

            return standardOutput;
        }

        internal static string ResolveGitExecutable(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return "git";

            string trimmedPath = configuredPath.Trim();
            if (!Path.IsPathRooted(trimmedPath))
            {
                throw new InvalidOperationException(
                    "WARLINE_GIT_EXECUTABLE must be an absolute path.");
            }

            string fullPath = Path.GetFullPath(trimmedPath);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    $"WARLINE_GIT_EXECUTABLE does not exist: {fullPath}");
            }

            return fullPath;
        }

        private static string ComputeSha256(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            for (int index = 0; index < hash.Length; index++)
                builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string NormalizeArtifactPath(string artifactPath)
        {
            string fullPath = Path.GetFullPath(artifactPath).Replace('\\', '/');
            string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory())
                .Replace('\\', '/')
                .TrimEnd('/') + "/";
            return fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(projectRoot.Length)
                : fullPath;
        }

        private static string FormatAndroidArchitectures(AndroidArchitecture architectures)
        {
            var values = new List<string>(2);
            if ((architectures & AndroidArchitecture.ARMv7) != 0)
                values.Add("ARMv7");
            if ((architectures & AndroidArchitecture.ARM64) != 0)
                values.Add("ARM64");
            return values.Count > 0 ? string.Join(",", values) : architectures.ToString();
        }

        private static string NormalizePackageType(string packageType)
        {
            string normalized = packageType?.Trim().ToUpperInvariant();
            if (normalized != "APK" && normalized != "AAB")
                throw new ArgumentException("Package type must be APK or AAB.", nameof(packageType));
            return normalized;
        }

        private static string NormalizeObjectType(string objectType)
        {
            return string.IsNullOrWhiteSpace(objectType) ? "Unknown" : objectType.Trim();
        }

        private static void RequireEvidence(string value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Evidence field {propertyName} is required.", propertyName);
        }

        private static AndroidBuildIncludedAsset CloneAsset(AndroidBuildIncludedAsset asset)
        {
            return new AndroidBuildIncludedAsset
            {
                SourceAssetPath = asset.SourceAssetPath,
                PackedBytes = asset.PackedBytes,
                ObjectTypes = asset.ObjectTypes.ToList()
            };
        }

        private static void AppendAccountingRow(
            StringBuilder builder,
            string measure,
            ulong bytes,
            string meaning)
        {
            builder.Append("| ").Append(measure)
                .Append(" | ").Append(bytes.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" | ").Append(meaning).Append(" |\n");
        }

        private static string FormatMiB(ulong bytes)
        {
            return (bytes / (1024d * 1024d)).ToString("F2", CultureInfo.InvariantCulture);
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class AssetAccumulator
        {
            public AssetAccumulator(string sourceAssetPath)
            {
                SourceAssetPath = sourceAssetPath;
            }

            public string SourceAssetPath { get; }
            public ulong PackedBytes { get; set; }
            public HashSet<string> ObjectTypes { get; } = new(StringComparer.Ordinal);
        }

        private readonly struct GitEvidence
        {
            public GitEvidence(string exactCommit, bool dirty)
            {
                ExactCommit = exactCommit;
                Dirty = dirty;
            }

            public string ExactCommit { get; }
            public bool Dirty { get; }
        }
    }

    public sealed class AndroidPackedAssetContribution
    {
        public string SourceAssetPath { get; set; }
        public string ObjectType { get; set; }
        public ulong PackedBytes { get; set; }
    }

    public sealed class AndroidBuildIncludedAsset
    {
        public string SourceAssetPath { get; set; }
        public ulong PackedBytes { get; set; }
        public List<string> ObjectTypes { get; set; } = new();
    }

    public sealed class AndroidPackedAssetAggregation
    {
        public int PackedFileCount { get; set; }
        public int PackedAssetEntryCount { get; set; }
        public int UnattributedPackedEntryCount { get; set; }
        public int TotalIncludedAssetCount { get; set; }
        public int ReportedIncludedAssetCount { get; set; }
        public ulong AttributedPackedAssetBytes { get; set; }
        public ulong UnattributedPackedAssetBytes { get; set; }
        public ulong PackedContentBytes { get; set; }
        public ulong PackedFileOverheadBytes { get; set; }
        public ulong AccountedPackedFileBytes { get; set; }
        public ulong BuildReportSummaryTotalSizeBytes { get; set; }
        public long BuildReportSummaryUnaccountedBytes { get; set; }
        public List<AndroidBuildIncludedAsset> TopAssets { get; set; } = new();
        public List<AndroidBuildIncludedAsset> AllIncludedTextures { get; set; } = new();
    }

    public sealed class AndroidBuildReportEvidence
    {
        public string PackageType { get; set; }
        public string ExactCommit { get; set; }
        public bool Dirty { get; set; }
        public string UnityVersion { get; set; }
        public string ScriptingBackend { get; set; }
        public string TargetArchitecture { get; set; }
        public bool FrameTimingStatsEnabled { get; set; }
        public string ArtifactPath { get; set; }
        public ulong ArtifactBytes { get; set; }
        public string ArtifactSha256 { get; set; }
    }

    public sealed class AndroidBuildReportProvenance
    {
        public AndroidBuildReportProvenance(string exactCommit, bool dirty)
        {
            if (string.IsNullOrWhiteSpace(exactCommit))
                throw new ArgumentException("Exact commit is required.", nameof(exactCommit));

            ExactCommit = exactCommit.Trim();
            Dirty = dirty;
        }

        public string ExactCommit { get; }
        public bool Dirty { get; }
    }

    public sealed class AndroidBuildReportDocument
    {
        public string TaskId { get; set; }
        public int SchemaVersion { get; set; }
        public string Status { get; set; }
        public string ExactCommit { get; set; }
        public bool Dirty { get; set; }
        public string UnityVersion { get; set; }
        public string ReleaseBuildType { get; set; }
        public string PackageType { get; set; }
        public string BuildTarget { get; set; }
        public string ScriptingBackend { get; set; }
        public string TargetArchitecture { get; set; }
        public bool FrameTimingStatsEnabled { get; set; }
        public bool DetailedBuildReport { get; set; }
        public string ArtifactPath { get; set; }
        public ulong ArtifactBytes { get; set; }
        public string ArtifactSha256 { get; set; }
        public string ArtifactSizeSemantics { get; set; }
        public string BuildReportSizeSemantics { get; set; }
        public int PackedFileCount { get; set; }
        public int PackedAssetEntryCount { get; set; }
        public int UnattributedPackedEntryCount { get; set; }
        public int TotalIncludedAssetCount { get; set; }
        public int ReportedIncludedAssetCount { get; set; }
        public ulong AttributedPackedAssetBytes { get; set; }
        public ulong UnattributedPackedAssetBytes { get; set; }
        public ulong PackedContentBytes { get; set; }
        public ulong PackedFileOverheadBytes { get; set; }
        public ulong AccountedPackedFileBytes { get; set; }
        public ulong BuildReportSummaryTotalSizeBytes { get; set; }
        public long BuildReportSummaryUnaccountedBytes { get; set; }
        public List<AndroidBuildIncludedAsset> BuildReportIncludedAssets { get; set; } = new();
        public bool AllIncludedTexturePathsExported { get; set; }
        public List<AndroidBuildIncludedAsset> BuildReportIncludedTextures { get; set; } = new();
    }
}

#endif
