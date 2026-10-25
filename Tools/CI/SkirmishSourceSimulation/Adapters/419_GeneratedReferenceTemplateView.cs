using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Immutable asset-domain projection; gameplay source hashes do not invalidate it.
    internal static class GeneratedReferenceTemplateView
    {
        internal static JObject Read(JObject descriptor, out string binaryDirectory)
        {
            string path = (string)descriptor["path"], sha = (string)descriptor["sha256"];
            JObject domain = Parse(AutonomousWorld.PinnedFile(path, sha, 32 * 1024 * 1024));
            binaryDirectory = Path.GetDirectoryName(path);
            if (descriptor["source_reference_path"] == null) return domain;

            string source = (string)descriptor["source_reference_path"];
            string sourceSha = (string)descriptor["source_reference_sha256"];
            var info = new FileInfo(source);
            if (!Path.IsPathRooted(source) || !info.Exists || info.Length < 1 || info.Length > 128 * 1024 * 1024 ||
                (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular original asset reference required");
            using (var stream = File.OpenRead(source))
            using (var hasher = SHA256.Create())
                if (BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != sourceSha)
                    throw new InvalidDataException("Original asset reference changed beneath projection");

            JObject manifest = Parse(AutonomousWorld.PinnedFile((string)descriptor["projection_manifest_path"],
                (string)descriptor["projection_manifest_sha256"], 1024 * 1024));
            string[] selected = { "schema_version", "kind", "authority", "opening_prefabs", "entities" };
            if ((int?)manifest["schema_version"] != 1 ||
                (string)manifest["kind"] != "PinnedExactTemplateDomainProjection" ||
                (string)manifest["source_path"] != source || (string)manifest["source_sha256"] != sourceSha ||
                (long?)manifest["source_bytes"] != info.Length ||
                (string)manifest["slice_path"] != path || (string)manifest["slice_sha256"] != sha ||
                (long?)manifest["slice_bytes"] != new FileInfo(path).Length ||
                !((JArray)manifest["selected_fields"]).Values<string>().SequenceEqual(selected) ||
                (int?)manifest["root_count"] != ((JObject)domain["opening_prefabs"]).Count ||
                (int?)manifest["entity_count"] != ((JArray)domain["entities"]).Count ||
                (string)domain["source_reference_sha256"] != sourceSha ||
                (string)domain["derived_read_domain"] != "ExactTypedPrefabClosureOnly")
                throw new InvalidDataException("Pinned asset-domain projection identity mismatch");
            binaryDirectory = Path.GetDirectoryName(source);
            return domain;
        }

        private static JObject Parse(byte[] bytes) => JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
    }
}
