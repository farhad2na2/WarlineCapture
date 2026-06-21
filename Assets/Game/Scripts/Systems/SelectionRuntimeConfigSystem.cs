using UnityEngine;

public sealed class SelectionRuntimeConfigSystem
{
    private const float DefaultPanSensitivity = 0.03f;
    private const float DefaultZoomSpeed = 20f;
    private const float DefaultMinZoomHeight = 10f;
    private const float DefaultMaxZoomHeight = 45f;

    public struct State
    {
        public Camera WorldCamera;
        public GameObject MoveOrderMarkerPrefab;
        public float OrderMarkerVisibleSeconds;
        public GameObject AttackOrderMarkerPrefab;
        public GameObject AttackTargetMarkerPrefab;
        public float DragThresholdPixels;
        public float SelectionModeHoldSeconds;
        public float PanSensitivity;
        public float ZoomSpeed;
        public float MinZoomHeight;
        public float MaxZoomHeight;
        public float NormalModeZoomHeight;
        public float BuildModeZoomHeight;
        public float NormalModePitch;
        public float BuildModePitch;
        public float NormalModeYaw;
        public float BuildModeYaw;
        public float NormalModeFieldOfView;
        public float BuildModeFieldOfView;
        public float FullscreenIsoZoomHeight;
        public float FullscreenIsoPitch;
        public float FullscreenIsoYaw;
        public float FullscreenIsoOrthographicSize;
        public float ZoomTransitionSmoothTime;
    }

    public State CreateState(RTSSelectionSystemConfig config, Camera fallbackWorldCamera)
    {
        return CreateStateFromConfig(config, fallbackWorldCamera);
    }

    public static State CreateStateFromConfig(RTSSelectionSystemConfig config, Camera fallbackWorldCamera)
    {
        State state = CreateDefaultState(fallbackWorldCamera);
        if (config != null)
            ApplyConfig(config, ref state);

        Normalize(ref state);
        return state;
    }

    private static State CreateDefaultState(Camera worldCamera)
    {
        return new State
        {
            WorldCamera = worldCamera,
            OrderMarkerVisibleSeconds = 1.25f,
            DragThresholdPixels = 8f,
            SelectionModeHoldSeconds = 1f,
            PanSensitivity = DefaultPanSensitivity,
            ZoomSpeed = DefaultZoomSpeed,
            MinZoomHeight = DefaultMinZoomHeight,
            MaxZoomHeight = DefaultMaxZoomHeight,
            NormalModeZoomHeight = 24f,
            BuildModeZoomHeight = 100f,
            NormalModePitch = 58f,
            BuildModePitch = 64f,
            NormalModeYaw = 10f,
            BuildModeYaw = 10f,
            NormalModeFieldOfView = 36f,
            BuildModeFieldOfView = 32f,
            FullscreenIsoZoomHeight = 40f,
            FullscreenIsoPitch = 82f,
            FullscreenIsoYaw = 10f,
            FullscreenIsoOrthographicSize = 24f,
            ZoomTransitionSmoothTime = 0.25f
        };
    }

    private static void ApplyConfig(RTSSelectionSystemConfig config, ref State state)
    {
        if (config.WorldCamera != null)
            state.WorldCamera = config.WorldCamera;
        state.MoveOrderMarkerPrefab = config.MoveOrderMarkerPrefab;
        state.OrderMarkerVisibleSeconds = Mathf.Max(0.01f, config.OrderMarkerVisibleSeconds);
        state.AttackOrderMarkerPrefab = config.AttackOrderMarkerPrefab;
        state.AttackTargetMarkerPrefab = config.AttackTargetMarkerPrefab;
        state.DragThresholdPixels = config.DragThresholdPixels;
        state.SelectionModeHoldSeconds = Mathf.Max(0.1f, config.SelectionModeHoldSeconds);
        state.PanSensitivity = config.PanSensitivity;
        state.ZoomSpeed = config.ZoomSpeed;
        state.MinZoomHeight = config.MinZoomHeight;
        state.MaxZoomHeight = config.MaxZoomHeight;
        state.NormalModeZoomHeight = config.NormalModeZoomHeight;
        state.BuildModeZoomHeight = config.BuildModeZoomHeight;
        state.NormalModePitch = config.NormalModePitch;
        state.BuildModePitch = config.BuildModePitch;
        state.NormalModeYaw = config.NormalModeYaw;
        state.BuildModeYaw = config.BuildModeYaw;
        state.NormalModeFieldOfView = config.NormalModeFieldOfView;
        state.BuildModeFieldOfView = config.BuildModeFieldOfView;
        state.FullscreenIsoZoomHeight = config.FullscreenIsoZoomHeight;
        state.FullscreenIsoPitch = config.FullscreenIsoPitch;
        state.FullscreenIsoYaw = config.FullscreenIsoYaw;
        state.FullscreenIsoOrthographicSize = config.FullscreenIsoOrthographicSize;
        state.ZoomTransitionSmoothTime = config.ZoomTransitionSmoothTime;
    }

    private static void Normalize(ref State state)
    {
        if (state.PanSensitivity <= 0f)
            state.PanSensitivity = DefaultPanSensitivity;
        if (state.ZoomSpeed <= 0f)
            state.ZoomSpeed = DefaultZoomSpeed;
        if (state.MinZoomHeight <= 0f)
            state.MinZoomHeight = DefaultMinZoomHeight;
        if (state.MaxZoomHeight <= state.MinZoomHeight)
            state.MaxZoomHeight = Mathf.Max(DefaultMaxZoomHeight, state.MinZoomHeight + 1f);
        if (state.NormalModeZoomHeight <= 0f)
            state.NormalModeZoomHeight = 24f;
        state.NormalModeZoomHeight = Mathf.Min(state.NormalModeZoomHeight, state.MaxZoomHeight);
        if (state.BuildModeZoomHeight < state.NormalModeZoomHeight)
            state.BuildModeZoomHeight = state.NormalModeZoomHeight;
        state.BuildModeZoomHeight = Mathf.Min(state.BuildModeZoomHeight, state.MaxZoomHeight);
        if (state.NormalModeFieldOfView <= 1f)
            state.NormalModeFieldOfView = 36f;
        if (state.BuildModeFieldOfView <= 1f)
            state.BuildModeFieldOfView = state.NormalModeFieldOfView;
        if (state.ZoomTransitionSmoothTime <= 0f)
            state.ZoomTransitionSmoothTime = 0.25f;
    }
}
