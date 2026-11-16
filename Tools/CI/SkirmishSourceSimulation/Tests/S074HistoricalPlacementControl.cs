using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Historical pregame Campaign masks and the older S002 water observation are
// diagnostic inputs only. They are not S074 live scene or source-fit evidence.
internal static class S074HistoricalPlacementControl
{
    internal static void Run(EntityManager em, Entity gridEntity, Entity boundary, Entity session,
        GeneratedBuildingDefinitions definitions, SkirmishResolvedSetup setup,
        SkirmishScenarioDefinitionConfig definition, JObject metadataRequest, string directory, string sourceRoot)
    {
        var outcome = new JObject { ["kind"] = "S074HistoricalPlacementControl", ["status"] = "Started",
            ["scope"] = "Original expanded starting-grant owners using retained historical pregame Campaign masks and S002 water observation; no current live occupancy, active-water equivalence, native parity or readiness.",
            ["historical_inputs"] = true, ["source_fit_certified"] = false, ["mission_ready"] = false };
        try
        {
            var map = definition.CampaignMap;
            if (setup.CatalogId != "S074" || setup.SetupHash != 977354998u || map.OperationMapId != "opmap.skirmish.industrial_basin" ||
                map.SourceBinding.SourceOperationMapId != "opmap.skirmish.desert_base_01" ||
                map.SourceBinding.SourceIdentityHash != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" ||
                map.SourceBinding.SourceContentHash != "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c" ||
                map.SurfaceMetadata.ContentHash != "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d")
                throw new InvalidDataException("Exact selected source binding required");
            string input = Path.Combine(sourceRoot, "Logs/SkirmishProduction/s073-source-inputs-20261101-r1");
            string physical = Path.Combine(input, "campaign-physical-source.json"), water = Path.Combine(input, "campaign-water-capture.json"),
                startup = Path.Combine(input, "campaign-startup-observation.json"), preservation = Path.Combine(sourceRoot, "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json");
            const string physicalSha = "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8";
            var domain = SourcePlacementDomain.LoadS002HistoricalInputOnly(physical, physicalSha, water,
                "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5", startup,
                "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03", sourceRoot, preservation,
                "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297");
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            if (grid.Width != 2048 || grid.Height != 1024 || grid.CellSize != 1 || !domain.WaterDomainKnown || domain.WaterFootprintCount != 194)
                throw new InvalidDataException("Historical source frame or water capture changed");
            var masks = domain.Build(grid.Width, grid.Height, grid.CellSize, grid.Origin.x, grid.Origin.y, grid.Origin.z);
            var roads = domain.CopyOriginalOwnerRoadMask();
            var waterMask = domain.BuildWaterMask(true, grid.Width, grid.Height, grid.CellSize, grid.Origin.x, grid.Origin.y, grid.Origin.z);
            var capture = JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(physical, physicalSha, 200 * 1024 * 1024)));
            var rows = capture["headlessPlacementInputs"];
            byte[] GridBytes(string key, string expected)
            {
                var row = rows[key]; var bytes = Convert.FromBase64String((string)row["base64"]);
                if ((string)row["encoding"] != "uint8-row-major-xfast" || bytes.Length != grid.Width * grid.Height ||
                    (string)row["sha256"] != expected || AutonomousWorld.Hash(bytes) != expected || bytes.Any(x => x > 1))
                    throw new InvalidDataException("Historical actual grid bytes changed: " + key);
                return bytes;
            }
            var walkable = GridBytes("gridWalkable", "6d75695d93deb1bf5805617cfba166466cf60dc0a99a8014fa58893f358f33b8");
            var gridRoad = GridBytes("gridRoad", "5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee");
            var walkBuffer = em.AddBuffer<GridWalkable>(gridEntity); var roadBuffer = em.AddBuffer<GridRoad>(gridEntity);
            for (int i = 0; i < walkable.Length; i++) { walkBuffer.Add(new GridWalkable { Value = walkable[i] }); roadBuffer.Add(new GridRoad { Value = gridRoad[i] }); }
            GeneratedFreshGridStorage.Initialize(em, gridEntity);
            var navigation = SourcePlacementDomain.ReadS002HistoricalNavigationRows(physical, physicalSha);
            int imported = GeneratedAuthoredNavigationImporter.Materialize(em, GeneratedAuthoredNavigationImporter.DecodeRows(navigation, Path.GetDirectoryName(physical)));
            var blockerState = new SystemState { EntityManager = em, Enabled = true }; var blockerOwner = new StaticGridBlockerUpdateSystem();
            blockerState.Begin(); blockerOwner.OnCreate(ref blockerState); blockerState.End();
            blockerState.Begin();
            try { if (!blockerState.Enabled || !blockerState.RequiredQueriesPresent) throw new InvalidOperationException("Original blocker prerequisites missing"); blockerOwner.OnUpdate(ref blockerState); }
            finally { blockerState.End(); }
            string graphPath = Path.Combine(sourceRoot, "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json");
            var graph = JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(graphPath, "edc4081763a3252716679fc4e36dae2b8341aa6e9c8426fa552e143096d510cb", 16 * 1024 * 1024)));
            var assets = new SourceConfigMaterializer(graph);
            var surface = assets.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var surfaceBlob)) throw new InvalidDataException("Original current source surface decoder failed");
            var surfaceEntity = em.CreateEntity();
            em.AddComponentData(surfaceEntity, new MapSurfaceComponent { SurfaceBlob = surfaceBlob, GridOrigin = surfaceBlob.Value.GridOrigin,
                CellSize = surfaceBlob.Value.CellSize, Dimensions = surfaceBlob.Value.Dimensions, HasSurfaceData = 1 });
            using (var all = em.CreateEntityQuery(new EntityQueryDesc { Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities }))
                RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em, true, true, all.ToEntityArray(Allocator.Temp).ToArray(),
                    entity => em.HasComponentNamed(entity, "Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
            string configPath = Path.Combine(sourceRoot, "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset");
            string config = Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(configPath, "a8d7ec905936c889fd39142d223562cea40b85edf42405e71b08baa8332c07e6", 1024 * 1024));
            var scalar = Regex.Match(config, @"^  buildPlaneY: ([^\r\n]+)$", RegexOptions.Multiline);
            if (!scalar.Success || !float.TryParse(scalar.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float plane) || !float.IsFinite(plane))
                throw new InvalidDataException("Actual configured build plane required");
            var registry = new Dictionary<int, RuntimeBuildingEntity>();
            var pipeline = new GeneratedStartingBuildingPipeline(em, gridEntity, boundary, definitions.Definitions, true, registry,
                masks.AuthoredBuildings, roads, masks.Sidewalks, waterMask, plane,
                (x,z,w,h) => { var b = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
                    for (int y=z; y<z+h; y++) for (int xx=x; xx<x+w; xx++) if (b.IsSet(y*grid.Width+xx)) return true; return false; });
            var configured = JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.Descriptor(metadataRequest["building_request_metadata"], 4 * 1024 * 1024)));
            var order = new List<SourceUnitPrefab>(); var entries = new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry>();
            foreach (JObject row in (JArray)configured["rows"])
            {
                var d = definitions.Definitions.Single(x => x.Prefab.name == (string)row["key"]);
                order.Add(d.Prefab); entries.Add(d.Prefab, new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry(d.DisplayName, d.Description, d.Prefab,
                    (bool)row["can_request"], Math.Max(0, d.CreditsCost)));
            }
            pipeline.BindConfiguredRequestMetadata(order, entries);
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary); int frames = 0;
            for (; frames < 64; frames++) { pipeline.ProcessRequests(frames / 60f, true); if (requests.ToArray().All(x => x.Status != BuildingRuntimeSpawnRequest.Pending)) break; }
            bool ready = pipeline.SubmitOrBind(session, setup, out var reason);
            var resultRows = new JArray();
            foreach (var item in requests.ToArray()) resultRows.Add(new JObject { ["request_id"] = item.RequestId, ["index"] = item.EntryIndex,
                ["building"] = item.BuildingId.ToString(), ["faction"] = item.FactionId, ["status_code"] = item.Status,
                ["result_code"] = item.ResultCode, ["runtime_id"] = item.BuildingRuntimeId });
            outcome["status"] = "OwnerProcessingReturned"; outcome["navigation_rows"] = imported; outcome["frames"] = frames + 1;
            outcome["water_footprints_historical"] = domain.WaterFootprintCount; outcome["requests"] = resultRows;
            outcome["registry_count"] = registry.Count; outcome["starting_owner_ready"] = ready; outcome["starting_owner_reason"] = reason.ToString();
            surfaceBlob.Dispose();
        }
        catch (Exception error) { outcome["status"] = "Failed"; outcome["error"] = error.ToString(); File.WriteAllText(Path.Combine(directory, "s074-historical-placement.json"), outcome.ToString() + Environment.NewLine); throw; }
        File.WriteAllText(Path.Combine(directory, "s074-historical-placement.json"), outcome.ToString() + Environment.NewLine);
        Console.WriteLine("[S074HistoricalPlacement] status=OwnerProcessingReturned historical=1 sourceFit=0 readiness=0");
    }
}
