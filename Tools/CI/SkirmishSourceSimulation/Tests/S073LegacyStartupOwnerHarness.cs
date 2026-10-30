using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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

    static void RequireSourcePrefabIdentityReadback(JObject source, JObject identity, JObject snapshot, string snapshotPath, string identityPath, string assetRepoRoot)
    {
        Need((int?)identity["schema_version"] == 1 &&
             (string)identity["kind"] == "S073LegacyPrefabSourceNameReadback" &&
             (string)identity["authority"] == "AssetDatabase exact GUID/local-fileID GameObject names; source data only" &&
             (string)identity["mission"] == "S073" &&
             identity["unity_world_allocated"]?.Value<bool>() == false &&
             identity["mission_world_allocated"]?.Value<bool>() == false &&
             identity["mission_initialized"]?.Value<bool>() == false &&
             identity["native_visual_readiness"]?.Value<bool>() == false,
             "Exact source-only S073 prefab identity readback required");
        Need((string)identity["prefab_capture_sha256"] == Sha256(snapshotPath),
             "S073 source-name readback must refer to the pinned prefab capture");
        Need((string)identity["source_readback_sha256"] == (string)source["source_artifacts"]?["config_readback_sha256"] &&
             ((string)identity["source_readback_sha256"])?.Length == 64 &&
             (string)identity["prefab_capture_sha256"] == (string)source["source_artifacts"]?["prefab_capture_sha256"] &&
             Sha256(identityPath) ==
                 (string)source["source_artifacts"]?["name_readback_sha256"],
             "S073 source config readback lineage mismatch");

        var expectedRoots = source["captured_unit_prefab_roots"] as JObject ??
            throw new InvalidDataException("Captured S073 prefab roots required");
        var keys = (source["initial_spawn"]?["Factions"] as JArray ?? throw new InvalidDataException("S073 factions required"))
            .OfType<JObject>().SelectMany(f => (f["Units"] as JArray ?? throw new InvalidDataException("S073 units required"))
                .OfType<JObject>().Select(u => (string)u["SourceKey"]))
            .Concat((source["initial_spawn"]?["UnitPrefabRegistryKeys"] as JArray ?? new JArray()).Values<string>())
            .Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        Need(keys.Length == 4 && expectedRoots.Properties().Select(p => p.Name).OrderBy(key => key, StringComparer.Ordinal)
            .SequenceEqual(keys), "Exact four S073 unit prefab source keys required");

        var rows = (identity["objects"] as JArray ?? throw new InvalidDataException("S073 source object rows required"))
            .OfType<JObject>().ToArray();
        string root = Path.GetFullPath(assetRepoRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (string key in keys)
        {
            var matches = rows.Where(row => (string)row["unit_source_key"] == key).ToArray();
            Need(matches.Length == 1, "Unique audited S073 source asset identity required for " + key);
            JObject row = matches[0];
            var sourcePins = identity["source_pins"] as JObject ?? throw new InvalidDataException("Pinned S073 identity inputs required");
            string assetPath = (string)row["asset_path"], metaPath = (string)row["meta_path"];
            Need(assetPath != null && metaPath != null && sourcePins[assetPath]?.Type == JTokenType.String &&
                 sourcePins[metaPath]?.Type == JTokenType.String &&
                 sourcePins[assetPath].Value<string>() == (string)row["asset_sha256"] &&
                 sourcePins[metaPath].Value<string>() == (string)row["meta_sha256"],
                 "S073 asset identity must be pinned to its prefab and .meta sources: " + key);
            foreach (string path in new[] { assetPath, metaPath })
            {
                string fullPath = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                Need(fullPath.StartsWith(root, StringComparison.Ordinal) && Sha256(fullPath) == (string)sourcePins[path],
                     "Current S073 prefab identity source pin changed: " + path);
            }
            Need((string)row["game_object_name"] == key &&
                 ((string)row["guid"])?.Length == 32 && row["file_id"]?.Type == JTokenType.Integer &&
                 ((string)row["asset_path"])?.EndsWith(".prefab", StringComparison.Ordinal) == true &&
                 (row["uses"] as JArray)?.OfType<JObject>().Any(use =>
                     ((string)use["source_path"] == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset" &&
                      ((string)use["field_path"])?.Contains(".units[") == true && (long?)use["file_id"] == (long?)row["file_id"]) ||
                     ((string)use["source_path"] == "Assets/Game/Configs/Skirmish/UnitRegistry.asset" &&
                      ((string)use["field_path"])?.Contains(".unitSpawnPrefabs[") == true && (long?)use["file_id"] == (long?)row["file_id"])) == true,
                 "S073 unit source reference must resolve to the exact authored prefab identity: " + key);
            Need((string)expectedRoots[key] == (string)snapshot["opening_prefabs"]?[key],
                 "S073 authored source key must resolve to the pinned converted prefab root: " + key);
        }
    }

    static int ExerciseOriginalUnitApplyOwner(EntityManager em, IDictionary<string, Entity> prefabs)
    {
        var owner = new InitialUnitSpawnApplySystem();
        var expected = new Dictionary<int, string>();
        using (var ecb = new EntityCommandBuffer(Allocator.Temp))
        {
            int ordinal = 0;
            foreach (var pair in prefabs.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                owner.InstantiateAndConfigureSpawnedUnit(
                    em, ecb, pair.Value, true, 1,
                    new int2(ordinal, 0), new float3(ordinal, 0, ordinal));
                expected.Add(ordinal++, pair.Key);
            }
            ecb.Playback(em);
        }

        using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(),
                   ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                   ComponentType.ReadOnly<UnitMoveVisualComponent>(), ComponentType.ReadOnly<UnitRespawnPrefab>()))
        {
            var matched = new HashSet<int>();
            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var cell = em.GetComponentData<UnitGrid>(entity).Cell;
                    if (cell.y != 0 || !expected.ContainsKey(cell.x)) continue;
                    Need(matched.Add(cell.x), "Original initial-unit apply owner duplicated a synthetic test cell");
                    Need(em.GetComponentData<Faction>(entity).Id == 1 &&
                         em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() == expected[cell.x],
                         "Original initial-unit apply owner changed faction/source identity");
                    Need(em.GetComponentData<UnitMoveVisualComponent>(entity).IsMoving == 0,
                         "Original initial-unit apply owner changed initial move-visual state");
                    Need(em.GetComponentData<UnitRespawnPrefab>(entity).Prefab == Entity.Null,
                         "Original initial-unit apply owner changed initial respawn prefab state");
                }
            }
            Need(matched.Count == expected.Count, "Original initial-unit apply owner did not materialize every test template");
        }
        return expected.Count;
    }

    static void Main(string[] args)
    {
        Need(args.Length == 5, "prefab capture, source identity readback, external S073 config, source repository, and current prefab asset repository required");
        var snapshot = JObject.Parse(File.ReadAllText(args[0]));
        var identity = JObject.Parse(File.ReadAllText(args[1]));
        var source = JObject.Parse(File.ReadAllText(args[2]));
        Need((string)source["kind"] == "S073LegacyInitialSpawnExternalData" && (string)source["mission"] == "S073",
            "Exact external S073 legacy config required");
        Need(Sha256(args[1]) == (string)source["source_artifacts"]?["name_readback_sha256"],
             "S073 external config must pin its source-identity readback");
        RequireCurrentLegacyAssets(source, args[3]);
        RequireSourcePrefabIdentityReadback(source, identity, snapshot, args[0], args[1], args[4]);
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
        int materializedPrefabControls = ExerciseOriginalUnitApplyOwner(em, imported.Prefabs);
        Need(materializedPrefabControls == 4, "Original unit apply owner source-root control count changed");

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
        Console.WriteLine("[S073LegacyStartupOwnerHarness] result=Passed actualStartupOwner=1 originalApplyOwnerPrefabControls=" + materializedPrefabControls + " syntheticTestPosition=1 importedPrefabRoots=4 importedEntities=140 sourceRows=10 acceptedByOriginalGate=10 factions=2 unitsEach=12 world=0 placedBuildings=0 fullMission=0 readiness=0");
    }
}
