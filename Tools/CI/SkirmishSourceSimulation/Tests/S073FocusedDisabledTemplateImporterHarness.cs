using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S073FocusedDisabledTemplateImporterHarness
{
    private static readonly string[] RequiredRoots =
    {
        "Unit_Veh_Drone",
        "Unit_Veh_Helicopter_Attack",
        "Unit_Veh_Helicopter_Attack_Small",
        "Unit_Veh_Helicopter_Transport",
        "Unit_Veh_Jet_01",
        "Unit_Veh_Jet_02",
        "Unit_Veh_Plane_Transport"
    };

    private static void Need(bool value, string reason)
    {
        if (!value) throw new InvalidOperationException(reason);
    }

    private static string Sha256(string path)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    public static int Main(string[] args)
    {
        Need(args.Length == 2, "Expected exact focused prefab capture and SHA256");
        string path = Path.GetFullPath(args[0]);
        Need(File.Exists(path) && Sha256(path) == args[1], "Exact immutable focused capture required");
        JObject source = JObject.Parse(File.ReadAllText(path));
        JObject roots = source["opening_prefabs"] as JObject;
        Need(roots != null && roots.Count == RequiredRoots.Length &&
             RequiredRoots.All(key => roots[key] != null), "Exact seven captured vehicle roots required");
        Need((bool?)source["replay_ready"] == false &&
             (bool?)source["full_mission_initialized"] == false &&
             (bool?)source["native_visual_readiness"] == false,
             "Source-only capture must retain non-readiness flags");

        string binaryDirectory = Path.GetDirectoryName(path);
        JObject preflight = GeneratedReferenceTemplateImporter.Preflight(source, binaryDirectory, RequiredRoots);
        Need((int?)preflight["roots"] == 7 && (int?)preflight["entities"] == 315 &&
             (bool?)preflight["world_allocated"] == false &&
             (int?)preflight["entity_identities_allocated"] == 0,
             "Complete source-template preflight changed");

        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int nextIdentity = 1;
        manager.AllocateLogicalIdentity = () => new Entity { Index = nextIdentity++, Version = 1 };
        var imported = GeneratedReferenceTemplateImporter.Import(manager, source, binaryDirectory, RequiredRoots);
        Need(imported.Prefabs.Count == 7 && imported.ImportedEntities == 315 && nextIdentity == 316,
             "Complete actual source-template import failed");

        int expectedDisabled = 0;
        foreach (JObject entity in (JArray)source["entities"])
        {
            JObject components = (JObject)entity["components"];
            bool sourceHasDisabled = (bool?)components["Unity.Entities.Disabled"]?["present"] == true;
            bool sourceEnabled = (bool)entity["entity_enabled"];
            bool shouldBeDisabled = sourceHasDisabled || !sourceEnabled;
            if (shouldBeDisabled) expectedDisabled++;

            Entity sourceId = ExactInitialValueCodec.EntityId((string)entity["id"]);
            Entity logicalId = imported.SourceToLogical[sourceId];
            Need(manager.HasComponent<Disabled>(logicalId) == shouldBeDisabled,
                 "Disabled tag mismatch for source entity " + sourceId);
        }
        Need(expectedDisabled == 6 && Enumerable.Range(1, 315).Count(index =>
                 manager.HasComponent<Disabled>(new Entity { Index = index, Version = 1 })) == 6,
             "All six source Disabled tags must import exactly once");
        foreach (string key in RequiredRoots)
            Need(manager.GetComponentData<UnitSourcePrefabKey>(imported.Prefabs[key]).Value.ToString() == key,
                 "Actual source prefab identity changed: " + key);

        Console.WriteLine("[S073FocusedDisabledTemplateImporterHarness] result=Passed roots=7 entities=315 sourceDisabledTags=6 duplicateTags=0 world=0 mission=0 readiness=0");
        return 0;
    }
}
