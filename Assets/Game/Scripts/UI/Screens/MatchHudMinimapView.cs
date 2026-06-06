using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchHudMinimapView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private RectTransform markerRoot;

    private bool _draggingViewport;
    private bool _dragMoved;
    private bool _hasManualViewportOverride;
    private Rect _manualViewportNormalizedRect;
    private Vector2 _viewportDragOffset;

    public Image MapImage => mapImage;
    public RectTransform MapRect => mapRect != null ? mapRect : mapImage != null ? mapImage.rectTransform : null;
    public RectTransform ViewportRect => viewportRect;
    public Button ZoomInButton => zoomInButton;
    public Button ZoomOutButton => zoomOutButton;
    public RectTransform MarkerRoot => markerRoot;
    public bool IsDraggingViewport => _draggingViewport;
    public bool HasManualViewportOverride => _hasManualViewportOverride;
    public Rect ManualViewportNormalizedRect => _manualViewportNormalizedRect;

    public event Action<Vector2> FocusRequested;
    public event Action<int, bool> ZoomHeldChanged;

    private void Awake()
    {
        EnsureZoomRelay(zoomInButton, 1);
        EnsureZoomRelay(zoomOutButton, -1);
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

    public void SetViewportNormalizedRect(Rect normalizedRect)
    {
        RectTransform rectTransform = MapRect;
        if (viewportRect == null || rectTransform == null)
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

        viewportRect.anchorMin = new Vector2(0f, 1f);
        viewportRect.anchorMax = new Vector2(0f, 1f);
        viewportRect.pivot = new Vector2(0f, 1f);
        viewportRect.anchoredPosition = new Vector2(left - parentTopLeft.x, top - parentTopLeft.y);
        viewportRect.sizeDelta = new Vector2(width, height);
    }

    private bool TryGetMapRectInViewportParent(RectTransform map, out Rect parentRect)
    {
        parentRect = default;
        RectTransform parent = viewportRect != null ? viewportRect.parent as RectTransform : null;
        if (parent == null)
            return false;

        Vector3[] corners = new Vector3[4];
        map.GetWorldCorners(corners);
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = parent.InverseTransformPoint(corners[i]);
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

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _dragMoved = false;
        _draggingViewport = false;

        if (viewportRect == null || !RectTransformUtility.RectangleContainsScreenPoint(viewportRect, eventData.position, eventData.pressEventCamera))
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            _viewportDragOffset = new Vector2(
                localPoint.x - viewportRect.rect.xMin,
                viewportRect.rect.yMax - localPoint.y);
        }

        _draggingViewport = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_draggingViewport || !TryGetMapLocalPoint(eventData, out Vector2 localPoint))
            return;

        _dragMoved = true;
        RectTransform rectTransform = MapRect;
        Rect map = rectTransform.rect;
        float rectWidth = Mathf.Max(6f, viewportRect.rect.width);
        float rectHeight = Mathf.Max(6f, viewportRect.rect.height);
        float localX = localPoint.x - map.xMin;
        float localY = map.yMax - localPoint.y;
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
        FocusRequested?.Invoke(normalizedCenter);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _draggingViewport = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_dragMoved || _draggingViewport || viewportRect == null && !TryGetNormalizedPoint(eventData, out _))
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

    private void EnsureZoomRelay(Button button, int direction)
    {
        if (button == null)
            return;

        MatchHudMinimapZoomPressRelay relay = button.GetComponent<MatchHudMinimapZoomPressRelay>();
        if (relay == null)
            relay = button.gameObject.AddComponent<MatchHudMinimapZoomPressRelay>();

        relay.Configure(this, direction);
    }

    internal void NotifyZoomHeld(int direction, bool held)
    {
        ZoomHeldChanged?.Invoke(direction, held);
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
