using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudMinimapView : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    internal const float ViewportDragHitPadding = 18f;
    private const string DragLogTag = "[FullMapViewportDrag]";

    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private RectTransform markerRoot;

    private bool _draggingViewport;
    private bool _dragMoved;
    private bool _showViewport = true;
    private bool _allowViewportDrag = true;
    private bool _allowMapFocus = true;
    private bool _allowZoom = true;
    private bool _openFullMapOnClick;
    private bool _hasManualViewportOverride;
    private bool _hasLastViewportLayout;
    private Rect _manualViewportNormalizedRect;
    private Vector2 _lastViewportAnchoredPosition;
    private Vector2 _lastViewportSize;
    private Vector2 _viewportDragOffset;
    private readonly Vector3[] _worldCorners = new Vector3[4];
    private Canvas _cachedCanvas;

    public Image MapImage => mapImage;
    public RectTransform MapRect => mapRect != null ? mapRect : mapImage != null ? mapImage.rectTransform : null;
    public RectTransform ViewportRect => viewportRect;
    public Button ZoomInButton => zoomInButton;
    public Button ZoomOutButton => zoomOutButton;
    public RectTransform MarkerRoot => markerRoot;
    public bool IsDraggingViewport => _draggingViewport;
    public bool HasManualViewportOverride => _hasManualViewportOverride;
    public Rect ManualViewportNormalizedRect => _manualViewportNormalizedRect;
    public bool UseFullMapProjection { get; private set; }
    public bool ShowsViewport => _showViewport;

    public event Action<Vector2> FocusRequested;
    public event Action<int, bool> ZoomHeldChanged;
    public event Action FullMapOpenRequested;

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        Camera eventCamera = ResolveEventCamera();
        RectTransform rect = MapRect;
        if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera))
            return true;

        return _allowZoom &&
               (ContainsButton(zoomInButton, screenPosition, eventCamera) ||
                ContainsButton(zoomOutButton, screenPosition, eventCamera));
    }

    private void Awake()
    {
        EnsureZoomRelay(zoomInButton, 1);
        EnsureZoomRelay(zoomOutButton, -1);
        EnsureViewportDragRelay();
        if (mapImage != null)
            mapImage.raycastTarget = true;
    }

    private void OnDisable()
    {
        ZoomHeldChanged?.Invoke(1, false);
        ZoomHeldChanged?.Invoke(-1, false);
        _draggingViewport = false;
        _dragMoved = false;
        _hasManualViewportOverride = false;
        _hasLastViewportLayout = false;
    }

    private void OnTransformParentChanged()
    {
        _cachedCanvas = null;
    }

    public void Configure(
        Image image,
        RectTransform rect,
        RectTransform viewport,
        Button zoomIn,
        Button zoomOut,
        RectTransform markers)
    {
        mapImage = image;
        mapRect = rect;
        viewportRect = viewport;
        zoomInButton = zoomIn;
        zoomOutButton = zoomOut;
        markerRoot = markers;
        Awake();
    }

    public void SetMapSprite(Sprite sprite)
    {
        if (mapImage == null)
            return;

        mapImage.sprite = sprite;
        mapImage.preserveAspect = false;
        mapImage.enabled = true;
        mapImage.raycastTarget = true;
    }

    public void SetProjectionMode(bool useFullMapProjection)
    {
        UseFullMapProjection = useFullMapProjection;
        _hasManualViewportOverride = false;
        _hasLastViewportLayout = false;
    }

    public void ApplyInteractionOptions(
        bool useFullMapProjection,
        bool showViewport,
        bool allowViewportDrag,
        bool allowMapFocus,
        bool allowZoom,
        bool openFullMapOnClick)
    {
        SetProjectionMode(useFullMapProjection);
        _showViewport = showViewport;
        _allowViewportDrag = allowViewportDrag;
        _allowMapFocus = allowMapFocus;
        _allowZoom = allowZoom;
        _openFullMapOnClick = openFullMapOnClick;
        _draggingViewport = false;
        _dragMoved = false;

        if (viewportRect != null)
        {
            viewportRect.gameObject.SetActive(showViewport);
            EnsureViewportDragRelay();
        }
        SetZoomVisible(zoomInButton, allowZoom);
        SetZoomVisible(zoomOutButton, allowZoom);
    }

    public void SetViewportNormalizedRect(Rect normalizedRect)
    {
        RectTransform rectTransform = MapRect;
        if (!_showViewport || viewportRect == null || rectTransform == null)
            return;

        if (!TryGetMapRectInViewportParent(rectTransform, out Rect map))
            map = rectTransform.rect;

        float normalizedWidth = Mathf.Clamp01(normalizedRect.width);
        float normalizedHeight = Mathf.Clamp01(normalizedRect.height);
        float normalizedLeft = Mathf.Clamp(Mathf.Clamp01(normalizedRect.xMin), 0f, Mathf.Max(0f, 1f - normalizedWidth));
        float normalizedBottom = Mathf.Clamp(Mathf.Clamp01(normalizedRect.yMin), 0f, Mathf.Max(0f, 1f - normalizedHeight));
        float normalizedTop = normalizedBottom + normalizedHeight;
        float width = Mathf.Max(6f, map.width * normalizedWidth);
        float height = Mathf.Max(6f, map.height * normalizedHeight);
        float left = map.xMin + map.width * normalizedLeft;
        float top = map.yMin + map.height * normalizedTop;
        RectTransform parent = viewportRect.parent as RectTransform;
        Vector2 parentTopLeft = parent != null
            ? new Vector2(parent.rect.xMin, parent.rect.yMax)
            : Vector2.zero;

        Vector2 anchoredPosition = new(left - parentTopLeft.x, top - parentTopLeft.y);
        Vector2 size = new(width, height);
        if (_hasLastViewportLayout &&
            Approximately(_lastViewportAnchoredPosition.x, anchoredPosition.x) &&
            Approximately(_lastViewportAnchoredPosition.y, anchoredPosition.y) &&
            Approximately(_lastViewportSize.x, size.x) &&
            Approximately(_lastViewportSize.y, size.y))
        {
            return;
        }

        viewportRect.anchorMin = new Vector2(0f, 1f);
        viewportRect.anchorMax = new Vector2(0f, 1f);
        viewportRect.pivot = new Vector2(0f, 1f);
        viewportRect.anchoredPosition = anchoredPosition;
        viewportRect.sizeDelta = size;
        _lastViewportAnchoredPosition = anchoredPosition;
        _lastViewportSize = size;
        _hasLastViewportLayout = true;
    }

    private bool TryGetMapRectInViewportParent(RectTransform map, out Rect parentRect)
    {
        parentRect = default;
        RectTransform parent = viewportRect != null ? viewportRect.parent as RectTransform : null;
        if (parent == null)
            return false;

        map.GetWorldCorners(_worldCorners);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(_worldCorners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (!IsFinite(min.x) || !IsFinite(min.y) ||
            !IsFinite(max.x) || !IsFinite(max.y) ||
            max.x <= min.x ||
            max.y <= min.y)
        {
            return false;
        }

        parentRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private Camera ResolveEventCamera()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private Canvas ResolveCanvas()
    {
        if (_cachedCanvas == null)
            _cachedCanvas = GetComponentInParent<Canvas>();
        return _cachedCanvas;
    }

    private static bool ContainsButton(Button button, Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        return rect != null &&
               button.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.5f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        TryBeginViewportDrag(eventData.position, eventData.pressEventCamera, "view");
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (eventData != null)
            eventData.useDragThreshold = false;
        Debug.Log($"{DragLogTag} initializePotentialDrag frame={Time.frameCount} view={name} pos={FormatPointerPosition(eventData)} useDragThreshold={(eventData != null ? eventData.useDragThreshold.ToString() : "null")}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        DragViewport(eventData.position, eventData.pressEventCamera, "view");
    }

    internal bool TryBeginViewportDrag(Vector2 screenPosition, Camera eventCamera, string source)
    {
        _dragMoved = false;
        _draggingViewport = false;
        bool containsViewport = ContainsViewportDragPoint(screenPosition, eventCamera);
        Debug.Log($"{DragLogTag} {source}PointerDown frame={Time.frameCount} view={name} pos={FormatVector2(screenPosition)} allow={_allowViewportDrag} show={_showViewport} viewportExists={viewportRect != null} viewportActive={IsViewportActive()} containsViewport={containsViewport}");

        if (!_allowViewportDrag ||
            viewportRect == null ||
            !viewportRect.gameObject.activeInHierarchy ||
            !containsViewport)
        {
            Debug.Log($"{DragLogTag} {source}PointerDownRejected frame={Time.frameCount} reason={ResolvePointerDownRejectReason(containsViewport)}");
            return false;
        }

        if (TryGetPointerLocalPointInViewportParent(screenPosition, eventCamera, out Vector2 pointerInParent) &&
            TryGetRectInParentSpace(viewportRect, viewportRect.parent as RectTransform, out Rect viewportInParent))
        {
            _viewportDragOffset = new Vector2(
                Mathf.Clamp(pointerInParent.x - viewportInParent.xMin, 0f, viewportInParent.width),
                Mathf.Clamp(viewportInParent.yMax - pointerInParent.y, 0f, viewportInParent.height));
            Debug.Log($"{DragLogTag} {source}PointerDownAccepted frame={Time.frameCount} pointerInParent={FormatVector2(pointerInParent)} viewportInParent={FormatRect(viewportInParent)} dragOffset={FormatVector2(_viewportDragOffset)}");
        }
        else
        {
            Debug.Log($"{DragLogTag} {source}PointerDownAccepted frame={Time.frameCount} offsetUnavailable=true");
        }

        _draggingViewport = true;
        return true;
    }

    internal bool DragViewport(Vector2 screenPosition, Camera eventCamera, string source)
    {
        if (!_allowViewportDrag)
        {
            Debug.Log($"{DragLogTag} {source}DragRejected frame={Time.frameCount} reason=ViewportDragDisabled pos={FormatVector2(screenPosition)}");
            return false;
        }

        if (!_draggingViewport)
        {
            Debug.Log($"{DragLogTag} {source}DragRejected frame={Time.frameCount} reason=NotDraggingViewport pos={FormatVector2(screenPosition)}");
            return false;
        }

        if (!TryGetPointerLocalPointInViewportParent(screenPosition, eventCamera, out Vector2 pointerInParent))
        {
            Debug.Log($"{DragLogTag} {source}DragRejected frame={Time.frameCount} reason=PointerParentPointFailed pos={FormatVector2(screenPosition)}");
            return false;
        }

        if (!TryGetMapRectInViewportParent(MapRect, out Rect map))
        {
            Debug.Log($"{DragLogTag} {source}DragRejected frame={Time.frameCount} reason=MapRectInViewportParentFailed pos={FormatVector2(screenPosition)}");
            return false;
        }

        if (!TryGetRectInParentSpace(viewportRect, viewportRect.parent as RectTransform, out Rect viewportInParent))
        {
            Debug.Log($"{DragLogTag} {source}DragRejected frame={Time.frameCount} reason=ViewportRectInParentFailed pos={FormatVector2(screenPosition)}");
            return false;
        }

        _dragMoved = true;
        float rectWidth = Mathf.Max(6f, viewportInParent.width);
        float rectHeight = Mathf.Max(6f, viewportInParent.height);
        float localX = pointerInParent.x - map.xMin;
        float localY = map.yMax - pointerInParent.y;
        float left = Mathf.Clamp(localX - _viewportDragOffset.x, 0f, Mathf.Max(0f, map.width - rectWidth));
        float top = Mathf.Clamp(localY - _viewportDragOffset.y, 0f, Mathf.Max(0f, map.height - rectHeight));

        float normalizedX = (left + (rectWidth * 0.5f)) / Mathf.Max(0.001f, map.width);
        float normalizedY = 1f - ((top + (rectHeight * 0.5f)) / Mathf.Max(0.001f, map.height));
        float normalizedWidth = rectWidth / Mathf.Max(0.001f, map.width);
        float normalizedHeight = rectHeight / Mathf.Max(0.001f, map.height);
        Vector2 normalizedCenter = new(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
        _manualViewportNormalizedRect = new Rect(
            normalizedCenter.x - normalizedWidth * 0.5f,
            normalizedCenter.y - normalizedHeight * 0.5f,
            normalizedWidth,
            normalizedHeight);
        _hasManualViewportOverride = true;
        SetViewportNormalizedRect(_manualViewportNormalizedRect);
        Debug.Log($"{DragLogTag} {source}DragAccepted frame={Time.frameCount} pos={FormatVector2(screenPosition)} pointerInParent={FormatVector2(pointerInParent)} map={FormatRect(map)} viewport={FormatRect(viewportInParent)} normalizedCenter={FormatVector2(normalizedCenter)} manualRect={FormatRect(_manualViewportNormalizedRect)}");
        FocusRequested?.Invoke(normalizedCenter);
        return true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndViewportDrag(eventData != null ? eventData.position : default, "view");
    }

    internal void EndViewportDrag(Vector2 screenPosition, string source)
    {
        Debug.Log($"{DragLogTag} {source}PointerUp frame={Time.frameCount} view={name} pos={FormatVector2(screenPosition)} wasDragging={_draggingViewport} dragMoved={_dragMoved}");
        _draggingViewport = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"{DragLogTag} viewPointerClick frame={Time.frameCount} view={name} pos={FormatPointerPosition(eventData)} dragMoved={_dragMoved} dragging={_draggingViewport} openFullMap={_openFullMapOnClick} allowMapFocus={_allowMapFocus}");
        if (_dragMoved || _draggingViewport || viewportRect == null && !TryGetNormalizedPoint(eventData, out _))
            return;

        if (_openFullMapOnClick)
        {
            FullMapOpenRequested?.Invoke();
            return;
        }

        if (!_allowMapFocus)
            return;

        if (viewportRect != null && RectTransformUtility.RectangleContainsScreenPoint(viewportRect, eventData.position, eventData.pressEventCamera))
            return;

        if (TryGetNormalizedPoint(eventData, out Vector2 normalized))
            FocusRequested?.Invoke(normalized);
    }

    public void ClearManualViewportOverride()
    {
        _hasManualViewportOverride = false;
    }

    private bool TryGetNormalizedPoint(PointerEventData eventData, out Vector2 normalized)
    {
        normalized = default;
        if (!TryGetMapLocalPoint(eventData, out Vector2 localPoint))
            return false;

        Rect rect = MapRect.rect;
        normalized = new Vector2(
            Mathf.Clamp01((localPoint.x - rect.xMin) / Mathf.Max(0.001f, rect.width)),
            Mathf.Clamp01((localPoint.y - rect.yMin) / Mathf.Max(0.001f, rect.height)));
        return true;
    }

    private bool TryGetMapLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = default;
        RectTransform rectTransform = MapRect;
        return rectTransform != null &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
    }

    private bool ContainsViewportDragPoint(Vector2 screenPosition, Camera eventCamera)
    {
        if (viewportRect == null)
            return false;

        if (RectTransformUtility.RectangleContainsScreenPoint(viewportRect, screenPosition, eventCamera))
            return true;

        if (!TryGetPointerLocalPointInViewportParent(screenPosition, eventCamera, out Vector2 pointerInParent) ||
            !TryGetRectInParentSpace(viewportRect, viewportRect.parent as RectTransform, out Rect viewportInParent))
        {
            return false;
        }

        viewportInParent.xMin -= ViewportDragHitPadding;
        viewportInParent.xMax += ViewportDragHitPadding;
        viewportInParent.yMin -= ViewportDragHitPadding;
        viewportInParent.yMax += ViewportDragHitPadding;
        return viewportInParent.Contains(pointerInParent);
    }

    private bool TryGetPointerLocalPointInViewportParent(Vector2 screenPosition, Camera eventCamera, out Vector2 localPoint)
    {
        localPoint = default;
        RectTransform parent = viewportRect != null ? viewportRect.parent as RectTransform : null;
        return parent != null &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, eventCamera, out localPoint);
    }

    private bool TryGetRectInParentSpace(RectTransform source, RectTransform parent, out Rect rect)
    {
        rect = default;
        if (source == null || parent == null)
            return false;

        source.GetWorldCorners(_worldCorners);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(_worldCorners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (!IsFinite(min.x) || !IsFinite(min.y) ||
            !IsFinite(max.x) || !IsFinite(max.y) ||
            max.x <= min.x ||
            max.y <= min.y)
        {
            return false;
        }

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private void EnsureZoomRelay(Button button, int direction)
    {
        if (button == null)
            return;

        MatchHudMinimapZoomPressRelay relay = button.GetComponent<MatchHudMinimapZoomPressRelay>();
        if (relay == null)
            relay = button.gameObject.AddComponent<MatchHudMinimapZoomPressRelay>();

        relay.Configure(this, direction);
    }

    private void EnsureViewportDragRelay()
    {
        if (viewportRect == null)
            return;

        Graphic graphic = viewportRect.GetComponent<Graphic>();
        if (graphic != null)
            graphic.raycastTarget = true;

        MatchHudMinimapViewportDragRelay relay = viewportRect.GetComponent<MatchHudMinimapViewportDragRelay>();
        if (relay == null)
            relay = viewportRect.gameObject.AddComponent<MatchHudMinimapViewportDragRelay>();

        relay.Configure(this);
        Debug.Log($"{DragLogTag} ensureViewportDragRelay frame={Time.frameCount} view={name} viewport={viewportRect.name} graphicRaycastTarget={(graphic != null ? graphic.raycastTarget.ToString() : "none")} relay={relay.GetType().Name}");
    }

    internal void NotifyZoomHeld(int direction, bool held)
    {
        if (!_allowZoom)
            held = false;

        ZoomHeldChanged?.Invoke(direction, held);
    }

    private static void SetZoomVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private bool IsViewportActive()
    {
        return viewportRect != null && viewportRect.gameObject.activeInHierarchy;
    }

    private string ResolvePointerDownRejectReason(bool containsViewport)
    {
        if (!_allowViewportDrag)
            return "ViewportDragDisabled";
        if (viewportRect == null)
            return "MissingViewportRect";
        if (!viewportRect.gameObject.activeInHierarchy)
            return "ViewportInactive";
        if (!containsViewport)
            return "PointerOutsideViewport";
        return "Unknown";
    }

    private static string FormatPointerPosition(PointerEventData eventData)
    {
        return eventData != null ? FormatVector2(eventData.position) : "null";
    }

    private static string FormatVector2(Vector2 value)
    {
        return $"({value.x:0.###},{value.y:0.###})";
    }

    private static string FormatRect(Rect rect)
    {
        return $"(x:{rect.x:0.###},y:{rect.y:0.###},w:{rect.width:0.###},h:{rect.height:0.###})";
    }
}

public sealed class MatchHudMinimapZoomPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private MatchHudMinimapView _view;
    private int _direction;

    public void Configure(MatchHudMinimapView view, int direction)
    {
        _view = view;
        _direction = direction;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _view?.NotifyZoomHeld(_direction, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _view?.NotifyZoomHeld(_direction, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _view?.NotifyZoomHeld(_direction, false);
    }
}

public sealed class MatchHudMinimapViewportDragRelay : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    private MatchHudMinimapView _view;

    public void Configure(MatchHudMinimapView view)
    {
        _view = view;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[FullMapViewportDrag] relayPointerDown frame={Time.frameCount} relay={name} pos={(eventData != null ? eventData.position.ToString() : "null")} viewExists={_view != null}");
        _view?.OnPointerDown(eventData);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (eventData != null)
            eventData.useDragThreshold = false;
        _view?.OnInitializePotentialDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[FullMapViewportDrag] relayDrag frame={Time.frameCount} relay={name} pos={(eventData != null ? eventData.position.ToString() : "null")} viewExists={_view != null}");
        _view?.OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[FullMapViewportDrag] relayPointerUp frame={Time.frameCount} relay={name} pos={(eventData != null ? eventData.position.ToString() : "null")} viewExists={_view != null}");
        _view?.OnPointerUp(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _view?.OnPointerClick(eventData);
    }
}
