public readonly struct UnitRenderBudgetCharacterPolicySystem
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
        return UnitRenderVisualKind.Detail;
    }

    public bool ShouldForceCharacterDetailVisual(bool isCharacter)
    {
        return isCharacter;
    }
}
