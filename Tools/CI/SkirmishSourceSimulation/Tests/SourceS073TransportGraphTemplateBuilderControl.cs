using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class SourceS073TransportGraphTemplateBuilderControl
{
    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 transport graph builder: " + message);
    }

    static SourceUnitPrefab CanonicalToken() =>
        SourceUnitPrefab.FromAssetIdentity(
            SourceS073TransportGraphTemplateBuilder.TransportKey,
            SourceS073TransportGraphTemplateBuilder.TransportPath,
            SourceS073TransportGraphTemplateBuilder.TransportGuid,
            SourceS073TransportGraphTemplateBuilder.TransportLocalId);

    static bool Rejects(JObject row, SourceUnitPrefab token)
    {
        try
        {
            SourceS073TransportGraphTemplateBuilder.Build(row, token);
            return false;
        }
        catch (InvalidDataException)
        {
            return true;
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (NotSupportedException)
        {
            return true;
        }
    }

    static void Main(string[] args)
    {
        Need(args.Length == 1, "repository root argument required");
        string metadataPath = Path.Combine(Path.GetFullPath(args[0]),
            "Design/AgentReports/SkirmishProduction/S073/TransportMetadataReadback20261101/s073-unit-production-metadata.json");
        byte[] bytes = File.ReadAllBytes(metadataPath);
        using var sha = SHA256.Create();
        string digest = string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2")));
        Need(digest == "1372d72063491572eeaa11ffc78a8bbf790f10d1ef9f673fe5627af4447c3e8e",
            "authored metadata hash mismatch");
        JObject metadata = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        JObject row = ((JArray)metadata["transports"]).OfType<JObject>().Single();

        SourceUnitPrefab canonical = CanonicalToken();
        var templates = SourceS073TransportGraphTemplateBuilder.Build(row, canonical);
        templates.TryGetValue(canonical, out var template);
        Need(templates.Count == 1 && template != null &&
             ReferenceEquals(template.Prefab, canonical),
            "valid graph was not keyed by the exact caller-supplied canonical token");
        Need(template.CreatePose() != null, "valid source graph did not produce a pose");

        SourceUnitPrefab wrongToken = SourceUnitPrefab.FromAssetIdentity(
            SourceS073TransportGraphTemplateBuilder.TransportKey,
            SourceS073TransportGraphTemplateBuilder.TransportPath,
            new string('0', 32),
            SourceS073TransportGraphTemplateBuilder.TransportLocalId);
        Need(Rejects(row, wrongToken), "mismatched canonical transport token was accepted");

        JObject malformed = (JObject)row.DeepClone();
        JArray nodes = (JArray)malformed["nodes"];
        Need(nodes.Count > 1, "source transport graph unexpectedly has no child node");
        nodes[1]["parent"] = 1;
        Need(Rejects(malformed, canonical), "cyclic/non-topological hierarchy was accepted");

        Console.WriteLine("[SourceS073TransportGraphTemplateBuilderControl] result=Passed scope=PinnedAuthoredTransportGraph_ExactCanonicalToken_NoDeliveryBinding");
    }
}
