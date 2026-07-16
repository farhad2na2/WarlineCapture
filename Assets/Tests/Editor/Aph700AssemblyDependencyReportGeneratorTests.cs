using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Game.Editor;

public sealed class Aph700AssemblyDependencyReportGeneratorTests
{
#if UNITY_EDITOR
    [NUnit.Framework.Test]
    public void FixtureReportIncludesEveryEdgeAndRanksTypeReferences()
    {
        RunFixtureValidation();
    }

    [NUnit.Framework.Test]
    public void FixtureTrackedReportValidationFailsClosedWithoutWriting()
    {
        RunTrackedArtifactValidationContract();
    }

    [NUnit.Framework.Test]
    public void FixtureAsmrefIsRejectedWithClearUnsupportedCondition()
    {
        RunAsmrefUnsupportedContract();
    }

    [NUnit.Framework.Test]
    public void FixtureLexicalMatrixAcceptsOnlyAnchoredTypeContexts()
    {
        RunLexicalContextMatrix();
    }

    [NUnit.Framework.Test]
    public void FixtureUnownedSourcePathAndContentAffectFingerprint()
    {
        RunUnownedSourceFingerprintContract();
    }

    [NUnit.Framework.Test]
    [NUnit.Framework.Timeout(600000)]
    public void CurrentRepositoryReportMatchesAsmdefsAndTrackedArtifacts()
    {
        string projectRoot = Directory.GetCurrentDirectory();
        Aph700ReportArtifacts artifacts =
            Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(projectRoot);
        ValidateCurrentRepository(projectRoot, artifacts);
    }

    [NUnit.Framework.Test]
    [NUnit.Framework.Timeout(600000)]
    public void CurrentRepositoryGenerationIsByteDeterministic()
    {
        string projectRoot = Directory.GetCurrentDirectory();
        Aph700ReportArtifacts first = Aph700AssemblyDependencyReportGenerator.Generate(projectRoot);
        Aph700ReportArtifacts second = Aph700AssemblyDependencyReportGenerator.Generate(projectRoot);
        RequireEqual(first.Json, second.Json, "JSON output changed between identical runs.");
        RequireEqual(first.Markdown, second.Markdown, "Markdown output changed between identical runs.");
    }
#endif

#if APH700_STANDALONE
    public static int Main(string[] args)
    {
        try
        {
            bool fixturesOnly = args.Any(argument =>
                string.Equals(argument, "--fixtures-only", StringComparison.Ordinal));
            string rootArgument = args.FirstOrDefault(argument =>
                !string.Equals(argument, "--fixtures-only", StringComparison.Ordinal));
            string projectRoot = rootArgument != null
                ? Path.GetFullPath(rootArgument)
                : Directory.GetCurrentDirectory();
            RunFixtureValidation();
            RunLexicalContextMatrix();
            RunUnownedSourceFingerprintContract();
            RunTrackedArtifactValidationContract();
            RunAsmrefUnsupportedContract();
            if (!fixturesOnly)
            {
                Aph700ReportArtifacts first =
                    Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(projectRoot);
                ValidateCurrentRepository(projectRoot, first);
                Aph700ReportArtifacts second = Aph700AssemblyDependencyReportGenerator.Generate(projectRoot);
                RequireEqual(first.Json, second.Json, "JSON output changed between identical runs.");
                RequireEqual(first.Markdown, second.Markdown, "Markdown output changed between identical runs.");
            }
            Console.WriteLine(
                "[Aph700AssemblyDependencyReportValidation] result=Passed " +
                $"mode={(fixturesOnly ? "fixtures" : "repository")} lexical=Passed " +
                "trackedArtifacts=Passed asmref=Passed determinism=Passed");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Console.Error.WriteLine("[Aph700AssemblyDependencyReportValidation] result=Failed");
            return 1;
        }
    }
#endif

    private static void RunFixtureValidation()
    {
        string root = Path.Combine(Path.GetTempPath(), "aph700-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Core"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Feature/Child"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Host"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Tests"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Editor"));

            WriteAsmdef(root, "Assets/Game/Core/Game.Core.asmdef", "Game.Core", "11111111111111111111111111111111");
            WriteAsmdef(
                root,
                "Assets/Game/Feature/Game.Feature.asmdef",
                "Game.Feature",
                "22222222222222222222222222222222",
                "Game.Core",
                "Unity.Entities");
            WriteAsmdef(
                root,
                "Assets/Game/Feature/Child/Game.Feature.Child.asmdef",
                "Game.Feature.Child",
                "33333333333333333333333333333333",
                "Game.Core");
            WriteAsmdef(
                root,
                "Assets/Game/Host/Game.Host.asmdef",
                "Game.Host",
                "44444444444444444444444444444444",
                "GUID:22222222222222222222222222222222",
                "Game.Core");

