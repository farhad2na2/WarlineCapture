using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionCompositionSystemHelper
    {
        private static bool BeginPlacementForConfiguredSpawnableWithoutEntityManager(
            BuildingPlacementCommandRequestCompositionSystemHelper.Context context,
            GameObject prefab)
        {
            if (context.DefinitionSystem == null ||
                !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
            {
                return false;
            }

            context.SessionSystem?.BeginPlacement(context.SessionContext, definition);
            return true;
        }
    }
}
