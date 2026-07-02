using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetCharacterPolicy
    {
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

            if (farEnoughForImpostor)
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
