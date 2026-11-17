#if UNITY_EDITOR
// =============================================================================
// SkirmishMissionAuthoringEntry.AuthorFromDraft
//
// One in-Editor entry point that turns a small hand-written draft into a
// registered, source-bound Skirmish Base Assault mission. Run through the
// repository wrapper (macOS) with:
//   WARLINE_MISSION_DRAFT=Tools/Missions/examples/<draft>.json \
//   Tools/CI/invoke_unity_macos.sh --timeout 900 --log <log> -- \
//     -quit -executeMethod Game.Editor.SkirmishMissionAuthoringEntry.AuthorFromDraft
// Log: "[SkirmishMissionAuthoring] result=Passed catalog=SNNN definition=skirmish.snnn
//       layout=<id> source=<opmap id>" or "result=Failed ... reason=<first failing check>".
// Exit code with -quit: 0 passed, 2 failed.
//
// DRAFT FORMAT (JSON, all coordinates are world X/Z metres of the Campaign source map):
// {
//   "catalog_id": "S006",                       // S001..S120; coded (S002-S005) and legacy
//                                               // preset ids (S001/S025/S073) are refused
//   "campaign_source": "Assets/.../OperationMap_X.asset",   // existing OperationMapDefinition
//   "anchors": { "<role>": [x, z], ... },       // required: base.player, base.enemy,
//                                               // staging.player, staging.enemy; usual:
//                                               // economy.*, service.*, air.*,
//                                               // supply.expansion.a/b and route waypoints
//   "pads":    { "<role>": [x, z] | null | {"at":[x,z],"anchor":"<anchor role>","size":[w,d]} },
//                                               // only pads NOT centred on a same-role anchor
//                                               // (spawn.infantry.*, spawn.vehicle.*, rally.*)
//                                               // or overrides; null = at the same-role anchor
//   "instance_pads": [ {"id":"s050.f1.refinery","kind":"Supply","faction":1,"at":[x,z],"size":[w,d],
//                       "anchor":"<anchor role>"}, ... ],   // optional measured per-building pads. kind is
//                                               // BaseBarracks|GroundStaging|AirReturn|Supply|Service, faction 1|2,
//                                               // the anchor role must sit exactly on "at". For every size x difficulty each
//                                               // starting structure takes the first unused entry of its faction and kind
//                                               // (draft order), else a pad derived at its compiled position (needs
//                                               // "structure_footprints"); only pads some variant uses are written.
//   "structure_footprints": { "<structure id>": [w, d] },   // optional Unity-verified footprints (metres) for derived pads
//   "routes":  [ {"kind":"MainHighway|FlankNorthRuins|FlankSouthSweep",
//                 "via":["staging.player", ..., "staging.enemy"],
//                 "width":12, "vehicles":true, "aircraft":true}, ... ],  // >=2 distinct, 1 main
//   "objective_areas": { "<FrontlineA|FrontlineB|FrontlineC|BreakthroughCorridorA|BreakthroughCorridorB|
//                          BreakthroughExit|ConvoyDestination>": {"rectangle":[[minX,minZ],[maxX,maxZ]]} |
//                          {"polygon":[[x,z],...]} },   // FC needs FrontlineA/B/C, BT the two corridors + exit,
//                                               // CE the destination; Base Assault takes none (world X/Z metres)
//   "area_measurement": {"artifact":"<repo-relative file>"},   // FC/BT/CE: the native-measurement file; the entry
//                                               // binds its SHA-256 (SkirmishObjectiveAreaCompiler requires measured proof)
//   "notes": "free text, ignored"
// }
// FC/BT/CE also need (anchors with a pads{} entry that states "size": measured, never defaulted):
//   BT, CE: "defense.approach.a" and "defense.approach.b" (faction 2 enemy approach towers);
//   CE:     "convoy.origin.1", ".2", ".3" (faction 1 truck bays; roles convoy.1/.2/.3 are derived) and at least two
//           vehicle routes whose middle waypoints differ. CE needs the pinned convoy logical assets
//           (SkirmishConvoyLogicalAssetsAuthoringEntry.EnsurePinnedLogicalAssets) first.
// A per-scenario SkirmishObjective_<id>.asset (kind + the matrix deadlines) is created for FC/BT/CE.
// DERIVED, never typed: objective/army/start (packaged InitialSetupMatrix row), recommended
// size (Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv when present), Shared profile
// assets, feature ids, anchor/pad/route ids, pad kind/faction/default size, the layout
// envelope and orthonormal world frame (player base -> enemy base), normalized anchors,
// measured deployments, route lengths and provisional transit times, cell size, the source
// pin (ContentHash / SourceBinding hash), layout content hash, every source/layout/view
// fingerprint, the engineering camera view (SkirmishEngineeringViewProfileFactory), role
// framing rows and opening focus actor, and one logical variant per compiling
// size x difficulty. Nothing here certifies native source fit, camera pictures, normal-input
// play or publication; the publication row stays InProgress and the library row Planned.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishMissionAuthoringEntry
    {
        private const string Tag = "[SkirmishMissionAuthoring]";
        private const string ScenarioRoot = "Assets/Game/Configs/SkirmishExpansion/Scenarios/";
        private const string SharedRoot = "Assets/Game/Configs/SkirmishExpansion/Shared";
        private const string CatalogPath = "Assets/Game/Resources/SkirmishExpansionCatalog.asset";
        private const string PublicationPath = SharedRoot + "/SkirmishPublicationManifest.asset";
        private const string ScenarioCatalogCsv = "Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv";
        private const float FrameMarginMetres = 32f;
        private const int AuthoringSeed = 1;
        private static readonly string[] DraftKeys = { "catalog_id", "campaign_source", "anchors", "pads", "routes", "notes", "instance_pads", "structure_footprints", "objective_areas", "area_measurement" };
        private static readonly SkirmishSizeId[] Sizes = { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };
        private static readonly SkirmishDifficultyId[] Difficulties =
            { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander };

        private sealed class AuthoringFailure : Exception
        { public AuthoringFailure(string check, string detail) : base(check + ": " + detail) { } }

        private sealed class Outcome { public string CatalogId = "unknown", DefinitionId, LayoutId, SourceId; public int Variants; }

        private sealed class Compiled
        {
            public SkirmishSizeId Size; public SkirmishDifficultyId Difficulty;
            public SkirmishResolvedSetup Setup; public SkirmishMissionLogicalAuthoringInputs Inputs;
        }

        public static void AuthorFromDraft()
        {
            bool quit = Environment.GetCommandLineArgs().Contains("-quit");
            bool passed = AuthorDraft(Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT"), out _);
            if (quit) EditorApplication.Exit(passed ? 0 : 2);
        }

        public static void AuthorFromDraftList()
        {
            bool quit = Environment.GetCommandLineArgs().Contains("-quit");
            string[] drafts;
            try
            {
                string listArgument = Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT_LIST");
                if (string.IsNullOrWhiteSpace(listArgument))
                    throw new AuthoringFailure("list", "WARLINE_MISSION_DRAFT_LIST is not set.");
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string listPath = Path.GetFullPath(Path.IsPathRooted(listArgument) ? listArgument : Path.Combine(project, listArgument));
                if (!File.Exists(listPath)) throw new AuthoringFailure("list", "File not found: " + listPath);
                drafts = File.ReadAllLines(listPath, Encoding.UTF8).Select(line => line.Trim())
                    .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal)).ToArray();
                if (drafts.Length == 0) throw new AuthoringFailure("list", "No draft paths in: " + listPath);
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishMissionAuthoringBatch] result=Failed reason=" + error.Message);
                if (quit) EditorApplication.Exit(2);
                return;
            }

            int passedCount = 0;
            for (int i = 0; i < drafts.Length; i++)
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                bool passed = AuthorDraft(drafts[i], out var outcome);
                timer.Stop();
                if (passed) passedCount++;
                Debug.Log("[SkirmishMissionAuthoringBatch] draft=" + (i + 1) + "/" + drafts.Length + " path=" + drafts[i] +
                    " catalog=" + outcome.CatalogId + " result=" + (passed ? "Passed" : "Failed") +
                    " seconds=" + timer.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
            }
            int failedCount = drafts.Length - passedCount;
            Debug.Log("[SkirmishMissionAuthoringBatch] result=Completed drafts=" + drafts.Length + " passed=" + passedCount + " failed=" + failedCount);
            if (quit) EditorApplication.Exit(failedCount == 0 ? 0 : 2);
        }

        private static bool AuthorDraft(string draftArgument, out Outcome outcome)
        {
            outcome = new Outcome();
            bool passed = false;
            try
            {
                Author(draftArgument, outcome);
                passed = true;
                Debug.Log(Tag + " result=Passed catalog=" + outcome.CatalogId + " definition=" + outcome.DefinitionId +
                    " layout=" + outcome.LayoutId + " source=" + outcome.SourceId + " variants=" + outcome.Variants +
                    " publication=InProgress library=Planned readiness=0");
            }
            catch (AuthoringFailure failure)
            {
                Debug.LogError(Tag + " result=Failed catalog=" + outcome.CatalogId + " reason=" + failure.Message);
            }
            catch (Exception error)
            {
                Debug.LogError(Tag + " result=Failed catalog=" + outcome.CatalogId + " reason=exception: " + error.GetType().Name + ": " + error.Message);
                Debug.LogException(error);
            }
            return passed;
        }

        private static void Author(string draftArgument, Outcome outcome)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new AuthoringFailure("editor", "Idle Editor (not Play Mode) required.");
            if (string.IsNullOrWhiteSpace(draftArgument)) throw new AuthoringFailure("draft", "WARLINE_MISSION_DRAFT is not set.");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string draftPath = Path.GetFullPath(Path.IsPathRooted(draftArgument) ? draftArgument : Path.Combine(project, draftArgument));
            if (!File.Exists(draftPath)) throw new AuthoringFailure("draft", "File not found: " + draftPath);
            JObject draft;
            try { draft = JObject.Parse(File.ReadAllText(draftPath)); }
            catch (Exception error) { throw new AuthoringFailure("draft", "Invalid JSON: " + error.Message); }
            foreach (var property in draft.Properties())
                if (!DraftKeys.Contains(property.Name)) throw new AuthoringFailure("draft", "Unknown key '" + property.Name + "'.");

            // --- identity -----------------------------------------------------------------
            string catalogId = (string)draft["catalog_id"];
            if (!SkirmishMissionDispatch.Canonical(catalogId)) throw new AuthoringFailure("catalog_id", "Canonical S001..S120 id required.");
            outcome.CatalogId = catalogId;
            if (SkirmishMissionDispatch.IsCodedExpanded(catalogId) || SkirmishMissionDispatch.TryGetLegacyIndex(catalogId, out _))
                throw new AuthoringFailure("catalog_id", catalogId + " keeps its coded/legacy-preset binding (CAMPAIGN_MAP_REUSE_POLICY); " +
                    "SkirmishGenericMissionAvailabilityScope also refuses legacy-index ids.");

            // --- tuple (objective/army/start) from the packaged matrix ------------------------
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError)) throw new AuthoringFailure("matrix", matrixError);
            if (!SkirmishSetupMatrixTable.TryFind(matrix, catalogId, SkirmishSizeId.Standard, out var tuple))
                throw new AuthoringFailure("matrix", "No Standard row for " + catalogId + ".");
            // Typed areas, defense/convoy pads and measured evidence for FC/BT/CE; null for Base Assault.
            var areaDraft = ObjectiveDraft.Read(draft, tuple.ObjectiveKind, catalogId, project);

            // --- Campaign source -------------------------------------------------------------
            string sourcePath = (string)draft["campaign_source"];
            var source = string.IsNullOrEmpty(sourcePath) ? null : AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            if (source == null) throw new AuthoringFailure("campaign_source", "No OperationMapDefinition at '" + sourcePath + "'.");
            if (!source.TryValidateMetadata(out string sourceError) || !source.TryValidateLocalContentReferences(out sourceError))
                throw new AuthoringFailure("campaign_source", sourceError);
            if (source.SourceSceneReference == null || string.IsNullOrEmpty(source.SourceSceneReference.AssetGUID))
                throw new AuthoringFailure("campaign_source", "Source scene reference missing.");
            outcome.SourceId = source.OperationMapId;

            // --- Shared profiles -------------------------------------------------------------
            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishExpansionCatalogConfig>(CatalogPath);
            if (catalog == null) throw new AuthoringFailure("catalog", "Missing " + CatalogPath);
            if (catalog.TryGetDefinition(catalogId, out var registered) && registered != null &&
                AssetDatabase.GetAssetPath(registered) != ScenarioRoot + catalogId + "/SkirmishScenario_" + catalogId + ".asset")
                throw new AuthoringFailure("catalog", "A different definition is already registered: " + AssetDatabase.GetAssetPath(registered));
            var catalogObject = new SerializedObject(catalog);
            var economy = CatalogReference<SkirmishEconomyConfig>(catalogObject, "economy");
            var upgrades = CatalogReference<SkirmishUpgradeConfig>(catalogObject, "upgrades");
            var intel = CatalogReference<SkirmishIntelConfig>(catalogObject, "intel");
            var roles = CatalogReference<SkirmishRoleCatalogConfig>(catalogObject, "roles");
            // Base Assault keeps its Shared objective; FC/BT/CE get a per-scenario one once the folder exists.
            var objective = tuple.ObjectiveKind == SkirmishObjectiveKind.BaseAssault
                ? SharedByKind<SkirmishObjectiveConfig>(x => x.Kind == tuple.ObjectiveKind, "objective " + tuple.ObjectiveKind) : null;
            if (tuple.ObjectiveKind == SkirmishObjectiveKind.ConvoyEscort) roles = ConvoyRoleCatalog();
            var army = SharedByKind<SkirmishArmyProfileConfig>(x => x.Kind == tuple.ArmyProfile, "army " + tuple.ArmyProfile);
            var start = SharedByKind<SkirmishStartPackageConfig>(x => x.Kind == tuple.StartProfile, "start " + tuple.StartProfile);
            string[] features = tuple.ArmyProfile == SkirmishArmyProfileId.AirMobile ? (string[])SkirmishS003FirstVisit.RequiredFeatureIds.Clone() :
                tuple.ArmyProfile == SkirmishArmyProfileId.CombinedArms ? (string[])SkirmishS005FirstVisit.RequiredFeatureIds.Clone() : null;
            SkirmishSizeId recommended = ReadRecommendedSize(project, catalogId);

            // --- layout ----------------------------------------------------------------------
            string folder = ScenarioRoot + catalogId;
            EnsureFolder(folder);
            if (objective == null) objective = ScenarioObjective(catalogId, tuple.ObjectiveKind, matrix, folder);
            string layoutPath = folder + "/SkirmishLayout_" + catalogId + ".asset";
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            bool newLayout = layout == null;
            if (newLayout) layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            LayoutGeometry.Apply(draft, source, catalogId, layout, null);
            if (newLayout) AssetDatabase.CreateAsset(layout, layoutPath); else EditorUtility.SetDirty(layout);
            outcome.LayoutId = layout.LayoutId;

            // --- definition ------------------------------------------------------------------
            string definitionPath = folder + "/SkirmishScenario_" + catalogId + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath);
            bool newDefinition = definition == null;
            if (newDefinition) definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
            definition.Configure(catalogId, objective, army, start, economy, upgrades, intel, roles, layout, recommended, features);
            definition.BindCampaignMap(source);
            definition.BindMissionLogicalBinding(null);
            if (newDefinition) AssetDatabase.CreateAsset(definition, definitionPath); else EditorUtility.SetDirty(definition);
            outcome.DefinitionId = definition.DefinitionId;

            // --- per-building structure pads --------------------------------------------------
            // Generic missions require one exact StructureInstancePads row per starting structure
            // (SkirmishStartingBuildingsService / SkirmishStartingGrantPlacementConstraint.TryResolvePad).
            // Compile every variant against the pad-less layout, plan the rows, then re-apply the layout.
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            var plannedSizes = new HashSet<SkirmishSizeId>();
            {
                var measuredPads = LayoutGeometry.ReadMeasured(draft, catalogId);
                var plannerVariants = new List<InstancePadVariant>();
                foreach (var size in Sizes)
                {
                    var sizeRows = new List<InstancePadVariant>();
                    foreach (var difficulty in Difficulties)
                    {
                        if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size, AuthoringSeed, manifest, matrix, out var plain, out _) ||
                            plain?.Structures == null) { sizeRows = null; break; }
                        sizeRows.Add(new InstancePadVariant { Size = size, Difficulty = difficulty, Structures = (SkirmishResolvedStructureEntry[])plain.Structures.Clone() });
                    }
                    if (sizeRows == null) continue;   // the compile loop below reports or skips this size
                    plannerVariants.AddRange(sizeRows); plannedSizes.Add(size);
                }
                if (plannerVariants.Count > 0)
                {
                    InstancePadPlan plan;
                    try
                    {
                        plan = SkirmishInstancePadPlanner.Plan(plannerVariants, measuredPads,
                            (string structureId, out float width, out float depth) => LayoutGeometry.TryFootprint(draft, structureId, out width, out depth));
                    }
                    catch (InstancePadPlanException failure) { throw new AuthoringFailure("instancePads", failure.Message); }
                    LayoutGeometry.Apply(draft, source, catalogId, layout, plan);
                    EditorUtility.SetDirty(layout);
                    int measuredRows = plan.Rows.Count(row => plan.Pads.First(pad => pad.Key == row.PadKey).Measured);
                    Debug.Log(Tag + " instancePads catalog=" + catalogId + " rows=" + plan.Rows.Count + " measured=" + measuredRows +
                        " derived=" + (plan.Rows.Count - measuredRows) + " pads=" + plan.Pads.Count + " unusedMeasuredPads=" + plan.UnusedMeasured +
                        " variants=" + plannerVariants.Count);
                }
            }

            // --- compile every size x difficulty without a binding; capture logical inputs ----
            var compiled = new List<Compiled>();
            var skippedSizes = new Dictionary<SkirmishSizeId, string>();
            foreach (var size in Sizes)
            {
                var rows = new List<Compiled>();
                string sizeFailure = null;
                foreach (var difficulty in Difficulties)
                {
                    if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size, AuthoringSeed, manifest, matrix, out var setup, out var reasons))
                    { sizeFailure = "compile." + size + "." + difficulty + ": " + Describe(reasons); break; }
                    if (!SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition, difficulty, size, AuthoringSeed, manifest, matrix,
                        out var inputs, out var captureReasons) || inputs == null)
                    { sizeFailure = "inputs." + size + "." + difficulty + ": " + Describe(captureReasons); break; }
                    rows.Add(new Compiled { Size = size, Difficulty = difficulty, Setup = setup, Inputs = inputs });
                }
                if (sizeFailure == null) { compiled.AddRange(rows); continue; }
                if (size == SkirmishSizeId.Standard) throw new AuthoringFailure(sizeFailure.Substring(0, sizeFailure.IndexOf(':')), sizeFailure.Substring(sizeFailure.IndexOf(':') + 2));
                skippedSizes[size] = sizeFailure;
                Debug.LogWarning(Tag + " size=" + size + " skipped (no logical variant) first=" + sizeFailure);
            }
            foreach (var size in plannedSizes)
                if (skippedSizes.TryGetValue(size, out string withRows))
                    throw new AuthoringFailure("instancePads", size + " compiled without per-building pads but not with them: " + withRows);
            if (!compiled.Any(x => x.Size == recommended))
            {
                Debug.LogWarning(Tag + " recommendedSize=" + recommended + " has no compiling variant; using Standard.");
                definition.Configure(catalogId, objective, army, start, economy, upgrades, intel, roles, layout, SkirmishSizeId.Standard, features);
                EditorUtility.SetDirty(definition);
            }
            var primary = compiled.First(x => x.Size == SkirmishSizeId.Standard && x.Difficulty == SkirmishDifficultyId.Regular);

            // --- view + variants (Tools/CI/SkirmishMissionLogicalBindingAuthor.cs rules) -----
            var view = SkirmishEngineeringViewProfileFactory.Create(definition, primary.Inputs);
            view.name = "SkirmishMissionViewProfile_" + catalogId;
            if (!view.IsValid || !view.Matches(primary.Setup, definition))
                throw new AuthoringFailure("view", "Engineering view profile is invalid or does not match the compiled setup.");
            var variants = compiled.Select(x => BuildVariant(x, view, areaDraft)).ToArray();

            var binding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            binding.name = "SkirmishMissionLogicalBinding_" + catalogId;
            binding.SchemaVersion = 1; binding.ContentVersion = 1;
            binding.DefinitionVersion = definition.ContentVersion;
            binding.ValueFormat = primary.Inputs.ValueFormat;
            binding.CatalogId = definition.CatalogId; binding.DefinitionId = definition.DefinitionId;
            binding.SourceOperationMapId = source.OperationMapId;
            binding.SourceSceneGuid = source.SourceSceneReference.AssetGUID;
            binding.SourceContentHash = source.ContentHash;
            binding.SourceSemanticFingerprint = primary.Inputs.SourceSemanticFingerprint;
            binding.LayoutId = layout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint = primary.Inputs.LayoutSemanticFingerprint;
            binding.ObjectiveKind = primary.Setup.ObjectiveKind;
            binding.Variants = variants;
            foreach (var row in compiled)
                if (row.Inputs.SourceSemanticFingerprint != binding.SourceSemanticFingerprint ||
                    row.Inputs.LayoutSemanticFingerprint != binding.ReviewedLayoutSemanticFingerprint || row.Inputs.ValueFormat != binding.ValueFormat)
                    throw new AuthoringFailure("fingerprints", "Source/layout fingerprints differ across " + row.Size + "/" + row.Difficulty + ".");

            string logicalFolder = folder + "/Logical";
            EnsureFolder(logicalFolder);
            string bindingPath = logicalFolder + "/SkirmishMissionLogicalBinding_" + catalogId + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(bindingPath) != null && !AssetDatabase.DeleteAsset(bindingPath))
                throw new AuthoringFailure("binding", "Could not replace " + bindingPath);
            SkirmishMissionLogicalBindingAuthoring.CreateExplicitBindingAsset(definition, binding, bindingPath,
                binding.SourceSemanticFingerprint, binding.ReviewedLayoutSemanticFingerprint);
            AssetDatabase.AddObjectToAsset(view, binding);
            definition.BindMissionLogicalBinding(binding);
            EditorUtility.SetDirty(binding); EditorUtility.SetDirty(definition);
            outcome.Variants = variants.Length;

            // --- registration ----------------------------------------------------------------
            Register(catalogObject, definition);
            KeepPublicationInProgress(catalogId, definition.DefinitionId);

            // --- final fail-closed validation (Standard/Regular, with binding) ----------------
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, AuthoringSeed, manifest, matrix,
                out var final, out var finalReasons))
                throw new AuthoringFailure("final-compile", Describe(finalReasons));
            if (string.IsNullOrEmpty(final.MissionLogicalBindingFingerprint))
                throw new AuthoringFailure("final-compile", "Compiled setup carries no mission logical binding fingerprint.");
            if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition, final, out var compiledBinding, out string bindingError))
                throw new AuthoringFailure("logical-binding", bindingError);
            if (!catalog.TryGetDefinition(catalogId, definition.ContentVersion, out var resolved) || resolved != definition)
                throw new AuthoringFailure("catalog", "Registered definition does not resolve by id/version.");
            AssetDatabase.SaveAssets();
            if (!AssetDatabase.Contains(view) || !AssetDatabase.Contains(binding) || !AssetDatabase.Contains(definition) || !AssetDatabase.Contains(layout))
                throw new AuthoringFailure("save", "Authored assets were not persisted.");
            Debug.Log(Tag + " compiled setup=" + final.SetupHash.ToString("X8", CultureInfo.InvariantCulture) +
                " logical=" + compiledBinding.Fingerprint + " layoutFingerprint=" + binding.ReviewedLayoutSemanticFingerprint +
                " sourceFingerprint=" + binding.SourceSemanticFingerprint + " binding=" + bindingPath);
        }

        // Framing rows, opening focus and pins follow Tools/CI/SkirmishMissionLogicalBindingAuthor.cs:89-133.
        private static SkirmishMissionLogicalVariant BuildVariant(Compiled row, SkirmishMissionViewProfile view, AreaDraft areaDraft)
        {
            var setup = row.Setup; var inputs = row.Inputs;
            var framing = inputs.RequiredFramingRoles.Select(role =>
            {
                bool aircraft = role == SkirmishRoleKind.TransportHeli || role == SkirmishRoleKind.TransportPlane ||
                    role == SkirmishRoleKind.Drone || SkirmishRoleIds.Category(role) == SkirmishPopulationCategory.Air;
                bool enemy = setup.Forces.Any(x => x.FactionId == 2 && x.Quantity > 0 && x.RoleKind == role);
                var stages = SkirmishViewCaptureStage.Opening | SkirmishViewCaptureStage.MinimumZoom |
                    SkirmishViewCaptureStage.MaximumZoom | SkirmishViewCaptureStage.MatchExitRestored;
                if (aircraft) stages |= SkirmishViewCaptureStage.FollowParked | SkirmishViewCaptureStage.FollowCruising |
                    SkirmishViewCaptureStage.ManualViewRestored;
                return new SkirmishRoleFramingRequirement
                {
                    Role = role, Stages = stages, ObservePlayer = true, ObserveEnemy = enemy,
                    MinimumViewportHeight = aircraft ? .04f : .025f, MaximumViewportDimension = .75f, SafeEdgeFraction = .025f,
                    MinimumSubjectElevation = inputs.SourceMinimumElevation, MaximumSubjectElevation = inputs.SourceMaximumElevation,
                    Evidence = SkirmishViewEvidenceRequirement.NativePng | SkirmishViewEvidenceRequirement.ActualMeshViewportBounds |
                        SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot | SkirmishViewEvidenceRequirement.ObstructionCandidateInventory
                };
            }).OrderBy(x => (byte)x.Role).ToArray();
            string focusId = null; var focusRole = SkirmishRoleKind.None;
            foreach (var role in inputs.RequiredFramingRoles.OrderBy(x => (byte)x))
            {
                for (int i = 0; i < setup.Forces.Length && focusId == null; i++)
                {
                    var force = setup.Forces[i];
                    if (force.FactionId != 1 || force.Quantity <= 0 || force.RoleKind != role) continue;
                    string candidate = force.RoleId + ".1." + i.ToString(CultureInfo.InvariantCulture) + ".0";
                    if (inputs.ActualOpeningPlayerActorIds.Contains(candidate)) { focusId = candidate; focusRole = role; }
                }
                if (focusId != null) break;
            }
            if (focusId == null) throw new AuthoringFailure("opening-focus", "No opening player actor for a required framing role (" + row.Size + "/" + row.Difficulty + ").");
            var variant = new SkirmishMissionLogicalVariant
            {
                Size = setup.SizeId, Difficulty = setup.DifficultyId, Areas = null, View = view,
                OpeningFocusRole = focusRole, OpeningFocusStableActorId = focusId, OpeningFocusSmoothSeconds = 2.25f,
                SourceMinimumElevation = inputs.SourceMinimumElevation, SourceMaximumElevation = inputs.SourceMaximumElevation,
                Framing = framing
            };
            if (areaDraft != null)
            {
                // FC/BT/CE: typed areas pinned to this variant's actual source/layout binding.
                var areas = ObjectiveDraft.Compile(areaDraft, row, out var areaConfig);
                variant.Areas = areaConfig;
                variant.ReviewedAreaLogicalFingerprint = areas.LogicalFingerprint;
                variant.ReviewedGeometryFingerprint = areas.GeometryFingerprint;
            }
            variant.ReviewedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            if (!SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant, setup, out string error))
                throw new AuthoringFailure("framing." + row.Size + "." + row.Difficulty, error);
            return variant;
        }

        // Per-scenario objective config for FC/BT/CE: kind from the matrix tuple, deadlines copied from the
        // packaged matrix rows (the compiler compares them), nothing invented. Re-runs update the same asset.
        private static SkirmishObjectiveConfig ScenarioObjective(string catalogId, SkirmishObjectiveKind kind,
            List<SkirmishSetupMatrixRow> matrix, string folder)
        {
            string path = folder + "/SkirmishObjective_" + catalogId + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SkirmishObjectiveConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("objectiveId").stringValue = SkirmishObjectiveIds.ToCode(kind);
            serialized.FindProperty("kind").intValue = (int)kind;
            foreach (var pair in new[] { (SkirmishSizeId.Standard, "standardDeadlineSeconds"), (SkirmishSizeId.War, "warDeadlineSeconds"),
                (SkirmishSizeId.LargeWar, "largeWarDeadlineSeconds") })
            {
                if (!SkirmishSetupMatrixTable.TryFind(matrix, catalogId, pair.Item1, out var row))
                    throw new AuthoringFailure("matrix", "No " + pair.Item1 + " row for " + catalogId + " (objective deadline).");
                serialized.FindProperty(pair.Item2).intValue = row.DeadlineSeconds;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // CE uses the explicit role catalog that adds the mode-owned Cargo Truck role; the definition must be pinned first.
        private static SkirmishRoleCatalogConfig ConvoyRoleCatalog()
        {
            var truck = SkirmishConvoyTruckDefinition.Load();
            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(SkirmishConvoyTruckAuthoring.RoleCatalogPath);
            if (truck == null || !truck.IsValid || catalog == null)
                throw new AuthoringFailure("convoy", "Pinned convoy logical assets missing; run SkirmishConvoyLogicalAssetsAuthoringEntry.EnsurePinnedLogicalAssets first.");
            return catalog;
        }

        private static void Register(SerializedObject catalogObject, SkirmishScenarioDefinitionConfig definition)
        {
            catalogObject.Update();
            var list = catalogObject.FindProperty("definitions");
            if (list == null || !list.isArray) throw new AuthoringFailure("catalog", "definitions[] not found.");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == definition) return;
            int index = list.arraySize;
            list.arraySize = index + 1;
            list.GetArrayElementAtIndex(index).objectReferenceValue = definition;
            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalogObject.targetObject);
        }

        private static void KeepPublicationInProgress(string catalogId, string definitionId)
        {
            var publication = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(PublicationPath);
            if (publication == null) throw new AuthoringFailure("publication", "Missing " + PublicationPath);
            var serialized = new SerializedObject(publication);
            var rows = serialized.FindProperty("rows");
            if (rows == null || !rows.isArray) throw new AuthoringFailure("publication", "rows[] not found.");
            SerializedProperty row = null;
            for (int i = 0; i < rows.arraySize; i++)
                if (rows.GetArrayElementAtIndex(i).FindPropertyRelative("CatalogId").stringValue == catalogId) { row = rows.GetArrayElementAtIndex(i); break; }
            if (row == null)
            {
                rows.arraySize++;
                row = rows.GetArrayElementAtIndex(rows.arraySize - 1);
                row.FindPropertyRelative("CatalogId").stringValue = catalogId;
                row.FindPropertyRelative("EvidencePath").stringValue = "Design/AgentReports/SkirmishExpansion/" + catalogId + "/";
                row.FindPropertyRelative("Notes").stringValue = "Source-bound candidate authored by SkirmishMissionAuthoringEntry. " +
                    "Native source fit, normal-input/ARIA journeys, result/return and device acceptance required; asset existence does not set Playable.";
                row.FindPropertyRelative("Status").intValue = (int)SkirmishPublicationStatus.InProgress;
            }
            else
            {
                int status = row.FindPropertyRelative("Status").intValue;
                if (status == (int)SkirmishPublicationStatus.Planned) row.FindPropertyRelative("Status").intValue = (int)SkirmishPublicationStatus.InProgress;
                else if (status != (int)SkirmishPublicationStatus.InProgress)
                    Debug.LogWarning(Tag + " publication row " + catalogId + " keeps existing status " + (SkirmishPublicationStatus)status + ".");
            }
            row.FindPropertyRelative("DefinitionId").stringValue = definitionId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(publication);
        }

        private static T CatalogReference<T>(SerializedObject catalog, string field) where T : UnityEngine.Object
        {
            var value = catalog.FindProperty(field)?.objectReferenceValue as T;
            if (value == null) throw new AuthoringFailure("profiles", "Catalog field '" + field + "' is not a " + typeof(T).Name + ".");
            return value;
        }

        private static T SharedByKind<T>(Func<T, bool> match, string what) where T : ScriptableObject
        {
            var found = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { SharedRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(x => x != null && match(x)).Distinct().ToArray();
            if (found.Length != 1) throw new AuthoringFailure("profiles", "Expected exactly one Shared " + what + " asset, found " + found.Length + ".");
            return found[0];
        }

        private static SkirmishSizeId ReadRecommendedSize(string project, string catalogId)
        {
            string path = Path.Combine(project, ScenarioCatalogCsv);
            if (!File.Exists(path)) return SkirmishSizeId.Standard;
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) return SkirmishSizeId.Standard;
            int column = Array.IndexOf(lines[0].Split(','), "recommended_size");
            foreach (string line in lines.Skip(1))
            {
                var cells = line.Split(',');
                if (column < 0 || cells.Length <= column || cells[0] != catalogId) continue;
                return Enum.TryParse(cells[column].Trim(), false, out SkirmishSizeId size) && size != SkirmishSizeId.None ? size : SkirmishSizeId.Standard;
            }
            return SkirmishSizeId.Standard;
        }

        private static string Describe(List<SkirmishCompileReason> reasons) => reasons == null || reasons.Count == 0 ? "rejected without reason" :
            reasons[0].Code + ":" + reasons[0].Field + ":" + reasons[0].Detail + (reasons.Count > 1 ? " (+" + (reasons.Count - 1) + " more)" : "");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private sealed class AreaDraft
        {
            public SkirmishObjectiveKind Kind;
            public SkirmishObjectiveArea[] Areas;
            public string Artifact, ArtifactSha256;
        }

        // Objective-specific draft rules for FC/BT/CE. Base Assault passes through untouched (Read returns null).
        // Geometry validity and playable-bounds containment are enforced by SkirmishObjectiveAreaCompiler per variant.
        private static class ObjectiveDraft
        {
            private static readonly string[] ConvoyRoles = { "convoy.origin.1", "convoy.origin.2", "convoy.origin.3" };
            private static readonly string[] DefenseRoles = { "defense.approach.a", "defense.approach.b" };

            // Same exact sets as SkirmishObjectiveAreaCompiler.RequiredRoles (private there).
            private static SkirmishObjectiveAreaRole[] RequiredAreaRoles(SkirmishObjectiveKind kind)
            {
                switch (kind)
                {
                    case SkirmishObjectiveKind.FrontlineControl:
                        return new[] { SkirmishObjectiveAreaRole.FrontlineA, SkirmishObjectiveAreaRole.FrontlineB, SkirmishObjectiveAreaRole.FrontlineC };
                    case SkirmishObjectiveKind.Breakthrough:
                        return new[] { SkirmishObjectiveAreaRole.BreakthroughCorridorA, SkirmishObjectiveAreaRole.BreakthroughCorridorB, SkirmishObjectiveAreaRole.BreakthroughExit };
                    case SkirmishObjectiveKind.ConvoyEscort:
                        return new[] { SkirmishObjectiveAreaRole.ConvoyDestination };
                    default: return Array.Empty<SkirmishObjectiveAreaRole>();
                }
            }

            public static AreaDraft Read(JObject draft, SkirmishObjectiveKind kind, string catalogId, string project)
            {
                var required = RequiredAreaRoles(kind);
                var anchors = draft["anchors"] as JObject;
                var pads = draft["pads"] as JObject;
                if (required.Length == 0 && kind != SkirmishObjectiveKind.BaseAssault)
                    throw new AuthoringFailure("objective", "Unsupported objective " + kind + ".");
                CheckObjectivePads(anchors, pads, DefenseRoles, kind == SkirmishObjectiveKind.Breakthrough || kind == SkirmishObjectiveKind.ConvoyEscort, "BT/CE");
                CheckObjectivePads(anchors, pads, ConvoyRoles, kind == SkirmishObjectiveKind.ConvoyEscort, "CE");
                if (required.Length == 0)
                {
                    if (draft["objective_areas"] != null || draft["area_measurement"] != null)
                        throw new AuthoringFailure("draft.objective_areas", "objective_areas/area_measurement apply to FC/BT/CE drafts, not Base Assault.");
                    return null;
                }

                var raw = draft["objective_areas"] as JObject ?? throw new AuthoringFailure("draft.objective_areas",
                    kind + " needs objective_areas{} with " + string.Join(", ", required) + ".");
                foreach (var property in raw.Properties())
                    if (!Enum.IsDefined(typeof(SkirmishObjectiveAreaRole), property.Name) ||
                        Array.IndexOf(required, (SkirmishObjectiveAreaRole)Enum.Parse(typeof(SkirmishObjectiveAreaRole), property.Name)) < 0)
                        throw new AuthoringFailure("draft.objective_areas", "Role '" + property.Name + "' is not one of " + string.Join(", ", required) + " for " + kind + ".");
                var areas = new List<SkirmishObjectiveArea>();
                foreach (var role in required)
                {
                    string label = "objective_areas." + role;
                    var spec = raw[role.ToString()] as JObject ?? throw new AuthoringFailure("draft.objective_areas", "Missing area " + role + ".");
                    if (spec.Count != 1) throw new AuthoringFailure("draft.objective_areas", label + " needs exactly one of rectangle or polygon.");
                    SkirmishObjectiveAreaShape shape;
                    JToken token;
                    if (spec["rectangle"] != null) { shape = SkirmishObjectiveAreaShape.AxisAlignedRectangle; token = spec["rectangle"]; }
                    else if (spec["polygon"] != null) { shape = SkirmishObjectiveAreaShape.Polygon; token = spec["polygon"]; }
                    else throw new AuthoringFailure("draft.objective_areas", label + " needs rectangle or polygon.");
                    var array = token as JArray ?? throw new AuthoringFailure("draft.objective_areas", label + " needs an array of [x, z] points.");
                    var points = new SkirmishObjectiveAreaPoint[array.Count];
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector2 value = LayoutGeometry.Point(array[i], label + "[" + i.ToString(CultureInfo.InvariantCulture) + "]");
                        points[i] = new SkirmishObjectiveAreaPoint(value.x, value.y);
                    }
                    areas.Add(new SkirmishObjectiveArea
                    {
                        StableAreaId = "area.skirmish." + catalogId.ToLowerInvariant() + "." + role.ToString().ToLowerInvariant(),
                        Role = role, Shape = shape, Points = points
                    });
                }

                // The measured artifact is evidence from a native run; it must exist and its bytes are pinned.
                var evidence = draft["area_measurement"] as JObject;
                string artifact = evidence != null && evidence.Count == 1 ? (string)evidence["artifact"] : null;
                if (string.IsNullOrWhiteSpace(artifact) || Path.IsPathRooted(artifact) || artifact.Split('/', '\\').Contains(".."))
                    throw new AuthoringFailure("draft.area_measurement", "area_measurement {\"artifact\": \"<repo-relative measurement file>\"} required for " + kind + ".");
                string full = Path.GetFullPath(Path.Combine(project, artifact));
                if (!File.Exists(full)) throw new AuthoringFailure("draft.area_measurement", "Measurement file not found: " + artifact);
                string sha;
                using (var hash = SHA256.Create()) sha = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant();
                return new AreaDraft { Kind = kind, Areas = areas.ToArray(), Artifact = artifact.Replace('\\', '/'), ArtifactSha256 = sha };
            }

            // The binder accepts defense approaches and truck bays only as exact mode-owned roles; each needs an anchor and
            // a pads{} entry with an explicit measured size (no default is guessed), and is refused where the mode forbids it.
            private static void CheckObjectivePads(JObject anchors, JObject pads, string[] roles, bool allowed, string modes)
            {
                foreach (string role in roles)
                {
                    bool present = anchors != null && anchors[role] != null;
                    if (present && !allowed) throw new AuthoringFailure("draft.anchors", role + " is only valid for " + modes + " drafts.");
                    if (!allowed) continue;
                    if (!present) throw new AuthoringFailure("draft.anchors", "Missing required role " + role + ".");
                    if (!(pads?[role] is JObject spec) || spec["size"] == null)
                        throw new AuthoringFailure("draft.pads", role + " needs a pads{} entry with an explicit \"size\": [w, d] (measured).");
                }
            }

            public static SkirmishCompiledObjectiveAreas Compile(AreaDraft draft, Compiled row, out SkirmishObjectiveAreaConfig config)
            {
                if (!row.Inputs.HasActualAreaBinding)
                    throw new AuthoringFailure("areas." + row.Size + "." + row.Difficulty, "Layout binding format carries no typed-area binding.");
                config = new SkirmishObjectiveAreaConfig
                {
                    AreaSetId = ("areas." + row.Setup.DefinitionId + "." + row.Size + "." + row.Difficulty).ToLowerInvariant(),
                    ObjectiveKind = draft.Kind, Binding = row.Inputs.ActualAreaBinding,
                    Areas = draft.Areas.Select(area => { area.Points = (SkirmishObjectiveAreaPoint[])area.Points.Clone(); return area; }).ToArray()
                };
                config.Measurement = new SkirmishObjectiveAreaEvidence
                {
                    ArtifactId = draft.Artifact, ArtifactSha256 = draft.ArtifactSha256,
                    GeometryFingerprint = SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config)
                };
                if (!SkirmishObjectiveAreaCompiler.TryCompile(config, config.Binding, draft.Kind, out var compiled, out string error))
                    throw new AuthoringFailure("areas." + row.Size + "." + row.Difficulty, error);
                return compiled;
            }
        }

        // Builds the SkirmishMapLayoutConfig serialized values from draft points in the
        // source map frame. The world frame runs player base -> enemy base; local layout
        // coordinates are that frame rotated, so an axis-aligned frame keeps map metres.
        private static class LayoutGeometry
        {
            [Serializable]
            private sealed class Fields
            {
                public SkirmishDeclaredLayoutConfiguration[] declaredConfigurations = Array.Empty<SkirmishDeclaredLayoutConfiguration>();
                public SkirmishStructureInstancePadBinding[] structureInstancePads = Array.Empty<SkirmishStructureInstancePadBinding>();
                public string layoutId, operationMapId, contentHash;
                public int contentVersion = 1;
                public float worldWidthMetres, worldDepthMetres, originX, originZ, cellSize;
                public SkirmishLayoutAnchorConfig[] anchors;
                public SkirmishLayoutRouteConfig[] routes;
                public SkirmishLegalPadConfig[] pads;
                public SkirmishLayoutWorldBindingConfig worldBinding;
                public SkirmishBindingValueFormat bindingValueFormat = SkirmishBindingValueFormat.NativeJsonV1;
            }

            private static Dictionary<string, Vector2> ReadPoints(JObject draft)
            {
                var anchorObject = draft["anchors"] as JObject ?? throw new AuthoringFailure("draft.anchors", "Object of role -> [x,z] required.");
                var points = new Dictionary<string, Vector2>(StringComparer.Ordinal);
                foreach (var property in anchorObject.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || property.Name.Any(char.IsWhiteSpace))
                        throw new AuthoringFailure("draft.anchors", "Invalid role '" + property.Name + "'.");
                    points.Add(property.Name, Point(property.Value, "anchors." + property.Name));
                }
                return points;
            }

            // Optional measured per-building pads. Positions are used exactly as written.
            public static List<MeasuredInstancePad> ReadMeasured(JObject draft, string catalogId)
            {
                var result = new List<MeasuredInstancePad>();
                var token = draft["instance_pads"];
                if (token == null) return result;
                var array = token as JArray ?? throw new AuthoringFailure("draft.instance_pads", "Array of pad objects required.");
                var points = ReadPoints(draft);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                string[] allowed = { "id", "kind", "faction", "at", "size", "anchor" };
                for (int i = 0; i < array.Count; i++)
                {
                    string label = "instance_pads[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    var item = array[i] as JObject ?? throw new AuthoringFailure("draft.instance_pads", label + " must be an object.");
                    foreach (var property in item.Properties())
                        if (!allowed.Contains(property.Name)) throw new AuthoringFailure("draft.instance_pads", label + " unknown key '" + property.Name + "'.");
                    string id = (string)item["id"];
                    if (string.IsNullOrWhiteSpace(id) || id.Any(char.IsWhiteSpace)) throw new AuthoringFailure("draft.instance_pads", label + " needs a nonempty id without spaces.");
                    if (!ids.Add(id)) throw new AuthoringFailure("draft.instance_pads", "Duplicate id '" + id + "'.");
                    if (("pad.skirmish." + catalogId.ToLowerInvariant() + ".i_" + id.Replace('.', '_')).Length > 61)
                        throw new AuthoringFailure("draft.instance_pads", label + " id '" + id + "' makes a pad id longer than 61 characters.");
                    if (points.ContainsKey("instance." + id) || (item["id"].Type != JTokenType.String))
                        throw new AuthoringFailure("draft.instance_pads", label + " id '" + id + "' collides with an anchor role.");
                    if (!Enum.TryParse((string)item["kind"], false, out SkirmishLegalPadKind kind) || !SkirmishInstancePadPlanner.IsInstancePadKind(kind))
                        throw new AuthoringFailure("draft.instance_pads", label + " kind must be BaseBarracks, GroundStaging, AirReturn, Supply or Service.");
                    if (item["faction"] == null || item["faction"].Type != JTokenType.Integer || ((int)item["faction"] != 1 && (int)item["faction"] != 2))
                        throw new AuthoringFailure("draft.instance_pads", label + " faction must be 1 or 2.");
                    Vector2 at = Point(item["at"], label + ".at"), size = Point(item["size"], label + ".size");
                    if (!(size.x > 0) || !(size.y > 0)) throw new AuthoringFailure("draft.instance_pads", label + " size must be positive.");
                    string anchor = (string)item["anchor"];
                    if (anchor == null || !points.TryGetValue(anchor, out var anchorPoint))
                        throw new AuthoringFailure("draft.instance_pads", label + " anchor must name a role in anchors{}.");
                    if (Mathf.Abs(anchorPoint.x - at.x) > .001f || Mathf.Abs(anchorPoint.y - at.y) > .001f)
                        throw new AuthoringFailure("draft.instance_pads", label + " anchor '" + anchor + "' must sit exactly on the pad centre.");
                    result.Add(new MeasuredInstancePad { Id = id, Kind = kind, Faction = (byte)(int)item["faction"], X = anchorPoint.x, Z = anchorPoint.y, Width = size.x, Depth = size.y, AnchorRole = anchor });
                }
                return result;
            }

            // Footprints a derived pad may use; only what the draft states (measured in Unity), never a guess.
            public static bool TryFootprint(JObject draft, string structureId, out float width, out float depth)
            {
                width = depth = 0f;
                var table = draft["structure_footprints"] as JObject;
                var entry = table?[structureId];
                if (entry == null) return false;
                var value = Point(entry, "structure_footprints." + structureId);
                width = value.x; depth = value.y;
                return width > 0 && depth > 0;
            }

            public static void Apply(JObject draft, OperationMapDefinition source, string catalogId, SkirmishMapLayoutConfig layout, InstancePadPlan plan = null)
            {
                string token = catalogId.ToLowerInvariant();
                var points = ReadPoints(draft);
                foreach (string required in new[] { "base.player", "base.enemy", "staging.player", "staging.enemy" })
                    if (!points.ContainsKey(required)) throw new AuthoringFailure("draft.anchors", "Missing required role " + required + ".");

                // Pads: one per anchor role with a known pad kind, plus explicit draft pads/overrides.
                var padSpecs = new Dictionary<string, (Vector2 at, string anchor, Vector2 size)>(StringComparer.Ordinal);
                foreach (var pair in points)
                    if (PadKind(pair.Key) != SkirmishLegalPadKind.None) padSpecs[pair.Key] = (pair.Value, pair.Key, DefaultSize(PadKind(pair.Key)));
                if (draft["pads"] is JObject padObject)
                    foreach (var property in padObject.Properties())
                    {
                        string role = property.Name; var kind = PadKind(role);
                        if (kind == SkirmishLegalPadKind.None) throw new AuthoringFailure("draft.pads", "No pad kind for role '" + role + "'.");
                        string anchorRole = points.ContainsKey(role) ? role : DefaultPadAnchor(role);
                        Vector2 at; Vector2 size = DefaultSize(kind);
                        if (property.Value.Type == JTokenType.Null)
                        {
                            if (!points.ContainsKey(role)) throw new AuthoringFailure("draft.pads", role + " is null but has no same-role anchor.");
                            at = points[role];
                        }
                        else if (property.Value is JObject spec)
                        {
                            at = spec["at"] == null ? (points.TryGetValue(role, out var own) ? own : throw new AuthoringFailure("draft.pads", role + " needs 'at'.")) : Point(spec["at"], "pads." + role + ".at");
                            if (spec["anchor"] != null) anchorRole = (string)spec["anchor"];
                            if (spec["size"] != null) size = Point(spec["size"], "pads." + role + ".size");
                        }
                        else at = Point(property.Value, "pads." + role);
                        if (anchorRole == null || !points.ContainsKey(anchorRole)) throw new AuthoringFailure("draft.pads", role + " has no anchor role in anchors{}.");
                        if (!(size.x > 0) || !(size.y > 0)) throw new AuthoringFailure("draft.pads", role + " size must be positive.");
                        padSpecs[role] = (at, anchorRole, size);
                    }

                // Frame from the actual player/enemy base points.
                Vector2 player = points["base.player"], enemy = points["base.enemy"];
                Vector2 delta = enemy - player;
                if (delta.magnitude < 1f) throw new AuthoringFailure("draft.anchors", "Player and enemy bases coincide.");
                Vector2 forward = delta / delta.magnitude, across = new Vector2(-forward.y, forward.x);
                var all = points.Values.Concat(padSpecs.Values.Select(x => x.at))
                    .Concat(plan == null ? Enumerable.Empty<Vector2>() : plan.Pads.Where(p => !p.Measured).Select(p => new Vector2(p.X, p.Z))).ToArray();
                float fMin = all.Min(p => Vector2.Dot(p - player, forward)), fMax = all.Max(p => Vector2.Dot(p - player, forward));
                float aMin = all.Min(p => Vector2.Dot(p - player, across)), aMax = all.Max(p => Vector2.Dot(p - player, across));
                float forwardMetres = fMax - fMin + 2 * FrameMarginMetres, acrossMetres = aMax - aMin + 2 * FrameMarginMetres;
                Vector2 origin = player + forward * (fMin - FrameMarginMetres) + across * ((aMin + aMax) * .5f);
                float localOriginX = Vector2.Dot(origin, forward), localOriginZ = Vector2.Dot(origin, across) - acrossMetres * .5f;
                Func<Vector2, Vector2> local = p => new Vector2(Vector2.Dot(p, forward), Vector2.Dot(p, across));

                Func<string, string> anchorId = role => "anchor.skirmish." + token + "." + role.Replace('.', '_');
                var anchors = points.OrderBy(x => x.Key, StringComparer.Ordinal).Select(pair =>
                {
                    var l = local(pair.Value);
                    return new SkirmishLayoutAnchorConfig
                    {
                        AnchorId = anchorId(pair.Key), RoleId = pair.Key,
                        NormalizedU = (l.x - localOriginX) / forwardMetres, NormalizedV = (l.y - localOriginZ) / acrossMetres,
                        WorldX = l.x, WorldZ = l.y
                    };
                }).ToArray();
                var pads = padSpecs.OrderBy(x => x.Key, StringComparer.Ordinal).Select(pair =>
                {
                    var l = local(pair.Value.at);
                    return new SkirmishLegalPadConfig
                    {
                        PadId = "pad.skirmish." + token + "." + pair.Key.Replace('.', '_'), AnchorId = anchorId(pair.Value.anchor),
                        RoleId = pair.Key, Kind = PadKind(pair.Key), CenterX = l.x, CenterZ = l.y,
                        WidthMetres = pair.Value.size.x, DepthMetres = pair.Value.size.y, FactionId = Faction(pair.Key)
                    };
                }).ToArray();
                var rows = Array.Empty<SkirmishStructureInstancePadBinding>();
                if (plan != null)
                {
                    // Measured entries reuse their draft anchor; derived pads get a new anchor at the same centre.
                    // Instance pads come after the kind pads, so first-match kind lookups keep their pad.
                    var padKeys = new Dictionary<string, (string padId, string anchorId)>(StringComparer.Ordinal);
                    var instanceAnchors = new List<SkirmishLayoutAnchorConfig>();
                    var instancePads = new List<SkirmishLegalPadConfig>();
                    foreach (var planned in plan.Pads)
                    {
                        string slug = planned.Measured ? planned.MeasuredId.Replace('.', '_') : planned.Slug;
                        string padId = "pad.skirmish." + token + "." + (planned.Measured ? "i_" : "") + slug;
                        string padAnchor;
                        if (planned.Measured) padAnchor = anchorId(planned.AnchorRole);
                        else
                        {
                            if (points.ContainsKey("instance." + planned.Slug)) throw new AuthoringFailure("instancePads", "Anchor role instance." + planned.Slug + " already exists.");
                            padAnchor = anchorId("instance." + planned.Slug);
                            var l = local(new Vector2(planned.X, planned.Z));
                            instanceAnchors.Add(new SkirmishLayoutAnchorConfig
                            {
                                AnchorId = padAnchor, RoleId = "instance." + planned.Slug,
                                NormalizedU = (l.x - localOriginX) / forwardMetres, NormalizedV = (l.y - localOriginZ) / acrossMetres,
                                WorldX = l.x, WorldZ = l.y
                            });
                        }
                        var c = local(new Vector2(planned.X, planned.Z));
                        // Draft sizes are world-aligned footprints; the pad lives in the rotated layout frame, so
                        // store the frame-axis envelope (plus the one-cell grid-snap margin) of that rectangle.
                        SkirmishInstancePadPlanner.EncloseWorldAlignedFootprint(forward.x, forward.y, across.x, across.y,
                            planned.Width, planned.Depth, source.GridMetadata.CellSize, out float padWidth, out float padDepth);
                        instancePads.Add(new SkirmishLegalPadConfig
                        {
                            PadId = padId, AnchorId = padAnchor, RoleId = "instance." + (planned.Measured ? planned.MeasuredId : planned.Slug),
                            Kind = planned.Kind, CenterX = c.x, CenterZ = c.y, WidthMetres = padWidth, DepthMetres = padDepth, FactionId = planned.Faction
                        });
                        padKeys.Add(planned.Key, (padId, padAnchor));
                    }
                    anchors = anchors.Concat(instanceAnchors).ToArray();
                    pads = pads.Concat(instancePads).ToArray();
                    rows = plan.Rows.OrderBy(r => (int)r.Size).ThenBy(r => (int)r.Difficulty).ThenBy(r => r.Faction)
                        .ThenBy(r => r.StructureId, StringComparer.Ordinal).ThenBy(r => r.Ordinal).Select(r => new SkirmishStructureInstancePadBinding
                        {
                            SizeId = r.Size, DifficultyId = r.Difficulty, FactionId = r.Faction, StructureId = r.StructureId, InstanceOrdinal = r.Ordinal,
                            PadId = padKeys[r.PadKey].padId, AnchorId = padKeys[r.PadKey].anchorId
                        }).ToArray();
                }
                var routes = Routes(draft["routes"] as JArray, points, anchorId, token);

                string physicalHash = source.SourceBinding.IsConfigured ? source.SourceBinding.SourceContentHash : source.ContentHash;
                var fields = new Fields
                {
                    layoutId = "layout.skirmish." + token, operationMapId = source.OperationMapId, contentHash = string.Empty,
                    worldWidthMetres = forwardMetres, worldDepthMetres = acrossMetres, originX = localOriginX, originZ = localOriginZ,
                    cellSize = source.GridMetadata.CellSize, anchors = anchors, routes = routes, pads = pads, structureInstancePads = rows,
                    worldBinding = new SkirmishLayoutWorldBindingConfig
                    {
                        OriginX = origin.x, OriginZ = origin.y, ForwardX = forward.x, ForwardZ = forward.y,
                        AcrossX = across.x, AcrossZ = across.y, ForwardMetres = forwardMetres, AcrossMetres = acrossMetres,
                        PlayerAnchorId = anchorId("base.player"), EnemyAnchorId = anchorId("base.enemy"), SourceHash = physicalHash,
                        HasMeasuredDeployments = false, BindAllConfigurations = true
                    }
                };
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fields), layout);
                // Measured deployments are the layout's own projection of its base anchors.
                if (!layout.TryGetAnchor("base.player", out var playerAnchor) || !layout.TryGetAnchor("base.enemy", out var enemyAnchor) ||
                    !layout.TryProjectToMap(playerAnchor.NormalizedU, playerAnchor.NormalizedV, out float px, out float pz) ||
                    !layout.TryProjectToMap(enemyAnchor.NormalizedU, enemyAnchor.NormalizedV, out float ex, out float ez))
                    throw new AuthoringFailure("layout.frame", "Base anchors do not project through the derived frame.");
                fields.worldBinding.HasMeasuredDeployments = true;
                fields.worldBinding.PlayerDeployment = new Vector2(px, pz);
                fields.worldBinding.EnemyDeployment = new Vector2(ex, ez);
                fields.contentHash = "authored." + token + "." + Sha256(JsonUtility.ToJson(fields)).Substring(0, 16);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fields), layout);
                if (layout.LayoutId != fields.layoutId || layout.Anchors.Length != anchors.Length || !layout.HasWorldBinding ||
                    !layout.WorldBinding.HasMeasuredDeployments || layout.BindingValueFormat != SkirmishBindingValueFormat.NativeJsonV1)
                    throw new AuthoringFailure("layout.serialize", "Layout values did not apply to SkirmishMapLayoutConfig.");
            }

            private static SkirmishLayoutRouteConfig[] Routes(JArray draftRoutes, Dictionary<string, Vector2> points, Func<string, string> anchorId, string token)
            {
                if (draftRoutes == null || draftRoutes.Count < 2) throw new AuthoringFailure("draft.routes", "At least two routes required.");
                var result = new List<SkirmishLayoutRouteConfig>(); var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in draftRoutes)
                {
                    var route = item as JObject ?? throw new AuthoringFailure("draft.routes", "Route object required.");
                    if (!Enum.TryParse((string)route["kind"], false, out SkirmishMeasuredRouteKind kind) || kind == SkirmishMeasuredRouteKind.None)
                        throw new AuthoringFailure("draft.routes", "kind must be MainHighway, FlankNorthRuins or FlankSouthSweep.");
                    var via = (route["via"] as JArray)?.Select(x => (string)x).ToArray();
                    if (via == null || via.Length < 2 || via.Any(x => x == null || !points.ContainsKey(x)))
                        throw new AuthoringFailure("draft.routes", "via must list >= 2 anchor roles from anchors{}.");
                    bool vehicles = route["vehicles"] == null || (bool)route["vehicles"];
                    bool aircraft = route["aircraft"] != null && (bool)route["aircraft"];
                    float width = route["width"] == null ? (vehicles ? 12f : 6f) : (float)route["width"];
                    string slug = kind == SkirmishMeasuredRouteKind.MainHighway ? "main" : kind == SkirmishMeasuredRouteKind.FlankNorthRuins ? "flank_north" : "flank_south";
                    string id = "route.skirmish." + token + "." + slug;
                    for (int n = 2; !ids.Add(id); n++) id = "route.skirmish." + token + "." + slug + "_" + n.ToString(CultureInfo.InvariantCulture);
                    float length = 0f;
                    for (int i = 1; i < via.Length; i++) length += Vector2.Distance(points[via[i - 1]], points[via[i]]);
                    // Provisional nominal speeds (infantry 4 m/s, ground 8, air 16); first contact at half transit.
                    float infantry = length / 4f, ground = length / 8f, air = length / 16f;
                    result.Add(new SkirmishLayoutRouteConfig
                    {
                        RouteId = id, WaypointAnchorIds = via.Select(anchorId).ToArray(), Kind = kind, LengthMetres = length,
                        InfantryTransitSeconds = infantry, GroundTransitSeconds = ground, AirTransitSeconds = air,
                        InfantryFirstContactSeconds = infantry * .5f, GroundFirstContactSeconds = ground * .5f, ProvisionalTimes = true,
                        WidthMetres = width, AllowsVehicles = vehicles, AllowsAircraft = aircraft
                    });
                }
                return result.ToArray();
            }

            private static SkirmishLegalPadKind PadKind(string role)
            {
                if (role.StartsWith("base.", StringComparison.Ordinal)) return SkirmishLegalPadKind.BaseBarracks;
                if (role.StartsWith("staging.", StringComparison.Ordinal)) return SkirmishLegalPadKind.GroundStaging;
                if (role.StartsWith("spawn.infantry.", StringComparison.Ordinal)) return SkirmishLegalPadKind.InfantrySpawn;
                if (role.StartsWith("spawn.vehicle.", StringComparison.Ordinal)) return SkirmishLegalPadKind.VehicleSpawn;
                if (role.StartsWith("rally.", StringComparison.Ordinal)) return SkirmishLegalPadKind.Rally;
                if (role.StartsWith("air.", StringComparison.Ordinal)) return SkirmishLegalPadKind.AirReturn;
                if (role.StartsWith("economy.", StringComparison.Ordinal)) return SkirmishLegalPadKind.Supply;
                if (role.StartsWith("service.", StringComparison.Ordinal)) return SkirmishLegalPadKind.Service;
                if (role.StartsWith("supply.expansion.", StringComparison.Ordinal)) return SkirmishLegalPadKind.SupplyExpansion;
                // Mode-owned exact roles (SkirmishMeasuredLayoutBinder): faction 1 truck bays, faction 2 approach towers.
                if (role == "convoy.origin.1" || role == "convoy.origin.2" || role == "convoy.origin.3") return SkirmishLegalPadKind.ObjectiveTruckSpawn;
                if (role == "defense.approach.a" || role == "defense.approach.b") return SkirmishLegalPadKind.DefenseApproach;
                return SkirmishLegalPadKind.None;
            }

            private static string DefaultPadAnchor(string role)
            {
                if (role.EndsWith(".player", StringComparison.Ordinal)) return "staging.player";
                if (role.EndsWith(".enemy", StringComparison.Ordinal)) return "staging.enemy";
                return null;
            }

            private static byte Faction(string role) => role.EndsWith(".player", StringComparison.Ordinal) ? (byte)1 :
                role.EndsWith(".enemy", StringComparison.Ordinal) ? (byte)2 :
                PadKind(role) == SkirmishLegalPadKind.ObjectiveTruckSpawn ? (byte)1 :
                PadKind(role) == SkirmishLegalPadKind.DefenseApproach ? (byte)2 : (byte)0;

            // Footprints of the existing measured Desert Base pads (SkirmishLayout_S005).
            private static Vector2 DefaultSize(SkirmishLegalPadKind kind)
            {
                switch (kind)
                {
                    case SkirmishLegalPadKind.BaseBarracks: return new Vector2(16, 12);
                    case SkirmishLegalPadKind.GroundStaging: return new Vector2(20, 14);
                    case SkirmishLegalPadKind.InfantrySpawn: return new Vector2(12, 10);
                    case SkirmishLegalPadKind.VehicleSpawn: return new Vector2(14, 12);
                    case SkirmishLegalPadKind.Rally: return new Vector2(10, 8);
                    case SkirmishLegalPadKind.AirReturn: return new Vector2(16, 16);
                    case SkirmishLegalPadKind.Service: return new Vector2(12, 10);
                    default: return new Vector2(14, 12);
                }
            }

            public static Vector2 Point(JToken token, string what)
            {
                var array = token as JArray;
                if (array == null || array.Count != 2 || array.Any(x => x.Type != JTokenType.Integer && x.Type != JTokenType.Float))
                    throw new AuthoringFailure("draft." + what, "Expected [x, z] numbers.");
                var value = new Vector2((float)array[0], (float)array[1]);
                if (!float.IsFinite(value.x) || !float.IsFinite(value.y)) throw new AuthoringFailure("draft." + what, "Finite values required.");
                return value;
            }

            private static string Sha256(string text)
            {
                using var hash = SHA256.Create();
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
#endif
