#if UNITY_EDITOR
using System;
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
        // Calls the original S005 production overlay and definition factory; no mission state is created.
        public static void RunS005FullFactoryReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return args[i + 1]; }
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS005FullFactoryData"));
            string evidencePath = Path.GetFullPath(Arg("-skirmishS005FullFactorySetupEvidence"));
            string evidenceHash = HashFile(evidencePath); var evidence = JObject.Parse(File.ReadAllText(evidencePath));
            if (HashFile(evidencePath) != evidenceHash) throw new InvalidDataException("Setup evidence changed");
            if (evidenceHash != "6404929a1c8a235b65e3a2faa3b0756318dd90342b9d06baf289543c12670eae" || (string)evidence["status"] != "Passed" || (int?)evidence["wrapper_exit"] != 0 || (bool?)evidence["mission_ready"] != false) throw new InvalidDataException("Exact S005 native compiler evidence required");
            var expansion = SkirmishExpansionCatalogConfig.Load();
            if (expansion == null || !expansion.TryGetDefinition("S005", out var scenario) || scenario == null ||
                !SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException("S005 authored inputs unavailable");
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            string scenePath = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(scenePath);
            if (preset == null || preset.buildingPlacement == null || scene == null || scene.UnitPrefabRegistryConfig == null)
                throw new InvalidOperationException("Original factory catalogs unavailable");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = scenario.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(scenario, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1, manifest, matrix, out var setup, out var reasons))
                throw new InvalidOperationException("Original S005 compiler rejected inputs: " + string.Join(";", reasons.Select(x => x.ToString())));
            if (setup.CatalogId != "S005" || setup.Seed != 1 || setup.SetupHash != 1865447784u || setup.Structures == null || setup.Structures.Length != 15) throw new InvalidDataException("Exact native S005 Standard setup required");
            string[] keys = setup.Structures.Select(row => SkirmishStructureIds.VisualKey(row.StructureId)).Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
            if (keys.Length != 7 || keys.Any(string.IsNullOrEmpty)) throw new InvalidDataException("Exact seven setup building keys required");
            var pins = SourcePins("Assets/Game/Scenes/Match.unity", AssetDatabase.GetAssetPath(preset.buildingPlacement), scenePath,
                AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig), AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(expansion),
                AssetDatabase.GetAssetPath(scenario), "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv");
            pins[evidencePath] = evidenceHash;
            VerifyPins(pins);
            var originalWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld; int worldCount = Unity.Entities.World.All.Count;
            var timingFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper").GetMethod("TryGetMetadata");
            var productionCreate = typeof(SkirmishProductionCatalog).GetMethods().Single(method => method.Name == "Create" && method.GetParameters().Length == 4);
            var timingResolver = Delegate.CreateDelegate(productionCreate.GetParameters()[3].ParameterType, timingFactory);
            using var production = (SkirmishProductionCatalog)productionCreate.Invoke(null, new object[] { setup, preset.buildingPlacement, scene, timingResolver });
            var helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            var resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true); MethodInfo configure = RequireMethod(helperType, "ConfigureAuthoringMetadataResolvers");
            var parameters = configure.GetParameters();
            configure.Invoke(helper, new[] { Delegate.CreateDelegate(parameters[0].ParameterType, resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public)), Delegate.CreateDelegate(parameters[1].ParameterType, resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public)) });
            RequireMethod(helperType, "RebuildSpawnablesLookup").Invoke(helper, new object[] { production.Config.Spawnables.ToList(), production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToList() });
            RequireMethod(helperType, "ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            MethodInfo resolve = RequireMethod(helperType, "TryResolveConfiguredSpawnablePrefab"), create = RequireMethod(helperType, "CreateRuntimeBuildingDefinition");
            var missing = new JArray(); var rows = new JArray();
            using (var utilityWorld = new Unity.Entities.World("S005FactoryRunwayReferenceOnly", Unity.Entities.WorldFlags.Game))
            {
            object runway = utilityWorld.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
            foreach (string key in keys)
            {
                object[] resolveArgs = { key, null };
                if (!(bool)resolve.Invoke(helper, resolveArgs) || !(resolveArgs[1] is GameObject prefab) || prefab.name != key) throw new InvalidDataException("Configured building key mismatch: " + key);
                object definition = create.Invoke(helper, new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, runway });
                if (definition == null) throw new InvalidDataException("Original factory returned no definition: " + key);
                var fields = new JObject();
                foreach (var field in definition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    fields[field.Name] = CaptureDefinitionValue(field.GetValue(definition), key + "/" + field.Name, missing, 0);
                rows.Add(new JObject { ["prefab"] = SourceAsset(prefab), ["fields"] = fields });
            }
            }
            if (missing.Count != 0 || !ReferenceEquals(originalWorld, Unity.Entities.World.DefaultGameObjectInjectionWorld) || Unity.Entities.World.All.Count != worldCount)
                throw new InvalidOperationException("Unsupported source values or changed World ownership");
            VerifyPins(pins);
            var result = new JObject { ["kind"] = "S005OriginalFullFactoryRunwayDefinitions", ["authority"] = "OriginalS005ProductionOverlayAndDefinitionFactory", ["setup_hash"] = setup.SetupHash,
                ["seed"] = setup.Seed, ["buildings"] = rows, ["source_pins"] = JObject.FromObject(pins), ["unsupported_values"] = missing,
                ["runway_reader_supplied"] = true, ["mission_world_created"] = false, ["mission_ready"] = false };
            Directory.CreateDirectory(output); string file = Path.Combine(output, "full-factory-runway-definitions.json");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[S005FullFactoryReference] result=Collected buildings=" + rows.Count + " unsupported=0 mission=0 readiness=0");
        }
    }
}
#endif
