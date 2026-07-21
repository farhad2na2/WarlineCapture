#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Pure Phase 0A planner that maps accepted static render-only migration owners onto the
    /// protected candidate <c>RenderOnly/*</c> buckets. Uses only the authored <c>Map</c> child
    /// folder identity from <c>nameHierarchyPath</c>; never classifies by leaf object name,
    /// prefab filename, proximity, or renderer shape. Does not open or mutate scenes.
    /// </summary>
    internal static class OperationMapRenderOnlyCandidateMigrationPlanner
    {
        internal const string PlanSchema = "warline.operation-map.render-only-candidate-migration-plan";
        internal const int PlanSchemaVersion = 1;

        internal const string BucketTerrain = "Terrain";
        internal const string BucketRoadsAndBridges = "RoadsAndBridges";
        internal const string BucketMountains = "Mountains";
        internal const string BucketVegetation = "Vegetation";
        internal const string BucketProps = "Props";
        internal const string BucketInfrastructure = "Infrastructure";
        internal const string BucketHorizon = "Horizon";

        private static readonly string[] ApprovedBuckets =
        {
            BucketTerrain,
            BucketRoadsAndBridges,
            BucketMountains,
            BucketVegetation,
            BucketProps,
            BucketInfrastructure,
            BucketHorizon
        };

        /// <summary>
        /// Explicit authored Map-child folder → candidate RenderOnly bucket table.
        /// Unknown folders fail closed.
        /// </summary>
        private static readonly Dictionary<string, string> MapChildFolderToBucket =
            new(StringComparer.Ordinal)
            {
                ["Ground"] = BucketTerrain,
                ["GroundHills"] = BucketTerrain,
                ["Beaches"] = BucketTerrain,
                ["Roads"] = BucketRoadsAndBridges,
                ["Bridges"] = BucketRoadsAndBridges,
                ["Docks"] = BucketRoadsAndBridges,
                ["Runways"] = BucketRoadsAndBridges,
                ["Mountains"] = BucketMountains,
                ["Plants"] = BucketVegetation,
                ["Grass"] = BucketVegetation,
                ["Trees"] = BucketVegetation,
                ["Bushes"] = BucketVegetation,
                ["Props"] = BucketProps,
                ["Rocks"] = BucketProps,
                ["Items"] = BucketProps,
                ["Ruins"] = BucketProps,
                ["Weapons"] = BucketProps,
                ["ResourceAreas"] = BucketProps,
                // Static-package duplicates retained as render-only until gameplay parity proves omission.
                ["_UnmappedBuildings"] = BucketProps,
                ["_UnmappedVehicleSources"] = BucketProps,
                ["Clouds"] = BucketHorizon,
                ["Skydome"] = BucketHorizon
            };

        internal static bool TryAssignBucket(
            string nameHierarchyPath,
            out string mapChildFolder,
            out string bucket,
            out string rejectionReason)
        {
            mapChildFolder = null;
            bucket = null;
            rejectionReason = null;

            if (string.IsNullOrWhiteSpace(nameHierarchyPath))
            {
                rejectionReason = "nameHierarchyPath-empty";
                return false;
            }

            string[] parts = nameHierarchyPath.Split('/');
            if (parts.Length < 2 ||
                !string.Equals(parts[0], "Map", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(parts[1]))
            {
                rejectionReason = "nameHierarchyPath-not-under-Map-child-folder";
                return false;
            }

            mapChildFolder = parts[1];
            if (!MapChildFolderToBucket.TryGetValue(mapChildFolder, out bucket))
            {
                rejectionReason = $"unapproved-map-child-folder:{mapChildFolder}";
                return false;
            }

            return true;
        }

        internal static bool TryCreatePlan(
            IReadOnlyList<OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport> owners,
            out OperationMapRenderOnlyCandidateMigrationPlan plan,
            out string rejectionReason)
        {
            plan = default;
            rejectionReason = null;

            if (owners == null)
            {
                rejectionReason = "owners-null";
                return false;
            }

            var assignments = new List<OperationMapRenderOnlyCandidateAssignment>(owners.Count);
            var ownerIds = new HashSet<string>(StringComparer.Ordinal);
            var countsByBucket = ApprovedBuckets.ToDictionary(bucket => bucket, _ => 0, StringComparer.Ordinal);
            var countsByMapChildFolder = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < owners.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport owner = owners[i];
                if (owner == null)
                {
                    rejectionReason = $"owner[{i}]-null";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(owner.globalObjectId) ||
                    !owner.globalObjectId.StartsWith("GlobalObjectId_V1-", StringComparison.Ordinal))
                {
                    rejectionReason = $"owner[{i}]-globalObjectId-invalid";
                    return false;
                }

                if (!ownerIds.Add(owner.globalObjectId))
                {
                    rejectionReason = $"duplicate-owner-globalObjectId:{owner.globalObjectId}";
                    return false;
                }

                if (!string.Equals(owner.candidateDisposition, "RenderOnlyEntityCandidate", StringComparison.Ordinal))
                {
                    rejectionReason = $"owner[{i}]-disposition-not-render-only:{owner.candidateDisposition}";
                    return false;
                }

                if (!TryAssignBucket(
                        owner.nameHierarchyPath,
                        out string mapChildFolder,
                        out string bucket,
                        out rejectionReason))
                {
                    rejectionReason = $"owner[{i}]:{rejectionReason}";
                    return false;
                }

                assignments.Add(
                    new OperationMapRenderOnlyCandidateAssignment(
                        owner.globalObjectId,
                        owner.nameHierarchyPath,
                        mapChildFolder,
                        bucket));
                countsByBucket[bucket]++;
                countsByMapChildFolder.TryGetValue(mapChildFolder, out int folderCount);
                countsByMapChildFolder[mapChildFolder] = folderCount + 1;
            }

            assignments.Sort(
                (left, right) => string.CompareOrdinal(left.SourceOwnerGlobalObjectId, right.SourceOwnerGlobalObjectId));

            plan = new OperationMapRenderOnlyCandidateMigrationPlan(
                PlanSchema,
                PlanSchemaVersion,
                "RenderOnlyCopyPlanReadyPendingCandidateHierarchy",
                assignments.Count,
                countsByBucket
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => new OperationMapRenderOnlyBucketCount(entry.Key, entry.Value))
                    .ToArray(),
                countsByMapChildFolder
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => new OperationMapRenderOnlyBucketCount(entry.Key, entry.Value))
                    .ToArray(),
                assignments);
            return true;
        }
    }

    internal readonly struct OperationMapRenderOnlyCandidateAssignment
    {
        public OperationMapRenderOnlyCandidateAssignment(
            string sourceOwnerGlobalObjectId,
            string nameHierarchyPath,
            string mapChildFolder,
            string destinationBucket)
        {
            SourceOwnerGlobalObjectId = sourceOwnerGlobalObjectId;
            NameHierarchyPath = nameHierarchyPath;
            MapChildFolder = mapChildFolder;
            DestinationBucket = destinationBucket;
        }

        public string SourceOwnerGlobalObjectId { get; }
        public string NameHierarchyPath { get; }
        public string MapChildFolder { get; }
        public string DestinationBucket { get; }
    }

    internal readonly struct OperationMapRenderOnlyBucketCount
    {
        public OperationMapRenderOnlyBucketCount(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; }
        public int Count { get; }
    }

    internal readonly struct OperationMapRenderOnlyCandidateMigrationPlan
    {
        public OperationMapRenderOnlyCandidateMigrationPlan(
            string schema,
            int schemaVersion,
            string status,
            int ownerCount,
            IReadOnlyList<OperationMapRenderOnlyBucketCount> countsByBucket,
            IReadOnlyList<OperationMapRenderOnlyBucketCount> countsByMapChildFolder,
            IReadOnlyList<OperationMapRenderOnlyCandidateAssignment> assignments)
        {
            Schema = schema;
            SchemaVersion = schemaVersion;
            Status = status;
            OwnerCount = ownerCount;
            CountsByBucket = countsByBucket;
            CountsByMapChildFolder = countsByMapChildFolder;
            Assignments = assignments;
        }

        public string Schema { get; }
        public int SchemaVersion { get; }
        public string Status { get; }
        public int OwnerCount { get; }
        public IReadOnlyList<OperationMapRenderOnlyBucketCount> CountsByBucket { get; }
        public IReadOnlyList<OperationMapRenderOnlyBucketCount> CountsByMapChildFolder { get; }
        public IReadOnlyList<OperationMapRenderOnlyCandidateAssignment> Assignments { get; }
    }
}

#endif
