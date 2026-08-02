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
        internal const string SpatialCellsPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_spatial_cells.json";
        internal const string CapacityBudgetPath =
            "Design/AgentReports/2026-07-28_dense_city_render_virtualization_capacity_budget.json";
        internal const int HistoricalPackedRenderRowCount = 82797;
        internal const int ExpectedEmbeddedDestroyedDuplicateRowRemoval = 4396;
        internal const int ExpectedPackedRenderRowCount =
            HistoricalPackedRenderRowCount - ExpectedEmbeddedDestroyedDuplicateRowRemoval;
        internal const float RenderCellSize = 32f;
        // VRP-067 device evidence (House, stationary interior route) materialized
        // the inclusive envelope x=42..53, z=19..27. Sweep that exact 12x9
        // footprint and its quarter-turn orientation across the whole grid so
        // pool sizing cannot depend on the sampled building's map position.
        private const int ObservedMaterializedEnvelopeWidthCells = 12;
        private const int ObservedMaterializedEnvelopeHeightCells = 9;
        private const int ObservedMaterializedEnvelopeOrientationCount = 2;
        private const int MapMmiEntityLimit = 24000;
        private const int ActiveProxySlotLimit = 8000;
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
                string spatialCellsOutputPath =
                    Path.Combine(projectRoot, SpatialCellsPath);
                string capacityBudgetOutputPath =
                    Path.Combine(projectRoot, CapacityBudgetPath);
                CapacityBudgetDocument capacityBudget = BuildCapacityBudget(
                    report.prototypeRecipes,
                    report.logicalPlacements,
                    report.spatialCells,
                    report.sourceRowsSha256,
                    report.prototypeRecipesSha256,
                    report.logicalPlacementsSha256,
                    report.spatialCellsSha256);
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

                byte[] spatialCellsJson = Utf8WithoutBom.GetBytes(
                    JsonUtility.ToJson(report.spatialCells, true) + "\n");
                report.spatialCellsPath = SpatialCellsPath;
                report.spatialCellsJsonSha256 = ComputeSha256(spatialCellsJson);
                report.spatialCells = null;

                byte[] capacityBudgetJson = Utf8WithoutBom.GetBytes(
                    JsonUtility.ToJson(capacityBudget, true) + "\n");

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

                string spatialCellsTemporaryPath = spatialCellsOutputPath + ".tmp";
                File.WriteAllBytes(spatialCellsTemporaryPath, spatialCellsJson);
                if (File.Exists(spatialCellsOutputPath))
                    File.Delete(spatialCellsOutputPath);
                File.Move(spatialCellsTemporaryPath, spatialCellsOutputPath);

                string capacityBudgetTemporaryPath = capacityBudgetOutputPath + ".tmp";
                File.WriteAllBytes(capacityBudgetTemporaryPath, capacityBudgetJson);
                if (File.Exists(capacityBudgetOutputPath))
                    File.Delete(capacityBudgetOutputPath);
                File.Move(capacityBudgetTemporaryPath, capacityBudgetOutputPath);

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
                    $"logicalPlacements={LogicalPlacementsPath} " +
                    $"spatialCells={SpatialCellsPath} " +
                    $"capacityBudget={CapacityBudgetPath} " +
                    $"capacitySlots={capacityBudget.totalProvisionalActiveProxySlots}");
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
                OperationMapBuildingAuthoring buildingOwner =
                    renderer.GetComponentInParent<OperationMapBuildingAuthoring>(true);
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
                    rows.Add(new Row(
                        renderer,
                        materials[subMesh],
                        subMesh,
                        signature,
                        denseOwner,
                        acceptedOwner,
                        buildingOwner));
                }
            }

            var semantic = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var signatures = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var rendererTypes = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var policies = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var materialSurfaces = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var policyDispositions = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var ownership = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var reasons = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var buildingFamilies = new Dictionary<string, CountPair>(StringComparer.Ordinal);
            var sourceRows = new List<SourceRowReport>(rows.Count);
            var ownerIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var rendererPathIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var logicalRowIdentityCollisions = new OperationMapRenderIdentityCollisionDetector();
            var logicalRowSources = new HashSet<string>(StringComparer.Ordinal);
            var eligiblePartCandidates = new List<EligiblePartCandidate>();
            int eligible = 0;
            int stableOwnerJoined = 0;
            int eligibleStableOwnerJoined = 0;
            Dictionary<BuildingRole, BuildingFamilyDecision> buildingFamilyDecisions =
                BuildBuildingFamilyDecisions(rows, signatureCounts);
            Dictionary<DenseCityPresentationIdentityAuthoring,
                    OperationMapRenderInfrastructureEligibilityDecision>
                infrastructureOwnerDecisions = BuildInfrastructureOwnerDecisions(rows);

            foreach (Row row in rows)
            {
                bool repeated =
                    signatureCounts.TryGetValue(row.Signature, out int signatureCount) &&
                    signatureCount > 1;
                bool policySupported = TryClassifyPolicy(
                    row.Renderer,
                    row.Material,
                    out string materialSurface,
                    out string policy,
                    out OperationMapRenderPolicyKey policyKey);
                string reason = Classify(
                    row,
                    repeated,
                    policySupported,
                    buildingFamilyDecisions,
                    infrastructureOwnerDecisions);
                bool isEligible = string.Equals(reason, "eligible", StringComparison.Ordinal);
                if (isEligible)
                    eligible++;

                Increment(semantic, Semantic(row), isEligible);
                Increment(signatures, row.Signature, isEligible);
                Increment(rendererTypes, row.Renderer.GetType().FullName ?? row.Renderer.GetType().Name, isEligible);
                Increment(policies, policy, isEligible);
                Increment(materialSurfaces, materialSurface, isEligible);
                Increment(
                    policyDispositions,
                    PolicyDisposition(policySupported, materialSurface, policyKey),
                    isEligible);
                Increment(ownership, Ownership(row), isEligible);
                Increment(reasons, reason, isEligible);
                if (row.IsGeneratedBuildingFamily)
                    Increment(buildingFamilies, row.BuildingFamilyName, isEligible);

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

            if (rows.Count != ExpectedPackedRenderRowCount ||
                HistoricalPackedRenderRowCount - rows.Count !=
                    ExpectedEmbeddedDestroyedDuplicateRowRemoval)
            {
                throw new InvalidOperationException(
                    "Normalized authoring render rows did not reconcile to the historical " +
                    $"packed evidence: current={rows.Count}, " +
                    $"expectedCurrent={ExpectedPackedRenderRowCount}, " +
                    $"historical={HistoricalPackedRenderRowCount}, " +
                    $"expectedDuplicateRemoval={ExpectedEmbeddedDestroyedDuplicateRowRemoval}.");
            }
            if (eligibleStableOwnerJoined != eligible)
            {
                throw new InvalidOperationException(
                    $"Every eligible row requires an exact stable-owner join: " +
                    $"{eligibleStableOwnerJoined} != {eligible}.");
            }
            RequireCompleteSelectedBuildingFamilies(
                rows,
                sourceRows,
                buildingFamilyDecisions);

            sourceRows.Sort(SourceRowReportComparer.Instance);
            for (int index = 0; index < sourceRows.Count; index++)
                sourceRows[index].sourceRowIndex = index;
            RenderGridSpec renderGrid = ResolveRenderGrid();
            PrototypeRecipeDocument prototypeRecipes =
                BuildPrototypeRecipes(eligiblePartCandidates, renderGrid);
            if (prototypeRecipes.eligibleSourceRowCount != eligible)
            {
                throw new InvalidOperationException(
                    $"Prototype recipes consumed {prototypeRecipes.eligibleSourceRowCount} " +
                    $"eligible rows, expected {eligible}.");
            }

            return new InventoryReport
            {
                schema = "warline.operation-map.render-virtualization-eligibility-inventory",
                schemaVersion = 6,
                result = "Passed",
                operationMapId = "opmap.skirmish.desert_base_01",
                candidateScenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                candidateSceneDependencyHash = AssetDatabase
                    .GetAssetDependencyHash(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)
                    .ToString(),
                packedEvidencePath =
                    "Design/AgentReports/2026-07-25_dense_city_packed_asset_sharing.json",
                packedEvidenceRenderRows = HistoricalPackedRenderRowCount,
                normalizedEmbeddedDestroyedDuplicateRowsRemoved =
                    ExpectedEmbeddedDestroyedDuplicateRowRemoval,
                intactVisualNormalizationResult = "Passed",
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
                renderCellSize = prototypeRecipes.logicalPlacements.spatialCells.cellSize,
                acceptedRenderGridOrigin =
                    prototypeRecipes.logicalPlacements.spatialCells.acceptedGridOrigin,
                renderGridOrigin =
                    prototypeRecipes.logicalPlacements.spatialCells.gridOrigin,
                renderGridDimensions =
                    prototypeRecipes.logicalPlacements.spatialCells.gridDimensions,
                renderGridCoordinateOffset =
                    prototypeRecipes.logicalPlacements.spatialCells.coordinateOffset,
                occupiedCellCount =
                    prototypeRecipes.logicalPlacements.spatialCells.occupiedCellCount,
                cellPlacementIndexCount =
                    prototypeRecipes.logicalPlacements.spatialCells.cellPlacementIndexCount,
                multiCellPlacementCount =
                    prototypeRecipes.logicalPlacements.spatialCells.multiCellPlacementCount,
                maximumCellsPerPlacement =
                    prototypeRecipes.logicalPlacements.spatialCells.maximumCellsPerPlacement,
                maximumPlacementsPerCell =
                    prototypeRecipes.logicalPlacements.spatialCells.maximumPlacementsPerCell,
                spatialCellsSha256 =
                    prototypeRecipes.logicalPlacements.spatialCells.spatialCellsSha256,
                mutationAuthorized = true,
                mutationBlocker = string.Empty,
                buildingFamilySelectionPolicy =
                    "Generated building families are selected atomically only when repeated, " +
                    "every canonical owner has intact and destroyed recipes, and every family " +
                    "row satisfies the closed renderer and render-policy schema.",
                infrastructureSelectionPolicy =
                    "Generated render-only Infrastructure rows are selected only when their " +
                    "stable owner hierarchy contains transforms, mesh filters, plain mesh " +
                    "renderers, and its single dense identity. Animation, lights, particles, " +
                    "physics/gameplay, LOD-special, custom behavior, unsupported renderers, " +
                    "and per-instance material-property blocks fail closed by stable reason; " +
                    "the existing repeated-signature and fixed render-policy gates still apply.",
                materialSurfaceSelectionPolicy =
                    "Opaque and alpha-clipped materials map to separate fixed shadow-on/off " +
                    "buckets. Transparent materials are eligible only with cast/static shadows " +
                    "disabled; transparent shadow-casting rows remain resident under the stable " +
                    "unsupported-render-policy reason.",
                infrastructureOwners = infrastructureOwnerDecisions
                    .OrderBy(pair => pair.Key.StableId, StringComparer.Ordinal)
                    .Select(pair => new InfrastructureOwnerReport
                    {
                        stableId = pair.Key.StableId,
                        selected = pair.Value.Selected,
                        reasonCode = pair.Value.ReasonCode
                    })
                    .ToList(),
                buildingFamilies = buildingFamilyDecisions.Values
                    .OrderBy(decision => (byte)decision.Role)
                    .Select(decision => decision.ToReport())
                    .ToList(),
                byBuildingFamily = BuildBreakdown(buildingFamilies),
                bySemanticCategory = BuildBreakdown(semantic),
                byPrototypeSignature = BuildBreakdown(signatures),
                byRendererType = BuildBreakdown(rendererTypes),
                byPolicyBucket = BuildBreakdown(policies),
                byMaterialSurface = BuildBreakdown(materialSurfaces),
                byPolicyDisposition = BuildBreakdown(policyDispositions),
                byGameplayOwnership = BuildBreakdown(ownership),
                byReasonCode = BuildBreakdown(reasons),
                sourceRows = sourceRows,
                prototypeRecipes = prototypeRecipes,
                logicalPlacements = prototypeRecipes.logicalPlacements,
                spatialCells = prototypeRecipes.logicalPlacements.spatialCells
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
                buildingFamily = row.IsGeneratedBuildingFamily
                    ? row.BuildingFamilyName
                    : string.Empty,
                buildingVisualState = row.BuildingVisualState.ToString(),
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
                RecipeKey = row.BuildingVisualState == OperationMapRenderVisualState.Any
                    ? sourceRow.ownerIdentitySource
                    : sourceRow.ownerIdentitySource + "|visual-state=" +
                      row.BuildingVisualState,
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
                PlacementWorldMatrix = row.DenseOwner.transform.localToWorldMatrix,
                VisualState = row.BuildingVisualState
            };
        }

        private static Dictionary<BuildingRole, BuildingFamilyDecision>
            BuildBuildingFamilyDecisions(
                IReadOnlyList<Row> rows,
                IReadOnlyDictionary<string, int> signatureCounts)
        {
            var result = new Dictionary<BuildingRole, BuildingFamilyDecision>();
            IEnumerable<IGrouping<BuildingRole, Row>> families = rows
                .Where(row => row.IsGeneratedBuildingFamily)
                .GroupBy(row => row.BuildingFamilyRole)
                .OrderBy(group => (byte)group.Key);
            foreach (IGrouping<BuildingRole, Row> family in families)
            {
                List<Row> familyRows = family.ToList();
                List<IGrouping<OperationMapBuildingAuthoring, Row>> owners = familyRows
                    .GroupBy(row => row.BuildingOwner)
                    .ToList();
                int completeOwnerCount = owners.Count(owner =>
                    owner.Any(row => row.BuildingVisualState == OperationMapRenderVisualState.Intact) &&
                    owner.Any(row => row.BuildingVisualState == OperationMapRenderVisualState.Destroyed));
                int unsupportedRendererRowCount = familyRows.Count(row => row.Renderer is not MeshRenderer);
                int unsupportedPolicyRowCount = familyRows.Count(row =>
                    !TryClassifyPolicy(row.Renderer, row.Material, out _, out _, out _));
                int repeatedSignatureRowCount = familyRows.Count(row =>
                    signatureCounts.TryGetValue(row.Signature, out int count) && count > 1);
                bool supportedRole = family.Key is BuildingRole.House or BuildingRole.Shop or BuildingRole.CityHall;

                string reasonCode;
                bool selected;
                if (!supportedRole)
                {
                    selected = false;
                    reasonCode = "gameplay-building-family-unsupported-role";
                }
                else if (owners.Count < 2)
                {
                    selected = false;
                    reasonCode = "gameplay-building-family-not-repeated";
                }
                else if (completeOwnerCount != owners.Count)
                {
                    selected = false;
                    reasonCode = "gameplay-building-family-incomplete-state-coverage";
                }
                else if (unsupportedRendererRowCount > 0)
                {
                    selected = false;
                    reasonCode = "gameplay-building-family-unsupported-renderer";
                }
                else if (unsupportedPolicyRowCount > 0)
                {
                    selected = false;
                    reasonCode = "gameplay-building-family-unsupported-render-policy";
                }
                else
                {
                    selected = true;
                    reasonCode = "eligible";
                }

                result.Add(
                    family.Key,
                    new BuildingFamilyDecision(
                        family.Key,
                        selected,
                        reasonCode,
                        owners.Count,
                        completeOwnerCount,
                        familyRows.Count,
                        familyRows.Count(row => row.BuildingVisualState == OperationMapRenderVisualState.Intact),
                        familyRows.Count(row => row.BuildingVisualState == OperationMapRenderVisualState.Destroyed),
                        repeatedSignatureRowCount,
                        unsupportedRendererRowCount,
                        unsupportedPolicyRowCount));
            }

            return result;
        }

        private static void RequireCompleteSelectedBuildingFamilies(
            IReadOnlyList<Row> rows,
            IReadOnlyList<SourceRowReport> sourceRows,
            IReadOnlyDictionary<BuildingRole, BuildingFamilyDecision> decisions)
        {
            Dictionary<OperationMapBuildingAuthoring, List<int>> selectedOwners = rows
                .Select((row, index) => new { row, index })
                .Where(value =>
                    value.row.IsGeneratedBuildingFamily &&
                    decisions[value.row.BuildingFamilyRole].Selected)
                .GroupBy(value => value.row.BuildingOwner)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(value => value.index).ToList());
            if (selectedOwners.Count == 0)
            {
                throw new InvalidOperationException(
                    "VRP-066 requires at least one complete repeated generated building family.");
            }

            foreach (KeyValuePair<OperationMapBuildingAuthoring, List<int>> pair in selectedOwners)
            {
                bool hasIntact = false;
                bool hasDestroyed = false;
                foreach (int rowIndex in pair.Value)
                {
                    Row row = rows[rowIndex];
                    if (!sourceRows[rowIndex].eligible)
                    {
                        throw new InvalidOperationException(
                            $"VRP-066 selected building family is not complete: " +
                            $"{pair.Key.StableId} contains resident row " +
                            $"{sourceRows[rowIndex].rendererPath} " +
                            $"({sourceRows[rowIndex].reasonCode}).");
                    }
                    hasIntact |= row.BuildingVisualState == OperationMapRenderVisualState.Intact;
                    hasDestroyed |= row.BuildingVisualState == OperationMapRenderVisualState.Destroyed;
                }
                if (!hasIntact || !hasDestroyed)
                {
                    throw new InvalidOperationException(
                        $"VRP-066 building {pair.Key.StableId} requires both intact and " +
                        "destroyed renderer recipes.");
                }
            }
        }

        private static PrototypeRecipeDocument BuildPrototypeRecipes(
            IReadOnlyList<EligiblePartCandidate> candidates,
            in RenderGridSpec renderGrid)
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
                .GroupBy(candidate => candidate.RecipeKey, StringComparer.Ordinal)
                .Select(group => BuildOwnerRecipe(group.Key, group.ToList()))
                .OrderBy(owner => owner.RecipeKey, StringComparer.Ordinal)
                .ToList();

            var prototypeCollisions = new OperationMapRenderIdentityCollisionDetector();
            var byPrototypeSource =
                new Dictionary<string, PrototypeAccumulator>(StringComparer.Ordinal);
            var prototypeSourceByOwner =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (OwnerRecipe owner in owners)
            {
                string prototypeSource = BuildPrototypeCanonicalSource(owner);
                prototypeSourceByOwner.Add(owner.RecipeKey, prototypeSource);
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
                prototypes,
                renderGrid);
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
            string recipeKey,
            List<EligiblePartCandidate> parts)
        {
            if (parts.Count == 0)
                throw new InvalidOperationException("Eligible owner recipe contains no parts.");
            parts.Sort(EligiblePartCandidateComparer.Instance);
            DenseCityPresentationSemanticCategory category = parts[0].SemanticCategory;
            if (parts.Any(part => part.SemanticCategory != category))
            {
                throw new InvalidOperationException(
                    $"Eligible owner spans semantic categories: {recipeKey}");
            }
            if (parts.Any(part =>
                    !string.Equals(
                        part.OwnerStableId,
                        parts[0].OwnerStableId,
                        StringComparison.Ordinal) ||
                    part.OwnerIdentity.Low != parts[0].OwnerIdentity.Low ||
                     part.OwnerIdentity.High != parts[0].OwnerIdentity.High ||
                     part.OwnerRole != parts[0].OwnerRole ||
                     part.VisualState != parts[0].VisualState ||
                     part.PlacementWorldMatrix != parts[0].PlacementWorldMatrix))
            {
                throw new InvalidOperationException(
                    $"Eligible owner contains inconsistent placement ownership: {recipeKey}");
            }
            return new OwnerRecipe
            {
                RecipeKey = recipeKey,
                OwnerIdentitySource = parts[0].OwnerIdentitySource,
                OwnerStableId = parts[0].OwnerStableId,
                OwnerIdentity = parts[0].OwnerIdentity,
                OwnerRole = parts[0].OwnerRole,
                WorldMatrix = parts[0].PlacementWorldMatrix,
                SemanticCategory = category,
                VisualState = parts[0].VisualState,
                PlacementIdentity = parts[0].VisualState ==
                                    OperationMapRenderVisualState.Any
                    ? parts[0].OwnerIdentity
                    : ProjectIdentity(recipeKey),
                Parts = parts
            };
        }

        private static OperationMapRenderIdentity128 ProjectIdentity(string source)
        {
            if (!OperationMapRenderIdentityProjection.TryProject(
                    source,
                    out OperationMapRenderIdentity128 identity,
                    out string error))
                throw new InvalidOperationException(error);
            return identity;
        }

        private static LogicalPlacementDocument BuildLogicalPlacements(
            IReadOnlyList<OwnerRecipe> owners,
            IReadOnlyDictionary<string, string> prototypeSourceByOwner,
            IReadOnlyDictionary<string, int> prototypeIndexBySource,
            IReadOnlyList<PrototypeRecipeReport> prototypes,
            in RenderGridSpec renderGrid)
        {
            List<OwnerRecipe> orderedOwners = owners
                .OrderBy(owner => owner.PlacementIdentity.Low)
                .ThenBy(owner => owner.PlacementIdentity.High)
                .ToList();
            List<OwnerRecipe> statefulOwners = owners
                .Where(owner => owner.VisualState != OperationMapRenderVisualState.Any)
                .GroupBy(owner => owner.OwnerIdentitySource, StringComparer.Ordinal)
                .Select(group =>
                {
                    OperationMapRenderVisualState[] states = group
                        .Select(owner => owner.VisualState)
                        .Distinct()
                        .OrderBy(state => state)
                        .ToArray();
                    if (states.Length != 2 ||
                        states[0] != OperationMapRenderVisualState.Intact ||
                        states[1] != OperationMapRenderVisualState.Destroyed)
                    {
                        throw new InvalidOperationException(
                            $"Stateful building {group.Key} requires one intact and one destroyed recipe.");
                    }
                    return group.First();
                })
                .OrderBy(owner => owner.OwnerIdentity.Low)
                .ThenBy(owner => owner.OwnerIdentity.High)
                .ToList();
            Dictionary<string, int> stateOwnerIndexBySource = statefulOwners
                .Select((owner, index) => new { owner.OwnerIdentitySource, index })
                .ToDictionary(
                    value => value.OwnerIdentitySource,
                    value => value.index,
                    StringComparer.Ordinal);
            List<LogicalStateOwnerReport> stateOwners = statefulOwners
                .Select((owner, index) => new LogicalStateOwnerReport
                {
                    stateOwnerIndex = index,
                    stableGameplayOwnerId = owner.OwnerStableId,
                    stableIdentityLow = owner.OwnerIdentity.Low,
                    stableIdentityHigh = owner.OwnerIdentity.High
                })
                .ToList();
            var identitySources = new HashSet<string>(StringComparer.Ordinal);
            var identityPairs = new HashSet<string>(StringComparer.Ordinal);
            var placements = new List<LogicalPlacementReport>(orderedOwners.Count);
            int placementPartRows = 0;
            for (int index = 0; index < orderedOwners.Count; index++)
            {
                OwnerRecipe owner = orderedOwners[index];
                bool stateful = owner.VisualState != OperationMapRenderVisualState.Any;
                if (stateful
                        ? owner.OwnerRole != OperationMapEntityPresentationRole.GameplayBuildings
                        : owner.OwnerRole != OperationMapEntityPresentationRole.RenderOnly)
                {
                    throw new InvalidOperationException(
                        $"Eligible placement role/state mismatch: " +
                        $"{owner.RecipeKey} role={owner.OwnerRole} state={owner.VisualState}.");
                }
                if (!identitySources.Add(owner.RecipeKey))
                {
                    throw new InvalidOperationException(
                        $"Duplicate placement identity source: {owner.RecipeKey}");
                }
                string identityPair =
                    owner.PlacementIdentity.Low.ToString(CultureInfo.InvariantCulture) +
                    ":" +
                    owner.PlacementIdentity.High.ToString(CultureInfo.InvariantCulture);
                if (!identityPairs.Add(identityPair))
                {
                    throw new InvalidOperationException(
                        $"Placement identity collision: {identityPair}");
                }

                string prototypeSource = prototypeSourceByOwner[owner.RecipeKey];
                int prototypeIndex = prototypeIndexBySource[prototypeSource];
                PrototypeRecipeReport prototype = prototypes[prototypeIndex];
                placementPartRows += prototype.partCount;
                placements.Add(new LogicalPlacementReport
                {
                    placementIndex = index,
                    stableOwnerId = stateful
                        ? owner.OwnerStableId + "#" + owner.VisualState
                        : owner.OwnerStableId,
                    stableIdentityLow = owner.PlacementIdentity.Low,
                    stableIdentityHigh = owner.PlacementIdentity.High,
                    sourceOwnerIdentityLow = owner.OwnerIdentity.Low,
                    sourceOwnerIdentityHigh = owner.OwnerIdentity.High,
                    prototypeIndex = prototypeIndex,
                    worldMatrix = ToArray(owner.WorldMatrix),
                    worldMatrixValue = owner.WorldMatrix,
                    cellIndex = -1,
                    stateOwnerIndex = stateful
                        ? stateOwnerIndexBySource[owner.OwnerIdentitySource]
                        : -1,
                    requiredVisualState = owner.VisualState.ToString(),
                    priority = 0,
                    semanticCategory = owner.SemanticCategory.ToString()
                });
            }

            SpatialCellDocument spatialCells =
                BuildSpatialCells(placements, prototypes, renderGrid);
            string placementsHash = ComputeLogicalPlacementsSha256(placements);
            return new LogicalPlacementDocument
            {
                schema = "warline.operation-map.render-virtualization-logical-placements",
                schemaVersion = 2,
                operationMapId = "opmap.skirmish.desert_base_01",
                result = "Passed",
                placementCount = placements.Count,
                stateOwnerCount = stateOwners.Count,
                stateLinkedPlacementCount = placements.Count(value => value.stateOwnerIndex >= 0),
                renderOnlyPlacementCount = placements.Count(value => value.stateOwnerIndex < 0),
                placementPartRowCount = placementPartRows,
                logicalPlacementsSha256 = placementsHash,
                stateRelationshipPolicy =
                    "Vegetation/Prop placements remain render-only. VRP-063 House intact and " +
                    "destroyed recipes share one deterministic canonical building state owner.",
                placements = placements,
                stateOwners = stateOwners,
                spatialCells = spatialCells
            };
        }

        private static CapacityBudgetDocument BuildCapacityBudget(
            PrototypeRecipeDocument prototypeRecipes,
            LogicalPlacementDocument logicalPlacements,
            SpatialCellDocument spatialCells,
            string sourceRowsSha256,
            string prototypeRecipesSha256,
            string logicalPlacementsSha256,
            string spatialCellsSha256)
        {
            if (prototypeRecipes == null || logicalPlacements == null || spatialCells == null)
                throw new InvalidOperationException("Capacity budget requires the in-memory inventory documents.");

            var policiesByPart = new OperationMapRenderPolicyKey[prototypeRecipes.parts.Count];
            var uniquePolicies = new HashSet<OperationMapRenderPolicyKey>();
            for (int partIndex = 0; partIndex < prototypeRecipes.parts.Count; partIndex++)
            {
                PrototypePartRecipeReport part = prototypeRecipes.parts[partIndex];
                if (!Enum.TryParse(part.policyBucket, out OperationMapRenderPolicyBucket bucket) ||
                    !Enum.TryParse(part.motionVectorMode, out OperationMapRenderMotionVectorMode motionVectorMode))
                {
                    throw new InvalidOperationException(
                        $"Prototype part {partIndex} has an unparseable render policy.");
                }

                var policy = new OperationMapRenderPolicyKey(
                    bucket,
                    part.layer,
                    part.renderingLayerMask,
                    motionVectorMode,
                    (OperationMapRenderShadowFlags)part.shadowFlags);
                if (!OperationMapRenderPolicyClassifier.TryValidate(policy, out string policyError))
                {
                    throw new InvalidOperationException(
                        $"Prototype part {partIndex} has invalid render policy: {policyError}");
                }

                policiesByPart[partIndex] = policy;
                uniquePolicies.Add(policy);
            }

            OperationMapRenderPolicyKey[] orderedPolicies = uniquePolicies.ToArray();
            Array.Sort(orderedPolicies, OperationMapRenderCapacitySweep.ComparePolicies);
            var policyIndexByKey = new Dictionary<OperationMapRenderPolicyKey, int>();
            for (int policyIndex = 0; policyIndex < orderedPolicies.Length; policyIndex++)
                policyIndexByKey.Add(orderedPolicies[policyIndex], policyIndex);

            var policyIndexByPart = new int[policiesByPart.Length];
            for (int partIndex = 0; partIndex < policiesByPart.Length; partIndex++)
                policyIndexByPart[partIndex] = policyIndexByKey[policiesByPart[partIndex]];

            int width = spatialCells.gridDimensions[0];
            int height = spatialCells.gridDimensions[1];
            if (width <= 0 || height <= 0 ||
                spatialCells.gridCellCount != checked(width * height))
            {
                throw new InvalidOperationException("Capacity budget received an invalid spatial grid.");
            }

            var occupiedCellsByLocalIndex = new Dictionary<int, SpatialCellReport>();
            foreach (SpatialCellReport cell in spatialCells.cells)
            {
                int localX = cell.coordinateX - spatialCells.coordinateOffset[0];
                int localZ = cell.coordinateZ - spatialCells.coordinateOffset[1];
                if (localX < 0 || localX >= width || localZ < 0 || localZ >= height)
                    throw new InvalidOperationException($"Spatial cell {cell.cellIndex} is outside its grid.");
                int localIndex = localZ * width + localX;
                if (!occupiedCellsByLocalIndex.TryAdd(localIndex, cell))
                    throw new InvalidOperationException($"Spatial grid cell {localIndex} is duplicated.");
            }

            int sampleShapeCount =
                ObservedMaterializedEnvelopeWidthCells ==
                ObservedMaterializedEnvelopeHeightCells
                    ? 1
                    : ObservedMaterializedEnvelopeOrientationCount;
            var inputs = new List<OperationMapRenderCapacitySweepInput>(
                checked(
                    width * height * sampleShapeCount *
                    orderedPolicies.Length));
            var peakSampleByPolicy = new string[orderedPolicies.Length];
            var peakRowsByPolicy = new int[orderedPolicies.Length];
            var seenPlacementAtSample = new int[logicalPlacements.placements.Count];
            int sampleOrdinal = 0;
            for (int centerZ = 0; centerZ < height; centerZ++)
            {
                for (int centerX = 0; centerX < width; centerX++)
                {
                    for (int orientation = 0;
                         orientation < sampleShapeCount;
                         orientation++)
                    {
                        int envelopeWidth = orientation == 0
                            ? ObservedMaterializedEnvelopeWidthCells
                            : ObservedMaterializedEnvelopeHeightCells;
                        int envelopeHeight = orientation == 0
                            ? ObservedMaterializedEnvelopeHeightCells
                            : ObservedMaterializedEnvelopeWidthCells;
                        string sampleIdentity = string.Format(
                            CultureInfo.InvariantCulture,
                            "grid-cell:{0}:{1}:materialized={2}x{3}",
                            centerX + spatialCells.coordinateOffset[0],
                            centerZ + spatialCells.coordinateOffset[1],
                            envelopeWidth,
                            envelopeHeight);
                        var requiredRowsByPolicy =
                            new int[orderedPolicies.Length];
                        int minimumX = Math.Max(
                            0,
                            centerX - (envelopeWidth - 1) / 2);
                        int maximumX = Math.Min(
                            width - 1,
                            minimumX + envelopeWidth - 1);
                        int minimumZ = Math.Max(
                            0,
                            centerZ - (envelopeHeight - 1) / 2);
                        int maximumZ = Math.Min(
                            height - 1,
                            minimumZ + envelopeHeight - 1);
                        for (int z = minimumZ; z <= maximumZ; z++)
                        {
                            for (int x = minimumX; x <= maximumX; x++)
                            {
                                if (!occupiedCellsByLocalIndex.TryGetValue(
                                        z * width + x,
                                        out SpatialCellReport cell))
                                    continue;
                                int end = checked(
                                    cell.firstPlacementIndex +
                                    cell.placementIndexCount);
                                for (int membershipIndex =
                                         cell.firstPlacementIndex;
                                     membershipIndex < end;
                                     membershipIndex++)
                                {
                                    int placementIndex =
                                        spatialCells.cellPlacementIndices[
                                            membershipIndex];
                                    if (seenPlacementAtSample[placementIndex] ==
                                        sampleOrdinal + 1)
                                        continue;
                                    seenPlacementAtSample[placementIndex] =
                                        sampleOrdinal + 1;
                                    LogicalPlacementReport placement =
                                        logicalPlacements.placements[
                                            placementIndex];
                                    PrototypeRecipeReport prototype =
                                        prototypeRecipes.prototypes[
                                            placement.prototypeIndex];
                                    int partEnd = checked(
                                        prototype.firstPart +
                                        prototype.partCount);
                                    for (int partIndex = prototype.firstPart;
                                         partIndex < partEnd;
                                         partIndex++)
                                    {
                                        requiredRowsByPolicy[
                                            policyIndexByPart[partIndex]]++;
                                    }
                                }
                            }
                        }

                        for (int policyIndex = 0;
                             policyIndex < orderedPolicies.Length;
                             policyIndex++)
                        {
                            int requiredRows =
                                requiredRowsByPolicy[policyIndex];
                            inputs.Add(
                                new OperationMapRenderCapacitySweepInput(
                                    sampleIdentity,
                                    orderedPolicies[policyIndex],
                                    requiredRows));
                            if (requiredRows > peakRowsByPolicy[policyIndex])
                            {
                                peakRowsByPolicy[policyIndex] = requiredRows;
                                peakSampleByPolicy[policyIndex] =
                                    sampleIdentity;
                            }
                        }

                        sampleOrdinal++;
                    }
                }
            }

            if (!OperationMapRenderCapacitySweep.TryCalculate(inputs, out var capacities, out string error))
                throw new InvalidOperationException($"Capacity budget sweep failed: {error}");
            if (capacities.Length != orderedPolicies.Length ||
                sampleOrdinal != checked(width * height * sampleShapeCount))
                throw new InvalidOperationException("Capacity budget sweep returned incomplete coverage.");

            var policyBudgets = new List<CapacityPolicyBudgetReport>(capacities.Length);
            int totalSlots = 0;
            int totalPeakRows = 0;
            for (int policyIndex = 0; policyIndex < capacities.Length; policyIndex++)
            {
                OperationMapRenderCapacitySweepResult capacity = capacities[policyIndex];
                if (!capacity.Policy.Equals(orderedPolicies[policyIndex]) ||
                    capacity.SweepSampleCount != sampleOrdinal ||
                    capacity.PeakRequiredPartRows != peakRowsByPolicy[policyIndex])
                {
                    throw new InvalidOperationException("Capacity budget sweep result did not preserve canonical policy data.");
                }

                totalSlots = checked(totalSlots + capacity.Capacity);
                totalPeakRows = checked(totalPeakRows + capacity.PeakRequiredPartRows);
                policyBudgets.Add(new CapacityPolicyBudgetReport
                {
                    policyBucket = capacity.Policy.Bucket.ToString(),
                    layer = capacity.Policy.Layer,
                    renderingLayerMask = capacity.Policy.RenderingLayerMask,
                    motionVectorMode = capacity.Policy.MotionVectorMode.ToString(),
                    shadowFlags = (byte)capacity.Policy.ShadowFlags,
                    sweepSampleCount = capacity.SweepSampleCount,
                    peakRequiredPartRows = capacity.PeakRequiredPartRows,
                    peakSampleIdentity = peakSampleByPolicy[policyIndex],
                    capacity = capacity.Capacity,
                    headroomCount = capacity.HeadroomCount
                });
            }

            return new CapacityBudgetDocument
            {
                schema = "warline.operation-map.render-virtualization-capacity-budget",
                schemaVersion = 2,
                operationMapId = "opmap.skirmish.desert_base_01",
                result = "Passed",
                samplingPolicy =
                    "Device-informed deterministic all-grid-cell materialized-envelope sweep. The exact " +
                    "VRP-067 House interior envelope measured 12x9 inclusive 32 m cells; both 12x9 and " +
                    "9x12 orientations are swept at every accepted-origin-aligned grid cell. Full camera " +
                    "pose, projection, zoom, and tactical-follow sweeps remain required before final acceptance.",
                provisionalOnly = true,
                cameraProjectionSweepPending = true,
                observedMaterializedEnvelopeWidthCells =
                    ObservedMaterializedEnvelopeWidthCells,
                observedMaterializedEnvelopeHeightCells =
                    ObservedMaterializedEnvelopeHeightCells,
                observedMaterializedEnvelopeOrientationCount =
                    sampleShapeCount,
                canonicalSampleCount = sampleOrdinal,
                policyCount = policyBudgets.Count,
                placementCount = logicalPlacements.placementCount,
                provisionalMapMmiEntityBudget = logicalPlacements.placementCount,
                mapMmiEntityLimit = MapMmiEntityLimit,
                provisionalMapMmiEntityBudgetWithinLimit =
                    logicalPlacements.placementCount <= MapMmiEntityLimit,
                totalPeakRequiredPartRows = totalPeakRows,
                totalProvisionalActiveProxySlots = totalSlots,
                activeProxySlotLimit = ActiveProxySlotLimit,
                provisionalActiveProxySlotsWithinLimit = totalSlots <= ActiveProxySlotLimit,
                zeroOverflowProvenForProvisionalCapacity = true,
                sourceRowsSha256 = sourceRowsSha256,
                prototypeRecipesSha256 = prototypeRecipesSha256,
                logicalPlacementsSha256 = logicalPlacementsSha256,
                spatialCellsSha256 = spatialCellsSha256,
                capacityByPolicy = policyBudgets
            };
        }

        private static SpatialCellDocument BuildSpatialCells(
            IReadOnlyList<LogicalPlacementReport> placements,
            IReadOnlyList<PrototypeRecipeReport> prototypes,
            in RenderGridSpec acceptedGrid)
        {
            var worldBoundsByPlacement = new Bounds[placements.Count];
            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                LogicalPlacementReport placement = placements[placementIndex];
                PrototypeRecipeReport prototype = prototypes[placement.prototypeIndex];
                Bounds combinedLocalBounds = new(
                    ToVector3(prototype.combinedLocalBoundsCenter),
                    ToVector3(prototype.combinedLocalBoundsExtents) * 2f);
                worldBoundsByPlacement[placementIndex] =
                    TransformBounds(combinedLocalBounds, placement.worldMatrixValue);
            }
            RenderGridSpec renderGrid =
                BuildRenderGridEnvelope(acceptedGrid, worldBoundsByPlacement);

            var placementGridCells = new int[placements.Count][];
            var placementsByGridCell = new Dictionary<int, List<int>>();
            var verticalBoundsByGridCell = new Dictionary<int, Vector2>();
            int multiCellPlacementCount = 0;
            int maximumCellsPerPlacement = 0;
            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                LogicalPlacementReport placement = placements[placementIndex];
                Bounds worldBounds = worldBoundsByPlacement[placementIndex];
                var cellBounds = new OperationMapRenderBoundsBlob
                {
                    Center = worldBounds.center,
                    Extents = worldBounds.extents
                };
                if (!OperationMapRenderCellAssignment.TryAssign(
                        cellBounds,
                        renderGrid.CellSize,
                        renderGrid.Origin,
                        renderGrid.Dimensions,
                        out int[] gridCells,
                        out string error))
                {
                    throw new InvalidOperationException(
                        $"Placement {placementIndex} cell assignment failed: {error}");
                }

                placementGridCells[placementIndex] = gridCells;
                if (gridCells.Length > 1)
                    multiCellPlacementCount++;
                maximumCellsPerPlacement =
                    Math.Max(maximumCellsPerPlacement, gridCells.Length);
                foreach (int gridCell in gridCells)
                {
                    if (!placementsByGridCell.TryGetValue(
                            gridCell,
                            out List<int> cellPlacements))
                    {
                        cellPlacements = new List<int>();
                        placementsByGridCell.Add(gridCell, cellPlacements);
                        verticalBoundsByGridCell.Add(
                            gridCell,
                            new Vector2(worldBounds.min.y, worldBounds.max.y));
                    }
                    else
                    {
                        Vector2 vertical = verticalBoundsByGridCell[gridCell];
                        vertical.x = Math.Min(vertical.x, worldBounds.min.y);
                        vertical.y = Math.Max(vertical.y, worldBounds.max.y);
                        verticalBoundsByGridCell[gridCell] = vertical;
                    }
                    if (cellPlacements.Count > 0 &&
                        cellPlacements[cellPlacements.Count - 1] == placementIndex)
                    {
                        throw new InvalidOperationException(
                            $"Duplicate placement {placementIndex} in grid cell {gridCell}.");
                    }
                    cellPlacements.Add(placementIndex);
                }
            }

            int[] orderedGridCells = placementsByGridCell.Keys.OrderBy(value => value).ToArray();
            var compactIndexByGridCell = new Dictionary<int, int>();
            for (int compactIndex = 0;
                 compactIndex < orderedGridCells.Length;
                 compactIndex++)
            {
                compactIndexByGridCell.Add(orderedGridCells[compactIndex], compactIndex);
            }
            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                placements[placementIndex].cellIndex =
                    compactIndexByGridCell[placementGridCells[placementIndex][0]];
            }

            var cells = new List<SpatialCellReport>(orderedGridCells.Length);
            var cellPlacementIndices = new List<int>();
            int maximumPlacementsPerCell = 0;
            foreach (int gridCell in orderedGridCells)
            {
                int localCoordinateX = gridCell % renderGrid.Dimensions.x;
                int localCoordinateZ = gridCell / renderGrid.Dimensions.x;
                int coordinateX = localCoordinateX + renderGrid.CoordinateOffset.x;
                int coordinateZ = localCoordinateZ + renderGrid.CoordinateOffset.y;
                List<int> cellPlacements = placementsByGridCell[gridCell];
                maximumPlacementsPerCell =
                    Math.Max(maximumPlacementsPerCell, cellPlacements.Count);
                int firstPlacementIndex = cellPlacementIndices.Count;
                cellPlacementIndices.AddRange(cellPlacements);

                Vector2 vertical = verticalBoundsByGridCell[gridCell];
                Vector3 minimum = new(
                    renderGrid.Origin.x + localCoordinateX * renderGrid.CellSize,
                    vertical.x,
                    renderGrid.Origin.z + localCoordinateZ * renderGrid.CellSize);
                Vector3 maximum = new(
                    minimum.x + renderGrid.CellSize,
                    vertical.y,
                    minimum.z + renderGrid.CellSize);
                var worldBounds = new Bounds();
                worldBounds.SetMinMax(minimum, maximum);
                cells.Add(new SpatialCellReport
                {
                    cellIndex = cells.Count,
                    coordinateX = coordinateX,
                    coordinateZ = coordinateZ,
                    worldBoundsCenter = ToArray(worldBounds.center),
                    worldBoundsExtents = ToArray(worldBounds.extents),
                    firstPlacementIndex = firstPlacementIndex,
                    placementIndexCount = cellPlacements.Count
                });
            }

            string spatialHash = ComputeSpatialCellsSha256(
                renderGrid,
                cells,
                cellPlacementIndices);
            return new SpatialCellDocument
            {
                schema = "warline.operation-map.render-virtualization-spatial-cells",
                schemaVersion = 1,
                operationMapId = "opmap.skirmish.desert_base_01",
                result = "Passed",
                cellSize = renderGrid.CellSize,
                acceptedGridOrigin = ToArray(renderGrid.AcceptedOrigin),
                gridOrigin = ToArray(renderGrid.Origin),
                gridDimensions =
                    new[] { renderGrid.Dimensions.x, renderGrid.Dimensions.y },
                coordinateOffset =
                    new[] { renderGrid.CoordinateOffset.x, renderGrid.CoordinateOffset.y },
                gridCellCount =
                    checked(renderGrid.Dimensions.x * renderGrid.Dimensions.y),
                occupiedCellCount = cells.Count,
                cellPlacementIndexCount = cellPlacementIndices.Count,
                multiCellPlacementCount = multiCellPlacementCount,
                maximumCellsPerPlacement = maximumCellsPerPlacement,
                maximumPlacementsPerCell = maximumPlacementsPerCell,
                spatialCellsSha256 = spatialHash,
                cells = cells,
                cellPlacementIndices = cellPlacementIndices
            };
        }

        private static RenderGridSpec BuildRenderGridEnvelope(
            in RenderGridSpec acceptedGrid,
            IReadOnlyList<Bounds> worldBounds)
        {
            if (worldBounds == null || worldBounds.Count == 0)
                throw new InvalidOperationException("Render grid requires placement bounds.");

            Vector3 minimum =
                new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum =
                new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < worldBounds.Count; index++)
            {
                Bounds bounds = worldBounds[index];
                if (!IsFinite(bounds.center) ||
                    !IsFinite(bounds.extents) ||
                    bounds.extents.x < 0f ||
                    bounds.extents.y < 0f ||
                    bounds.extents.z < 0f)
                {
                    throw new InvalidOperationException(
                        $"Placement {index} has invalid transformed bounds.");
                }
                minimum = Vector3.Min(minimum, bounds.min);
                maximum = Vector3.Max(maximum, bounds.max);
            }

            int minimumX = Mathf.FloorToInt(
                (minimum.x - acceptedGrid.AcceptedOrigin.x) / acceptedGrid.CellSize);
            int minimumZ = Mathf.FloorToInt(
                (minimum.z - acceptedGrid.AcceptedOrigin.z) / acceptedGrid.CellSize);
            Vector3 alignedOrigin = new(
                acceptedGrid.AcceptedOrigin.x + minimumX * acceptedGrid.CellSize,
                acceptedGrid.AcceptedOrigin.y,
                acceptedGrid.AcceptedOrigin.z + minimumZ * acceptedGrid.CellSize);
            int width = Mathf.CeilToInt(
                (maximum.x - alignedOrigin.x) / acceptedGrid.CellSize);
            int height = Mathf.CeilToInt(
                (maximum.z - alignedOrigin.z) / acceptedGrid.CellSize);
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Render grid envelope is empty.");
            return new RenderGridSpec(
                acceptedGrid.CellSize,
                acceptedGrid.AcceptedOrigin,
                alignedOrigin,
                new int2(width, height),
                new int2(minimumX, minimumZ));
        }

        private static RenderGridSpec ResolveRenderGrid()
        {
            GridAuthoringConfig grid =
                AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(
                    OperationMapBuildingCandidateMigrationEditor.GridConfigPath);
            if (grid == null ||
                grid.Width <= 0 ||
                grid.Height <= 0 ||
                !float.IsFinite(grid.CellSize) ||
                grid.CellSize <= 0f ||
                !IsFinite(grid.Origin))
            {
                throw new InvalidOperationException(
                    "Accepted operation-map render grid metadata is invalid.");
            }

            float widthInMeters = grid.Width * grid.CellSize;
            float heightInMeters = grid.Height * grid.CellSize;
            int width = Mathf.RoundToInt(widthInMeters / RenderCellSize);
            int height = Mathf.RoundToInt(heightInMeters / RenderCellSize);
            if (width <= 0 ||
                height <= 0 ||
                !Mathf.Approximately(width * RenderCellSize, widthInMeters) ||
                !Mathf.Approximately(height * RenderCellSize, heightInMeters))
            {
                throw new InvalidOperationException(
                    "Accepted operation-map bounds must tile exactly into 32 m render cells.");
            }

            return new RenderGridSpec(
                RenderCellSize,
                grid.Origin,
                grid.Origin,
                new int2(width, height),
                int2.zero);
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

        private static string Classify(
            Row row,
            bool repeated,
            bool policySupported,
            IReadOnlyDictionary<BuildingRole, BuildingFamilyDecision> buildingFamilyDecisions,
            IReadOnlyDictionary<DenseCityPresentationIdentityAuthoring,
                OperationMapRenderInfrastructureEligibilityDecision>
                infrastructureOwnerDecisions)
        {
            if (row.DenseOwner == null)
                return row.AcceptedOwner != null
                    ? "accepted-map-resident-pending-vrp021"
                    : "unresolved-owner";
            if (row.DenseOwner.Category ==
                DenseCityPresentationSemanticCategory.GameplayBuildingIntact)
            {
                if (!row.IsGeneratedBuildingFamily ||
                    !buildingFamilyDecisions.TryGetValue(
                        row.BuildingFamilyRole,
                        out BuildingFamilyDecision decision))
                {
                    return "gameplay-building-family-unresolved";
                }
                return decision.Selected ? "eligible" : decision.ReasonCode;
            }
            if (row.Renderer is not MeshRenderer)
                return "unsupported-renderer";
            if (!policySupported)
                return "unsupported-render-policy";
            if (!repeated)
                return "unique-presentation-signature";

            if (row.DenseOwner.Category ==
                DenseCityPresentationSemanticCategory.Infrastructure)
            {
                if (!infrastructureOwnerDecisions.TryGetValue(
                        row.DenseOwner,
                        out OperationMapRenderInfrastructureEligibilityDecision decision))
                {
                    return "infrastructure-owner-decision-missing";
                }
                return decision.Selected ? "eligible" : decision.ReasonCode;
            }

            return row.DenseOwner.Category switch
            {
                DenseCityPresentationSemanticCategory.Vegetation => "eligible",
                DenseCityPresentationSemanticCategory.Prop => "eligible",
                DenseCityPresentationSemanticCategory.Horizon =>
                    "unique-environment-content-resident",
                _ => "unsupported-semantic-category"
            };
        }

        private static Dictionary<DenseCityPresentationIdentityAuthoring,
                OperationMapRenderInfrastructureEligibilityDecision>
            BuildInfrastructureOwnerDecisions(IReadOnlyList<Row> rows)
        {
            return rows
                .Where(row =>
                    row.DenseOwner != null &&
                    row.DenseOwner.Category ==
                        DenseCityPresentationSemanticCategory.Infrastructure)
                .Select(row => row.DenseOwner)
                .Distinct()
                .OrderBy(owner => owner.StableId, StringComparer.Ordinal)
                .ToDictionary(
                    owner => owner,
                    OperationMapRenderInfrastructureEligibilityPolicy.Evaluate);
        }

        private static bool TryClassifyPolicy(
            Renderer renderer,
            Material material,
            out string materialSurface,
            out string policy,
            out OperationMapRenderPolicyKey policyKey)
        {
            policyKey = default;
            if (material == null)
            {
                materialSurface = "MissingMaterial";
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
            materialSurface = surface.ToString();

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

        private static string PolicyDisposition(
            bool supported,
            string materialSurface,
            OperationMapRenderPolicyKey policyKey)
        {
            if (supported)
                return "SupportedPolicy:" + policyKey.Bucket;
            return string.Equals(
                materialSurface,
                OperationMapRenderMaterialSurface.Transparent.ToString(),
                StringComparison.Ordinal)
                ? "Resident:TransparentShadowPolicyUnsupported"
                : "Resident:UnsupportedRenderPolicy";
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
                AppendCanonical(canonical, row.buildingFamily);
                AppendCanonical(canonical, row.buildingVisualState);
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
                AppendCanonical(canonical, placement.sourceOwnerIdentityLow);
                AppendCanonical(canonical, placement.sourceOwnerIdentityHigh);
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

        private static string ComputeSpatialCellsSha256(
            in RenderGridSpec renderGrid,
            IReadOnlyList<SpatialCellReport> cells,
            IReadOnlyList<int> cellPlacementIndices)
        {
            var canonical =
                new StringBuilder((cells.Count + cellPlacementIndices.Count) * 64);
            AppendCanonical(canonical, renderGrid.CellSize);
            AppendCanonical(canonical, renderGrid.AcceptedOrigin.x);
            AppendCanonical(canonical, renderGrid.AcceptedOrigin.y);
            AppendCanonical(canonical, renderGrid.AcceptedOrigin.z);
            AppendCanonical(canonical, renderGrid.Origin.x);
            AppendCanonical(canonical, renderGrid.Origin.y);
            AppendCanonical(canonical, renderGrid.Origin.z);
            AppendCanonical(canonical, renderGrid.Dimensions.x);
            AppendCanonical(canonical, renderGrid.Dimensions.y);
            AppendCanonical(canonical, renderGrid.CoordinateOffset.x);
            AppendCanonical(canonical, renderGrid.CoordinateOffset.y);
            AppendCanonical(canonical, cells.Count);
            foreach (SpatialCellReport cell in cells)
            {
                AppendCanonical(canonical, cell.cellIndex);
                AppendCanonical(canonical, cell.coordinateX);
                AppendCanonical(canonical, cell.coordinateZ);
                AppendCanonical(canonical, cell.worldBoundsCenter);
                AppendCanonical(canonical, cell.worldBoundsExtents);
                AppendCanonical(canonical, cell.firstPlacementIndex);
                AppendCanonical(canonical, cell.placementIndexCount);
            }
            AppendCanonical(canonical, cellPlacementIndices.Count);
            for (int index = 0; index < cellPlacementIndices.Count; index++)
                AppendCanonical(canonical, cellPlacementIndices[index]);
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

        private static Vector3 ToVector3(IReadOnlyList<float> value)
        {
            if (value == null || value.Count != 3)
                throw new InvalidOperationException("Expected an exact three-value vector.");
            return new Vector3(value[0], value[1], value[2]);
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

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
                OperationMapEntityPresentationIdentityAuthoring acceptedOwner,
                OperationMapBuildingAuthoring buildingOwner)
            {
                Renderer = renderer;
                Material = material;
                SubMesh = subMesh;
                Signature = signature;
                DenseOwner = denseOwner;
                AcceptedOwner = acceptedOwner;
                BuildingOwner = buildingOwner;
                BuildingVisualState = ResolveBuildingVisualState(renderer, buildingOwner);
            }

            internal Renderer Renderer { get; }
            internal Material Material { get; }
            internal int SubMesh { get; }
            internal string Signature { get; }
            internal DenseCityPresentationIdentityAuthoring DenseOwner { get; }
            internal OperationMapEntityPresentationIdentityAuthoring AcceptedOwner { get; }
            internal OperationMapBuildingAuthoring BuildingOwner { get; }
            internal OperationMapRenderVisualState BuildingVisualState { get; }
            internal bool IsGeneratedBuildingFamily =>
                DenseOwner != null &&
                BuildingOwner != null &&
                DenseOwner.transform == BuildingOwner.transform &&
                DenseOwner.Role == OperationMapEntityPresentationRole.GameplayBuildings &&
                DenseOwner.Category ==
                    DenseCityPresentationSemanticCategory.GameplayBuildingIntact &&
                BuildingOwner.Definition != null &&
                BuildingOwner.Definition.ConfiguredRole != BuildingRole.None;
            internal BuildingRole BuildingFamilyRole =>
                IsGeneratedBuildingFamily
                    ? BuildingOwner.Definition.ConfiguredRole
                    : BuildingRole.None;
            internal string BuildingFamilyName => BuildingFamilyRole.ToString();

            private static OperationMapRenderVisualState ResolveBuildingVisualState(
                Renderer renderer,
                OperationMapBuildingAuthoring building)
            {
                if (building == null)
                    return OperationMapRenderVisualState.Any;
                Transform target = renderer.transform;
                Transform intact = building.IntactVisualRoot != null
                    ? building.IntactVisualRoot.transform
                    : null;
                Transform destroyed = building.DestroyedVisualRoot != null
                    ? building.DestroyedVisualRoot.transform
                    : null;
                bool inIntact = intact != null &&
                                (target == intact || target.IsChildOf(intact));
                bool inDestroyed = destroyed != null &&
                                   (target == destroyed || target.IsChildOf(destroyed));
                if (inIntact == inDestroyed)
                {
                    throw new InvalidOperationException(
                        $"Building renderer {renderer.name} must resolve to exactly one visual state.");
                }
                return inIntact
                    ? OperationMapRenderVisualState.Intact
                    : OperationMapRenderVisualState.Destroyed;
            }
        }

        private struct CountPair
        {
            internal int Eligible;
            internal int Excluded;
        }

        private sealed class BuildingFamilyDecision
        {
            internal BuildingFamilyDecision(
                BuildingRole role,
                bool selected,
                string reasonCode,
                int ownerCount,
                int completeOwnerCount,
                int totalRowCount,
                int intactRowCount,
                int destroyedRowCount,
                int repeatedSignatureRowCount,
                int unsupportedRendererRowCount,
                int unsupportedPolicyRowCount)
            {
                Role = role;
                Selected = selected;
                ReasonCode = reasonCode;
                OwnerCount = ownerCount;
                CompleteOwnerCount = completeOwnerCount;
                TotalRowCount = totalRowCount;
                IntactRowCount = intactRowCount;
                DestroyedRowCount = destroyedRowCount;
                RepeatedSignatureRowCount = repeatedSignatureRowCount;
                UnsupportedRendererRowCount = unsupportedRendererRowCount;
                UnsupportedPolicyRowCount = unsupportedPolicyRowCount;
            }

            internal BuildingRole Role { get; }
            internal bool Selected { get; }
            internal string ReasonCode { get; }
            internal int OwnerCount { get; }
            internal int CompleteOwnerCount { get; }
            internal int TotalRowCount { get; }
            internal int IntactRowCount { get; }
            internal int DestroyedRowCount { get; }
            internal int RepeatedSignatureRowCount { get; }
            internal int UnsupportedRendererRowCount { get; }
            internal int UnsupportedPolicyRowCount { get; }

            internal BuildingFamilyReport ToReport() => new()
            {
                family = Role.ToString(),
                selected = Selected,
                reasonCode = ReasonCode,
                ownerCount = OwnerCount,
                completeOwnerCount = CompleteOwnerCount,
                excludedOwnerCount = OwnerCount - CompleteOwnerCount,
                totalRowCount = TotalRowCount,
                eligibleRowCount = Selected ? TotalRowCount : 0,
                excludedRowCount = Selected ? 0 : TotalRowCount,
                intactRowCount = IntactRowCount,
                destroyedRowCount = DestroyedRowCount,
                repeatedSignatureRowCount = RepeatedSignatureRowCount,
                unsupportedRendererRowCount = UnsupportedRendererRowCount,
                unsupportedPolicyRowCount = UnsupportedPolicyRowCount
            };
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
            public int normalizedEmbeddedDestroyedDuplicateRowsRemoved;
            public string intactVisualNormalizationResult;
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
            public float renderCellSize;
            public float[] acceptedRenderGridOrigin;
            public float[] renderGridOrigin;
            public int[] renderGridDimensions;
            public int[] renderGridCoordinateOffset;
            public int occupiedCellCount;
            public int cellPlacementIndexCount;
            public int multiCellPlacementCount;
            public int maximumCellsPerPlacement;
            public int maximumPlacementsPerCell;
            public string spatialCellsSha256;
            public string spatialCellsPath;
            public string spatialCellsJsonSha256;
            public bool mutationAuthorized;
            public string mutationBlocker;
            public string buildingFamilySelectionPolicy;
            public string infrastructureSelectionPolicy;
            public string materialSurfaceSelectionPolicy;
            public List<BuildingFamilyReport> buildingFamilies;
            public List<InfrastructureOwnerReport> infrastructureOwners;
            public List<Breakdown> byBuildingFamily;
            public List<Breakdown> bySemanticCategory;
            public List<Breakdown> byPrototypeSignature;
            public List<Breakdown> byRendererType;
            public List<Breakdown> byPolicyBucket;
            public List<Breakdown> byMaterialSurface;
            public List<Breakdown> byPolicyDisposition;
            public List<Breakdown> byGameplayOwnership;
            public List<Breakdown> byReasonCode;
            [NonSerialized] public List<SourceRowReport> sourceRows;
            [NonSerialized] public PrototypeRecipeDocument prototypeRecipes;
            [NonSerialized] public LogicalPlacementDocument logicalPlacements;
            [NonSerialized] public SpatialCellDocument spatialCells;
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
            [NonSerialized] public SpatialCellDocument spatialCells;
        }

        [Serializable]
        private sealed class LogicalPlacementReport
        {
            public int placementIndex;
            public string stableOwnerId;
            public ulong stableIdentityLow;
            public ulong stableIdentityHigh;
            public ulong sourceOwnerIdentityLow;
            public ulong sourceOwnerIdentityHigh;
            public int prototypeIndex;
            public float[] worldMatrix;
            public int cellIndex;
            public int stateOwnerIndex;
            public string requiredVisualState;
            public int priority;
            public string semanticCategory;
            [NonSerialized] public Matrix4x4 worldMatrixValue;
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
        private sealed class SpatialCellDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public float cellSize;
            public float[] acceptedGridOrigin;
            public float[] gridOrigin;
            public int[] gridDimensions;
            public int[] coordinateOffset;
            public int gridCellCount;
            public int occupiedCellCount;
            public int cellPlacementIndexCount;
            public int multiCellPlacementCount;
            public int maximumCellsPerPlacement;
            public int maximumPlacementsPerCell;
            public string spatialCellsSha256;
            public List<SpatialCellReport> cells;
            public List<int> cellPlacementIndices;
        }

        [Serializable]
        private sealed class SpatialCellReport
        {
            public int cellIndex;
            public int coordinateX;
            public int coordinateZ;
            public float[] worldBoundsCenter;
            public float[] worldBoundsExtents;
            public int firstPlacementIndex;
            public int placementIndexCount;
        }

        [Serializable]
        private sealed class CapacityBudgetDocument
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public string samplingPolicy;
            public bool provisionalOnly;
            public bool cameraProjectionSweepPending;
            public int observedMaterializedEnvelopeWidthCells;
            public int observedMaterializedEnvelopeHeightCells;
            public int observedMaterializedEnvelopeOrientationCount;
            public int canonicalSampleCount;
            public int policyCount;
            public int placementCount;
            public int provisionalMapMmiEntityBudget;
            public int mapMmiEntityLimit;
            public bool provisionalMapMmiEntityBudgetWithinLimit;
            public int totalPeakRequiredPartRows;
            public int totalProvisionalActiveProxySlots;
            public int activeProxySlotLimit;
            public bool provisionalActiveProxySlotsWithinLimit;
            public bool zeroOverflowProvenForProvisionalCapacity;
            public string sourceRowsSha256;
            public string prototypeRecipesSha256;
            public string logicalPlacementsSha256;
            public string spatialCellsSha256;
            public List<CapacityPolicyBudgetReport> capacityByPolicy;
        }

        [Serializable]
        private sealed class CapacityPolicyBudgetReport
        {
            public string policyBucket;
            public int layer;
            public uint renderingLayerMask;
            public string motionVectorMode;
            public byte shadowFlags;
            public int sweepSampleCount;
            public int peakRequiredPartRows;
            public string peakSampleIdentity;
            public int capacity;
            public int headroomCount;
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
        private sealed class BuildingFamilyReport
        {
            public string family;
            public bool selected;
            public string reasonCode;
            public int ownerCount;
            public int completeOwnerCount;
            public int excludedOwnerCount;
            public int totalRowCount;
            public int eligibleRowCount;
            public int excludedRowCount;
            public int intactRowCount;
            public int destroyedRowCount;
            public int repeatedSignatureRowCount;
            public int unsupportedRendererRowCount;
            public int unsupportedPolicyRowCount;
        }

        [Serializable]
        private sealed class InfrastructureOwnerReport
        {
            public string stableId;
            public bool selected;
            public string reasonCode;
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
            public string buildingFamily;
            public string buildingVisualState;
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
            internal string RecipeKey;
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
            internal OperationMapRenderVisualState VisualState;
        }

        private sealed class OwnerRecipe
        {
            internal string RecipeKey;
            internal string OwnerIdentitySource;
            internal string OwnerStableId;
            internal OperationMapRenderIdentity128 OwnerIdentity;
            internal OperationMapRenderIdentity128 PlacementIdentity;
            internal OperationMapEntityPresentationRole OwnerRole;
            internal Matrix4x4 WorldMatrix;
            internal DenseCityPresentationSemanticCategory SemanticCategory;
            internal OperationMapRenderVisualState VisualState;
            internal List<EligiblePartCandidate> Parts;
        }

        private readonly struct RenderGridSpec
        {
            internal RenderGridSpec(
                float cellSize,
                Vector3 acceptedOrigin,
                Vector3 origin,
                int2 dimensions,
                int2 coordinateOffset)
            {
                CellSize = cellSize;
                AcceptedOrigin = acceptedOrigin;
                Origin = origin;
                Dimensions = dimensions;
                CoordinateOffset = coordinateOffset;
            }

            internal float CellSize { get; }
            internal Vector3 AcceptedOrigin { get; }
            internal Vector3 Origin { get; }
            internal int2 Dimensions { get; }
            internal int2 CoordinateOffset { get; }
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
