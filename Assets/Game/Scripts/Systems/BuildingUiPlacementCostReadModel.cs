using UnityEngine;

namespace Game.Runtime
{
    public static class BuildingUiPlacementCostReadModel
    {
        public static int ActiveCredits(in BuildingUiCommandSystemHelper.Context context) =>
            Mathf.Max(0, context.GetActivePlacementCreditsCost?.Invoke() ?? 0);
    }
}