            WriteFile(
                root,
                "Assets/Game/Core/CoreTypes.cs",
                "namespace Core\n{\n" +
                "    public struct SharedType { }\n" +
                "    public static class PublicContainer\n    {\n" +
                "        public sealed class NestedType { }\n" +
                "        private sealed class HiddenNestedType { }\n" +
                "    }\n}\n");
            WriteFile(
                root,
                "Assets/Game/Feature/FeatureTypes.cs",
                "using Core;\nnamespace Feature\n{\n" +
                "    public sealed class FeatureType { }\n" +
                "    public sealed class GenericHolder<T> { }\n" +
                "    public sealed class Consumer\n    {\n" +
                "        private SharedType _one;\n        private SharedType _two;\n" +
                "        private PublicContainer.NestedType _nested;\n" +
                "        private GenericHolder<SharedType> _generic;\n" +
                "        private string SharedType => \"member-name-only\";\n" +
                "        private object Mirror => new { SharedType = \"member-name-only\" };\n" +
                "        private object Read(dynamic value) => value.SharedType;\n" +
                "        private string Text => \"SharedType\";\n" +
                "        // SharedType\n    }\n}\n");
            WriteFile(
                root,
                "Assets/Game/Feature/Child/ChildTypes.cs",
                "namespace Feature.Child\n{\n" +
                "    public sealed class ChildConsumer { private Core.SharedType _value; }\n}\n");
            WriteFile(
                root,
                "Assets/Game/Host/HostTypes.cs",
                "using Alias = Core.SharedType;\nusing Feature;\nnamespace Host\n{\n" +
                "    public sealed class HostConsumer\n    {\n" +
                "        private Alias _value;\n        private FeatureType _feature;\n    }\n}\n");

            Aph700ReportArtifacts first = Aph700AssemblyDependencyReportGenerator.Generate(root);
            Aph700ReportArtifacts second = Aph700AssemblyDependencyReportGenerator.Generate(root);
            RequireEqual(first.Json, second.Json, "Fixture JSON was not deterministic.");
            RequireEqual(first.Markdown, second.Markdown, "Fixture Markdown was not deterministic.");

            Aph700ReportArtifacts bound = Aph700AssemblyDependencyReportGenerator.Generate(
                root,
                new string('c', 40),
                new string('d', 64),
                dirty: false);
            using JsonDocument boundDocument = JsonDocument.Parse(bound.Json);
            RequireEqual(new string('c', 40), boundDocument.RootElement.GetProperty("exactCommit").GetString(),
                "Fixture exact commit was not serialized.");
            RequireEqual(new string('d', 64),
                boundDocument.RootElement.GetProperty("environmentIdentitySha256").GetString(),
                "Fixture environment identity was not serialized.");
            Require(!boundDocument.RootElement.GetProperty("dirty").GetBoolean(),
                "Fixture clean identity was not serialized.");

            using JsonDocument document = JsonDocument.Parse(first.Json);
            JsonElement report = document.RootElement;
            RequireEqual(4, report.GetProperty("summary").GetProperty("assemblyCount").GetInt32(),
                "Fixture assembly count changed.");
            RequireEqual(4, report.GetProperty("summary").GetProperty("firstPartyEdgeCount").GetInt32(),
                "Fixture first-party edge count changed.");
            RequireEqual(1, report.GetProperty("summary").GetProperty("externalReferenceCount").GetInt32(),
                "Fixture external-reference count changed.");

            string[] actualEdges = report.GetProperty("firstPartyEdges").EnumerateArray()
                .Select(EdgeKey)
                .ToArray();
            string[] expectedEdges =
            {
                "Game.Feature->Game.Core",
                "Game.Feature.Child->Game.Core",
                "Game.Host->Game.Core",
                "Game.Host->Game.Feature"
            };
            RequireSequenceEqual(expectedEdges, actualEdges, "Fixture did not list the exact asmdef edge set.");

