using UnityEngine;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static partial class BuildingDefinitionAuthoringMetadataPrefabSystemHelper
    {
        public static bool TryGetUnitDefinitionMetadata(
            GameObject prefab,
            out BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata metadata)
        {
            metadata = default;
            if (prefab == null || !prefab.TryGetComponent(out UnitGridAuthoring authoring))
                return false;

            metadata = new BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata
            {
                DisplayName = authoring.ConfiguredDisplayName,
                Description = authoring.ConfiguredDescription,
                FootprintCells = authoring.GetConfiguredFootprintCells(),
                CanRequest = authoring.CanRequest,
                Price = authoring.MaterialsCost,
                CreditsCost = authoring.Price
            };
            return true;
        }
    }
}
