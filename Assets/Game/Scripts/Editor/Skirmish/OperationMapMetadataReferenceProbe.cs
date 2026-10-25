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
}
#endif
