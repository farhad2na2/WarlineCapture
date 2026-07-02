using Unity.Entities;
using UnityEngine;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public sealed class SelectionUiCameraSystemHelper
    {
        private enum MatchHudZoomLevel
        {
            ZoomedOut,
            Default,
            ZoomedIn
        }

        private const float DefaultMinZoomHeight = 10f;
        private const float DefaultMaxZoomHeight = 45f;
        private const float DefaultZoomSpeed = 20f;
        private const float MatchHudZoomOutBoundaryUsage = 0.88f;

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
        private float _matchHudZoomTransitionSmoothTime = 0.25f;
        private float _fullscreenIsoPitch = 82f;
        private float _fullscreenIsoYaw = 10f;
        private float _fullscreenIsoOrthographicSize = 24f;
        private MatchHudZoomLevel _matchHudZoomLevel = MatchHudZoomLevel.Default;
        private bool _matchHudZoomTransitionActive;
        private float _matchHudZoomTargetHeight = 24f;
        private Vector3 _matchHudZoomFocusWorldPosition;

        public SelectionUiCameraSystemHelper(RtsCameraSystem cameraSystem, RtsCameraRequestSystem cameraRequestSystem)
        {
            _cameraSystem = cameraSystem ?? ResolveDefaultCameraSystem();
            _cameraRequestSystem = cameraRequestSystem ?? ResolveDefaultCameraRequestSystem();
        }

        public bool IsNormalIsoModeActive => _cameraSystem != null && _cameraSystem.NormalIsoModeActive;
        public bool IsCameraDragging => _cameraSystem != null && _cameraSystem.IsDragging;
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
                _matchHudZoomTransitionSmoothTime = config.ZoomTransitionSmoothTime;
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
            if (_matchHudZoomTransitionSmoothTime <= 0f)
                _matchHudZoomTransitionSmoothTime = 0.25f;
            _matchHudZoomLevel = MatchHudZoomLevel.Default;
            _matchHudZoomTransitionActive = false;
            _matchHudZoomTargetHeight = _normalModeZoomHeight;
        }

        public void ToggleNormalIsoMode()
        {
            if (_cameraSystem == null)
                return;

            if (_cameraSystem.NormalIsoModeActive)
                ExitNormalIsoMode();
            else
                EnterNormalIsoMode();
        }

        public void MoveCameraGroundCenterTo(Vector3 focusWorldPosition)
        {
            if (_cameraRequestSystem == null || !TryGetDefaultEntityManager(out EntityManager em))
                return;

            _cameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            ProcessCameraRequests(em);
        }

        public void ZoomPerspective(float direction, float deltaTime)
        {
            if (_cameraRequestSystem == null || _worldCamera == null || Mathf.Approximately(direction, 0f) || !TryGetDefaultEntityManager(out EntityManager em))
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

        public MatchHudZoomControlState ReadZoomControlState()
        {
            if (_cameraRequestSystem == null || _cameraSystem == null || _worldCamera == null)
                return MatchHudZoomControlState.Disabled;

            if (!TryGetDefaultEntityManager(out EntityManager em) || HasValidTacticalFollowPose(em))
                return MatchHudZoomControlState.Disabled;

            return new MatchHudZoomControlState(
                zoomInEnabled: _matchHudZoomLevel != MatchHudZoomLevel.ZoomedIn,
                zoomOutEnabled: _matchHudZoomLevel != MatchHudZoomLevel.ZoomedOut);
        }

        public void UpdateZoomTransition()
        {
            if (!_matchHudZoomTransitionActive)
                return;
            if (_cameraSystem == null || _cameraRequestSystem == null || _worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
            {
                _matchHudZoomTransitionActive = false;
                return;
            }
            if (HasValidTacticalFollowPose(em))
            {
                _matchHudZoomTransitionActive = false;
                return;
            }

            _cameraRequestSystem.QueueUpdatePerspectiveMode(
                em,
                _matchHudZoomTargetHeight,
                _normalModePitch,
                _normalModeYaw,
                _normalModeFieldOfView,
                _matchHudZoomTransitionSmoothTime,
                completeTransitionOnArrive: false);
            _cameraRequestSystem.QueueMoveGroundCenterTo(em, _matchHudZoomFocusWorldPosition);
            _cameraRequestSystem.QueueSetNormalIsoModeActive(em, false);
            ProcessCameraRequests(em);

            if (IsMatchHudZoomTransitionComplete())
                _matchHudZoomTransitionActive = false;
        }

        public bool RequestZoomInLevel()
        {
            MatchHudZoomLevel targetLevel = _matchHudZoomLevel switch
            {
                MatchHudZoomLevel.ZoomedOut => MatchHudZoomLevel.Default,
                MatchHudZoomLevel.Default => MatchHudZoomLevel.ZoomedIn,
                _ => MatchHudZoomLevel.ZoomedIn
            };

            return ApplyMatchHudZoomLevel(targetLevel);
        }

        public bool RequestZoomOutLevel()
        {
            MatchHudZoomLevel targetLevel = _matchHudZoomLevel switch
            {
                MatchHudZoomLevel.ZoomedIn => MatchHudZoomLevel.Default,
                MatchHudZoomLevel.Default => MatchHudZoomLevel.ZoomedOut,
                _ => MatchHudZoomLevel.ZoomedOut
            };

            return ApplyMatchHudZoomLevel(targetLevel);
        }

        public void SmoothMoveCameraGroundCenterTo(Vector3 focusWorldPosition)
        {
            if (_cameraRequestSystem == null || _worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
                return;

            _cameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: true);
            _cameraRequestSystem.QueueClearDragging(em);
            ProcessCameraRequests(em);
        }

        public void FollowCameraGroundCenterTo(Vector3 focusWorldPosition)
        {
            if (_cameraRequestSystem == null || _worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
                return;

            _cameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: false);
            _cameraRequestSystem.QueueClearDragging(em);
            ProcessCameraRequests(em);
        }

        private void EnterNormalIsoMode()
        {
            if (_cameraSystem == null || _worldCamera == null)
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

            if (_cameraRequestSystem == null || !TryGetDefaultEntityManager(out EntityManager em))
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
            if (_cameraSystem == null || _worldCamera == null)
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

            if (_cameraRequestSystem == null || !TryGetDefaultEntityManager(out EntityManager em))
                return;

            _cameraRequestSystem.QueueApplyPerspectiveModeInstant(em, targetHeight, _normalModePitch, _normalModeYaw, _normalModeFieldOfView);
            _cameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            _cameraRequestSystem.QueueClearDragging(em);
            _cameraRequestSystem.QueueSetNormalIsoModeActive(em, false);
            ProcessCameraRequests(em);
        }

        private bool ApplyMatchHudZoomLevel(MatchHudZoomLevel targetLevel)
        {
            if (targetLevel == _matchHudZoomLevel)
                return false;
            if (_cameraSystem == null || _cameraRequestSystem == null || _worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
                return false;
            if (HasValidTacticalFollowPose(em))
                return false;

            _matchHudZoomFocusWorldPosition = _cameraSystem.GetCameraGroundCenterWorld(_worldCamera);
            _matchHudZoomTargetHeight = ResolveMatchHudZoomHeight(targetLevel);
            _matchHudZoomLevel = targetLevel;
            _matchHudZoomTransitionActive = true;

            _cameraRequestSystem.QueueClearSmoothFocusTarget(em);
            _cameraRequestSystem.QueueClearDragging(em);
            _cameraRequestSystem.QueueCompleteZoomTransition(em);
            _cameraRequestSystem.QueueResetTransitionVelocities(em);
            _cameraRequestSystem.QueueSetNormalIsoModeActive(em, false);
            ProcessCameraRequests(em);
            UpdateZoomTransition();
            return true;
        }

        private float ResolveMatchHudZoomHeight(MatchHudZoomLevel zoomLevel)
        {
            return zoomLevel switch
            {
                MatchHudZoomLevel.ZoomedOut => _cameraSystem.ResolveClampSafePerspectiveHeight(
                    _worldCamera,
                    _maxZoomHeight,
                    _normalModeZoomHeight,
                    _normalModePitch,
                    _normalModeYaw,
                    _normalModeFieldOfView,
                    MatchHudZoomOutBoundaryUsage),
                MatchHudZoomLevel.ZoomedIn => _minZoomHeight,
                _ => _normalModeZoomHeight
            };
        }

        private bool IsMatchHudZoomTransitionComplete()
        {
            if (_worldCamera == null)
                return true;

            Vector3 euler = _worldCamera.transform.rotation.eulerAngles;
            return Mathf.Abs(_worldCamera.transform.position.y - _matchHudZoomTargetHeight) <= 0.05f &&
                   Mathf.Abs(Mathf.DeltaAngle(euler.x, _normalModePitch)) <= 0.1f &&
                   Mathf.Abs(Mathf.DeltaAngle(euler.y, _normalModeYaw)) <= 0.1f &&
                   Mathf.Abs(_worldCamera.fieldOfView - _normalModeFieldOfView) <= 0.05f;
        }

        private void ProcessCameraRequests(EntityManager em)
        {
            if (_cameraRequestSystem == null || _cameraSystem == null)
                return;

            _cameraRequestSystem.ProcessPendingRequests(em, _cameraSystem, _worldCamera);
        }

        private static RtsCameraSystem ResolveDefaultCameraSystem()
        {
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? world.GetOrCreateSystemManaged<RtsCameraSystem>()
                : null;
        }

        private static RtsCameraRequestSystem ResolveDefaultCameraRequestSystem()
        {
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? world.GetOrCreateSystemManaged<RtsCameraRequestSystem>()
                : null;
        }

        private static bool TryGetDefaultEntityManager(out EntityManager em)
        {
            em = default;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            em = world.EntityManager;
            return true;
        }

        private static bool HasValidTacticalFollowPose(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraPoseComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            TacticalFollowCameraPoseComponent pose =
                em.GetComponentData<TacticalFollowCameraPoseComponent>(query.GetSingletonEntity());
            return pose.Valid != 0;
        }
    }
}
