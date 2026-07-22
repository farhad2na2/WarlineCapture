using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityRealizedPresentationSet
    {
        internal DenseCityRealizedPresentationSet(
            IReadOnlyList<DenseCityRealizedBuildingPresentation> buildings,
            IReadOnlyList<Transform> renderOnly)
        {
            Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            RenderOnly = renderOnly ?? throw new ArgumentNullException(nameof(renderOnly));
        }

        internal IReadOnlyList<DenseCityRealizedBuildingPresentation> Buildings { get; }
        internal IReadOnlyList<Transform> RenderOnly { get; }
    }

    internal static class DenseCityPresentationReplayTransaction
    {
        internal static DenseCityRealizedPresentationSet Realize(
            string operationMapId,
            DenseCityGenerationRecordSet records,
            DenseCityPresentationHierarchyContext hierarchy,
            DenseCityBuildingDefinitionLibrary definitionLibrary)
        {
            IReadOnlyList<DenseCityRealizedBuildingPresentation> buildings = null;
            try
            {
                buildings = DenseCityBuildingPresentationReplayTransaction.Realize(
                    operationMapId,
                    records,
                    hierarchy,
                    definitionLibrary);
                IReadOnlyList<Transform> renderOnly =
                    DenseCityRenderOnlyPresentationReplayTransaction.Realize(records, hierarchy);
                return new DenseCityRealizedPresentationSet(buildings, renderOnly);
            }
            catch
            {
                DestroyBuildings(buildings);
                throw;
            }
        }

        private static void DestroyBuildings(
            IReadOnlyList<DenseCityRealizedBuildingPresentation> buildings)
        {
            if (buildings == null)
                return;

            for (int index = buildings.Count - 1; index >= 0; index--)
            {
                DenseCityRealizedBuildingPresentation building = buildings[index];
                if (building.Authoring != null)
                    UnityEngine.Object.DestroyImmediate(building.Authoring.gameObject);
            }
        }
    }
}
