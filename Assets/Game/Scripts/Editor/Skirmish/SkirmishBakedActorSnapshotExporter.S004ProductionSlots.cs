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
        // Calls the original S004 production overlay and definition factory; no mission state is created.
        public static void RunS004ProductionSlotReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return args[i + 1]; }
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS004ProductionSlotData"));
            string evidencePath = Path.GetFullPath(Arg("-skirmishS004ProductionSlotSetupEvidence"));
            string evidenceHash = HashFile(evidencePath); var evidence = JObject.Parse(File.ReadAllText(evidencePath));
            if (HashFile(evidencePath) != evidenceHash) throw new InvalidDataException("Setup evidence changed");
            ValidateS004SetupEvidenceEnvelope(evidence);
            var expansion = SkirmishExpansionCatalogConfig.Load();
            if (expansion == null || !expansion.TryGetDefinition("S004", out var scenario) || scenario == null ||
                !SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                throw new InvalidOperationException("S004 authored inputs unavailable");
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            string scenePath = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(scenePath);
            if (preset == null || preset.buildingPlacement == null || scene == null || scene.UnitPrefabRegistryConfig == null)
                throw new InvalidOperationException("Original factory catalogs unavailable");
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = scenario.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(scenario, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, S004BuildingDefinitionSeed, manifest, matrix, out var setup, out var reasons))
                throw new InvalidOperationException("Original S004 compiler rejected inputs: " + string.Join(";", reasons.Select(x => x.ToString())));
            ValidateS004CompiledSetupAgainstEvidence(setup, evidence);
            string[] keys = setup.Structures.Select(row => SkirmishStructureIds.VisualKey(row.StructureId)).Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
            if (keys.Length != 9 || keys.Any(string.IsNullOrEmpty)) throw new InvalidDataException("Exact nine setup building keys required");
            var pins = SourcePins("Assets/Game/Scenes/Match.unity", AssetDatabase.GetAssetPath(preset.buildingPlacement), scenePath,
                AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig), AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(expansion),
                AssetDatabase.GetAssetPath(scenario), "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv");
            pins[evidencePath] = evidenceHash;
            VerifyPins(pins);
            var originalWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld; int worldCount = Unity.Entities.World.All.Count;
            using var production = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, scene);
            var helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            var resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true); MethodInfo configure = RequireMethod(helperType, "ConfigureAuthoringMetadataResolvers");
            var parameters = configure.GetParameters();
            configure.Invoke(helper, new[] { Delegate.CreateDelegate(parameters[0].ParameterType, resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public)), Delegate.CreateDelegate(parameters[1].ParameterType, resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public)) });
            RequireMethod(helperType, "RebuildSpawnablesLookup").Invoke(helper, new object[] { production.Config.Spawnables.ToList(), production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToList() });
            RequireMethod(helperType, "ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            MethodInfo resolve = RequireMethod(helperType, "TryResolveConfiguredSpawnablePrefab"), create = RequireMethod(helperType, "CreateRuntimeBuildingDefinition");
            var missing = new JArray(); var rows = new JArray();
            foreach (string key in keys)
            {
                object[] resolveArgs = { key, null };
                if (!(bool)resolve.Invoke(helper, resolveArgs) || !(resolveArgs[1] is GameObject prefab) || prefab.name != key) throw new InvalidDataException("Configured building key mismatch: " + key);
                object definition = create.Invoke(helper, new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, null });
                var field = definition?.GetType().GetField("ProductionSlots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) throw new InvalidDataException("Original production slots unavailable: " + key);
                rows.Add(new JObject { ["prefab"] = SourceAsset(prefab), ["production_slots"] = CaptureDefinitionValue(field.GetValue(definition), key + "/ProductionSlots", missing, 0) });
            }
            if (missing.Count != 0 || !ReferenceEquals(originalWorld, Unity.Entities.World.DefaultGameObjectInjectionWorld) || Unity.Entities.World.All.Count != worldCount)
                throw new InvalidOperationException("Unsupported source values or changed World ownership");
            VerifyPins(pins);
            var result = new JObject { ["kind"] = "S004OriginalProductionSlots", ["authority"] = "OriginalS004ProductionOverlayAndDefinitionFactory", ["setup_hash"] = setup.SetupHash,
                ["seed"] = setup.Seed, ["buildings"] = rows, ["source_pins"] = JObject.FromObject(pins), ["unsupported_values"] = missing,
                ["runway_reader_supplied"] = false, ["mission_world_created"] = false, ["mission_ready"] = false };
            Directory.CreateDirectory(output); string file = Path.Combine(output, "production-slots.json");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[S004ProductionSlotReference] result=Collected buildings=" + rows.Count + " unsupported=0 mission=0 readiness=0");
        }
    }
}
#endif
