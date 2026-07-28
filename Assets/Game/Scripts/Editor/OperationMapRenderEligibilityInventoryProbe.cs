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
    using Unity.Mathematics;
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
        internal const string PrototypeRecipesPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_prototype_recipes.json";
        internal const string LogicalPlacementsPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_logical_placements.json";
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
                string prototypeRecipesOutputPath =
                    Path.Combine(projectRoot, PrototypeRecipesPath);
                string logicalPlacementsOutputPath =
                    Path.Combine(projectRoot, LogicalPlacementsPath);
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

                byte[] prototypeRecipesJson = Utf8WithoutBom.GetBytes(
                    JsonUtility.ToJson(report.prototypeRecipes, true) + "\n");
                report.prototypeRecipesPath = PrototypeRecipesPath;
                report.prototypeRecipesJsonSha256 = ComputeSha256(prototypeRecipesJson);
                report.prototypeRecipes = null;

                byte[] logicalPlacementsJson = Utf8WithoutBom.GetBytes(
                    JsonUtility.ToJson(report.logicalPlacements, true) + "\n");
                report.logicalPlacementsPath = LogicalPlacementsPath;
                report.logicalPlacementsJsonSha256 = ComputeSha256(logicalPlacementsJson);
                report.logicalPlacements = null;

                string sourceRowsTemporaryPath = sourceRowsOutputPath + ".tmp";
                File.WriteAllBytes(sourceRowsTemporaryPath, sourceRowsGzip);
                if (File.Exists(sourceRowsOutputPath))
                    File.Delete(sourceRowsOutputPath);
                File.Move(sourceRowsTemporaryPath, sourceRowsOutputPath);

                string prototypeRecipesTemporaryPath =
                    prototypeRecipesOutputPath + ".tmp";
                File.WriteAllBytes(prototypeRecipesTemporaryPath, prototypeRecipesJson);
                if (File.Exists(prototypeRecipesOutputPath))
                    File.Delete(prototypeRecipesOutputPath);
                File.Move(prototypeRecipesTemporaryPath, prototypeRecipesOutputPath);

                string logicalPlacementsTemporaryPath =
                    logicalPlacementsOutputPath + ".tmp";
                File.WriteAllBytes(logicalPlacementsTemporaryPath, logicalPlacementsJson);
                if (File.Exists(logicalPlacementsOutputPath))
                    File.Delete(logicalPlacementsOutputPath);
                File.Move(logicalPlacementsTemporaryPath, logicalPlacementsOutputPath);

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
                    $"prototypes={report.prototypeCount} parts={report.prototypePartCount} " +
                    $"report={ReportPath} sourceRows={SourceRowsPath} " +
                    $"prototypeRecipes={PrototypeRecipesPath} " +
                    $"logicalPlacements={LogicalPlacementsPath}");
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
            var eligiblePartCandidates = new List<EligiblePartCandidate>();
            int eligible = 0;
            int stableOwnerJoined = 0;
            int eligibleStableOwnerJoined = 0;

            foreach (Row row in rows)
            {
                bool repeated =
                    signatureCounts.TryGetValue(row.Signature, out int signatureCount) &&
                    signatureCount > 1;
                bool policySupported = TryClassifyPolicy(
                    row.Renderer,
                    row.Material,
                    out string policy,
                    out OperationMapRenderPolicyKey policyKey);
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
                if (isEligible)
                {
                    eligiblePartCandidates.Add(
                        BuildEligiblePartCandidate(row, sourceRow, policyKey));
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
            PrototypeRecipeDocument prototypeRecipes =
                BuildPrototypeRecipes(eligiblePartCandidates);
            if (prototypeRecipes.eligibleSourceRowCount != eligible)
            {
                throw new InvalidOperationException(
                    $"Prototype recipes consumed {prototypeRecipes.eligibleSourceRowCount} " +
                    $"eligible rows, expected {eligible}.");
            }

            return new InventoryReport
            {
                schema = "warline.operation-map.render-virtualization-eligibility-inventory",
                schemaVersion = 4,
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
                logicalPlacementCount = prototypeRecipes.logicalPlacementCount,
                prototypeCount = prototypeRecipes.prototypeCount,
                prototypePartCount = prototypeRecipes.prototypePartCount,
                prototypeRecipeRowsConsumed = prototypeRecipes.eligibleSourceRowCount,
                prototypeRecipesSha256 = prototypeRecipes.prototypeRecipesSha256,
                placementCount = prototypeRecipes.logicalPlacements.placementCount,
                stateOwnerCount = prototypeRecipes.logicalPlacements.stateOwnerCount,
                stateLinkedPlacementCount =
                    prototypeRecipes.logicalPlacements.stateLinkedPlacementCount,
                renderOnlyPlacementCount =
                    prototypeRecipes.logicalPlacements.renderOnlyPlacementCount,
                placementPartRowCount =
                    prototypeRecipes.logicalPlacements.placementPartRowCount,
                logicalPlacementsSha256 =
                    prototypeRecipes.logicalPlacements.logicalPlacementsSha256,
                mutationAuthorized = false,
                mutationBlocker =
                    "VRP-002 raw Android profile remains open; source-row inventory authorizes no mutation.",
                bySemanticCategory = BuildBreakdown(semantic),
                byPrototypeSignature = BuildBreakdown(signatures),
                byRendererType = BuildBreakdown(rendererTypes),
                byPolicyBucket = BuildBreakdown(policies),
                byGameplayOwnership = BuildBreakdown(ownership),
                byReasonCode = BuildBreakdown(reasons),
                sourceRows = sourceRows,
                prototypeRecipes = prototypeRecipes,
                logicalPlacements = prototypeRecipes.logicalPlacements
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

        private static EligiblePartCandidate BuildEligiblePartCandidate(
            Row row,
            SourceRowReport sourceRow,
            OperationMapRenderPolicyKey policy)
        {
            if (row.DenseOwner == null || !sourceRow.stableOwnerJoined)
            {
                throw new InvalidOperationException(
                    $"Eligible row lacks a dense generated owner: " +
                    $"{sourceRow.logicalRowIdentitySource}");
            }

            Mesh mesh = row.Renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null)
            {
                throw new InvalidOperationException(
                    $"Eligible row lacks MeshFilter source: {sourceRow.logicalRowIdentitySource}");
            }

            Matrix4x4 localToPlacement = GetLocalToAncestor(
                row.DenseOwner.transform,
                row.Renderer.transform);
            Color linearBaseColor =
                row.Material != null && row.Material.HasProperty("_BaseColor")
                    ? row.Material.GetColor("_BaseColor").linear
                    : Color.white.linear;
            var fingerprintInput = new OperationMapRenderPrototypeFingerprintInput
            {
                RendererPath = sourceRow.rendererPath,
                MeshAssetGuid = sourceRow.meshAssetGuid,
                MeshLocalId = sourceRow.meshLocalId,
                MaterialAssetGuid = sourceRow.materialAssetGuid,
                MaterialLocalId = sourceRow.materialLocalId,
                SubMeshIndex = sourceRow.subMeshIndex,
                LocalToPlacement = ToFloat4x4(localToPlacement),
                LocalBounds = new OperationMapRenderBoundsBlob
                {
                    Center = mesh.bounds.center,
                    Extents = mesh.bounds.extents
                },
                LinearBaseColor = new float4(
                    linearBaseColor.r,
                    linearBaseColor.g,
                    linearBaseColor.b,
                    linearBaseColor.a),
                PolicyBucket = policy.Bucket,
                Layer = policy.Layer,
                RenderingLayerMask = policy.RenderingLayerMask,
                MotionVectorMode = policy.MotionVectorMode,
                ShadowFlags = policy.ShadowFlags,
                LodFlags = OperationMapRenderLodFlags.Lod0
            };
            if (!OperationMapRenderPrototypeFingerprint.TryCompute(
                    fingerprintInput,
                    out OperationMapRenderIdentity128 partFingerprint,
                    out string fingerprintError))
            {
                throw new InvalidOperationException(
                    $"Eligible part fingerprint rejected: {fingerprintError} " +
                    $"row={sourceRow.logicalRowIdentitySource}");
            }

            string partCanonicalSource = BuildPartCanonicalSource(
                sourceRow,
                localToPlacement,
                mesh.bounds,
                linearBaseColor,
                policy);
            return new EligiblePartCandidate
            {
                OwnerIdentitySource = sourceRow.ownerIdentitySource,
                SemanticCategory = row.DenseOwner.Category,
                SourceRow = sourceRow,
                LocalToPlacement = localToPlacement,
                LocalBounds = mesh.bounds,
                LinearBaseColor = linearBaseColor,
                Policy = policy,
                LodFlags = OperationMapRenderLodFlags.Lod0,
                PartFingerprint = partFingerprint,
                PartCanonicalSource = partCanonicalSource,
                OwnerStableId = sourceRow.ownerStableId,
                OwnerIdentity = new OperationMapRenderIdentity128
                {
                    Low = sourceRow.ownerIdentityLow,
                    High = sourceRow.ownerIdentityHigh
                },
                OwnerRole = row.DenseOwner.Role,
                PlacementWorldMatrix = row.DenseOwner.transform.localToWorldMatrix
            };
        }

        private static PrototypeRecipeDocument BuildPrototypeRecipes(
            IReadOnlyList<EligiblePartCandidate> candidates)
        {
            var partCollisions = new OperationMapRenderIdentityCollisionDetector();
            foreach (EligiblePartCandidate candidate in candidates)
            {
                if (!partCollisions.TryRegister(
                        candidate.PartFingerprint,
                        candidate.PartCanonicalSource,
                        out string collisionError))
                {
                    throw new InvalidOperationException(collisionError);
                }
            }

            List<OwnerRecipe> owners = candidates
                .GroupBy(candidate => candidate.OwnerIdentitySource, StringComparer.Ordinal)
                .Select(group => BuildOwnerRecipe(group.Key, group.ToList()))
                .OrderBy(owner => owner.OwnerIdentitySource, StringComparer.Ordinal)
                .ToList();

            var prototypeCollisions = new OperationMapRenderIdentityCollisionDetector();
            var byPrototypeSource =
                new Dictionary<string, PrototypeAccumulator>(StringComparer.Ordinal);
            var prototypeSourceByOwner =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (OwnerRecipe owner in owners)
            {
                string prototypeSource = BuildPrototypeCanonicalSource(owner);
                prototypeSourceByOwner.Add(owner.OwnerIdentitySource, prototypeSource);
                OperationMapRenderIdentity128 prototypeIdentity =
                    ProjectAndRegister(prototypeSource, prototypeCollisions);
                if (!byPrototypeSource.TryGetValue(
                        prototypeSource,
                        out PrototypeAccumulator accumulator))
                {
                    accumulator = new PrototypeAccumulator
                    {
                        PrototypeSource = prototypeSource,
                        PrototypeIdentity = prototypeIdentity,
                        SemanticCategory = owner.SemanticCategory,
                        Parts = owner.Parts
                    };
                    byPrototypeSource.Add(prototypeSource, accumulator);
                }
                accumulator.PlacementCount++;
            }

            List<PrototypeAccumulator> ordered = byPrototypeSource.Values
                .OrderBy(prototype => prototype.PrototypeIdentity.Low)
                .ThenBy(prototype => prototype.PrototypeIdentity.High)
                .ToList();
            var prototypeIndexBySource = ordered
                .Select((prototype, index) => new { prototype.PrototypeSource, Index = index })
                .ToDictionary(pair => pair.PrototypeSource, pair => pair.Index, StringComparer.Ordinal);
            var prototypes = new List<PrototypeRecipeReport>(ordered.Count);
            var parts = new List<PrototypePartRecipeReport>();
            foreach (PrototypeAccumulator prototype in ordered)
            {
                int firstPart = parts.Count;
                Bounds combinedBounds = default;
                bool hasBounds = false;
                foreach (EligiblePartCandidate part in prototype.Parts)
                {
                    Bounds transformed = TransformBounds(
                        part.LocalBounds,
                        part.LocalToPlacement);
                    if (hasBounds)
                        combinedBounds.Encapsulate(transformed);
                    else
                    {
                        combinedBounds = transformed;
                        hasBounds = true;
                    }
                    parts.Add(BuildPartReport(parts.Count, prototypes.Count, part));
                }

                if (!hasBounds)
                    throw new InvalidOperationException("Prototype contains no part bounds.");
                prototypes.Add(new PrototypeRecipeReport
                {
                    prototypeIndex = prototypes.Count,
                    prototypeIdentityLow = prototype.PrototypeIdentity.Low,
                    prototypeIdentityHigh = prototype.PrototypeIdentity.High,
                    prototypeCanonicalSource = prototype.PrototypeSource,
                    semanticCategory = prototype.SemanticCategory.ToString(),
                    placementCount = prototype.PlacementCount,
                    firstPart = firstPart,
                    partCount = prototype.Parts.Count,
                    combinedLocalBoundsCenter = ToArray(combinedBounds.center),
                    combinedLocalBoundsExtents = ToArray(combinedBounds.extents)
                });
            }

            string recipesHash = ComputePrototypeRecipesSha256(prototypes, parts);
            LogicalPlacementDocument logicalPlacements = BuildLogicalPlacements(
                owners,
                prototypeSourceByOwner,
                prototypeIndexBySource,
                prototypes);
            return new PrototypeRecipeDocument
            {
                schema = "warline.operation-map.render-virtualization-prototype-recipes",
                schemaVersion = 1,
                operationMapId = "opmap.skirmish.desert_base_01",
                result = "Passed",
                logicalPlacementCount = owners.Count,
                prototypeCount = prototypes.Count,
                prototypePartCount = parts.Count,
                eligibleSourceRowCount = candidates.Count,
                prototypeRecipesSha256 = recipesHash,
                prototypes = prototypes,
                parts = parts,
                logicalPlacements = logicalPlacements
            };
        }

        private static OwnerRecipe BuildOwnerRecipe(
            string ownerIdentitySource,
            List<EligiblePartCandidate> parts)
        {
            if (parts.Count == 0)
                throw new InvalidOperationException("Eligible owner recipe contains no parts.");
            parts.Sort(EligiblePartCandidateComparer.Instance);
            DenseCityPresentationSemanticCategory category = parts[0].SemanticCategory;
            if (parts.Any(part => part.SemanticCategory != category))
            {
                throw new InvalidOperationException(
                    $"Eligible owner spans semantic categories: {ownerIdentitySource}");
            }
            if (parts.Any(part =>
                    !string.Equals(
                        part.OwnerStableId,
                        parts[0].OwnerStableId,
                        StringComparison.Ordinal) ||
                    part.OwnerIdentity.Low != parts[0].OwnerIdentity.Low ||
                    part.OwnerIdentity.High != parts[0].OwnerIdentity.High ||
                    part.OwnerRole != parts[0].OwnerRole ||
                    part.PlacementWorldMatrix != parts[0].PlacementWorldMatrix))
            {
                throw new InvalidOperationException(
                    $"Eligible owner contains inconsistent placement ownership: {ownerIdentitySource}");
            }
            return new OwnerRecipe
            {
                OwnerIdentitySource = ownerIdentitySource,
                OwnerStableId = parts[0].OwnerStableId,
                OwnerIdentity = parts[0].OwnerIdentity,
                OwnerRole = parts[0].OwnerRole,
                WorldMatrix = parts[0].PlacementWorldMatrix,
                SemanticCategory = category,
                Parts = parts
            };
        }

        private static LogicalPlacementDocument BuildLogicalPlacements(
            IReadOnlyList<OwnerRecipe> owners,
            IReadOnlyDictionary<string, string> prototypeSourceByOwner,
            IReadOnlyDictionary<string, int> prototypeIndexBySource,
            IReadOnlyList<PrototypeRecipeReport> prototypes)
        {
            List<OwnerRecipe> orderedOwners = owners
                .OrderBy(owner => owner.OwnerIdentity.Low)
                .ThenBy(owner => owner.OwnerIdentity.High)
                .ToList();
            var identitySources = new HashSet<string>(StringComparer.Ordinal);
            var identityPairs = new HashSet<string>(StringComparer.Ordinal);
            var placements = new List<LogicalPlacementReport>(orderedOwners.Count);
            int placementPartRows = 0;
            for (int index = 0; index < orderedOwners.Count; index++)
            {
                OwnerRecipe owner = orderedOwners[index];
                if (owner.OwnerRole != OperationMapEntityPresentationRole.RenderOnly)
                {
                    throw new InvalidOperationException(
                        $"Current eligible placement is not render-only: " +
                        $"{owner.OwnerIdentitySource} role={owner.OwnerRole}.");
                }
                if (!identitySources.Add(owner.OwnerIdentitySource))
                {
                    throw new InvalidOperationException(
                        $"Duplicate placement identity source: {owner.OwnerIdentitySource}");
                }
                string identityPair =
                    owner.OwnerIdentity.Low.ToString(CultureInfo.InvariantCulture) +
                    ":" +
                    owner.OwnerIdentity.High.ToString(CultureInfo.InvariantCulture);
                if (!identityPairs.Add(identityPair))
                {
                    throw new InvalidOperationException(
                        $"Placement identity collision: {identityPair}");
                }

                string prototypeSource = prototypeSourceByOwner[owner.OwnerIdentitySource];
                int prototypeIndex = prototypeIndexBySource[prototypeSource];
                PrototypeRecipeReport prototype = prototypes[prototypeIndex];
                placementPartRows += prototype.partCount;
                placements.Add(new LogicalPlacementReport
                {
                    placementIndex = index,
                    stableOwnerId = owner.OwnerStableId,
                    stableIdentityLow = owner.OwnerIdentity.Low,
                    stableIdentityHigh = owner.OwnerIdentity.High,
                    prototypeIndex = prototypeIndex,
                    worldMatrix = ToArray(owner.WorldMatrix),
                    cellIndex = -1,
                    stateOwnerIndex = -1,
                    requiredVisualState = OperationMapRenderVisualState.Any.ToString(),
                    priority = 0,
                    semanticCategory = owner.SemanticCategory.ToString()
                });
            }

            string placementsHash = ComputeLogicalPlacementsSha256(placements);
            return new LogicalPlacementDocument
            {
                schema = "warline.operation-map.render-virtualization-logical-placements",
                schemaVersion = 1,
                operationMapId = "opmap.skirmish.desert_base_01",
                result = "Passed",
                placementCount = placements.Count,
                stateOwnerCount = 0,
                stateLinkedPlacementCount = 0,
                renderOnlyPlacementCount = placements.Count,
                placementPartRowCount = placementPartRows,
                logicalPlacementsSha256 = placementsHash,
                stateRelationshipPolicy =
                    "Current Vegetation/Prop pilot is render-only: stateOwnerIndex=-1, " +
                    "RequiredVisualState=Any. Building state linkage remains deferred.",
                placements = placements,
                stateOwners = new List<LogicalStateOwnerReport>()
            };
        }

        private static string BuildPrototypeCanonicalSource(OwnerRecipe owner)
        {
            var builder = new StringBuilder(owner.Parts.Count * 512);
            AppendCanonical(builder, "prototype-recipe-v1");
            AppendCanonical(builder, (int)owner.SemanticCategory);
            AppendCanonical(builder, owner.Parts.Count);
            foreach (EligiblePartCandidate part in owner.Parts)
                AppendCanonical(builder, part.PartCanonicalSource);
            return builder.ToString();
        }

        private static string BuildPartCanonicalSource(
            SourceRowReport row,
            Matrix4x4 localToPlacement,
            Bounds localBounds,
            Color linearBaseColor,
            OperationMapRenderPolicyKey policy)
        {
            var builder = new StringBuilder(768);
            AppendCanonical(builder, "prototype-part-v1");
            AppendCanonical(builder, row.rendererPath);
            AppendCanonical(builder, row.meshAssetGuid);
            AppendCanonical(builder, row.meshLocalId);
            AppendCanonical(builder, row.materialAssetGuid);
            AppendCanonical(builder, row.materialLocalId);
            AppendCanonical(builder, row.subMeshIndex);
            for (int index = 0; index < 16; index++)
                AppendCanonical(builder, localToPlacement[index]);
            AppendCanonical(builder, localBounds.center.x);
            AppendCanonical(builder, localBounds.center.y);
            AppendCanonical(builder, localBounds.center.z);
            AppendCanonical(builder, localBounds.extents.x);
            AppendCanonical(builder, localBounds.extents.y);
            AppendCanonical(builder, localBounds.extents.z);
            AppendCanonical(builder, linearBaseColor.r);
            AppendCanonical(builder, linearBaseColor.g);
            AppendCanonical(builder, linearBaseColor.b);
            AppendCanonical(builder, linearBaseColor.a);
            AppendCanonical(builder, (int)policy.Bucket);
            AppendCanonical(builder, policy.Layer);
            AppendCanonical(builder, policy.RenderingLayerMask);
            AppendCanonical(builder, (int)policy.MotionVectorMode);
            AppendCanonical(builder, (int)policy.ShadowFlags);
            AppendCanonical(builder, (int)OperationMapRenderLodFlags.Lod0);
            return builder.ToString();
        }

        private static PrototypePartRecipeReport BuildPartReport(
            int partIndex,
            int prototypeIndex,
            EligiblePartCandidate part) =>
            new()
            {
                partIndex = partIndex,
                prototypeIndex = prototypeIndex,
                rendererPath = part.SourceRow.rendererPath,
                partFingerprintLow = part.PartFingerprint.Low,
                partFingerprintHigh = part.PartFingerprint.High,
                meshAssetGuid = part.SourceRow.meshAssetGuid,
                meshLocalId = part.SourceRow.meshLocalId,
                materialAssetGuid = part.SourceRow.materialAssetGuid,
                materialLocalId = part.SourceRow.materialLocalId,
                subMeshIndex = part.SourceRow.subMeshIndex,
                localToPlacement = ToArray(part.LocalToPlacement),
                localBoundsCenter = ToArray(part.LocalBounds.center),
                localBoundsExtents = ToArray(part.LocalBounds.extents),
                linearBaseColor = new[]
                {
                    part.LinearBaseColor.r,
                    part.LinearBaseColor.g,
                    part.LinearBaseColor.b,
                    part.LinearBaseColor.a
                },
                policyBucket = part.Policy.Bucket.ToString(),
                layer = part.Policy.Layer,
                renderingLayerMask = part.Policy.RenderingLayerMask,
                motionVectorMode = part.Policy.MotionVectorMode.ToString(),
                shadowFlags = (byte)part.Policy.ShadowFlags,
                lodFlags = (byte)part.LodFlags
            };

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

        private static bool TryClassifyPolicy(
            Renderer renderer,
            Material material,
            out string policy,
            out OperationMapRenderPolicyKey policyKey)
        {
            policyKey = default;
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
            if (!OperationMapRenderPolicyClassifier.TryClassify(
                    input,
                    out OperationMapRenderPolicyKey key,
                    out string error))
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
            policyKey = key;
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

        private static string ComputePrototypeRecipesSha256(
            IReadOnlyList<PrototypeRecipeReport> prototypes,
            IReadOnlyList<PrototypePartRecipeReport> parts)
        {
            var canonical = new StringBuilder((prototypes.Count + parts.Count) * 512);
            foreach (PrototypeRecipeReport prototype in prototypes)
            {
                AppendCanonical(canonical, prototype.prototypeIndex);
                AppendCanonical(canonical, prototype.prototypeIdentityLow);
                AppendCanonical(canonical, prototype.prototypeIdentityHigh);
                AppendCanonical(canonical, prototype.prototypeCanonicalSource);
                AppendCanonical(canonical, prototype.semanticCategory);
                AppendCanonical(canonical, prototype.placementCount);
                AppendCanonical(canonical, prototype.firstPart);
                AppendCanonical(canonical, prototype.partCount);
                AppendCanonical(canonical, prototype.combinedLocalBoundsCenter);
                AppendCanonical(canonical, prototype.combinedLocalBoundsExtents);
            }
            foreach (PrototypePartRecipeReport part in parts)
            {
                AppendCanonical(canonical, part.partIndex);
                AppendCanonical(canonical, part.prototypeIndex);
                AppendCanonical(canonical, part.rendererPath);
                AppendCanonical(canonical, part.partFingerprintLow);
                AppendCanonical(canonical, part.partFingerprintHigh);
                AppendCanonical(canonical, part.meshAssetGuid);
                AppendCanonical(canonical, part.meshLocalId);
                AppendCanonical(canonical, part.materialAssetGuid);
                AppendCanonical(canonical, part.materialLocalId);
                AppendCanonical(canonical, part.subMeshIndex);
                AppendCanonical(canonical, part.localToPlacement);
                AppendCanonical(canonical, part.localBoundsCenter);
                AppendCanonical(canonical, part.localBoundsExtents);
                AppendCanonical(canonical, part.linearBaseColor);
                AppendCanonical(canonical, part.policyBucket);
                AppendCanonical(canonical, part.layer);
                AppendCanonical(canonical, part.renderingLayerMask);
                AppendCanonical(canonical, part.motionVectorMode);
                AppendCanonical(canonical, part.shadowFlags);
                AppendCanonical(canonical, part.lodFlags);
            }
            return ComputeSha256(Utf8WithoutBom.GetBytes(canonical.ToString()));
        }

        private static string ComputeLogicalPlacementsSha256(
            IReadOnlyList<LogicalPlacementReport> placements)
        {
            var canonical = new StringBuilder(placements.Count * 256);
            foreach (LogicalPlacementReport placement in placements)
            {
                AppendCanonical(canonical, placement.placementIndex);
                AppendCanonical(canonical, placement.stableOwnerId);
                AppendCanonical(canonical, placement.stableIdentityLow);
                AppendCanonical(canonical, placement.stableIdentityHigh);
                AppendCanonical(canonical, placement.prototypeIndex);
                AppendCanonical(canonical, placement.worldMatrix);
                AppendCanonical(canonical, placement.cellIndex);
                AppendCanonical(canonical, placement.stateOwnerIndex);
                AppendCanonical(canonical, placement.requiredVisualState);
                AppendCanonical(canonical, placement.priority);
                AppendCanonical(canonical, placement.semanticCategory);
            }
            return ComputeSha256(Utf8WithoutBom.GetBytes(canonical.ToString()));
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector3 minimum = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                minimum = Vector3.Min(minimum, transformed);
                maximum = Vector3.Max(maximum, transformed);
            }
            var result = new Bounds();
            result.SetMinMax(minimum, maximum);
            return result;
        }

        private static Matrix4x4 GetLocalToAncestor(Transform ancestor, Transform target)
        {
            if (ancestor == null || target == null)
                throw new ArgumentNullException(ancestor == null ? nameof(ancestor) : nameof(target));
            if (ancestor == target)
                return Matrix4x4.identity;

            var chain = new List<Transform>();
            Transform current = target;
            while (current != null && current != ancestor)
            {
                chain.Add(current);
                current = current.parent;
            }
            if (current != ancestor)
            {
                throw new InvalidOperationException(
                    $"Renderer '{GetPath(target)}' is not beneath owner '{GetPath(ancestor)}'.");
            }

            Matrix4x4 result = Matrix4x4.identity;
            for (int index = chain.Count - 1; index >= 0; index--)
            {
                Transform item = chain[index];
                result *= Matrix4x4.TRS(
                    item.localPosition,
                    item.localRotation,
                    item.localScale);
            }
            return result;
        }

        private static float4x4 ToFloat4x4(Matrix4x4 value) =>
            new(
                ToFloat4(value.GetColumn(0)),
                ToFloat4(value.GetColumn(1)),
                ToFloat4(value.GetColumn(2)),
                ToFloat4(value.GetColumn(3)));

        private static float4 ToFloat4(Vector4 value) =>
            new(value.x, value.y, value.z, value.w);

        private static float[] ToArray(Matrix4x4 value)
        {
            var result = new float[16];
            for (int index = 0; index < result.Length; index++)
                result[index] = value[index];
            return result;
        }

        private static float[] ToArray(Vector3 value) =>
            new[] { value.x, value.y, value.z };

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

        private static void AppendCanonical(StringBuilder builder, uint value) =>
            AppendCanonical(builder, value.ToString(CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, float value) =>
            AppendCanonical(builder, value.ToString("R", CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, IReadOnlyList<float> values)
        {
            AppendCanonical(builder, values?.Count ?? -1);
            if (values == null)
                return;
            for (int index = 0; index < values.Count; index++)
                AppendCanonical(builder, values[index]);
        }

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
            public int logicalPlacementCount;
            public int prototypeCount;
            public int prototypePartCount;
            public int prototypeRecipeRowsConsumed;
            public string prototypeRecipesSha256;
            public string prototypeRecipesPath;
            public string prototypeRecipesJsonSha256;
            public int placementCount;
            public int stateOwnerCount;
            public int stateLinkedPlacementCount;
            public int renderOnlyPlacementCount;
            public int placementPartRowCount;
            public string logicalPlacementsSha256;
            public string logicalPlacementsPath;
            public string logicalPlacementsJsonSha256;
            public bool mutationAuthorized;
            public string mutationBlocker;
            public List<Breakdown> bySemanticCategory;
            public List<Breakdown> byPrototypeSignature;
            public List<Breakdown> byRendererType;
            public List<Breakdown> byPolicyBucket;
            public List<Breakdown> byGameplayOwnership;
            public List<Breakdown> byReasonCode;
            [NonSerialized] public List<SourceRowReport> sourceRows;
            [NonSerialized] public PrototypeRecipeDocument prototypeRecipes;
            [NonSerialized] public LogicalPlacementDocument logicalPlacements;
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
        private sealed class PrototypeRecipeDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public int logicalPlacementCount;
            public int prototypeCount;
            public int prototypePartCount;
            public int eligibleSourceRowCount;
            public string prototypeRecipesSha256;
            public List<PrototypeRecipeReport> prototypes;
            public List<PrototypePartRecipeReport> parts;
            [NonSerialized] public LogicalPlacementDocument logicalPlacements;
        }

        [Serializable]
        private sealed class LogicalPlacementDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public int placementCount;
            public int stateOwnerCount;
            public int stateLinkedPlacementCount;
            public int renderOnlyPlacementCount;
            public int placementPartRowCount;
            public string logicalPlacementsSha256;
            public string stateRelationshipPolicy;
            public List<LogicalPlacementReport> placements;
            public List<LogicalStateOwnerReport> stateOwners;
        }

        [Serializable]
        private sealed class LogicalPlacementReport
        {
            public int placementIndex;
            public string stableOwnerId;
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
            public int prototypeIndex;
            public float[] worldMatrix;
            public int cellIndex;
            public int stateOwnerIndex;
            public string requiredVisualState;
            public int priority;
            public string semanticCategory;
        }

        [Serializable]
        private sealed class LogicalStateOwnerReport
        {
            public int stateOwnerIndex;
            public string stableGameplayOwnerId;
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
        }

        [Serializable]
        private sealed class PrototypeRecipeReport
        {
            public int prototypeIndex;
            public ulong prototypeIdentityLow;
            public ulong prototypeIdentityHigh;
            public string prototypeCanonicalSource;
            public string semanticCategory;
            public int placementCount;
            public int firstPart;
            public int partCount;
            public float[] combinedLocalBoundsCenter;
            public float[] combinedLocalBoundsExtents;
        }

        [Serializable]
        private sealed class PrototypePartRecipeReport
        {
            public int partIndex;
            public int prototypeIndex;
            public string rendererPath;
            public ulong partFingerprintLow;
            public ulong partFingerprintHigh;
            public string meshAssetGuid;
            public long meshLocalId;
            public string materialAssetGuid;
            public long materialLocalId;
            public int subMeshIndex;
            public float[] localToPlacement;
            public float[] localBoundsCenter;
            public float[] localBoundsExtents;
            public float[] linearBaseColor;
            public string policyBucket;
            public int layer;
            public uint renderingLayerMask;
            public string motionVectorMode;
            public byte shadowFlags;
            public byte lodFlags;
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

        private sealed class EligiblePartCandidate
        {
            internal string OwnerIdentitySource;
            internal DenseCityPresentationSemanticCategory SemanticCategory;
            internal SourceRowReport SourceRow;
            internal Matrix4x4 LocalToPlacement;
            internal Bounds LocalBounds;
            internal Color LinearBaseColor;
            internal OperationMapRenderPolicyKey Policy;
            internal OperationMapRenderLodFlags LodFlags;
            internal OperationMapRenderIdentity128 PartFingerprint;
            internal string PartCanonicalSource;
            internal string OwnerStableId;
            internal OperationMapRenderIdentity128 OwnerIdentity;
            internal OperationMapEntityPresentationRole OwnerRole;
            internal Matrix4x4 PlacementWorldMatrix;
        }

        private sealed class OwnerRecipe
        {
            internal string OwnerIdentitySource;
            internal string OwnerStableId;
            internal OperationMapRenderIdentity128 OwnerIdentity;
            internal OperationMapEntityPresentationRole OwnerRole;
            internal Matrix4x4 WorldMatrix;
            internal DenseCityPresentationSemanticCategory SemanticCategory;
            internal List<EligiblePartCandidate> Parts;
        }

        private sealed class PrototypeAccumulator
        {
            internal string PrototypeSource;
            internal OperationMapRenderIdentity128 PrototypeIdentity;
            internal DenseCityPresentationSemanticCategory SemanticCategory;
            internal List<EligiblePartCandidate> Parts;
            internal int PlacementCount;
        }

        private sealed class EligiblePartCandidateComparer : IComparer<EligiblePartCandidate>
        {
            internal static readonly EligiblePartCandidateComparer Instance = new();

            public int Compare(EligiblePartCandidate left, EligiblePartCandidate right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;
                int result = string.CompareOrdinal(
                    left.SourceRow.rendererPath,
                    right.SourceRow.rendererPath);
                if (result != 0)
                    return result;
                result = left.SourceRow.subMeshIndex.CompareTo(right.SourceRow.subMeshIndex);
                if (result != 0)
                    return result;
                result = left.PartFingerprint.Low.CompareTo(right.PartFingerprint.Low);
                return result != 0
                    ? result
                    : left.PartFingerprint.High.CompareTo(right.PartFingerprint.High);
            }
        }
    }
}

#endif
