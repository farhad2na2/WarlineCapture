using UnityEngine;

public struct UnitRenderBudgetCameraMotionSystem
{
    private const int CameraSettleFrames = 8;
    private const float CameraMoveThresholdSq = 0.0004f;
    private const float CameraRotateThresholdDegrees = 0.03f;

    private bool _hasCameraSnapshot;
    private Vector3 _lastCameraPosition;
    private Quaternion _lastCameraRotation;

    public bool IsCameraMotionActive(
        Camera camera,
        ref UnitRenderBudgetScheduleSystem scheduleSystem,
        ref UnitRenderBudgetDiagnosticStateSystem diagnosticStateSystem,
        int frame)
    {
        Vector3 currentPosition = camera.transform.position;
        Quaternion currentRotation = camera.transform.rotation;
        if (!_hasCameraSnapshot)
        {
            _hasCameraSnapshot = true;
            _lastCameraPosition = currentPosition;
            _lastCameraRotation = currentRotation;
            return false;
        }

        bool moved = Vector3.SqrMagnitude(currentPosition - _lastCameraPosition) > CameraMoveThresholdSq;
        bool rotated = Quaternion.Angle(currentRotation, _lastCameraRotation) > CameraRotateThresholdDegrees;
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
