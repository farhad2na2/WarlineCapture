#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Configs;
    using UnityEngine;

    /// <summary>
    /// Non-mutating Phase 0A scaffolding for immutable migration records and deterministic hashing.
    /// Does not open, save, or mutate scenes, Addressables, static presentation, or rollback artifacts.
    /// </summary>
    internal static class OperationMapEntityPresentationMigrationEditor
    {
        internal const string RecordSchema = "warline.operation-map.entity-presentation-migration-record";
        internal const int RecordSchemaVersion = 1;

        internal const string RoleGameplayBuildings = "GameplayBuildings";
        internal const string RoleGameplayVehicles = "GameplayVehicles";
        internal const string RoleRenderOnly = "RenderOnly";
        internal const string AcceptedSubScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity";
        internal const string CandidateSubScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_candidate.unity";
        internal const string StaticOwnerDecisionIdentity =
            "gpt56-phase0a-static-owner-review-2026-07-21";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        private static readonly HashSet<string> ApprovedRoles = new(StringComparer.Ordinal)
        {
            RoleGameplayBuildings,
            RoleGameplayVehicles,
            RoleRenderOnly
        };

        private static readonly HashSet<string> ApprovedStaticDependencyDispositions =
            new(StringComparer.Ordinal)
            {
                "BakeEntitiesGraphics",
                "BakeEntitiesGraphicsLod",
                "BakeEntityTransform",
                "OmitInertAnimatorWithoutController"
            };

        internal static bool TryEvaluateMutationReadiness(
            int gameplayBuildingCount,
            int gameplayVehicleCount,
            int renderOnlyEntityCount,
            int rejectedUnresolvedCount,
            int vehicleAlreadyReadyCount,
            int vehicleCleanupRequiredCount,
            int buildingAttachmentOrphanCount,
            int buildingAttachmentSharedCount,
            int buildingAttachmentDualStateCount,
            out OperationMapEntityPresentationMutationReadiness readiness,
            out string rejectionReason)
        {
            readiness = OperationMapEntityPresentationMutationReadiness.NotReady;
            rejectionReason = null;

            if (rejectedUnresolvedCount != 0)
            {
                rejectionReason = "rejected-unresolved-owners-present";
                return false;
            }

            if (gameplayBuildingCount <= 0)
            {
                rejectionReason = "gameplay-building-owners-missing";
                return false;
            }

            if (gameplayVehicleCount <= 0)
            {
                rejectionReason = "gameplay-vehicle-owners-missing";
                return false;
            }

            if (renderOnlyEntityCount <= 0)
            {
                rejectionReason = "render-only-owners-missing";
                return false;
            }

            if (vehicleCleanupRequiredCount != 0)
            {
                rejectionReason = "vehicle-ecs-cleanup-required";
                return false;
            }

            if (vehicleAlreadyReadyCount != gameplayVehicleCount)
            {
                rejectionReason = "vehicle-ecs-ready-count-mismatch";
                return false;
            }

            if (buildingAttachmentOrphanCount != 0)
            {
                rejectionReason = "building-attachment-orphans-present";
                return false;
            }

            if (buildingAttachmentSharedCount != 0)
            {
                rejectionReason = "building-attachment-shared-claims-present";
                return false;
            }

            if (buildingAttachmentDualStateCount != 0)
            {
                rejectionReason = "building-attachment-dual-state-present";
                return false;
            }

            readiness =
                OperationMapEntityPresentationMutationReadiness.CandidateTransactionReadyPendingMutation;
            return true;
        }

        public static void RunDryRunPlan()
        {
            OperationMapEntityPresentationMigrationPlan plan = CreateCurrentDryRunPlan(out string reportPath);

            Debug.Log(
                $"[OperationMapEntityPresentationMigrationEditor] " +
                $"status={plan.Status} staticOwners={plan.Records.Count} " +
                $"recordSetHash={plan.RecordSetHash} " +
                $"placementJoinSetHash={plan.PlacementJoinSetHash} report={reportPath}");
        }

        internal static OperationMapEntityPresentationMigrationPlan CreateCurrentDryRunPlan(
            out string reportPath)
        {
            reportPath =
                Environment.GetEnvironmentVariable(
                    OperationMapEntityPresentationMigrationInventoryProbe.ReportPathEnvironmentVariable) ??
                OperationMapEntityPresentationMigrationInventoryProbe.DefaultReportPath;
            if (!Path.IsPathRooted(reportPath))
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                reportPath = Path.GetFullPath(Path.Combine(projectRoot, reportPath));
            }

            if (!File.Exists(reportPath))
                throw new FileNotFoundException("Migration inventory report is missing.", reportPath);

            string json = File.ReadAllText(reportPath, Utf8WithoutBom);
            if (!OperationMapEntityPresentationMigrationInventoryProbe.HasRequiredReportShape(json))
                throw new InvalidOperationException("Migration inventory report shape is invalid.");

            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report =
                JsonUtility.FromJson<
                    OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport>(json);
            if (!TryCreateDryRunCandidatePlan(
                    report,
                    CandidateSubScenePath,
                    StaticOwnerDecisionIdentity,
                    out OperationMapEntityPresentationMigrationPlan plan,
                    out string rejectionReason))
            {
                throw new InvalidOperationException(
                    $"Migration dry-run plan rejected: {rejectionReason}");
            }

            return plan;
        }

        internal static bool TryCreateRecord(
            string sourceScenePath,
            string sourceOwnerGlobalObjectId,
            string sourceOwnerHierarchyPath,
            string approvedRole,
            string prefabAssetGuid,
            long prefabLocalId,
            string sourceRendererPayloadCanonical,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 worldScale,
            string componentDispositionCanonical,
            string destinationSubScenePath,
            string destinationStableIdentity,
            string placementConfigIdentitiesCanonical,
            string rollbackChunkIdsCanonical,
            string rollbackManifestPath,
            string rollbackManifestContentHash,
            string rollbackCanonicalSceneDependencyHash,
            string decisionOwner,
            out OperationMapEntityPresentationMigrationRecord record,
            out string rejectionReason)
        {
            record = default;
            rejectionReason = null;

            if (!TryRequireRepositoryRelativePath(sourceScenePath, "sourceScenePath", out rejectionReason) ||
                !TryRequireGlobalObjectId(
                    sourceOwnerGlobalObjectId,
                    "sourceOwnerGlobalObjectId",
                    out rejectionReason) ||
                !TryRequireNonEmpty(
                    sourceOwnerHierarchyPath,
                    "sourceOwnerHierarchyPath",
                    out rejectionReason) ||
                !TryRequireApprovedRole(approvedRole, out rejectionReason) ||
                !TryRequireOptionalAssetGuid(prefabAssetGuid, "prefabAssetGuid", out rejectionReason) ||
                !TryRequireCanonicalSet(
                    sourceRendererPayloadCanonical,
                    "sourceRendererPayloadCanonical",
                    '\n',
                    required: true,
                    out rejectionReason) ||
                !TryRequireFinite(worldPosition, "worldPosition", out rejectionReason) ||
                !TryRequireFinite(worldRotation, "worldRotation", out rejectionReason) ||
                !TryRequireFinite(worldScale, "worldScale", out rejectionReason) ||
                !TryRequireCanonicalSet(
                    componentDispositionCanonical,
                    "componentDispositionCanonical",
                    '|',
                    required: true,
                    out rejectionReason) ||
                !TryRequireRepositoryRelativePath(
                    destinationSubScenePath,
                    "destinationSubScenePath",
                    out rejectionReason) ||
                !TryRequireNonEmpty(destinationStableIdentity, "destinationStableIdentity", out rejectionReason) ||
                !TryRequireCanonicalSet(
                    placementConfigIdentitiesCanonical,
                    "placementConfigIdentitiesCanonical",
                    '\n',
                    required: false,
                    out rejectionReason) ||
                !TryRequireCanonicalSet(
                    rollbackChunkIdsCanonical,
                    "rollbackChunkIdsCanonical",
                    '\n',
                    required: true,
                    out rejectionReason) ||
                !TryRequireRepositoryRelativePath(
                    rollbackManifestPath,
                    "rollbackManifestPath",
                    out rejectionReason) ||
                !TryRequireLowerHex(
                    rollbackManifestContentHash,
                    "rollbackManifestContentHash",
                    32,
                    out rejectionReason) ||
                !TryRequireLowerHex(
                    rollbackCanonicalSceneDependencyHash,
                    "rollbackCanonicalSceneDependencyHash",
                    32,
                    out rejectionReason) ||
                !TryRequireNonEmpty(decisionOwner, "decisionOwner", out rejectionReason))
            {
                return false;
            }

            if (prefabLocalId < 0)
            {
                rejectionReason = "prefabLocalId-negative";
                return false;
            }

            if (string.IsNullOrEmpty(prefabAssetGuid) != (prefabLocalId == 0))
            {
                rejectionReason = "prefabAssetGuid-localId-pair-invalid";
                return false;
            }

            record = new OperationMapEntityPresentationMigrationRecord(
                sourceScenePath,
                sourceOwnerGlobalObjectId,
                sourceOwnerHierarchyPath,
                approvedRole,
                prefabAssetGuid ?? string.Empty,
                prefabLocalId,
                sourceRendererPayloadCanonical,
                worldPosition,
                worldRotation,
                worldScale,
                componentDispositionCanonical,
                destinationSubScenePath,
                destinationStableIdentity,
                placementConfigIdentitiesCanonical ?? string.Empty,
                rollbackChunkIdsCanonical,
                rollbackManifestPath,
                rollbackManifestContentHash,
                rollbackCanonicalSceneDependencyHash,
                decisionOwner);
            return true;
        }

        internal static bool TryCreateDryRunCandidatePlan(
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
            string destinationSubScenePath,
            string decisionOwner,
            out OperationMapEntityPresentationMigrationPlan plan,
            out string rejectionReason)
        {
            plan = null;
            rejectionReason = null;

            if (report == null)
            {
                rejectionReason = "inventory-null";
                return false;
            }

            if (!string.Equals(
                    report.reportSchema,
                    OperationMapEntityPresentationMigrationInventoryProbe.ReportSchema,
                    StringComparison.Ordinal) ||
                report.reportSchemaVersion !=
                    OperationMapEntityPresentationMigrationInventoryProbe.ReportSchemaVersion ||
                !string.Equals(report.result, "InventoryCompletePendingReview", StringComparison.Ordinal))
            {
                rejectionReason = "inventory-schema-version-or-result-rejected";
                return false;
            }

            if (!TryRequireRepositoryRelativePath(
                    destinationSubScenePath,
                    "destinationSubScenePath",
                    out rejectionReason) ||
                !TryRequireNonEmpty(decisionOwner, "decisionOwner", out rejectionReason))
            {
                return false;
            }

            if (report.counts == null ||
                report.manifest == null ||
                report.owners == null ||
                report.sources == null ||
                report.placementJoins == null ||
                report.classificationCounts == null)
            {
                rejectionReason = "inventory-required-section-null";
                return false;
            }

            if (!TryValidateInventoryCounts(report, out rejectionReason) ||
                !TryValidateManifest(report.manifest, out rejectionReason))
            {
                return false;
            }

            var ownersById =
                new Dictionary<string, OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>(
                    StringComparer.Ordinal);
            var ownerHierarchyPaths = new List<string>(report.owners.Count);
            var rendererPayloadsByOwner = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var rollbackChunksByOwner = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            for (int i = 0; i < report.owners.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport owner =
                    report.owners[i];
                if (owner == null ||
                    !TryRequireGlobalObjectId(owner.globalObjectId, "owner.globalObjectId", out rejectionReason) ||
                    !TryRequireCanonicalAtom(owner.hierarchyPath, "owner.hierarchyPath", out rejectionReason) ||
                    !TryRequireOptionalAssetGuid(
                        owner.prefabAssetGuid,
                        "owner.prefabAssetGuid",
                        out rejectionReason) ||
                    owner.prefabLocalId < 0 ||
                    string.IsNullOrEmpty(owner.prefabAssetGuid) != (owner.prefabLocalId == 0) ||
                    !TryRequireFinite(owner.worldPosition, "owner.worldPosition", out rejectionReason) ||
                    !TryRequireFinite(owner.worldRotation, "owner.worldRotation", out rejectionReason) ||
                    !TryRequireFinite(owner.worldScale, "owner.worldScale", out rejectionReason) ||
                    owner.sourceRendererCount <= 0 ||
                    owner.hierarchyObjectCount <= 0 ||
                    owner.blockingDependencyCount != 0 ||
                    owner.externalSceneReferenceCount != 0 ||
                    owner.componentTypes == null ||
                    owner.externalSceneReferences == null ||
                    owner.externalSceneReferences.Count != 0 ||
                    !string.Equals(
                        owner.candidateDisposition,
                        "RenderOnlyEntityCandidate",
                        StringComparison.Ordinal) ||
                    owner.dispositionCounts == null)
                {
                    rejectionReason ??= $"owner[{i}]-invalid-or-blocked";
                    return false;
                }

                if (!ownersById.TryAdd(owner.globalObjectId, owner))
                {
                    rejectionReason = $"duplicate-owner-globalObjectId:{owner.globalObjectId}";
                    return false;
                }

                ownerHierarchyPaths.Add(owner.hierarchyPath);
                rendererPayloadsByOwner.Add(owner.globalObjectId, new List<string>(owner.sourceRendererCount));
                rollbackChunksByOwner.Add(
                    owner.globalObjectId,
                    new HashSet<string>(StringComparer.Ordinal));
            }

            if (ownersById.Count != report.counts.migrationOwnerCount ||
                HasDuplicateOrNestedHierarchy(ownerHierarchyPaths, out rejectionReason))
            {
                rejectionReason ??= "owner-count-mismatch";
                return false;
            }

            var sourcesById =
                new Dictionary<string, OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport>(
                    StringComparer.Ordinal);
            var sourceIndices = new HashSet<int>();
            var chunkIds = new HashSet<string>(StringComparer.Ordinal);
            var actualClassificationCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < report.sources.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport source =
                    report.sources[i];
                if (!TryValidateSource(source, out rejectionReason))
                {
                    rejectionReason = $"source[{i}]:{rejectionReason}";
                    return false;
                }

                if (!sourceIndices.Add(source.sourceIndex) ||
                    source.sourceIndex < 0 ||
                    source.sourceIndex >= report.counts.sourceCount)
                {
                    rejectionReason = $"source-index-invalid-or-duplicate:{source.sourceIndex}";
                    return false;
                }

                if (!sourcesById.TryAdd(source.sourceGlobalObjectId, source))
                {
                    rejectionReason = $"duplicate-source-globalObjectId:{source.sourceGlobalObjectId}";
                    return false;
                }

                if (!ownersById.ContainsKey(source.migrationOwnerGlobalObjectId))
                {
                    rejectionReason =
                        $"source-owner-missing:{source.migrationOwnerGlobalObjectId}";
                    return false;
                }

                string rendererPayload = BuildRendererPayload(source);
                rendererPayloadsByOwner[source.migrationOwnerGlobalObjectId].Add(rendererPayload);
                rollbackChunksByOwner[source.migrationOwnerGlobalObjectId].Add(source.chunkId);
                chunkIds.Add(source.chunkId);
                actualClassificationCounts.TryGetValue(source.classification, out int count);
                actualClassificationCounts[source.classification] = count + 1;
            }

            if (sourcesById.Count != report.counts.sourceCount ||
                sourceIndices.Count != report.counts.sourceCount ||
                chunkIds.Count != report.counts.chunkCount ||
                !ClassificationCountsAgree(report, actualClassificationCounts, out rejectionReason))
            {
                rejectionReason ??= "source-or-chunk-count-mismatch";
                return false;
            }

            foreach (KeyValuePair<string, OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>
                         entry in ownersById)
            {
                if (rendererPayloadsByOwner[entry.Key].Count != entry.Value.sourceRendererCount)
                {
                    rejectionReason = $"owner-source-count-mismatch:{entry.Key}";
                    return false;
                }
            }

            if (!TryValidatePlacementJoins(
                    report,
                    out string placementJoinSetHash,
                    out rejectionReason))
            {
                return false;
            }

            var records = new List<OperationMapEntityPresentationMigrationRecord>(ownersById.Count);
            foreach (string ownerId in ownersById.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport owner =
                    ownersById[ownerId];
                List<string> rendererPayloads = rendererPayloadsByOwner[ownerId];
                rendererPayloads.Sort(StringComparer.Ordinal);
                if (rendererPayloads.Distinct(StringComparer.Ordinal).Count() != rendererPayloads.Count)
                {
                    rejectionReason = $"duplicate-owner-renderer-payload:{ownerId}";
                    return false;
                }

                string rollbackChunksCanonical = string.Join(
                    "\n",
                    rollbackChunksByOwner[ownerId].OrderBy(value => value, StringComparer.Ordinal));
                if (!TryBuildComponentDisposition(owner, out string disposition, out rejectionReason))
                {
                    rejectionReason = $"owner-disposition:{ownerId}:{rejectionReason}";
                    return false;
                }

                if (!TryCreateRecord(
                        report.manifest.canonicalScenePath,
                        owner.globalObjectId,
                        owner.hierarchyPath,
                        RoleRenderOnly,
                        owner.prefabAssetGuid,
                        owner.prefabLocalId,
                        string.Join("\n", rendererPayloads),
                        owner.worldPosition,
                        owner.worldRotation,
                        owner.worldScale,
                        disposition,
                        destinationSubScenePath,
                        $"entity-presentation:{owner.globalObjectId}",
                        string.Empty,
                        rollbackChunksCanonical,
                        report.manifest.path,
                        report.manifest.contentHash,
                        report.manifest.canonicalSceneDependencyHash,
                        decisionOwner,
                        out OperationMapEntityPresentationMigrationRecord record,
                        out rejectionReason))
                {
                    rejectionReason = $"owner-record:{ownerId}:{rejectionReason}";
                    return false;
                }

                records.Add(record);
            }

            string recordSetHash = ComputeOrderedRecordSetHash(records);
            plan = new OperationMapEntityPresentationMigrationPlan(
                records,
                recordSetHash,
                placementJoinSetHash);
            return true;
        }

        internal static string ComputeRecordHash(in OperationMapEntityPresentationMigrationRecord record)
        {
            if (!TryValidateRecord(record, out string rejectionReason))
                throw new InvalidOperationException($"Cannot hash invalid migration record: {rejectionReason}");

            var builder = new StringBuilder(1024);
            builder.Append(RecordSchema).Append('\n')
                .Append(RecordSchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            AppendField(builder, "sourceScenePath", record.SourceScenePath);
            AppendField(builder, "sourceOwnerGlobalObjectId", record.SourceOwnerGlobalObjectId);
            AppendField(builder, "sourceOwnerHierarchyPath", record.SourceOwnerHierarchyPath);
            AppendField(builder, "approvedRole", record.ApprovedRole);
            AppendField(builder, "prefabAssetGuid", record.PrefabAssetGuid);
            AppendField(
                builder,
                "prefabLocalId",
                record.PrefabLocalId.ToString(CultureInfo.InvariantCulture));
            AppendField(
                builder,
                "sourceRendererPayloadCanonical",
                record.SourceRendererPayloadCanonical);
            AppendVectorBits(builder, "worldPosition", record.WorldPosition);
            AppendQuaternionBits(builder, "worldRotation", record.WorldRotation);
            AppendVectorBits(builder, "worldScale", record.WorldScale);
            AppendField(builder, "componentDispositionCanonical", record.ComponentDispositionCanonical);
            AppendField(builder, "destinationSubScenePath", record.DestinationSubScenePath);
            AppendField(builder, "destinationStableIdentity", record.DestinationStableIdentity);
            AppendField(
                builder,
                "placementConfigIdentitiesCanonical",
                record.PlacementConfigIdentitiesCanonical);
            AppendField(builder, "rollbackChunkIdsCanonical", record.RollbackChunkIdsCanonical);
            AppendField(builder, "rollbackManifestPath", record.RollbackManifestPath);
            AppendField(
                builder,
                "rollbackManifestContentHash",
                record.RollbackManifestContentHash);
            AppendField(
                builder,
                "rollbackCanonicalSceneDependencyHash",
                record.RollbackCanonicalSceneDependencyHash);
            AppendField(builder, "decisionOwner", record.DecisionOwner);
            return ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString()));
        }

        internal static string ComputeOrderedRecordSetHash(
            IReadOnlyList<OperationMapEntityPresentationMigrationRecord> records)
        {
            if (!TryValidateRecordSet(records, out string rejectionReason))
                throw new InvalidOperationException($"Cannot hash invalid migration record set: {rejectionReason}");

            OperationMapEntityPresentationMigrationRecord[] ordered = OrderBySourceIdentity(records);
            var builder = new StringBuilder(ordered.Length * 96);
            builder.Append(RecordSchema).Append(".set\n")
                .Append(RecordSchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n')
                .Append(ordered.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (int i = 0; i < ordered.Length; i++)
            {
                builder.Append(ordered[i].SourceOwnerGlobalObjectId)
                    .Append('\0')
                    .Append(ComputeRecordHash(ordered[i]))
                    .Append('\n');
            }

            return ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString()));
        }

        internal static bool TryValidateRecordSet(
            IReadOnlyList<OperationMapEntityPresentationMigrationRecord> records,
            out string rejectionReason)
        {
            if (records == null)
            {
                rejectionReason = "records-null";
                return false;
            }

            var sourceIdentities = new HashSet<string>(StringComparer.Ordinal);
            var destinationIdentities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < records.Count; i++)
            {
                OperationMapEntityPresentationMigrationRecord record = records[i];
                if (!TryValidateRecord(record, out rejectionReason))
                {
                    rejectionReason = $"record[{i}]:{rejectionReason}";
                    return false;
                }

                if (!sourceIdentities.Add(record.SourceOwnerGlobalObjectId))
                {
                    rejectionReason =
                        $"duplicate-sourceOwnerGlobalObjectId:{record.SourceOwnerGlobalObjectId}";
                    return false;
                }

                if (!destinationIdentities.Add(record.DestinationStableIdentity))
                {
                    rejectionReason =
                        $"duplicate-destinationStableIdentity:{record.DestinationStableIdentity}";
                    return false;
                }
            }

            rejectionReason = null;
            return true;
        }

        internal static bool TryValidateRecord(
            in OperationMapEntityPresentationMigrationRecord record,
            out string rejectionReason)
        {
            return TryCreateRecord(
                record.SourceScenePath,
                record.SourceOwnerGlobalObjectId,
                record.SourceOwnerHierarchyPath,
                record.ApprovedRole,
                record.PrefabAssetGuid,
                record.PrefabLocalId,
                record.SourceRendererPayloadCanonical,
                record.WorldPosition,
                record.WorldRotation,
                record.WorldScale,
                record.ComponentDispositionCanonical,
                record.DestinationSubScenePath,
                record.DestinationStableIdentity,
                record.PlacementConfigIdentitiesCanonical,
                record.RollbackChunkIdsCanonical,
                record.RollbackManifestPath,
                record.RollbackManifestContentHash,
                record.RollbackCanonicalSceneDependencyHash,
                record.DecisionOwner,
                out _,
                out rejectionReason);
        }

        internal static string ComputeSha256(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            using SHA256 algorithm = SHA256.Create();
            return ToLowerHex(algorithm.ComputeHash(bytes));
        }

        private static OperationMapEntityPresentationMigrationRecord[] OrderBySourceIdentity(
            IReadOnlyList<OperationMapEntityPresentationMigrationRecord> records)
        {
            var ordered = new OperationMapEntityPresentationMigrationRecord[records.Count];
            for (int i = 0; i < records.Count; i++)
                ordered[i] = records[i];

            Array.Sort(
                ordered,
                (left, right) =>
                {
                    int comparison = string.CompareOrdinal(
                        left.SourceOwnerGlobalObjectId,
                        right.SourceOwnerGlobalObjectId);
                    return comparison != 0
                        ? comparison
                        : string.CompareOrdinal(
                            left.DestinationStableIdentity,
                            right.DestinationStableIdentity);
                });
            return ordered;
        }

        private static bool TryRequireApprovedRole(string approvedRole, out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(approvedRole))
            {
                rejectionReason = "approvedRole-empty";
                return false;
            }

            if (!ApprovedRoles.Contains(approvedRole))
            {
                rejectionReason = $"approvedRole-unknown:{approvedRole}";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireRepositoryRelativePath(
            string path,
            string fieldName,
            out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                rejectionReason = $"{fieldName}-empty";
                return false;
            }

            if (path.IndexOf('\\') >= 0 ||
                path.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                path.StartsWith("/", StringComparison.Ordinal) ||
                path.Contains(":"))
            {
                rejectionReason = $"{fieldName}-not-repository-relative";
                return false;
            }

            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                rejectionReason = $"{fieldName}-must-start-with-Assets/";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireNonEmpty(string value, string fieldName, out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                rejectionReason = $"{fieldName}-empty";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireGlobalObjectId(
            string value,
            string fieldName,
            out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0 ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                !value.StartsWith("GlobalObjectId_V1-", StringComparison.Ordinal))
            {
                rejectionReason = $"{fieldName}-invalid";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireOptionalAssetGuid(
            string value,
            string fieldName,
            out string rejectionReason)
        {
            if (string.IsNullOrEmpty(value))
            {
                rejectionReason = null;
                return true;
            }

            if (value.Length != 32 ||
                value.Any(character =>
                    (character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')))
            {
                rejectionReason = $"{fieldName}-invalid";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireLowerHex(
            string value,
            string fieldName,
            int requiredLength,
            out string rejectionReason)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length != requiredLength ||
                value.Any(character =>
                    (character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')))
            {
                rejectionReason = $"{fieldName}-invalid";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireCanonicalSet(
            string value,
            string fieldName,
            char separator,
            bool required,
            out string rejectionReason)
        {
            if (string.IsNullOrEmpty(value))
            {
                rejectionReason = required ? $"{fieldName}-empty" : null;
                return !required;
            }

            if (value.IndexOf('\r') >= 0 ||
                string.IsNullOrWhiteSpace(value))
            {
                rejectionReason = $"{fieldName}-malformed";
                return false;
            }

            string[] entries = value.Split(separator);
            string previous = null;
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry) ||
                    !string.Equals(entry, entry.Trim(), StringComparison.Ordinal) ||
                    (previous != null && string.CompareOrdinal(previous, entry) >= 0))
                {
                    rejectionReason = $"{fieldName}-not-sorted-unique-canonical";
                    return false;
                }

                previous = entry;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryValidateInventoryCounts(
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
            out string rejectionReason)
        {
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryCountsReport counts =
                report.counts;
            if (counts.sourceCount <= 0 ||
                counts.chunkCount <= 0 ||
                counts.migrationOwnerCount <= 0 ||
                counts.unresolvedSourceObjectCount != 0 ||
                counts.ownersRequiringDependencyReviewCount != 0 ||
                counts.blockingDependencyCount != 0 ||
                counts.externalSceneReferenceCount != 0 ||
                counts.unresolvedCount != 0 ||
                counts.mixedOrAmbiguousCount != 0 ||
                counts.gameplayBuildingCandidateCount != 0 ||
                counts.gameplayVehicleCandidateCount != 0 ||
                counts.unresolvedBuildingPlacementCount != 0 ||
                counts.unresolvedVehiclePlacementCount != 0 ||
                counts.reusedBuildingSourceObjectCount != 0 ||
                counts.reusedVehicleSourceObjectCount != 0 ||
                counts.protectedAuthoredCandidateCount < 0 ||
                counts.staticRenderOnlyCandidateCount < 0 ||
                counts.protectedAuthoredCandidateCount +
                    counts.staticRenderOnlyCandidateCount != counts.sourceCount ||
                counts.buildingPlacementCount < 0 ||
                counts.vehiclePlacementCount < 0)
            {
                rejectionReason = "inventory-counts-rejected";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryValidateManifest(
            OperationMapEntityPresentationMigrationInventoryProbe.ManifestIdentityReport manifest,
            out string rejectionReason)
        {
            if (!TryRequireRepositoryRelativePath(
                    manifest.path,
                    "manifest.path",
                    out rejectionReason) ||
                !OperationMapIdentityRules.IsValidOperationMapId(manifest.operationMapId) ||
                !TryRequireLowerHex(manifest.contentHash, "manifest.contentHash", 32, out rejectionReason) ||
                !TryRequireRepositoryRelativePath(
                    manifest.canonicalScenePath,
                    "manifest.canonicalScenePath",
                    out rejectionReason) ||
                !TryRequireLowerHex(
                    manifest.canonicalSceneGuid,
                    "manifest.canonicalSceneGuid",
                    32,
                    out rejectionReason) ||
                !TryRequireLowerHex(
                    manifest.canonicalSceneDependencyHash,
                    "manifest.canonicalSceneDependencyHash",
                    32,
                    out rejectionReason))
            {
                rejectionReason ??= "manifest-invalid";
                return false;
            }

            return true;
        }

        private static bool TryValidateSource(
            OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport source,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (source == null ||
                !TryRequireGlobalObjectId(
                    source.sourceGlobalObjectId,
                    "sourceGlobalObjectId",
                    out rejectionReason) ||
                !TryRequireCanonicalAtom(
                    source.sourceHierarchyPath,
                    "sourceHierarchyPath",
                    out rejectionReason) ||
                !TryRequireLowerHex(
                    source.sourceDependencyHash,
                    "sourceDependencyHash",
                    32,
                    out rejectionReason) ||
                !TryRequireCanonicalAtom(source.chunkId, "chunkId", out rejectionReason) ||
                !TryRequireLowerHex(source.meshAssetGuid, "meshAssetGuid", 32, out rejectionReason) ||
                source.meshLocalId <= 0 ||
                !TryRequireGlobalObjectId(
                    source.migrationOwnerGlobalObjectId,
                    "migrationOwnerGlobalObjectId",
                    out rejectionReason) ||
                !source.sourceObjectResolved ||
                source.materialGuids == null ||
                source.buildingJoinCount != 0 ||
                source.vehicleJoinCount != 0 ||
                (source.classification != "StaticRenderOnlyCandidate" &&
                 source.classification != "ProtectedAuthoredCandidate"))
            {
                rejectionReason ??= "source-required-value-invalid";
                return false;
            }

            for (int i = 0; i < source.materialGuids.Count; i++)
            {
                if (!TryRequireLowerHex(
                        source.materialGuids[i],
                        $"materialGuids[{i}]",
                        32,
                        out rejectionReason))
                {
                    return false;
                }

                if (i > 0 &&
                    string.CompareOrdinal(source.materialGuids[i - 1], source.materialGuids[i]) >= 0)
                {
                    rejectionReason = "materialGuids-not-sorted-unique";
                    return false;
                }
            }

            rejectionReason = null;
            return true;
        }

        private static string BuildRendererPayload(
            OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport source)
        {
            var fields = new List<string>(8 + source.materialGuids.Count)
            {
                source.sourceGlobalObjectId,
                source.sourceHierarchyPath,
                source.sourceDependencyHash,
                source.chunkId,
                source.meshAssetGuid,
                source.meshLocalId.ToString(CultureInfo.InvariantCulture),
                source.materialGuids.Count.ToString(CultureInfo.InvariantCulture)
            };
            fields.AddRange(source.materialGuids);
            fields.Add(source.overlaySource ? "1" : "0");
            return EncodeLengthPrefixed(fields);
        }

        private static bool TryBuildComponentDisposition(
            OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport owner,
            out string disposition,
            out string rejectionReason)
        {
            disposition = null;
            rejectionReason = null;
            var componentTypes = new HashSet<string>(StringComparer.Ordinal);
            int componentTotal = 0;
            for (int i = 0; i < owner.componentTypes.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.DependencyTypeCountReport entry =
                    owner.componentTypes[i];
                if (entry == null ||
                    !TryRequireCanonicalAtom(entry.type, "component.type", out rejectionReason) ||
                    entry.count <= 0 ||
                    !componentTypes.Add(entry.type))
                {
                    rejectionReason ??= $"component[{i}]-invalid-or-duplicate";
                    return false;
                }

                componentTotal += entry.count;
            }

            var byType = new Dictionary<string, int>(StringComparer.Ordinal);
            int dispositionTotal = 0;
            for (int i = 0; i < owner.dispositionCounts.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.DependencyTypeCountReport entry =
                    owner.dispositionCounts[i];
                if (entry == null ||
                    !TryRequireCanonicalDispositionType(entry.type, out rejectionReason) ||
                    entry.count <= 0)
                {
                    rejectionReason ??= $"disposition[{i}]-invalid";
                    return false;
                }

                if (!byType.TryAdd(entry.type, entry.count))
                {
                    rejectionReason = $"duplicate-disposition-type:{entry.type}";
                    return false;
                }

                dispositionTotal += entry.count;
            }

            if (byType.Count == 0 || componentTypes.Count == 0)
            {
                rejectionReason = "disposition-empty";
                return false;
            }

            if (componentTotal != dispositionTotal)
            {
                rejectionReason = "component-disposition-count-mismatch";
                return false;
            }

            disposition = string.Join(
                "|",
                byType.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry =>
                        $"{entry.Key}={entry.Value.ToString(CultureInfo.InvariantCulture)}"));
            rejectionReason = null;
            return true;
        }

        private static bool TryValidatePlacementJoins(
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
            out string placementJoinSetHash,
            out string rejectionReason)
        {
            placementJoinSetHash = null;
            rejectionReason = null;
            var resolvedSourceIds = new HashSet<string>(StringComparer.Ordinal);
            var placementKeys = new HashSet<string>(StringComparer.Ordinal);
            var canonicalEntries = new List<string>(report.placementJoins.Count);
            int buildingCount = 0;
            int vehicleCount = 0;

            for (int i = 0; i < report.placementJoins.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.PlacementJoinReport join =
                    report.placementJoins[i];
                bool isBuilding = join != null && join.kind == "Building";
                bool isVehicle = join != null && join.kind == "Vehicle";
                if (join == null ||
                    (!isBuilding && !isVehicle) ||
                    join.placementIndex < 0 ||
                    (isBuilding &&
                     join.placementIndex >= report.counts.buildingPlacementCount) ||
                    (isVehicle &&
                     join.placementIndex >= report.counts.vehiclePlacementCount) ||
                    !TryRequireCanonicalAtom(join.sourcePath, "placement.sourcePath", out rejectionReason) ||
                    !string.Equals(join.resolveState, "Exact", StringComparison.Ordinal) ||
                    !TryRequireCanonicalAtom(
                        join.resolutionMethod,
                        "placement.resolutionMethod",
                        out rejectionReason) ||
                    join.scenePathMatchCount <= 0 ||
                    join.transformTupleMatchCount <= 0 ||
                    !TryRequireGlobalObjectId(
                        join.resolvedSourceGlobalObjectId,
                        "placement.resolvedSourceGlobalObjectId",
                        out rejectionReason))
                {
                    rejectionReason ??= $"placement[{i}]-not-exact";
                    return false;
                }

                string placementKey =
                    $"{join.kind}:{join.placementIndex.ToString(CultureInfo.InvariantCulture)}";
                if (!placementKeys.Add(placementKey) ||
                    !resolvedSourceIds.Add(join.resolvedSourceGlobalObjectId))
                {
                    rejectionReason = $"placement[{i}]-duplicate-or-reused";
                    return false;
                }

                string canonical = EncodeLengthPrefixed(
                    new[]
                    {
                        join.kind,
                        join.placementIndex.ToString(CultureInfo.InvariantCulture),
                        join.sourcePath,
                        join.resolveState,
                        join.resolutionMethod,
                        join.scenePathMatchCount.ToString(CultureInfo.InvariantCulture),
                        join.transformTupleMatchCount.ToString(CultureInfo.InvariantCulture),
                        join.resolvedSourceGlobalObjectId
                    });
                canonicalEntries.Add(canonical);
                if (isBuilding)
                {
                    buildingCount++;
                }
                else
                {
                    vehicleCount++;
                }
            }

            if (buildingCount != report.counts.buildingPlacementCount ||
                vehicleCount != report.counts.vehiclePlacementCount)
            {
                rejectionReason = "placement-count-mismatch";
                return false;
            }

            canonicalEntries.Sort(StringComparer.Ordinal);
            var builder = new StringBuilder(canonicalEntries.Count * 128);
            builder.Append(RecordSchema).Append(".placement-joins\n")
                .Append(RecordSchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n')
                .Append(canonicalEntries.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (int i = 0; i < canonicalEntries.Count; i++)
                builder.Append(canonicalEntries[i]).Append('\n');

            placementJoinSetHash = ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString()));
            rejectionReason = null;
            return true;
        }

        private static bool ClassificationCountsAgree(
            OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
            IReadOnlyDictionary<string, int> actual,
            out string rejectionReason)
        {
            var declared = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < report.classificationCounts.Count; i++)
            {
                OperationMapEntityPresentationMigrationInventoryProbe.ClassificationCountReport entry =
                    report.classificationCounts[i];
                if (entry == null ||
                    (entry.classification != "StaticRenderOnlyCandidate" &&
                     entry.classification != "ProtectedAuthoredCandidate") ||
                    entry.count < 0 ||
                    !declared.TryAdd(entry.classification, entry.count))
                {
                    rejectionReason = $"classification-count[{i}]-invalid";
                    return false;
                }
            }

            foreach (string classification in new[]
                     {
                         "StaticRenderOnlyCandidate",
                         "ProtectedAuthoredCandidate"
                     })
            {
                actual.TryGetValue(classification, out int actualCount);
                declared.TryGetValue(classification, out int declaredCount);
                int expectedCount = classification == "StaticRenderOnlyCandidate"
                    ? report.counts.staticRenderOnlyCandidateCount
                    : report.counts.protectedAuthoredCandidateCount;
                if (actualCount != declaredCount || actualCount != expectedCount)
                {
                    rejectionReason = $"classification-count-mismatch:{classification}";
                    return false;
                }
            }

            rejectionReason = null;
            return actual.Count == declared.Count;
        }

        private static bool HasDuplicateOrNestedHierarchy(
            List<string> hierarchyPaths,
            out string rejectionReason)
        {
            hierarchyPaths.Sort(StringComparer.Ordinal);
            for (int i = 1; i < hierarchyPaths.Count; i++)
            {
                string previous = hierarchyPaths[i - 1];
                string current = hierarchyPaths[i];
                if (string.Equals(previous, current, StringComparison.Ordinal) ||
                    current.StartsWith(previous + "/", StringComparison.Ordinal))
                {
                    rejectionReason = $"duplicate-or-nested-owner-hierarchy:{previous}";
                    return true;
                }
            }

            rejectionReason = null;
            return false;
        }

        private static bool TryRequireCanonicalAtom(
            string value,
            string fieldName,
            out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOf('\r') >= 0 ||
                value.IndexOf('\n') >= 0 ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                rejectionReason = $"{fieldName}-malformed";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireCanonicalDispositionType(
            string value,
            out string rejectionReason)
        {
            if (!TryRequireCanonicalAtom(value, "disposition.type", out rejectionReason) ||
                value.IndexOf('|') >= 0 ||
                value.IndexOf('=') >= 0 ||
                !ApprovedStaticDependencyDispositions.Contains(value))
            {
                rejectionReason ??= "disposition.type-malformed";
                return false;
            }

            return true;
        }

        private static string EncodeLengthPrefixed(IEnumerable<string> fields)
        {
            var builder = new StringBuilder();
            foreach (string field in fields)
            {
                int byteCount = Utf8WithoutBom.GetByteCount(field);
                builder.Append(byteCount.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(field);
            }

            return builder.ToString();
        }

        private static bool TryRequireFinite(Vector3 value, string fieldName, out string rejectionReason)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
            {
                rejectionReason = $"{fieldName}-non-finite";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool TryRequireFinite(Quaternion value, string fieldName, out string rejectionReason)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z) || !IsFinite(value.w))
            {
                rejectionReason = $"{fieldName}-non-finite";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void AppendField(StringBuilder builder, string name, string value)
        {
            builder.Append(name).Append('=').Append(value).Append('\n');
        }

        private static void AppendVectorBits(StringBuilder builder, string name, Vector3 value)
        {
            builder.Append(name).Append('=')
                .Append(BitConverter.SingleToInt32Bits(value.x).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.y).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.z).ToString("x8", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        private static void AppendQuaternionBits(StringBuilder builder, string name, Quaternion value)
        {
            builder.Append(name).Append('=')
                .Append(BitConverter.SingleToInt32Bits(value.x).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.y).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.z).ToString("x8", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(BitConverter.SingleToInt32Bits(value.w).ToString("x8", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }

    internal enum OperationMapEntityPresentationMigrationPlanStatus : byte
    {
        StaticOwnersReadyGameplayOwnersPending = 1
    }

    internal enum OperationMapEntityPresentationMutationReadiness : byte
    {
        NotReady = 0,
        CandidateTransactionReadyPendingMutation = 1
    }

    internal sealed class OperationMapEntityPresentationMigrationPlan
    {
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<
            OperationMapEntityPresentationMigrationRecord> records;

        internal OperationMapEntityPresentationMigrationPlan(
            IList<OperationMapEntityPresentationMigrationRecord> records,
            string recordSetHash,
            string placementJoinSetHash)
        {
            var copy = new OperationMapEntityPresentationMigrationRecord[records.Count];
            records.CopyTo(copy, 0);
            this.records = Array.AsReadOnly(copy);
            RecordSetHash = recordSetHash;
            PlacementJoinSetHash = placementJoinSetHash;
        }

        internal OperationMapEntityPresentationMigrationPlanStatus Status =>
            OperationMapEntityPresentationMigrationPlanStatus.StaticOwnersReadyGameplayOwnersPending;

        internal IReadOnlyList<OperationMapEntityPresentationMigrationRecord> Records => records;
        internal string RecordSetHash { get; }
        internal string PlacementJoinSetHash { get; }
    }

    /// <summary>
    /// Immutable deterministic disposition for one existing map owner pending candidate migration.
    /// </summary>
    internal readonly struct OperationMapEntityPresentationMigrationRecord
    {
        internal OperationMapEntityPresentationMigrationRecord(
            string sourceScenePath,
            string sourceOwnerGlobalObjectId,
            string sourceOwnerHierarchyPath,
            string approvedRole,
            string prefabAssetGuid,
            long prefabLocalId,
            string sourceRendererPayloadCanonical,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 worldScale,
            string componentDispositionCanonical,
            string destinationSubScenePath,
            string destinationStableIdentity,
            string placementConfigIdentitiesCanonical,
            string rollbackChunkIdsCanonical,
            string rollbackManifestPath,
            string rollbackManifestContentHash,
            string rollbackCanonicalSceneDependencyHash,
            string decisionOwner)
        {
            SourceScenePath = sourceScenePath;
            SourceOwnerGlobalObjectId = sourceOwnerGlobalObjectId;
            SourceOwnerHierarchyPath = sourceOwnerHierarchyPath;
            ApprovedRole = approvedRole;
            PrefabAssetGuid = prefabAssetGuid;
            PrefabLocalId = prefabLocalId;
            SourceRendererPayloadCanonical = sourceRendererPayloadCanonical;
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
            WorldScale = worldScale;
            ComponentDispositionCanonical = componentDispositionCanonical;
            DestinationSubScenePath = destinationSubScenePath;
            DestinationStableIdentity = destinationStableIdentity;
            PlacementConfigIdentitiesCanonical = placementConfigIdentitiesCanonical;
            RollbackChunkIdsCanonical = rollbackChunkIdsCanonical;
            RollbackManifestPath = rollbackManifestPath;
            RollbackManifestContentHash = rollbackManifestContentHash;
            RollbackCanonicalSceneDependencyHash = rollbackCanonicalSceneDependencyHash;
            DecisionOwner = decisionOwner;
        }

        internal string SourceScenePath { get; }
        internal string SourceOwnerGlobalObjectId { get; }
        internal string SourceOwnerHierarchyPath { get; }
        internal string ApprovedRole { get; }
        internal string PrefabAssetGuid { get; }
        internal long PrefabLocalId { get; }
        internal string SourceRendererPayloadCanonical { get; }
        internal Vector3 WorldPosition { get; }
        internal Quaternion WorldRotation { get; }
        internal Vector3 WorldScale { get; }
        internal string ComponentDispositionCanonical { get; }
        internal string DestinationSubScenePath { get; }
        internal string DestinationStableIdentity { get; }
        internal string PlacementConfigIdentitiesCanonical { get; }
        internal string RollbackChunkIdsCanonical { get; }
        internal string RollbackManifestPath { get; }
        internal string RollbackManifestContentHash { get; }
        internal string RollbackCanonicalSceneDependencyHash { get; }
        internal string DecisionOwner { get; }
    }
}

#endif
