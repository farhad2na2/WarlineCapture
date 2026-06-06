using Unity.Entities;
using UnityEngine;

public sealed class SelectionUiCameraSystem
{
    private const float DefaultMinZoomHeight = 10f;
    private const float DefaultMaxZoomHeight = 45f;
    private const float DefaultZoomSpeed = 20f;

    private readonly RtsCameraSystem _cameraSystem;
    private readonly RtsCameraRequestSystem _cameraRequestSystem;
    private Camera _worldCamera;
    private float _minZoomHeight = DefaultMinZoomHeight;
    private float _maxZoomHeight = DefaultMaxZoomHeight;
    private float _zoomSpeed = DefaultZoomSpeed;
    private float _normalModeZoomHeight = 24f;
    private float _normalModePitch = 58f;
    private float _normalModeYaw = 10f;
    private float _normalModeFieldOfView = 36f;
    private float _fullscreenIsoPitch = 82f;
    private float _fullscreenIsoYaw = 10f;
    private float _fullscreenIsoOrthographicSize = 24f;

    public SelectionUiCameraSystem(RtsCameraSystem cameraSystem, RtsCameraRequestSystem cameraRequestSystem)
    {
        _cameraSystem = cameraSystem ?? new RtsCameraSystem();
        _cameraRequestSystem = cameraRequestSystem ?? new RtsCameraRequestSystem();
    }

    public bool IsNormalIsoModeActive => _cameraSystem.NormalIsoModeActive;
    public bool IsCameraDragging => _cameraSystem.IsDragging;
    public Camera WorldCamera => _worldCamera;

    public void Init(RTSSelectionSystemConfig config, Camera worldCamera)
    {
        _worldCamera = worldCamera != null ? worldCamera : config != null ? config.WorldCamera : null;
        if (config != null)
        {
            _minZoomHeight = config.MinZoomHeight;
            _maxZoomHeight = config.MaxZoomHeight;
            _zoomSpeed = config.ZoomSpeed;
            _normalModeZoomHeight = config.NormalModeZoomHeight;
            _normalModePitch = config.NormalModePitch;
            _normalModeYaw = config.NormalModeYaw;
            _normalModeFieldOfView = config.NormalModeFieldOfView;
            _fullscreenIsoPitch = config.FullscreenIsoPitch;
            _fullscreenIsoYaw = config.FullscreenIsoYaw;
            _fullscreenIsoOrthographicSize = config.FullscreenIsoOrthographicSize;
        }

        if (_minZoomHeight <= 0f)
            _minZoomHeight = DefaultMinZoomHeight;
        if (_maxZoomHeight <= _minZoomHeight)
            _maxZoomHeight = Mathf.Max(DefaultMaxZoomHeight, _minZoomHeight + 1f);
        if (_zoomSpeed <= 0f)
            _zoomSpeed = DefaultZoomSpeed;
        if (_normalModeZoomHeight <= 0f)
            _normalModeZoomHeight = 24f;
        _normalModeZoomHeight = Mathf.Min(_normalModeZoomHeight, _maxZoomHeight);
        if (_normalModeFieldOfView <= 1f)
            _normalModeFieldOfView = 36f;
    }

    public void ToggleNormalIsoMode()
    {
        if (_cameraSystem.NormalIsoModeActive)
            ExitNormalIsoMode();
        else
            EnterNormalIsoMode();
    }

    public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
        ProcessCameraRequests(em);
    }

    public void ZoomPerspective(float direction, float deltaTime)
    {
        if (_worldCamera == null || Mathf.Approximately(direction, 0f) || !TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueuePerspectiveZoom(
            em,
            Mathf.Sign(direction),
            _zoomSpeed,
            Mathf.Max(deltaTime, 1f / 60f),
            _minZoomHeight,
            _maxZoomHeight);
        ProcessCameraRequests(em);
    }

    public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (_worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: true);
        _cameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(em);
    }

    public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
    {
        if (_worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: false);
        _cameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(em);
    }

    private void EnterNormalIsoMode()
    {
        if (_worldCamera == null)
            return;

        Vector3 focusWorldPosition = _cameraSystem.GetCameraGroundCenterWorld(_worldCamera);
        float currentGroundSpan = _cameraSystem.GetVisibleGroundVerticalSpan(_worldCamera);
        float targetHeight = Mathf.Clamp(_worldCamera.transform.position.y, _minZoomHeight, _maxZoomHeight);
        float targetOrthographicSize = Mathf.Clamp(
            _cameraSystem.CalculateOrthographicSizeForGroundSpan(
                _worldCamera,
                currentGroundSpan,
                targetHeight,
                _fullscreenIsoPitch,
                _fullscreenIsoYaw,
                _fullscreenIsoOrthographicSize),
            8f,
            48f);

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueueSetFullscreenIsoTargets(em, targetHeight, targetOrthographicSize);
        _cameraRequestSystem.QueueApplyFullscreenIsoModeInstant(em, targetHeight, targetOrthographicSize, _fullscreenIsoPitch, _fullscreenIsoYaw);
        _cameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
        _cameraRequestSystem.QueueClearDragging(em);
        _cameraRequestSystem.QueueSetNormalIsoModeActive(em, true);
        ProcessCameraRequests(em);
    }

    private void ExitNormalIsoMode()
    {
        if (_worldCamera == null)
            return;

        Vector3 focusWorldPosition = _cameraSystem.GetCameraGroundCenterWorld(_worldCamera);
        float currentGroundSpan = _cameraSystem.GetVisibleGroundVerticalSpan(_worldCamera);
        float targetHeight = _cameraSystem.CalculatePerspectiveHeightForGroundSpan(
            _worldCamera,
            currentGroundSpan,
            _normalModePitch,
            _normalModeYaw,
            _normalModeFieldOfView,
            _minZoomHeight,
            _maxZoomHeight,
            _normalModeZoomHeight);

        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        _cameraRequestSystem.QueueApplyPerspectiveModeInstant(em, targetHeight, _normalModePitch, _normalModeYaw, _normalModeFieldOfView);
        _cameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
        _cameraRequestSystem.QueueClearDragging(em);
        _cameraRequestSystem.QueueSetNormalIsoModeActive(em, false);
        ProcessCameraRequests(em);
    }

    private void ProcessCameraRequests(EntityManager em)
    {
        _cameraRequestSystem.ProcessPendingRequests(em, _cameraSystem, _worldCamera);
    }

    private static bool TryGetDefaultEntityManager(out EntityManager em)
    {
        em = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        em = world.EntityManager;
        return true;
    }
}
