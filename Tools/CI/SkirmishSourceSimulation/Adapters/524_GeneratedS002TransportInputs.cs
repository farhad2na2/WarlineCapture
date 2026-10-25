using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using Game.Components;
using Game.Runtime.TransportPose;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed class GeneratedS002TransportInputs
    {
        internal readonly IReadOnlyDictionary<SourceUnitPrefab, SourceTransportTemplateData> Templates;
        readonly GeneratedUnitProductionMetadata metadata;
        readonly Dictionary<SourceUnitPrefab, BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata> transports =
            new Dictionary<SourceUnitPrefab, BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata>();

        internal GeneratedS002TransportInputs(GeneratedUnitProductionMetadata metadata, string repositoryRoot)
        {
            this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            PoseMath.AdmitInstalledMono();
            string config = System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(
                System.IO.Path.Combine(repositoryRoot, "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset"),
                "a522b765a869d99411b4dc3dbd8ada37a543850aa9a4cb2eed8da8ee9502c2c3", 65536));
            // These three source-config properties are absent from the normalized transport
            // export. Verify their exact independently audited authored values; never fill DTO defaults.
            foreach (string line in new[] { "  productionDurationSeconds: 5", "  productionTransportPrefab: {fileID: 0}",
                "  footprintCells: {x: 1, y: 1}", "  autoCalculateFootprint: 0" })
                if (!config.Split('\n').Contains(line)) throw new NotSupportedException("Actual transport getter input changed");
            var result = new Dictionary<SourceUnitPrefab, SourceTransportTemplateData>();
            foreach (var pair in metadata.TransportStaticData)
            {
                if (pair.Key.name != "Unit_Veh_Helicopter_Transport") throw new NotSupportedException("Only actual S002 authored helicopter branch admitted");
                var record = pair.Value;
                var properties = (JObject)record["authoring_properties"];
                bool B(string field) => properties[field] is JObject value && (string)value["codec"] == "bool-v1"
                    ? (bool)value["value"] : throw new ArgumentException("Missing exact source bool " + field);
                int I(string field) => int.Parse((string)properties[field]["value"], CultureInfo.InvariantCulture);
                float F(string field) => Float(properties[field]);
                transports.Add(pair.Key, new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                    5f, null, B("IsAirUnit"), F("ProductionTransportArrivalSeconds"),
                    F("ProductionTransportHoldForNextReadySeconds"), I("ProductionTransportMaxConcurrent"),
                    B("ProductionTransportRequiresAirportRunway"), B("ProductionTransportUsesRunwayLanding"), new Vector2Int(1, 1)));
                var nodes = (JArray)record["nodes"];
                var frames = new PoseFrame[nodes.Count]; var meshes = new List<MeshBounds>();
                var enabled = new List<bool>(); var spins = new List<int>(); int door = -1;
                for (int index = 0; index < nodes.Count; index++)
                {
                    var node = (JObject)nodes[index];
                    if ((int)node["index"] != index || (bool?)node["active_self"] != true)
                        throw new NotSupportedException("Complete source-ordered active static hierarchy required");
                    string name = (string)node["name"];
                    frames[index] = new PoseFrame { Parent = (int)node["parent"], Name = name,
                        Position = Vector(node["local_position"]), Rotation = Rotation(node["local_rotation"]),
                        Scale = Vector(node["local_scale"]), InitialEulerPresent = true,
                        InitialEulerDegrees = Vector(node["initial_local_euler_degrees"]) };
                    if (name.EndsWith("_X", StringComparison.Ordinal) || name.EndsWith("_Y", StringComparison.Ordinal) || name.EndsWith("_Z", StringComparison.Ordinal)) spins.Add(index);
                    if (door < 0 && name == "Door_X") door = index;
                    foreach (JObject mesh in (JArray)node["meshes"])
                    {
                        if ((string)mesh["kind"] != "MeshFilter") throw new NotSupportedException("Static source mesh only");
                        bool present = (bool?)mesh["renderer_present"] ?? throw new ArgumentException("Missing renderer presence");
                        bool active = (bool?)mesh["renderer_enabled"] ?? throw new ArgumentException("Missing renderer enablement");
                        var fields = (JObject)mesh["local_bounds"]["fields"];
                        meshes.Add(new MeshBounds { Node = index, Present = present,
                            Center = Vector(fields["UnityEngine.Bounds::m_Center"]), Extents = Vector(fields["UnityEngine.Bounds::m_Extents"]) });
                        enabled.Add(active);
                    }
                    if (((JArray)node["renderers"]).Any(renderer => (string)renderer["type"] != "UnityEngine.MeshRenderer"))
                        throw new NotSupportedException("Unsupported skinned/animated renderer");
                }
                var frameCopy = (PoseFrame[])frames.Clone(); var meshCopy = meshes.ToArray(); var spinCopy = spins.ToArray();
                Func<PoseGraph> create = () => new PoseGraph(frameCopy, meshCopy, spinCopy, true, true, true, true);
                create(); // Entire static pose validation before any payment or source entity mutation.
                result.Add(pair.Key, new SourceTransportTemplateData(pair.Key, create, meshCopy, enabled.ToArray(), true, door));
            }
            Templates = result;
        }
        internal bool TryMetadata(SourceUnitPrefab prefab, out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata value) =>
            metadata.TryMetadata(prefab, out value) || (prefab != null && transports.TryGetValue(prefab, out value));
        static float Float(JToken raw)
        {
            if ((string)raw?["codec"] != "f32-bits-v1") throw new ArgumentException("Exact f32 codec required");
            uint bits = uint.Parse((string)raw["bits"], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            if (!math.isfinite(value)) throw new ArgumentException("Finite source transform required"); return value;
        }
        static float3 Vector(JToken raw)
        {
            var fields = (JObject)raw?["fields"] ?? throw new ArgumentException("Exact vector fields required");
            return new float3(Float(fields["UnityEngine.Vector3::x"]), Float(fields["UnityEngine.Vector3::y"]), Float(fields["UnityEngine.Vector3::z"]));
        }
        static quaternion Rotation(JToken raw)
        {
            var fields = (JObject)raw?["fields"] ?? throw new ArgumentException("Exact quaternion fields required");
            return new quaternion(Float(fields["UnityEngine.Quaternion::x"]), Float(fields["UnityEngine.Quaternion::y"]),
                Float(fields["UnityEngine.Quaternion::z"]), Float(fields["UnityEngine.Quaternion::w"]));
        }
    }
}
