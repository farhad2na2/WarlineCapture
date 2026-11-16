using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Runtime;
using Newtonsoft.Json.Linq;

// Isolated source-host boundary checks; no World or mission admission.
internal static class SourceS003DiagnosticConfigurationControl
{
    static string Hash(string path)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Fixture output directory required");
        Directory.CreateDirectory(args[0]);
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(args[0], "selection.json"));
        var qualification = new JObject { ["path"] = "/fixture/regular.json", ["sha256"] = new string('a', 64) };
        var initialization = new JObject { ["mission_id"] = "S003", ["selection"] = new JObject { ["seed"] = 104732, ["difficulty_id"] = 2, ["size_id"] = 1 }, ["configuration_qualification"] = qualification };
        JObject Good() => new JObject { ["schema_version"] = 1, ["kind"] = "S003SourceDiagnosticSelection", ["mission_id"] = "S003", ["seed"] = 104732, ["size_id"] = 1, ["selected_difficulty"] = "Recruit", ["capture_difficulty"] = "Regular", ["capture_qualification"] = qualification.DeepClone(), ["native_parity"] = false, ["runtime_admission"] = false, ["mission_ready"] = false };
        void Set(string text, bool wrongHash = false)
        {
            File.WriteAllText(path, text, new UTF8Encoding(false));
            Environment.SetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG", path);
            Environment.SetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256", wrongHash ? new string('0', 64) : Hash(path));
        }
        void Rejected(string name, Action action)
        {
            try { action(); } catch (Exception) { if (SourceS003DiagnosticConfiguration.Current != null) throw new Exception(name + " leaked a context"); return; }
            throw new Exception(name + " was accepted");
        }
        try
        {
            Set(Good().ToString());
            using (var active = SourceS003DiagnosticConfiguration.Open(initialization))
            {
                if (active == null || active.SelectedDifficulty.ToString() != "Recruit") throw new Exception("valid config rejected");
                bool duplicate = false;
                try { SourceS003DiagnosticConfiguration.Open(initialization); } catch (InvalidOperationException) { duplicate = true; }
                if (!duplicate) throw new Exception("nested context accepted");
            }
            if (SourceS003DiagnosticConfiguration.Current != null) throw new Exception("context not released");
            Set(Good().ToString(), true); Rejected("wrong hash", () => SourceS003DiagnosticConfiguration.Open(initialization));
            var bad = Good(); bad["seed"] = 104733; Set(bad.ToString()); Rejected("wrong seed", () => SourceS003DiagnosticConfiguration.Open(initialization));
            bad = Good(); bad["capture_qualification"]["sha256"] = new string('b', 64); Set(bad.ToString()); Rejected("wrong capture", () => SourceS003DiagnosticConfiguration.Open(initialization));
            bad = Good(); bad["selected_difficulty"] = "1"; Set(bad.ToString()); Rejected("numeric difficulty", () => SourceS003DiagnosticConfiguration.Open(initialization));
            bad = Good(); bad["extra"] = 1; Set(bad.ToString()); Rejected("extra field", () => SourceS003DiagnosticConfiguration.Open(initialization));
            bad = Good(); bad["native_parity"] = true; Set(bad.ToString()); Rejected("native promotion", () => SourceS003DiagnosticConfiguration.Open(initialization));
            Set(Good().ToString()); var changed = SourceS003DiagnosticConfiguration.Open(initialization); File.AppendAllText(path, " ");
            bool changedRejected = false; try { changed.Dispose(); } catch (Exception) { changedRejected = true; }
            if (!changedRejected || SourceS003DiagnosticConfiguration.Current != null) throw new Exception("changed input or cleanup not enforced");
            Console.WriteLine("[S003DiagnosticConfigurationControl] result=Passed valid=1 negatives=8 cleanup=1 world=0 readiness=0");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG", null);
            Environment.SetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256", null);
        }
    }
}
