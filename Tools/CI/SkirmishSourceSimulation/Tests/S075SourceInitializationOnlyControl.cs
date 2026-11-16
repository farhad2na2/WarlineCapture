using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// One-shot source-world initialization diagnostic. This control sends no camera,
// UI, command, or simulation-tick input and makes no mission-fit/readiness claim.
internal static class S075SourceInitializationOnlyControl
{
    private const string Repository = "/Volumes/T7/Projects/WarlineCapture";
    private const string InitializationPath = Repository + "/T Logs/SkirmishProduction/s075-source-initialization-candidate-20261114-r3/initialization.json";
    private const string InitializationSha256 = "954ee3b4a4ed10364ae16841dacdacd08fc9ed89bf5175573e12f3c31f786c65";
    private const string BuildReceiptPath = Repository + "/T Logs/SkirmishProduction/s075-expanded-air-context-build-20261114-r3/build.json";
    private const string BuildReceiptSha256 = "388ff6caf8fb4232ea9b4414948d0e04fa58bdae7287f0c4138ffaf04cd4babf";
    private const string BuildManifestPath = Repository + "/Tools/CI/SkirmishSourceSimulation/source_manifest.s003derived.plan.json";
    private const string MatchScenePath = Repository + "/Assets/Game/Scenes/Match.unity";

    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

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

