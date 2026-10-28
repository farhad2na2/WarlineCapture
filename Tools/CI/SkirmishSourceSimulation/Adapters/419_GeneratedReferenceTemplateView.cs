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
        // Exact diagnostic native bytes; only the declared typed consumer subset may qualify. Full Unity replay remains false.
        internal const string QualifiedFocusedPrefabSha="21e7474adfe2682a638bc6c23708f48b9611c526051871c0d93b4cb18a6ae4aa";
        internal static JObject Read(JObject descriptor, out string binaryDirectory)
        {
            string path = (string)descriptor["path"], sha = (string)descriptor["sha256"];
            JObject domain = Parse(AutonomousWorld.PinnedFile(path, sha, 32 * 1024 * 1024));
            binaryDirectory = Path.GetDirectoryName(AutonomousWorld.ResolvePinnedPath(path));
            bool focused=(string)domain["kind"]=="AuthoredSkirmishPrefabReferenceData";
            if(focused&&descriptor["source_reference_path"]==null)throw new InvalidDataException("Exact focused prefab consumer projection/provenance required; full native replay remains unqualified");
            if (descriptor["source_reference_path"] == null) return domain;

            string source = (string)descriptor["source_reference_path"];
            string sourceSha = (string)descriptor["source_reference_sha256"];
            if(sourceSha==QualifiedFocusedPrefabSha&&!focused)throw new InvalidDataException("Focused diagnostic namespace cannot be relabelled as another template schema");
            string sourcePhysical = AutonomousWorld.ResolvePinnedPath(source);
            var info = new FileInfo(sourcePhysical);
            if (!Path.IsPathRooted(source) || !info.Exists || info.Length < 1 || info.Length > 128 * 1024 * 1024 ||
                (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular original asset reference required");
            using (var stream = File.OpenRead(sourcePhysical))
            using (var hasher = SHA256.Create())
                if (BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != sourceSha)
                    throw new InvalidDataException("Original asset reference changed beneath projection");

            JObject manifest = Parse(AutonomousWorld.PinnedFile((string)descriptor["projection_manifest_path"],
                (string)descriptor["projection_manifest_sha256"], 1024 * 1024));
            string[] selected = focused?new[]{"schema_version","kind","authority","source_scenes","unity_version","template_namespace_id","requested_prefab_keys","requested_prefab_keys_sha256","opening_prefabs","entities","unsupported_values","replay_ready","fresh_world_allocator_exported","full_mission_initialized","native_visual_readiness","geometry_exported","consumer_rule"}:new[]{"schema_version","kind","authority","opening_prefabs","entities"};
            if(focused){if(sourceSha!=QualifiedFocusedPrefabSha&&sourceSha!="83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f")throw new InvalidDataException("Unqualified focused native source identity");var original=Parse(File.ReadAllBytes(sourcePhysical));foreach(string name in selected)if(!JToken.DeepEquals(domain[name],original[name]))throw new InvalidDataException("Focused projection changed native field/namespace: "+name);if((bool?)domain["replay_ready"]!=false||(bool?)domain["full_mission_initialized"]!=false||(bool?)domain["geometry_exported"]!=false)throw new InvalidDataException("Focused diagnostic native scope must remain explicit");}
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
