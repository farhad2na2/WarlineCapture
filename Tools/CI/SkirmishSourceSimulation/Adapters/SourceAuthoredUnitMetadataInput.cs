using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Explicit asset-only input; the capture authority still checks the actual
    // compiler setup, wrapper evidence, source pins and prefab identities.
    internal sealed class SourceAuthoredUnitMetadataInput
    {
        internal readonly string MetadataPath, MetadataSha256, IdentityPath;
        internal readonly SourceAuthoredUnitMetadataAuthority Authority;

        private SourceAuthoredUnitMetadataInput(string metadataPath, string metadataSha256,
            string identityPath, SourceAuthoredUnitMetadataAuthority authority)
        {
            MetadataPath = metadataPath;
            MetadataSha256 = metadataSha256;
            IdentityPath = identityPath;
            Authority = authority;
        }

        internal static SourceAuthoredUnitMetadataInput Open(SkirmishResolvedSetup setup,
            string descriptorPath, string descriptorSha256, string repositoryRoot)
        {
            if (!Path.IsPathFullyQualified(descriptorPath ?? "") ||
                !Path.IsPathFullyQualified(repositoryRoot ?? ""))
                throw new InvalidDataException("Absolute source input and repository paths required");
            var descriptor = JObject.Parse(new UTF8Encoding(false, true).GetString(
                AutonomousWorld.PinnedFile(descriptorPath, descriptorSha256, 65536)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            string[] fields = { "schema_version", "kind", "mission_id", "metadata", "identity", "receipt", "log", "native_ready", "mission_ready" };
            if (descriptor.Count != fields.Length || descriptor.Properties().Any(p => !fields.Contains(p.Name)) ||
                (int?)descriptor["schema_version"] != 1 ||
                (string)descriptor["kind"] != "SourceAuthoredUnitMetadataInput" ||
                setup == null || (string)descriptor["mission_id"] != setup.CatalogId ||
                descriptor["native_ready"]?.Type != JTokenType.Boolean || (bool)descriptor["native_ready"] ||
                descriptor["mission_ready"]?.Type != JTokenType.Boolean || (bool)descriptor["mission_ready"])
                throw new InvalidDataException("Exact unqualified source unit metadata descriptor required");

            (string path, string sha) Read(string name, long maximum)
            {
                var value = descriptor[name] as JObject;
                string path = (string)value?["path"], sha = (string)value?["sha256"];
                if (value == null || value.Count != 2 || !Path.IsPathFullyQualified(path ?? "") ||
                    sha == null || sha.Length != 64 || sha.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                    throw new InvalidDataException("Exact pinned " + name + " input required");
                AutonomousWorld.PinnedFile(path, sha, maximum);
                return (path, sha);
            }

            var metadata = Read("metadata", 16 * 1024 * 1024);
            var identity = Read("identity", 4 * 1024 * 1024);
            var receipt = Read("receipt", 1024 * 1024);
            var log = Read("log", 64 * 1024 * 1024);
            var authority = SourceAuthoredUnitMetadataAuthority.FromCapture(setup, metadata.path, metadata.sha,
                receipt.path, receipt.sha, log.path, repositoryRoot);
            authority.ReadIndependentIdentities(identity.path);
            return new SourceAuthoredUnitMetadataInput(metadata.path, metadata.sha, identity.path, authority);
        }
    }
}
