using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;

// Data-only control for the original S002 setup compiler. Never creates a World.
internal static class S002SourceGraphCompilerControl
{
    private const string ScenarioPath = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset";
    private const string CatalogPath = "Assets/Game/Resources/SkirmishExpansionCatalog.asset";
    private const string MapPath = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
    private static readonly string[] RequestFields = {
        "schema_version", "kind", "mission", "seed", "difficulty_id", "size_id",
        "source_config_graph", "setup_matrix", "repository_root", "output_directory"
    };

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static JObject ReadJson(string path, string expected, int maximum)
    {
        var raw = AutonomousWorld.PinnedFile(path, expected, maximum);
        using (var text = new StringReader(System.Text.Encoding.UTF8.GetString(raw)))
        using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
        {
            var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Exactly one JSON document required: " + path);
            return value;
        }
    }

    private static JObject Descriptor(JObject owner, string field)
    {
        var row = owner[field] as JObject ?? throw new InvalidDataException("Descriptor required: " + field);
        if (row.Properties().Count() != 2 || row.Properties().Any(p => p.Name != "path" && p.Name != "sha256") ||
            row["path"]?.Type != JTokenType.String || row["sha256"]?.Type != JTokenType.String)
            throw new InvalidDataException("Exact path/SHA descriptor required: " + field);
        return row;
    }

    private static void RequireFalse(JObject result, string field)
    {
        if ((bool?)result[field] != false) throw new InvalidDataException("Diagnostic qualification must remain false: " + field);
    }

