using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Exercises the actual source-host reader on real source-compiler readbacks.
internal static class SourceS004DifficultyInputControl
{
    static string Hash(byte[] data) { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
    static void Reject(JObject value, string path)
    {
        byte[] raw = Encoding.UTF8.GetBytes(value.ToString(Formatting.None)); File.WriteAllBytes(path, raw);
        try { SourceS004CompiledSetupInput.Read(path, Hash(raw)); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Invalid setup admitted: " + path);
    }
    static void Main(string[] args)
    {
        if (args.Length != 9) throw new ArgumentException("Four path/hash pairs and fresh fixture directory required");
        if (Directory.Exists(args[8])) throw new IOException("Fresh output required");
        Directory.CreateDirectory(args[8]); string[] names = { "Recruit", "Regular", "Veteran", "Commander" };
        for (int i = 0; i < 4; i++)
        {
            string path = args[2 * i], hash = args[2 * i + 1];
            var setup = SourceS004CompiledSetupInput.Read(path, hash);
            if (setup.DifficultyId.ToString() != names[i] || !setup.MeasuredLayoutBound) throw new InvalidDataException("Wrong accepted setup");
            var source = JObject.Parse(File.ReadAllText(path));
            var bad = (JObject)source.DeepClone(); bad["difficulty"] = "wrong"; Reject(bad, Path.Combine(args[8], names[i] + "-label.json"));
            bad = (JObject)source.DeepClone(); bad["compiled_setup"]["MeasuredLayoutBound"] = false; Reject(bad, Path.Combine(args[8], names[i] + "-unbound.json"));
            bad = (JObject)source.DeepClone(); bad["compiled_setup"]["Seed"] = 104734; Reject(bad, Path.Combine(args[8], names[i] + "-seed.json"));
            bad = (JObject)source.DeepClone(); bad["runtime_admission"] = true; Reject(bad, Path.Combine(args[8], names[i] + "-scope.json"));
            try { SourceS004CompiledSetupInput.Read(path, new string('0', 64)); }
            catch (InvalidDataException) { continue; }
            throw new InvalidOperationException("Wrong file digest accepted");
        }
        Console.WriteLine("[SourceS004DifficultyInputControl] result=Passed accepted=4 rejected=20 world=0 readiness=0");
    }
}
