using Game.Components;
using Unity.Mathematics;

namespace Game.Rendering
{
    // This is a pure, Burst-compatible decision boundary. The runtime caller owns
    // envelope construction and state mutation; this contract only says whether it
    // must schedule a rebuild for the supplied immutable inputs.
    public struct OperationMapRenderCellEnvelope
    {
        public int2 Min;
        public int2 Max;
    }

    public struct OperationMapRenderGuardEnvelopeInput
    {
        public byte InitialViewApplied;
        public byte ForceRebuild;
        public byte CameraDiscontinuity;
        public byte MapGenerationChanged;
        public int DirtyStateChangeCount;
        public OperationMapRenderCellEnvelope RequiredEnvelope;
        public OperationMapRenderCellEnvelope GuardEnvelope;
    }

    public static class OperationMapRenderGuardEnvelopeDecision
    {
        public static bool TryDecide(
            in OperationMapRenderGuardEnvelopeInput input,
            out OperationMapRenderRebuildReason reason)
        {
            reason = OperationMapRenderRebuildReason.None;
            if (!IsValid(input.RequiredEnvelope) || !IsValid(input.GuardEnvelope))
                return false;

            if (input.InitialViewApplied == 0)
            {
                reason = OperationMapRenderRebuildReason.InitialView;
                return true;
            }

            if (input.MapGenerationChanged != 0)
            {
                reason = OperationMapRenderRebuildReason.MapGenerationChanged;
                return true;
            }

            if (input.ForceRebuild != 0 || input.CameraDiscontinuity != 0 ||
                !Contains(input.GuardEnvelope, input.RequiredEnvelope))
            {
                reason = OperationMapRenderRebuildReason.CameraEnvelopeChanged;
                return true;
            }

            if (input.DirtyStateChangeCount > 0)
            {
                reason = OperationMapRenderRebuildReason.VisualStateChanged;
                return true;
            }

            return true;
        }

        public static bool Contains(
            in OperationMapRenderCellEnvelope outer,
            in OperationMapRenderCellEnvelope inner)
        {
            return outer.Min.x <= inner.Min.x &&
                   outer.Min.y <= inner.Min.y &&
                   outer.Max.x >= inner.Max.x &&
                   outer.Max.y >= inner.Max.y;
        }

        private static bool IsValid(in OperationMapRenderCellEnvelope envelope)
        {
            return envelope.Min.x <= envelope.Max.x &&
                   envelope.Min.y <= envelope.Max.y;
        }
    }
}