            JsonElement childAssembly = report.GetProperty("assemblies").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == "Game.Feature.Child");
            JsonElement featureAssembly = report.GetProperty("assemblies").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == "Game.Feature");
            RequireEqual(1, childAssembly.GetProperty("sourceFileCount").GetInt32(),
                "Nested asmdef did not own its source file.");
            RequireEqual(1, featureAssembly.GetProperty("sourceFileCount").GetInt32(),
                "Parent asmdef incorrectly absorbed nested source.");

            JsonElement featureToCore = report.GetProperty("firstPartyEdges").EnumerateArray()
                .Single(item => EdgeKey(item) == "Game.Feature->Game.Core");
            JsonElement childToCore = report.GetProperty("firstPartyEdges").EnumerateArray()
                .Single(item => EdgeKey(item) == "Game.Feature.Child->Game.Core");
            RequireEqual(4, featureToCore.GetProperty("resolvedTypeOccurrenceCount").GetInt32(),
                "Non-type identifiers leaked into counts or explicit fixture type syntax was omitted.");
            RequireEqual(1, childToCore.GetProperty("resolvedTypeOccurrenceCount").GetInt32(),
                "Qualified fixture type reference was not resolved.");
            JsonElement nestedReference = featureToCore.GetProperty("topTypeReferences").EnumerateArray()
                .Single(item => item.GetProperty("fullTypeName").GetString() ==
                                "Core.PublicContainer.NestedType");
            RequireEqual(1, nestedReference.GetProperty("occurrenceCount").GetInt32(),
                "Public nested cross-assembly type was not indexed and resolved exactly once.");
            JsonElement sharedReference = featureToCore.GetProperty("topTypeReferences").EnumerateArray()
                .Single(item => item.GetProperty("fullTypeName").GetString() == "Core.SharedType");
            RequireEqual(3, sharedReference.GetProperty("occurrenceCount").GetInt32(),
                "Member/property names were incorrectly counted as SharedType references.");
            JsonElement coreAssembly = report.GetProperty("assemblies").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == "Game.Core");
            RequireEqual(3, coreAssembly.GetProperty("declaredTypeCount").GetInt32(),
                "Nested public declaration indexing or private nested filtering changed.");
            Require(first.Markdown.Contains("## Every First-Party Assembly Edge", StringComparison.Ordinal),
                "Fixture Markdown omitted the edge table.");
            Require(first.Markdown.Contains("## Top Cross-Domain Type References", StringComparison.Ordinal),
                "Fixture Markdown omitted the ranked type table.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void RunTrackedArtifactValidationContract()
    {
        string root = CreateMinimalFixture("aph700-tracked-artifacts-");
        try
        {
            string jsonPath = Path.Combine(root, Aph700AssemblyDependencyReportGenerator.JsonReportPath);
            string markdownPath = Path.Combine(root, Aph700AssemblyDependencyReportGenerator.MarkdownReportPath);
            ExpectThrows<FileNotFoundException>(
                () => Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(root),
                "missing");
            Require(!File.Exists(jsonPath) && !File.Exists(markdownPath),
                "Fail-closed validation created missing tracked artifacts.");

            Aph700ReportArtifacts written =
                Aph700AssemblyDependencyReportGenerator.GenerateAndWriteReports(root);
            Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(root);

            const string staleJson = "{\"stale\":true}\n";
            File.WriteAllText(jsonPath, staleJson, new UTF8Encoding(false));
            ExpectThrows<InvalidDataException>(
                () => Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(root),
                "stale");
            RequireEqual(staleJson, File.ReadAllText(jsonPath),
                "Validation rewrote a stale JSON report before reporting failure.");

            File.WriteAllText(jsonPath, written.Json, new UTF8Encoding(false));
            File.Delete(markdownPath);
            ExpectThrows<FileNotFoundException>(
                () => Aph700AssemblyDependencyReportGenerator.ValidateTrackedReports(root),
                "missing");
            Require(!File.Exists(markdownPath),
                "Validation recreated a missing Markdown report.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void RunLexicalContextMatrix()
    {
        string root = Path.Combine(Path.GetTempPath(), "aph700-lexical-matrix-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Core"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Feature"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Tests"));
            Directory.CreateDirectory(Path.Combine(root, "Assets/Editor"));
            WriteAsmdef(root, "Assets/Game/Core/Game.Core.asmdef", "Game.Core",
                "11111111111111111111111111111111");
            WriteAsmdef(root, "Assets/Game/Feature/Game.Feature.asmdef", "Game.Feature",
                "22222222222222222222222222222222", "Game.Core");
            WriteFile(
                root,
                "Assets/Game/Core/CoreTypes.cs",
                "namespace Core\n{\n" +
                "    public struct SharedType { }\n" +
                "    public struct NegativeType { }\n" +
                "    public sealed class SharedAttribute : System.Attribute { }\n" +
                "    public class Base<T> { }\n" +
                "}\n");
            WriteFile(
                root,
                "Assets/Game/Feature/LexicalMatrix.cs",
                "using Core;\n" +
                "using System.Collections.Generic;\n" +
                "namespace Feature\n{\n" +
                "    [Shared]\n" +
                "    public sealed class Derived : Base<SharedType>\n    {\n" +
                "        private SharedType _field;\n" +
                "        private List<List<SharedType>> _nested;\n" +
                "        private string SharedType => \"property-name-only\";\n" +
                "        private string NegativeType => \"property-name-only\";\n" +
                "        private SharedType Cast(object value)\n        {\n" +
                "            return (SharedType)value;\n        }\n" +
                "        private void Exercise(dynamic value, int min, int max)\n        {\n" +
                "            using SharedType lease = new SharedType();\n" +
                "            bool comparison = min < SharedType && SharedType > max;\n" +
                "            bool negativeComparison = min < NegativeType && NegativeType > max;\n" +
                "            object member = value.SharedType;\n" +
                "            object negativeMember = value.NegativeType;\n" +
                "            object anonymous = new { SharedType = min };\n" +
                "            object negativeAnonymous = new { NegativeType = min };\n" +
                "            Visit<SharedType>();\n" +
                "            bool pattern = value is SharedType;\n" +
                "            object converted = value as SharedType;\n" +
                "            object reflected = typeof(SharedType);\n" +
                "            string text = \"SharedType\";\n" +
                "            // SharedType\n        }\n" +
                "        private void Visit<T>() { }\n" +
                "    }\n}\n");

            Aph700ReportArtifacts artifacts = Aph700AssemblyDependencyReportGenerator.Generate(root);
            using JsonDocument document = JsonDocument.Parse(artifacts.Json);
            JsonElement edge = document.RootElement.GetProperty("firstPartyEdges").EnumerateArray()
                .Single(item => EdgeKey(item) == "Game.Feature->Game.Core");
            RequireEqual(13, edge.GetProperty("resolvedTypeOccurrenceCount").GetInt32(),
                "Lexical matrix accepted a negative identifier or omitted a positive type context.");
            RequireEqual(11, GetTypeOccurrenceCount(edge, "Core.SharedType"),
                "SharedType positive/negative lexical matrix changed.");
            RequireEqual(1, GetTypeOccurrenceCount(edge, "Core.SharedAttribute"),
                "Short attribute syntax did not resolve to the Attribute-suffixed declaration.");
            RequireEqual(1, GetTypeOccurrenceCount(edge, "Core.Base"),
                "Generic base-list type was not resolved exactly once.");
            Require(!HasTypeReference(edge, "Core.NegativeType"),
                "A type used only as comparison/property/member identifiers leaked into the report.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void RunUnownedSourceFingerprintContract()
    {
        string root = CreateMinimalFixture("aph700-unowned-fingerprint-");
        try
        {
            const string firstPath = "Assets/Tests/UnownedA.cs";
            const string secondPath = "Assets/Tests/UnownedB.cs";
            WriteFile(root, firstPath, "public sealed class Unowned { public int Value => 1; }\n");
            Aph700ReportArtifacts first = Aph700AssemblyDependencyReportGenerator.Generate(root);
            string firstFingerprint = GetFingerprint(first);
            RequireEqual(1, GetUnownedSourceCount(first),
                "Fixture did not identify the unowned scoped source.");

            WriteFile(root, firstPath, "public sealed class Unowned { public int Value => 2; }\n");
            Aph700ReportArtifacts contentChanged = Aph700AssemblyDependencyReportGenerator.Generate(root);
            string contentFingerprint = GetFingerprint(contentChanged);
            Require(!string.Equals(firstFingerprint, contentFingerprint, StringComparison.Ordinal),
                "Editing unowned source content did not invalidate the source fingerprint.");

            File.Delete(Path.Combine(root, firstPath));
            WriteFile(root, secondPath, "public sealed class Unowned { public int Value => 2; }\n");
            Aph700ReportArtifacts pathChanged = Aph700AssemblyDependencyReportGenerator.Generate(root);
            Require(!string.Equals(contentFingerprint, GetFingerprint(pathChanged), StringComparison.Ordinal),
                "Moving identical unowned source content did not invalidate the source fingerprint.");
            RequireEqual(1, GetUnownedSourceCount(pathChanged),
                "Unowned source count changed after the path-only fixture mutation.");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static void RunAsmrefUnsupportedContract()
    {
        string root = CreateMinimalFixture("aph700-asmref-");
        try
        {
            WriteFile(
                root,
                "Assets/Game/Host/Game.Host.asmref",
                "{\n  \"reference\": \"Game.Core\"\n}\n");
            ExpectThrows<NotSupportedException>(
                () => Aph700AssemblyDependencyReportGenerator.Generate(root),
                ".asmref");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    private static string CreateMinimalFixture(string prefix)
    {
        string root = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Core"));
        Directory.CreateDirectory(Path.Combine(root, "Assets/Game/Host"));
        Directory.CreateDirectory(Path.Combine(root, "Assets/Tests"));
        Directory.CreateDirectory(Path.Combine(root, "Assets/Editor"));
        WriteAsmdef(root, "Assets/Game/Core/Game.Core.asmdef", "Game.Core",
            "11111111111111111111111111111111");
        WriteAsmdef(root, "Assets/Game/Host/Game.Host.asmdef", "Game.Host",
            "22222222222222222222222222222222", "Game.Core");
        WriteFile(root, "Assets/Game/Core/CoreType.cs",
            "namespace Core { public struct CoreType { } }\n");
        WriteFile(root, "Assets/Game/Host/HostType.cs",
            "using Core; namespace Host { public sealed class HostType { private CoreType _value; } }\n");
        return root;
    }

    private static void ValidateCurrentRepository(string projectRoot, Aph700ReportArtifacts artifacts)
    {
        using JsonDocument document = JsonDocument.Parse(artifacts.Json);
        JsonElement report = document.RootElement;
        RequireEqual("APH-700", report.GetProperty("taskId").GetString(), "Report task ID changed.");
        RequireEqual(1, report.GetProperty("schemaVersion").GetInt32(), "Report schema version changed.");

        string[] asmdefPaths = EnumerateFirstPartyAsmdefs(projectRoot);
        var nameByPath = new Dictionary<string, string>(StringComparer.Ordinal);
        var pathByName = new Dictionary<string, string>(StringComparer.Ordinal);
        var nameByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string asmdefPath in asmdefPaths)
        {
            using JsonDocument asmdef = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, asmdefPath)));
            string name = asmdef.RootElement.GetProperty("name").GetString();
            nameByPath.Add(asmdefPath, name);
            pathByName.Add(name, asmdefPath);
            string guid = ReadGuid(Path.Combine(projectRoot, asmdefPath + ".meta"));
            if (!string.IsNullOrWhiteSpace(guid))
                nameByGuid.Add(guid, name);
        }

        var expectedEdges = new SortedSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> assembly in nameByPath)
        {
            using JsonDocument asmdef = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectRoot, assembly.Key)));
            if (!asmdef.RootElement.TryGetProperty("references", out JsonElement references))
                continue;
            foreach (JsonElement referenceElement in references.EnumerateArray())
            {
                string reference = referenceElement.GetString();
                string target = null;
                if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                    nameByGuid.TryGetValue(reference.Substring("GUID:".Length).Trim(), out target);
                else
                    pathByName.TryGetValue(reference, out string _);
                if (target == null && pathByName.ContainsKey(reference))
                    target = reference;
                if (target != null)
                    expectedEdges.Add(assembly.Value + "->" + target);
            }
        }

        string[] actualEdges = report.GetProperty("firstPartyEdges").EnumerateArray()
            .Select(EdgeKey)
            .ToArray();
        RequireEqual(asmdefPaths.Length,
            report.GetProperty("summary").GetProperty("assemblyCount").GetInt32(),
            "Report did not discover every current first-party asmdef.");
        RequireSequenceEqual(expectedEdges.ToArray(), actualEdges,
            "Report edge list differs from the current first-party asmdefs.");
        RequireEqual(actualEdges.Length, actualEdges.Distinct(StringComparer.Ordinal).Count(),
            "Report contains duplicate first-party edges.");
        Require(actualEdges.SequenceEqual(actualEdges.OrderBy(item => item, StringComparer.Ordinal)),
            "Report first-party edges are not ordinally sorted.");

        JsonElement topReferences = report.GetProperty("topCrossDomainTypeReferences");
        Require(topReferences.GetArrayLength() > 0, "Report did not produce cross-domain type references.");
        var edgeSet = new HashSet<string>(actualEdges, StringComparer.Ordinal);
        foreach (JsonElement reference in topReferences.EnumerateArray())
        {
            Require(edgeSet.Contains(EdgeKey(reference)),
                "Ranked type reference does not belong to a declared first-party edge.");
            Require(reference.GetProperty("occurrenceCount").GetInt32() > 0,
                "Ranked type reference has no occurrences.");
        }

        string fingerprint = report.GetProperty("sourceFingerprintSha256").GetString();
        Require(fingerprint != null && fingerprint.Length == 64 &&
                fingerprint.All(character => char.IsDigit(character) || character is >= 'a' and <= 'f'),
            "Source fingerprint is not lowercase SHA-256.");
        RequireEqual(0,
            report.GetProperty("summary").GetProperty("unownedScopedSourceFileCount").GetInt32(),
            "Scoped C# source exists outside all discovered first-party asmdef roots.");

        string jsonPath = Path.Combine(projectRoot, Aph700AssemblyDependencyReportGenerator.JsonReportPath);
        string markdownPath = Path.Combine(projectRoot, Aph700AssemblyDependencyReportGenerator.MarkdownReportPath);
        Require(File.Exists(jsonPath), "Tracked JSON report is missing.");
        Require(File.Exists(markdownPath), "Tracked Markdown report is missing.");
        RequireEqual(File.ReadAllText(jsonPath), artifacts.Json, "Tracked JSON report is stale.");
        RequireEqual(File.ReadAllText(markdownPath), artifacts.Markdown, "Tracked Markdown report is stale.");
    }

    private static string[] EnumerateFirstPartyAsmdefs(string projectRoot)
    {
        string[] roots = { "Assets/Game", "Assets/Tests", "Assets/Editor" };
        return roots.SelectMany(root => Directory.EnumerateFiles(
                Path.Combine(projectRoot, root), "*.asmdef", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(projectRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static void WriteAsmdef(
        string root,
        string relativePath,
        string name,
        string guid,
        params string[] references)
    {
        string json = JsonSerializer.Serialize(new { name, references }, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        WriteFile(root, relativePath, json + "\n");
        WriteFile(root, relativePath + ".meta", "fileFormatVersion: 2\nguid: " + guid + "\n");
    }

    private static void WriteFile(string root, string relativePath, string content)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static string ReadGuid(string path)
    {
        if (!File.Exists(path))
            return null;
        string line = File.ReadLines(path).FirstOrDefault(item => item.StartsWith("guid:", StringComparison.Ordinal));
        return line?.Substring("guid:".Length).Trim();
    }

    private static string EdgeKey(JsonElement element)
    {
        return element.GetProperty("sourceAssembly").GetString() + "->" +
               element.GetProperty("targetAssembly").GetString();
    }

    private static int GetTypeOccurrenceCount(JsonElement edge, string fullTypeName)
    {
        return edge.GetProperty("topTypeReferences").EnumerateArray()
            .Single(item => item.GetProperty("fullTypeName").GetString() == fullTypeName)
            .GetProperty("occurrenceCount")
            .GetInt32();
    }

    private static bool HasTypeReference(JsonElement edge, string fullTypeName)
    {
        return edge.GetProperty("topTypeReferences").EnumerateArray()
            .Any(item => item.GetProperty("fullTypeName").GetString() == fullTypeName);
    }

    private static string GetFingerprint(Aph700ReportArtifacts artifacts)
    {
        using JsonDocument document = JsonDocument.Parse(artifacts.Json);
        return document.RootElement.GetProperty("sourceFingerprintSha256").GetString();
    }

    private static int GetUnownedSourceCount(Aph700ReportArtifacts artifacts)
    {
        using JsonDocument document = JsonDocument.Parse(artifacts.Json);
        return document.RootElement.GetProperty("summary")
            .GetProperty("unownedScopedSourceFileCount")
            .GetInt32();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RequireEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
    }

    private static void RequireSequenceEqual(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual,
        string message)
    {
        if (expected.Count == actual.Count && expected.SequenceEqual(actual, StringComparer.Ordinal))
            return;
        throw new InvalidOperationException(
            $"{message} Expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
    }

    private static TException ExpectThrows<TException>(Action action, string requiredMessageFragment)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            Require(exception.Message.Contains(requiredMessageFragment, StringComparison.OrdinalIgnoreCase),
                $"Expected {typeof(TException).Name} message to contain '{requiredMessageFragment}', " +
                $"got '{exception.Message}'.");
            return exception;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(TException).Name}, got {exception.GetType().Name}.", exception);
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but no exception was thrown.");
    }
}
