using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class DenseCityCandidatePresentationIdentityBackfill
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-24_dense_city_generated_identity_backfill.json";
        private const string ConfigPath =
            "Assets/Game/Configs/OperationMaps/Skirmish/" +
            "SkirmishDesertBase_MapWideCity_Config.asset";

        private static readonly DenseCityPresentationCategory[] RenderOnlyCategories =
        {
            DenseCityPresentationCategory.Infrastructure,
            DenseCityPresentationCategory.Vegetation,
            DenseCityPresentationCategory.Prop,
            DenseCityPresentationCategory.Horizon
        };

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Maps/Skirmish Desert Base/Backfill Dense City Candidate Presentation Identities")]
        public static void BackfillCandidate()
        {
            if (!TryBackfillCandidate(out string summary, out string error))
            {
                string message = $"Dense-city presentation identity backfill rejected: {error}";
                if (Application.isBatchMode)
                {
                    Debug.LogError(message);
                    EditorApplication.Exit(1);
                    return;
                }

                throw new InvalidOperationException(message);
            }

            Debug.Log($"[DenseCityCandidatePresentationIdentityBackfill] result=Passed {summary}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        internal static BackfillResult Apply(
            IDenseCityGenerationRecordSource records,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityGeneratedRootAuthoring entityRoot)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (entityRoot == null ||
                entityRoot.Role != DenseCityGeneratedRootRole.EntityPresentationSource)
            {
                throw new ArgumentException(
                    "The dense-city entity-presentation root is required.",
                    nameof(entityRoot));
            }

            DenseCityPresentationIdentityAuthoring[] existingIdentities =
                entityRoot.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true);
            if (existingIdentities.Length > 0)
            {
                return UpgradeExistingIdentitySemantics(
                    records,
                    hierarchy,
                    entityRoot,
                    existingIdentities);
            }

            var expectedIds = new HashSet<string>(StringComparer.Ordinal);
            var plan = new List<PlannedIdentity>();
            DenseCityProtectedAutobahnReplacementManifestAuthoring manifest =
                entityRoot.GetComponentInChildren<
                    DenseCityProtectedAutobahnReplacementManifestAuthoring>(true);
            var manifestIds = manifest == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : manifest.Entries
                    .Select(entry => entry.StableId)
                    .ToHashSet(StringComparer.Ordinal);
            if (manifest != null &&
                (manifestIds.Count != manifest.Entries.Count ||
                 manifestIds.Any(string.IsNullOrWhiteSpace)))
            {
                throw new InvalidOperationException(
                    "Protected Autobahn manifest contains empty or duplicate stable identities.");
            }
            IReadOnlyList<DenseCityBuildingBakeRecord> buildings = records.Buildings;
            Dictionary<string, OperationMapBuildingAuthoring> buildingByStableId =
                IndexGeneratedBuildings(entityRoot, buildings.Count);
            for (int index = 0; index < buildings.Count; index++)
            {
                string stableId = buildings[index].Identity.CreateBakedStableId();
                if (!expectedIds.Add(stableId) ||
                    !buildingByStableId.TryGetValue(stableId, out OperationMapBuildingAuthoring owner))
                {
                    throw new InvalidOperationException(
                        $"Generated building identity does not resolve exactly once: '{stableId}'.");
                }

                PlanIdentity(
                    plan,
                    owner.gameObject,
                    stableId,
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    DenseCityPresentationSemanticCategory.GameplayBuildingIntact,
                    false);
            }

            int renderOnlyCount = 0;
            for (int categoryIndex = 0; categoryIndex < RenderOnlyCategories.Length; categoryIndex++)
            {
                DenseCityPresentationCategory category = RenderOnlyCategories[categoryIndex];
                DenseCityPresentationBakeRecord[] expected = records.Presentations
                    .Where(record =>
                        record.Category == category &&
                        !manifestIds.Contains(record.Identity.CreateBakedStableId()))
                    .ToArray();
                Transform parent = hierarchy.ResolveIndependentParent(category);
                Transform[] owners = Enumerable.Range(0, parent.childCount)
                    .Select(parent.GetChild)
                    .Where(owner =>
                    {
                        DenseCityPresentationIdentityAuthoring identity =
                            owner.GetComponent<DenseCityPresentationIdentityAuthoring>();
                        return identity == null || !manifestIds.Contains(identity.StableId);
                    })
                    .ToArray();
                if (owners.Length != expected.Length)
                {
                    throw new InvalidOperationException(
                        $"Dense-city {category} owner count is {owners.Length}; expected {expected.Length}.");
                }

                for (int index = 0; index < expected.Length; index++)
                {
                    DenseCityPresentationBakeRecord record = expected[index];
                    Transform owner = owners[index];
                    hierarchy.RequireIndependentRoot(category, owner);
                    RequirePresentationMatch(owner, record);
                    string stableId = record.Identity.CreateBakedStableId();
                    if (!expectedIds.Add(stableId))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate generated presentation identity: '{stableId}'.");
                    }

                    PlanIdentity(
                        plan,
                        owner.gameObject,
                        stableId,
                        OperationMapEntityPresentationRole.RenderOnly,
                        (DenseCityPresentationSemanticCategory)record.Category,
                        record.AllowsProtectedOverlap);
                    renderOnlyCount++;
                }
            }
            if (manifest != null)
            {
                Transform infrastructure =
                    hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Infrastructure);
                Dictionary<string, DenseCityPresentationIdentityAuthoring[]> identitiesByStableId =
                    entityRoot.GetComponentsInChildren<
                            DenseCityPresentationIdentityAuthoring>(true)
                        .GroupBy(identity => identity.StableId, StringComparer.Ordinal)
                        .ToDictionary(
                            group => group.Key,
                            group => group.ToArray(),
                            StringComparer.Ordinal);
                foreach (DenseCityProtectedAutobahnReplacementManifestEntry entry in manifest.Entries)
                {
                    if (!identitiesByStableId.TryGetValue(
                            entry.StableId,
                            out DenseCityPresentationIdentityAuthoring[] matches) ||
                        matches.Length != 1 ||
                        !matches[0].transform.IsChildOf(infrastructure) ||
                        !expectedIds.Add(entry.StableId))
                    {
                        throw new InvalidOperationException(
                            $"Protected Autobahn manifest identity does not resolve once: " +
                            $"'{entry.StableId}'.");
                    }
                    PlanIdentity(
                        plan,
                        matches[0].gameObject,
                        entry.StableId,
                        OperationMapEntityPresentationRole.RenderOnly,
                        DenseCityPresentationSemanticCategory.Infrastructure,
                        false);
                    renderOnlyCount++;
                }
            }

            Dictionary<string, PlannedIdentity> plannedById = plan.ToDictionary(
                item => item.StableId,
                item => item,
                StringComparer.Ordinal);
            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DenseCityPresentationIdentityAuthoring identity in existingIdentities)
            {
                if (!identity.TryValidate(out string error) ||
                    !plannedById.TryGetValue(identity.StableId, out PlannedIdentity planned) ||
                    planned.Owner != identity.gameObject ||
                    planned.Role != identity.Role ||
                    planned.Category != identity.Category ||
                    planned.AllowsProtectedOverlap != identity.AllowsProtectedOverlap ||
                    !existingIds.Add(identity.StableId))
                {
                    throw new InvalidOperationException(
                        $"Dense-city generated identity is invalid, unexpected, or duplicated: " +
                        $"'{identity.StableId}' ({error ?? "ownership mismatch"}).");
                }
            }

            int added = 0;
            int existing = 0;
            for (int index = 0; index < plan.Count; index++)
            {
                PlannedIdentity item = plan[index];
                if (item.Existing)
                {
                    existing++;
                    continue;
                }

                var identity = item.Owner.AddComponent<DenseCityPresentationIdentityAuthoring>();
                identity.ConfigureForEditor(
                    item.StableId,
                    item.Role,
                    item.Category,
                    item.AllowsProtectedOverlap);
                if (!identity.TryValidate(out string error))
                    throw new InvalidOperationException(error);
                added++;
            }

            DenseCityPresentationIdentityAuthoring[] finalIdentities =
                entityRoot.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true);
            if (finalIdentities.Length != expectedIds.Count)
            {
                throw new InvalidOperationException(
                    $"Dense-city generated identity count is {finalIdentities.Length}; " +
                    $"expected {expectedIds.Count}.");
            }

            return new BackfillResult(buildings.Count, renderOnlyCount, added, existing);
        }

        private static BackfillResult UpgradeExistingIdentitySemantics(
            IDenseCityGenerationRecordSource records,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityGeneratedRootAuthoring entityRoot,
            DenseCityPresentationIdentityAuthoring[] existingIdentities)
        {
            if (existingIdentities.Length !=
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedIdentities)
            {
                throw new InvalidOperationException(
                    $"Dense-city existing identity count is {existingIdentities.Length}; expected " +
                    $"{OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedIdentities}.");
            }

            var overlapIds = records.Presentations
                .Where(record => record.AllowsProtectedOverlap)
                .Select(record => record.Identity.CreateBakedStableId())
                .ToHashSet(StringComparer.Ordinal);
            var existingIdSet = existingIdentities
                .Select(identity => identity.StableId)
                .ToHashSet(StringComparer.Ordinal);
            int resolvedRecordOverlapCount = overlapIds.Count(existingIdSet.Contains);
            int expectedProtectedOverlapOwners = resolvedRecordOverlapCount > 0
                ? overlapIds.Count
                : records.Presentations
                    .Where(record => record.AllowsProtectedOverlap)
                    .Select(record => new Vector2Int(
                        Mathf.RoundToInt(record.WorldMatrix.m03 * 1000f),
                        Mathf.RoundToInt(record.WorldMatrix.m23 * 1000f)))
                    .Distinct()
                    .Count();
            var plan = new List<PlannedIdentity>(existingIdentities.Length);
            var expectedIds = new HashSet<string>(StringComparer.Ordinal);

            OperationMapBuildingAuthoring[] buildings =
                entityRoot.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
            if (buildings.Length !=
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedIdentities -
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedRenderOnlyOwners)
            {
                throw new InvalidOperationException(
                    $"Dense-city generated building owner count is {buildings.Length}; expected " +
                    $"{OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedIdentities - OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedRenderOnlyOwners}.");
            }
            foreach (OperationMapBuildingAuthoring building in buildings)
            {
                DenseCityPresentationIdentityAuthoring identity =
                    building.GetComponent<DenseCityPresentationIdentityAuthoring>();
                if (identity == null ||
                    !string.Equals(identity.StableId, building.StableId, StringComparison.Ordinal) ||
                    !expectedIds.Add(identity.StableId))
                {
                    throw new InvalidOperationException(
                        $"Dense-city building owner does not have one matching stable identity: " +
                        $"'{building.StableId}'.");
                }
                PlanIdentity(
                    plan,
                    building.gameObject,
                    identity.StableId,
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    DenseCityPresentationSemanticCategory.GameplayBuildingIntact,
                    false);
            }

            int renderOnlyCount = 0;
            int protectedOverlapCount = 0;
            foreach (DenseCityPresentationCategory category in RenderOnlyCategories)
            {
                Transform parent = hierarchy.ResolveIndependentParent(category);
                for (int index = 0; index < parent.childCount; index++)
                {
                    Transform owner = parent.GetChild(index);
                    hierarchy.RequireIndependentRoot(category, owner);
                    DenseCityPresentationIdentityAuthoring identity =
                        owner.GetComponent<DenseCityPresentationIdentityAuthoring>();
                    if (identity == null || !expectedIds.Add(identity.StableId))
                    {
                        throw new InvalidOperationException(
                            $"Dense-city {category} owner does not have one unique stable identity: " +
                            $"'{owner.name}'.");
                    }

                    bool allowsProtectedOverlap =
                        overlapIds.Contains(identity.StableId) ||
                        IsLegacySubgradeProtectedRouteOwner(owner);
                    if (allowsProtectedOverlap)
                    {
                        if (category != DenseCityPresentationCategory.Infrastructure)
                        {
                            throw new InvalidOperationException(
                                $"Dense-city protected-overlap owner is outside Infrastructure: " +
                                $"'{identity.StableId}'.");
                        }
                        protectedOverlapCount++;
                    }
                    PlanIdentity(
                        plan,
                        owner.gameObject,
                        identity.StableId,
                        OperationMapEntityPresentationRole.RenderOnly,
                        (DenseCityPresentationSemanticCategory)category,
                        allowsProtectedOverlap);
                    renderOnlyCount++;
                }
            }

            if (renderOnlyCount !=
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedRenderOnlyOwners)
            {
                throw new InvalidOperationException(
                    $"Dense-city render-only owner count is {renderOnlyCount}; expected " +
                    $"{OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGeneratedRenderOnlyOwners}.");
            }
            if (protectedOverlapCount != expectedProtectedOverlapOwners)
            {
                throw new InvalidOperationException(
                    $"Dense-city protected-overlap owner count is {protectedOverlapCount}; " +
                    $"the deterministic migration contract requires " +
                    $"{expectedProtectedOverlapOwners}.");
            }
            if (expectedIds.Count != existingIdentities.Length)
            {
                throw new InvalidOperationException(
                    $"Dense-city semantic hierarchy resolves {expectedIds.Count} identities; " +
                    $"expected {existingIdentities.Length}.");
            }

            Dictionary<string, PlannedIdentity> plannedById = plan.ToDictionary(
                item => item.StableId,
                item => item,
                StringComparer.Ordinal);
            foreach (DenseCityPresentationIdentityAuthoring identity in existingIdentities)
            {
                if (!identity.TryValidate(out string error) ||
                    !plannedById.TryGetValue(identity.StableId, out PlannedIdentity planned) ||
                    planned.Owner != identity.gameObject ||
                    planned.Role != identity.Role ||
                    planned.Category != identity.Category ||
                    planned.AllowsProtectedOverlap != identity.AllowsProtectedOverlap)
                {
                    throw new InvalidOperationException(
                        $"Dense-city generated identity is invalid or outside its semantic owner: " +
                        $"'{identity.StableId}' ({error ?? "ownership mismatch"}).");
                }
            }

            return new BackfillResult(buildings.Length, renderOnlyCount, 0, plan.Count);
        }

        // Accepted candidates predating semantic flags are upgraded once here. Production
        // generation and readiness consume only the explicit AllowsProtectedOverlap flag.
        private static bool IsLegacySubgradeProtectedRouteOwner(Transform owner)
        {
            var protectedFootprint = Rect.MinMaxRect(
                DenseCityProtectedAutobahnReplacementPlanner.LegacyMinimumWorldX,
                DenseCityProtectedAutobahnReplacementPlanner.LegacyMinimumWorldZ,
                DenseCityProtectedAutobahnReplacementPlanner.LegacyMaximumWorldX,
                DenseCityProtectedAutobahnReplacementPlanner.LegacyMaximumWorldZ);
            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                Bounds bounds = renderer.bounds;
                var footprint = Rect.MinMaxRect(
                    bounds.min.x,
                    bounds.min.z,
                    bounds.max.x,
                    bounds.max.z);
                if (bounds.max.y < -0.5f && protectedFootprint.Overlaps(footprint))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryBackfillCandidate(out string summary, out string error)
        {
            summary = null;
            error = null;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string entityPath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string entityBackup = Path.GetTempFileName();
            string entityMetaPath = entityPath + ".meta";
            string entityMetaBackup = Path.GetTempFileName();
            string[] protectedPaths =
            {
                OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                ConfigPath
            };
            string[] protectedHashes = protectedPaths.Select(ComputeHash).ToArray();
            File.Copy(ToPhysicalPath(entityPath), entityBackup, true);
            File.Copy(ToPhysicalPath(entityMetaPath), entityMetaBackup, true);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene mapScene = default;
            Scene entityScene = default;
            try
            {
                mapScene = EditorSceneManager.OpenScene(
                    DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                    OpenSceneMode.Additive);
                entityScene = EditorSceneManager.OpenScene(entityPath, OpenSceneMode.Additive);
                if (!SceneManager.SetActiveScene(mapScene))
                {
                    throw new InvalidOperationException(
                        "Dense-city map candidate could not become the active generation scene.");
                }
                DenseCityGeneratedRootAuthoring mapRoot =
                    RequireGeneratedRoot(mapScene, DenseCityGeneratedRootRole.MapBakeSource);
                DenseCityGeneratedRootAuthoring entityRoot =
                    RequireGeneratedRoot(
                        entityScene,
                        DenseCityGeneratedRootRole.EntityPresentationSource);
                if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                        mapScene,
                        entityScene,
                        mapRoot.GenerationId,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }

                RuntimeCityRAndDMapView view = RequireMapView(mapScene);
                DenseCityProtectedAutobahnRouteDescriptor protectedAutobahnReplacement =
                    DenseCityCandidateAuthoringTransaction
                        .CreateProtectedAutobahnReplacementDescriptor(entityScene, view);
                bool hasAcceptedAutobahnManifest =
                    entityRoot.GetComponentInChildren<
                        DenseCityProtectedAutobahnReplacementManifestAuthoring>(true) != null;
                DenseMiddleEasternCityEditModeBuilder.Result generated;
                DenseCityGenerationRecordSnapshot backfillRecords;
                if (hasAcceptedAutobahnManifest)
                {
                    DenseMiddleEasternCityEditModeBuilder.Result replacementGenerated =
                        RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(
                            view,
                            protectedAutobahnReplacement);
                    generated = RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(view);
                    backfillRecords = new DenseCityGenerationRecordSnapshot(
                        replacementGenerated.Records.Buildings,
                        generated.Records.Surfaces,
                        generated.Records.Presentations);
                }
                else
                {
                    generated = RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(
                        view,
                        protectedAutobahnReplacement);
                    backfillRecords = generated.Records;
                }
                try
                {
                    BackfillResult result = Apply(
                        backfillRecords,
                        DenseCityPresentationHierarchyContext.Create(entityRoot),
                        entityRoot);
                    if (!hasAcceptedAutobahnManifest)
                    {
                        DenseCityCandidateAuthoringTransaction.MarkRealizedProtectedAutobahnTiles(
                            entityRoot,
                            view.GeneratedRoot,
                            protectedAutobahnReplacement);
                    }
                    ConfigureDenseReadinessContract(entityScene);
                    if (!DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                            mapScene,
                            entityScene,
                            OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                            mapRoot.GenerationId,
                            out string readinessError))
                    {
                        throw new InvalidOperationException(readinessError);
                    }
                    if (!EditorSceneManager.SaveScene(entityScene, entityPath, false))
                        throw new InvalidOperationException("Dense-city identity candidate save failed.");

                    RequireProtectedHashes(protectedPaths, protectedHashes);
                    WriteReport(projectRoot, result, ComputeHash(entityPath));
                    summary =
                        $"buildings={result.Buildings} renderOnly={result.RenderOnly} " +
                        $"added={result.Added} existing={result.Existing} report={ReportPath}";
                }
                finally
                {
                    RuntimeCityRAndDEditModeBuilder.Clear(view);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                if (entityScene.IsValid() && entityScene.isLoaded)
                    EditorSceneManager.CloseScene(entityScene, true);
                entityScene = default;
                File.Copy(entityBackup, ToPhysicalPath(entityPath), true);
                File.Copy(entityMetaBackup, ToPhysicalPath(entityMetaPath), true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return false;
            }
            finally
            {
                if (mapScene.IsValid() && mapScene.isLoaded)
                    EditorSceneManager.CloseScene(mapScene, true);
                if (entityScene.IsValid() && entityScene.isLoaded)
                    EditorSceneManager.CloseScene(entityScene, true);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
                File.Delete(entityBackup);
                File.Delete(entityMetaBackup);
            }
        }

        private static Dictionary<string, OperationMapBuildingAuthoring> IndexGeneratedBuildings(
            DenseCityGeneratedRootAuthoring entityRoot,
            int expectedCount)
        {
            OperationMapBuildingAuthoring[] buildings =
                entityRoot.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
            var result = new Dictionary<string, OperationMapBuildingAuthoring>(
                buildings.Length,
                StringComparer.Ordinal);
            for (int index = 0; index < buildings.Length; index++)
            {
                OperationMapBuildingAuthoring building = buildings[index];
                if (!OperationMapIdentityRules.IsValidGeneratedStableId(building.StableId) ||
                    !result.TryAdd(building.StableId, building))
                {
                    throw new InvalidOperationException(
                        $"Generated building has an invalid or duplicate stable id: '{building.StableId}'.");
                }
            }
            if (result.Count != expectedCount)
            {
                throw new InvalidOperationException(
                    $"Generated building count is {result.Count}; expected {expectedCount}.");
            }
            return result;
        }

        private static void ConfigureDenseReadinessContract(Scene entityScene)
        {
            OperationMapEntityPresentationRootAuthoring[] roots = entityScene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true))
                .ToArray();
            OperationMapEntityPresentationRootAuthoring buildingRoot = roots.Single(root =>
                root.Role == OperationMapEntityPresentationRole.GameplayBuildings);
            var serialized = new SerializedObject(buildingRoot);
            serialized.FindProperty("expectedGameplayBuildingCount").intValue =
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedDenseGameplayBuildings;
            serialized.FindProperty("expectedGameplayVehicleCount").intValue =
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles;
            serialized.FindProperty("expectedRenderOnlyCount").intValue =
                OperationMapEntityPresentationCandidateBakeValidator.ExpectedRenderOnlyOwners +
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedRenderOnlyOwners;
            serialized.FindProperty("expectedGeneratedIdentityCount").intValue =
                OperationMapEntityPresentationCandidateBakeValidator
                    .ExpectedDenseGeneratedIdentities;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(buildingRoot);
            if (!buildingRoot.TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        private static void RequirePresentationMatch(
            Transform owner,
            DenseCityPresentationBakeRecord record)
        {
            GameObject prefab =
                DenseCityRenderOnlyPresentationRealizer.LoadRequiredPrefab(record, out _);
            if (PrefabUtility.GetCorrespondingObjectFromSource(owner.gameObject) != prefab ||
                !string.Equals(
                    owner.name,
                    $"{prefab.name}_{record.Identity.DeterministicSequence:D6}",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Dense-city presentation source/name drift: '{record.Identity.StableKey}'.");
            }

            DenseCityRenderOnlyPresentationRealizer.RequireMaterialIdentity(owner.gameObject, record);
            DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(
                owner.localToWorldMatrix,
                record);
        }

        private static void PlanIdentity(
            List<PlannedIdentity> plan,
            GameObject owner,
            string stableId,
            OperationMapEntityPresentationRole role,
            DenseCityPresentationSemanticCategory category,
            bool allowsProtectedOverlap)
        {
            DenseCityPresentationIdentityAuthoring identity =
                owner.GetComponent<DenseCityPresentationIdentityAuthoring>();
            if (identity != null)
            {
                if (!string.Equals(identity.StableId, stableId, StringComparison.Ordinal) ||
                    identity.Role != role)
                {
                    throw new InvalidOperationException(
                        $"Existing dense-city identity mismatch on '{owner.name}'.");
                }
                if (identity.Category != category ||
                    identity.AllowsProtectedOverlap != allowsProtectedOverlap)
                {
                    identity.ConfigureForEditor(
                        stableId,
                        role,
                        category,
                        allowsProtectedOverlap);
                    EditorUtility.SetDirty(identity);
                }
                plan.Add(new PlannedIdentity(
                    owner,
                    stableId,
                    role,
                    category,
                    allowsProtectedOverlap,
                    true));
            }
            else
            {
                plan.Add(new PlannedIdentity(
                    owner,
                    stableId,
                    role,
                    category,
                    allowsProtectedOverlap,
                    false));
            }
        }

        private static DenseCityGeneratedRootAuthoring RequireGeneratedRoot(
            Scene scene,
            DenseCityGeneratedRootRole role)
        {
            DenseCityGeneratedRootAuthoring[] roots = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .Where(root => root.Role == role)
                .ToArray();
            return roots.Length == 1
                ? roots[0]
                : throw new InvalidOperationException(
                    $"Dense-city candidate requires exactly one {role} root.");
        }

        private static RuntimeCityRAndDMapView RequireMapView(Scene scene)
        {
            RuntimeCityRAndDMapView[] views = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RuntimeCityRAndDMapView>(true))
                .ToArray();
            return views.Length == 1
                ? views[0]
                : throw new InvalidOperationException(
                    "Dense-city map candidate requires exactly one runtime city map view.");
        }

        private static void RequireProtectedHashes(string[] paths, string[] hashes)
        {
            for (int index = 0; index < paths.Length; index++)
            {
                string actual = ComputeHash(paths[index]);
                if (!string.Equals(actual, hashes[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Protected asset changed during dense-city identity backfill: '{paths[index]}'.");
                }
            }
        }

        private static void WriteReport(
            string projectRoot,
            BackfillResult result,
            string candidateHash)
        {
            var report = new BackfillReport
            {
                schema = "warline.dense-city.generated-presentation-identity-backfill",
                schemaVersion = 1,
                status = "Passed",
                buildings = result.Buildings,
                renderOnly = result.RenderOnly,
                added = result.Added,
                existing = result.Existing,
                candidateEntitySceneSha256 = candidateHash
            };
            string absolutePath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ComputeHash(string assetPath)
        {
            using FileStream stream = File.OpenRead(ToPhysicalPath(assetPath));
            using SHA256 sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static string ToPhysicalPath(string assetPath) =>
            Path.GetFullPath(assetPath);

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry => entry.isLoaded && entry.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private readonly struct PlannedIdentity
        {
            internal PlannedIdentity(
                GameObject owner,
                string stableId,
                OperationMapEntityPresentationRole role,
                DenseCityPresentationSemanticCategory category,
                bool allowsProtectedOverlap,
                bool existing)
            {
                Owner = owner;
                StableId = stableId;
                Role = role;
                Category = category;
                AllowsProtectedOverlap = allowsProtectedOverlap;
                Existing = existing;
            }

            internal GameObject Owner { get; }
            internal string StableId { get; }
            internal OperationMapEntityPresentationRole Role { get; }
            internal DenseCityPresentationSemanticCategory Category { get; }
            internal bool AllowsProtectedOverlap { get; }
            internal bool Existing { get; }
        }

        internal readonly struct BackfillResult
        {
            internal BackfillResult(int buildings, int renderOnly, int added, int existing)
            {
                Buildings = buildings;
                RenderOnly = renderOnly;
                Added = added;
                Existing = existing;
            }

            internal int Buildings { get; }
            internal int RenderOnly { get; }
            internal int Added { get; }
            internal int Existing { get; }
        }

        [Serializable]
        private sealed class BackfillReport
        {
            public string schema;
            public int schemaVersion;
            public string status;
            public int buildings;
            public int renderOnly;
            public int added;
            public int existing;
            public string candidateEntitySceneSha256;
        }
    }
}
