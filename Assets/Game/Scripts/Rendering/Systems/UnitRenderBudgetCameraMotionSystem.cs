using Unity.Mathematics;

public struct UnitRenderBudgetCameraMotion
{
    private const int CameraSettleFrames = 8;
    private const float CameraMoveThresholdSq = 0.0004f;
    private const float CameraRotateThresholdDegrees = 0.03f;

    private bool _hasCameraSnapshot;
    private float3 _lastCameraPosition;
    private quaternion _lastCameraRotation;

    public bool IsCameraMotionActive(
        RuntimeCameraSnapshotComponent camera,
        ref UnitRenderBudgetSchedule scheduleSystem,
        ref UnitRenderBudgetDiagnosticState diagnosticStateSystem,
        int frame)
    {
        float3 currentPosition = camera.Position;
        quaternion currentRotation = camera.Rotation;
        if (!_hasCameraSnapshot)
        {
            _hasCameraSnapshot = true;
            _lastCameraPosition = currentPosition;
            _lastCameraRotation = currentRotation;
            return false;
        }

        bool moved = math.distancesq(currentPosition, _lastCameraPosition) > CameraMoveThresholdSq;
        float rotationDot = math.clamp(math.abs(math.dot(currentRotation.value, _lastCameraRotation.value)), 0f, 1f);
        bool rotated = math.degrees(math.acos(rotationDot) * 2f) > CameraRotateThresholdDegrees;
        _lastCameraPosition = currentPosition;
        _lastCameraRotation = currentRotation;

        if (moved || rotated)
        {
            scheduleSystem.MarkCameraMotion(frame, CameraSettleFrames);
            diagnosticStateSystem.ResetDiagnosticFrame();
            return true;
        }

        return scheduleSystem.IsWithinLodResume(frame);
    }
}
