using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.Transforms;

public sealed class MatchHudMinimapInputSystem
{
    private const int CaptureResolution = 1024;
    private static readonly Color32 RasterBackground = new(12, 20, 22, 255);
    private static readonly Color32 RasterGrid = new(28, 39, 42, 255);
    private static readonly Color32 RasterRoad = new(128, 109, 68, 255);
    private static readonly Color PlayerMarkerColor = new(0.2f, 0.95f, 0.62f, 0.95f);
    private static readonly Color EnemyMarkerColor = new(1f, 0.22f, 0.18f, 0.95f);
    private static readonly Color NeutralMarkerColor = new(0.82f, 0.88f, 0.9f, 0.9f);
    private const int MaxMarkers = 256;
    private const float StaticMapRetrySeconds = 0.75f;
    private const float CameraCenteredMapRefreshSeconds = 0.6f;
    private const float CameraCenteredMapRefreshFraction = 0.18f;
    private const float CameraCenteredMapSizeRefreshFraction = 0.05f;

    private MatchHudMinimapView _view;
    private RuntimeGameplayStateSystem _runtimeGameplayStateSystem;
    private SelectionUiCameraSystem _selectionUiCameraSystem;
    private readonly System.Collections.Generic.List<Image> _markerPool = new();
    private Camera _captureCamera;
    private RenderTexture _renderTexture;
    private Texture2D _captureTexture;
    private Sprite _captureSprite;
    private bool _staticMapDirty = true;
    private float _nextStaticMapRetryTime;
    private bool _hasCapturedProjectionGrid;
    private MatchHudMinimapProjectionGrid _capturedProjectionGrid;
    private MatchHudMinimapProjectionGrid _currentProjectionGrid;

    public void Bind(
        MatchHudMinimapView view,
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        SelectionUiCameraSystem selectionUiCameraSystem)
    {
        if (_view == view)
            return;

        Unbind();
        _view = view;
        _runtimeGameplayStateSystem = runtimeGameplayStateSystem;
        _selectionUiCameraSystem = selectionUiCameraSystem;

        if (_view == null)
            return;

        _view.FocusRequested += HandleFocusRequested;
        _view.ZoomHeldChanged += HandleZoomHeldChanged;
        _staticMapDirty = true;
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
    }

    public void Dispose()
    {
        Unbind();
        ReleaseCaptureResources();
    }

    public void NotifyStaticMapChanged()
    {
        _staticMapDirty = true;
    }

    public void Update()
    {
        if (_view == null || !MatchHudMinimapProjectionSystem.TryGetGrid(out GridConfig grid))
            return;

        Camera worldCamera = _selectionUiCameraSystem != null ? _selectionUiCameraSystem.WorldCamera : null;
        MatchHudMinimapProjectionGrid projectionGrid = MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(
            grid,
            worldCamera,
            ResolveMapAspect());
        _currentProjectionGrid = projectionGrid;
        if (ShouldRefreshCameraCenteredMap(projectionGrid))
            _staticMapDirty = true;

        if (_staticMapDirty)
        {
            if (Time.unscaledTime >= _nextStaticMapRetryTime)
            {
                _staticMapDirty = !RenderStaticMap(projectionGrid, out MatchHudMinimapProjectionGrid renderedProjectionGrid, out bool usedFallback);
                projectionGrid = renderedProjectionGrid;
                _currentProjectionGrid = renderedProjectionGrid;
                _capturedProjectionGrid = renderedProjectionGrid;
                _hasCapturedProjectionGrid = true;
                if (_staticMapDirty)
                {
                    _nextStaticMapRetryTime = Time.unscaledTime + StaticMapRetrySeconds;
                }
                else
                {
                    _nextStaticMapRetryTime = Time.unscaledTime + (usedFallback ? StaticMapRetrySeconds : CameraCenteredMapRefreshSeconds);
                }
            }
            else if (_hasCapturedProjectionGrid)
            {
                projectionGrid = _capturedProjectionGrid;
                _currentProjectionGrid = _capturedProjectionGrid;
            }
        }

        if (MatchHudMinimapProjectionSystem.TryGetCameraViewportRect(worldCamera, projectionGrid, out Rect viewport))
            _view.SetViewportNormalizedRect(viewport);

        UpdateMarkers(projectionGrid);
        UpdateHeldZoom();
    }

