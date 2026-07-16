namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    public sealed class Aph700ReportArtifacts
    {
        public Aph700ReportArtifacts(string json, string markdown)
        {
            Json = json;
            Markdown = markdown;
        }

        public string Json { get; }
        public string Markdown { get; }
    }

    public static class Aph700AssemblyDependencyReportGenerator
    {
        public const string JsonReportPath =
            "Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json";

        public const string MarkdownReportPath =
            "Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.md";

        private static readonly string[] FirstPartyRoots =
        {
            "Assets/Game",
            "Assets/Tests",
            "Assets/Editor"
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Game/Tools/Architecture/Generate APH-700 Assembly Dependency Report")]
        public static void GenerateFromMenu()
        {
            Aph700ReportArtifacts artifacts = GenerateAndWriteReports(Directory.GetCurrentDirectory());
            Aph700ReportDocument report = JsonSerializer.Deserialize<Aph700ReportDocument>(artifacts.Json, JsonOptions);
            UnityEngine.Debug.Log(
                $"[Aph700AssemblyDependencyReport] result=Passed assemblies={report.Summary.AssemblyCount} " +
                $"firstPartyEdges={report.Summary.FirstPartyEdgeCount} " +
                $"resolvedTypeOccurrences={report.Summary.ResolvedCrossDomainTypeOccurrenceCount} " +
                $"json={JsonReportPath} markdown={MarkdownReportPath}");
        }
#endif

        public static Aph700ReportArtifacts Generate(string projectRoot)
        {
            return Generate(projectRoot, identity: null);
        }

        public static Aph700ReportArtifacts Generate(
            string projectRoot,
            string exactCommit,
            string environmentIdentitySha256,
            bool dirty)
        {
            return Generate(
                projectRoot,
                new ArchitectureEvidenceIdentity(exactCommit, environmentIdentitySha256, dirty));
        }

        private static Aph700ReportArtifacts Generate(
            string projectRoot,
            ArchitectureEvidenceIdentity identity)
        {
            string normalizedRoot = NormalizeProjectRoot(projectRoot);
            ThrowIfAssemblyReferenceFilesExist(normalizedRoot);
            List<Aph700AssemblyDefinition> assemblies = DiscoverAssemblies(normalizedRoot);
            List<string> unownedSourceFiles = AssignOwnedSourceFiles(normalizedRoot, assemblies);

            Aph700ReferenceScanResult scan = Aph700CSharpSourceReferenceScanner.Scan(normalizedRoot, assemblies);
            scan.UnownedScopedSourceFileCount = unownedSourceFiles.Count;
            Aph700ReportDocument report = BuildReport(
                normalizedRoot,
                assemblies,
                unownedSourceFiles,
                scan,
                identity);
            string json = NormalizeNewlines(JsonSerializer.Serialize(report, JsonOptions)) + "\n";
            string markdown = BuildMarkdown(report);
            return new Aph700ReportArtifacts(json, markdown);
        }

        public static Aph700ReportArtifacts GenerateAndWriteReports(string projectRoot)
        {
            string normalizedRoot = NormalizeProjectRoot(projectRoot);
            ArchitectureEvidenceIdentity identity =
                ArchitectureEvidenceIdentityUtility.ResolveIfAvailable(normalizedRoot);
            Aph700ReportArtifacts artifacts = Generate(normalizedRoot, identity);
            WriteIfChanged(Path.Combine(normalizedRoot, JsonReportPath), artifacts.Json);
            WriteIfChanged(Path.Combine(normalizedRoot, MarkdownReportPath), artifacts.Markdown);
            return artifacts;
        }

        public static Aph700ReportArtifacts ValidateTrackedReports(string projectRoot)
        {
            string normalizedRoot = NormalizeProjectRoot(projectRoot);
            string jsonPath = Path.Combine(normalizedRoot, JsonReportPath);
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Tracked APH-700 JSON report is missing: '{JsonReportPath}'.", jsonPath);
            Aph700ReportDocument tracked;
            try
            {
                tracked = JsonSerializer.Deserialize<Aph700ReportDocument>(
                    File.ReadAllText(jsonPath),
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Tracked APH-700 JSON report is stale or malformed.", exception);
            }

            ArchitectureEvidenceIdentity identity =
                string.IsNullOrWhiteSpace(tracked?.ExactCommit) &&
                string.IsNullOrWhiteSpace(tracked?.EnvironmentIdentitySha256)
                    ? null
                    : new ArchitectureEvidenceIdentity(
                        tracked?.ExactCommit,
                        tracked?.EnvironmentIdentitySha256,
                        tracked?.Dirty ?? true);
            Aph700ReportArtifacts expected = Generate(normalizedRoot, identity);
            ValidateTrackedReport(
                jsonPath,
                expected.Json,
                "JSON");
            ValidateTrackedReport(
                Path.Combine(normalizedRoot, MarkdownReportPath),
                expected.Markdown,
                "Markdown");
            return expected;
        }

        private static Aph700ReportDocument BuildReport(
            string projectRoot,
            IReadOnlyList<Aph700AssemblyDefinition> assemblies,
            IReadOnlyList<string> unownedSourceFiles,
            Aph700ReferenceScanResult scan,
            ArchitectureEvidenceIdentity identity)
        {
            var byName = assemblies.ToDictionary(item => item.Name, StringComparer.Ordinal);
            var byGuid = assemblies
                .Where(item => !string.IsNullOrWhiteSpace(item.Guid))
                .ToDictionary(item => item.Guid, StringComparer.OrdinalIgnoreCase);
            var firstPartyEdges = new List<Aph700AssemblyEdgeRecord>();
            var externalReferences = new List<Aph700ExternalReferenceRecord>();

            foreach (Aph700AssemblyDefinition source in assemblies)
            {
                foreach (string reference in source.References.OrderBy(item => item, StringComparer.Ordinal))
                {
                    Aph700AssemblyDefinition target = ResolveFirstPartyReference(reference, byName, byGuid);
                    if (target == null)
                    {
                        externalReferences.Add(new Aph700ExternalReferenceRecord
                        {
                            SourceAssembly = source.Name,
                            SourceAsmdefPath = source.AsmdefPath,
                            DeclaredReference = reference,
                            ReferenceKind = reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase)
                                ? "externalOrUnresolvedGuid"
                                : "externalName"
                        });
                        continue;
                    }

                    Aph700EdgeReferenceSummary edgeSummary = scan.GetEdge(source.Name, target.Name);
                    firstPartyEdges.Add(new Aph700AssemblyEdgeRecord
                    {
                        SourceAssembly = source.Name,
                        TargetAssembly = target.Name,
                        SourceAsmdefPath = source.AsmdefPath,
                        TargetAsmdefPath = target.AsmdefPath,
                        DeclaredReference = reference,
                        ResolvedTypeOccurrenceCount = edgeSummary.OccurrenceCount,
                        DistinctResolvedTypeCount = edgeSummary.TypeReferences.Count,
                        ReferencingSourceFileCount = edgeSummary.SourceFiles.Count,
                        TopTypeReferences = edgeSummary.TypeReferences
                            .OrderByDescending(item => item.OccurrenceCount)
                            .ThenByDescending(item => item.SourceFiles.Count)
                            .ThenBy(item => item.FullTypeName, StringComparer.Ordinal)
                            .Take(10)
                            .Select(item => item.ToRecord(source.Name, target.Name))
                            .ToList()
                    });
                }
            }

            firstPartyEdges = firstPartyEdges
                .OrderBy(item => item.SourceAssembly, StringComparer.Ordinal)
                .ThenBy(item => item.TargetAssembly, StringComparer.Ordinal)
                .ThenBy(item => item.DeclaredReference, StringComparer.Ordinal)
                .ToList();
            externalReferences = externalReferences
                .OrderBy(item => item.SourceAssembly, StringComparer.Ordinal)
                .ThenBy(item => item.DeclaredReference, StringComparer.Ordinal)
                .ToList();

            List<Aph700CrossDomainTypeReferenceRecord> topReferences = scan.AllTypeReferences
                .OrderByDescending(item => item.OccurrenceCount)
                .ThenByDescending(item => item.SourceFiles.Count)
                .ThenBy(item => item.SourceAssembly, StringComparer.Ordinal)
                .ThenBy(item => item.TargetAssembly, StringComparer.Ordinal)
                .ThenBy(item => item.FullTypeName, StringComparer.Ordinal)
                .Take(50)
                .Select(item => item.ToRecord(item.SourceAssembly, item.TargetAssembly))
                .ToList();

            var report = new Aph700ReportDocument
            {
                ExactCommit = identity?.ExactCommit,
                EnvironmentIdentitySha256 = identity?.EnvironmentIdentitySha256,
                Dirty = identity?.Dirty,
                SourceFingerprintSha256 = ComputeSourceFingerprint(
                    projectRoot,
                    assemblies,
                    unownedSourceFiles),
                Assemblies = assemblies.Select(assembly => new Aph700AssemblyRecord
                    {
                        Name = assembly.Name,
                        AsmdefPath = assembly.AsmdefPath,
                        AsmdefGuid = assembly.Guid,
                        SourceFileCount = assembly.SourceFiles.Count,
                        DeclaredTypeCount = scan.GetDeclaredTypeCount(assembly.Name),
                        FirstPartyDependencyCount = firstPartyEdges.Count(edge =>
                            string.Equals(edge.SourceAssembly, assembly.Name, StringComparison.Ordinal)),
                        ExternalDependencyCount = externalReferences.Count(edge =>
                            string.Equals(edge.SourceAssembly, assembly.Name, StringComparison.Ordinal))
                    })
                    .OrderBy(item => item.Name, StringComparer.Ordinal)
                    .ToList(),
                FirstPartyEdges = firstPartyEdges,
                ExternalReferences = externalReferences,
                TopCrossDomainTypeReferences = topReferences,
                Limitations = new List<string>
                {
                    "First-party scope is path-owned: asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.",
                    "Type-reference counts are deterministic source-level lexical resolutions in explicit type contexts against direct first-party asmdef dependencies; they are not compiler symbol counts.",
                    "Comments and string/character literal contents are excluded. Interpolated-string expressions are excluded with their containing strings.",
                    "Ambiguous simple names are counted in the summary and omitted instead of being assigned heuristically.",
                    "Top-level declarations and public nested class, struct, interface, enum, record, and delegate declarations are indexed; generated code outside the scoped roots is excluded.",
                    "Member-access-only and semantically ambiguous parenthesized identifier uses are omitted; syntactically anchored casts and generic type expressions are included.",
                    "First-party .asmref files are rejected with a fail-closed unsupported-condition error until ownership resolution is implemented."
                }
            };

            report.Summary = new Aph700SummaryRecord
            {
                AssemblyCount = report.Assemblies.Count,
                FirstPartyEdgeCount = firstPartyEdges.Count,
                ExternalReferenceCount = externalReferences.Count,
                SourceFileCount = assemblies.Sum(item => item.SourceFiles.Count),
                DeclaredTypeCount = report.Assemblies.Sum(item => item.DeclaredTypeCount),
                ResolvedCrossDomainTypeOccurrenceCount = scan.AllTypeReferences.Sum(item => item.OccurrenceCount),
                DistinctCrossDomainTypeReferenceCount = scan.AllTypeReferences.Count,
                AmbiguousTypeTokenOccurrenceCount = scan.AmbiguousTypeTokenOccurrenceCount,
                UnownedScopedSourceFileCount = scan.UnownedScopedSourceFileCount
            };
            return report;
        }

        private static string BuildMarkdown(Aph700ReportDocument report)
        {
            var builder = new StringBuilder(32768);
            builder.Append("# APH-700 First-Party Assembly Dependency Report\n\n");
            builder.Append("- Task: `").Append(report.TaskId).Append("`\n");
            builder.Append("- Exact commit: `").Append(report.ExactCommit ?? "not-bound").Append("`\n");
            builder.Append("- Environment identity SHA-256: `")
                .Append(report.EnvironmentIdentitySha256 ?? "not-bound").Append("`\n");
            builder.Append("- Dirty at capture start: `")
                .Append(report.Dirty.HasValue ? report.Dirty.Value.ToString().ToLowerInvariant() : "not-bound")
                .Append("`\n");
            builder.Append("- Source fingerprint (SHA-256): `").Append(report.SourceFingerprintSha256).Append("`\n");
            builder.Append("- Determinism: ").Append(report.DeterminismContract).Append("\n");
            builder.Append("- Scope: ").Append(report.Scope).Append("\n\n");
            builder.Append("## Summary\n\n");
            builder.Append("| Metric | Count |\n|---|---:|\n");
            AppendMetric(builder, "First-party assemblies", report.Summary.AssemblyCount);
            AppendMetric(builder, "First-party asmdef edges", report.Summary.FirstPartyEdgeCount);
            AppendMetric(builder, "External declared references", report.Summary.ExternalReferenceCount);
            AppendMetric(builder, "Owned C# source files", report.Summary.SourceFileCount);
            AppendMetric(builder, "Indexed visible types", report.Summary.DeclaredTypeCount);
            AppendMetric(builder, "Resolved cross-domain type occurrences", report.Summary.ResolvedCrossDomainTypeOccurrenceCount);
            AppendMetric(builder, "Distinct cross-domain type references", report.Summary.DistinctCrossDomainTypeReferenceCount);
            AppendMetric(builder, "Ambiguous type tokens omitted", report.Summary.AmbiguousTypeTokenOccurrenceCount);
            AppendMetric(builder, "Unowned scoped C# source files", report.Summary.UnownedScopedSourceFileCount);
            builder.Append("\n## First-Party Assemblies\n\n");
            builder.Append("| Assembly | asmdef | Sources | Types | First-party edges | External refs |\n|---|---|---:|---:|---:|---:|\n");
            foreach (Aph700AssemblyRecord assembly in report.Assemblies)
            {
                builder.Append("| `").Append(assembly.Name).Append("` | `").Append(assembly.AsmdefPath)
                    .Append("` | ").Append(assembly.SourceFileCount).Append(" | ").Append(assembly.DeclaredTypeCount)
                    .Append(" | ").Append(assembly.FirstPartyDependencyCount).Append(" | ")
                    .Append(assembly.ExternalDependencyCount).Append(" |\n");
            }

            builder.Append("\n## Every First-Party Assembly Edge\n\n");
            builder.Append("| Source | Target | Type occurrences | Distinct types | Source files |\n|---|---|---:|---:|---:|\n");
            foreach (Aph700AssemblyEdgeRecord edge in report.FirstPartyEdges)
            {
                builder.Append("| `").Append(edge.SourceAssembly).Append("` | `").Append(edge.TargetAssembly)
                    .Append("` | ").Append(edge.ResolvedTypeOccurrenceCount).Append(" | ")
                    .Append(edge.DistinctResolvedTypeCount).Append(" | ")
                    .Append(edge.ReferencingSourceFileCount).Append(" |\n");
            }

            builder.Append("\n## Top Cross-Domain Type References\n\n");
            builder.Append("| Rank | Source | Target | Type | Occurrences | Source files |\n|---:|---|---|---|---:|---:|\n");
            for (int index = 0; index < report.TopCrossDomainTypeReferences.Count; index++)
            {
                Aph700CrossDomainTypeReferenceRecord reference = report.TopCrossDomainTypeReferences[index];
                builder.Append("| ").Append(index + 1).Append(" | `").Append(reference.SourceAssembly)
                    .Append("` | `").Append(reference.TargetAssembly).Append("` | `")
                    .Append(reference.FullTypeName).Append("` | ").Append(reference.OccurrenceCount)
                    .Append(" | ").Append(reference.SourceFileCount).Append(" |\n");
            }

            builder.Append("\n## External Declared References\n\n");
            builder.Append("These are retained so every reference declared by a first-party asmdef remains auditable; they are not first-party domain edges.\n\n");
            builder.Append("| Source | Declared reference | Kind |\n|---|---|---|\n");
            foreach (Aph700ExternalReferenceRecord reference in report.ExternalReferences)
            {
                builder.Append("| `").Append(reference.SourceAssembly).Append("` | `")
                    .Append(reference.DeclaredReference).Append("` | ").Append(reference.ReferenceKind).Append(" |\n");
            }

            builder.Append("\n## Measurement Boundaries\n\n");
            foreach (string limitation in report.Limitations)
                builder.Append("- ").Append(limitation).Append("\n");
            return builder.ToString();
        }

        private static List<Aph700AssemblyDefinition> DiscoverAssemblies(string projectRoot)
        {
            var asmdefPaths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string relativeRoot in FirstPartyRoots)
            {
                string absoluteRoot = Path.Combine(projectRoot, relativeRoot);
                if (!Directory.Exists(absoluteRoot))
                    throw new DirectoryNotFoundException($"APH-700 first-party root is missing: '{relativeRoot}'.");

                foreach (string path in Directory.EnumerateFiles(absoluteRoot, "*.asmdef", SearchOption.AllDirectories))
                    asmdefPaths.Add(ToProjectPath(projectRoot, path));
            }

            var assemblies = new List<Aph700AssemblyDefinition>(asmdefPaths.Count);
            foreach (string asmdefPath in asmdefPaths)
            {
                string absolutePath = Path.Combine(projectRoot, asmdefPath);
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(absolutePath));
                JsonElement root = document.RootElement;
                string name = root.GetProperty("name").GetString();
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidDataException($"Asmdef has no name: '{asmdefPath}'.");

                var references = new List<string>();
                if (root.TryGetProperty("references", out JsonElement referencesElement))
                {
                    foreach (JsonElement reference in referencesElement.EnumerateArray())
                    {
                        string value = reference.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            references.Add(value);
                    }
                }

                assemblies.Add(new Aph700AssemblyDefinition
                {
                    Name = name,
                    AsmdefPath = asmdefPath,
                    RootPath = NormalizePath(Path.GetDirectoryName(asmdefPath)),
                    Guid = ReadGuid(absolutePath + ".meta"),
                    References = references.Distinct(StringComparer.Ordinal).ToList()
                });
            }

            string duplicateName = assemblies.GroupBy(item => item.Name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(item => item, StringComparer.Ordinal)
                .FirstOrDefault();
            if (duplicateName != null)
                throw new InvalidDataException($"Duplicate first-party asmdef name: '{duplicateName}'.");

            string duplicateGuid = assemblies.Where(item => !string.IsNullOrWhiteSpace(item.Guid))
                .GroupBy(item => item.Guid, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(item => item, StringComparer.Ordinal)
                .FirstOrDefault();
            if (duplicateGuid != null)
                throw new InvalidDataException($"Duplicate first-party asmdef GUID: '{duplicateGuid}'.");

            return assemblies.OrderBy(item => item.Name, StringComparer.Ordinal).ToList();
        }

        private static void ThrowIfAssemblyReferenceFilesExist(string projectRoot)
        {
            var asmrefPaths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string relativeRoot in FirstPartyRoots)
            {
                string absoluteRoot = Path.Combine(projectRoot, relativeRoot);
                if (!Directory.Exists(absoluteRoot))
                    throw new DirectoryNotFoundException($"APH-700 first-party root is missing: '{relativeRoot}'.");

                foreach (string path in Directory.EnumerateFiles(absoluteRoot, "*.asmref", SearchOption.AllDirectories))
                    asmrefPaths.Add(ToProjectPath(projectRoot, path));
            }

            if (asmrefPaths.Count == 0)
                return;

            throw new NotSupportedException(
                "APH-700 cannot produce a trustworthy ownership report while first-party .asmref files exist. " +
                "Assembly-reference ownership is not implemented; remove the .asmref files or add explicit " +
                "ownership resolution before generating the report. Paths: " + string.Join(", ", asmrefPaths));
        }

        private static List<string> AssignOwnedSourceFiles(
            string projectRoot,
            IReadOnlyList<Aph700AssemblyDefinition> assemblies)
        {
            List<Aph700AssemblyDefinition> rootsBySpecificity = assemblies
                .OrderByDescending(item => item.RootPath.Length)
                .ThenBy(item => item.RootPath, StringComparer.Ordinal)
                .ToList();
            var sourcePaths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string relativeRoot in FirstPartyRoots)
            {
                foreach (string path in Directory.EnumerateFiles(
                             Path.Combine(projectRoot, relativeRoot), "*.cs", SearchOption.AllDirectories))
                {
                    sourcePaths.Add(ToProjectPath(projectRoot, path));
                }
            }

            var unownedSourceFiles = new List<string>();
            foreach (string sourcePath in sourcePaths)
            {
                Aph700AssemblyDefinition owner = rootsBySpecificity.FirstOrDefault(item =>
                    IsPathAtOrBelow(sourcePath, item.RootPath));
                if (owner != null)
                    owner.SourceFiles.Add(sourcePath);
                else
                    unownedSourceFiles.Add(sourcePath);
            }

            return unownedSourceFiles;
        }

        private static Aph700AssemblyDefinition ResolveFirstPartyReference(
            string reference,
            IReadOnlyDictionary<string, Aph700AssemblyDefinition> byName,
            IReadOnlyDictionary<string, Aph700AssemblyDefinition> byGuid)
        {
            if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
            {
                string guid = reference.Substring("GUID:".Length).Trim();
                return byGuid.TryGetValue(guid, out Aph700AssemblyDefinition target) ? target : null;
            }

            return byName.TryGetValue(reference, out Aph700AssemblyDefinition namedTarget) ? namedTarget : null;
        }

        private static string ComputeSourceFingerprint(
            string projectRoot,
            IReadOnlyList<Aph700AssemblyDefinition> assemblies,
            IReadOnlyList<string> unownedSourceFiles)
        {
            var inputs = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Aph700AssemblyDefinition assembly in assemblies)
            {
                inputs.Add(assembly.AsmdefPath);
                if (File.Exists(Path.Combine(projectRoot, assembly.AsmdefPath + ".meta")))
                    inputs.Add(assembly.AsmdefPath + ".meta");
                foreach (string sourceFile in assembly.SourceFiles)
                    inputs.Add(sourceFile);
            }
            foreach (string unownedSourceFile in unownedSourceFiles)
                inputs.Add(unownedSourceFile);

            using SHA256 sha256 = SHA256.Create();
            foreach (string relativePath in inputs)
            {
                AppendHashBytes(sha256, Encoding.UTF8.GetBytes(relativePath));
                AppendHashBytes(sha256, new byte[] { 0 });
                string content = NormalizeNewlines(File.ReadAllText(Path.Combine(projectRoot, relativePath)));
                AppendHashBytes(sha256, Encoding.UTF8.GetBytes(content));
                AppendHashBytes(sha256, new byte[] { 0 });
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return ToLowerHex(sha256.Hash);
        }

        private static void AppendHashBytes(HashAlgorithm hash, byte[] bytes)
        {
            hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        private static string ReadGuid(string metaPath)
        {
            if (!File.Exists(metaPath))
                return null;

            foreach (string line in File.ReadLines(metaPath))
            {
                if (line.StartsWith("guid:", StringComparison.Ordinal))
                    return line.Substring("guid:".Length).Trim();
            }

            return null;
        }

        private static string NormalizeProjectRoot(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string ToProjectPath(string projectRoot, string absolutePath)
        {
            return NormalizePath(Path.GetRelativePath(projectRoot, absolutePath));
        }

        private static bool IsPathAtOrBelow(string path, string root)
        {
            return string.Equals(path, root, StringComparison.Ordinal) ||
                   path.StartsWith(root + "/", StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            return path?.Replace('\\', '/');
        }

        private static string NormalizeNewlines(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }

        private static void WriteIfChanged(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
                return;
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static void ValidateTrackedReport(string path, string expected, string artifactName)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"APH-700 tracked {artifactName} report is missing. Generate it explicitly before validation.",
                    path);
            }

            string actual = File.ReadAllText(path);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"APH-700 tracked {artifactName} report is stale. Validation does not rewrite reports; " +
                    "regenerate the artifacts explicitly after all source changes are complete.");
            }
        }

        private static void AppendMetric(StringBuilder builder, string name, int value)
        {
            builder.Append("| ").Append(name).Append(" | ").Append(value).Append(" |\n");
        }
    }

}
