#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string S073BuildingPlacementPath = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01_BuildingPlacements.asset";
        const string S073SourceScenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        const string S073PhysicalMapDefinitionPath = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

        static JArray S073WorldRotationBits(Quaternion value)
        {
            float[] components = { value.x, value.y, value.z, value.w };
            var bits = new JArray();
            foreach (float component in components)
            {
                if (float.IsNaN(component) || float.IsInfinity(component))
                    throw new InvalidOperationException("Finite source placement quaternion required");
                bits.Add(BitConverter.ToInt32(BitConverter.GetBytes(component), 0));
            }
            return bits;
        }

        // Captures the original selected S073 map's distinct placed building prefabs
        // through the same runtime definition factory used by map placement. This is
        // finite source/bounds data only; it does not process placements or admit a mission.
        public static void RunS073MapPlacementBuildingReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishS073MapBuildingReferenceData");
            if (at < 0 || at + 1 >= args.Length)
                throw new ArgumentException("Explicit fresh S073 map building reference output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string outputFile = Path.Combine(output, "s073-map-building-reference.json");
            if (File.Exists(outputFile)) throw new InvalidOperationException("Fresh reference output required");

            SkirmishPresetConfig preset = SkirmishLegacyProductionIdentity.RequireBinding("S073");
            OperationMapDefinition source = SkirmishLegacyProductionIdentity.ResolveSource("S073", out JObject sourceResolution);
            string sourceError = null;
            bool sourceValid = source != null && source.TryValidateMetadata(out sourceError);
            if (source == null || source.OperationMapId != "opmap.skirmish.industrial_basin" ||
                !sourceValid || string.IsNullOrEmpty(source.SourceIdentityHash))
                throw new InvalidOperationException("Exact current S073 selected map source identity required: " + (sourceError ?? "missing source"));
            OperationMapDefinition physicalSource = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(S073PhysicalMapDefinitionPath);
            if (physicalSource == null || physicalSource.OperationMapId != source.SourceBinding.SourceOperationMapId ||
                source.SourceBinding.SourceOperationMapId != "opmap.skirmish.desert_base_01" ||
                source.SourceBinding.SourceIdentityHash != physicalSource.SourceIdentityHash ||
                source.SourceBinding.SourceContentHash != physicalSource.ContentHash ||
                source.SourceIdentityHash != physicalSource.SourceIdentityHash)
                throw new InvalidOperationException("S073 logical map no longer binds the exact audited original Desert Base Campaign definition");
            string scenePath = S073SourceScenePath;
            string sceneText = File.ReadAllText(scenePath);
            string sceneViewScriptGuid = AssetDatabase.AssetPathToGUID("Assets/Game/Scripts/Composition/OperationMapSceneView.cs");
            string[] sceneViewBlocks = Regex.Split(sceneText, @"(?m)^--- !u!114 ")
                .Where(block => Regex.IsMatch(block, @"(?m)^  m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*" + Regex.Escape(sceneViewScriptGuid) + @",\s*type:\s*3\}\s*$"))
                .ToArray();
            if (sceneViewBlocks.Length != 1 || !Regex.IsMatch(sceneViewBlocks[0], @"(?m)^  operationMapId: opmap\.skirmish\.desert_base_01\s*$"))
                throw new InvalidOperationException("Exact audited original Campaign scene owner component required");
            MatchCollection placementReferences = Regex.Matches(sceneViewBlocks[0], @"(?m)^  buildingPlacements: \{fileID: 11400000, guid: ([a-f0-9]{32}), type: 2\}\s*$");
            if (placementReferences.Count != 1) throw new InvalidOperationException("Unique serialized original map-building placement reference required");
            string placementsGuid = placementReferences[0].Groups[1].Value;
            string placementsPath = AssetDatabase.GUIDToAssetPath(placementsGuid);
            if (placementsPath != S073BuildingPlacementPath)
                throw new InvalidOperationException("Selected S073 map no longer resolves to its exact original building placements: " + (placementsPath ?? "<unresolved>"));

            MapBuildingPlacementConfig config = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(placementsPath);
            if (config == null || !config.SpawnOnMatchStart || config.Placements == null || config.Placements.Count != 432)
                throw new InvalidOperationException("Exact current 432-row S073 map building placement config required");

            var placementCounts = new Dictionary<GameObject, int>();
            int missingSourcePathRows = 0;
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                if (placement == null || placement.BuildingPrefab == null)
                    throw new InvalidOperationException("Original S073 map placement row " + i + " has no prefab instance");
                if (string.IsNullOrWhiteSpace(placement.SourcePath)) missingSourcePathRows++;
                placementCounts.TryGetValue(placement.BuildingPrefab, out int count);
                placementCounts[placement.BuildingPrefab] = count + 1;
            }
            if (placementCounts.Count != 23 || placementCounts.Values.Sum() != 432)
                throw new InvalidOperationException("Expected exactly 23 distinct source prefabs across the unchanged 432-row map config");

            string presetPath = AssetDatabase.GetAssetPath(preset);
            string constructionPath = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            string unitCatalogPath = AssetDatabase.GetAssetPath(preset.buildingPlacement?.UnitPrefabRegistryConfig);
            if (preset.buildingPlacement == null || preset.buildingPlacement.UnitPrefabRegistryConfig == null ||
                preset.buildingPlacement.Spawnables == null || preset.buildingPlacement.Spawnables.Count != 6 ||
                string.IsNullOrEmpty(presetPath) || string.IsNullOrEmpty(constructionPath) || string.IsNullOrEmpty(unitCatalogPath))
                throw new InvalidOperationException("Exact S073 legacy construction and unit registry required");

            GameObject[] prefabs = placementCounts.Keys.OrderBy(p => AssetDatabase.GetAssetPath(p), StringComparer.Ordinal).ToArray();
            var prefabIndices = prefabs.Select((prefab, index) => new { prefab, index }).ToDictionary(x => x.prefab, x => x.index);
            var placementRows = new JArray();
            for (int i = 0; i < config.Placements.Count; i++)
            {
                MapBuildingPlacementConfigEntry placement = config.Placements[i];
                Quaternion sourceWorldRotation = Quaternion.Euler(placement.WorldEulerAngles);
                placementRows.Add(new JObject { ["row_index"] = i, ["source_path"] = placement.SourcePath,
                    ["category"] = placement.Category, ["prefab_index"] = prefabIndices[placement.BuildingPrefab],
                    ["prefab"] = SourceAsset(placement.BuildingPrefab), ["faction_id"] = placement.FactionId,
                    ["world_center"] = BuildingReferenceVector(placement.WorldCenter),
                    ["world_position"] = BuildingReferenceVector(placement.WorldPosition),
                    ["world_euler_angles"] = BuildingReferenceVector(placement.WorldEulerAngles),
                    ["world_rotation_f32bits"] = S073WorldRotationBits(sourceWorldRotation),
                    ["world_scale"] = BuildingReferenceVector(placement.WorldScale), ["yaw_degrees"] = placement.YawDegrees,
                    ["rotate_vertical"] = placement.RotateVertical,
                    ["instantiate_visual_when_source_missing"] = placement.InstantiateVisualWhenSourceMissing,
                    ["use_geometric_footprint_center"] = placement.UseGeometricFootprintCenter });
            }
            var roots = new List<string> { presetPath, constructionPath, unitCatalogPath, placementsPath, scenePath,
                S073PhysicalMapDefinitionPath,
                AssetDatabase.GetAssetPath(source), "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073MapBuildings.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceGeometry.cs",
                "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs" };
            roots.AddRange(prefabs.Select(AssetDatabase.GetAssetPath));
            SortedDictionary<string, string> pins = SourcePins(roots.ToArray());
            World originalDefault = World.DefaultGameObjectInjectionWorld;
            var rows = new JArray();
            var missing = new JArray();
            Type helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            MethodInfo configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers");
            ParameterInfo[] parameters = configure?.GetParameters();
            if (parameters == null || parameters.Length != 2) throw new InvalidOperationException("Original building metadata resolver API changed");
            object buildingResolver = Delegate.CreateDelegate(parameters[0].ParameterType,
                resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            object unitResolver = Delegate.CreateDelegate(parameters[1].ParameterType,
                resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            configure.Invoke(helper, new[] { buildingResolver, unitResolver });
            helperType.GetMethod("RebuildSpawnablesLookup").Invoke(helper, new object[] {
                preset.buildingPlacement.Spawnables.ToList(),
                preset.buildingPlacement.UnitPrefabRegistryConfig.UnitSpawnPrefabs.Where(p => p != null).ToList() });
            // Null matches the actual S073 legacy production recipe behavior.
            helperType.GetMethod("ConfigureProductionRecipes").Invoke(helper, new object[] { null });

            using (var world = new World("S073OriginalMapBuildingReference", WorldFlags.Game))
            {
                var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                MethodInfo create = helperType.GetMethod("CreateRuntimeBuildingDefinition");
                if (create == null) throw new InvalidOperationException("Original runtime building definition factory missing");
                foreach (GameObject prefabObject in prefabs)
                {
                    string prefabPath = AssetDatabase.GetAssetPath(prefabObject);
                    if (string.IsNullOrEmpty(prefabPath)) throw new InvalidOperationException("Placed prefab source path missing: " + prefabObject.name);
                    var authoring = prefabObject.GetComponents<Component>().FirstOrDefault(c => c != null &&
                        c.GetType().FullName == "Game.Authoring.BuildingDefinitionAuthoring");
                    object definition = create.Invoke(helper, new object[] { prefabObject, prefabObject.name,
                        "Authored map building.", new Vector2Int(10, 10), 500, runway });
                    if (definition == null) throw new InvalidOperationException("Original runtime factory rejected map prefab: " + prefabObject.name);
                    var fields = new JObject();
                    foreach (FieldInfo field in definition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        fields[field.Name] = CaptureDefinitionValue(field.GetValue(definition), prefabObject.name + "/" + field.Name, missing, 0);
                    rows.Add(new JObject { ["prefab"] = SourceAsset(prefabObject), ["map_placement_rows"] = placementCounts[prefabObject],
                        ["definition_type"] = definition.GetType().FullName,
                        ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition",
                        ["fields"] = fields, ["evaluated_geometry"] = CaptureBuildingReferenceGeometry(prefabObject, authoring) });
                }
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during source-only reference capture");
            foreach (KeyValuePair<string, string> pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S073 source changed during map-building reference capture: " + pin.Key);

            var report = new JObject { ["schema_version"] = 1, ["kind"] = "S073OriginalMapBuildingReferences",
                ["authority"] = "SelectedS073MapConfig_OriginalRuntimeBuildingFactory_AndModelBounds",
                ["catalog_id"] = "S073", ["operation_map_id"] = source.OperationMapId,
                ["source_identity_hash"] = source.SourceIdentityHash, ["source_resolution"] = sourceResolution,
                ["physical_source_binding"] = new JObject { ["operation_map_id"] = source.SourceBinding.SourceOperationMapId,
                    ["source_identity_hash"] = source.SourceBinding.SourceIdentityHash,
                    ["source_content_hash"] = source.SourceBinding.SourceContentHash,
                    ["definition_path"] = S073PhysicalMapDefinitionPath },
                ["source_scene_path"] = scenePath, ["source_scene_operation_map_id"] = "opmap.skirmish.desert_base_01",
                ["placement_config"] = new JObject { ["path"] = placementsPath, ["guid"] = placementsGuid,
                    ["spawn_on_match_start"] = config.SpawnOnMatchStart, ["placement_count"] = config.Placements.Count,
                    ["hide_authoring_visuals_after_spawn"] = config.HideAuthoringVisualsAfterSpawn,
                    ["use_existing_static_presentation_when_authoring_visual_missing"] = config.UseExistingStaticPresentationWhenAuthoringVisualMissing,
                    ["distinct_prefab_count"] = rows.Count, ["missing_source_path_rows"] = missingSourcePathRows },
                ["placement_rotation_authority"] = "UnityEngine.Quaternion.Euler(MapBuildingPlacementConfigEntry.WorldEulerAngles), matching MapBuildingPlacementSpawnPrefabSystemHelper.CreateAuthoredMapVisualInstance",
                ["definitions"] = rows, ["placement_rows"] = placementRows, ["unsupported_values"] = missing, ["source_pins"] = JObject.FromObject(pins),
                ["placements_processed"] = false, ["mission_world_allocated"] = false,
                ["full_mission_initialized"] = false, ["native_visual_readiness"] = false };
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishS073MapBuildingReferences] result=Passed placementRows=432 rotations=432 prefabs=" + rows.Count +
                " unsupported=" + missing.Count + " placementsProcessed=0 missionWorld=0 fullMission=0");
        }

        // Captures the exact runway anchor produced by the original map-placement
        // owner and the original road-gate door transforms. This is source data
        // only: it does not spawn actors into a mission World or admit S073.
        public static void RunS073SpecialMapBuildingRuntimeReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishS073SpecialBuildingReferenceData");
            if (at < 0 || at + 1 >= args.Length)
                throw new ArgumentException("Explicit fresh S073 special-building output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string outputFile = Path.Combine(output, "s073-special-building-runtime-reference.json");
            if (File.Exists(outputFile)) throw new InvalidOperationException("Fresh special-building reference output required");

            var config = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(S073BuildingPlacementPath);
            if (config == null || config.Placements == null || config.Placements.Count != 432)
                throw new InvalidOperationException("Exact unchanged S073 432-row placement source required");
            var airportRows = config.Placements.Select((placement, index) => new { placement, index })
                .Where(x => x.placement != null &&
                    (string.Equals(x.placement.Category, "Building_Airport", StringComparison.Ordinal) ||
                     (x.placement.BuildingPrefab != null && x.placement.BuildingPrefab.name.IndexOf("Airport", StringComparison.OrdinalIgnoreCase) >= 0)))
                .ToArray();
            if (airportRows.Length != 1 || airportRows[0].index != 0)
                throw new InvalidOperationException("Expected the unique original airport placement at S073 source row zero");
            var gates = config.Placements.Select((placement, index) => new { placement, index })
                .Where(x => x.placement != null && x.placement.BuildingPrefab != null &&
                    x.placement.BuildingPrefab.name == "Building_Road_Barrier")
                .ToArray();
            if (gates.Length != 3 || !gates.Select(x => x.index).OrderBy(x => x).SequenceEqual(new[] { 108, 109, 110 }))
                throw new InvalidOperationException("Expected the three original road-barrier placements at rows 108-110");

            World defaultWorldBeforeSceneLoad = World.DefaultGameObjectInjectionWorld;
            Scene scene = EditorSceneManager.OpenScene(S073SourceScenePath, OpenSceneMode.Additive);
            World defaultWorldAfterSceneLoad = World.DefaultGameObjectInjectionWorld;
            var sceneViews = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Game.Composition.OperationMapSceneView>(true))
                .Where(view => view != null && view.OperationMapId == "opmap.skirmish.desert_base_01")
                .ToArray();
            string viewError = null;
            if (sceneViews.Length != 1 || !sceneViews[0].TryValidate(out viewError))
                throw new InvalidOperationException("Original Campaign map scene view failed validation: " + (viewError ?? "unique view missing"));
            var view = sceneViews[0];
            if (view.BuildingPlacements != config || view.BuildingAuthoringRoot == null ||
                view.MapRoot == null || view.BuildingAuthoringRoot.parent != view.MapRoot)
                throw new InvalidOperationException("S073 scene does not expose the exact selected placement config and original root chain");

            var helperType = LoadedType("Game.Runtime.MapBuildingPlacementSpawnPrefabSystemHelper");
            var attach = helperType.GetMethod("TryAttachMapRunwayAnchor", BindingFlags.Static | BindingFlags.NonPublic);
            if (attach == null) throw new InvalidOperationException("Original runtime map-runway anchor owner missing");
            var temporary = new GameObject("S073_SourceOnly_RunwayAnchorProbe");
            JObject runwayResult;
            JObject runwayPathingOpening = null;
            try
            {
                var placement = airportRows[0].placement;
                temporary.transform.SetPositionAndRotation(placement.WorldPosition, Quaternion.Euler(placement.WorldEulerAngles));
                temporary.transform.localScale = placement.WorldScale;
                object attached = attach.Invoke(null, new object[] { view.BuildingAuthoringRoot, placement, temporary.transform, null });
                Transform anchor = temporary.transform.Find("Runway");
                Transform start = anchor != null ? anchor.Find("Runway_Start") : null;
                Transform end = anchor != null ? anchor.Find("Runway_End") : null;
                if (attached is bool ok && ok && anchor != null && start != null && end != null)
                {
                    runwayResult = new JObject {
                        ["resolved"] = true,
                        ["row_index"] = airportRows[0].index,
                        ["category"] = placement.Category,
                        ["prefab_path"] = AssetDatabase.GetAssetPath(placement.BuildingPrefab),
                        ["placement_position"] = BuildingReferenceVector(placement.WorldPosition),
                        ["anchor_position"] = BuildingReferenceVector(anchor.position),
                        ["anchor_rotation_xyzw"] = new JArray(anchor.rotation.x, anchor.rotation.y, anchor.rotation.z, anchor.rotation.w),
                        ["start_position"] = BuildingReferenceVector(start.position),
                        ["end_position"] = BuildingReferenceVector(end.position),
                        ["owner"] = "MapBuildingPlacementSpawnPrefabSystemHelper.TryAttachMapRunwayAnchor",
                        ["scene_path"] = S073SourceScenePath
                    };
                    Type definitionType = LoadedType("Game.Runtime.BuildingDefinition");
                    object definition = Activator.CreateInstance(definitionType, true);
                    definitionType.GetField("DisplayName").SetValue(definition, "Building_Airport");
                    definitionType.GetField("Prefab").SetValue(definition, placement.BuildingPrefab);
                    definitionType.GetField("HasRunway").SetValue(definition, true);
                    Type runtimeBuildingType = LoadedType("Game.Runtime.RuntimeBuildingEntity");
                    object runtimeBuilding = Activator.CreateInstance(runtimeBuildingType, true);
                    runtimeBuildingType.GetField("Definition").SetValue(runtimeBuilding, definition);
                    runtimeBuildingType.GetField("Instance").SetValue(runtimeBuilding, temporary);
                    Type pathingType = LoadedType("Game.Runtime.BuildingRunwayPathingSurface");
                    MethodInfo resolveOpening = pathingType.GetMethod("TryResolve", BindingFlags.Static | BindingFlags.Public);
                    if (resolveOpening == null) throw new InvalidOperationException("Original runway pathing-opening owner missing");
                    object[] openingArgs = { runtimeBuilding, null };
                    if (!(resolveOpening.Invoke(null, openingArgs) is bool openingResolved) || !openingResolved)
                        throw new InvalidOperationException("Original runway owner could not derive the airport pathing opening from its captured runtime anchor");
                    object opening = openingArgs[1];
                    Type openingType = opening.GetType();
                    JArray ReadFloat2(string name)
                    {
                        object pair = openingType.GetField(name).GetValue(opening);
                        Type pairType = pair.GetType();
                        return new JArray((float)pairType.GetField("x").GetValue(pair), (float)pairType.GetField("y").GetValue(pair));
                    }
                    runwayPathingOpening = new JObject {
                        ["center_xz"] = ReadFloat2("Center"),
                        ["right_xz"] = ReadFloat2("Right"),
                        ["forward_xz"] = ReadFloat2("Forward"),
                        ["half_extents_xz"] = ReadFloat2("HalfExtents"),
                        ["valid"] = (bool)openingType.GetProperty("IsValid").GetValue(opening),
                        ["owner"] = "BuildingRunwayPathingSurface.TryResolve via BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData"
                    };
                    if (!(bool)runwayPathingOpening["valid"])
                        throw new InvalidOperationException("Original runway owner returned an invalid pathing opening");
                }
                else
                {
                    Transform runways = view.MapRoot.Find("Runways");
                    Renderer[] candidateRenderers = runways != null ? runways.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                    runwayResult = new JObject {
                        ["resolved"] = false,
                        ["reason"] = "Original owner returned false; no runway anchor was synthesized",
                        ["row_index"] = airportRows[0].index,
                        ["runways_root_found"] = runways != null,
                        ["runways_child_count"] = runways != null ? runways.childCount : 0,
                        ["renderer_count"] = candidateRenderers.Length,
                        ["renderer_hierarchy_paths"] = new JArray(candidateRenderers.Select(renderer => HierarchyPath(renderer.transform))),
                        ["owner"] = "MapBuildingPlacementSpawnPrefabSystemHelper.TryAttachMapRunwayAnchor",
                        ["scene_path"] = S073SourceScenePath
                    };
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }

            var gateRows = new JArray();
            foreach (var row in gates)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(row.placement.BuildingPrefab, scene) as GameObject;
                if (instance == null) throw new InvalidOperationException("Could not instantiate original road-barrier prefab for read-only source capture");
                try
                {
                    Transform door = FindDescendantByName(instance.transform, "Door_Z");
                    string selectedName = "Door_Z";
                    if (door == null)
                    {
                        door = FindDescendantByName(instance.transform, "SM_Prop_Road_Barrier_Arm_01");
                        selectedName = "SM_Prop_Road_Barrier_Arm_01";
                    }
                    if (door == null)
                        throw new InvalidOperationException("Original road-barrier visual has neither supported runtime door transform");
                    gateRows.Add(new JObject {
                        ["row_index"] = row.index,
                        ["category"] = row.placement.Category,
                        ["faction_id"] = row.placement.FactionId,
                        ["prefab_path"] = AssetDatabase.GetAssetPath(row.placement.BuildingPrefab),
                        ["door_lookup_name"] = selectedName,
                        ["door_hierarchy_path"] = HierarchyPath(door),
                        ["door_local_position"] = BuildingReferenceVector(door.localPosition),
                        ["door_local_rotation_xyzw"] = new JArray(door.localRotation.x, door.localRotation.y, door.localRotation.z, door.localRotation.w),
                        ["door_local_euler_angles"] = BuildingReferenceVector(door.localEulerAngles),
                        ["owner"] = "BuildingRuntimeVisualPresentationSystemHelper.InitializeBuildingVisuals"
                    });
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }

            if (!ReferenceEquals(defaultWorldAfterSceneLoad, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed after source-scene load during special-building reference capture");
            string[] roots = { S073SourceScenePath, S073BuildingPlacementPath,
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073MapBuildings.cs",
                "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs",
                "Assets/Game/Scripts/Systems/BuildingRunwayPathingSurface.cs",
                "Assets/Game/Scripts/Components/StaticGridBlockerSurfaceOpeningComponent.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeVisualPresentationSystemHelper.cs",
                AssetDatabase.GetAssetPath(airportRows[0].placement.BuildingPrefab),
                AssetDatabase.GetAssetPath(gates[0].placement.BuildingPrefab) };
            SortedDictionary<string, string> pins = SourcePins(roots);
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S073 source changed during special-building reference capture: " + pin.Key);
            var report = new JObject {
                ["schema_version"] = 1,
                ["kind"] = "S073OriginalSpecialBuildingRuntimeReference",
                ["authority"] = "OriginalCampaignScene_OriginalPlacementAndVisualOwners",
                ["catalog_id"] = "S073",
                ["source_scene_path"] = S073SourceScenePath,
                ["airport_runway"] = runwayResult,
                ["airport_pathing_opening"] = runwayPathingOpening,
                ["road_gates"] = gateRows,
                ["source_pins"] = JObject.FromObject(pins),
                ["mission_world_allocated"] = false,
                ["editor_default_world_changed_by_source_scene_load"] = !ReferenceEquals(defaultWorldBeforeSceneLoad, defaultWorldAfterSceneLoad),
                ["placements_processed"] = false,
                ["full_mission_initialized"] = false,
                ["native_visual_readiness"] = false
            };
            Directory.CreateDirectory(output);
            File.WriteAllText(outputFile, report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishS073SpecialBuildingReference] result=Passed runwayResolved=" + runwayResult["resolved"] +
                " pathingOpening=" + (runwayPathingOpening != null) + " roadGates=" + gateRows.Count + " missionWorld=0 fullMission=0");
        }

        static string HierarchyPath(Transform value)
        {
            if (value == null) return string.Empty;
            var names = new Stack<string>();
            for (Transform current = value; current != null; current = current.parent) names.Push(current.name);
            return string.Join("/", names.ToArray());
        }

        static Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendantByName(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
