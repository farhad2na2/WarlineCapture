#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Captures only the original baked navigation rows for an explicitly pinned
        // Campaign source. This does not initialize or validate a mission.
        public static void RunPinnedNavigationSourceInputExport()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Environment.GetEnvironmentVariable("WARLINE_PINNED_NAVIGATION_EXPORT_OUTPUT");
            ValidateS050PhaseAOutputForControls(projectRoot, output);
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            var observed = new Dictionary<UnityEngine.Object, S050AssetIdentity>();
            var locators = new JObject();
            var report = new JObject { ["kind"] = "PinnedCampaignNavigationInputExport", ["sourceFitCertified"] = false,
                ["missionReady"] = false, ["missionInitialized"] = false, ["liveOccupancyMeasured"] = false,
                ["packageAssetLocators"] = locators };
            Exception failure = null;
            bool created = false, disposed = false;
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Idle owned Editor required");
                string path = Environment.GetEnvironmentVariable("WARLINE_PINNED_NAVIGATION_EXPORT_REQUEST");
                string digest = Environment.GetEnvironmentVariable("WARLINE_PINNED_NAVIGATION_EXPORT_SHA256");
                Require(Path.IsPathRooted(path ?? "") && digest != null && digest.Length == 64 &&
                    digest.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f') && S050FileHash(path) == digest,
                    "Exact pinned navigation request required");
                pins[path] = digest;
                using var reader = new JsonTextReader(new StringReader(File.ReadAllText(path))) { DateParseHandling = DateParseHandling.None };
                var input = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                Require(!reader.Read(), "One navigation request document required");
                S050Keys(input, "schema_version", "kind", "source_path", "source_sha256", "grid_path", "grid_sha256",
                    "surface_path", "surface_sha256", "operation_map_id", "source_identity", "content_hash", "frame",
                    "selection", "source_fit_certified", "mission_ready");
                Require((int?)input["schema_version"] == 1 && (string)input["kind"] == "PinnedCampaignNavigationInputExport" &&
                    (bool?)input["source_fit_certified"] == false && (bool?)input["mission_ready"] == false,
                    "Explicit unqualified navigation request required");
                var selection = input["selection"] as JObject;
                S050Keys(selection, "path", "sha256");
                string selectionPath = (string)selection["path"], selectionSha = (string)selection["sha256"];
                Require(Path.IsPathRooted(selectionPath ?? "") && S050FileHash(selectionPath) == selectionSha,
                    "Pinned source selection evidence required");
                pins[selectionPath] = selectionSha;
                var selected = JObject.Parse(File.ReadAllText(selectionPath),
                    new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                Require((bool?)selected["compiler_accepted"] == true && (bool?)selected["world_allocated"] == false &&
                    (bool?)selected["source_fit_verified"] == false && (bool?)selected["mission_ready"] == false &&
                    (string)selected["operation_map_id"] == (string)input["operation_map_id"] &&
                    (string)selected["source_content_hash"] == (string)input["content_hash"],
                    "Actual unqualified compiled source selection required");
                foreach (string key in new[] { "source", "grid", "surface" })
                {
                    string assetPath = (string)input[key + "_path"];
                    Require(!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                        !assetPath.Split('/').Contains("..") && S050FileHash(Path.Combine(projectRoot, assetPath)) == (string)input[key + "_sha256"],
                        "Exact selected asset required: " + key);
                    S050PinAssetFile(assetPath, pins, locators);
                }
                string sourcePath = (string)input["source_path"], sourceSha = (string)input["source_sha256"], mapId = (string)input["operation_map_id"];
                var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
                Require(source != null && !EditorUtility.IsDirty(source) && source.OperationMapId == mapId &&
                    source.SourceIdentityHash == (string)input["source_identity"] && source.ContentHash == (string)input["content_hash"],
                    "Actual selected Campaign source required");
                Require(AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid) == (string)input["grid_path"] &&
                    source.GridMetadata.ContentHash == (string)input["grid_sha256"] &&
                    AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID) == (string)input["surface_path"] &&
                    source.SurfaceMetadata.ContentHash == (string)input["surface_sha256"], "Source-owned grid and surface required");
                var frame = input["frame"] as JObject;
                S050Keys(frame, "width", "height", "cell_size", "origin");
                Require(frame["width"]?.Type == JTokenType.Integer && frame["height"]?.Type == JTokenType.Integer &&
                    frame["cell_size"]?.Type == JTokenType.Integer && frame["origin"] is JArray origin && origin.Count == 3 &&
                    origin.All(x => x.Type == JTokenType.Integer), "Finite integer grid frame required");
                var expected = new GridConfig { Width = (int)frame["width"], Height = (int)frame["height"],
                    CellSize = (int)frame["cell_size"], Origin = new float3((int)frame["origin"][0], (int)frame["origin"][1], (int)frame["origin"][2]) };
                Require(expected.Width > 0 && expected.Height > 0 && checked(expected.Width * expected.Height) <= 2097152 &&
                    expected.CellSize > 0 && source.GridMetadata.Dimensions.x == expected.Width && source.GridMetadata.Dimensions.y == expected.Height &&
                    source.GridMetadata.CellSize == expected.CellSize && source.GridMetadata.Origin.x == expected.Origin.x &&
                    source.GridMetadata.Origin.y == expected.Origin.y && source.GridMetadata.Origin.z == expected.Origin.z,
                    "Source grid metadata differs from pinned frame");
                foreach (string scene in new[] { AssetDatabase.GUIDToAssetPath(source.NavigationMetadata.AuthoredSubSceneGuid), Support })
                {
                    Require(File.Exists(scene), "Selected baked scene required");
                    pins[scene] = Hash(scene); pins[scene + ".meta"] = Hash(scene + ".meta");
                    foreach (string dep in AssetDatabase.GetDependencies(scene, true)) S050CleanConsumedAsset(dep, pins, observed, locators);
                }
                foreach (string dep in AssetDatabase.GetDependencies(sourcePath, true)) S050CleanConsumedAsset(dep, pins, observed, locators);
                S050CleanConsumedAsset(sourcePath, pins, observed, locators);
                S050PinBuiltinEngine(pins);
                foreach (string code in new[] { "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.PinnedNavigationInputExport.cs",
                    "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S050FixedMapReadDomain.cs",
                    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceData.cs" }) pins[code] = Hash(code);
                report["capturedProjectRoot"] = projectRoot;
                report["unityVersion"] = Application.unityVersion;
                report["loadedRuntimeAssemblies"] = PhysicalInputLoadedOwners(pins);
                report["sourcePath"] = sourcePath; report["sourceSha256"] = sourceSha; report["operationMapId"] = mapId;
                report["sourceMetadata"] = JObject.Parse(EditorJsonUtility.ToJson(source));
                var world = StreamingWorld(); created = true;
                try
                {
                    Load(world, source.NavigationMetadata.AuthoredSubSceneGuid);
                    Load(world, AssetDatabase.AssetPathToGUID(Support));
                    var em = world.EntityManager;
                    using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad));
                    Require(grids.CalculateEntityCount() == 1, "Unique original selected grid required");
                    var grid = grids.GetSingleton<GridConfig>();
                    Require(grid.Width == expected.Width && grid.Height == expected.Height && grid.CellSize == expected.CellSize &&
                        math.all(grid.Origin == expected.Origin), "Actual baked grid frame differs");
                    var before = em.EntityOrderVersion;
                    var missing = new JArray();
                    var rows = (JArray)S050OriginalSnapshotHelper("CaptureAuthoredNavigationBlockers", em, missing);
                    Require(missing.Count == 0 && rows.Count <= 100000 && before == em.EntityOrderVersion,
                        "Unsupported navigation scalar or structural change");
                    report["fixedMapReadDomain"] = new JObject { ["kind"] = "OriginalCampaignNavigationRowsOnly",
                        ["sourceDefinitionSha256"] = sourceSha, ["operationMapId"] = mapId,
                        ["gridFrame"] = frame.DeepClone(), ["authoredNavigationBlockers"] = rows,
                        ["unsupportedSelectedNavigationValues"] = missing, ["structuralVersionBefore"] = before,
                        ["structuralVersionAfter"] = em.EntityOrderVersion, ["actorOrMissionStateExported"] = false,
                        ["sourceFitCertified"] = false, ["missionReady"] = false };
                }
                finally { world.Dispose(); disposed = !world.IsCreated; }
                report["result"] = "Collected";
            }
            catch (Exception error) { failure = error; report["result"] = "Failed"; report["exception"] = error.ToString(); }
            finally
            {
                if (report["loadedRuntimeAssemblies"] != null) try
                {
                    var after = PhysicalInputLoadedOwners(pins); report["loadedRuntimeAssembliesAfter"] = after;
                    Require(JToken.DeepEquals(report["loadedRuntimeAssemblies"], after), "Loaded owner assembly changed");
                }
                catch (Exception error) { failure = failure ?? error; report["result"] = "Failed"; report["loadedOwnerIdentityFailure"] = error.ToString(); }
                var dirty = new JArray();
                foreach (var pair in observed) try
                {
                    var asset = pair.Key; var before = pair.Value; Require(asset != null, "Consumed asset disappeared");
                    bool known = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
                    string assetPath = AssetDatabase.GetAssetPath(asset), type = asset.GetType().AssemblyQualifiedName;
                    bool shader = before.TypeName == typeof(Shader).AssemblyQualifiedName;
                    bool same = !shader || S050ShaderIdentityMatchesForControls(before.Known, before.Guid, before.LocalId,
                        before.Path, before.TypeName, known, guid, localId, assetPath, type);
                    bool isDirty = EditorUtility.IsDirty(asset);
                    dirty.Add(new JObject { ["path"] = assetPath, ["dirtyBefore"] = before.DirtyBefore, ["dirtyAfter"] = isDirty, ["shaderIdentityMatches"] = same });
                    Require(same && (!isDirty || !S050RequiresCleanAssetTypeForControls(asset.GetType())), "Consumed mechanical asset changed");
                }
                catch (Exception error) { failure = failure ?? error; report["result"] = "Failed"; report["consumedAssetFailure"] = error.ToString(); }
                var mutations = new JArray();
                foreach (var pin in pins) try { Require(S050FileHash(pin.Key) == pin.Value, "Consumed file changed: " + pin.Key); }
                catch (Exception error) { mutations.Add(error.ToString()); failure = failure ?? error; report["result"] = "Failed"; }
                report["readPins"] = JObject.FromObject(pins); report["consumedAssetDirtyCensus"] = dirty;
                report["sourceMutationFailures"] = mutations; report["worldCreated"] = created; report["worldDisposed"] = disposed;
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using var file = new FileStream(output, FileMode.CreateNew); using var writer = new StreamWriter(file);
                writer.Write(report.ToString());
            }
            if (failure != null) throw failure;
            Require(created && disposed, "Owned streaming World disposal required");
            Debug.Log("[PinnedCampaignNavigationInputExport] result=Collected source=" + (string)report["operationMapId"] + " WorldDisposed=True mission=0");
        }
    }
}
#endif
