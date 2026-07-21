#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Authoring;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Candidate-only copy of accepted static render-only migration owners into the protected
    /// <c>AuthoredOperationMapEntityPresentation/RenderOnly/*</c> buckets. Never mutates the
    /// accepted source scene, accepted SubScene, Addressables, or production presentation mode.
    /// </summary>
    internal static class OperationMapRenderOnlyCandidateMigrationEditor
    {
        internal const int ExpectedOwnerCount = 9090;
        internal const int ExpectedBuildingCount = 432;

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Populate Candidate Render-Only Owners")]
        public static void PopulateCandidateRenderOnlyOwners()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            string candidateMetaPhysicalPath = candidatePhysicalPath + ".meta";
            if (!File.Exists(candidatePhysicalPath) || !File.Exists(candidateMetaPhysicalPath))
                throw new FileNotFoundException("Protected candidate SubScene has not been created.", candidatePhysicalPath);

            byte[] candidateBackup = File.ReadAllBytes(candidatePhysicalPath);
            byte[] candidateMetaBackup = File.ReadAllBytes(candidateMetaPhysicalPath);
            string acceptedSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
            string acceptedSubSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report =
                    LoadInventoryReport();
                if (!OperationMapRenderOnlyCandidateMigrationPlanner.TryCreatePlan(
                        report.owners,
                        out OperationMapRenderOnlyCandidateMigrationPlan plan,
                        out string planRejection))
                {
                    throw new InvalidOperationException($"Render-only migration plan rejected: {planRejection}");
                }

                if (plan.OwnerCount != ExpectedOwnerCount ||
                    report.counts.migrationOwnerCount != ExpectedOwnerCount ||
                    report.owners.Count != ExpectedOwnerCount)
                {
                    throw new InvalidOperationException(
                        $"Expected {ExpectedOwnerCount} render-only owners, plan={plan.OwnerCount}, " +
                        $"counts={report.counts.migrationOwnerCount}, owners={report.owners.Count}.");
                }

                Scene sourceScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);

                Transform renderOnlyRoot = RequirePath(
                    candidateScene,
                    "AuthoredOperationMapEntityPresentation/RenderOnly");
                RequireBucketsEmpty(renderOnlyRoot);
                RequireBuildingCount(candidateScene, ExpectedBuildingCount);

                var bucketRoots = new Dictionary<string, Transform>(StringComparer.Ordinal);
                var parentProxyCaches = new Dictionary<string, Dictionary<string, Transform>>(StringComparer.Ordinal);
                for (int i = 0; i < plan.CountsByBucket.Count; i++)
                {
                    string bucket = plan.CountsByBucket[i].Name;
                    Transform bucketRoot = renderOnlyRoot.Find(bucket);
                    if (bucketRoot == null)
                        throw new InvalidOperationException($"Candidate RenderOnly bucket is missing: {bucket}");
                    bucketRoots[bucket] = bucketRoot;
                    parentProxyCaches[bucket] = new Dictionary<string, Transform>(StringComparer.Ordinal);
                }

                int migrated = 0;
                for (int i = 0; i < plan.Assignments.Count; i++)
                {
                    OperationMapRenderOnlyCandidateAssignment assignment = plan.Assignments[i];
                    if (!GlobalObjectId.TryParse(assignment.SourceOwnerGlobalObjectId, out GlobalObjectId sourceId) ||
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) is not GameObject sourceOwner ||
                        sourceOwner.scene != sourceScene)
                    {
                        throw new InvalidOperationException(
                            $"Render-only source identity could not be resolved: {assignment.SourceOwnerGlobalObjectId}");
                    }

                    if (!string.Equals(
                            NormalizeNameHierarchyPath(sourceOwner.transform),
                            assignment.NameHierarchyPath,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Render-only source hierarchy drifted for {assignment.SourceOwnerGlobalObjectId}: " +
                            $"expected '{assignment.NameHierarchyPath}', actual '{NormalizeNameHierarchyPath(sourceOwner.transform)}'.");
                    }

                    if (!bucketRoots.TryGetValue(assignment.DestinationBucket, out Transform bucketRoot))
                        throw new InvalidOperationException($"Missing destination bucket: {assignment.DestinationBucket}");

                    Transform candidateParent = RequireMirroredParentChain(
                        sourceOwner.transform.parent,
                        bucketRoot,
                        candidateScene,
                        parentProxyCaches[assignment.DestinationBucket]);
                    GameObject candidateOwner = UnityEngine.Object.Instantiate(sourceOwner);
                    candidateOwner.name = sourceOwner.name;
                    SceneManager.MoveGameObjectToScene(candidateOwner, candidateScene);
                    candidateOwner.transform.SetParent(candidateParent, false);
                    candidateOwner.SetActive(true);
                    OperationMapEntityPresentationIdentityAuthoring identity =
                        candidateOwner.GetComponent<OperationMapEntityPresentationIdentityAuthoring>() ??
                        candidateOwner.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
                    identity.ConfigureForEditor(
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        assignment.SourceOwnerGlobalObjectId,
                        OperationMapEntityPresentationRole.RenderOnly,
                        OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
                    if (!identity.TryValidate(out string identityError))
                        throw new InvalidOperationException(identityError);
                    RemoveProhibitedCandidateComponents(candidateOwner);
                    RequireExactVisualParity(sourceOwner, candidateOwner, assignment.SourceOwnerGlobalObjectId);
                    migrated++;

                    if ((migrated % 1000) == 0)
                    {
                        Debug.Log(
                            $"[OperationMapRenderOnlyCandidateMigrationEditor] progress={migrated}/{plan.OwnerCount}");
                    }
                }

                if (migrated != ExpectedOwnerCount)
                    throw new InvalidOperationException($"Expected {ExpectedOwnerCount} migrated owners, found {migrated}.");

                RequireBuildingCount(candidateScene, ExpectedBuildingCount);
                RequireBucketCounts(renderOnlyRoot, plan);

                if (!EditorSceneManager.SaveScene(candidateScene, candidatePath, false))
                    throw new InvalidOperationException("Candidate render-only migration save failed.");
                EditorSceneManager.CloseScene(candidateScene, true);
                EditorSceneManager.CloseScene(sourceScene, true);

                RequireHashUnchanged(
                    acceptedSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
                RequireHashUnchanged(
                    acceptedSubSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

                string bucketSummary = string.Join(
                    ",",
                    plan.CountsByBucket.Select(entry => $"{entry.Name}={entry.Count}"));
                Debug.Log(
                    $"[OperationMapRenderOnlyCandidateMigrationEditor] status=Created owners={migrated} " +
                    $"candidate={candidatePath} buckets={bucketSummary} " +
                    $"buildingsPreserved={ExpectedBuildingCount} productionCutover=0");
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
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[OperationMapRenderOnlyCandidateMigrationEditor] scene-setup-restore-skipped: {exception.Message}");
                }
            }
        }

        internal static OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport LoadInventoryReport()
        {
            string reportPath =
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
                JsonUtility.FromJson<OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport>(json);
            if (report == null ||
                report.counts == null ||
                report.owners == null ||
                report.counts.migrationOwnerCount != report.owners.Count)
            {
                throw new InvalidOperationException("Migration inventory owners section is incomplete or drifted.");
            }

            return report;
        }

        private static void RequireBucketsEmpty(Transform renderOnlyRoot)
        {
            for (int i = 0; i < renderOnlyRoot.childCount; i++)
            {
                Transform bucket = renderOnlyRoot.GetChild(i);
                if (bucket.childCount != 0)
                {
                    throw new InvalidOperationException(
                        $"Candidate RenderOnly/{bucket.name} is not empty; refusing to overwrite it.");
                }
            }
        }

        private static void RequireBucketCounts(
            Transform renderOnlyRoot,
            OperationMapRenderOnlyCandidateMigrationPlan plan)
        {
            for (int i = 0; i < plan.CountsByBucket.Count; i++)
            {
                OperationMapRenderOnlyBucketCount expected = plan.CountsByBucket[i];
                Transform bucket = renderOnlyRoot.Find(expected.Name);
                if (bucket == null)
                    throw new InvalidOperationException($"Candidate RenderOnly bucket disappeared: {expected.Name}");
                int actual = bucket
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)
                    .Count(identity => identity.Role == OperationMapEntityPresentationRole.RenderOnly);
                if (actual != expected.Count)
                {
                    throw new InvalidOperationException(
                        $"Candidate RenderOnly/{expected.Name} expected {expected.Count} owners, found {actual}.");
                }
            }
        }

        internal static Transform RequireMirroredParentChain(
            Transform sourceParent,
            Transform bucketRoot,
            Scene candidateScene,
            Dictionary<string, Transform> cache)
        {
            if (sourceParent == null)
                return bucketRoot;

            string sourceId = GlobalObjectId.GetGlobalObjectIdSlow(sourceParent.gameObject).ToString();
            if (cache.TryGetValue(sourceId, out Transform existing))
                return existing;

            Transform mirroredParent = RequireMirroredParentChain(
                sourceParent.parent,
                bucketRoot,
                candidateScene,
                cache);
            var proxy = new GameObject($"__SourceTransform__{sourceParent.name}");
            SceneManager.MoveGameObjectToScene(proxy, candidateScene);
            proxy.transform.SetParent(mirroredParent, false);
            CopyLocalTransform(sourceParent, proxy.transform);
            cache.Add(sourceId, proxy.transform);
            return proxy.transform;
        }

        internal static void CopyLocalTransform(Transform source, Transform destination)
        {
            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        internal static void RequireExactVisualParity(
            GameObject sourceOwner,
            GameObject candidateOwner,
            string sourceGlobalObjectId)
        {
            const float matrixTolerance = 0.0001f;
            const float boundsTolerance = 0.001f;
            if (!OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                    sourceOwner.transform.localToWorldMatrix,
                    candidateOwner.transform.localToWorldMatrix,
                    matrixTolerance))
            {
                throw new InvalidOperationException(
                    $"Render-only owner matrix parity failed: {sourceGlobalObjectId}");
            }

            Renderer[] sourceRenderers = sourceOwner.GetComponentsInChildren<Renderer>(true);
            Renderer[] candidateRenderers = candidateOwner.GetComponentsInChildren<Renderer>(true);
            if (sourceRenderers.Length != candidateRenderers.Length)
            {
                throw new InvalidOperationException(
                    $"Render-only renderer count parity failed for {sourceGlobalObjectId}: " +
                    $"{sourceRenderers.Length}/{candidateRenderers.Length}.");
            }

            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                Renderer sourceRenderer = sourceRenderers[i];
                Renderer candidateRenderer = candidateRenderers[i];
                if (sourceRenderer.GetType() != candidateRenderer.GetType() ||
                    !OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                        sourceRenderer.transform.localToWorldMatrix,
                        candidateRenderer.transform.localToWorldMatrix,
                        matrixTolerance) ||
                    Vector3.Distance(sourceRenderer.bounds.center, candidateRenderer.bounds.center) > boundsTolerance ||
                    Vector3.Distance(sourceRenderer.bounds.size, candidateRenderer.bounds.size) > boundsTolerance)
                {
                    throw new InvalidOperationException(
                        $"Render-only renderer transform/bounds parity failed for " +
                        $"{sourceGlobalObjectId} at renderer {i} ({sourceRenderer.name}).");
                }
            }
        }

        private static void RequireBuildingCount(Scene candidateScene, int expected)
        {
            int actual = 0;
            GameObject[] roots = candidateScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                actual += roots[i].GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length;
            if (actual != expected)
                throw new InvalidOperationException($"Expected {expected} preserved buildings, found {actual}.");
        }

        private static string NormalizeNameHierarchyPath(Transform transform)
        {
            var segments = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        internal static void RemoveProhibitedCandidateComponents(GameObject owner)
        {
            foreach (Collider collider in owner.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            foreach (Rigidbody body in owner.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(body);
            foreach (Animator animator in owner.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                    UnityEngine.Object.DestroyImmediate(animator);
            }
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

        private static string ResolveProjectPath(string projectRoot, string repositoryPath) =>
            Path.GetFullPath(Path.Combine(projectRoot, repositoryPath));

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void RequireHashUnchanged(string expected, string path)
        {
            if (!string.Equals(expected, ComputeSha256(path), StringComparison.Ordinal))
                throw new InvalidOperationException($"Protected accepted source changed: {path}");
        }
    }
}

#endif
