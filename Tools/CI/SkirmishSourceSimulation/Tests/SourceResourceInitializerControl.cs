using System;
using System.IO;
using System.Linq;
using Game.Runtime;
using Game.Components;
using Unity.Entities;
using Newtonsoft.Json.Linq;

internal static class SourceResourceInitializerControl
{
    public static int Main(string[] args)
    {
        if (args.Length != 6) throw new ArgumentException("Run envelope, initialization, hash, root, output and mode required");
        var run = JObject.Parse(File.ReadAllText(args[0]));
        run["output_directory"] = args[4];
        if (!Directory.Exists(args[4])) throw new InvalidDataException("Existing isolated output required");
        using var world = new AutonomousWorld();
        world.PrepareFreshInitialRoads(run);
        try
        {
            world.Initialize(args[1], args[2], 1f / 60f, args[3], args[3]);
            throw new InvalidOperationException("Negative metadata-origin control unexpectedly initialized");
        }
        catch (Exception error)
        {
            bool badDescriptor = args[5] == "bad-descriptor" && world.InitializationStage == "ReadingAuthoredConfig" &&
                error.Message.Contains("Exact pinned source resource bindings required");
            bool metadataBoundary = args[5] == "valid-descriptor" && world.InitializationStage == "CompiledAuthoredSourceWithResources" &&
                error.Message.Contains("Origin or derived target requires native metadata authority");
            bool expandedBoundary = args[5] == "expanded-good" && world.InitializationStage == "ExpandedPhysicalInputsSelected" &&
                error.Message.Contains("Origin or derived target requires native metadata authority");
            bool expandedRejected = (args[5] == "expanded-missing" || args[5] == "expanded-promoted" || args[5] == "canonical-expanded-rejected") &&
                world.InitializationStage == "ReadingAuthoredConfig" && error.Message.Contains("Actual authored S002 BaseAssault required");
            bool badReplay = args[5] == "expanded-bad-replay" && world.InitializationStage == "ResolvedAuthoredSourceResources" &&
                error.Message.Contains("Source input hash changed");
            bool wrongMap = args[5] == "expanded-wrong-map" && world.InitializationStage == "CompiledAuthoredSourceWithResources" &&
                error.Message.Contains("S004 selected map descriptor differs from the actual bound source object");
            bool wrongPhysical = args[5] == "expanded-wrong-physical" && world.InitializationStage == "ExpandedSessionMapSelected" &&
                error.Message.Contains("Exact map physical input descriptor required");
            bool sourceGrid = args[5] == "expanded-grid" && world.InitializationStage == "ExpandedSourceRoadsInitialized" &&
                error is GeneratedInitializationBlocked && error.Message.Contains("S004 source grid, surface, fresh storage and initial road owner executed");
            if (sourceGrid)
            {
                var grid = world.Entities.GetComponentData<GridConfig>(world.GridEntity);
                var walkable = world.Entities.GetBuffer<GridWalkable>(world.GridEntity);
                int blocked = 0; for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blocked++;
                if (grid.Width != 2048 || grid.Height != 1024 || walkable.Length != 2097152 || blocked != 22840 ||
                    !world.Entities.HasComponent<MapSurfaceComponent>(world.SurfaceEntity) ||
                    !world.Entities.HasComponent<DynamicBlockerComponent>(world.GridEntity) ||
                    !world.Entities.HasComponent<DynamicOccupancyComponent>(world.GridEntity) ||
                    (bool?)world.InitialRoadReceipt["actual_original_sync_executed"] != true ||
                    (int?)world.InitialRoadReceipt["cells"] != walkable.Length)
                    throw new InvalidDataException("S004 original grid owner output differs from authored inputs");
                world.CompleteInitialRoadInputs();
                Console.WriteLine("[SourceS004GridOwner] result=Passed cells=2097152 blocked=22840 freshStorage=1 originalRoadSync=1 sourceOnly=1 gameplay=0 readiness=0");
            }
            bool historical = args[5] == "expanded-history" && world.InitializationStage == "ExpandedHistoricalBlockersInitialized" &&
                error is GeneratedInitializationBlocked && error.Message.Contains("S004 historical source blockers and original blocker owner executed");
            bool wrongHistory = args[5] == "expanded-history-drift" && world.InitializationStage == "ExpandedSourceGridConstructed" &&
                error.Message.Contains("Historical source drift must be declared exactly");
            bool wrongFactory = args[5] == "expanded-factory-drift" && world.InitializationStage == "ExpandedHistoricalBlockersInitialized" &&
                error.Message.Contains("Historical factory source drift must be declared exactly");
            bool badMapReference = args[5] == "expanded-map-tamper" && world.InitializationStage == "ExpandedPhysicalInputsSelected" &&
                error.Message.Contains("Source input hash changed");
            bool badMapMode = args[5] == "expanded-map-invalid" && world.InitializationStage == "ExpandedPhysicalInputsSelected" &&
                error.Message.Contains("Exact S004 source-only metadata diagnostic required");
            if (wrongFactory && (world.Boundary != Entity.Null || world.Entities.HasBuffer<SkirmishStartingBuildingRequest>(world.Session)))
                throw new InvalidDataException("Rejected historical factory must not submit starting grants");
            bool historicalGrants = args[5] == "expanded-grants" && world.InitializationStage == "ExpandedHistoricalStartingGrantsSubmitted" &&
                error is GeneratedInitializationBlocked && error.Message.Contains("S004 historical source definitions published and original starting grants submitted");
            bool historicalPlacement = (args[5] == "expanded-placement" || args[5] == "expanded-mask-placement") && world.InitializationStage == "ExpandedHistoricalPlacementProcessed" &&
                error is GeneratedInitializationBlocked && error.Message.Contains("S004 original starting placement owner processed historical diagnostic inputs");
            bool invalidPlacement = args[5] == "expanded-placement-invalid" && world.InitializationStage == "ExpandedHistoricalStartingGrantsSubmitted" &&
                error.Message.Contains("Exact source-only placement assumptions required");
            bool sourceOpening = (args[5] == "expanded-opening" || args[5] == "expanded-outer") && world.InitializationStage == "ExpandedHistoricalOpeningObserved" &&
                error is GeneratedInitializationBlocked && error.Message.Contains("S004 original source opening ledger observed");
            if (invalidPlacement && (world.HistoricalPlacementObservation != null || world.Buildings.Count != 0))
                throw new InvalidDataException("Rejected placement assumptions must not place buildings");
            if (historical)
            {
                using var histories = world.Entities.CreateEntityQuery(typeof(StaticGridBlocker), typeof(StaticBlockerPrevBounds));
                if (world.AuthoredNavigationCount != 348 || histories.CalculateEntityCount() != 348 ||
                    (bool?)world.InitialRoadReceipt["actual_original_sync_executed"] != true)
                    throw new InvalidDataException("Original blocker owner did not process the complete historical capture");
                world.CompleteInitialRoadInputs();
                Console.WriteLine("[SourceS004HistoricalBlockers] result=Passed rows=348 originalBlockerOwner=1 historical=1 nativeParity=0 gameplay=0 readiness=0");
            }
            if (historicalGrants)
            {
                var requests = world.Entities.GetBuffer<BuildingRuntimeSpawnRequest>(world.Boundary);
                var tracked = world.Entities.GetBuffer<SkirmishStartingBuildingRequest>(world.Session);
                var configured = world.Entities.GetBuffer<BuildingConfiguredSpawnableReadModel>(world.Boundary);
                if (requests.Length != 20 || tracked.Length != 20 || configured.Length != 9 || world.Buildings.Count != 0)
                    throw new InvalidDataException("Original S004 historical grant submission differs");
                for (int i = 0; i < requests.Length; i++) if (requests[i].Status != BuildingRuntimeSpawnRequest.Pending)
                    throw new InvalidDataException("Unprocessed starting grant must remain pending");
                world.CompleteInitialRoadInputs();
                Console.WriteLine("[SourceS004HistoricalStartingGrants] result=Passed configured=9 submitted=20 placed=0 historical=1 nativeParity=0 readiness=0");
            }
            if (historicalPlacement)
            {
                var observed = world.HistoricalPlacementObservation;
                if ((observed?["requests"] as JArray)?.Count != 20 || (bool?)observed["mission_ready"] != false ||
                    (bool?)observed["original_blocker_owner_after_placement"] != true ||
                    (int?)observed["registry_count"] != world.Buildings.Count)
                    throw new InvalidDataException("Actual placement observation incomplete");
                int helipads = 0;
                foreach (var row in (JArray)observed["requests"])
                    if ((string)row["building"] == "Building_Helipad")
                    {
                        helipads++;
                        if (args[5] == "expanded-placement" && ((int?)row["status"] != BuildingRuntimeSpawnRequest.Succeeded ||
                            (bool?)row["registered"] != true))
                            throw new InvalidDataException("Baseline S004 helipad must be registered");
                        if ((int?)row["status"] == BuildingRuntimeSpawnRequest.Succeeded &&
                            ((bool?)row["registered"] != true || (bool?)row["has_path_blocker_entity"] != false))
                            throw new InvalidDataException("Placed S004 helipad must not create a path blocker");
                    }
                if (helipads != 2) throw new InvalidDataException("Both actual S004 helipad requests required");
                if (args[5] == "expanded-mask-placement" &&
                    ((string)observed["authored_building_mask"] != "HistoricalCapture" ||
                     (string)observed["sidewalk_mask"] != "HistoricalCapture" || (string)observed["water_mask"] != "IgnoredUnknown"))
                    throw new InvalidDataException("Actual historical masks and explicit unknown water required");
                if (args[5] == "expanded-placement")
                {
                var initialization = JObject.Parse(File.ReadAllText(args[1]));
                var setupDescriptor = (JObject)initialization["source_historical_factory"]["setup"];
                using var replay = SourceValidatedLayoutJson.OpenDescriptor(world.ActualDefinition.MapLayout, initialization["layout_json_replay"]);
                var sessionId = world.Entities.GetComponentData<SkirmishExpandedSessionComponent>(world.Session).SessionId;
                var force = world.Setup.Forces[0];
                using (var opening = SourceS004OpeningContext.Bind(world.Entities, world.Session, world.Setup,
                    (string)setupDescriptor["path"], (string)setupDescriptor["sha256"], world.SourceRepositoryRoot))
                {
                    SourceS004OpeningContext.Require(world.Entities, sessionId, force, 0, 0,
                        force.SupplyCost / force.Quantity, force.RuntimePrefabKey, world.Setup, null);
                    bool badMember = false;
                    try { SourceS004OpeningContext.Require(world.Entities, sessionId, force, 0, force.Quantity,
                        force.SupplyCost / force.Quantity, force.RuntimePrefabKey, world.Setup, null); }
                    catch (InvalidOperationException) { badMember = true; }
                    if (!badMember) throw new InvalidDataException("Out-of-range opening member must reject");
                }
                bool unboundRejected = false;
                try { SourceS004OpeningContext.Require(world.Entities, sessionId, force, 0, 0,
                    force.SupplyCost / force.Quantity, force.RuntimePrefabKey, world.Setup, null); }
                catch (InvalidOperationException) { unboundRejected = true; }
                if (!unboundRejected) throw new InvalidDataException("Disposed opening source scope must reject");
                }
                using (var stream = new FileStream(Path.Combine(args[4], "placement-observation.json"), FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write(observed.ToString());
                world.CompleteInitialRoadInputs();
                Console.WriteLine("[SourceS004HistoricalPlacement] result=Observed requests=20 registry=" + world.Buildings.Count + " historical=1 nativeParity=0 readiness=0");
            }
            if (sourceOpening)
            {
                var observed = world.HistoricalOpeningObservation;
                if (world.StartingPipeline == null || world.SourceDefinitions == null ||
                    (bool?)world.HistoricalPlacementObservation?["same_world_original_pipeline_retained"] != true)
                    throw new InvalidDataException("Completed original starting pipeline and definitions must remain in the same world");
                if ((bool?)observed?["mission_ready"] != false || (bool?)observed["native_parity"] != false ||
                    (string)observed["session_phase"] != world.Entities.GetComponentData<SkirmishExpandedSessionComponent>(world.Session).Phase.ToString())
                    throw new InvalidDataException("Actual original opening observation required");
                if ((bool?)observed["original_ledger_completed"] == true &&
                    ((int?)observed["observed_owned_units"] != world.Setup.Forces.Sum(f => f.Quantity) ||
                     (int?)observed["player_units"] != world.Setup.Forces.Where(f => f.FactionId == 1).Sum(f => f.Quantity) ||
                     (int?)observed["enemy_units"] != world.Setup.Forces.Where(f => f.FactionId == 2).Sum(f => f.Quantity) ||
                     (int?)observed["other_units"] != 0 || (bool?)observed["original_supply_initialized"] != true))
                    throw new InvalidDataException("Original opening unit or supply conservation differs from setup");
                if (args[5] == "expanded-outer" && ((bool?)observed["outer_spawn_owner_invoked"] != true ||
                    ((string)observed["session_phase"] == "Playing" &&
                     ((int?)observed["session_spawn_complete"] != 1 || (bool?)observed["native_presentation_deferred"] != true))))
                    throw new InvalidDataException("Actual original outer spawn transition or source view check differs");
                var openingInput = JObject.Parse(File.ReadAllText(args[1]));
                if (openingInput.Property("source_metadata_diagnostic") != null &&
                    ((int?)observed["map_publication"]?["physical_source_validated"] != 0 ||
                     (string)observed["map_publication"]?["scenario_id"] != "scenario.skirmish.desert_base_standard" ||
                     (string)observed["map_publication"]?["mission_id"] != "skirmish.base_assault" ||
                     (string)observed["map_publication"]?["operation_map_id"] != world.Setup.OperationMapId))
                    throw new InvalidDataException("Original S004 source map metadata identity differs");
                if (openingInput.Property("source_historical_placement_masks") != null &&
                    ((string)observed["placement_masks"]?["authored_buildings"] != "HistoricalCapture" ||
                     (string)observed["placement_masks"]?["sidewalks"] != "HistoricalCapture" ||
                     (string)observed["placement_masks"]?["water"] != "IgnoredUnknown"))
                    throw new InvalidDataException("Opening must retain actual historical mask and water scope");
                using (var stream = new FileStream(Path.Combine(args[4], "opening-placement-observation.json"), FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write(world.HistoricalPlacementObservation.ToString());
                using (var stream = new FileStream(Path.Combine(args[4], "opening-observation.json"), FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write(observed.ToString());
                world.CompleteInitialRoadInputs();
                Console.WriteLine("[SourceS004HistoricalOpening] result=Observed ledger=" + (bool)observed["original_ledger_completed"] +
                    " units=" + (int)observed["observed_owned_units"] + " historical=1 nativeParity=0 readiness=0");
            }
            if (!badDescriptor && !metadataBoundary && !expandedBoundary && !expandedRejected && !badReplay && !wrongMap && !wrongPhysical && !sourceGrid && !historical && !wrongHistory && !historicalGrants && !historicalPlacement && !invalidPlacement && !sourceOpening && !wrongFactory && !badMapReference && !badMapMode)
            {
                if (args[5] == "expanded-opening" || args[5] == "expanded-outer")
                {
                    int units = 0;
                    using (var query = world.Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent)))
                        units = query.CalculateEntityCount();
                    var failure = new JObject { ["stage"] = world.InitializationStage, ["exception"] = error.ToString(),
                        ["observed_owned_role_entities"] = units, ["native_parity"] = false, ["mission_ready"] = false };
                    using var stream = new FileStream(Path.Combine(args[4], "opening-failure.json"), FileMode.CreateNew, FileAccess.Write);
                    using var writer = new StreamWriter(stream); writer.Write(failure.ToString());
                }
                throw;
            }
            Console.WriteLine("[SourceResourceInitializerControl] result=Passed mode=" + args[5] + " stage=" + world.InitializationStage +
                " stop=IntentionalNegativeControl gameplay=0 readiness=0");
            return 0;
        }
    }
}
