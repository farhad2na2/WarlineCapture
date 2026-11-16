#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        public static void RunCompleteUnitReferenceData()
        {
            RunReferenceData();
            RunUnitReferenceData();
        }

        // Authoring metadata only. No mission, production update, instantiated
        // transport, camera state or future command/outcome is sampled here.
        public static void RunUnitReferenceData()
        {
            RunUnitReferenceDataFor("S002", 104731, true);
        }

        // Asset-only selected mission context. The original S002 entry point
        // above retains its existing identity and output contract.
        public static void RunSelectedUnitReferenceData()
        {
            var args = Environment.GetCommandLineArgs();
            int missionAt = Array.IndexOf(args, "-skirmishUnitScenario");
            int seedAt = Array.IndexOf(args, "-skirmishUnitSeed");
            if (missionAt < 0 || missionAt + 1 >= args.Length || seedAt < 0 || seedAt + 1 >= args.Length ||
                !SkirmishMissionDispatch.Canonical(args[missionAt + 1]) ||
                !int.TryParse(args[seedAt + 1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int seed))
                throw new ArgumentException("Explicit canonical unit metadata scenario and seed required");
            RunUnitReferenceDataFor(args[missionAt + 1], seed, false);
        }

        // A separate AssetDatabase readback checks the captured GUID/local-ID
        // bindings. It does not bake or admit runtime entity templates.
        public static void RunSelectedUnitReferenceDataWithIdentityAudit()
        {
            RunSelectedUnitReferenceData();
            RunUnitReferenceDataIdentityAudit();
        }

        // Exact compiler-selected handoff for authored, non-packaged mission inputs.
        // The validated setup is managed value data; metadata still comes from the
        // original native authoring factories below.
        public static void RunCompilerSelectedUnitReferenceDataWithIdentityAudit()
        {
            var args = Environment.GetCommandLineArgs();
            int missionAt = Array.IndexOf(args, "-skirmishUnitScenario");
            int seedAt = Array.IndexOf(args, "-skirmishUnitSeed");
            int requestAt = Array.IndexOf(args, "-skirmishUnitPhysicalInputRequest");
            int requestShaAt = Array.IndexOf(args, "-skirmishUnitPhysicalInputRequestSha256");
            int resultAt = Array.IndexOf(args, "-skirmishUnitCompilerResult");
            int resultShaAt = Array.IndexOf(args, "-skirmishUnitCompilerResultSha256");
            int outputAt = Array.IndexOf(args, "-skirmishUnitReferenceData");
            if (missionAt < 0 || missionAt + 1 >= args.Length || seedAt < 0 || seedAt + 1 >= args.Length ||
                requestAt < 0 || requestAt + 1 >= args.Length || requestShaAt < 0 || requestShaAt + 1 >= args.Length ||
                resultAt < 0 || resultAt + 1 >= args.Length || resultShaAt < 0 || resultShaAt + 1 >= args.Length ||
                outputAt < 0 || outputAt + 1 >= args.Length ||
                !SkirmishMissionDispatch.Canonical(args[missionAt + 1]) ||
                !int.TryParse(args[seedAt + 1], System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int seed))
                throw new ArgumentException("Exact compiler-selected mission, result/request pins and fresh output required");
            string catalogId = args[missionAt + 1];
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
            RunUnitReferenceDataFor(catalogId, seed, false, selectedSetup, selectionDescriptor,
                new JObject { ["path"] = requestPath, ["sha256"] = requestSha },
                new JObject { ["path"] = resultPath, ["sha256"] = resultSha }, assetReadback, compilerPins);
            RunUnitReferenceDataIdentityAudit();
        }

        private static void RunUnitReferenceDataIdentityAudit()
        {
            var args=Environment.GetCommandLineArgs(); int at=Array.IndexOf(args,"-skirmishUnitReferenceData");
            if(at<0 || at+1>=args.Length)throw new ArgumentException("Unit metadata output required");
            string output=Path.GetFullPath(args[at+1]); string dataPath=Path.Combine(output,"unit-production-metadata.json");
            string dataHash=SkirmishOfflineDataExporter.HashFile(dataPath);
            var data=JObject.Parse(File.ReadAllText(dataPath));
            var units=data["units"] as JArray ?? throw new InvalidDataException("Captured unit list missing");
            var transports=data["transports"] as JArray ?? throw new InvalidDataException("Captured transport list missing");
            var pins=data["source_pins"] as JObject ?? throw new InvalidDataException("Captured pins missing");
            var identities=new Dictionary<string,JObject>(StringComparer.Ordinal);
            void Check(JToken token)
            {
                string path=(string)token?["path"],key=(string)token?["key"];
                if(String.IsNullOrWhiteSpace(path)||String.IsNullOrWhiteSpace(key) ||
                    pins[path]==null || pins[path+".meta"]==null)throw new InvalidDataException("Pinned unit asset identity required");
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset==null || !EditorUtility.IsPersistent(asset) || asset.name!=key ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId) ||
                    guid!=(string)token["guid"] || localId!=(long?)token["local_id"])
                    throw new InvalidDataException("Independent unit prefab identity readback differs: "+path);
                var row=new JObject { ["key"]=key,["path"]=path,["guid"]=guid,["local_id"]=localId };
                if(identities.TryGetValue(key,out var prior) && !JToken.DeepEquals(prior,row))
                    throw new InvalidDataException("Conflicting unit identity: "+key);
                identities[key]=row;
            }
            foreach(JObject row in units)Check(row["prefab"]);
            foreach(JObject row in transports)Check(row["prefab"]);
            foreach(var pin in pins.Properties())
                if(SkirmishOfflineDataExporter.HashFile(pin.Name)!=(string)pin.Value)
                    throw new InvalidDataException("Source changed during identity readback: "+pin.Name);
            if(SkirmishOfflineDataExporter.HashFile(dataPath)!=dataHash)throw new InvalidDataException("Unit metadata changed during identity readback");
            var result=new JObject { ["kind"]="AuthoredUnitPrefabIdentityReadback",["catalog_id"]=(string)data["catalog_id"],
                ["setup_hash"]=(uint?)data["setup_hash"],["seed"]=(int?)data["seed"],["metadata_sha256"]=dataHash,
                ["units"]=units.Count,["transports"]=transports.Count,["identities"]=JArray.FromObject(identities.Values.OrderBy(x=>(string)x["key"],StringComparer.Ordinal)),
                ["runtime_templates_verified"]=false,["mission_ready"]=false };
            string target=Path.Combine(output,"unit-prefab-identity-readback.json");
            using(var stream=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream))writer.Write(result.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
            Debug.Log("[SkirmishAuthoredUnitIdentityAudit] result=Passed units="+units.Count+" distinct="+identities.Count+" transports="+transports.Count+" runtimeTemplates=0 readiness=0");
        }

        private static JObject ReadUniqueObject(string path)
        {
            using (var reader = new JsonTextReader(new StringReader(File.ReadAllText(path)))
            {
                DateParseHandling = Newtonsoft.Json.DateParseHandling.None
            })
            {
                var value = JObject.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Load
                });
                if (reader.Read()) throw new InvalidDataException("Trailing data after pinned JSON object: " + path);
                return value;
            }
        }

        private static bool IsSha256(string value)
        {
            return value != null && value.Length == 64 && value.All(c =>
                c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        }

        private static void RunUnitReferenceDataFor(string catalogId, int seed, bool requireS002OpeningKeys,
            SkirmishResolvedSetup selectedSetup = null, JObject compilerSelectionDescriptor = null,
            JObject physicalInputRequestDescriptor = null, JObject compilerResultDescriptor = null,
            JObject compilerAssetReadback = null, IDictionary<string, string> compilerPins = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-skirmishUnitReferenceData");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            // Retain the original preset for legacy entries. Expanded catalog
            // IDs have a compiler-selected definition but no legacy preset
            // token; their shared source factory uses the same base placement
            // catalog as the building metadata owner.
            int sourcePresetIndex = SkirmishMissionDispatch.TryGetLegacyIndex(catalogId, out int legacyIndex)
                ? legacyIndex : SkirmishPresetConfig.DesertBaseScenarioIndex;
            var preset = SkirmishPresetConfig.Load(sourcePresetIndex);
            if (preset == null || preset.buildingPlacement == null) throw new InvalidOperationException("Actual source preset missing");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null) throw new InvalidOperationException("Actual scene unit catalog missing");
            var expansion = SkirmishExpansionCatalogConfig.Load();
            SkirmishResolvedSetup setup;
            if (selectedSetup != null)
            {
                if (selectedSetup.CatalogId != catalogId || selectedSetup.Seed != seed ||
                    selectedSetup.Forces == null || selectedSetup.Structures == null || selectedSetup.RoleOverlays == null ||
                    selectedSetup.SizeId == SkirmishSizeId.None || selectedSetup.DifficultyId == SkirmishDifficultyId.None ||
                    compilerSelectionDescriptor == null || physicalInputRequestDescriptor == null ||
                    compilerResultDescriptor == null || compilerAssetReadback == null || compilerPins == null)
                    throw new InvalidOperationException("Exact verified compiler-selected setup and evidence required");
                setup = selectedSetup;
            }
            else
            {
                var definition = expansion?.Definitions.FirstOrDefault(d => d != null && d.CatalogId == catalogId);
                if (definition == null) throw new InvalidOperationException("Actual selected scenario definition missing");
                if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError)) throw new InvalidOperationException(matrixError);
                var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
                if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, seed,
                    manifest, matrix, out setup, out var reasons)) throw new InvalidOperationException(string.Join(";", reasons));
            }
            using var catalog = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            var units = catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToArray();
            if (units.Length == 0 || units.Length > 1024) throw new InvalidOperationException("Configured source unit domain invalid");
            var roots = units.Where(p => p != null).Select(AssetDatabase.GetAssetPath).Concat(new[] {
                AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(preset.buildingPlacement),
                AssetDatabase.GetAssetPath(sceneConfig), AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig),
                AssetDatabase.GetAssetPath(expansion) }).Distinct().ToArray();
            var pins = SourcePins(roots);
            if (compilerPins != null)
                foreach (var pin in compilerPins)
                {
                    if (pins.TryGetValue(pin.Key, out string prior) && prior != pin.Value)
                        throw new InvalidDataException("Compiler-selected source pin conflicts with metadata owner pin: " + pin.Key);
                    pins[pin.Key] = pin.Value;
                }
            var productionFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper")
                .GetMethod("TryGetMetadata", BindingFlags.Public | BindingFlags.Static);
            var definitionFactory = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper")
                .GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Public | BindingFlags.Static);
            if (productionFactory == null || definitionFactory == null) throw new InvalidOperationException("Original source metadata factory absent");
            var rows = new JArray(); var missing = new JArray(); var transports = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int index = 0; index < units.Length; index++)
            {
                GameObject prefab = units[index];
                if (prefab == null) { rows.Add(new JObject { ["index"] = index, ["prefab"] = new JObject { ["codec"] = "null-v1" } }); continue; }
                var productionArgs = new object[] { prefab, null }; var definitionArgs = new object[] { prefab, null };
                bool hasDefinition = (bool)definitionFactory.Invoke(null, definitionArgs);
                bool hasProduction = (bool)productionFactory.Invoke(null, productionArgs);
                var row = new JObject { ["index"] = index, ["key"] = prefab.name, ["prefab"] = SourceAsset(prefab),
                    ["has_definition_metadata"] = hasDefinition, ["has_production_metadata"] = hasProduction };
                if (hasDefinition) row["definition_metadata"] = CaptureMetadataFields(definitionArgs[1], prefab.name + "/definition", missing);
                if (hasDefinition || hasProduction)
                    row["authoring_catalog_properties"] = CaptureUnitCatalogAuthoringProperties(prefab, missing);
                if (hasProduction)
                {
                    row["production_metadata"] = CaptureMetadataFields(productionArgs[1], prefab.name + "/production", missing);
                    var transport = productionArgs[1].GetType().GetField("ProductionTransportPrefab").GetValue(productionArgs[1]) as GameObject;
                    if (transport != null) transports[AssetDatabase.GetAssetPath(transport)] = transport;
                }
                rows.Add(row);
            }
            if (requireS002OpeningKeys)
                foreach (string key in OpeningKeys)
                    if (!rows.OfType<JObject>().Any(r => (string)r["key"] == key && (bool?)r["has_definition_metadata"] == true && (bool?)r["has_production_metadata"] == true))
                        throw new InvalidOperationException("Required opening unit metadata missing: " + key);
            var transportRows = new JArray();
            foreach (var pair in transports.OrderBy(p => p.Key, StringComparer.Ordinal))
                transportRows.Add(CaptureSourceTransportGraph(pair.Value, missing));
            if (missing.Count != 0) throw new InvalidOperationException("Required authored metadata has unsupported values: " + missing);
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed during export: " + pin.Key);
            var result = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishUnitProductionMetadata",
                ["authority"] = "OriginalConfiguredAuthoringFactories_NotMissionState", ["setup_hash"] = setup.SetupHash,
                ["seed"] = setup.Seed, ["source_preset_index"] = sourcePresetIndex,
                ["units"] = rows, ["transports"] = transportRows, ["source_pins"] = JObject.FromObject(pins),
                ["unsupported_values"] = missing, ["baked_actor_footprint_substituted"] = false,
                ["ui_catalog_runtime_policy_sampled"] = false,
                ["runtime_camera_basis_exported"] = false, ["full_mission_initialized"] = false, ["native_visual_readiness"] = false };
            if (!requireS002OpeningKeys)
            {
                result["catalog_id"] = setup.CatalogId;
                result["difficulty"] = setup.DifficultyId.ToString();
                result["size"] = setup.SizeId.ToString();
            }
            if (selectedSetup != null)
            {
                result["compiler_selection"] = new JObject
                {
                    ["result"] = compilerResultDescriptor.DeepClone(),
                    ["physical_input_request"] = physicalInputRequestDescriptor.DeepClone(),
                    ["catalog_id"] = catalogId, ["seed"] = seed, ["size"] = setup.SizeId.ToString(),
                    ["difficulty"] = setup.DifficultyId.ToString(), ["setup_hash"] = setup.SetupHash,
                    ["operation_map_id"] = setup.OperationMapId, ["source_content_hash"] = setup.ContentHash,
                    ["layout_fingerprint"] = setup.LayoutBindingFingerprint,
                    ["status"] = "ConfigurationCompiled", ["runtime_admission"] = false,
                    ["source_fit_certified"] = false, ["mission_ready"] = false,
                    ["native_acceptance"] = false, ["world_allocated"] = false
                };
                result["compiler_selection_asset_readback"] = compilerAssetReadback.DeepClone();
            }
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "unit-production-metadata.json"), result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishAuthoredUnitProductionMetadata] result=Passed units=" + rows.Count + " transports=" + transportRows.Count + " unsupported=0 fullMission=0");
        }

        static JObject CaptureMetadataFields(object value, string path, JArray missing)
        {
            var fields = new JObject();
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), path + "/" + field.Name, missing, 0);
            return new JObject { ["type"] = value.GetType().FullName, ["fields"] = fields };
        }

        static JObject CaptureUnitCatalogAuthoringProperties(GameObject prefab, JArray missing)
        {
            var authoring = prefab.GetComponents<Component>().SingleOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.UnitGridAuthoring");
            if (authoring == null) throw new InvalidOperationException("Required source unit authoring absent: " + prefab.name);
            var fields = new JObject();
            foreach (string name in new[] { "ConfiguredDisplayName", "ConfiguredDescription", "CanRequest", "MaterialsCost", "Price",
                "ProductionDurationSeconds", "PortraitSprite", "PortraitCardSprite", "PortraitActionSprite", "IsAirUnit", "IsProductionTransportUnit",
                "SoldierTransportCapacity", "ConfiguredAllowIdleWander", "ConfiguredResourceHaulerBarrelCapacity", "ConfiguredCanAttack",
                "ConfiguredAttackDamage", "ConfiguredAttackRange", "ConfiguredSpeed", "ConfiguredMaxHealth" })
            {
                var property = authoring.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property == null) throw new InvalidOperationException("Required catalog authoring getter absent: " + name);
                fields[name] = CaptureDefinitionValue(property.GetValue(authoring), prefab.name + "/catalogAuthoring/" + name, missing, 0);
            }
            return new JObject { ["source_type"] = authoring.GetType().FullName, ["fields"] = fields,
                ["runtime_catalog_admission_and_role_overlay_applied"] = false };
        }

        static JObject CaptureSourceTransportGraph(GameObject prefab, JArray missing)
        {
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            if (transforms.Length == 0 || transforms.Length > 4096) throw new InvalidOperationException("Source transport hierarchy bound exceeded");
            var indices = transforms.Select((t, i) => new { t, i }).ToDictionary(p => p.t, p => p.i);
            var nodes = new JArray();
            foreach (var transform in transforms)
            {
                var meshes = new JArray();
                foreach (var mesh in transform.GetComponents<MeshFilter>())
                    if (mesh.sharedMesh != null)
                    {
                        var renderer = transform.GetComponent<MeshRenderer>();
                        meshes.Add(new JObject { ["kind"] = "MeshFilter", ["mesh"] = SourceAsset(mesh.sharedMesh),
                            ["renderer_present"] = renderer != null,
                            ["renderer_enabled"] = renderer != null ? (JToken)renderer.enabled : JValue.CreateNull(),
                            ["local_bounds"] = CaptureDefinitionValue(mesh.sharedMesh.bounds, prefab.name + "/meshBounds", missing, 0) });
                    }
                foreach (var mesh in transform.GetComponents<SkinnedMeshRenderer>())
                    if (mesh.sharedMesh != null) meshes.Add(new JObject { ["kind"] = "SkinnedMeshRenderer", ["mesh"] = SourceAsset(mesh.sharedMesh),
                        ["local_bounds"] = CaptureDefinitionValue(mesh.localBounds, prefab.name + "/skinBounds", missing, 0), ["enabled"] = mesh.enabled });
                var renderers = new JArray();
                foreach (var renderer in transform.GetComponents<Renderer>())
                    renderers.Add(new JObject { ["type"] = renderer.GetType().FullName, ["enabled"] = renderer.enabled,
                        ["local_bounds"] = CaptureDefinitionValue(renderer.localBounds, prefab.name + "/rendererLocalBounds", missing, 0),
                        ["world_bounds"] = CaptureDefinitionValue(renderer.bounds, prefab.name + "/rendererWorldBounds", missing, 0) });
                nodes.Add(new JObject { ["index"] = indices[transform], ["parent"] = transform.parent != null && indices.TryGetValue(transform.parent, out int parent) ? parent : -1,
                    ["name"] = transform.name, ["active_self"] = transform.gameObject.activeSelf,
                    ["local_position"] = CaptureDefinitionValue(transform.localPosition, prefab.name + "/position", missing, 0),
                    ["local_rotation"] = CaptureDefinitionValue(transform.localRotation, prefab.name + "/rotation", missing, 0),
                    ["initial_local_euler_degrees"] = CaptureDefinitionValue(transform.localEulerAngles, prefab.name + "/euler", missing, 0),
                    ["local_scale"] = CaptureDefinitionValue(transform.localScale, prefab.name + "/scale", missing, 0), ["meshes"] = meshes, ["renderers"] = renderers });
            }
            var row = new JObject { ["prefab"] = SourceAsset(prefab), ["nodes"] = nodes };
            var authoring = prefab.GetComponents<Component>().SingleOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.UnitGridAuthoring");
            if (authoring != null)
            {
                var fields = new JObject();
                foreach (string name in new[] { "IsAirUnit", "IsProductionTransportUnit", "SoldierTransportCapacity", "VehicleTransportCapacity", "CargoWeightCapacity",
                    "TransportCruiseHeight", "RunwayTaxiSpeed", "ConfiguredSpeed", "ProductionTransportArrivalSeconds", "ProductionTransportHoldForNextReadySeconds",
                    "ProductionTransportMaxConcurrent", "ProductionTransportRequiresAirportRunway", "ProductionTransportUsesRunwayLanding" })
                {
                    var property = authoring.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (property == null) throw new InvalidOperationException("Source transport property missing: " + name);
                    fields[name] = CaptureDefinitionValue(property.GetValue(authoring), prefab.name + "/" + name, missing, 0);
                }
                row["authoring_properties"] = fields;
            }
            return row;
        }
    }
}
#endif
