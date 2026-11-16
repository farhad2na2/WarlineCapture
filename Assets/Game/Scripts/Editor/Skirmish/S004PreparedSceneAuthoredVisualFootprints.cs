#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    // Opt-in, read-only source traversal of authored building visual bounds in the prepared S004 scene.
    internal static class S004PreparedSceneAuthoredVisualFootprints
    {
        private const string ScenePath = "Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity";
        private const string InventoryKey = "WARLINE_S004_VISUAL_INVENTORY_INPUT";
        private const string OutputDirKey = "WARLINE_S004_AUTHORED_FOOTPRINT_OUTPUT_DIR";
        private const string MapId = "opmap.skirmish.cityedgeairfield_prepared";
        private const int ExpectedCount = 344;

        public static void CapturePreparedAuthoredVisualFootprints()
        {
            SceneSetup[] setup = null;
            Scene ownedScene = default;
            var worldsBefore = new List<World>();
            string outputDir = null;
            int exitCode = 1;
            bool setupCaptured = false;
            bool worldsCaptured = false;
            try
            {
                worldsBefore = CaptureWorlds();
                worldsCaptured = true;
                if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                    throw new InvalidOperationException("Capture requires Edit Mode.");
                if (EditorSceneManager.previewSceneCount != 0)
                    throw new InvalidOperationException("Existing preview scenes prevent fail-closed setup restoration.");
                foreach (Scene scene in LoadedScenes())
                    if (scene.isDirty)
                        throw new InvalidOperationException("Dirty loaded scene prevents additive capture: " + scene.path);

                outputDir = RequiredFreshOutputDirectory();
                string inventoryPath = RequiredAbsolute(InventoryKey);
                JObject inventory = JObject.Parse(File.ReadAllText(inventoryPath));
                if ((string)inventory["kind"] != "S004PreparedEntitiesVisualDependencyInventory" ||
                    (string)inventory["status"] != "PassedInventoryOnly")
                    throw new InvalidDataException("Prepared visual inventory kind/status mismatch.");
                string fullScenePath = Path.GetFullPath(ScenePath);
                JObject inputScene = inventory["input_scene"] as JObject;
                if (inputScene == null || Path.GetFullPath((string)inputScene["path"]) != fullScenePath ||
                    Sha256(File.ReadAllBytes(fullScenePath)) != (string)inputScene["sha256"])
                    throw new InvalidDataException("Prepared scene differs from pinned inventory input.");

                setup = EditorSceneManager.GetSceneManagerSetup();
                setupCaptured = true;
                if (setup.Any(item => item.isLoaded && item.path == ScenePath))
                    throw new InvalidOperationException("PreparedEntities is already loaded; refusing to close a scene not opened by this capture.");
                var pins = CollectPins(inventory, inventoryPath);
                ownedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                if (!ownedScene.IsValid() || !ownedScene.isLoaded || ownedScene.path != ScenePath || ownedScene.isDirty)
                    throw new InvalidOperationException("Exact clean PreparedEntities scene failed to open additively.");
                if (!WorldsEqual(worldsBefore, CaptureWorlds()))
                    throw new InvalidOperationException("Scene open changed ECS World ownership.");

                GridFrame grid = ResolveGrid(ownedScene);
                JArray buildings = CaptureBuildings(ownedScene, grid);
                if (buildings.Count != ExpectedCount)
                    throw new InvalidDataException("Expected exactly 344 prepared authored buildings; found " + buildings.Count);
                VerifyPinsUnchanged(pins);
                var report = new JObject
                {
                    ["kind"] = "S004PreparedSceneAuthoredVisualFootprints",
                    ["schema_version"] = 1,
                    ["status"] = "PassedSourceTraversal",
                    ["scope"] = "EditModeAdditiveSceneReadOnly_ExactProjectAuthoredFootprintOwnerAndBakerTraversal",
                    ["scene_path"] = ScenePath,
                    ["scene_sha256"] = pins[fullScenePath],
                    ["inventory_path"] = inventoryPath,
                    ["inventory_sha256"] = Sha256(File.ReadAllBytes(inventoryPath)),
                    ["grid"] = grid.Json,
                    ["building_count"] = buildings.Count,
                    ["buildings"] = buildings,
                    ["source_pins"] = PinJson(pins),
                    ["source_pin_rehash_before_after"] = true,
                    ["scene_loaded_additively"] = true,
                    ["scene_mutated_or_saved"] = false,
                    ["gameplay_world_created"] = false,
                    ["baker_executed"] = false,
                    ["scene_fit_claim"] = false,
                    ["mission_readiness"] = false,
                    ["visual_readiness"] = false
                };
                CloseAndRestore(ref ownedScene, setup);
                setupCaptured = false;
                if (!WorldsEqual(worldsBefore, CaptureWorlds()))
                    throw new InvalidOperationException("Capture cleanup changed ECS World ownership.");
                WriteFreshDirectory(outputDir, report);
                Debug.Log("[S004PreparedSceneAuthoredVisualFootprints] result=Passed buildings=" + buildings.Count + " output=" + outputDir);
                exitCode = 0;
            }
            catch (Exception error)
            {
                Debug.LogError("[S004PreparedSceneAuthoredVisualFootprints] result=Failed error=" + error.GetType().Name + ":" +
                    error.Message.Replace('\n', ' ').Replace('\r', ' '));
            }
            finally
            {
                try
                {
                    if (ownedScene.IsValid() && ownedScene.isLoaded)
                    {
                        if (ownedScene.isDirty)
                            throw new InvalidOperationException("Owned scene became dirty; refusing to discard its state.");
                        if (!EditorSceneManager.CloseScene(ownedScene, true))
                            throw new InvalidOperationException("Could not close owned additive scene.");
                    }
                    if (setupCaptured)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(setup);
                        VerifySetup(setup);
                    }
                    if (worldsCaptured && !WorldsEqual(worldsBefore, CaptureWorlds()))
                        throw new InvalidOperationException("Final cleanup changed ECS World ownership.");
                }
                catch (Exception cleanupError)
                {
                    Debug.LogError("[S004PreparedSceneAuthoredVisualFootprints] cleanup=Failed error=" + cleanupError.Message);
                    exitCode = 1;
                }
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }

        private static JArray CaptureBuildings(Scene scene, GridFrame grid)
        {
            var owners = new List<OperationMapBuildingAuthoring>();
            foreach (GameObject root in scene.GetRootGameObjects())
                owners.AddRange(root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true));
            if (owners.Count != ExpectedCount) throw new InvalidDataException("Prepared scene authoring count mismatch: " + owners.Count);
            var rows = new JArray();
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var placementIndices = new HashSet<int>();
            foreach (OperationMapBuildingAuthoring owner in owners.OrderBy(x => x.PlacementIndex))
            {
                if (owner.OperationMapId != MapId || string.IsNullOrEmpty(owner.StableId) ||
                    !stableIds.Add(owner.StableId) || !placementIndices.Add(owner.PlacementIndex))
                    throw new InvalidDataException("Missing/duplicate prepared building stable identity.");
                if (owner.IntactVisualRoot == null || owner.IntactVisualRoot.transform.parent != owner.transform)
                    throw new InvalidDataException("Intact visual root is not an immediate child: " + owner.StableId);
                SerializedObject serialized = new SerializedObject(owner);
                Vector2Int serializedOrigin = ReadVector2Int(serialized, "originCell");
                Vector2Int serializedFootprint = ReadVector2Int(serialized, "generatedFootprintCells");
                Bounds bounds = MeasureBakerVisualBounds(owner.IntactVisualRoot, out JArray meshRows);
                int2 origin = new int2(serializedOrigin.x, serializedOrigin.y);
                Vector2Int bakerInputFootprint = owner.FootprintCells;
                int2 footprint = new int2(bakerInputFootprint.x, bakerInputFootprint.y);
                ProjectWithOwner(bounds, grid.Origin, grid.CellSize, ref origin, ref footprint);
                if (owner.PlacementIndex != rows.Count)
                    throw new InvalidDataException("Prepared placement indices are not dense 0..343.");
                rows.Add(new JObject
                {
                    ["stable_id"] = owner.StableId,
                    ["operation_map_id"] = owner.OperationMapId,
                    ["placement_index"] = owner.PlacementIndex,
                    ["source_global_object_id"] = owner.SourceGlobalObjectId,
                    ["faction_id"] = owner.FactionId,
                    ["blocker_policy"] = (byte)owner.BlockerPolicy,
                    ["serialized_origin_cell"] = Vec(serializedOrigin),
                    ["serialized_generated_footprint_cells"] = Vec(serializedFootprint),
                    ["baker_input_footprint_cells"] = Vec(bakerInputFootprint),
                    ["calculated_origin_cell"] = new JArray(origin.x, origin.y),
                    ["calculated_footprint_cells"] = new JArray(footprint.x, footprint.y),
                    ["world_visual_bounds"] = BoundsJson(bounds),
                    ["mesh_renderer_inputs"] = meshRows,
                    ["visual_traversal"] = "EnabledActiveInHierarchyMeshRenderer+MeshFilter.sharedMesh.bounds.TransformPoint(8Corners)",
                    ["baker_executed"] = false
                });
            }
            if (placementIndices.Count != ExpectedCount || stableIds.Count != ExpectedCount)
                throw new InvalidDataException("Prepared stable identity/placement coverage incomplete.");
            return rows;
        }

        private static Bounds MeasureBakerVisualBounds(GameObject root, out JArray meshRows)
        {
            bool found = false;
            Bounds bounds = default;
            meshRows = new JArray();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    !renderer.TryGetComponent<MeshFilter>(out MeshFilter filter) || filter.sharedMesh == null) continue;
                Bounds mesh = filter.sharedMesh.bounds;
                RequireFinite(mesh.center, "mesh center"); RequireFinite(mesh.size, "mesh size");
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh, out string meshGuid, out long meshLocalId))
                    throw new InvalidDataException("Mesh persistent identity unavailable: " + filter.sharedMesh.name);
                meshRows.Add(new JObject { ["renderer_path"] = HierarchyPath(renderer.transform, root.transform),
                    ["mesh_name"] = filter.sharedMesh.name, ["mesh_asset_path"] = AssetDatabase.GetAssetPath(filter.sharedMesh),
                    ["mesh_guid"] = meshGuid, ["mesh_local_file_id"] = meshLocalId,
                    ["mesh_local_bounds"] = BoundsJson(mesh), ["renderer_local_to_world"] = MatrixJson(renderer.transform.localToWorldMatrix) });
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                {
                    Vector3 point = renderer.transform.TransformPoint(new Vector3(x == 0 ? mesh.min.x : mesh.max.x,
                        y == 0 ? mesh.min.y : mesh.max.y, z == 0 ? mesh.min.z : mesh.max.z));
                    RequireFinite(point, "transformed mesh bounds point");
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) throw new InvalidDataException("No active enabled MeshRenderer/sharedMesh bounds on intact visual root: " + root.name);
            return bounds;
        }

        private static void ProjectWithOwner(Bounds bounds, Vector3 origin, float cellSize, ref int2 cell, ref int2 footprint)
        {
            MethodInfo method = typeof(OperationMapBuildingAuthoring).GetMethod("ProjectAuthoredFootprint",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException("ProjectAuthoredFootprint owner method unavailable.");
            object[] args = { bounds, origin, cellSize, cell, footprint };
            try { method.Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
            cell = (int2)args[3]; footprint = (int2)args[4];
        }

        private static GridFrame ResolveGrid(Scene scene)
        {
            var grids = new List<GridAuthoring>();
            foreach (GameObject root in scene.GetRootGameObjects()) grids.AddRange(root.GetComponentsInChildren<GridAuthoring>(true));
            if (grids.Count > 1) throw new InvalidDataException("Expected at most one owning-scene GridAuthoring; found " + grids.Count);
            GridAuthoringConfig config;
            Vector3 origin;
            if (grids.Count == 0)
            {
                const string fallback = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";
                config = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(fallback);
                if (config == null) throw new InvalidDataException("Baker fallback grid config missing: " + fallback);
                origin = config.Origin;
            }
            else
            {
                var owner = grids[0];
                if (owner.gameObject.scene != scene) throw new InvalidDataException("Grid authoring belongs to another scene.");
                var serialized = new SerializedObject(owner);
                config = serialized.FindProperty("config")?.objectReferenceValue as GridAuthoringConfig;
                if (config == null) throw new InvalidDataException("Owning grid has no config.");
                origin = config.Origin;
            }
            if (config.Width <= 0 || config.Height <= 0 || config.CellSize <= 0 || !Finite(config.CellSize))
                throw new InvalidDataException("Owning grid frame is invalid.");
            RequireFinite(origin, "grid origin");
            string assetPath = AssetDatabase.GetAssetPath(config);
            string guid; long localId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config, out guid, out localId))
                throw new InvalidDataException("Grid configuration persistent identity unavailable.");
            return new GridFrame(origin, config.CellSize, config.Width, config.Height, assetPath, guid, localId, config.name);
        }

        private static Dictionary<string, string> CollectPins(JObject inventory, string inventoryPath)
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            paths.Add(Path.GetFullPath(ScenePath)); paths.Add(Path.GetFullPath(ScenePath + ".meta"));
            paths.Add(Path.GetFullPath(inventoryPath));
            foreach (string dependency in AssetDatabase.GetDependencies(ScenePath, true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    string absolute = Path.GetFullPath(dependency);
                    paths.Add(absolute);
                    if (File.Exists(absolute + ".meta")) paths.Add(absolute + ".meta");
                }
            var binding = inventory["source_map_binding_context"] as JObject;
            foreach (string key in new[] { "runtime_binding_scene", "operation_map_asset" })
            {
                string value = (string)binding?[key];
                if (!string.IsNullOrEmpty(value)) paths.Add(Path.GetFullPath(value));
            }
            foreach (string source in new[] { "Assets/Game/Scripts/Authorings/OperationMapBuildingAuthoring.Footprint.cs",
                         "Assets/Game/Scripts/Authorings/OperationMapBuildingAuthoring.cs", "Assets/Game/Scripts/Authorings/GridAuthoring.cs" })
                paths.Add(Path.GetFullPath(source));
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Required source pin missing.", path);
                pins.Add(path, Sha256(File.ReadAllBytes(path)));
            }
            return pins;
        }

        private static void VerifyPinsUnchanged(Dictionary<string, string> pins)
        {
            foreach (var pin in pins)
                if (!File.Exists(pin.Key) || Sha256(File.ReadAllBytes(pin.Key)) != pin.Value)
                    throw new InvalidDataException("Source pin changed during scene capture: " + pin.Key);
        }

        private static JObject PinJson(Dictionary<string, string> pins)
        {
            var result = new JObject(); foreach (var pin in pins) result[pin.Key] = pin.Value; return result;
        }
        private static Vector2Int ReadVector2Int(SerializedObject serialized, string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.propertyType != SerializedPropertyType.Vector2Int)
                throw new InvalidDataException("Serialized Vector2Int field missing: " + name);
            return property.vector2IntValue;
        }
        private static JArray Vec(Vector2Int value) => new JArray(value.x, value.y);
        private static JArray MatrixJson(Matrix4x4 matrix) { var values = new JArray(); for (int i = 0; i < 16; i++) values.Add(matrix[i]); return values; }
        private static string HierarchyPath(Transform value, Transform root)
        {
            var parts = new List<string>();
            for (Transform current = value; current != null; current = current.parent)
            { parts.Add(current.name); if (current == root) break; }
            parts.Reverse(); return string.Join("/", parts);
        }
        private static JObject BoundsJson(Bounds b) => new JObject { ["center"] = new JArray(b.center.x, b.center.y, b.center.z),
            ["size"] = new JArray(b.size.x, b.size.y, b.size.z), ["min"] = new JArray(b.min.x, b.min.y, b.min.z),
            ["max"] = new JArray(b.max.x, b.max.y, b.max.z) };
        private static IEnumerable<Scene> LoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i); if (scene.IsValid() && scene.isLoaded) yield return scene;
            }
        }
        private static bool WorldsEqual(IReadOnlyList<World> expected, IReadOnlyList<World> actual)
        {
            if (expected.Count != actual.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (!ReferenceEquals(expected[i], actual[i])) return false;
            return true;
        }
        private static List<World> CaptureWorlds()
        {
            var worlds = new List<World>();
            foreach (World world in World.All) worlds.Add(world);
            return worlds;
        }
        private static void CloseAndRestore(ref Scene owned, SceneSetup[] setup)
        {
            if (owned.IsValid() && owned.isLoaded)
            {
                if (owned.isDirty) throw new InvalidOperationException("Owned scene became dirty; refusing to close.");
                if (!EditorSceneManager.CloseScene(owned, true)) throw new InvalidOperationException("Could not close owned scene.");
            }
            owned = default;
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            VerifySetup(setup);
        }
        private static void VerifySetup(SceneSetup[] expected)
        {
            SceneSetup[] actual = EditorSceneManager.GetSceneManagerSetup();
            if (actual.Length != expected.Length) throw new InvalidOperationException("Original scene setup length was not restored.");
            for (int i = 0; i < actual.Length; i++)
                if (actual[i].path != expected[i].path || actual[i].isLoaded != expected[i].isLoaded || actual[i].isActive != expected[i].isActive)
                    throw new InvalidOperationException("Original scene setup was not restored exactly.");
        }
        private static string RequiredAbsolute(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value) || !Path.IsPathRooted(value)) throw new InvalidOperationException(key + " must be absolute.");
            return Path.GetFullPath(value);
        }
        private static string RequiredFreshOutputDirectory()
        {
            string path = RequiredAbsolute(OutputDirKey);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string logs = Path.GetFullPath(Path.Combine(root, "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(logs, StringComparison.Ordinal) || Directory.Exists(path) || File.Exists(path))
                throw new InvalidOperationException("Fresh output directory under project Logs/SkirmishProduction is required.");
            return path;
        }
        private static void WriteFreshDirectory(string directory, JObject report)
        {
            Directory.CreateDirectory(directory);
            using (var stream = new FileStream(Path.Combine(directory, "authored-visual-footprints.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        }
        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var b = new StringBuilder(); foreach (byte value in sha.ComputeHash(bytes)) b.Append(value.ToString("x2")); return b.ToString();
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void RequireFinite(Vector3 v, string what)
        {
            if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z)) throw new InvalidDataException("Nonfinite " + what);
        }

        private readonly struct GridFrame
        {
            public readonly Vector3 Origin; public readonly float CellSize; public readonly int Width, Height;
            public readonly JObject Json;
            public GridFrame(Vector3 origin, float size, int width, int height, string path, string guid, long localId, string name)
            {
                Origin = origin; CellSize = size; Width = width; Height = height;
                Json = new JObject { ["asset_path"] = path, ["guid"] = guid, ["local_file_id"] = localId,
                    ["name"] = name, ["origin"] = new JArray(origin.x, origin.y, origin.z),
                    ["cell_size"] = size, ["width"] = width, ["height"] = height };
            }
        }
    }
}
#endif
