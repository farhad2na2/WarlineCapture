public struct UnitRenderBudgetSchedule
{
    private int _nextUpdateFrame;
    private int _lodResumeFrame;
    private bool _budgetStable;
    private int _stableUnitCount;

    public bool ShouldSkipStableBudget(bool cameraMotionActive, int currentUnitCount)
    {
        return !cameraMotionActive && _budgetStable && currentUnitCount == _stableUnitCount;
    }

    public bool ShouldSkipUpdateFrame(bool cameraMotionActive, int frame)
    {
        return !cameraMotionActive && frame < _nextUpdateFrame;
    }

    public void ScheduleNextUpdate(bool cameraMotionActive, int frame, int updateIntervalFrames)
    {
        _nextUpdateFrame = frame + (cameraMotionActive ? 1 : updateIntervalFrames);
    }

    public void MarkCameraMotion(int frame, int settleFrames)
    {
        _budgetStable = false;
        _lodResumeFrame = frame + settleFrames;
    }

    public bool IsWithinLodResume(int frame)
    {
        return frame < _lodResumeFrame;
    }

    public void RecordBudgetStability(int currentUnitCount, bool budgetStable)
    {
        _budgetStable = budgetStable;
        _stableUnitCount = currentUnitCount;
    }
}
