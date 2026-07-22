using System;
using System.Collections.Generic;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class DenseCityBakeReadinessValidator
    {
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
    }
}
