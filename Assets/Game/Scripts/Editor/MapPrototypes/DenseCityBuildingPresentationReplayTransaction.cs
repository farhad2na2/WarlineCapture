using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityBuildingPresentationReplayTransaction
    {
        internal static IReadOnlyList<DenseCityRealizedBuildingPresentation> Realize(
            string operationMapId,
            IDenseCityGenerationRecordSource records,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityBuildingDefinitionLibrary definitionLibrary)
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
                    DenseCityRealizedBuildingPresentation owner =
                        DenseCityBuildingPresentationRealizer.Realize(
                            operationMapId,
                            building,
                            intact,
                            destroyed,
                            hierarchy,
                            definitionLibrary);
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
