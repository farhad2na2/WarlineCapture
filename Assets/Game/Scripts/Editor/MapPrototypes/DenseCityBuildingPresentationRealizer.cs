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
            DenseCityBuildingDefinitionLibrary definitionLibrary,
            DenseCityBuildingMaterialLibrary materialLibrary = null,
            int? placementIndexOverride = null)
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
            int placementIndex =
                placementIndexOverride ?? building.Identity.DeterministicSequence;
            if (placementIndex < 0)
            {
                throw new InvalidOperationException(
                    "Generated building placement index must be non-negative.");
            }

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
                owner = new GameObject($"Building_{placementIndex:D6}");
                owner.transform.SetParent(parent, false);
                DenseCityRenderOnlyPresentationRealizer.ApplyWorldMatrix(
                    owner.transform,
                    building.WorldMatrix);
                hierarchy.RequireIndependentRoot(
                    DenseCityPresentationCategory.GameplayBuildingIntact,
                    owner.transform,
                    building.Role);

                Transform intactRoot = InstantiateVisual(
                    intactPresentation,
                    owner.transform,
                    "IntactVisual",
                    materialLibrary,
                    building,
                    normalizeIntactVisual: true);
                Transform destroyedRoot = InstantiateVisual(
                    destroyedPresentation,
                    owner.transform,
                    "DestroyedVisual");
                BuildingDefinitionAuthoring definition = owner.AddComponent<BuildingDefinitionAuthoring>();
                definition.ConfigureForEditor(definitionConfig);
                OperationMapBuildingAuthoring authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
                var presentationIdentity =
                    owner.AddComponent<DenseCityPresentationIdentityAuthoring>();
                presentationIdentity.ConfigureForEditor(
                    building.Identity.CreateBakedStableId(),
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    Game.Components.DenseCityPresentationSemanticCategory.GameplayBuildingIntact);
                authoring.ConfigureGeneratedForEditor(
                    operationMapId,
                    building.Identity.CreateBakedStableId(),
                    placementIndex,
                    (byte)building.FactionId,
                    building.OriginCell,
                    building.FootprintCells,
                    Mathf.RoundToInt(building.MaximumHealth),
                    definition,
                    intactRoot.gameObject,
                    destroyedRoot.gameObject);
                if (!authoring.TryValidate(out string error))
                    throw new InvalidOperationException($"Generated building authoring is invalid: {error}");
                if (!presentationIdentity.TryValidate(out error))
                {
                    throw new InvalidOperationException(
                        $"Generated building presentation identity is invalid: {error}");
                }
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
            string name,
            DenseCityBuildingMaterialLibrary materialLibrary = null,
            DenseCityBuildingBakeRecord building = default,
            bool normalizeIntactVisual = false)
        {
            GameObject prefab = DenseCityRenderOnlyPresentationRealizer.LoadRequiredPrefab(
                presentation,
                out _);
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(prefab, owner);
            instance.name = name;
            DenseCityRenderOnlyPresentationRealizer.ApplyWorldMatrix(
                instance.transform,
                presentation.WorldMatrix);
            if (normalizeIntactVisual)
                DenseCityBuildingIntactVisualPolicy.NormalizeRealizedIntactVisual(instance);
            if (materialLibrary != null)
            {
                DenseCityBuildingMaterialSelection selection = materialLibrary.Select(
                    prefab,
                    building.WorldMatrix.GetColumn(3),
                    unchecked((uint)building.Identity.Seed),
                    building.Role);
                ApplyMaterialSelection(instance, materialLibrary, selection);
            }
            DenseCityRenderOnlyPresentationRealizer.RequireMaterialIdentity(instance, presentation);
            DenseCityRenderOnlyPresentationRealizer.RequireMatrixParity(
                instance.transform.localToWorldMatrix,
                presentation);
            return instance.transform;
        }

        private static void ApplyMaterialSelection(
            GameObject instance,
            DenseCityBuildingMaterialLibrary materialLibrary,
            DenseCityBuildingMaterialSelection selection)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material resolved = materialLibrary.Resolve(materials[materialIndex], selection);
                    if (resolved == materials[materialIndex])
                        continue;
                    materials[materialIndex] = resolved;
                    changed = true;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
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
