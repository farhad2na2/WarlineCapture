using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Runtime.TransportPose;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Diagnostic root and rotor state only. This does not claim a complete
    // hierarchy, imported renderer bounds, native scene, or writer parity.
    internal sealed class SourceS004AirSelfAuthoredPose : ISourceAirSelfPose
    {
        readonly string[] names;
        readonly quaternion[] rotorRotations;
        quaternion rootRotation = quaternion.identity;
        float3 rootPosition;
        internal int RotorCount => names.Length;
        public float3 RootPosition => rootPosition;
        public float3 Forward => math.rotate(rootRotation, new float3(0, 0, 1));
        internal quaternion RotorRotation(int index) => rotorRotations[index];

        static string Hash(byte[] bytes)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

        internal SourceS004AirSelfAuthoredPose(string repositoryRoot, string reportPath, string expectedReportSha, SourceUnitPrefab prefab)
        {
            if (prefab == null || prefab.name != "Unit_Veh_Helicopter_Attack_Small" ||
                prefab.SourceAssetPath != "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab" ||
                prefab.SourceAssetGuid != "fcc8f80798992470f8b8769d3bc4c41a" || prefab.SourceAssetLocalId != 5616646512338707422L)
                throw new InvalidDataException("Exact source attack helicopter token required");
            byte[] bytes = File.ReadAllBytes(reportPath);
            if (Hash(bytes) != expectedReportSha) throw new InvalidDataException("Source transform inventory hash changed");
            var report = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            if ((string)report["status"] != "Collected" || (string)report["scope"] != "SerializedPrefabVariantInventory_NotResolvedUnityHierarchy" ||
                (bool?)report["runtime_template_admitted"] != false || (bool?)report["native_parity"] != false ||
                (string)report["prefab_asset"] != prefab.SourceAssetPath || (string)report["prefab_guid"] != prefab.SourceAssetGuid)
                throw new InvalidDataException("Exact source-only transform inventory required");
            var pins = report["source_pins"] as JObject;
            string[] expectedPaths = { "Assets/Game/Prefabs/Characters/Unit.prefab", "Assets/Game/Prefabs/Characters/Unit.prefab.meta",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab", "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab.meta",
                prefab.SourceAssetPath, prefab.SourceAssetPath + ".meta" };
            if (pins == null || pins.Count != expectedPaths.Length) throw new InvalidDataException("Complete exact variant source pins required");
            foreach (string path in expectedPaths)
            {
                var source = Path.GetFullPath(Path.Combine(repositoryRoot, path));
                if ((string)pins[path] != Hash(File.ReadAllBytes(source))) throw new InvalidDataException("Prefab variant source changed: " + path);
            }
            var rows = report["added_transforms"] as JArray ?? throw new InvalidDataException("Added transform inventory missing");
            var rotors = rows.OfType<JObject>().Where(row => ((string)row["name"])?.EndsWith("_X", StringComparison.Ordinal) == true ||
                ((string)row["name"])?.EndsWith("_Y", StringComparison.Ordinal) == true || ((string)row["name"])?.EndsWith("_Z", StringComparison.Ordinal) == true).ToArray();
            if (rotors.Length != 2 || rotors.Any(row => (bool?)row["active"] != true) ||
                !rotors.Select(row => (string)row["name"]).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(new[] { "Blades_Back_X", "Blades_Main_Y" }))
                throw new InvalidDataException("Exact active authored rotor inventory required");
            names = rotors.Select(row => (string)row["name"]).ToArray();
            rotorRotations = rotors.Select(row => ParseRotation((string)row["local_rotation_serialized"])).ToArray();
            PoseMath.Ready();
        }

        static quaternion ParseRotation(string value)
        {
            var parts = value?.Split(',');
            if (parts == null || parts.Length != 4) throw new InvalidDataException("Serialized rotor rotation required");
            float[] values = new float[4]; string[] labels = { "x", "y", "z", "w" };
            for (int i = 0; i < 4; i++)
            {
                string[] pair = parts[i].Trim().Split(':');
                if (pair.Length != 2 || pair[0].Trim() != labels[i] ||
                    !float.TryParse(pair[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                    throw new InvalidDataException("Exact serialized rotor quaternion required");
            }
            var result = new quaternion(values[0], values[1], values[2], values[3]); PoseMath.Unit(result); return result;
        }
        public void SetRoot(float3 position, quaternion rotation)
        { PoseMath.Finite(position); PoseMath.Unit(rotation); rootPosition = position; rootRotation = rotation; }
        public void Spin(float delta)
        {
            PoseMath.Finite(delta); if (delta < 0 || delta > 1) throw new ArgumentOutOfRangeException(nameof(delta));
            float degrees = 15000f * delta;
            for (int i = 0; i < names.Length; i++)
            {
                float3 euler = names[i].EndsWith("_X", StringComparison.Ordinal) ? new float3(degrees, 0, 0) :
                    names[i].EndsWith("_Y", StringComparison.Ordinal) ? new float3(0, degrees, 0) : new float3(0, 0, degrees);
                rotorRotations[i] = math.normalize(math.mul(rotorRotations[i], PoseMath.Euler(euler)));
            }
        }
    }
}