    private void HandleFocusRequested(Vector2 normalized)
    {
        if (_runtimeGameplayStateSystem == null ||
            _selectionUiCameraSystem == null ||
            !MatchHudMinimapProjectionSystem.TryGetGrid(out GridConfig grid))
        {
            return;
        }

        _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
        MatchHudMinimapProjectionGrid projectionGrid = _currentProjectionGrid.Width > 0f
            ? _currentProjectionGrid
            : MatchHudMinimapProjectionSystem.CreateCameraCenteredGrid(
                grid,
                _selectionUiCameraSystem.WorldCamera,
                ResolveMapAspect());
        Vector3 focusWorld = MatchHudMinimapProjectionSystem.ClampWorldToGrid(
            grid,
            MatchHudMinimapProjectionSystem.NormalizedToWorld(projectionGrid, normalized));
        _selectionUiCameraSystem.MoveCameraGroundCenterTo(focusWorld);
        _staticMapDirty = true;
        Update();
    }

    private float ResolveMapAspect()
    {
        RectTransform mapRect = _view != null ? _view.MapRect : null;
        if (mapRect == null || mapRect.rect.height <= 0.001f)
            return 1f;

        return Mathf.Max(0.1f, mapRect.rect.width / mapRect.rect.height);
    }

    private bool ShouldRefreshCameraCenteredMap(MatchHudMinimapProjectionGrid projectionGrid)
    {
        if (!_hasCapturedProjectionGrid)
            return true;

        if (Time.unscaledTime < _nextStaticMapRetryTime)
            return false;

        Vector2 capturedCenter = new(
            _capturedProjectionGrid.Origin.x + _capturedProjectionGrid.Width * 0.5f,
            _capturedProjectionGrid.Origin.z + _capturedProjectionGrid.Height * 0.5f);
        Vector2 currentCenter = new(
            projectionGrid.Origin.x + projectionGrid.Width * 0.5f,
            projectionGrid.Origin.z + projectionGrid.Height * 0.5f);
        float centerRefreshDistance = Mathf.Max(4f, Mathf.Min(projectionGrid.Width, projectionGrid.Height) * CameraCenteredMapRefreshFraction);
        if ((currentCenter - capturedCenter).sqrMagnitude >= centerRefreshDistance * centerRefreshDistance)
            return true;

        float widthDelta = Mathf.Abs(projectionGrid.Width - _capturedProjectionGrid.Width);
        float heightDelta = Mathf.Abs(projectionGrid.Height - _capturedProjectionGrid.Height);
        return widthDelta >= Mathf.Max(1f, _capturedProjectionGrid.Width * CameraCenteredMapSizeRefreshFraction) ||
               heightDelta >= Mathf.Max(1f, _capturedProjectionGrid.Height * CameraCenteredMapSizeRefreshFraction);
    }

    private void HandleZoomHeldChanged(int direction, bool held)
    {
        if (_runtimeGameplayStateSystem == null)
            return;

        if (direction > 0)
            _runtimeGameplayStateSystem.ZoomInHeld = held;
        else if (direction < 0)
            _runtimeGameplayStateSystem.ZoomOutHeld = held;

        if (held)
        {
            _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            ApplyZoomStep(direction);
        }
    }

    private bool RenderStaticMap(
        MatchHudMinimapProjectionGrid requestedGrid,
        out MatchHudMinimapProjectionGrid renderedGrid,
        out bool usedFallback)
    {
        renderedGrid = requestedGrid;
        usedFallback = false;
        Camera worldCamera = _selectionUiCameraSystem != null ? _selectionUiCameraSystem.WorldCamera : null;
        if (_view == null || _view.MapImage == null)
            return false;

        EnsureCaptureResources();
        bool captured = false;
        if (worldCamera != null)
        {
            int cullingMask = ResolveCaptureMask(worldCamera);
            CaptureMap(renderedGrid, cullingMask);
            captured = true;

            if (IsBlankOrWhiteCapture(_captureTexture))
            {
                int expandedMask = ResolveExpandedCaptureMask();
                if (expandedMask != cullingMask)
                    CaptureMap(renderedGrid, expandedMask);
            }

        }

        bool usingFallback = !captured || IsBlankOrWhiteCapture(_captureTexture);
        if (usingFallback)
        {
            usedFallback = true;
            // Keep the live HUD useful even when the render camera has no eligible static layers yet.
            if (MatchHudMinimapProjectionSystem.TryGetGridRoads(out GridConfig roadGrid, out DynamicBuffer<GridRoad> roads))
                DrawRasterMap(renderedGrid, roadGrid, roads, _captureTexture);
            else
                DrawRasterMap(renderedGrid, _captureTexture);
        }

        _captureTexture.Apply(false, false);

        if (_captureSprite == null)
        {
            _captureSprite = Sprite.Create(
                _captureTexture,
                new Rect(0, 0, CaptureResolution, CaptureResolution),
                new Vector2(0.5f, 0.5f),
                100f);
            _captureSprite.name = "Runtime_MatchHudMinimapSprite";
        }

        _view.SetMapSprite(_captureSprite);
        return true;
    }

