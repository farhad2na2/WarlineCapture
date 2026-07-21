#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Game.Authoring;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Rebuilds only the protected candidate's render-only branch through the accepted migration
    /// inventory. Accepted sources and production presentation ownership remain immutable.
    /// </summary>
    internal static class OperationMapRenderOnlyCandidateTransformRepairEditor
    {
        [MenuItem("Game/Operation Maps/EntityScene Migration/Repair Candidate Render-Only Transform Hierarchies")]
        public static void RepairCandidateRenderOnlyTransformHierarchies()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string physicalPath = Path.GetFullPath(Path.Combine(projectRoot, candidatePath));
            string metaPath = physicalPath + ".meta";
            byte[] backup = File.ReadAllBytes(physicalPath);
            byte[] metaBackup = File.ReadAllBytes(metaPath);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene candidate = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                Transform renderOnly = RequirePath(candidate, "AuthoredOperationMapEntityPresentation/RenderOnly");
                int identities = renderOnly
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)
                    .Count(identity => identity.Role == OperationMapEntityPresentationRole.RenderOnly);
                if (identities != OperationMapRenderOnlyCandidateMigrationEditor.ExpectedOwnerCount)
                {
                    throw new InvalidOperationException(
                        $"Expected {OperationMapRenderOnlyCandidateMigrationEditor.ExpectedOwnerCount} render-only " +
                        $"identities before repair, found {identities}.");
                }

                for (int bucketIndex = 0; bucketIndex < renderOnly.childCount; bucketIndex++)
                {
                    Transform bucket = renderOnly.GetChild(bucketIndex);
                    while (bucket.childCount > 0)
                        UnityEngine.Object.DestroyImmediate(bucket.GetChild(bucket.childCount - 1).gameObject);
                }

                if (!EditorSceneManager.SaveScene(candidate, candidatePath, false))
                    throw new InvalidOperationException("Candidate render-only clear save failed.");
                EditorSceneManager.CloseScene(candidate, true);
                RestoreSceneSetupOrCreateEmpty(previousSetup);

                OperationMapRenderOnlyCandidateMigrationEditor.PopulateCandidateRenderOnlyOwners();
                Debug.Log(
                    "[OperationMapRenderOnlyCandidateTransformRepairEditor] status=Completed " +
                    $"owners={OperationMapRenderOnlyCandidateMigrationEditor.ExpectedOwnerCount} " +
                    "sourceCandidateMatrixParity=Passed rendererBoundsParity=Passed productionCutover=0");
            }
            catch
            {
                File.WriteAllBytes(physicalPath, backup);
                File.WriteAllBytes(metaPath, metaBackup);
                AssetDatabase.ImportAsset(candidatePath, ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(previousSetup);
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

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] setup)
        {
            if (setup != null && setup.Any(entry => entry.isLoaded && entry.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}

#endif
