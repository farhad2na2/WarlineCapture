using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

internal static class S073PinSampleTimingControl
{
    private static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static void Main(string[] args)
    {
        if (args.Length != 2) throw new InvalidDataException("Preservation receipt and exact digest required");
        byte[] input = File.ReadAllBytes(args[0]);
        if (Hash(input) != args[1]) throw new InvalidDataException("Preservation receipt changed");
        var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(input));
        var files = receipt["files"] as JArray;
        string root = Path.GetFullPath((string)receipt["preserved_root"]).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (files == null || files.Count < 128 || (int?)receipt["project_files"] != files.Count)
            throw new InvalidDataException("Bounded preservation source list required");
        var type = Type.GetType("Game.Runtime.SourcePackedCampaignGeometry, SourcePlacementDomain", true);
        var nativeHash = type.GetMethod("HashFile", BindingFlags.NonPublic | BindingFlags.Static);
        if (nativeHash == null) throw new MissingMethodException(type.FullName, "HashFile");
        var watch = Stopwatch.StartNew(); long bytes = 0;
        for (int i = 0; i < 128; i++)
        {
            var row = files[i * (files.Count - 1) / 127];
            string relative = (string)row["relative_path"], path = Path.GetFullPath(Path.Combine(root, relative));
            if (Path.IsPathRooted(relative) || !path.StartsWith(root, StringComparison.Ordinal)) throw new InvalidDataException("Preserved file escaped root");
            if ((string)nativeHash.Invoke(null, new object[] { path, 512L * 1024 * 1024 }) != (string)row["sha256"])
                throw new InvalidDataException("Preserved source sample changed: " + relative);
            bytes += new FileInfo(path).Length;
        }
        watch.Stop();
        if (Hash(File.ReadAllBytes(args[0])) != args[1]) throw new InvalidDataException("Preservation receipt changed during timing");
        Console.WriteLine("[S073PinSampleTimingControl] result=Passed files=128 bytes=" + bytes + " elapsed_ms=" +
            watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " scope=SampleOnly_NotFullVerificationOrMissionReadiness");
    }
}
