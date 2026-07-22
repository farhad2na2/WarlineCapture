using System;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityRealizedBuildingPresentation
    {
        internal DenseCityRealizedBuildingPresentation(
            OperationMapBuildingAuthoring authoring,
            Transform intactVisualRoot,
            Transform destroyedVisualRoot)
        {
            Authoring = authoring != null ? authoring : throw new ArgumentNullException(nameof(authoring));
            IntactVisualRoot = intactVisualRoot != null
                ? intactVisualRoot
                : throw new ArgumentNullException(nameof(intactVisualRoot));
            DestroyedVisualRoot = destroyedVisualRoot != null
                ? destroyedVisualRoot
                : throw new ArgumentNullException(nameof(destroyedVisualRoot));
        }

        internal OperationMapBuildingAuthoring Authoring { get; }
        internal Transform IntactVisualRoot { get; }
        internal Transform DestroyedVisualRoot { get; }
    }

    internal static class DenseCityBuildingPresentationRealizer
    {
        internal static DenseCityRealizedBuildingPresentation Realize(
            string operationMapId,
            DenseCityBuildingBakeRecord building,
            DenseCityPresentationBakeRecord intactPresentation,
            DenseCityPresentationBakeRecord destroyedPresentation,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityBuildingDefinitionLibrary definitionLibrary)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
                throw new ArgumentException("A valid operation-map id is required.", nameof(operationMapId));
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (definitionLibrary == null)
                throw new ArgumentNullException(nameof(definitionLibrary));
            RequireLinkedPresentations(building, intactPresentation, destroyedPresentation);
            if (building.FactionId > byte.MaxValue)
                throw new InvalidOperationException("Generated building faction exceeds byte storage.");

            BuildingDefinitionAuthoringConfig definitionConfig =
                definitionLibrary.ResolveAsset(building.Role);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    definitionConfig,
                    out string definitionGuid,
                    out long definitionLocalId) ||
                definitionLocalId <= 0 ||
                !string.Equals(
                    definitionGuid,
                    building.DefinitionConfigAssetGuid,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Dense-city building definition identity is unavailable or mismatched: " +
                    $"'{building.Identity.StableKey}'.");
            }

            Transform parent = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                building.Role);
            GameObject owner = null;
            try
            {
                owner = new GameObject($"Building_{building.Identity.DeterministicSequence:D6}");
                owner.transform.SetParent(parent, false);
                DenseCityRenderOnlyPresentationRealizer.ApplyWorldMatrix(
                    owner.transform,
                    building.WorldMatrix);
                hierarchy.RequireIndependentRoot(
                    DenseCityPresentationCategory.GameplayBuildingIntact,
                    owner.transform,
                    building.Role);

                Transform intactRoot = InstantiateVisual(intactPresentation, owner.transform, "IntactVisual");
                Transform destroyedRoot = InstantiateVisual(
                    destroyedPresentation,
                    owner.transform,
                    "DestroyedVisual");
                BuildingDefinitionAuthoring definition = owner.AddComponent<BuildingDefinitionAuthoring>();
                definition.ConfigureForEditor(definitionConfig);
                OperationMapBuildingAuthoring authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
                authoring.ConfigureGeneratedForEditor(
                    operationMapId,
                    building.Identity.CreateBakedStableId(),
                    building.Identity.DeterministicSequence,
                    (byte)building.FactionId,
                    building.OriginCell,
                    building.FootprintCells,
                    Mathf.RoundToInt(building.MaximumHealth),
                    definition,
                    intactRoot.gameObject,
                    destroyedRoot.gameObject);
                if (!authoring.TryValidate(out string error))
                    throw new InvalidOperationException($"Generated building authoring is invalid: {error}");
                DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(
                    owner.transform.localToWorldMatrix,
                    intactPresentation);
                return new DenseCityRealizedBuildingPresentation(authoring, intactRoot, destroyedRoot);
            }
            catch
            {
                if (owner != null)
                    UnityEngine.Object.DestroyImmediate(owner);
                throw;
            }
        }

        private static Transform InstantiateVisual(
            DenseCityPresentationBakeRecord presentation,
            Transform owner,
            string name)
        {
            GameObject prefab = DenseCityRenderOnlyPresentationRealizer.LoadRequiredPrefab(
                presentation,
                out string prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, owner);
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate dense-city prefab '{prefabPath}'.");
            instance.name = name;
            DenseCityRenderOnlyPresentationRealizer.ApplyWorldMatrix(
                instance.transform,
                presentation.WorldMatrix);
            DenseCityRenderOnlyPresentationRealizer.RequireMaterialIdentity(instance, presentation);
            DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(
                instance.transform.localToWorldMatrix,
                presentation);
            return instance.transform;
        }

        private static void RequireLinkedPresentations(
            DenseCityBuildingBakeRecord building,
            DenseCityPresentationBakeRecord intact,
            DenseCityPresentationBakeRecord destroyed)
        {
            if (intact.Category != DenseCityPresentationCategory.GameplayBuildingIntact ||
                destroyed.Category != DenseCityPresentationCategory.GameplayBuildingDestroyed ||
                intact.Identity.StableKey != building.IntactPresentationIdentity.StableKey ||
                destroyed.Identity.StableKey != building.DestroyedPresentationIdentity.StableKey)
            {
                throw new InvalidOperationException(
                    $"Dense-city building presentation group is incomplete or mismatched: " +
                    $"'{building.Identity.StableKey}'.");
            }
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(building.WorldMatrix[index] - intact.WorldMatrix[index]) <= 0.0001f &&
                    Mathf.Abs(building.WorldMatrix[index] - destroyed.WorldMatrix[index]) <= 0.0001f)
                {
                    continue;
                }
                throw new InvalidOperationException(
                    $"Dense-city building visual transforms differ from their owner record: " +
                    $"'{building.Identity.StableKey}'.");
            }
        }
    }
}