    private static int Run(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("One exact source-only compiler request required.");
        string requestPath = Path.GetFullPath(args[0]);
        byte[] requestRaw = File.ReadAllBytes(requestPath);
        if (requestRaw.Length == 0 || requestRaw.Length > 1024 * 1024) throw new InvalidDataException("Bounded compiler request required.");
        JObject request;
        using (var text = new StringReader(System.Text.Encoding.UTF8.GetString(requestRaw)))
        using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
        {
            request = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Exactly one request document required.");
        }
        if (request.Properties().Count() != RequestFields.Length || request.Properties().Any(p => !RequestFields.Contains(p.Name)) ||
            (int?)request["schema_version"] != 1 || (string)request["kind"] != "S002SourceGraphCompileRequest" ||
            (string)request["mission"] != "S002" || (int?)request["seed"] != 104731 ||
            (int?)request["difficulty_id"] != 2 || (int?)request["size_id"] != 1)
            throw new InvalidDataException("Exact S002/104731/Regular/Standard compiler profile required.");

        string repository = Path.GetFullPath((string)request["repository_root"]);
        string output = Path.GetFullPath((string)request["output_directory"]);
        if (Path.GetFullPath(Environment.CurrentDirectory) != repository)
            throw new InvalidDataException("Compiler control must run from its pinned repository root.");
        if (!Directory.Exists(output) || Directory.GetFileSystemEntries(output).Length != 0)
            throw new InvalidOperationException("Fresh empty output directory required.");

        JObject graphDescriptor = Descriptor(request, "source_config_graph");
        JObject matrixDescriptor = Descriptor(request, "setup_matrix");
        string graphPath = Path.GetFullPath((string)graphDescriptor["path"]);
        string matrixPath = Path.GetFullPath((string)matrixDescriptor["path"]);
        JObject graph = ReadJson(graphPath, (string)graphDescriptor["sha256"], 16 * 1024 * 1024);
        if ((int?)graph["schema_version"] != 1 || (string)graph["authority"] != "AuthoredConfigAssets" ||
            (string)graph["definition_path"] != ScenarioPath || (string)graph["catalog_path"] != CatalogPath ||
            (bool?)graph["compiled_setup"] != false || (bool?)graph["world_initialized"] != false ||
            (bool?)graph["native_world_state_used"] != false)
            throw new InvalidDataException("Current authored S002 source graph required.");
        JObject graphMatrix = graph["setup_matrix"] as JObject ?? throw new InvalidDataException("Graph-owned setup matrix required.");
        if (Path.GetFullPath((string)graphMatrix["path"]) != matrixPath ||
            (string)graphMatrix["sha256"] != (string)matrixDescriptor["sha256"])
            throw new InvalidDataException("Compiler matrix must be the graph-owned current matrix.");

        var pins = graph["source_pins"] as JObject ?? throw new InvalidDataException("Current graph source pins required.");
        if (pins.Count == 0) throw new InvalidDataException("Nonempty source graph pin set required.");
        foreach (var pin in pins.Properties()) AutonomousWorld.PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
        AutonomousWorld.PinnedFile(matrixPath, (string)matrixDescriptor["sha256"], 4 * 1024 * 1024);

        JObject map = ((JArray)graph["nodes"]).OfType<JObject>()
            .SingleOrDefault(row => (string)row["source_path"] == MapPath)
            ?? throw new InvalidDataException("Current selected Desert Base map node required.");
        if ((string)map["fields"]?["operationMapId"] != "opmap.skirmish.desert_base_01")
            throw new InvalidDataException("Exact selected S002 map identity required.");
        var geometryTargets = graph["geometry_source_targets"] as JObject
            ?? throw new InvalidDataException("Pinned current map source-target descriptors required.");
        string[] expectedTargetFields = { "gridMetadata", "surfaceMetadata", "mapSurfaceDataReference", "sourceSceneReference" };
        if (geometryTargets.Properties().Count() != expectedTargetFields.Length ||
            geometryTargets.Properties().Any(p => !expectedTargetFields.Contains(p.Name)))
            throw new InvalidDataException("Exact current Desert Base map source-target set required.");
        foreach (var target in geometryTargets.Properties())
        {
            JObject row = target.Value as JObject ?? throw new InvalidDataException("Exact map source target required: " + target.Name);
            string targetPath = Path.GetFullPath(Path.Combine(repository, (string)row["path"]));
            byte[] targetRaw = File.Exists(targetPath) ? File.ReadAllBytes(targetPath) : Array.Empty<byte>();
            if (!targetPath.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                targetRaw.Length == 0 || (long?)row["bytes"] != targetRaw.LongLength || Hash(targetRaw) != (string)row["sha256"])
                throw new InvalidDataException("Current map source target pin changed: " + target.Name);
        }

        var values = new SourceConfigMaterializer(graph);
        var definition = values.ByPath<SkirmishScenarioDefinitionConfig>(ScenarioPath);
        var catalog = values.ByPath<SkirmishExpansionCatalogConfig>(CatalogPath);
        byte[] matrixBytes = AutonomousWorld.PinnedFile(matrixPath, (string)matrixDescriptor["sha256"], 4 * 1024 * 1024);
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object> {
            { SkirmishExpansionCatalogConfig.ResourceName, catalog },
            { SkirmishSetupMatrixTable.PackagedResourceName, new UnityEngine.TextAsset(System.Text.Encoding.UTF8.GetString(matrixBytes)) }
        });
        if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
            throw new InvalidDataException("Current S002 setup matrix rejected: " + matrixError);
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
        bool accepted = SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
            SkirmishSizeId.Standard, 104731, manifest, matrix, out var setup, out var reasons);

        foreach (var pin in pins.Properties()) AutonomousWorld.PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
        AutonomousWorld.PinnedFile(matrixPath, (string)matrixDescriptor["sha256"], 4 * 1024 * 1024);
        foreach (var target in geometryTargets.Properties())
        {
            JObject row = (JObject)target.Value;
            string targetPath = Path.GetFullPath(Path.Combine(repository, (string)row["path"]));
            byte[] targetRaw = File.ReadAllBytes(targetPath);
            if ((long?)row["bytes"] != targetRaw.LongLength || Hash(targetRaw) != (string)row["sha256"])
                throw new InvalidDataException("Map source target changed during compiler control: " + target.Name);
        }
        if (File.ReadAllBytes(requestPath).Length != requestRaw.Length || Hash(File.ReadAllBytes(requestPath)) != Hash(requestRaw))
            throw new InvalidDataException("Compiler request changed during control.");

        var result = new JObject {
            ["schema_version"] = 1, ["kind"] = "S002SourceGraphCompileResult",
            ["status"] = accepted ? "ConfigurationCompiled" : "CompilerRejected",
            ["mission"] = "S002", ["seed"] = 104731, ["difficulty"] = "Regular", ["size"] = "Standard",
            ["definition_id"] = definition.DefinitionId, ["operation_map_id"] = definition.OperationMapId,
            ["selected_map_source"] = "opmap.skirmish.desert_base_01", ["compiler_accepted"] = accepted,
            ["compiler_reasons"] = new JArray(reasons.Select(row => new JObject {
                ["code"] = row.Code.ToString(), ["field"] = row.Field, ["detail"] = row.Detail
            })),
            ["setup_hash"] = accepted ? setup.SetupHash : (uint?)null,
            ["force_count"] = accepted ? setup.Forces.Length : 0,
            ["structure_count"] = accepted ? setup.Structures.Length : 0,
            ["source_graph_sha256"] = (string)graphDescriptor["sha256"],
            ["setup_matrix_sha256"] = (string)matrixDescriptor["sha256"],
            ["world_allocated"] = false, ["geometry_materialized"] = false,
            ["source_fit_verified"] = false, ["native_acceptance"] = false,
            ["mission_ready"] = false,
            ["scope"] = "Original S002 SkirmishSetupCompiler data-only diagnostic. Map/geometry targets are hash checked but not materialized or fit tested. No native metadata qualification or runtime admission."
        };
        foreach (string field in new[] { "world_allocated", "geometry_materialized", "source_fit_verified", "native_acceptance", "mission_ready" }) RequireFalse(result, field);
        File.WriteAllText(Path.Combine(output, "compiler-result.json"), result.ToString(Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S002SourceGraphCompilerControl] result=Completed status=" + result["status"] +
            " compiler_accepted=" + accepted.ToString().ToLowerInvariant() + " world_allocated=False mission_ready=False");
        return 0;
    }

    private static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception error)
        {
            Console.Error.WriteLine("[S002SourceGraphCompilerControl] result=Failed " + error);
            return 1;
        }
    }
}
