using System;
using System.Collections.Generic;
using Game.Operations.Contracts;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// W38 (gap table step 2): mission-keyed Operations runtime registry. A mission is an Operations session when it
    /// is a catalog <c>operation.*</c> ID with a registered native config. Configs live under
    /// <c>Resources/Operations/Missions/&lt;code&gt;</c>; O001 keeps its shipped path (<c>Operations/StreetSignals</c>)
    /// so its asset, GUID, facts and checkpoints stay identical. No mission-ID switch: the only per-mission data here
    /// is the legacy resource path table.
    /// </summary>
    public static class OperationsMissionRegistry
    {
        public const string DefaultMissionId = "operation.o001";
        public const string MissionResourceRoot = "Operations/Missions/";
        private const string IdPrefix = "operation.";
        private static readonly Dictionary<string, string> LegacyResourcePaths = new(StringComparer.Ordinal)
        {
            ["operation.o001"] = "Operations/StreetSignals",
        };
        private static readonly Dictionary<string, OperationsReconMissionConfig> Loaded = new(StringComparer.Ordinal);

        public static bool TryGetCatalogEntry(string missionId, out OperationsCatalogEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(missionId)) return false;
            foreach (var candidate in OperationsCatalogIndex.Entries)
                if (string.Equals(candidate.MissionId, missionId, StringComparison.Ordinal)) { entry = candidate; return true; }
            return false;
        }

        /// <summary>"operation.o002" -> "O002"; null for a non-Operations ID.</summary>
        public static string Code(string missionId) =>
            !string.IsNullOrEmpty(missionId) && missionId.StartsWith(IdPrefix, StringComparison.Ordinal) && missionId.Length > IdPrefix.Length
                ? missionId.Substring(IdPrefix.Length).ToUpperInvariant() : null;

        /// <summary>The localization prefix for a mission's own copy ("operations.o001.").</summary>
        public static string CopyPrefix(string missionId) => "operations." + (Code(missionId) ?? Code(DefaultMissionId)).ToLowerInvariant() + ".";

        public static string ResourcePath(string missionId)
        {
            if (!TryGetCatalogEntry(missionId, out _)) return null;
            return LegacyResourcePaths.TryGetValue(missionId, out string legacy) ? legacy : MissionResourceRoot + Code(missionId);
        }

        /// <summary>The registered native config for a catalog mission, or null (cached per mission ID).</summary>
        public static OperationsReconMissionConfig Load(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return null;
            if (Loaded.TryGetValue(missionId, out var cached) && cached != null) return cached;
            string path = ResourcePath(missionId);
            var config = path == null ? null : Resources.Load<OperationsReconMissionConfig>(path);
            if (config != null && !string.Equals(config.missionId, missionId, StringComparison.Ordinal)) config = null;
            if (config != null) Loaded[missionId] = config;
            return config;
        }

        public static bool IsRegistered(string missionId) => Load(missionId) != null;

        /// <summary>The seed a new Operations run starts from: the selected mission's catalog canonical seed.</summary>
        public static uint NewRunSeed(string missionId) =>
            TryGetCatalogEntry(missionId, out var entry) && entry.CanonicalSeed > 0 ? (uint)entry.CanonicalSeed : 1102u;
    }
}
