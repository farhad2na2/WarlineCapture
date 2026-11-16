using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// Source-only one-shot S075 map initialization diagnostic. This helper does not
// bind a camera, tick owners, run ARIA, or issue UI/gameplay commands.
internal static class S075SourceMapInitializationControl
{
    private const string Repository = "/Volumes/T7/Projects/WarlineCapture";
    private const string InitializationPath = Repository + "/T Logs/SkirmishProduction/s075-source-initialization-candidate-20261115-r9/initialization.json";
    private const string InitializationSha256 = "0f28ecc2c860266bd07dee65f0205349461cd74c9de409fce40330bb355480c8";
    private const string SourceMapPath = Repository + "/T Logs/SkirmishProduction/s026-source-map-bundle-20261107-r2/map-package.json";
    private const string SourceMapSha256 = "233a93bc7738b40f97beecf001f31cdd9d169cdc42e011f45ab7483c61d723b1";
    private const string MatchScenePath = Repository + "/Assets/Game/Scenes/Match.unity";

    private static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return String.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2")));
    }

    private static byte[] ReadPinned(JObject descriptor, string expectedPath, string label, int maxBytes)
    {
        if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
            descriptor["sha256"]?.Type != JTokenType.String)
            throw new InvalidDataException("Exact pinned " + label + " descriptor required");
        string path = Path.GetFullPath((string)descriptor["path"]);
        if (path != expectedPath || !File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Canonical regular " + label + " path required");
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0 || bytes.Length > maxBytes || Hash(bytes) != (string)descriptor["sha256"])
            throw new InvalidDataException("Pinned " + label + " bytes changed or exceed bounds");
        return bytes;
    }

    private static JObject RequireDescriptor(JObject run, string name)
    {
        return run[name] as JObject ?? throw new InvalidDataException("Pinned run input required: " + name);
    }

    private static void VerifySourceBuild(JObject run)
    {
        string executable = Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string buildPath = Path.Combine(Path.GetDirectoryName(executable), "build.json");
        var buildDescriptor = RequireDescriptor(run, "build_receipt");
        if (buildDescriptor.Count != 2 || Path.GetFullPath((string)buildDescriptor["path"] ?? "") != buildPath)
            throw new InvalidDataException("Build receipt must pin the executing source-map overlay build");
        byte[] bytes = AutonomousWorld.PinnedFile(buildPath, (string)buildDescriptor["sha256"], 16 * 1024 * 1024);
        var build = JObject.Parse(Encoding.UTF8.GetString(bytes));
        if ((string)build["kind"] != "SkirmishSourceSimulationBuild" || (string)build["status"] != "Passed" ||
            (string)build["mission"] != "S075" || (int?)build["seed"] != 104804 ||
            Path.GetFullPath((string)build["executable"]?["path"] ?? "") != executable ||
            Hash(File.ReadAllBytes(executable)) != (string)build["executable"]?["sha256"])
            throw new InvalidDataException("Executing source-map overlay must match a passed S075/104804 build");
    }

    private static void BindPaidCatalogProbe(AutonomousWorld world, JObject initial, JObject package, JObject stageTimings)
    {
        var metadata = initial["unit_production_metadata"] as JObject ?? throw new InvalidDataException("Actual S075 production metadata required");
        string metadataPath = (string)metadata["path"], metadataSha = (string)metadata["sha256"];
        MeasureStage(stageTimings, "metadata_bind_seconds", () => world.BindActualUnitMetadata(metadataPath, metadataSha, Repository));
        GeneratedS002TransportInputs transport = null;
        MeasureStage(stageTimings, "production_preparation_seconds", () =>
        {
            transport = new GeneratedS002TransportInputs(world.ActualUnitMetadata, Repository);
            world.PrepareActualProductionInputs(25, 0x12345678u, transport.TryMetadata);
        });
        SkirmishProductionDeliveryBasisComponent actualBasis = default;
        MeasureStage(stageTimings, "delivery_input_setup_seconds", () =>
        {
            var descriptor = package["clearance_descriptor"] as JObject ?? throw new InvalidDataException("Captured source-map clearance descriptor required");
            var clearance = new GeneratedS002DropClearance((string)descriptor["path"], (string)descriptor["sha256"]);
            var grid = world.Entities.GetComponentData<GridConfig>(world.GridEntity);
            if (!Game.Simulation.SharedProductionDeliveryKernel.TryResolveAxisAlignedMapFrame(grid.Width, grid.Height, grid.CellSize, grid.Origin, out var basis))
                throw new InvalidDataException("Actual admitted source grid must supply delivery basis");
            using (var query = world.Entities.CreateEntityQuery(typeof(GridConfig)))
            using (var grids = query.ToEntityArray(Unity.Collections.Allocator.Temp))
                if (grids.Length != 1 || grids[0] != world.GridEntity) throw new InvalidDataException("Exactly one actual placement grid required");
            if (world.Entities.HasComponent<SkirmishProductionDeliveryBasisComponent>(world.Session))
                throw new InvalidDataException("Fresh source session delivery basis required");
            world.Entities.AddComponentData(world.Session, new SkirmishProductionDeliveryBasisComponent { Basis = basis, SetupHash = world.Setup.SetupHash, PlacementGrid = world.GridEntity });
            actualBasis = world.Entities.GetComponentData<SkirmishProductionDeliveryBasisComponent>(world.Session);
            if (actualBasis.SetupHash != world.Setup.SetupHash || actualBasis.PlacementGrid != world.GridEntity || !Game.Simulation.SharedProductionDeliveryKernel.IsValidProductionDeliveryLogicalBasis(in actualBasis.Basis))
                throw new InvalidDataException("Actual source delivery basis readback mismatch");
            world.ActualDeliveryOwner.Helicopters = SourceHelicopterDelivery.WithLogicalBasis(world.ActualDeliveryOwner, transport.Templates, actualBasis.Basis,
                (SourceProductionGroundWorld w, Unity.Mathematics.int2 p, ref Unity.Collections.NativeBitArray reserved) => clearance.Reserve(w, p, ref reserved));
        });
        MeasureStage(stageTimings, "configured_building_metadata_seconds", () => world.BindSourceConfiguredBuildingMetadata(Repository, Repository));
        MeasureStage(stageTimings, "paid_catalog_binding_seconds", () => world.BindActualPaidCatalog(metadataPath, metadataSha));
    }

    private static void MeasureStage(JObject stageTimings, string name, Action action)
    {
        long started = Stopwatch.GetTimestamp();
        try { action(); }
        finally { stageTimings[name] = (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency; }
    }

    private static void WriteStartupPreparationFailure(string output, JObject stageTimings, long setupStartTimestamp,
        long setupCompleteTimestamp, Exception error)
    {
        string failurePath = Path.Combine(output, "s075-source-map-startup-preparation-failure.json");
        try
        {
            if (File.Exists(failurePath)) return;
            var failure = new JObject
            {
                ["kind"] = "S075SourceMapStartupPreparationFailure",
                ["status"] = "StartupPreparationFailed",
                ["setup_elapsed_monotonic_seconds"] = (setupCompleteTimestamp - setupStartTimestamp) / (double)Stopwatch.Frequency,
                ["setup_preparation_elapsed_monotonic_seconds"] = (Stopwatch.GetTimestamp() - setupCompleteTimestamp) / (double)Stopwatch.Frequency,
                ["setup_stage_elapsed_seconds"] = (JObject)stageTimings.DeepClone(),
                ["exception_type"] = error.GetType().FullName,
                ["exception_message"] = error.Message,
                ["scope"] = "Actual S075 source setup/preparation failure timings; no readiness or mission-fit claim"
            };
            File.WriteAllText(failurePath, failure.ToString(Formatting.Indented) + Environment.NewLine);
        }
        catch (Exception writeError)
        {
            Console.Error.WriteLine("[S075SourceMapInitializationControl] Could not write startup failure timing receipt: " + writeError.Message);
        }
    }

    internal static int Run(JObject run)
    {
        string[] names = { "schema_version", "kind", "mission", "seed", "repository_root", "source_repository_root",
            "output_directory", "mission_scene", "initialization", "build_receipt", "source_derived_map",
            "source_derived_map_logical_join" };
        bool transformProbe = (bool?)run?["isolated_transform_probe"] == true;
        bool ownerTickProbe = (bool?)run?["owner_tick_probe"] == true;
        bool publicContextProbe = (bool?)run?["public_context_probe"] == true;
        bool ariaPolicyProbe = (bool?)run?["aria_policy_probe"] == true;
        int ariaStepCount = ariaPolicyProbe ? ((int?)run["aria_step_count"] ?? 1) : 0;
        int ownerTickCount = ownerTickProbe ? ((int?)run["owner_tick_count"] ?? 1) : 0;
        bool paidCatalogProbe = (bool?)run?["paid_catalog_probe"] == true || ownerTickProbe;
        if (run == null || (transformProbe && paidCatalogProbe) || ((publicContextProbe || ariaPolicyProbe) && !ownerTickProbe) || (ownerTickProbe && (ownerTickCount < 1 || ownerTickCount > 8 || run.Property("paid_catalog_probe") != null)) || (!ownerTickProbe && run.Property("owner_tick_count") != null) || (ariaPolicyProbe && (ariaStepCount < 1 || ariaStepCount > 1800)) || (!ariaPolicyProbe && run.Property("aria_step_count") != null) || run.Properties().Count() != names.Length + (transformProbe || paidCatalogProbe ? 1 : 0) + (run.Property("owner_tick_count") != null ? 1 : 0) + (publicContextProbe ? 1 : 0) + (ariaPolicyProbe ? 1 : 0) + (run.Property("aria_step_count") != null ? 1 : 0) || run.Properties().Any(p => !names.Contains(p.Name) && p.Name != "isolated_transform_probe" && p.Name != "paid_catalog_probe" && p.Name != "owner_tick_probe" && p.Name != "owner_tick_count" && p.Name != "public_context_probe" && p.Name != "aria_policy_probe" && p.Name != "aria_step_count") ||
            (int?)run["schema_version"] != 1 || (string)run["kind"] != "SkirmishSourceSimulationRun" ||
            (string)run["mission"] != "S075" || (int?)run["seed"] != 104804)
            throw new InvalidDataException("Exact S075/104804 source-map initialization request required");
        if (Path.GetFullPath((string)run["repository_root"] ?? "") != Repository ||
            Path.GetFullPath((string)run["source_repository_root"] ?? "") != Repository)
            throw new InvalidDataException("Exact source/repository roots required");
        string output = Path.GetFullPath((string)run["output_directory"] ?? "");
        if (!Directory.Exists(output) || !output.StartsWith(Repository + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Existing repository-owned isolated output directory required");

        var initDescriptor = RequireDescriptor(run, "initialization");
        byte[] initBytes = ReadPinned(initDescriptor, InitializationPath, "S075 initialization", 4 * 1024 * 1024);
        if ((string)initDescriptor["sha256"] != InitializationSha256)
            throw new InvalidDataException("Exact S075 candidate initialization required");
        var initial = JObject.Parse(Encoding.UTF8.GetString(initBytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        SourceAdmittedProfiles.RequireInitialization(initial);
        if ((string)initial["mission_id"] != "S075" || (int?)initial["selection"]?["seed"] != 104804)
            throw new InvalidDataException("Initialization must select exact S075/104804");

        ReadPinned(RequireDescriptor(run, "mission_scene"), MatchScenePath, "Match scene", 16 * 1024 * 1024);
        byte[] packageBytes = ReadPinned(RequireDescriptor(run, "source_derived_map"), SourceMapPath, "physical Campaign map package", 16 * 1024 * 1024);
        VerifySourceBuild(run);

        // Prepare consumes only pinned captured physical-map data and the pinned
        // original public logical-map join. No values are synthesized here.
        var prepared = SourceDerivedMapRuntime.Prepare(run, initial);
        if (prepared == null || !prepared.IsLogicalAlias ||
            prepared.LogicalMapId != "opmap.skirmish.industrial_basin" ||
            prepared.PhysicalMapId != "opmap.skirmish.desert_base_01")
            throw new InvalidDataException("Verified S075 Industrial Basin to Desert Base source-map alias required");
        var frame = prepared.LogicalJoinReceipt["package_frame_join"]?["frame"] as JObject;
        var origin = frame?["origin"] as JArray;
        if (frame == null || origin == null || origin.Count != 3)
            throw new InvalidDataException("Verified source-map join frame required");
        prepared.VerifyLogicalSelection(initial, (string)initial["static_map"]?["operation_map_id"],
            (int)frame["width"], (int)frame["height"], (float)frame["cell_size"],
            (float)origin[0], (float)origin[1], (float)origin[2]);

        string resultPath = Path.Combine(output, "s075-source-map-initialization-only.json");
        if (File.Exists(resultPath)) throw new IOException("Fresh source-map initialization output required");
        JObject result;
        using (var world = new AutonomousWorld())
        {
            // Both owners receive the exact same prepared instance before any
            // initialization work can consume map inputs.
            SourceDeclaredRuntimeInputs.BindSourceDerivedMap(prepared);
            world.BindSourceDerivedMap(prepared);
            world.PrepareFreshInitialRoads(run);
            long setupStartTimestamp = Stopwatch.GetTimestamp();
            world.Initialize((string)initDescriptor["path"], (string)initDescriptor["sha256"], 1f / 60f,
                Path.GetFullPath((string)run["source_repository_root"]), Path.GetFullPath((string)run["repository_root"]));

            if (world.Setup == null || world.Setup.CatalogId != "S075" || world.Setup.Seed != 104804 ||
                world.Setup.SetupHash != 1280564676u || world.Setup.OperationMapId != prepared.LogicalMapId ||
                world.Setup.DefinitionId != "skirmish.s075" || world.Setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault ||
                world.Setup.ArmyProfileId != SkirmishArmyProfileId.AirMobile || world.Setup.StartPackageId != SkirmishStartPackageId.FieldBase ||
                world.ActualDefinitionScenarioIndex != -1 || world.Tick < 1 || world.Tick > 64 ||
                world.SimulationSeconds != world.Tick * (double)(1f / 60f) ||
                world.SourceNumericCamera != null)
                throw new InvalidDataException("Initialized S075 source context differs from actual precompiled setup or advanced beyond initialization: " +
                    (world.Setup == null ? "setup=null" : $"catalog={world.Setup.CatalogId}, seed={world.Setup.Seed}, hash={world.Setup.SetupHash}, map={world.Setup.OperationMapId}, expectedMap={prepared.LogicalMapId}, definition={world.Setup.DefinitionId}, objective={world.Setup.ObjectiveKind}, army={world.Setup.ArmyProfileId}, start={world.Setup.StartPackageId}") +
                    $", scenarioIndex={world.ActualDefinitionScenarioIndex}, tick={world.Tick}, seconds={world.SimulationSeconds}, cameraPresent={world.SourceNumericCamera != null}");
            var grid = world.Entities.GetComponentData<GridConfig>(world.GridEntity);
            prepared.VerifyLogicalSelection(initial, world.Setup.OperationMapId, grid.Width, grid.Height,
                grid.CellSize, grid.Origin.x, grid.Origin.y, grid.Origin.z);
            using (var snapshots = world.Entities.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent)))
                if (snapshots.CalculateEntityCount() != 0) throw new InvalidDataException("Camera snapshot unexpectedly exists");
            if (world.SourceDerivedMapReceipt == null ||
                (int?)world.SourceDerivedMapReceipt["installed_navigation_count"] != prepared.Navigation.Count)
                throw new InvalidDataException("Actual installed source-derived map receipt required");

            int deathCandidateCount;
            using (var candidates = world.Entities.CreateEntityQuery(SourceNativeDeathBinding.UnitAuthoringPreflightQuery()))
                deathCandidateCount = candidates.CalculateEntityCount();
            SourceNativeDeathBinding.ValidateActualUnits(world.Entities);

            var session = world.Entities.GetComponentData<SkirmishExpandedSessionComponent>(world.Session);
            int expectedActors = world.Setup.Forces.Sum(force => force.Quantity);
            if (world.OpeningActors.Count != expectedActors || session.SetupHash != world.Setup.SetupHash)
                throw new InvalidDataException("Actual initialized session/force roster differs from compiled setup");
            bool groundSessionGuardAccepted;
            string groundSessionGuardRejection = null;
            try
            {
                SourceGroundMissionFamily.RequireSession(world.Entities, world.Session, true);
                groundSessionGuardAccepted = true;
            }
            catch (NotSupportedException error)
            {
                groundSessionGuardAccepted = false;
                groundSessionGuardRejection = error.Message;
            }
            var qualifiedAirTransformSetup = SourceGroundMissionFamily.QualifyAirSourceSession(world.Entities, world.Session);
            if (!ReferenceEquals(qualifiedAirTransformSetup, world.Setup))
                throw new InvalidDataException("Air transform qualification must retain the actual compiled setup");
            bool nullSessionRejected = false;
            try { SourceGroundMissionFamily.QualifyAirSourceSession(world.Entities, Entity.Null); }
            catch (NotSupportedException) { nullSessionRejected = true; }
            if (!nullSessionRejected) throw new InvalidDataException("Air transform qualification accepted a null session");
            int? projectedTransforms = transformProbe ? world.RunIsolatedSourceTransformProbe() : (int?)null;
            int setupTick = world.Tick;
            long setupCompleteTimestamp = Stopwatch.GetTimestamp();
            var setupStageTimings = new JObject();
            JObject buildingEligibility = null;
            if (paidCatalogProbe)
            {
                JObject package = null;
                try
                {
                    MeasureStage(setupStageTimings, "source_map_package_parse_seconds", () =>
                        package = JObject.Parse(Encoding.UTF8.GetString(packageBytes)));
                    BindPaidCatalogProbe(world, initial, package, setupStageTimings);
                }
                catch (Exception error)
                {
                    WriteStartupPreparationFailure(output, setupStageTimings, setupStartTimestamp, setupCompleteTimestamp, error);
                    throw;
                }
                var tower = world.SourceDefinitions.Definitions.Single(definition => definition?.Prefab?.name == "Building_GuardTower");
                var eligibility = world.ActualAvailability.ReadBuildingSemantic(tower.Prefab, tower.MaterialsCost);
                bool nonBuildingRejected = false;
                try { world.ActualAvailability.ReadBuildingSemantic(world.ActualConfiguredUnits[0], 0); }
                catch (NotSupportedException) { nonBuildingRejected = true; }
                if (!nonBuildingRejected || eligibility.CommandAdapterPresent) throw new InvalidDataException("Read-only source building eligibility boundary changed");
                buildingEligibility = new JObject { ["source_failure"] = eligibility.Failure.ToString(),
                    ["eligible"] = eligibility.PrimaryInteractable, ["required_building"] = eligibility.RequiredBuilding,
                    ["nonbuilding_prefab_rejected"] = nonBuildingRejected, ["placement_command_bound"] = eligibility.CommandAdapterPresent,
                    ["scope"] = "Actual projected factory member and original request-failure owner; read only, no selection or placement" };
            }
            var ownerTicks = new JArray();
            long ownerTickStartTimestamp;
            if (ownerTickProbe)
            {
                SourceOwnerOrderAdmission.RequirePlan();
                long ownerInitializationStarted = Stopwatch.GetTimestamp();
                try
                {
                    MeasureStage(setupStageTimings, "building_lifetime_owner_initialization_seconds", () => world.InitializeActualBuildingLifetime(Repository));
                    MeasureStage(setupStageTimings, "continuation_owner_initialization_seconds", world.InitializeActualContinuationOwners);
                }
                catch (Exception error)
                {
                    setupStageTimings["owner_initialization_total_seconds"] = (Stopwatch.GetTimestamp() - ownerInitializationStarted) / (double)Stopwatch.Frequency;
                    WriteStartupPreparationFailure(output, setupStageTimings, setupStartTimestamp, setupCompleteTimestamp, error);
                    throw;
                }
                setupStageTimings["owner_initialization_total_seconds"] = (Stopwatch.GetTimestamp() - ownerInitializationStarted) / (double)Stopwatch.Frequency;
                // End setup preparation immediately before the first ordinary owner tick.
                ownerTickStartTimestamp = Stopwatch.GetTimestamp();
                for (int i = 0; i < ownerTickCount; i++) ownerTicks.Add(world.AdvanceConstructedSourceOwners());
            }
            else
            {
                ownerTickStartTimestamp = Stopwatch.GetTimestamp();
            }
            JObject publicContext = null;
            if (publicContextProbe)
            {
                var actualSession = world.Entities.GetComponentData<SkirmishExpandedSessionComponent>(world.Session);
                var actualMatch = world.Entities.GetComponentData<SkirmishMatchState>(world.Session);
                int sourcePreset = SkirmishPresetConfig.SourcePresetIndex(actualMatch.ScenarioIndex);
                if (!GroundMissionContextFactory.TryCreateLogicalPublicData(world.ActualDefinition, world.Setup, world.ActualCompileManifest,
                    world.ActualCompileMatrix, world.ActualCanonicalCatalog, actualSession, actualMatch.ScenarioIndex, sourcePreset,
                    out var context, out string reason)) throw new InvalidDataException("Actual source logical context rejected: " + reason);
                if (!GroundMissionContextFactory.MatchesLogicalSession(context, world.Setup, actualSession, actualMatch.ScenarioIndex))
                    throw new InvalidDataException("Public context is not bound to the actual source session");
                var view = SkirmishAriaPublicProjection.FromSession(world.Entities, world.Session, world.Army);
                if (!view.AirProfile || !view.Playing || view.Finished) throw new InvalidDataException("Actual active public AirMobile projection required");
                var offers = new JObject();
                foreach (var role in new[] { SkirmishRoleKind.Rifle, SkirmishRoleKind.LogisticsTruck, SkirmishRoleKind.Tanker })
                {
                    var offer = world.ActualAriaPaid.Read(role);
                    offers[role.ToString()] = new JObject { ["present"] = offer.Present, ["can_request"] = offer.CanRequest, ["reason"] = offer.Reason };
                }
                publicContext = new JObject { ["actual_session_bound"] = true, ["catalog_id"] = context.CatalogId,
                    ["setup_hash"] = context.SetupHash, ["playable_scenario_index"] = context.PlayableScenarioIndex,
                    ["source_preset_index"] = context.SourcePresetIndex, ["observation_scope"] = context.ObservationScope.ToString(),
                    ["air_profile"] = view.AirProfile, ["playing"] = view.Playing, ["own_infantry"] = view.OwnInfantry,
                    ["own_materials"] = view.OwnMaterials, ["public_gameplay_view"] = JObject.FromObject(view),
                    ["actual_paid_offers"] = offers, ["scope"] = "Actual same-world public projection and read-only paid-owner offers; no ARIA policy or command" };
            }
            var ariaSteps = new JArray();
            if (ariaPolicyProbe)
            {
                world.InitializeSourceS002Aria(world.ActualAriaPaid);
                using var ariaLog = new StreamWriter(Path.Combine(output, "aria-steps.jsonl"));
                using var ownerLog = new StreamWriter(Path.Combine(output, "owner-ticks.jsonl"));
                foreach (var initialTick in ownerTicks) { ownerLog.WriteLine(initialTick.ToString(Formatting.None)); ownerLog.Flush(); }
                for (int i = 0; i < ariaStepCount; i++)
                {
                    if (i > 0) { var tickRow = world.AdvanceConstructedSourceOwners(); ownerTicks.Add(tickRow); ownerLog.WriteLine(tickRow.ToString(Formatting.None)); ownerLog.Flush(); }
                    if (world.Entities.GetComponentData<SkirmishObjectiveStateComponent>(world.Session).Outcome != SkirmishOutcomeKind.None) break;
                    JObject step;
                    try { step = world.StepSourceS002Aria(); }
                    catch (Exception error)
                    {
                        var failure = new JObject { ["kind"] = "S075ActualSourcePolicyFailureObservation", ["world_id"] = world.WorldId,
                            ["tick"] = world.Tick, ["simulation_seconds"] = world.SimulationSeconds,
                            ["setup_elapsed_monotonic_seconds"] = (setupCompleteTimestamp - setupStartTimestamp) / (double)Stopwatch.Frequency,
                            ["setup_preparation_elapsed_monotonic_seconds"] = (ownerTickStartTimestamp - setupCompleteTimestamp) / (double)Stopwatch.Frequency,
                            ["run_elapsed_monotonic_seconds"] = (Stopwatch.GetTimestamp() - ownerTickStartTimestamp) / (double)Stopwatch.Frequency,
                            ["run_elapsed_scope"] = "From immediately before the first configured constructed-owner tick through this policy failure observation; monotonic wall-clock elapsed, not simulated time",
                            ["exception_type"] = error.GetType().FullName, ["exception_message"] = error.Message,
                            ["completed_aria_steps"] = ariaSteps.Count, ["completed_owner_ticks"] = ownerTicks.Count,
                            ["source_delivered_members"] = world.ActualDeliveryOwner.DeliveredMembers,
                            ["source_delivery_events"] = JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents),
                            ["scope"] = "Actual same-world source observation at policy failure; no native/player readiness claim" };
                        try { failure["public_gameplay_view"] = JObject.FromObject(SkirmishAriaPublicProjection.FromSession(world.Entities, world.Session, world.Army)); }
                        catch (Exception readError) { failure["public_view_read_error"] = readError.Message; }
                        try { failure["guard_tower_source_control"] = SourceAriaS002Controller.ReadS003GuardTowerControl(world.Entities); }
                        catch (Exception readError) { failure["guard_tower_source_control_error"] = readError.Message; }
                        try
                        {
                            var tower = world.SourceDefinitions.Definitions.Single(definition => definition?.Prefab?.name == "Building_GuardTower");
                            var availability = world.ActualAvailability.ReadBuildingSemantic(tower.Prefab, tower.MaterialsCost);
                            failure["guard_tower_semantic_eligibility"] = new JObject { ["source_failure"] = availability.Failure.ToString(),
                                ["required_building"] = availability.RequiredBuilding, ["eligible"] = availability.PrimaryInteractable,
                                ["placement_command_bound"] = availability.CommandAdapterPresent,
                                ["scope"] = "Actual projected factory member and original request-failure owner; read only, no selection or placement" };
                        }
                        catch (Exception readError) { failure["guard_tower_semantic_eligibility_error"] = readError.Message; }
                        File.WriteAllText(Path.Combine(output, "aria-policy-failure-observation.json"), failure.ToString(Formatting.Indented) + Environment.NewLine);
                        throw;
                    }
                    ariaSteps.Add(step);
                    ariaLog.WriteLine(step.ToString(Formatting.None)); ariaLog.Flush();
                    if ((bool?)step["stopped"] == true) break;
                }
            }
            long runEndTimestamp = Stopwatch.GetTimestamp();
            result = new JObject
            {
                ["kind"] = "S075SourceMapInitializationOnlyDiagnostic",
                ["status"] = ownerTickProbe ? "BoundedSourceOwnerTicksCompleted" : "InitializedSourceWorld_NoGameplayOwnerTick",
                ["world_id"] = world.WorldId,
                ["initialization_sha256"] = (string)initDescriptor["sha256"],
                ["setup_hash"] = world.Setup.SetupHash,
                ["operation_map_id"] = world.Setup.OperationMapId,
                ["physical_source_map_id"] = prepared.PhysicalMapId,
                ["logical_join_sha256"] = prepared.LogicalJoinSha,
                ["grid_width"] = grid.Width,
                ["grid_height"] = grid.Height,
                ["grid_cell_size"] = grid.CellSize,
                ["grid_origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
                ["navigation_rows"] = prepared.Navigation.Count,
                ["building_count"] = world.Buildings.Count,
                ["opening_actor_count"] = world.OpeningActors.Count,
                ["compiled_force_actor_count"] = expectedActors,
                ["death_candidate_count"] = deathCandidateCount,
                ["ground_session_guard_accepted"] = groundSessionGuardAccepted,
                ["ground_session_guard_rejection"] = groundSessionGuardRejection,
                ["ground_session_guard_scope"] = "Read-only actual initialized ECS session; no owner step invoked",
                ["air_transform_session_qualified"] = true,
                ["air_transform_null_session_rejected"] = nullSessionRejected,
                ["isolated_transform_probe"] = transformProbe,
                ["isolated_transform_nodes_projected"] = projectedTransforms,
                ["paid_catalog_probe"] = paidCatalogProbe,
                ["actual_paid_catalog_bound"] = paidCatalogProbe && world.ActualCatalog != null,
                ["guard_tower_semantic_eligibility"] = buildingEligibility,
                ["owner_tick_probe"] = ownerTickProbe,
                ["owner_tick_count"] = ownerTicks.Count,
                ["owner_ticks"] = ownerTicks,
                ["public_context_probe"] = publicContextProbe,
                ["public_context"] = publicContext,
                ["aria_policy_probe"] = ariaPolicyProbe,
                ["aria_step_count"] = ariaSteps.Count,
                ["aria_steps"] = ariaSteps,
                ["death_metadata_validation"] = "Actual query candidates matched pinned death key/duration/index/selection/bypass metadata only; no clip loading/playback claim",
                ["initialization_stage"] = world.InitializationStage,
                ["tick"] = world.Tick,
                ["setup_tick"] = setupTick,
                ["setup_clock_seconds"] = setupTick * (double)(1f / 60f),
                ["setup_clock_scope"] = "Bounded starting-building request processing inside Initialize",
                ["setup_elapsed_monotonic_seconds"] = (setupCompleteTimestamp - setupStartTimestamp) / (double)Stopwatch.Frequency,
                ["setup_elapsed_scope"] = "From immediately before Initialize through initialized-world checks and the optional isolated transform probe",
                ["setup_preparation_elapsed_monotonic_seconds"] = (ownerTickStartTimestamp - setupCompleteTimestamp) / (double)Stopwatch.Frequency,
                ["setup_preparation_scope"] = "From completed initialized-world checks through optional paid-catalog binding and constructed-owner initialization, ending immediately before the first ordinary owner tick (or probe completion when owner ticks are disabled)",
                ["setup_stage_elapsed_seconds"] = setupStageTimings,
                ["setup_stage_timing_scope"] = "Monotonic wall-clock measurements of separately named startup/preparation stages; values are subsets of setup_preparation_elapsed_monotonic_seconds and can omit control flow between stages",
                ["run_elapsed_monotonic_seconds"] = (runEndTimestamp - ownerTickStartTimestamp) / (double)Stopwatch.Frequency,
                ["run_elapsed_scope"] = ownerTickProbe ? "From immediately before the first configured constructed-owner tick through configured source owner/ARIA probe work; monotonic wall-clock elapsed, not simulated time" : "No constructed-owner tick was configured; elapsed is from completion of setup preparation through any remaining read-only probe work, measured by a monotonic wall clock",
                ["camera_provider_bound"] = false,
                ["camera_snapshot_count"] = 0,
                ["gameplay_ticks_run"] = ownerTickProbe,
                ["aria_run"] = ariaSteps.Count > 0,
                ["ui_or_command_input_sent"] = ariaSteps.OfType<JObject>().Any(step => (bool?)step["executed"] == true),
                ["synthetic_input_created"] = false,
                ["native_context_qualified"] = false,
                ["source_fit_certified"] = false,
                ["camera_framing_observed"] = false,
                ["mission_ready"] = false,
                ["scope"] = ariaPolicyProbe ? "Actual S075 source owners and bounded shared-policy logical steps; any executed semantic actions are recorded in aria_steps, with no rendered UI, synthetic input, delivery success, full strategy, fit or readiness claim" : ownerTickProbe ? "Actual S075 source owners advanced bounded ticks after source paid-catalog binding; no camera, ARIA, UI, player command, delivery success, fit or readiness claim" : transformProbe ? "Actual source map and S075 world initialized, then one isolated source transform phase; no complete gameplay owner tick, camera, ARIA, UI, command, fit or readiness claim" : paidCatalogProbe ? "Actual source world and paid catalog bound with captured source-map clearance and logical grid basis; no delivery, gameplay owner tick, camera, ARIA, UI, command, fit or readiness claim" : "Actual source map and S075 world initialized once, including bounded starting-building setup frames; no camera, gameplay owner tick, ARIA, UI, command, fit or readiness claim"
            };
            world.CompleteInitialRoadInputs();
        }
        File.WriteAllText(resultPath, result.ToString(Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S075SourceMapInitializationControl] " + result.ToString(Formatting.None));
        return 0;
    }
}
