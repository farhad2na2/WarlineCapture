using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Authored scene input only. This does not imply native world state or readiness.
    internal static class S073ExchangeSourceBinding
    {
        private const string QualifiedGraph = "cf51352a9046d8680cc8611b6d253fc812568b4169163afcb85e375ba7954427";
        private const string Asset = "Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset";
        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        internal static ResourceExchangeRecipeConfigSet Load(string repository, string graphPath, string expected)
        {
            string root = Path.GetFullPath(repository).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (expected != QualifiedGraph || Hash(graphPath) != expected)
                throw new InvalidDataException("Exact qualified S073 exchange source graph required");
            var graph = JObject.Parse(File.ReadAllText(graphPath));
            if ((int?)graph["schema_version"] != 1 || (string)graph["authority"] != "AuthoredConfigAssets" ||
                (string)graph["scene_path"] != "Assets/Game/Scenes/Match.unity" ||
                (bool?)graph["compiled_setup"] != false || (bool?)graph["world_initialized"] != false ||
                (bool?)graph["native_world_state_used"] != false)
                throw new InvalidDataException("Scene-bound authored exchange graph required");
            var pins = graph["source_pins"] as JObject ?? throw new InvalidDataException("Exchange source pins required");
            foreach (var pin in pins.Properties())
            {
                string path = Path.GetFullPath(pin.Name);
                if (!Path.IsPathRooted(pin.Name) || !path.StartsWith(root, StringComparison.Ordinal) || Hash(path) != (string)pin.Value)
                    throw new InvalidDataException("Exchange source pin changed: " + pin.Name);
            }
            var nodes = graph["nodes"] as JArray;
            if (nodes == null || nodes.Count != 1 || (string)nodes[0]["source_path"] != Asset ||
                (string)nodes[0]["source_type"] != "ResourceExchangeRecipeConfigSet" ||
                (string)nodes[0]["source_sha256"] != (string)pins[Path.Combine(root, Asset)])
                throw new InvalidDataException("Exact scene exchange asset node required");
            var config = new SourceConfigMaterializer(graph).ByPath<ResourceExchangeRecipeConfigSet>(Asset);
            if (Hash(graphPath) != expected) throw new InvalidDataException("Exchange graph changed during materialization");
            foreach (var pin in pins.Properties())
                if (Hash(pin.Name) != (string)pin.Value) throw new InvalidDataException("Exchange source changed during materialization: " + pin.Name);
            return config;
        }
    }
}
