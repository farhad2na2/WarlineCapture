using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
namespace Game.Runtime
{
    // CLI provenance only. It reads actually loaded modules and never accesses world state.
    internal static class SourceRuntimeManifest
    {
        static string Hash(string path)
        {
            using var file = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }
        static JObject FilePin(string path)
        {
            path = Path.GetFullPath(path);
            return new JObject { ["path"] = path, ["sha256"] = Hash(path), ["bytes"] = new FileInfo(path).Length };
        }
        internal static JObject Capture(string explicitRuntimeConfig)
        {
            using var process = Process.GetCurrentProcess();
            string executable = process.MainModule?.FileName ?? throw new NotSupportedException("Actual runtime process executable unavailable");
            string entry = Assembly.GetEntryAssembly()?.Location ?? throw new NotSupportedException("Actual managed entry image unavailable");
            var runtimePin = FilePin(executable);
            var configPin = FilePin(explicitRuntimeConfig);
            var entryPin = FilePin(entry);
            JArray assemblies = null;
            for (int pass = 0; pass < 8; pass++)
            {
                var loaded = AppDomain.CurrentDomain.GetAssemblies();
                var rows = new JArray();
                foreach (var assembly in loaded.OrderBy(a => a.FullName, StringComparer.Ordinal))
                {
                    var row = new JObject { ["identity"] = assembly.FullName, ["dynamic"] = assembly.IsDynamic };
                    if (!assembly.IsDynamic)
                    {
                        string location = assembly.Location;
                        if (string.IsNullOrEmpty(location) || !File.Exists(location))
                            throw new NotSupportedException("Loaded managed assembly has no hashable file: " + assembly.FullName);
                        row["file"] = FilePin(location);
                        row["loaded_module_mvid"] = assembly.ManifestModule.ModuleVersionId.ToString("D");
                    }
                    rows.Add(row);
                }
                if (AppDomain.CurrentDomain.GetAssemblies().All(a => loaded.Contains(a))) { assemblies = rows; break; }
            }
            if (assemblies == null) throw new NotSupportedException("Loaded runtime module set did not stabilize during capture");
            return new JObject
            {
                ["kind"] = "ActualLoadedManagedRuntimeManifest", ["schema_version"] = 1,
                ["runtime_executable"] = runtimePin, ["runtime_config"] = configPin,
                ["managed_entry"] = entryPin, ["clr_version"] = Environment.Version.ToString(),
                ["actually_loaded_managed_assemblies"] = assemblies,
                ["scope"] = "Loaded assemblies at this capture point; late-loaded files are listed by the final capture. Native library census is not inferred."
            };
        }
        internal static void RequireUnchanged(JObject before, JObject after)
        {
            foreach (string key in new[] { "runtime_executable", "runtime_config", "managed_entry" })
                if (!JToken.DeepEquals(before[key], after[key])) throw new InvalidOperationException("Runtime file drift: " + key);
            var final = ((JArray)after["actually_loaded_managed_assemblies"]).OfType<JObject>()
                .Where(a => !(bool)a["dynamic"]).ToDictionary(a => (string)a["file"]["path"], StringComparer.Ordinal);
            foreach (var original in ((JArray)before["actually_loaded_managed_assemblies"]).OfType<JObject>())
            {
                if ((bool)original["dynamic"]) continue;
                string path = (string)original["file"]["path"];
                if (!final.TryGetValue(path, out var current) || !JToken.DeepEquals(original["file"], current["file"]) ||
                    !JToken.DeepEquals(original["loaded_module_mvid"], current["loaded_module_mvid"]))
                    throw new InvalidOperationException("Loaded managed runtime drift: " + path);
            }
        }
        internal static JObject Write(string directory, string phase, string explicitRuntimeConfig)
        {
            var manifest = Capture(explicitRuntimeConfig);
            manifest["phase"] = phase;
            File.WriteAllText(Path.Combine(directory, "runtime-" + phase + ".json"), manifest.ToString(Newtonsoft.Json.Formatting.Indented));
            return manifest;
        }
    }
}
