using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class MatchHudMinimapInputSystem
{
    private const int CaptureResolution = 512;
    private static readonly Color32 RasterBackgroundA = new(150, 136, 116, 255);
    private static readonly Color32 RasterBackgroundB = new(126, 116, 101, 255);
    private static readonly Color32 RasterGrid = new(97, 109, 97, 255);
    private static readonly Color32 RasterRoad = new(83, 75, 61, 255);
    private static readonly Color32 RasterDirtRoad = new(112, 91, 62, 255);
    private static readonly Color32 RasterSidewalk = new(150, 145, 130, 255);
    private static readonly Color32 RasterBridge = new(82, 103, 116, 255);
    private static readonly Color32 RasterRamp = new(132, 103, 58, 255);
    private static readonly Color32 RasterPlaza = new(119, 112, 99, 255);
    private static readonly Color32 RasterBlocked = new(86, 78, 70, 255);
    private static readonly Color PlayerMarkerColor = new(0.2f, 0.95f, 0.62f, 0.95f);
    private static readonly Color EnemyMarkerColor = new(1f, 0.22f, 0.18f, 0.95f);
    private static readonly Color NeutralMarkerColor = new(1f, 0.78f, 0.25f, 0.95f);
    private const int MaxMarkers = 1024;
    private const int WarmupStaticMapRefreshCount = 1;
    private const float WarmupStaticMapRefreshSeconds = 1f;
    private const float CameraCenteredMapRefreshSeconds = 0.75f;
    private const float CameraCenteredMapSizeRefreshFraction = 0.08f;
    private const float CameraDragRecaptureDebounceSeconds = 0.45f;
    private const float CameraViewportEdgeRefreshMargin = 0.12f;
    private const float MinimapZoomedInScale = 0.5f;
    private const float MarkerRefreshSeconds = 0.1f;
    private const int MinRasterFeatureCount = 24;

    private MatchHudMinimapView _view;
    private IMatchRuntimeState _runtimeGameplayStateSystem;
    private IMatchHudCameraControl _selectionUiCameraSystem;
    private IMatchHudMinimapDataSource _minimapDataSource;
    private readonly System.Collections.Generic.List<Image> _markerPool = new();
    private readonly System.Collections.Generic.List<MatchHudMinimapMarkerModel> _markerScratch = new();
    private readonly System.Collections.Generic.List<MatchHudMinimapRoadCellModel> _roadScratch = new();
    private readonly System.Collections.Generic.List<MatchHudMinimapSurfaceFeatureModel> _surfaceScratch = new();
    private Camera _captureCamera;
    private RenderTexture _renderTexture;
    private Texture2D _readbackTexture;
    private Texture2D _rasterTexture;
    private Sprite _captureSprite;
    private Color32[] _rasterPixels;
    private readonly Vector3[] _mapWorldCorners = new Vector3[4];
    private readonly MatchHudMinimapProjectionGrid[] _rasterProjectionCandidates = new MatchHudMinimapProjectionGrid[4];
    private bool _staticMapDirty = true;
    private float _nextStaticMapRetryTime;
    private int _warmupStaticMapRefreshesRemaining;
    private bool _hasCapturedProjectionGrid;
    private bool _hasCachedGrid;
    private bool _markersDirty = true;
    private MatchHudMinimapProjectionGrid _capturedProjectionGrid;
    private MatchHudMinimapProjectionGrid _currentProjectionGrid;
    private MatchHudMinimapGridModel _cachedGrid;
    private float _nextMarkerRefreshTime;
    private float _cameraDragRefreshBlockedUntil;
    private bool _minimapZoomedIn;

    public void Bind(
        MatchHudMinimapView view,
        IMatchRuntimeState runtimeGameplayStateSystem,
        IMatchHudCameraControl selectionUiCameraSystem,
        IMatchHudMinimapDataSource minimapDataSource)
    {
        if (_view == view)
            return;

        Unbind();
        _view = view;
        _runtimeGameplayStateSystem = runtimeGameplayStateSystem;
        _selectionUiCameraSystem = selectionUiCameraSystem;
        _minimapDataSource = minimapDataSource;

        if (_view == null)
            return;

        _view.FocusRequested += HandleFocusRequested;
        _view.ZoomHeldChanged += HandleZoomHeldChanged;
        _hasCapturedProjectionGrid = false;
        _hasCachedGrid = false;
        _markersDirty = true;
        _cachedGrid = default;
        _currentProjectionGrid = default;
        _capturedProjectionGrid = default;
        _nextMarkerRefreshTime = 0f;
        _nextStaticMapRetryTime = 0f;
        _cameraDragRefreshBlockedUntil = 0f;
        _warmupStaticMapRefreshesRemaining = 0;
        _minimapZoomedIn = false;
        _staticMapDirty = true;
        _view.SetProjectionMode(useFullMapProjection: true);
        Update();
    }

    public void Unbind()
    {
        if (_view != null)
        {
            _view.FocusRequested -= HandleFocusRequested;
            _view.ZoomHeldChanged -= HandleZoomHeldChanged;
        }

        if (_runtimeGameplayStateSystem != null)
        {
            _runtimeGameplayStateSystem.ZoomInHeld = false;
            _runtimeGameplayStateSystem.ZoomOutHeld = false;
        }

        _view = null;
        _runtimeGameplayStateSystem = null;
        _selectionUiCameraSystem = null;
        _minimapDataSource = null;
    }

    public void Dispose()
    {
        Unbind();
        ReleaseCaptureResources();
    }

    public void NotifyStaticMapChanged()
    {
        _staticMapDirty = true;
        _markersDirty = true;
    }

    public void Update()
    {
        if (_view == null || !_view.isActiveAndEnabled || !TryGetGrid(out MatchHudMinimapGridModel grid))
            return;

        Camera worldCamera = _selectionUiCameraSystem != null ? _selectionUiCameraSystem.WorldCamera : null;
        MatchHudMinimapProjectionGrid desiredProjectionGrid = _view.UseFullMapProjection
            ? MatchHudMinimapProjectionGrid.FromGridModel(grid)
            : MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(
                grid,
                worldCamera,
                ResolveMapAspect());
        desiredProjectionGrid = ApplyMinimapZoom(desiredProjectionGrid);
        bool cameraRefreshBlocked = IsCameraRefreshBlocked();

        if (ShouldRefreshCameraCenteredMap(desiredProjectionGrid, worldCamera, cameraRefreshBlocked))
            _staticMapDirty = true;

        MatchHudMinimapProjectionGrid projectionGrid = _hasCapturedProjectionGrid
            ? _capturedProjectionGrid
            : desiredProjectionGrid;
        _currentProjectionGrid = projectionGrid;

        if (_staticMapDirty)
        {
            if (_hasCapturedProjectionGrid && cameraRefreshBlocked)
            {
                projectionGrid = _capturedProjectionGrid;
                _currentProjectionGrid = _capturedProjectionGrid;
            }
            else if (Time.unscaledTime >= _nextStaticMapRetryTime)
            {
                bool wasFirstCapture = !_hasCapturedProjectionGrid;
                bool rendered = RenderStaticMap(
                    desiredProjectionGrid,
                    out MatchHudMinimapProjectionGrid renderedProjectionGrid);
                _staticMapDirty = !rendered;
                if (rendered)
                {
                    projectionGrid = renderedProjectionGrid;
                    _currentProjectionGrid = renderedProjectionGrid;
                    _capturedProjectionGrid = renderedProjectionGrid;
                    _hasCapturedProjectionGrid = true;
                    _markersDirty = true;
                    if (!_view.IsDraggingViewport)
                        _view.ClearManualViewportOverride();
                }

                if (rendered)
                {
                    if (wasFirstCapture)
                        _warmupStaticMapRefreshesRemaining = WarmupStaticMapRefreshCount;
                    else if (_warmupStaticMapRefreshesRemaining > 0)
                        _warmupStaticMapRefreshesRemaining--;
                }

                _nextStaticMapRetryTime = Time.unscaledTime + (_warmupStaticMapRefreshesRemaining > 0
                    ? WarmupStaticMapRefreshSeconds
                    : CameraCenteredMapRefreshSeconds);
            }
            else if (_hasCapturedProjectionGrid)
            {
                projectionGrid = _capturedProjectionGrid;
                _currentProjectionGrid = _capturedProjectionGrid;
            }
        }
        else if (_warmupStaticMapRefreshesRemaining > 0 && Time.unscaledTime >= _nextStaticMapRetryTime)
        {
            _staticMapDirty = true;
        }

        if (MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(worldCamera, projectionGrid, out Rect viewport))
        {
            if (_view.HasManualViewportOverride)
            {
                _view.SetViewportNormalizedRect(_view.ManualViewportNormalizedRect);
            }
            else
            {
                _view.SetViewportNormalizedRect(viewport);
            }
        }

        UpdateMarkersIfDue(projectionGrid);
    }

    private void HandleFocusRequested(Vector2 normalized)
    {
        if (_runtimeGameplayStateSystem == null ||
            _selectionUiCameraSystem == null ||
            !TryGetGrid(out MatchHudMinimapGridModel grid))
        {
            return;
        }

        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        MatchHudMinimapProjectionGrid projectionGrid = _currentProjectionGrid.Width > 0f
            ? _currentProjectionGrid
            : _view != null && _view.UseFullMapProjection
                ? MatchHudMinimapProjectionGrid.FromGridModel(grid)
                : MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(
                    grid,
                    _selectionUiCameraSystem.WorldCamera,
                    ResolveMapAspect());
        Vector3 focusWorld = MatchHudMinimapProjectionSystem.ClampWorldToGrid(
            grid,
            MatchHudMinimapProjectionSystem.NormalizedToWorld(projectionGrid, normalized));
        _selectionUiCameraSystem.MoveCameraGroundCenterTo(focusWorld);
        if (!_view.IsDraggingViewport)
        {
            _view.ClearManualViewportOverride();
            _staticMapDirty = true;
            _nextStaticMapRetryTime = 0f;
        }
        Update();
    }

    private float ResolveMapAspect()
    {
        RectTransform mapRect = _view != null ? _view.MapRect : null;
        if (mapRect == null || mapRect.rect.height <= 0.001f)
            return 1f;

        return Mathf.Max(0.1f, mapRect.rect.width / mapRect.rect.height);
    }

    private MatchHudMinimapProjectionGrid ApplyMinimapZoom(MatchHudMinimapProjectionGrid projectionGrid)
    {
        if (!_minimapZoomedIn)
            return projectionGrid;

        float width = Mathf.Max(1f, projectionGrid.Width * MinimapZoomedInScale);
        float height = Mathf.Max(1f, projectionGrid.Height * MinimapZoomedInScale);
        Vector3 center = projectionGrid.Origin + new Vector3(projectionGrid.Width * 0.5f, 0f, projectionGrid.Height * 0.5f);
        return new MatchHudMinimapProjectionGrid(
            new Vector3(center.x - width * 0.5f, projectionGrid.Origin.y, center.z - height * 0.5f),
            width,
            height);
    }

    private bool ShouldRefreshCameraCenteredMap(
        MatchHudMinimapProjectionGrid desiredProjectionGrid,
        Camera worldCamera,
        bool cameraRefreshBlocked)
    {
        if (!_hasCapturedProjectionGrid)
            return true;
        if (_view != null && _view.UseFullMapProjection)
            return false;
        if (_view != null && _view.IsDraggingViewport)
            return false;
        if (cameraRefreshBlocked)
            return false;

        if (!MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(worldCamera, _capturedProjectionGrid, out Rect capturedViewport))
            return true;
        if (IsViewportNearRefreshEdge(capturedViewport))
            return true;

        float widthDelta = Mathf.Abs(desiredProjectionGrid.Width - _capturedProjectionGrid.Width);
        float heightDelta = Mathf.Abs(desiredProjectionGrid.Height - _capturedProjectionGrid.Height);
        return widthDelta >= Mathf.Max(1f, _capturedProjectionGrid.Width * CameraCenteredMapSizeRefreshFraction) ||
               heightDelta >= Mathf.Max(1f, _capturedProjectionGrid.Height * CameraCenteredMapSizeRefreshFraction);
    }

    private bool IsCameraRefreshBlocked()
    {
        if (_selectionUiCameraSystem != null && _selectionUiCameraSystem.IsCameraDragging)
            _cameraDragRefreshBlockedUntil = Time.unscaledTime + CameraDragRecaptureDebounceSeconds;

        return Time.unscaledTime < _cameraDragRefreshBlockedUntil;
    }

    private static bool IsViewportNearRefreshEdge(Rect viewport)
    {
        return viewport.xMin <= CameraViewportEdgeRefreshMargin ||
               viewport.yMin <= CameraViewportEdgeRefreshMargin ||
               viewport.xMax >= 1f - CameraViewportEdgeRefreshMargin ||
               viewport.yMax >= 1f - CameraViewportEdgeRefreshMargin;
    }

    private void HandleZoomHeldChanged(int direction, bool held)
    {
        if (!held || direction == 0)
            return;

        bool zoomedIn = direction > 0;
        if (_minimapZoomedIn == zoomedIn)
            return;

        _minimapZoomedIn = zoomedIn;
        _view?.ClearManualViewportOverride();
        if (_runtimeGameplayStateSystem != null)
            _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        _staticMapDirty = true;
        _markersDirty = true;
        _nextStaticMapRetryTime = 0f;
        Update();
    }

    private bool RenderStaticMap(
        MatchHudMinimapProjectionGrid requestedGrid,
        out MatchHudMinimapProjectionGrid renderedGrid)
    {
        renderedGrid = requestedGrid;
        Camera worldCamera = _selectionUiCameraSystem != null ? _selectionUiCameraSystem.WorldCamera : null;
        if (_view == null)
            return false;

        EnsureCaptureResources();
        bool captured = worldCamera != null && !Application.isBatchMode;

        bool readbackMatchesRenderTexture = false;
        if (captured)
        {
            CaptureMap(renderedGrid, ResolveCaptureMask());
            readbackMatchesRenderTexture = true;
        }

        if (!captured || IsFlatCapture())
        {
            DrawRasterMap(renderedGrid, out renderedGrid, !_minimapZoomedIn);
            readbackMatchesRenderTexture = false;
        }

        ApplyRenderedMapToView(readbackMatchesRenderTexture);
        return true;
    }

    private void ApplyRenderedMapToView(bool readbackMatchesRenderTexture)
    {
        if (_view == null || _view.MapImage == null)
            return;

        if (!readbackMatchesRenderTexture)
            ReadRenderTextureInto(_readbackTexture);

        if (_captureSprite == null)
        {
            _captureSprite = Sprite.Create(
                _readbackTexture,
                new Rect(0, 0, CaptureResolution, CaptureResolution),
                new Vector2(0.5f, 0.5f),
                100f);
            _captureSprite.name = "Runtime_MatchHudMinimapSprite";
        }

        _view.SetMapSprite(_captureSprite);
    }

    private void UpdateMarkersIfDue(MatchHudMinimapProjectionGrid grid)
    {
        if (!_markersDirty && Time.unscaledTime < _nextMarkerRefreshTime)
            return;

        _markersDirty = false;
        _nextMarkerRefreshTime = Time.unscaledTime + MarkerRefreshSeconds;
        UpdateMarkers(grid);
    }

    private void UpdateMarkers(MatchHudMinimapProjectionGrid grid)
    {
        if (_view == null || _view.MapRect == null)
            return;

        RectTransform markerParent = _view.MarkerRoot != null ? _view.MarkerRoot : _view.MapRect;
        if (markerParent == null || !TryGetRectInParentSpace(_view.MapRect, markerParent, _mapWorldCorners, out Rect mapRect))
            return;

        Vector2 parentTopLeft = new(markerParent.rect.xMin, markerParent.rect.yMax);
        int markerIndex = 0;
        _markerScratch.Clear();
        _minimapDataSource?.GetMarkers(_markerScratch);
        for (int i = 0; i < _markerScratch.Count && markerIndex < MaxMarkers; i++)
        {
            MatchHudMinimapMarkerModel marker = _markerScratch[i];
            if (!MatchHudMinimapProjectionSystem.TryWorldToNormalized(grid, marker.Position, out Vector2 normalized))
                continue;
            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
                continue;

            SetMarker(markerIndex, normalized, ResolveMarkerColor(marker.Allegiance), mapRect, parentTopLeft);
            markerIndex++;
        }

        for (int i = markerIndex; i < _markerPool.Count; i++)
        {
            GameObject markerObject = _markerPool[i].gameObject;
            if (markerObject.activeSelf)
                markerObject.SetActive(false);
        }
    }

    private void SetMarker(int index, Vector2 normalized, Color color, Rect mapRect, Vector2 parentTopLeft)
    {
        Image marker = EnsureMarker(index);
        if (marker == null)
            return;

        RectTransform rect = marker.rectTransform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 mapPoint = new(
            mapRect.xMin + mapRect.width * Mathf.Clamp01(normalized.x),
            mapRect.yMin + mapRect.height * Mathf.Clamp01(normalized.y));
        Vector2 anchoredPosition = new(
            mapPoint.x - parentTopLeft.x,
            mapPoint.y - parentTopLeft.y);
        if (!Approximately(rect.anchoredPosition.x, anchoredPosition.x) ||
            !Approximately(rect.anchoredPosition.y, anchoredPosition.y))
        {
            rect.anchoredPosition = anchoredPosition;
        }

        if (!ApproximatelyColor(marker.color.r, color.r) ||
            !ApproximatelyColor(marker.color.g, color.g) ||
            !ApproximatelyColor(marker.color.b, color.b) ||
            !ApproximatelyColor(marker.color.a, color.a))
        {
            marker.color = color;
        }

        if (!marker.gameObject.activeSelf)
            marker.gameObject.SetActive(true);
    }

    private Image EnsureMarker(int index)
    {
        while (_markerPool.Count <= index)
        {
            RectTransform parent = _view != null && _view.MarkerRoot != null ? _view.MarkerRoot : _view != null ? _view.MapRect : null;
            if (parent == null)
                return null;

            GameObject markerObject = new("MinimapMarker");
            markerObject.transform.SetParent(parent, false);
            markerObject.layer = parent.gameObject.layer;
            RectTransform rect = markerObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = markerObject.AddComponent<Image>();
            image.raycastTarget = false;
            Outline outline = markerObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            _markerPool.Add(image);
        }

        return _markerPool[index];
    }

    private bool TryGetGrid(out MatchHudMinimapGridModel grid)
    {
        if (_hasCachedGrid && _cachedGrid.IsValid)
        {
            grid = _cachedGrid;
            return true;
        }

        grid = default;
        if (_minimapDataSource == null || !_minimapDataSource.TryGetGrid(out grid) || !grid.IsValid)
            return false;

        _cachedGrid = grid;
        _hasCachedGrid = true;
        return true;
    }

    private static bool TryGetRectInParentSpace(RectTransform source, RectTransform parent, Vector3[] corners, out Rect rect)
    {
        rect = default;
        if (source == null || parent == null || corners == null || corners.Length < 4)
            return false;

        source.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = parent.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return rect.width > 0f && rect.height > 0f;
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.5f;
    }

    private static bool ApproximatelyColor(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.001f;
    }

    private static Color ResolveMarkerColor(MatchHudMinimapMarkerAllegiance allegiance)
    {
        return allegiance switch
        {
            MatchHudMinimapMarkerAllegiance.Player => PlayerMarkerColor,
            MatchHudMinimapMarkerAllegiance.Enemy => EnemyMarkerColor,
            _ => NeutralMarkerColor
        };
    }

    private void CaptureMap(MatchHudMinimapProjectionGrid grid, int cullingMask)
    {
        MatchHudMinimapProjectionSystem.ConfigureCaptureCamera(_captureCamera, grid, cullingMask);

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = _captureCamera.targetTexture;
        bool previousFog = RenderSettings.fog;
        try
        {
            RenderSettings.fog = false;
            _captureCamera.targetTexture = _renderTexture;
            _captureCamera.Render();
        }
        finally
        {
            RenderSettings.fog = previousFog;
            RenderTexture.active = previousActive;
            _captureCamera.targetTexture = previousTarget;
        }
    }

    private bool IsFlatCapture()
    {
        ReadRenderTextureInto(_readbackTexture);

        Color32[] pixels = _readbackTexture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
            return true;

        int step = Mathf.Max(1, pixels.Length / 4096);
        int min = 255;
        int max = 0;
        double sum = 0d;
        double sumSquares = 0d;
        int samples = 0;
        for (int i = 0; i < pixels.Length; i += step)
        {
            Color32 pixel = pixels[i];
            int luminance = (pixel.r + pixel.g + pixel.b) / 3;
            min = Mathf.Min(min, luminance);
            max = Mathf.Max(max, luminance);
            sum += luminance;
            sumSquares += luminance * luminance;
            samples++;
        }

        if (samples == 0)
            return true;

        float average = (float)(sum / samples);
        float variance = Mathf.Max(0f, (float)(sumSquares / samples) - average * average);
        float stdDev = Mathf.Sqrt(variance);
        int luminanceRange = max - min;
        return average < 8f ||
               average > 247f ||
               (luminanceRange < 3 && stdDev < 1f);
    }

    private void ReadRenderTextureInto(Texture2D texture)
    {
        if (texture == null || _renderTexture == null)
            return;

        RenderTexture previousActive = RenderTexture.active;
        try
        {
            RenderTexture.active = _renderTexture;
            texture.ReadPixels(new Rect(0, 0, CaptureResolution, CaptureResolution), 0, 0, false);
            texture.Apply(false);
        }
        finally
        {
            RenderTexture.active = previousActive;
        }
    }

    private void DrawRasterMap(
        MatchHudMinimapProjectionGrid requestedGrid,
        out MatchHudMinimapProjectionGrid renderedGrid,
        bool allowExpandedFallback)
    {
        renderedGrid = requestedGrid;
        if (_rasterPixels == null || _rasterPixels.Length != CaptureResolution * CaptureResolution)
            _rasterPixels = new Color32[CaptureResolution * CaptureResolution];

        int candidateCount = FillRasterProjectionCandidates(requestedGrid, allowExpandedFallback);
        for (int i = 0; i < candidateCount; i++)
        {
            MatchHudMinimapProjectionGrid candidate = _rasterProjectionCandidates[i];
            DrawRasterBase(_rasterPixels);
            DrawRasterGrid(_rasterPixels);
            int featureCount = DrawRasterSurfaceFeatures(candidate, _rasterPixels);
            featureCount += DrawRasterRoads(candidate, _rasterPixels);
            renderedGrid = candidate;
            if (featureCount >= MinRasterFeatureCount || i == candidateCount - 1)
                break;
        }

        _rasterTexture.SetPixels32(_rasterPixels);
        _rasterTexture.Apply(false, false);
        Graphics.Blit(_rasterTexture, _renderTexture);
    }

    private int FillRasterProjectionCandidates(
        MatchHudMinimapProjectionGrid requestedGrid,
        bool allowExpandedFallback)
    {
        if (!allowExpandedFallback)
        {
            _rasterProjectionCandidates[0] = requestedGrid;
            return 1;
        }

        MatchHudMinimapProjectionGrid fullGrid = TryGetGrid(out MatchHudMinimapGridModel grid)
            ? MatchHudMinimapProjectionGrid.FromGridModel(grid)
            : requestedGrid;
        Vector3 center = requestedGrid.Origin + new Vector3(requestedGrid.Width * 0.5f, 0f, requestedGrid.Height * 0.5f);
        float aspect = requestedGrid.Width / Mathf.Max(0.001f, requestedGrid.Height);
        _rasterProjectionCandidates[0] = requestedGrid;
        _rasterProjectionCandidates[1] = CreateExpandedRasterGrid(center, requestedGrid.Width * 2f, requestedGrid.Height * 2f, aspect, fullGrid);
        _rasterProjectionCandidates[2] = CreateExpandedRasterGrid(center, requestedGrid.Width * 4f, requestedGrid.Height * 4f, aspect, fullGrid);
        _rasterProjectionCandidates[3] = fullGrid;
        return 4;
    }

    private static MatchHudMinimapProjectionGrid CreateExpandedRasterGrid(
        Vector3 center,
        float width,
        float height,
        float aspect,
        MatchHudMinimapProjectionGrid fullGrid)
    {
        height = Mathf.Min(Mathf.Max(1f, height), fullGrid.Height);
        width = Mathf.Min(Mathf.Max(height * aspect, width), fullGrid.Width);
        return new MatchHudMinimapProjectionGrid(
            new Vector3(center.x - width * 0.5f, fullGrid.Origin.y, center.z - height * 0.5f),
            width,
            height);
    }

    private static void DrawRasterBase(Color32[] pixels)
    {
        for (int y = 0; y < CaptureResolution; y++)
        {
            for (int x = 0; x < CaptureResolution; x++)
            {
                float vertical = y / (float)(CaptureResolution - 1);
                int ripple = ((x * 17 + y * 31) & 15) - 8;
                int ridge = Mathf.RoundToInt(Mathf.Sin((x * 0.025f) + (y * 0.013f)) * 7f);
                pixels[x + y * CaptureResolution] = LerpColor(RasterBackgroundA, RasterBackgroundB, vertical, ripple + ridge);
            }
        }
    }

    private static Color32 LerpColor(Color32 a, Color32 b, float t, int offset)
    {
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)) + offset, 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)) + offset, 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)) + offset, 0, 255),
            255);
    }

    private static void DrawRasterGrid(Color32[] pixels)
    {
        const int step = 32;
        for (int x = 0; x < CaptureResolution; x += step)
            for (int y = 0; y < CaptureResolution; y++)
                BlendRasterPixel(pixels, x, y, RasterGrid, 0.28f);

        for (int y = 0; y < CaptureResolution; y += step)
            for (int x = 0; x < CaptureResolution; x++)
                BlendRasterPixel(pixels, x, y, RasterGrid, 0.28f);
    }

    private int DrawRasterRoads(MatchHudMinimapProjectionGrid projectionGrid, Color32[] pixels)
    {
        _roadScratch.Clear();
        _minimapDataSource?.GetRoadCells(ToAreaModel(projectionGrid), _roadScratch);
        if (_roadScratch.Count == 0)
            return 0;

        int featureCount = 0;
        for (int i = 0; i < _roadScratch.Count; i++)
        {
            MatchHudMinimapRoadCellModel road = _roadScratch[i];
            if (!MatchHudMinimapProjectionSystem.TryWorldToNormalized(projectionGrid, road.WorldPosition, out Vector2 normalized) ||
                normalized.x < 0f ||
                normalized.x > 1f ||
                normalized.y < 0f ||
                normalized.y > 1f)
            {
                continue;
            }

            int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int roadRadius = Mathf.Clamp(Mathf.RoundToInt((CaptureResolution / Mathf.Max(1f, projectionGrid.Height)) * road.CellSize * 1.3f), 1, 4);
            DrawRasterDot(pixels, pixelX, pixelY, ResolveRoadRasterColor(road.Kind), roadRadius);
            featureCount++;
        }

        return featureCount;
    }

    private int DrawRasterSurfaceFeatures(MatchHudMinimapProjectionGrid projectionGrid, Color32[] pixels)
    {
        _surfaceScratch.Clear();
        _minimapDataSource?.GetSurfaceFeatures(ToAreaModel(projectionGrid), _surfaceScratch);
        if (_surfaceScratch.Count == 0)
            return 0;

        int featureCount = 0;
        for (int i = 0; i < _surfaceScratch.Count; i++)
        {
            MatchHudMinimapSurfaceFeatureModel feature = _surfaceScratch[i];
            Color32 color = ResolveSurfaceRasterColor(feature.Kind);
            if (feature.FillArea)
            {
                DrawRasterAreaFeature(projectionGrid, feature, color, pixels);
                featureCount++;
                continue;
            }

            if (!MatchHudMinimapProjectionSystem.TryWorldToNormalized(projectionGrid, feature.Center, out Vector2 normalized) ||
                normalized.x < 0f ||
                normalized.x > 1f ||
                normalized.y < 0f ||
                normalized.y > 1f)
            {
                continue;
            }

            int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int radius = Mathf.Clamp(Mathf.RoundToInt((CaptureResolution / Mathf.Max(1f, projectionGrid.Height)) * feature.CellSize * 1.2f), 1, 3);
            DrawRasterDot(pixels, pixelX, pixelY, color, radius);
            featureCount++;
        }

        return featureCount;
    }

    private static MatchHudMinimapAreaModel ToAreaModel(MatchHudMinimapProjectionGrid projectionGrid)
    {
        return new MatchHudMinimapAreaModel(projectionGrid.Origin, projectionGrid.Width, projectionGrid.Height);
    }

    private static void DrawRasterAreaFeature(
        MatchHudMinimapProjectionGrid projectionGrid,
        MatchHudMinimapSurfaceFeatureModel feature,
        Color32 color,
        Color32[] pixels)
    {
        float minX = feature.Center.x - feature.HalfExtents.x;
        float maxX = feature.Center.x + feature.HalfExtents.x;
        float minZ = feature.Center.z - feature.HalfExtents.y;
        float maxZ = feature.Center.z + feature.HalfExtents.y;
        Rect projectionRect = new(projectionGrid.Origin.x, projectionGrid.Origin.z, projectionGrid.Width, projectionGrid.Height);
        Rect featureRect = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        if (!projectionRect.Overlaps(featureRect))
            return;

        int pixelMinX = Mathf.Clamp(Mathf.FloorToInt(((minX - projectionGrid.Origin.x) / projectionGrid.Width) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
        int pixelMaxX = Mathf.Clamp(Mathf.CeilToInt(((maxX - projectionGrid.Origin.x) / projectionGrid.Width) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
        int pixelMinY = Mathf.Clamp(Mathf.FloorToInt(((minZ - projectionGrid.Origin.z) / projectionGrid.Height) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
        int pixelMaxY = Mathf.Clamp(Mathf.CeilToInt(((maxZ - projectionGrid.Origin.z) / projectionGrid.Height) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
        for (int y = pixelMinY; y <= pixelMaxY; y++)
        {
            for (int x = pixelMinX; x <= pixelMaxX; x++)
                BlendRasterPixel(pixels, x, y, color, 0.58f);
        }
    }

    private static Color32 ResolveRoadRasterColor(MatchHudMinimapRoadKind kind)
    {
        return kind switch
        {
            MatchHudMinimapRoadKind.DirtRoad => RasterDirtRoad,
            MatchHudMinimapRoadKind.Sidewalk => RasterSidewalk,
            _ => RasterRoad
        };
    }

    private static Color32 ResolveSurfaceRasterColor(MatchHudMinimapSurfaceFeatureKind kind)
    {
        return kind switch
        {
            MatchHudMinimapSurfaceFeatureKind.Blocked => RasterBlocked,
            MatchHudMinimapSurfaceFeatureKind.Bridge => RasterBridge,
            MatchHudMinimapSurfaceFeatureKind.Ramp => RasterRamp,
            MatchHudMinimapSurfaceFeatureKind.Highway => RasterSidewalk,
            MatchHudMinimapSurfaceFeatureKind.DirtRoad => RasterDirtRoad,
            MatchHudMinimapSurfaceFeatureKind.Plaza => RasterPlaza,
            _ => RasterRoad
        };
    }

    private static void DrawRasterDot(Color32[] pixels, int centerX, int centerY, Color32 color, int radius)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                    BlendRasterPixel(pixels, centerX + x, centerY + y, color, 0.9f);
            }
        }
    }

    private static void BlendRasterPixel(Color32[] pixels, int x, int y, Color32 color, float amount)
    {
        if ((uint)x >= CaptureResolution || (uint)y >= CaptureResolution)
            return;

        int index = x + y * CaptureResolution;
        Color32 current = pixels[index];
        pixels[index] = new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.r, color.r, amount)), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.g, color.g, amount)), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.b, color.b, amount)), 0, 255),
            255);
    }

    private void EnsureCaptureResources()
    {
        if (_renderTexture == null)
        {
            _renderTexture = new RenderTexture(CaptureResolution, CaptureResolution, 16, RenderTextureFormat.ARGB32)
            {
                name = "Runtime_MatchHudMinimapRenderTexture",
                useMipMap = false,
                autoGenerateMips = false
            };
            _renderTexture.Create();
        }

        if (_readbackTexture == null)
        {
            _readbackTexture = new Texture2D(CaptureResolution, CaptureResolution, TextureFormat.RGBA32, false)
            {
                name = "Runtime_MatchHudMinimapReadbackTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        if (_rasterTexture == null)
        {
            _rasterTexture = new Texture2D(CaptureResolution, CaptureResolution, TextureFormat.RGBA32, false)
            {
                name = "Runtime_MatchHudMinimapRasterTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        if (_captureCamera == null)
        {
            GameObject cameraObject = new("Runtime_MatchHudMinimapCaptureCamera");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(cameraObject);
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _captureCamera = cameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
        }
    }

    private void ReleaseCaptureResources()
    {
        if (_captureSprite != null)
            DestroyRuntimeObject(_captureSprite);
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            DestroyRuntimeObject(_renderTexture);
        }
        if (_readbackTexture != null)
            DestroyRuntimeObject(_readbackTexture);
        if (_rasterTexture != null)
            DestroyRuntimeObject(_rasterTexture);
        if (_captureCamera != null)
            DestroyRuntimeObject(_captureCamera.gameObject);

        _renderTexture = null;
        _readbackTexture = null;
        _rasterTexture = null;
        _captureSprite = null;
        _rasterPixels = null;
        _captureCamera = null;
    }

    private static void DestroyRuntimeObject(Object value)
    {
        if (value == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(value);
        else
            Object.DestroyImmediate(value);
    }

    private static int ResolveCaptureMask()
    {
        return RemoveUiLayer(~0);
    }

    private static int RemoveUiLayer(int mask)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            mask &= ~(1 << uiLayer);

        return mask;
    }

}
