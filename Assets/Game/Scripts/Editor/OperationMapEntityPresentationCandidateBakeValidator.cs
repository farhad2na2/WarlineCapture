#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary>
    /// In-memory bake of the protected candidate SubScene authoring for Phase 0A validation.
    /// Does not mutate accepted source, production Addressables, or presentation mode.
    /// </summary>
    internal static class OperationMapEntityPresentationCandidateBakeValidator
    {
        internal const int ExpectedGameplayBuildings = 432;
        internal const int ExpectedPresentationRoots = 3;
        internal const int ExpectedRenderOnlyOwners = 9090;
        internal const int ExpectedPresentationIdentities = 9544;

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Bake And Validate Candidate Entity Presentation")]
        public static void BakeAndValidateCandidateEntityPresentation()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            if (!File.Exists(candidatePhysicalPath))
                throw new FileNotFoundException("Protected candidate SubScene has not been created.", candidatePhysicalPath);

            string acceptedSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
            string acceptedSubSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            World bakeWorld = null;
            object blobStore = null;
            try
            {
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                RequireAuthoringCounts(candidateScene);

                bakeWorld = new World("OperationMapEntityPresentationCandidateBake");
                blobStore = CreateBlobAssetStore();
                if (!TryBakeScene(bakeWorld, candidateScene, candidatePath, blobStore, out string bakeError))
                    throw new InvalidOperationException($"Candidate bake failed: {bakeError}");

                OperationMapEntityPresentationCandidateBakeReport report = ValidateBakedWorld(bakeWorld.EntityManager);
                WriteReport(projectRoot, report);

                if (!report.Passed)
                    throw new InvalidOperationException($"Candidate bake validation failed: {report.RejectionReason}");

                RequireHashUnchanged(
                    acceptedSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
                RequireHashUnchanged(
                    acceptedSubSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

                Debug.Log(
                    $"[OperationMapEntityPresentationCandidateBakeValidator] status=Validated " +
                    $"gameplayBuildings={report.GameplayBuildingCount} " +
                    $"presentationRoots={report.PresentationRootCount} " +
                    $"presentationIdentities={report.PresentationIdentityCount} " +
                    $"renderMeshEntities={report.RenderMeshEntityCount} " +
                    $"buildingRenderChildren={report.BuildingRenderChildCount} " +
                    $"nonFiniteTransforms={report.NonFiniteTransformCount} " +
                    $"managedMapVisualCompanions={report.ManagedMapVisualCompanionCount} " +
                    $"productionCutover=0 report={report.ReportPath}");
            }
            finally
            {
                if (bakeWorld != null)
                    bakeWorld.Dispose();
                DisposeBlobAssetStore(blobStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
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

        private static void RequireAuthoringCounts(Scene candidateScene)
        {
            int buildings = 0;
            int roots = 0;
            int identities = 0;
            int renderOnlyOwners = 0;
            GameObject[] sceneRoots = candidateScene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                buildings += sceneRoots[i].GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length;
                roots += sceneRoots[i].GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true).Length;
                identities += sceneRoots[i]
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true).Length;
            }

            Transform renderOnly = RequirePath(candidateScene, "AuthoredOperationMapEntityPresentation/RenderOnly");
            renderOnlyOwners = renderOnly
                .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)
                .Count(identity => identity.Role == OperationMapEntityPresentationRole.RenderOnly);

            if (buildings != ExpectedGameplayBuildings)
                throw new InvalidOperationException($"Expected {ExpectedGameplayBuildings} building authorings, found {buildings}.");
            if (roots != ExpectedPresentationRoots)
                throw new InvalidOperationException($"Expected {ExpectedPresentationRoots} presentation roots, found {roots}.");
            if (renderOnlyOwners != ExpectedRenderOnlyOwners)
                throw new InvalidOperationException($"Expected {ExpectedRenderOnlyOwners} render-only owners, found {renderOnlyOwners}.");
            if (identities != ExpectedPresentationIdentities)
                throw new InvalidOperationException($"Expected {ExpectedPresentationIdentities} presentation identities, found {identities}.");
        }

        private static bool TryBakeScene(
            World world,
            Scene scene,
            string scenePath,
            object blobStore,
            out string rejectionReason)
        {
            rejectionReason = null;
            try
            {
                Type bakingUtilityType = Type.GetType("Unity.Entities.BakingUtility, Unity.Entities.Hybrid", true);
                Type bakingSettingsType = Type.GetType("Unity.Entities.BakingSettings, Unity.Entities.Hybrid", true);
                object settings = Activator.CreateInstance(bakingSettingsType);
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (string.IsNullOrEmpty(guid))
                {
                    rejectionReason = "candidate-guid-missing";
                    return false;
                }

                bakingSettingsType.GetField("SceneGUID")?.SetValue(settings, new Hash128(guid));
                object assignName = Enum.Parse(bakingUtilityType.GetNestedType("BakingFlags"), "AssignName");
                object addGuid = Enum.Parse(bakingUtilityType.GetNestedType("BakingFlags"), "AddEntityGUID");
                object flags = Enum.ToObject(
                    bakingUtilityType.GetNestedType("BakingFlags"),
                    Convert.ToUInt32(assignName) | Convert.ToUInt32(addGuid));
                bakingSettingsType.GetProperty("BakingFlags")?.SetValue(settings, flags);
                bakingSettingsType.GetProperty("BlobAssetStore")?.SetValue(settings, blobStore);

                MethodInfo bakeScene = bakingUtilityType.GetMethod(
                    "BakeScene",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (bakeScene == null)
                {
                    rejectionReason = "BakeScene-method-missing";
                    return false;
                }

                object result = bakeScene.Invoke(null, new object[] { world, scene, settings, false, null });
                if (result is bool ok && !ok)
                {
                    rejectionReason = "BakeScene-returned-false";
                    return false;
                }

                return true;
            }
            catch (TargetInvocationException exception)
            {
                rejectionReason = exception.InnerException?.Message ?? exception.Message;
                return false;
            }
            catch (Exception exception)
            {
                rejectionReason = exception.Message;
                return false;
            }
        }

        private static OperationMapEntityPresentationCandidateBakeReport ValidateBakedWorld(EntityManager entityManager)
        {
            var report = new OperationMapEntityPresentationCandidateBakeReport
            {
                result = "CandidateBakeValidationFailed",
                gameplayBuildingCount = entityManager.CreateEntityQuery(typeof(OperationMapBuildingIdentity)).CalculateEntityCount(),
                presentationRootCount = entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationRoot)).CalculateEntityCount(),
                presentationIdentityCount = entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationIdentity)).CalculateEntityCount(),
                buildingPresentationCount = entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation)).CalculateEntityCount(),
                renderMeshEntityCount = CountRenderMeshEntities(entityManager),
                buildingRenderChildCount = CountBuildingRenderChildren(entityManager),
                nonFiniteTransformCount = CountNonFiniteTransforms(entityManager),
                managedMapVisualCompanionCount = CountManagedMapVisualCompanions(entityManager),
                authoringBuildingCount = ExpectedGameplayBuildings,
                authoringRenderOnlyOwnerCount = ExpectedRenderOnlyOwners
            };

            if (report.gameplayBuildingCount != ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"gameplay-building-count:{report.gameplayBuildingCount}";
                return report;
            }

            if (report.presentationRootCount != ExpectedPresentationRoots)
            {
                report.rejectionReason = $"presentation-root-count:{report.presentationRootCount}";
                return report;
            }

            if (report.presentationIdentityCount != ExpectedPresentationIdentities)
            {
                report.rejectionReason = $"presentation-identity-count:{report.presentationIdentityCount}";
                return report;
            }

            if (report.buildingPresentationCount != ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"building-presentation-count:{report.buildingPresentationCount}";
                return report;
            }

            if (report.buildingRenderChildCount < ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"building-render-child-count:{report.buildingRenderChildCount}";
                return report;
            }

            if (report.nonFiniteTransformCount != 0)
            {
                report.rejectionReason = $"non-finite-transforms:{report.nonFiniteTransformCount}";
                return report;
            }

            if (report.managedMapVisualCompanionCount != 0)
            {
                report.rejectionReason = $"managed-map-visual-companions:{report.managedMapVisualCompanionCount}";
                return report;
            }

            // Render-only owners contribute at least one renderable entity each in aggregate with
            // building visual children; require a lower bound rather than exact equality.
            int minimumRenderMeshes = ExpectedRenderOnlyOwners;
            if (report.renderMeshEntityCount < minimumRenderMeshes)
            {
                report.rejectionReason = $"render-mesh-entities-below-owner-count:{report.renderMeshEntityCount}";
                return report;
            }

            report.result = "CandidateBakeValidationPassed";
            report.rejectionReason = null;
            return report;
        }

        private static int CountRenderMeshEntities(EntityManager entityManager)
        {
            Type materialMeshInfo = Type.GetType("Unity.Rendering.MaterialMeshInfo, Unity.Entities.Graphics");
            if (materialMeshInfo == null)
                return 0;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly(materialMeshInfo));
            return query.CalculateEntityCount();
        }

        private static int CountBuildingRenderChildren(EntityManager entityManager)
        {
            int count = 0;
            using NativeArray<OperationMapBuildingPresentation> presentations =
                entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation))
                    .ToComponentDataArray<OperationMapBuildingPresentation>(Allocator.Temp);
            for (int i = 0; i < presentations.Length; i++)
            {
                if (presentations[i].IntactVisualRoot != Entity.Null)
                    count++;
                if (presentations[i].DestroyedVisualRoot != Entity.Null)
                    count++;
            }

            return count;
        }

        private static int CountNonFiniteTransforms(EntityManager entityManager)
        {
            int count = 0;
            using NativeArray<LocalTransform> transforms =
                entityManager.CreateEntityQuery(typeof(LocalTransform))
                    .ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < transforms.Length; i++)
            {
                float3 p = transforms[i].Position;
                float s = transforms[i].Scale;
                if (!math.isfinite(p.x) || !math.isfinite(p.y) || !math.isfinite(p.z) || !math.isfinite(s))
                    count++;
            }

            return count;
        }

        private static int CountManagedMapVisualCompanions(EntityManager entityManager)
        {
            // Building gameplay entities must not retain CompanionLink-managed visuals.
            Type companion = Type.GetType("Unity.Entities.CompanionLink, Unity.Entities.Hybrid") ??
                             Type.GetType("Unity.Entities.Hybrid.CompanionLink, Unity.Entities.Hybrid");
            if (companion == null)
                return 0;

            ComponentType companionType = ComponentType.ReadOnly(companion);
            int count = 0;
            using NativeArray<Entity> buildings =
                entityManager.CreateEntityQuery(typeof(OperationMapBuildingIdentity))
                    .ToEntityArray(Allocator.Temp);
            for (int i = 0; i < buildings.Length; i++)
            {
                if (entityManager.HasComponent(buildings[i], companionType))
                    count++;
            }

            return count;
        }

        private static void WriteReport(string projectRoot, OperationMapEntityPresentationCandidateBakeReport report)
        {
            string reportPath = Path.Combine(
                projectRoot,
                "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json");
            report.reportPath = reportPath.Replace('\\', '/');
            report.GameplayBuildingCount = report.gameplayBuildingCount;
            report.PresentationRootCount = report.presentationRootCount;
            report.PresentationIdentityCount = report.presentationIdentityCount;
            report.BuildingPresentationCount = report.buildingPresentationCount;
            report.RenderMeshEntityCount = report.renderMeshEntityCount;
            report.BuildingRenderChildCount = report.buildingRenderChildCount;
            report.NonFiniteTransformCount = report.nonFiniteTransformCount;
            report.ManagedMapVisualCompanionCount = report.managedMapVisualCompanionCount;
            report.Passed = string.Equals(report.result, "CandidateBakeValidationPassed", StringComparison.Ordinal);
            report.RejectionReason = report.rejectionReason;
            report.ReportPath = report.reportPath;

            string json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
            File.WriteAllText(reportPath, json, Utf8WithoutBom);
        }

        private static object CreateBlobAssetStore()
        {
            Type storeType =
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities") ??
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities.Hybrid");
            if (storeType == null)
                throw new InvalidOperationException("BlobAssetStore type was not found.");
            ConstructorInfo ctor = storeType.GetConstructor(new[] { typeof(int) });
            return ctor != null ? ctor.Invoke(new object[] { 128 }) : Activator.CreateInstance(storeType);
        }

        private static void DisposeBlobAssetStore(object store)
        {
            if (store is IDisposable disposable)
                disposable.Dispose();
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

        [Serializable]
        private sealed class OperationMapEntityPresentationCandidateBakeReport
        {
            public string result;
            public string rejectionReason;
            public string reportPath;
            public int gameplayBuildingCount;
            public int presentationRootCount;
            public int presentationIdentityCount;
            public int buildingPresentationCount;
            public int renderMeshEntityCount;
            public int buildingRenderChildCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
            public int authoringBuildingCount;
            public int authoringRenderOnlyOwnerCount;

            // Convenience properties used by caller logging (not serialized by JsonUtility field rules).
            [NonSerialized] public bool Passed;
            [NonSerialized] public string RejectionReason;
            [NonSerialized] public string ReportPath;
            [NonSerialized] public int GameplayBuildingCount;
            [NonSerialized] public int PresentationRootCount;
            [NonSerialized] public int PresentationIdentityCount;
            [NonSerialized] public int BuildingPresentationCount;
            [NonSerialized] public int RenderMeshEntityCount;
            [NonSerialized] public int BuildingRenderChildCount;
            [NonSerialized] public int NonFiniteTransformCount;
            [NonSerialized] public int ManagedMapVisualCompanionCount;
        }
    }
}

#endif