    private void ApplyZoomStep(int direction)
    {
        if (direction == 0 ||
            _selectionUiCameraSystem == null ||
            _selectionUiCameraSystem.WorldCamera == null)
        {
            return;
        }

        _selectionUiCameraSystem.ZoomPerspective(Mathf.Sign(direction), Time.unscaledDeltaTime);
    }

    private void UpdateHeldZoom()
    {
        if (_runtimeGameplayStateSystem == null)
            return;

        if (_runtimeGameplayStateSystem.ZoomInHeld)
            ApplyZoomStep(1);
        if (_runtimeGameplayStateSystem.ZoomOutHeld)
            ApplyZoomStep(-1);
    }

    private void UpdateMarkers(MatchHudMinimapProjectionGrid grid)
    {
        if (_view == null || _view.MapRect == null)
            return;

        int markerIndex = 0;
        if (TryGetDefaultEntityManager(out EntityManager em))
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>());
            if (!query.IsEmptyIgnoreFilter)
            {
                using Unity.Collections.NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length && markerIndex < MaxMarkers; i++)
                {
                    Entity entity = entities[i];
                    UnitHealth health = em.GetComponentData<UnitHealth>(entity);
                    if (health.Current <= 0)
                        continue;

                    LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                    if (!MatchHudMinimapProjectionSystem.TryWorldToNormalized(grid, transform.Position, out Vector2 normalized))
                        continue;
                    if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
                        continue;

                    Faction faction = em.GetComponentData<Faction>(entity);
                    SetMarker(markerIndex, normalized, ResolveMarkerColor(faction.Id));
                    markerIndex++;
                }
            }
        }

        for (int i = markerIndex; i < _markerPool.Count; i++)
            _markerPool[i].gameObject.SetActive(false);
    }

    private void SetMarker(int index, Vector2 normalized, Color color)
    {
        Image marker = EnsureMarker(index);
        if (marker == null)
            return;

        RectTransform rect = marker.rectTransform;
        RectTransform parent = rect.parent as RectTransform;
        if (!TryGetRectInParentSpace(_view.MapRect, parent, out Rect mapRect))
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 parentTopLeft = new(parent.rect.xMin, parent.rect.yMax);
        Vector2 mapPoint = new(
            mapRect.xMin + mapRect.width * Mathf.Clamp01(normalized.x),
            mapRect.yMin + mapRect.height * Mathf.Clamp01(normalized.y));
        rect.anchoredPosition = new Vector2(
            mapPoint.x - parentTopLeft.x,
            mapPoint.y - parentTopLeft.y);
        marker.color = color;
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
            rect.sizeDelta = new Vector2(7f, 7f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = markerObject.AddComponent<Image>();
            image.raycastTarget = false;
            _markerPool.Add(image);
        }

        return _markerPool[index];
    }

    private static bool TryGetRectInParentSpace(RectTransform source, RectTransform parent, out Rect rect)
    {
        rect = default;
        if (source == null || parent == null)
            return false;

        Vector3[] corners = new Vector3[4];
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

    private static Color ResolveMarkerColor(byte factionId)
    {
        if (FactionIdentitySystem.IsPlayerControlled(factionId))
            return PlayerMarkerColor;
        if (FactionIdentitySystem.IsHostileToPlayer(factionId))
            return EnemyMarkerColor;
        return NeutralMarkerColor;
    }

    private void CaptureMap(MatchHudMinimapProjectionGrid grid, int cullingMask)
    {
        MatchHudMinimapProjectionSystem.ConfigureCaptureCamera(_captureCamera, grid, cullingMask);

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = _captureCamera.targetTexture;
        _captureCamera.targetTexture = _renderTexture;
        _captureCamera.Render();
        RenderTexture.active = _renderTexture;
        _captureTexture.ReadPixels(new Rect(0, 0, CaptureResolution, CaptureResolution), 0, 0, false);
        RenderTexture.active = previousActive;
        _captureCamera.targetTexture = previousTarget;
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

        if (_captureTexture == null)
        {
            _captureTexture = new Texture2D(CaptureResolution, CaptureResolution, TextureFormat.RGBA32, false)
            {
                name = "Runtime_MatchHudMinimapTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        if (_captureCamera == null)
        {
            GameObject cameraObject = new("Runtime_MatchHudMinimapCaptureCamera");
            Object.DontDestroyOnLoad(cameraObject);
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _captureCamera = cameraObject.AddComponent<Camera>();
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
    }

    private void ReleaseCaptureResources()
    {
        if (_captureSprite != null)
            Object.Destroy(_captureSprite);
        if (_captureTexture != null)
            Object.Destroy(_captureTexture);
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Object.Destroy(_renderTexture);
        }
        if (_captureCamera != null)
            Object.Destroy(_captureCamera.gameObject);

        _captureSprite = null;
        _captureTexture = null;
        _renderTexture = null;
        _captureCamera = null;
    }

    private static int ResolveCaptureMask(Camera worldCamera)
    {
        int mask = worldCamera != null && worldCamera.cullingMask != 0 ? worldCamera.cullingMask : ~0;
        return RemoveUiLayer(mask);
    }

    private static int ResolveExpandedCaptureMask()
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

    private static bool IsBlankOrWhiteCapture(Texture2D texture)
    {
        if (texture == null)
            return true;

        Color32[] pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
            return true;

        int step = Mathf.Max(1, pixels.Length / 512);
        int samples = 0;
        int brightSamples = 0;
        int darkSamples = 0;
        int minLuminance = 255;
        int maxLuminance = 0;
        for (int i = 0; i < pixels.Length; i += step)
        {
            Color32 pixel = pixels[i];
            int luminance = (pixel.r + pixel.g + pixel.b) / 3;
            minLuminance = Mathf.Min(minLuminance, luminance);
            maxLuminance = Mathf.Max(maxLuminance, luminance);
            if (luminance > 235)
                brightSamples++;
            if (luminance < 20)
                darkSamples++;
            samples++;
        }

        if (samples == 0)
            return true;

        int luminanceRange = maxLuminance - minLuminance;
        return brightSamples > samples * 0.9f ||
               luminanceRange < 8;
    }

    private static void DrawRasterMap(
        MatchHudMinimapProjectionGrid projectionGrid,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length != CaptureResolution * CaptureResolution)
            pixels = new Color32[CaptureResolution * CaptureResolution];

        DrawRasterBase(projectionGrid, grid.CellSize, pixels);
        DrawRasterRoads(projectionGrid, grid, roads, pixels);
        texture.SetPixels32(pixels);
    }

    private static void DrawRasterMap(MatchHudMinimapProjectionGrid projectionGrid, Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length != CaptureResolution * CaptureResolution)
            pixels = new Color32[CaptureResolution * CaptureResolution];

        DrawRasterBase(projectionGrid, 10f, pixels);
        texture.SetPixels32(pixels);
    }

    private static void DrawRasterBase(MatchHudMinimapProjectionGrid projectionGrid, float cellSize, Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = RasterBackground;

        int widthCells = Mathf.Max(1, Mathf.RoundToInt(projectionGrid.Width / Mathf.Max(0.001f, cellSize)));
        int heightCells = Mathf.Max(1, Mathf.RoundToInt(projectionGrid.Height / Mathf.Max(0.001f, cellSize)));
        DrawRasterGrid(pixels, widthCells, heightCells);
    }

    private static void DrawRasterGrid(Color32[] pixels, int gridWidth, int gridHeight)
    {
        int verticalStride = Mathf.Max(1, CaptureResolution / Mathf.Min(gridWidth, 64));
        int horizontalStride = Mathf.Max(1, CaptureResolution / Mathf.Min(gridHeight, 64));
        for (int x = 0; x < CaptureResolution; x += verticalStride)
        {
            for (int y = 0; y < CaptureResolution; y++)
                SetRasterPixel(pixels, x, y, RasterGrid);
        }

        for (int y = 0; y < CaptureResolution; y += horizontalStride)
        {
            for (int x = 0; x < CaptureResolution; x++)
                SetRasterPixel(pixels, x, y, RasterGrid);
        }
    }

    private static void DrawRasterRoads(
        MatchHudMinimapProjectionGrid projectionGrid,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        Color32[] pixels)
    {
        if (grid.Width <= 0 || grid.Height <= 0)
            return;

        int expected = grid.Width * grid.Height;
        int count = Mathf.Min(expected, roads.Length);
        for (int index = 0; index < count; index++)
        {
            if (roads[index].Value == 0)
                continue;

            int cellX = index % grid.Width;
            int cellY = index / grid.Width;
            Vector3 worldPosition = new(
                grid.Origin.x + (cellX + 0.5f) * grid.CellSize,
                grid.Origin.y,
                grid.Origin.z + (cellY + 0.5f) * grid.CellSize);
            if (!MatchHudMinimapProjectionSystem.TryWorldToNormalized(projectionGrid, worldPosition, out Vector2 normalized) ||
                normalized.x < 0f ||
                normalized.x > 1f ||
                normalized.y < 0f ||
                normalized.y > 1f)
            {
                continue;
            }

            int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            DrawRasterDot(pixels, pixelX, pixelY, RasterRoad, 3);
        }
    }

    private static void DrawRasterDot(Color32[] pixels, int centerX, int centerY, Color32 color, int radius)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                    SetRasterPixel(pixels, centerX + x, centerY + y, color);
            }
        }
    }

    private static void SetRasterPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if ((uint)x >= CaptureResolution || (uint)y >= CaptureResolution)
            return;

        pixels[x + y * CaptureResolution] = color;
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
