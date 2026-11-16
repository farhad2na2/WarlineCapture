#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        private const string S004ReadModelOutputArg = "-skirmishS004ConfiguredSpawnableReadModel";
        private const string S004ReadModelEvidenceArg = "-skirmishS004ConfiguredSpawnableSetupEvidence";

        // Calls the original publisher against the original S004 production
        // overlay. The utility World owns an isolated EntityManager, the original
        // runway reader required during definition rebuild, and a boundary buffer.
        // The owner may make temporary hidden bounds probes, which its cleanup callback destroys.
        public static void RunS004ConfiguredSpawnableReadModelReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int at = Array.IndexOf(args, name);
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Required argument " + name);
                return args[at + 1];
            }

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg(S004ReadModelOutputArg));
            string resultPath = Path.Combine(output, "configured-spawnable-readmodel.json");
            if (File.Exists(resultPath)) throw new InvalidOperationException("Fresh S004 configured-readmodel output required");
            string evidencePath = Path.GetFullPath(Arg(S004ReadModelEvidenceArg));
            if (!File.Exists(evidencePath)) throw new FileNotFoundException("Original compiled S004 evidence missing", evidencePath);
            string evidenceHash = SkirmishOfflineDataExporter.HashFile(evidencePath);
            JObject evidence = JObject.Parse(File.ReadAllText(evidencePath));
            if (SkirmishOfflineDataExporter.HashFile(evidencePath) != evidenceHash)
                throw new InvalidOperationException("Compiled S004 evidence changed while being read");
            ValidateS004SetupEvidenceEnvelope(evidence);

            var expansion = SkirmishExpansionCatalogConfig.Load();
            if (expansion == null || !expansion.TryGetDefinition("S004", out var scenario) || scenario == null)
                throw new InvalidOperationException("Exactly one packaged S004 scenario definition required");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException(matrixError);
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null)
                throw new InvalidOperationException("Actual DesertBase building-placement catalog missing");
            string buildingCatalog = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            const string matchGuid = "b2010000000000000000000000000004";
            string sceneCatalog = AssetDatabase.GUIDToAssetPath(matchGuid);
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(sceneCatalog);
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null ||
                !StringComparer.Ordinal.Equals(AssetDatabase.AssetPathToGUID(sceneCatalog), matchGuid))
                throw new InvalidOperationException("Exact serialized Match building catalog/registry missing");
            string unitCatalog = AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig);
            string matrixPath = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv";
            var pins = SourcePins("Assets/Game/Scenes/Match.unity", buildingCatalog, sceneCatalog, unitCatalog,
                AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(expansion),
                AssetDatabase.GetAssetPath(scenario), matrixPath,
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryCommands.cs",
                "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs",
                "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.BuildingData.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004BuildingDefinitions.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004ConfiguredSpawnableReadModel.cs");
            pins[evidencePath] = evidenceHash;
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = scenario.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(scenario, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                S004BuildingDefinitionSeed, manifest, matrix, out var setup, out var compileReasons))
                throw new InvalidOperationException("Original S004 compiler rejected packaged inputs: " +
                    string.Join(";", compileReasons.Select(item => item.ToString())));
            ValidateS004CompiledSetupAgainstEvidence(setup, evidence);
            var structureKeys = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in setup.Structures ?? Array.Empty<SkirmishResolvedStructureEntry>())
            {
                string key = SkirmishStructureIds.VisualKey(row.StructureId);
                if (string.IsNullOrEmpty(row.StructureId) || string.IsNullOrEmpty(key))
                    throw new InvalidDataException("Original S004 structure-to-visual-key mapping unavailable");
                structureKeys[row.StructureId] = key;
            }
            string[] keys = structureKeys.Values.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (keys.Length != 9) throw new InvalidDataException("S004 compiled setup must yield exactly nine distinct original visual keys");

            using var production = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            if (production.Config == null || production.Config.Spawnables == null ||
                production.Config.UnitPrefabRegistryConfig == null)
                throw new InvalidOperationException("Original S004 production overlay is incomplete");
            var originalDefaultWorld = World.DefaultGameObjectInjectionWorld;
            Type definitionHelperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type metadataResolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object definitionHelper = Activator.CreateInstance(definitionHelperType, true);
            MethodInfo configure = RequireMethod(definitionHelperType, "ConfigureAuthoringMetadataResolvers");
            ParameterInfo[] configureParameters = configure.GetParameters();
            if (configureParameters.Length != 2) throw new InvalidOperationException("Original metadata resolver signature changed");
            object buildingResolver = Delegate.CreateDelegate(configureParameters[0].ParameterType,
                metadataResolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            object unitResolver = Delegate.CreateDelegate(configureParameters[1].ParameterType,
                metadataResolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            configure.Invoke(definitionHelper, new[] { buildingResolver, unitResolver });
            var buildings = new List<GameObject>(production.Config.Spawnables);
            var units = new List<GameObject>(production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
            RequireMethod(definitionHelperType, "RebuildSpawnablesLookup").Invoke(definitionHelper, new object[] { buildings, units });
            RequireMethod(definitionHelperType, "ConfigureProductionRecipes").Invoke(definitionHelper,
                new object[] { production.Config.RuntimeProductionRecipes });

            Type compositionType = LoadedType("Game.Runtime.BuildingRuntimeProcessingCompositionSystemHelper");
            object compositionHelper = Activator.CreateInstance(compositionType, true);
            MethodInfo publish = RequireMethod(compositionType, "PublishConfiguredSpawnablesReadModel");
            if (publish.GetParameters().Length != 3) throw new InvalidOperationException("Original readmodel publisher signature changed");
            MethodInfo rebuildDefinitions = RequireMethod(definitionHelperType, "RebuildConfiguredSpawnableDefinitions");
            if (rebuildDefinitions.GetParameters().Length != 2) throw new InvalidOperationException("Original configured-definition rebuild signature changed");
            object destroyCallback = Delegate.CreateDelegate(rebuildDefinitions.GetParameters()[1].ParameterType,
                typeof(SkirmishBakedActorSnapshotExporter).GetMethod(nameof(DestroyTwoSeedObject), BindingFlags.Static | BindingFlags.NonPublic));
            var missingFields = new JArray();
            var rows = new JArray();
            using (var world = new World("S004ConfiguredSpawnableReadModelCapture", WorldFlags.Game))
            {
                var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                try
                {
                rebuildDefinitions.Invoke(definitionHelper, new[] { runway, destroyCallback });
                var byKey = new Dictionary<string, JObject>(StringComparer.Ordinal);
                var entryByKey = new Dictionary<string, object>(StringComparer.Ordinal);
                var definitionByKey = new Dictionary<string, object>(StringComparer.Ordinal);
                MethodInfo configuredCount = RequireMethod(definitionHelperType, "get_ConfiguredSpawnableCount");
                MethodInfo getDefinition = definitionHelperType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(method => method.Name == "TryGetConfiguredDefinition" && method.GetParameters().Length == 2 &&
                        method.GetParameters()[0].ParameterType == typeof(int));
                MethodInfo getEntry = definitionHelperType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Single(method => method.Name == "TryGetConfiguredSpawnable" && method.GetParameters().Length == 2 &&
                        method.GetParameters()[0].ParameterType == typeof(int));
                for (int index = 0; index < (int)configuredCount.Invoke(definitionHelper, null); index++)
                {
                    object[] defArgs = { index, null };
                    object[] entryArgs = { index, null };
                    if (!(bool)getDefinition.Invoke(definitionHelper, defArgs) || !(bool)getEntry.Invoke(definitionHelper, entryArgs)) continue;
                    object definition = defArgs[1];
                    object entry = entryArgs[1];
                    GameObject prefab = (GameObject)entry.GetType().GetField("Prefab").GetValue(entry);
                    if (prefab == null) continue;
                    string key = prefab.name;
                    if (!keys.Contains(key, StringComparer.Ordinal)) continue;
                    if (byKey.ContainsKey(key)) throw new InvalidDataException("Configured production overlay contains duplicate S004 visual key: " + key);
                    byKey.Add(key, SourceAsset(prefab));
                    entryByKey.Add(key, entry);
                    definitionByKey.Add(key, definition);
                }
                if (!keys.All(byKey.ContainsKey)) throw new InvalidDataException("Original S004 production overlay is missing a compiled visual key");
                var prefabPins = SourcePins(keys.Select(key => (string)byKey[key]["path"]).ToArray());
                foreach (var pin in prefabPins) pins[pin.Key] = pin.Value;
                pins[evidencePath] = evidenceHash;

                EntityManager em = world.EntityManager;
                Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
                publish.Invoke(compositionHelper, new object[] { definitionHelper, em, boundary });
                DynamicBuffer<BuildingConfiguredSpawnableReadModel> buffer = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary, true);
                MethodInfo normalizeKey = definitionHelperType.GetMethod("NormalizeSpawnableKey", BindingFlags.Static | BindingFlags.Public)
                    ?? throw new InvalidOperationException("Original boundary key normalization unavailable");
                var keyByBoundaryId = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    string boundaryId = (string)normalizeKey.Invoke(null, new object[] { key });
                    if (string.IsNullOrEmpty(boundaryId) || keyByBoundaryId.ContainsKey(boundaryId))
                        throw new InvalidDataException("S004 visual keys have empty or duplicate original boundary IDs");
                    keyByBoundaryId.Add(boundaryId, key);
                }
                var models = new Dictionary<string, BuildingConfiguredSpawnableReadModel>(StringComparer.Ordinal);
                for (int i = 0; i < buffer.Length; i++)
                {
                    BuildingConfiguredSpawnableReadModel model = buffer[i];
                    string id = model.BuildingId.ToString();
                    if (!keyByBoundaryId.TryGetValue(id, out string visualKey)) continue;
                    if (models.ContainsKey(visualKey)) throw new InvalidDataException("Original publisher emitted duplicate S004 BuildingId: " + id);
                    models.Add(visualKey, model);
                }
                foreach (string key in keys)
                {
                    if (!models.TryGetValue(key, out var model)) throw new InvalidDataException("Original publisher omitted S004 key: " + key);
                    object definition = definitionByKey[key];
                    object entry = entryByKey[key];
                    JObject definitionInputs = new JObject();
                    foreach (string fieldName in new[] { "Prefab", "DisplayName", "CreditsCost", "MaterialsCost", "FootprintCells" })
                    {
                        FieldInfo field = definition.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field == null) throw new InvalidDataException("Original runtime definition input field missing: " + fieldName);
                        definitionInputs[fieldName] = CaptureDefinitionValue(field.GetValue(definition), key + "/" + field.Name, missingFields, 0);
                    }
                    var authoring = ((GameObject)entry.GetType().GetField("Prefab").GetValue(entry)).GetComponents<Component>()
                        .FirstOrDefault(component => component != null && component.GetType().FullName == "Game.Authoring.BuildingDefinitionAuthoring");
                    if (authoring == null) throw new InvalidDataException("Actual S004 BuildingDefinitionAuthoring missing: " + key);
                    var entryFact = new JObject
                    {
                        ["display_name"] = (string)entry.GetType().GetField("DisplayName").GetValue(entry),
                        ["can_request"] = (bool)entry.GetType().GetField("CanRequest").GetValue(entry),
                        ["prefab"] = byKey[key],
                        ["authoring_component"] = SourceAsset(authoring),
                        ["factory"] = "BuildingDefinitionPrefabSystemHelper.RebuildConfiguredSpawnableDefinitions",
                        ["definition_type"] = definition.GetType().FullName,
                        ["definition_projection_inputs"] = definitionInputs,
                        ["projection_rules"] = new JObject
                        {
                            ["BuildingId"] = "Original ResolveBoundaryId(prefab, entry.DisplayName): normalized prefab name with entry display-name fallback",
                            ["DisplayName"] = "ConfiguredSpawnableEntry.DisplayName",
                            ["Price"] = "max(0, BuildingDefinition.CreditsCost)",
                            ["MaterialsCost"] = "max(0, BuildingDefinition.MaterialsCost)",
                            ["FootprintCells"] = "componentwise max(1, BuildingDefinition.FootprintCells)",
                            ["CanRequest"] = "ConfiguredSpawnableEntry.CanRequest"
                        }
                    };
                    var readModel = new JObject
                    {
                        ["BuildingId"] = model.BuildingId.ToString(),
                        ["DisplayName"] = model.DisplayName.ToString(),
                        ["Price"] = model.Price,
                        ["MaterialsCost"] = model.MaterialsCost,
                        ["FootprintCells"] = new JObject { ["x"] = model.FootprintCells.x, ["y"] = model.FootprintCells.y },
                        ["CanRequest"] = model.CanRequest
                    };
                    rows.Add(new JObject { ["visual_key"] = key,
                        ["structure_ids"] = JArray.FromObject(structureKeys.Where(pair => pair.Value == key).Select(pair => pair.Key).OrderBy(value => value, StringComparer.Ordinal)),
                        ["source_definition"] = entryFact, ["read_model"] = readModel });
                }
                }
                finally
                {
                    RequireMethod(definitionHelperType, "ClearConfiguredSpawnableDefinitions")
                        .Invoke(definitionHelper, new object[] { destroyCallback });
                }
            }
            if (!ReferenceEquals(originalDefaultWorld, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during isolated readmodel capture");
            if (missingFields.Count != 0) throw new InvalidDataException("Original BuildingDefinition serializer returned unsupported values");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S004 readmodel source changed during capture: " + pin.Key);
            var result = new JObject
            {
                ["schema_version"] = 1,
                ["kind"] = "S004OriginalConfiguredSpawnableReadModel",
                ["authority"] = "OriginalS004ProductionOverlayAndPublishConfiguredSpawnablesReadModel",
                ["setup_evidence"] = new JObject { ["path"] = evidencePath, ["sha256"] = evidenceHash, ["setup_hash"] = setup.SetupHash },
                ["catalog_id"] = setup.CatalogId, ["seed"] = setup.Seed, ["setup_hash"] = setup.SetupHash,
                ["building_catalog"] = buildingCatalog, ["scene_catalog"] = sceneCatalog, ["unit_catalog"] = unitCatalog,
                ["publisher"] = "BuildingRuntimeProcessingCompositionSystemHelper.PublishConfiguredSpawnablesReadModel",
                ["publisher_field_order"] = JArray.FromObject(new[] { "BuildingId", "DisplayName", "Price", "MaterialsCost", "FootprintCells", "CanRequest" }),
                ["entries"] = rows, ["source_pins"] = JObject.FromObject(pins),
                ["capture_world_scope"] = "IsolatedDisposableUtilityWorldWithOriginalBuildingRunwaySystemAndBoundaryBuffer;NoSceneStreamOrSimulationTick;WorldDisposedBeforeArtifactWrite",
                ["scene_loaded"] = false, ["mission_building_spawned"] = false,
                ["temporary_bounds_probes"] = "Original CacheBuildingBounds may instantiate hidden prefab probes; callback immediately destroys them",
                ["mission_initialized"] = false, ["native_visual_readiness"] = false, ["mission_readiness"] = false
            };
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                stream.Write(bytes, 0, bytes.Length);
            }
            Debug.Log("[SkirmishS004ConfiguredSpawnableReadModel] result=Passed keys=" + rows.Count + " scene=0 missionBuildings=0 readiness=0");
        }
    }
}
#endif
