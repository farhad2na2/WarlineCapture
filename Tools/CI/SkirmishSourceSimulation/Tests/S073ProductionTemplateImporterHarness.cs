using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S073ProductionTemplateImporterHarness
{
    private static readonly string[] RequiredS073PrefabKeys =
    {
        "Unit_Chr_Soldier_Male_02_Alt_04",
        "Unit_Veh_APC_Fast",
        "Unit_Veh_APC_Heavy",
        "Unit_Veh_APC_Slow",
        "Unit_Veh_Light_Armored_Car",
        "Unit_Veh_Missle_Launcher_Air",
        "Unit_Veh_Missle_Launcher_Ground",
        "Unit_Veh_Radar_Tank",
        "Unit_Veh_Tank_USA",
        "Unit_Veh_Truck_Canopy",
        "Unit_Veh_Truck_Tanker",
        "Unit_Veh_Truck_Tray"
    };

    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static string Sha256(string path)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    private static void RequireRejected(JObject source, string binaryDirectory)
    {
        var malformed = (JObject)source.DeepClone();
        var unsupported = (JArray)malformed["unsupported_values"];
        var row = (JObject)unsupported[0].DeepClone();
        string[] path = ((string)row["path"]).Split('/');
        path[2] = "GameplayFieldMustNotBeOmitted";
        row["path"] = string.Join("/", path);
        unsupported.Add(row);
        try
        {
            GeneratedReferenceTemplateImporter.Preflight(malformed, binaryDirectory, RequiredS073PrefabKeys);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException("Unknown unsupported field was accepted as presentation-only");
    }

    public static int Main(string[] args)
    {
        Need(args.Length == 2, "Expected captured native template path and SHA256");
        string path = Path.GetFullPath(args[0]);
        Need(File.Exists(path) && Sha256(path) == args[1], "Exact immutable focused capture required");
        JObject source = JObject.Parse(File.ReadAllText(path));
        Need((string)source["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
             (string)source["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState" &&
             (string)source["template_namespace_id"] == "2ce53f22f46b472b9c9cafb424831c54" &&
             (bool?)source["replay_ready"] == false && (bool?)source["full_mission_initialized"] == false &&
             (bool?)source["native_visual_readiness"] == false,
             "Captured source-only scope and non-readiness flags changed");
        var roots = source["opening_prefabs"] as JObject;
        Need(roots != null && roots.Count == 16 && RequiredS073PrefabKeys.All(key => roots[key] != null),
             "Captured namespace must contain every source-derived S073 startup/production prefab");
        Need(RequiredS073PrefabKeys.Length == 12 &&
             RequiredS073PrefabKeys.Distinct(StringComparer.Ordinal).Count() == RequiredS073PrefabKeys.Length,
             "Exact unique S073 source recipe key set required");

        JObject preflight = GeneratedReferenceTemplateImporter.Preflight(
            source, Path.GetDirectoryName(path), RequiredS073PrefabKeys);
        Need((int?)preflight["roots"] == 16 && (int?)preflight["entities"] == 546 &&
             (int?)preflight["omitted_presentation_components"] == 20 &&
             (int?)preflight["unsupported_unconsumed_values"] == 29 &&
             (bool?)preflight["world_allocated"] == false &&
             (int?)preflight["entity_identities_allocated"] == 0,
             "Actual source template preflight domain changed");

        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 1;
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        var imported = GeneratedReferenceTemplateImporter.Import(
            em, source, Path.GetDirectoryName(path), RequiredS073PrefabKeys);
        Need(imported.Prefabs.Count == 16 && imported.ImportedEntities == 546 &&
             imported.OmittedPresentationComponents == 20 && next == 547,
             "Actual selected source template graph did not import completely");
        foreach (string key in RequiredS073PrefabKeys)
            Need(imported.Prefabs.ContainsKey(key) &&
                 em.GetComponentData<UnitSourcePrefabKey>(imported.Prefabs[key]).Value.ToString() == key,
                 "Imported source prefab identity changed: " + key);
        Entity air = imported.Prefabs["Unit_Veh_Missle_Launcher_Air"];
        Entity ground = imported.Prefabs["Unit_Veh_Missle_Launcher_Ground"];
        Need(em.HasComponent<AirMissileLauncherComponent>(air) &&
             !em.HasComponent<AirMissileLauncherVfxReferenceComponent>(air) &&
             em.HasComponent<GroundMissileLauncherComponent>(ground) &&
             !em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(ground),
             "Only actual missile VFX references may be omitted; gameplay launcher components must remain");
        RequireRejected(source, Path.GetDirectoryName(path));

        Console.WriteLine("[S073ProductionTemplateImporterHarness] result=Passed roots=16 requiredS073RecipeRoots=12 entities=546 omittedPresentationComponents=20 unsupportedPresentationReferences=29 unknownFieldRejected=1 missionWorld=0 fullMission=0 nativeReadiness=0");
        return 0;
    }
}
