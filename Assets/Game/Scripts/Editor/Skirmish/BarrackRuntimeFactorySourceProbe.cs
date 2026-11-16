#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using Game.Authoring;
using Game.Composition;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Read-only direct asset probe for the current Barrack factory. It loads only
    /// disposable prefab contents; it does not open a mission scene or create a World.
    /// </summary>
    public static class BarrackRuntimeFactorySourceProbe
    {
        private const string OutputEnvironment = "WARLINE_BARRACK_RUNTIME_FACTORY_PROBE_OUTPUT";
        private const string PrefabPath = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
        private const string PrefabGuid = "683195b1e21c94df7add42ab26cd2a33";
        private const string Marker = "[BarrackRuntimeFactorySourceProbe] result=Passed scope=direct_asset_factory_source_only";
        private static readonly string[] RequiredCodeSources =
        {
            "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingModelBounds.cs",
            "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
            "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs",
            "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
            "Assets/Game/Scripts/RuntimeState/BuildingDefinition.cs",
            "Assets/Game/Scripts/Systems/BuildingDefinitionProductionRecipes.cs",
            "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs",
            "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs",
            "Assets/Game/Scripts/Systems/BuildingRunwaySystem.PrefabGeometry.cs",
            "Assets/Game/Scripts/Components/BuildingProducerCoreProjection.cs",
            "Assets/Game/Scripts/Game.Runtime.asmdef",
            "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
            "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
            "Assets/Game/Scripts/Editor/Skirmish/BarrackRuntimeFactorySourceProbe.cs"
        };

        public static void RunBarrackRuntimeFactorySourceProbe()
        {
            int exitCode = 1;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle Editor required; play mode is forbidden");
                if (EditorApplication.isCompiling)
                    throw new InvalidOperationException("Unity script compilation must be idle");
                World[] worldsBefore = World.All.ToArray();
                Scene activeSceneBefore = SceneManager.GetActiveScene();
                int sceneCountBefore = SceneManager.sceneCount;
                string[] sceneSetupBefore = EditorSceneManager.GetSceneManagerSetup()
                    .Select(setup => setup.path + "|" + setup.isLoaded + "|" + setup.isActive).ToArray();

                string output = Environment.GetEnvironmentVariable(OutputEnvironment);
                if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                    throw new InvalidOperationException(OutputEnvironment + " must be an absolute fresh JSON path");
                output = Path.GetFullPath(output);
                if (File.Exists(output) || !Directory.Exists(Path.GetDirectoryName(output)))
                    throw new InvalidOperationException("Output must be fresh and its parent directory must exist");

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null || AssetDatabase.GetAssetPath(prefab) != PrefabPath || prefab.name != "Building_Barrack")
                    throw new InvalidDataException("Exact current Barrack prefab root required");
                string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
                if (!string.Equals(guid, PrefabGuid, StringComparison.Ordinal) ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string objectGuid, out long localId) ||
                    !string.Equals(objectGuid, guid, StringComparison.Ordinal) || localId <= 0)
                    throw new InvalidDataException("Current imported Barrack prefab GUID/localID identity unresolved");
                UnityEngine.Object[] roots = AssetDatabase.LoadAllAssetsAtPath(PrefabPath)
                    .Where(value => value is GameObject)
                    .Where(value => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string candidateGuid, out long candidateId) &&
                        candidateGuid == guid && candidateId == localId).ToArray();
                if (roots.Length != 1 || !ReferenceEquals(roots[0], prefab))
                    throw new InvalidDataException("Barrack GUID/localID must identify exactly one loaded prefab GameObject");
                if (EditorUtility.IsDirty(prefab))
                    throw new InvalidOperationException("Barrack prefab must be clean before readback");
                BuildingDefinitionAuthoring sourceAuthoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
                if (sourceAuthoring == null) throw new InvalidDataException("Barrack source root authoring component is required");
                Vector2Int serializedAuthoringFootprint = sourceAuthoring.ConfiguredFootprintCells;
                if (!BuildingModelBounds.TryMeasure(prefab, out Bounds assetVisualBounds))
                    throw new InvalidDataException("Original BuildingModelBounds could not measure the AssetDatabase prefab");
                Vector2Int assetVisualFootprint = BuildingModelBounds.EnclosingCells(assetVisualBounds);

                string[] dependencies = AssetDatabase.GetDependencies(PrefabPath, true)
                    .Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                var pins = new SortedDictionary<string, string>(StringComparer.Ordinal);
                var dependencyRows = new JArray();
                foreach (string dependency in dependencies)
                {
                    if (!dependency.StartsWith("Assets/", StringComparison.Ordinal))
                        throw new InvalidDataException("Non-Assets transitive dependency requires explicit package pin support: " + dependency);
                    PinFile(dependency, pins);
                    PinFile(dependency + ".meta", pins);
                    UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(dependency);
                    if (asset != null && EditorUtility.IsDirty(asset))
                        throw new InvalidOperationException("Dirty prefab dependency rejected: " + dependency);
                    dependencyRows.Add(new JObject
                    {
                        ["path"] = dependency,
                        ["sha256"] = pins[dependency],
                        ["meta_sha256"] = pins[dependency + ".meta"]
                    });
                }
                foreach (string source in RequiredCodeSources)
                {
                    PinFile(source, pins);
                    PinFile(source + ".meta", pins);
                }
                foreach (string source in new[] { "ProjectSettings/ProjectVersion.txt", "Packages/manifest.json", "Packages/packages-lock.json" })
                    PinFile(source, pins);
                var assemblyRows = new JArray
                {
                    PinLoadedAssembly(typeof(BuildingDefinitionPrefabSystemHelper).Assembly, "Game.Runtime", pins),
                    PinLoadedAssembly(typeof(BuildingDefinitionAuthoringMetadataPrefabSystemHelper).Assembly,
                        "Game.Composition", pins),
                    PinLoadedAssembly(typeof(BarrackRuntimeFactorySourceProbe).Assembly, "Game.Editor", pins)
                };

                Vector2Int contentsAuthoringFootprint;
                Vector2Int contentsVisualFootprint;
                Bounds contentsVisualBounds;
                Vector2Int factoryFootprint;
                GameObject contentsRoot = null;
                try
                {
                    contentsRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                    if (contentsRoot == null || contentsRoot.name != "Building_Barrack")
                        throw new InvalidDataException("Exact disposable Barrack prefab contents root required");
                    BuildingDefinitionAuthoring contentsAuthoring = contentsRoot.GetComponent<BuildingDefinitionAuthoring>();
                    if (contentsAuthoring == null) throw new InvalidDataException("Disposable Barrack root authoring component required");
                    if (!BuildingModelBounds.TryMeasure(contentsRoot, out contentsVisualBounds))
                        throw new InvalidDataException("Original BuildingModelBounds could not measure disposable prefab contents");
                    contentsVisualFootprint = BuildingModelBounds.EnclosingCells(contentsVisualBounds);

                    var definitions = new BuildingDefinitionPrefabSystemHelper();
                    definitions.ConfigureAuthoringMetadataResolvers(
                        BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                        BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
                    var runway = new BuildingRunwaySystem();
                    BuildingDefinition definition = definitions.CreateRuntimeBuildingDefinition(
                        contentsRoot, contentsRoot.name, string.Empty, Vector2Int.one, 1, runway);
                    if (definition == null) throw new InvalidDataException("Original runtime factory returned null");
                    contentsAuthoringFootprint = contentsAuthoring.ConfiguredFootprintCells;
                    factoryFootprint = definition.FootprintCells;
                }
                finally
                {
                    if (contentsRoot != null) PrefabUtility.UnloadPrefabContents(contentsRoot);
                }

                if (assetVisualFootprint != contentsVisualFootprint || factoryFootprint != contentsVisualFootprint)
                    throw new InvalidDataException("Asset and disposable contents visual measurements/factory footprint do not agree");
                if (EditorUtility.IsDirty(prefab))
                    throw new InvalidOperationException("Original loaded prefab became dirty during disposable contents readback");
                World[] worldsAfter = World.All.ToArray();
                if (worldsBefore.Length != worldsAfter.Length ||
                    worldsBefore.Where((world, index) => !ReferenceEquals(world, worldsAfter[index])).Any())
                    throw new InvalidOperationException("Probe created, disposed, or reordered an ECS World");
                Scene activeSceneAfter = SceneManager.GetActiveScene();
                if (activeSceneBefore.handle != activeSceneAfter.handle || sceneCountBefore != SceneManager.sceneCount ||
                    !sceneSetupBefore.SequenceEqual(EditorSceneManager.GetSceneManagerSetup()
                        .Select(setup => setup.path + "|" + setup.isLoaded + "|" + setup.isActive)))
                    throw new InvalidOperationException("Probe changed the open scene setup");

                var payload = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "BarrackRuntimeFactoryDirectAssetSourceProbe",
                    ["status"] = "PassedDirectAssetFactorySourceProbe",
                    ["authority"] = "Current Unity AssetDatabase source identity, evaluated prefab contents, and original CreateRuntimeBuildingDefinition output",
                    ["unity_version"] = Application.unityVersion,
                    ["prefab"] = new JObject
                    {
                        ["path"] = PrefabPath,
                        ["guid"] = guid,
                        ["local_id"] = localId,
                        ["identity_basis"] = "AssetDatabase.TryGetGUIDAndLocalFileIdentifier on exact loaded prefab root",
                        ["asset_sha256"] = pins[PrefabPath],
                        ["meta_sha256"] = pins[PrefabPath + ".meta"]
                    },
                    ["footprint"] = new JObject
                    {
                        ["asset_serialized_authoring_cells"] = new JArray(serializedAuthoringFootprint.x, serializedAuthoringFootprint.y),
                        ["contents_effective_authoring_cells"] = new JArray(contentsAuthoringFootprint.x, contentsAuthoringFootprint.y),
                        ["asset_visual_bounds"] = BoundsJson(assetVisualBounds),
                        ["asset_visual_cells"] = new JArray(assetVisualFootprint.x, assetVisualFootprint.y),
                        ["contents_visual_bounds"] = BoundsJson(contentsVisualBounds),
                        ["contents_visual_cells"] = new JArray(contentsVisualFootprint.x, contentsVisualFootprint.y),
                        ["factory_cells"] = new JArray(factoryFootprint.x, factoryFootprint.y),
                        ["factory_method"] = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition",
                        ["bound_owner"] = "BuildingModelBounds.TryMeasure/EnclosingCells",
                        ["factory_target"] = "disposable PrefabUtility.LoadPrefabContents root; not AssetDatabase persistent object",
                        ["asset_contents_geometry_equivalence"] = true,
                        ["authored_configured_cells_used_as_factory_authority"] = false,
                        ["resolver_may_mutate_temporary_authoring_instance"] = true,
                        ["temporary_contents_unloaded_without_save"] = true
                    },
                    ["dependency_paths"] = dependencyRows,
                    ["loaded_assemblies"] = assemblyRows,
                    ["source_pins"] = JObject.FromObject(pins),
                    ["scope"] = new JObject
                    {
                        ["direct_prefab_asset_only"] = true,
                        ["mission_scene_opened"] = false,
                        ["temporary_prefab_contents_scene_used"] = true,
                        ["open_scene_setup_changed"] = false,
                        ["ecs_world_created_by_probe"] = false,
                        ["mission_setup_invoked"] = false,
                        ["asset_repair_invoked"] = false,
                        ["source_files_changed"] = false,
                        ["loaded_source_prefab_dirty_after_probe"] = false,
                        ["runtime_gameplay_admission"] = false,
                        ["mission_ready"] = false
                    }
                };

                Recheck(prefab, guid, localId, dependencies, pins);
                if (File.Exists(output)) throw new InvalidOperationException("Output appeared before exclusive publication");
                byte[] bytes = Encoding.UTF8.GetBytes(payload.ToString(Formatting.Indented) + "\n");
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    stream.Write(bytes, 0, bytes.Length);
                Debug.Log(Marker + " prefab=1 identity=1 footprint=1 dependencies=" + dependencies.Length + " mission=0 world=0");
                exitCode = 0;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[BarrackRuntimeFactorySourceProbe] result=Failed " + error.Message);
            }
            finally
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            }
        }

        private static void PinFile(string relativePath, SortedDictionary<string, string> pins)
        {
            if (!File.Exists(relativePath)) throw new FileNotFoundException("Required pinned source missing", relativePath);
            pins[relativePath] = Sha256(File.ReadAllBytes(relativePath));
        }

        private static JObject PinLoadedAssembly(Assembly assembly, string expectedName,
            SortedDictionary<string, string> pins)
        {
            if (assembly == null || assembly.GetName().Name != expectedName || string.IsNullOrEmpty(assembly.Location))
                throw new InvalidOperationException("Expected loaded owner assembly unavailable: " + expectedName);
            string path = Path.GetRelativePath(Directory.GetCurrentDirectory(), assembly.Location)
                .Replace(Path.DirectorySeparatorChar, '/');
            if (path.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(path))
                throw new InvalidOperationException("Loaded owner assembly path is outside the project: " + assembly.Location);
            PinFile(path, pins);
            return new JObject { ["name"] = expectedName, ["path"] = path, ["sha256"] = pins[path] };
        }

        private static void Recheck(GameObject prefab, string guid, long localId, string[] dependencies,
            SortedDictionary<string, string> pins)
        {
            if (EditorUtility.IsDirty(prefab) || AssetDatabase.GetAssetPath(prefab) != PrefabPath ||
                AssetDatabase.AssetPathToGUID(PrefabPath) != guid ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string afterGuid, out long afterId) ||
                afterGuid != guid || afterId != localId)
                throw new InvalidOperationException("Barrack identity or dirty state changed during probe");
            string[] afterDependencies = AssetDatabase.GetDependencies(PrefabPath, true)
                .Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (!dependencies.SequenceEqual(afterDependencies, StringComparer.Ordinal))
                throw new InvalidOperationException("Barrack dependency closure changed during probe");
            foreach (string dependency in dependencies)
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(dependency);
                if (asset != null && EditorUtility.IsDirty(asset))
                    throw new InvalidOperationException("Prefab dependency became dirty during probe: " + dependency);
            }
            foreach (KeyValuePair<string, string> pin in pins)
                if (!File.Exists(pin.Key) || Sha256(File.ReadAllBytes(pin.Key)) != pin.Value)
                    throw new InvalidOperationException("Pinned source changed during probe: " + pin.Key);
        }

        private static JObject BoundsJson(Bounds bounds) => new JObject
        {
            ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
            ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
            ["min"] = new JArray(bounds.min.x, bounds.min.y, bounds.min.z),
            ["max"] = new JArray(bounds.max.x, bounds.max.y, bounds.max.z)
        };

        private static string Sha256(byte[] value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(value)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
#endif
