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
        Need(args.Length == 11, "Expected captured templates/hash, legacy config/hash, building definitions/hash, source root, registry evidence/hash, and supplemental capture/hash");
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

        string registryEvidencePath = Path.GetFullPath(args[7]);
        Need(Sha256(registryEvidencePath) == args[8], "Exact source-verified S073 registry evidence required");
        string supplementalPath = Path.GetFullPath(args[9]);
        Need(Sha256(supplementalPath) == args[10], "Exact existing supplemental prefab capture required");
        JObject supplemental = JObject.Parse(File.ReadAllText(supplementalPath));
        JObject supplementalRoots = supplemental["opening_prefabs"] as JObject;
        const string transportKey = "Unit_Veh_Helicopter_Transport";
        Need((string)supplemental["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
             (string)supplemental["authority"] == "ActualSourceAssetsAndNativeBakers_NotMissionState" &&
             (bool?)supplemental["replay_ready"] == false &&
             (bool?)supplemental["full_mission_initialized"] == false &&
             supplementalRoots != null && supplementalRoots[transportKey] != null &&
             !supplementalRoots.Properties().Any(root => roots[root.Name] != null),
             "Disjoint existing source-only transport capture required");
        string[] supplementalKeys = supplementalRoots.Properties().Select(root => root.Name).ToArray();
        var supplementalImport = GeneratedReferenceTemplateImporter.Import(
            em, supplemental, Path.GetDirectoryName(supplementalPath), supplementalKeys);
        Need(supplementalImport.Prefabs.Count == supplementalRoots.Count &&
             supplementalImport.Prefabs.ContainsKey(transportKey), "Original transport prefab capture failed to import");
        foreach (var pair in supplementalImport.Prefabs)
            Need(imported.Prefabs.TryAdd(pair.Key, pair.Value), "Supplemental prefab root overlaps: " + pair.Key);

        string legacyConfigPath = Path.GetFullPath(args[2]);
        Need(Sha256(legacyConfigPath) == args[3], "Exact external S073 legacy initialization config required");
        JObject legacyConfig = JObject.Parse(File.ReadAllText(legacyConfigPath));
        string definitionPath = Path.GetFullPath(args[4]);
        Need(Sha256(definitionPath) == args[5], "Exact original-source S073 building definitions required");
        var definitions = GeneratedBuildingDefinitions.LoadS073Legacy(
            definitionPath, args[5], imported.Prefabs, legacyConfig, Path.GetFullPath(args[6]));
        Need(definitions.Definitions.Count == 6 && definitions.Definitions.Select(d => d.Prefab.name)
                 .Distinct(StringComparer.Ordinal).Count() == 6,
             "Exact six original S073 building definitions required");
        var barrack = definitions.Definitions.Single(d => d.Prefab.name == "Building_Barrack");
        Need(barrack.FootprintCells.x == 28 && barrack.FootprintCells.y == 15 && barrack.ProductionSlots.Count == 1 &&
             barrack.ProductionSlots[0].SpawnUnitPrefab.name == "Unit_Chr_Soldier_Male_02_Alt_04" &&
             barrack.ProductionSlots[0].Quantity == 4 && barrack.SecondarySpawnUnitPrefab == null &&
             barrack.TertiarySpawnUnitPrefab == null && barrack.QuaternarySpawnUnitPrefab == null,
             "S073 original Barrack recipe/domain changed");
        var fuel = definitions.Definitions.Single(d => d.Prefab.name == "Building_Fuel_Bladder");
        Need(fuel.FootprintCells.x == 14 && fuel.FootprintCells.y == 10 && fuel.ProductionSlots.Count == 11 &&
             fuel.ProductionSlots.All(slot => slot.Quantity == 1 && imported.Prefabs.ContainsKey(slot.SpawnUnitPrefab.name)),
             "S073 original Fuel Bladder recipe/domain changed");
        foreach (var definition in definitions.Definitions.Where(d => d.Prefab.name != "Building_Barrack" && d.Prefab.name != "Building_Fuel_Bladder"))
            Need(definition.ProductionSlots.Count == 0, "S073 non-producer acquired runtime-overlay recipes");

        var units = GeneratedUnitProductionMetadata.LoadS073ConfiguredUnitRegistry(
            registryEvidencePath, args[8], Path.GetFullPath(args[6]), definitions.Assets,
            imported.Prefabs, definitions.AssetIdentities);
        Need(units.Ordered.Count == 3 && units.FactoryDomainQualified &&
             units.Ordered.Select(row => row.Prefab.name).SequenceEqual(new[]
             {
                 "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker"
             }), "Actual ordered S073 source registry must materialize exactly three configured units");
        Entity boundary = em.CreateEntity();
        DynamicBuffer<BuildingConfiguredUnitReadModel> readModels = em.AddBuffer<BuildingConfiguredUnitReadModel>(boundary);
        units.PublishConfiguredUnitReadModels(readModels);
        Need(readModels.Length == 3 &&
             readModels[0].UnitId.ToString() == "unit_chr_soldier_male_02_alt_04" &&
             readModels[1].UnitId.ToString() == "unit_veh_truck_tray" && readModels[1].IsVehicle == 1 &&
             readModels[2].UnitId.ToString() == "unit_veh_truck_tanker" && readModels[2].IsVehicle == 1 &&
             readModels[0].CanRequest == 1 && readModels[1].CanRequest == 1 && readModels[2].CanRequest == 1 &&
             readModels[0].Price > 0 && readModels[1].Price > 0 && readModels[2].Price > 0 &&
             readModels[0].CreditsCost > 0 && readModels[1].CreditsCost > 0 && readModels[2].CreditsCost > 0,
             "Original S073 configured-unit runtime projection changed or incomplete: " +
             string.Join(";", Enumerable.Range(0, readModels.Length).Select(i =>
                 readModels[i].UnitId + "/" + readModels[i].DisplayName + "/" + readModels[i].Price + "/" +
                 readModels[i].CreditsCost + "/" + readModels[i].CanRequest + "/" + readModels[i].IsVehicle)));

        Console.WriteLine("[S073ProductionTemplateImporterHarness] result=Passed roots=16 supplementalTransport=1 requiredS073RecipeRoots=12 sourceRegistryUnits=3 configuredUnitReadModels=3 entities=546 omittedPresentationComponents=20 unsupportedPresentationReferences=29 unknownFieldRejected=1 legacyDefinitions=6 barrackSlots=1 fuelBladderSlots=11 missionWorld=0 placedBuildings=0 fullMission=0 nativeReadiness=0");
        return 0;
    }
}
