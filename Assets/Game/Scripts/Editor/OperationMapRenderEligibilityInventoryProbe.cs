#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Read-only VRP-003/VRP-021 inventory. It classifies authoring renderer/material rows and
    /// retains their exact stable-owner join, but never mutates presentation ownership.
    /// </summary>
    internal static class OperationMapRenderEligibilityInventoryProbe
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_eligibility_inventory.json";
        internal const string SourceRowsPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_source_rows.json.gz";
        internal const int ExpectedPackedRenderRowCount = 82797;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    OpenSceneMode.Single);
                InventoryReport report = Build(scene);
                string outputPath = Path.Combine(projectRoot, ReportPath);
                string sourceRowsOutputPath = Path.Combine(projectRoot, SourceRowsPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                byte[] sourceRowsJson = Utf8WithoutBom.GetBytes(
                    JsonUtility.ToJson(
                        new SourceRowsDocument
                        {
                            schema =
                                "warline.operation-map.render-virtualization-source-row-inventory",
                            schemaVersion = 1,
                            operationMapId = report.operationMapId,
                            sourceRowCount = report.sourceRowCount,
                            sourceRowsSha256 = report.sourceRowsSha256,
                            sourceRows = report.sourceRows
                        },
                        false) +
                    "\n");
                byte[] sourceRowsGzip = CompressGzip(sourceRowsJson);
                report.sourceRowsPath = SourceRowsPath;
                report.sourceRowsCompression = "gzip";
                report.sourceRowsJsonSha256 = ComputeSha256(sourceRowsJson);
                report.sourceRowsGzipSha256 = ComputeSha256(sourceRowsGzip);
                report.sourceRows = null;

                string sourceRowsTemporaryPath = sourceRowsOutputPath + ".tmp";
                File.WriteAllBytes(sourceRowsTemporaryPath, sourceRowsGzip);
                if (File.Exists(sourceRowsOutputPath))
                    File.Delete(sourceRowsOutputPath);
                File.Move(sourceRowsTemporaryPath, sourceRowsOutputPath);

                string temporaryPath = outputPath + ".tmp";
                File.WriteAllText(
                    temporaryPath,
                    JsonUtility.ToJson(report, true) + "\n",
                    Utf8WithoutBom);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                File.Move(temporaryPath, outputPath);

                Debug.Log(
                    $"[OperationMapRenderEligibilityInventoryProbe] result=Passed " +
                    $"rows={report.totalRenderRows} eligible={report.eligibleRenderRows} " +
                    $"excluded={report.excludedRenderRows} joined={report.stableOwnerJoinedRenderRows} " +
                    $"report={ReportPath} sourceRows={SourceRowsPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[OperationMapRenderEligibilityInventoryProbe] result=Failed");
                throw;
            }
            finally
            {
                Restore(previousSetup);
            }
        }

        private static InventoryReport Build(Scene scene)
        {
            Renderer[] renderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .OrderBy(renderer => GetPath(renderer.transform), StringComparer.Ordinal)
                .ToArray();

            var rows = new List<Row>(ExpectedPackedRenderRowCount);
            var signatureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;
                DenseCityPresentationIdentityAuthoring denseOwner =
                    renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
                OperationMapEntityPresentationIdentityAuthoring acceptedOwner =
                    renderer.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>(true);
                for (int subMesh = 0; subMesh < materials.Length; subMesh++)
                {
                    string signature =
                        (denseOwner != null
                            ? denseOwner.Category.ToString()
                            : "AcceptedMapResident") +
                        "|" +
                        BuildSignature(renderer, materials[subMesh], subMesh);
                    if (denseOwner != null)
                    {
                        signatureCounts.TryGetValue(signature, out int count);
                        signatureCounts[signature] = count + 1;
                    }
                    rows.Add(new Row(renderer, materials[subMesh], subMesh, signature, denseOwner, acceptedOwner));
                }
            }

            var semantic = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var signatures = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var rendererTypes = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var policies = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var ownership = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var reasons = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var sourceRows = new List<SourceRowReport>(rows.Count);
            var ownerIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var rendererPathIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var logicalRowIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var logicalRowSources = new HashSet<string>(StringComparer.Ordinal);
            int eligible = 0;
            int stableOwnerJoined = 0;
            int eligibleStableOwnerJoined = 0;

            foreach (Row row in rows)
            {
                bool repeated =
                    signatureCounts.TryGetValue(row.Signature, out int signatureCount) &&
                    signatureCount > 1;
                bool policySupported = TryClassifyPolicy(row.Renderer, row.Material, out string policy);
                string reason = Classify(row, repeated, policySupported);
                bool isEligible = string.Equals(reason, "eligible", StringComparison.Ordinal);
                if (isEligible)
                    eligible++;

                Increment(semantic, Semantic(row), isEligible);
                Increment(signatures, row.Signature, isEligible);
                Increment(rendererTypes, row.Renderer.GetType().FullName ?? row.Renderer.GetType().Name, isEligible);
                Increment(policies, policy, isEligible);
                Increment(ownership, Ownership(row), isEligible);
                Increment(reasons, reason, isEligible);

                SourceRowReport sourceRow = BuildSourceRow(
                    row,
                    policy,
                    reason,
                    isEligible,
                    ownerIdentityCollisions,
                    rendererPathIdentityCollisions,
                    logicalRowIdentityCollisions,
                    logicalRowSources);
                sourceRows.Add(sourceRow);
                if (sourceRow.stableOwnerJoined)
                {
                    stableOwnerJoined++;
                    if (isEligible)
                        eligibleStableOwnerJoined++;
                }
            }

            if (rows.Count != ExpectedPackedRenderRowCount)
            {
                throw new InvalidOperationException(
                    $"Authoring render rows did not reconcile to packed evidence: " +
                    $"{rows.Count} != {ExpectedPackedRenderRowCount}.");
            }
            if (eligibleStableOwnerJoined != eligible)
            {
                throw new InvalidOperationException(
                    $"Every eligible row requires an exact stable-owner join: " +
                    $"{eligibleStableOwnerJoined} != {eligible}.");
            }

            sourceRows.Sort(SourceRowReportComparer.Instance);
            for (int index = 0; index < sourceRows.Count; index++)
                sourceRows[index].sourceRowIndex = index;

            return new InventoryReport
            {
                schema = "warline.operation-map.render-virtualization-eligibility-inventory",
                schemaVersion = 2,
                result = "Passed",
                operationMapId = "opmap.skirmish.desert_base_01",
                candidateScenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                candidateSceneDependencyHash = AssetDatabase
                    .GetAssetDependencyHash(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)
                    .ToString(),
                packedEvidencePath =
                    "Design/AgentReports/2026-07-25_dense_city_packed_asset_sharing.json",
                packedEvidenceRenderRows = ExpectedPackedRenderRowCount,
                totalRenderRows = rows.Count,
                eligibleRenderRows = eligible,
                excludedRenderRows = rows.Count - eligible,
                stableOwnerJoinedRenderRows = stableOwnerJoined,
                unresolvedOwnerRenderRows = rows.Count - stableOwnerJoined,
                eligibleStableOwnerJoinedRenderRows = eligibleStableOwnerJoined,
                sourceRowCount = sourceRows.Count,
                sourceRowsSha256 = ComputeSourceRowsSha256(sourceRows),
                mutationAuthorized = false,
                mutationBlocker =
                    "VRP-002 raw Android profile remains open; source-row inventory authorizes no mutation.",
                bySemanticCategory = BuildBreakdown(semantic),
                byPrototypeSignature = BuildBreakdown(signatures),
                byRendererType = BuildBreakdown(rendererTypes),
                byPolicyBucket = BuildBreakdown(policies),
                byGameplayOwnership = BuildBreakdown(ownership),
                byReasonCode = BuildBreakdown(reasons),
                sourceRows = sourceRows
            };
        }

        private static SourceRowReport BuildSourceRow(
            Row row,
            string policy,
            string reason,
            bool eligible,
            OperationMapRenderIdentityCollisionDetector ownerIdentityCollisions,
            OperationMapRenderIdentityCollisionDetector rendererPathIdentityCollisions,
            OperationMapRenderIdentityCollisionDetector logicalRowIdentityCollisions,
            HashSet<string> logicalRowSources)
        {
            string ownerKind;
            string ownerStableId;
            string ownerRole;
            string ownerSourcePath;
            Transform ownerTransform;
            bool stableOwnerJoined;
            if (row.DenseOwner != null)
            {
                if (!row.DenseOwner.TryValidate(out string error))
                    throw new InvalidOperationException($"Invalid dense stable owner: {error}");
                ownerKind = "DenseGenerated";
                ownerStableId = row.DenseOwner.StableId;
                ownerRole = row.DenseOwner.Role.ToString();
                ownerSourcePath = GetPath(row.DenseOwner.transform);
                ownerTransform = row.DenseOwner.transform;
                stableOwnerJoined = true;
            }
            else if (row.AcceptedOwner != null)
            {
                if (!row.AcceptedOwner.TryValidate(out string error))
                    throw new InvalidOperationException($"Invalid accepted-map stable owner: {error}");
                ownerKind = "AcceptedMap";
                ownerStableId = row.AcceptedOwner.SourceGlobalObjectId;
                ownerRole = row.AcceptedOwner.Role.ToString();
                ownerSourcePath = GetPath(row.AcceptedOwner.transform);
                ownerTransform = row.AcceptedOwner.transform;
                stableOwnerJoined = true;
            }
            else
            {
                ownerKind = "Unresolved";
                ownerStableId = GlobalObjectId.GetGlobalObjectIdSlow(row.Renderer.gameObject).ToString();
                ownerRole = string.Empty;
                ownerSourcePath = GetPath(row.Renderer.transform);
                ownerTransform = row.Renderer.transform;
                stableOwnerJoined = false;
            }

            string ownerIdentitySource =
                ownerKind.ToLowerInvariant() + "|" + ownerStableId;
            string rendererPath = GetIndexedRelativePath(ownerTransform, row.Renderer.transform);
            string rendererPathIdentitySource = "renderer-path|" + rendererPath;
            string logicalRowIdentitySource =
                ownerIdentitySource +
                "|" +
                rendererPathIdentitySource +
                "|submesh=" +
                row.SubMesh.ToString(CultureInfo.InvariantCulture);
            if (!logicalRowSources.Add(logicalRowIdentitySource))
            {
                throw new InvalidOperationException(
                    $"Duplicate logical renderer-row source: {logicalRowIdentitySource}");
            }

            OperationMapRenderIdentity128 ownerIdentity = ProjectAndRegister(
                ownerIdentitySource,
                ownerIdentityCollisions);
            OperationMapRenderIdentity128 rendererPathIdentity = ProjectAndRegister(
                rendererPathIdentitySource,
                rendererPathIdentityCollisions);
            OperationMapRenderIdentity128 logicalRowIdentity = ProjectAndRegister(
                logicalRowIdentitySource,
                logicalRowIdentityCollisions);

            Mesh mesh = row.Renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : row.Renderer.GetComponent<MeshFilter>()?.sharedMesh;
            ReadPersistentIdentity(mesh, out string meshGuid, out long meshLocalId);
            ReadPersistentIdentity(row.Material, out string materialGuid, out long materialLocalId);
            if (eligible &&
                (string.IsNullOrEmpty(meshGuid) ||
                 meshLocalId == 0 ||
                 string.IsNullOrEmpty(materialGuid) ||
                 materialLocalId == 0))
            {
                throw new InvalidOperationException(
                    $"Eligible source row lacks persistent mesh/material identity: " +
                    $"{logicalRowIdentitySource}");
            }

            return new SourceRowReport
            {
                ownerKind = ownerKind,
                stableOwnerJoined = stableOwnerJoined,
                ownerStableId = ownerStableId,
                ownerRole = ownerRole,
                ownerSourcePath = ownerSourcePath,
                ownerIdentitySource = ownerIdentitySource,
                ownerIdentityLow = ownerIdentity.Low,
                ownerIdentityHigh = ownerIdentity.High,
                semanticCategory = Semantic(row),
                rendererPath = rendererPath,
                rendererPathIdentitySource = rendererPathIdentitySource,
                rendererPathIdentityLow = rendererPathIdentity.Low,
                rendererPathIdentityHigh = rendererPathIdentity.High,
                logicalRowIdentitySource = logicalRowIdentitySource,
                logicalRowIdentityLow = logicalRowIdentity.Low,
                logicalRowIdentityHigh = logicalRowIdentity.High,
                rendererType = row.Renderer.GetType().FullName ?? row.Renderer.GetType().Name,
                subMeshIndex = row.SubMesh,
                meshAssetGuid = meshGuid,
                meshLocalId = meshLocalId,
                materialAssetGuid = materialGuid,
                materialLocalId = materialLocalId,
                prototypeSignature = row.Signature,
                policy = policy,
                eligible = eligible,
                reasonCode = reason
            };
        }

        private static OperationMapRenderIdentity128 ProjectAndRegister(
            string source,
            OperationMapRenderIdentityCollisionDetector collisions)
        {
            if (!OperationMapRenderIdentityProjection.TryProject(
                    source,
                    out OperationMapRenderIdentity128 identity,
                    out string projectionError))
            {
                throw new InvalidOperationException(projectionError);
            }
            if (!collisions.TryRegister(identity, source, out string collisionError))
                throw new InvalidOperationException(collisionError);
            return identity;
        }

        private static string Classify(Row row, bool repeated, bool policySupported)
        {
            if (row.DenseOwner == null)
                return row.AcceptedOwner != null
                    ? "accepted-map-resident-pending-vrp021"
                    : "unresolved-owner";
            if (row.Renderer is not MeshRenderer)
                return "unsupported-renderer";
            if (!policySupported)
                return "unsupported-render-policy";
            if (!repeated)
                return "unique-presentation-signature";

            return row.DenseOwner.Category switch
            {
                DenseCityPresentationSemanticCategory.Vegetation => "eligible",
                DenseCityPresentationSemanticCategory.Prop => "eligible",
                DenseCityPresentationSemanticCategory.GameplayBuildingIntact =>
                    "gameplay-building-state-sync-not-accepted",
                DenseCityPresentationSemanticCategory.Infrastructure =>
                    "infrastructure-deferred-after-render-only-pilot",
                DenseCityPresentationSemanticCategory.Horizon =>
                    "unique-environment-content-resident",
                _ => "unsupported-semantic-category"
            };
        }

        private static bool TryClassifyPolicy(Renderer renderer, Material material, out string policy)
        {
            if (material == null)
            {
                policy = "Unsupported:missing-material";
                return false;
            }

            OperationMapRenderMaterialSurface surface;
            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                surface = OperationMapRenderMaterialSurface.Transparent;
            else if (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f)
                surface = OperationMapRenderMaterialSurface.AlphaClipped;
            else
                surface = OperationMapRenderMaterialSurface.Opaque;

            OperationMapRenderShadowFlags shadowFlags = OperationMapRenderShadowFlags.None;
            if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                shadowFlags |= OperationMapRenderShadowFlags.CastShadows;
            if (renderer.receiveShadows)
                shadowFlags |= OperationMapRenderShadowFlags.ReceiveShadows;

            OperationMapRenderMotionVectorMode motion = renderer.motionVectorGenerationMode switch
            {
                MotionVectorGenerationMode.Camera => OperationMapRenderMotionVectorMode.Camera,
                MotionVectorGenerationMode.Object => OperationMapRenderMotionVectorMode.Object,
                MotionVectorGenerationMode.ForceNoMotion => OperationMapRenderMotionVectorMode.ForceNoMotion,
                _ => (OperationMapRenderMotionVectorMode)byte.MaxValue
            };
            var input = new OperationMapRenderPolicyClassificationInput(
                surface,
                renderer.gameObject.layer,
                renderer.renderingLayerMask,
                motion,
                shadowFlags);
            if (!OperationMapRenderPolicyClassifier.TryClassify(input, out OperationMapRenderPolicyKey key, out string error))
            {
                policy = "Unsupported:" + error;
                return false;
            }

            policy = string.Join(
                "|",
                key.Bucket,
                key.Layer.ToString(CultureInfo.InvariantCulture),
                key.RenderingLayerMask.ToString(CultureInfo.InvariantCulture),
                key.MotionVectorMode,
                (byte)key.ShadowFlags);
            return true;
        }

        private static string Semantic(Row row) =>
            row.DenseOwner != null ? row.DenseOwner.Category.ToString() : "AcceptedMapResident";

        private static string Ownership(Row row)
        {
            if (row.DenseOwner != null)
                return "DenseGenerated:" + row.DenseOwner.Role;
            if (row.AcceptedOwner != null)
                return "AcceptedMap:" + row.AcceptedOwner.Role;
            return "Unresolved";
        }

        private static string BuildSignature(Renderer renderer, Material material, int subMesh)
        {
            Mesh mesh = renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;
            DenseCityPresentationIdentityAuthoring denseOwner =
                renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
            GameObject prefabSource = denseOwner != null
                ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(denseOwner.gameObject)
                : null;
            string prefab = PersistentIdentity(prefabSource);
            string rendererSource = PersistentIdentity(
                PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer));
            return string.Join(
                "|",
                string.IsNullOrEmpty(prefab) ? "mesh" : "prefab",
                prefab,
                rendererSource,
                PersistentIdentity(mesh),
                PersistentIdentity(material),
                subMesh.ToString(CultureInfo.InvariantCulture));
        }

        private static string PersistentIdentity(UnityEngine.Object value)
        {
            ReadPersistentIdentity(value, out string guid, out long localId);
            if (string.IsNullOrEmpty(guid) || localId == 0)
                return string.Empty;
            return guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
        }

        private static void ReadPersistentIdentity(
            UnityEngine.Object value,
            out string guid,
            out long localId)
        {
            if (value == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out guid, out localId) ||
                string.IsNullOrEmpty(guid) ||
                localId == 0)
            {
                guid = string.Empty;
                localId = 0;
            }
        }

        private static string GetPath(Transform transform)
        {
            var parts = new List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string GetIndexedRelativePath(Transform owner, Transform target)
        {
            if (owner == null || target == null)
                throw new ArgumentNullException(owner == null ? nameof(owner) : nameof(target));
            if (owner == target)
                return "<owner>";

            var parts = new List<string>();
            Transform current = target;
            while (current != null && current != owner)
            {
                parts.Add(
                    current.name +
                    "[" +
                    current.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) +
                    "]");
                current = current.parent;
            }
            if (current != owner)
            {
                throw new InvalidOperationException(
                    $"Renderer '{GetPath(target)}' is not beneath owner '{GetPath(owner)}'.");
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string ComputeSourceRowsSha256(IReadOnlyList<SourceRowReport> rows)
        {
            var canonical = new StringBuilder(rows.Count * 256);
            for (int index = 0; index < rows.Count; index++)
            {
                SourceRowReport row = rows[index];
                AppendCanonical(canonical, row.sourceRowIndex);
                AppendCanonical(canonical, row.ownerKind);
                AppendCanonical(canonical, row.stableOwnerJoined);
                AppendCanonical(canonical, row.ownerStableId);
                AppendCanonical(canonical, row.ownerRole);
                AppendCanonical(canonical, row.ownerSourcePath);
                AppendCanonical(canonical, row.ownerIdentitySource);
                AppendCanonical(canonical, row.ownerIdentityLow);
                AppendCanonical(canonical, row.ownerIdentityHigh);
                AppendCanonical(canonical, row.semanticCategory);
                AppendCanonical(canonical, row.rendererPath);
                AppendCanonical(canonical, row.rendererPathIdentitySource);
                AppendCanonical(canonical, row.rendererPathIdentityLow);
                AppendCanonical(canonical, row.rendererPathIdentityHigh);
                AppendCanonical(canonical, row.logicalRowIdentitySource);
                AppendCanonical(canonical, row.logicalRowIdentityLow);
                AppendCanonical(canonical, row.logicalRowIdentityHigh);
                AppendCanonical(canonical, row.rendererType);
                AppendCanonical(canonical, row.subMeshIndex);
                AppendCanonical(canonical, row.meshAssetGuid);
                AppendCanonical(canonical, row.meshLocalId);
                AppendCanonical(canonical, row.materialAssetGuid);
                AppendCanonical(canonical, row.materialLocalId);
                AppendCanonical(canonical, row.prototypeSignature);
                AppendCanonical(canonical, row.policy);
                AppendCanonical(canonical, row.eligible);
                AppendCanonical(canonical, row.reasonCode);
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(Utf8WithoutBom.GetBytes(canonical.ToString()));
            var hex = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
                hex.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
            return hex.ToString();
        }

        private static byte[] CompressGzip(byte[] source)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(
                       output,
                       System.IO.Compression.CompressionLevel.Optimal,
                       true))
                gzip.Write(source, 0, source.Length);
            return output.ToArray();
        }

        private static string ComputeSha256(byte[] source)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(source);
            var hex = new StringBuilder(digest.Length * 2);
            for (int index = 0; index < digest.Length; index++)
                hex.Append(digest[index].ToString("x2", CultureInfo.InvariantCulture));
            return hex.ToString();
        }

        private static void AppendCanonical(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value)
                .Append('\n');
        }

        private static void AppendCanonical(StringBuilder builder, bool value) =>
            AppendCanonical(builder, value ? "1" : "0");

        private static void AppendCanonical(StringBuilder builder, int value) =>
            AppendCanonical(builder, value.ToString(CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, long value) =>
            AppendCanonical(builder, value.ToString(CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, ulong value) =>
            AppendCanonical(builder, value.ToString(CultureInfo.InvariantCulture));

        private static void Increment(Dictionary<string, CountPair> values, string key, bool eligible)
        {
            values.TryGetValue(key, out CountPair count);
            if (eligible)
                count.Eligible++;
            else
                count.Excluded++;
            values[key] = count;
        }

        private static List<Breakdown> BuildBreakdown(Dictionary<string, CountPair> values) =>
            values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new Breakdown
                {
                    key = pair.Key,
                    eligible = pair.Value.Eligible,
                    excluded = pair.Value.Excluded,
                    total = pair.Value.Eligible + pair.Value.Excluded
                })
                .ToList();

        private static void Restore(SceneSetup[] setup)
        {
            if (setup != null && setup.Any(entry => entry.isLoaded))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private readonly struct Row
        {
            internal Row(
                Renderer renderer,
                Material material,
                int subMesh,
                string signature,
                DenseCityPresentationIdentityAuthoring denseOwner,
                OperationMapEntityPresentationIdentityAuthoring acceptedOwner)
            {
                Renderer = renderer;
                Material = material;
                SubMesh = subMesh;
                Signature = signature;
                DenseOwner = denseOwner;
                AcceptedOwner = acceptedOwner;
            }

            internal Renderer Renderer { get; }
            internal Material Material { get; }
            internal int SubMesh { get; }
            internal string Signature { get; }
            internal DenseCityPresentationIdentityAuthoring DenseOwner { get; }
            internal OperationMapEntityPresentationIdentityAuthoring AcceptedOwner { get; }
        }

        private struct CountPair
        {
            internal int Eligible;
            internal int Excluded;
        }

        [Serializable]
        private sealed class InventoryReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string candidateScenePath;
            public string candidateSceneDependencyHash;
            public string packedEvidencePath;
            public int packedEvidenceRenderRows;
            public int totalRenderRows;
            public int eligibleRenderRows;
            public int excludedRenderRows;
            public int stableOwnerJoinedRenderRows;
            public int unresolvedOwnerRenderRows;
            public int eligibleStableOwnerJoinedRenderRows;
            public int sourceRowCount;
            public string sourceRowsSha256;
            public string sourceRowsPath;
            public string sourceRowsCompression;
            public string sourceRowsJsonSha256;
            public string sourceRowsGzipSha256;
            public bool mutationAuthorized;
            public string mutationBlocker;
            public List<Breakdown> bySemanticCategory;
            public List<Breakdown> byPrototypeSignature;
            public List<Breakdown> byRendererType;
            public List<Breakdown> byPolicyBucket;
            public List<Breakdown> byGameplayOwnership;
            public List<Breakdown> byReasonCode;
            [NonSerialized] public List<SourceRowReport> sourceRows;
        }

        [Serializable]
        private sealed class SourceRowsDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public int sourceRowCount;
            public string sourceRowsSha256;
            public List<SourceRowReport> sourceRows;
        }

        [Serializable]
        private sealed class Breakdown
        {
            public string key;
            public int eligible;
            public int excluded;
            public int total;
        }

        [Serializable]
        private sealed class SourceRowReport
        {
            public int sourceRowIndex;
            public string ownerKind;
            public bool stableOwnerJoined;
            public string ownerStableId;
            public string ownerRole;
            public string ownerSourcePath;
            public string ownerIdentitySource;
            public ulong ownerIdentityLow;
            public ulong ownerIdentityHigh;
            public string semanticCategory;
            public string rendererPath;
            public string rendererPathIdentitySource;
            public ulong rendererPathIdentityLow;
            public ulong rendererPathIdentityHigh;
            public string logicalRowIdentitySource;
            public ulong logicalRowIdentityLow;
            public ulong logicalRowIdentityHigh;
            public string rendererType;
            public int subMeshIndex;
            public string meshAssetGuid;
            public long meshLocalId;
            public string materialAssetGuid;
            public long materialLocalId;
            public string prototypeSignature;
            public string policy;
            public bool eligible;
            public string reasonCode;
        }

        private sealed class SourceRowReportComparer : IComparer<SourceRowReport>
        {
            internal static readonly SourceRowReportComparer Instance = new();

            public int Compare(SourceRowReport left, SourceRowReport right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;

                int result = string.CompareOrdinal(left.ownerIdentitySource, right.ownerIdentitySource);
                if (result != 0)
                    return result;
                result = string.CompareOrdinal(left.rendererPath, right.rendererPath);
                if (result != 0)
                    return result;
                result = left.subMeshIndex.CompareTo(right.subMeshIndex);
                if (result != 0)
                    return result;
                result = string.CompareOrdinal(left.meshAssetGuid, right.meshAssetGuid);
                if (result != 0)
                    return result;
                result = left.meshLocalId.CompareTo(right.meshLocalId);
                if (result != 0)
                    return result;
                result = string.CompareOrdinal(left.materialAssetGuid, right.materialAssetGuid);
                return result != 0 ? result : left.materialLocalId.CompareTo(right.materialLocalId);
            }
        }
    }
}

#endif
