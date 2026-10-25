#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // One bounded source-asset read. No World, scene, mission, object, or player is created.
    public static class OperationMapMetadataReferenceProbe
    {
        const string AssetPath = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        const string AssetSha = "68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8";
        const string OutputEnvironment = "WARLINE_OPMAP_METADATA_REFERENCE_OUTPUT";
        static string Sha(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static string FileSha(string path) { using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        static JObject AssemblyIdentity(Assembly assembly) => new JObject { ["path"] = assembly.Location, ["sha256"] = FileSha(assembly.Location), ["mvid"] = assembly.ManifestModule.ModuleVersionId.ToString(), ["full_name"] = assembly.FullName };
        static JArray F(params float[] values) { var a = new JArray(); foreach (float value in values) a.Add(BitConverter.ToInt32(BitConverter.GetBytes(value), 0)); return a; }
        static JArray V(float3 v) => F(v.x, v.y, v.z);
        static JArray Q(quaternion q) => F(q.value.x, q.value.y, q.value.z, q.value.w);
        static JObject Method(MethodInfo method) { if (method == null || method.GetMethodBody() == null) throw new InvalidOperationException("Expected exact managed conversion method"); return new JObject { ["declaring_type"] = method.DeclaringType.FullName, ["name"] = method.Name, ["metadata_token"] = method.MetadataToken, ["il_sha256"] = Sha(method.GetMethodBody().GetILAsByteArray()) }; }

        // Separate receipt: actual imported source values through the shared V2 writers.
        // Keeps the existing 9-rotation metadata reference format unchanged.
        public static void RunBindingSourceFingerprints()
        {
            int exit = 1;
            try
            {
                CaptureBindingSourceFingerprints("WARLINE_OPMAP_BINDING_SOURCE_REFERENCE_OUTPUT", "binding-source-reference.json", AssetPath, AssetSha, "cbfc2f62e3142413485019bd83197539");
                CaptureBindingSourceFingerprints("WARLINE_OPMAP_LEGACY_BINDING_SOURCE_REFERENCE_OUTPUT", "legacy-binding-source-reference.json", "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset", "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0", "55e36a8d7e7a84e2b9893cee052b0e61");
                Debug.Log("[OperationMapBindingSourceReference] result=Passed assets=2 valueFormat=2");
                exit = 0;
            }
            catch (Exception exception) { Debug.LogException(exception); }
            EditorApplication.Exit(exit);
        }

        // Private fixed-two-source helper; no public asset selection/export surface.
        static void CaptureBindingSourceFingerprints(string outputEnvironment, string outputName, string sourcePath, string sourceSha, string expectedGuid)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Environment.GetEnvironmentVariable(outputEnvironment);
            if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output)) throw new InvalidOperationException("Explicit absolute binding reference output required");
            output = Path.GetFullPath(output);
            if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || Path.GetFileName(output) != outputName) throw new InvalidOperationException("Owned private/tmp binding-source-reference.json required");
            if (FileSha(Path.Combine(root, sourcePath)) != sourceSha) throw new InvalidOperationException("Approved source asset changed");
            var definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            if (definition == null || AssetDatabase.GetAssetPath(definition) != sourcePath || EditorUtility.IsDirty(definition)) throw new InvalidOperationException("Clean actual approved source required");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(definition, out string guid, out long localId) || guid != expectedGuid || localId != 11400000) throw new InvalidOperationException("Actual source identity changed");
            string[] relativePins = { sourcePath, sourcePath + ".meta", "Assets/Game/Scripts/Configs/OperationMapConfigModels.cs", "Assets/Game/Scripts/Configs/OperationMapDefinition.cs", "Assets/Game/Scripts/Configs/Skirmish/SkirmishBindingCanonicalValues.cs", "Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs", "Assets/Game/Scripts/Configs/Skirmish/SkirmishMissionLogicalBindingConfig.cs", "Assets/Game/Scripts/Simulation/SkirmishBindingCanonicalBytes.cs", "Assets/Game/Scripts/Configs/Game.Configs.asmdef", "Assets/Game/Scripts/Editor/Skirmish/OperationMapMetadataReferenceProbe.cs", "Assets/Game/Scripts/Editor/Skirmish/OperationMapMetadataReferenceProbe.cs.meta" };
            var pins = new JObject(); foreach (string path in relativePins) pins[path] = FileSha(Path.Combine(root, path));
            if (!SkirmishBindingCanonicalValues.HasSupportedSchema()) throw new InvalidOperationException("Exact shared canonical value schema required");
            string missionSource = SkirmishBindingCanonicalValues.SourceFingerprint(definition);
            string fullSource = SkirmishBindingCanonicalValues.FullSourceFingerprint(definition);
            var producers = new JObject { ["Game.Configs"] = AssemblyIdentity(typeof(SkirmishBindingCanonicalValues).Assembly), ["probe_owner"] = AssemblyIdentity(typeof(OperationMapMetadataReferenceProbe).Assembly) };
            Assembly simulation = null; foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) if (assembly.GetName().Name == "Game.Simulation") { if (simulation != null) throw new InvalidOperationException("Ambiguous loaded Simulation assembly"); simulation = assembly; }
            if (simulation == null) throw new InvalidOperationException("Actual shared byte writer assembly not loaded"); producers["Game.Simulation"] = AssemblyIdentity(simulation);
            var result = new JObject { ["schema_version"] = 1, ["kind"] = "NativeOperationMapBindingSourceFingerprints", ["value_format"] = 2, ["source_asset"] = new JObject { ["path"] = sourcePath, ["sha256"] = sourceSha, ["guid"] = guid, ["local_id"] = localId }, ["source_pins"] = pins, ["schema_supported"] = true, ["mission_source_v2"] = missionSource, ["full_layout_source_v2"] = fullSource, ["source_input_transcripts"] = OperationMapBindingValueTranscript.Capture(definition), ["writer_methods"] = new JObject { ["mission_source"] = Method(typeof(SkirmishBindingCanonicalValues).GetMethod("SourceFingerprint", new[] { typeof(OperationMapDefinition) })), ["full_layout_source"] = Method(typeof(SkirmishBindingCanonicalValues).GetMethod("FullSourceFingerprint", new[] { typeof(OperationMapDefinition) })) }, ["producer_assemblies"] = producers, ["unity_version"] = Application.unityVersion, ["little_endian"] = BitConverter.IsLittleEndian, ["world_created"] = false, ["mission_run"] = false, ["scope"] = "Actual clean imported source values through SAME schema-checked canonical C# byte writers. Source identity only, no source-fit/native-root/renderer/playability claim." };
            foreach (string path in relativePins) if ((string)pins[path] != FileSha(Path.Combine(root, path))) throw new InvalidOperationException("Source changed during binding fingerprint probe");
            foreach (var producer in producers.Properties()) if ((string)producer.Value["sha256"] != FileSha((string)producer.Value["path"])) throw new InvalidOperationException("Loaded producer changed");
            if (EditorUtility.IsDirty(definition)) throw new InvalidOperationException("Probe dirtied source asset");
            byte[] bytes = Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n"); if (bytes.Length > 65536) throw new InvalidOperationException("Bounded binding reference exceeded64KiB");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            Debug.Log("[OperationMapBindingSourceReceipt] result=Passed valueFormat=2 missionSource=" + missionSource + " fullSource=" + fullSource + " output=" + output + " sha256=" + Sha(bytes));
        }

        public static void Run()
        {
            int exit = 1;
            BlobAssetReference<OperationMapBlob> blob = default;
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string output = Environment.GetEnvironmentVariable(OutputEnvironment);
                if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output)) throw new InvalidOperationException("Explicit absolute output required");
                output = Path.GetFullPath(output);
                if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || Path.GetFileName(output) != "metadata-reference.json") throw new InvalidOperationException("Owned private/tmp metadata-reference.json required");
                string asset = Path.Combine(root, AssetPath);
                if (FileSha(asset) != AssetSha) throw new InvalidOperationException("Actual approved asset preimage differs");
                string[] relativePins = { AssetPath, AssetPath + ".meta", "Assets/Game/Scripts/Configs/OperationMapDefinition.cs", "Assets/Game/Scripts/Components/OperationMapComponents.cs", "Assets/Game/Scripts/Editor/Skirmish/OperationMapMetadataReferenceProbe.cs", "Assets/Game/Scripts/Editor/Skirmish/OperationMapMetadataReferenceProbe.cs.meta" };
                var pins = new JObject();
                foreach (string relative in relativePins) pins[relative] = FileSha(Path.Combine(root, relative));
                var producerAssemblies = new JObject {
                    ["UnityEngine.CoreModule"] = AssemblyIdentity(typeof(Quaternion).Assembly),
                    ["Unity.Mathematics"] = AssemblyIdentity(typeof(quaternion).Assembly),
                    ["Unity.Entities"] = AssemblyIdentity(typeof(BlobAssetReference<>).Assembly),
                    ["Game.Configs"] = AssemblyIdentity(typeof(OperationMapDefinition).Assembly),
                    ["probe_owner"] = AssemblyIdentity(typeof(OperationMapMetadataReferenceProbe).Assembly)
                };
                string editorExecutable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                if (string.IsNullOrEmpty(editorExecutable) || !File.Exists(editorExecutable)) throw new InvalidOperationException("Current Editor executable identity unavailable");
                var editorIdentity = new JObject { ["path"] = editorExecutable, ["sha256"] = FileSha(editorExecutable), ["unity_version"] = Application.unityVersion };
                OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(AssetPath);
                if (definition == null || AssetDatabase.GetAssetPath(definition) != AssetPath || EditorUtility.IsDirty(definition)) throw new InvalidOperationException("Clean exact source definition required");
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(definition, out string guid, out long localId)) throw new InvalidOperationException("Asset identity unavailable");
                if (guid != "cbfc2f62e3142413485019bd83197539" || localId != 11400000) throw new InvalidOperationException("Approved asset GUID/local identity differs");
                if (definition.Anchors.Length != 8 || definition.Cameras.Length != 1) throw new InvalidOperationException("Expected bounded approved metadata domain8+1");
                if (!definition.TryCreatePersistentMetadataBlob(out blob, out string error) || !blob.IsCreated) throw new InvalidOperationException("Original metadata factory failed: " + error);
                ref OperationMapBlob value = ref blob.Value;
                if (value.Anchors.Length != definition.Anchors.Length || value.Cameras.Length != definition.Cameras.Length) throw new InvalidOperationException("Original metadata output cardinality mismatch");
                var rotations = new JArray();
                var anchors = new JArray();
                for (int i = 0; i < value.Anchors.Length; i++)
                {
                    var source = definition.Anchors[i]; var row = value.Anchors[i];
                    if (!row.Id.Equals(new Unity.Collections.FixedString64Bytes(source.AnchorId))) throw new InvalidOperationException("Anchor source order/identity differs");
                    rotations.Add(new JObject { ["domain"] = "anchor", ["index"] = i, ["id"] = row.Id.ToString(), ["euler_bits"] = F(source.EulerAngles.x, source.EulerAngles.y, source.EulerAngles.z), ["quaternion_bits"] = Q(row.Rotation) });
                    anchors.Add(new JObject { ["id"] = row.Id.ToString(), ["kind"] = (int)row.Kind, ["position_bits"] = V(row.Position), ["rotation_bits"] = Q(row.Rotation), ["radius_bits"] = F(row.Radius), ["faction_id"] = row.FactionId, ["lane_index"] = row.LaneIndex });
                }
                var cameras = new JArray();
                for (int i = 0; i < value.Cameras.Length; i++)
                {
                    var source = definition.Cameras[i]; var row = value.Cameras[i];
                    if (!row.Id.Equals(new Unity.Collections.FixedString64Bytes(source.CameraId))) throw new InvalidOperationException("Camera source order/identity differs");
                    rotations.Add(new JObject { ["domain"] = "camera", ["index"] = i, ["id"] = row.Id.ToString(), ["euler_bits"] = F(source.EulerAngles.x, source.EulerAngles.y, source.EulerAngles.z), ["quaternion_bits"] = Q(row.Rotation) });
                    cameras.Add(new JObject { ["id"] = row.Id.ToString(), ["position_bits"] = V(row.Position), ["rotation_bits"] = Q(row.Rotation), ["fov_bits"] = F(row.FieldOfView), ["orthographic_size_bits"] = F(row.OrthographicSize), ["is_orthographic"] = row.IsOrthographic, ["clamp_to_camera_bounds"] = row.ClampToCameraBounds });
                }
                var header = new JObject {
                    ["operation_map_id"] = value.OperationMapId.ToString(), ["source_operation_map_id"] = value.SourceOperationMapId.ToString(), ["planning_camera_id"] = value.PlanningCameraId.ToString(), ["battle_camera_id"] = value.BattleCameraId.ToString(),
                    ["source_identity_hash"] = value.SourceIdentityHash.ToString(), ["source_content_hash"] = value.SourceContentHash.ToString(), ["content_hash"] = value.ContentHash.ToString(), ["generated_metadata_hash"] = value.GeneratedMetadataHash.ToString(), ["schema_version"] = value.SchemaVersion, ["content_version"] = value.ContentVersion,
                    ["grid"] = new JObject { ["asset_guid"] = value.Grid.AssetGuid.ToString(), ["content_hash"] = value.Grid.ContentHash.ToString(), ["origin_bits"] = V(value.Grid.Origin), ["dimensions"] = new JArray(value.Grid.Dimensions.x, value.Grid.Dimensions.y), ["cell_size_bits"] = F(value.Grid.CellSize), ["authored_blocked_cells"] = value.Grid.AuthoredBlockedCellCount },
                    ["surface"] = new JObject { ["asset_guid"] = value.Surface.AssetGuid.ToString(), ["content_hash"] = value.Surface.ContentHash.ToString(), ["runtime_blob_hash"] = value.Surface.RuntimeBlobHash.ToString(), ["surface_count"] = value.Surface.SurfaceCount, ["payload_version"] = value.Surface.PayloadVersion, ["payload_encoding"] = value.Surface.PayloadEncoding, ["minimum_height_bits"] = F(value.Surface.MinimumHeight), ["maximum_height_bits"] = F(value.Surface.MaximumHeight) },
                    ["navigation"] = new JObject { ["authored_subscene_guid"] = value.Navigation.AuthoredSubSceneGuid.ToString(), ["grid_authoring_local_id"] = value.Navigation.GridAuthoringLocalId, ["static_blockers"] = value.Navigation.StaticGridBlockerCount, ["uses_surface_movement_metadata"] = value.Navigation.UsesSurfaceMovementMetadata, ["supports_dynamic_blockers"] = value.Navigation.SupportsDynamicBlockers, ["supports_dynamic_occupancy"] = value.Navigation.SupportsDynamicOccupancy },
                    ["minimap"] = new JObject { ["id"] = value.Minimap.Id.ToString(), ["origin_bits"] = V(value.Minimap.ProjectionOrigin), ["size_bits"] = F(value.Minimap.ProjectionSize.x, value.Minimap.ProjectionSize.y), ["orientation_bits"] = F(value.Minimap.OrientationDegrees) }
                };
                Assembly owner = typeof(OperationMapDefinition).Assembly;
                var result = new JObject { ["schema_version"] = 1, ["kind"] = "NativeOriginalOperationMapMetadataAssetReference", ["scope"] = "SourceAssetDataOnly_NotMissionStartup_NotNativeRootAbsence", ["source_asset"] = new JObject { ["path"] = AssetPath, ["sha256"] = AssetSha, ["guid"] = guid, ["local_id"] = localId }, ["source_pins"] = pins, ["unity_version"] = Application.unityVersion, ["editor_executable"] = editorIdentity, ["producer_assemblies"] = producerAssemblies, ["little_endian"] = BitConverter.IsLittleEndian, ["owner_assembly"] = new JObject { ["path"] = owner.Location, ["sha256"] = FileSha(owner.Location), ["mvid"] = owner.ManifestModule.ModuleVersionId.ToString() }, ["rotation_engine_assembly"] = new JObject { ["path"] = typeof(Quaternion).Assembly.Location, ["sha256"] = FileSha(typeof(Quaternion).Assembly.Location), ["mvid"] = typeof(Quaternion).Assembly.ManifestModule.ModuleVersionId.ToString() }, ["factory"] = Method(typeof(OperationMapDefinition).GetMethod("TryCreatePersistentMetadataBlob", BindingFlags.Public | BindingFlags.Instance)), ["probe_entry"] = Method(typeof(OperationMapMetadataReferenceProbe).GetMethod("Run", BindingFlags.Public | BindingFlags.Static)), ["rotation_provider"] = Method(typeof(OperationMapDefinition).GetMethod("ToQuaternion", BindingFlags.NonPublic | BindingFlags.Static)), ["header"] = header, ["anchors"] = anchors, ["cameras"] = cameras, ["rotations"] = rotations, ["world_created"] = false, ["mission_run"] = false, ["native_root_state_captured"] = false };
                foreach (string relative in relativePins) if ((string)pins[relative] != FileSha(Path.Combine(root, relative))) throw new InvalidOperationException("Source changed during asset-data probe: " + relative);
                foreach (var producer in producerAssemblies.Properties()) {
                    var identity = (JObject)producer.Value;
                    if ((string)identity["sha256"] != FileSha((string)identity["path"])) throw new InvalidOperationException("Loaded producer assembly changed: " + producer.Name);
                }
                if ((string)editorIdentity["sha256"] != FileSha(editorExecutable)) throw new InvalidOperationException("Editor executable changed during probe");
                if (EditorUtility.IsDirty(definition)) throw new InvalidOperationException("Probe changed source definition");
                byte[] bytes = Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
                if (bytes.Length > 1048576) throw new InvalidOperationException("Bounded reference output exceeded1MiB");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                Debug.Log("[OperationMapMetadataReference] result=Passed rotations=9 anchors=8 cameras=1 output=" + output + " sha256=" + Sha(bytes));
                exit = 0;
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { if (blob.IsCreated) blob.Dispose(); }
            EditorApplication.Exit(exit);
        }
    }
    // Field-only diagnostic reader. Finite existing canonical DTO shapes only.
    internal static class OperationMapBindingValueTranscript
    {
        static int nodes;
        static readonly System.Collections.Generic.Dictionary<string,string[]> Shapes = new System.Collections.Generic.Dictionary<string,string[]> {
            ["Game.Configs.SkirmishMeasuredLayoutBinder+SourceValues"] = "OperationMapId SourceIdentityHash ContentHash GeneratedMetadataHash PlanningCameraId BattleCameraId SchemaVersion ContentVersion PresentationKind RenderResidencyMode SourceBinding Bounds Grid Surface Navigation Cameras Anchors Minimap Scene SurfaceData MinimapRaster StaticPresentation Buildings Vehicles HeavyMetadata AdditionalBuildings".Split(' '),
            ["Game.Configs.SkirmishMissionLogicalBindingCompiler+Source"] = "Id Identity Content Metadata Scene SchemaVersion ContentVersion Bounds Grid Surface Navigation".Split(' '),
            ["Game.Configs.SkirmishMeasuredLayoutBinder+StableReference"] = "Guid SubObjectName".Split(' '),
            ["Game.Configs.SkirmishMeasuredLayoutBinder+PlacementValues"] = "SpawnOnMatchStart HideAuthoringVisualsAfterSpawn UseExistingStaticPresentationWhenAuthoringVisualMissing Placements".Split(' '),
            ["Game.Configs.SkirmishMeasuredLayoutBinder+PlacementValue"] = "SourcePath Category PrefabPresent Faction Center Position EulerAngles Scale Yaw RotateVertical InstantiateVisualWhenSourceMissing UseGeometricFootprintCenter".Split(' '),
            ["Game.Configs.OperationMapBoundsConfig"] = "worldMin worldMax playableMin playableMax cameraMin cameraMax".Split(' '),
            ["Game.Configs.OperationMapGridMetadataConfig"] = "assetGuid contentHash origin dimensions cellSize authoredBlockedCellCount".Split(' '),
            ["Game.Configs.OperationMapSurfaceMetadataConfig"] = "assetGuid contentHash runtimeBlobHash surfaceCount payloadVersion payloadEncoding minimumHeight maximumHeight".Split(' '),
            ["Game.Configs.OperationMapNavigationMetadataConfig"] = "authoredSubSceneGuid gridAuthoringLocalId staticGridBlockerCount usesSurfaceMovementMetadata supportsDynamicBlockers supportsDynamicOccupancy".Split(' '),
            ["Game.Configs.OperationMapCameraConfig"] = "cameraId position eulerAngles orthographic fieldOfView orthographicSize clampToCameraBounds".Split(' '),
            ["Game.Configs.OperationMapAnchorConfig"] = "anchorId kind position eulerAngles radius factionId laneIndex".Split(' '),
            ["Game.Configs.OperationMapMinimapConfig"] = "minimapId projectionOrigin projectionSize orientationDegrees".Split(' '),
            ["Game.Configs.OperationMapSourceBindingConfig"] = "sourceOperationMapId sourceIdentityHash sourceContentHash".Split(' '),
            ["UnityEngine.Vector3"] = "x y z".Split(' '), ["UnityEngine.Vector2"] = "x y".Split(' '), ["UnityEngine.Vector2Int"] = "m_X m_Y".Split(' ')
        };
        static object Snapshot(Type owner, string methodName, OperationMapDefinition source)
        {
            var method = owner.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(OperationMapDefinition) }, null);
            if (method == null || !Shapes.ContainsKey(method.ReturnType.FullName)) throw new InvalidOperationException("Finite original typed source snapshot missing");
            return method.Invoke(null, new object[] { source });
        }
        internal static JObject Capture(OperationMapDefinition source)
        {
            if (!SkirmishBindingCanonicalValues.HasSupportedSchema()) throw new InvalidOperationException("Exact shared value schema required before transcript"); nodes = 0;
            return new JObject { ["mission_source"] = Read(Snapshot(typeof(SkirmishMissionLogicalBindingCompiler), "SourceValueSnapshot", source), 0), ["full_layout_source"] = Read(Snapshot(typeof(SkirmishMeasuredLayoutBinder), "CanonicalSourceValueSnapshot", source), 0) };
        }
        static JToken Read(object value, int depth)
        {
            if (++nodes > 4096 || depth > 16) throw new InvalidOperationException("Bounded source transcript exceeded");
            if (value == null) return JValue.CreateNull(); Type type = value.GetType();
            if (type == typeof(string)) return new JValue((string)value);
            if (type == typeof(float)) return new JObject { ["f32bits"] = BitConverter.ToInt32(BitConverter.GetBytes((float)value), 0) };
            if (type == typeof(bool)) return new JValue((bool)value);
            if (type == typeof(int) || type == typeof(long) || type == typeof(byte)) return new JValue(Convert.ToInt64(value));
            if (type.IsEnum) return new JObject { ["enum_type"] = type.FullName, ["value"] = Convert.ToInt64(value) };
            if (type.IsArray) { var array = (Array)value; if (array.Rank != 1 || array.Length > 256) throw new InvalidOperationException("Finite source array bound"); var result = new JArray(); for (int i = 0; i < array.Length; i++) result.Add(Read(array.GetValue(i), depth + 1)); return result; }
            if (!Shapes.TryGetValue(type.FullName, out var names)) throw new InvalidOperationException("Unadmitted transcript type: " + type.FullName);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length != names.Length) throw new InvalidOperationException("Finite source field schema changed: " + type.FullName);
            var output = new JObject(); foreach (string name in names) { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (field == null) throw new InvalidOperationException("Missing finite source field " + name); output[name] = Read(field.GetValue(value), depth + 1); } return output;
        }
    }

}
#endif
