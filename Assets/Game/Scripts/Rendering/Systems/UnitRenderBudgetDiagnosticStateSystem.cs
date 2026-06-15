public struct UnitRenderBudgetDiagnosticState
{
    private static readonly bool EnableRenderBudgetDiagnostics = false;
    private const int DiagnosticIntervalFrames = 120;
    private const int DiagnosticSampleMaxLength = 900;

    private int _nextDiagnosticFrame;

    public struct FrameCounters
    {
        public int Changed;
        public int Hidden;
        public int Shown;
        public int MidShown;
        public int LowShown;
        public int FarCount;
        public int MissingMidInstance;
        public int MissingLowInstance;
        public int VisualStateChanges;
        public int VisualStatePending;
        public int VisualTransitionsCommitted;
        public int VisibleCharacterSafeGate;
        public int VisibleCharacterMidInstances;
        public int VisibleCharacterSafeMidInstances;
        public int VisibleCharacterLowInstances;
        public int VisibleCharacterSafeLowInstances;
        public int VisibleCharacterUsingSafeMid;
        public int VisibleCharacterUsingSafeLow;
        public int VisibleCharacterUsingFarImpostor;
        public int VisibleCharacterMidSuppressed;
        public int VisibleCharacterForcedDetailByUnsafeMid;
        public int VisibleCharacterBudgetDetail;
        public int VisibleCharacterSafetyDetail;
        public int VisibleMidSafetyPatched;
        public int VisibleNearDetail;
        public int VisibleNearMid;
    }

    public FrameCounters CreateFrameCounters(
        UnitRenderBudgetDecision.Result decisionResult,
        UnitRenderBudgetVisibilityApply.Result applyResult)
    {
        return new FrameCounters
        {
            Changed = decisionResult.Changed,
            Hidden = applyResult.Hidden,
            Shown = applyResult.Shown,
            MidShown = decisionResult.MidShown,
            LowShown = decisionResult.LowShown,
            FarCount = decisionResult.FarCount,
            MissingMidInstance = decisionResult.MissingMidInstance,
            MissingLowInstance = decisionResult.MissingLowInstance,
            VisualStateChanges = decisionResult.VisualStateChanges,
            VisualStatePending = decisionResult.VisualStatePending,
            VisualTransitionsCommitted = decisionResult.VisualTransitionsCommitted,
            VisibleCharacterSafeGate = decisionResult.VisibleCharacterSafeGate,
            VisibleCharacterMidInstances = decisionResult.VisibleCharacterMidInstances,
            VisibleCharacterSafeMidInstances = decisionResult.VisibleCharacterSafeMidInstances,
            VisibleCharacterLowInstances = decisionResult.VisibleCharacterLowInstances,
            VisibleCharacterSafeLowInstances = decisionResult.VisibleCharacterSafeLowInstances,
            VisibleCharacterUsingSafeMid = decisionResult.VisibleCharacterUsingSafeMid,
            VisibleCharacterUsingSafeLow = decisionResult.VisibleCharacterUsingSafeLow,
            VisibleCharacterUsingFarImpostor = decisionResult.VisibleCharacterUsingFarImpostor,
            VisibleCharacterMidSuppressed = 0,
            VisibleCharacterForcedDetailByUnsafeMid = decisionResult.VisibleCharacterForcedDetailByUnsafeMid,
            VisibleCharacterBudgetDetail = decisionResult.VisibleCharacterBudgetDetail,
            VisibleCharacterSafetyDetail = decisionResult.VisibleCharacterSafetyDetail,
            VisibleMidSafetyPatched = decisionResult.VisibleMidSafetyPatched,
            VisibleNearDetail = decisionResult.VisibleNearDetail,
            VisibleNearMid = decisionResult.VisibleNearMid
        };
    }

    public bool ShouldRunDiagnostics(int frame)
    {
        return EnableRenderBudgetDiagnostics && frame >= _nextDiagnosticFrame;
    }

    public void ScheduleNextDiagnostics(int frame)
    {
        _nextDiagnosticFrame = frame + DiagnosticIntervalFrames;
    }

    public void ResetDiagnosticFrame()
    {
        _nextDiagnosticFrame = 0;
    }

    public bool ShouldAppendDiagnosticSample(string sample)
    {
        return sample.Length <= DiagnosticSampleMaxLength;
    }
}
