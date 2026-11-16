using System;

namespace Game.Simulation
{
    [Serializable]
    public sealed class AuthoredReferenceIdentity
    {
        public string key;
        public string path;
        public string guid;
        public long localId;
        public string configPath;
        public string configGuid;
        public long configLocalId;
        public string qualification;
    }

    [Serializable]
    public sealed class OneProducerAuthoredReferencePackage
    {
        public int schemaVersion;
        public string kind;
        public string scope;
        public AuthoredReferenceIdentity producer;
        public AuthoredReferenceIdentity unit;
        public AuthoredReferenceIdentity transport;
        public string roleKind;
        public string producerKind;
        public int recipeSlot;
        public int quantity;
        public string[] sourcePinPaths;
        public string[] sourcePinSha256;
    }

    public static class OneProducerAuthoredReferenceContract
    {
        public const string PackageKind = "OneProducerAuthoredReferencePackage";
        public const string GenericScope = "GenericAuthoredReferenceOnly";

        public static void RequireValid(OneProducerAuthoredReferencePackage package)
        {
            if (package == null || package.schemaVersion != 1 || package.kind != PackageKind ||
                package.scope != GenericScope || package.roleKind != "Rifle" ||
                package.producerKind != "Barracks" || package.recipeSlot < 0 || package.quantity <= 0)
                throw new InvalidOperationException("Exact generic one-producer authored-reference package required");
            RequireIdentity(package.producer, "Building_Barrack");
            RequireIdentity(package.unit, "Unit_Chr_Soldier_Male_02_Alt_04");
            RequireIdentity(package.transport, "Unit_Veh_Helicopter_Transport");
            if (package.sourcePinPaths == null || package.sourcePinSha256 == null ||
                package.sourcePinPaths.Length == 0 || package.sourcePinPaths.Length != package.sourcePinSha256.Length)
                throw new InvalidOperationException("Exact source-pin rows required");
            string prior = null;
            for (int i = 0; i < package.sourcePinPaths.Length; i++)
            {
                string path = package.sourcePinPaths[i];
                string hash = package.sourcePinSha256[i];
                if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    path.Contains("..") || path.Contains("\\") ||
                    (prior != null && string.CompareOrdinal(prior, path) >= 0) ||
                    !IsLowerHex(hash, 64))
                    throw new InvalidOperationException("Canonical, sorted package source pins required");
                prior = path;
            }
        }

        public static bool MatchesIdentity(AuthoredReferenceIdentity expected, string key, string path,
            string guid, long localId, string configPath, string configGuid, long configLocalId)
        {
            return expected != null && expected.key == key && expected.path == path &&
                expected.guid == guid && expected.localId != 0 && expected.localId == localId &&
                expected.configPath == configPath && expected.configGuid == configGuid &&
                expected.configLocalId != 0 && expected.configLocalId == configLocalId;
        }

        public static void RequireIdentity(AuthoredReferenceIdentity identity, string key)
        {
            if (identity == null || identity.key != key || string.IsNullOrWhiteSpace(identity.path) ||
                !identity.path.StartsWith("Assets/", StringComparison.Ordinal) || identity.path.Contains("..") ||
                identity.path.Contains("\\") || !IsLowerHex(identity.guid, 32) || identity.localId == 0 ||
                string.IsNullOrWhiteSpace(identity.configPath) || !identity.configPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                identity.configPath.Contains("..") || identity.configPath.Contains("\\") ||
                !IsLowerHex(identity.configGuid, 32) || identity.configLocalId == 0 ||
                identity.qualification != "GenericAuthoredReferenceWitness")
                throw new InvalidOperationException("Exact generic authored source identity required for " + key);
        }

        private static bool IsLowerHex(string value, int length)
        {
            if (value == null || value.Length != length) return false;
            for (int i = 0; i < value.Length; i++)
                if (!(value[i] >= '0' && value[i] <= '9' || value[i] >= 'a' && value[i] <= 'f')) return false;
            return true;
        }
    }
}
