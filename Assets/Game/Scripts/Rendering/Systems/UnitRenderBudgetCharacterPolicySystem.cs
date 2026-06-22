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

        bool canUseSafeMid = hasSafeMid && (!movingVisibleCharacter || midRootAnimatable);
        bool canUseSafeLow = hasSafeLow && (!movingVisibleCharacter || lowRootAnimatable);

        if (movingVisibleCharacter)
        {
            if (lowEnoughForSafeLow && canUseSafeLow)
                return UnitRenderVisualKind.Low;
            if (canUseSafeMid)
                return UnitRenderVisualKind.Mid;

            return UnitRenderVisualKind.Detail;
        }

        if (farEnoughForImpostor)
            return UnitRenderVisualKind.Far;
        if (lowEnoughForSafeLow && canUseSafeLow)
            return UnitRenderVisualKind.Low;
        if (canUseSafeMid)
            return UnitRenderVisualKind.Mid;

        return UnitRenderVisualKind.Detail;
    }

    public bool ShouldForceCharacterDetailVisual(bool isCharacter)
    {
        return false;
    }
}
