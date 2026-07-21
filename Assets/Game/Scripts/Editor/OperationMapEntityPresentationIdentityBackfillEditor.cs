#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Game.Authoring;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Transactionally adds deterministic source identities to an already-populated candidate.
    /// This is candidate-only migration metadata; accepted source scenes and production ownership
    /// remain immutable.
    /// </summary>
    internal static class OperationMapEntityPresentationIdentityBackfillEditor
    {
        internal const int ExpectedBuildingCount = 432;
        internal const int ExpectedVehicleCount = 22;
        internal const int ExpectedRenderOnlyCount = 9090;
        internal const int ExpectedIdentityCount =
            ExpectedBuildingCount + ExpectedVehicleCount + ExpectedRenderOnlyCount;

        [MenuItem("Game/Operation Maps/EntityScene Migration/Backfill Candidate Presentation Identities")]
        public static void BackfillCandidatePresentationIdentities()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            string candidateMetaPhysicalPath = candidatePhysicalPath + ".meta";
            if (!File.Exists(candidatePhysicalPath) || !File.Exists(candidateMetaPhysicalPath))
                throw new FileNotFoundException("Protected candidate SubScene is missing.", candidatePhysicalPath);

            byte[] candidateBackup = File.ReadAllBytes(candidatePhysicalPath);
            byte[] candidateMetaBackup = File.ReadAllBytes(candidateMetaPhysicalPath);
            string acceptedScenePath =
                OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath;
            string acceptedSubScenePath = OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath;
            string acceptedSceneHash = ComputeSha256(ResolveProjectPath(projectRoot, acceptedScenePath));
            string acceptedSubSceneHash = ComputeSha256(ResolveProjectPath(projectRoot, acceptedSubScenePath));
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport buildings =
                    OperationMapBuildingCandidateMigrationEditor.LoadInventory(projectRoot);
                OperationMapVehicleEcsConversionInventoryProbe.ConversionReport vehicles =
                    OperationMapVehicleCandidateMigrationEditor.LoadInventory(projectRoot);
                OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport renderInventory =
                    OperationMapRenderOnlyCandidateMigrationEditor.LoadInventoryReport();
                if (!OperationMapRenderOnlyCandidateMigrationPlanner.TryCreatePlan(
                        renderInventory.owners,
                        out OperationMapRenderOnlyCandidateMigrationPlan renderPlan,
                        out string planRejection))
                {
                    throw new InvalidOperationException($"Render-only identity plan rejected: {planRejection}");
                }

                RequireInventoryCounts(buildings, vehicles, renderPlan);
                HashSet<string> expectedKeys = BuildExpectedKeys(buildings, vehicles, renderPlan);

                Scene sourceScene = EditorSceneManager.OpenScene(acceptedScenePath, OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                Transform candidateRoot = RequirePath(candidateScene, "AuthoredOperationMapEntityPresentation");
                OperationMapEntityPresentationIdentityAuthoring[] existing =
                    candidateRoot.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true);

                if (existing.Length == ExpectedIdentityCount)
                {
                    RequireExactIdentitySet(existing, expectedKeys);
                    Debug.Log(
                        $"[OperationMapEntityPresentationIdentityBackfillEditor] status=AlreadyComplete " +
                        $"identities={existing.Length} productionCutover=0");
                    return;
                }

                if (existing.Length != 0)
                {
                    throw new InvalidOperationException(
                        $"Candidate identity backfill is partial: {existing.Length}/{ExpectedIdentityCount}.");
                }

                int added = 0;
                added += BackfillBuildings(candidateScene, sourceScene, buildings);
                added += BackfillVehicles(candidateScene, sourceScene, vehicles);
                added += BackfillRenderOnly(
                    candidateScene,
                    sourceScene,
                    renderPlan,
                    out int renderOnlyTransformMismatchCount);
                if (added != ExpectedIdentityCount)
                    throw new InvalidOperationException($"Expected {ExpectedIdentityCount} identities, added {added}.");

                OperationMapEntityPresentationIdentityAuthoring[] completed =
                    candidateRoot.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true);
                RequireExactIdentitySet(completed, expectedKeys);
                if (!EditorSceneManager.SaveScene(candidateScene, candidatePath, false))
                    throw new InvalidOperationException("Candidate identity backfill save failed.");

                RequireHashUnchanged(acceptedSceneHash, ResolveProjectPath(projectRoot, acceptedScenePath));
                RequireHashUnchanged(acceptedSubSceneHash, ResolveProjectPath(projectRoot, acceptedSubScenePath));
                Debug.Log(
                    $"[OperationMapEntityPresentationIdentityBackfillEditor] status=Completed " +
                    $"identities={completed.Length} buildings={ExpectedBuildingCount} " +
                    $"vehicles={ExpectedVehicleCount} renderOnly={ExpectedRenderOnlyCount} productionCutover=0");
                if (renderOnlyTransformMismatchCount > 0)
                {
                    Debug.LogWarning(
                        $"[OperationMapEntityPresentationIdentityBackfillEditor] " +
                        $"renderOnlySourceMatrixMismatch={renderOnlyTransformMismatchCount} " +
                        $"status=IdentityRecoveredParityRejectedPendingMigrationFix");
                }
            }
            catch
            {
                File.WriteAllBytes(candidatePhysicalPath, candidateBackup);
                File.WriteAllBytes(candidateMetaPhysicalPath, candidateMetaBackup);
                AssetDatabase.ImportAsset(candidatePath, ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static int BackfillBuildings(
            Scene candidateScene,
            Scene sourceScene,
            OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport report)
        {
            OperationMapBuildingAuthoring[] authorings = candidateScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true))
                .OrderBy(authoring => authoring.PlacementIndex)
                .ToArray();
            if (authorings.Length != ExpectedBuildingCount)
                throw new InvalidOperationException($"Candidate building count is {authorings.Length}.");

            for (int i = 0; i < authorings.Length; i++)
            {
                OperationMapBuildingAuthoring authoring = authorings[i];
                OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildingPlacementReport row =
                    report.placements[i];
                if (authoring.PlacementIndex != i || row.placementIndex != i ||
                    !string.Equals(authoring.SourceGlobalObjectId, row.ownerSourceGlobalObjectId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Candidate building identity drifted at placement {i}.");
                }

                GameObject source = ResolveSource(row.ownerSourceGlobalObjectId, sourceScene, $"building {i}");
                Transform intactRoot = authoring.IntactVisualRoot != null
                    ? authoring.IntactVisualRoot.transform
                    : throw new InvalidOperationException($"Candidate building {i} has no intact visual root.");
                if (intactRoot.childCount != 1 ||
                    !string.Equals(intactRoot.GetChild(0).name, source.name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Candidate building visual owner drifted at placement {i}.");
                }

                Configure(
                    intactRoot.GetChild(0).gameObject,
                    row.ownerSourceGlobalObjectId,
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    i);
            }

            return authorings.Length;
        }

        private static int BackfillVehicles(
            Scene candidateScene,
            Scene sourceScene,
            OperationMapVehicleEcsConversionInventoryProbe.ConversionReport report)
        {
            Transform vehicleRoot = RequirePath(
                candidateScene,
                "AuthoredOperationMapEntityPresentation/GameplayVehicles");
            if (vehicleRoot.childCount != ExpectedVehicleCount)
                throw new InvalidOperationException($"Candidate vehicle count is {vehicleRoot.childCount}.");

            for (int i = 0; i < report.placements.Count; i++)
            {
                OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport row = report.placements[i];
                GameObject source = ResolveSource(row.authoredSourceGlobalObjectId, sourceScene, $"vehicle {i}");
                Transform candidate = vehicleRoot.GetChild(i);
                string expectedName = $"Vehicle_{i:D2}_{source.name}";
                if (row.placementIndex != i ||
                    !string.Equals(candidate.name, expectedName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Candidate vehicle owner drifted at placement {i}.");
                }

                Configure(
                    candidate.gameObject,
                    row.authoredSourceGlobalObjectId,
                    OperationMapEntityPresentationRole.GameplayVehicles,
                    i);
            }

            return report.placements.Count;
        }

        private static int BackfillRenderOnly(
            Scene candidateScene,
            Scene sourceScene,
            OperationMapRenderOnlyCandidateMigrationPlan plan,
            out int sourceMatrixMismatchCount)
        {
            Transform renderRoot = RequirePath(candidateScene, "AuthoredOperationMapEntityPresentation/RenderOnly");
            var candidatesByBucketAndName = new Dictionary<
                string,
                Dictionary<string, List<Transform>>>(StringComparer.Ordinal);
            var usedCandidates = new HashSet<Transform>();
            sourceMatrixMismatchCount = 0;
            for (int bucketIndex = 0; bucketIndex < renderRoot.childCount; bucketIndex++)
            {
                Transform bucket = renderRoot.GetChild(bucketIndex);
                var candidatesByName = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
                for (int candidateIndex = 0; candidateIndex < bucket.childCount; candidateIndex++)
                {
                    Transform candidate = bucket.GetChild(candidateIndex);
                    if (!candidatesByName.TryGetValue(candidate.name, out List<Transform> candidates))
                    {
                        candidates = new List<Transform>();
                        candidatesByName[candidate.name] = candidates;
                    }
                    candidates.Add(candidate);
                }
                candidatesByBucketAndName[bucket.name] = candidatesByName;
            }

            int added = 0;
            for (int i = 0; i < plan.Assignments.Count; i++)
            {
                OperationMapRenderOnlyCandidateAssignment assignment = plan.Assignments[i];
                if (!candidatesByBucketAndName.TryGetValue(
                        assignment.DestinationBucket,
                        out Dictionary<string, List<Transform>> candidatesByName))
                {
                    throw new InvalidOperationException(
                        $"Candidate render-only bucket is missing: {assignment.DestinationBucket}");
                }

                GameObject source = ResolveSource(
                    assignment.SourceOwnerGlobalObjectId,
                    sourceScene,
                    $"render-only assignment {i}");
                if (!candidatesByName.TryGetValue(source.name, out List<Transform> namedCandidates))
                {
                    throw new InvalidOperationException(
                        $"Candidate render-only owner name is missing for {assignment.SourceOwnerGlobalObjectId}: " +
                        $"{assignment.DestinationBucket}/{source.name}.");
                }

                Transform candidate = null;
                int matchCount = 0;
                Matrix4x4 sourceWorldMatrix = source.transform.localToWorldMatrix;
                Matrix4x4 existingCopyWorldMatrix = CreateExistingCopyWorldMatrix(source.transform);
                for (int candidateIndex = 0; candidateIndex < namedCandidates.Count; candidateIndex++)
                {
                    Transform possible = namedCandidates[candidateIndex];
                    if (usedCandidates.Contains(possible) ||
                        !MatricesApproximatelyEqual(
                            possible.localToWorldMatrix,
                            existingCopyWorldMatrix,
                            0.0001f))
                    {
                        continue;
                    }

                    candidate = possible;
                    matchCount++;
                }

                if (matchCount != 1)
                {
                    string diagnostics = string.Join(
                        "; ",
                        namedCandidates.Take(4).Select(possible =>
                            $"candidate={possible.position.ToString("R")}" +
                            $" localScale={possible.localScale.ToString("R")}" +
                            $" lossyScale={possible.lossyScale.ToString("R")}" +
                            $" sourceDelta={MaximumMatrixDelta(possible.localToWorldMatrix, sourceWorldMatrix):R}" +
                            $" copyDelta={MaximumMatrixDelta(possible.localToWorldMatrix, existingCopyWorldMatrix):R}"));
                    throw new InvalidOperationException(
                        $"Candidate render-only owner join count is {matchCount} for " +
                        $"{assignment.SourceOwnerGlobalObjectId} ({assignment.DestinationBucket}/{source.name}). " +
                        $"source={source.transform.position.ToString("R")} " +
                        $"localScale={source.transform.localScale.ToString("R")} " +
                        $"lossyScale={source.transform.lossyScale.ToString("R")} " +
                        $"namedCandidates={namedCandidates.Count}; {diagnostics}");
                }

                usedCandidates.Add(candidate);
                if (!MatricesApproximatelyEqual(candidate.localToWorldMatrix, sourceWorldMatrix, 0.0001f))
                    sourceMatrixMismatchCount++;

                Configure(
                    candidate.gameObject,
                    assignment.SourceOwnerGlobalObjectId,
                    OperationMapEntityPresentationRole.RenderOnly,
                    OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
                added++;
            }

            if (usedCandidates.Count != ExpectedRenderOnlyCount)
                throw new InvalidOperationException($"Matched render-only candidate count is {usedCandidates.Count}.");

            return added;
        }

        internal static Matrix4x4 CreateExistingCopyWorldMatrix(Transform source) =>
            Matrix4x4.TRS(source.localPosition, source.localRotation, source.localScale);

        private static void Configure(
            GameObject owner,
            string sourceGlobalObjectId,
            OperationMapEntityPresentationRole role,
            int placementIndex)
        {
            OperationMapEntityPresentationIdentityAuthoring identity =
                owner.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
            identity.ConfigureForEditor(
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                sourceGlobalObjectId,
                role,
                placementIndex);
            if (!identity.TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        private static HashSet<string> BuildExpectedKeys(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport buildings,
            OperationMapVehicleEcsConversionInventoryProbe.ConversionReport vehicles,
            OperationMapRenderOnlyCandidateMigrationPlan renderPlan)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < buildings.placements.Count; i++)
                AddExpectedKey(keys, buildings.placements[i].ownerSourceGlobalObjectId,
                    OperationMapEntityPresentationRole.GameplayBuildings, i);
            for (int i = 0; i < vehicles.placements.Count; i++)
                AddExpectedKey(keys, vehicles.placements[i].authoredSourceGlobalObjectId,
                    OperationMapEntityPresentationRole.GameplayVehicles, i);
            for (int i = 0; i < renderPlan.Assignments.Count; i++)
                AddExpectedKey(keys, renderPlan.Assignments[i].SourceOwnerGlobalObjectId,
                    OperationMapEntityPresentationRole.RenderOnly,
                    OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
            if (keys.Count != ExpectedIdentityCount)
                throw new InvalidOperationException($"Expected identity set is not unique: {keys.Count}.");
            return keys;
        }

        private static void AddExpectedKey(
            HashSet<string> keys,
            string sourceGlobalObjectId,
            OperationMapEntityPresentationRole role,
            int placementIndex)
        {
            string key = CreateKey(sourceGlobalObjectId, role, placementIndex);
            if (!keys.Add(key))
                throw new InvalidOperationException($"Duplicate expected presentation identity: {key}.");
        }

        private static void RequireExactIdentitySet(
            IReadOnlyCollection<OperationMapEntityPresentationIdentityAuthoring> identities,
            HashSet<string> expectedKeys)
        {
            if (identities.Count != ExpectedIdentityCount)
                throw new InvalidOperationException($"Candidate identity count is {identities.Count}.");

            var actualKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (OperationMapEntityPresentationIdentityAuthoring identity in identities)
            {
                if (!identity.TryValidate(out string error))
                    throw new InvalidOperationException(error);
                string key = CreateKey(identity.SourceGlobalObjectId, identity.Role, identity.PlacementIndex);
                if (!actualKeys.Add(key))
                    throw new InvalidOperationException($"Duplicate candidate presentation identity: {key}.");
            }

            if (!actualKeys.SetEquals(expectedKeys))
                throw new InvalidOperationException("Candidate presentation identities do not match accepted inventories.");
        }

        private static string CreateKey(
            string sourceGlobalObjectId,
            OperationMapEntityPresentationRole role,
            int placementIndex) => $"{(byte)role}|{placementIndex}|{sourceGlobalObjectId}";

        private static void RequireInventoryCounts(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport buildings,
            OperationMapVehicleEcsConversionInventoryProbe.ConversionReport vehicles,
            OperationMapRenderOnlyCandidateMigrationPlan renderPlan)
        {
            if (buildings?.placements?.Count != ExpectedBuildingCount ||
                buildings.counts?.placementCount != ExpectedBuildingCount ||
                vehicles?.placements?.Count != ExpectedVehicleCount ||
                vehicles.counts?.placementCount != ExpectedVehicleCount ||
                renderPlan.OwnerCount != ExpectedRenderOnlyCount)
            {
                throw new InvalidOperationException("Accepted presentation identity inventory counts drifted.");
            }
        }

        private static GameObject ResolveSource(string value, Scene sourceScene, string label)
        {
            if (!GlobalObjectId.TryParse(value, out GlobalObjectId globalObjectId) ||
                GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) is not GameObject source ||
                source.scene != sourceScene)
            {
                throw new InvalidOperationException($"Could not resolve accepted source for {label}: {value}");
            }

            return source;
        }

        internal static bool MatricesApproximatelyEqual(Matrix4x4 left, Matrix4x4 right, float tolerance)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(left[i] - right[i]) > tolerance)
                    return false;
            }

            return true;
        }

        private static float MaximumMatrixDelta(Matrix4x4 left, Matrix4x4 right)
        {
            float maximum = 0f;
            for (int i = 0; i < 16; i++)
                maximum = Mathf.Max(maximum, Mathf.Abs(left[i] - right[i]));
            return maximum;
        }

        private static Transform RequirePath(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(owner => owner.name == segments[0]);
            Transform current = root != null ? root.transform : null;
            for (int i = 1; i < segments.Length && current != null; i++)
                current = current.Find(segments[i]);
            return current ?? throw new InvalidOperationException($"Candidate hierarchy path is missing: {path}");
        }

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry => entry.isLoaded && entry.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static string ResolveProjectPath(string projectRoot, string repositoryPath) =>
            Path.GetFullPath(Path.Combine(projectRoot, repositoryPath));

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void RequireHashUnchanged(string expected, string path)
        {
            string actual = ComputeSha256(path);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException($"Protected source changed: {path}");
        }
    }
}

#endif
