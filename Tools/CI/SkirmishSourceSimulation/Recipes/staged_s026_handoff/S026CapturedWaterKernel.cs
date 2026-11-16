using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

// Captured source geometry only. The bool passed to the shared kernel selects
// its rasterization branch; it is not a claim that an S026 match was active.
internal static class S026CapturedWaterKernel
{
    static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static float Bits(JToken token)
    {
        string text = (string)token;
        if (text == null || text.Length != 8 || !uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint bits))
            throw new InvalidDataException("Exact captured float bits required");
        float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Finite captured float required");
        return value;
    }
    static float3 Vector(JToken token) => new float3(Bits(token["xBits"]), Bits(token["yBits"]), Bits(token["zBits"]));
    static void RequireCapture(JObject capture)
    {
        var domain = capture["fixedMapReadDomain"]; var frame = domain?["gridFrame"];
        if ((string)capture["result"] != "Collected" || (string)capture["sourceSha256"] != "68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8" ||
            (string)capture["operationMapId"] != "opmap.skirmish.desert_base_01" || (string)domain?["sourceDefinitionSha256"] != (string)capture["sourceSha256"] ||
            (string)domain?["operationMapId"] != (string)capture["operationMapId"] ||
            (string)domain?["kind"] != "OriginalCampaignFixedMapReadDomain" || (string)domain?["stage"] != "Collected" ||
            (string)domain?["waterAuthority"] != "Original RenderBoundsPresentation.Reader.CollectWaterFootprints" ||
            !((string)domain?["directReaderType"] ?? "").StartsWith("Game.Rendering.RenderBoundsPresentation+Reader,", StringComparison.Ordinal) ||
            (bool?)domain?["gatewayReferenceUnchanged"] != true || !JToken.DeepEquals(domain?["structuralVersionBefore"], domain?["structuralVersionAfter"]) ||
            (bool?)domain?["actorOrMissionStateExported"] != false || (bool?)domain?["sourceFitCertified"] != false ||
            (bool?)domain?["missionReady"] != false || (bool?)domain?["physicalWaterGeometryCaptured"] != true ||
            (bool?)domain?["activeMissionCreateMaskExecuted"] != false || (int?)frame?["width"] != 2048 || (int?)frame?["height"] != 1024 ||
            (float?)frame?["cellSize"] != 1f || !Vector(frame["origin"]).Equals(new float3(0, 0, 0)))
            throw new InvalidDataException("Captured Campaign map, owner provenance or grid frame changed");
    }
    static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException("capture, capture SHA-256 and fresh mask output required");
            var bytes = File.ReadAllBytes(args[0]);
            if (Hash(bytes) != args[1]) throw new InvalidDataException("Captured water input changed");
            var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            var domain = capture["fixedMapReadDomain"]; var frame = domain?["gridFrame"];
            RequireCapture(capture);
            string map = (string)capture["operationMapId"]; int width = (int)frame["width"];
            bool badMap = false, badFrame = false;
            try { capture["operationMapId"] = "opmap.invalid"; RequireCapture(capture); } catch (InvalidDataException) { badMap = true; }
            finally { capture["operationMapId"] = map; }
            try { frame["width"] = width - 1; RequireCapture(capture); } catch (InvalidDataException) { badFrame = true; }
            finally { frame["width"] = width; }
            if (!badMap || !badFrame) throw new InvalidDataException("Wrong-map and wrong-frame controls must reject");
            RequireCapture(capture);
            var rows = domain["waterFootprints"] as JArray;
            if (rows == null || rows.Count != 398 || (int?)domain["waterFootprintCount"] != rows.Count)
                throw new InvalidDataException("Complete recorded footprint array required");
            var values = new SharedWaterGeometryKernel.Footprint[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; var m = row["worldToLocalRowMajor"] as JArray;
                if (m == null || m.Count != 16) throw new InvalidDataException("Exact water transform required");
                float[] a = m.Select(x => Bits(x["bits"])).ToArray();
                values[i] = new SharedWaterGeometryKernel.Footprint {
                    WorldToLocal = new float4x4(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8],a[9],a[10],a[11],a[12],a[13],a[14],a[15]),
                    LocalCenter = Vector(row["localBounds"]["center"]), LocalExtents = Vector(row["localBounds"]["extents"]),
                    WorldCenter = Vector(row["worldBounds"]["center"]), WorldExtents = Vector(row["worldBounds"]["extents"]) };
            }
            bool[] mask = SharedWaterGeometryKernel.BuildMask(true, values, 2048, 1024, 1, new float3(0, 0, 0));
            var result = new byte[mask.Length]; int occupied = 0;
            for (int i = 0; i < mask.Length; i++) if (mask[i]) { result[i] = 1; occupied++; }
            if (Hash(File.ReadAllBytes(args[0])) != args[1]) throw new InvalidDataException("Captured water input changed during rasterization");
            using (var output = new FileStream(args[2], FileMode.CreateNew, FileAccess.Write)) output.Write(result, 0, result.Length);
            Console.WriteLine("[S026CapturedWaterKernel] result=Passed footprints=" + values.Length + " cells=" + result.Length + " occupied=" + occupied + " sha256=" + Hash(result) + " wrongMapRejected=1 wrongFrameRejected=1 activeMatchClaim=0 missionReady=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
