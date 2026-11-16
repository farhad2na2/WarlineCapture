using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEngine;

internal static class SkirmishMissionLogicalBindingAuthor
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Expected one pinned authoring request JSON.");
            var request = JObject.Parse(File.ReadAllText(args[0]));
            if ((bool?)request["camera_selection_probe"] == true)
                return RunCameraSelectionProbe(request);
            if (request["focus_pair_probe"] != null)
            {
                var probe = (JObject)request["focus_pair_probe"];
                var role = (SkirmishRoleKind)Enum.Parse(typeof(SkirmishRoleKind), (string)probe["role"], true);
                string actor = (string)probe["actor_id"];
                if (String.IsNullOrEmpty(actor) || !actor.StartsWith(SkirmishRoleIds.ToId(role) + ".", StringComparison.Ordinal))
                    throw new InvalidDataException("Opening focus actor does not match its role.");
                throw new InvalidDataException("Focus pair probe is reserved for runner negative controls.");
            }
            string root = (string)request["source_repository_root"];
            string catalogId = (string)request["catalog_id"];
            int seed = (int)request["seed"];
            var size = (SkirmishSizeId)Enum.Parse(typeof(SkirmishSizeId), (string)request["size"], true);
            var difficulty = (SkirmishDifficultyId)Enum.Parse(typeof(SkirmishDifficultyId), (string)request["difficulty"], true);
            string graphPath = PinnedPath(root, request["graph"]);
            string layoutPath = PinnedPath(root, request["layout"]);
            string matrixPath = PinnedPath(root, request["setup_matrix"]);
            var graph = JObject.Parse(File.ReadAllText(graphPath));
            var materializer = new SourceConfigMaterializer(graph);
            var layout = materializer.LogicalLayout(JObject.Parse(File.ReadAllText(layoutPath)));
            string sourcePath = (string)request["campaign_source_path"];
            var profiles = (JObject)request["profiles"];
            var source = materializer.ByPath<OperationMapDefinition>(sourcePath);
            var definition = new SkirmishScenarioDefinitionConfig();
            definition.Configure(catalogId,
                materializer.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),
                materializer.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),
                materializer.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),
                materializer.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),
                materializer.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),
                materializer.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),
                materializer.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]), layout);
            definition.BindCampaignMap(source);

            var catalog = materializer.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
            UnityEngine.Resources.BindSourceResources(new System.Collections.Generic.Dictionary<string, object>
            {
                [SkirmishExpansionCatalogConfig.ResourceName] = catalog,
                [SkirmishSetupMatrixTable.PackagedResourceName] = new TextAsset(File.ReadAllText(matrixPath))
            });
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out var matrixError))
                throw new InvalidDataException(matrixError);

            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size,
                    seed, manifest, matrix, out var setup, out var compileReasons))
                throw new InvalidOperationException("Current mission compiler rejected source definition: " +
                    string.Join(";", compileReasons.Select(x => x.Code + ":" + x.Field + ":" + x.Detail)));
            if (!SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition, difficulty,
                    size, seed, manifest, matrix, out var inputs, out var captureReasons) || inputs == null)
                throw new InvalidOperationException("Current mission authoring-input owner rejected source: " + string.Join(";", captureReasons));

            var camera = SelectPerspectiveBattleCamera(source.BattleCameraId, source.Cameras.ToArray());
            var view = SkirmishEngineeringViewProfileFactory.Create(definition, inputs);
            view.MinHeight = 22f;
            view.Pitch = camera.EulerAngles.x;
            view.Yaw = camera.EulerAngles.y;
            view.NormalFieldOfView = camera.FieldOfView;
            view.BuildFieldOfView = camera.FieldOfView;
            view.MaximumGroundCenterSlantDistance = 60f;
            // Keep the engineering candidate within the source view owner's
            // ground-center limit for this Campaign camera and terrain.
            view.MaxHeight = Mathf.Min(38f, Mathf.Floor(view.GroundReferenceY +
                view.MaximumGroundCenterSlantDistance * Mathf.Sin(view.Pitch * Mathf.Deg2Rad)));
            view.NormalHeight = Mathf.Min(30f, view.MaxHeight);
            view.BuildHeight = view.MaxHeight;
            if (!view.IsValid) throw new InvalidOperationException("Authored candidate view failed source profile validation.");

            var focusPair = (from role in inputs.RequiredFramingRoles
                             let prefix = SkirmishRoleIds.ToId(role) + "."
                             from actorId in inputs.ActualOpeningPlayerActorIds
                             where !String.IsNullOrEmpty(SkirmishRoleIds.ToId(role)) &&
                                   actorId.StartsWith(prefix, StringComparison.Ordinal)
                             select new { Role = role, ActorId = actorId }).FirstOrDefault();
            if (focusPair == null)
                throw new InvalidOperationException("No opening player actor matches a supported required framing role.");
            string focusId = focusPair.ActorId;
            var focusRole = focusPair.Role;
            var rows = inputs.RequiredFramingRoles.Select(role =>
            {
                bool aircraft = role == SkirmishRoleKind.TransportHeli || role == SkirmishRoleKind.TransportPlane ||
                    role == SkirmishRoleKind.Drone || SkirmishRoleIds.Category(role) == SkirmishPopulationCategory.Air;
                bool enemy = setup.Forces.Any(x => x.FactionId == 2 && x.Quantity > 0 && x.RoleKind == role);
                var stages = SkirmishViewCaptureStage.Opening | SkirmishViewCaptureStage.MinimumZoom |
                    SkirmishViewCaptureStage.MaximumZoom | SkirmishViewCaptureStage.MatchExitRestored;
                if (aircraft) stages |= SkirmishViewCaptureStage.FollowParked |
                    SkirmishViewCaptureStage.FollowCruising | SkirmishViewCaptureStage.ManualViewRestored;
                return new SkirmishRoleFramingRequirement
                {
                    Role = role, Stages = stages, ObservePlayer = true, ObserveEnemy = enemy,
                    MinimumViewportHeight = aircraft ? .04f : .025f,
                    MaximumViewportDimension = .75f, SafeEdgeFraction = .025f,
                    MinimumSubjectElevation = inputs.SourceMinimumElevation,
                    MaximumSubjectElevation = inputs.SourceMaximumElevation,
                    Evidence = SkirmishViewEvidenceRequirement.NativePng |
                        SkirmishViewEvidenceRequirement.ActualMeshViewportBounds |
                        SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot |
                        SkirmishViewEvidenceRequirement.ObstructionCandidateInventory
                };
            }).OrderBy(x => (byte)x.Role).ToArray();
            var variant = new SkirmishMissionLogicalVariant
            {
                Size = setup.SizeId, Difficulty = setup.DifficultyId,
                View = view, OpeningFocusRole = focusRole,
                OpeningFocusStableActorId = focusId, OpeningFocusSmoothSeconds = 2.25f,
                SourceMinimumElevation = inputs.SourceMinimumElevation,
                SourceMaximumElevation = inputs.SourceMaximumElevation, Framing = rows
            };
            variant.ReviewedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);

            var binding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            binding.SchemaVersion = 1;
            binding.ContentVersion = 1;
            binding.DefinitionVersion = definition.ContentVersion;
            binding.ValueFormat = inputs.ValueFormat;
            binding.CatalogId = definition.CatalogId;
            binding.DefinitionId = definition.DefinitionId;
            binding.SourceOperationMapId = source.OperationMapId;
            binding.SourceSceneGuid = source.SourceSceneReference.AssetGUID;
            binding.SourceContentHash = source.ContentHash;
            binding.SourceSemanticFingerprint = inputs.SourceSemanticFingerprint;
            binding.LayoutId = definition.MapLayout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint = inputs.LayoutSemanticFingerprint;
            binding.ObjectiveKind = setup.ObjectiveKind;
            binding.Variants = new[] { variant };
            var bindingJson = Serialize(binding, variant, view, rows);
            var roundTrippedBinding = materializer.LogicalBinding(bindingJson);
            definition.BindMissionLogicalBinding(roundTrippedBinding);

            if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size,
                    seed, manifest, matrix, out var bound, out var boundReasons))
                throw new InvalidOperationException("Current mission compiler rejected authored binding: " +
                    string.Join(";", boundReasons.Select(x => x.Code + ":" + x.Field + ":" + x.Detail)));
            if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition, bound, out var compiledBinding, out var bindingError))
                throw new InvalidOperationException("Current logical-binding compiler rejected candidate: " + bindingError);

            var outDir = Path.GetFullPath((string)request["output_directory"]);
            if (Directory.Exists(outDir) && Directory.EnumerateFileSystemEntries(outDir).Any()) throw new InvalidOperationException("Output directory must be fresh and empty.");
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "mission-logical-binding.json"), bindingJson.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            var proof = new JObject
            {
                ["kind"] = "MissionCameraSeededLogicalBindingCandidate",
                ["status"] = "ExperimentalCompiledSourceOnlyCandidate",
                ["production_ready"] = false,
                ["request_pin_pre_post_verification"] = false,
                ["catalog_id"] = setup.CatalogId, ["seed"] = setup.Seed,
                ["size"] = setup.SizeId.ToString(), ["difficulty"] = setup.DifficultyId.ToString(),
                ["setup_hash_without_binding"] = setup.SetupHash, ["setup_hash_with_binding"] = bound.SetupHash,
                ["logical_binding_fingerprint"] = compiledBinding.Fingerprint,
                ["source_fingerprint"] = inputs.SourceSemanticFingerprint,
                ["layout_fingerprint"] = inputs.LayoutSemanticFingerprint,
                ["map_camera_id"] = camera.CameraId,
                ["map_camera_position"] = new JArray(camera.Position.x, camera.Position.y, camera.Position.z),
                ["map_camera_euler_degrees"] = new JArray(camera.EulerAngles.x, camera.EulerAngles.y, camera.EulerAngles.z),
                ["map_camera_field_of_view"] = camera.FieldOfView,
                ["camera_bounds_y"] = new JArray(source.Bounds.CameraMin.y, source.Bounds.CameraMax.y),
                ["authored_view_heights"] = new JArray(view.MinHeight, view.NormalHeight, view.MaxHeight, view.BuildHeight),
                ["authored_view_pitch_yaw_fov"] = new JArray(view.Pitch, view.Yaw, view.NormalFieldOfView),
                ["authored_ground_center_slant_cap"] = view.MaximumGroundCenterSlantDistance,
                ["source_elevation_range"] = new JArray(inputs.SourceMinimumElevation, inputs.SourceMaximumElevation),
                ["opening_player_actor_ids"] = new JArray(inputs.ActualOpeningPlayerActorIds),
                ["focus_actor_id"] = focusId,
                ["required_role_rows"] = new JArray(rows.Select(x => x.Role.ToString())),
                ["framing_thresholds_authority"] = "AuthoredCandidate_PresentationTargets_NotObserved",
                ["framing_thresholds"] = new JObject { ["ground_min_viewport_height"] = .025f,
                    ["air_min_viewport_height"] = .04f, ["max_viewport_dimension"] = .75f, ["safe_edge_fraction"] = .025f },
                ["evidence_requirements_only"] = true, ["native_pngs_observed"] = false,
                ["mesh_bounds_observed"] = false, ["native_camera_observed"] = false,
                ["camera_observation_claim"] = false, ["screen_fit_verified"] = false,
                ["validation_passed"] = false, ["native_acceptance"] = false,
                ["world_allocated"] = false, ["placement_performed"] = false,
                ["source_fit_verified"] = false, ["source_fit_certified"] = false,
                ["runtime_admission"] = false, ["mission_ready"] = false
            };
            proof["request_catalog_id"] = catalogId;
            proof["experimental_tool"] = true;
            proof["production_ready"] = false;
            proof["request_seed"] = seed;
            proof["observation_claim"] = false;
            proof["source_fit_claim"] = false;
            proof["runtime_claim"] = false;
            proof["readiness_claim"] = false;
            File.WriteAllText(Path.Combine(outDir, "candidate-proof.json"), proof.ToString() + "\n");
            Console.WriteLine("[SkirmishMissionLogicalBindingAuthor] result=ExperimentalCompiledSourceOnlyCandidate production_ready=0 mission=" + catalogId + " setup=" + bound.SetupHash +
                " roles=" + rows.Length + " world=0 placement=0 readiness=0");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    // A mission view is derived from the source's battle camera. Planning and battle
    // cameras may intentionally differ; source fit and actual camera observation remain
    // separate gates and are not asserted by this author.
    private static OperationMapCameraConfig SelectPerspectiveBattleCamera(
        string battleCameraId, OperationMapCameraConfig[] cameras)
    {
        if (String.IsNullOrWhiteSpace(battleCameraId) || cameras == null)
            throw new InvalidOperationException("Battle camera selection requires an id and camera records.");
        var matches = cameras.Where(x => String.Equals(x.CameraId, battleCameraId, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(matches.Length == 0
                ? "Battle camera id does not resolve to exactly one camera record."
                : "Battle camera id resolves to duplicate camera records.");
        if (matches[0].Orthographic)
            throw new InvalidOperationException("Expected a perspective battle camera.");
        return matches[0];
    }

    // Compiled, no-world control seam used by the Python runner. Values are synthetic
    // and only establish selection/rejection behavior; they are not source observations.
    private static int RunCameraSelectionProbe(JObject request)
    {
        var cameras = ((JArray)request["cameras"]).Select(token =>
        {
            var row = (JObject)token;
            var euler = (JArray)row["euler"];
            return new OperationMapCameraConfig((string)row["id"], Vector3.zero,
                new Vector3((float)euler[0], (float)euler[1], (float)euler[2]),
                (bool)row["orthographic"], (float)row["field_of_view"], 0f, false);
        }).ToArray();
        var selected = SelectPerspectiveBattleCamera((string)request["battle_camera_id"], cameras);
        Console.WriteLine(new JObject
        {
            ["result"] = "Passed",
            ["selected_camera_id"] = selected.CameraId,
            ["euler"] = new JArray(selected.EulerAngles.x, selected.EulerAngles.y, selected.EulerAngles.z),
            ["field_of_view"] = selected.FieldOfView
        }.ToString());
        return 0;
    }

    private static string PinnedPath(string root, JToken pin)
    {
        string path = (string)pin["path"];
        if (!Path.IsPathRooted(path)) path = Path.Combine(root, path);
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new FileNotFoundException("Pinned input missing", path);
        string expected = (string)pin["sha256"];
        if (String.IsNullOrWhiteSpace(expected)) throw new InvalidDataException("Pinned input SHA-256 required: " + path);
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
        {
            string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (!String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Pinned input hash mismatch: " + path);
        }
        return path;
    }

    private static JObject Serialize(SkirmishMissionLogicalBindingConfig binding,
        SkirmishMissionLogicalVariant variant, SkirmishMissionViewProfile view,
        SkirmishRoleFramingRequirement[] rows)
    {
        var viewJson = new JObject();
        foreach (var field in typeof(SkirmishMissionViewProfile).GetFields(BindingFlags.Instance | BindingFlags.Public))
            viewJson[field.Name] = field.GetValue(view) == null ? JValue.CreateNull() : JToken.FromObject(field.GetValue(view));
        var framing = new JArray(rows.Select(x => new JObject
        {
            ["Role"] = (int)x.Role, ["Stages"] = (int)x.Stages, ["ObservePlayer"] = x.ObservePlayer,
            ["ObserveEnemy"] = x.ObserveEnemy, ["MinimumViewportHeight"] = x.MinimumViewportHeight,
            ["MaximumViewportDimension"] = x.MaximumViewportDimension, ["SafeEdgeFraction"] = x.SafeEdgeFraction,
            ["MinimumSubjectElevation"] = x.MinimumSubjectElevation, ["MaximumSubjectElevation"] = x.MaximumSubjectElevation,
            ["Evidence"] = (int)x.Evidence
        }));
        var v = new JObject
        {
            ["Size"] = (int)variant.Size, ["Difficulty"] = (int)variant.Difficulty, ["Areas"] = JValue.CreateNull(),
            ["ReviewedAreaLogicalFingerprint"] = JValue.CreateNull(), ["ReviewedGeometryFingerprint"] = JValue.CreateNull(),
            ["View"] = viewJson, ["OpeningFocusRole"] = (int)variant.OpeningFocusRole,
            ["OpeningFocusStableActorId"] = variant.OpeningFocusStableActorId,
            ["OpeningFocusSmoothSeconds"] = variant.OpeningFocusSmoothSeconds,
            ["SourceMinimumElevation"] = variant.SourceMinimumElevation,
            ["SourceMaximumElevation"] = variant.SourceMaximumElevation,
            ["Framing"] = framing, ["ReviewedViewFingerprint"] = variant.ReviewedViewFingerprint
        };
        return new JObject
        {
            ["SchemaVersion"] = binding.SchemaVersion, ["ContentVersion"] = binding.ContentVersion,
            ["DefinitionVersion"] = binding.DefinitionVersion, ["ValueFormat"] = (int)binding.ValueFormat,
            ["CatalogId"] = binding.CatalogId, ["DefinitionId"] = binding.DefinitionId,
            ["SourceOperationMapId"] = binding.SourceOperationMapId, ["SourceSceneGuid"] = binding.SourceSceneGuid,
            ["SourceContentHash"] = binding.SourceContentHash,
            ["SourceSemanticFingerprint"] = binding.SourceSemanticFingerprint,
            ["LayoutId"] = binding.LayoutId,
            ["ReviewedLayoutSemanticFingerprint"] = binding.ReviewedLayoutSemanticFingerprint,
            ["ObjectiveKind"] = (int)binding.ObjectiveKind, ["Variants"] = new JArray(v)
        };
    }
}
