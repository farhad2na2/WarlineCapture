using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class SourceAuthoredUnitMetadataInputControl
{
    private static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    public static int Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Descriptor, scratch directory and source root required");
        var original = JObject.Parse(File.ReadAllText(args[0]));
        var setup = new SkirmishResolvedSetup { CatalogId = "S005" };
        Directory.CreateDirectory(args[1]);
        int rejected = 0;
        for (int i = 0; i < 8; i++)
        {
            var changed = (JObject)original.DeepClone();
            switch (i)
            {
                case 1: changed["mission_id"] = "S006"; break;
                case 2: changed["native_ready"] = true; break;
                case 3: changed["mission_ready"] = true; break;
                case 4: changed["extra"] = true; break;
                case 5: changed["metadata"]["sha256"] = new string('0', 64); break;
                case 6: changed["identity"]["sha256"] = new string('0', 64); break;
                case 7: changed["log"]["sha256"] = new string('0', 64); break;
            }
            var bytes = Encoding.UTF8.GetBytes(changed.ToString(Formatting.None));
            string path = Path.GetFullPath(Path.Combine(args[1], "invalid-" + i + ".json"));
            File.WriteAllBytes(path, bytes);
            try
            {
                SourceAuthoredUnitMetadataInput.Open(setup, path, i == 0 ? new string('0', 64) : Hash(bytes), args[2]);
                throw new Exception("Invalid descriptor unexpectedly accepted: " + i);
            }
            catch (InvalidDataException) { rejected++; }
            catch (InvalidOperationException) { rejected++; }
        }
        if (rejected != 8) throw new Exception("Incomplete descriptor rejection controls");
        Console.WriteLine("[SourceAuthoredUnitMetadataInputControl] result=Passed invalid=8 world=0 readiness=0");
        return 0;
    }
}
