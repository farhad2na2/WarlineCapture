#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        private const string S004BuildingDefinitionOutputArgument = "-skirmishS004BuildingDefinitionReferenceData";
        private const string S004BuildingDefinitionSetupEvidenceArgument = "-skirmishS004BuildingDefinitionSetupEvidence";
        private const int S004BuildingDefinitionSeed = 104733;

        // Resolves the exact S004 compiled structure keys through the original
        // configured-prefab resolver and runtime-definition factory. No scene is
        // loaded and no mission/gameplay world is created or initialized.
        public static void RunS004BuildingDefinitionReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int at = Array.IndexOf(args, name);
                if (at < 0 || at + 1 >= args.Length)
                    throw new ArgumentException("Required argument " + name);
                return args[at + 1];
            }

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg(S004BuildingDefinitionOutputArgument));
            string resultPath = Path.Combine(output, "building-definitions.json");
            if (File.Exists(resultPath)) throw new InvalidOperationException("Fresh S004 building-definition output required");

            string evidencePath = Path.GetFullPath(Arg(S004BuildingDefinitionSetupEvidenceArgument));
            if (!File.Exists(evidencePath)) throw new FileNotFoundException("Original S004 compiled-setup evidence missing", evidencePath);
            string evidenceSha256 = SkirmishOfflineDataExporter.HashFile(evidencePath);
            JObject evidence = JObject.Parse(File.ReadAllText(evidencePath));
            if (SkirmishOfflineDataExporter.HashFile(evidencePath) != evidenceSha256)
                throw new InvalidOperationException("Compiled S004 setup evidence changed while being read");
            ValidateS004SetupEvidenceEnvelope(evidence);

            var expansion = SkirmishExpansionCatalogConfig.Load();
            if (expansion == null || !expansion.TryGetDefinition("S004", out var definitionSource) || definitionSource == null)
                throw new InvalidOperationException("Exactly one packaged S004 scenario definition required");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException(matrixError);

            // Match the actual production composition inputs: DesertBase is the
            // shared baseline and the serialized Match scene owns the second catalog.
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null)
                throw new InvalidOperationException("Actual DesertBase building-placement catalog missing");
            string buildingCatalog = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            if (string.IsNullOrEmpty(buildingCatalog)) throw new InvalidOperationException("DesertBase catalog source path missing");

            const string sceneConfigGuid = "b2010000000000000000000000000004";
            string sceneCatalog = AssetDatabase.GUIDToAssetPath(sceneConfigGuid);
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(sceneCatalog);
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null ||
                !StringComparer.Ordinal.Equals(AssetDatabase.AssetPathToGUID(sceneCatalog), sceneConfigGuid))
                throw new InvalidOperationException("Exact serialized Match building catalog/registry missing");
            string unitCatalog = AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig);
            if (string.IsNullOrEmpty(unitCatalog)) throw new InvalidOperationException("Match unit-registry source path missing");

            // Pin every compiler, catalog, factory and evidence input before they
            // are consumed. The final pass below rejects any mid-capture drift.
            string matrixResourcePath = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv";
            var pins = SourcePins("Assets/Game/Scenes/Match.unity", buildingCatalog, sceneCatalog,
                unitCatalog, AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(expansion),
                AssetDatabase.GetAssetPath(definitionSource), matrixResourcePath,
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.BuildingData.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004BuildingDefinitions.cs");
            pins[evidencePath] = evidenceSha256;

            var manifest = new SkirmishContentManifest
            {
                RequiredFeatureIds = definitionSource.RequiredFeatureIds ?? Array.Empty<string>()
            };
            if (!SkirmishSetupCompiler.TryCompile(definitionSource, SkirmishDifficultyId.Regular,
                SkirmishSizeId.Standard, S004BuildingDefinitionSeed, manifest, matrix,
                out var setup, out var compileReasons))
                throw new InvalidOperationException("Original S004 compiler rejected packaged inputs: " +
                    string.Join(";", compileReasons.Select(item => item.ToString())));

            ValidateS004CompiledSetupAgainstEvidence(setup, evidence);
            var structureIds = (setup.Structures ?? Array.Empty<SkirmishResolvedStructureEntry>())
                .Select(item => item.StructureId)
                .ToArray();
            if (structureIds.Length == 0 || structureIds.Any(string.IsNullOrEmpty))
                throw new InvalidDataException("Compiled S004 setup has an empty starting-structure identity");

            var structureKeys = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (string structureId in structureIds)
            {
                string key = SkirmishStructureIds.VisualKey(structureId);
                if (string.IsNullOrEmpty(key))
                    throw new InvalidDataException("Original VisualKey mapping rejected compiled S004 structure: " + structureId);
                if (structureKeys.TryGetValue(structureId, out string prior) && !StringComparer.Ordinal.Equals(prior, key))
                    throw new InvalidDataException("Compiled S004 structure maps inconsistently: " + structureId);
                structureKeys[structureId] = key;
            }
            string[] keys = structureKeys.Values.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (keys.Length == 0 || keys.Length > structureIds.Length ||
                keys.Any(string.IsNullOrEmpty) || keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length)
                throw new InvalidDataException("Compiled S004 structure set did not produce unique exact configured prefab keys");

            using var production = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            if (production.Config == null || production.Config.Spawnables == null)
                throw new InvalidOperationException("Original S004 production catalog overlay missing");

            var originalDefaultWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            var resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            MethodInfo configure = RequireMethod(helperType, "ConfigureAuthoringMetadataResolvers");
            ParameterInfo[] resolverParameters = configure.GetParameters();
            if (resolverParameters.Length != 2) throw new InvalidOperationException("Original metadata resolver signature changed");
            object buildingResolver = Delegate.CreateDelegate(resolverParameters[0].ParameterType,
                resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            object unitResolver = Delegate.CreateDelegate(resolverParameters[1].ParameterType,
                resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            configure.Invoke(helper, new[] { buildingResolver, unitResolver });

            var buildings = new List<GameObject>(production.Config.Spawnables);
            var units = new List<GameObject>(production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
            RequireMethod(helperType, "RebuildSpawnablesLookup").Invoke(helper, new object[] { buildings, units });
            RequireMethod(helperType, "ConfigureProductionRecipes").Invoke(helper,
                new object[] { production.Config.RuntimeProductionRecipes });
            MethodInfo resolve = RequireMethod(helperType, "TryResolveConfiguredSpawnablePrefab");
            MethodInfo create = RequireMethod(helperType, "CreateRuntimeBuildingDefinition");
            FieldInfo footprintField = null;
            Type definitionType = null;
            var definitions = new JArray();

            foreach (string key in keys)
            {
                object[] resolveArgs = { key, null };
                if (!(bool)resolve.Invoke(helper, resolveArgs))
                    throw new InvalidOperationException("Original configured-spawnable resolver rejected S004 key: " + key);
                var prefab = resolveArgs[1] as GameObject;
                if (prefab == null || !StringComparer.Ordinal.Equals(prefab.name, key))
                    throw new InvalidOperationException("Resolved S004 source prefab name/key mismatch: " + key);
                var authoring = prefab.GetComponents<Component>().FirstOrDefault(component => component != null &&
                    StringComparer.Ordinal.Equals(component.GetType().FullName, "Game.Authoring.BuildingDefinitionAuthoring"));
                if (authoring == null)
                    throw new InvalidOperationException("Actual BuildingDefinitionAuthoring missing on configured S004 prefab: " + key);

                // Run the original factory with its optional runway reader unset.
                // That avoids constructing any ECS World; FootprintCells is derived
                // from the prefab's original authored definition and visual bounds.
                object definition = create.Invoke(helper, new object[]
                    { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, null });
                if (definition == null) throw new InvalidOperationException("Original runtime-definition factory returned null: " + key);
                definitionType ??= definition.GetType();
                if (definition.GetType() != definitionType)
                    throw new InvalidOperationException("Original factory returned inconsistent definition type");
                footprintField ??= definitionType.GetField("FootprintCells", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (footprintField == null || footprintField.FieldType != typeof(Vector2Int))
                    throw new InvalidOperationException("Original runtime definition footprint field unavailable");
                Vector2Int footprint = (Vector2Int)footprintField.GetValue(definition);
                if (footprint.x <= 0 || footprint.y <= 0)
                    throw new InvalidDataException("Original runtime definition has invalid footprint for " + key + ": " + footprint);

                JObject assetReference = SourceAsset(prefab);
                JObject authoringReference = SourceAsset(authoring);
                string[] sourceStructureIds = structureKeys.Where(pair => StringComparer.Ordinal.Equals(pair.Value, key))
                    .Select(pair => pair.Key).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                definitions.Add(new JObject
                {
                    ["visual_key"] = key,
                    ["structure_ids"] = JArray.FromObject(sourceStructureIds),
                    ["prefab"] = assetReference,
                    ["authoring_component"] = authoringReference,
                    ["definition_type"] = definitionType.FullName,
                    ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition",
                    ["runtime_footprint_cells"] = new JObject { ["x"] = footprint.x, ["y"] = footprint.y }
                });
            }

            if (!ReferenceEquals(originalDefaultWorld, Unity.Entities.World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during source-only building metadata capture");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("Source changed during S004 building metadata capture: " + pin.Key);

            var structureRows = new JArray();
            foreach (var structure in setup.Structures)
            {
                string key = SkirmishStructureIds.VisualKey(structure.StructureId);
                structureRows.Add(new JObject
                {
                    ["faction_id"] = structure.FactionId,
                    ["structure_id"] = structure.StructureId,
                    ["visual_key"] = key
                });
            }
            var result = new JObject
            {
                ["schema_version"] = 1,
                ["kind"] = "S004OriginalFactoryBuildingDefinitions",
                ["authority"] = "OriginalCompiledSetupAndConfiguredPrefabFactory_NoMissionState",
                ["setup_evidence"] = new JObject
                {
                    ["path"] = evidencePath,
                    ["sha256"] = pins[evidencePath],
                    ["status"] = (string)evidence["status"],
                    ["setup_hash"] = setup.SetupHash
                },
                ["setup_identity"] = new JObject
                {
                    ["catalog_id"] = setup.CatalogId,
                    ["definition_id"] = setup.DefinitionId,
                    ["scenario_setup_id"] = setup.ScenarioSetupId,
                    ["content_version"] = setup.ContentVersion,
                    ["size_id"] = setup.SizeId.ToString(),
                    ["difficulty_id"] = setup.DifficultyId.ToString(),
                    ["seed"] = setup.Seed,
                    ["setup_hash"] = setup.SetupHash
                },
                ["structure_entry_count"] = structureRows.Count,
                ["distinct_structure_id_count"] = structureKeys.Count,
                ["distinct_visual_key_count"] = keys.Length,
                ["compiled_structure_entries"] = structureRows,
                ["building_catalog"] = buildingCatalog,
                ["scene_catalog"] = sceneCatalog,
                ["scene_catalog_guid"] = sceneConfigGuid,
                ["unit_catalog"] = unitCatalog,
                ["factory_definitions"] = definitions,
                ["source_pins"] = JObject.FromObject(pins),
                ["scene_loaded"] = false,
                ["gameplay_world_created"] = false,
                ["runway_system_supplied_to_factory"] = false,
                ["runway_derived_fields_captured"] = false,
                ["factory_scope"] = "Original configured-spawnable resolver and runtime-definition factory; runtime footprint only. Optional runway reader omitted so no ECS/scene world is created. No full factory/native startup parity claim.",
                ["mission_initialized"] = false,
                ["native_visual_readiness"] = false,
                ["mission_readiness"] = false
            };

            Directory.CreateDirectory(output);
            byte[] outputBytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            using (var stream = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(outputBytes, 0, outputBytes.Length);
            Debug.Log("[SkirmishS004OriginalFactoryBuildingDefinitions] result=Passed structureEntries=" + structureRows.Count +
                " distinctStructureIds=" + structureKeys.Count + " distinctVisualKeys=" + keys.Length + " missionReady=0");
        }

        private static void ValidateS004SetupEvidenceEnvelope(JObject evidence)
        {
            if (!StringComparer.Ordinal.Equals((string)evidence["status"], "Passed") ||
                !StringComparer.Ordinal.Equals((string)evidence["kind"], "S004StandardCompiledSetupSourceExport"))
                throw new InvalidDataException("Passed S004 Standard compiled-setup evidence required");
            JObject identity = evidence["identity"] as JObject;
            JObject compiled = evidence["compiled_setup"] as JObject;
            JObject sourceBound = evidence["source_bound_compiler_result"] as JObject;
            if (identity == null || compiled == null || sourceBound == null ||
                !StringComparer.Ordinal.Equals((string)sourceBound["status"], "Compiled") ||
                sourceBound["compiled"]?.Value<bool>() != true)
                throw new InvalidDataException("Compiled S004 source-bound result missing from evidence");
            RequireEvidenceIdentity(identity, "catalog_id", "S004");
            RequireEvidenceIdentity(identity, "definition_id", "skirmish.s004");
            RequireEvidenceIdentity(identity, "scenario_setup_id", "scenario.skirmish.s004");
            RequireEvidenceIdentity(identity, "difficulty_id", "Regular");
            RequireEvidenceIdentity(identity, "size_id", "Standard");
            if (identity["seed"]?.Value<int>() != S004BuildingDefinitionSeed ||
                identity["content_version"]?.Value<int>() != 2 || identity["measured_layout_bound"]?.Value<bool>() != true)
                throw new InvalidDataException("Compiled S004 evidence identity/seed/layout binding mismatch");
            if ((string)sourceBound["catalog_id"] != "S004" ||
                (string)sourceBound["definition_id"] != "skirmish.s004")
                throw new InvalidDataException("Compiled source-bound evidence is for another definition");
        }

        private static void ValidateS004CompiledSetupAgainstEvidence(SkirmishResolvedSetup setup, JObject evidence)
        {
            if (setup == null || setup.Structures == null) throw new InvalidDataException("Original compiler returned no S004 structures");
            JObject identity = (JObject)evidence["identity"];
            JObject compiled = (JObject)evidence["compiled_setup"];
            if (!StringComparer.Ordinal.Equals(setup.CatalogId, "S004") ||
                !StringComparer.Ordinal.Equals(setup.DefinitionId, "skirmish.s004") ||
                !StringComparer.Ordinal.Equals(setup.ScenarioSetupId, "scenario.skirmish.s004") ||
                setup.ContentVersion != 2 || setup.SizeId != SkirmishSizeId.Standard ||
                setup.DifficultyId != SkirmishDifficultyId.Regular || setup.Seed != S004BuildingDefinitionSeed ||
                setup.SetupHash != identity["setup_hash"]?.Value<uint>() ||
                setup.SetupHash != compiled["SetupHash"]?.Value<uint>())
                throw new InvalidDataException("Fresh original S004 compiler output differs from compiled-setup evidence identity/hash");

            JArray evidenceStructures = compiled["Structures"] as JArray;
            if (evidenceStructures == null || evidenceStructures.Count != setup.Structures.Length)
                throw new InvalidDataException("Compiled setup evidence structure entries differ from fresh compiler output");
            var expected = evidenceStructures.Select(item =>
                ((int?)item?["FactionId"] ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
                ((string)item?["StructureId"] ?? string.Empty)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var actual = setup.Structures.Select(item =>
                item.FactionId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
                (item.StructureId ?? string.Empty)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidDataException("Fresh compiler S004 structure identities differ from compiled-setup evidence");
        }

        private static void RequireEvidenceIdentity(JObject identity, string field, string expected)
        {
            if (!StringComparer.Ordinal.Equals((string)identity[field], expected))
                throw new InvalidDataException("Compiled S004 evidence " + field + " mismatch");
        }

        private static MethodInfo RequireMethod(Type type, string name) =>
            type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Original source method unavailable: " + type.FullName + "." + name);
    }
}
#endif
