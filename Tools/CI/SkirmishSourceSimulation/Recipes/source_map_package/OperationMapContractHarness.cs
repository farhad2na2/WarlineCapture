using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json.Linq;

// Data-only source host for the original public operation-map contract validator.
// Inputs are explicit so the same harness can validate any authored logical/physical pair.
internal static class OperationMapContractHarness
{
    public static int Main(string[] args)
    {
        try
        {
            if (args == null || args.Length != 4)
                throw new InvalidDataException("Expected <source-config-graph.json> <logical-source-path> <physical-source-path> <independent-evidence.json>");

            string graphPath = args[0];
            string logicalPath = args[1];
            string physicalPath = args[2];
            JObject graph = JObject.Parse(File.ReadAllText(graphPath));
            var assets = new SourceConfigMaterializer(graph);
            OperationMapDefinition logical = assets.ByPath<OperationMapDefinition>(logicalPath);
            OperationMapDefinition physical = assets.ByPath<OperationMapDefinition>(physicalPath);
            var definitions = new[] { logical, physical };
            JArray graphNodes = (JArray)graph["nodes"];
            ValidateAuthoredBytes(graphNodes, logicalPath, args[3], 0);
            ValidateAuthoredBytes(graphNodes, physicalPath, args[3], 1);
            OperationMapContractEvidence[] evidence = ReadEvidence(args[3]);

            if (evidence.Length != definitions.Length)
                throw new InvalidDataException("Independent evidence must contain exactly one row per materialized definition.");
            CheckEvidence(logical, evidence[0]);
            CheckEvidence(physical, evidence[1]);
            if (!OperationMapContractValidation.TryValidate(definitions, Array.Empty<ScenarioSetupConfig>(), evidence, out string error))
                throw new InvalidDataException("Original public validator rejected materialized definitions: " + error);

            var originalBinding = logical.SourceBinding;
            string changedSourceId = "opmap.skirmish.source_host_negative_control";
            int suffix = 0;
            while (definitions.Any(definition => definition.OperationMapId == changedSourceId))
                changedSourceId = "opmap.skirmish.source_host_negative_control_" + (++suffix);
            typeof(OperationMapDefinition).GetField("sourceBinding", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(logical, new OperationMapSourceBindingConfig(
                    changedSourceId, originalBinding.SourceIdentityHash, originalBinding.SourceContentHash));
            bool changedBindingRejected = !OperationMapContractValidation.TryValidate(
                definitions, Array.Empty<ScenarioSetupConfig>(), evidence, out string negativeError);
            if (!changedBindingRejected || String.IsNullOrEmpty(negativeError))
                throw new InvalidDataException("Original public validator accepted a changed logical source binding.");

            Console.WriteLine(new JObject {
                ["kind"] = "OriginalOperationMapContractSourceOnlyResult",
                ["status"] = "Passed",
                ["logical_map_id"] = logical.OperationMapId,
                ["physical_source_map_id"] = physical.OperationMapId,
                ["definitions_count"] = definitions.Length,
                ["scenario_count"] = 0,
                ["original_public_validator"] = "OperationMapContractValidation.TryValidate",
                ["actual_materialized_assets"] = new JArray(logicalPath, physicalPath),
                ["independent_evidence_path"] = Path.GetFullPath(args[3]),
                ["independently_pinned_evidence_count"] = evidence.Length,
                ["changed_binding_rejected"] = changedBindingRejected,
                ["changed_binding_error"] = negativeError,
                ["world_initialized"] = false
            }.ToString(Newtonsoft.Json.Formatting.None));
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[OriginalOperationMapContract] result=Failed " + e);
            return 1;
        }
    }

    private static OperationMapContractEvidence[] ReadEvidence(string path)
    {
        JArray rows = JArray.Parse(File.ReadAllText(path));
        var result = new OperationMapContractEvidence[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            JObject row = (JObject)rows[i];
            result[i] = new OperationMapContractEvidence(
                (string)row["operationMapId"], (int)row["schemaVersion"], (int)row["contentVersion"],
                (string)row["sourceIdentityHash"], (string)row["contentHash"], (string)row["generatedMetadataHash"]);
        }
        return result;
    }

    private static void ValidateAuthoredBytes(JArray graphNodes, string sourcePath, string evidencePath, int index)
    {
        JObject evidence = (JObject)JArray.Parse(File.ReadAllText(evidencePath))[index];
        JObject node = null;
        foreach (JObject candidate in graphNodes)
        {
            if ((string)candidate["source_path"] == sourcePath && (string)candidate["source_type"] == "OperationMapDefinition")
            {
                node = candidate;
                break;
            }
        }
        if (node == null)
            throw new InvalidDataException("Authored source graph is missing operation-map node: " + sourcePath);
        string actualHash;
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(sourcePath))
            actualHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", String.Empty).ToLowerInvariant();
        if ((string)evidence["sourcePath"] != sourcePath ||
            (string)evidence["authoredDefinitionSha256"] != actualHash ||
            (string)node["source_sha256"] != actualHash)
            throw new InvalidDataException("Independent evidence, graph pin, and authored bytes do not agree: " + sourcePath);
    }

    private static void CheckEvidence(OperationMapDefinition actual, OperationMapContractEvidence expected)
    {
        if (actual.OperationMapId != expected.OperationMapId || actual.SchemaVersion != expected.SchemaVersion ||
            actual.ContentVersion != expected.ContentVersion || actual.SourceIdentityHash != expected.SourceIdentityHash ||
            actual.ContentHash != expected.ContentHash || actual.GeneratedMetadataHash != expected.GeneratedMetadataHash)
            throw new InvalidDataException("Materialized definition differs from independent version/hash evidence: " + actual.OperationMapId);
    }
}
