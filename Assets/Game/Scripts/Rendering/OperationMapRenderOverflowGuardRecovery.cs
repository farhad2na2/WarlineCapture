using Game.Components;

namespace Game.Rendering
{
    /// <summary>One replacement for a strictly smaller view of an overflowed guard.</summary>
    public static class OperationMapRenderOverflowGuardRecovery
    {
        public static bool ShouldReplace(int publishedOverflowCount,
            in OperationMapRenderCellEnvelope previousGuard,
            in OperationMapRenderCellEnvelope currentMaterialized)
        {
            if (publishedOverflowCount <= 0 ||
                previousGuard.Min.x > previousGuard.Max.x || previousGuard.Min.y > previousGuard.Max.y ||
                currentMaterialized.Min.x > currentMaterialized.Max.x || currentMaterialized.Min.y > currentMaterialized.Max.y ||
                !OperationMapRenderGuardEnvelopeDecision.Contains(previousGuard, currentMaterialized))
                return false;
            return !previousGuard.Min.Equals(currentMaterialized.Min) || !previousGuard.Max.Equals(currentMaterialized.Max);
        }
    }
}
