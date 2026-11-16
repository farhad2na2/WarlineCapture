using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Collections;

internal static class S073CurrentSourceCatalogControl
{
    private static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static string Value(JObject fields, string name) => (string)fields[name]?["value"];
    private static int Integer(JObject fields, string name) => int.Parse(Value(fields, name), System.Globalization.CultureInfo.InvariantCulture);

    private static void ExerciseGuidIndexControls()
    {
        string root = Path.Combine(Path.GetTempPath(), "s073-guid-index-" + Guid.NewGuid().ToString("N"));
        string assets = Path.Combine(root, "Assets");
        Directory.CreateDirectory(assets);
        const string firstGuid = "11111111111111111111111111111111";
        const string otherGuid = "22222222222222222222222222222222";
        const string bomGuid = "33333333333333333333333333333333";
        string first = Path.Combine(assets, "first.asset.meta");
        string bomMeta = Path.Combine(assets, "bom.asset.meta");
        string added = Path.Combine(assets, "added.asset.meta");
        string original = "fileFormatVersion: 2\nguid: " + firstGuid + "\n";
        try
        {
            File.WriteAllText(first, original);
            File.WriteAllText(bomMeta, "fileFormatVersion: 2\nguid: " + bomGuid + "\n", System.Text.Encoding.Unicode);
            var index = new S073AssetGuidIndex(root);
            Need(index.FindUniquePath(firstGuid) == "Assets/first.asset", "GUID index must resolve the exact unique asset");
            Need(index.FindUniquePath(bomGuid) == "Assets/bom.asset", "GUID index must preserve BOM-aware text decoding");
            bool wrongRootRejected = false;
            try { index.EnsureRepositoryRoot(root + "-different"); } catch (InvalidDataException) { wrongRootRejected = true; }
            Need(wrongRootRejected, "GUID index must reject use with a different repository root");
            File.WriteAllText(added, "fileFormatVersion: 2\nguid: " + otherGuid + "\n");
            bool additionRejected = false;
            try { index.VerifyFresh(); } catch (InvalidDataException) { additionRejected = true; }
            Need(additionRejected, "GUID index freshness must reject an added meta file");
            File.Delete(added);

            File.WriteAllText(added, "fileFormatVersion: 2\nguid: " + firstGuid + "\n");
            var duplicateIndex = new S073AssetGuidIndex(root);
            bool duplicateRejected = false;
            try { duplicateIndex.FindUniquePath(firstGuid); } catch (InvalidDataException) { duplicateRejected = true; }
            Need(duplicateRejected, "GUID index must fail closed on duplicate GUIDs");
            File.Delete(added);

            var removalIndex = new S073AssetGuidIndex(root);
            File.Delete(first);
            bool removalRejected = false;
            try { removalIndex.VerifyFresh(); } catch (InvalidDataException) { removalRejected = true; }
            Need(removalRejected, "GUID index freshness must reject a removed meta file");

            File.WriteAllText(first, original);
            var contentIndex = new S073AssetGuidIndex(root);
            File.WriteAllText(first, "fileFormatVersion: 2\nguid: " + firstGuid + "\nchanged: 1\n");
            bool contentChangeRejected = false;
            try { contentIndex.VerifyFresh(); } catch (InvalidDataException) { contentChangeRejected = true; }
            Need(contentChangeRejected, "GUID index freshness must reject changed meta content");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Main(string[] args)
    {
        Need(args.Length == 8, "repo, definitions, legacy source, identity, identity SHA, prefab capture, capture SHA, output required");
        ExerciseGuidIndexControls();
        string repo = Path.GetFullPath(args[0]);
        var definitionsDocument = JObject.Parse(File.ReadAllText(args[1]));
        var definitions = new List<RuntimeDefinition>();
        foreach (JObject row in (JArray)definitionsDocument["definitions"])
        {
            JObject fields = (JObject)row["fields"];
            JObject prefab = (JObject)fields["Prefab"];
            var token = SourceUnitPrefab.FromAssetIdentity((string)prefab["key"], (string)prefab["path"],
                (string)prefab["guid"], (long)prefab["local_id"]);
            definitions.Add(new RuntimeDefinition { Prefab = token, DisplayName = Value(fields, "DisplayName"),
                Description = Value(fields, "Description"), CreditsCost = Integer(fields, "CreditsCost"),
                MaterialsCost = Integer(fields, "MaterialsCost") });
        }
        JObject buildingEvidence;
        var buildings = S073NativeConfiguredCatalog.BindCurrentAuthored(repo, definitions, out buildingEvidence);
        Need(buildings.Count == 6 && ((JArray)buildingEvidence["rows"]).Count == 6,
            "All six current Construction building configs must bind through original owner projection");
        var changed = new List<RuntimeDefinition>(definitions);
        changed[1] = new RuntimeDefinition { Prefab = definitions[1].Prefab, DisplayName = definitions[1].DisplayName,
            Description = definitions[1].Description, CreditsCost = definitions[1].CreditsCost + 1,
            MaterialsCost = definitions[1].MaterialsCost };
        bool mismatchRejected = false;
        try { S073NativeConfiguredCatalog.BindCurrentAuthored(repo, changed, out _); }
        catch (InvalidDataException) { mismatchRejected = true; }
        Need(mismatchRejected, "Changed current definition cost must fail closed");

        JObject identity = JObject.Parse(File.ReadAllText(args[3]));
        JObject capture = JObject.Parse(File.ReadAllText(args[5]));
        var roots = (JObject)capture["opening_prefabs"];
        var em = new EntityManager();
        int nextEntity = 1000;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextEntity++, Version = 1 };
        var imported = new GeneratedReferenceTemplateImporter.Result { Manager = em };
        foreach (JObject row in (JArray)identity["objects"])
        {
            string name = (string)row["unit_source_key"];
            if (string.IsNullOrEmpty(name)) continue;
            Entity template = em.CreateEntity(typeof(Prefab), typeof(UnitSourcePrefabKey));
            em.SetComponentData(template, new UnitSourcePrefabKey { Value = new FixedString64Bytes(name) });
            imported.Prefabs.Add(name, template);
            imported.SourceToLogical.Add(ExactInitialValueCodec.EntityId((string)roots[name]), template);
        }
        JObject unitEvidence;
        var units = S073NativeConfiguredUnits.BindCurrentAuthored(em, imported, repo, args[3], args[4],
            args[5], args[6], Path.Combine(repo, "Assets/Game/Configs/Skirmish/UnitRegistry.asset"), out unitEvidence);
        Need(units.Count == 3 && ((JArray)unitEvidence["rows"]).Count == 3,
            "All three current UnitRegistry configs must bind in the identity-only EntityManager fixture");
        for (int i = 0; i < 3; i++)
            Need(units.TryRead(i, out var prefab, out _, out var displayedMaterials, out var requestable, out _) &&
                 prefab != null && displayedMaterials >= 0 && requestable,
                "Current source unit read model must retain authored order/cost/requestability at " + i);

        var result = new JObject { ["kind"] = "S073CurrentSourceCatalogControl", ["status"] = "Passed",
            ["scope"] = "CurrentAssetReadbackAndIdentityJoin_NoMissionWorldOrReadiness",
            ["mission_world_allocated"] = false, ["identity_only_entity_manager_fixture"] = true,
            ["building_count"] = buildings.Count, ["unit_count"] = units.Count,
            ["changed_definition_rejected"] = mismatchRejected,
            ["guid_index_controls"] = new JObject { ["duplicate_guid_rejected"] = true,
                ["added_meta_rejected"] = true, ["removed_meta_rejected"] = true, ["changed_meta_rejected"] = true,
                ["wrong_root_rejected"] = true, ["bom_metadata_resolved"] = true },
            ["buildings"] = buildingEvidence, ["units"] = unitEvidence };
        File.WriteAllText(args[7], result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        Console.WriteLine("[S073CurrentSourceCatalogControl] result=Passed buildings=6 units=3 changed_definition_rejected=1 guid_index_controls=6 mission_world=0");
    }
}
