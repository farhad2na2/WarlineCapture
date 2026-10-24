#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Authors draft configuration only. No publication or native source-fit certification.</summary>
    public static class SkirmishConfigOnlyBatchBuilder
    {
        public const string ScenarioRoot = "Assets/Game/Configs/SkirmishExpansion/Scenarios";
        private static readonly SkirmishSizeId[] Sizes =
            { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };
        private static readonly string[] Features = { "ground", "intel", "transport", "advanced_ground", "objective_ba" };
        private static readonly HashSet<string> CampaignSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset",
            "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/Variants/AshLinePort/Candidate/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH04M04GroundedSignal/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH05M02TrustUnderFire/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH05M03NetworkCollapse/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH05M04LastCorridor/Definition.asset",
            "Assets/Game/GeneratedOperationMaps/CH05M05CommandNode/Definition.asset"
        };

        public sealed class Request
        {
            public string CatalogId;
            public OperationMapDefinition CampaignSource;
            public string ExpectedSourceIdentityHash;
            public string ExpectedSourceContentHash;
            public string ExpectedSourceAssetSha256;
            public string ExpectedSceneGuid;
            public string ExpectedGridGuid;
            public string ExpectedSurfaceGuid;
            public SkirmishMapLayoutConfig MeasuredLayout;
            public SkirmishExpansionAuthoredSet Profiles;
            public SkirmishStartPackageConfig Start;
        }

        public sealed class RowResult
        {
            public string CatalogId;
            public SkirmishSizeId Size;
            public bool CompilerPassed;
            public bool RuntimeLayoutBound;
            public uint SetupHash;
            public string Detail;
        }

        public sealed class Result
        {
            public bool Applied;
            public string MatrixSha256;
            public readonly List<string> Errors = new List<string>();
            public readonly List<RowResult> Rows = new List<RowResult>();
            public readonly List<string> ChangedAssets = new List<string>();
            public bool Success => Applied && Errors.Count == 0;
            public string AcceptanceGate => "Draft only; native placement, routes, visuals, journeys, recovery and player/device acceptance pending.";
        }

        /// <summary>
        /// Uses the repository's complete matrix; never caller-modified budgets. Every request
        /// receives all three size results. Any preflight error prevents all persistent writes.
        /// I/O/import failures during apply throw; this API is not a filesystem transaction.
        /// </summary>
        public static Result BuildBatch(IReadOnlyList<Request> requests, SkirmishExpansionCatalogConfig catalog,
            string outputRoot = ScenarioRoot)
        {
            var result = new Result();
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author drafts in Edit mode.");
            if (!ValidRoot(outputRoot)) result.Errors.Add("Output root must remain inside " + ScenarioRoot + ".");
            if (catalog == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(catalog)))
                result.Errors.Add("A persistent expansion catalog is required.");
            else if (!catalog.TryValidate(out string catalogError)) result.Errors.Add(catalogError);
            if (requests == null || requests.Count == 0) { result.Errors.Add("A nonempty batch is required."); return result; }
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            bool matrixLoaded = SkirmishSetupMatrixTable.TryLoad(projectRoot, out var matrix, out string matrixError);
            if (!matrixLoaded) result.Errors.Add(matrixError);
            else result.MatrixSha256 = HashFile(Path.Combine(projectRoot, SkirmishSetupMatrixTable.RelativeCsvPath));
            var drafts = new List<SkirmishScenarioDefinitionConfig>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (Request request in requests)
                {
                    var errors = new List<string>();
                    string id = request?.CatalogId ?? "<missing>";
                    if (!seen.Add(id)) errors.Add("Duplicate requested identity.");
                    ValidateRequest(request, outputRoot, catalog, errors);
                    if (!matrixLoaded) errors.Add("Canonical setup matrix unavailable.");
                    SkirmishScenarioDefinitionConfig draft = null;
                    if (errors.Count == 0)
                    {
                        draft = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
                        draft.name = "SkirmishScenario_" + id;
                        Configure(draft, request, request.MeasuredLayout);
                        drafts.Add(draft);
                    }
                    else drafts.Add(null);

                    foreach (SkirmishSizeId size in Sizes)
                    {
                        var row = new RowResult { CatalogId = id, Size = size };
                        if (errors.Count != 0) row.Detail = string.Join("; ", errors);
                        else
                        {
                            int count = 0;
                            foreach (var vector in matrix) if (vector.CatalogId == id && vector.SizeId == size) count++;
                            if (count != 1) row.Detail = "Expected exactly one canonical matrix row; got " + count + ".";
                            else
                            {
                                row.CompilerPassed = SkirmishSetupCompiler.TryCompile(draft, SkirmishDifficultyId.Regular,
                                    size, 0, new SkirmishContentManifest { RequiredFeatureIds = Features }, matrix,
                                    out var setup, out var reasons);
                                row.RuntimeLayoutBound = row.CompilerPassed && setup.MeasuredLayoutBound;
                                row.SetupHash = row.CompilerPassed ? setup.SetupHash : 0;
                                row.Detail = row.CompilerPassed
                                    ? (row.RuntimeLayoutBound ? "Compiled draft; native source fit pending." : "Compiled draft; generic runtime layout binding and native source fit pending.")
                                    : string.Join("; ", reasons);
                            }
                        }
                        result.Rows.Add(row);
                        if (!row.CompilerPassed) result.Errors.Add(id + "/" + size + ": " + row.Detail);
                    }
                }
                if (result.Errors.Count != 0) return result;

                for (int i = 0; i < requests.Count; i++)
                {
                    Request request = requests[i];
                    string folder = outputRoot + "/" + request.CatalogId;
                    EnsureFolder(folder);
                    var layoutDraft = UnityEngine.Object.Instantiate(request.MeasuredLayout);
                    layoutDraft.name = "SkirmishLayout_" + request.CatalogId;
                    try
                    {
                        var layout = Persist(layoutDraft, folder + "/SkirmishLayout_" + request.CatalogId + ".asset", result);
                        Configure(drafts[i], request, layout);
                        var definition = Persist(drafts[i], folder + "/SkirmishScenario_" + request.CatalogId + ".asset", result);
                        Register(catalog, definition, result);
                    }
                    finally
                    {
                        if (!EditorUtility.IsPersistent(layoutDraft)) UnityEngine.Object.DestroyImmediate(layoutDraft);
                    }
                }
                result.Applied = true;
                return result;
            }
            finally
            {
                foreach (var draft in drafts)
                    if (draft != null && !EditorUtility.IsPersistent(draft)) UnityEngine.Object.DestroyImmediate(draft);
            }
        }

        private static void ValidateRequest(Request r, string outputRoot, SkirmishExpansionCatalogConfig catalog, List<string> errors)
        {
            if (r == null) { errors.Add("Missing request."); return; }
            string group = PilotGroup(r.CatalogId);
            if (group == null) errors.Add("Only the six uncoded BA/G pilot identities are supported.");
            if (r.Profiles == null || !Persistent(r.Profiles.ObjectiveBa) || !Persistent(r.Profiles.ArmyGround) ||
                !Persistent(r.Start) || !Persistent(r.Profiles.Economy) || !Persistent(r.Profiles.Upgrades) ||
                !Persistent(r.Profiles.Intel) || !Persistent(r.Profiles.Roles))
                errors.Add("Explicit persistent BA/G, start, economy, upgrade, intel and role profiles are required.");
            else
            {
                if (r.Profiles.ObjectiveBa.Kind != SkirmishObjectiveKind.BaseAssault ||
                    r.Profiles.ArmyGround.Kind != SkirmishArmyProfileId.GroundManeuver) errors.Add("BA/G profiles required.");
                bool field = r.CatalogId == "S049" || r.CatalogId == "S097";
                if (r.Start.Kind != (field ? SkirmishStartPackageId.FieldBase : SkirmishStartPackageId.EstablishedBase))
                    errors.Add("Start profile does not match the catalog tuple.");
            }
            ValidateCampaignSource(r, errors);
            if (!Persistent(r.MeasuredLayout)) errors.Add("Explicit persistent measured logical layout required.");
            else
            {
                var layout = r.MeasuredLayout;
                if (group != null && (layout.OperationMapId != "opmap.skirmish." + group ||
                    layout.LayoutId != "layout.skirmish." + GroupCode(r.CatalogId) + ".ba")) errors.Add("Logical layout identity does not match catalog group.");
                if (!layout.HasWorldBinding || !layout.WorldBinding.HasMeasuredDeployments ||
                    !layout.WorldBinding.BindAllConfigurations || layout.WorldBinding.SourceHash != r.ExpectedSourceContentHash)
                    errors.Add("All-size measured deployments with the exact source hash are required.");
                if (r.CampaignSource != null) ValidateGeometryDeclaration(layout, r.CampaignSource, errors);
            }
            if (ValidRoot(outputRoot) && group != null)
            {
                string folder = outputRoot + "/" + r.CatalogId;
                string definitionPath = folder + "/SkirmishScenario_" + r.CatalogId + ".asset";
                CheckTarget<SkirmishScenarioDefinitionConfig>(definitionPath, errors);
                CheckTarget<SkirmishMapLayoutConfig>(folder + "/SkirmishLayout_" + r.CatalogId + ".asset", errors);
                if (catalog != null && catalog.TryGetDefinition(r.CatalogId, out var existing) &&
                    AssetDatabase.GetAssetPath(existing) != definitionPath) errors.Add("Catalog identity already belongs to a different definition path.");
            }
        }

        // Shared source admission only; measured-layout/publication guards stay in ValidateRequest.
        internal static void ValidateCampaignSource(Request r, List<string> errors)
        {
            if (!Persistent(r.CampaignSource)) errors.Add("Explicit persistent existing Campaign source required.");
            else
            {
                var source = r.CampaignSource;
                if (!CampaignSources.Contains(AssetDatabase.GetAssetPath(source))) errors.Add("Source is outside the current Campaign reuse inventory.");
                if (EditorUtility.IsDirty(source)) errors.Add("Campaign source has unsaved changes; freeze before authoring.");
                if (!source.TryValidateMetadata(out string error) || !source.TryValidateLocalContentReferences(out error))
                    errors.Add("Campaign metadata: " + error);
                if (string.IsNullOrEmpty(r.ExpectedSourceIdentityHash) || source.SourceIdentityHash != r.ExpectedSourceIdentityHash ||
                    string.IsNullOrEmpty(r.ExpectedSourceContentHash) || source.ContentHash != r.ExpectedSourceContentHash ||
                    string.IsNullOrEmpty(r.ExpectedSourceAssetSha256) || HashFile(AssetDatabase.GetAssetPath(source)) != r.ExpectedSourceAssetSha256 ||
                    string.IsNullOrEmpty(r.ExpectedSceneGuid) || source.SourceSceneReference?.AssetGUID != r.ExpectedSceneGuid ||
                    string.IsNullOrEmpty(r.ExpectedGridGuid) || source.GridMetadata.AssetGuid != r.ExpectedGridGuid ||
                    string.IsNullOrEmpty(r.ExpectedSurfaceGuid) || source.MapSurfaceDataReference?.AssetGUID != r.ExpectedSurfaceGuid)
                    errors.Add("Pinned Campaign source/hash/scene/grid/surface mismatch.");
            }
        }

        internal static void ValidateLogicalGeometryDeclaration(SkirmishMapLayoutConfig layout, OperationMapDefinition source, List<string> errors)
            => ValidateGeometryDeclaration(layout, source, errors);

        private static void ValidateGeometryDeclaration(SkirmishMapLayoutConfig layout, OperationMapDefinition source, List<string> errors)
        {
            if (layout.Anchors == null || layout.Pads == null || layout.Routes == null)
            { errors.Add("Typed anchors, pads and routes are required."); return; }
            var frame = layout.WorldBinding;
            if (!Finite(frame.ForwardX) || !Finite(frame.ForwardZ) || !Finite(frame.AcrossX) || !Finite(frame.AcrossZ) ||
                !Finite(frame.ForwardMetres) || !Finite(frame.AcrossMetres) ||
                Mathf.Abs(frame.ForwardX * frame.ForwardX + frame.ForwardZ * frame.ForwardZ - 1f) > 0.01f ||
                Mathf.Abs(frame.AcrossX * frame.AcrossX + frame.AcrossZ * frame.AcrossZ - 1f) > 0.01f ||
                Mathf.Abs(frame.ForwardX * frame.AcrossX + frame.ForwardZ * frame.AcrossZ) > 0.01f)
                errors.Add("Measured frame must have finite orthonormal axes and extents.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var anchor in layout.Anchors)
            {
                if (string.IsNullOrEmpty(anchor.AnchorId) || !ids.Add(anchor.AnchorId)) errors.Add("Missing/duplicate anchor identity.");
                if (!layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) ||
                    !Inside(source, x, z, 0f, 0f)) errors.Add("Anchor outside pinned source bounds: " + anchor.AnchorId);
            }
            foreach (string role in new[] { "base.player", "base.enemy", "staging.player", "staging.enemy", "economy.player", "economy.enemy", "service.player", "service.enemy" })
                if (!layout.TryGetAnchor(role, out _)) errors.Add("Missing typed anchor: " + role);
            foreach (string role in new[] { "base.player", "base.enemy" })
                if (layout.TryGetAnchor(role, out var anchor))
                {
                    Vector2 deployment = role == "base.player" ? frame.PlayerDeployment : frame.EnemyDeployment;
                    if (!layout.TryProjectToMap(anchor.NormalizedU, anchor.NormalizedV, out float x, out float z) ||
                        !Inside(source, deployment.x, deployment.y, 0, 0) ||
                        Vector2.Distance(deployment, new Vector2(x, z)) > 0.5f) errors.Add("Measured deployment mismatch: " + role);
                }
            foreach (var pad in layout.Pads)
            {
                bool legal = ids.Contains(pad.AnchorId) && Finite(pad.WidthMetres) && Finite(pad.DepthMetres) &&
                    pad.WidthMetres > 0 && pad.DepthMetres > 0;
                foreach (int dx in new[] { -1, 1 }) foreach (int dz in new[] { -1, 1 })
                    legal &= layout.TryProjectLocalToMap(pad.CenterX + dx * pad.WidthMetres / 2f,
                        pad.CenterZ + dz * pad.DepthMetres / 2f, out float x, out float z) && Inside(source, x, z, 0f, 0f);
                if (!legal) errors.Add("Pad outside pinned source bounds or invalid: " + pad.PadId);
            }
            foreach (byte faction in new byte[] { 1, 2 })
                foreach (var kind in new[] { SkirmishLegalPadKind.BaseBarracks, SkirmishLegalPadKind.GroundStaging,
                    SkirmishLegalPadKind.InfantrySpawn, SkirmishLegalPadKind.VehicleSpawn, SkirmishLegalPadKind.Rally,
                    SkirmishLegalPadKind.Supply, SkirmishLegalPadKind.Service })
                    if (!layout.TryGetPad(faction, kind, out _)) errors.Add("Missing typed pad: " + faction + "/" + kind);
            if (layout.Routes.Length < 2) errors.Add("Two explicit approach routes required.");
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in layout.Routes)
            {
                if (string.IsNullOrEmpty(route.RouteId) || !routeIds.Add(route.RouteId)) errors.Add("Missing/duplicate route identity.");
                if (route.WaypointAnchorIds == null || route.WaypointAnchorIds.Length < 2 || route.WidthMetres <= 0)
                    errors.Add("Route needs width and connected anchor declarations: " + route.RouteId);
                else
                {
                    paths.Add(string.Join(">", route.WaypointAnchorIds));
                    foreach (string id in route.WaypointAnchorIds) if (!ids.Contains(id)) errors.Add("Missing route waypoint: " + id);
                }
            }
            if (paths.Count < 2) errors.Add("Two distinct approach waypoint declarations required.");
            // Static declarations cannot establish collision, navigation or native facility clearance.
        }

        private static bool Inside(OperationMapDefinition source, float x, float z, float width, float depth) =>
            !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(z) && !float.IsInfinity(z) &&
            x - width / 2 >= source.Bounds.WorldMin.x && x + width / 2 <= source.Bounds.WorldMax.x &&
            z - depth / 2 >= source.Bounds.WorldMin.z && z + depth / 2 <= source.Bounds.WorldMax.z;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void Configure(SkirmishScenarioDefinitionConfig definition, Request r, SkirmishMapLayoutConfig layout)
        {
            var p = r.Profiles;
            definition.Configure(r.CatalogId, p.ObjectiveBa, p.ArmyGround, r.Start, p.Economy, p.Upgrades,
                p.Intel, p.Roles, layout, SkirmishSizeId.Standard, Features);
            definition.BindCampaignMap(r.CampaignSource);
        }

        private static T Persist<T>(T draft, string path, Result result) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(draft, path); result.ChangedAssets.Add(path); return draft; }
            if (EditorJsonUtility.ToJson(existing) != EditorJsonUtility.ToJson(draft))
            {
                EditorUtility.CopySerialized(draft, existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                result.ChangedAssets.Add(path);
            }
            return existing;
        }

        private static void Register(SkirmishExpansionCatalogConfig catalog, SkirmishScenarioDefinitionConfig definition, Result result)
        {
            if (catalog.TryGetDefinition(definition.CatalogId, out var current) && current == definition) return;
            var data = new SerializedObject(catalog);
            var array = data.FindProperty("definitions");
            int index = array.arraySize;
            array.arraySize++;
            array.GetArrayElementAtIndex(index).objectReferenceValue = definition;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            string path = AssetDatabase.GetAssetPath(catalog);
            if (!result.ChangedAssets.Contains(path)) result.ChangedAssets.Add(path);
        }

        private static void CheckTarget<T>(string path, List<string> errors) where T : ScriptableObject
        {
            var target = AssetDatabase.LoadMainAssetAtPath(path);
            if ((target != null && !(target is T)) || (File.Exists(path) && target == null)) errors.Add("Incompatible existing target: " + path);
        }
        private static bool Persistent(UnityEngine.Object asset) => asset != null && EditorUtility.IsPersistent(asset);
        private static bool ValidRoot(string path) => !string.IsNullOrEmpty(path) && !path.Contains("\\") &&
            !path.Contains("..") && !path.EndsWith("/", StringComparison.Ordinal) &&
            (path == ScenarioRoot || path.StartsWith(ScenarioRoot + "/", StringComparison.Ordinal));
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static string PilotGroup(string id)
        {
            switch (id) { case "S026": return "city_crossroads"; case "S049": case "S050": return "mountain_pass";
                case "S074": return "industrial_basin"; case "S097": case "S098": return "airfield_plains"; default: return null; }
        }
        private static string GroupCode(string id) => id == "S026" ? "cc" : id == "S074" ? "ib" : id == "S049" || id == "S050" ? "mp" : "ap";
        public static string HashFile(string path)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
