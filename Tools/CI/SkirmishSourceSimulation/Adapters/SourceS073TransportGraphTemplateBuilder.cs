using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Simulation;
using Game.Runtime.TransportPose;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Converts the independently pinned S073 authored transport hierarchy into the
    // runtime's template DTO. It does not bind delivery, camera state, or bounds callbacks.
    internal static class SourceS073TransportGraphTemplateBuilder
    {
        internal const string TransportKey = "Unit_Veh_Helicopter_Transport";
        internal const string TransportPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab";
        internal const string TransportGuid = "e540888d7cd8a489f9472b3456bf2406";
        internal const long TransportLocalId = 5616646512338707422L;

        internal static IReadOnlyDictionary<SourceUnitPrefab, SourceTransportTemplateData> Build(
            JObject transportRow, SourceUnitPrefab canonicalTransportToken)
        {
            if (transportRow == null) throw new ArgumentNullException(nameof(transportRow));
            ValidateCanonicalToken(transportRow["prefab"] as JObject, canonicalTransportToken);
            JArray nodes = transportRow["nodes"] as JArray ??
                throw new InvalidDataException("S073 transport nodes missing");
            if (nodes.Count == 0 || nodes.Count > 128)
                throw new InvalidDataException("Bounded actual S073 transport hierarchy required");

            PoseMath.AdmitInstalledMono();
            var frames = new PoseFrame[nodes.Count];
            var meshes = new List<MeshBounds>();
            var rendererEnabled = new List<bool>();
            var spinNodes = new List<int>();
            int doorNode = -1;

            for (int index = 0; index < nodes.Count; index++)
            {
                JObject node = nodes[index] as JObject ??
                    throw new InvalidDataException("S073 transport node must be an object");
                Require((int?)node["index"] == index && (bool?)node["active_self"] == true,
                    "Complete source-ordered active S073 transport hierarchy required");
                string name = (string)node["name"];
                if (string.IsNullOrEmpty(name)) throw new InvalidDataException("S073 transport node name missing");
                frames[index] = new PoseFrame
                {
                    Parent = ReadParent(node["parent"], index),
                    Name = name,
                    Position = ReadVector3(node["local_position"], "transport[" + index + "].position"),
                    Rotation = ReadQuaternion(node["local_rotation"], "transport[" + index + "].rotation"),
                    Scale = ReadVector3(node["local_scale"], "transport[" + index + "].scale"),
                    InitialEulerPresent = true,
                    InitialEulerDegrees = ReadVector3(node["initial_local_euler_degrees"], "transport[" + index + "].euler")
                };
                if (name.EndsWith("_X", StringComparison.Ordinal) || name.EndsWith("_Y", StringComparison.Ordinal) ||
                    name.EndsWith("_Z", StringComparison.Ordinal)) spinNodes.Add(index);
                if (doorNode < 0 && name == "Door_X") doorNode = index;

                JArray nodeMeshes = node["meshes"] as JArray ??
                    throw new InvalidDataException("S073 transport mesh inventory missing");
                foreach (JObject mesh in nodeMeshes)
                {
                    Require((string)mesh["kind"] == "MeshFilter", "Unsupported S073 transport mesh type");
                    JObject bounds = mesh["local_bounds"]?["fields"] as JObject ??
                        throw new InvalidDataException("S073 transport mesh bounds missing");
                    bool rendererPresent = ReadRequiredBool(mesh["renderer_present"], "transport.mesh.renderer_present");
                    bool enabled = ReadRequiredBool(mesh["renderer_enabled"], "transport.mesh.renderer_enabled");
                    meshes.Add(new MeshBounds
                    {
                        Node = index,
                        Present = rendererPresent,
                        Center = ReadVector3Field(bounds, "UnityEngine.Bounds::m_Center", "transport.mesh.center"),
                        Extents = ReadVector3Field(bounds, "UnityEngine.Bounds::m_Extents", "transport.mesh.extents")
                    });
                    rendererEnabled.Add(enabled);
                }

                JArray renderers = node["renderers"] as JArray ??
                    throw new InvalidDataException("S073 transport renderer inventory missing");
                foreach (JObject renderer in renderers)
                    Require((string)renderer["type"] == "UnityEngine.MeshRenderer",
                        "Unsupported S073 transport renderer type");
            }

            PoseFrame[] frameCopy = (PoseFrame[])frames.Clone();
            MeshBounds[] meshCopy = meshes.ToArray();
            bool[] enabledCopy = rendererEnabled.ToArray();
            int[] spinCopy = spinNodes.ToArray();
            Func<PoseGraph> createPose = () => new PoseGraph(
                (PoseFrame[])frameCopy.Clone(), (MeshBounds[])meshCopy.Clone(), (int[])spinCopy.Clone(),
                true, true, true, true);

            // Validate the complete source hierarchy before callers can use the template.
            createPose();
            var template = new SourceTransportTemplateData(
                canonicalTransportToken, createPose, meshCopy, enabledCopy, true, doorNode);
            return new Dictionary<SourceUnitPrefab, SourceTransportTemplateData>
            {
                [canonicalTransportToken] = template
            };
        }

        static void ValidateCanonicalToken(JObject asset, SourceUnitPrefab token)
        {
            if (asset == null || token == null ||
                token.name != TransportKey || token.SourceAssetPath != TransportPath ||
                token.SourceAssetGuid != TransportGuid || token.SourceAssetLocalId != TransportLocalId ||
                (string)asset["key"] != TransportKey || (string)asset["path"] != TransportPath ||
                (string)asset["guid"] != TransportGuid || (long?)asset["local_id"] != TransportLocalId)
                throw new InvalidDataException("Exact same-run canonical S073 transport prefab token required");
        }

        static int ReadParent(JToken token, int index)
        {
            if (token == null || token.Type != JTokenType.Integer)
                throw new InvalidDataException("S073 transport parent index missing at node " + index);
            int parent = (int)token;
            if (parent < -1 || parent >= index)
                throw new InvalidDataException("S073 transport parent must precede its child at node " + index);
            return parent;
        }

        static float ReadF32Bits(JToken token, string label)
        {
            if ((string)token?["codec"] != "f32-bits-v1" || ((string)token["bits"])?.Length != 8 ||
                !uint.TryParse((string)token["bits"], NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out uint bits))
                throw new InvalidDataException("Exact pinned f32 required: " + label);
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            if (!math.isfinite(value)) throw new InvalidDataException("Finite pinned f32 required: " + label);
            return value;
        }

        static float3 ReadVector3(JToken token, string label)
        {
            JObject fields = token?["fields"] as JObject;
            Require((string)token?["codec"] == "struct-fields-v1" &&
                (string)token["type"] == "UnityEngine.Vector3" && fields != null,
                "Exact pinned Unity Vector3 required: " + label);
            return ReadVector3Fields(fields, "UnityEngine.Vector3::x", "UnityEngine.Vector3::y",
                "UnityEngine.Vector3::z", label);
        }

        static float3 ReadVector3Field(JObject fields, string key, string label)
        {
            JObject vector = fields[key] as JObject ?? throw new InvalidDataException("Exact vector field required: " + label);
            Require((string)vector["codec"] == "struct-fields-v1" &&
                (string)vector["type"] == "UnityEngine.Vector3" && vector["fields"] is JObject,
                "Exact pinned Unity Vector3 required: " + label);
            return ReadVector3(vector, label);
        }

        static float3 ReadVector3Fields(JObject fields, string x, string y, string z, string label) =>
            new float3(ReadF32Bits(fields[x], label + ".x"), ReadF32Bits(fields[y], label + ".y"),
                ReadF32Bits(fields[z], label + ".z"));

        static quaternion ReadQuaternion(JToken token, string label)
        {
            JObject fields = token?["fields"] as JObject;
            Require((string)token?["codec"] == "struct-fields-v1" &&
                (string)token["type"] == "UnityEngine.Quaternion" && fields != null,
                "Exact pinned Unity Quaternion required: " + label);
            quaternion value = new quaternion(
                ReadF32Bits(fields["UnityEngine.Quaternion::x"], label + ".x"),
                ReadF32Bits(fields["UnityEngine.Quaternion::y"], label + ".y"),
                ReadF32Bits(fields["UnityEngine.Quaternion::z"], label + ".z"),
                ReadF32Bits(fields["UnityEngine.Quaternion::w"], label + ".w"));
            if (math.abs(math.lengthsq(value.value) - 1f) > 0.0001f)
                throw new InvalidDataException("Normalized pinned quaternion required: " + label);
            return value;
        }

        static bool ReadRequiredBool(JToken token, string label)
        {
            if (token == null || token.Type != JTokenType.Boolean)
                throw new InvalidDataException("Exact boolean required: " + label);
            return (bool)token;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
