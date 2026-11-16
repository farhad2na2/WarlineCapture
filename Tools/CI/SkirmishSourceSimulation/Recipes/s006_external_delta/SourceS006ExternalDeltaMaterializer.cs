using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Data-only diagnostic seam. The caller must independently validate the input
// descriptor and its pins. This does not admit a mission or certify source fit.
internal static class SourceS006ExternalDeltaMaterializer
{
    internal static SkirmishScenarioDefinitionConfig MaterializeVerified(
        byte[] bytes, string expectedSha256, SkirmishScenarioDefinitionConfig sourceDefinition,
        SkirmishResolvedSetup setup, OperationMapDefinition map, out SkirmishMapLayoutConfig layout)
    {
        if (bytes == null || string.IsNullOrEmpty(expectedSha256) || expectedSha256.Length != 64 ||
            expectedSha256.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
            throw new InvalidOperationException("Exact lowercase SHA-256 and delta bytes required.");
        string actual;
        using (var sha = SHA256.Create())
            actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        if (actual != expectedSha256) throw new InvalidOperationException("Delta bytes differ from the verified input pin.");
        var text = new UTF8Encoding(false, true).GetString(bytes);
        JObject input;
        using (var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None, MaxDepth = 64 })
        {
            input = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidOperationException("Trailing JSON content is not supported.");
        }
        if (input.Descendants().OfType<JValue>().Any(value => value.Type == JTokenType.Float &&
                (double.IsNaN((double)value) || double.IsInfinity((double)value))))
            throw new InvalidOperationException("Non-finite diagnostic configuration is not supported.");
        return Materialize(input, sourceDefinition, setup, map, out layout);
    }

    private static SkirmishScenarioDefinitionConfig Materialize(
        JObject input, SkirmishScenarioDefinitionConfig sourceDefinition,
        SkirmishResolvedSetup setup, OperationMapDefinition map,
        out SkirmishMapLayoutConfig layout)
    {
        if (input == null || (string)input["kind"] != "S006ExternalConfigDeltaCandidate" ||
            (int?)input["schema_version"] != 1 || (bool?)input["runtime_admission"] != false ||
            (bool?)input["source_fit_certified"] != false || (bool?)input["mission_ready"] != false ||
            sourceDefinition?.CatalogId != "S006" || sourceDefinition.MapLayout == null ||
            sourceDefinition.CampaignMap != map || setup?.CatalogId != "S006" || map == null)
            throw new InvalidOperationException("Exact diagnostic S006 source and external delta required.");

        var groups = input["rows"] as JArray ?? throw new InvalidOperationException("Delta rows required.");
        var matches = groups.OfType<JObject>().Where(r => (string)r["size"] == setup.SizeId.ToString()).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Exactly one matching size required.");
        var delta = matches[0]["disposable_pad_projection"]?["candidate_additions"] as JObject ??
            throw new InvalidOperationException("Candidate additions required.");
        if ((string)delta["declared_size"] != setup.SizeId.ToString() ||
            (string)delta["declared_difficulty"] != setup.DifficultyId.ToString() ||
            (bool?)delta["measured_deployments"] != false || (bool?)delta["bind_all_configurations"] != false)
            throw new InvalidOperationException("Exact unmeasured declared tuple required.");

        var original = sourceDefinition.MapLayout;
        layout = Activator.CreateInstance<SkirmishMapLayoutConfig>();
        foreach (var field in typeof(SkirmishMapLayoutConfig).GetFields(
                     BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            field.SetValue(layout, field.GetValue(original));
        var removed = (delta["removed_pad_ids"] as JArray)?.Values<string>().ToArray() ??
            throw new InvalidOperationException("Removed pad identities required.");
        if (removed.Length == 0 || removed.Distinct(StringComparer.Ordinal).Count() != removed.Length ||
            removed.Any(id => original.Pads.Count(p => p.PadId == id) != 1))
            throw new InvalidOperationException("Exact unique removed pad identities required.");
        var anchors = (delta["anchors"] as JArray)?.ToObject<SkirmishLayoutAnchorConfig[]>() ??
            throw new InvalidOperationException("Anchors required.");
        var pads = (delta["pads"] as JArray)?.ToObject<SkirmishLegalPadConfig[]>() ??
            throw new InvalidOperationException("Pads required.");
        var bindings = (delta["instance_bindings"] as JArray)?.ToObject<SkirmishStructureInstancePadBinding[]>() ??
            throw new InvalidOperationException("Instance bindings required.");
        var af = typeof(SkirmishMapLayoutConfig).GetField("anchors", BindingFlags.Instance | BindingFlags.NonPublic);
        var pf = typeof(SkirmishMapLayoutConfig).GetField("pads", BindingFlags.Instance | BindingFlags.NonPublic);
        var df = typeof(SkirmishMapLayoutConfig).GetField("declaredConfigurations", BindingFlags.Instance | BindingFlags.NonPublic);
        if (af == null || pf == null || df == null) throw new InvalidOperationException("Exact layout fields required.");
        af.SetValue(layout, original.Anchors.Concat(anchors).ToArray());
        pf.SetValue(layout, original.Pads.Where(p => !removed.Contains(p.PadId)).Concat(pads).ToArray());
        layout.BindStructureInstancePads(bindings);
        var player = delta["player_deployment"] as JArray;
        var enemy = delta["enemy_deployment"] as JArray;
        if (player?.Count != 2 || enemy?.Count != 2) throw new InvalidOperationException("Two deployment coordinates required.");
        df.SetValue(layout, new[] { new SkirmishDeclaredLayoutConfiguration {
            SizeId = setup.SizeId, DifficultyId = setup.DifficultyId,
            PlayerDeployment = new Vector2((float)player[0], (float)player[1]),
            EnemyDeployment = new Vector2((float)enemy[0], (float)enemy[1]) } });
        var frame = layout.WorldBinding;
        frame.HasMeasuredDeployments = false;
        frame.BindAllConfigurations = false;
        layout.ApplyWorldBinding(frame);

        var definition = Activator.CreateInstance<SkirmishScenarioDefinitionConfig>();
        definition.Configure("S006", sourceDefinition.ObjectiveConfig, sourceDefinition.ArmyProfileConfig,
            sourceDefinition.StartPackageConfig, sourceDefinition.EconomyConfig, sourceDefinition.UpgradeCatalog,
            sourceDefinition.IntelConfig, sourceDefinition.RoleCatalog, layout, featureIds: sourceDefinition.RequiredFeatureIds);
        definition.BindCampaignMap(map);
        return definition;
    }
}
