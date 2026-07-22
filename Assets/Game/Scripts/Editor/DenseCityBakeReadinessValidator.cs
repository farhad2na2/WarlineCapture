using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class DenseCityBakeReadinessValidator
    {
        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Dense City Bake Readiness")]
        public static void ValidateCurrentCandidate() => ValidateCurrentCandidateCore();

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidateCore();

        internal static bool TryResolveGenerationState(
            Scene operationMapScene,
            Scene entityPresentationScene,
            out bool generated,
            out string generationId,
            out string error)
        {
            generated = false;
            generationId = null;
            DenseCityGeneratedRootAuthoring[] mapRoots = FindGeneratedRoots(operationMapScene);
            DenseCityGeneratedRootAuthoring[] entityRoots = FindGeneratedRoots(entityPresentationScene);
            if (mapRoots.Length == 0 && entityRoots.Length == 0)
            {
                error = null;
                return true;
            }
            if (mapRoots.Length != 1 || entityRoots.Length != 1)
            {
                error =
                    $"Dense-city generation ownership is partial or duplicated: " +
                    $"mapRoots={mapRoots.Length} entityRoots={entityRoots.Length}.";
                return false;
            }
            if (mapRoots[0].Role != DenseCityGeneratedRootRole.MapBakeSource ||
                entityRoots[0].Role != DenseCityGeneratedRootRole.EntityPresentationSource)
            {
                error = "Dense-city generated roots have incorrect scene roles.";
                return false;
            }
            if (!string.Equals(mapRoots[0].GenerationId, entityRoots[0].GenerationId, StringComparison.Ordinal))
            {
                error = "Dense-city generated roots do not share one generation id.";
                return false;
            }

            generated = true;
            generationId = mapRoots[0].GenerationId;
            error = null;
            return true;
        }

        public static bool TryValidateAuthoringOwnership(
            Scene operationMapScene,
            Scene entityPresentationScene,
            string expectedOperationMapId,
            string expectedGenerationId,
            out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(expectedOperationMapId))
            {
                error = "Expected operation-map id is invalid.";
                return false;
            }
            if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                    operationMapScene,
                    entityPresentationScene,
                    expectedGenerationId,
                    out error))
            {
                return false;
            }

            DenseCityGeneratedRootAuthoring mapGeneratedRoot = FindGeneratedRoots(operationMapScene)
                .Single(root => root.Role == DenseCityGeneratedRootRole.MapBakeSource);
            DenseCityGeneratedRootAuthoring entityGeneratedRoot = FindGeneratedRoots(entityPresentationScene)
                .Single(root => root.Role == DenseCityGeneratedRootRole.EntityPresentationSource);
            if (!TryValidateGeneratedRendererOwnership(
                    mapGeneratedRoot,
                    entityGeneratedRoot,
                    out error))
            {
                return false;
            }
            if (!TryValidateProxyOwnership(mapGeneratedRoot, out error))
                return false;
            if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    mapGeneratedRoot.gameObject,
                    out error) ||
                !DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    entityGeneratedRoot.gameObject,
                    out error))
            {
                return false;
            }

            DenseCityAuthoredOverrideAuthoring[] mapOverrides = FindOverrides(operationMapScene);
            if (FindOverrides(entityPresentationScene).Length != 0)
            {
                error = "Dense-city authored overrides must remain in the operation-map authoring scene.";
                return false;
            }
            var overrideIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DenseCityAuthoredOverrideAuthoring authoredOverride in mapOverrides)
            {
                if (!authoredOverride.TryValidate(out error))
                    return false;
                if (authoredOverride.transform == mapGeneratedRoot.transform ||
                    authoredOverride.transform.IsChildOf(mapGeneratedRoot.transform))
                {
                    error = $"Authored override '{authoredOverride.StableId}' is inside the disposable generated root.";
                    return false;
                }
                if (!overrideIds.Add(authoredOverride.StableId))
                {
                    error = $"Duplicate dense-city authored override id: '{authoredOverride.StableId}'.";
                    return false;
                }
            }

            if (FindBuildings(operationMapScene).Length != 0)
            {
                error = "Operation-map building authoring must live in the entity-presentation scene.";
                return false;
            }
            OperationMapBuildingAuthoring[] buildings = FindBuildings(entityPresentationScene);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (OperationMapBuildingAuthoring building in buildings)
            {
                if (!building.TryValidate(out error))
                    return false;
                if (!string.Equals(building.OperationMapId, expectedOperationMapId, StringComparison.Ordinal))
                {
                    error = $"Building {building.PlacementIndex} belongs to a different operation map.";
                    return false;
                }

                OperationMapEntityPresentationRootAuthoring authoredOwner =
                    building.GetComponentInParent<OperationMapEntityPresentationRootAuthoring>(true);
                DenseCityGeneratedRootAuthoring generatedOwner =
                    building.GetComponentInParent<DenseCityGeneratedRootAuthoring>(true);
                bool hasAuthoredOwner = authoredOwner != null &&
                                        authoredOwner.Role == OperationMapEntityPresentationRole.GameplayBuildings;
                bool hasGeneratedOwner = generatedOwner == entityGeneratedRoot &&
                                         generatedOwner.Role == DenseCityGeneratedRootRole.EntityPresentationSource;
                if (hasAuthoredOwner == hasGeneratedOwner)
                {
                    error =
                        $"Building {building.PlacementIndex} must have exactly one approved entity-presentation owner.";
                    return false;
                }
                if (!stableIds.Add(building.StableId))
                {
                    error = $"Duplicate operation-map building stable id: '{building.StableId}'.";
                    return false;
                }
                string placementKey = $"{building.OperationMapId}:{building.PlacementIndex}";
                if (!placementIds.Add(placementKey))
                {
                    error = $"Duplicate operation-map building placement: '{placementKey}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateProxyOwnership(
            DenseCityGeneratedRootAuthoring mapGeneratedRoot,
            out string error)
        {
            MeshFilter[] proxies = mapGeneratedRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter proxy in proxies)
            {
                MapBakeGroupAuthoring owner = proxy.GetComponent<MapBakeGroupAuthoring>();
                if (owner == null ||
                    proxy.GetComponents<MapBakeGroupAuthoring>().Length != 1 ||
                    proxy.GetComponentInParent<MapBakeGroupAuthoring>(true) != owner)
                {
                    error =
                        $"Dense-city surface proxy requires exactly one nearest bake-group owner: " +
                        $"'{GetHierarchyPath(proxy.transform, mapGeneratedRoot.transform)}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateGeneratedRendererOwnership(
            DenseCityGeneratedRootAuthoring mapGeneratedRoot,
            DenseCityGeneratedRootAuthoring entityGeneratedRoot,
            out string error)
        {
            Renderer[] proxyRenderers = mapGeneratedRoot.GetComponentsInChildren<Renderer>(true);
            if (proxyRenderers.Length != 0)
            {
                error =
                    $"Dense-city proxy hierarchy contains detailed renderer " +
                    $"'{GetHierarchyPath(proxyRenderers[0].transform, mapGeneratedRoot.transform)}'.";
                return false;
            }

            Transform entityRoot = entityGeneratedRoot.transform;
            Transform[] renderOnlyCategories =
            {
                entityRoot.Find("RenderOnly/Infrastructure"),
                entityRoot.Find("RenderOnly/Vegetation"),
                entityRoot.Find("RenderOnly/Props"),
                entityRoot.Find("RenderOnly/Horizon")
            };
            Renderer[] presentationRenderers =
                entityGeneratedRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in presentationRenderers)
            {
                OperationMapBuildingAuthoring buildingOwner =
                    renderer.GetComponentInParent<OperationMapBuildingAuthoring>(true);
                bool hasBuildingOwner = buildingOwner != null &&
                                        buildingOwner.transform.IsChildOf(entityRoot);
                bool hasRenderOnlyOwner = renderOnlyCategories.Any(category =>
                    category != null &&
                    (renderer.transform == category || renderer.transform.IsChildOf(category)));
                if (hasBuildingOwner || hasRenderOnlyOwner)
                    continue;

                error =
                    $"Dense-city generated renderer is unclassified: " +
                    $"'{GetHierarchyPath(renderer.transform, entityRoot)}'.";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateCurrentCandidateCore()
        {
            string mapPath = OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath;
            string entityPath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            if (!File.Exists(Path.Combine(projectRoot, mapPath)) ||
                !File.Exists(Path.Combine(projectRoot, entityPath)))
            {
                throw new FileNotFoundException("Dense-city readiness source scene pair is incomplete.");
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene mapScene = EditorSceneManager.OpenScene(mapPath, OpenSceneMode.Single);
                Scene entityScene = EditorSceneManager.OpenScene(entityPath, OpenSceneMode.Additive);
                if (!TryResolveGenerationState(
                        mapScene,
                        entityScene,
                        out bool generated,
                        out string generationId,
                        out string stateError))
                {
                    throw new InvalidOperationException(stateError);
                }
                if (!generated)
                {
                    Debug.Log("[DenseCityBakeReadiness] result=NotGenerated");
                    return;
                }
                if (!TryValidateAuthoringOwnership(
                        mapScene,
                        entityScene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        generationId,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }

                Debug.Log($"[DenseCityBakeReadiness] result=Passed generationId={generationId}");
            }
            finally
            {
                if (previousSetup.Any(entry => entry.isLoaded && entry.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static DenseCityGeneratedRootAuthoring[] FindGeneratedRoots(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .ToArray();

        private static DenseCityAuthoredOverrideAuthoring[] FindOverrides(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityAuthoredOverrideAuthoring>(true))
                .ToArray();

        private static OperationMapBuildingAuthoring[] FindBuildings(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true))
                .ToArray();

        private static string GetHierarchyPath(Transform owner, Transform root)
        {
            string path = owner.name;
            Transform current = owner.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return root.name + "/" + path;
        }
    }
}
