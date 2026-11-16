using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

internal static class S073CapturedWaterTimingControl
{
    private static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    private static float F(JToken token) => BitConverter.ToSingle(BitConverter.GetBytes((int)token), 0);
    private static float3 Vector(JToken token)
    {
        var row = (JArray)token;
        if (row.Count != 3) throw new InvalidDataException("Exact captured vector required");
        return new float3(F(row[0]), F(row[1]), F(row[2]));
    }
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new InvalidDataException("Water capture and exact digest required");
        byte[] input = File.ReadAllBytes(args[0]);
        if (Hash(input) != args[1]) throw new InvalidDataException("Water capture changed");
        var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(input));
        var rows = capture["footprints"] as JArray;
        if (rows == null || rows.Count != 194) throw new InvalidDataException("Exact bounded water capture required");
        var values = new SharedWaterGeometryKernel.Footprint[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i]; var m = (JArray)row["world_to_local_bits"];
            if (m.Count != 16) throw new InvalidDataException("Exact water transform required");
            values[i] = new SharedWaterGeometryKernel.Footprint {
                WorldToLocal = new float4x4(F(m[0]), F(m[1]), F(m[2]), F(m[3]), F(m[4]), F(m[5]), F(m[6]), F(m[7]),
                    F(m[8]), F(m[9]), F(m[10]), F(m[11]), F(m[12]), F(m[13]), F(m[14]), F(m[15])),
                LocalCenter = Vector(row["local_center_bits"]), LocalExtents = Vector(row["local_extents_bits"]),
                WorldCenter = Vector(row["world_center_bits"]), WorldExtents = Vector(row["world_extents_bits"])
            };
        }
        var watch = Stopwatch.StartNew();
        bool[] mask = SharedWaterGeometryKernel.BuildMask(true, values, 2048, 1024, 1, new float3(0, 0, 0));
        watch.Stop();
        var bytes = new byte[mask.Length]; int count = 0;
        for (int i = 0; i < mask.Length; i++) if (mask[i]) { bytes[i] = 1; count++; }
        if (Hash(File.ReadAllBytes(args[0])) != args[1]) throw new InvalidDataException("Water source changed during test");
        Console.WriteLine("[S073CapturedWaterTimingControl] result=Passed cells=" + mask.Length + " occupied=" + count +
            " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " sha256=" + Hash(bytes) + " scope=CapturedSourceKernel_NotNativeOrMissionReadiness");
    }
}
