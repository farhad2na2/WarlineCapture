using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetCharacterPolicy
    {
        private const bool EnableFarImpostorVisuals = true;

        public UnitRenderVisualKind ResolveVisibleCharacterVisualKind(
            bool movingVisibleCharacter,
            bool forceDetailNearVisible,
            bool forceDetailByBudget,
            bool farEnoughForImpostor,
            bool lowEnoughForSafeLow,
            bool hasSafeMid,
            bool midRootAnimatable,
            bool hasSafeLow,
            bool lowRootAnimatable)
        {
            if (forceDetailNearVisible || forceDetailByBudget)
                return UnitRenderVisualKind.Detail;

            if (movingVisibleCharacter)
            {
                if (hasSafeMid && midRootAnimatable)
                    return UnitRenderVisualKind.Mid;
                if (hasSafeLow && lowRootAnimatable)
                    return UnitRenderVisualKind.Low;

                return UnitRenderVisualKind.Detail;
            }

            if (farEnoughForImpostor && EnableFarImpostorVisuals)
                return UnitRenderVisualKind.Far;
            if (lowEnoughForSafeLow && hasSafeLow)
                return UnitRenderVisualKind.Low;
            if (hasSafeMid)
                return UnitRenderVisualKind.Mid;

            return UnitRenderVisualKind.Detail;
        }

        public bool ShouldForceCharacterDetailVisual(bool isCharacter)
        {
            return false;
        }
    }
}
