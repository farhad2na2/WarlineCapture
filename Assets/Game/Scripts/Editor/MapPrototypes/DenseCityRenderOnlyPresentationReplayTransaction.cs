using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal static class DenseCityRenderOnlyPresentationReplayTransaction
    {
        internal static IReadOnlyList<Transform> Realize(
            IDenseCityGenerationRecordSource records,
            DenseCityPresentationHierarchyContext hierarchy)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));

            IReadOnlyList<DenseCityPresentationBakeRecord> presentations = records.Presentations;
            var realized = new List<Transform>(presentations.Count);
            string previousStableKey = null;
            try
            {
                for (int index = 0; index < presentations.Count; index++)
                {
                    DenseCityPresentationBakeRecord presentation = presentations[index];
                    if (presentation.Category is DenseCityPresentationCategory.GameplayBuildingIntact or
                        DenseCityPresentationCategory.GameplayBuildingDestroyed or
                        DenseCityPresentationCategory.BuildingAttachmentIntact or
                        DenseCityPresentationCategory.BuildingAttachmentDestroyed)
                    {
                        continue;
                    }

                    DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(
                        presentation.Category);
                    string stableKey = presentation.Identity.StableKey;
                    if (previousStableKey != null &&
                        string.CompareOrdinal(previousStableKey, stableKey) >= 0)
                    {
                        throw new InvalidOperationException(
                            "Dense-city render-only presentation records are not in strict stable identity order.");
                    }

                    Transform root = DenseCityRenderOnlyPresentationRealizer.Realize(
                        presentation,
                        hierarchy);
                    realized.Add(root);
                    previousStableKey = stableKey;
                }

                return realized;
            }
            catch
            {
                for (int index = realized.Count - 1; index >= 0; index--)
                {
                    Transform root = realized[index];
                    if (root != null)
                        UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
                throw;
            }
        }
    }
}
