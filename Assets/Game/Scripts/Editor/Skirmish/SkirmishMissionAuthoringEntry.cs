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
//   "routes":  [ {"kind":"MainHighway|FlankNorthRuins|FlankSouthSweep",
//                 "via":["staging.player", ..., "staging.enemy"],
//                 "width":12, "vehicles":true, "aircraft":true}, ... ],  // >=2 distinct, 1 main
//   "notes": "free text, ignored"
// }
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
        private static readonly string[] DraftKeys = { "catalog_id", "campaign_source", "anchors", "pads", "routes", "notes" };
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
            var outcome = new Outcome();
            bool passed = false;
            try
            {
                Author(Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT"), outcome);
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
            if (quit) EditorApplication.Exit(passed ? 0 : 2);
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
            if (tuple.ObjectiveKind != SkirmishObjectiveKind.BaseAssault)
                throw new AuthoringFailure("objective", tuple.ObjectiveKind + " needs typed objective areas (Variants[].Areas + area/geometry " +
                    "fingerprints) and a Shared objective config; this entry authors Base Assault only.");

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
            var objective = SharedByKind<SkirmishObjectiveConfig>(x => x.Kind == tuple.ObjectiveKind, "objective " + tuple.ObjectiveKind);
            var army = SharedByKind<SkirmishArmyProfileConfig>(x => x.Kind == tuple.ArmyProfile, "army " + tuple.ArmyProfile);
            var start = SharedByKind<SkirmishStartPackageConfig>(x => x.Kind == tuple.StartProfile, "start " + tuple.StartProfile);
            string[] features = tuple.ArmyProfile == SkirmishArmyProfileId.AirMobile ? (string[])SkirmishS003FirstVisit.RequiredFeatureIds.Clone() :
                tuple.ArmyProfile == SkirmishArmyProfileId.CombinedArms ? (string[])SkirmishS005FirstVisit.RequiredFeatureIds.Clone() : null;
            SkirmishSizeId recommended = ReadRecommendedSize(project, catalogId);

            // --- layout ----------------------------------------------------------------------
            string folder = ScenarioRoot + catalogId;
            EnsureFolder(folder);
            string layoutPath = folder + "/SkirmishLayout_" + catalogId + ".asset";
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
            bool newLayout = layout == null;
            if (newLayout) layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            LayoutGeometry.Apply(draft, source, catalogId, layout);
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

            // --- compile every size x difficulty without a binding; capture logical inputs ----
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            var compiled = new List<Compiled>();
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
                Debug.LogWarning(Tag + " size=" + size + " skipped (no logical variant) first=" + sizeFailure);
            }
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
            var variants = compiled.Select(x => BuildVariant(x, view)).ToArray();

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
        private static SkirmishMissionLogicalVariant BuildVariant(Compiled row, SkirmishMissionViewProfile view)
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
            variant.ReviewedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            if (!SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant, setup, out string error))
                throw new AuthoringFailure("framing." + row.Size + "." + row.Difficulty, error);
            return variant;
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

            public static void Apply(JObject draft, OperationMapDefinition source, string catalogId, SkirmishMapLayoutConfig layout)
            {
                string token = catalogId.ToLowerInvariant();
                var anchorObject = draft["anchors"] as JObject ?? throw new AuthoringFailure("draft.anchors", "Object of role -> [x,z] required.");
                var points = new Dictionary<string, Vector2>(StringComparer.Ordinal);
                foreach (var property in anchorObject.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || property.Name.Any(char.IsWhiteSpace))
                        throw new AuthoringFailure("draft.anchors", "Invalid role '" + property.Name + "'.");
                    points.Add(property.Name, Point(property.Value, "anchors." + property.Name));
                }
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
                var all = points.Values.Concat(padSpecs.Values.Select(x => x.at)).ToArray();
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
                var routes = Routes(draft["routes"] as JArray, points, anchorId, token);

                string physicalHash = source.SourceBinding.IsConfigured ? source.SourceBinding.SourceContentHash : source.ContentHash;
                var fields = new Fields
                {
                    layoutId = "layout.skirmish." + token, operationMapId = source.OperationMapId, contentHash = string.Empty,
                    worldWidthMetres = forwardMetres, worldDepthMetres = acrossMetres, originX = localOriginX, originZ = localOriginZ,
                    cellSize = source.GridMetadata.CellSize, anchors = anchors, routes = routes, pads = pads,
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
                return SkirmishLegalPadKind.None;
            }

            private static string DefaultPadAnchor(string role)
            {
                if (role.EndsWith(".player", StringComparison.Ordinal)) return "staging.player";
                if (role.EndsWith(".enemy", StringComparison.Ordinal)) return "staging.enemy";
                return null;
            }

            private static byte Faction(string role) => role.EndsWith(".player", StringComparison.Ordinal) ? (byte)1 :
                role.EndsWith(".enemy", StringComparison.Ordinal) ? (byte)2 : (byte)0;

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

            private static Vector2 Point(JToken token, string what)
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
