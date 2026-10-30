using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S073LegacyStartupOwnerHarness
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static string Sha256(string path)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static void RequireCurrentLegacyAssets(JObject source, string repoRoot)
    {
        var pins = source["source_artifacts"]?["configuration_asset_sha256"] as JObject ??
            throw new InvalidDataException("Current S073 legacy asset pins required");
        string root = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var pin in pins.Properties())
        {
            if (Path.IsPathRooted(pin.Name) || pin.Name.Split('/').Any(part => part == ".." || part == ""))
                throw new InvalidDataException("S073 config asset path must remain repository-relative");
            string path = Path.GetFullPath(Path.Combine(root, pin.Name.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.Ordinal) || Sha256(path) != (string)pin.Value)
                throw new InvalidDataException("S073 original legacy configuration changed: " + pin.Name);
        }
    }

    static void Main(string[] args)
    {
        Need(args.Length == 3, "prefab capture, external S073 config, and current source repository required");
        var snapshot = JObject.Parse(File.ReadAllText(args[0]));
        var source = JObject.Parse(File.ReadAllText(args[1]));
        Need((string)source["kind"] == "S073LegacyInitialSpawnExternalData" && (string)source["mission"] == "S073",
            "Exact external S073 legacy config required");
        RequireCurrentLegacyAssets(source, args[2]);
        var data = source["initial_spawn"].ToObject<LegacyInitialSpawnData>();
        var expectedRoots = source["captured_unit_prefab_roots"] as JObject ?? throw new InvalidDataException("Captured S073 prefab roots required");
        Need(JToken.DeepEquals(expectedRoots, snapshot["opening_prefabs"]), "Captured S073 prefab root namespace changed");

        var em = new EntityManager();
        int nextIdentity = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextIdentity++, Version = 1 };
        var keys = data.UnitPrefabRegistryKeys.Concat(data.Factions.SelectMany(f => f.Units).Select(u => u.SourceKey))
            .Distinct(StringComparer.Ordinal).ToArray();
        var imported = GeneratedReferenceTemplateImporter.Import(em, snapshot, Path.GetDirectoryName(args[0]), keys);
        Need(imported.Prefabs.Count == 4 && imported.ImportedEntities == 140 && imported.ImportedComponents == 862 &&
             imported.OmittedPresentationComponents == 4, "Actual pinned S073 prefab closure import changed");

        var initialized = new CustomGameStartupSystemHelper(em).InitializeFromLegacyData(data, imported.Prefabs);
        Need(initialized.Initialized && initialized.FactionCount == 2 && initialized.UnitRosterCount == 3 &&
             initialized.InitialUnitEntryCount == 10 && initialized.InitialBuildingEntryCount == 10 &&
             initialized.UnitRegistryEntryCount == 3 && initialized.VisualEntryCount == 3 &&
             initialized.MissingVisualReferenceCount == 2, "Original legacy startup owner result changed");

        Entity plan;
        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                                                ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
        {
            Need(query.CalculateEntityCount() == 1, "Unique original startup config required");
            plan = query.GetSingletonEntity();
            var config = em.GetComponentData<InitialUnitsSpawnConfig>(plan);
            Need(config.BlockerCount == 0 && config.SpawnRadiusCells == 6 && config.RandomSeed == 1 &&
                 config.InitialMaterials == 220 && config.MaterialsCapacity == 600 && config.InitialAiMaterials == 220 &&
                 config.AiMaterialsCapacity == 600 && config.InitialOil == 0 && config.InitialFuel == 160 &&
                 config.CreateFactionBases == 0, "S073 original resource/budget values changed");
            var unitRows = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(plan);
            int firstFactionUnits = 0, secondFactionUnits = 0;
            for (int i = 0; i < unitRows.Length; i++)
            {
                if (unitRows[i].FactionId == 1) firstFactionUnits += unitRows[i].Count;
                if (unitRows[i].FactionId == 2) secondFactionUnits += unitRows[i].Count;
            }
            Need(firstFactionUnits == 12 && secondFactionUnits == 12, "S073 actual per-faction unit grant changed");
            Need(em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan).Length == 2 &&
                 em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(plan).Length == 10 &&
                 em.GetBuffer<UnitPrefabRegistryEntry>(plan).Length == 3 &&
                 em.GetBuffer<CustomGameVisualRegistryEntry>(plan).Length == 3,
                 "Original S073 startup buffers changed");
            Need(!em.HasComponent<InitialUnitsSpawnInitialized>(plan), "Do not invent initial spawn completion");
        }

        var match = em.CreateEntity();
        em.AddComponentData(match, new SkirmishMatchState());
        var boundary = em.CreateEntity();
        var requests = em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
        int queued = SourceS073LegacySpawnRequestAdapter.Queue(em, plan, boundary, true);
        Need(queued == 10 && requests.Length == 10, "Shared original legacy enqueue owner did not create all ten requests");

        int row = 0;
        foreach (var faction in data.Factions)
        foreach (var building in faction.Buildings)
        {
            var request = requests[row];
            int expectedX = faction.SpawnX + building.OriginOffsetX;
            int expectedY = faction.SpawnY + building.OriginOffsetY;
            Need(request.RequestId == row + 1 && request.RequestKind == BuildingRuntimeSpawnRequest.KindBuilding &&
                 request.Status == BuildingRuntimeSpawnRequest.Pending && request.HasOwnerFaction == 1 &&
                 request.RequirePreferredOrigin == 1 && request.FactionId == faction.FactionId &&
                 request.PlanEntity == plan && request.PreferredOrigin.x == expectedX && request.PreferredOrigin.y == expectedY &&
                 request.BuildingId.ToString() == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(building.LookupKey),
                 "Shared request owner changed source row " + row);
            Need(GeneratedRuntimeSpawnProcessor.IsLegacyInitialRequest(em, request),
                 "Shared request rejected by original runtime spawn gate at row " + row);
            row++;
        }
        Need(row == 10, "S073 request result accounting changed");
        Console.WriteLine("[S073LegacyStartupOwnerHarness] result=Passed actualStartupOwner=1 importedPrefabRoots=4 importedEntities=140 sourceRows=10 acceptedByOriginalGate=10 factions=2 unitsEach=12 world=0 placedBuildings=0 fullMission=0 readiness=0");
    }
}
