using System;
using System.Collections.Generic;
using Game.Authoring;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityBuildingPresentationReplayTransaction
    {
        internal static IReadOnlyList<DenseCityRealizedBuildingPresentation> Realize(
            string operationMapId,
            IDenseCityGenerationRecordSource records,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityBuildingDefinitionLibrary definitionLibrary,
            DenseCityBuildingMaterialLibrary materialLibrary = null)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (definitionLibrary == null)
                throw new ArgumentNullException(nameof(definitionLibrary));

            IReadOnlyList<DenseCityBuildingBakeRecord> buildings = records.Buildings;
            IReadOnlyList<DenseCityPresentationBakeRecord> presentations = records.Presentations;
            var presentationByStableKey = new Dictionary<string, DenseCityPresentationBakeRecord>(
                presentations.Count,
                StringComparer.Ordinal);
            var attachmentsByOwner = new Dictionary<string, List<DenseCityPresentationBakeRecord>>(
                buildings.Count,
                StringComparer.Ordinal);
            IndexPresentations(presentations, presentationByStableKey, attachmentsByOwner);

            var realized = new List<DenseCityRealizedBuildingPresentation>(buildings.Count);
            int placementIndexBase = ResolvePlacementIndexBase(operationMapId, hierarchy);
            string previousBuildingStableKey = null;
            try
            {
                for (int index = 0; index < buildings.Count; index++)
                {
                    DenseCityBuildingBakeRecord building = buildings[index];
                    string buildingStableKey = building.Identity.StableKey;
                    if (previousBuildingStableKey != null &&
                        string.CompareOrdinal(previousBuildingStableKey, buildingStableKey) >= 0)
                    {
                        throw new InvalidOperationException(
                            "Dense-city building records are not in strict stable identity order.");
                    }
                    DenseCityPresentationBakeRecord intact = RequirePresentation(
                        presentationByStableKey,
                        building.IntactPresentationIdentity.StableKey);
                    DenseCityPresentationBakeRecord destroyed = RequirePresentation(
                        presentationByStableKey,
                        building.DestroyedPresentationIdentity.StableKey);
                    if (index > int.MaxValue - placementIndexBase)
                    {
                        throw new InvalidOperationException(
                            "Generated building placement-index capacity is exhausted.");
                    }
                    DenseCityRealizedBuildingPresentation owner =
                        DenseCityBuildingPresentationRealizer.Realize(
                            operationMapId,
                            building,
                            intact,
                            destroyed,
                            hierarchy,
                            definitionLibrary,
                            materialLibrary,
                            placementIndexBase + index);
                    realized.Add(owner);
                    if (attachmentsByOwner.TryGetValue(buildingStableKey, out var attachments))
                        RealizeAttachments(attachments, owner, hierarchy);
                    previousBuildingStableKey = buildingStableKey;
                }
                return realized;
            }
            catch
            {
                for (int index = realized.Count - 1; index >= 0; index--)
                {
                    DenseCityRealizedBuildingPresentation owner = realized[index];
                    if (owner.Authoring != null)
                        UnityEngine.Object.DestroyImmediate(owner.Authoring.gameObject);
                }
                throw;
            }
        }

        private static int ResolvePlacementIndexBase(
            string operationMapId,
            DenseCityPresentationHierarchyContext hierarchy)
        {
            Scene scene = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                Game.Configs.GeneratedCityBuildingRole.Other).gameObject.scene;
            int maximumPlacementIndex = -1;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                OperationMapBuildingAuthoring[] existing =
                    root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true);
                for (int index = 0; index < existing.Length; index++)
                {
                    OperationMapBuildingAuthoring building = existing[index];
                    if (!string.Equals(building.OperationMapId, operationMapId, StringComparison.Ordinal))
                        continue;
                    maximumPlacementIndex = Math.Max(
                        maximumPlacementIndex,
                        building.PlacementIndex);
                }
            }
            if (maximumPlacementIndex == int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Generated building placement-index capacity is exhausted.");
            }
            return maximumPlacementIndex + 1;
        }

        private static void IndexPresentations(
            IReadOnlyList<DenseCityPresentationBakeRecord> presentations,
            Dictionary<string, DenseCityPresentationBakeRecord> presentationByStableKey,
            Dictionary<string, List<DenseCityPresentationBakeRecord>> attachmentsByOwner)
        {
            string previousStableKey = null;
            for (int index = 0; index < presentations.Count; index++)
            {
                DenseCityPresentationBakeRecord presentation = presentations[index];
                string stableKey = presentation.Identity.StableKey;
                if (previousStableKey != null && string.CompareOrdinal(previousStableKey, stableKey) >= 0)
                {
                    throw new InvalidOperationException(
                        "Dense-city presentation records are not in strict stable identity order.");
                }
                if (!presentationByStableKey.TryAdd(stableKey, presentation))
                    throw new InvalidOperationException($"Duplicate dense-city presentation: '{stableKey}'.");
                if (presentation.Category is DenseCityPresentationCategory.BuildingAttachmentIntact or
                    DenseCityPresentationCategory.BuildingAttachmentDestroyed)
                {
                    if (!attachmentsByOwner.TryGetValue(
                            presentation.BuildingOwnerStableKey,
                            out List<DenseCityPresentationBakeRecord> ownerAttachments))
                    {
                        ownerAttachments = new List<DenseCityPresentationBakeRecord>();
                        attachmentsByOwner.Add(presentation.BuildingOwnerStableKey, ownerAttachments);
                    }
                    ownerAttachments.Add(presentation);
                }
                previousStableKey = stableKey;
            }
        }

        private static DenseCityPresentationBakeRecord RequirePresentation(
            IReadOnlyDictionary<string, DenseCityPresentationBakeRecord> presentations,
            string stableKey)
        {
            if (!presentations.TryGetValue(stableKey, out DenseCityPresentationBakeRecord presentation))
                throw new InvalidOperationException($"Dense-city building presentation is missing: '{stableKey}'.");
            return presentation;
        }

        private static void RealizeAttachments(
            IReadOnlyList<DenseCityPresentationBakeRecord> attachments,
            DenseCityRealizedBuildingPresentation owner,
            DenseCityPresentationHierarchyContext hierarchy)
        {
            for (int index = 0; index < attachments.Count; index++)
            {
                DenseCityPresentationBakeRecord attachment = attachments[index];
                Transform stateRoot = attachment.Category ==
                                      DenseCityPresentationCategory.BuildingAttachmentIntact
                    ? owner.IntactVisualRoot
                    : owner.DestroyedVisualRoot;
                DenseCityRenderOnlyPresentationRealizer.RealizeAttachment(
                    attachment,
                    stateRoot,
                    hierarchy);
            }
        }
    }
}
