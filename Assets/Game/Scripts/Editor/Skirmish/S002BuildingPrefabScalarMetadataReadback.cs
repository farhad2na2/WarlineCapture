#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Skirmish
{
    /// <summary>
    /// Compares the eight T1 source-projected building rows with the original
    /// metadata owner on isolated prefab-content roots. No authored or mission
    /// scene, World, Baker, or saved prefab is used.
    /// </summary>
    public static class S002BuildingPrefabScalarMetadataReadback
    {
        private const string Marker = "[S002BuildingPrefabScalarReadback] result=Passed rows=8 scope=ImportedPrefabScalarMetadata";
        private const string Scope = "ImportedPrefabContentsPreviewScenes_NoAuthoredOrMissionScene_NoWorld_NoBaker_NoPrefabSave_NoFit_NoReadiness";
        private const string OwnerTypeName = "Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper";
        private const string OwnerMethodName = "TryGetBuildingDefinitionMetadata";

        public static void Run()
        {
            string output = null;
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string[] args = Environment.GetCommandLineArgs();
                output = Path.GetFullPath(Arg(args, "-s002-building-scalar-readback-output"));
                string manifestPath = Path.GetFullPath(Arg(args, "-s002-building-scalar-readback-pins"));
                RequireUnder(root, output, "Observation output");
                RequireUnder(root, manifestPath, "Pin manifest");

                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
                if ((string)manifest["kind"] != "S002BuildingPrefabScalarReadbackPinManifest" ||
                    (string)manifest["scope"] != "ImportedPrefabScalarMetadata" ||
                    (string)manifest["t1_receipt_sha256"] != (string)manifest["source_pins"]?[(string)manifest["t1_receipt_path"]] ||
                    (string)manifest["t1_projection_sha256"] != (string)manifest["source_pins"]?[(string)manifest["t1_projection_path"]] ||
                    (string)manifest["t1_inputs_sha256"] != (string)manifest["source_pins"]?[(string)manifest["t1_inputs_path"]])
                    throw new InvalidDataException("Exact T1 scalar source projection manifest required.");

                JObject sourcePins = manifest["source_pins"] as JObject ??
                    throw new InvalidDataException("T1 source and readback-owner pins required.");
                JObject before = HashPins(root, sourcePins);
                string receiptPath = ResolveRelative(root, RequiredString(manifest, "t1_receipt_path"));
                string projectionPath = ResolveRelative(root, RequiredString(manifest, "t1_projection_path"));
                string inputsPath = ResolveRelative(root, RequiredString(manifest, "t1_inputs_path"));
                JObject t1Receipt = JObject.Parse(File.ReadAllText(receiptPath));
                JObject projection = JObject.Parse(File.ReadAllText(projectionPath));
                JObject inputs = JObject.Parse(File.ReadAllText(inputsPath));
                ValidateT1(t1Receipt, projection, inputs, manifest);
                ValidateProjectionPins(root, projection, sourcePins);

                Dictionary<string, JObject> expected = (projection["building_definition_scalar_metadata"]?["rows"] as JArray)
                    ?.OfType<JObject>().ToDictionary(row => RequiredString(row, "key"), StringComparer.Ordinal)
                    ?? throw new InvalidDataException("Pinned T1 scalar policy result rows required.");
                Dictionary<string, JObject> providerFacts = ResolveProviderFacts(projection);
                var rows = new JArray();
                var issues = new List<string>();
                MethodInfo metadataOwner = ResolveMetadataOwner();
                JArray inputRows = inputs["rows"] as JArray;
                foreach (JObject input in inputRows.OfType<JObject>())
                {
                    string key = RequiredString(input, "key");
                    try
                    {
                        if (!expected.TryGetValue(key, out JObject expectedRow) ||
                            !providerFacts.TryGetValue(key, out JObject providerFact))
                            throw new InvalidDataException("T1 expected scalar/provider row is missing.");
                        rows.Add(ObserveProvider(root, input, expectedRow, providerFact, metadataOwner));
                    }
                    catch (Exception error)
                    {
                        issues.Add(key + ": " + error.Message);
                        rows.Add(new JObject
                        {
                            ["key"] = key,
                            ["status"] = "Failed",
                            ["source_prefab_path"] = input["source_prefab_path"],
                            ["source_owner_prefab_path"] = input["source_owner_prefab_path"],
                            ["source_config_path"] = input["source_config_path"],
                            ["t1_prefab_component_file_id_is_source_component_not_root_id"] = input["prefab_component_file_id"],
                            ["temporary_prefab_contents_preview_scene_used"] = true,
                            ["prefab_contents_unload_attempted_without_save"] = true,
                            ["issues"] = new JArray(error.Message)
                        });
                    }
                }
                if (rows.Count != 8 || rows.Select(row => (string)row["key"]).Distinct(StringComparer.Ordinal).Count() != 8)
                    issues.Add("Exactly eight distinct T1 building candidates are required.");

                JObject after = HashPins(root, sourcePins);
                if (!JToken.DeepEquals(before, after)) issues.Add("Pinned T1/source-owner inputs changed during readback.");
                bool passed = issues.Count == 0 && rows.All(row => (string)row["status"] == "Passed");
                var result = new JObject
                {
                    ["kind"] = "S002BuildingPrefabScalarMetadataObservation",
                    ["status"] = passed ? "Passed" : "Failed",
                    ["scope"] = Scope,
                    ["unity_version"] = Application.unityVersion,
                    ["t1_receipt_path"] = manifest["t1_receipt_path"],
                    ["t1_receipt_sha256"] = manifest["t1_receipt_sha256"],
                    ["t1_projection_path"] = manifest["t1_projection_path"],
                    ["t1_projection_sha256"] = manifest["t1_projection_sha256"],
                    ["t1_inputs_path"] = manifest["t1_inputs_path"],
                    ["t1_inputs_sha256"] = manifest["t1_inputs_sha256"],
                    ["candidate_count"] = rows.Count,
                    ["candidates"] = rows,
                    ["issues"] = new JArray(issues),
                    ["temporary_prefab_contents_preview_scene_used"] = true,
                    ["authored_or_mission_scene_loaded"] = false,
                    ["world_created"] = false,
                    ["baker_invoked"] = false,
                    ["prefab_asset_saved"] = false,
                    ["source_fit_verified"] = false,
                    ["mission_ready"] = false,
                    ["source_pins_before"] = before,
                    ["source_pins_after"] = after
                };
                WriteNew(output, result);
                if (passed)
                {
                    Debug.Log(Marker);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[S002BuildingPrefabScalarReadback] result=Failed " + String.Join(" | ", issues));
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S002BuildingPrefabScalarReadback] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static JObject ObserveProvider(string root, JObject input, JObject expected, JObject provider,
            MethodInfo metadataOwner)
        {
            string key = RequiredString(input, "key");
            string path = RequiredString(input, "source_prefab_path");
            if (RequiredString(provider, "key") != key || RequiredString(provider, "path") != path)
                throw new InvalidDataException("T1 input and compiled S002 provider path/key disagree.");
            string expectedGuid = RequiredString(provider, "guid");
            string ownerPath = RequiredString(input, "source_owner_prefab_path");
            long expectedComponentId = RequiredLong(input, "prefab_component_file_id");
            string expectedConfigPathFromT1 = RequiredString(input, "source_config_path");
            string expectedOwnerGuidFromT1 = null;
            string expectedConfigGuidFromT1 = null;
            JObject providerConfig = provider["config"] as JObject;
            if (providerConfig != null)
            {
                JArray ownerBindings = providerConfig["owner_bindings"] as JArray;
                JObject binding = ownerBindings != null && ownerBindings.Count == 1
                    ? ownerBindings[0] as JObject : null;
                if (RequiredString(providerConfig, "config_path") != expectedConfigPathFromT1 || binding == null ||
                    RequiredString(binding, "owner_prefab_path") != ownerPath ||
                    RequiredLong(binding, "owner_component_file_id") != expectedComponentId ||
                    RequiredString(binding, "config_path") != expectedConfigPathFromT1)
                    throw new InvalidDataException("T1 scalar input owner/config differs from the compiled provider binding.");
                expectedOwnerGuidFromT1 = RequiredString(binding, "owner_component_guid");
                expectedConfigGuidFromT1 = RequiredString(binding, "config_guid");
                if (RequiredString(binding, "config_asset_guid") != expectedConfigGuidFromT1)
                    throw new InvalidDataException("T1 source config owner GUIDs disagree.");
            }
            else
            {
                if (RequiredString(provider, "config_path") != expectedConfigPathFromT1 || ownerPath != path)
                    throw new InvalidDataException("T1 special source binding differs from the compiled prefab/config path.");
                expectedOwnerGuidFromT1 = expectedGuid;
                expectedConfigGuidFromT1 = RequiredString(provider, "config_guid");
            }
            var issues = new List<string>();

            GameObject importedRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (importedRoot == null || mainAsset == null || !(mainAsset is GameObject) || mainAsset != importedRoot)
                issues.Add("AssetDatabase did not return the imported prefab main GameObject as its root.");
            string rootGuid = null;
            long rootLocalId = 0;
            bool rootIdFound = importedRoot != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                importedRoot, out rootGuid, out rootLocalId);
            string importedPath = importedRoot == null ? null : AssetDatabase.GetAssetPath(importedRoot);
            string assetGuid = AssetDatabase.AssetPathToGUID(path);
            bool importedAssetDirtyBefore = importedRoot != null && EditorUtility.IsDirty(importedRoot);
            if (importedAssetDirtyBefore)
                issues.Add("Imported prefab main asset is already dirty before the readback.");
            if (importedPath != path || assetGuid != expectedGuid || !rootIdFound || rootGuid != expectedGuid)
                issues.Add("Imported prefab main-root path/GUID differs from the pinned T1 provider asset.");

            GameObject prefabContents = null;
            try
            {
                prefabContents = PrefabUtility.LoadPrefabContents(path);
                if (prefabContents == null) throw new InvalidDataException("LoadPrefabContents returned null.");
                BuildingDefinitionAuthoring[] authorings = prefabContents.GetComponents<BuildingDefinitionAuthoring>();
                if (authorings == null || authorings.Length != 1)
                    issues.Add("Exactly one root BuildingDefinitionAuthoring is required.");
                BuildingDefinitionAuthoring authoring = authorings != null && authorings.Length == 1 ? authorings[0] : null;
                if (authoring == null)
                    return FailedCandidate(key, path, issues, importedPath, expectedGuid, rootGuid, rootLocalId);

                UnityEngine.Object sourceComponent = PrefabUtility.GetCorrespondingObjectFromOriginalSource(authoring);
                if (sourceComponent == null && ownerPath == path && importedRoot != null)
                    sourceComponent = importedRoot.GetComponent<BuildingDefinitionAuthoring>();
                JObject sourceComponentIdentity = DescribeIdentity(sourceComponent);
                string expectedOwnerGuid = AssetDatabase.AssetPathToGUID(ownerPath);
                string componentGuid = null;
                long componentId = 0;
                bool componentIdFound = sourceComponent != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    sourceComponent, out componentGuid, out componentId);
                if (!componentIdFound || AssetDatabase.GetAssetPath(sourceComponent) != ownerPath ||
                    componentGuid != expectedOwnerGuid || componentGuid != expectedOwnerGuidFromT1 ||
                    componentId != expectedComponentId)
                    issues.Add("Original-source component GUID/localID differs from the T1 owner component reference.");

                var serializedAuthoring = new SerializedObject(authoring);
                serializedAuthoring.Update();
                SerializedProperty configProperty = serializedAuthoring.FindProperty("config");
                UnityEngine.Object configObject = configProperty == null ? null : configProperty.objectReferenceValue;
                JObject configIdentity = DescribeIdentity(configObject);
                string expectedConfigPath = RequiredString(input, "source_config_path");
                string expectedConfigGuid = AssetDatabase.AssetPathToGUID(expectedConfigPath);
                if (configObject == null || !(configObject is BuildingDefinitionAuthoringConfig) ||
                    (string)configIdentity?["path"] != expectedConfigPath ||
                    (string)configIdentity?["guid"] != expectedConfigGuid ||
                    expectedConfigGuid != expectedConfigGuidFromT1 ||
                    (bool?)configIdentity?["identity_resolved"] != true)
                    issues.Add("Imported authoring component's effective config identity differs from the exact T1 config.");

                JObject configValues = ReadConfigRawValues(configObject);
                CompareConfigInputValues(input, configValues, issues);
                JObject prefabValues = ReadPrefabValues(authoring);
                CompareFootprintInputValues(input, prefabValues, issues);

                BuildingDefinitionAuthoringScalarSource source = ReadScalarSource(input);
                BuildingDefinitionAuthoringScalarMetadata projected = BuildingDefinitionAuthoringScalarProjection.Project(source);
                CompareProjectedValues(expected, projected, issues);

                bool previewDirtyBeforeOwner = EditorUtility.IsDirty(authoring);
                object[] call = { prefabContents, null };
                bool ownerSucceeded = (bool)metadataOwner.Invoke(null, call);
                bool previewDirtyAfterOwner = EditorUtility.IsDirty(authoring);
                bool importedAssetDirtyAfterOwner = importedRoot != null && EditorUtility.IsDirty(importedRoot);
                if (importedAssetDirtyAfterOwner != importedAssetDirtyBefore)
                    issues.Add("Original owner changed dirty state of the imported main prefab asset.");
                if (!ownerSucceeded || call[1] == null)
                    issues.Add("Original metadata owner returned false/null on isolated imported prefab contents.");
                JObject actualScalars = null;
                if (ownerSucceeded && call[1] != null)
                {
                    object metadata = call[1];
                    Vector2Int footprint = (Vector2Int)ReadPublicField(metadata, "FootprintCells");
                    actualScalars = new JObject
                    {
                        ["max_health"] = (int)ReadPublicField(metadata, "MaxHealth"),
                        ["footprint_width"] = footprint.x,
                        ["footprint_depth"] = footprint.y,
                        ["price"] = (int)ReadPublicField(metadata, "Price"),
                        ["materials_cost"] = (int)ReadPublicField(metadata, "MaterialsCost"),
                        ["production_duration_seconds"] = Float((float)ReadPublicField(metadata, "ProductionDurationSeconds"))
                    };
                    CompareActualValues(expected, actualScalars, issues);
                }
                return new JObject
                {
                    ["key"] = key,
                    ["status"] = issues.Count == 0 ? "Passed" : "Failed",
                    ["asset_path"] = importedPath,
                    ["expected_asset_guid"] = expectedGuid,
                    ["actual_prefab_main_root_identity"] = DescribeIdentity(importedRoot),
                    ["actual_prefab_main_root_local_id_is_observed_not_inferred"] = rootLocalId.ToString(CultureInfo.InvariantCulture),
                    ["t1_provider_local_id"] = provider["file_id"],
                    ["t1_provider_local_id_semantics"] = provider["local_id_validation"],
                    ["source_owner_prefab_path"] = ownerPath,
                    ["expected_source_component_local_id"] = expectedComponentId.ToString(CultureInfo.InvariantCulture),
                    ["resolved_original_source_component_identity"] = sourceComponentIdentity,
                    ["effective_config_identity"] = configIdentity,
                    ["config_raw_values_observed"] = configValues,
                    ["prefab_scalar_values_observed_before_owner"] = prefabValues,
                    ["t1_scalar_policy_expected"] = expected,
                    ["shared_scalar_policy_recomputed"] = ScalarJson(projected),
                    ["original_metadata_owner"] = OwnerTypeName + "." + OwnerMethodName,
                    ["original_metadata_owner_scalar_values"] = actualScalars,
                    ["imported_asset_dirty_before"] = importedAssetDirtyBefore,
                    ["imported_asset_dirty_after_owner"] = importedAssetDirtyAfterOwner,
                    ["isolated_prefab_contents_dirty_before_owner"] = previewDirtyBeforeOwner,
                    ["isolated_prefab_contents_dirty_after_owner"] = previewDirtyAfterOwner,
                    ["prefab_contents_unloaded_without_save"] = true,
                    ["issues"] = new JArray(issues)
                };
            }
            finally
            {
                if (prefabContents != null) PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static Dictionary<string, JObject> ResolveProviderFacts(JObject projection)
        {
            var facts = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JObject row in (projection["structure_rows"] as JArray).OfType<JObject>())
            {
                string key = RequiredString(row, "visual_key");
                JObject provider;
                if (row["provider_prefab"] is JObject direct) provider = direct;
                else
                {
                    JObject special = row["special_source_binding"] as JObject ??
                        throw new InvalidDataException("T1 structure has neither provider nor special source binding.");
                    provider = new JObject
                    {
                        ["key"] = key,
                        ["path"] = special["prefab_path"],
                        ["guid"] = special["prefab_guid"],
                        ["file_id"] = special["root_local_id"],
                        ["config_path"] = special["authoring_config_path"],
                        ["config_guid"] = special["authoring_config_guid"],
                        ["local_id_validation"] = special["local_id_validation"] ?? "special_source_binding_unqualified"
                    };
                }
                if (RequiredString(provider, "key") != key)
                    throw new InvalidDataException("Compiled provider key differs from the T1 visual key: " + key);
                if (facts.TryGetValue(key, out JObject previous))
                {
                    if (!String.Equals((string)previous["key"], (string)provider["key"], StringComparison.Ordinal) ||
                        !String.Equals((string)previous["path"], (string)provider["path"], StringComparison.Ordinal) ||
                        !String.Equals((string)previous["guid"], (string)provider["guid"], StringComparison.Ordinal) ||
                        !JToken.DeepEquals(previous["file_id"], provider["file_id"]) ||
                        !JToken.DeepEquals(previous["local_id_validation"], provider["local_id_validation"]) ||
                        !JToken.DeepEquals(previous["config"], provider["config"]) ||
                        !JToken.DeepEquals(previous["config_path"], provider["config_path"]) ||
                        !JToken.DeepEquals(previous["config_guid"], provider["config_guid"]))
                        throw new InvalidDataException("Duplicate S002 structure rows disagree on provider source: " + key);
                }
                else facts.Add(key, (JObject)provider.DeepClone());
            }
            if (facts.Count != 8) throw new InvalidDataException("Exactly eight unique T1 S002 structure providers required.");
            return facts;
        }

        private static void ValidateT1(JObject receipt, JObject projection, JObject inputs, JObject manifest)
        {
            if ((string)receipt["kind"] != "S002CurrentSetupProviderProjectionReceipt" ||
                (string)receipt["status"] != "JoinedWithUnqualifiedPrefabLocalIds" ||
                (string)receipt["setup"]?["catalog_id"] != "S002" || (int?)receipt["setup"]?["seed"] != 104731 ||
                (string)projection["kind"] != "S002CurrentSetupProviderIdentityProjection" ||
                (string)projection["status"] != "JoinedWithUnqualifiedPrefabLocalIds" ||
                (string)inputs["kind"] != "S002BuildingDefinitionScalarMetadataInputs")
                throw new InvalidDataException("Pinned T1 S002 projection/input receipts required.");
            if (projection["world_allocated"]?.Value<bool>() != false ||
                projection["metadata_factory_execution"]?.Value<bool>() != false ||
                projection["source_fit_verified"]?.Value<bool>() != false ||
                projection["mission_ready"]?.Value<bool>() != false ||
                inputs["world_allocated"]?.Value<bool>() != false ||
                inputs["factory_execution"]?.Value<bool>() != false ||
                inputs["mission_ready"]?.Value<bool>() != false)
                throw new InvalidDataException("T1 upstream scope flags must remain false.");
            if ((string)projection["building_definition_scalar_metadata"]?["kind"] != "S002BuildingDefinitionScalarMetadataSourceProjection" ||
                (string)projection["building_definition_scalar_metadata"]?["status"] != "PassedSourceOnlyScalarPolicy" ||
                projection["building_definition_scalar_metadata"]?["policy_controls_passed"]?.Value<bool>() != true ||
                projection["building_definition_scalar_metadata"]?["unity_effective_prefab_state_verified"]?.Value<bool>() != false)
                throw new InvalidDataException("Pinned source-only scalar policy result required.");
            JArray rows = inputs["rows"] as JArray;
            JArray expected = projection["building_definition_scalar_metadata"]?["rows"] as JArray;
            if (rows == null || expected == null || rows.Count != 8 || expected.Count != 8 ||
                RequiredString(manifest, "t1_projection_sha256") != (string)receipt["projection"]?["sha256"] ||
                RequiredString(manifest, "t1_inputs_sha256") != (string)receipt["scalar_source_inputs"]?["sha256"])
                throw new InvalidDataException("Exactly eight pinned scalar inputs and output rows required.");
            var inputKeys = rows.OfType<JObject>().Select(row => RequiredString(row, "key")).ToArray();
            var expectedKeys = expected.OfType<JObject>().Select(row => RequiredString(row, "key")).ToArray();
            if (inputKeys.Distinct(StringComparer.Ordinal).Count() != 8 ||
                expectedKeys.Distinct(StringComparer.Ordinal).Count() != 8 ||
                !new HashSet<string>(inputKeys, StringComparer.Ordinal).SetEquals(expectedKeys))
                throw new InvalidDataException("T1 input and projected scalar rows must have the exact same eight unique keys.");
        }

        private static void ValidateProjectionPins(string root, JObject projection, JObject manifestPins)
        {
            JObject projectionPins = projection["source_pins"] as JObject ??
                throw new InvalidDataException("Complete T1 projection source pins required.");
            foreach (var property in projectionPins.Properties())
            {
                string absolute = Path.GetFullPath(property.Name);
                string rootPrefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!absolute.StartsWith(rootPrefix, StringComparison.Ordinal))
                    throw new InvalidDataException("T1 source pin path escapes project root.");
                string relative = absolute.Substring(rootPrefix.Length).Replace('\\', '/');
                if ((string)manifestPins[relative] != (string)property.Value)
                    throw new InvalidDataException("Readback pin manifest omits or changes a T1 projection source pin: " + relative);
            }
        }

        private static MethodInfo ResolveMetadataOwner()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(item => item.GetName().Name == "Game.Composition");
            if (assembly == null) throw new InvalidOperationException("Original Game.Composition assembly is not loaded.");
            Type owner = assembly.GetType(OwnerTypeName, throwOnError: true);
            MethodInfo method = owner.GetMethod(OwnerMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Type outputType = typeof(BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata);
            Type[] expected = { typeof(GameObject), outputType.MakeByRefType() };
            if (method == null || method.ReturnType != typeof(bool) ||
                !method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(expected))
                throw new MissingMethodException(OwnerTypeName, OwnerMethodName);
            return method;
        }

        private static BuildingDefinitionAuthoringScalarSource ReadScalarSource(JObject row) =>
            new BuildingDefinitionAuthoringScalarSource(
                RequiredBool(row, "has_config"), RequiredInt(row, "prefab_max_health"),
                RequiredInt(row, "prefab_footprint_width"), RequiredInt(row, "prefab_footprint_depth"),
                RequiredInt(row, "prefab_price"), RequiredInt(row, "prefab_materials_cost"),
                RequiredFloat(row, "prefab_production_duration_seconds"), RequiredInt(row, "config_max_health"),
                RequiredInt(row, "config_raw_price"), RequiredInt(row, "config_raw_materials_cost"),
                RequiredFloat(row, "config_raw_production_duration_seconds"));

        private static JObject ReadConfigRawValues(UnityEngine.Object config)
        {
            if (config == null) return null;
            var serialized = new SerializedObject(config);
            serialized.Update();
            SerializedProperty maxHealth = serialized.FindProperty("maxHealth");
            SerializedProperty price = serialized.FindProperty("price");
            SerializedProperty materialsCost = serialized.FindProperty("materialsCost");
            SerializedProperty duration = serialized.FindProperty("productionDurationSeconds");
            return new JObject
            {
                ["max_health"] = IntToken(maxHealth),
                ["raw_price"] = IntToken(price),
                ["raw_materials_cost"] = IntToken(materialsCost),
                ["raw_production_duration_seconds"] = Float(duration == null ? float.NaN : duration.floatValue)
            };
        }

        private static JObject ReadPrefabValues(BuildingDefinitionAuthoring authoring)
        {
            var serialized = new SerializedObject(authoring);
            serialized.Update();
            SerializedProperty footprint = serialized.FindProperty("footprintCells");
            SerializedProperty maxHealth = serialized.FindProperty("maxHealth");
            SerializedProperty price = serialized.FindProperty("price");
            SerializedProperty materialsCost = serialized.FindProperty("materialsCost");
            SerializedProperty duration = serialized.FindProperty("productionDurationSeconds");
            return new JObject
            {
                ["max_health"] = IntToken(maxHealth),
                ["footprint_width"] = footprint == null ? JValue.CreateNull() : new JValue(footprint.vector2IntValue.x),
                ["footprint_depth"] = footprint == null ? JValue.CreateNull() : new JValue(footprint.vector2IntValue.y),
                ["price"] = IntToken(price),
                ["materials_cost"] = IntToken(materialsCost),
                ["production_duration_seconds"] = Float(duration == null ? float.NaN : duration.floatValue)
            };
        }

        private static JToken IntToken(SerializedProperty property) =>
            property == null ? JValue.CreateNull() : new JValue(property.intValue);

        private static void CompareConfigInputValues(JObject input, JObject actual, List<string> issues)
        {
            if (actual == null || (int?)actual["max_health"] != (int?)input["config_max_health"] ||
                (int?)actual["raw_price"] != (int?)input["config_raw_price"] ||
                (int?)actual["raw_materials_cost"] != (int?)input["config_raw_materials_cost"] ||
                !SameFloat((float?)actual["raw_production_duration_seconds"], (float?)input["config_raw_production_duration_seconds"]))
                issues.Add("Imported config raw scalar values differ from the exact T1 source values.");
        }

        private static void CompareFootprintInputValues(JObject input, JObject actual, List<string> issues)
        {
            if ((int?)actual["footprint_width"] != (int?)input["prefab_footprint_width"] ||
                (int?)actual["footprint_depth"] != (int?)input["prefab_footprint_depth"])
                issues.Add("Imported authoring footprint differs from the exact T1 source prefab values.");
        }

        private static void CompareProjectedValues(JObject expected, BuildingDefinitionAuthoringScalarMetadata actual,
            List<string> issues)
        {
            JObject projected = ScalarJson(actual);
            if ((int?)expected["max_health"] != (int?)projected["max_health"] ||
                (int?)expected["footprint_width"] != (int?)projected["footprint_width"] ||
                (int?)expected["footprint_depth"] != (int?)projected["footprint_depth"] ||
                (int?)expected["price"] != (int?)projected["price"] ||
                (int?)expected["materials_cost"] != (int?)projected["materials_cost"] ||
                !SameFloat((float?)expected["production_duration_seconds"], (float?)projected["production_duration_seconds"]))
                issues.Add("Current shared scalar policy differs from the pinned T1 scalar projection result.");
        }

        private static void CompareActualValues(JObject expected, JObject actual, List<string> issues)
        {
            if ((int?)expected["max_health"] != (int?)actual["max_health"] ||
                (int?)expected["footprint_width"] != (int?)actual["footprint_width"] ||
                (int?)expected["footprint_depth"] != (int?)actual["footprint_depth"] ||
                (int?)expected["price"] != (int?)actual["price"] ||
                (int?)expected["materials_cost"] != (int?)actual["materials_cost"] ||
                !SameFloat((float?)expected["production_duration_seconds"], (float?)actual["production_duration_seconds"]))
                issues.Add("Original imported-prefab metadata owner differs from pinned T1 scalar projection.");
        }

        private static JObject ScalarJson(BuildingDefinitionAuthoringScalarMetadata value) => new JObject
        {
            ["max_health"] = value.MaxHealth, ["footprint_width"] = value.FootprintWidth,
            ["footprint_depth"] = value.FootprintDepth, ["price"] = value.Price,
            ["materials_cost"] = value.MaterialsCost, ["production_duration_seconds"] = Float(value.ProductionDurationSeconds)
        };

        private static bool SameFloat(float? left, float? right)
        {
            if (!left.HasValue || !right.HasValue) return left.HasValue == right.HasValue;
            if (float.IsNaN(left.Value) || float.IsNaN(right.Value)) return float.IsNaN(left.Value) && float.IsNaN(right.Value);
            return left.Value.Equals(right.Value);
        }

        private static JToken Float(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? new JValue(value.ToString("R", CultureInfo.InvariantCulture)) : new JValue(value);

        private static JObject DescribeIdentity(UnityEngine.Object value)
        {
            if (value == null) return null;
            bool resolved = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId);
            return new JObject { ["path"] = AssetDatabase.GetAssetPath(value), ["guid"] = resolved ? guid : null,
                ["local_id"] = resolved ? localId.ToString(CultureInfo.InvariantCulture) : null,
                ["type"] = value.GetType().FullName, ["identity_resolved"] = resolved };
        }

        private static object ReadPublicField(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            if (field == null) throw new MissingFieldException(value.GetType().FullName, name);
            return field.GetValue(value);
        }

        private static JObject FailedCandidate(string key, string path, List<string> issues,
            string actualPath, string expectedGuid, string actualGuid, long actualRootId) => new JObject
        {
            ["key"] = key, ["status"] = "Failed", ["asset_path"] = actualPath,
            ["expected_asset_path"] = path, ["expected_asset_guid"] = expectedGuid,
            ["actual_asset_guid"] = actualGuid,
            ["actual_prefab_main_root_local_id_observed"] = actualRootId > 0 ? actualRootId.ToString(CultureInfo.InvariantCulture) : null,
            ["issues"] = new JArray(issues)
        };

        private static JObject HashPins(string root, JObject expected)
        {
            var result = new JObject();
            foreach (var property in expected.Properties())
            {
                string relative = property.Name.Replace('\\', '/');
                string path = ResolveRelative(root, relative);
                string actual = HashFile(path);
                if (actual != (string)property.Value) throw new InvalidDataException("Pinned T1/source input changed: " + relative);
                result[relative] = actual;
            }
            return result;
        }

        private static string ResolveRelative(string root, string relative)
        {
            if (Path.IsPathRooted(relative) || relative.Split('/').Any(part => part == ".."))
                throw new InvalidDataException("Relative T1 input/pin path required.");
            string path = Path.GetFullPath(Path.Combine(root, relative));
            RequireUnder(root, path, "Pinned path");
            return path;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static string RequiredString(JObject row, string name)
        {
            string value = (string)row[name];
            if (String.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Required string missing: " + name);
            return value;
        }

        private static bool RequiredBool(JObject row, string name) =>
            (bool?)row[name] ?? throw new InvalidDataException("Required bool missing: " + name);

        private static int RequiredInt(JObject row, string name) =>
            (int?)row[name] ?? throw new InvalidDataException("Required integer missing: " + name);

        private static long RequiredLong(JObject row, string name) =>
            (long?)row[name] ?? throw new InvalidDataException("Required long missing: " + name);

        private static float RequiredFloat(JObject row, string name) =>
            (float?)row[name] ?? throw new InvalidDataException("Required float missing: " + name);

        private static void RequireUnder(string root, string path, string label)
        {
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException(label + " must remain inside the project.");
        }

        private static string Arg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length) throw new InvalidDataException("Missing argument " + name);
            return args[index + 1];
        }

        private static void WriteNew(string path, JObject value)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream)) writer.Write(value.ToString(Formatting.Indented) + "\n");
        }
    }
}
#endif
