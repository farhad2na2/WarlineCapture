#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        static readonly string[] StartingBuildingKeys = { "Building_Barrack", "Building_GroundStaging", "Building_OilPump",
            "Building_Refinery", "Building_Fuel_Bladder", "Building_Ammunition_Depot", "Building_GuardTower", "Building_Satelite_Dish" };

        // Source metadata through the ORIGINAL definition factory. No player build,
        // mission, native instance creation, production tick or outcome is involved.
        public static void RunBuildingReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-skirmishBuildingReferenceData");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string catalogId = BuildingReferenceArgument(args, "-skirmishBuildingReferenceCatalog", "S002");
            string seedText = BuildingReferenceArgument(args, "-skirmishBuildingReferenceSeed", "104731");
            if (!int.TryParse(seedText, out int seed)) throw new ArgumentException("Explicit integer source seed required");
            string keyText = BuildingReferenceArgument(args, "-skirmishBuildingReferenceKeys", null);
            string[] keys = keyText == null ? StartingBuildingKeys : keyText.Split(',').Select(k => k.Trim()).ToArray();
            if (keys.Length < 1 || keys.Length > 32 || keys.Any(k => string.IsNullOrEmpty(k) || k.Length > 128) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("Unique bounded configured building keys required");
            bool captureGeometry = Array.IndexOf(args, "-skirmishBuildingReferenceGeometry") >= 0;
            // TryQueueExpanded in SkirmishLaunchProjection loads the shared MAP
            // preset (index0), even when the S002 scenario selection is index4.
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null) throw new InvalidOperationException("Actual S002 placement preset missing");
            string buildingCatalog = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            if (string.IsNullOrEmpty(buildingCatalog)) throw new InvalidOperationException("S002 placement preset source path missing");
            // Match.unity's actual serialized buildingPlacementConfig reference.
            string sceneCatalog = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(sceneCatalog);
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null) throw new InvalidOperationException("Actual Match scene catalog missing");
            string unitCatalog = AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig);
            if (string.IsNullOrEmpty(unitCatalog)) throw new InvalidOperationException("Actual S002 unit registry source missing");
            var expansion = SkirmishExpansionCatalogConfig.Load();
            var definitionSource = expansion?.Definitions.FirstOrDefault(d => d != null && d.CatalogId == catalogId);
            if (definitionSource == null) throw new InvalidOperationException("Actual S002 scenario definition missing");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError)) throw new InvalidOperationException(matrixError);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definitionSource.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definitionSource, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, seed,
                manifest, matrix, out var setup, out var reasons)) throw new InvalidOperationException("Original S002 compiler rejected source: " + string.Join(";", reasons));
            RunBuildingReferenceDataFor(setup, output, keys, captureGeometry,
                SkirmishPresetConfig.DesertBaseScenarioIndex, null, null, null, null, null, null);
        }

        // Exact compiler-selected handoff for authored mission inputs. The setup
        // and asset identities are validated by the same owners as the unit path;
        // building keys come only from the selected setup's composed spawnables.
        public static void RunCompilerSelectedBuildingReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs();
            int missionAt = Array.IndexOf(args, "-skirmishUnitScenario");
            int seedAt = Array.IndexOf(args, "-skirmishUnitSeed");
            int requestAt = Array.IndexOf(args, "-skirmishUnitPhysicalInputRequest");
            int requestShaAt = Array.IndexOf(args, "-skirmishUnitPhysicalInputRequestSha256");
            int resultAt = Array.IndexOf(args, "-skirmishUnitCompilerResult");
            int resultShaAt = Array.IndexOf(args, "-skirmishUnitCompilerResultSha256");
            int outputAt = Array.IndexOf(args, "-skirmishBuildingReferenceData");
            if (missionAt < 0 || missionAt + 1 >= args.Length || seedAt < 0 || seedAt + 1 >= args.Length ||
                requestAt < 0 || requestAt + 1 >= args.Length || requestShaAt < 0 || requestShaAt + 1 >= args.Length ||
                resultAt < 0 || resultAt + 1 >= args.Length || resultShaAt < 0 || resultShaAt + 1 >= args.Length ||
                outputAt < 0 || outputAt + 1 >= args.Length ||
                !SkirmishMissionDispatch.Canonical(args[missionAt + 1]) ||
                !int.TryParse(args[seedAt + 1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int seed))
                throw new ArgumentException("Exact compiler-selected mission, result/request pins and fresh building output required");
            string catalogId = args[missionAt + 1];
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[outputAt + 1]);
            string requestPath = Path.GetFullPath(args[requestAt + 1]);
            string requestSha = args[requestShaAt + 1];
            string resultPath = Path.GetFullPath(args[resultAt + 1]);
            string resultSha = args[resultShaAt + 1];
            if (!IsSha256(requestSha) || !IsSha256(resultSha) ||
                SkirmishOfflineDataExporter.HashFile(requestPath) != requestSha ||
                SkirmishOfflineDataExporter.HashFile(resultPath) != resultSha)
                throw new InvalidDataException("Compiler-selected physical request/result SHA pin changed");
            JObject physicalRequest = ReadUniqueObject(requestPath);
            JObject selection = ReadUniqueObject(resultPath);
            var selectionDescriptor = physicalRequest["selection"] as JObject;
            if (selectionDescriptor == null || selectionDescriptor.Count != 2 ||
                (string)selectionDescriptor["path"] != resultPath || (string)selectionDescriptor["sha256"] != resultSha ||
                (string)selection["catalog_id"] != catalogId || (int?)selection["seed"] != seed)
                throw new InvalidDataException("Physical request, exact compiler selection and requested mission identity differ");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var compilerPins = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [requestPath] = requestSha,
                [resultPath] = resultSha
            };
            JObject authoringRequest = CompilerSelectedAuthoringEvidence.Validate(physicalRequest, selection,
                resultPath, projectRoot, compilerPins, out SkirmishResolvedSetup selectedSetup,
                out JArray hydratedAssetGuids);
            JObject assetReadback = CompilerSelectedUnityAssetReadback.Validate(authoringRequest, selection,
                projectRoot, compilerPins, hydratedAssetGuids);
            string sourcePresetPath = ResolveCompilerSelectedBuildingPreset(selection, projectRoot);
            RunBuildingReferenceDataFor(selectedSetup, output, null, false, -1, sourcePresetPath,
                new JObject { ["path"] = requestPath, ["sha256"] = requestSha },
                new JObject { ["path"] = resultPath, ["sha256"] = resultSha },
                selectionDescriptor, assetReadback, compilerPins);
        }

        private static string ResolveCompilerSelectedBuildingPreset(JObject selection, string projectRoot)
        {
            var inputPins = selection["input_pins"] as JObject;
            if (inputPins == null || inputPins.Count == 0)
                throw new InvalidDataException("Compiler-selected input pins required to identify the source preset");
            string root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string rootPrefix = root + Path.DirectorySeparatorChar;
            var candidates = new List<Tuple<string, SkirmishPresetConfig>>();
            foreach (var pin in inputPins.Properties())
            {
                string path = Path.GetFullPath(pin.Name);
                if (!path.StartsWith(rootPrefix, StringComparison.Ordinal)) continue;
                string relative = path.Substring(rootPrefix.Length).Replace('\\', '/');
                if (!relative.StartsWith("Assets/Game/Resources/", StringComparison.Ordinal) ||
                    !relative.EndsWith(".asset", StringComparison.Ordinal)) continue;
                var preset = AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(relative);
                if (preset != null) candidates.Add(Tuple.Create(relative, preset));
            }
            if (candidates.Count != 1)
                throw new InvalidDataException("One compiler-pinned SkirmishPresetConfig resource is required; unsupported or ambiguous source baseline");

            string presetPath = candidates[0].Item1;
            SkirmishPresetConfig selectedPreset = candidates[0].Item2;
            string presetAbsolutePath = Path.GetFullPath(Path.Combine(root, presetPath));
            if (selectedPreset == null || EditorUtility.IsDirty(selectedPreset) ||
                AssetDatabase.GetAssetPath(selectedPreset) != presetPath || selectedPreset.buildingPlacement == null)
                throw new InvalidDataException("Compiler-pinned source preset must resolve to its persistent placement asset");
            string placementPath = AssetDatabase.GetAssetPath(selectedPreset.buildingPlacement);
            if (string.IsNullOrEmpty(placementPath))
                throw new InvalidDataException("Compiler-selected source preset placement asset path missing");
            RequireCompilerSelectedInputPin(inputPins, presetAbsolutePath);
            RequireCompilerSelectedInputPin(inputPins, presetAbsolutePath + ".meta");
            RequireCompilerSelectedInputPin(inputPins, Path.GetFullPath(Path.Combine(root, placementPath)));
            RequireCompilerSelectedInputPin(inputPins, Path.GetFullPath(Path.Combine(root, placementPath)) + ".meta");
            RequireCompilerSelectedInputPin(inputPins, Path.Combine(root, "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs"));
            RequireCompilerSelectedInputPin(inputPins, Path.Combine(root, "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs.meta"));
            string sceneConfigPath = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            if (string.IsNullOrEmpty(sceneConfigPath)) throw new InvalidDataException("Current Match placement config identity missing");
            string sceneConfigAbsolutePath = Path.GetFullPath(Path.Combine(root, sceneConfigPath));
            RequireCompilerSelectedInputPin(inputPins, sceneConfigAbsolutePath);
            RequireCompilerSelectedInputPin(inputPins, sceneConfigAbsolutePath + ".meta");
            string matchScenePath = Path.Combine(root, "Assets/Game/Scenes/Match.unity");
            RequireCompilerSelectedInputPin(inputPins, matchScenePath);
            RequireCompilerSelectedInputPin(inputPins, matchScenePath + ".meta");
            return presetPath;
        }

        private static void RequireCompilerSelectedInputPin(JObject inputPins, string path)
        {
            string fullPath = Path.GetFullPath(path);
            var pin = inputPins[fullPath] as JObject;
            long maximumBytes = (long?)pin?["maximum_bytes"] ?? 0;
            var info = File.Exists(fullPath) ? new FileInfo(fullPath) : null;
            if (pin == null || pin.Count != 2 || (string)pin["sha256"] == null || maximumBytes < 1 ||
                info == null || info.Length > maximumBytes || !IsSha256((string)pin["sha256"]) ||
                SkirmishOfflineDataExporter.HashFile(fullPath) != (string)pin["sha256"])
                throw new InvalidDataException("Selected compiler result must pin current preset/placement source: " + fullPath);
        }

        private static void RunBuildingReferenceDataFor(SkirmishResolvedSetup setup, string output,
            string[] requestedKeys, bool captureGeometry, int sourcePresetIndex, string compilerSelectedPresetPath,
            JObject physicalInputRequestDescriptor,
            JObject compilerResultDescriptor, JObject compilerSelectionDescriptor,
            JObject compilerAssetReadback, IDictionary<string, string> compilerPins)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            string catalogId = setup.CatalogId;
            int seed = setup.Seed;
            var preset = compilerSelectedPresetPath == null
                ? SkirmishPresetConfig.Load(sourcePresetIndex)
                : AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(compilerSelectedPresetPath);
            if (preset == null || preset.buildingPlacement == null) throw new InvalidOperationException("Actual source placement preset missing");
            string buildingCatalog = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            if (string.IsNullOrEmpty(buildingCatalog)) throw new InvalidOperationException("Source placement preset path missing");
            string sceneCatalog = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(sceneCatalog);
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null) throw new InvalidOperationException("Actual Match scene catalog missing");
            string unitCatalog = AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig);
            if (string.IsNullOrEmpty(unitCatalog)) throw new InvalidOperationException("Actual source unit registry missing");
            string geometrySource = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceGeometry.cs";
            SortedDictionary<string, string> pins;
            if (compilerResultDescriptor == null)
            {
                var expansion = SkirmishExpansionCatalogConfig.Load();
                var definitionSource = expansion?.Definitions.FirstOrDefault(d => d != null && d.CatalogId == catalogId);
                if (definitionSource == null) throw new InvalidOperationException("Actual legacy scenario definition missing");
                pins = SourcePins(buildingCatalog, sceneCatalog, unitCatalog, AssetDatabase.GetAssetPath(expansion),
                    AssetDatabase.GetAssetPath(definitionSource), geometrySource);
            }
            else
            {
                if (compilerPins == null) throw new InvalidOperationException("Validated compiler-selected source pins required");
                // Compiler-selected authored missions may not exist in the packaged
                // expansion catalog. Their exact authoring graph/result pins remain
                // authoritative and are merged below with the native factory inputs.
                pins = SourcePins(buildingCatalog, sceneCatalog, unitCatalog, geometrySource);
            }
            // The actual native composition merges baseline+scene source prefabs and
            // compiles mission recipes. Export that overlay, not either input alone.
            using var production = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            if (compilerPins != null)
                foreach (var pin in compilerPins)
                {
                    if (pins.TryGetValue(pin.Key, out string prior) && prior != pin.Value)
                        throw new InvalidDataException("Compiler-selected source pin conflicts with building metadata owner pin: " + pin.Key);
                    pins[pin.Key] = pin.Value;
                }
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var buildings = new List<GameObject>(production.Config.Spawnables);
            var units = new List<GameObject>(production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
            string[] keys = requestedKeys;
            if (keys == null)
            {
                if (buildings.Count < 1 || buildings.Count > 32 || buildings.Any(p => p == null))
                    throw new InvalidOperationException("Selected setup must provide a bounded complete configured building set");
                keys = buildings.Select(p => p.name).ToArray();
                if (keys.Any(string.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                    throw new InvalidOperationException("Selected configured building keys must be nonempty and unique");
            }
            if (keys.Length < 1 || keys.Length > 32 || keys.Any(k => string.IsNullOrEmpty(k) || k.Length > 128) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("Unique bounded configured building keys required");
            Type helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            MethodInfo configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers");
            var parameters = configure.GetParameters();
            object buildingResolver = Delegate.CreateDelegate(parameters[0].ParameterType,
                resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            object unitResolver = Delegate.CreateDelegate(parameters[1].ParameterType,
                resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            configure.Invoke(helper, new[] { buildingResolver, unitResolver });
            helperType.GetMethod("RebuildSpawnablesLookup").Invoke(helper, new object[] { buildings, units });
            helperType.GetMethod("ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            var rows = new JArray(); var missing = new JArray();
            using (var world = new World("SourceBuildingDefinitionMetadata", WorldFlags.Game))
            {
                var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                var create = helperType.GetMethod("CreateRuntimeBuildingDefinition");
                foreach (string key in keys)
                {
                    var resolveArgs = new object[] { key, null };
                    if (!(bool)helperType.GetMethod("TryResolveConfiguredSpawnablePrefab").Invoke(helper, resolveArgs))
                        throw new InvalidOperationException("Original configured building resolver rejected: " + key);
                    GameObject prefab = (GameObject)resolveArgs[1];
                    var sourceAuthoring = prefab.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.BuildingDefinitionAuthoring");
                    if (sourceAuthoring == null) throw new InvalidOperationException("Actual building authoring metadata absent: " + key);
                    object definition = create.Invoke(helper, new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, runway });
                    if (definition == null) throw new InvalidOperationException("Original factory returned no definition");
                    var fields = new JObject();
                    foreach (var field in definition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        fields[field.Name] = CaptureDefinitionValue(field.GetValue(definition), key + "/" + field.Name, missing, 0);
                    var row = new JObject { ["key"] = key, ["prefab"] = SourceAsset(prefab), ["definition_type"] = definition.GetType().FullName,
                        ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition", ["fields"] = fields };
                    if (captureGeometry) row["evaluated_geometry"] = CaptureBuildingReferenceGeometry(prefab, sourceAuthoring);
                    rows.Add(row);
                }
            }
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed during metadata export: " + pin.Key);
            var report = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishBuildingDefinitions",
                ["authority"] = "OriginalConfiguredPrefabMetadataFactory_NotMissionState", ["definitions"] = rows,
                ["building_catalog"] = buildingCatalog, ["scene_catalog"] = sceneCatalog, ["unit_catalog"] = unitCatalog,
                ["setup_hash"] = setup.SetupHash, ["seed"] = setup.Seed, ["catalog_id"] = catalogId,
                ["requested_keys"] = JArray.FromObject(keys),
                ["source_pins"] = JObject.FromObject(pins), ["unsupported_values"] = missing,
                ["full_mission_initialized"] = false, ["native_visual_readiness"] = false };
            if (compilerResultDescriptor != null)
            {
                if (physicalInputRequestDescriptor == null || compilerSelectionDescriptor == null || compilerAssetReadback == null || compilerPins == null)
                    throw new InvalidOperationException("Complete compiler-selected building evidence required");
                report["compiler_selection"] = new JObject
                {
                    ["result"] = compilerResultDescriptor.DeepClone(),
                    ["physical_input_request"] = physicalInputRequestDescriptor.DeepClone(),
                    ["selection_descriptor"] = compilerSelectionDescriptor.DeepClone(),
                    ["catalog_id"] = catalogId, ["seed"] = seed,
                    ["size"] = setup.SizeId.ToString(), ["difficulty"] = setup.DifficultyId.ToString(),
                    ["source_preset_asset"] = compilerSelectedPresetPath,
                    ["setup_hash"] = setup.SetupHash, ["operation_map_id"] = setup.OperationMapId,
                    ["source_content_hash"] = setup.ContentHash, ["layout_fingerprint"] = setup.LayoutBindingFingerprint,
                    ["status"] = "ConfigurationCompiled", ["runtime_admission"] = false,
                    ["source_fit_certified"] = false, ["mission_ready"] = false,
                    ["native_acceptance"] = false, ["world_allocated"] = false
                };
                report["compiler_selection_asset_readback"] = compilerAssetReadback.DeepClone();
            }
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "building-definitions.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=" + rows.Count + " unsupported=" + missing.Count + " fullMission=0");
        }

        static Type LoadedType(string name)
        {
            var matches = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).Where(t => t != null).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Exact loaded source type required: " + name);
            return matches[0];
        }
        static JObject SourceAsset(UnityEngine.Object value)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id))
                throw new InvalidOperationException("Source asset identity unresolved: " + value.name);
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Asset source path missing");
            return new JObject { ["codec"] = "source-asset-reference-v1", ["type"] = value.GetType().FullName,
                ["key"] = value.name, ["path"] = path, ["guid"] = guid, ["local_id"] = id };
        }
        static JToken CaptureDefinitionValue(object value, string path, JArray missing, int depth)
        {
            if (depth > 8) throw new InvalidOperationException("Definition nesting bound exceeded");
            if (value == null) return new JObject { ["codec"] = "null-v1" };
            // Unity's unassigned optional asset handles are CLR-nonnull but compare
            // equal to Unity null. Preserve the same null branch as native owners.
            if (value is UnityEngine.Object asset)
                return asset == null ? new JObject { ["codec"] = "null-v1" } : SourceAsset(asset);
            Type type = value.GetType();
            if (type.IsValueType || value is string) return S002InitialWorldEntityExport.CaptureValue(value, path, missing);
            if (value is IEnumerable sequence)
            {
                var items = new JArray();
                foreach (object item in sequence)
                {
                    if (items.Count >= 4096) throw new InvalidOperationException("Definition list bound exceeded");
                    items.Add(CaptureDefinitionValue(item, path + "/" + items.Count, missing, depth + 1));
                }
                return new JObject { ["codec"] = "definition-list-v1", ["items"] = items };
            }
            var fields = new JObject();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), path + "/" + field.Name, missing, depth + 1);
            return new JObject { ["codec"] = "definition-fields-v1", ["type"] = type.FullName, ["fields"] = fields };
        }
    }
}
#endif
