using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal static class SourceNavigationFactsTool
    {
        internal static bool TryRun(string[] args, out int result)
        {
            result = 1;
            if (args.Length == 0 || args[0] != "prepare-navigation-facts") return false;
            try
            {
                if (args.Length != 3) throw new ArgumentException("prepare-navigation-facts requires raw descriptor JSON and fresh output directory");
                string directory = Path.GetFullPath(args[2]);
                if (Directory.Exists(directory) || File.Exists(directory)) throw new InvalidDataException("Fresh cache preparation output required");
                Directory.CreateDirectory(directory);
                var utf8 = new UTF8Encoding(false, true);
                var descriptor = JObject.Parse(File.ReadAllText(args[1]), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (descriptor["facts"] != null) throw new InvalidDataException("Preparation requires original raw descriptor");
                string path = (string)descriptor["path"], rawSha = (string)descriptor["sha256"];
                byte[] raw = AutonomousWorld.PinnedFile(path, rawSha, 128 * 1024 * 1024);
                var source = JObject.Parse(utf8.GetString(raw), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                var rows = GeneratedAuthoredNavigationImporter.Decode(source, Path.GetDirectoryName(path));
                byte[] encoded = SourceNavigationFacts.Encode(rows, rawSha);
                var decoded = SourceNavigationFacts.Decode(encoded, rawSha);
                if (!SourceNavigationFacts.AllFactsEqual(rows, decoded) || !encoded.SequenceEqual(SourceNavigationFacts.Encode(decoded, rawSha)))
                    throw new InvalidDataException("Every original decoded authored fact/bit/order/identity must match");
                string cache = Path.Combine(directory, "navigation.facts"), manifestPath = Path.Combine(directory, "manifest.json");
                File.WriteAllBytes(cache, encoded);
                string cacheSha = AutonomousWorld.Hash(encoded);
                var manifest = new JObject { ["kind"] = SourceNavigationFacts.Format, ["source_sha256"] = rawSha, ["cache_sha256"] = cacheSha, ["producer_sha256"] = SourceNavigationFactsIdentity.Producer, ["schema_sha256"] = SourceNavigationFacts.Schema, ["rows"] = rows.Count };
                File.WriteAllText(manifestPath, manifest.ToString());
                descriptor["facts"] = new JObject { ["format"] = SourceNavigationFacts.Format, ["path"] = cache, ["sha256"] = cacheSha, ["manifest_path"] = manifestPath, ["manifest_sha256"] = AutonomousWorld.Hash(File.ReadAllBytes(manifestPath)) };
                File.WriteAllText(Path.Combine(directory, "descriptor.json"), descriptor.ToString());
                File.WriteAllText(Path.Combine(directory, "preparation.json"), new JObject { ["source_bytes"] = raw.Length, ["cache_bytes"] = encoded.Length, ["decoded_row_count"] = rows.Count, ["all_original_decoded_field_bits_order_enabled_id_equal"] = true, ["no_runtime_entities_created_or_cached"] = true, ["cache_sha256"] = cacheSha, ["schema_sha256"] = SourceNavigationFacts.Schema, ["producer_sha256"] = SourceNavigationFactsIdentity.Producer }.ToString());
                Console.WriteLine("[NavigationFactsPrepare] result=Passed rows=" + rows.Count + " bytes=" + encoded.Length);
                result = 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); }
            return true;
        }
    }
}
