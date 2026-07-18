using System;

namespace Game.Editor
{
    internal readonly struct OperationMapAddressablesBuildReport
    {
        public OperationMapAddressablesBuildReport(
            int schemaVersion,
            string buildResultHash,
            string buildTarget,
            ulong aggregateBundleBytes,
            OperationMapAddressablesBuildMapReport[] maps,
            OperationMapAddressablesPartitionReport[] partitions,
            string[] requiredAddresses,
            OperationMapAddressablesEntitiesArtifactReport[] entitiesArtifacts,
            OperationMapAddressablesDuplicateDependencyReport[] duplicateDependencies)
        {
            SchemaVersion = schemaVersion;
            BuildResultHash = buildResultHash ?? string.Empty;
            BuildTarget = buildTarget ?? string.Empty;
            AggregateBundleBytes = aggregateBundleBytes;
            Maps = maps ?? Array.Empty<OperationMapAddressablesBuildMapReport>();
            Partitions = partitions ?? Array.Empty<OperationMapAddressablesPartitionReport>();
            RequiredAddresses = requiredAddresses ?? Array.Empty<string>();
            EntitiesArtifacts = entitiesArtifacts ?? Array.Empty<OperationMapAddressablesEntitiesArtifactReport>();
            DuplicateDependencies = duplicateDependencies ?? Array.Empty<OperationMapAddressablesDuplicateDependencyReport>();
        }

        public int SchemaVersion { get; }
        public string BuildResultHash { get; }
        public string BuildTarget { get; }
        public ulong AggregateBundleBytes { get; }
        public OperationMapAddressablesBuildMapReport[] Maps { get; }
        public OperationMapAddressablesPartitionReport[] Partitions { get; }
        public string[] RequiredAddresses { get; }
        public OperationMapAddressablesEntitiesArtifactReport[] EntitiesArtifacts { get; }
        public OperationMapAddressablesDuplicateDependencyReport[] DuplicateDependencies { get; }
    }

    internal readonly struct OperationMapAddressablesBuildMapReport
    {
        public OperationMapAddressablesBuildMapReport(string mapId, int bundleCount, ulong bundleBytes)
        {
            MapId = mapId ?? string.Empty;
            BundleCount = bundleCount;
            BundleBytes = bundleBytes;
        }

        public string MapId { get; }
        public int BundleCount { get; }
        public ulong BundleBytes { get; }
    }

    internal readonly struct OperationMapAddressablesPartitionReport
    {
        public OperationMapAddressablesPartitionReport(string label, int entryCount, int bundleCount)
        {
            Label = label ?? string.Empty;
            EntryCount = entryCount;
            BundleCount = bundleCount;
        }

        public string Label { get; }
        public int EntryCount { get; }
        public int BundleCount { get; }
    }

    internal readonly struct OperationMapAddressablesEntitiesArtifactReport
    {
        public OperationMapAddressablesEntitiesArtifactReport(string identity, ulong bytes)
        {
            Identity = identity ?? string.Empty;
            Bytes = bytes;
        }

        public string Identity { get; }
        public ulong Bytes { get; }
    }

    internal readonly struct OperationMapAddressablesDuplicateDependencyReport
    {
        public OperationMapAddressablesDuplicateDependencyReport(
            string assetGuid,
            string assetPath,
            int bundleCount,
            ulong duplicateBytes)
        {
            AssetGuid = assetGuid ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            BundleCount = bundleCount;
            DuplicateBytes = duplicateBytes;
        }

        public string AssetGuid { get; }
        public string AssetPath { get; }
        public int BundleCount { get; }
        public ulong DuplicateBytes { get; }
    }
}
