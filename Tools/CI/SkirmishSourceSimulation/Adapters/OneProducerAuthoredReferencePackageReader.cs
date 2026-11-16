using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal static class OneProducerAuthoredReferencePackageReader
    {
        const string JsonRelative = "Assets/Game/Resources/OneProducerAuthoredReferencePackage.json";
        const string AssetRelative = "Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset";
        const string PackageScriptMeta = "Assets/Game/Scripts/Configs/Skirmish/OneProducerAuthoredReferencePackageConfig.cs.meta";

        internal static OneProducerAuthoredReferencePackage Load(JObject descriptor, string repositoryRoot)
        {
            string[] expectedFields = { "asset_path", "asset_sha256", "json_path", "json_sha256" };
            if (descriptor == null || descriptor.Count != expectedFields.Length ||
                descriptor.Properties().Any(p => !expectedFields.Contains(p.Name)) ||
                descriptor.Properties().Any(p => p.Value.Type != JTokenType.String))
                throw new InvalidDataException("Exact opt-in authored-reference package input required");

            string root = Path.GetFullPath(repositoryRoot ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string assetPath = Resolve(root, JsonRelative, (string)descriptor["json_path"]);
            string dataPath = Resolve(root, AssetRelative, (string)descriptor["asset_path"]);
            byte[] jsonBytes = ReadPinned(root, assetPath, (string)descriptor["json_sha256"], 256 * 1024);
            byte[] assetBytes = ReadPinned(root, dataPath, (string)descriptor["asset_sha256"], 256 * 1024);
            JObject json = Parse(jsonBytes);
            RequireKeys(json, "schemaVersion", "kind", "scope", "producer", "unit", "transport", "roleKind", "producerKind", "recipeSlot", "quantity", "sourcePinPaths", "sourcePinSha256");
            RequireIdentityKeys((JObject)json["producer"]);
            RequireIdentityKeys((JObject)json["unit"]);
            RequireIdentityKeys((JObject)json["transport"]);
            var package = JsonConvert.DeserializeObject<OneProducerAuthoredReferencePackage>(json.ToString(Formatting.None))
                ?? throw new InvalidDataException("Authored-reference package JSON did not deserialize");
            try { OneProducerAuthoredReferenceContract.RequireValid(package); }
            catch (Exception error) { throw new InvalidDataException("Authored-reference package shape rejected", error); }

            string prior = null;
            var pinned = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < package.sourcePinPaths.Length; i++)
            {
                string relative = package.sourcePinPaths[i];
                if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative) ||
                    relative.Contains("\\") || relative.Split('/').Any(part => part == ".." || part == "." || part.Length == 0) ||
                    (prior != null && string.CompareOrdinal(prior, relative) >= 0))
                    throw new InvalidDataException("Canonical package source-pin paths required");
                prior = relative;
                byte[] sourceBytes = ReadPinned(root, Path.GetFullPath(Path.Combine(root, relative)), package.sourcePinSha256[i], 32 * 1024 * 1024);
                pinned.Add(relative, new UTF8Encoding(false, true).GetString(sourceBytes));
            }

            string yaml = new UTF8Encoding(false, true).GetString(assetBytes);
            string jsonMeta = GetPinnedText(pinned, JsonRelative + ".meta");
            string scriptMeta = GetPinnedText(pinned, PackageScriptMeta);
            string jsonGuid = MetaGuid(jsonMeta), scriptGuid = MetaGuid(scriptMeta);
            RequireRef(yaml, "packageJson", 4900000, jsonGuid, 3);
            RequireRef(yaml, "producerPrefabReference", package.producer.localId, package.producer.guid, 3);
            RequireRef(yaml, "unitPrefabReference", package.unit.localId, package.unit.guid, 3);
            RequireRef(yaml, "transportPrefabReference", package.transport.localId, package.transport.guid, 3);
            RequireRef(yaml, "producerConfigReference", package.producer.configLocalId, package.producer.configGuid, 2);
            RequireRef(yaml, "unitConfigReference", package.unit.configLocalId, package.unit.configGuid, 2);
            RequireRef(yaml, "transportConfigReference", package.transport.configLocalId, package.transport.configGuid, 2);
            if (!Regex.IsMatch(yaml, @"(?m)^\s*m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*" + Regex.Escape(scriptGuid) + @",\s*type:\s*3\}\s*$"))
                throw new InvalidDataException("Package asset does not bind its exact script identity");
            RequireIdentityMeta(pinned, package.producer);
            RequireIdentityMeta(pinned, package.unit);
            RequireIdentityMeta(pinned, package.transport);
            RequireConfigReference(GetPinnedText(pinned, package.producer.path), package.producer.configLocalId, package.producer.configGuid);
            RequireConfigReference(GetPinnedText(pinned, package.unit.path), package.unit.configLocalId, package.unit.configGuid);
            RequireConfigReference(GetPinnedText(pinned, package.transport.path), package.transport.configLocalId, package.transport.configGuid);
            RequireRef(GetPinnedText(pinned, package.unit.configPath), "productionTransportPrefab",
                package.transport.localId, package.transport.guid, 3);
            RequireRecipeReference(GetPinnedText(pinned, package.producer.path), package);
            RunFocusedControls(package, yaml, GetPinnedText(pinned, package.unit.configPath), root, assetPath);
            return package;
        }

        internal static void RequireCurrentSourceBindings(OneProducerAuthoredReferencePackage package,
            SkirmishResolvedSetup setup, GeneratedUnitProductionMetadata unitMetadata,
            GeneratedBuildingDefinitions sourceDefinitions,
            System.Collections.Generic.IReadOnlyDictionary<SourceUnitPrefab, RuntimeDefinition> definitionsByPrefab)
        {
            OneProducerAuthoredReferenceContract.RequireValid(package);
            var roles = setup?.RoleOverlays?.Where(row => row.RoleKind == SkirmishRoleKind.Rifle).ToArray();
            if (roles == null || roles.Length != 1 || roles[0].Producer != SkirmishProducerKind.Barracks ||
                roles[0].SquadMembers != package.quantity ||
                SkirmishRoleCatalogConfig.RuntimePrefabKey(roles[0].RoleKind) != package.unit.key)
                throw new NotSupportedException("Current compiled setup does not bind the generic package role/producer/quantity");
            var unitRows = unitMetadata?.Ordered?.Where(row => row.Prefab.name == package.unit.key).ToArray();
            if (unitRows == null || unitRows.Length != 1 ||
                !MatchesPrefab(package.unit, unitRows[0].Prefab))
                throw new NotSupportedException("Current source unit identity does not match the generic package");
            var transport = unitRows[0].Production.ProductionTransportPrefab;
            if (!MatchesPrefab(package.transport, transport))
                throw new NotSupportedException("Current source transport identity does not match the generic package");
            if (!sourceDefinitions.Assets.TryGetValue(package.producer.key, out var producer) ||
                !MatchesPrefab(package.producer, producer) || definitionsByPrefab == null ||
                !definitionsByPrefab.TryGetValue(producer, out var definition) || definition == null ||
                definition.Prefab != producer || definition.ProductionSlots == null ||
                package.recipeSlot >= definition.ProductionSlots.Count)
                throw new NotSupportedException("Current source producer identity or recipe slot is unavailable");
            var slot = definition.ProductionSlots[package.recipeSlot];
            if (slot == null || !ReferenceEquals(slot.SpawnUnitPrefab, unitRows[0].Prefab) || slot.Quantity != package.quantity)
                throw new NotSupportedException("Current source producer slot does not bind the packaged unit/quantity");
        }

        static bool MatchesPrefab(AuthoredReferenceIdentity expected, SourceUnitPrefab actual) => actual != null &&
            expected.key == actual.name && expected.path == actual.SourceAssetPath &&
            expected.guid == actual.SourceAssetGuid && expected.localId == actual.SourceAssetLocalId;

        static string FindPin(OneProducerAuthoredReferencePackage package, string path)
        {
            for (int i = 0; i < package.sourcePinPaths.Length; i++) if (package.sourcePinPaths[i] == path) return package.sourcePinSha256[i];
            throw new InvalidDataException("Required package pin missing: " + path);
        }

        static string GetPinnedText(System.Collections.Generic.IDictionary<string, string> pinned, string path) =>
            pinned.TryGetValue(path, out string text) ? text : throw new InvalidDataException("Required authored source pin missing: " + path);

        static void RequireIdentityMeta(System.Collections.Generic.IDictionary<string, string> pinned, AuthoredReferenceIdentity identity)
        {
            if (MetaGuid(GetPinnedText(pinned, identity.path + ".meta")) != identity.guid ||
                MetaGuid(GetPinnedText(pinned, identity.configPath + ".meta")) != identity.configGuid)
                throw new InvalidDataException("Package identity GUID does not match current source meta: " + identity.key);
        }

        static void RequireConfigReference(string yaml, long configLocalId, string configGuid)
        {
            string pattern = @"propertyPath:\s*config\s*\n\s*value:\s*\n\s*objectReference:\s*\{fileID:\s*" + configLocalId.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",\s*guid:\s*" + Regex.Escape(configGuid) + @",\s*type:\s*2\}";
            if (!Regex.IsMatch(yaml, pattern)) throw new InvalidDataException("Current source prefab/config binding differs");
        }

        static void RequireRecipeReference(string yaml, OneProducerAuthoredReferencePackage package)
        {
            string index = package.recipeSlot.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string quantity = @"propertyPath:\s*productions\.Array\.data\[" + index + @"\]\.quantity\s*\n\s*value:\s*" + package.quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string prefab = @"propertyPath:\s*productions\.Array\.data\[" + index + @"\]\.spawnUnitPrefab\s*\n\s*value:\s*\n\s*objectReference:\s*\{fileID:\s*" +
                package.unit.localId.ToString(System.Globalization.CultureInfo.InvariantCulture) + @",\s*guid:\s*" + Regex.Escape(package.unit.guid) + @",\s*type:\s*3\}";
            if (!Regex.IsMatch(yaml, quantity) || !Regex.IsMatch(yaml, prefab))
                throw new InvalidDataException("Current source producer slot/reference/quantity differs from package");
        }

        static string Resolve(string root, string expectedRelative, string supplied)
        {
            string expected = Path.GetFullPath(Path.Combine(root, expectedRelative));
            string actual = Path.GetFullPath(supplied ?? string.Empty);
            if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw new InvalidDataException("Package path differs from configured input: " + expectedRelative);
            return actual;
        }

        static byte[] ReadPinned(string root, string path, string expectedSha, int maximum)
        {
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new InvalidDataException("Package source path escaped repository root");
            var info = new FileInfo(full);
            if (!info.Exists || info.Length <= 0 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular package source file required: " + full);
            byte[] bytes = File.ReadAllBytes(full);
            if (AutonomousWorld.Hash(bytes) != expectedSha) throw new InvalidDataException("Package source pin mismatch: " + full);
            return bytes;
        }

        static JObject Parse(byte[] bytes) => JObject.Parse(new UTF8Encoding(false, true).GetString(bytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });

        static void RequireKeys(JObject row, params string[] keys)
        {
            if (row == null || row.Count != keys.Length || row.Properties().Any(p => !keys.Contains(p.Name)))
                throw new InvalidDataException("Exact package JSON property set required");
        }

        static void RequireIdentityKeys(JObject row) => RequireKeys(row, "key", "path", "guid", "localId", "configPath", "configGuid", "configLocalId", "qualification");
        static string MetaGuid(string text)
        {
            var match = Regex.Match(text ?? string.Empty, @"(?m)^guid:\s*([0-9a-f]{32})\s*$");
            if (!match.Success) throw new InvalidDataException("Exact package asset meta GUID required");
            return match.Groups[1].Value;
        }

        static void RequireRef(string yaml, string field, long localId, string guid, int type)
        {
            string pattern = @"(?m)^\s*" + Regex.Escape(field) + @":\s*\{fileID:\s*" + localId.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                @",\s*guid:\s*" + Regex.Escape(guid) + @",\s*type:\s*" + type.ToString(System.Globalization.CultureInfo.InvariantCulture) + @"\}\s*$";
            if (!Regex.IsMatch(yaml, pattern)) throw new InvalidDataException("Serialized package object reference mismatch: " + field);
        }

        static void RunFocusedControls(OneProducerAuthoredReferencePackage package, string yaml, string unitConfigYaml, string root, string packageJsonPath)
        {
            var wrongScope = Clone(package); wrongScope.scope = "S002ProviderMembership";
            ExpectReject(() => OneProducerAuthoredReferenceContract.RequireValid(wrongScope), "unqualified identity promotion");
            if (OneProducerAuthoredReferenceContract.MatchesIdentity(package.unit, package.unit.key, package.unit.path,
                    package.unit.guid, package.unit.localId + 1, package.unit.configPath, package.unit.configGuid, package.unit.configLocalId))
                throw new InvalidDataException("Package identity mutation control failed");
            ExpectReject(() => RequireRef(yaml, "unitPrefabReference", package.unit.localId + 1, package.unit.guid, 3), "native reference mutation");
            string wrongTransportGuid = unitConfigYaml.Replace(package.transport.guid, new string('0', 32));
            ExpectReject(() => RequireRef(wrongTransportGuid, "productionTransportPrefab",
                package.transport.localId, package.transport.guid, 3), "unit config transport reference mutation");
            ExpectReject(() => ReadPinned(root, packageJsonPath, new string('0', 64), 256 * 1024), "package bytes pin mutation");
            Console.WriteLine("[OneProducerAuthoredReferencePackageControls] result=Passed cases=5 scope=GenericAuthoredReferenceOnly no_membership_promotion=true");
        }

        static OneProducerAuthoredReferencePackage Clone(OneProducerAuthoredReferencePackage source) => new OneProducerAuthoredReferencePackage
        {
            schemaVersion = source.schemaVersion, kind = source.kind, scope = source.scope,
            producer = Clone(source.producer), unit = Clone(source.unit), transport = Clone(source.transport),
            roleKind = source.roleKind, producerKind = source.producerKind, recipeSlot = source.recipeSlot,
            quantity = source.quantity, sourcePinPaths = (string[])source.sourcePinPaths.Clone(),
            sourcePinSha256 = (string[])source.sourcePinSha256.Clone()
        };

        static AuthoredReferenceIdentity Clone(AuthoredReferenceIdentity source) => new AuthoredReferenceIdentity
        {
            key = source.key, path = source.path, guid = source.guid, localId = source.localId,
            configPath = source.configPath, configGuid = source.configGuid, configLocalId = source.configLocalId,
            qualification = source.qualification
        };

        static void ExpectReject(Action action, string label)
        {
            try { action(); }
            catch (Exception) { return; }
            throw new InvalidDataException("Authored-reference negative control accepted: " + label);
        }
    }
}
