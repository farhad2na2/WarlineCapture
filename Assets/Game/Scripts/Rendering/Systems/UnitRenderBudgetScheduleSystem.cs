namespace Game.Rendering
{
    public struct UnitRenderBudgetSchedule
    {
        private const int CameraMotionUpdateIntervalFrames = 3;
        private int _nextUpdateFrame;
        private int _lodResumeFrame;
        private bool _budgetStable;
        private int _stableUnitCount;
        private int _stableSelectedUnitCount;
        private int _stableSelectedUnitHash;

        public bool ShouldSkipStableBudget(
            bool cameraMotionActive,
            int currentUnitCount,
            int currentSelectedUnitCount,
            int currentSelectedUnitHash)
        {
            return
                !cameraMotionActive &&
                currentSelectedUnitCount == 0 &&
                currentSelectedUnitHash == _stableSelectedUnitHash &&
                _budgetStable &&
                currentUnitCount == _stableUnitCount;
        }

        public bool ShouldSkipUpdateFrame(
            bool cameraMotionActive,
            int frame,
            int currentSelectedUnitCount,
            int currentSelectedUnitHash)
        {
            return
                currentSelectedUnitCount == _stableSelectedUnitCount &&
                currentSelectedUnitHash == _stableSelectedUnitHash &&
                frame < _nextUpdateFrame;
        }

        public void ScheduleNextUpdate(bool cameraMotionActive, int frame, int updateIntervalFrames)
        {
            _nextUpdateFrame = frame + (cameraMotionActive ? CameraMotionUpdateIntervalFrames : updateIntervalFrames);
        }

        public void MarkCameraMotion(int frame, int settleFrames)
        {
            _budgetStable = false;
            if (!IsWithinLodResume(frame))
                _nextUpdateFrame = frame;
            _lodResumeFrame = frame + settleFrames;
        }

        public bool IsWithinLodResume(int frame)
        {
            return frame < _lodResumeFrame;
        }

        public void RecordBudgetStability(
            int currentUnitCount,
            int currentSelectedUnitCount,
            int currentSelectedUnitHash,
            bool budgetStable)
        {
            _budgetStable = budgetStable;
            _stableUnitCount = currentUnitCount;
            _stableSelectedUnitCount = currentSelectedUnitCount;
            _stableSelectedUnitHash = currentSelectedUnitHash;
        }
    }
}
