#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Game.Authoring;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Candidate-only copy of the 22 accepted vehicle instances under
    /// <c>AuthoredOperationMapEntityPresentation/GameplayVehicles</c>. The accepted scene
    /// instance is authoritative so per-instance renderer-child transforms survive while
    /// the existing <c>UnitGridAuthoring</c> baker path remains unchanged. Does not mutate
    /// accepted source, Addressables, or production presentation mode.
    /// </summary>
    internal static class OperationMapVehicleCandidateMigrationEditor
    {
        internal const string VehicleInventoryPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.json";
        internal const int ExpectedVehicleCount = 22;

        [MenuItem("Game/Operation Maps/EntityScene Migration/Populate Candidate Gameplay Vehicles")]
        public static void PopulateCandidateGameplayVehicles()
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
                OperationMapVehicleEcsConversionInventoryProbe.ConversionReport report = LoadInventory(projectRoot);
                MapVehiclePlacementConfig placements = AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(
                    report.vehiclePlacementConfigPath);
                if (placements == null || placements.Placements.Count != report.placements.Count ||
                    placements.Placements.Count != ExpectedVehicleCount)
                {
                    throw new InvalidOperationException("Vehicle placement configuration does not match the accepted inventory.");
                }

                Scene sourceScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                Transform vehicleRoot = RequirePath(
                    candidateScene,
                    "AuthoredOperationMapEntityPresentation/GameplayVehicles");
                if (vehicleRoot.childCount != 0)
                {
                    RequireReplaceableVehiclePopulation(vehicleRoot);
                    for (int childIndex = vehicleRoot.childCount - 1; childIndex >= 0; childIndex--)
                        UnityEngine.Object.DestroyImmediate(vehicleRoot.GetChild(childIndex).gameObject);
                }

                int migrated = 0;
                for (int i = 0; i < report.placements.Count; i++)
                {
                    OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport row =
                        report.placements[i];
                    if (row == null || row.placementIndex != i ||
                        !string.Equals(row.authoredJoinResolveState, "Exact", StringComparison.Ordinal) ||
                        !string.Equals(row.conversionDisposition, "AlreadyProducesEcsGameplayAndRender", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Vehicle inventory row {i} is not mutation-ready.");
                    }

                    MapVehiclePlacementConfigEntry placement = placements.Placements[i];
                    if (placement == null || placement.VehiclePrefab == null ||
                        !string.Equals(placement.SourcePath, row.sourcePath, StringComparison.Ordinal) ||
                        !string.Equals(AssetDatabase.GetAssetPath(placement.VehiclePrefab), row.vehiclePrefabPath,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Vehicle placement {i} drifted from its accepted inventory row.");
                    }

                    if (placement.VehiclePrefab.GetComponent<UnitGridAuthoring>() == null)
                        throw new InvalidOperationException($"Vehicle prefab lacks UnitGridAuthoring: {row.vehiclePrefabPath}");

                    if (!GlobalObjectId.TryParse(row.authoredSourceGlobalObjectId, out GlobalObjectId sourceId) ||
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) is not GameObject sourceOwner ||
                        sourceOwner.scene != sourceScene)
                    {
                        throw new InvalidOperationException($"Vehicle source identity could not be resolved at placement {i}.");
                    }

                    GameObject candidateOwner = PrefabUtility.InstantiatePrefab(
                        placement.VehiclePrefab,
                        candidateScene) as GameObject;
                    if (candidateOwner == null)
                        throw new InvalidOperationException($"Failed to instantiate gameplay vehicle prefab at placement {i}.");

                    candidateOwner.transform.SetParent(vehicleRoot, false);
                    candidateOwner.transform.SetPositionAndRotation(
                        placement.WorldPosition,
                        Quaternion.Euler(placement.WorldEulerAngles));
                    candidateOwner.transform.localScale = placement.WorldScale;
                    candidateOwner.SetActive(true);
                    UnitGridAuthoring candidateAuthoring = candidateOwner.GetComponent<UnitGridAuthoring>();
                    Transform placeholderModel = ResolveModelRoot(candidateAuthoring);
                    if (candidateAuthoring == null || placeholderModel == null)
                    {
                        throw new InvalidOperationException(
                            $"Gameplay vehicle prefab has no candidate Baker/model ownership at placement {i}.");
                    }

                    UnityEngine.Object.DestroyImmediate(placeholderModel.gameObject);
                    GameObject acceptedModel = UnityEngine.Object.Instantiate(sourceOwner);
                    acceptedModel.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(acceptedModel, candidateScene);
                    acceptedModel.name = "Model";
                    acceptedModel.transform.SetParent(candidateOwner.transform, true);
                    Transform candidateModel = acceptedModel.transform;
                    AlignModelToAcceptedVisual(candidateModel, sourceOwner.transform, i);
                    var candidateAuthoringSerialized = new SerializedObject(candidateAuthoring);
                    candidateAuthoringSerialized.FindProperty("modelRoot").objectReferenceValue = candidateModel;
                    candidateAuthoringSerialized.ApplyModifiedPropertiesWithoutUndo();

                    OperationMapEntityPresentationIdentityAuthoring identity =
                        candidateModel.GetComponent<OperationMapEntityPresentationIdentityAuthoring>() ??
                        candidateModel.gameObject.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
                    identity.ConfigureForEditor(
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        row.authoredSourceGlobalObjectId,
                        OperationMapEntityPresentationRole.GameplayVehicles,
                        i);
                    if (!identity.TryValidate(out string identityError))
                        throw new InvalidOperationException(identityError);
                    RemoveProhibitedCandidateComponents(candidateOwner);
                    RequireAcceptedVisualParity(sourceOwner, candidateModel, i);
                    migrated++;
                }

                if (migrated != ExpectedVehicleCount)
                    throw new InvalidOperationException($"Expected {ExpectedVehicleCount} migrated vehicles, found {migrated}.");

                int buildings = candidateScene.GetRootGameObjects()
                    .Sum(root => root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length);
                if (buildings != 432)
                    throw new InvalidOperationException($"Expected 432 preserved buildings, found {buildings}.");

                if (!EditorSceneManager.SaveScene(candidateScene, candidatePath, false))
                    throw new InvalidOperationException("Candidate vehicle migration save failed.");
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

                Debug.Log(
                    $"[OperationMapVehicleCandidateMigrationEditor] status=Created vehicles={migrated} " +
                    $"candidate={candidatePath} cleanupRequired=0 productionCutover=0");
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
                        $"[OperationMapVehicleCandidateMigrationEditor] scene-setup-restore-skipped: {exception.Message}");
                }
            }
        }

        private static Transform ResolveModelRoot(UnitGridAuthoring authoring)
        {
            if (authoring == null)
                return null;
            var serialized = new SerializedObject(authoring);
            Transform modelRoot = serialized.FindProperty("modelRoot").objectReferenceValue as Transform;
            return modelRoot != null ? modelRoot : authoring.transform.Find("Model");
        }

        private static void RequireAcceptedVisualParity(GameObject source, Transform candidateModel, int placementIndex)
        {
            float matrixResidual = MaxMatrixResidual(
                source.transform.localToWorldMatrix,
                candidateModel.localToWorldMatrix);
            bool sourceHasBounds = TryCombinedRendererBounds(source, out Bounds sourceBounds);
            bool candidateHasBounds = TryCombinedRendererBounds(candidateModel.gameObject, out Bounds candidateBounds);
            float boundsResidual = sourceHasBounds && candidateHasBounds
                ? Mathf.Max(
                    (sourceBounds.center - candidateBounds.center).magnitude,
                    (sourceBounds.extents - candidateBounds.extents).magnitude)
                : 0f;
            if (matrixResidual > OperationMapEntityPresentationTransformParityValidator.MatrixTolerance ||
                sourceHasBounds != candidateHasBounds ||
                boundsResidual > OperationMapEntityPresentationTransformParityValidator.BoundsTolerance)
            {
                throw new InvalidOperationException(
                    $"Candidate vehicle {placementIndex} differs from its accepted visual: " +
                    $"matrixResidual={matrixResidual:R} boundsResidual={boundsResidual:R} " +
                    $"sourceRenderers='{DescribeRendererBounds(source)}' " +
                    $"candidateRenderers='{DescribeRendererBounds(candidateModel.gameObject)}'.");
            }
        }

        private static string DescribeRendererBounds(GameObject owner) => string.Join(
            ";",
            owner.GetComponentsInChildren<Renderer>(true).Select(renderer =>
                $"{renderer.name}:{renderer.bounds.center}:{renderer.bounds.extents}"));

        private static void AlignModelToAcceptedVisual(
            Transform candidateModel,
            Transform acceptedVisual,
            int placementIndex)
        {
            Transform parent = candidateModel.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            if (Mathf.Abs(parentScale.x) < 0.000001f ||
                Mathf.Abs(parentScale.y) < 0.000001f ||
                Mathf.Abs(parentScale.z) < 0.000001f)
            {
                throw new InvalidOperationException(
                    $"Candidate vehicle {placementIndex} has a zero-scale gameplay parent.");
            }

            candidateModel.SetPositionAndRotation(acceptedVisual.position, acceptedVisual.rotation);
            Vector3 acceptedScale = acceptedVisual.lossyScale;
            candidateModel.localScale = new Vector3(
                acceptedScale.x / parentScale.x,
                acceptedScale.y / parentScale.y,
                acceptedScale.z / parentScale.z);
        }

        private static float MaxMatrixResidual(Matrix4x4 left, Matrix4x4 right)
        {
            float residual = 0f;
            for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                residual = Mathf.Max(residual, Mathf.Abs(left[row, column] - right[row, column]));
            return residual;
        }

        private static bool TryCombinedRendererBounds(GameObject owner, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in owner.GetComponentsInChildren<Renderer>(true))
            {
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        internal static OperationMapVehicleEcsConversionInventoryProbe.ConversionReport LoadInventory(string projectRoot)
        {
            string path = ResolveProjectPath(projectRoot, VehicleInventoryPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Accepted vehicle ECS conversion inventory is missing.", path);

            var report = JsonUtility.FromJson<OperationMapVehicleEcsConversionInventoryProbe.ConversionReport>(
                File.ReadAllText(path));
            if (report == null ||
                !string.Equals(report.result, "AllPlacementsAlreadyProduceEcs", StringComparison.Ordinal) ||
                report.counts == null ||
                report.placements == null ||
                report.counts.placementCount != report.placements.Count ||
                report.counts.cleanupRequiredCount != 0 ||
                report.counts.unresolvedJoinCount != 0)
            {
                throw new InvalidOperationException("Accepted vehicle inventory is incomplete or drifted.");
            }

            return report;
        }

        private static void RequireReplaceableVehiclePopulation(Transform vehicleRoot)
        {
            if (vehicleRoot.childCount != ExpectedVehicleCount)
            {
                throw new InvalidOperationException(
                    $"Candidate gameplay-vehicle population is partial: {vehicleRoot.childCount}.");
            }

            for (int i = 0; i < vehicleRoot.childCount; i++)
            {
                Transform candidateOwner = vehicleRoot.GetChild(i);
                OperationMapEntityPresentationIdentityAuthoring identity =
                    candidateOwner.GetComponentInChildren<OperationMapEntityPresentationIdentityAuthoring>(true);
                if (identity == null ||
                    identity.Role != OperationMapEntityPresentationRole.GameplayVehicles ||
                    identity.PlacementIndex != i)
                {
                    throw new InvalidOperationException(
                        $"Candidate gameplay-vehicle owner {i} is not safe to replace.");
                }
            }
        }

        private static void RemoveProhibitedCandidateComponents(GameObject owner)
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