    private static void ValidateRun(JObject run)
    {
        string[] keys = { "schema_version", "kind", "mission", "seed", "initialization", "repository_root",
            "source_repository_root", "output_directory", "mission_scene", "build_receipt", "diagnostic_executable" };
        if (run == null || run.Properties().Count() != keys.Length || run.Properties().Any(p => !keys.Contains(p.Name)) ||
            (int?)run["schema_version"] != 1 || (string)run["kind"] != "SkirmishSourceSimulationRun" ||
            (string)run["mission"] != "S075" || (int?)run["seed"] != 104804)
            throw new InvalidDataException("Exact bounded S075/104804 source initialization run required");

        string repositoryRoot = Path.GetFullPath((string)run["repository_root"] ?? "");
        string sourceRoot = Path.GetFullPath((string)run["source_repository_root"] ?? "");
        if (repositoryRoot != Repository || sourceRoot != Repository)
            throw new InvalidDataException("Exact repository and source roots required");
        string output = Path.GetFullPath((string)run["output_directory"] ?? "");
        if (!Directory.Exists(output) || !output.StartsWith(Repository + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Existing repository-owned isolated output directory required");

        var init = run["initialization"] as JObject;
        ReadPinned(init, InitializationPath, "S075 initialization", 4 * 1024 * 1024);
        if ((string)init["sha256"] != InitializationSha256)
            throw new InvalidDataException("Exact retained S075 initialization required");
        var scene = run["mission_scene"] as JObject;
        ReadPinned(scene, MatchScenePath, "Match scene", 16 * 1024 * 1024);

        var buildDescriptor = run["build_receipt"] as JObject;
        byte[] buildBytes = ReadPinned(buildDescriptor, BuildReceiptPath, "S075 source build receipt", 16 * 1024 * 1024);
        if ((string)buildDescriptor["sha256"] != BuildReceiptSha256)
            throw new InvalidDataException("Exact current S075 r3 source build receipt required");
        var build = JObject.Parse(Encoding.UTF8.GetString(buildBytes));
        if ((string)build["kind"] != "SkirmishSourceSimulationBuild" || (string)build["status"] != "Passed" ||
            (string)build["mission"] != "S075" || (int?)build["seed"] != 104804 || (int?)build["compile_exit"] != 0 ||
            Path.GetFullPath((string)build["source_manifest"]?["path"] ?? "") != BuildManifestPath ||
            (string)build["source_manifest"]?["sha256"] != Hash(File.ReadAllBytes(BuildManifestPath)))
            throw new InvalidDataException("Passed current S075 S003-derived source build required");
        string executable = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        ReadPinned(run["diagnostic_executable"] as JObject, executable, "diagnostic executable", 128 * 1024 * 1024);
    }

    private static JArray FindMapInputGaps(JObject initialization)
    {
        var gaps = new JArray();
        var navigation = initialization["navigation_reference"] as JObject;
        if (navigation == null || navigation["path"]?.Type != JTokenType.String || navigation["sha256"]?.Type != JTokenType.String)
            gaps.Add("navigation_reference: pinned physical-source navigation capture is missing");
        else
        {
            try
            {
                byte[] bytes = AutonomousWorld.PinnedFile((string)navigation["path"], (string)navigation["sha256"], 128 * 1024 * 1024);
                var binding = JObject.Parse(Encoding.UTF8.GetString(bytes));
                var readback = binding["native_readback"] as JObject;
                if (readback == null || readback["path"]?.Type != JTokenType.String || readback["sha256"]?.Type != JTokenType.String)
                    gaps.Add("navigation_reference.native_readback: compiler-bound native readback descriptor is missing");
            }
            catch (Exception error)
            {
                gaps.Add("navigation_reference: pinned capture is not readable as the required source/native-readback owner input (" + error.GetType().Name + ")");
            }
        }
        var placement = initialization["placement_domain"] as JObject;
        var masks = placement?["original_owner_masks"] as JObject;
        if (masks == null || masks["path"]?.Type != JTokenType.String || masks["sha256"]?.Type != JTokenType.String)
            gaps.Add("placement_domain.original_owner_masks: pinned original-owner mask capture is missing");
        else
        {
            string path = Path.GetFullPath((string)masks["path"]);
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                Hash(File.ReadAllBytes(path)) != (string)masks["sha256"])
                gaps.Add("placement_domain.original_owner_masks: pinned original-owner mask bytes are unavailable or changed");
        }
        return gaps;
    }

    public static int Main(string[] args)
    {
        if (args == null || args.Length != 1)
            throw new ArgumentException("One pinned S075 source run request JSON path required");

        var run = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[0])));
        ValidateRun(run);
        string initPath = (string)run["initialization"]["path"];
        string initSha = (string)run["initialization"]["sha256"];
        var initialization = JObject.Parse(Encoding.UTF8.GetString(ReadPinned(
            (JObject)run["initialization"], InitializationPath, "S075 initialization", 4 * 1024 * 1024)));
        SourceAdmittedProfiles.RequireInitialization(initialization);
        if ((string)initialization["mission_id"] != "S075" || (int?)initialization["selection"]?["seed"] != 104804)
            throw new InvalidDataException("Pinned initialization must compile the exact S075/104804 selection");

        JArray inputGaps = FindMapInputGaps(initialization);
        if (inputGaps.Count != 0)
        {
            Console.WriteLine("[S075SourceInitializationOnlyControl] " + new JObject
            {
                ["kind"] = "S075SourceInitializationOnlyDiagnostic",
                ["status"] = "BlockedByMissingSourceMapInputs",
                ["input_gaps"] = inputGaps,
                ["world_allocated"] = false,
                ["filesystem_output_written"] = false,
                ["gameplay_ticks_run"] = false,
                ["aria_run"] = false,
                ["ui_or_command_input_sent"] = false,
                ["synthetic_input_created"] = false,
                ["source_fit_certified"] = false,
                ["mission_ready"] = false
            }.ToString(Formatting.None));
            return 2;
        }

        var outputPath = Path.Combine((string)run["output_directory"], "s075-source-initialization-only.json");
        if (File.Exists(outputPath)) throw new IOException("Fresh initialization-only output path required");
        JObject result;
        using (var world = new AutonomousWorld())
        {
            world.PrepareFreshInitialRoads(run);
            world.Initialize(initPath, initSha, 1f / 60f,
                Path.GetFullPath((string)run["source_repository_root"]),
                Path.GetFullPath((string)run["repository_root"]));

            Need(world.Setup != null && world.Setup.CatalogId == "S075" && world.Setup.DefinitionId == "skirmish.s075" &&
                world.Setup.Seed == 104804 && world.Setup.SetupHash == 1280564676u &&
                world.Setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
                world.Setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile &&
                world.Setup.StartPackageId == SkirmishStartPackageId.FieldBase,
                "Actual compiled setup differs from the pinned S075 target");
            Need(world.ActualDefinition != null && world.ActualDefinitionScenarioIndex == -1 &&
                world.Session != Entity.Null && world.Entities.Exists(world.Session),
                "Actual compiled definition and source session required");
            Need(world.Tick == 0 && world.SimulationSeconds == 0 && world.SourceNumericCamera == null,
                "Initialization-only run must stop before camera binding or simulation ticks");
            using (var snapshots = world.Entities.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent)))
                Need(snapshots.CalculateEntityCount() == 0, "No source camera snapshot may be created");

            var session = world.Entities.GetComponentData<SkirmishExpandedSessionComponent>(world.Session);
            Need(session.SetupHash == world.Setup.SetupHash && session.Phase == SkirmishSessionPhase.Playing,
                "Actual initialized source session does not match the compiled setup");
            int expectedActors = world.Setup.Forces.Sum(force => force.Quantity);
            Need(world.OpeningActors.Count == expectedActors,
                "Actual opening actor count differs from the compiled force roster");

            result = new JObject
            {
                ["kind"] = "S075SourceInitializationOnlyDiagnostic",
                ["status"] = "InitializedSourceWorld_NoTicks",
                ["world_id"] = world.WorldId,
                ["initialization_sha256"] = initSha,
                ["catalog_id"] = world.Setup.CatalogId,
                ["definition_id"] = world.Setup.DefinitionId,
                ["setup_hash"] = world.Setup.SetupHash,
                ["seed"] = world.Setup.Seed,
                ["scenario_index"] = world.ActualDefinitionScenarioIndex,
                ["session_entity"] = world.Session.Index + ":" + world.Session.Version,
                ["session_phase"] = session.Phase.ToString(),
                ["opening_actor_count"] = world.OpeningActors.Count,
                ["compiled_force_actor_count"] = expectedActors,
                ["building_count"] = world.Buildings.Count,
                ["grid_entity"] = world.GridEntity.Index + ":" + world.GridEntity.Version,
                ["grid_width"] = world.Entities.GetComponentData<GridConfig>(world.GridEntity).Width,
                ["grid_height"] = world.Entities.GetComponentData<GridConfig>(world.GridEntity).Height,
                ["initialization_stage"] = world.InitializationStage,
                ["tick"] = world.Tick,
                ["source_camera_provider_bound"] = false,
                ["runtime_camera_snapshot_count"] = 0,
                ["gameplay_ticks_run"] = false,
                ["aria_run"] = false,
                ["ui_or_command_input_sent"] = false,
                ["synthetic_input_created"] = false,
                ["native_context_qualified"] = false,
                ["source_fit_certified"] = false,
                ["camera_framing_observed"] = false,
                ["mission_ready"] = false,
                ["scope"] = "One actual source-world initialization only; no camera, owner tick, ARIA, UI, command, fit or readiness claim"
            };
            world.CompleteInitialRoadInputs();
        }
        File.WriteAllText(outputPath, result.ToString(Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S075SourceInitializationOnlyControl] " + result.ToString(Formatting.None));
        return 0;
    }
}
