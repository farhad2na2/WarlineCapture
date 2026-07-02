using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Rendering
{
    using UnitDistance = UnitRenderBudgetDistance.UnitDistance;

    public readonly struct UnitRenderBudgetFreezeDiagnostic
    {
        private static readonly bool EnableRenderBudgetFreezeLogs = false;
        private const double FreezeLogThresholdSeconds = 0.05d;

        public void LogFreezeIfNeeded(
            EntityManager em,
            double elapsed,
            NativeList<UnitDistance> distances,
            int detailedCount,
            bool cameraMotionActive,
            UnitRenderBudgetDiagnosticState.FrameCounters counters,
            float visibleCharacterLowDistanceSq,
            float visibleCharacterImpostorNearDistance,
            float visibleCharacterImpostorFarDistance,
            ref UnitRenderBudgetDiagnosticLog diagnosticLogSystem)
        {
            if (!EnableRenderBudgetFreezeLogs || elapsed < FreezeLogThresholdSeconds)
                return;

            diagnosticLogSystem.EnqueueLog(
                em,
                $"[FreezeDetect:ECS] UnitRenderBudgetSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms units={distances.Length} detailed={detailedCount} mid={counters.MidShown} low={counters.LowShown} far={counters.FarCount} cameraMotion={(cameraMotionActive ? 1 : 0)} visibleCharacterSafeGate={counters.VisibleCharacterSafeGate} visibleCharacterMidInstances={counters.VisibleCharacterMidInstances} visibleCharacterSafeMidInstances={counters.VisibleCharacterSafeMidInstances} visibleCharacterLowInstances={counters.VisibleCharacterLowInstances} visibleCharacterSafeLowInstances={counters.VisibleCharacterSafeLowInstances} visibleCharacterUsingSafeMid={counters.VisibleCharacterUsingSafeMid} visibleCharacterUsingSafeLow={counters.VisibleCharacterUsingSafeLow} visibleCharacterUsingFarImpostor={counters.VisibleCharacterUsingFarImpostor} visibleCharacterBudgetDetail={counters.VisibleCharacterBudgetDetail} visibleCharacterSafetyDetail={counters.VisibleCharacterSafetyDetail} visibleCharacterMidSuppressed={counters.VisibleCharacterMidSuppressed} visibleNearDetail={counters.VisibleNearDetail} visibleNearMid={counters.VisibleNearMid} visibleCharacterLowDistance={math.sqrt(visibleCharacterLowDistanceSq):F0} visibleCharacterImpostorBand={visibleCharacterImpostorNearDistance:F0}-{visibleCharacterImpostorFarDistance:F0} visibleCharacterForcedDetailByUnsafeMid={counters.VisibleCharacterForcedDetailByUnsafeMid} missingMid={counters.MissingMidInstance} missingLow={counters.MissingLowInstance} visualStateChanges={counters.VisualStateChanges} visualPending={counters.VisualStatePending} visualCommitted={counters.VisualTransitionsCommitted} visibleMidSafetyPatched={counters.VisibleMidSafetyPatched} changed={counters.Changed} shown={counters.Shown} hidden={counters.Hidden}");
        }
    }
}
