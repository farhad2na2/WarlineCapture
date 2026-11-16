using System;
using System.IO;
using System.Security.Cryptography;
using Game.Runtime;

namespace Game.Components {
    public struct GridConfig { }
}

namespace Game.Runtime {
    internal sealed class SourceProductionGroundWorld {
        internal Grid Grid = new Grid();
        internal void ReserveRect(ref Unity.Collections.NativeBitArray reserved, int x0, int y0, int x1, int y1) { }
    }
    internal sealed class Grid {
        internal int Width = 2048, Height = 1024;
        internal float CellSize = 1;
        internal Unity.Mathematics.float3 Origin = Unity.Mathematics.float3.zero;
    }
    internal static class GridUtils {
        internal static Unity.Mathematics.int2 WorldToCell(Grid grid, Unity.Mathematics.float3 point) => default;
    }
    internal static class AutonomousWorld {
        internal static byte[] PinnedFile(string path, string sha, int maximum) {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length > maximum || Hash(bytes) != sha) throw new InvalidDataException("Pinned file mismatch");
            return bytes;
        }
        internal static string Hash(byte[] bytes) {
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
    internal static class SourceCapturedFixedMapInputs {
        internal static Newtonsoft.Json.Linq.JObject Read(Newtonsoft.Json.Linq.JObject root) => throw new NotSupportedException();
    }
}

internal static class S003ClearanceConsumerHarness {
    static int Main(string[] args) {
        if (args.Length != 5) return 64;
        try {
            Environment.SetEnvironmentVariable("WARLINE_S003_PHYSICAL_WATER_DIAGNOSTIC", "1");
            new GeneratedS002DropClearance(args[0], args[1], args[2], args[3], long.Parse(args[4]));
            Console.WriteLine("[S003ClearanceConsumer] accepted");
            return 0;
        }
        catch (Exception error) {
            Console.WriteLine("[S003ClearanceConsumer] rejected " + error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }
}
